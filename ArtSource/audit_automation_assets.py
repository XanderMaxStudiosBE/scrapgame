"""Blender FBX round-trip for original automation models, not a Unity import check."""
import bpy,json,math,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];FOLDER=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((FOLDER/'automation_asset_manifest.json').read_text());results=[]
for name,expected in manifest['assets'].items():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(FOLDER/(name+'.fbx')))
    models=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(models)==1,name
    vertices=[o.matrix_world@v.co for o in models for v in o.data.vertices]
    assert all(math.isfinite(c) for v in vertices for c in v),name
    dimensions=[max(v[a] for v in vertices)-min(v[a] for v in vertices) for a in range(3)]
    assert all(abs(a-b)<.025 for a,b in zip(dimensions,expected['dimensions_blender'])),(name,dimensions)
    triangles=sum(len(p.vertices)-2 for o in models for p in o.data.polygons)
    assert triangles==expected['triangles']<=2500,(name,triangles)
    for o in models:
        assert len(o.data.materials)==1 and o.data.uv_layers.active,name
        assert all(math.isfinite(c) and 0<=c<=1 for uv in o.data.uv_layers.active.data for c in uv.uv),name
        assert all(math.isfinite(c) for p in o.data.polygons for c in p.normal),name
    results.append({'name':name,'status':'passed','triangles':triangles,'metre_dimensions':dimensions,'sha256':hashlib.sha256((FOLDER/(name+'.fbx')).read_bytes()).hexdigest()})
report={'scope':'Blender FBX round-trip, metre bounds, finite normals/vertices, shared-atlas UVs, one material, 2500-triangle budget; Unity unrun','assets':results}
(FOLDER/'automation_asset_audit.json').write_text(json.dumps(report,indent=2)+'\n');print('SCRAPSHIFT_AUTOMATION_AUDIT '+json.dumps(report))
