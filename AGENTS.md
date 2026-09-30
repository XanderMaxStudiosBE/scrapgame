# Scrapshift agent instructions

Read `PROJECT_HANDOFF.md` before starting work. It records the design and verified implementation state. Reinspect the checkout because newer sessions may have added code.

## Product direction

- Scrapshift is a first-person retro 3D scrapyard simulator. Manual labor gradually gives way to powered machines and automation.
- Core loop: acquire scrap, inspect it, repair or dismantle it, sell items/materials, and upgrade the yard.
- Prioritize one complete wire-processing loop: collect wire, strip it manually, sell copper, buy a powered wire stripper, feed it, collect output, and sell again.
- The earlier video-store concept was rejected in favor of the scrapyard. Do not introduce a video-rental business.
- Use chunky low-poly assets, coarse textures, rusty industrial surfaces, fog, and warm workshop lighting. Keep controls and UI readable.

## Technical direction

- Unity and C# are the chosen stack. Preserve an existing project's compatible version/pipeline; do not silently migrate engines or upgrade Unity.
- At the initial handoff, no Unity project exists. For initial creation, Unity 6 LTS with URP is the intended baseline. Local Unity 6000.3.25f1 is available; use that as the initial compatibility target unless repository evidence or the user specifies another version. It has not yet been tested with this game.
- Desktop PC, keyboard/mouse, and single-player are the initial development scope. These are prototype targets, not promises of shipped platform support.
- Use relative project paths. Never rely on the author's Windows drive paths in runtime code or Cloud instructions.
- Preserve `Campaign/` as source artwork and documentation. Concept art is a mood reference, not gameplay evidence.
- Prefer small systems with editable item/recipe/machine data over a large framework. No required paid assets or external accounts for the prototype.
- Supply a working scene or a reproducible editor setup tool with assigned references and documented steps.
- Preserve Unity `.meta` files/GUIDs once assets exist. Ignore generated Unity directories such as Library, Temp, Logs, obj, and build output.
- Use physical objects for useful player interactions; use quantities/simplified simulation where appropriate. Avoid permanently active physics for every scrap fragment or unnecessary per-frame updates for every machine.

## Implementation and verification

- Implement and verify the smallest complete gameplay loop before adding conveyors, vehicles, large maps, or elaborate repairs.
- Prevent duplicate processing outputs, negative inventory/money, duplicate upgrades, and loss/duplication across saves. Provide a repeatable source of scrap.
- Save money, owned equipment, relevant inventory/world state, and processing state consistently.
- Run checks appropriate to the change. If Unity is unavailable, state exactly what remains unverified in the editor; do not claim a playable build merely because C# files exist.
- Update `PROJECT_HANDOFF.md` after meaningful work with implemented features, checks performed, bugs, and the next concrete task. Keep planned and implemented behavior clearly separated.
- Preserve existing work. Do not create another repository or replace this checkout without a user request.

## Campaign boundaries

- Kickstarter is the selected crowdfunding platform. The creator confirmed Antwerp, Belgium and euro funding for production.
- Funding target, budget, rewards, dates, and release-platform commitments are undecided. Do not invent them.
- Do not publish the campaign, submit it for review, accept agreements, or enter identity/payment details as part of a game-development task.
- Keep concept-art labels and accurate AI disclosures. Generated marketing artwork must not be described as captured gameplay.
- The working title has not been checked/cleared for publication.
