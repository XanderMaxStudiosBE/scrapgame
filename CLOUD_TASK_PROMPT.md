You are continuing SCRAPSHIFT in this repository. Read AGENTS.md and PROJECT_HANDOFF.md first, then inspect the checkout and implement the first playable prototype. Work directly in the repository; do not stop at a plan.

WHAT THE GAME IS
Scrapshift is an indie first-person 3D scrapyard simulator with retro PS1/late-1990s PC-inspired graphics. The player grows a small shabby yard into an automated recycling operation.
Tagline: “Start with your bare hands. Build a scrapyard that works for you.”
Core loop: acquire scrap → inspect → repair or dismantle → recover materials → sell → upgrade.
The important choice is repairing for a higher price versus dismantling for useful parts versus processing quickly into materials. The earlier video-store idea was dropped; keep the scrapyard focus.

PROGRESSION
Start by carrying objects, unscrewing parts and sorting materials manually. Upgrade hand tools, a trolley and workbench. Then buy powered wire strippers, cutters and crushers that the player feeds individually. Later introduce conveyors, magnetic separation, automatic sorting and connected processing lines. Longer-term possibilities include power, storage, larger orders and automatic loading.
Machines should automate tasks the player has already learned. Keep valuable repairs and management decisions meaningful.

ACTUAL IMPLEMENTATION STATE
At the 2026-09-30 audit, the repository contained campaign artwork and documentation only. There was no Unity project, C# gameplay code, scene, prefab, runtime asset collection, playable build, tests or CI. No gameplay feature has been implemented or tested. Recheck for newer work before scaffolding anything.

Completed assets:
- Transparent logo, campaign cover, square Coming-to-Kickstarter poster, and salvage/upgrade/automate concept illustration.
- Seven campaign section headers in PNG and editable SVG.
- Preview gallery, palette/usage notes, asset manifest, original generation prompts and campaign-story draft.
All live under Campaign/Scrapshift/. Campaign/Scrapshift-Kickstarter-Kit.zip is an earlier export; use source files as authoritative.
Important documents: AGENTS.md, PROJECT_HANDOFF.md, Campaign/Scrapshift/README.md, Campaign/Scrapshift/campaign-story-draft.md, Campaign/Scrapshift/preview.html and Campaign/Scrapshift/asset-manifest.json.
There are no completed runtime 3D models, animations, audio or trailer.

MOST RECENT WORK
We were preparing a private Kickstarter campaign and then moving the project to Cloud. The creator confirmed Antwerp, Belgium, euro funding and funding game production. The campaign title is “Scrapshift — A Retro Scrapyard Simulator”; category Games / Video Games. Title, pitch, uploaded cover, location, story and risks were saved through browser work. AI involvement and generated content were selected. Final detailed AI-use/source explanations were entered but were not subsequently confirmed saved.
Budget, goal, rewards, delivery dates and release-platform commitments remain undecided. Do not publish or submit the campaign, enter identity/payment details, or invent commitments as part of this development task. Live website state is not in the repository.

TECHNICAL DECISIONS
Use Unity and C#. For an existing project, preserve its compatible editor version and pipeline. Otherwise scaffold Unity 6 LTS with URP; local Unity 6000.3.25f1 was found and is the intended initial compatibility target. No game/editor validation has occurred. Cloud may not have Unity.
Initial development scope: desktop PC, keyboard/mouse, single-player. These are prototype targets, not release promises.
Use relative paths. The current local checkout is D:\Ai_Cod\scrapgame; the old D:\Ai Cod\New-game path is obsolete. Neither path should be hardcoded into the project.
Preserve Campaign/. Keep Unity metadata/GUIDs, add appropriate generated-folder ignores, and avoid paid dependencies or required external services.
Separate player interaction, item/recipe data, carryable objects, processing, economy, progression, persistence and UI. Make prices/yields/times editable.
Use active physics where interaction benefits; use quantities or simplified simulation for bulk/distant material. Avoid unnecessary per-frame machine logic.

VISUAL DIRECTION
Chunky low-poly models, coarse textures, rusty surfaces, muted colors, fog and warm workshop lighting. Homemade machinery with exposed motors and mismatched panels.
Palette: ivory #E8DDC4, rust #B75935, olive #69735B, charcoal #202927.
Keep rendering stable and UI readable. Existing generated marketing artwork is concept art and mood reference; do not mistake it for gameplay evidence or require its illustration detail in runtime graphics.

NEXT IMPLEMENTATION PRIORITY
Build one complete loop:
1. Walk around one compact fenced scrapyard with a covered workbench, scrap source, bins, selling point and machine upgrade area.
2. Pick up scrap wire, carry/drop it, and process it manually at the workbench with visible progress and copper output.
3. Sell copper, earn money and buy a powered wire stripper.
4. Feed wire into the machine, show processing, collect copper and sell again.
5. Provide a repeatable scrap source and tune a short introductory path to the machine.
6. Add clear interaction prompts, money/objectives UI, pause and save/load.

Prevent duplicate outputs, invalid processing, negative balances and repeated purchases. Save money, equipment ownership, relevant inventory/world state and processing consistently.
Suggested controls: WASD move, mouse look, E interact, Q drop, left mouse work action, Escape pause. Refine as needed and display controls.
Supply a working scene or reproducible editor setup tool with assigned references.
Fans, radios and car batteries were discussed as future scrap objects; replacing a fan motor and switching it on is a possible next repair. All conveyors, appliance repairs, larger maps and additional machines are unfinished. Vehicles are optional, not a firm requirement.

KNOWN ISSUES AND VERIFICATION
No known gameplay bugs exist because no game runs yet. Browser clicking/upload automation was unreliable during campaign setup; it is not a game defect. The final AI explanations may need saving if separately authorized campaign work resumes.
No Unity import/compile/build/playtest has been performed. The working title has not been checked for existing uses.
Run meaningful checks available in Cloud. If Unity is unavailable, provide precise local verification steps and clearly state unverified editor compilation/playability. Do not equate writing scripts with a verified playable build.

CONTINUATION INSTRUCTIONS
Implement the small complete loop before expanding. Preserve any newer work you find. Report implemented behavior, checks actually run and remaining issues. Add a README with editor version, scene/setup steps, controls and playthrough instructions. Update PROJECT_HANDOFF.md with verified status and the next concrete task after meaningful progress.

