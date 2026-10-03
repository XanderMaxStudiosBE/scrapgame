"""Prepare Poly Haven's CC0 tyre for repeated retro scenery; run in Blender.

Only this third-party resource is exported. Original yard assets stay unchanged.
Source is tracked under ArtSource/ThirdParty/PolyHaven/OldTyre.
"""
import bpy
import hashlib
import json
import math
from pathlib import Path
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/ThirdParty/PolyHaven/OldTyre'
OUT = ROOT / 'Assets/Scrapshift/Resources/ThirdParty/PolyHaven/OldTyre'

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(SOURCE / 'source.fbx'))
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
assert len(meshes) == 1
obj = meshes[0]
obj.data.transform(obj.matrix_world)
obj.parent = None
obj.matrix_world = Matrix.Identity(4)
# The publisher's tyre stands on its tread; stock piles need a flat, horizontal tyre.
obj.data.transform(Matrix.Rotation(math.pi / 2, 4, 'X'))
bpy.context.view_layer.objects.active = obj
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
source_triangles = sum(len(p.vertices) - 2 for p in obj.data.polygons)
assert source_triangles == 2880
modifier = obj.modifiers.new('Repeated scenery budget', 'DECIMATE')
modifier.ratio = .064
modifier.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=modifier.name)
if obj.data.has_custom_normals:
    bpy.ops.mesh.customdata_custom_splitnormals_clear()
low = Vector([min(v.co[i] for v in obj.data.vertices) for i in range(3)])
high = Vector([max(v.co[i] for v in obj.data.vertices) for i in range(3)])
size = high - low
centre = (low + high) / 2
for vertex in obj.data.vertices:
    vertex.co.x = (vertex.co.x - centre.x) * .6 / size.x
    vertex.co.y = (vertex.co.y - centre.y) * .6 / size.y
    vertex.co.z = (vertex.co.z - low.z) * .165 / size.z
for polygon in obj.data.polygons:
    polygon.use_smooth = True
    polygon.material_index = 0
obj.data.materials.clear()
obj.name = 'OldTyre'
obj.data.name = 'OldTyre / 184 triangles'
triangles = sum(len(p.vertices) - 2 for p in obj.data.polygons)
assert triangles == 184, triangles
assert len(obj.data.uv_layers) == 1
tree = BVHTree.FromPolygons([v.co for v in obj.data.vertices], [p.vertices[:] for p in obj.data.polygons])
assert tree.ray_cast(Vector((0, 0, 1)), Vector((0, 0, -1)))[0] is None, 'Tyre opening must remain hollow'
OUT.mkdir(parents=True, exist_ok=True)
path = OUT / 'OldTyre.fbx'
bpy.ops.export_scene.fbx(
    filepath=str(path), use_selection=True, object_types={'MESH'},
    axis_forward='-Z', axis_up='Y', global_scale=1,
    apply_scale_options='FBX_SCALE_UNITS', bake_anim=False,
    use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False,
)
# Import the actual deliverable instead of treating source geometry as import evidence.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(path))
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
vertices = [obj.matrix_world @ v.co for v in obj.data.vertices]
low = Vector([min(v[i] for v in vertices) for i in range(3)])
high = Vector([max(v[i] for v in vertices) for i in range(3)])
assert all(abs(actual - expected) < .00001 for actual, expected in zip(high - low, (.6, .6, .165)))
assert abs(low.z) < .00001
assert all(math.isfinite(v) for vertex in vertices for v in vertex)
assert sum(len(p.vertices) - 2 for p in obj.data.polygons) == triangles
assert all(-.0001 <= uv <= 1.0001 for corner in obj.data.uv_layers[0].data for uv in corner.uv)
record = {
    'verification': 'Blender FBX round-trip only; Unity import/rendering unverified',
    'sourceTriangles': source_triangles,
    'triangles': triangles,
    'dimensionsMetresBlender': list(high - low),
    'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
}
(SOURCE / 'mesh_audit.json').write_text(json.dumps(record, indent=2) + '\n')
print('PASS CC0 tyre: 184 triangles, flat metre-scale bounds, finite UVs/vertices and hollow opening; exported FBX reimported')
