"""Eye-height Blender render of actual backdrop FBX exports and placement data.

blender -b --python-exit-code 1 --python ArtSource/preview_compact_backdrop.py
This is not Unity, a screenshot of gameplay, a finished build or FPS evidence.
Lighting is an explicit Blender approximation; no postprocessed concept art.
"""
import bpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector

ROOT=Path(__file__).resolve().parents[1]
PROPS=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
WORLD=ROOT/'Assets/Scrapshift/Resources/ScrapshiftWorld'
OUT=ROOT/'Design/Previews/CompactBackdropEyeHeight.png'
OUT.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)


def material(name,path,alpha=False):
    m=bpy.data.materials.new(name);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.86
    tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path))
    m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
    if alpha:m.node_tree.links.new(tex.outputs['Alpha'],bs.inputs['Alpha'])
    return m


atlas=material('Recovered existing WorldAtlas',WORLD/'WorldAtlas.png')
fence=material('Original alpha ChainLink',WORLD/'ChainLink.png',True)
roof=material('Existing weathered galvanised metal',ROOT/'Assets/Scrapshift/Art/Textures/CorrugatedMetal.png')
gravel=material('New compact original PackedGravel',WORLD/'PackedGravel.png')
cache={}


def place(model,position,yaw=0,scale=1):
    if model not in cache:
        before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(PROPS/(model+'.fbx')))
        imported=set(bpy.data.objects)-before
        source=next(o for o in imported if o.type=='MESH')
        source.data.transform(source.matrix_world);source.matrix_world=Matrix.Identity(4);source.parent=None
        source.data.materials.clear();source.data.materials.append(atlas)
        for polygon in source.data.polygons:polygon.material_index=0
        cache[model]=source.data
        for obj in imported:bpy.data.objects.remove(obj,do_unlink=True)
    obj=bpy.data.objects.new(model,cache[model]);bpy.context.collection.objects.link(obj)
    obj.location=(position[0],position[2],position[1]);obj.rotation_euler.z=-math.radians(yaw);obj.scale=(scale,)*3
    return obj


def plane(name,points,uv,mat):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(points,[],[(0,1,2,3)]);mesh.update()
    channel=mesh.uv_layers.new()
    for loop in mesh.polygons[0].loop_indices:channel.data[loop].uv=uv[mesh.loops[loop].vertex_index]
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);obj.data.materials.append(mat)
    return obj


for p in json.loads((PROPS/'compact_backdrop_layout.json').read_text())['placements']:
    obj=place(p['model'],(p['x'],p['y'],p['z']),p['yaw'],p['scale']);obj.name=p['name']
plane('Verge and yard context only',[(-48,-46,-.05),(48,-46,-.05),(48,46,-.05),(-48,46,-.05)],[(0,0),(24,0),(24,23),(0,23)],gravel)
# Actual original cutout fence and imported posts give eye-height/metre context.
plane('Northern boundary chain link',[(-24,18,.62),(24,18,.62),(24,18,2.1),(-24,18,2.1)],[(0,0),(48,0),(48,1.48),(0,1.48)],fence)
plane('North lower corrugated panels',[(-24,18,0),(24,18,0),(24,18,.62),(-24,18,.62)],[(0,0),(24,0),(24,.31),(0,.31)],roof)
for x in range(-24,25,4):place('FencePost',(x,0,18))
for x in [-24,24]:
    plane('Side chain link',[(x,-18,.62),(x,18,.62),(x,18,2.1),(x,-18,2.1)],[(0,0),(36,0),(36,1.48),(0,1.48)],fence)
    for z in range(-18,19,4):place('FencePost',(x,0,z))

scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=24
scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.render.threads_mode='FIXED';scene.render.threads=8
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.42,.48,.56,1)
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.68
bpy.ops.object.light_add(type='SUN');sun=bpy.context.object;sun.data.energy=1.1;sun.data.color=(1,.9,.76);sun.data.angle=math.radians(4)
e,a=math.radians(45),math.radians(145)
sun.rotation_euler=Vector((-math.cos(e)*math.sin(a),-math.cos(e)*math.cos(a),-math.sin(e))).to_track_quat('-Z','Y').to_euler()
# Soft sky fill is preview-only; no runtime light is created by the backdrop.
bpy.ops.object.light_add(type='AREA',location=(0,9,13));fill=bpy.context.object;fill.data.energy=700;fill.data.size=28;fill.data.color=(.72,.8,1)
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
bpy.ops.object.camera_add(location=(-3,-4,1.7));camera=bpy.context.object;scene.camera=camera
target=Vector((2,31,3.5));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=24
labelmat=bpy.data.materials.new('Preview provenance');labelmat.use_nodes=True
bs=labelmat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.97,.93,.82,1)
bs.inputs['Emission Color'].default_value=(.97,.93,.82,1);bs.inputs['Emission Strength'].default_value=1
bpy.ops.object.text_add();label=bpy.context.object;label.parent=camera;label.location=(-.70,.36,-1)
label.data.body='ACTUAL FBX EXPORTS / BLENDER / NOT UNITY GAMEPLAY';label.data.size=.018;label.data.materials.append(labelmat)
scene.render.filepath=str(OUT);bpy.ops.render.render(write_still=True)
print('BACKDROP_PREVIEW eye-height 1.7m, actual FBX exports/layout, original atlas, approximate Blender lighting; not Unity rendering/gameplay/FPS.')
