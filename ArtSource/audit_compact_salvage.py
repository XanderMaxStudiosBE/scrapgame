"""Actual Blender FBX round-trip of the independent original salvage pack.

blender -b --python-exit-code 1 --python ArtSource/audit_compact_salvage.py
Verifies exported geometry, UVs, normals, dimensions and independent membership.
This does not execute Unity's importer, rendering, physics or batching.
"""
import bpy, json, math, hashlib, uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest = json.loads((FOLDER / 'salvage_asset_manifest.json').read_text())
records = []
for name, expected in manifest['assets'].items():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(FOLDER / (name + '.fbx')))
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    assert len(objects) == 1, (name, 'single shared-material geometry required')
    vertices = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
    assert all(math.isfinite(c) for v in vertices for c in v), (name, 'finite vertices')
    dimensions = [max(v[a] for v in vertices) - min(v[a] for v in vertices) for a in range(3)]
    assert all(abs(a - e) < .025 for a, e in zip(dimensions, expected['dimensions_blender'])), (name, dimensions)
    bounds = {'min': [min(v[a] for v in vertices) for a in [0, 2, 1]],
              'max': [max(v[a] for v in vertices) for a in [0, 2, 1]]}
    assert all(abs(bounds[edge][a] - expected['bounds_unity'][edge][a]) < .025
               for edge in ['min', 'max'] for a in range(3)), (name, 'bounds')
    assert bounds['min'][1] >= -.002 and bounds['max'][1] < 1.6, (name, 'grounded stock height')
    triangles = sum(len(p.vertices) - 2 for o in objects for p in o.data.polygons)
    assert triangles == expected['triangles'] <= 3500, (name, triangles)
    minimum_area = math.inf
    for obj in objects:
        assert len(obj.data.materials) == 1 and obj.data.uv_layers.active, (name, 'atlas material/UV')
        assert all(math.isfinite(c) and 0 <= c <= 1 for uv in obj.data.uv_layers.active.data for c in uv.uv), (name, 'UV bounds')
        assert all(math.isfinite(c) for p in obj.data.polygons for c in p.normal), (name, 'normals')
        obj.data.calc_loop_triangles()
        for face in obj.data.loop_triangles:
            a, b, c = [obj.matrix_world @ obj.data.vertices[i].co for i in face.vertices]
            area = (b - a).cross(c - a).length / 2
            assert area > 1e-10, (name, 'degenerate triangle')
            minimum_area = min(minimum_area, area)
    records.append({'name': name, 'status': 'passed', 'triangles': triangles,
                    'metre_dimensions': dimensions, 'bounds_unity': bounds,
                    'minimum_triangle_area': minimum_area,
                    'sha256': hashlib.sha256((FOLDER / (name + '.fbx')).read_bytes()).hexdigest()})

path = FOLDER / 'salvage_asset_audit.json'
path.write_text(json.dumps({'scope': 'Blender FBX round-trip, finite vertices/normals, grounded metre bounds, nondegenerate triangles, atlas UVs and single material; Unity import not run',
                            'assets': records}, indent=2) + '\n')
meta = Path(str(path) + '.meta')
if not meta.exists():
    meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n')
print('SCRAPSHIFT_SALVAGE_AUDIT ' + json.dumps(records))
