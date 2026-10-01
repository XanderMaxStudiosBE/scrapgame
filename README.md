# SCRAPSHIFT

A first-person scrapyard simulator prototype: collect wire and broken appliances, strip or restore them, sell useful material, fulfil customer orders and improve your yard. The visual direction is worn retro realism inspired by the warmth and readable everyday props of Retro Rewind, using original industrial assets.

**Current Cloud verification:** 69 pure C# scenarios pass, including 20,000 mixed transactions and a twelve-day progression from zero money. Eighteen authored FBX files pass Blender scale/UV/triangle audits. Seven original WAVs pass format/sample checks. **Unity is unavailable here:** the 128 supplied EditMode cases, engine compilation, rendering, audio and actual FPS still need local verification. Blender previews are asset previews, not gameplay screenshots.

## Open or update in Unity

Use **Unity 6000.3.25f1** and the pinned **URP 17.3.0** packages. Open this repository as a Unity project and allow imports to finish. Open the existing `Generated/Scrapyard` bootstrap scene and press Play. New runtime props, menus and gameplay appear automatically; do not delete `Generated/`, reset the scene/balance asset or remove your saves.

For a first checkout, use **Scrapshift → Create or Open Prototype**. It creates missing scene/balance/rendering assets and reuses existing ones. **Update Existing Prototype Visuals** validates tracked materials without replacing scene or balance data. Manually authored scenes without `YardBootstrap` need deliberate integration; they are not overwritten.

Older Balance assets receive defaults only for missing/zero new repair and investment fields. Existing wire rules and positive custom values are retained. Tune prices, yields, durations, work strokes and capacity in `Generated/Balance.asset`.

## Controls and Settings

| Default input | Action |
| --- | --- |
| WASD / arrow keys | Walk with collision |
| Mouse | Look |
| E | Use station / pick up item |
| Left mouse | One manual work stroke at a loaded bench |
| Q | Drop carried item |
| Escape | Pause, cancel rebinding or go back |

Pause → **Settings → Controls** lists all seven actions and their current bindings. Select a row, release the activation input, then press a supported key or mouse button (buttons 1–7). Capture commits mouse bindings after release so Cancel/Back remain usable. Conflicts offer **Swap** or **Cancel**, keeping every action bound. Escape is reserved permanently. Default movement aliases cannot silently steal a customized arrow binding. Sensitivity and invert-Y apply immediately; Restore Defaults resets controls/look only.

Gameplay/look are blocked during all menus and capture. Captured input is consumed through release. Resuming requires held menu/gameplay buttons to be released before fresh gameplay input is accepted. HUD/objectives/control hints use actual bindings. Unity's legacy input backend is retained, with centralized action bindings; no input-system migration is needed.

**Video** offers Laptop/Balanced/Detailed, 30/60/120 frame limits, 55–95° FOV, an optional warm grade and an optional gameplay FPS/frame-time readout. Presets use 75/90/100% render scale and 20/35/50m shadow distance. Laptop uses shorter hard shadows and disables post processing. Balanced/Detailed can use subtle warm grading and bloom. The HUD remains at display resolution. Settings change a private runtime URP clone and restore Editor pipeline/VSync/frame cap when it is disposed.

**Audio** controls master, effects and ambience. Original gravel footsteps, tool/handling/sale cues and nearby machine/fan loops provide feedback; outdoor breeze and sparse birds provide ambience. Gameplay sounds pause with menus; quiet ambience continues. Video/audio defaults are separate from Controls defaults.

## Lighting and visual direction

The yard now uses an editable `Resources/ScrapshiftLighting/CozyAfternoon` profile: a blue-to-warm gradient sky, neutral warm daylight, three downward task lights over stripping/buying/restoration, matching horizon fog and a small deterministic sky reflection map. Diffusers glow mildly; contact shading grounds static props through one collider-free mesh. Laptop/Balanced/Detailed use one/two/four shadow cascades with the existing render-scale/shadow-distance/map-resolution budgets. Laptop still uses hard shadows and no post processing. The editor enables the generated `ScrapshiftURP.asset` soft-shadow capability flag idempotently so Balanced/Detailed variants are included in player builds; all other pipeline values and its GUID are preserved. No realtime reflection probes or fullscreen ambient-occlusion pass are added.

Assign a custom **Yard lighting profile** on the existing bootstrap to tune colors, fill, sun and fog; a missing assignment uses the tracked default. Sky/reflection/emissive materials are privately owned, and stopping Play restores the previous sky, ambient, fog, reflection and camera background. Tracked materials/scene data and game/preferences saves are preserved. Actual Unity rendering and performance remain unverified in Cloud.

The next finishing milestones are in [GAME_FINISHING_PLAN.md](GAME_FINISHING_PLAN.md). The new lighting is an implemented source milestone; the radio loop, full title flow and finished player build are planned.

## What to do in the yard

The 96 × 80m yard has a central workshop, western vehicle/appliance salvage, eastern metal sorting, northern loading/storage and southern office/entry district. Pause → **Yard map** shows your position and thirteen numbered destinations. Select a pin or station to track it and resume; **Follow objective** returns to automatic guidance. The HUD shows destination, distance and Ahead/Left/Right/Behind/Nearby relative to your view. Automatic guidance follows carried material, ready/partial work and storage, selecting the nearest renewable wire crate when supply is needed. Bearings guide direction; they do not find a path through obstacles. A selected destination stays tracked until changed or New game and is not saved between sessions. Office, cars, containers, crane and radio are scenery; no driving, crane operation or enterable office is implemented.

### Wire processing

The manual bench shows an actual cable coil, emerging copper and a short pliers stroke on successful work. Looking at a loaded wire/restoration bench shows current steps and a progress bar; repaired fans explicitly require a power-on test before the HUD marks them tested. Progress follows saved jobs and hand-tool upgrades. These animations stop with menus and use fixed decorative meshes without active physics.

1. Take renewable wire from **WIRE DELIVERY** or the three remote wire crates.
2. Place it on **STRIPPING BENCH**, then use four separate manual-work strokes with empty hands. Collect three copper.
3. Sell copper at **SCRAP BUYER** for €12 per default bundle. Three loads earn €36.
4. Buy **POWERED STRIPPER** for €36, feed one wire bundle and wait five seconds. Animated rollers and lamp indicate processing; collect copper from its tray with empty hands.
5. Sell, store or deliver that copper. The manual bench remains usable while the machine runs.

Aim at the station body within 3.2m. Prompts show current action, progress, price or reason it is unavailable. The aiming dot changes colour for a usable target. Carry one item at a time; dropped items stay as fixed objects rather than simulated rigidbody piles.

### Storage and customer orders

Wire/copper bins east of the workshop store your carried bundle. Use a bin with empty hands to retrieve one. Storage keeps the original item ID/quantity and counts toward yard capacity; it does not duplicate inventory.

Accept a request at **CUSTOMER ORDERS**, north of the workshop. Deliver copper there, including partial bundles over multiple trips. Five requests repeat with different quantities/rewards. Only required copper is consumed; surplus remains carried. Completion pays once. No deadline or daily fee. Pause → **Orders & storage** shows progress and stock.

### Fan restoration or salvage

**APPLIANCE SALVAGE**, southwest of the workshop, supplies two broken desk fans per day. Carry one to **RESTORATION BENCH**, northwest of the hub. Load it and inspect the motor, then choose:

- **Restore:** pay €8 for a replacement motor, perform three work strokes, explicitly power on/test, collect the tested fan and sell it for €42.
- **Dismantle:** perform four work strokes and collect three copper instead.

Parts are charged once; the repair/salvage choice and partial work persist. The tested bench fan spins and makes a nearby air sound. A repaired fan is never sale-ready until tested. Storage bins accept wire/copper only.

### Diary and investments

Use **YARD DIARY** near the entry. Day review shows income, cash, customer completions, repair totals and appliance stock. **Finish day / return tomorrow** refreshes fans and finishes the stripper's current load overnight, preserving inventory, player position, unfinished hand work and orders. No automatic clock or penalty forces you to end a day. Wire remains renewable.

| Investment | Price | Effect |
| --- | --- | --- |
| Storage rack | €60 | Twelve extra bundle slots, bounded at 100; a rack appears by the bins |
| Better hand tools | €75 | One fewer stroke per wire/repair/salvage job, minimum one; yields stay intact |
| Stripper tune-up | €120 | Requires the machine; future loads run 40% faster |

Purchases are guarded against double charges. Already-worked hand jobs complete if the new tool threshold is reached; repaired fans still require testing. A running machine retains its saved timer when tuning is purchased.

## Persistence

Under `Application.persistentDataPath` (company `XanderMaxStudiosBE`, product `Scrapshift`):

- `yard-v1.json`: cash/equipment/upgrades, carried/dropped/stored items, station and fan progress, orders, day statistics and player position/look.
- `controls-v1.json`: action bindings, sensitivity and invert-Y.
- `presentation-v1.json`: graphics/FOV/frame limit/FPS display and audio volumes.

Writes use flushed temporary files and atomic replacement with `.bak` recovery. Old gameplay enum values/version-one saves are retained with additive fields. Corrupt gameplay and backup block saving instead of silently overwriting progress. New game requires a second click and archives old saves. Preferences survive new games; preference failures keep valid defaults or backup values and display a notice. Offline elapsed time does not simulate work; advancing the diary is the explicit overnight operation.

A primary file with missing required fields now correctly tries the backup. If both files are unreadable, saving stays blocked and neither file is overwritten.

Saving occurs after transactions, periodically, on pause/focus loss and normal exit. Pausing freezes gameplay and processing. Saved player positions retain safe expanded-yard coordinates/height rather than reverting to the old small enclosure.

## Verification

Check original asset packaging without Unity/Blender:

```sh
python3 Tests/audit-original-assets.py
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

The same command also runs three filesystem/recovery checks against the real SaveStore with a narrow serializer adapter after its field checks. They verify missing-field primary recovery, double corruption protection and backup-only loading without modifying files. They do **not** validate Unity JSON; the existing Unity corruption test covers the real serializer.

Core coverage includes inventory/money conservation, exactly-once processing, capacity/output reservations, 100 renewable wire cycles, invalid/overflow/ID guards, rebinding/conflicts/defaults, dynamic guidance, expanded-world positions, storage/partial contracts, repair/salvage/day transactions, upgrade prerequisites/progress, legacy balance migration and presentation validation. Mixed tests reconstruct copied core data; actual JSON round-trips are supplied Unity tests.

In Unity use **Window → General → Test Runner → EditMode → Run All**. There are **128 supplied cases**, all unrun in Cloud, including actual gameplay/preferences JSON and backup recovery, old saves, model/audio imports, geometry/colliders, dynamic station parts and graphics cleanup. A source/syntax/Blender audit does not replace Unity compilation or a player build.

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

- Import with the pinned editor; check zero compile errors, normal URP shaders, metre scale/axes, atlas appearance, roof joins, shadow readability, signs and HUD at different Game-view sizes.
- Check gradient sky/sun direction, fog joins, workshop/fan readability, task-light cutoff/fixtures, static contact shade and copper reflections. Switch all presets and grade; watch cascade transitions/shadow shimmer while walking. Stop Play and confirm sky/fog/ambient/reflections restore. Run the supplied lighting/shader tests in Unity.
- Complete wire processing, orders, both fan choices and all investments. Test wrong inputs/full hands, duplicate purchase/collection, partial/surplus delivery, capacity and the explicit fan test.
- Save/reopen with carried/dropped/stored material, running/output-ready machine, paid partial fan repair, active partial order and upgrades. Confirm old saves and tuned Balance values. Verify archived New game and backups through tests.
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
