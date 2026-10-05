# Compact whole-object automation and finishing pass — 2026-10-05

Stage D is implemented in source: paid intact-object intake, a primary dismantler, standing deliveries, material dispatch and connected sorting. This completes the brief's manual → powered → conveyor → whole-object automation sequence. It does **not** establish a finished release, Unity compilation, rendering, native serialization, player performance or a verified build.

## What players can do

- Buy and freely place the level-12 primary dismantler and material dispatch station. Existing enum identities are retained; primary is appended as kind 9. Both recurring services start off.
- Load an intact, uninspected car/fridge already owned at receiving into the primary without buying it again. Partially dismantled objects stay in their manual chain. Whole objects never become portable conveyor bundles.
- Review and explicitly enable a standing delivery. Each object costs its actual recipe price. The clock advances only while powered, funded, idle and able to reserve every output. Purchases stop safely without fees, debt or offline catch-up. Turning deliveries off preserves an already paid job, which can finish with power.
- Connect the primary component output to storage, powered Tier 2 and sorting. Fridges also yield ready plastic: route it directly to export on a material branch, while components go through Tier 2.
- Feed saleable materials into export, review/cancel/confirm a powered shipment, or explicitly enable automatic dispatch. Materials retain their recovered/imported sale-XP eligibility. Exports use ordinary material prices and reputation; they do not consume customer requests or pay their bonuses.
- Inspect receipts, current controls, actual timers, power, blocked capacity and available outputs. Confirmation guards reject changed quotes, intake or service terms. Menus, Settings and focus loss pause processing; paid primary progress appears on the HUD.

Default new rules specify primary €350 / 6×7m / 6kW / 24s / 48 output units; export €200 / 3×2.5m / 2kW / 5s / 48 units; standing delivery 60 eligible idle seconds. Existing valid positive custom/serialized machine values are preserved, including the former export's positive duration/capacity. The interface reads the actual prepared rules. No new equipment is granted at startup.

## Persistence and integrity

`yard-v2.json` now uses schema 4. Valid schema 2/3 saves upgrade explicitly in memory, retaining earned cash/XP, identities, owned equipment, partial work, transport and career data. Reads do not rewrite source bytes; the next successful save retains the earlier file as backup. Unsupported/corrupt state remains protected. Preferences remain separate.

Primary intake snapshots the whole-object ID, kind, duration, yields and eligibility. Output-unit/global-stack reservations and all future IDs are checked before charging or consuming. Completion commits all components once. Power, capacity and identity blockage preserve the object/progress. Busy or enabled industrial equipment cannot move or be dismantled. Export quotes and commits share the ordinary guarded sale calculation, retaining stock on cash/XP overflow. See [core verification](COMPACT_INDUSTRY_CORE_VERIFICATION.md) and [integration contract](COMPACT_STAGE_D_CONTRACT.md).

Industrial events use chronological time and durable-ID ordering. Default intake plus processing is an 84-second unclogged cycle. A tick requiring more than 8,192 estimated industrial transitions rejects before changing industry state; callers must split that unusually large/custom-tuned elapsed input. Normal frame deltas are below this bound.

## World, feedback and performance work

Two new original machinery FBXs share the working atlas: a 3,032-triangle open gantry/cradle and a 1,612-triangle dispatch/roller/weighing station. Loaded car/fridge views are cached by saved identity. Private timing marks and clamps stop with paused/blocked work. Ports and power sockets follow actual scaled definitions. [Machinery authoring and audits](../ArtSource/INDUSTRY_ASSETS.md) record reproducible source checks.

Four new original salvage props replace repeated perimeter stock: stripped hatchback shell, washer/microwave lot, timber cable reel and radiator rack. The existing 29 pooled roots, thirteen coarse collision volumes, occupancy clearance and central building space remain. Public stock is 193 pre-batch renderers / 42,960 triangles versus 203 / 34,860 previously. This is a source budget, not a measured frame-rate improvement. [Salvage verification](COMPACT_SALVAGE_VERIFICATION.md) records fallback and geometry checks. Both new previews are labelled Blender asset renders, never gameplay.

Intermediate manual strokes apply immediately but use a fixed one-second save window. Further strokes cannot postpone it. Completed work, purchases, sales, pause/focus and explicit save/quit still commit promptly. A failed checkpoint retains progress and stops per-frame retries; new edits or explicit saves permit another attempt. Periodic saves also wait after a reported failure.

Settings sliders apply immediately and save 0.4 seconds after the last edit; close/focus/quit/disposal flush the final values. Rebinding, restore-defaults and existing public save APIs remain immediate. Audio previews skip unrelated graphics application.

Typed equipment view snapshots replace repeated key-string construction. Timer/stroke/quantity-only changes preserve roots and unchanged content displays; readiness, depleted outputs and changed visible materials refresh details. The single motor loop follows the nearest actually powered working machine, with stable ID ties. Compact save, view synchronization and HUD refresh now have Unity profiler markers. No performance result is claimed without local measurement.

The default XP curve is shorter, with level 10 at 640 XP and level 12 at 1,040. Exact older defaults migrate once; custom curves and already earned cash/XP remain. Purchases, level gates, recipe sale XP and request rewards are unchanged. [Pacing audit](COMPACT_PACING_VERIFICATION.md) records legitimate paid routes and separates simulated timers/transaction counts from real playtime.

## Executed cloud checks

- 303 pure C# scenarios pass with warnings treated as errors: prior manual/career/control/transport behavior plus 23 industry, 11 connected industry integration, 16 industry guidance, 9 pacing, 9 preference-write and 8 checkpoint cases.
- Fourteen real filesystem/recovery branches pass using production save stores and narrow snapshot adapters. New branches preserve schema-three transport source bytes and resume a partially processed paid primary exactly once; invalid industrial saves leave primary and backup intact. These are **not** Unity JSON tests.
- Ten runtime-material, nine editor-recovery/report and four dressing-occupancy branch groups pass. Existing working texture/material recovery remains covered.
- Actual coordinator/Core/controller/settings and actual equipment/work/industry presentation helpers compile against controlled API/value adapters with warnings as errors. Seventeen actual coordinator branches exercise cache identity, pause/focus, manual checkpoints, all-phase transaction saves and nearest motor selection; sixteen industry UI guards and twelve presentation groups also pass. Preference/runtime files retain their separate six controlled persistence checks. Engine-heavy helpers are explicitly adapted, not silently treated as Unity execution.
- Blender round-trip and independent packaging audits pass for the six new FBXs, including bounds, normals, finite/nondegenerate geometry, UVs, material count and hashes. Original/compact/automation/world/CC0 tyre/ground/backdrop audits, nine material checks and six texture checks pass. Existing art/material/catalogue/profile/scene/package bytes are preserved; new metadata is complete and GUIDs are unique.

Native Editor tests are supplied for real Balance/yard JSON, industry transactions/transport/guidance/menus, imported geometry, cached work presentation, preference persistence and runtime efficiency. **All native Unity tests remain unrun.** No Store pack was purchased or added publicly; optional private piston integration and original fallbacks remain.

## Local verification before acceptance

1. Close Unity before pulling, preserving local import/material edits if Git reports a conflict. Reopen the existing CompactScrapyard and allow new FBX/script imports. No scene or save reset is needed. Confirm no red Console errors, then run the EditMode suite.
2. Play a fresh earning loop and continue representative schema-2/3 saves. Verify exact cash/XP, custom curves, partial customer requests, controls/settings, manual work, pause/focus, checkpoint failure/retry, backup recovery and New Yard archives.
3. Buy/place/connect the industrial equipment. Review/cancel each service before enabling; load an intact owned object without another charge; change the next purchase kind; stop deliveries while paid work continues; test cash/power/overload/capacity/ID blockage and partial/completed reloads.
4. Build car and mixed-fridge production routes. Check material-only export admission/filtering, backpressure, ordinary recovered/imported XP, manual/automatic quotes/receipts, and safe move/dismantle restrictions. Keep early manual earning available.
5. Inspect signs, new stock, original machinery/load views and walk/build clearance at player height on all presets. Test batching, repeated Play cleanup, small-window menus and audio near multiple working machines. Capture Editor and standalone CPU/GPU frame times using the new profiler markers.
6. Create and play a desktop build with Resources/shader inclusion, persistence and controls verified. Obtain actual visual/pacing acceptance. Pure/source/Blender checks cannot replace that evidence.
