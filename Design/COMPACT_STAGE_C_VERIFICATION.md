# Compact yard: Stage C automation

Implemented on top of A/B, following the creator's resumed Oct 2 request and inline **Compact Starter Yard** map. The reference image is `ChatGPT Image Oct 2, 2026, 05_07_37 PM-4000x3000-2000x1500.jpg`; its Windows path is not a runtime dependency. It depicts an example purchased layout, not starting inventory or captured gameplay. The 48 × 36m boundary, southern entrance, southwestern office/sales and southeastern receiving area remain fixed; the interior remains player designed. Existing scenes and scenery are preserved.

## Delivered behavior

- Level 10 unlocks purchased ported storage, Tier 2, conveyors, splitters and mergers. Fresh yards still start with manual tools and scrap. Export and automatic whole-car/appliance intake remain Stage D work.
- Storage holds components/materials, accepts carried bundles and withdraws matching units as one carry bundle. Recovered and noneligible goods stay separate; transferring never grants XP. A batch keeps its selected identity and retires the other source records once. Individual withdrawal remains a core API.
- Tier 2 reserves a supported recipe from its input buffer, retaining exact inputs, yields, timing and recovery eligibility. It waits for power, respects capacity, holds ready outputs until transport/collection and never starts a new batch using elapsed time from before that batch existed. Its recipe selection filters incoming components; processed materials can leave regardless of that input selection.
- Conveyors connect actual equipment ports. Default Tier 1 has a front output adapter; storage/Tier 2 have one input and one output. Splitters have one input/three outputs, mergers three inputs/one output. Routes use outward stubs, straight sections and alternative orthogonal corners. Each individual port supports one link.
- Splitters choose available branches fairly; merger inputs progress fairly. Destinations retain all accepted quantities. Full buffers, filters, slot/identity limits and output reservations cause backpressure rather than discarded or duplicated items. No transport earns XP; only legitimate recovered-material sales do.
- Routes reject occupied ground, other tracks, self intersections/U-turns, fences and protected infrastructure. Connected equipment uses quarter-turn rotations. Preview also checks actual world colliders; pure geometry checks are not a physics substitute. Belt geometry is deliberately collider-free so the existing player can cross a line without a jump control; placement still reserves the track corridor.
- Price is the catalogue price per 2m section, rounded up for the full route. Default €12/section, speed 1.2m/s, spacing .65m, eight moving units/link, 12m/link and 64 links. Empty belts refund half their actual paid price. Equipment must be emptied and disconnected from both cables and belts before moving/dismantling.
- Free wiring now leaves processing headroom instead of filling every slot with unsellable raw inputs. Rejected acquisitions/placements/withdrawals leave inventory and money unchanged.

## Controls and player flow

Open the catalogue using the saved Build binding (default **B**), or the pause/office menu. Choose equipment, rotate with the saved Rotate binding (**R**) and confirm a valid preview with Interact (**E**). Conveyor purchase enters port selection: aim at an output, select it, then aim at a different input. The preview shows route, validity and full price. Rotate switches the elbow; Interact purchases the valid route; **Escape/B cancels without spending**. Equipment pages can start from a specific output and dismantle an empty attached belt.

Inspect storage/junctions for deposit, grouped withdrawal and output filters. Inspect Tier 2 to select compatible input recipes and connect its generator. One default generator supplies 6kW; Tier 2 draws 5kW and Tier 1 3kW, so both together need more supply. Overload stops all consumers on that network without losing progress. Menus, Settings, rebinding and lost focus freeze simulation; held activation input must be released before resuming gameplay. Construction keeps machines running while directing action input to the preview.

Feed the line with components recovered through existing manual car/fridge dismantling or free wiring. Withdraw materials from output storage and sell at the existing counter. Whole-object feeding and automatic sales are not part of this stage. Equipment unlocks enable purchases, without granting machines or resetting existing progress.

## Persistence and compatibility

Compact saves retain the filename **yard-v2.json**, now containing **schema version 3**. Version-two files undergo complete source validation, then gain an empty conveyor list and default filters/cursors in memory. Cash, experience, placements, IDs, inventory, partial work and exact job snapshots are retained. Read does not rewrite the original file; the next normal atomic save retains it in `.bak`. Existing backup recovery, unreadable-file retention and archived New yard behavior stay in force. Version-one legacy files and shared control/video/audio preferences remain separate and untouched.

Schema three includes link endpoints/ports/corner choice/paid price/cooldown and every moving unit's identity, kind, quantity, progress and recovery eligibility. Validation covers the combined global identity/record budget, geometry, capacities, ranges and connected endpoints. A required root conveyor list prevents accepting incomplete schema-three files. Lower editable capacity/transport limits preserve valid existing durable records rather than deleting them.

Older custom balance assets receive only zero/missing new transport defaults. Positive custom tuning survives, negative values stay invalid. Exact historical default placeholder names enable/rename the delivered Stage C entries; other custom names/choices/stats remain. Unity and URP versions, input system and material-recovery implementation remain unchanged.

## Art and runtime cost

Four original Blender-authored models use the existing worn PropAtlas: ported storage, Tier 2 scrapper, splitter and merger. Total **5,824 triangles**, maximum **2,184** per model, one shared material each. Authoring lives in `ArtSource/build_automation_assets.py`; Blender round-trip data and hashes are separate from earlier manifests. Earlier source FBX/PNG/material/meta files remain unchanged. No paid or downloaded art, campaign publication or copied reference-game assets.

Each belt builds three owned combined meshes for track/support/roller/direction details. Geometry and core route/port tables are cached until a layout or footprint changes. Active moving parts reuse pooled original visuals, with at most **16 spare roots total**, not an unbounded pool per material. No moving-part Rigidbody/collider/component Update, new light or realtime reflection probe. Static machine bodies/colliders/targets survive changes to contents/jobs; only their small current-contents/status children change. View refresh and HUD retain existing throttles. Full-buffer fingerprint strings remain a potential profiling target.

Transport bounds each frame's transfers to one arrival per input and one launch per output, using elapsed time for motion/cooldown. A severe long frame slows throughput conservatively instead of rushing new items through multiple links. These are source-level budgets and conservation behavior, not measured FPS improvements.

## Executed cloud checks

Run `SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-core-tests.sh` in this cloud workspace. **191 scenarios pass, zero fail**, compiling real core/scenario sources with warnings as errors. Coverage includes legacy and compact material/money conservation, line filters/fairness, power/pause, durable snapshots, batch withdrawals, recovery headroom, geometry/refunds/IDs/cache invalidation and long-run/reconstruction checks. The same runner exercises **three legacy and nine compact real filesystem branches** using narrow snapshot adapters, plus **seven real material-recovery branch groups** using a Unity API adapter. Those adapters do not verify Unity JSON or rendering.

Executed packaging checks:

```sh
python3 Tests/audit-original-assets.py
python3 Tests/audit-world-assets.py
python3 Tests/audit-compact-assets.py
python3 Tests/audit-automation-assets.py
python3 Tests/audit-material-bindings.py
```

Blender export/reimport checks cover the four new models; manifests/hash checks cover the original 31, earlier nine compact and new four models. Seven material-reference/authoring cases, 256 complete unique metadata GUIDs and 133 C# syntax parses pass. Supplemental coordinator/domain and main integration-test compilation against minimal local Unity/NUnit API adapters catches C# contracts/scoping, not engine compilation. **65 new supplied Unity cases** cover real JSON, buffer/belt/build cancellation, stable machine bodies, bounded visual pools, imported meshes/ports/materials and collider-free transport. **All Unity tests remain unrun here.**

Requested agents owned scrapping/storage/tuning, construction/transport and world/art; main owned shared state, migration, menus/HUD/coordinator, runtime pooling, filesystem integration, documentation and push. Construction independently reviewed main integration and identified pooling/batch usability improvements addressed in this pass.

## Required Unity verification

1. Pull, stop Play and import in **Unity 6000.3.25f1 / URP 17.3.0**. Confirm zero red Console errors. Run **Scrapshift → Create or Open Compact Yard**, then open the compact scene. Do not reset saves to test the upgrade. Run EditMode tests and inspect results rather than treating skipped/empty XML as success.
2. Open an existing A/B save containing partial manual/powered work; verify cash, items, IDs, positions, job progress and preferences. Save/restart, inspect schema three and the original schema-two backup. Test corrupt-primary/backup/archive recovery with disposable test files.
3. Verify a fresh yard has the fixed corners/open interior and no gifted advanced machines. Check the manual progression, renewable supply near capacity and paid level-ten unlocks. A temporary test copy of Balance may shorten the XP curve for focused automation testing; preserve production values and ordinary saved progress.
4. Place input storage → powered Tier 2 → output storage; select a wire recipe, deposit several loads, connect belts and withdraw/sell grouped outputs. Confirm every expected copper/insulation quantity, proper sale eligibility and no XP from transfers. Inspect recognizable original meshes, socket/cable alignment, ports, arrows, ground contact and current status.
5. Exercise both elbows and rotated endpoints; reject crossings/gear/fence/entrance/delivery/scenery/loose scrap. Check nearest-port targeting, preview/color/cost, free cancellation, refunded empty removal and protected connected/loaded moves. Confirm walking across collider-free belts and approaching every storage port.
6. Test three splitter branches, multiple merger inputs and output filters; deliberately fill downstream storage. Watch retained moving items, stable spacing and resumed flow after grouped withdrawal. Try unsupported Tier 2 inputs and changed recipe selection while old output waits.
7. Disconnect/overload power mid-job and restore it; pause every menu, Settings page and capture mode while items move. Hold Interact/Work/Build through menu dismissal. Cancel captures with Escape and check actual-binding prompts/default restoration/restart. Neither work nor movement may leak.
8. Save/restart a powered partial job, ready output, full branch and moving units with cooldowns. Verify no duplicate sale/output, no item loss and exact recovery eligibility. Compare before/after materials through a full chain.
9. Inspect Laptop/Balanced/Detailed after material repair; if white props persist collect **Scrapshift → Diagnose Rendering** output. The prior screenshot-driven texture fix still lacks local confirmation. Profile full moving/buffered networks in Editor and standalone on the laptop, especially garbage collection and view refresh; verify audio/menu/build/shader cleanup and a desktop player build.

Next implementation milestone is **D: scheduled delivery/intake, primary car/appliance processing, connected sorting and export sales**, with another explicit migration and conservation tests. Production completion, visual acceptance, pacing and a verified desktop build remain outstanding.
