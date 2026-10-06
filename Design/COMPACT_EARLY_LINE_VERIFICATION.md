# Early production lines and a stocked starter yard — October 6, 2026

The creator's Unity screenshots showed a sparse yard and frustrating level10 transport gates. This pass changes the existing compact game directly. It supersedes the old gate/first-tier transport restrictions in the original automation brief. Unity is unavailable in Cloud: source implementation and checks below are evidence of source behavior, not a rendered or finished player build.

## What changed

- Basic conveyors and ported storage unlock at level1, splitters/mergers at3, faster Tier2 at5. Prices stay12 per2m of belt,70 storage,30 junctions,160 Tier2. Generator45/Tier1 60 and advanced primary350/export200 at level12 remain purchased upgrades. No starting machinery/cash/XP grant.
- Workbench, Tier1 and Tier2 receive supported components through IN and emit finished job yields through OUT. Their queued inputs cannot bypass processing. A bench reserves one batch but still requires manual tool strokes; powered tiers prepare one qualified batch only when powered and able to reserve its full output. Tier2 still runs faster and has a larger bay.
- Inputs and outputs have independent limits, each using the equipment's existing configured capacity. A full input bay can feed ongoing work without occupying its already reserved output space. Output shrink, missing power, allocation limits or a blocked downstream belt preserve paid inputs/progress/items. Completing or moving material never awards sale XP.
- Amber IN / sage OUT mouths have recognizable trays, rollers, stencilled direction labels and selectable physical hit volumes. Body aiming searches nearby mouths of both roles and rejects the wrong side. Choose OUT, preview a straight/elbow route, snap to IN, and confirm its actual price; Rotate changes the elbow and Escape cancels without spending.
- Carried bundles still load idle benches/Tier1 directly when the complete batch fits. Busy stations queue compatible carried inputs, and a supported oversized direct batch falls back to its input queue. Aiming a physical OUT mouth refuses incoming cargo without consuming it. Generators remain power equipment; primary cars/fridges use suitable whole-object intake; exports are material-only sinks.
- Catalogue cards distinguish available equipment from later upgrades, show actual cycle/power/bay/port roles, and use configured levels. Input/OUT counts, manual versus powered work and downstream blockers have matching inspection/HUD guidance. The old journal readiness key/bit remains saved, but completion/text follow the actual storage/belt gate.
- Twenty-four additional modular stock pockets frame the office, manual bench, receiving area and stock-row approaches. New original parts shelves, tool racks and mixed metal skips join stripped car shells, appliances, reels, radiators and CC0 tyres. Shallow office utility fittings/notices/fastener tray make the counters feel used. See [stock verification](COMPACT_STARTER_STOCK_VERIFICATION.md) for actual bounds and budgets.
- Active scenery stays visible when the player approaches or works nearby. Restored spawn positions receive clearance once. When construction or loose items release a hidden module, its collision returns only after the player has walked clear; the existing view cadence checks pending modules without adding per-prop updates.

## Compatibility

Schema4 and filename `yard-v2.json` remain unchanged. Existing `contents` are processor IN; existing snapshotted `job.yields` are OUT. Paid jobs, partial strokes/timers, IDs, moving items, recovery eligibility and manual collection remain supported. Processor durable safety allows each bay up to4096 units independently; storage/junction/industrial shared-stock safety remains4096. Global record/ID limits are unchanged.

Tier1 output index0 stays at its existing -Z point, preserving saved belts. Only the new +Z intake is added. Other existing port coordinates/indices remain intact. No scene regeneration or save reset is needed.

`routingRulesVersion` intentionally initializes0 so absent serialized fields can migrate once. Only exact default named level10 gates change; custom names/gates/statistics and explicit version1 former gates stay authoritative. The tracked default Balance asset now explicitly contains the paced XP curve and current gates. Existing earned XP/cash is retained. Preferences and imported local Store packs are untouched; no third-party Store content was published.

## Executed evidence

The final core run passes340 scenarios. Run `SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-core-tests.sh` in this checkout. Core scenarios exercise earlier transport, manual/powered queues, filters, independent bay limits, partial paid snapshots, capacity changes, safe allocation and conserved imported/recovered lineage. A sustained80-wire feeder with explicitly roomy storage/record budgets produces exactly400 material units; default full stores still block safely; a blocked destination fills the24-unit IN bay, retains ready OUT and resumes across reconstruction without manual queue withdrawal. Read-only guidance is tested against actual transfer queries, including a later compatible motor yield through a steel filter.

The full runner also executes fourteen actual save-filesystem/recovery branches with narrow serializer adapters, ten runtime-material groups, nine editor-recovery/report groups and four actual occupancy collector groups. These adapters do not execute Unity JSON or native physics/rendering.

Supplemental source/API checks execute19 actual coordinator branches, including OUT-mouth rejection, busy-IN queuing, large-stack fallback, unchanged view caching, motor selection, all simulation phases before saving, pause/focus and preference/checkpoint flushing. Actual presentation helpers pass12 existing geometry/motion/ownership groups.32 machine builders across four yaws check real port geometry, exact role hit targets, original outlet coordinates, physics-free ghosts, private meshes, outward normals and bounded geometry; these are controlled value adapters.

Nine source visibility cases pass against the actual dressing, occupancy and clutter code. They cover ordinary bench work retaining all fresh stock flags, restored spawn clearance, gear/loose-item removal while inside a hidden module, diagonal capsule clearance and restoration after walking clear without another transaction. Run `bash /workspace/tooling/scrapshift-adapters/run-dressing-visibility.sh /workspace/scrapgame`; native Unity collision execution remains unrun.

Native EditMode fixtures are supplied for Resources-loaded default gates, absent-version JSON migration, separate bay job JSON/resume, port ray targets/collider counts, manual queues and coordinator interactions. They remain unrun. Original/new asset packaging, Blender round-trip geometry, material/texture and GUID/metadata audits pass. Existing art/material/profile/scene/package/campaign files are preserved except the explicitly revised default compact Balance asset.

## Local Unity acceptance

1. Close Unity, pull the newest main, reopen `Assets/Scrapshift/Generated/CompactScrapyard.unity`, wait for imports and run EditMode tests. Continue the existing yard; do not reset the save. A saved machine overlapping decoration hides only that cosmetic stock module.
2. From gate/office/bench/receiving at player height, verify stock shelves, tools, skips, wreck/appliance lots and tyres are obvious in Laptop/Balanced/Detailed. Confirm counter/gate/delivery approaches and free placement remain clear. Capture the same viewpoints as the rejected screenshots.
3. At low level buy storage/belts/Tier1/generator. Connect power, feed a full input stack and route OUT to storage. Check moving parts, correct IN/OUT snaps, displayed full route cost, elbow change, wrong mouth rejection and Escape without payment. Fill destination, pause/Settings/focus, save/quit/reopen, then clear stock and watch production resume without loss/repeated output.
4. Route storage through the manual bench. Ensure receiving does not perform tool strokes, manual work and collection still work, finished output transfers and a carried oversized recipe queues one batch at a time. Rebind controls and verify hints still use actual bindings.
5. Confirm sorting at3 and Tier2 at5 are useful upgrades. Whole-object intake/export still require earned purchases and explicit recurring-service approval. Test preserved older Tier1 outlet layouts and schema2/3 partial saves.
6. Test actual port colliders/player collision, UI text at small/fullscreen sizes, sounds, repeated Play cleanup and Unity serialization. Profile Editor and standalone CPU/GPU frame times for the added stock/shadow casters. No measured FPS or native rendering/finished-build claim is made by this pass.

## Ownership

Separated agents owned component Core IO/tests, world stock/art, port geometry/native fixtures and catalogue/guidance. A read-only reviewer reproduced the sustained shared-capacity jam and independently verified its fix. Main owned unlock migration/default asset, career readiness, physical port selection/coordinator integration, live bay counts, remaining tests, current documentation and publication.
