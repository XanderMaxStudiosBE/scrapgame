"""Labelled Blender inspection sheet of actual new salvage FBX exports.

blender -b --python-exit-code 1 --python ArtSource/preview_compact_salvage.py
Only the independent Design preview is written. No asset or Unity data changes.
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'Assets/Scrapshift/Resources/ScrapshiftProps'
OUT = ROOT / 'Design/Previews/CompactSalvageProps.png'
OUT.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

atlas = bpy.data.materials.new('Actual existing original atlas')
atlas.use_nodes = True
bs = atlas.node_tree.nodes.get('Principled BSDF')
bs.inputs['Roughness'].default_value = .85
tex = atlas.node_tree.nodes.new('ShaderNodeTexImage')
tex.image = bpy.data.images.load(str(FOLDER / 'ScrapshiftPropAtlas.png'))
atlas.node_tree.links.new(tex.outputs['Color'], bs.inputs['Base Color'])

for name, position, angle in [('CompactSalvageShell', (-1.25, 1.05, 0), 70),
                              ('CompactWasherLot', (1.38, 1.1, 0), 0),
                              ('CompactCableReel', (-1.35, -1.4, 0), -15),
                              ('CompactRadiatorRack', (1.2, -1.22, 0), -8)]:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(FOLDER / (name + '.fbx')))
    imported = set(bpy.data.objects) - before
    meshes = [o for o in imported if o.type == 'MESH']
    for obj in meshes:
        obj.data.transform(obj.matrix_world)
        obj.parent = None
        obj.matrix_world = Matrix.Identity(4)
        obj.data.materials.clear()
        obj.data.materials.append(atlas)
        for poly in obj.data.polygons:
            poly.material_index = 0
        obj.location = position
        obj.rotation_euler.z = math.radians(angle)
    for obj in imported:
        if obj.type != 'MESH':
            bpy.data.objects.remove(obj, do_unlink=True)

ground = bpy.data.materials.new('Preview matte studio ground')
ground.diffuse_color = (.18, .17, .14, 1)
bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -.015))
bpy.context.object.data.materials.append(ground)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE_NEXT'
scene.eevee.taa_render_samples = 32
scene.render.resolution_x = 1500
scene.render.resolution_y = 1100
scene.render.resolution_percentage = 100
scene.render.threads_mode = 'FIXED'
scene.render.threads = 8
scene.world.use_nodes = True
scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value = .65
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value = (.44, .49, .55, 1)
for position, energy, size, color in [((-3, -4, 7), 1100, 5, (1, .89, .72)),
                                     ((4, 1, 6), 700, 4, (.71, .82, 1))]:
    bpy.ops.object.light_add(type='AREA', location=position)
    lamp = bpy.context.object
    lamp.data.energy, lamp.data.size, lamp.data.color = energy, size, color
    lamp.rotation_euler = (Vector((0, 0, .5)) - lamp.location).to_track_quat('-Z', 'Y').to_euler()
bpy.ops.object.camera_add(location=(5.2, -8.4, 7.5))
camera = bpy.context.object
camera.rotation_euler = (Vector((0, -.1, .45)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 7
scene.camera = camera
scene.view_settings.view_transform = 'AgX'
scene.view_settings.look = 'AgX - Medium High Contrast'
label_material = bpy.data.materials.new('Preview disclosure')
label_material.use_nodes = True
bs = label_material.node_tree.nodes.get('Principled BSDF')
bs.inputs['Emission Color'].default_value = (.95, .92, .82, 1)
bs.inputs['Emission Strength'].default_value = 1
bs.inputs['Base Color'].default_value = (.95, .92, .82, 1)
bpy.ops.object.text_add()
label = bpy.context.object
label.parent = camera
label.location = (-3.32, 2.34, -1)
label.data.body = 'ORIGINAL SALVAGE PROPS / ACTUAL FBX EXPORTS\nBLENDER ASSET PREVIEW / NOT UNITY GAMEPLAY'
label.data.size = .10
label.data.materials.append(label_material)
scene.render.filepath = str(OUT)
bpy.ops.render.render(write_still=True)
print('SALVAGE_PREVIEW actual FBX exports and existing PropAtlas; Blender studio lighting, not Unity/gameplay/FPS.')
