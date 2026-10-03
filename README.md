# SCRAPSHIFT

A first-person retro scrapyard automation prototype. The **48 × 36m compact yard** starts with manual car/refrigerator dismantling, component processing and material-sales XP. Buy and freely place powered machines, then at level 10 build conveyor, storage and Tier 2 networks with splitters and mergers. Fixed office/delivery corners leave the interior yours to arrange. The original 96 × 80m repair/order/day game remains a separate legacy scene.

**Current Cloud verification:** 191 pure C# scenarios, twelve real save-filesystem/recovery branches, ten runtime-material, nine editor-recovery/report and four dressing-occupancy branch groups pass (adapters are not Unity JSON or rendering). Nine material-reference/authoring and six texture-metadata checks pass. Five new backdrop FBXs pass Blender round-trip/exterior-layout checks, and two original ground maps pass pixel/import/alpha audits. The earlier art and working textures remain intact. **Unity is unavailable:** engine tests, compilation, rendering, controls/collisions, real JSON, audio, FPS and a desktop build/playthrough still need local verification. The creator confirms textures work and has requested a stronger environment; the latest atmosphere changes still need local visual acceptance. Blender previews are labelled asset examples, not gameplay screenshots.

[Stage C implementation, save migration and local checklist](Design/COMPACT_STAGE_C_VERIFICATION.md) records conveyors, corners, ported storage, Tier 2 and junctions. [A/B verification](Design/COMPACT_STAGE_AB_VERIFICATION.md) and [original compact art](Design/COMPACT_WORLD_VERIFICATION.md) record the earlier work. **Automatic whole-object intake/export and production completion remain future work.**

Existing tyre piles now use a free **CC0 Old Tyre by MP/Poly Haven**, adapted to a shared 184-triangle mesh and 1K URP maps. Source/checksum/mask audits pass; actual Unity appearance and performance need local checks. See [tyre verification and Store import status](Design/OLD_TYRE_VERIFICATION.md). Store packs imported on the creator's machine are not included or placed by this change; keep their source files local/private because this repository is public.

**Optional local Store piston:** if Junkyard Car Parts is installed, stop Play and run **Scrapshift → Enable Imported Car Parts**. It prepares an ignored local mesh for the office spares pocket with existing URP metal. Public checkouts keep the original motor; other Store packs remain unintegrated. [Local setup/verification](Design/LOCAL_CAR_PARTS_VERIFICATION.md) records private file handling and the required Unity checks.

## Open or update in Unity

Use **Unity 6000.3.25f1** and the pinned **URP 17.3.0** packages. After pulling/importing, run **Scrapshift → Create or Open Compact Yard**, then press Play in `Generated/CompactScrapyard`. The original **Create or Open Prototype** command is an alias. It creates only missing generated assets/scenes, preserves existing scene/balance edits, makes the compact scene the first build scene and keeps `Generated/Scrapyard` as the legacy option. Opening the old scene directly still plays the old game; a pull alone does not reconstruct generated scenes.

Compact Start/Continue opens the earning/building loop. Existing compact schema-two saves upgrade in memory to schema three, retaining partial work and ownership; the next save retains the old file in backup. The filename remains `yard-v2.json`. New yard archives compact files; optional legacy import preserves source JSON/files. Cash, portable quantities and exact wire jobs can transfer; old repairs, upgrades, requests/escrow and daily records stay in the legacy scene. Imported machinery needs a purchased generator/cable. Existing inventory gets no invented historical XP. Shared control/video/audio preferences remain separate.

Tune the new recipes/prices/yields/time/power/capacity/XP curve/starting supplies in `Resources/ScrapshiftCompact/Balance.asset`. The old `Generated/Balance.asset` remains for the legacy game. **Update Existing Prototype Visuals** still validates tracked materials without replacing scene or balance data. Do not delete Generated, old saves, custom materials or preferences.

The latest Console screenshot exposed a repeating material repair conflict. WorldProps now authors enabled emission with the GI flags that URP expects; recovery follows Unity's flag rules and preserves deliberately disabled custom emission. Automatic recovery remembers attempted material states using stable asset references and catalogue expectations. A repeated state stops retries with one warning naming the material and changed properties; explicit **Scrapshift → Repair Missing Material Bindings** permits one fresh attempt. Healthy imports settle without repeated saves.

The creator confirms texture recovery now works locally. Their earlier full report showed null PropAtlas/world/ground/fence texture references in both catalogue and materials. Six original atlas/mask/glow/alpha PNGs now have explicit Texture2D import settings with original GUIDs/pixels retained. The catalogue records original paths: editor recovery loads missing references by path before repairing materials, with one forced import per unavailable source and bounded warnings. Valid custom textures remain intact. Runtime uses Resources fallback for those original maps without modifying the loaded catalogue. Standalone build coverage and other engine checks still need local verification.

Close Unity before updating. If a pull is blocked, preserve local material/import changes with `git stash push -m "Unity import backup before update" -- '*.mat' '*.meta' Assets/Scrapshift/Resources/ScrapshiftRendering/Materials.asset`, then `git pull --ff-only`. Keep the backup saved while checking the new import. Reopen, let importing finish, run **Scrapshift → Repair Missing Material Bindings** outside Play, then start Play again; an existing world may retain its earlier private material copies. Inspect props, fence holes and ground transparency on every preset, restart Unity and verify references remain loaded. Run EditMode tests for the six real texture imports and Resources fallback. These engine checks are not executed in Cloud.

If textures remain white, use **Scrapshift → Save Rendering Report...** and choose Desktop. The updated report includes each expected source path, physical file existence, importer type/shape, texture loaded by path, GUID/local file ID and explicit missing-reference errors. Two null references no longer report a successful match. **Diagnose Rendering** copies the complete report; export cancellation leaves files and clipboard untouched. Automatic Play snapshots use `Temp/ScrapshiftRenderingReport.txt`, which can be cleared on editor exit. The repair log is separate from the full report.

## Compact yard loop

1. Inspect delivery car/fridge, finish its manual stages with empty hands, then collect motor/wiring/casing/compressor/plastic outputs individually. Whole large objects stay in the receiving area.
2. Carry components to a manual bench, load and work, then collect every material output once. Sell at the office counter; quotes show prices, level bonus and eligible sale XP.
3. Defaults start at €8. Selling starter scrap plus two renewable wire loads reaches €117, covering a €45 generator and €60 Tier 1. Replacement cars/fridges cost €25/€15; free wiring and at least one retained manual bench protect the earning loop.
4. Select equipment in the catalogue, aim at nearby ground, rotate/grid-snap and confirm a green preview. Cancel spends nothing. Inspect empty disconnected equipment to move/dismantle it; paid equipment refunds half its paid price.
5. Inspect powered equipment to connect physical ports. Supply must meet total connected demand (default 6kW per generator, 3kW per Tier 1). Overload/disconnection pauses work without losing inputs/progress. Tier 1 is manually fed/collected. All menus/Settings pause processing.
6. At level 10, purchase input storage, Tier 2 (5kW), output storage and conveyors. Select output/input ports, switch the preview elbow with Rotate and confirm the displayed full price. Escape cancels freely. Feed recovered components into storage and connect a powered processing chain; full destinations retain their items.
7. Add splitters/mergers and storage output filters to route materials. Withdraw matching output units as one carried bundle and sell them at the counter. Tier 2 recipe filters affect intake. Transfers grant no XP. Empty belts before dismantling; empty and disconnect equipment before moving it.

## Controls and Settings

| Default input | Action |
| --- | --- |
| WASD / arrow keys | Walk with collision |
| Mouse | Look |
| E | Use station / pick up item |
| Left mouse | One manual work stroke at a loaded bench |
| Q | Drop carried item |
| B | Equipment catalogue / cancel construction |
| R | Rotate construction preview |
| Escape | Pause, cancel rebinding/construction or go back |

Pause → **Settings → Controls** lists all nine actions and their current bindings. Existing seven-action files retain every custom key/mouse/look choice; appended B/R use unused fallbacks if already assigned. Select a row, release the activation input, then press a supported key or mouse button (buttons 1–7). Capture commits mouse bindings after release so Cancel/Back remain usable. Conflicts offer **Swap** or **Cancel**, keeping every action bound. Escape is reserved permanently. Default movement aliases cannot silently steal a customized arrow binding. Sensitivity and invert-Y apply immediately; Restore Defaults resets controls/look only.

Gameplay/look are blocked during all menus and capture. Captured input is consumed through release. Resuming requires held menu/gameplay buttons to be released before fresh gameplay input is accepted. HUD/objectives/control hints use actual bindings. Unity's legacy input backend is retained, with centralized action bindings; no input-system migration is needed.

**Video** offers Laptop/Balanced/Detailed, 30/60/120 frame limits, 55–95° FOV, an optional warm grade and an optional gameplay FPS/frame-time readout. Presets use 75/90/100% render scale and 20/35/50m shadow distance. Laptop uses shorter hard shadows and disables post processing. Balanced/Detailed can use subtle warm grading and bloom. The HUD remains at display resolution. Settings change a private runtime URP clone and restore Editor pipeline/VSync/frame cap when it is disposed.

**Audio** controls master, effects and ambience. Original gravel footsteps, tool/handling/sale cues and nearby machine/fan loops and the radio listening test provide feedback; outdoor breeze and sparse birds provide ambience. Gameplay sounds pause with menus; quiet ambience continues. Video/audio defaults are separate from Controls defaults.

## Lighting and visual direction

The compact yard now uses **bolted ivory signs with bold charcoal lettering** at the office, shop, sales counter, deliveries, wire crate and placed equipment. Font alpha/depth rendering and actual glyph fitting replace the old floating text sizing. World font materials are private, shared within the yard and updated on atlas rebuild; stopping/reloading the world cleans them up. The HUD puts objectives, held items and receipts on contrasting panels and shows the current catalogue binding.

**Eleven substantial salvage groups inside the perimeter** add discarded fridges, tyre/rim heaps, metal bins, pipe racks, drums, pallets and copper coils. Eight secondary exterior heaps, two service pockets and eight flat offcut patches complete 29 clusters across eight batching sectors. Thirteen coarse static collision volumes make inside stock solid to the player; dressing hides with its collision near saved equipment, belts, loose items and the restored player. Valid construction can replace stock because its IgnoreRaycast-layer volumes are excluded from build/interaction rays. No Rigidbody, inventory, extra lights or per-fragment updates. Most of the interior stays freely buildable. Native controller collision and batching still need local checks.

Nearby brick workshops, sawtooth roofs, containers, poles/cables and grouped angular foliage replace the isolated trees/flat skyline. Five original new FBXs reuse the working atlas: 23 pre-batch renderers/39,428 placed triangles in six exterior regions. Two original 256px ground maps and owned material copies add packed gravel and feathered wheel lanes connecting entrance, office and receiving; irregular dust/moss stays in three small ground meshes. Existing maps/materials/scenes remain unchanged. See [atmosphere implementation and local checklist](Design/COMPACT_ATMOSPHERE_VERIFICATION.md). The [eye-height asset preview](Design/Previews/CompactBackdropEyeHeight.png) is Blender rendering of actual exports, **not Unity gameplay**.

Optional free/paid vendor packs within the creator's USD50 budget are listed in [the asset shortlist](Design/ASSET_SHORTLIST.md). Current scenery uses the project's original assets; no vendor package is required or imported.

The compact default is editable `Resources/ScrapshiftLighting/CompactAfternoon`: daylight now faces the office, ambient side/ground fill is brighter, and three downward task lamps use intensity 0.9 and narrower cones instead of 3.4. The unmodified old default assigned by existing compact scenes resolves to this new profile; selected custom/tuned profiles remain selected. Legacy keeps `CozyAfternoon`. Both retain the gradient sky, matching horizon fog, tiny deterministic reflection map and four-light budget. Stock has soft contact grounding. Laptop/Balanced/Detailed retain existing render/shadow budgets; Laptop still has no post processing. No realtime probes or fullscreen ambient occlusion are added. Existing pipeline/package/editor pins and preference files stay intact.

Assign a custom **Yard lighting profile** on the existing bootstrap to tune colors, fill, sun and fog; a missing assignment uses the tracked default. Sky/reflection/emissive materials are privately owned, and stopping Play restores the previous sky, ambient, fog, reflection and camera background. Tracked materials/scene data and game/preferences saves are preserved. Actual Unity rendering and performance remain unverified in Cloud.

The next finishing milestones are in [GAME_FINISHING_PLAN.md](GAME_FINISHING_PLAN.md). Lighting, varied appliance repairs, neighbourhood requests, daily work receipts and the title/welcome/help/journal flow are implemented source milestones. Actual Unity validation, further art/pacing refinement and a verified desktop player build remain outstanding.

## Retained legacy concept-world pass

The legacy `Generated/Scrapyard` scene includes original tool storage and repair kits, gutter/bracing details, hanging fluorescent fixtures, warmer office windows/interior silhouettes, recognizable washer/fridge/microwave salvage rows and teal shelters/containers. Open wire fences, textured poplars and distant brick factories give the existing 96 × 80m yard context. Layered gravel, tyre wear, rough shallow puddles and sparse weeds replace straight decorative lane strips. Twelve added FBX models share a separate worn atlas and metal/smoothness mask; original models and edited materials stay intact. Scenery adds no inventory or lights. Eight new foreground groups use coarse static collision while preserving old routes/footprints; pieces covering a loaded player or loose item are hidden for that session. The old boundary/container colliders and every station anchor remain.

See [world implementation and verification](WORLD_PASS_VERIFICATION.md) for the per-area changes, budgets and local checklist. [Overview](Assets/Scrapshift/Art/Previews/WorldOverview.png), [workshop detail](Assets/Scrapshift/Art/Previews/WorkshopWorldDetails.png) and [appliance lane](Assets/Scrapshift/Art/Previews/ApplianceLane.png) and [office](Assets/Scrapshift/Art/Previews/OfficeWorldDetails.png) are labelled **Blender previews, not Unity gameplay**. They use exported game meshes and the runtime layout; actual Unity rendering and creator visual approval remain outstanding. The uploaded AI concept references and supplied provenance are preserved under `Campaign/Scrapshift/references/`.

## Legacy scene: what to do in the yard

The 96 × 80m yard has a central workshop, western vehicle/appliance salvage, eastern metal sorting, northern loading/storage and southern office/entry district. Pause → **Yard map** shows your position and fourteen numbered destinations. Select a pin or station to track it and resume; **Follow objective** returns to automatic guidance. The HUD shows destination, distance and Ahead/Left/Right/Behind/Nearby relative to your view. Automatic guidance follows carried material, ready/partial work and storage, selecting the nearest renewable wire crate when supply is needed. Bearings guide direction; they do not find a path through obstacles. A selected destination stays tracked until changed or New game and is not saved between sessions. Office, cars, containers and crane are scenery; the fixed workshop shelf radio is decorative, while radios taken from electronics salvage are repairable. No driving, crane operation or enterable office is implemented.

Complete the six goals in **How to play → Yard journal** to establish your yard: recover copper, earn income, help a customer, buy the powered stripper, test a restored appliance and purchase a yard improvement. Chapter one then offers free play, without extra rewards or consuming items. The journal can reopen the recap. Completion acknowledgement is saved separately from settings through an additive yard-save field.

### Wire processing

The manual bench shows an actual cable coil, emerging copper and a short pliers stroke on successful work. Looking at a loaded wire/restoration bench shows current steps and a progress bar; repaired appliances explicitly require a power-on test before the HUD marks them tested. Progress follows saved jobs and hand-tool upgrades. These animations stop with menus and use fixed decorative meshes without active physics.

1. Take renewable wire from **WIRE DELIVERY** or the three remote wire crates.
2. Place it on **STRIPPING BENCH**, then use four separate manual-work strokes with empty hands. Collect three copper.
3. Sell copper at **SCRAP BUYER** for €12 per default bundle. Three loads earn €36.
4. Buy **POWERED STRIPPER** for €36, feed one wire bundle and wait five seconds. Animated rollers and lamp indicate processing; collect copper from its tray with empty hands.
5. Sell, store or deliver that copper. The manual bench remains usable while the machine runs.

Aim at the station body within 3.2m. Prompts show current action, progress, price or reason it is unavailable. The aiming dot changes colour for a usable target. Carry one item at a time; dropped items stay as fixed objects rather than simulated rigidbody piles.

### Storage and customer orders

Wire/copper bins east of the workshop store your carried bundle. Use a bin with empty hands to retrieve one. Storage keeps the original item ID/quantity and counts toward yard capacity; it does not duplicate inventory.

Use **CUSTOMER BOARD**, north of the workshop, to view two independent request queues. The same page is available through Pause → **Customer requests & storage**. Accept a request on its card, then carry matching material to the physical board and interact to deliver. Copper orders keep their original five quantities/rewards and saved sequence; partial bundles are accepted and surplus stays carried.

Neighbourhood restoration requests give tested appliances another use:

| Customer | Needs | Default payout |
| --- | --- | --- |
| Marta's Cafe | One tested fan | €52 |
| Rowan's Garage | One tested radio | €44 |
| Neighbourhood Hall | Two tested fans | €104 |
| Local Repair Club | Two tested radios | €88 |

These requests repeat separately from copper orders. Their payout is ordinary resale plus the editable €10 bonus per appliance, locked when accepted. Two-item requests keep separate deliveries across days and saves; payment happens only when complete. Broken/wrong appliances stay with you, and a balance-limit failure retains the final item and earlier deliveries. No deadlines or daily fees. The buyer still offers ordinary resale; the HUD/map sends a matching tested item to the customer board. Restoration inspection shows a matching accepted request's agreed payout.

### Fan and radio restoration or salvage

**APPLIANCE SALVAGE** in the southwest supplies two broken desk fans per day. **ELECTRONICS SALVAGE** in the southeast supplies one portable radio. Carry either to the shared **RESTORATION BENCH** northwest of the hub, load it and inspect its fault. One job occupies the bench at a time.

| Default fault | Parts | Repair steps | Tested resale |
| --- | --- | --- | --- |
| Fan: seized motor | €8 | 3 | €42 |
| Fan: loose power lead | €4 | 2 | €42 |
| Fan: dust-clogged bearings | €0 | 2 | €42 |
| Radio: failed capacitor | €6 | 3 | €34 |
| Radio: loose power lead | €3 | 2 | €34 |
| Radio: oxidized tuner contacts | €0 | 2 | €34 |

Inspection compares parts cost, steps, resale after parts and salvage value. After repairing, explicitly power on/test before collection and sale. Alternatively dismantle a fan in four steps for three copper, or a radio in three steps for two copper. Better tools shorten each job by one step, minimum one. Balance tuning changes the base recipes; lead repairs cost half base parts, minimum €1, and cleaning uses no parts.

Faults cycle across deliveries and stay with the item/job through dropping, later days and saves. Parts charge once, choices remain exclusive and partial jobs/output are protected. The tested bench fan spins; the tested radio plays an original short instrumental listening phrase through the same bounded spatial source. Storage bins accept wire/copper only. The portable radio uses an original 1,308-triangle model and the existing atlas.

### Welcome, help and yard journal

The first welcome shows actual bindings and current Balance values. Established old yards skip it. **How to play / Yard journal** is accessible from the title and pause menus; scrollable pages explain controls, wire processing, restoration, storage/orders and the relaxed day. Its six saved goals cover copper recovery, first income, customer completion, powered equipment, appliance restoration and investment. Goals have no bonus payment or deadline. Legacy saves recognize established facts without inventing unknown past actions.

Pause offers Return to title and Save & Quit. Returning keeps the current yard; Continue uses it without reloading or resetting. Save & Quit stays open on write failure. Quit stops Play in the editor and exits a desktop player. Escape backs out of nested pages and cannot unpause the title screen; all resume paths use the input-release gate. Settings and preferences remain separate from yard progress.

### Diary and investments

Use **YARD DIARY** near the entry. Day review shows earned income, parts/equipment spending, cash change, stripped loads, copper and appliance sales/deliveries, completed repairs/salvage/requests and appliance stock. **Finish day / return tomorrow** refreshes fans/radios and finishes the stripper's current load overnight, preserving inventory, player position, unfinished hand work and orders. A paused work receipt appears after closing the day; continue or Escape opens the next day through the existing input-release gate. The stripper's overnight completion belongs to the finished day. **Review last finished day** reopens the saved receipt without changing finances or advancing time.

Old saves keep their income/cash but label earlier unrecorded work and costs as unavailable. Their next day has a complete ledger with the actual opening balance. Display counters saturate safely and report a limit instead of overflowing; money still uses the original guarded transactions. No automatic clock or penalty forces you to end a day. Wire remains renewable.

| Investment | Price | Effect |
| --- | --- | --- |
| Storage rack | €60 | Twelve extra bundle slots, bounded at 100; a rack appears by the bins |
| Better hand tools | €75 | One fewer stroke per wire/repair/salvage job, minimum one; yields stay intact |
| Stripper tune-up | €120 | Requires the machine; future loads run 40% faster |

Purchases are guarded against double charges. Already-worked hand jobs complete if the new tool threshold is reached; repaired appliances still require testing. A running machine retains its saved timer when tuning is purchased.

## Persistence

Under `Application.persistentDataPath` (company `XanderMaxStudiosBE`, product `Scrapshift`):

- `yard-v1.json`: cash/equipment/upgrades, carried/dropped/stored items, station/appliance faults and progress, copper/restoration requests and agreed payout, current daily ledger/last-day receipt, welcome/journal goals and player position/look.
- `yard-v2.json` (schema 3): compact cash/XP, equipment/positions/power, buffers/filters/recipes, partial dismantling/work, carried/dropped goods and conveyor endpoints/moving units/progress. Valid schema-two compact files migrate without altering source bytes on read.
- `controls-v1.json`: action bindings, sensitivity and invert-Y.
- `presentation-v1.json`: graphics/FOV/frame limit/FPS display and audio volumes.

Writes use flushed temporary files and atomic replacement with `.bak` recovery. Old gameplay enum values/version-one saves are retained with additive fields. Corrupt gameplay and backup block saving instead of silently overwriting progress. New game requires a second click and archives old saves. Preferences survive new games; preference failures keep valid defaults or backup values and display a notice. Offline elapsed time does not simulate work; advancing the diary is the explicit overnight operation.

A primary file with missing required fields now correctly tries the backup. If both files are unreadable, saving stays blocked and neither file is overwritten.

Saving occurs after transactions, periodically, on pause/focus loss and normal exit. Pausing freezes gameplay and processing. Saved player positions retain safe expanded-yard coordinates/height rather than reverting to the old small enclosure.

## Verification

Check original asset packaging without Unity/Blender:

```sh
python3 Tests/audit-original-assets.py
python3 Tests/audit-world-assets.py
python3 Tests/audit-compact-assets.py
python3 Tests/audit-automation-assets.py
python3 Tests/audit-material-bindings.py
python3 Tests/audit-texture-metadata.py
```

This checks FBX headers/budgets and hashes against the last Blender audit, atlas size, WAV samples/durations, metadata GUIDs and assembly JSON. It does not verify Unity import or rendering.

Run pure C# checks with Mono:

```sh
./Tests/run-core-tests.sh
```

This Cloud instance has Mono extracted outside the repository:

```sh
SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-core-tests.sh
```

The same command runs three legacy and nine compact filesystem/recovery checks against the real save implementations with narrow serializer adapters. They include corruption protection, atomic backups, compact migration retaining original bytes and moving-item snapshot recovery. Ten runtime-material and nine editor-recovery/report groups use controlled API adapters, including null catalogue/source recovery, custom-texture retention and bounded failed imports. Four dressing-occupancy groups protect fixed/rotated objects, dropped parts and whole conveyor routes without changing the model. These checks do **not** validate Unity JSON or rendering.

Core coverage includes inventory/money conservation, exactly-once processing, capacity/output reservations, 100 renewable wire cycles, invalid/overflow/ID guards, rebinding/conflicts/defaults, dynamic guidance, expanded-world positions, storage/independent partial copper/restoration requests, locked quote and final-delivery overflow guards, daily cost/work reconciliation and legacy reports, repair/salvage/day transactions, upgrade prerequisites/progress, legacy balance migration and presentation validation. Mixed tests reconstruct copied core data; actual JSON round-trips are supplied Unity tests.

In Unity use **Window → General → Test Runner → EditMode → Run All**. The supplied suite includes actual gameplay/preferences JSON, recovery, model/audio imports, geometry/colliders, automation, dynamic parts and graphics cleanup. All engine cases remain unrun in Cloud. Follow the [Stage C local checklist](Design/COMPACT_STAGE_C_VERIFICATION.md). A source/syntax/Blender audit does not replace Unity compilation or a player build.

Optional batch commands, with `UNITY_EDITOR` set to your installed editor:

```sh
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" \
  -executeMethod Scrapshift.PrototypeSetup.Generate -logFile /tmp/scrapshift-setup.log
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform EditMode -testResults /tmp/scrapshift-results.xml \
  -logFile /tmp/scrapshift-tests.log
```

Inspect exit status, Console/editor logs and populated test-result XML. An empty/skipped result is not a pass.

### Required local checks

- Start/Continue/New yard, the welcome, title/pause/help/journal/credits/settings navigation and Save & Quit need actual Unity verification. Test Escape at each level, current bindings, scrollable help, fresh/legacy/backup/corrupt saves and small windows.
- Import with the pinned editor; check zero compile errors, normal URP shaders, metre scale/axes, atlas appearance, roof joins, shadow readability, signs and HUD at different Game-view sizes.
- Check gradient sky/sun direction, fog joins, workshop/fan readability, task-light cutoff/fixtures, static contact shade and copper reflections. Switch all presets and grade; watch cascade transitions/shadow shimmer while walking. Stop Play and confirm sky/fog/ambient/reflections restore. Run the supplied lighting/shader tests in Unity.
- Complete wire processing, orders, repair and salvage for fans/radios, all three faults and all investments. Test wrong inputs/full hands, duplicate purchase/collection, partial/surplus delivery, capacity and the explicit appliance tests.
- Accept both request types, deliver matching copper/tested fans/radios, and reopen partial two-item requests after a day/save. Check wrong/broken items, ordinary resale, quote changes in Balance, final-payout overflow and immediate dynamic hints.
- Finish/review/reopen day receipts; verify costs, completed work, partial deliveries and overnight machine credit; test legacy unrecorded detail, Escape, held-input resume and small windows. Reviewing must never pay or advance another day.
- Save/reopen with carried/dropped/stored material, running/output-ready machine, paid partial fan/radio repair and saved fault, active partial order and upgrades. Confirm old saves and tuned Balance values. Verify archived New game and backups through tests.
- Check cable/copper tabletop alignment, pliers movement, independent fan feedback, partial-job progress and explicit test status. Pause mid-stroke, resume and verify animation/input isolation; inspect small Game-view HUD fit.
- Select map pins/sidebar stations, resume with held input and verify the release gate; turn through all directions, arrive nearby and choose Follow objective. Check nearest wire sources, active fan jobs while the stripper runs, storage/customer routes and no save changes from tracking.
- Check collision and station approach rays in every district; recover dropped old-save items. Inspect authored roller/output/lamp alignment and restored fan rotation.
- Pause during processing. Open every menu and Settings tab, capture/release keys/buttons, test keyboard/mouse/arrow conflicts and cancellation through Escape/Cancel/Back. Hold controls while resuming; no movement, work, pickup or drop should leak. Check actual-binding HUD/objectives immediately and after restart/default restoration.
- Switch presets, FOV and grade; reopen Unity Play and confirm the original Editor pipeline/VSync/frame cap is restored. Check sliders, machine-distance falloff, footstep timing, paused loops and continuing ambience. Listen and adjust mixing locally.
- For lag, enable **Video → Show gameplay FPS**, maximize Game view, close Scene view during play, and compare Laptop with Balanced from the same spot. Capture CPU/GPU Profiler frame times. Compare a standalone desktop player; Editor FPS includes Editor overhead. Gameplay HUD text/progress now caches on transactions, target/menu changes or ten Hz while playing; paused HUD skips periodic refresh. The Profiler marker `Scrapshift.RefreshHud` identifies this work. No FPS gain has been measured in Cloud.
- Build a desktop player and repeat complete gameplay, input, audio, shader/text and persistence checks. These remain prototype features until that succeeds.

## Source and limitations

`Core/` owns authoritative transactions/preferences/guidance. `Runtime/` owns input, pause/menus, saves, presentation/audio and world/item views. `Editor/` contains non-destructive setup and scoped import policies. `Tests/` shares core scenarios with Unity wrappers. `ArtSource/` authors original Blender meshes/atlas and deterministic synthesized WAVs; exported assets are tracked, so users do not need Blender/Python to play.

This is a developing prototype. There is no general recipe framework, vehicle/conveyor automation, power network, NPC economy or production build verification. The user has played earlier wire/settings builds locally; the new visual/business milestones need fresh Unity review. Campaign assets remain concept artwork and documentation, not game footage. No campaign publication or production commitments are part of this work.
