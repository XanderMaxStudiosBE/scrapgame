# Compact yard finishing pass — 2026-10-03

This is a coherent starter-chapter implementation, not a release claim. Unity 6000.3.25f1 and URP 17.3.0 remain pinned. The existing compact scene, recipes, construction, power, transport, materials and legacy yard are preserved. Cloud has no Unity editor or player.

## What changed

The compact yard now has a persistent nine-goal workshop journal: inspect scrap, dismantle an object, process a manual batch, sell recovered materials, power a Tier 1 scrapper, complete a powered batch, reach level 10, connect a conveyor and finish the neighbourhood request book. Goals remain complete after equipment is removed. Completing the chapter opens a recap once, with recorded work and income, then free play. Reviewing, Escape and acknowledgement grant no money, XP or items.

Six sequential, editable customer requests give recovered materials a purpose beyond ordinary sales:

| Customer | Request | Units | Completion bonus | Level |
| --- | --- | ---: | ---: | ---: |
| Mara's repair shop | Copper for workshop rewiring | 6 | €6 | 1 |
| Riverside metalworks | Steel for bench frames | 12 | €8 | 1 |
| Neighbourhood makers | Plastic for recycled casings | 8 | €8 | 2 |
| Mara's repair shop | Copper for motor windings | 12 | €10 | 3 |
| Riverside metalworks | Steel for shed supports | 24 | €12 | 5 |
| Neighbourhood makers | Copper for a community workshop | 20 | €20 | 10 |

Requests have no fee, deadline or new clock. Partial deliveries earn the ordinary material value and eligible sale XP for the quantity consumed; the final delivery adds the quoted bonus once. Surplus keeps its identity and recovery eligibility. Imported stock earns no new recovery XP. Wrong materials, locked requests and cash/XP overflow leave both bundle and request unchanged. The same sale calculation commits ordinary and customer transactions.

Delivery requires the physical office sales counter. Pause and Journal can review the request and its quote. Confirmation is available at Sales, including a request page opened from Sales. Leaving that menu context or losing focus clears permission to deliver; UI confirmation is invalidated on page/settings or quote changes. Core recomputes the quote at commit.

The title, pause, welcome and inspection/equipment pages use a consistent scrollable ledger layout. Continue shows actual cash, level, equipment and next milestone. Pause has journal, requests and save status/retry. Help, Credits and the protected archived reset remain accessible. Nested Back restores the previous equipment selection and scroll position. Focus loss pauses, clears old menu/confirmation context and retains the first-shift welcome until explicitly continued.

The HUD separates next action/bearing, held bundle, target hint, progress and notice. Text height is measured for wrapping. Guidance uses actual moved equipment, loose items, saved jobs, customer quotes and fixed world service anchors. It identifies busy, full, unpowered, locked and capacity-blocked situations, and the renewable-wire earning route. Bearings are directional hints, not obstacle pathfinding. Guidance never mutates progress or inventory.

Journal defaults to **J**, appears as a tenth rebindable action and uses current binding labels. Existing seven- or nine-action settings retain every original key/mouse choice, sensitivity and invert-Y; new actions use unused fallbacks if their preferred keys are taken. Escape remains reserved. The legacy yard also opens its existing journal with this action. No input backend migration occurred.

Dismantling now exposes components on the actual original car and fridge: the bonnet opens, the motor lifts clear, the fridge opens its compressor bay, late trim disappears and collected components cease to be displayed. Saved progress reproduces those stages. Original readable meshes are partitioned once into three privately owned meshes, preserving faces, UVs, transformed normals and mirrored winding. Shared source geometry/materials and gameplay collision stay intact. Missing/unreadable models retain their original/fallback rendering. Manual tools pulse after successful work; powered drive marks and motor sound stop when unpowered, output-blocked, ready or paused.

## Compatibility and ownership

- `yard-v2.json` remains schema **3**. Nullable `career` adds journal flags, recorded work/sale totals, completion acknowledgement and a durable active-request snapshot. Older schema 2/3 saves remain valid. Only visible established evidence is inferred; historical quantities, income and customer rewards are not invented. Existing backup/archive and schema-two migration behavior is unchanged.
- Request definitions are in `CompactRules`. An older balance with missing version/array receives defaults in memory. Valid custom definitions stay authoritative; an explicit version-one empty book stays disabled. An active request keeps its agreed material, quantity, customer, bonus and level even if future balance definitions change.
- `controls-v1.json` stays version **1**, separate from yard/video/audio saves. Appended Journal migration validates a copy before changing the old array. Restore Controls Defaults changes bindings/look only.
- The coordinator owns pause, input gating, work animation timing and views. Journal/Settings/menus freeze authoritative processing and transport. No new per-object `Update`, Rigidbody, light, physics scrap or inventory source is introduced.
- Existing tracked models, maps, materials, catalogues, lighting profiles, scenes, package pins and GUIDs are unchanged. No paid assets were bought. Locally imported Store packs remain private and unintegrated except for the previously documented optional local piston.

## Checks executed in Cloud

- `SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-core-tests.sh`: **227 pure C# scenarios, zero failures**, compiled with warnings as errors. This includes 16 new career/request cases, 18 new read-only guidance cases and two Journal migration cases, alongside the existing 191. The full earning-to-completion case starts at zero cash, uses actual dismantling/processing/sales, purchases power and transport, finishes six requests and acknowledges free play without extra rewards.
- The same runner passes **12 real save-filesystem branches**, ten runtime-material groups, nine editor-recovery/report groups and four dressing-occupancy groups. Narrow serializers/value/API adapters are used; this does not execute Unity JSON, imports, rendering or collision.
- Actual combined Core, compact coordinator/menu/view/automation, work/equipment visuals, controller, construction, save and settings source plus new native test wrappers compile with warnings as errors against a local Unity API adapter. This is a source contract check, not Unity compilation.
- **Six controlled coordinator cases execute** with throwing assertions: nested selection/scroll/Back; Pause request rejection without financial changes; physical Sales context and resume cleanup; once-only completion acknowledgement; focus-loss stale Sales context rejection; focus retaining the first welcome. The focus regression fails against the previous implementation and passes the fix. Actual Unity GUI/input/lifecycle are not simulated.
- Work presentation uses the actual original FBXs in a Blender/value-adapter inspection: car partition 1,081/47/48 triangles; fridge 336/264/208. Original face totals/UVs are conserved, transformed normals and mirrored winding are checked, and opened geometry remains within conservative collision envelopes. The inspected Blender preview is not Unity gameplay.
- Original, world, compact, automation and CC0 tyre packaging/hash audits pass; nine material and six texture-metadata checks pass. **309 unique GUIDs** and complete asset metadata pass. **158 C# syntax parses** and `git diff --check` pass. **431 earlier tracked art/material/data/meta/scene/package/settings files** remain byte-for-byte unchanged.

## Supplied native tests — unrun

In Unity, run **Window → General → Test Runner → EditMode → Run All**. New fixtures are `CompactCareerTests`, `CompactGuidanceTests`, `CompactMenuFlowTests` and `CompactWorkVisualTests`. They include the shared pure scenarios, three actual JSON/balance cases, six coordinator cases and nine geometry/presentation cases. All engine tests remain unrun in Cloud.

## Required local play/build checks

1. Pull with Unity closed, reopen the existing compact scene and confirm zero compile errors. Keep the built-in Vehicles package enabled for locally imported vehicle demo scripts. Do not delete scenes/materials/saves to install this change.
2. Start a new archived yard and Continue an existing schema 2/3 yard. Verify nine goals, unknown historical totals, welcome, save feedback and separate custom control/video/audio preferences. Save/restart after a partial request, final request and acknowledged chapter; no replayed reward or new popup after acknowledgement.
3. At Sales deliver two copper units, restart, deliver the remaining four from a larger bundle and verify the surplus remains. Review/cancel repeatedly and check no consumption/reward. Check wrong/locked inputs and ordinary sales. Verify imported goods give cash but no recovery XP. Confirm all six requests lead to free play.
4. Review requests from Pause/Journal with a matching carried bundle; no Confirm delivery should appear. Visit Sales → Requests, switch away from Unity, return and open Pause → Journal → Requests; delivery must remain unavailable. Repeat native menu tests and held-input resume with real events.
5. Rebind Journal, interact and work to keys/mouse, cancel through Escape/Cancel/Back, swap conflicts and restore defaults. Test old seven-/nine-action files with J already occupied. Actual labels must update immediately and persist independently of a new yard.
6. Check title/pause/Help/Journal/requests/catalogue/equipment at 800×600, 720p and a maximized Game view with long binding labels and notices. All buttons/content must remain reachable; panel text/progress/held-item/notice must not overlap. Check Settings opened from title, welcome and Pause; every nested Escape remains paused until the actual resume step.
7. Dismantle the actual car and fridge through every stage, collect each output, save/restart mid-stage and ready/partly collected. Inspect panel/component positions, normals/UVs/materials, shadows, collision and target rays on all presets. Verify unreadable/fallback behavior and cleanup of owned meshes through reload/reset/stop Play. Partition meshes need native import/rendering checks.
8. Pause mid-tool stroke and powered processing, open Settings/rebind, lose focus, disconnect/overload power and block output. Work, conveyor progress, animation and motor audio must freeze/preserve state; resuming must require fresh input. Check real world-resource cleanup on exit.
9. Play the full starter chapter and gather pacing feedback. Profile CPU/GPU/GC and frame time on the creator's laptop in both Editor and a desktop player. No FPS gain is claimed. Build and repeat gameplay, shader/text/audio and JSON recovery checks before considering a release candidate.

## What remains

Stage D scheduled deliveries, primary automatic whole-car/appliance processing, connected sorting/export and an automatic sales endpoint remain unavailable. Native import/rendering, small-window readability, controller/physics/audio, real serialization, full playthrough, laptop profiling, build verification and creator acceptance are still required. Art polish and game pacing should follow that evidence rather than another engine, pipeline or asset reset.
