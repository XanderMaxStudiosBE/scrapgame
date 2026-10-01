#!/usr/bin/env python3
"""Dependency-free packaging checks. Does not compile, import or render through Unity."""
from pathlib import Path
import hashlib,json,re,struct,wave
ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Assets/Scrapshift'
PROPS=ASSETS/'Resources/ScrapshiftProps'
manifest=json.loads((PROPS/'asset_manifest.json').read_text())
audit=json.loads((PROPS/'asset_audit.json').read_text())
records={record['name']:record for record in audit['assets']}
assert set(records)==set(manifest['assets']), 'FBX audit does not match manifest'
for name,expected in manifest['assets'].items():
    raw=(PROPS/(name+'.fbx')).read_bytes();record=records[name]
    assert raw.startswith(b'Kaydara FBX Binary'),name+' invalid FBX header'
    assert record['status']=='passed' and record['triangles']==expected['triangles']<=5000,name+' missing/budget-failed audit'
    assert hashlib.sha256(raw).hexdigest()==record['sha256'],name+' changed since Blender audit; rerun audit_yard_assets.py'
print('PASS',len(records),'FBX headers, budgets and matching Blender audit hashes (not Unity import)')
raw=(PROPS/'ScrapshiftPropAtlas.png').read_bytes()
assert raw[:8]==b'\x89PNG\r\n\x1a\n' and raw[12:16]==b'IHDR'
assert struct.unpack('>II',raw[16:24])==(512,512),'Unexpected atlas dimensions'
print('PASS 512x512 original atlas header')
audio=ASSETS/'Resources/ScrapshiftAudio';sound_manifest=json.loads((audio/'audio_manifest.json').read_text())
assert {p.stem for p in audio.glob('*.wav')}==set(sound_manifest),'Sound files do not match manifest'
for name,expected in sound_manifest.items():
    with wave.open(str(audio/(name+'.wav'))) as stream:
        assert stream.getnchannels()==1 and stream.getsampwidth()==2 and stream.getframerate()==expected['sample_rate']==22050,name+' wrong format'
        size=stream.getnframes();assert size==int(expected['seconds']*22050) and size<=16*22050,name+' wrong duration'
        peak=max(abs(sample) for sample in struct.unpack('<'+'h'*size,stream.readframes(size)))/32768
        assert .001<peak<=.71,name+' silent/clipped sample data'
print('PASS',len(sound_manifest),'mono PCM sound headers, durations and sample peaks')
guids={}
for path in ASSETS.rglob('*'):
    if path.suffix=='.meta':
        match=re.search(r'^guid: ([0-9a-f]{32})$',path.read_text(),re.M);assert match,path
        assert match[1] not in guids,('Duplicate GUID',path,guids.get(match[1]));guids[match[1]]=path
    else:assert Path(str(path)+'.meta').exists(),('Missing .meta',path)
print('PASS',len(guids),'unique GUIDs and complete asset/folder metadata')
for path in ASSETS.rglob('*.asmdef'):json.loads(path.read_text())
print('PASS assembly-definition JSON; Unity compilation remains unverified')
