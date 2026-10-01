# Scrapshift project handoff

Updated: 2026-10-01 (UTC). Reinspect the checkout before continuing.

## Latest milestone: presentation, laptop presets and original yard sound

Settings retains Controls and adds Video/Audio pages. Separate validated `presentation-v1.json` with atomic backup stores Laptop/Balanced/Detailed preset, 30/60/120 frame cap, 55–95° FOV, optional warm grade and master/effects/ambience volumes. Presets use 75/90/100% URP render scale, 20/35/50m shadow distance, 512/1024/2048 shadow maps and 2/2/4× MSAA. Laptop uses hard shadows and disables post processing. Balanced/Detailed can enable mild exposure/contrast/desaturation, neutral tonemapping and restrained bloom. A private URP clone avoids editing project assets; Dispose restores original quality pipeline/frame cap/VSync/FOV/post/light shadows. Checked relevant setters against Unity Graphics 6000.3 source; soft-shadow support has an internal setter, so the implementation changes owned scene Light shadows instead. Runtime asmdef references pinned URP/Core libraries; no engine/input migration.

Seven original deterministic synthesized PCM clips are tracked with source/manifest. Four bounded sources play grounded distance-based gravel footsteps, successful tool/handling/sale cues, spatial stripper/tested-fan loops and low outdoor ambience. Effects/machine/fan pause while menus are open; ambience continues and volume changes apply immediately. The previous generic beep is removed. Core transactions still own gameplay; UI purchase cues can play while paused. A private cached GUI skin adds readable warm dark panels/buttons, compact HUD/prompts/dot crosshair and dimmed menu background without mutating shared GUI skin. Mounted decorative signs replace floating instructions at hub/fan/storage/remote stations; colliders/marker routing are preserved.

54 core cases pass, zero failed (three new presentation-validation/preset cases and one legacy-balance migration case). Four presentation core/store Unity cases plus eight audio/sign import checks bring the supplied Unity suite to 95 cases, all unrun here. Original WAV header/sample audit passes. All 56 C# files pass tree-sitter syntax parsing (this is not Unity compilation). Old local Balance assets get an idempotent migration of only zero/missing new repair/upgrade fields; positive custom values and original wire rules are preserved, while invalid negative values still fail validation. An actual Unity save test covers upgrade flags and a tuned partial machine load. Unity compilation/import, actual post-processing look, font/layout fit, audio listening/mixing, pause/capture transitions, preferences recovery, pipeline cleanup and laptop/standalone FPS remain local checks. Do not claim a render or frame-rate improvement from code checks. Continue the authorized session with integration review and meaningful economy/save tests.

## Latest milestone: useful yard investments

The diary now has an Investments page: €60 storage rack adds twelve item slots (bounded at 100), €75 hand tools shorten each wire/repair/salvage job by one stroke (minimum one), and €120 tuning after stripper ownership shortens future machine loads to 60% duration. Existing running loads retain their saved timer. Purchases validate a single known upgrade, prerequisites and money, charge once, and persist additive version-one flags. A new original 416-triangle rack appears beside the storage bins after purchase; no new blocking collider can strand previously dropped inventory. Effective progress/capacity hints update immediately.

50 pure C# scenarios pass; six new investment checks cover purchases, prerequisites, capacity, work/yield, active timers, bounds and resumed flags. Thirteen Blender FBX round-trip audits pass. The Unity suite supplies 81 cases including storage-rack import, all unrun here. Unity still needs checks for Investments layout, rack appearance, serialization, input isolation and runtime rendering. Continue the active session with audio/presentation/laptop settings.

## Latest milestone: fan restoration, salvage and a relaxed day loop

Implemented a second complete scrap loop. Appliance salvage west/south supplies up to two broken desk fans per day. Carry/drop/pickup uses original fan meshes and version-one item IDs. Load the separate restoration bench north/west of the hub; inspect the seized motor, then choose a replacement (€8) or dismantling. Three rebound manual-work strokes fit the motor, then an explicit power-on test enables collection/resale (€42). Four salvage strokes instead produce three copper. Choice/parts charge, outputs, test counters and resale are exactly-once guarded; full hands/capacity/overflow/ID limits never consume protected material. Fan head/source colliders resolve to their station markers; the tested bench fan has a separately pivoted authored rotor. Repair menu transitions now stop the rest of the same Update frame before simultaneous work input can leak through.

Yard diary near the entry shows day, income, cash, orders and restored/dismantled fans. Returning tomorrow refreshes daily appliance stock and finishes one running stripper load; inventory, partial repair, player position and no-deadline contracts persist. No daily fees or deadlines. The board/repair/diary interfaces share existing pause/cursor/Escape/input-release behavior. Wire remains renewable. Old item enum IDs are retained (new fan IDs appended), and all save additions default to empty/zero on older version-one files. Bin APIs explicitly reject fan kinds.

44 pure C# scenarios pass (ten new fan/day/ID-limit cases), zero failed. Added two Unity fan JSON/raycast/rotor checks and two authored frame/rotor cases: 74 engine cases supplied, unrun here. Twelve FBX meshes passed the Blender import/dimension/UV/budget audit. Audit caught the separate rotor's unbaked local rotation; export now bakes rotation and the corrected file passes. Rendered previews remain Blender asset previews, not Unity. Actual engine import, input/menu isolation, fan animations/audio, scene playability, day behavior and old-save migration need local checks. Continue the authorized work session with business upgrades, presentation, audio and laptop controls.

## Latest milestone: original authored industrial props

Following the user's criticism and Retro Rewind reference, authored ten original metre-scale props in Blender 4.3.2: worn/rusty hatchbacks, yard office, corrugated shipping container, broad trussed workshop canopy, sorting skip, salvage fan, workbench/vise, coiled-wire crate and bundled scrap pallet. These replace the matching primitive world/station visuals via `AuthoredYardProps`, keeping original station anchors, marker routing and explicit collision footprints. The fan mesh is prepared for the next repair milestone; no fan gameplay exists at this checkpoint.

All models share a tracked 512px worn atlas and URP material. Scoped `AuthoredPropImport` sets readable metre/axis imports for regional batching, disables imported animation/lights/colliders/materials, and uses bilinear atlas/mips. Original Blender source, manifest, exported assets and an inspected rendered contact sheet are tracked; the contact sheet is a Blender asset preview, **not gameplay**. Roof/windshield slope issues found in the first preview were corrected and rerendered. No models or image assets from Retro Rewind are copied.

Blender reimport audit passed for all ten FBX files: actual metre dimensions, matching triangle counts (308–1,684 per model), finite vertices and UVs within the shared atlas. Audit JSON records its exact scope. Ten new Unity cases require real imported meshes, correct vertical scale, readability, atlas/URP shader and no imported collision components; now 60 supplied engine cases, all unrun here. The existing 34 pure gameplay/control/guidance/business scenarios still pass. Unity import, shader/axis/material appearance, collisions/batching, saved-item accessibility and runtime FPS remain local checks. Source and regeneration instructions are in ArtSource/README.md. Continue the active work session toward the repair loop and presentation/performance settings; do not stop at this checkpoint unless the user says stop.

## Active work session: storage and customer orders

The user authorized sustained autonomous development (two hours from 07:15:32 UTC on 2026-10-01, unless they say stop). They then rejected the primitive visuals and supplied Retro Rewind - Video Store Simulator as the aesthetic reference, choosing worn retro realism: believable industrial shapes, muted worn textures and warm lighting. SCRAPSHIFT remains a scrapyard game. Visual refinement and a more interesting repair loop now take priority alongside the following completed business systems.

`ScrapItem.storage` retains the same item ID/quantity in wire/copper bins, counts toward the existing yard capacity, rejects wrong material/full hands and prevents pickup through a stored ID. `YardBusinessVisual` creates two physical bins east of the hub and a customer board north. Stored views are removed; bin stock displays refresh only on transactions. `CustomerOrders` defines five repeating no-deadline copper contracts. Accept/partial delivery/completion are guarded in YardModel; only required copper is consumed, final payment is checked before consuming the last material, surplus remains carried, and completion pays/advances once. Additive version-one fields preserve old saves (default zero/None/no active order). Orders/storage have dynamic binding prompts, physical interactions, map labels, live board notes, optional HUD status and a paused journal; the existing input release/escape gates remain.

Verification: 34 pure C# scenarios pass, zero failed. Eight new core cases cover storage/order conservation, capacity, partial/surplus delivery, resume, invalid states, overflow and prompts. Three Unity tests cover actual JSON/legacy saves and business markers; total Unity suite now 50 cases before the authored-asset milestone, all engine tests unrun here. No Unity performance/playability claim. Existing controls, saves, balance asset and processing loop are preserved. Local README checks cover all new interactions and persistence.

Original Blender-authored prop meshes and a shared atlas are under development in the current workspace, with a rendered asset preview (not Unity gameplay). Do not describe them as engine-verified; finish importer/runtime integration, asset audit and visual checks before the next milestone. No Retro Rewind assets/screenshots are imported into the game.

## Latest continuation: cozy retro visual refinement

User requested better visuals and asked to begin. Added `CozyYardDetails`: shared sage/blue/ochre/warm-window palette, pitched office roof/fascia/ridge, window framing, porch/door details and potted vegetation; workshop tool board/shelf/radio/mug/plant/lamp housing; original sixteen-triangle faceted tree/shrub meshes with metre UVs, outward flat faces and owned-mesh cleanup. `YardGeometry.PaletteMaterial` exposes its existing shared material cache without temporary objects. The world now uses faceted two-tier tree crowns, round twelve-sided vehicle tyres (existing Cylinder helper made public), painted vehicle roofs/windows/handles/headlights, container rails/stencil plates, delivery wheel ruts, dock markings and gate nameboard. Lighting uses a lighter haze and explicit sky/equator/ground ambient fill under the existing warm sun.

Preserved all gameplay, source/station anchors, save schema/preferences and physical collision footprints. Accents have no active colliders, simulation, frame updates or extra realtime lights; small details/vegetation omit shadows and are included in regional static batches. Live font geometry stays outside batching. Props remain decorative (radio, office, cars, containers). Existing texture/material assets and GUIDs are preserved; no engine/package/input migration occurred.

Checks: 26 core scenarios pass, zero failed; whitespace check passes. Added one Unity geometry/collider test (39 supplied Unity cases total), unrun here. Actual Unity import/compilation, geometry/sign/window/roof/wheel appearance, Trilight lighting, batching, saved-item access and Editor/player FPS need local verification. No screenshot, playable build or performance improvement is claimed. Pull and Play in the existing scene; no Generated reset required. Next gameplay work remains useful storage/customer orders and appliance repair after validating the visual/performance baseline.

## Latest continuation: an explorable scrapyard world

The user explicitly requested a full scrapyard rather than the small test area. Implemented a 96 × 80 metre yard in `ScrapyardWorld`, replacing the original enclosing fences/ground. Kept the existing hub stations and spawn at their old coordinates. Five readable districts: workshop center; nine salvage vehicles and two wire sources west; nine sorting bins, shelter and another source east; six containers, loading dock and stationary gantry north; office exterior and entry gate south. Original roof/drums/clutter are reused via `YardProps.WorkshopSurroundings`. Warm workshop lighting and original coarse materials remain, with a lighter distant fog/sky and blocky tree horizon.

Three renewable remote wire crates reuse Supply/AcquireWire and existing capacity, inventory, recipe and sale guards. `InteractionTarget.displayName` customizes supply prompts without hardcoded keys. No additional recipes, car dismantling/driving, crane operation, enterable office or functional container storage were added. This is an expanded explorable world around the working wire loop, with those props clearly scenery.

`YardWorldLayout` centralizes world bounds, source locations and area labels. Player restore and dropped-item placement now use the larger bounds. Restore retains player height (clamped to safe vertical limits) for the raised dock. Existing version-1 gameplay saves and independent input preferences are unchanged. New pause-menu Yard map shows districts/sources/player position; Back/Escape returns to pause, then Escape resumes. The existing paused early-return/input release gates keep gameplay and machine time stopped throughout.

World geometry uses shared materials and per-district static batching. It introduces no physics simulation, realtime lights or per-frame scenery loops; small decorative parts and distant crowns omit shadows. Larger scenery can still increase GPU/CPU cost: no FPS improvement or Unity rendering result is claimed.

Verification in Cloud: **26 pure C# scenarios passed**, zero failed, warnings treated as errors. New scenarios cover old/new bounds, district identification, and remote wire drop/state restoration/processing without duplicated money or material. Their state test is not JSON serialization. **38 Unity EditMode cases are supplied but unrun here**, including new source-approach/raycast/continuous-lane collision checks and actual JSON save/player restore for expanded coordinates, dock height and remote bundles. No Unity Editor is installed. Local compilation, rendering/static batching, collisions, map/input behavior, old-save compatibility in Play, desktop build and Profiler measurements remain required; use README's expanded-world checklist. Existing bootstrap scenes update on Play without deleting Generated or tuned balance/materials.

Next: verify the new world locally and record frame times, then populate it with useful storage, varied daily deliveries/customer orders and one appliance repair loop. Keep refining the cozy retro mood without multiplying physics objects. The previous performance/Settings work remains intact.

## Latest continuation: performance and cozy retro direction

The user reports the Settings/visual update works locally, but steady frame rate is low while looking/walking in the Unity Editor. No Profiler capture, hardware specification or standalone-build comparison has been provided. The desired art direction is now explicitly a **cozy retro simulator**; AGENTS.md records this preference.

Targeted source improvements: item/station views refresh on transactions/load/machine completion rather than every frame; bench/roller animation stays frame-based. Cache input KeyCodes and binding labels, reusable HUD styles, and the bench/lamp material instances. Batch only stationary ground/fence/workshop/clutter; dynamic stations/items/feedback stay outside the batch. Material clones are cleaned up on destruction. Named `Scrapshift.SyncViews` and `Scrapshift.Save` Profiler markers support local diagnosis without Deep Profile. Gameplay saves, balancing and package/editor versions are unchanged.

Checks: 23 engine-independent C# scenarios passed after the change; extended the existing Unity control-label test to cover cached labels through arrow-conflict swap and defaults restoration. Unity compilation, static batching, dynamic-view regression checks and actual FPS/frame-time improvement are **unverified here**. The 33-case EditMode suite remains a local check. The README gives a Profiler/Game-view/desktop comparison and explicit interaction/machine completion checks.

Next: verify frame times locally before expanding. Recommended next playable feature is fan repair (inspect/bad motor/replace/test/sell), then varied daily scrap and small customer orders, then a modest storage/yard expansion. Cozy atmosphere should come from warm lighting, soft fog, restrained pixel textures, relaxed pacing and satisfying workshop ambience, preserving readable controls.

## Current implementation and evidence

The first prototype was pushed to `main` as `2a0f4fe`. The user then ran it in local Unity **6000.3.25f1** and reached the purchased-machine stage. They initially had trouble identifying the machine's feed interaction. That is limited user playtest evidence; comprehensive save/build/test results were not supplied.

This continuation implements the requested **Settings/rebindable controls** and the explicitly confirmed uploaded **visual-clarity milestone**. It preserves the existing game and campaign assets, editor/package pins, balance asset data, material/economy transactions, station anchors and version-1 gameplay saves.

### Added this session

- Pause → Settings/Controls with seven rebindable actions, keyboard and mouse capture, cancellation, explicit conflict swap/cancel, defaults, sensitivity and invert-Y.
- Central engine-independent binding preferences and rebind state machine; legacy Unity input adapter. Default WASD/arrows, E, Q, left mouse and fixed Escape preserved. No input backend migration.
- Separate `controls-v1.json` with atomic writes/backup. Changes apply immediately; New game and yard loading leave preferences intact. Invalid settings recover backup/defaults with notice.
- Capture excludes the selection click and consumes captured UI events through release. Pause/menu/capture suppress gameplay; resume requires full key/button release and a fresh input. Escape always cancels or backs out.
- Current bindings in HUD prompts, contextual objectives, control legend and carried-item hints. Opening Settings clears old transient tutorial text so rebinds do not leave stale key labels.
- Seven original procedural tileable 64×64 PNGs, tracked reusable URP Lit materials, stable metadata, Resources-backed shader references and an import validator. Point/Repeat textures; metre-based box/cylinder UVs.
- Recognizable wire-filled delivery crate, table/work tools, copper buyer/scale/bin, gravel/fence/workshop props and powered stripper with input opening, counter-rotating rollers, exposed motor, output tray, status lamp, input/output displays and key-free labels.
- Contextual availability/error reasons, range guidance and usable-target crosshair color. Objectives derive from current state, including dropped bundles and machine-owned saves.
- Sale/purchase/feed confirmations; manual progress/pulse, rotating machine parts and distinct restrained sound tones.
- Non-destructive **Update Existing Prototype Visuals** command. Existing `YardBootstrap` scenes receive runtime improvements on next Play without replacing scene/balance assets. New props preserve original station collider footprints; the workshop remains open and extra clutter is collision-free to protect old dropped-item access.

### Verification

**Executed in Cloud:** real core C# compiled with Mono 6.12, warnings as errors; **23 scenarios passed / 0 failed** (12 existing gameplay, 7 control preferences/rebinding, 4 guidance). Core checks cover the complete wire economy, conservation/guards, partial station resumption, renewable supply, invalid state/time/overflow, binding defaults/cancel/conflicts/aliases/restore/validation/labels, all station hint states, custom key text, save-state objectives, and presentation read-only behavior.

The Settings agent also checked 10,000 deterministic rebind/cancel/restore operations and confirmed all 108 supported codes against Unity's official `6000.3` KeyCode enum. Asset checks confirmed PNG dimensions, matching tile edges, importer flags, material texture/shader references, metadata uniqueness and outward cylinder winding. These are source/asset checks, not Unity rendering evidence.

**Not executed in this Cloud:** Unity import/compilation, real menu/input capture, release gate timing with hardware, engine pause/cursor behavior, JSON settings/game saves, render/readability/collider feel, generated scene update, desktop build and shader inclusion. Existing user playtest applies to the previous prototype only. **33 EditMode cases** are supplied: the 23 shared scenarios, two existing gameplay persistence cases, four settings/legacy-key/persistence/menu cases and four geometry cases. No test runner or screenshots have been presented as successful Unity verification.

### Agent ownership

- Settings/input agent: preferences, capture/conflict UI, input adapter/persistence, control tests and event-isolation review.
- Texture agent: seven PNGs/materials, library/import validator, authoring code and texture documentation.
- Machine agent: stripper geometry and animation/display hooks.
- Environment agent: yard props, collision-compatibility review/fixes and geometry tests.
- Main agent: architecture/integration, player action routing, metre-scaled box geometry, prompts/objectives, feedback, scene update path, runner and docs.

## Next concrete task

1. Pull this update locally and open the existing scene in Unity 6000.3.25f1. Do not delete Generated or reset tuned balance/save files. Allow tracked materials to import; optionally run **Update Existing Prototype Visuals**.
2. Run all EditMode cases and inspect actual test outcomes. Play the full loop and the expanded Settings/visual checklist in README.
3. Verify every binding, supported mouse actions, Escape cancellation/back, swap/cancel including arrow aliases, defaults, immediate prompts, sensitivity/invert-Y, persistence across restart and independence from New game/game saves.
4. Hold actions while resuming/capturing; verify zero input leakage. Pause during machine work; verify no world progress or movement in menus. Check focus loss.
5. Inspect textured props/UV density, world-label readability, raycast reach/marker resolution, old dropped-item access and update repeatability; build a desktop player and check shader/text/preferences.
6. Record actual Unity evidence here. Tune spacing/feedback before implementing the proposed fan repair; no fan/conveyor/new recipe expansion was added.

The wire loop and Settings systems exist. Continue them; do not scaffold a second game.

## How to work here

Read `AGENTS.md` and `README.md`. Use the current checkout and relative project paths. The old Windows workspace paths are historical and must not appear in runtime logic. Cloud currently has no Unity Editor. Mono was extracted outside the checkout using Debian packages verified by APT; it is available in this instance at `/workspace/tooling/mono`.

From the repository root:

```sh
SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-core-tests.sh
```

With a system Mono installation, run `./Tests/run-core-tests.sh`. The shared cases live in `CoreScenarios.cs`, `ControlScenarios.cs` and `GuidanceScenarios.cs` under `Assets/Scrapshift/Tests/Editor/`; the standalone entry point is `Tests/CoreRunner.cs`. Unity-specific persistence tests are in `PrototypeTests.cs`.

The .NET installer and Unity package host returned HTTP 403 under current Cloud networking; no Unity download/import was attempted successfully. Debian's normal package mirror was accessible. No additional secrets were requested. The code can be developed and core-tested here; Unity verification must currently happen locally.

## Structure and constraints

- `Assets/Scrapshift/Core`: yard state/rules/transactions, ControlPreferences/ControlRebind and YardGuidance. Pure C#.
- `Assets/Scrapshift/Runtime`: player/action input, SettingsMenu/preferences store, game coordinator/HUD, save store, balance type, textured low-poly prop builders and material library.
- `Assets/Scrapshift/Editor`: setup/non-destructive visual update and texture/material validator. Scene and Resources materials reference URP shaders for builds.
- `Assets/Scrapshift/Tests/Editor`: shared scenario wrappers, game/control persistence, menu/key and geometry tests.
- `Assets/Scrapshift/Art/Textures`, `Resources/ScrapshiftMaterials`: original PNGs, authoring source and tracked reusable materials.
- `Packages`, `ProjectSettings`: pinned editor/package setup and legacy mouse/keyboard input axes.
- `Campaign/Scrapshift`: preserved campaign source artwork and documents. The ZIP is an older export, not authoritative for current docs.

Prototype limitations: one material recipe, a static item-placement model (no throwing/rigidbody piles), immediate inspection through prompts, a minimal IMGUI interface, low-poly procedural geometry, one upgrade, no repairable appliances/conveyors/power network, and no verified engine build yet. A pause delta test proves model timing behavior; it does not prove Unity input/UI behavior. The core partial-resume test is not a serialization round-trip. Both distinctions matter when reporting readiness.

## Product direction

Scrapshift is a single-player, first-person retro 3D scrapyard simulator. Tagline: **Start with your bare hands. Build a scrapyard that works for you.** Acquire scrap → inspect → repair or dismantle → recover materials → sell → improve the yard. The selected concept is the scrapyard; the earlier video-store business was rejected.

Progression: manual carrying/stripping/sorting → powered individually fed machines → conveyors/separation/automated sorting → connected industrial material flow. These later stages remain plans. A fan repaired by replacing its motor is a sensible next repair experiment. Radios, car batteries, extra tools, vehicles, larger yards, power, and orders are not implemented commitments.

Art: chunky late-1990s geometry, coarse surfaces, rust, muted olive, charcoal, aged ivory, fog and warm workshop light. Campaign illustrations are mood references, not gameplay screenshots or an exact runtime target. Keep UI legible. Desktop keyboard/mouse is a prototype target, not a release-platform promise.

## Preserved campaign context

The creator confirmed Antwerp, Belgium and euro funding for game production. The working title has not been cleared. No production budget, funding target, reward commitments, release date, or final platform list has been agreed.

Historical private draft URL: https://www.kickstarter.com/projects/775234141/1027128765/edit/basics?ref=pbuild_dashboard

The earlier browser session observed title, pitch, cover, Antwerp location, story and risks saved, with AI involvement selected. Final detailed AI-use/source explanations were entered but not confirmed saved. Browser state was not re-audited during development. No campaign publishing, review submission, agreements, identity/payment work, or new commitments are authorized by this game task.

The campaign kit includes the transparent logo, cover, social poster, progression concept image, seven section headers in PNG/SVG, asset manifest, generation prompts, story draft, and HTML gallery. Preserve concept-art labels and AI disclosures. Capture genuine game footage only after Unity validation.
