# Scrapshift project handoff

Updated: 2026-09-30 (Europe/Brussels). Reinspect the checkout before continuing.

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
