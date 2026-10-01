"""Inspect exported original props up close. Blender asset preview only, not Unity gameplay."""
import bpy
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
PREVIEW=ROOT/'Assets/Scrapshift/Art/Previews/WorkshopDetails.png'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
mat=bpy.data.materials.new('Exported original atlas');mat.use_nodes=True
nodes=mat.node_tree.nodes;bs=nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.85
tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(MODELS/'ScrapshiftPropAtlas.png'))
mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
for i,name in enumerate(['Workbench','PoweredStripper','BuyingScale','SalvageFan','WireBundle','CopperBundle']):
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(MODELS/(name+'.fbx')))
    meshes=[o for o in bpy.data.objects if o not in before and o.type=='MESH']
    for o in meshes:
        o.data.transform(o.matrix_world);o.parent=None;o.matrix_world=Matrix.Identity(4)
        o.data.materials.clear();o.data.materials.append(mat)
        for poly in o.data.polygons:poly.material_index=0
        scale=.7 if i<3 else 1.9
        o.scale=(scale,)*3;o.location=((i%3-1)*2.5,(i//3)*3.6,0)
        low=min(v.co.z*scale for v in o.data.vertices);o.location.z-=low
bpy.ops.mesh.primitive_plane_add(size=100)
ground=bpy.context.object;ground.location.z=-.012
surface=bpy.data.materials.new('Neutral inspection floor');surface.diffuse_color=(.24,.23,.20,1);ground.data.materials.append(surface)
for pos,energy,color,size in [((2,-3,8),1300,(1,.89,.72),7),((-5,3,6),650,(.72,.81,1),5)]:
    bpy.ops.object.light_add(type='AREA',location=pos);lamp=bpy.context.object;lamp.data.energy=energy;lamp.data.color=color;lamp.data.size=size
bpy.ops.object.camera_add(location=(8,-12,10));camera=bpy.context.object
camera.rotation_euler=(Vector((0,1.8,.6))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=10
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=False
scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.world.color=(.3,.3,.3);scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
scene.render.filepath=str(PREVIEW);bpy.ops.render.render(write_still=True)
print('EXPORTED WORKSHOP DETAIL PREVIEW',PREVIEW)
