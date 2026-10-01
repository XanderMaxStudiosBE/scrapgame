"""Original SCRAPSHIFT meshes. Blender 4.3+: blender -b --python ArtSource/build_yard_assets.py
Metre scale, baked bevels, flat/weighted normals, shared 512px worn palette atlas.
No downloaded models; renders are asset previews, not Unity gameplay captures.
"""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
PREV=ROOT/'Assets/Scrapshift/Art/Previews'
OUT.mkdir(parents=True, exist_ok=True); PREV.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
PALETTE=[('sage',(91,103,82)),('blue',(88,111,118)),('cream',(185,178,157)),('rust',(135,78,48)),('metal',(59,65,64)),('rubber',(32,34,33)),('wood',(109,91,66)),('glass',(61,82,87)),('warmglass',(160,132,82)),('copper',(160,100,59)),('leaf',(75,87,64)),('leaflight',(106,116,86)),('ochre',(143,119,71)),('darkwood',(72,62,48)),('ivory',(216,207,181)),('soil',(88,80,65))]
MAT={}; atlas=bpy.data.images.new('ScrapshiftPropAtlas',512,512); pixels=[0.]*(512*512*4)
rng=random.Random(381)
for index,(name,rgb) in enumerate(PALETTE):
    for y in range(128):
        for x in range(128):
            noise=rng.uniform(-7,7); dirt=-10*max(0,(16-y)/16); color=[c+noise+dirt for c in rgb]
            if name in ['sage','blue','cream','ochre'] and rng.random()<.018: color=[122,77,48]
            if x%19==0 and rng.random()<.18: color=[c-16 for c in color]
            px=index%4*128+x; py=index//4*128+y; offset=(py*512+px)*4
            pixels[offset:offset+4]=[max(0,min(1,c/255)) for c in color]+[1]
    m=bpy.data.materials.new(name); m.diffuse_color=tuple(c/255 for c in rgb)+(1,); MAT[name]=m
atlas.pixels=pixels; atlas.filepath_raw=str(OUT/'ScrapshiftPropAtlas.png'); atlas.file_format='PNG'; atlas.save()
atlasmat=bpy.data.materials.new('ScrapshiftPropAtlas'); atlasmat.use_nodes=True
bs=atlasmat.node_tree.nodes.get('Principled BSDF'); bs.inputs['Roughness'].default_value=.85
tex=atlasmat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=atlas;tex.interpolation='Linear'
atlasmat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
CURRENT=[]
def add(o, mat):
    o.data.materials.append(MAT[mat]);CURRENT.append(o);return o
# Designer coordinates are Unity-style x/right, y/up, z/depth; Blender uses Z up.
def xyz(p):return(p[0],p[2],p[1])
def cube(name,pos,size,mat='metal',bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=xyz(pos));o=bpy.context.object;o.name=name
    o.scale=xyz(size);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Worn softened edges','BEVEL');mod.width=bevel;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=o.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');mod.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return add(o,mat)
def cyl(name,pos,radius,depth,mat='metal',axis='Y',verts=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius,depth=depth,location=xyz(pos));o=bpy.context.object;o.name=name
    if axis=='X':o.rotation_euler[1]=math.pi/2
    if axis=='Z':o.rotation_euler[0]=math.pi/2
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return add(o,mat)
def beam(name,a,b,width,mat='metal'):
    va,vb=Vector(xyz(a)),Vector(xyz(b));o=cube(name,((a[0]+b[0])/2,(a[1]+b[1])/2,(a[2]+b[2])/2),(width,(vb-va).length,width),mat)
    o.rotation_euler=(vb-va).to_track_quat('Z','Y').to_euler();return o
def profile(name,width,points,mat):
    # Extrude a side profile: points are (depth,height), width along X.
    n=len(points);v=[(-width/2,z,y) for z,y in points]+[(width/2,z,y) for z,y in points]
    f=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    f += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(v,[],f);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    # Blender fixes normals consistently on convex/extruded shells.
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
    return add(o,mat)
def ring(name,pos,major,minor,mat='metal',axis='Y'):
    bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=4,location=xyz(pos),major_radius=major,minor_radius=minor)
    o=bpy.context.object;o.name=name
    if axis=='X':o.rotation_euler[1]=math.pi/2
    if axis=='Z':o.rotation_euler[0]=math.pi/2
    return add(o,mat)
def window(pos,width,height):
    cube('Dirty framed window',pos,(width,height,.035),'glass')
    for x in [-width/2,0,width/2]:cube('Window frame',(pos[0]+x,pos[1],pos[2]-.035),(.055,height+.1,.075),'cream')
    for y in [-height/2,height/2]:cube('Window frame',(pos[0],pos[1]+y,pos[2]-.035),(width+.1,.055,.075),'cream')
def hatchback(rust=False):
    body=profile('Dented hatchback body',2.05,[(-2.15,.45),(-2.18,.76),(-1.1,.88),(-.76,1.43),(-.35,1.57),(.65,1.55),(1.29,1.05),(2.05,.91),(2.13,.45)],'rust' if rust else 'blue')
    bpy.context.view_layer.objects.active=body
    mod=body.modifiers.new('Soft worn body edges','BEVEL');mod.width=.025;mod.segments=2
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod=body.modifiers.new('Body weighted normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=mod.name)
    # Glass panels follow the sloping cabin rather than a box silhouette.
    front=cube('Dirty windshield',(0,1.2,-.96),(1.7,.55,.025),'glass');front.rotation_euler[0]=math.radians(-34)
    rear=cube('Rear window',(0,1.27,.97),(1.7,.49,.025),'glass');rear.rotation_euler[0]=math.radians(38)
    for x in [-1.033,1.033]:
        cube('Side glazing',(x,1.25,.13),(.025,.40,1.32),'glass')
        cube('Side pillar',(x,1.25,.10),(.035,.46,.085),'metal')
        cube('Door seam',(x,.79,.86),(.025,.45,.025),'metal')
        cube('Door handle',(x,.95,.50),(.05,.055,.22),'metal',.015)
        for z in [-1.34,1.36]:
            cyl('Worn tyre',(x,.39,z),.37,.22,'rubber','X',16)
            cyl('Rust wheel rim',(x*1.115,.39,z),.21,.025,'rust','X')
            cyl('Wheel hub',(x*1.13,.39,z),.07,.03,'metal','X',8)
    cube('Front grille',(0,.62,-2.20),(.85,.25,.045),'metal')
    for x in [-.76,.76]:cube('Faded headlights',(x,.68,-2.2),(.36,.21,.045),'cream',.025)
    cube('Bent front bumper',(0,.45,-2.24),(2.1,.13,.12),'metal',.03)
    cube('Rear bumper',(0,.47,2.15),(2.1,.14,.12),'metal',.02)
    for x in [-.75,.75]:cube('Rear lamp',(x,.7,2.15),(.24,.15,.035),'rust')
    for x,z,s in [(-.75,-1.8,.3),(.9,.8,.25),(-.4,1.8,.4)]:cube('Surface rust scar',(x,.90,z),(s,.015,.20),'rust')
def office():
    cube('Office timber walls',(0,1.5,0),(8,3,6),'wood',.05)
    for x in [-4.03,4.03]:cube('Corner flashing',(x,1.5,0),(.09,3.05,6.08),'cream')
    for z in [-3.03,3.03]:cube('Foundation trim',(0,.18,z),(8.1,.30,.08),'darkwood')
    profile('Office roof gable',8,[(-3,3),(3,3),(3,3.08),(-3,3.08)],'wood')
    # Pitched along width: build sloping sheets and a triangular gable end.
    for side in [-1,1]:
        o=cube('Pitched corrugated roof',(side*2.15,3.61,0),(4.6,.12,6.8),'metal');o.rotation_euler[1]=side*math.radians(16)
        for i in range(9):
            beam('Roof seam',(side*.1,4.19,-3.3+i*.8),(side*4.4,3.0,-3.3+i*.8),.045,'metal')
    # Triangular facade closes the space under the ridge.
    mesh=bpy.data.meshes.new('Gable');mesh.from_pydata([(-4,-3.04,3),(4,-3.04,3),(0,-3.04,4.2),(-4,3.04,3),(4,3.04,3),(0,3.04,4.2)],[],[(0,1,2),(5,4,3)]);mesh.update();o=bpy.data.objects.new('Timber gable ends',mesh);bpy.context.collection.objects.link(o);add(o,'wood')
    window((1.5,1.75,-3.06),2,1.15)
    cube('Office steel door',(-2,1.15,-3.06),(1.25,2.3,.08),'sage',.015)
    cube('Door handle',(-1.55,1.1,-3.15),(.065,.20,.07),'metal')
    cube('Porch awning',(-2,2.70,-3.65),(2.7,.10,1.6),'metal')
    for x in [-3.25,-.75]:beam('Porch bracket',(x,2.6,-3.7),(x,2.15,-3.09),.055)
    cube('Office sign backing',(0,2.55,-3.13),(3.8,.43,.03),'cream')
    cube('Utility vent',(3.1,1,-3.12),(.75,.55,.07),'metal')
    for i in range(5):cube('Vent slat',(3.1,.80+i*.10,-3.17),(.64,.035,.015),'cream')
    cyl('Chimney',(2.5,4,1.6),.14,1.25,'rust');cube('Chimney cap',(2.5,4.64,1.6),(.48,.06,.45),'metal')
def container():
    cube('Container shell',(0,1.4,0),(9,2.8,4),'sage',.065)
    for x in [-4.46,4.46]:
        for z in [-1.96,1.96]:cube('Welded corner post',(x,1.4,z),(.17,2.9,.17),'metal',.018)
    for z in [-2.04,2.04]:
        for i in range(22):cube('Corrugation rib',(-4.25+i*.4,1.4,z),(.065,2.65,.06),'sage')
        for y in [.10,2.73]:cube('Container frame',(0,y,z),(9,.12,.1),'metal')
    for x in [-.18,.18]:cyl('Door locking bar',(x,1.4,-2.13),.026,2.3,'metal')
    cube('Faded identification plate',(-2.5,1.8,-2.11),(2.3,.60,.025),'cream')
    for x in [-4.4,4.4]:
        for y in [.25,2.55]:cube('Corner casting',(x,y,-2.1),(.18,.20,.09),'metal')
def workshop():
    # A broad industrial canopy shelters the existing workstations; open front and sides.
    for x in [-9,9]:
        for z in [-3,3]:cube('Workshop steel upright',(x,1.95,z),(.16,3.9,.16),'rust')
    for z in [-3,3]:beam('Workshop eave beam',(-9.1,3.85,z),(9.1,3.85,z),.18,'metal')
    for x in [-9,-4.5,0,4.5,9]:
        beam('Roof truss left',(x,3.9,-3.7),(x,4.65,0),.10,'metal')
        beam('Roof truss right',(x,4.65,0),(x,3.9,3.7),.10,'metal')
        beam('Truss tie',(x,3.9,-3.6),(x,3.9,3.6),.07,'metal')
    for side in [-1,1]:
        roof=cube('Workshop corrugated roof',(0,4.27,side*1.85),(19,.12,3.8),'metal');roof.rotation_euler[0]=-side*math.radians(11.5)
        for x in range(-9,10):beam('Raised roof seam',(x,4.68,.01*side),(x,3.90,3.75*side),.035,'metal')
    cube('Workshop rear half wall',(0,1.2,3.2),(18,2.4,.09),'wood')
    cube('Workshop rear flashing',(0,2.44,3.22),(18,.10,.1),'rust')
    cube('Workshop nameplate',(0,3.9,-3.14),(5,.55,.04),'cream')
    for x in [-7,7]:
        cube('Warm industrial lamp',(x,3.5,-1),(.65,.16,.26),'metal',.03)
        cube('Lamp underside',(x,3.41,-1),(.55,.025,.18),'warmglass')
    # No walls or geometry on the stations' approach paths.
def bin_model():
    cube('Skip bottom',(0,.15,0),(4,.3,4),'metal',.03)
    for x in [-1.96,1.96]:cube('Skip side wall',(x,.7,0),(.1,1.4,4),'rust')
    cube('Skip rear wall',(0,.7,1.95),(4,1.4,.10),'rust')
    cube('Low access lip',(0,.38,-1.95),(4,.76,.10),'rust')
    for x in [-1.98,1.98]:
        for z in [-1.8,0,1.8]:cube('Skip reinforcing rib',(x,.7,z),(.13,1.45,.08),'metal')
    for i in range(8):
        o=cube('Bent sorted metal',((i%3-1)*1.0,.55+(i//3)*.25,(i%2-.5)*2),(1.1,.16,1.4),'metal',.02);o.rotation_euler=(.08*i,.1*i,.35*i)
    cube('Skip faded label',(0,.44,-2.02),(1.5,.32,.02),'cream')
def fan(frame_only=False):
    cube('Fan weighted base',(0,.045,0),(.44,.09,.30),'sage',.035)
    cyl('Fan pedestal',(0,.30,.03),.055,.47,'metal')
    cyl('Fan rear motor',(0,.65,.08),.12,.22,'sage','Z')
    ring('Front guard rim',(0,.65,-.075),.255,.013,'metal','Z')
    ring('Rear guard rim',(0,.65,.075),.255,.012,'metal','Z')
    for i in range(12):
        a=i*math.tau/12
        beam('Front guard spoke',(0,.65,-.10),(.255*math.cos(a),.65+.255*math.sin(a),-.075),.008,'metal')
    for radius in [.08,.16,.22]:ring('Guard concentric wire',(0,.65,-.085),radius,.006,'metal','Z')
    if frame_only:return
    fan_rotor(.65)
def fan_rotor(height=0):
    cyl('Rotor hub',(0,height,-.025),.045,.05,'cream','Z')
    for i in range(3):
        a=i*math.tau/3
        o=cube('Fan blade',(.11*math.cos(a),height+.11*math.sin(a),-.005),(.18,.065,.015),'cream',.015);o.rotation_euler[1]=-a

def workbench():
    for x in [-.98,.98]:
        for z in [-.47,.47]:cube('Steel workbench leg',(x,.50,z),(.12,1,.12),'metal',.012)
    cube('Lower workbench shelf',(0,.28,0),(2.05,.09,1.05),'darkwood')
    cube('Thick worn worktop',(0,1.015,0),(2.5,.17,1.3),'wood',.025)
    for x in [-1.05,1.05]:cube('Workbench cross brace',(x,.78,0),(.08,.10,1.0),'rust')
    cube('Working metal plate',(0,1.11,-.10),(1.10,.025,.75),'metal')
    cube('Vise foot',(-.86,1.16,.23),(.46,.12,.36),'sage',.02)
    for z in [.09,.33]:cube('Vise jaw',(-.86,1.28,z),(.40,.18,.08),'sage',.01)
    cyl('Vise screw',(-.86,1.21,-.09),.03,.35,'metal','Z')
    cyl('Vise sliding handle',(-.86,1.21,-.26),.014,.23,'metal','X')
    for x in [.78,.88]:
        cube('Stripping plier grip',(x,1.12,-.3),(.045,.04,.24),'rust',.01)
        cube('Stripping plier jaw',(x,1.12,-.13),(.04,.04,.12),'metal')
    for x,z in [(-.9,-.5),(.9,.5)]:cyl('Tabletop fixing bolt',(x,1.107,z),.025,.018,'metal',verts=6)
def wire_crate():
    cube('Wire crate floor',(0,.18,0),(2.3,.22,1.3),'wood',.015)
    for x in [-1.08,1.08]:
        cube('Wire crate side',(x,.60,0),(.14,.84,1.3),'wood',.01)
        for z in [-.55,.55]:cube('Crate corner strap',(x,.65,z),(.16,.9,.07),'metal')
    cube('Wire crate back',(0,.60,.58),(2.3,.84,.14),'wood')
    cube('Low wire crate front',(0,.40,-.58),(2.3,.45,.14),'wood')
    for i in range(5):
        x=(i-2)*.36
        ring('Coiled scrap wire',(x,.47+(i%2)*.09,0),.20,.025,'rubber')
        ring('Coiled scrap wire',(x,.51+(i%2)*.09,0),.18,.025,'rubber')
        cyl('Copper wire end',(x,.49,-.22),.019,.10,'copper','Z',8)
        cube('Wire bundle tie',(x,.54,0),(.075,.13,.40),'cream')
def pallet_bundle():
    for x in [-.9,0,.9]:cube('Pallet spacer',(x,.12,0),(.20,.24,1.6),'darkwood')
    for i in range(6):cube('Pallet top slat',(-1.05+i*.42,.27,0),(.34,.065,1.6),'wood')
    for i in range(4):
        o=cube('Stacked recovered panel',((i%2-.5)*.08,.43+i*.14,0),(2.05,.10,1.3),'rust' if i%2==0 else 'metal',.02)
        o.rotation_euler[2]=(.025 if i%2==0 else -.035)
    for x in [-.72,.72]:cube('Salvage stack binding',(x,.67,-.69),(.06,.7,.03),'cream')

def atlas_uv(objects):
    for o in objects:
        if o.type!='MESH':continue
        mesh=o.data; uv=mesh.uv_layers.active or mesh.uv_layers.new(name='UVMap')
        for poly in mesh.polygons:
            name=mesh.materials[poly.material_index].name
            index=next(i for i,(n,_) in enumerate(PALETTE) if n==name)
            normal=poly.normal; axes=[i for i in range(3) if i!=max(range(3),key=lambda i:abs(normal[i]))]
            coords=[mesh.vertices[mesh.loops[k].vertex_index].co for k in poly.loop_indices]
            mins=[min(v[a] for v in coords) for a in axes]; maxs=[max(v[a] for v in coords) for a in axes]
            for k in poly.loop_indices:
                v=mesh.vertices[mesh.loops[k].vertex_index].co
                u=(v[axes[0]]-mins[0])/max(.001,maxs[0]-mins[0]);w=(v[axes[1]]-mins[1])/max(.001,maxs[1]-mins[1])
                uv.data[k].uv=((index%4+(u*.84+.08))/4,(index//4+(w*.84+.08))/4)
            poly.material_index=0
        mesh.materials.clear();mesh.materials.append(atlasmat)
def export(name,builder):
    global CURRENT;CURRENT=[]
    bpy.ops.object.select_all(action='DESELECT');builder();atlas_uv(CURRENT)
    for o in CURRENT:o.select_set(True)
    bpy.context.view_layer.objects.active=CURRENT[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    # Import support ships in Unity: no GLB runtime package needed.
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,use_mesh_modifiers=True,path_mode='STRIP')
    tri=sum(max(0,len(p.vertices)-2) for p in o.data.polygons)
    stats[name]={'triangles':tri,'vertices':len(o.data.vertices),'dimensions_blender':list(o.dimensions),'materials':len(o.data.materials)}
    o.hide_render=True;o.hide_set(True);return o
stats={}
models=[export(n,f) for n,f in [('WornHatchback',hatchback),('YardOffice',office),('ShippingContainer',container),('WorkshopCanopy',workshop),('SortingSkip',bin_model),('SalvageFan',fan),('Workbench',workbench),('WireCrate',wire_crate),('PalletBundle',pallet_bundle),('RustyHatchback',lambda:hatchback(True)),('SalvageFanFrame',lambda:fan(True)),('FanRotor',fan_rotor)]]
(OUT/'asset_manifest.json').write_text(json.dumps({'source':'Original Blender-authored models; build_yard_assets.py','units':'metres','atlas':'ScrapshiftPropAtlas.png','assets':stats},indent=2))
# A rendered contact sheet of the actual exported source meshes, not gameplay.
for i,o in enumerate([o for o in models if o.name not in ['SalvageFanFrame','FanRotor']]):
    o.hide_render=False;o.hide_set(False)
    scale=.8 if i in [0,5,6,7,8] else .20
    o.scale=(scale,)*3;o.location=((i%3-1)*3.6,(i//3)*4,0)
# Ground for contact shadows.
bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.location.z=-.015
m=bpy.data.materials.new('Preview ground');m.diffuse_color=(.19,.18,.15,1);ground.data.materials.append(m)
bpy.ops.object.light_add(type='AREA',location=(2,-4,12));bpy.context.object.data.energy=1800;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=8
bpy.ops.object.light_add(type='AREA',location=(-8,5,8));bpy.context.object.data.energy=850;bpy.context.object.data.color=(.68,.77,1);bpy.context.object.data.size=6
bpy.ops.object.camera_add(location=(12,-18,16));cam=bpy.context.object;target=Vector((0,5,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=17
scene=bpy.context.scene;scene.camera=cam;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=False
scene.render.resolution_x=1600;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.world.color=(.24,.25,.26);scene.view_settings.view_transform='AgX'
scene.render.filepath=str(PREV/'WornRetroAssetSheet.png');bpy.ops.render.render(write_still=True)
print('SCRAPSHIFT_ASSET_STATS '+json.dumps(stats))
