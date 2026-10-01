"""Audit the exported FBX files through Blender's importer (not a Unity import test)."""
import bpy, json, math, hashlib
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
folder=root/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((folder/'asset_manifest.json').read_text())
results=[]
for name,expected in manifest['assets'].items():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(folder/(name+'.fbx')))
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert objects, name+' has no mesh'
    verts=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
    assert all(math.isfinite(c) for v in verts for c in v),name+' non-finite vertex'
    dimensions=[max(v[a] for v in verts)-min(v[a] for v in verts) for a in range(3)]
    for actual,wanted in zip(dimensions,expected['dimensions_blender']):
        assert abs(actual-wanted)<.025,(name,dimensions,expected['dimensions_blender'])
    triangles=sum(max(0,len(p.vertices)-2) for o in objects for p in o.data.polygons)
    assert triangles==expected['triangles'],(name,triangles,expected['triangles'])
    assert triangles<=5000,name+' exceeds per-prop budget'
    for o in objects:
        assert o.data.uv_layers.active,name+' missing UV map'
        for uv in o.data.uv_layers.active.data:
            assert all(math.isfinite(v) and 0<=v<=1 for v in uv.uv),name+' UV outside atlas'
    results.append({'name':name,'triangles':triangles,'metre_dimensions':dimensions,'uv':'within shared atlas','status':'passed','sha256':hashlib.sha256((folder/(name+'.fbx')).read_bytes()).hexdigest()})
report={'scope':'Blender FBX round-trip, metre dimensions, finite vertices, UVs and triangle budget; Unity import not run','assets':results}
(folder/'asset_audit.json').write_text(json.dumps(report,indent=2))
print('SCRAPSHIFT_FBX_AUDIT '+json.dumps(report))
