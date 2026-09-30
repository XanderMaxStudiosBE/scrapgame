# Scrapshift project handoff

Updated: 2026-09-30 (Europe/Brussels). This document combines conversation decisions with a filesystem audit. Future agents should recheck the current checkout.

## Verified repository state

The current local repository is `D:\Ai_Cod\scrapgame`. The original conversation used `D:\Ai Cod\New-game`, which no longer exists. Use repository-relative paths in Cloud.

At inspection, branch was `main`, latest commit was `bfad65f` (`Initial Scrapshift project`), and the working tree was clean before adding these handoff documents.

**There is no implemented game in this checkout.** The tracked content is campaign artwork and documentation only. No `Assets/`, `Packages/`, `ProjectSettings/`, C# gameplay scripts, Unity scenes, prefabs, executable, tests, or CI configuration were found. Earlier assistant descriptions of systems were plans, not evidence of implementation.

## What Scrapshift is

An indie first-person 3D scrapyard simulator with a retro visual style. The player builds a small, shabby yard into an automated recycling operation.

Working tagline: **Start with your bare hands. Build a scrapyard that works for you.**

Core loop: acquire scrap -> inspect -> repair or dismantle -> recover materials -> sell -> improve tools, machines, and yard.

Key choice: repair for a higher sale price, dismantle for components, or process into materials for quicker income. Automation should replace familiar manual tasks while preserving useful decisions and hands-on repair work.

The user considered a video store and scrapyard combination, then selected the scrapyard. A video-rental shop is outside the selected concept. Electronics can still be salvageable objects.

## Progression and scope

| Stage | Player activity | Planned upgrades |
| --- | --- | --- |
| Manual yard | Carry junk, unscrew parts, sort into bins | Hand tools, trolley, larger workbench |
| Powered workshop | Feed machines and collect output | Wire stripper, metal cutter, small crusher |
| Automated yard | Arrange equipment and move batches | Conveyors, magnetic separator, automatic sorting |
| Industrial operation | Manage material flow, power, storage, larger orders | Linked lines and automatic loading |

All stages are design intentions. None is implemented here.

The first prototype priority was refined to: walk around a compact yard, collect scrap wire, strip it manually at a workbench, sell copper, buy a powered wire stripper, feed it, collect copper, and sell again. This proves the manual-to-automatic transition.

Earlier prototype ideas included a fan, radio, and car battery, plus replacing a fan motor and switching the fan on. Treat these as follow-up ideas, not prerequisites for the wire loop. No mechanics for them have been specified or implemented.

## Art and atmosphere

- PS1/late-1990s PC inspiration: chunky low-poly models, low-resolution textures, muted colors, rusty surfaces, fog beyond the fence, warm workbench lighting.
- Stable rendering and legible menus; subtle dithering/pixelation is optional.
- Homemade industrial machinery with exposed motors, mismatched panels, and rattling belts.
- Palette: ivory `#E8DDC4`, rust `#B75935`, olive `#69735B`, charcoal `#202927`.
- Existing marketing scenes are generated concept illustrations. Their detail level is not a requirement for the game's rendering or assets.

## Important technical decisions

Unity and C# were selected after comparing Godot, Unity, and Unreal. Unity was favored for the larger 3D simulation and code-based collaboration. Unity 6 LTS and the Universal 3D/URP template were proposed. No project was created in this conversation.

Local inspection found editor executables at `D:\Ai_Cod\Unity\6000.3.25f1\Editor\Unity.exe` and `D:\Ai_Cod\6000.6.3f1\Editor\Unity.exe`. These are workstation-specific and are not evidence that Cloud has Unity. Use 6000.3.25f1 as the initial LTS compatibility target unless later instructions/evidence change it. The game has not been tested with either installation.

Desktop PC, keyboard/mouse, and single-player were proposed as practical prototype defaults. Potential controls: WASD movement, mouse look, E interact, Q drop, left mouse work action, Escape pause. They are not yet implemented and may be refined.

Planned architecture separates player interaction, item definitions, carryable objects, recipes, processing stations/machines, economy, progression, save/load, and UI. Editable values should control prices, yields, costs, and processing times.

For scale, simulate physical scrap near useful interactions; represent bulk/distant processing efficiently. Avoid endless active rigidbodies and expensive updates on every machine every frame.

## Existing files and assets

| Path | Purpose |
| --- | --- |
| `Campaign/Scrapshift/assets/scrapshift-logo.png` | Verified transparent logo, 2022 x 778 |
| `Campaign/Scrapshift/assets/scrapshift-kickstarter-cover.png` | Campaign cover concept art, 1672 x 941 |
| `Campaign/Scrapshift/assets/scrapshift-social-square.png` | Coming-to-Kickstarter poster, 1254 x 1254 |
| `Campaign/Scrapshift/assets/scrapshift-progression.png` | Salvage/upgrade/automate concept graphic, 1536 x 1024 |
| `Campaign/Scrapshift/assets/section-*.png` | Seven upload-ready section headers, each 1600 x 220 |
| `Campaign/Scrapshift/assets/section-*.svg` | Editable versions of those section headers |
| `Campaign/Scrapshift/preview.html` | Local gallery of the visual kit |
| `Campaign/Scrapshift/README.md` | Art direction, usage, and campaign ordering |
| `Campaign/Scrapshift/asset-manifest.json` | Asset names, dimensions, file sizes |
| `Campaign/Scrapshift/generation-prompts.md` | Original built-in image-generation prompts |
| `Campaign/Scrapshift/campaign-story-draft.md` | Draft pitch, planned systems, development stage, AI use, and risks |
| `Campaign/Scrapshift-Kickstarter-Kit.zip` | Earlier visual-kit export; not the authoritative source for current docs |
| `AGENTS.md` | Persistent agent instructions |
| `CLOUD_TASK_PROMPT.md` | Paste-ready continuation prompt |

Section titles: The Game; Hands to Automation; Development Plan; Rewards; About the Creator; Budget; Risks and Challenges.

The four main illustrations were made using the built-in image-generation tool. Section graphics were drawn as native SVG and PNG. No runtime 3D models, textures, animations, audio, or finished trailer have been created.

## Most recent work

Before the Cloud move, we were preparing the private Kickstarter draft in the Codex browser. Draft editor: `https://www.kickstarter.com/projects/775234141/1027128765/edit/basics?ref=pbuild_dashboard`.

Creator account displayed as MaxStudios Games. The user confirmed Antwerp, Belgium, euro currency, and that crowdfunding should fund game production.

Observed browser progress:
- Title: `Scrapshift — A Retro Scrapyard Simulator`.
- Subtitle: `Salvage, repair and recycle in a retro 3D scrapyard. Start by hand and build your way toward an automated operation.`
- Existing category: Games / Video Games.
- Cover uploaded and Antwerp location entered; Save was activated and the subsequent story page was reached.
- Campaign story and risks were entered and saved. The story clearly described concept/early pre-production.
- AI involvement was selected Yes, and the generated-content option was selected.
- Detailed AI-use and source/consent explanations were entered last, **but no subsequent Save confirmation was observed for these final fields**. Recheck if future authorized campaign work resumes.

No launch, review submission, funding goal, rewards, payment details, or release dates were completed. Live website state is external to the repository and was not re-audited during this handoff.

Then the user requested a Cloud development prompt; one was provided in chat. The current task creates durable repository handoff documents and a more complete prompt.

## Systems currently being worked on

No gameplay implementation or running development process was found. The active work was campaign preparation and now handoff documentation. Player controls, processing, economy, machine upgrades, and persistence are pending work for the Cloud agent.

## Unfinished features

Everything in the playable prototype remains to be built: Unity project, yard scene, movement/look, interaction prompts, carrying/inspection, wire stripping, copper outputs, selling, money, repeatable scrap acquisition, machine purchasing/processing, UI, pause, save/load, and feedback.

Later scope: appliance repairs, additional tools and processors, conveyors, sorting, yard layout/expansion, power, storage, larger orders, automatic loading, and possibly vehicles. Vehicles were an engine-selection consideration, not a firm design commitment.

Campaign gaps: production budget, funding target, reward tiers/prices, schedule, creator biography, gameplay screenshots/trailer, audience-building plan, title availability check, and any final AI-asset policy for the shipped game.

## Known bugs, issues, and validation limits

- There are no known runtime bugs because there is no runtime implementation to test.
- The historical workspace path is invalid; current checkout is `D:\Ai_Cod\scrapgame`.
- Browser mouse clicks and file-chooser automation were unreliable during campaign setup. Keyboard activation worked for saving and navigating; the cover ultimately appeared uploaded. These are observed browser-session issues, not game bugs.
- Final AI explanation fields may be unsaved, as noted above.
- No editor import, compilation, build, gameplay test, or save/load test has been run.
- The title has not been cleared for existing uses. No crowdfunding costs or dates have been agreed.
- Concept art must stay identified as illustration. It cannot substitute for actual gameplay footage.

## Continue development

1. Read this file and `AGENTS.md`; inspect the current branch and working tree. Preserve campaign assets and any newer code.
2. If no Unity project exists, scaffold it in this repository using the chosen Unity/C# and URP baseline. Keep `Campaign/` outside runtime asset imports unless deliberately copying a specific logo.
3. Create a compact yard and first-person movement/interaction. Make setup reproducible through a committed scene or editor setup command.
4. Complete manual wire processing, selling, and a repeatable scrap source. Balance a short path to the first machine.
5. Add purchased powered wire processing with idle/processing/output-ready feedback and no duplication or negative balances.
6. Add robust persistence and basic UI/pause. Verify consistent save/load of ownership, materials, and processing.
7. Verify the complete loop and report exactly what was executed. If Cloud cannot run Unity, provide a local editor validation checklist and do not claim editor compilation or gameplay success.
8. Update this handoff after implementation, replacing plans with verified status. Advance to appliance repair or conveyor automation only once the first loop works.

Do not publish or manage Kickstarter as part of the game implementation task. Do not invent commitments. Cloud will only receive tracked files in the connected repository revision; the user needs to commit and push these new handoff files to make them available there.
