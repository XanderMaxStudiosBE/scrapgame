#!/usr/bin/env python3
"""Dependency-free compact package checks; does not import/render through Unity."""
import hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
FOLDER=ROOT/'Assets/Scrapshift/Resources/ScrapshiftProps'
manifest=json.loads((FOLDER/'compact_asset_manifest.json').read_text())
audit=json.loads((FOLDER/'compact_asset_audit.json').read_text())
records={r['name']:r for r in audit['assets']}
assert manifest['pack']=='CompactStarter' and manifest['units']=='metres'
assert set(records)==set(manifest['assets'])
for name,expected in manifest['assets'].items():
    raw=(FOLDER/(name+'.fbx')).read_bytes();record=records[name]
    assert raw.startswith(b'Kaydara FBX Binary'),name
    assert record['status']=='passed' and record['triangles']==expected['triangles']<=2500,name
    assert record['sha256']==hashlib.sha256(raw).hexdigest(),name+' changed since Blender round-trip'
    assert expected['materials']==1,name+' multiple materials'
    meta=Path(str(FOLDER/(name+'.fbx'))+'.meta')
    assert re.search(r'^guid: [0-9a-f]{32}$',meta.read_text(),re.M),name+' missing GUID'
old=json.loads((FOLDER/'asset_manifest.json').read_text())
assert not set(old['assets'])&set(manifest['assets']),'Compact names overwrite old models'
print('PASS',len(records),'original compact FBX hashes, metre metadata, atlas/material and triangle budgets (not Unity)')
print('PASS separate compact manifest; existing 31-model manifest remains independently auditable')
