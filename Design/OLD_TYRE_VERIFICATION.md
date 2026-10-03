# Free tyre integration — 2026-10-03

The creator requested free tyres and uploaded `old_tyre_4k.blend.zip`. That upload exceeds the environment's 32 MiB transfer limit and was not inspected. Retrieved the matching official **Old Tyre** by **MP**, licensed CC0 by Poly Haven, as a 1K FBX/PNG set. All four files match publisher MD5 and retained SHA-256 checksums. Sources, credit, license and modification records are in `ArtSource/ThirdParty/PolyHaven/OldTyre`.

The original 2,880-triangle mesh is simplified to **184 triangles**, rotated flat, centred and fitted to .6m diameter/.165m height. Blender reimports the actual exported FBX and checks its bounds, UVs, finite vertices and hollow central opening. An inspected Blender render of the exported low-detail mesh shows recognisable worn tread/rubber; it does not execute Unity or establish visual acceptance.

The runtime resource has its own shared **URP/Lit material**, 1K albedo/OpenGL normal maps and a derived linear metallic/smoothness mask. Rubber has zero metallic in RGB; alpha is `255 - source roughness`. Modern/legacy albedo, color and smoothness aliases agree to preserve bindings through URP material upgrades. A narrowly scoped importer sets metre/axis conversion, readable batching meshes, calculated Mikk tangents, no animation/lights/cameras/imported materials/colliders, compressed bilinear/mipmapped textures and normal/linear mask import types. It leaves Store packages and original resources untouched.

The **21 existing tyre positions in three optional piles** now reuse this mesh/material, with different yaw for stacked wear. Centre/outer diameter/height, stock collision envelopes, visibility pooling and regional static batching are preserved. If the model/material is unavailable during import, the prior ring fallback remains. There are no new stock clusters, renderer instances, colliders, active physics, lights, per-frame updates, recoverable items or save changes. The previous triangle estimate increases by 1,848, from 33,012 to **34,860**, within the unchanged supplied 35,000-triangle native test budget. This estimate and reuse are not measured performance results.

## Executed checks

- Blender source/export reimport: 2,880 → 184 triangles, flat metre bounds, finite UVs/vertices and hollow source opening.
- `python3 Tests/audit-old-tyre.py`: original publisher checksums/provenance, exported mesh/audit hash, every pixel's roughness conversion, importer policy and URP map/GUID/legacy links pass.
- Existing original-assets packaging, nine material authoring/reference and six texture-metadata checks pass; existing art/resources remain intact.
- Updated runtime helper/clutter/visibility and new editor importer compile with Mono warnings-as-errors against narrow API adapters. Four new/changed C# syntax parses pass. This is not Unity compilation.
- Two supplied Unity tests check actual imported mesh/bounds/maps/no physics and repeated mesh/material reuse. Both remain **unrun**, alongside the existing batched/unbatched clutter bounds/budget/occupancy tests.

Re-export the model with `blender --background --python-exit-code 1 --python ArtSource/prepare_old_tyre.py`, then rerun the packaging audit. Blender and Pillow are authoring/audit tools only; the local Unity project loads tracked FBX/PNG/materials and does not require them.

## Required local checks

Close Unity before pulling. Keep locally imported Store packs; do not add their raw source files to this public repository. Preserve existing tracked edits if Git blocks the pull. Reopen the current compact scene and Play; no scene regeneration or New yard is required.

Run `OldTyreTests` and `CompactClutterTests` in EditMode. Check tyre height/axis/centre, normal-map tangents, rough nonmetallic rubber, close/oblique views, all video presets, static batching, visibility after equipment placement/restored routes, stock collision clearance, resource cleanup and player-build shader inclusion. Compare Editor/player frame times with the Profiler before claiming improved FPS. Unity is unavailable in Cloud, so these checks and creator visual acceptance remain outstanding.

## Store pack status

The creator reports all three selected Store packs downloaded/imported locally, but exact pack contents/path names have not been supplied. **They are not integrated in Cloud or placed in the runtime yard.** “Add to My Assets” adds an account entitlement; Window → Package Manager → My Assets → Download → Import in the Unity Editor installs files locally. It does not automatically decorate the game.

The GitHub repository is public. Store packages, including free packages governed by the Store EULA, stay local/private unless their actual license permits raw redistribution. Share selected props/dependencies privately when further integration is requested; the CC0 tyre can be tracked publicly. No Store purchase, material conversion or in-game placement is claimed here.
