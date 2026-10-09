# Compact reference environment — October 8, 2026

The three files in `VisualTargets-2026-10-08` are generated concept references. They were viewed before this pass. The existing authored office, ground maps, fence material, exterior workshop/tree assets and four-light daylight setup were inspected. No Unity executable is available in this workspace, so this document does not claim a rendered game, native physics acceptance, measured FPS or a desktop build.

## Implemented environment changes

`Assets/Scrapshift/Runtime/CompactYardWorld.cs` now gives the office one continuous shallow service awning instead of two isolated flat hoods. Its pitched corrugated sheet, five rafters, raised seams, wall braces, gutter, downpipe and small repair patch use the existing coarse industrial materials. All 21 generated boxes stay within the previously reserved office area, x=-23..-11 / z=-17..-10. The roof remains approximately three metres above the ground; braces rise behind the existing nameplate. No ground posts, colliders, interaction targets, inventory or placement reservations are added. The two existing counter fixtures and lighting anchors remain intact.

Thirty-six large weathered fence screen panels join the existing chain-link and lower sheets. Five boundary spans retain occasional chain-link gaps, varied sheet heights and a mix of corrugated metal/wood, with fixing rails and a top support rail. The closed entry gate remains chain-link. The new geometry occupies the historical fence planes at x=±24 / z=±18 and uses the unchanged fence colliders. This provides a legible yard enclosure without moving the limits or scattering objects across construction space.

The unchanged wheel-lane texture now tiles along 2.5 metres of travelled route rather than stretching one small tread pattern across an entire gate/office/receiving route. Existing shoulders, paired tracks, transparent edges, route geometry, ground collision and private material lifetime remain. The lane mesh still has 24 vertices and 18 triangles; no extra overlay renderer or material is created.

The existing `CompactAfternoon` profile already supplies a bright readable office-facing sun, restrained warm counter/delivery lights and fog beyond useful yard distance. Its bytes, custom profile selection and lighting lifetime behavior are preserved. The existing exterior buildings/trees and all authored FBXs, textures, materials, metadata and scene assets are preserved. The runtime setup tool remains the route for a checkout without its generated scene; existing locally authored scenes do not require regeneration.

Fixed anchors, 48 × 36m limits, original collision dimensions and all previously added stock are retained. This pass adds no fixed workshop, purchased example factory, starting equipment, money, XP or save changes. Equipment and associated presentation remain movable through the existing equipment system.

## Source budgets and executed checks

Compared with the previous environment, the pass adds 77 fence-box renderer objects and replaces two counter hood boxes with 21 canopy boxes: a net increase of 96 pre-batch renderer objects and 1,152 triangles. Regional static batching remains. New screens cast shadows; their 41 rails do not. Only the main canopy sheet casts a shadow; its 20 fittings do not. There are 37 new caster objects and two removed hood casters, a net increase of 35. These counts describe source geometry, not draw calls or FPS. No lights, Rigidbody components or per-object frame updates are added.

Executed in this environment:

- `python3 Tests/audit-compact-ground.py`: original packed-gravel/wheel-lane pixel hashes, CRCs, repeat/mipmap/import policies, transparent lane edges and private material contracts pass.
- `python3 Tests/audit-world-assets.py`: twelve original world models, shared atlas/triangle budgets, five surface-map hashes, material/depth policies and 81 existing dressing declarations pass.
- `python3 Tests/audit-compact-backdrop.py`: original backdrop hashes, metre bounds, road/yard exclusion and 39,428 source triangles / 23 pre-batch renderers / six sectors pass.
- `git diff --check`: no whitespace errors at the environment check.
- Independent floor review compiles the actual `CompactYardClutter.Describe` declarations and fresh core state against narrow vector/bounds declarations. Counting protected service areas plus every interior stock/equipment/scrap footprint conservatively without subtracting overlaps leaves 1,152.45m² (66.69%) of the 1,728m² yard. Expanding every stock/equipment/scrap footprint by 0.4m per side still leaves 934.73m² (54.09%); exact padded union leaves 1,005.39m². The canopy stays within an existing reservation and screens stay on existing boundary planes. These are source-area checks, not native collision execution. See `Tests/run-starter-floor.sh` for the preserved runner.

`CompactReferenceEnvironmentTests.cs` supplies three native EditMode cases. They check the original anchors and 19 fixed colliders; conservatively count fully clear 0.5m floor cells using actual fixed colliders, reserved services, fresh gear/scrap and all interior stock footprints; check the canopy's reserved envelope, generated mesh ownership and caster budget; check 36 screen panels / 41 non-casting rails; and verify lane normals and texture travel against real geometry. Exact triangle expectations apply only to this pass's generated boxes and lane mesh, not imported FBX internals. Unity execution remains unrun.

## Local Unity acceptance still required

1. Close Unity before pulling; reopen the existing compact scene and existing save. When the generated scene is absent, use the repository's compact setup tool. Preserve custom scenes/profiles; no save reset is necessary.
2. Run EditMode including `CompactReferenceEnvironmentTests`, existing compact world/atmosphere/clutter/visibility tests and the saved-layout regressions.
3. Capture actual player-height entrance, office/counter and manual-working-area views at approximately 1.7m eye height / 70° horizontal FOV. Compare all three presets with the concept images. Confirm the yard reads as 48 × 36m, the open floor remains obvious and previous shelves/tools/skips are still visible.
4. Inspect the counter nameplate and wall braces for overlap, the pitched sheet/gutter for z-fighting, fence cutout/opaque depth, panel shadows, rotated/transformed runtime roots and metre tread repetition. Check that both counter approaches and receiving services remain usable.
5. Continue a populated older save. Confirm machinery/belts/loose items still hide only their overlapping cosmetic modules, restored player positions remain protected, and hidden-module collisions restore only after the player leaves. No added scenery should intersect occupied construction beyond the historical fixed reservations.
6. Measure batching, shadow casters and CPU/GPU frame time in Editor and standalone before making a performance claim. Create and play a desktop build. Creator approval of actual engine visuals and game feel remains outstanding.
