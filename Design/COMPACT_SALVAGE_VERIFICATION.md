# Recognizable compact-yard salvage — 2026-10-05

The creator requested continued world improvement toward a cozy, worn retro scrapyard. This pass replaces repeated stock silhouettes within the existing perimeter dressing. It introduces no gameplay inventory, simulation or paid dependency, and does not claim a finished game or verified Unity appearance.

## Implemented

Four new original metre-scale FBXs use the existing working `ScrapshiftPropAtlas` and recovered `PropAtlas` material. Older FBXs, PNGs, materials and metadata are untouched. The independent `salvage_asset_manifest.json` and `salvage_asset_audit.json` record only these new names:

| Original resource | Geometry | Source triangles |
| --- | --- | ---: |
| CompactSalvageShell | Already-stripped hatchback: missing doors/glass/hood, open engine bay, exposed seats/frame, bare front hubs | 1,596 |
| CompactWasherLot | Two worn washing machines, one missing door, visible drum, microwave and unplugged lead | 1,648 |
| CompactCableReel | Timber cheeks/board seams, copper winding, axle sockets, chocks and unwound cable | 1,620 |
| CompactRadiatorRack | Open cradle, two radiator cores with cooling fins/end tanks and cut hose stubs | 780 |

`CompactYardClutter` replaces one west panel bin with stripped-car stock and two repeated fridge groups with washer/electronics/reel lots. Three existing drum groups gain timber reels; two existing panel groups gain radiator cradles. Small ground offcuts now include hollow rubber seals instead of rectangular rubber strips. One shell, two washer lots, five reels and five radiator racks share the existing 29 pooled roots, eight batching sectors, 21 grounding meshes and thirteen optional coarse collision volumes.

The central construction area, fixed services, stock envelopes and `.4m` occupancy clearance are unchanged. Each new model is fitted after rotation. Missing new assets choose the previous bin/fridge/coil/motor stock, rather than leaving a new empty stock pocket. Meshless/zero-sized/nonfinite imported stock rejects and disposes only its private failed instance. Imported source meshes remain Unity-owned; procedural rings/contact meshes retain the existing owner cleanup. There are no new lights, rigidbodies, interaction targets or per-object updates.

## Executed in Cloud

- Blender 4.3.2 reimports all four actual FBXs and verifies independent pack membership, metre dimensions/grounding, finite vertices/normals, nondegenerate triangle areas, atlas UVs, one material, hashes and a 3,500-triangle per-model cap. A tiny microwave-window bevel initially produced degenerate faces; the source bevel was corrected and the final exports pass.
- Production `CompactYardClutter.Build`, `Describe`, `Model`, `IsBlocked` and `RefreshVisibility` compile and execute against narrow hierarchy/math/mesh adapters using actual Blender-imported bounds and triangle counts. All imported/procedural bounding corners fit their declared envelopes under a translated/rotated yard; stock/collision pooling hides and restores without deleting candidates. Invalid geometry retains its shared source and leaves no phantom instance.
- Public stock is **193 pre-batch renderers / 42,960 placed triangles**, compared with actual previous checkout **203 / 34,860** with the current CC0 tyres: ten fewer renderers and 8,100 additional triangles. The older atmosphere estimate of 33,012 predates that tyre integration. These are source/adapter budgets, not measured Unity frame rates. One valid local piston can add at most 804 triangles within the supplied 45,012 test cap.
- Omitting all four new models safely uses the existing stock: **203 renderers / 35,532 triangles**, including the new ground seals. Twenty-nine envelopes/thirteen optional colliders and pooling still pass. Source geometry and fallback budgets are checked separately.
- `python3 Tests/audit-compact-salvage.py` passes portable FBX/hash/dimension/membership/metadata/fallback integration checks. Existing original asset and GUID checks pass. Runtime and the expanded test wrapper compile with warnings as errors against the controlled API adapter. Seven additional native cases cover four actual model imports/shared ownership, composition/stock pooling, missing sources and zero-extent/meshless cleanup; all native cases remain unrun here.

[Actual exported prop inspection](Previews/CompactSalvageProps.png) uses the existing atlas in Blender with studio lighting and a visible disclosure. It is an inspected asset preview, not Unity gameplay, lighting/collision verification or FPS evidence. The source scripts author only the new pack:

```sh
blender --background --python-exit-code 1 --python ArtSource/build_compact_salvage.py
blender --background --python-exit-code 1 --python ArtSource/audit_compact_salvage.py
python3 Tests/audit-compact-salvage.py
MESA_SHADER_CACHE_DIR=/tmp/scrapshift-mesa blender --background --python-exit-code 1 --python ArtSource/preview_compact_salvage.py
```

Regenerating the four new models overwrites their new FBXs/manifest and invalidates the prior audit hashes; rerun the audit. Existing atlas and other packs are not regenerated. Blender is an authoring dependency; tracked FBXs need no Blender installation to play in Unity.

## Required local Unity checks

1. With Unity closed, pull latest main. Reopen the existing CompactScrapyard, allow four new FBX imports, then start a fresh Play session. No scene regeneration or save reset is required. Check the west stripped shell, north/east washer lots and wooden reels/radiator racks from player height in Laptop/Balanced/Detailed. Verify recognizable shapes, atlas alignment, open apertures, grounding, useful sightlines and restrained lighting. Decoration is already dismantled stock; it is not another scrap job or free material source.
2. Run expanded `CompactClutterTests` and existing world/occupancy tests. Inspect actual imported axes/readability/normals, both batching modes and shared mesh/material cleanup after repeated Play sessions. Confirm old/missing-resource fallbacks without deleting your tracked source assets.
3. Walk around inside stock and fixed office/delivery routes. Existing thirteen solid volumes should still block the player and hide together with scenery when a saved/placed machine, loose item, full conveyor route or player needs that pocket. Build/move/remove into those areas and reload representative saves; keep stock decoration separate from gameplay ownership.
4. Profile Editor and standalone frame time on Laptop, including a busy network and the north/east/west views. Compare actual frame times with the previous build; no performance improvement is inferred from fewer pre-batch renderers. Verify a desktop build includes the new Resources and existing shaders/textures before accepting the art pass.
