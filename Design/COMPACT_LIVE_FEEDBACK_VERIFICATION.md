# Live line feedback and bounded checkpoints — 2026-10-09

This continuation adds operating feedback to the existing compact yard and fixes
repeated background save attempts. It preserves the reference-yard construction
work, save schema 4, paid jobs, cargo lineage, catalogue prices and progression.
Unity 6000.3.25f1 / URP 17.3.0 are unchanged. Unity is unavailable in this workspace.

## Implemented

`AutomationModel.FlowStatus(beltId)` is read-only. It distinguishes empty or
filtered OUT, unfinished manual/powered/primary work, eligible output, normal
spacing/capacity waits, incompatible or full IN, travelling cargo and a head
stopped at IN. Inventory-record and item-identity limits retain stock and explain
why new units cannot launch. Missing routes/endpoints are described safely.
Launch uses the same candidate/spacing/allocation helpers as the query; diagnostic
formatting occurs only in the query, without rebuilding the transport layout.

The query follows actual transport rules. A full destination does not prevent
launching; incoming cargo travels and then waits at IN. Processor input filters
do not filter paid recovered outputs. Supported later yields can leave while
unsupported earlier yields remain reserved. A final existing stock unit can
reuse its ID/record; a reserved output still needs a new ID.

Looking at a connected physical mouth shows the actual machine names, IDs,
OUT/IN indices and route status. Empty connected IN uses the current Interact
binding; carried intake keeps its existing compatibility/capacity refusal.
Machine bodies show independent processor IN/OUT quantities, reserved versus
ready outputs, full bays, selected filters and idle processing blockers. Route
cards show operating status separately from loaded-belt dismantling restrictions.
HUD feedback uses the existing cached refresh cadence and measured text layout.
No processing, transfer, money or inventory is performed by these queries.

Processing, industrial timers/sales and conveyor progress queue a fixed
one-second checkpoint from the first unsaved change. Ongoing work cannot postpone
it. The former immediate save on every allocated transit ID is removed.
Successful saves clear both manual/passive windows and move the periodic deadline
15 seconds ahead. A failed save retains live progress, disarms pending requests
and blocks background rearming/retries; Pause's explicit save, lifecycle saves
or the existing new-manual-stroke retry can recover. Purchases, sales, manual
completion, explicit saves and pause/quit boundaries remain immediate. Automatic
journal completion flushes its pending changes once and respects a prior failure.
Focus loss attempts one snapshot even when pausing already flushed pending work.

## Executed source checks

Run from the repository root; `mcs`/`mono` may be on PATH or an unpacked toolchain
may be supplied using `SCRAPSHIFT_MONO_ROOT`:

```sh
Tests/run-core-tests.sh
Tests/run-port-interaction.sh
Tests/run-automation-checkpoint.sh
python3 Tests/audit-original-assets.py
python3 Tests/audit-material-bindings.py
python3 Tests/audit-texture-metadata.py
```

- 359 pure scenarios pass with warnings-as-errors, including 12 new flow groups.
  Repeated queries preserve the complete state/rules/notices/layout cache;
  filters/full arrivals/resume, independent bays, mixed yields, power/paid primary
  work, record/ID limits and distinct splitter branches are covered. Existing
  filesystem recovery, materials, editor recovery and occupancy groups also pass.
- 18 portable interaction/presentation groups execute 34 unchanged extracted
  coordinator methods plus five real socket helpers. Added cases exercise body
  bay/filter feedback, both connected mouths, rebound input labels, carried
  refusal priority and route-card backpressure/dismantling separation.
- Nine checkpoint groups execute 18 unchanged production scheduling/save methods
  with actual core/input settings and a controlled clock/writer. Continuous
  transport saves nine times in 9.6 seconds and stays at least one second apart
  through the periodic deadline. Forty seconds of background transport after a
  failed write makes one attempt/warning, conserves all 24 units and succeeds on
  explicit retry. Timer-only work, automatic money/XP, purchase/sale, pause,
  focus/quit, chapter completion and manual retries/completion are covered.
- Original asset/GUID, material and texture checks pass. C# syntax and whitespace
  checks pass. No original authored asset, scene, balance or package is changed.

These adapters check source branches and conservation, not Unity compilation,
the native serializer, real input/physics, disk latency, draw calls or frame time.
The native `CompactFlowStatusTests` and expanded `CompactRuntimeEfficiencyTests`
are supplied and have not been run in Unity.

## Next native acceptance

Open the existing compact scene/save without resetting or regenerating authored
scenes. Run EditMode tests, then play a purchased manual/powered line: inspect
body bays and exact mouths during manual work, disconnected power, changed
filters, full IN, stopped cargo and recovery. Check splitter indices and actual
rebound controls. Verify the measured HUD at the supported presets/aspects.
Save/quit/reopen during transit and paid work; profile checkpoint timing and
actual filesystem writes in Editor and desktop player. Exercise a recoverable
write failure and explicit retry. Continue the visual/player-height and desktop
build acceptance listed in the reference integration verification document.
