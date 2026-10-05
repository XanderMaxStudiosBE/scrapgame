# Stage D working integration contract — 2026-10-05

Implementation in progress; this is an ownership/interface agreement, not verification or a release claim. Existing Unity6000.3.25f1/URP17.3.0, manual loop, Stage C transport, settings and original art remain.

## Shared design

- Append `EquipmentKind.PrimaryScrapper=9`, retain `ExportStation=8` and all other enum identities. Primary takes intact large scrap; whole vehicles/appliances never enter portable-item belts. Output components use existing recipes and recovered lineage. Export accepts saleable materials only and uses the ordinary guarded material-sale/XP calculation.
- Primary default: €350, level12,6×7m,6kW,24s processing,48 output units. Export default: €200,level12,3×2.5m,2kW,5s dispatch interval,48 units. All statistics editable. Primary1 component output(+Z); no portable input. Export1 material input(-Z); no output.
- Explicitly enabled standing delivery purchases one selected car/fridge per interval from existing large-recipe prices. Default interval60s, no fees/debt/offline catch-up, purchases stop on disabled/power/money/capacity/ID problems. A machine holds at most one whole object. Allow manual feeding of an intact uninspected owned receiving-area object without another charge. No partial dismantling transfer, no carrying a whole car. Timed delivery is a contracted machinery service; it does not promise a drivable delivery vehicle or a simulated road route.
- Snapshot yields/duration/identity/lineage at intake. Reserve positive output slots before consuming/buying. Complete into the primary buffer once; allocate output IDs only after every completion check passes. Blocked/unpowered work preserves the whole object and progress. Finished component buffers must drain sufficiently before another object can begin.
- Automatic dispatch requires explicit enable and power. Wrong inputs are blocked; recovered/imported eligibility is retained. Dispatch consumes buffer material atomically through the normal sale calculation, credits career totals without free customer bonuses and never awards XP for transport. Quotes/cash/XP overflow retain stock. No whole-object buy/resell route manufactures XP without actual processing.
- New schema4 uses the existing `yard-v2.json` filename. Explicit validated2→3→4/3→4 migration, no gifts, identity changes, historical simulation or source-file overwrite. Schema2/3 reject industry-only fields/equipment; backups retain previous bytes. Busy industry equipment cannot move/dismantle. Current Manual/Stage C saves and preferences remain readable.

## Core contract

Gameplay agent owns NEW `Core/CompactIndustry.cs` and changes `CompactScrappingModel.cs` plus new industry scenarios/native wrappers. Main owns `CompactYardState.cs`, `CompactScrappingRules.cs`, migration/header/save integration and runner/docs. Automation agent owns `CompactAutomation.cs`, `CompactConstruction.cs` and new Stage D integration scenarios/wrappers. Agree before crossing ownership.

New public serializable types in CompactIndustry.cs:

```
PrimaryScrapJob: int id; ScrapObjectKind kind; float duration,remaining;
                bool xpEligible; PartAmount[] yields;
IndustrialMachineState: int version=1; bool enabled;
                       ScrapObjectKind purchaseKind; float remaining;
                       PrimaryScrapJob primary;
                       int objectsProcessed,exportedUnits,exportedRevenue,exportedXp;
```

`EquipmentState.industry` is nullable. `remaining` is the standing-delivery or export timer; `primary.remaining` is authoritative dismantling progress. Existing `EquipmentState.contents` is the component/material buffer. New rules: `industryRulesVersion=1`, `deliveryIntervalSeconds=60`. Missing older defaults are filled without replacing tuned positive values/custom catalogue entries; only exact old unavailable Export catalogue marker is upgraded.

`ScrappingModel.Industry` exposes `CompactIndustryModel`, constructed after Career. It offers `SetEnabled(int,bool)`, `SetPurchaseKind(int,ScrapObjectKind)`, `CanFeedScrap(int,int,out string)`, `FeedScrap(int,int)`, `DispatchQuote(int)`, `DispatchNow(int)`, `Status(int)`, `Tick(float)`, `LastMessage`; quote shape reuses CompactSaleQuote. Config/quotes must not manipulate carriedId. Status is read-only. Tick uses unpaused delta and model.HasPower; no wallclock or Unity refs. Main explicitly calls Tick alongside Model/Automation.

Core model must count reserved primary yields in OccupiedSlots, recognize primary/export buffers, validate industry state/identity/busy shape/schema, centralize carried/buffer sale calculation and provide atomic buffer consumption for dispatch. Existing constructors/controls/manuals remain stable. Industry validation entry point is owned by gameplay helper and called by ScrappingModel.Validate.

Automation agent adds primary/export port/support rules, reserved output capacity, safe construction/move restrictions and tests the actual purchased/scheduled primary→storage→Tier2→sorting→export chain, including backpressure, power shortage, conservation, lineage, partitioned ticks and saves. No duplicating independent transport/sale implementations.

## Runtime/art contract

Main owns coordinator/input/pages/state/saves/guidance/view integration and existing shared menus. UI agent owns NEW `Runtime/CompactYardGame.IndustryMenus.cs` and native tests only; shared integration requirements are sent to main. World agent owns CompactYardClutter and separate original salvage art/audits. Machine art agent owns NEW CompactIndustryVisuals and separate original primary/export art/audits/native tests.

Machine art `CompactIndustryVisuals.BuildEquipment(kind,parent,position,yaw,colliders,rules)` uses original atlas, shared existing port geometry and actual definitions. `PrimaryPowerSocket(rules)` supplies main's cable endpoint. Any job/drive presentation helper is cached, coordinator-timed and read-only. No per-object Update, new lights, Rigidbody or shared mesh/material mutation.

Primary source≤4500tri, export≤2500tri, one shared atlas material; target height≤4.5/2.4m and geometry inside conservative footprints. World replaces repetitive stock in existing roots with identifiable salvage silhouettes,≤+25renderers/+12k placed triangles; retains29 root/envelope/occupancy/collision behavior and mostly open build space. New pack filenames/GUIDs independent; old PNG/FBX/WAV/materials remain unchanged.

Native Unity compilation/import/serialization/rendering/collision/audio/FPS/build/playthrough must be distinguished from source/adapter/Blender checks. Update this contract when actual decisions change and record final verification separately.
