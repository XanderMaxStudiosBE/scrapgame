"""Render actual exported industrial FBXs with existing scrap, not Unity gameplay.

blender -b --python-exit-code 1 --python ArtSource/preview_industry_assets.py
"""
import bpy, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];PROPS=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
material=bpy.data.materials.new('Existing worn atlas / preview');material.use_nodes=True
nodes=material.node_tree.nodes;surface=nodes.get('Principled BSDF');surface.inputs['Roughness'].default_value=.86
texture=nodes.new('ShaderNodeTexImage');texture.image=bpy.data.images.load(str(PROPS/'ScrapshiftPropAtlas.png'));texture.interpolation='Linear'
material.node_tree.links.new(texture.outputs['Color'],surface.inputs['Base Color'])


def place(name,point):
    before=set(bpy.context.scene.objects);bpy.ops.import_scene.fbx(filepath=str(PROPS/(name+'.fbx')))
    for obj in set(bpy.context.scene.objects)-before:
        if obj.type!='MESH':continue
        obj.location+=Vector(point);obj.data.materials.clear();obj.data.materials.append(material)


place('CompactPrimaryScrapper',(-4.8,0,0));place('WornHatchback',(-4.8,-.10,.46))
place('CompactPrimaryScrapper',(2.5,0,0));place('CompactRefrigerator',(2.5,-.10,.46))
place('CompactExportStation',(8.15,-.9,0))
bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.location.z=-.02
base=bpy.data.materials.new('Neutral preview ground');base.diffuse_color=(.20,.19,.155,1);base.use_nodes=True
base.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.20,.19,.155,1)
base.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.92;ground.data.materials.append(base)
bpy.ops.object.light_add(type='AREA',location=(-5,-8,12));bpy.context.object.data.energy=2300;bpy.context.object.data.color=(1,.84,.64);bpy.context.object.data.size=9
bpy.ops.object.light_add(type='AREA',location=(6,5,9));bpy.context.object.data.energy=1500;bpy.context.object.data.color=(.69,.78,1);bpy.context.object.data.size=10
bpy.ops.object.camera_add(location=(16,-23,12));camera=bpy.context.object;target=Vector((1,.2,1.5))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=20.7
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=False
scene.render.resolution_x=1680;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.world.color=(.26,.27,.29);scene.view_settings.view_transform='AgX'
scene.render.filepath=str(ROOT/'Design/Previews/IndustrialMachineryAssetPreview.png');bpy.ops.render.render(write_still=True)
print('SCRAPSHIFT_INDUSTRY_SOURCE_PREVIEW '+scene.render.filepath+' — actual source meshes, not Unity gameplay')
