# Compact industry core verification — 2026-10-05

Stage D core behavior is implemented in `CompactIndustry.cs` and the shared `ScrappingModel`. This is source/transaction evidence, not a Unity gameplay, rendering or build claim. Read the working Stage D contract and the final integrated verification for migration, construction, conveyors, menus, art and saves.

## Implemented behavior

- Schema 4 appends Primary Scrapper and enables material Export without changing existing item/equipment enum identities. `Industry` is constructed after `Career`. Prior schema 2/3 state validates only without industrial equipment/fields; actual migration remains explicit and owned by the integration layer.
- Manual feed transfers one intact, uninspected owned car/fridge into a primary, retaining its global ID and charging nothing. Inspected/partial/exhausted objects stay in their manual chain. A whole object never becomes a portable stack or conveyor item.
- An explicitly enabled standing service reserves every output before buying one car/fridge at the existing large-recipe price. It holds at most one object. Money, output-unit/global-slot capacity and future output-ID headroom are checked before any charge. Purchases incur no debt, delivery fee or clock-based historical rewards.
- Duration/yields/kind/whole-object identity/eligibility are snapshotted at intake. Later recipe edits do not rewrite paid work. Completion creates all unique output stacks in the existing primary buffer once, replacing equal reserved slots; no sale XP is awarded. Blocking capacity or IDs, and missing power, preserve the paid object and processing progress.
- Primary enable controls **standing purchases**. Disabling them preserves the purchase timer, and an existing paid/manual object still finishes under sufficient power. Changing purchase kind affects the next delivery only. The primary output buffer is withdrawable, but portable deposit into it is rejected.
- Export admission requires a saleable material and matching filter; components/appliances stay upstream or carried. Persisted material/filter validation uses material type, so later tuning a price to zero retains stock and blocks dispatch safely until a valid price returns.
- Dispatch quotes select the first matching material group, summing only the same kind **and** recovery eligibility. Imported/recovered lineages never mix for XP. Explicit `DispatchNow` requires power but does not enable automatic dispatch. The automatic service requires explicit enable and uses its full configured dispatch interval.
- Carried sales, contract deliveries and exports share the guarded sale calculation. Export consumes the quoted buffer group atomically, updates cash/normal eligible-material XP/career sale totals, and records exporter totals. It does not borrow/change `carriedId`, advance customer requests or pay customer bonuses. Display counters saturate at `int.MaxValue`; cash/XP overflow rejects with stock intact.
- Configuration, preview quotes and status do not allocate items or consume stock. Invalid live primary yields/duration/purchase price/service intervals block before creating paid work or crediting a sale.

## Time and ordering

No wall clock, background callbacks or offline catch-up is consulted. Main passes unpaused gameplay delta to Model → Industry → Automation.

The primary purchase timer accumulates **eligible idle time**: connected power, enough cash, reserved output capacity and IDs, and no existing whole-object job. Busy, disabled or blocked time freezes it. With default tuning, 60 eligible idle seconds buy a car, then 24 processing seconds complete it; the next eligible idle interval begins after that. Export clocks similarly freeze while disabled, unpowered, empty, filtered-out or overflow-blocked.

Industrial events resolve chronologically across active machines and simultaneous events use durable equipment-ID order. A cached sorted machine list is rebuilt only when equipment identity/order changes. In-session countdowns retain double precision and serialize the agreed float clocks; loading starts from those persisted floats and introduces no elapsed offline time. Partition tests compare exact quantities/IDs/cash/XP/counters and timers within 0.0001 seconds, not an impossible promise of bit-identical float serialization.

To bound catch-up cost, every tick first conservatively estimates transitions. Inputs requiring more than 8,192 transitions throw `ArgumentOutOfRangeException` **before state writes**, directing the caller to split the elapsed gameplay delta. No accepted elapsed seconds or paid progress are silently discarded. Normal frame deltas/default tuning remain well within this limit. Invalid/nonfinite/negative elapsed input also rejects before changes; zero delta performs no events. The integration layer should surface this diagnostic for extreme custom tuning or oversized calls instead of retrying the same unsupported delta.

## Executed checks

All actual Core sources and scenario sources compile under Mono with warnings as errors. The 23 new pure scenarios pass:

- Opt-in/default behavior and read-only previews/configuration without changing carried identity.
- Owned intact transfer, exactly-once primary output, rejected partial scrapping and reserved slot/unit accounting.
- Default cadence, eligible funding/power/capacity freeze, paid-work completion after disable and next-kind selection.
- Saved partial processing and completed-buffer resume, recipe tuning preservation, capacity reduction and output-ID exhaustion.
- Grouped export/carry identity, imported zero-XP lineage, quote/cancellation, cash/XP overflow, automatic cadence and enable/disable behavior.
- 252-second tick versus 2,520 × 0.1-second ticks for two primaries plus export; three exact primary cycles and matching durable output IDs/balances/counters.
- Reordered equipment collections with scarce funds produce the same oldest-ID purchase winner.
- Oversized/invalid ticks reject before changes, schema 2/3 protection, duplicate IDs, illegal industry-on-normal-equipment, corrupt timers/versions/yields/counters/jobs and wrong export contents.
- Zero-priced material stock/filter preservation, debt-free/load/pause behavior, real zero-cash manual recovery funding a standing purchase, invalid live tuning and exact career/export ledgers.

Two native tests are supplied for actual Unity JSON partial-primary/completed-output and export timer/eligibility/carried-ID round trips. Their source compiles against a small Unity/NUnit API-only adapter; they have **not** executed in Unity. The original packaging/metadata audit passes; this core change adds no vendor assets.

## Required local verification

Run Unity EditMode tests, including the native JSON cases and integration suites. Then play manual feed, scheduled purchases, power shortages, backpressure, export quotes/manual/automatic dispatch, carried inventory, pauses/focus changes, partial/completed save reloads, legacy migration/recovery, New Yard and the full primary-to-export chain. Confirm actual UI messaging, imported scripts, assets, physics, audio, FPS and a desktop player build. Those engine results are not established by the pure or adapter checks above.
