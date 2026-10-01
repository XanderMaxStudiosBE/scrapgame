"""Original mesh/lighting composition study. Blender render, not Unity/gameplay/shader/FPS evidence."""
import bpy,math,re
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
OUT=ROOT/'Assets/Scrapshift/Art/Previews/YardLightingStudy.png'
profile=(ROOT/'Assets/Scrapshift/Resources/ScrapshiftLighting/CozyAfternoon.asset').read_text()
def color(name):return tuple(map(float,re.search(r'^  '+name+r': \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)',profile,re.M).groups()))
def value(name):return float(re.search(r'^  '+name+r': ([-\d.]+)$',profile,re.M)[1])
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
mat=bpy.data.materials.new('Actual original prop atlas');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.82
tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(MODELS/'ScrapshiftPropAtlas.png'));mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
cache={}
def place(name,pos,scale=(1,1,1)):
    if name not in cache:
        before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(MODELS/(name+'.fbx')))
        obj=next(o for o in bpy.data.objects if o not in before and o.type=='MESH')
        obj.data.transform(obj.matrix_world);obj.parent=None;obj.matrix_world=Matrix.Identity(4)
        obj.data.materials.clear();obj.data.materials.append(mat)
        for poly in obj.data.polygons:poly.material_index=0
        cache[name]=obj.data
        for extra in set(bpy.data.objects)-before:bpy.data.objects.remove(extra,do_unlink=True)
    obj=bpy.data.objects.new(name,cache[name]);bpy.context.collection.objects.link(obj);obj.location=pos;obj.scale=scale;return obj
place('WorkshopCanopy',(0,4,0))
for name,pos in [('Workbench',(-2.5,4,0)),('BuyingScale',(2.2,4,0)),('PoweredStripper',(7,2,0)),('WireCrate',(-7,2,0)),('Workbench',(-7,12,0)),('SalvageFan',(-7,12,1.1)),('WireBundle',(-2.5,3.9,1.19)),('CopperBundle',(-2.15,4.0,1.17))]:place(name,pos)
for row in range(3):
    for col in range(3):place('WornHatchback' if (row+col)%2 else 'RustyHatchback',(-37+col*7,-2+row*8,0))
for col in range(4):place('ShippingContainer',(-21+col*13,30,0))
place('YardOffice',(-16,-30,0)).rotation_euler.z=math.pi
bpy.ops.mesh.primitive_plane_add(size=180);floor=bpy.context.object
floorMat=bpy.data.materials.new('Tracked coarse yard gravel');floorMat.use_nodes=True
floorTex=floorMat.node_tree.nodes.new('ShaderNodeTexImage');floorTex.image=bpy.data.images.load(str(ROOT/'Assets/Scrapshift/Art/Textures/Gravel.png'));floorTex.interpolation='Closest'
uv=floorMat.node_tree.nodes.new('ShaderNodeTexCoord');scale=floorMat.node_tree.nodes.new('ShaderNodeVectorMath');scale.operation='SCALE';scale.inputs[3].default_value=180
floorMat.node_tree.links.new(uv.outputs['UV'],scale.inputs[0]);floorMat.node_tree.links.new(scale.outputs['Vector'],floorTex.inputs['Vector']);floorMat.node_tree.links.new(floorTex.outputs['Color'],floorMat.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);floor.data.materials.append(floorMat)
world=bpy.context.scene.world;world.use_nodes=True
world.node_tree.nodes.get('Background').inputs['Color'].default_value=(*color('skyFill'),1);world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.6
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=value('sunIntensity');sun.data.color=color('sunColor');sun.data.angle=math.radians(4)
elevation=math.radians(value('sunElevation'));azimuth=math.radians(value('sunAzimuth'))
# Unity +Z north maps to Blender +Y, with Y-up mapped to Z-up.
direction=Vector((-math.cos(elevation)*math.sin(azimuth),-math.cos(elevation)*math.cos(azimuth),-math.sin(elevation)))
sun.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
lens=bpy.data.materials.new('Warm task-light diffuser');lens.use_nodes=True
lb=lens.node_tree.nodes.get('Principled BSDF');lb.inputs['Base Color'].default_value=(*color('lampColor'),1);lb.inputs['Emission Color'].default_value=(*color('lampColor'),1);lb.inputs['Emission Strength'].default_value=1.15
for pos in [(-2.5,3.65,2.68),(2.2,3.65,2.68),(-7,11.75,2.5)]:
    bpy.ops.object.light_add(type='SPOT',location=pos);lamp=bpy.context.object;lamp.data.energy=value('lampIntensity')*40;lamp.data.color=color('lampColor');lamp.data.spot_size=math.radians(100);lamp.data.spot_blend=.35;lamp.data.shadow_soft_size=.18
    bpy.ops.mesh.primitive_cube_add(size=1,location=(pos[0],pos[1],pos[2]+.05));bulb=bpy.context.object;bulb.scale=(.31,.19,.025);bulb.data.materials.append(lens)
# Broad study fill approximates URP's unoccluded trilight ambient; it is not a fourth runtime work light.
bpy.ops.object.light_add(type='AREA',location=(-4,-1,5));fill=bpy.context.object
fill.data.energy=1000;fill.data.size=9;fill.data.color=color('skyFill')
fill.rotation_euler=(Vector((0,4,1))-fill.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(-8,-5,1.8));camera=bpy.context.object
camera.rotation_euler=(Vector((0,4,1.25))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=25
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=False
scene.render.resolution_x=1400;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
# Render-world settings approximate the style only; Unity's sky/contact shaders are not exercised here.
scene.render.filepath=str(OUT);bpy.ops.render.render(write_still=True)
print('BLENDER LIGHTING COMPOSITION STUDY, NOT UNITY GAMEPLAY:',OUT)
