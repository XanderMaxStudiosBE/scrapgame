# Scrapshift agent instructions

Read `PROJECT_HANDOFF.md` before starting work. It records the design and verified implementation state. Reinspect the checkout because newer sessions may have added code.

## Product direction

- Scrapshift is a first-person retro 3D scrapyard simulator. Manual labor gradually gives way to powered machines and automation.
- Core loop: acquire scrap, inspect it, repair or dismantle it, sell items/materials, and upgrade the yard.
- Active direction: a compact, freely buildable scrapyard automation game. Read Design/COMPACT_AUTOMATION_BRIEF.md and the current handoff. Wire stripping remains one component chain, not the scope limit. Implement playable stages: manual car/fridge/component scrapping and sales XP; construction/generator/Tier1; level-10 belts/storage/Tier2; automated intake/sorting/export. Equipment placement is player-designed; only entrance, office/sales, delivery and boundaries are fixed.
- The approximate starting yard is 48 × 36m. Keep interiors buildable, walking distances short and advanced equipment purchased rather than granted at start. The creator asks for multiple agents with separated world, gameplay and construction ownership; main owns shared contracts, integration, persistence and verification.
- The earlier video-store concept was rejected in favor of the scrapyard. Do not introduce a video-rental business.
- Visual reference chosen by the user: Retro Rewind - Video Store Simulator. Use believable, recognizable props, worn surfaces, warm practical lighting and a compact readable HUD in an original scrapyard setting; do not copy that game's assets, characters or video-store business. The user dislikes the current primitive-box look and requests worn retro realism.
- Aim for a cozy retro simulator: chunky low-poly assets, coarse restrained textures, worn industrial surfaces, soft fog and warm workshop lighting. Favor welcoming colors, satisfying tool/machine sounds and relaxed pacing; keep controls and UI crisp and readable.

## Technical direction

- Unity and C# are the chosen stack. Preserve an existing project's compatible version/pipeline; do not silently migrate engines or upgrade Unity.
- The existing project targets Unity 6000.3.25f1 and pinned URP 17.3.0. The creator uses Unity locally; cloud source/adapter checks do not verify engine compilation, rendering or a build.
- Desktop PC, keyboard/mouse, and single-player are the initial development scope. These are prototype targets, not promises of shipped platform support.
- Use relative project paths. Never rely on the author's Windows drive paths in runtime code or Cloud instructions.
- Preserve `Campaign/` as source artwork and documentation. Concept art is a mood reference, not gameplay evidence.
- Prefer small systems with editable item/recipe/machine data over a large framework. No required paid assets or external accounts for the prototype.
- Supply a working scene or a reproducible editor setup tool with assigned references and documented steps.
- Preserve Unity `.meta` files/GUIDs once assets exist. Ignore generated Unity directories such as Library, Temp, Logs, obj, and build output.
- Use physical objects for useful player interactions; use quantities/simplified simulation where appropriate. Avoid permanently active physics for every scrap fragment or unnecessary per-frame updates for every machine.

## Implementation and verification

- Complete coherent stages of the automation brief. Preserve prior systems/assets and the old scene as legacy/reference; make the compact yard the default without deleting old saves or hand-edited scenes. Version new data and make migration explicit/recoverable. Never label planned automation as implemented.
- Prevent duplicate processing outputs, negative inventory/money, duplicate upgrades, and loss/duplication across saves. Provide a repeatable source of scrap.
- Save money, owned equipment, relevant inventory/world state, and processing state consistently.
- Compact Stage C includes conveyors/storage/Tier2/junctions; read Design/COMPACT_STAGE_C_VERIFICATION.md. Compact yard-v2.json now uses schema 4 with validated in-memory migration from schema 2/3; preserve original backups, global IDs, paid snapshot outputs and recovery eligibility. Stage D intake/primary/export is implemented in source; read Design/COMPACT_STAGE_D_VERIFICATION.md. Recurring purchases/exports require explicit enable and start off. Preserve custom progression curves; exact version-zero older defaults migrate once to the paced curve.
- Run checks appropriate to the change. If Unity is unavailable, state exactly what remains unverified in the editor; do not claim a playable build merely because C# files exist.
- Update `PROJECT_HANDOFF.md` after meaningful work with implemented features, checks performed, bugs, and the next concrete task. Keep planned and implemented behavior clearly separated.
- Preserve existing work. Do not create another repository or replace this checkout without a user request.

## Campaign boundaries

- Kickstarter is the selected crowdfunding platform. The creator confirmed Antwerp, Belgium and euro funding for production.
- Funding target, budget, rewards, dates, and release-platform commitments are undecided. Do not invent them.
- Do not publish the campaign, submit it for review, accept agreements, or enter identity/payment details as part of a game-development task.
- Keep concept-art labels and accurate AI disclosures. Generated marketing artwork must not be described as captured gameplay.
- The working title has not been checked/cleared for publication.
