Continue developing SCRAPSHIFT into a complete scrapyard automation game.

Read AGENTS.md, PROJECT_HANDOFF.md, and README.md, then inspect the current implementation. Use multiple agents, preserve existing assets and working systems, and implement the changes directly.

These new instructions supersede earlier restrictions that limited development to the original wire-processing prototype.

GAME VISION

Scrapshift is a single-player, first-person 3D scrapyard simulator with worn retro graphics.

The player starts as an inexperienced scrapper with a small yard, basic tools, some scrap, and very little money.

They dismantle old cars, refrigerators, and other discarded equipment into components. Components are processed further into valuable materials, which are sold to earn money and experience.

Over time, the player buys generators, powered machines, conveyors, storage, and equipment that can automate the entire operation.

Tagline:
“Start with your bare hands. Build a scrapyard that works for you.”

NEW WORLD: COMPACT AND PLAYER-DESIGNED

Replace the old prototype yard as the active gameplay world. Do not keep its fixed station arrangement as the design foundation.

Create a new compact scrapyard. Approximately 48 × 36 metres is the proposed starting footprint; tune it if needed to keep walking distances short and construction enjoyable.

Use Design/compact-yard-concept.png or the attached map as a visual reference.

IMPORTANT: The map shows an example player-built layout, not mandatory equipment positions.

Only essential infrastructure is fixed:
- Entrance and fence.
- Small office/shop and selling interface.
- Scrap delivery area.
- Basic landscape and boundary decoration.

Players must be able to place and rearrange:
- Workbenches.
- Generators.
- Scrapping machines.
- Storage containers.
- Conveyors.
- Splitters, mergers, and other purchased production equipment.

Do not restrict equipment to predetermined machine pads.

Keep most of the interior buildable and open. Avoid a huge industrial map, excessive empty space, long travel distances, or decorative clutter that prevents construction.

Start with a modest manual setup. Advanced machinery shown on the map is future equipment, not starting inventory.

Preserve the old scene as a legacy/reference scene if useful, but make the new yard the default. Preserve code and reusable assets. Do not silently delete user saves or hand-edited content.

CURRENT PROJECT

Inspect the repository rather than assuming it is still the earliest prototype.

Existing work includes the wire-processing economy, powered stripper, carrying, pause, persistence, Settings and rebinding, and later additions documented in the current handoff such as repairs, orders, authored props, textures, and audio.

Reuse working systems where appropriate. Do not scaffold a second unrelated game or regress implemented settings.

Existing Cloud checks do not prove Unity rendering, gameplay, or serialization. Report verification accurately.

MANUAL SCRAPPING AND COMPONENT CHAINS

Replace wire-only acquisition as the main progression with hierarchical scrapping.

Initial examples:
- Old car → engine/motor, wiring, body metal, and other reusable parts.
- Refrigerator → compressor/motor, wiring, metal casing, and plastic.
- Motor → copper winding, steel, and other appropriate recoverable materials.
- Wiring → copper and insulation.

Define recipes and yields in editable data.

The player should:
1. Acquire or receive a scrap object.
2. Inspect it.
3. Perform understandable dismantling interactions.
4. Remove components.
5. Carry portable components to a workbench.
6. Dismantle those components further.
7. Collect and sell recovered materials.

Large cars and refrigerators stay on accessible dismantling positions or are moved with appropriate equipment. Do not make the player carry a whole car like a small bundle.

Use representative low-poly components and readable work stages. Detailed mechanical simulation of every screw is unnecessary.

Keep the existing wire-stripping activity as part of the larger processing chain.

STARTING ECONOMY

Give the player enough starting scrap to progress without debt or a softlock.

Early earnings should feel small. Provide affordable or renewable opportunities so mistakes do not permanently stop progress.

Create a clear purchase interface for equipment and scrap supply.

Show material quantities, unit prices, total sale value, and purchase costs.

All prices, yields, processing durations, starting supplies, and upgrade costs must be editable.

LEVELS AND SALES EXPERIENCE

Selling recovered materials grants experience.

Higher levels gradually improve the sale prices the player receives, representing skill and business reputation.

Implement:
- Experience and level.
- An editable level curve.
- A visible progress indicator.
- Sale-price bonuses by level.
- Unlock notifications.
- Clearly displayed locked equipment and requirements.

Keep bonuses moderate and configurable. Explain how much of a sale price comes from the player's level.

Prevent trivial experience exploits:
- No rewards for repeated collection of the same output.
- No experience from moving items between containers.
- Prevent profitable buy-and-immediately-resell loops that manufacture levels.
- Award experience only through valid completed sales.

Treat exact balance numbers as provisional tuning, not fixed commitments.

POWER AND GENERATORS

Before powered automation, the player buys their first generator.

Generators produce a defined amount of power. Machines require defined amounts to operate.

Implement:
- Generator output.
- Machine consumption.
- Player-built power connections.
- A readable connected-network supply/demand display.
- Clear powered/unpowered status.
- A deterministic, understandable insufficient-power policy.

Recommended initial behavior: overloaded networks pause processing and explain the shortage. Preserve inputs and processing progress.

Show generator output and equipment demand before purchase.

Keep initial power mechanics simple. If fuel is introduced, provide a later automated supply option so fuel does not permanently prevent full automation.

Existing powered equipment must integrate consistently with the new power system.

TIER 1 SCRAPPER

The first purchasable scrapping machine is Tier 1.

It:
- Requires generator power.
- Accepts supported scrap/components manually.
- Processes them into defined outputs.
- Has a visible input area and output collection area.
- Shows recipe, progress, power status, and output capacity.
- Pauses safely when unpowered or output is blocked.

Tier 1 is an intermediate step: it automates processing, while the player still transports inputs and outputs.

LEVEL 10 UNLOCKS

Use level 10 as the initial configurable unlock for:
- Conveyor construction.
- Storage containers with conveyor ports.
- Tier 2 scrapper.

Unlocking equipment allows its purchase; it does not automatically grant free equipment.

The Tier 2 scrapper has:
- Conveyor input and output ports.
- Automatic intake of supported items.
- Configurable recipes where needed.
- Greater capability or throughput than Tier 1.
- Appropriate power consumption.
- Safe blocked-output and insufficient-power behavior.

Keep unlock levels and equipment statistics editable.

FREE PLACEMENT AND CONSTRUCTION

Implement a usable build mode with:
- Build catalogue.
- Ghost placement preview.
- Rotation.
- Optional grid snapping.
- Port snapping.
- Valid/invalid placement feedback.
- Collision and yard-boundary validation.
- Purchase cost display.
- Cancellation without spending money.
- Moving and dismantling existing equipment.

Protect access to the entrance and essential infrastructure.

Handle contents and connections safely when moving or dismantling equipment. Explain any restriction rather than deleting items.

Construction controls must appear in Settings and use the existing rebinding infrastructure.

Save placements, rotations, equipment ownership, connections, and stored contents.

CONVEYORS

Create satisfying player-built conveyors inspired by Satisfactory’s connection and placement experience, using original visuals and implementation.

Do not copy its assets, UI, or exact designs.

Support:
- Choosing start and end connection points.
- Snapping to machine and storage ports.
- Straight sections and practical corners.
- Visible supports.
- Clear travel direction.
- Moving item representations.
- Splitters and mergers.
- Throughput and capacity limits.
- Blocked-line behavior.

Ground-level belts are sufficient initially. Slopes or raised sections can follow after the basic system works.

Simulation must preserve item counts:
- No duplication.
- No lost items at junctions.
- No repeated output when loading saves.
- No item destruction when downstream storage fills.

Use efficient item records and pooled visual representations. Do not make every conveyor item an expensive physics simulation.

Do not let belts teleport items through unsupported connections.

STORAGE AND SALES

Containers hold materials/components with a defined capacity.

Players can manually deposit and withdraw items. Conveyor ports enable automatic intake/output and useful filtering.

Provide clear contents and capacity displays.

Later automate dispatch/sales through a purchased export station or configured collection contract. Keep existing orders integrated where appropriate.

FULL AUTOMATION IS THE LONG-TERM TARGET

Design and implement a path that can eventually run:

Scheduled scrap delivery
→ automatic unloading/intake
→ primary dismantling into components
→ component processing
→ material separation
→ conveyors and sorting
→ storage
→ automatic dispatch/sales.

A wire stripper alone is not full automation.

Cars and appliances need suitable primary dismantling equipment; do not send whole cars down small component belts.

Later automation should also handle any recurring required supply, including generator fuel if used.

Players should still be able to do tasks manually and make meaningful choices about repairs, recipes, layout, purchases, and orders.

MULTIPLE AGENTS

Use multiple agents with distinct responsibilities:

1. World/assets/textures:
Build the compact new environment and original retro props/materials.

2. Gameplay/economy/progression:
Implement component recipes, dismantling, sales experience, levels, and unlocks.

3. Construction/power/automation:
Implement placement, generators, power networks, conveyors, machine ports, and storage.

The main agent owns architecture, integration, shared contracts, persistence, verification, and documentation.

Agree on shared data interfaces and asset conventions first. Give agents separate files/folders and avoid conflicting ownership of central gameplay files.

Use a staged integration plan. If multiple agents are unavailable, report that and proceed sequentially.

VISUALS AND SETTINGS

Preserve the existing worn retro style, recognizable authored props, original textures, and audio.

Avoid returning to generic coloured blocks.

Keep:
- Rebindable keyboard/mouse actions.
- Persistent settings.
- Dynamic control prompts.
- Mouse sensitivity and invert-Y.
- Existing video/audio options where implemented.
- Correct pause and menu input handling.

Make machinery and component states recognizable. Use readable prompts, short work animations, and restrained feedback.

IMPLEMENTATION ORDER

Implement coherent playable stages:

A. New compact yard, buildable space, manual car/fridge/component scrapping, sales and XP.
B. Free placement, generator power, and manually fed Tier 1 processing.
C. Level-10 unlocks, connected Tier 2 scrapper, conveyors and storage.
D. Automated intake, sorting and export that demonstrate the complete production chain.

Work through these stages rather than delivering disconnected skeleton systems. Keep each stage playable.

Do not rewrite unrelated working systems. If the full scope exceeds one task, finish a coherent stage, state exactly what remains, and update the handoff.

SAVE COMPATIBILITY AND VERIFICATION

Version save data and migrate compatible previous saves. Provide a clear, recoverable transition to the new yard; do not silently reset progress.

Verify:
- Multi-stage dismantling and material conservation.
- Valid sales and XP.
- Level bonuses and unlocks.
- Affordable progression without softlocks.
- Placement, rotation, collision checks and refunds.
- Generator connections and insufficient power.
- Machine input/output capacity.
- Conveyor routing, splitting, merging and backpressure.
- Storage contents.
- Pause.
- Save/load across layouts and active production.
- Existing settings and rebinding.

Run meaningful available tests. If Unity is unavailable, distinguish pure C# checks from unverified Unity behavior and provide a local playtest checklist.

DELIVERABLES

Deliver the implemented game changes, original assets, reproducible scene setup/update tools, and updated documentation.

Update AGENTS.md and PROJECT_HANDOFF.md so the new compact, freely buildable automation direction replaces outdated wire-demo-only restrictions.

Document:
- What actually works.
- Agent contributions.
- Tests executed.
- Known issues.
- Save migration behavior.
- How to open the new yard.
- Remaining automation stages.

Preserve Campaign/ and accurate concept-art labels. Do not modify or publish Kickstarter.

Begin implementation now.