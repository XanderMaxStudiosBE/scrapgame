"""Explicit Unity texture metadata, preserving existing GUIDs and custom import settings."""
from pathlib import Path
import re
import uuid


def metadata_kind(source):
    """Classify supported metadata; reject malformed input instead of silently replacing it."""
    guids = re.findall(r'^guid: ([0-9a-fA-F]{32})\s*$', source, re.M)
    if len(guids) != 1 or len(re.findall(r'^guid:', source, re.M)) != 1:
        raise ValueError('Texture metadata requires exactly one valid 32-character GUID')
    if not re.search(r'^fileFormatVersion: 2\s*$', source, re.M):
        raise ValueError('Unsupported texture metadata file format')
    lines = [line.strip() for line in source.splitlines() if line.strip() and not line.lstrip().startswith('#')]
    if all(line.startswith(('fileFormatVersion:', 'guid:')) for line in lines):
        return 'bare', guids[0]
    if not re.search(r'^TextureImporter:\s*$', source, re.M):
        raise ValueError('PNG metadata has no TextureImporter declaration')
    for field in ('serializedVersion', 'textureType', 'textureShape'):
        if not re.search(r'^  ' + field + r': \d+\s*$', source, re.M):
            raise ValueError('TextureImporter metadata lacks ' + field)
    return 'importer', guids[0]


def importer_metadata(guid, *, linear=False, repeat=False, max_size=512, alpha_transparency=False):
    if not re.fullmatch(r'[0-9a-fA-F]{32}', guid):
        raise ValueError('Invalid texture GUID')
    if max_size < 32 or max_size > 16384 or max_size & (max_size - 1):
        raise ValueError('Texture maximum size must be a supported power of two (32–16384)')
    return f'''fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: {0 if linear else 1}
    linearTexture: 0
  isReadable: 0
  textureFormat: 1
  maxTextureSize: {max_size}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 2
    mipBias: 0
    wrapU: {0 if repeat else 1}
    wrapV: {0 if repeat else 1}
    wrapW: {0 if repeat else 1}
  nPOTScale: 0
  compressionQuality: 50
  spriteMode: 0
  alphaSource: 1
  alphaIsTransparency: {1 if alpha_transparency else 0}
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  userData:
  assetBundleName:
  assetBundleVariant:
'''


def ensure_texture_metadata(path, *, linear=False, repeat=False, max_size=512, alpha_transparency=False):
    path = Path(path)
    if path.suffix.lower() != '.png':
        raise ValueError('This helper only handles PNG assets')
    metadata = Path(str(path) + '.meta')
    if metadata.exists():
        kind, guid = metadata_kind(metadata.read_text())
        if kind == 'importer':
            return guid  # Preserve complete/custom importer bytes, including platform overrides.
    else:
        guid = uuid.uuid4().hex
    metadata.write_text(importer_metadata(guid, linear=linear, repeat=repeat, max_size=max_size, alpha_transparency=alpha_transparency))
    return guid
