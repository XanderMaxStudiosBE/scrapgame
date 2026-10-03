"""Original compact surface pixels, references and repeat policies; no Unity rendering."""
import hashlib
import json
from pathlib import Path
import re
import struct
import sys
import zlib

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'ArtSource'))
from texture_metadata import metadata_kind

folder=ROOT/'Assets/Scrapshift/Resources/ScrapshiftWorld'
manifest=json.loads((folder/'compact_ground_manifest.json').read_text())


def rgba(path):
    data=path.read_bytes()
    assert data[:8]==b'\x89PNG\r\n\x1a\n'
    position=8;compressed=b'';size=None
    while position<len(data):
        length=struct.unpack_from('>I',data,position)[0]
        tag=data[position+4:position+8];payload=data[position+8:position+8+length]
        assert zlib.crc32(tag+payload)&0xffffffff==struct.unpack_from('>I',data,position+8+length)[0]
        if tag==b'IHDR':
            w,h,depth,channels,compression,filtering,interlace=struct.unpack('>IIBBBBB',payload)
            assert (depth,channels,compression,filtering,interlace)==(8,6,0,0,0)
            size=(w,h)
        if tag==b'IDAT':compressed+=payload
        position+=12+length
    assert size==(256,256)
    raw=zlib.decompress(compressed);rows=[];stride=1+256*4
    assert len(raw)==stride*256
    for y in range(256):
        assert raw[y*stride]==0
        rows.append([tuple(raw[y*stride+1+x*4:y*stride+5+x*4]) for x in range(256)])
    return data,rows


for name in ('PackedGravel','WheelLane'):
    data,pixels=rgba(folder/(name+'.png'))
    assert hashlib.sha256(data).hexdigest()==manifest['textures'][name]['sha256']
    metadata=(folder/(name+'.png.meta')).read_text()
    kind,guid=metadata_kind(metadata);assert kind=='importer'
    assert re.search(r'^  textureType: 0$',metadata,re.M) and re.search(r'^  textureShape: 1$',metadata,re.M)
    for axis in ('U','V'):assert re.search(r'^    wrap'+axis+r': 0$',metadata,re.M)
    assert 'enableMipMap: 1' in metadata and 'sRGBTexture: 1' in metadata
    if name=='PackedGravel':
        assert all(p[3]==255 for row in pixels for p in row)
        assert len({p[:3] for row in pixels for p in row})>80
        assert all(max(p[:3])<210 for row in pixels for p in row),'No white clipping in authored gravel'
    else:
        assert all(p[3]==0 for p in pixels[0]+pixels[-1])
        assert all(row[0][3]==row[-1][3]==0 for row in pixels)
        interior=[p[3] for row in pixels[80:176] for p in row[80:176]]
        assert min(interior)>0 and max(interior)<190,'Feathered straight alpha lane must not fill white'
    print('PASS',name,'RGBA/CRC/hash/typed import/repeat/mipmap/alpha policy')

policy=(ROOT/'Assets/Scrapshift/Editor/WorldSurfaceImport.cs').read_text()
assert 'PackedGravel.png' in policy and 'WheelLane.png' in policy and 'TextureWrapMode.Repeat' in policy
runtime=(ROOT/'Assets/Scrapshift/Runtime/CompactGroundSurface.cs').read_text()
assert 'new Material(original)' in runtime and 'SetTexture("_MainTex",map)' in runtime and 'SetTexture("_BaseMap",map)' in runtime
print('PASS additive import names and private runtime material/legacy texture slots')
print('Scope: pixels/packaging/source contracts; actual Unity import/material lifetime/rendering remain local')
