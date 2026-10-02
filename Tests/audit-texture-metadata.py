#!/usr/bin/env python3
"""Authoring/import metadata checks, not Unity import or rendering evidence."""
import ast
from pathlib import Path
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'ArtSource'))
from texture_metadata import ensure_texture_metadata, importer_metadata, metadata_kind

GUID = '0123456789abcdef0123456789abcdef'
ASSETS = ROOT / 'Assets/Scrapshift/Resources'
EXPECTED = {
    'ScrapshiftProps/ScrapshiftPropAtlas.png': '2aae9f63e35c48059df30872f60b10ba',
    'ScrapshiftWorld/WorldAtlas.png': 'c76c7be327654ceea12c4690e5376372',
    'ScrapshiftWorld/WorldMetalGloss.png': 'cfafe8f4344a4042a129211403e40f32',
    'ScrapshiftWorld/WorldGlow.png': '7f0a2f58603e4c45b4b5f74a02cc068e',
    'ScrapshiftWorld/GroundLayers.png': '0132aa6e8c8d4f31a7eb52d838e11004',
    'ScrapshiftWorld/ChainLink.png': 'be425c347335451ba551e9296bcf8380',
}


class TextureMetadataTests(unittest.TestCase):
    def test_bare_metadata_upgrades_without_changing_guid_or_source_pixels(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'Art.png'
            path.write_bytes(b'unchanged source bytes')
            meta = Path(str(path) + '.meta')
            meta.write_text('fileFormatVersion: 2\nguid: ' + GUID + '\n')
            self.assertEqual(('bare', GUID), metadata_kind(meta.read_text()))
            self.assertEqual(GUID, ensure_texture_metadata(path))
            self.assertEqual(('importer', GUID), metadata_kind(meta.read_text()))
            self.assertEqual(b'unchanged source bytes', path.read_bytes())

    def test_custom_import_settings_and_unknown_fields_are_byte_preserved(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'Custom.png'
            source = importer_metadata(GUID).replace('filterMode: 1', 'filterMode: 0') + '  customFutureProperty: 987\n'
            meta = Path(str(path) + '.meta'); meta.write_text(source)
            self.assertEqual(GUID, ensure_texture_metadata(path, linear=True, repeat=True, max_size=64))
            self.assertEqual(source, meta.read_text())

    def test_malformed_metadata_is_rejected_and_not_overwritten(self):
        samples = [
            '', 'fileFormatVersion: 2\nguid: broken\n',
            'fileFormatVersion: 1\nguid: ' + GUID + '\n',
            'fileFormatVersion: 2\nguid: ' + GUID + '\nguid: ' + GUID + '\n',
            'fileFormatVersion: 2\nguid: ' + GUID + '\nDefaultImporter: {}\n',
            'fileFormatVersion: 2\nguid: ' + GUID + '\nTextureImporter:\n  externalObjects: {}\n',
        ]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'Broken.png'; meta = Path(str(path) + '.meta')
            for source in samples:
                with self.subTest(source=source):
                    meta.write_text(source)
                    with self.assertRaises(ValueError): ensure_texture_metadata(path)
                    self.assertEqual(source, meta.read_text())

    def test_new_metadata_is_stable_and_other_asset_types_are_protected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'New.png'
            first = ensure_texture_metadata(path)
            self.assertEqual(32, len(first))
            self.assertEqual(first, ensure_texture_metadata(path))
            with self.assertRaises(ValueError): ensure_texture_metadata(Path(directory) / 'Model.fbx')
            self.assertFalse((Path(directory) / 'Model.fbx.meta').exists())

    def test_world_png_authoring_emits_metadata_even_when_materials_already_exist(self):
        # Execute the real PNG writer in isolation, without regenerating any production art.
        import struct, zlib, hashlib
        tree = ast.parse((ROOT / 'ArtSource/build_world_surfaces.py').read_text())
        function = next(node for node in tree.body if isinstance(node, ast.FunctionDef) and node.name == 'png')
        with tempfile.TemporaryDirectory() as directory:
            scope = dict(OUT=Path(directory), manifest={}, struct=struct, zlib=zlib, hashlib=hashlib,
                         ensure_texture_metadata=ensure_texture_metadata)
            exec(compile(ast.Module(body=[function], type_ignores=[]), '<isolated PNG authoring>', 'exec'), scope)
            path = scope['png']('WorldMetalGloss', 2, lambda x, y: (120, 0, 0, 65))
            self.assertTrue(path.read_bytes().startswith(b'\x89PNG\r\n\x1a\n'))
            source = Path(str(path) + '.meta').read_text()
            self.assertEqual('importer', metadata_kind(source)[0])
            self.assertIn('sRGBTexture: 0', source)
            self.assertIn('alphaSource: 1', source)
            self.assertIn('maxTextureSize: 32', source)
        yard_tree = ast.parse((ROOT / 'ArtSource/build_yard_assets.py').read_text())
        prepare = next(node for node in yard_tree.body if isinstance(node, ast.FunctionDef) and node.name == 'prepare_materials')
        self.assertTrue(any(isinstance(node, ast.Call) and isinstance(node.func, ast.Name) and
                            node.func.id == 'ensure_texture_metadata' for node in ast.walk(prepare)))

    def test_tracked_texture_importers_preserve_existing_guids_and_alpha(self):
        for name, expected in EXPECTED.items():
            with self.subTest(name=name):
                source = Path(str(ASSETS / name) + '.meta').read_text()
                self.assertEqual(('importer', expected), metadata_kind(source))
                self.assertIn('textureType: 0', source); self.assertIn('textureShape: 1', source)
                self.assertIn('alphaSource: 1', source)
                self.assertIn('alphaIsTransparency: ' + ('1' if 'ScrapshiftWorld/' in name and 'WorldMetalGloss' not in name else '0'), source)
                self.assertIn('textureCompression: 0', source)
                self.assertIn('sRGBTexture: ' + ('0' if 'WorldMetalGloss' in name else '1'), source)


if __name__ == '__main__':
    unittest.main()
