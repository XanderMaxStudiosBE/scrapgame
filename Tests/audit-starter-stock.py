#!/usr/bin/env python3
"""Portable packaging/hash/membership checks for the original starter stock fixtures.

Uses the actual Blender audit; does not execute Unity import/rendering or claim FPS.
"""
import hashlib, json, re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest = json.loads((FOLDER / 'starter_stock_manifest.json').read_text())
audit = json.loads((FOLDER / 'starter_stock_audit.json').read_text())
expected_triangles = {'CompactPartsShelf': 1600, 'CompactMixedSkip': 632, 'CompactWorkshopRack': 1380}
records = {r['name']: r for r in audit['assets']}
assert manifest['pack'] == 'CompactStarterStock' and manifest['units'] == 'metres'
assert manifest['atlas'] == 'ScrapshiftPropAtlas.png'
assert manifest['runtimeMaterial'] == 'ScrapshiftMaterials/PropAtlas'
assert set(records) == set(manifest['assets']) == set(expected_triangles)
for other_path in [FOLDER / 'asset_manifest.json', *FOLDER.glob('*_asset_manifest.json')]:
    if other_path.name == 'starter_stock_manifest.json':
        continue
    other = json.loads(other_path.read_text())
    assert not set(records) & set(other['assets']), 'Independent source filenames must never replace another pack'

for name, record in records.items():
    expected = manifest['assets'][name]
    path = FOLDER / (name + '.fbx')
    raw = path.read_bytes()
    assert raw.startswith(b'Kaydara FBX Binary'), name
    assert record['sha256'] == hashlib.sha256(raw).hexdigest(), name + ' changed since actual Blender round-trip'
    assert record['status'] == 'passed'
    assert record['triangles'] == expected['triangles'] == expected_triangles[name] <= 2400, name
    assert record['minimum_triangle_area'] > 1e-10, name + ' degenerate face'
    assert expected['materials'] == 1, name
    assert all(abs(record['metre_dimensions'][i] - expected['dimensions_blender'][i]) < .025 for i in range(3)), name
    assert all(abs(record['bounds_unity'][edge][i] - expected['bounds_unity'][edge][i]) < .025
               for edge in ['min', 'max'] for i in range(3)), name
    assert record['bounds_unity']['min'][1] >= -.002 and record['bounds_unity']['max'][1] < 2.5, name
    assert re.search(r'^guid: [0-9a-f]{32}$', Path(str(path) + '.meta').read_text(), re.M), name

for filename in ['starter_stock_manifest.json', 'starter_stock_audit.json']:
    assert re.search(r'^guid: [0-9a-f]{32}$', Path(str(FOLDER / filename) + '.meta').read_text(), re.M), filename

source = (ROOT / 'Assets/Scrapshift/Runtime/CompactYardClutter.cs').read_text()
for name in expected_triangles:
    assert 'Model("' + name + '"' in source, name + ' exported but never placed'
assert 'FallbackShelf(parent,false)' in source and 'FallbackShelf(parent,true)' in source
assert 'FallbackSkip(parent)' in source
assert 'RefreshVisibility(root,occupied)' in source and 'StaticBatchingUtility.Combine(objects,sector.gameObject)' in source
assert all(phrase not in source for phrase in ['AddComponent<Rigidbody>', 'AddComponent<Light>', 'void Update(', 'PlayerPrefs'])
author = (ROOT / 'ArtSource/build_starter_stock.py').read_text()
assert 'b.prepare_materials(write_atlas=False)' in author
assert "'starter_stock_manifest.json'" in author and 'b.stats = {}' in author
print('PASS 3 independent original starter stock FBX hashes, nondegenerate geometry, single atlas material, metre bounds and complete metadata')
print('PASS', sum(expected_triangles.values()), 'unique source triangles; pooled placement, missing-model fallback and existing batching remain connected')
print('Unity import/rendering/collision/performance remain local verification.')
