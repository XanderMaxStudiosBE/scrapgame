# SCRAPSHIFT — scrapyard prototype

Start with your bare hands. Build a scrapyard that works for you.

A Unity/C# prototype of a first-person cozy retro scrapyard, now laid out as a 96 × 80 metre world. The user ran the previous version in Unity 6000.3.25f1 and reached the purchased-machine stage. This is limited playtest evidence, not comprehensive verification. This update adds Settings/rebinding, original textures, recognizable stations, contextual prompts and state-based guidance. **This update's Unity compilation, menus, rendering and playability still need local verification.** The engine-independent code passes 26 executable scenarios in Cloud. Campaign illustrations remain concept art, not gameplay captures.

## Open locally

1. Install **Unity 6000.3.25f1** in Unity Hub, with your desktop build support. Add this repository's root as the project (the folder containing `Assets`, `Packages`, and `ProjectSettings`).
2. Open it and allow package import. `Packages/manifest.json` targets **URP 17.3.0** and **Test Framework 1.6.0**; dependency resolution still requires verification in this editor. Do not silently upgrade the editor to resolve an import problem.
3. Choose **Scrapshift → Create or Open Prototype**. The command creates URP renderer/pipeline assets, a balance asset, a copied campaign logo, and `Assets/Scrapshift/Generated/Scrapyard.unity`. It assigns scene references and adds the scene to Build Settings. If Unity requests a restart for legacy input, accept it and reopen the scene.
4. Press Play and click the Game view to capture the mouse. There is no essential Inspector wiring to do manually.
5. To create a desktop executable, open **File → Build Profiles**, select your installed desktop target, check that `Scrapyard` is included, and **Build And Run** into `Builds/`.

The generated scene creates original low-poly geometry at runtime: gravel yard, textured fence, corrugated workshop, wire-filled crate, tool-equipped bench, copper buyer/scale, and powered machine with feed opening, motor, rollers and output tray. No paid assets or external runtime services are required. Package installation uses Unity's package service; editor installation/licensing is managed by Unity Hub.

The setup command reuses existing generated balance, pipeline, and scene assets. It does not overwrite scene edits on a second run. Generated assets are ignored until editor verification; to share approved scene/balance edits, deliberately move them into a tracked asset folder or force-add the generated assets and their `.meta` files. All supplied source assets already have stable `.meta` files. Preserve GUIDs.

## Upgrade an existing project

Pull this update and allow Unity to import the tracked textures/materials and scripts. Open your existing bootstrap scene and press Play: `YardBootstrap` builds the improved props and wires the Settings menu automatically. You do **not** need to delete `Generated/`, rebuild the scene, or reset your balance asset/save.

**Scrapshift → Update Existing Prototype Visuals** validates texture imports/material availability without replacing the scene or balance asset. Existing material edits and GUIDs are preserved. **Create or Open Prototype** also reuses existing scene/data assets. A manually authored scene which does not use `YardBootstrap` requires deliberate integration of these builders; it will not be overwritten. Gameplay save schema and station anchors remain unchanged; no save migration is performed.

## Settings and controls

Open **Escape → Settings** for the Controls page. Each action displays its current primary binding; default movement also shows the existing arrow-key aliases. Select an action, release the button/key used to select it, then press a supported keyboard key or mouse button. Actions: forward, backward, left, right, interact, drop and manual work. Mouse buttons 1–7 are supported, including left mouse as the default manual action.

**Escape** is permanently reserved: it cancels a pending capture/conflict, backs out to pause, then resumes. Cancellation changes no binding. Mouse bindings commit after release, allowing the visible Cancel/Back buttons to work during capture. Conflicts show the affected action and offer **Swap bindings** or **Cancel**; swapping keeps every action bound. A customized action owns its primary key, including arrow keys, so restored movement defaults never silently steal that key. **Restore Defaults** resets bindings, sensitivity (2) and invert-Y (off).

Sensitivity and invert-Y apply immediately. Bindings and look preferences persist separately in `controls-v1.json` under `Application.persistentDataPath`, with a backup. Missing preferences use the original controls; invalid data falls back to a valid backup or working defaults with a notice. Save failures keep changes active and display the error. Starting a new yard or loading gameplay progress does not reset controls.

Gameplay and mouse look are blocked throughout pause/Settings/capture. Activation clicks are excluded from capture; captured clicks are consumed through release. Resuming requires menu keys/buttons to be released and a fresh gameplay input, preventing a resume/capture click from stripping, interacting or dropping. HUD prompts, objectives, carried-item hints and the controls legend all resolve current bindings. Unity's existing **legacy Input** backend is retained; movement now polls centralized action bindings rather than the fixed Horizontal/Vertical axes. Mouse-look axes and reserved Escape remain intact. No input package migration or new runtime dependency is required.

## Default controls and first playthrough

| Input | Action |
| --- | --- |
| WASD / arrow keys | Walk with collision |
| Mouse | Look |
| E | Use the station or pick up the bundle under the crosshair |
| Left mouse click | One manual wire-stripping stroke while looking at the loaded bench |
| Q | Drop the carried bundle in front of you |
| Escape | Pause/resume and release/capture the cursor |

The following keys describe **defaults**; in game, follow the current HUD labels after rebinding.

1. Walk to **1 / DELIVERY**, look at the crate within reach, and press E for a free wire bundle. Inspect its type in the HUD. Q drops it; aim at a dropped bundle and E picks it up again.
2. At **2 / WORKBENCH**, press E to place the wire. Click four separate times while aiming at the bench. The material changes size/color as insulation is removed; copper is visible when complete. Press E with empty hands to collect three copper units.
3. At **3 / SELL COPPER**, E sells the carried bundle for **€12**. Repeat three manual loads to earn **€36**.
4. At **4 / POWERED STRIPPER**, E buys the machine. Get another wire bundle and E feeds it. The roller turns and the status lamp changes while processing for five seconds.
5. With empty hands, E collects three copper units from the finished machine. Sell them for another €12. The bench remains usable while the machine runs.

Look at the station body within 3.2 metres for its current action, price, progress, output count or specific reason it is unavailable. The crosshair turns green for a usable target. Objectives derive from the current yard state and handle carried/dropped bundles and machine-owned saves. Sales show money gained; purchase/feed/manual work provide text, movement and restrained sound feedback. You can carry one bundle. The free delivery source is renewable; loose and station-held bundles share a cap of 24 so finished output has room to be collected. All materials are represented by bounded data records; dropped bundles are fixed collision objects rather than persistent rigidbody simulations.

Tune copper yield/unit value, upgrade price, manual stroke count, processing time, and bundle cap in `Generated/Balance.asset`. The initial milestone uses one wire-to-copper recipe, not a general factory framework.

## Persistence

The save is `yard-v1.json` under `Application.persistentDataPath` (company `XanderMaxStudiosBE`, product `Scrapshift`). It records money, ownership, carried/dropped bundles and positions, bench progress/output, machine remaining time/output, and player position/look. Saving occurs after interactions, periodically, on pause/focus loss, and on normal exit.

Writes use a flushed temporary file and same-volume atomic replacement with a `.bak` fallback. A corrupt primary attempts the backup; if both are unreadable, saving is blocked and a warning is shown. **New game** requires a second click and archives existing saves before resetting. File-system permission failures are reported. Offline time does not advance machines. Pausing freezes processing; restored player height is normalized to the ground inside the fence.

## Verification

Executed in Cloud: the real pure C# core and shared scenario sources compiled with Mono 6.12, warnings treated as errors; **26 scenarios passed, zero failed** (12 gameplay, 7 controls, 4 contextual guidance, 3 world/inventory checks). Coverage includes the full manual-to-machine economy loop, carry/drop, invalid inputs, one-time consumption/output/sales, purchase guards, reserved output capacity, partial station resumption, zero/invalid time steps, invalid save-state rejection, overflow protection, and 100 renewable scrap cycles. Control checks cover defaults/arrow aliases, keyboard and mouse rebinding, cancellation, conflict swap/cancel, reserved/unsupported inputs, default restoration, validation and labels. Guidance checks cover each station state, custom control labels, dropped-item/owned-machine objectives and read-only presentation. The partial-resume core test reuses data; it is **not** a serialization test.

Repeat on a machine with Mono:

```sh
./Tests/run-core-tests.sh
```

This Cloud instance has an extracted Mono installation outside the repository:

```sh
SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-core-tests.sh
```

In Unity, open **Window → General → Test Runner → EditMode → Run All**. The shared 26 scenarios also run there, together with Unity-specific tests for gameplay/control serialization and isolation, backup recovery, supported legacy keys, menu labels/back handling, and generated geometry. **These Unity tests have not been run here.** Review the current runner/test counts rather than assuming an empty or skipped suite is valid.

Optional batch commands from the repository root, with `UNITY_EDITOR` pointing at the installed editor executable:

```sh
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" \
  -executeMethod Scrapshift.PrototypeSetup.Generate -logFile /tmp/scrapshift-setup.log
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform EditMode -testResults /tmp/scrapshift-results.xml \
  -logFile /tmp/scrapshift-tests.log
```

Inspect exit statuses, editor logs and the current result XML; a launch or empty test result is not a pass.

### Required local playtest

- Import with the pinned editor/packages; confirm zero Console compile errors and normal URP materials, readable signs/HUD, and audible work feedback.
- Walk into fences and benches; verify collision. Pick up, drop near walls and under the workshop roof, then retrieve wire/copper. Confirm mouse look and reach prompts.
- Complete the five-step walkthrough above; check money after every sale/purchase and check visible bench/roller feedback.
- Try empty hands, copper in each input, insufficient funds, a second purchase, repeated collection, and feeding a busy/full machine. No material should disappear or duplicate.
- Fill most delivery capacity while a station holds material. Finish processing and collect its output without a capacity deadlock.
- Pause during processing; wait; resume. Focus loss must pause and release the cursor.
- Quit/reopen with a carried bundle, a dropped bundle, a partly worked bench, and a running machine. Verify all quantities, ownership, money, and remaining work. Repeat with ready outputs.
- Test the save warning/backup behavior in EditMode tests. Test the two-click New game flow and verify the archived files before removing anything.
- Open an existing generated scene with tuned balance and prior saves. Confirm upgraded props/materials, original asset GUIDs, unchanged balances and recoverable dropped items. Use the non-destructive Update Existing Prototype Visuals command twice.
- Identify the input opening and output tray without extra instructions; aim at the machine body and buyer scale from normal approach distances. Check collision, signs, texture repetition and distant shimmer.
- Open Settings while the machine processes; wait and verify no progress, movement, work, pickup or drop occurs. Change every action to a new key; rebind manual work to a different mouse button. Verify HUD/objectives immediately match and old primary bindings stop acting.
- Cancel armed capture with Escape, the visible Cancel button and bottom Back button; confirm each leaves the binding unchanged. Also cancel during a held mouse button and test mouse side buttons. Cancel conflicts through both choices; confirm no change on cancellation. Create a keyboard conflict, a mouse conflict and an arrow-alias conflict; test swap and cancel. Escape must always remain usable.
- Capture left mouse while the pointer is over a different UI button, then release/resume. No UI choice or gameplay action should leak from that click. Hold a movement/interact/drop key while resuming; no gameplay action should occur until release and fresh input.
- Change sensitivity and invert-Y, restart the player, and verify them and custom bindings. Start New game and reload yard progress; preferences should remain. Restore Defaults and verify original WASD/arrows, E, Q, left mouse, sensitivity 2 and normal Y.
- Build and run a desktop player; repeat a complete loop and Settings check. Verify shader/text availability, preferences persistence and game save directory.

## Code map

- `Core/ControlPreferences.cs`: bindings, aliases, conflict transactions and defaults.
- `Core/YardGuidance.cs`: read-only contextual actions and state-based objectives.
- `Runtime/PlayerInputSettings.cs`, `SettingsMenu.cs`: legacy input adapter, preferences storage, capture/release gate and Controls UI.
- `Runtime/YardProps.cs`, `WireStripperVisual.cs`, `RetroMaterialLibrary.cs`: low-poly props and tracked textured URP materials.
- `Core/YardState.cs`, `Core/YardModel.cs`: inventory, economy, station transactions, time, validation; no engine dependency.
- `Runtime/PrototypeBalance.cs`: editable recipe/economy asset.
- `Runtime/FirstPersonController.cs`, `InteractionTarget.cs`: movement/look and interaction targets.
- `Runtime/PrototypeGame.cs`: interaction dispatch, bounded item views, machine feedback, HUD/pause, lifecycle saves.
- `Runtime/SaveStore.cs`: Unity JSON persistence, atomic writes, backup recovery.
- `Runtime/YardBootstrap.cs`, `YardGeometry.cs`: original procedural yard and runtime asset presentation.
- `Editor/PrototypeSetup.cs`: repeatable project/scene setup with serialized references, including the surface shader for player builds.
- `Tests/Editor`: shared core cases and Unity persistence tests.

Paths above are under `Assets/Scrapshift/`. No Unity Editor is installed in Cloud; downloads from the .NET installer and Unity package host were denied by the current network policy. Mono was obtained from Debian's signed package repository with normal signature/hash verification. No engine API stubs were used as evidence of Unity compilation.

## Next milestones

1. Locally verify this update in Unity 6000.3.25f1, including menus, existing scenes/saves and desktop build; record results and commit verified generated assets/package lockfile when appropriate.
2. Tune movement, station spacing, feedback, and pacing from a full manual-to-automatic playthrough.
3. Add one repairable appliance (for example a fan/motor) before expanding to conveyors or more processing recipes.

## Performance and the next cozy-retro milestone

The user reports steady low frame rate while walking/looking **inside the Unity Editor**. This performance pass removes per-frame inventory-view rebuilding, collider-array discovery and transform reparenting; views refresh on gameplay transactions, loading and machine completion. Bench/machine animation remains frame-driven. Input KeyCode parsing/binding labels, HUD styles and the two changing material instances are cached. Stationary ground/fence/workshop/clutter are runtime static-batched separately from interactive stations and moving parts. Savings need to be measured locally; no FPS improvement has been verified in Cloud.

For a useful comparison:

1. Maximize the Game view on Play; turn off Game-view Gizmos. Compare with the ordinary Editor layout, which can add Scene-view and Inspector overhead.
2. Open **Window → Analysis → Profiler**, record around 10 seconds walking the same route with the same loose bundles, and inspect CPU Timeline/GC Alloc and Rendering batches. Named `Scrapshift.SyncViews` and `Scrapshift.Save` markers identify view rebuilding and durable save writes without enabling Deep Profile. Leave Deep Profile off for the baseline because it adds overhead.
3. Compare a desktop player (Development Build with Autoconnect Profiler when profiling) at the same resolution. Use frame time: 16.7 ms is 60 FPS; 33.3 ms is 30 FPS. Check CPU versus GPU before choosing shadow/resolution changes.
4. Verify pickup/drop/carry/sell, each manual stroke, machine completion/output/lamp, pause and Settings. These are the state changes which now trigger view refresh. Check rebind/restore immediately updates labels. Confirm static-batched surroundings while rollers, lamp, carried items and bench output remain independent.

Save writes still flush synchronously for durability. If freezes happen specifically on interaction/autosave, inspect the `Save` call separately; this pass targets continuous frame overhead. If the GPU dominates, follow up with measured shadow/render-quality presets; lighting quality has not been lowered speculatively.

Recommended next scope after performance verification: **repair one broken fan** (inspect → identify failed motor → replace → switch on → sell). Then add varied daily scrap deliveries and small customer orders, then make the expanded yard more useful with storage, deliveries and unlockable work areas. Add conveyors after these choices and the existing loop feel satisfying.

Cozy retro direction: muted earthy colors with warm ivory accents, soft afternoon light/fog, chunky silhouettes, subtle coarse textures and readable UI. Add gentle yard ambience, a quiet optional workshop radio, satisfying clicks/whirs, useful personal workshop clutter and forgiving progression. Use a consistent restrained visual language; aggressive vertex wobble, strong dithering and heavy effects are optional experiments and must not hurt clarity or comfort.

## Original textures and contributions

Seven tracked 64×64 tileable PNGs and URP materials cover rusted paint, dark industrial metal, corrugated metal, weathered wood, gravel, copper and wire insulation. They were created procedurally with deterministic authoring code, not downloaded or derived from campaign artwork. Imports use Point/Repeat. Custom box and twelve-sided cylinder UVs target one repeat per metre (64 texels/m) to avoid stretched primitive textures. See `Assets/Scrapshift/Art/Textures/README.md` and `generate_textures.py` for palette, import settings and regeneration details.

Agent contributions: Settings agent owned preferences/input/capture UI and its tests; texture agent authored the seven PNGs/materials/library/validator; machine agent built the recognizable stripper; environment agent built yard props and geometry tests. Main agent integrated gameplay/input, live prompts/objectives, feedback, metre-scaled boxes, the scene update path, tests and documentation.

See `PROJECT_HANDOFF.md` for continuation status. This prototype adds no Kickstarter funding target, reward, date, or release commitment.


## Expanded scrapyard world

Open the existing prototype scene and press Play after pulling: its bootstrap builds the new world automatically. Keep Generated, balance assets, materials and saves. The old 24 × 20 metre enclosure is replaced by a **96 × 80 metre fenced yard** (16 times the ground area), while all four original functional stations and the player spawn retain their coordinates.

- Center: familiar workshop, delivery crate, buyer and powered stripper.
- West: nine low-poly salvage vehicles in walkable rows, with two renewable wire crates.
- East: nine metal sorting bins, a sheltered sorting bay and a third renewable wire crate.
- North: six storage containers, loading platform and stationary gantry landmark.
- South: entry gate, yard office exterior and stock pallets.
- Pale lane edges, two-sided district signs, warm lighting, a lighter fog horizon and trees establish the cozy retro setting.

**Escape → Yard map** shows districts, wire sources and your current position. Escape returns to the pause menu; Escape again resumes. Movement, tools and machine progress stay paused on the map. Area names also appear in the HUD. Wire sources share the existing inventory/capacity guards and immediately use rebound interaction prompts. Restore/drop bounds now cover the expanded yard; version-1 save files and separate control preferences are retained, including player height on the loading platform.

Scenery is static, uses shared materials and is batched by district. Small decorative parts and distant tree canopies do not cast shadows. There are no vehicle physics, active scrap piles, per-frame scenery searches or additional realtime lights. Vehicles, office, crane and containers are scenery; they are not driveable, enterable or usable machines/storage yet. Actual FPS is still a local measurement, especially because the world is larger.

Local verification required:

1. Run all EditMode tests (39 supplied cases), including the new district/collision/source reachability and expanded save/restore checks. Cloud ran only the 26 engine-independent scenarios; Unity tests remain unrun.
2. Walk from the original spawn through all districts, try each wire crate, process/sell its wire, and inspect signs, map, fence boundaries and vehicle/bin collisions. Verify no old fence blocks exploration.
3. Drop wire or copper in west/east/north areas, save/restart and retrieve it. Save while standing on the loading platform and verify safe height restoration. Load a previous hub save and complete the original manual-to-machine loop.
4. Pause during stripping-machine processing, open the map, use Back/Escape, enter Settings and resume with held keys. Confirm no movement, action or processing leaks into menus.
5. Compare Profiler CPU/GPU/frame time at the workshop and facing each populated district, and test a desktop build. Verify regional batching and shader/text rendering. Do not infer a performance improvement from source changes alone.


## Cozy retro visual pass

The latest pass replaces cube tree crowns with original sixteen-triangle faceted vegetation, adds a pitched office roof, sage fascia, framed warm windows, porch awning and potted shrubs, and adds painted roofs, blue-green windows, door handles, faded headlights and twelve-sided tyres to salvage cars. Containers have corner rails and painted storage plates. The workshop has a tool board, shelf, decorative radio/mug/plants and a visible lamp housing. Wheel ruts, dock safety markings and an entry nameboard add identity to the larger yard. Sky/ground ambient fill and warm sun keep shaded areas readable.

These are static visual details; the radio, office and vehicles gain no new interactions. Palette materials are shared, new vegetation owns/cleans up its generated meshes, signs stay outside static batches, and accents do not add active colliders, rigidbodies, realtime lights or frame updates. Existing station anchors, save data and materials remain intact.

26 core checks still pass in Cloud. The 39-case Unity suite now includes faceted mesh winding and decoration collider checks, but is unrun here. Pull and Play in the existing scene, then inspect roof joins, window/sign facing, tyre orientation, tree silhouettes, workshop/tool visibility, saved-item accessibility and shadow readability. Compare frame time against the previous yard version in Editor and desktop player; neither visual quality nor a performance gain has been verified in Unity here.


## Customer orders and working storage

Two labeled bins east of the workshop now store/retrieve wire and copper. Deposit uses your carried bundle; retrieving with empty hands returns the same original bundle ID. Stored bundles count toward the existing yard limit and disappear from loose-world item views. Store wrong material or retrieve with full hands and nothing is consumed.

The customer board north of the workshop offers five repeating, optional copper orders. Accept a request with your interaction binding, then bring copper back. Partial deliveries are saved and the reward arrives exactly once on completion. Only the amount needed is consumed; surplus remains carried. Orders have no deadline or penalty. Ordinary copper sales still work. Escape → Orders & storage shows the current request, reward, completed contracts and stored quantities; all world processing stays paused there.

The gameplay save remains `yard-v1.json`: additive storage/order fields default to no storage/no contract in older version-one saves. Input preferences remain separate. Core storage/order tests cover conservation, capacity, partial completion, surplus, completion payment, overflow, invalid states and rebound guidance. The 34 engine-independent scenarios pass in Cloud. Unity adds JSON round-trip, legacy-file and station/marker tests; these remain unrun here.

Local checks: load an older save; store/retrieve at both bins; save/restart with stored stock and a half-delivered order; complete and retry an order; deliver an oversized copper bundle and sell its surplus; verify the map and journal; pause a running stripper while reading the journal. Keep the existing full wire-loop and binding/menu checks.


## Authored industrial models and visual reference

The user supplied [Retro Rewind](https://store.steampowered.com/app/3552140/Retro_Rewind__Video_Store_Simulator/) as an aesthetic reference: believable recognizable props, worn surfaces, warm practical lighting and clear UI. SCRAPSHIFT stays an original scrapyard game. Ten original Blender-authored FBX assets now replace the matching primitive cars, containers, office, workshop canopy, sorting skips, workbench, delivery crate and scrap pallets. The fan mesh is prepared for the repair loop. A shared worn 512px atlas keeps their material count small; models contain 308–1,684 triangles each. No Blender installation is needed to play/import the tracked FBX files in Unity.

Authoring source and regeneration/audit commands: `ArtSource/README.md`. `Assets/Scrapshift/Art/Previews/WornRetroAssetSheet.png` is an inspected Blender **asset preview**, not Unity gameplay. The exported FBX files passed a Blender round-trip for actual dimensions, vertex/UV validity and triangle budgets. Their ten supplied Unity import/material/scale tests remain unrun; the complete supplied suite is now 60 cases. Pure core checks remain 34 passing.

Allow the new model/atlas/importer scripts to finish importing, then press Play in the existing scene. Watch Console for authored-prop fallback warnings: they mean that model/material did not load, and the older procedural appearance is being used. Locally verify metre scale/axes, atlas colors and URP shader, roof/windshield facing, model/collider alignment, station approaches, old-save access and per-district batches. Measure actual frame time; reduced per-prop object/material count is not proof of an FPS improvement.


## Restore or dismantle a desk fan

Find **APPLIANCE SALVAGE** southwest of the vehicle rows. Two broken fans arrive each day. Carry one to the **RESTORATION BENCH** northwest of the hub, place it, open inspection and identify the seized motor. Choose:

- **Repair:** pay €8 once for a replacement, fit it with three manual-work presses using your actual binding, then interact to power on/test. Collect the tested fan and sell it at the buyer for €42.
- **Salvage:** choose dismantling, use four manual-work presses, then collect three copper for ordinary sale or a customer order.

The choice stays saved, including paid parts and partial strokes. Wrong inputs/full hands/capacity/overflow never consume protected stock. Tested fan rotor animation advances only with gameplay time. Fans can be dropped/retrieved around the yard; wire/copper bins deliberately accept only their labeled materials.

The **YARD DIARY** near the entrance reviews earnings and progress. Finish a day there to replenish fan stock and finish the current stripper load overnight. Inventory, partial repairs and customer orders are kept. There are no deadlines, daily fees or automatic time pressure. Day and income appear in the save; the input/preferences format remains unchanged.

44 core scenarios pass in Cloud; 74 Unity cases are supplied but unrun, including partial paid-fan JSON restore, head-ray marker routing and zero-delta rotor behavior. Twelve authored FBX files passed Blender round-trip/scale/UV/budget audits. Local checks: play both fan branches, save/restart after paying and one stroke, verify the motor is not charged twice, test/collect/sell exactly once, collect both daily fans and return tomorrow, preserve stored bundles/partial contracts, pause while a tested fan spins, and press interaction/work together while opening a menu. Check actual bindings, fan-held visibility/drop height and source/head ray reachability in Unity.

### Yard investments

Use the yard diary near the entry, then Investments. Buy a storage rack (€60, twelve extra bundle slots), hand tools (€75, one fewer work stroke per job) or machine tuning (€120, requires the stripper and speeds future loads). Existing saves retain inventory and active machine timers. Verify each purchase, repeat-purchase prevention, dynamic hints and save/reload in Unity.

### Laptop graphics and sound

Pause → Settings → Video offers Laptop, Balanced and Detailed, frame cap, FOV and optional warm grade. Start with Laptop if Unity Editor play is slow; compare a standalone player, since Editor overhead differs. Audio controls master, tools/machines and outdoor ambience. These preferences persist in `presentation-v1.json`, separately from controls and gameplay.

Local checks: switch all presets without pink shaders, inspect warm grade on/off and shadow readability, verify 30/60/120 limits in a standalone player, reopen after exiting Play to confirm Editor pipeline/VSync restoration, and reload to confirm settings. Listen near/away from running machines, walk on gravel, use tools/sell, open menus to check machine/footstep pause and continuing ambience, then mute each slider. Repeat rebind cancellation/conflict tests after changing Settings tabs. None of these engine checks has run in Cloud.

Older local Balance assets are handled automatically: missing/zero new fan and investment fields get defaults, while existing wire rules and positive custom values are retained. No Generated-folder reset is needed.
