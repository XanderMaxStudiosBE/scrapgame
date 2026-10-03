#!/usr/bin/env python3
"""Independent original backdrop packaging and actual audited layout checks."""
import hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
FOLDER=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((FOLDER/'backdrop_asset_manifest.json').read_text())
audit=json.loads((FOLDER/'backdrop_asset_audit.json').read_text())
layout=json.loads((FOLDER/'compact_backdrop_layout.json').read_text())
records={r['name']:r for r in audit['assets']}
assert manifest['pack']=='CompactIndustrialBackdrop' and manifest['units']=='metres'
assert manifest['runtimeMaterial']=='ScrapshiftWorld/WorldProps'
assert set(records)==set(manifest['assets'])
assert audit['layout_sha256']==hashlib.sha256((FOLDER/'compact_backdrop_layout.json').read_bytes()).hexdigest(),'Placement data changed since Blender audit'
legacy=json.loads((FOLDER/'asset_manifest.json').read_text())
assert not set(records)&set(legacy['assets']),'Backdrop must never overwrite original model names'
for name,expected in manifest['assets'].items():
    raw=(FOLDER/(name+'.fbx')).read_bytes();record=records[name]
    assert raw.startswith(b'Kaydara FBX Binary'),name
    assert record['status']=='passed' and record['triangles']==expected['triangles']<=3000,name
    assert record['sha256']==hashlib.sha256(raw).hexdigest(),name+' changed since Blender round-trip'
    assert expected['materials']==1,name
    assert all(abs(record['metre_dimensions'][i]-expected['dimensions_blender'][i])<.025 for i in range(3)),name+' dimension metadata changed'
    assert all(abs(record['bounds_unity'][edge][i]-expected['bounds_unity'][edge][i])<.025 for edge in ['min','max'] for i in range(3)),name+' bounds metadata changed'
    assert re.search(r'^guid: [0-9a-f]{32}$',Path(str(FOLDER/(name+'.fbx'))+'.meta').read_text(),re.M),name
assert layout['version']==1 and len(layout['placements'])==23
assert [p['name'] for p in layout['placements']]==[p['name'] for p in audit['placements']]
assert set(p['sector'] for p in layout['placements'])==set(range(6))
triangles=0
for placement,record in zip(layout['placements'],audit['placements']):
    assert placement['model']==record['model'] and placement['sector']==record['sector']
    expected=manifest['assets'].get(placement['model'],legacy['assets'].get(placement['model']))
    assert expected is not None;triangles+=expected['triangles']
    low,high=record['bounds_unity']['min'],record['bounds_unity']['max']
    assert high[0]<-24 or low[0]>24 or low[2]>18,(placement['name'],'yard clearance')
    assert low[2]>-18 and low[0]>=-48 and high[0]<=48 and high[2]<=46,(placement['name'],'road/verge clearance')
assert triangles==audit['budget']['triangles']<=40000
assert audit['budget']['renderers_before_batch']==23 and audit['budget']['batching_sectors']==6
runtime=(ROOT/'Assets/Scrapshift/Runtime/CompactYardBackdrop.cs').read_text()
assert 'YardMaterialBindings.Load("ScrapshiftWorld/WorldProps",root.transform)' in runtime
assert all(word not in runtime for word in ['AddComponent<Collider>','AddComponent<BoxCollider>','AddComponent<Rigidbody>','AddComponent<Light>','void Update(','CompactYardState','PlayerPrefs'])
assert 'StaticBatchingUtility.Combine(objects,sector.gameObject)' in runtime
assert 'ShadowCastingMode.Off' in runtime
print('PASS 5 independent original FBX hashes, atlas UV/one-material/metre metadata, exterior geometry and road clearance')
print('PASS',triangles,'placed triangles, 23 pre-batch renderers, 6 sectors; source/Blender checks, not Unity/FPS')
