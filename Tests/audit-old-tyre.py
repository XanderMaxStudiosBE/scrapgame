"""Verify CC0 provenance, source/mesh hashes, URP slots and linear mask conversion.

This packaging audit does not execute Unity's importer, renderer or static batching.
"""
import hashlib
import json
from pathlib import Path
import re
from PIL import Image, ImageChops, ImageOps

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/ThirdParty/PolyHaven/OldTyre'
OUT = ROOT / 'Assets/Scrapshift/Resources/ThirdParty/PolyHaven/OldTyre'
provenance = json.loads((SOURCE / 'sources.json').read_text())
assert provenance['license'] == 'CC0-1.0'
assert provenance['author'] == 'MP'
assert provenance['page'] == 'https://polyhaven.com/a/old_tyre'
paths = {'source.fbx': SOURCE / 'source.fbx', 'Roughness.png': SOURCE / 'Roughness.png',
         'Albedo.png': OUT / 'Albedo.png', 'Normal.png': OUT / 'Normal.png'}
for name, path in paths.items():
    data = path.read_bytes()
    expected = provenance['publisherFiles'][name]
    assert len(data) == expected['size'], name
    assert hashlib.md5(data).hexdigest() == expected['md5'], name
    assert hashlib.sha256(data).hexdigest() == expected['sha256'], name
print('PASS four original files match publisher checksums and retained SHA-256 provenance')

record = json.loads((SOURCE / 'mesh_audit.json').read_text())
fbx = (OUT / 'OldTyre.fbx').read_bytes()
assert fbx.startswith(b'Kaydara FBX Binary')
assert hashlib.sha256(fbx).hexdigest() == record['sha256']
assert record['sourceTriangles'] == 2880 and record['triangles'] == 184
assert all(abs(a - b) < .00001 for a, b in zip(record['dimensionsMetresBlender'], (.6, .6, .165)))
print('PASS exported FBX hash matches 184-triangle metre-scale Blender round-trip audit')

mask = Image.open(OUT / 'MetalSmoothness.png')
rough = Image.open(SOURCE / 'Roughness.png').convert('L')
assert mask.size == (1024, 1024) and mask.mode == 'RGBA'
for channel in mask.split()[:3]: assert channel.getextrema() == (0, 0), 'Rubber is nonmetallic'
assert ImageChops.difference(mask.getchannel('A'), ImageOps.invert(rough)).getbbox() is None
assert 0 <= mask.getchannel('A').getextrema()[0] < mask.getchannel('A').getextrema()[1] < 160
print('PASS every mask pixel is nonmetallic with alpha equal to inverse roughness')

material = (OUT / 'OldTyre.mat').read_text()
for name, prop, linear, texture_type in [('Albedo', '_BaseMap', False, 0), ('Normal', '_BumpMap', True, 1), ('MetalSmoothness', '_MetallicGlossMap', True, 0)]:
    image = Image.open(OUT / (name + '.png'))
    assert image.size == (1024, 1024)
    metadata = (OUT / (name + '.png.meta')).read_text()
    guid = re.search(r'^guid: ([0-9a-f]{32})$', metadata, re.M)[1]
    assert '    sRGBTexture: ' + ('0' if linear else '1') in metadata
    assert '  textureType: ' + str(texture_type) in metadata
    assert '  maxTextureSize: 1024' in metadata and '    enableMipMap: 1' in metadata
    assert re.search(r'    - ' + prop + r':\n        m_Texture: \{fileID: 2800000, guid: ' + guid + r', type: 3\}', material)
assert '_NORMALMAP' in material and '_METALLICSPECGLOSSMAP' in material
assert '    - _BaseMap:' in material and '    - _MainTex:' in material
assert '    - _Color: {r: 1, g: 1, b: 1, a: 1}' in material
assert '    - _Glossiness: 1' in material and '    - _GlossMapScale: 1' in material
assert 'guid: 933532a4fcc9baf4fa0491de14d08ed7' in material
print('PASS 1K/mipmap/linear normal and smoothness import policy, URP texture links and legacy aliases')
print('Scope: source and packaging; Unity import/axes/tangents/material rendering/FPS remain local checks')
