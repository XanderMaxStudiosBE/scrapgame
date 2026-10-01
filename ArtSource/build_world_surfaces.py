"""Original world-dressing surfaces; standard-library PNG writer, no external image assets.
Run python3 ArtSource/build_world_surfaces.py. Only this new pack's textures are regenerated.
Existing material files and all legacy textures are preserved.
"""
from material_compatibility import migration_safe
from pathlib import Path
import math,random,struct,zlib,json,hashlib,uuid
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Scrapshift/Resources/ScrapshiftWorld';OUT.mkdir(parents=True,exist_ok=True)
COLORS=[(112,128,102),(105,137,141),(203,198,179),(144,87,57),(91,99,96),(39,42,40),(151,127,90),(68,94,100),(215,172,98),(180,112,67),(81,105,71),(115,137,91),(184,142,74),(90,75,55),(224,218,197),(127,88,65)]
manifest={}
def png(name,size,paint):
    # Paint uses bottom-left coordinates, matching the original atlas's UV convention.
    rows=[]
    for y in reversed(range(size)):
        row=bytearray([0])
        for x in range(size):row.extend(max(0,min(255,round(v))) for v in paint(x,y))
        rows.append(bytes(row))
    def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
    raw=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',size,size,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(b''.join(rows),9))+chunk(b'IEND',b'')
    path=OUT/(name+'.png');path.write_bytes(raw)
    manifest[name]={'size':size,'sha256':hashlib.sha256(raw).hexdigest()}
    return path

def noise(x,y,seed=41):return ((x*73856093^y*19349663^seed*83492791)&65535)/65535

def atlas(x,y):
    tile=x//128+(y//128)*4;u=x%128;v=y%128;base=COLORS[tile]
    variation=(noise(x,y)-.5)*11+math.sin((u+v*.35)*.10)*2
    if tile in (6,13):variation+=math.sin(u*.36+math.sin(v*.024)*1.8)*8-(9 if u%31<2 else 0)
    if tile in (0,1,2,12,14):
        # Deliberate edge chips instead of a uniform orange coating.
        edge=min(u,v,127-u,127-v)
        if edge<18 and noise(u,v,87)>.82:base=(125,94,68)
        variation-=max(0,(14-v)/14)*8
    if tile==15:
        row=v//16;shift=(row%2)*16;seam=v%16<2 or (u+shift)%32<2
        base=(87,84,71) if seam else tuple(c+(noise((u+shift)//32,row,31)-.5)*16 for c in base)
    return (*[c+variation for c in base],255)

png('WorldAtlas',512,atlas)
metals=[.08,.08,.08,.18,.6,0,0,.05,0,.72,0,0,.10,0,.03,0]
smooth=[.17,.20,.16,.12,.28,.06,.08,.35,.3,.38,.03,.03,.12,.06,.13,.03]
png('WorldMetalGloss',512,lambda x,y:(metals[x//128+(y//128)*4]*255,0,0,smooth[x//128+(y//128)*4]*255))
png('WorldGlow',64,lambda x,y:(145,95,37,255) if x//16+(y//16)*4==8 else (0,0,0,255))

def detail(x,y):
    tile=x//256+(y//256)*2;u=(x%256)/255*2-1;v=(y%256)/255*2-1
    angle=math.atan2(v,u);radius=math.hypot(u,v)
    edge=max(0,min(1,(1+.05*math.sin(angle*7)+.08*math.sin(angle*11)-radius)/.20))
    n=noise(x,y,27);grain=(n-.5)*27
    if tile==0:rgb=(128+grain,117+grain,94+grain);alpha=edge*(.35+n*.34)
    elif tile==1:
        tread=abs(u)<.70 and abs(u)>.24 and (int((v+1)*24)+int(abs(u)*7))%3!=0
        rgb=(81+grain*.35,79+grain*.35,68+grain*.35);alpha=edge*(.48 if tread else .09)
    elif tile==2:
        rgb=(111+grain*.12,122+grain*.16,115+grain*.1);alpha=edge*(.66+math.sin(u*5+v*9)*.08)
    else:rgb=(101+grain,106+grain,78+grain*.7);alpha=edge*(.18+n*.36)
    return (*rgb,alpha*255)
png('GroundLayers',512,detail)

def fence(x,y):
    # Four crossed wire diamonds per repeat; the gaps remain actual cutout holes.
    a=(x+y)%64;b=(x-y)%64;wire=min(a,64-a,b,64-b)<2
    rusty=noise(x//8,y//8,19)>.9
    return (*((122,94,69) if rusty else (128,136,128)),255 if wire else 0)
png('ChainLink',256,fence)

def meta(path):
    p=Path(str(path)+'.meta')
    if not p.exists():p.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
def guid(path):
    import re
    meta(path);return re.search(r'^guid: (\w+)$',Path(str(path)+'.meta').read_text(),re.M)[1]
shader='933532a4fcc9baf4fa0491de14d08ed7'
template=(ROOT/'Assets/Scrapshift/Resources/ScrapshiftMaterials/PropAtlas.mat').read_text()
def material(name,texture,keywords=(),metallic=0,smoothness=.08,transparent=False,clip=False,emission=False):
    path=OUT/(name+'.mat')
    if path.exists():return # Preserve customized pack materials on authoring reruns.
    s=template.replace('m_Name: PropAtlas','m_Name: '+name).replace('guid: 2aae9f63e35c48059df30872f60b10ba','guid: '+guid(OUT/(texture+'.png')))
    s=s.replace('m_ValidKeywords: []','m_ValidKeywords:'+''.join('\n  - '+k for k in keywords) if keywords else 'm_ValidKeywords: []')
    s=s.replace('- _Smoothness: 0.08','- _Smoothness: '+str(smoothness)).replace('- _Metallic: 0','- _Metallic: '+str(metallic)).replace('- _SpecularHighlights: 0','- _SpecularHighlights: 1')
    if emission:
        extra=''
        for prop,tex in [('_MetallicGlossMap','WorldMetalGloss'),('_EmissionMap','WorldGlow')]:
            extra+='    - '+prop+':\n        m_Texture: {fileID: 2800000, guid: '+guid(OUT/(tex+'.png'))+', type: 3}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n'
        s=s.replace('    m_Ints: []',extra+'    m_Ints: []').replace('_EmissionColor: {r: 0, g: 0, b: 0, a: 1}','_EmissionColor: {r: 1, g: 1, b: 1, a: 1}')
    if clip:s=s.replace('- _AlphaClip: 0','- _AlphaClip: 1').replace('- _Cull: 2','- _Cull: 0').replace('m_CustomRenderQueue: -1','m_CustomRenderQueue: 2450').replace('    - _Blend: 0','    - _Cutoff: 0.5\n    - _Blend: 0')
    if transparent:
        s=s.replace('- _Surface: 0','- _Surface: 1').replace('- _SrcBlend: 1','- _SrcBlend: 5').replace('- _DstBlend: 0','- _DstBlend: 10').replace('- _ZWrite: 1','- _ZWrite: 0').replace('m_CustomRenderQueue: -1','m_CustomRenderQueue: 3000').replace('stringTagMap: {}','stringTagMap:\n    RenderType: Transparent').replace('disabledShaderPasses: []','disabledShaderPasses:\n  - ShadowCaster\n  - DepthOnly')
    path.write_text(migration_safe(s));meta(path)
material('WorldProps','WorldAtlas',('_METALLICSPECGLOSSMAP','_EMISSION'),1,1,emission=True)
material('GroundWear','GroundLayers',('_SURFACE_TYPE_TRANSPARENT',),smoothness=.04,transparent=True)
material('RoughPuddles','GroundLayers',('_SURFACE_TYPE_TRANSPARENT',),smoothness=.42,transparent=True)
material('WireFence','ChainLink',('_ALPHATEST_ON',),metallic=.35,smoothness=.16,clip=True)
(OUT/'surface_manifest.json').write_text(json.dumps({'source':'Original procedural world surfaces: ArtSource/build_world_surfaces.py','scope':'PNG data and authored material links; not Unity shader/rendering evidence','textures':manifest},indent=2))
print('WORLD_SURFACES',json.dumps(manifest))
