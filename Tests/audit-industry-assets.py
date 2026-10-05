#!/usr/bin/env python3
"""Independent original Stage D packaging/hashes. Does not execute Unity."""
import json, hashlib, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];FOLDER=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((FOLDER/'industry_asset_manifest.json').read_text())
audit=json.loads((FOLDER/'industry_asset_audit.json').read_text());records={r['name']:r for r in audit['assets']}
assert manifest['pack']=='CompactIndustry' and manifest['units']=='metres'
assert set(records)==set(manifest['assets'])=={'CompactPrimaryScrapper','CompactExportStation'}
for name,expected in manifest['assets'].items():
    raw=(FOLDER/(name+'.fbx')).read_bytes();record=records[name]
    assert raw.startswith(b'Kaydara FBX Binary') and record['status']=='passed',name
    assert record['triangles']==expected['triangles']<=manifest['triangle_budgets'][name] and expected['materials']==1,name
    assert record['sha256']==hashlib.sha256(raw).hexdigest(),name+' differs from Blender round-trip'
    assert re.search(r'^guid: [0-9a-f]{32}$',Path(str(FOLDER/(name+'.fbx'))+'.meta').read_text(),re.M),name
for old_name in ['asset_manifest.json','compact_asset_manifest.json','automation_asset_manifest.json']:
    assert not set(json.loads((FOLDER/old_name).read_text())['assets'])&set(manifest['assets']),'Earlier models overwritten'
assert {entry['name'] for entry in audit['loaded_objects']}=={'WornHatchback','CompactRefrigerator'}
assert all(entry['status']=='passed' and entry['configurations']==4 for entry in audit['loaded_objects'])
print('PASS',len(records),'new original industry FBX hashes, footprints, budgets, GUIDs and independent pack; Unity unrun')
