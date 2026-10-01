# Concept-world integration — 2026-10-01

The creator adopted the uploaded world brief for the existing game. The three supplied images are AI concept art, retained with their supplied provenance under `Campaign/Scrapshift/references/world-2026-10-01/`. They guide warmth, recognizable subjects and industrial context; their layout is not substituted for saved gameplay coordinates.

## Integrated areas

| Area | Changes in the normal bootstrap yard |
| --- | --- |
| Workshop | Original canopy, benches, buyer and stripper retained. New gutter/downpipe/flashing/bracing pack, pegboard spanners/pliers/hammer/screwdrivers, labelled parts drawers/cases, multimeter/test leads/nuts and suspended fluorescent fixtures. Shelf radio remains decorative; processing displays, rollers and appliance rotors remain independent. |
| Restoration | Matching repair-tool kit and scaled canopy details at the authoritative restoration anchor. Original fault/repair/test interactions remain. |
| Office | Original building retained. Warm window/door glass, desk/paper/lamp silhouettes, half-raised blinds, meter/conduit, rainwater pipes, waiting bench and porch details imply a modest interior. Office remains scenery. |
| Salvage lanes | Twelve original appliance-row instances with recognizable circular-door washers, controls/hoses, refrigerator and microwave on slatted pallets; five faded-teal lean-to shelters along the perimeter. Existing renewable crates, cars and sorting bins remain usable. Appliances are dressing, not new inventory or repair recipes. |
| Boundary/loading | Alternating teal container variant with the original collision footprint; open cutout wire fences with capped posts keep the exact old boundary colliders. Twenty-four textured, branched poplars and three tiled-brick factory silhouettes sit beyond the yard. |
| Ground | Existing gravel floor/collider retained. Four regional sets of irregular dust/moss/tyre wear and small rough puddles, plus seventeen sparse weed clumps. Eight transparent meshes total, 676 vertices/624 triangles. No broad glossy water plane or new physics. |

All twelve new exported models share one separate 512px worn-color atlas, a linear metallic/smoothness mask and localized warm-glass emission. Metal, wood, rubber, leaves and brick have different surface response. Fence and ground use three additional shared URP materials. Existing FBXs, textures, material edits and their metadata are preserved. The default lighting profile and its four-light budget remain; task-fixture geometry follows the existing lights. No realtime probes, fullscreen effect, per-frame scenery scans or active scrap physics were added.

`WorldDressing.json` contains 65 bounded placements. Station-relative entries resolve through `YardNavigation`; startup validation rejects unknown models/districts, invalid anchors, non-finite transforms and excessive count/scale/extent. Dressing joins the existing district static batches; disabled legacy fence renderers are excluded. All added dressing is collider-free. Containers/fences retain their original coarse collision volumes. Existing bootstrap scenes receive the pass automatically on Start/Continue; no scene reconstruction or serialized-reference migration is needed.

## Executed in Cloud

- 97 core scenarios, including conservation/progression/repair/contracts, pass. Three actual SaveStore filesystem/recovery branches pass with the existing narrow JSON adapter; this is not Unity serialization evidence.
- Blender 4.3.2 round-trip: all 31 FBXs have finite vertices, correct metre dimensions, atlas UVs, matching triangle/hash records and at most 5,000 triangles per model. The new pack totals 13,364 triangles across twelve unique models; the largest is the 3,708-triangle tool wall. Outward front/back brick-panel normals are checked to prevent backface disappearance.
- Portable packaging audit checks all FBX hashes, eight unchanged WAVs, complete/unique metadata and lighting links. The world audit checks five original PNG hashes/CRCs, mask semantics, cutout holes, fade variation, material references, all placement transforms and appliance/shelter footprint clearance along protected routes and supply approaches. These footprint checks do not execute Unity physics.
- All 87 Assets C# files parse; Python authoring files compile; `git diff --check` passes. Existing metadata GUIDs, original FBXs/textures/materials, gameplay Core/save/input code, scene/balance assets and package pins remain unchanged.
- Exported assets and the shared layout are reimported for `WorldOverview.png`, `WorkshopWorldDetails.png`, `ApplianceLane.png` and `OfficeWorldDetails.png`. Each is labelled **BLENDER WORLD PREVIEW / NOT UNITY GAMEPLAY**. Blender lighting, ambient fill, sky and tone mapping approximate the composition; they do not validate URP shaders, runtime fog, game UI or FPS. Previews omit some runtime signs/dynamic stock/contact details. No visual match/creator approval or performance gain is claimed.

## Required local Unity checks

Open the existing scene in Unity **6000.3.25f1 / URP 17.3.0**, allow imports to finish and use the ordinary Start/Continue flow. Run all **187 supplied EditMode cases**; none ran in Cloud. The eighteen added cases cover real model imports/scale/shared masks/no simulation, layout validation/anchors, both fence orientations and entry boundary preservation, upward bounded ground layers, and integrated original source/collider footprints. Existing route/raycast, dynamic-workstation, JSON, menu/input and lighting-disposal cases remain relevant.

Inspect the three areas at walking eye height in **Laptop and Balanced**. Check readable shadowed tools and job displays, warm diffusers, paint/wood/metal separation, wire-cutout holes at distance, grass/fence backfaces, factory brick scale, puddle roughness/fog/transparency sorting and absence of shimmer or shader errors. Look at gutters/roof joins and hanging supports. Compare actual Unity captures with the concept references before accepting the visual milestone.

Walk all protected routes and every station approach; interact with the original delivery/salvage crates, wire bench/buyer/machine, storage, board, restoration and diary. Keep dropped bundles reachable near walls, benches, office and lane edges; Continue older saves at remote coordinates/dock height. Finish manual/powered and fan/radio repair/test loops and confirm independently moving parts/output/stock. Save/restart, close days, review both request queues, pause/settings/rebind/Escape/held-input resume and Return to title. Stop Play and confirm the Editor sky/fog/reflection/pipeline/camera globals are restored.

Record Editor and standalone CPU/GPU frame times and memory on the laptop in representative workshop, salvage-lane and distant-yard views. Check draw calls/overdraw, shadow cost and startup/static-batching memory before tuning budgets. No Unity compilation, shader import, playable build, actual collision/input/JSON behavior or measured performance has been verified here.
