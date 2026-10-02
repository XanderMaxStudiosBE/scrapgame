#!/usr/bin/env python3
"""Actual new-asset hash/packaging checks; not engine rendering or physics."""
from pathlib import Path
import json,hashlib,re
ROOT=Path(__file__).resolve().parents[1];FOLDER=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((FOLDER/'automation_asset_manifest.json').read_text())
audit=json.loads((FOLDER/'automation_asset_audit.json').read_text());records={r['name']:r for r in audit['assets']}
assert manifest['pack']=='CompactAutomation' and manifest['units']=='metres'
assert set(records)==set(manifest['assets'])
for name,expected in manifest['assets'].items():
    raw=(FOLDER/(name+'.fbx')).read_bytes();record=records[name]
    assert raw.startswith(b'Kaydara FBX Binary') and record['status']=='passed',name
    assert record['triangles']==expected['triangles']<=2500 and expected['materials']==1,name
    assert record['sha256']==hashlib.sha256(raw).hexdigest(),name+' changed since Blender audit'
    assert re.search(r'^guid: [0-9a-f]{32}$',Path(str(FOLDER/(name+'.fbx'))+'.meta').read_text(),re.M),name
for old_name in ['asset_manifest.json','compact_asset_manifest.json']:
    assert not set(json.loads((FOLDER/old_name).read_text())['assets'])&set(manifest['assets']),'Overwritten earlier model name'
print('PASS',len(records),'new original automation FBX hashes, budgets, GUIDs and independent pack; Unity unrun')
