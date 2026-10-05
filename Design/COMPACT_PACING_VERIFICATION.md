# Compact yard progression pacing — 5 October 2026

This pass reduces the amount of repeated work needed to reach purchased automation. It changes level thresholds, preserving normal recovered-material sale XP, the €8 starting balance, object prices, equipment prices, level gates, and the six finite customer requests. It does not grant money, experience, machines, belts, or historical journal totals.

## Curve and migration

| Level | Former cumulative XP | Paced cumulative XP |
| --- | ---: | ---: |
| 1 | 0 | 0 |
| 2 | 30 | 20 |
| 3 | 80 | 45 |
| 4 | 160 | 80 |
| 5 | 280 | 125 |
| 6 | 440 | 185 |
| 7 | 650 | 265 |
| 8 | 930 | 365 |
| 9 | 1290 | 490 |
| 10 | 1750 | 640 |
| 11 | 2320 | 820 |
| 12 | 3010 | 1040 |

`CompactRules.progressionRulesVersion` deliberately has no nonzero field initializer. A previously serialized balance asset without that field is version zero. Preparing or validating its rules stamps version one, replacing the curve only when the complete array exactly matches the former default. Every valid custom positive curve remains authoritative, including a one-point edit at any former threshold or a different array length. Invalid curves still fail validation; migration is not a repair of invalid tuning.

Version-one rules are never rewritten. To deliberately keep the former default, use that array with `progressionRulesVersion: 1`. A version-zero custom curve that exactly equals the entire former default cannot be distinguished from an unchanged old default; that exact match is the stated migration rule.

This is a balance migration, separate from the yard schema migration. Existing earned XP and money retain their exact values. Those XP may now qualify for an earlier level, reputation percentage, customer request, or equipment gate. No sale, customer completion, bonus, work count, or historical revenue is invented. Persisted partial customer deliveries keep their snapshots and progress.

## Actual model audit

The audit calls the actual `ScrappingModel`, `ConstructionModel`, and `AutomationModel`. Every carried material is collected from a legitimate component/whole-object job or storage batch and then sold or delivered using the normal transaction API. It never assigns money or experience after constructing the fresh default yard. Replacement cars/fridges, machines, generators, and belts are paid through the construction and delivery APIs. Full scrapping, construction, and automation validation succeeds at the end of every route.

The comparison explicitly stamps all supplied curves as version one. This lets the audit retain the former curve for comparison rather than triggering its intended migration.

The reproducible cloud adapter and results are retained outside the checkout:

- `/workspace/tooling/scrapshift-adapters/CompactBalanceAudit.cs`
- `/workspace/tooling/scrapshift-adapters/run-compact-balance-audit.sh`
- `/workspace/tooling/scrapshift-adapters/compact-balance-audit.csv`

Run in this cloud workspace:

```sh
SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono \
  bash /workspace/tooling/scrapshift-adapters/run-compact-balance-audit.sh /workspace/scrapgame
```

The adapter is not a shipped runtime dependency or a file available merely by pulling the game repository. The essential migration, earned-save, and complete fresh transaction routes also live in the tracked `CompactPacingScenarios` and their Unity test wrapper, so they remain reproducible from the repository's regular core/Editor test workflows.

### Route assumptions

The main route manually inspects/dismantles the initially owned car and refrigerator, processes their components at the workbench, and sells material. When a matching customer request is unlocked, it makes partial deliveries before ordinary surplus sales. Thereafter it buys cars for steel requests and refrigerators for plastic requests when affordable, using free wiring as the fallback source. Between requests, the preferred source is wiring, cars, or refrigerators.

It purchases a powered Tier 1 scrapper as soon as the opening affords €105. Before level 10, components use that individually fed machine. At level 10 it pays for a source storage, Tier 2 scrapper, output storage, generator, and two conveyors. The route then loads up to 24 renewable wiring units into source storage, advances the actual machine/belt models in 0.1-second steps until all output reaches storage, withdraws batches, and makes normal material/customer sales. At level 12 it pays for the primary scrapper, export station, and sufficient generator power.

The Stage D comparison ends at **powered ownership**, with standing deliveries and auto dispatch still disabled. It does not assert that a complete new primary-to-export conveyor layout has been purchased or installed. Functional industrial transport, intake, partial resume, and export conservation have their own Stage D integration scenarios.

### Opening and equipment investment

The same initial manually processed car and refrigerator produce 66 sale XP, €85 normal sale revenue, and €14 from two completed requests. Including the €8 starting cash, this leaves **€107**, enough for the €45 generator and €60 Tier 1 scrapper, with €2 left. It involves 81 successful transactions, including 34 workbench strokes and 10 whole-object dismantling stages. The request-aware opening is level 2 under the former curve and level 3 under the paced curve.

Players can also ignore customer requests: the same opening leaves €93 at 66 XP. One additional manually processed renewable wiring unit earns €12 and 8 XP, funding that same €105 powered pair with €0 left. Renewable wiring still works with zero cash; powering the next batch earns money without loans or automatic gifts.

| Purchased milestone | Cost | Cumulative equipment cost |
| --- | ---: | ---: |
| Tier 1 scrapper + generator | €105 | €105 |
| Two storage boxes + Tier 2 + generator + two belts | €393 | €498 |
| Primary + export + one additional generator | €595 | €1093 |

The industrial ownership route reuses the Tier 1 generator's 3 kW spare capacity for the 2 kW exporter; the additional 6 kW generator powers the 6 kW primary. A player choosing a different layout or fully separate power supply may spend more. New industrial conveyors/junctions are additional purchases.

### Preferred wiring route comparison

These are cumulative results, with the same initial objects, real customer deliveries, and paid hardware. “Transactions” counts successful model calls that change gameplay state. It excludes opening UI pages, selecting a machine, aiming, walking, and reading. It is a lower bound on interaction effort, not a measured mouse-click count.

| Milestone | Former curve | Paced curve |
| --- | ---: | ---: |
| Level-10 XP reached | 1750 | 646 |
| Cash before Stage C purchase | €2722 | €887 |
| Transactions before Stage C purchase | 1350 | 522 |
| Renewable wiring units before Stage C | 191 | 53 |
| Paid replacement whole objects before Stage C | 5 | 5 |
| Cash after Stage C purchase | €2329 | €494 |
| Level-12 XP after actual Stage C batches | 3014 | 1046 |
| Transactions through level 12/request completion | 1702 | 642 |
| Total renewable wiring units at that point | 349 | 103 |
| Cash before industrial ownership | €4914 | €1325 |
| Cash after industrial ownership | €4319 | €730 |
| Cumulative simulated machine/transport seconds | 1581.3 | 532.9 |

Both routes complete all six requests, paying exactly €64 customer bonuses in total and granting **zero additional contract XP**. The paced curve reduces transactions to level 10 by about 61%, and transactions through the working factory/request book by about 62%. Equipment remains earned and affordable rather than being granted at start.

For comparison, ordinary wiring sales without requests need 211 total renewable wire units to cross former level 10, and 368 to cross former level 12 after the initial car/fridge opening. That was a substantial repetitive-work requirement before players could use the advanced purchased machinery.

### Other whole-object preferences

These routes still satisfy unlocked steel/plastic requests first, then prefer their named whole-object source. They switch to real storage/Tier 2 batches after level 10 and purchase the same powered industrial ownership.

| Paced route | Cash before Stage C | Total transactions through industrial ownership | Cash after industrial ownership | Simulated seconds before ownership |
| --- | ---: | ---: | ---: | ---: |
| Prefer wiring | €887 | 647 | €730 | 532.9 |
| Prefer cars | €616 | 574 | €426 | 544.8 |
| Prefer refrigerators | €566 | 750 | €409 | 549.9 |

The car preference processes 13 cars and four refrigerators before its factory phase, including the two initially owned objects. The refrigerator preference processes three cars and 21 refrigerators. Their 15 and 22 replacement objects respectively are genuinely paid. Both routes afford all hardware with money remaining.

One default fully dismantled car recovers 42 sale XP and €52 of base material value against a €25 replacement price. It needs six large-object work stages and 23 seconds of powered component processing. One refrigerator recovers 24 sale XP and €30 base value against €15, with four large-object work stages and 13 seconds of powered component processing. A wiring unit recovers 8 sale XP and €11 base value, with four manual strokes or four seconds of Tier 1 work. Sale reputation may increase these cash amounts, but never those recovered-material XP amounts.

Refrigerators remain less efficient for XP per interaction; their plastic and customer requests provide variety. Batching reduces carrying/selling repetition. It is not currently a guarantee of faster raw throughput: belt spacing and each 0.1-second simulated transfer step make the audited factory batches slightly slower in cumulative machine/transport time than individually feeding every wire into Tier 1. Integer-euro reputation rounding also means a larger batched sale can earn slightly more cash than equivalent material sold as many small bundles; the normal quote formula is unchanged.

## Playtime interpretation and next local check

The measured quantities above are model transactions and simulated timers. **They are not measured real playtime, Unity frame rate, or a first-hour guarantee.** Travel, UI input, reading, inspection presentation, save operations, and player decisions are outside the pure model. Machine work can also overlap player travel rather than adding serially to wall time.

The default walking speed is 4 m/s. As an illustrative layout assumption, taking wire from approximately `(20,-5)` to Tier 1 at `(0,5)` and making separate copper/plastic sale trips to the office near `(-17,-13.5)` gives roughly 136 metres, or 34 seconds of walking per sequential cycle, before its four-second work time and interactions. Moving machines closer to sales or collecting storage batches changes that substantially. This is an arithmetic illustration, not an observed route duration.

A reasonable **local playtest target** is first powered processing around 8–15 minutes, first storage/Tier 2 route around 25–40 minutes, and first industrial ownership around 45–70 minutes. Verify those targets with a fresh save, ordinary walking/reading, and player-chosen equipment placement. If the compact yard still feels repetitive, shorten carrying trips and improve batch interaction before adding money or XP gifts. Check that players understand the customer's partial delivery and the distinction between individually fed Tier 1, conveyor-fed Tier 2, and opt-in standing industrial deliveries.

## Verification evidence and limits

- **9/9 pure pacing scenarios passed** against the actual current Core and all scenario sources compiled with Mono warnings treated as errors.
- The scenarios test exact former default migration/idempotence; a custom edit at every positive threshold; explicit version-one former curve retention; malformed curve/version rejection; an actually earned older schema-3 save; fresh manual→powered→working factory→powered industrial ownership; car/fridge preferences; and ordinary sales/renewable recovery without customer rewards.
- The complete routes check the cash identity `€8 + normal sales + actual request bonuses − paid scrap − purchased hardware`, and equality between XP and the sum of actual normal sale quotes. Timed production/belt movement is checked to grant no XP.
- The external audit reran against the current rules implementation with the comparison curves explicitly versioned. All routes end with scrapping, construction, and automation validation.
- The native Editor wrapper and real `CompactBalance.PreparedRules` compile against a narrow API adapter. Two Unity tests recreate a Balance JSON document with the new version field absent and retain a version-one explicit former curve. **Those Unity JSON/ScriptableObject tests were not executed in the cloud.** The adapter deliberately does not implement JSON serialization.
- Local Unity compilation, native Balance import/serialization, UI unlock hints, live saved-player progression, actual travel time, rendering, performance, and a playable desktop build still need local verification.
