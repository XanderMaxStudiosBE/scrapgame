"""Blender round-trip for the independent original compact asset pack, not Unity."""
import bpy,json,math,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
FOLDER=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((FOLDER/'compact_asset_manifest.json').read_text())
results=[]
for name,expected in manifest['assets'].items():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(FOLDER/(name+'.fbx')))
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert len(objects)==1,(name,'expected single shared-material mesh')
    vertices=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
    assert all(math.isfinite(c) for v in vertices for c in v),(name,'non-finite vertex')
    dimensions=[max(v[a] for v in vertices)-min(v[a] for v in vertices) for a in range(3)]
    assert all(abs(a-b)<.025 for a,b in zip(dimensions,expected['dimensions_blender'])),(name,dimensions)
    triangles=sum(len(p.vertices)-2 for o in objects for p in o.data.polygons)
    assert triangles==expected['triangles']<=2500,(name,'triangle budget',triangles)
    assert all(len(o.data.materials)==1 for o in objects),(name,'material slots')
    for o in objects:
        assert o.data.uv_layers.active,(name,'no UVs')
        assert all(math.isfinite(c) and 0<=c<=1 for uv in o.data.uv_layers.active.data for c in uv.uv),(name,'UV outside atlas')
        # Face normals must be finite; Unity import/culling is still a local check.
        assert all(math.isfinite(c) for p in o.data.polygons for c in p.normal),(name,'non-finite normals')
    results.append({'name':name,'status':'passed','triangles':triangles,'metre_dimensions':dimensions,'sha256':hashlib.sha256((FOLDER/(name+'.fbx')).read_bytes()).hexdigest()})
report={'scope':'Blender FBX round-trip, finite vertices/normals, metre bounds, UVs, single material and 2500 triangle budget; Unity import not run','assets':results}
(FOLDER/'compact_asset_audit.json').write_text(json.dumps(report,indent=2)+'\n')
print('SCRAPSHIFT_COMPACT_AUDIT '+json.dumps(report))
