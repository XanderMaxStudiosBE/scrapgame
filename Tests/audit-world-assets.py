#!/usr/bin/env python3
"""Check actual additive asset data, material links and visible lane clearance; not Unity collision/rendering."""
from pathlib import Path
import json,re,math,hashlib,struct,zlib
ROOT=Path(__file__).resolve().parents[1];WORLD=ROOT/'Assets/Scrapshift/Resources/ScrapshiftWorld';PROPS=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((PROPS/'asset_manifest.json').read_text())['assets'];pack={k:v for k,v in manifest.items() if v.get('pack')=='ScrapshiftWorld'}
assert len(pack)==12
for name,record in pack.items():assert 0<record['triangles']<=5000 and record['materials']==1,name
print('PASS 12 original world models, single shared material and triangle budgets')
surfaces=json.loads((WORLD/'surface_manifest.json').read_text())['textures'];pixels={}
for name,record in surfaces.items():
    raw=(WORLD/(name+'.png')).read_bytes();assert hashlib.sha256(raw).hexdigest()==record['sha256'],name+' changed since authored manifest'
    width,height,depth,color=struct.unpack('>IIBB',raw[16:26]);assert (width,height)==(record['size'],)*2 and depth==8 and color==6
    offset=8;compressed=bytearray()
    while offset<len(raw):
        length=struct.unpack('>I',raw[offset:offset+4])[0];kind=raw[offset+4:offset+8];data=raw[offset+8:offset+8+length]
        assert zlib.crc32(kind+data)&0xffffffff==struct.unpack('>I',raw[offset+8+length:offset+12+length])[0],name+' bad PNG CRC'
        if kind==b'IDAT':compressed.extend(data)
        offset+=length+12
    scan=zlib.decompress(compressed);stride=1+width*4;assert len(scan)==stride*height
    assert all(scan[y*stride]==0 for y in range(height))
    pixels[name]=(width,b''.join(scan[y*stride+1:(y+1)*stride] for y in reversed(range(height))))
def rgba(name,x,y):
    size,raw=pixels[name];index=(y*size+x)*4;return raw[index:index+4]
for tile in [5,6,10,11,13,15]:assert rgba('WorldMetalGloss',tile%4*128+64,tile//4*128+64)[0]==0,'Nonmetal tile is metallic'
assert rgba('WorldMetalGloss',64,192)[3]<100,'Metal smoothness too glossy'
assert rgba('WorldGlow',8,40)[:3]!=b'\0\0\0' and rgba('WorldGlow',8,8)[:3]==b'\0\0\0','Emission must stay in warm-glass tile'
alpha=pixels['ChainLink'][1][3::4];assert .03<sum(a>127 for a in alpha)/len(alpha)<.25,'Wire holes must remain open'
assert len(set(pixels['GroundLayers'][1][3::4]))>32,'Ground edges lack an irregular fade'
print('PASS 5 PNG hashes/CRCs/dimensions, localized emission, metal/gloss masks, fence holes and ground alpha fades')
for mat,textures in [('WorldProps',['WorldAtlas','WorldMetalGloss','WorldGlow']),('GroundWear',['GroundLayers']),('RoughPuddles',['GroundLayers']),('WireFence',['ChainLink'])]:
    source=(WORLD/(mat+'.mat')).read_text();assert 'guid: 933532a4fcc9baf4fa0491de14d08ed7' in source
    for tex in textures:
        guid=re.search(r'^guid: (\w+)$',(WORLD/(tex+'.png.meta')).read_text(),re.M)[1];assert 'guid: '+guid in source,(mat,tex)
assert '_ALPHATEST_ON' in (WORLD/'WireFence.mat').read_text()
for name in ['GroundWear','RoughPuddles']:assert '_ZWrite: 0' in (WORLD/(name+'.mat')).read_text()
print('PASS URP material links, cutout fence and transparent ground depth settings')
nav=(ROOT/'Assets/Scrapshift/Core/YardNavigation.cs').read_text();enum=re.search(r'enum YardLandmark \{([^}]+)',nav)[1].split(',');ids={name.strip():i for i,name in enumerate(enum)}
anchors={ids[name]:(float(x),float(z)) for name,x,z in re.findall(r'new YardDestination\(YardLandmark\.(\w+),"[^"]+",(-?[\d.]+)f?,(-?[\d.]+)f?\)',nav)}
layout=json.loads((WORLD/'WorldDressing.json').read_text());assert layout['version']==1 and 0<len(layout['props'])<=128
routes=[(0,z) for z in range(-36,26)]+[(x,-13) for x in range(-44,45)]+[(-42,z) for z in range(-13,20)]+[(x,17) for x in range(-42,-32)]+[(31,z) for z in range(-20,-12)]+[(-30,z) for z in range(-13,-7)]
supplies=[(-33,-20.8),(33,-20.8),(-30,-7.8),(-33,17.2),(31,-23.8)]
for p in layout['props']:
    assert p['model'] in pack
    assert isinstance(p.get('solid',False),bool) and (not p.get('solid') or p['model'] in ['ApplianceRow','SalvageShelter'])
    assert p['district'] in ['Workshop surroundings','Restoration surroundings','North loading district','West vehicle salvage','East metal sorting','South entry district','Distant landscape']
    assert all(math.isfinite(p[k]) for k in ['x','y','z','yaw','sx','sy','sz']) and all(.1<=p[k]<=3 for k in ['sx','sy','sz'])
    ax,az=anchors[p['anchor']] if p['anchor'] else (0,0);x=p['x']+ax;z=p['z']+az
    assert abs(x)<=90 and abs(z)<=90
    if p['model'] not in ['ApplianceRow','SalvageShelter']:continue
    w,d,h=pack[p['model']]['dimensions_blender'];rx=w*p['sx']/2;rz=d*p['sz']/2;c=math.cos(math.radians(p['yaw']));s=math.sin(math.radians(p['yaw']))
    for px,pz in routes+supplies:
        dx=px-x;dz=pz-z;u=c*dx-s*dz;v=s*dx+c*dz
        distance=math.hypot(max(0,abs(u)-rx),max(0,abs(v)-rz))
        assert distance>.30,(p['model'],'visible dressing crosses protected route',(px,pz),p)
print('PASS',len(layout['props']),'bounded placements, authoritative station anchors and appliance/shelter clearance along protected lanes/supply approaches (not engine collision)')
