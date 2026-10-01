"""Reimport actual FBX exports and shared runtime dressing JSON. Blender previews, never gameplay.
URP ambient/sky/fog and tone mapping are approximated, not executed. Extra broad fill is preview-only.
"""
import bpy,math,json,re,sys
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];MODELS=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps';WORLD=ROOT/'Assets/Scrapshift/Resources/ScrapshiftWorld';OUT=ROOT/'Assets/Scrapshift/Art/Previews'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
profile=(ROOT/'Assets/Scrapshift/Resources/ScrapshiftLighting/CozyAfternoon.asset').read_text()
def color(name):return tuple(map(float,re.search(r'^  '+name+r': \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)',profile,re.M).groups()))
def value(name):return float(re.search(r'^  '+name+r': ([-\d.]+)$',profile,re.M)[1])
def texturemat(name,path,rough=.82,alpha=False,repeat=1):
    m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=rough
    tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path));m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if alpha:m.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha'])
    if repeat!=1:
        uv=m.node_tree.nodes.new('ShaderNodeTexCoord');scale=m.node_tree.nodes.new('ShaderNodeVectorMath');scale.operation='SCALE';scale.inputs[3].default_value=repeat
        m.node_tree.links.new(uv.outputs['UV'],scale.inputs[0]);m.node_tree.links.new(scale.outputs['Vector'],tex.inputs['Vector']);tex.interpolation='Linear'
    return m
legacy=texturemat('Actual unchanged legacy atlas',MODELS/'ScrapshiftPropAtlas.png')
worldmat=texturemat('Actual additive WorldProps atlas',WORLD/'WorldAtlas.png')
bs=worldmat.node_tree.nodes.get('Principled BSDF');nodes=worldmat.node_tree.nodes;links=worldmat.node_tree.links
mask=nodes.new('ShaderNodeTexImage');mask.image=bpy.data.images.load(str(WORLD/'WorldMetalGloss.png'));mask.image.colorspace_settings.name='Non-Color'
split=nodes.new('ShaderNodeSeparateColor');links.new(mask.outputs['Color'],split.inputs[0]);links.new(split.outputs['Red'],bs.inputs['Metallic'])
sub=nodes.new('ShaderNodeMath');sub.operation='SUBTRACT';sub.inputs[0].default_value=1;links.new(mask.outputs['Alpha'],sub.inputs[1]);links.new(sub.outputs[0],bs.inputs['Roughness'])
glow=nodes.new('ShaderNodeTexImage');glow.image=bpy.data.images.load(str(WORLD/'WorldGlow.png'));links.new(glow.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=1
wire=texturemat('Actual wire fence cutout',WORLD/'ChainLink.png',.84,True)
dirt=texturemat('Actual dirt/wear layer',WORLD/'GroundLayers.png',.96,True);wet=texturemat('Actual rough puddle layer',WORLD/'GroundLayers.png',.58,True)
manifest=json.loads((MODELS/'asset_manifest.json').read_text())['assets'];cache={}
def place(name,p,scale=(1,1,1),yaw=0):
    if name not in cache:
        before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(MODELS/(name+'.fbx')))
        o=next(o for o in bpy.data.objects if o not in before and o.type=='MESH');o.data.transform(o.matrix_world);o.parent=None;o.matrix_world=Matrix.Identity(4)
        o.data.materials.clear();o.data.materials.append(worldmat if manifest[name].get('pack')=='ScrapshiftWorld' else legacy)
        for poly in o.data.polygons:poly.material_index=0
        cache[name]=o.data
        for extra in set(bpy.data.objects)-before:bpy.data.objects.remove(extra,do_unlink=True)
    o=bpy.data.objects.new(name,cache[name]);bpy.context.collection.objects.link(o);o.location=(p[0],p[2],p[1]);o.scale=(scale[0],scale[2],scale[1]);o.rotation_euler.z=math.radians(-yaw);return o
nav=(ROOT/'Assets/Scrapshift/Core/YardNavigation.cs').read_text()
anchors={i+1:(float(x),float(z)) for i,(_,x,z) in enumerate(re.findall(r'new YardDestination\(YardLandmark\.(\w+),"[^"]+",(-?[\d.]+)f?,(-?[\d.]+)f?\)',nav))}
# Numeric anchors used here precede indirect salvage coordinates in the authoritative catalog.
for p in json.loads((WORLD/'WorldDressing.json').read_text())['props']:
    ax,az=anchors[p['anchor']] if p['anchor'] else (0,0)
    place(p['model'],(p['x']+ax,p['y'],p['z']+az),(p['sx'],p['sy'],p['sz']),p['yaw'])
place('WorkshopCanopy',(0,0,4));place('ToolWall',(-2.5,0,5.45));place('PortableRadio',(-1.2,1.16,5.25))
for name,p in [('Workbench',(-2.5,0,4)),('BuyingScale',(2.2,0,4)),('PoweredStripper',(7,0,2)),('WireCrate',(-7,0,2)),('Workbench',(-7,0,12)),('SalvageFan',(-7,1.1,11.95)),('WireCrate',(-33,0,-19)),('WireCrate',(33,0,-19)),('WireBundle',(-2.5,1.19,3.9)),('CopperBundle',(-2.15,1.17,4))]:place(name,p)
place('WorkshopCanopy',(-7,0,12),(.26,.7,.5))
for x,z in [(-30,-6),(-33,19),(31,-22)]:place('WireCrate',(x,0,z))
for row in range(3):
    for col in range(3):
        place('WornHatchback' if (row+col)%2 else 'RustyHatchback',(-37+col*7,0,-2+row*8))
        place('SortingSkip',(23+col*7,0,-1+row*8))
for i in range(6):place('ShippingContainer' if i%2==0 else 'TealContainer',(-34+i*13,0,30))
place('YardOffice',(-16,0,-30),yaw=180);place('OfficeDetails',(-16,0,-30),yaw=180)
for i in range(3):place('PalletBundle',(16+i*5,0,-30))
for i in range(4):place('PalletBundle',(23+i*5,0,-27))
def box(name,p,size,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(p[0],p[2],p[1]));o=bpy.context.object;o.name=name;o.scale=(size[0],size[2],size[1]);o.data.materials.append(mat);return o
metal=texturemat('Existing sorting roof',ROOT/'Assets/Scrapshift/Art/Textures/CorrugatedMetal.png',repeat=10)
for x in [20,42]:
    for z in [-22,-29]:box('Existing sorting shelter post',(x,2.1,z),(.2,4.2,.2),metal)
box('Existing sorting roof',(31,4.2,-25.5),(24,.18,9),metal)
for x in [-9,9]:box('Existing gantry upright',(x,4,24),(.6,8,.6),metal)
box('Existing gantry beam',(0,8,24),(19,.7,.8),metal)
box('Existing loading platform',(24,.3,22),(15,.6,6),metal)
for i in range(4):place('PalletBundle',(19+i*3,.6,22))
# Metre-scaled boundary UVs and the same fixed post spacing as the runtime helper.
for x,z,width,alongx,height in [(0,39.5,96,True,2.2),(-47.5,0,80,False,2.2),(47.5,0,80,False,2.2),(-26,-39.5,44,True,2.2),(26,-39.5,44,True,2.2),(0,-39.5,8,True,2)]:
    pts=[(-width/2,0,0),(-width/2,0,height),(width/2,0,height),(width/2,0,0)] if alongx else [(0,-width/2,0),(0,-width/2,height),(0,width/2,height),(0,width/2,0)]
    mesh=bpy.data.meshes.new('Runtime fence plane');mesh.from_pydata(pts,[],[(0,1,2,3)]);uv=mesh.uv_layers.new();mesh.materials.append(wire)
    for k,v in enumerate([(0,0),(0,height),(width,height),(width,0)]):uv.data[k].uv=v
    o=bpy.data.objects.new('Open fence',mesh);bpy.context.collection.objects.link(o);o.location=(x,z,0)
    spans=math.ceil(width/4)
    for i in range(spans+1):place('FencePost',(x+(-width/2+width*i/spans if alongx else 0),0,z+(0 if alongx else -width/2+width*i/spans)))
# Floor uses the unchanged real gravel texture, with original additive layered wear.
gravel=texturemat('Existing metre-scaled yard gravel',ROOT/'Assets/Scrapshift/Art/Textures/Gravel.png',repeat=96)
bpy.ops.mesh.primitive_plane_add(size=1);floor=bpy.context.object;floor.scale=(96,80,1);floor.data.materials.append(gravel)
outside=bpy.data.materials.new('Outside grassland');outside.diffuse_color=(.26,.3,.2,1)
box('Outside grassland',(0,-.65,0),(180,.4,160),outside)
def patch(x,z,rx,rz,tile,yaw,height,mat):
    c=math.cos(math.radians(yaw));s=math.sin(math.radians(yaw));v=[(x,z,height)];uv=[(tile%2*.5+.25,tile//2*.5+.25)]
    for i in range(12):
        a=i*math.tau/12;r=1+.05*math.sin(i*2.1+x);u=math.cos(a);w=math.sin(a);dx=u*rx*r;dz=w*rz*r
        v.append((x+c*dx+s*dz,z-s*dx+c*dz,height));uv.append((tile%2*.5+.25+u*.24,tile//2*.5+.25+w*.24))
    mesh=bpy.data.meshes.new('Runtime ground patch');mesh.from_pydata(v,[],[(0,i+1,(i+1)%12+1) for i in range(12)]);layer=mesh.uv_layers.new()
    for poly in mesh.polygons:
        for k in poly.loop_indices:layer.data[k].uv=uv[mesh.loops[k].vertex_index]
    mesh.materials.append(mat);o=bpy.data.objects.new('Runtime ground layer',mesh);bpy.context.collection.objects.link(o)
for region in range(4):
    cx=-24 if region%2==0 else 24;cz=-20 if region//2==0 else 20
    for i in range(9):patch(cx+(i%3-1)*12+math.sin(i*4+region)*2,cz+(i//3-1)*10,3.5+i%3,2.3,3 if i%4==0 else 0,i*37,.009,dirt)
    patch(cx-4,cz+4,2.3,1,2,region*29,.012,wet);patch(cx+9,cz-7,1.4,.65,2,region*43,.012,wet)
    patch(-1.15 if region%2==0 else 1.15,cz,.45,8,1,0,.010,dirt);patch(cx,-13,.45,13,1,90,.010,dirt)
world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes.get('Background').inputs['Color'].default_value=(*color('skyFill'),1);world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.8
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=value('sunIntensity');sun.data.color=color('sunColor');sun.data.angle=math.radians(4)
e=math.radians(value('sunElevation'));a=math.radians(value('sunAzimuth'));sun.rotation_euler=Vector((-math.cos(e)*math.sin(a),-math.cos(e)*math.cos(a),-math.sin(e))).to_track_quat('-Z','Y').to_euler()
for p in [(-2.5,2.68,3.65),(2.2,2.68,3.65),(-7,2.5,11.75)]:
    place('FluorescentFixture',(p[0],p[1]+.04,p[2]))
    ceiling=3.28 if p[1]<2.6 else 4.28;length=ceiling-p[1]-.64
    for x in [-.34,.34]:box('Actual ceiling suspension',(p[0]+x,p[1]+.64+length/2,p[2]),(.009,length,.009),metal)
    bpy.ops.object.light_add(type='SPOT',location=(p[0],p[2],p[1]));lamp=bpy.context.object;lamp.data.energy=value('lampIntensity')*40;lamp.data.color=color('lampColor');lamp.data.spot_size=math.radians(100);lamp.data.spot_blend=.35
bpy.ops.object.light_add(type='AREA',location=(-4,-2,3.2));fill=bpy.context.object;fill.data.energy=900;fill.data.size=9;fill.data.color=color('skyFill');fill.rotation_euler=(Vector((0,4,1))-fill.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.cycles.samples=64;scene.cycles.use_denoising=False;scene.eevee.taa_render_samples=32;scene.render.resolution_x=1120;scene.render.resolution_y=700;scene.render.resolution_percentage=100;scene.render.threads_mode='FIXED';scene.render.threads=8
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
bpy.ops.object.camera_add();camera=bpy.context.object;scene.camera=camera
# Camera-relative authored label makes provenance visible in every standalone preview.
labelmat=bpy.data.materials.new('Preview label');labelmat.use_nodes=True;lb=labelmat.node_tree.nodes.get('Principled BSDF');lb.inputs['Emission Color'].default_value=(.85,.87,.8,1);lb.inputs['Emission Strength'].default_value=1
bpy.ops.object.text_add();label=bpy.context.object;label.parent=camera;label.location=(-.58,.325,-1);label.data.body='BLENDER WORLD PREVIEW / NOT UNITY GAMEPLAY';label.data.size=.021;label.data.materials.append(labelmat)
shots=[('EyeHeightForecourt',(0,-20,1.7),(0,4,1.7),25),('WorldOverview',(6,-7,1.8),(-3,7,1.7),25),('WorkshopWorldDetails',(-5.5,1.45,1.75),(-2.4,5.3,1.65),27),('ApplianceLane',(-42,-11,1.75),(-43,8,1.7),28),('OfficeWorldDetails',(-23,-20,1.8),(-16,-30,1.8),28)]
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
for name,position,target,lens in shots:
    if args and name not in args:continue
    camera.location=position;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=lens
    scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
print('WORLD_PREVIEWS: actual exported meshes and runtime JSON, Blender approximated lighting; no Unity/FPS evidence.')
