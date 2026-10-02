# Screenshot-driven finishing pass

**2026-10-02 follow-up:** the earlier recovery did not resolve the creator's white-prop screenshot on all presets. The current fix adds public upgrader aliases to the actual twelve tracked materials, synchronizes authoring aliases after edits and detects default sampler objects distinct from whiteTexture. Modern texture/tint/UV/roughness values, GUIDs and all PNG/FBX bytes remain the same; the material files themselves now have compatibility additions. Seven material-authoring checks and seven real-runtime branch groups against a narrow API adapter pass. Three actual Unity regression cases are supplied but unrun. Use README's Repair Missing Material Bindings / Diagnose Rendering steps; the exact local cause and visual result still require Unity confirmation. The sections below describe the earlier pass.

The creator supplied actual Unity gameplay showing white props/ground wedges, an empty forecourt and large HUD panels, and confirmed **Laptop**. This pass addresses those concrete symptoms and adds an opening-chapter completion flow. It is not a finished-game or visual-approval claim.

## Material recovery

The pinned URP upgrader's public source (`6000.3/staging`, `Editor/AssetPostProcessors/MaterialPostprocessor.cs`, `LitUpdaterV1`) renames `_MainTex` to `_BaseMap`, `_Color` to `_BaseColor`, and derives smoothness from legacy fields. The hand-authored source materials lack those aliases and an embedded URP asset version. An upgrade from version zero can replace their existing albedo binding with a missing one. This is a **plausible source-supported cause**, not a reproduction of the creator's import state; no Console error was supplied.

`ScrapshiftRendering/Materials.asset` holds direct original texture references independently of the twelve source materials. Delayed editor recovery fills null/engine-white-placeholder maps after import, normalizes Lit keywords from existing values, and synchronizes public legacy aliases. A valid legacy albedo is preferred over the fallback, including its UV transform. Valid custom maps, tint, current UVs, smoothness, workflow and shader choices remain. The two authored straight-alpha ground layers additionally normalize alpha blend factors, fading specular and depth writes; custom additive blends and shaders remain untouched. Future newly generated world materials serialize the compatibility aliases without stamping package-private URP version data. Existing tracked material bytes/GUIDs are unchanged.

Runtime loaders independently guard known materials and share one recovery copy per resource/world owner when needed. Copies are disposed with that owner; imported materials/textures are not runtime-owned. Recovery applies to original authored props, world props/fence/ground, procedural boxes/cylinders and lamp source materials. Existing coarse 64px surface textures now import with bilinear filtering, mipmaps and modest anisotropy to reduce distant aliasing. The delayed editor pass updates old import settings automatically; source PNGs and metadata GUIDs are retained.

After pulling and letting Unity import/compile, use the existing scene. No scene regeneration, save deletion or preference reset is required. **Scrapshift → Repair Missing Material Bindings** explicitly repeats the idempotent recovery if needed. Unity may save recovered material/importer state locally; inspect legitimate local custom changes before discarding them.

## World and presentation

Sixteen original-model placements bring two inward-facing sheltered salvage bays and smaller entry salvage groups into the empty forecourt, with sparse weeds. The layout now has 81 placements. Only eight new foreground groups opt into coarse static collision: six appliance rows and two shelters, with an open front and aisle between their corner posts. Original routes, stations, boundary, floor, cars, office and container footprints remain. No new rigidbodies, inventory, lights or per-frame world scan is added.

At startup, a new piece that would cover a loaded player position or loose saved item is hidden for that session. The save/entities are not moved or edited. Stored/carried item coordinates are excluded. Contact grounding is built afterward so hidden pieces leave no ghost contact patches. Newly visible rows, shelter feet and teal containers receive the existing one-mesh contact treatment; row patches follow actual rotation. Laptop's existing render/shadow/post budget and four-light rig are retained.

The HUD now uses compact day/cash, objective and navigation blocks. Empty-hand/no-target prompts disappear. Actual-binding interactions, held-item/drop hints and work progress appear contextually; request summaries remain on wide displays. Pause pages draw without gameplay HUD behind them. Help/Settings still display all movement/work bindings, including saved AZERTY choices. Station plaques are lower and smaller with shorter workshop labels. Data still refreshes at ten Hz/on changes; no input backend migration.

## Opening chapter

Completion is checked on Continue or after view-changing transactions, without a per-frame inventory scan. Completing all six existing yard goals presents **Chapter one / A yard of your own**, then continues into free play. A new additive version-one `openingChapterSeen` acknowledgement prevents repeated automatic popups. Established legacy facts are recorded into the existing journal when completion is presented, so later sales/day changes cannot erase the completed chapter. It grants no money/material and consumes no jobs/items. The journal can reopen the recap without changing progression, and Back/Escape returns to that paused journal rather than inadvertently starting gameplay from the title. Escape resumes through the existing held-input release gate; menus freeze gameplay/processing/audio work. Save-write errors remain visible on the completion page. Existing saves default to unacknowledged; established yards receive the recap on Continue. Gameplay and preference versions remain unchanged.

## Executed checks

- 100 pure C# scenarios, including a real from-€0 route through all six goals, premature completion rejection, once-only acknowledgement, legacy evidence consumption, day/reconstruction persistence and continued processing. Existing rebinding/persistence/conflict/defaults/guidance/pause-data/economy tests remain included; the 20,000-action conservation and longer progression checks still pass.
- Three filesystem/recovery branches against the real SaveStore with a narrow serializer adapter. This is not Unity JSON execution.
- Six Python material-reference/authoring tests, including deliberately broken albedo references and first-time generation in an isolated miniature checkout. These validate data/serialization contracts, not a Unity upgrader run.
- Original packaging: 31 unchanged FBX files match existing Blender scale/UV/budget audit hashes; eight unchanged WAVs, complete unique asset metadata and lighting links pass.
- World PNG hashes/CRCs/masks/alpha/material links and all 81 placements pass; portable geometry checks preserve every protected route/supply approach. These are not engine collision tests.
- All 95 Assets C# files parse; changed Python sources compile and `git diff --check` passes.

There are **219 supplied Unity EditMode cases**, all unrun in Cloud. New cases cover missing/custom/legacy material maps, keyword/alpha state, repeat repair, custom specular workflow, private-copy lifetime, direct catalog imports, rotated contact geometry, static/open shelter collision, saved access, actual chapter JSON defaults/persistence and recap pause/resume.

[Eye-height forecourt](Assets/Scrapshift/Art/Previews/EyeHeightForecourt.png) reimports the real FBXs/textures and shared current layout in Blender. It is labelled **not Unity gameplay**. Blender approximates light/ambient/sky and omits runtime HUD, live signs and custom contact shader. It does not verify this recovery, Laptop rendering or FPS.

## Required local Unity checks

1. Pull, finish compilation/import and inspect Console. Confirm the recovery log if materials needed repair. Inspect PropAtlas and ground materials for restored `_BaseMap`; verify valid tuned material maps/tints/roughness remain.
2. Run every EditMode case in Unity 6000.3.25f1 / URP 17.3.0. Also perform a player build; shader variant stripping and actual serialization remain untested here.
3. Continue the same save in **Laptop**. Check workshop/car/tree colors and fence holes; white wedges must be gone and dirt/puddle edges must fade. Walk around new bays and old stations, including saved positions/dropped stock near a new footprint. Check real shadows/fog, ground shimmer, suspended fixture orientation and billboard/plaques at eye height.
4. Check contextual HUD at the creator's Game-view size, narrower windows and long hints; verify actual saved Z/S/Q/D, Interact/Drop and keyboard/mouse Work rebinding. Capture/cancel/swap/default restoration, pause/resume/Escape and sensitivity/invert-Y remain functional requirements.
5. Earn six goals, trigger completion, hold buttons while resuming, quit/restart, reopen recap and keep working. Confirm no repeated popup/reward or lost inventory, contracts, partial jobs or preferences. Verify save-failure visibility and old-file JSON defaults.
6. Measure gameplay CPU/GPU frame times in Editor and a standalone Laptop build. No FPS gain or creator visual acceptance is claimed. Refine the next art/pacing pass from actual game images and measured results.
