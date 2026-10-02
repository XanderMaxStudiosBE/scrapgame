"""Actual FBX exports in the new compact fixed layout, rendered in Blender.

Lighting/tone mapping are approximations; no Unity shaders, collision, UI or FPS.
Default view is the manual starter; -- equipment renders the purchased-machine detail.
"""
import bpy,math,json,sys,re
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
WORLD=ROOT/'Assets/Scrapshift/Resources/ScrapshiftWorld'
OUT=ROOT/'Assets/Scrapshift/Art/Previews'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,path,alpha=False):
    m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.85
    t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(path));m.node_tree.links.new(t.outputs['Color'],bs.inputs['Base Color'])
    if alpha:m.node_tree.links.new(t.outputs['Alpha'],bs.inputs['Alpha'])
    return m
props=mat('Existing unchanged PropAtlas',MODELS/'ScrapshiftPropAtlas.png')
world=mat('Existing unchanged WorldAtlas',WORLD/'WorldAtlas.png')
fence=mat('Existing cutout ChainLink',WORLD/'ChainLink.png',True)
gravel=mat('Existing coarse Gravel',ROOT/'Assets/Scrapshift/Art/Textures/Gravel.png')
wood=mat('Existing WeatheredWood',ROOT/'Assets/Scrapshift/Art/Textures/WeatheredWood.png')
rust=mat('Existing RustPaint',ROOT/'Assets/Scrapshift/Art/Textures/RustPaint.png')
metal=mat('Existing DarkMetal',ROOT/'Assets/Scrapshift/Art/Textures/DarkMetal.png')
roof=mat('Existing CorrugatedMetal',ROOT/'Assets/Scrapshift/Art/Textures/CorrugatedMetal.png')
dust=mat('Existing GroundWear alpha',WORLD/'GroundLayers.png',True)
wet=mat('Existing RoughPuddles alpha',WORLD/'GroundLayers.png',True)
wet.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.72
old=json.loads((MODELS/'asset_manifest.json').read_text())['assets'];cache={}
def place(name,p,yaw=0,scale=1):
    if name not in cache:
        before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(MODELS/(name+'.fbx')))
        o=next(o for o in set(bpy.data.objects)-before if o.type=='MESH')
        o.data.transform(o.matrix_world);o.parent=None;o.matrix_world=Matrix.Identity(4)
        o.data.materials.clear();o.data.materials.append(world if old.get(name,{}).get('pack')=='ScrapshiftWorld' else props)
        for poly in o.data.polygons:poly.material_index=0
        cache[name]=o.data
        for extra in set(bpy.data.objects)-before:bpy.data.objects.remove(extra,do_unlink=True)
    o=bpy.data.objects.new(name,cache[name]);bpy.context.collection.objects.link(o);o.location=(p[0],p[2],p[1]);o.rotation_euler.z=-math.radians(yaw)
    o.scale=(scale,scale,scale) if isinstance(scale,(int,float)) else (scale[0],scale[2],scale[1]);return o
def box(name,p,size,material):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(p[0],p[2],p[1]));o=bpy.context.object;o.name=name;o.scale=(size[0],size[2],size[1]);o.data.materials.append(material)
    # Match runtime metre-projected box UVs, avoiding object-scale texel stretch.
    uv=o.data.uv_layers.active
    for poly in o.data.polygons:
        axes=[i for i in range(3) if i!=max(range(3),key=lambda i:abs(poly.normal[i]))]
        for k in poly.loop_indices:
            v=o.data.vertices[o.data.loops[k].vertex_index].co
            uv.data[k].uv=((v[axes[0]]+.5)*o.scale[axes[0]],(v[axes[1]]+.5)*o.scale[axes[1]])
    return o
def boundary(x,z,width,along_x,height=2.1):
    v=[(-width/2,0,0),(-width/2,0,height),(width/2,0,height),(width/2,0,0)] if along_x else [(0,-width/2,0),(0,-width/2,height),(0,width/2,height),(0,width/2,0)]
    mesh=bpy.data.meshes.new('Runtime fence plane');mesh.from_pydata(v,[],[(0,1,2,3)]);layer=mesh.uv_layers.new();mesh.materials.append(fence)
    for i,p in enumerate([(0,0),(0,height),(width,height),(width,0)]):layer.data[i].uv=p
    o=bpy.data.objects.new('Fixed wire fence',mesh);bpy.context.collection.objects.link(o);o.location=(x,z,0)
    box('Lower fence sheets',(x,.31,z),(width,.62,.09) if along_x else (.09,.62,width),roof)
    spans=math.ceil(width/4)
    for i in range(spans+1):place('FencePost',(x+(-width/2+width*i/spans if along_x else 0),0,z+(0 if along_x else -width/2+width*i/spans)))
box('Compact packed gravel',(0,-.2,0),(48,.4,36),gravel)
def patch(x,z,rx,rz,tile,yaw,height,material):
    c=math.cos(math.radians(yaw));s=math.sin(math.radians(yaw));vertices=[(x,z,height)];uv=[(tile%2*.5+.25,tile//2*.5+.25)]
    for i in range(12):
        angle=i*math.tau/12;u=math.cos(angle);v=math.sin(angle);r=1+.04*math.sin(i*2.7+x)
        vertices.append((x+c*u*rx*r+s*v*rz*r,z-s*u*rx*r+c*v*rz*r,height));uv.append((tile%2*.5+.25+u*.24,tile//2*.5+.25+v*.24))
    mesh=bpy.data.meshes.new('Runtime compact ground layer');mesh.from_pydata(vertices,[],[(0,i+1,(i+1)%12+1) for i in range(12)]);layer=mesh.uv_layers.new();mesh.materials.append(material)
    for poly in mesh.polygons:
        for k in poly.loop_indices:layer.data[k].uv=uv[mesh.loops[k].vertex_index]
    o=bpy.data.objects.new('Runtime compact ground layer',mesh);bpy.context.collection.objects.link(o)
patch(-.85,-13,.4,4.5,1,0,.009,dust);patch(.85,-13,.4,4.5,1,0,.009,dust)
patch(7,-8,.4,10,1,60,.009,dust);patch(8.4,-7.3,.4,10,1,60,.009,dust)
for i in range(12):patch(-18+i%4*12,-5+i//4*9,2.8,1.65,3 if i%4==0 else 0,i*47,.010,dust)
patch(4,-11,1.5,.6,2,25,.012,wet);patch(-17,8,1,.65,2,-10,.012,wet)
box('Outside verge',(0,-.5,0),(95,.35,78),gravel)
box('Service road',(0,-.008,-23),(90,.016,7),metal)
boundary(0,18,48,True);boundary(-24,0,36,False);boundary(24,0,36,False)
boundary(-13,-18,22,True);boundary(13,-18,22,True);boundary(0,-18,4,True,2)
for x in [-2.15,2.15]:box('Gate upright',(x,1.5,-18),(.18,3,.18),metal)
source=(ROOT/'Assets/Scrapshift/Runtime/CompactYardWorld.cs').read_text()
anchors={name:tuple(float(v.strip().rstrip('f')) for v in values.split(',')) for name,values in re.findall(r'public static readonly Vector3 (\w+)=new Vector3\(([^)]+)\);',source)}
place('YardOffice',anchors['OfficeAnchor'],180);place('OfficeDetails',anchors['OfficeAnchor'],180)
for x in [-19,-15]:
    box('Counter rain hood',(x,3.34,-10.75),(2.3,.075,1.1),roof)
    place('FluorescentFixture',(x,2.69,-10.6))
shop=anchors['ShopAnchor'];sales=anchors['SalesAnchor']
box('Shop counter top',(shop[0],.93,shop[2]-.1),(1.9,.11,.70),wood)
for x in [shop[0]-.72,shop[0]+.72]:box('Shop leg',(x,.44,shop[2]-.1),(.065,.88,.55),metal)
place('BuyingScale',(sales[0],0,sales[2]-.1),180,.65)
place('CompactDeliveryTruck',(20.5,0,-15),180)
for x in [19,23]:
    for z in [-17.2,-13]:box('Receiving post',(x,1.825,z),(.09,3.65,.09),metal)
box('Receiving roof',(21,3.67,-15.1),(4.4,.08,4.65),roof)
place('FluorescentFixture',anchors['DeliveryLightAnchor'])
place('WireCrate',anchors['WireAnchor'])
delivery=anchors['DeliveryAnchor'];box('Delivery crate',(delivery[0],.3,delivery[2]),(1.3,.6,.7),wood)
for i in range(15):
    angle=i*math.tau/15;p=(math.cos(angle)*31,0,math.sin(angle)*25)
    if p[2]<-18 and abs(p[0])<17:continue
    place('CompactBoundaryTree',p,i*47,.8+i%3*.14)
place('IndustrialWorks',(9,0,42),10)
for north,edge,count in [(True,18,10),(False,-24,7),(False,24,7)]:
    for i in range(count):
        v=-21+i*4.6 if north else -13+i*4.6
        place('WeedClump',(v,0,edge-.35) if north else (edge-.35 if edge>0 else edge+.35,0,v),i*61)
for x in [-22.5,22.5]:place('PalletBundle',(x,0,17.55),scale=(.46,.62,.40))
place('Workbench',(-5,0,-4));place('RepairTools',(-4.65,1.14,-3.9));place('WornHatchback',(18,0,-10));place('CompactRefrigerator',(20,0,-5))
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
equipment='equipment' in args
if equipment:
    place('CompactGenerator',(1,0,0),20);place('CompactTier1Scrapper',(-2,0,0),-12)
    place('CompactMotor',(-2,.71,-.78));place('CompactCompressor',(-1.55,.71,-.78))
profile=(ROOT/'Assets/Scrapshift/Resources/ScrapshiftLighting/CozyAfternoon.asset').read_text()
def color(name):return tuple(map(float,re.search(r'^  '+name+r': \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)',profile,re.M).groups()))
def value(name):return float(re.search(r'^  '+name+r': ([-\d.]+)$',profile,re.M)[1])
w=bpy.context.scene.world;w.use_nodes=True;w.node_tree.nodes.get('Background').inputs['Color'].default_value=(*color('skyFill'),1);w.node_tree.nodes.get('Background').inputs['Strength'].default_value=.8
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=value('sunIntensity');sun.data.color=color('sunColor');sun.data.angle=math.radians(4)
e=math.radians(value('sunElevation'));a=math.radians(value('sunAzimuth'));sun.rotation_euler=Vector((-math.cos(e)*math.sin(a),-math.cos(e)*math.cos(a),-math.sin(e))).to_track_quat('-Z','Y').to_euler()
for p in [(-19,2.65,-10.6),(-15,2.65,-10.6),anchors['DeliveryLightAnchor']]:
    bpy.ops.object.light_add(type='SPOT',location=(p[0],p[2],p[1]));lamp=bpy.context.object;lamp.data.energy=value('lampIntensity')*40;lamp.data.color=color('lampColor');lamp.data.spot_size=math.radians(100);lamp.data.spot_blend=.35
bpy.ops.object.light_add(type='AREA',location=(0,-6,8));fill=bpy.context.object;fill.data.energy=900;fill.data.size=16;fill.data.color=color('skyFill')
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=32;scene.render.resolution_x=1120;scene.render.resolution_y=700;scene.render.resolution_percentage=100;scene.render.threads_mode='FIXED';scene.render.threads=8
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam
cam.location=(4,-7,5) if equipment else (26,28,24)
target=Vector((-1,0,.7) if equipment else (0,0,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=28 if equipment else 25
lm=bpy.data.materials.new('Visible provenance');lm.use_nodes=True;lb=lm.node_tree.nodes.get('Principled BSDF');lb.inputs['Emission Color'].default_value=(.87,.87,.81,1);lb.inputs['Emission Strength'].default_value=1
bpy.ops.object.text_add();label=bpy.context.object;label.parent=cam;half_width=18/cam.data.lens;label.location=(-half_width*.93,half_width*.625*.88,-1);label.data.body='BLENDER ASSET EXAMPLE / NOT UNITY GAMEPLAY' if equipment else 'COMPACT STARTER / BLENDER PREVIEW / NOT UNITY GAMEPLAY';label.data.size=.018;label.data.materials.append(lm)
scene.render.filepath=str(OUT/('CompactEquipmentExample.png' if equipment else 'CompactStarterOverview.png'));bpy.ops.render.render(write_still=True)
print('COMPACT_PREVIEW: actual exports, fixed/new starter layout and wear geometry; approximate Blender lighting, omitted live labels/UI. No Unity/FPS evidence.')
