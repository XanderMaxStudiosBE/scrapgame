# Scrapshift map redesign — creator feedback, October 9, 2026

Status: DESIGN REQUEST, NOT IMPLEMENTED. The creator rejects the current map shown in the attached real Unity screenshots. The next task is a deliberate layout/art pass, not another scatter-prop pass. Sound also needs improvement but is deferred until after the map.

## Required inputs

- `current-01-yard.png`, `current-02-delivery.png`, `current-03-workbench.png`: actual creator screenshots of the rejected map. Inspect the images, not only their filenames.
- `layout-plan.svg`: proposed spatial composition, drawn to an approximate 48 x 36m proportion. It is a design diagram, not gameplay or a mandatory equipment arrangement. Adapt its orientation to the actual scene's gate/office/delivery coordinates.
- `CLOUD_PROMPT.md`: complete continuation instructions.
- `../VisualTargets-2026-10-08/`: previous concept references for worn surfaces, human-scale props and readable work areas. This new brief supersedes their background building arrangement where it conflicts with a single distinctive workshop.
- Current root `AGENTS.md` and `PROJECT_HANDOFF.md`, plus latest reference integration/live feedback verification documents. Preserve the newer implemented systems.

## What the screenshots show

The dominant floor reads as one broad, uniform sandy plane. The workshop is visually weak: a bench, toolboard and assorted props appear detached from an enclosing work area. Similar brick warehouses repeat across the background and compete for attention. Tyres, skips, shelves and reels are separated by empty floor rather than forming believable storage/activity groups. The map needs structure and identity before adding more objects.

These observations come from three views, not an overhead survey or measured lighting diagnosis. Confirm them against the actual scene. Do not infer missing colliders, broken materials or incorrect world dimensions from screenshots alone.

## Proposed composition

Keep the compact footprint and existing simulation. Orient the diagram with the entrance at the bottom. Positions are relative design targets, not runtime coordinates.

1. **Arrival:** clear gate, recognizable SCRAPSHIFT sign and small office/sales counter nearby. A vehicle-width worn lane leads to receiving; the entrance view makes the office and workshop easy to find.
2. **Workshop landmark:** one modest, distinctive workshop at the left perimeter with a connected open-sided canopy facing the yard. Visible door, roof silhouette, structural posts and practical service details. Work happens under/alongside this canopy rather than on a stranded bench. The building may be scenery; do not invent a new inaccessible gameplay interior.
3. **Receiving:** short recognizable dismantling/delivery pocket near the entrance and accessible workshop side. A car/fridge, trolley or components show its purpose. Leave approach and whole-object delivery footprints functional.
4. **Buildable production floor:** contiguous centre/right space for purchased machines, belts, power and storage. Keep at least half the usable starting floor free for building/circulation. Leave space for short production lines and player movement; no compulsory pads or decorative lanes that become invisible construction blockers.
5. **Scrap edges:** small purposeful perimeter groups (tyres by a rack; appliances with scrap-metal bay; cable reels near stock shelves). Vary group silhouette and height. Suppress/move cosmetic groups when they conflict with player construction using existing occupancy behavior.
6. **Outside the fence:** one distant industrial silhouette and varied trees/bushes/utility lines to anchor the setting. Reduce repeated warehouses dominating every direction. No expanded playable area or expensive detailed town.

The diagram shows workshop architecture as proposed perimeter scenery, not a purchased machine. This extends the old fixed-infrastructure list only as necessary for that landmark. Keep gameplay equipment freely placeable, including under the shelter where clearance permits. Fresh starter workbench placement may favor it; existing player benches stay where saved.

## Ground, lighting and props

Use distinct but restrained ground regions: worn concrete at the workshop, compacted gravel in the building area, darker wheel-worn dirt on the receiving lane, edge weeds/rubble by fences. Blend transitions through actual project-supported materials/meshes/decals; do not hide the same tiled plane under hundreds of tiny boxes. Keep navigation and construction on a predictable level surface initially.

Use coherent texture scale and an olive/rust/ivory/charcoal palette. Retro means intentional restrained detail, not random texture noise. Keep outdoor shadows readable and distinguish warm canopy light from daylight. Test the current URP exposure/shadow settings before adding lights. Give workshop props believable ground contact, thickness and proportions.

Reuse authored models/materials where suitable. Inventory assets already present before making replacements. Do not download paid assets, introduce required accounts or publish locally imported third-party packs without established licensing. Preserve Campaign sources and Unity GUIDs.

## Compatibility and implementation

Inspect `Assets/Scrapshift/Runtime/CompactYardBootstrap.cs`, `CompactYardBackdrop.cs`, `CompactYardClutter.cs`, `CompactDressingOccupancy.cs`, `YardGroundDressing.cs`, `YardLighting.cs`, and the actual editor scene-generation entry point. These are starting points, not an exclusive edit list. Trace what generates the screenshot before changing code.

Only revise current environment systems or integrate one explicit compatible layout variant. Do not leave the requested map in an unused scene. Preserve hand-edited scenes; provide a clear editor action if scene updates are required rather than silently overwriting them. Prefer non-destructive scenery updates around existing saved placements. New fixed geometry must avoid saved equipment, transit routes, delivery bounds and player spawn; if it cannot, retain the legacy layout for that save and document the opt-in/new-yard behavior. Do not delete or teleport paid equipment.

Preserve physical OUT-to-IN snapping, independent input/output bays, manual strokes, powered processing, downstream backpressure, filters, sales/XP, rebindings, pause and the new bounded checkpoints. Preserve port IDs/coordinates, paid jobs and save schema. Do not redesign audio, economy or conveyor algorithms during this map pass.

## Acceptance evidence

Implement a simple layout pass first and validate its silhouette and functional circulation before polishing meshes/textures. Then provide comparable entrance, receiving and workshop views, plus overhead bounds showing free construction space. The diagram's view markers indicate desired inspection directions; match the creator screenshot positions too when comparing before/after.

Check: office/workshop identifiable at player height; fewer competing repeated buildings; bench feels supported by a work area; purposeful stock groups; visible ground regions; delivery and sales work; equipment can be purchased/placed/rotated; a short OUT-to-IN line can be built; an older saved factory remains usable. Check canopy/roof/collision/headroom and that decoration does not hide or block usable mouths.

Use existing presets and profile draw calls, shadows and frame times when Unity is available. Report actual measurements rather than renderer counts as FPS. Add targeted tests for changed occupancy/save contracts only if needed; decorative placement does not need implementation-mirroring tests.

If Unity is unavailable, provide labeled renders using actual authored assets at actual proposed transforms, code/source checks and a local Unity capture checklist. Do not label these gameplay or claim visual acceptance. An image generator cannot substitute for inspecting the implemented map. The creator decides visual acceptance.
