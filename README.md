# SCRAPSHIFT — first wire-loop prototype

Start with your bare hands. Build a scrapyard that works for you.

A Unity/C# prototype implementation of a compact, first-person scrapyard. The repository now includes gameplay code and a reproducible scene generator. **Unity import, editor compilation, visual quality, and playability have not yet been verified.** The engine-independent rules compile and pass 12 executable checks in Cloud. Campaign illustrations remain concept art, not gameplay captures.

## Open locally

1. Install **Unity 6000.3.25f1** in Unity Hub, with your desktop build support. Add this repository's root as the project (the folder containing `Assets`, `Packages`, and `ProjectSettings`).
2. Open it and allow package import. `Packages/manifest.json` targets **URP 17.3.0** and **Test Framework 1.6.0**; dependency resolution still requires verification in this editor. Do not silently upgrade the editor to resolve an import problem.
3. Choose **Scrapshift → Create or Open Prototype**. The command creates URP renderer/pipeline assets, a balance asset, a copied campaign logo, and `Assets/Scrapshift/Generated/Scrapyard.unity`. It assigns scene references and adds the scene to Build Settings. If Unity requests a restart for legacy input, accept it and reopen the scene.
4. Press Play and click the Game view to capture the mouse. There is no essential Inspector wiring to do manually.
5. To create a desktop executable, open **File → Build Profiles**, select your installed desktop target, check that `Scrapyard` is included, and **Build And Run** into `Builds/`.

The generated scene creates original primitive geometry at runtime: fence, corrugated workshop, salvage pile, delivery crate, workbench, copper buyer, machine, and collection bin. No paid assets or external runtime services are required. Package installation uses Unity's package service; editor installation/licensing is managed by Unity Hub.

The setup command reuses existing generated balance, pipeline, and scene assets. It does not overwrite scene edits on a second run. Generated assets are ignored until editor verification; to share approved scene/balance edits, deliberately move them into a tracked asset folder or force-add the generated assets and their `.meta` files. All supplied source assets already have stable `.meta` files. Preserve GUIDs.

## Controls and first playthrough

| Input | Action |
| --- | --- |
| WASD / arrow keys | Walk with collision |
| Mouse | Look |
| E | Use the station or pick up the bundle under the crosshair |
| Left mouse click | One manual wire-stripping stroke while looking at the loaded bench |
| Q | Drop the carried bundle in front of you |
| Escape | Pause/resume and release/capture the cursor |

1. Walk to **1 / DELIVERY**, look at the crate within reach, and press E for a free wire bundle. Inspect its type in the HUD. Q drops it; aim at a dropped bundle and E picks it up again.
2. At **2 / WORKBENCH**, press E to place the wire. Click four separate times while aiming at the bench. The material changes size/color as insulation is removed; copper is visible when complete. Press E with empty hands to collect three copper units.
3. At **3 / SELL COPPER**, E sells the carried bundle for **€12**. Repeat three manual loads to earn **€36**.
4. At **4 / POWERED STRIPPER**, E buys the machine. Get another wire bundle and E feeds it. The roller turns and the status lamp changes while processing for five seconds.
5. With empty hands, E collects three copper units from the finished machine. Sell them for another €12. The bench remains usable while the machine runs.

Look at a station for its current action, price, progress, or output count. You can carry one bundle. The free delivery source is renewable; loose and station-held bundles share a cap of 24 so finished output has room to be collected. All materials are represented by bounded data records; dropped bundles are fixed collision objects rather than persistent rigidbody simulations.

Tune copper yield/unit value, upgrade price, manual stroke count, processing time, and bundle cap in `Generated/Balance.asset`. The initial milestone uses one wire-to-copper recipe, not a general factory framework.

## Persistence

The save is `yard-v1.json` under `Application.persistentDataPath` (company `XanderMaxStudiosBE`, product `Scrapshift`). It records money, ownership, carried/dropped bundles and positions, bench progress/output, machine remaining time/output, and player position/look. Saving occurs after interactions, periodically, on pause/focus loss, and on normal exit.

Writes use a flushed temporary file and same-volume atomic replacement with a `.bak` fallback. A corrupt primary attempts the backup; if both are unreadable, saving is blocked and a warning is shown. **New game** requires a second click and archives existing saves before resetting. File-system permission failures are reported. Offline time does not advance machines. Pausing freezes processing; restored player height is normalized to the ground inside the fence.

## Verification

Executed in Cloud: the actual pure C# core and shared scenario source compiled with Mono 6.12, warnings treated as errors; **12 scenarios passed, zero failed**. Coverage includes the full manual-to-machine economy loop, carry/drop, invalid inputs, one-time consumption/output/sales, purchase guards, reserved output capacity, partial station resumption, zero/invalid time steps, invalid save-state rejection, overflow protection, and 100 renewable scrap cycles. The partial-resume core test reuses data; it is **not** a serialization test.

Repeat on a machine with Mono:

```sh
./Tests/run-core-tests.sh
```

This Cloud instance has an extracted Mono installation outside the repository:

```sh
SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-core-tests.sh
```

In Unity, open **Window → General → Test Runner → EditMode → Run All**. Expected: 12 shared core cases plus two save-file tests (14 total). The two additional tests exercise `JsonUtility` round-trip, atomic replacement, backup recovery, and corrupt-save protection. **These Unity tests have not been run here.**

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
- Build and run a desktop player; repeat a complete loop. Check for missing shaders/text and verify the player build's save directory.

## Code map

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

1. Import and playtest in Unity 6000.3.25f1, fix integration issues, and commit verified generated assets and package lockfile.
2. Tune movement, station spacing, feedback, and pacing from a full manual-to-automatic playthrough.
3. Add one repairable appliance (for example a fan/motor) before expanding to conveyors or larger yards.

See `PROJECT_HANDOFF.md` for continuation status. This prototype adds no Kickstarter funding target, reward, date, or release commitment.
