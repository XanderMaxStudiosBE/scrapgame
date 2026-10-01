"""Build direct original-material texture references; never edits source materials.
Run python3 ArtSource/build_material_catalog.py. Existing metadata/GUIDs are retained.
"""
from pathlib import Path
import uuid,re
root=Path(__file__).resolve().parents[1];asset=root/'Assets/Scrapshift';folder=asset/'Resources/ScrapshiftRendering';folder.mkdir(exist_ok=True)
def guid(p):
    m=Path(str(p)+'.meta')
    if not m.exists():m.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+('folderAsset: yes\n' if p.is_dir() else ''))
    return re.search(r'^guid: (\w+)$',m.read_text(),re.M)[1]
script=guid(asset/'Runtime/YardMaterialCatalog.cs')
s='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+script+', type: 3}\n  m_Name: Materials\n  m_EditorClassIdentifier:\n  entries:\n'
entries=[]
for name in ['RustPaint','DarkMetal','CorrugatedMetal','WeatheredWood','Gravel','Copper','WireInsulation']:
    entries.append(('ScrapshiftMaterials/'+name,asset/('Resources/ScrapshiftMaterials/'+name+'.mat'),asset/('Art/Textures/'+name+'.png'),None,None))
entries.append(('ScrapshiftMaterials/PropAtlas',asset/'Resources/ScrapshiftMaterials/PropAtlas.mat',asset/'Resources/ScrapshiftProps/ScrapshiftPropAtlas.png',None,None))
for name,tex in [('WorldProps','WorldAtlas'),('GroundWear','GroundLayers'),('RoughPuddles','GroundLayers'),('WireFence','ChainLink')]:
    entries.append(('ScrapshiftWorld/'+name,asset/('Resources/ScrapshiftWorld/'+name+'.mat'),asset/('Resources/ScrapshiftWorld/'+tex+'.png'),asset/'Resources/ScrapshiftWorld/WorldMetalGloss.png' if name=='WorldProps' else None,asset/'Resources/ScrapshiftWorld/WorldGlow.png' if name=='WorldProps' else None))
for resource,mat,tex,mask,glow in entries:
    s+='  - resource: '+resource+'\n    material: {fileID: 2100000, guid: '+guid(mat)+', type: 2}\n    albedo: {fileID: 2800000, guid: '+guid(tex)+', type: 3}\n'
    for prop,p in [('metallicGloss',mask),('emission',glow)]:s+='    '+prop+': '+('{fileID: 2800000, guid: '+guid(p)+', type: 3}' if p else '{fileID: 0}')+'\n'
    s+='    straightAlpha: '+('1' if resource.endswith('/GroundWear') or resource.endswith('/RoughPuddles') else '0')+'\n'
p=folder/'Materials.asset';p.write_text(s);guid(p);guid(folder)
print('MATERIAL_CATALOG: 12 independent texture bindings, preserved existing GUIDs')
