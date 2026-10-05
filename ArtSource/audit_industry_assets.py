"""Actual Blender FBX round-trip of original Stage D machinery; Unity unrun."""
import bpy, json, math, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
FOLDER=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((FOLDER/'industry_asset_manifest.json').read_text());records=[]
for name,expected in manifest['assets'].items():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(FOLDER/(name+'.fbx')))
    models=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(models)==1,name
    vertices=[o.matrix_world@v.co for o in models for v in o.data.vertices]
    assert all(math.isfinite(c) for v in vertices for c in v),name
    lower=[min(v[a] for v in vertices) for a in range(3)]
    upper=[max(v[a] for v in vertices) for a in range(3)]
    dimensions=[upper[a]-lower[a] for a in range(3)]
    assert all(abs(a-b)<.025 for a,b in zip(dimensions,expected['dimensions_blender'])),(name,dimensions)
    width,depth,height=manifest['footprints_unity'][name]
    assert lower[0]>=-width/2-.001 and upper[0]<=width/2+.001,(name,'width',lower,upper)
    assert lower[1]>=-depth/2-.001 and upper[1]<=depth/2+.001,(name,'depth',lower,upper)
    assert lower[2]>=-.001 and upper[2]<=height+.001,(name,'height',lower,upper)
    triangles=sum(len(p.vertices)-2 for o in models for p in o.data.polygons)
    assert triangles==expected['triangles']<=manifest['triangle_budgets'][name],(name,triangles)
    for obj in models:
        assert len(obj.data.materials)==1 and obj.data.uv_layers.active,name
        assert all(math.isfinite(c) and 0<=c<=1 for uv in obj.data.uv_layers.active.data for c in uv.uv),name
        assert all(math.isfinite(c) and p.normal.length>.99 for p in obj.data.polygons for c in p.normal),name
    records.append({'name':name,'status':'passed','triangles':triangles,'metre_bounds_blender':[lower,upper],
                    'sha256':hashlib.sha256((FOLDER/(name+'.fbx')).read_bytes()).hexdigest()})
loads=[]
for name in ['WornHatchback','CompactRefrigerator']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(FOLDER/(name+'.fbx')))
    vertices=[o.matrix_world@v.co for o in bpy.context.scene.objects if o.type=='MESH' for v in o.data.vertices]
    assert vertices,name
    # Same root offset and uniform min(width/depth) scaling as SyncJob. Test
    # normal, reduced, enlarged and unequal user-tuned catalogue footprints.
    for width,depth in [(6,7),(4.2,4.9),(7.2,8.4),(3.6,8.4)]:
        scale=min(1,width/6,depth/7)
        loaded=[(v.x*scale,v.y*scale-.10,v.z*scale+.46) for v in vertices]
        assert all(abs(x)<=width/2 and abs(z)<=depth/2 and .43<=y<2.75 for x,z,y in loaded),(name,width,depth,'Cradle/head clearance')
    loads.append({'name':name,'status':'passed','configurations':4,'scope':'Original imported load vertices remain within scaled primary footprint and below dismantling head'})
report={'scope':'Actual Blender FBX round-trip, metre footprint/height, finite normals/vertices, atlas UVs, one material and per-model triangle budgets; original loaded car/fridge cradle/head clearance; Unity unrun','assets':records,'loaded_objects':loads}
(FOLDER/'industry_asset_audit.json').write_text(json.dumps(report,indent=2)+'\n')
print('SCRAPSHIFT_INDUSTRY_AUDIT '+json.dumps(report))
