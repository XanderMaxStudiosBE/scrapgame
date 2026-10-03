"""Blender import/layout audit of original compact backdrop; not Unity evidence."""
import bpy, hashlib, json, math, uuid
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
FOLDER=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((FOLDER/'backdrop_asset_manifest.json').read_text())
layout=json.loads((FOLDER/'compact_backdrop_layout.json').read_text())
legacy=json.loads((FOLDER/'asset_manifest.json').read_text())
models={};records=[]
for name in [*manifest['assets'],'TealContainer']:
    expected=manifest['assets'].get(name,legacy['assets'].get(name))
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(FOLDER/(name+'.fbx')))
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert len(objects)==1,(name,'single atlas mesh')
    vertices=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
    assert all(math.isfinite(c) for v in vertices for c in v),(name,'non-finite vertex')
    dimensions=[max(v[a] for v in vertices)-min(v[a] for v in vertices) for a in range(3)]
    assert all(abs(a-b)<.025 for a,b in zip(dimensions,expected['dimensions_blender'])),(name,dimensions)
    triangles=sum(len(p.vertices)-2 for o in objects for p in o.data.polygons)
    assert triangles==expected['triangles']<=3000,(name,'triangle budget',triangles)
    for o in objects:
        assert len(o.data.materials)==1,(name,'atlas material slots')
        assert o.data.uv_layers.active,(name,'missing UVs')
        assert all(math.isfinite(c) and 0<=c<=1 for uv in o.data.uv_layers.active.data for c in uv.uv),(name,'atlas UVs')
        assert all(math.isfinite(c) and p.area>0 for p in o.data.polygons for c in p.normal),(name,'invalid/zero-area face')
    if name in ['BackdropBrickWorkshop','BackdropSawtoothWorks']:
        # Real brick course faces must point outward on all four elevations.
        width,depth=(18,8) if name=='BackdropBrickWorkshop' else (14,10)
        sides=set()
        for p in objects[0].data.polygons:
            center=objects[0].matrix_world@p.center
            normal=(objects[0].matrix_world.to_3x3()@p.normal).normalized()
            if .1<center.z<4.7:
                if abs(abs(center.x)-width/2)<.002 and abs(normal.x)>.99:
                    assert center.x*normal.x>0,(name,'inward end wall');sides.add('x+' if center.x>0 else 'x-')
                if abs(abs(center.y)-depth/2)<.002 and abs(normal.y)>.99:
                    assert center.y*normal.y>0,(name,'inward facade');sides.add('z+' if center.y>0 else 'z-')
        assert len(sides)==4,(name,'missing brick elevation')
    points=[(v.x,v.z,v.y) for v in vertices]
    models[name]={'points':points,'triangles':triangles,'bounds':{'min':[min(v[a] for v in points) for a in range(3)],'max':[max(v[a] for v in points) for a in range(3)]}}
    if name in manifest['assets']:
        record={'name':name,'status':'passed','triangles':triangles,'metre_dimensions':dimensions,'bounds_unity':models[name]['bounds'],'sha256':hashlib.sha256((FOLDER/(name+'.fbx')).read_bytes()).hexdigest()}
        assert all(abs(record['bounds_unity'][edge][a]-expected['bounds_unity'][edge][a])<.025 for edge in ['min','max'] for a in range(3)),(name,'manifest bounds')
        records.append(record)

instances=[];all_points=[];names=set();total=0;sectors=set()
for p in layout['placements']:
    assert p['name'] not in names,'Duplicate placement';names.add(p['name']);sectors.add(p['sector'])
    model=models[p['model']];a=math.radians(p['yaw']);cs,sn=math.cos(a),math.sin(a)
    points=[(p['x']+(v[0]*cs+v[2]*sn)*p['scale'],p['y']+v[1]*p['scale'],p['z']+(-v[0]*sn+v[2]*cs)*p['scale']) for v in model['points']]
    bounds={'min':[min(v[a] for v in points) for a in range(3)],'max':[max(v[a] for v in points) for a in range(3)]}
    low,high=bounds['min'],bounds['max']
    assert high[0]<-24 or low[0]>24 or low[2]>18,(p['name'],'intrudes freely buildable yard',bounds)
    assert low[2]>-18,(p['name'],'intrudes south entrance/service road',bounds)
    assert low[0]>=-48 and high[0]<=48 and high[2]<=46,(p['name'],'outside verge budget',bounds)
    assert high[1]<=10.6,(p['name'],'skyline height budget')
    instances.append({'name':p['name'],'model':p['model'],'sector':p['sector'],'bounds_unity':bounds});all_points+=points;total+=model['triangles']
assert len(sectors)==6 and len(instances)<=32
assert total<=40000,('Total backdrop triangles',total)
report={'scope':'Blender FBX round-trip, metre dimensions, atlas UVs, outward brick walls, actual rotated placement bounds and exterior/road clearance. Unity import/render/batching/FPS not run.','layout_sha256':hashlib.sha256((FOLDER/'compact_backdrop_layout.json').read_bytes()).hexdigest(),'assets':records,'placements':instances,'budget':{'triangles':total,'renderers_before_batch':len(instances),'batching_sectors':len(sectors),'bounds_unity':{'min':[min(v[a] for v in all_points) for a in range(3)],'max':[max(v[a] for v in all_points) for a in range(3)]}}}
target=FOLDER/'backdrop_asset_audit.json';target.write_text(json.dumps(report,indent=2)+'\n')
meta=Path(str(target)+'.meta')
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
print('SCRAPSHIFT_BACKDROP_AUDIT '+json.dumps(report['budget']))
