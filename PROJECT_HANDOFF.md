# Scrapshift project handoff

Updated: 2026-09-30 (Europe/Brussels). Reinspect the checkout before continuing.

## Current implementation and evidence

The Cloud checkout was advanced from `bfad65f` to `dfa6ec9` (`Add Codex Cloud handoff`) before development. Those commits contained only campaign assets and handoff documents. This session added the first Unity project implementation directly in the existing repository; no earlier working gameplay systems were replaced. Campaign source files are unchanged.

**Implemented, but not yet Unity-verified:**

- Unity **6000.3.25f1**, URP **17.3.0**, Test Framework **1.6.0** project scaffold.
- `Scrapshift → Create or Open Prototype` editor command generates a referenced bootstrap scene, pipeline/renderer, balance asset, and copied logo. The generated yard is constructed at runtime from original primitives. Existing generated assets are reused.
- Fenced yard, covered corrugated workshop, repeatable scrap supply, manual bench, selling area/bin, and powered wire stripper.
- CharacterController first-person movement/collision, mouse look, raycast interaction/inspection prompts, carry/drop, and crosshair.
- Four manual stripping clicks produce a three-unit copper bundle; selling pays €12. Three loads fund a €36 machine. Powered processing takes five seconds and provides collectable output.
- Editable balance data; one carried bundle; bounded world/station inventory. Station transactions consume once and do not recreate already collected outputs. Capacity reserves room for station outputs.
- Machine status lamp, rotating roller, bench progress geometry/color, synthesized tool feedback, money/objective HUD, pause/focus handling, and two-click New game.
- Save data includes money, ownership, carried and dropped bundles/positions, manual progress/output, machine remaining time/output, and player pose. Atomic replacement, backup fallback, unreadable-save protection, and archiving on New game are implemented. Machines do not progress offline.
- Stable `.meta` files for source assets; generated directories and build outputs ignored.

**Actually verified in Cloud:** the engine-independent C# core compiled with Mono 6.12 with warnings treated as errors. **12 scenarios passed / 0 failed**: complete loop, carrying/dropping, invalid inputs, bench exactly-once behavior, machine exactly-once behavior, purchase guards, output capacity, partial state resume, zero/invalid elapsed time, invalid state rejection, overflow protection, and renewable supply. These checks execute the real core sources, not a translated implementation or Unity API stubs.

**Not verified:** Unity package resolution/import, engine/editor C# compilation, the scene generator, URP presentation, input/collision feel, audio, actual pause/focus behavior, Unity JSON/file persistence tests, and desktop build/playability. Two Unity save-file tests plus the same 12 core cases are supplied, but their Unity run is outstanding. No gameplay footage or playable executable has been produced. Do not describe this as a verified playable build.

## Next concrete task

1. Open the repository root in **Unity 6000.3.25f1**, resolve the pinned packages, and fix any import/compiler issues without silently migrating engines/editor versions.
2. Run **Scrapshift → Create or Open Prototype**, open `Assets/Scrapshift/Generated/Scrapyard.unity`, and run the 14 EditMode cases.
3. Follow the complete local playtest checklist in `README.md`, including material conservation across quit/reload, capacity pressure, pause/focus, corrupt-save backup, and New game.
4. Build and run a desktop player. Confirm shader inclusion, readable world signs, and saves. Record real results here.
5. Commit the verified scene/data/render assets and Unity-generated package lockfile when appropriate. Generated assets are currently ignored deliberately; move them into a tracked directory or explicitly add them with their `.meta` files. Do not lose edited balance or scene work by blindly regenerating them.
6. Tune presentation and pacing; only then move on to a repairable appliance or conveyors.

The first loop's source is now present. **Do not scaffold a second game or rebuild these systems from scratch.**

## How to work here

Read `AGENTS.md` and `README.md`. Use the current checkout and relative project paths. The old Windows workspace paths are historical and must not appear in runtime logic. Cloud currently has no Unity Editor. Mono was extracted outside the checkout using Debian packages verified by APT; it is available in this instance at `/workspace/tooling/mono`.

From the repository root:

```sh
SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-core-tests.sh
```

With a system Mono installation, run `./Tests/run-core-tests.sh`. The shared cases live in `Assets/Scrapshift/Tests/Editor/CoreScenarios.cs`; the standalone entry point is `Tests/CoreRunner.cs`. Unity-specific persistence tests are in `PrototypeTests.cs`.

The .NET installer and Unity package host returned HTTP 403 under current Cloud networking; no Unity download/import was attempted successfully. Debian's normal package mirror was accessible. No additional secrets were requested. The code can be developed and core-tested here; Unity verification must currently happen locally.

## Structure and constraints

- `Assets/Scrapshift/Core`: serializable yard state, balance rules, transactional inventory/economy/processing. Pure C#.
- `Assets/Scrapshift/Runtime`: player, interaction targets, game coordinator/HUD, save store, balance asset type, procedural yard.
- `Assets/Scrapshift/Editor`: reproducible setup command. Scene references include a URP shader to retain it in builds.
- `Assets/Scrapshift/Tests/Editor`: core scenario wrapper and persistence tests.
- `Packages`, `ProjectSettings`: pinned editor/package setup and legacy mouse/keyboard input axes.
- `Campaign/Scrapshift`: preserved campaign source artwork and documents. The ZIP is an older export, not authoritative for current docs.

Prototype limitations: one material recipe, a static item-placement model (no throwing/rigidbody piles), immediate inspection through prompts, a minimal IMGUI interface, procedural box geometry, one upgrade, no repairable appliances/conveyors/power network, and no verified engine build yet. A pause delta test proves model timing behavior; it does not prove Unity input/UI behavior. The core partial-resume test is not a serialization round-trip. Both distinctions matter when reporting readiness.

## Product direction

Scrapshift is a single-player, first-person retro 3D scrapyard simulator. Tagline: **Start with your bare hands. Build a scrapyard that works for you.** Acquire scrap → inspect → repair or dismantle → recover materials → sell → improve the yard. The selected concept is the scrapyard; the earlier video-store business was rejected.

Progression: manual carrying/stripping/sorting → powered individually fed machines → conveyors/separation/automated sorting → connected industrial material flow. These later stages remain plans. A fan repaired by replacing its motor is a sensible next repair experiment. Radios, car batteries, extra tools, vehicles, larger yards, power, and orders are not implemented commitments.

Art: chunky late-1990s geometry, coarse surfaces, rust, muted olive, charcoal, aged ivory, fog and warm workshop light. Campaign illustrations are mood references, not gameplay screenshots or an exact runtime target. Keep UI legible. Desktop keyboard/mouse is a prototype target, not a release-platform promise.

## Preserved campaign context

The creator confirmed Antwerp, Belgium and euro funding for game production. The working title has not been cleared. No production budget, funding target, reward commitments, release date, or final platform list has been agreed.

Historical private draft URL: https://www.kickstarter.com/projects/775234141/1027128765/edit/basics?ref=pbuild_dashboard

The earlier browser session observed title, pitch, cover, Antwerp location, story and risks saved, with AI involvement selected. Final detailed AI-use/source explanations were entered but not confirmed saved. Browser state was not re-audited during development. No campaign publishing, review submission, agreements, identity/payment work, or new commitments are authorized by this game task.

The campaign kit includes the transparent logo, cover, social poster, progression concept image, seven section headers in PNG/SVG, asset manifest, generation prompts, story draft, and HTML gallery. Preserve concept-art labels and AI disclosures. Capture genuine game footage only after Unity validation.
