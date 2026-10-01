"""Original exported appliance preview. Blender render, not Unity/gameplay evidence."""
import bpy
from pathlib import Path
from mathutils import Vector, Matrix
root=Path(__file__).resolve().parents[1]
props=root/'Assets/Scrapshift/Resources/ScrapshiftProps'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
material=bpy.data.materials.new('Original worn atlas');material.use_nodes=True
bs=material.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.85
tex=material.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(props/'ScrapshiftPropAtlas.png'))
material.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
for name,x in [('SalvageFan',-.5),('PortableRadio',.45)]:
    before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(props/(name+'.fbx')))
    for obj in [o for o in bpy.data.objects if o not in before and o.type=='MESH']:
        obj.data.transform(obj.matrix_world);obj.parent=None;obj.matrix_world=Matrix.Identity(4)
        obj.data.materials.clear();obj.data.materials.append(material)
        for polygon in obj.data.polygons:polygon.material_index=0
        obj.location=(x,0,-min(v.co.z for v in obj.data.vertices))
bpy.ops.mesh.primitive_plane_add(size=50)
floor=bpy.data.materials.new('Neutral preview floor');floor.diffuse_color=(.23,.22,.19,1);bpy.context.object.data.materials.append(floor)
for pos,energy,color in [((1,-2,4),250,(1,.91,.79)),((-2,1,3),140,(.72,.81,1))]:
    bpy.ops.object.light_add(type='AREA',location=pos);lamp=bpy.context.object;lamp.data.energy=energy;lamp.data.color=color;lamp.data.size=3
bpy.ops.object.camera_add(location=(1.7,-3,1.5));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,.38))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=1.9
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=False
scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world.color=(.25,.26,.27);scene.view_settings.view_transform='AgX'
scene.render.filepath=str(root/'Assets/Scrapshift/Art/Previews/ApplianceDetails.png');bpy.ops.render.render(write_still=True)
