Continue Scrapshift in the existing repository on the latest work branch. This branch contains the newer yard/conveyor/live feedback implementation (41588db or descendants) and this redesign pack; main currently has the earlier references. Fetch and inspect newer work before editing. Do not regress to the older main implementation or discard local changes.

The creator has played the current map and rejects its appearance. This task is a substantial map composition pass. Sound also needs improvement, but do the map first and defer audio.

READ AND LOOK

Read AGENTS.md, PROJECT_HANDOFF.md, Design/MapRedesign-2026-10-09/README.md and the latest environment/reference/live feedback verification documents.

Actually open all three current-*.png screenshots in Design/MapRedesign-2026-10-09. They show the map the creator dislikes. Open layout-plan.svg and the previous three Design/VisualTargets-2026-10-08 images. The SVG is the new composition proposal; the earlier concepts guide atmosphere and prop readability. None of the concept art is implemented gameplay evidence.

WHY THE CURRENT MAP FAILS

The ground is visually uniform, workshop props feel stranded, similar warehouses repeat in the background, and isolated scrap props do not form believable activity areas. Adding more small props or another awning alone will not address this feedback.

IMPLEMENT THIS DIRECTION

Keep a compact approximately 48 x 36m yard. Build a coherent arrival area with gate/sign/office, one distinctive modest workshop landmark along a perimeter, and an attached open-sided canopy facing the yard. Group manual work visually around that canopy. Keep receiving nearby with a practical delivery approach. Group cosmetic scrap into purposeful edge bays. Reduce competing repeated background warehouses; use restrained varied outside-fence scenery.

Keep the centre/right floor contiguous and open for player-designed automation. At least half the starting floor must remain usable for building/circulation. Machines, workbenches, generators, storage and conveyors remain freely placed and rotatable. The drawing is not a fixed factory layout or starting inventory grant. Shelter architecture is proposed perimeter scenery; equipment underneath must still be movable.

Break up the uniform ground with workshop concrete, gravel construction floor, a wheel-worn receiving lane and sparse weeds/rubble at the edges. Use coherent texture scale, recognizable low-poly silhouettes, worn surfaces, readable daylight and restrained warm canopy light. Start with a layout/blockout pass, validate the views, then refine art.

PRESERVE THE GAME

Keep existing physical IN/OUT ports and OUT-to-IN belt construction, independent bays, manual strokes, power, processing, filters/backpressure, economy/progression, Settings/rebindings, pause and bounded save checkpoints. Preserve port IDs, positions, paid job snapshots, saved factories and GUIDs. No save reset, paid-equipment teleport or new engine/package migration.

Trace the actual runtime/editor builders so changes appear in the active world. Preserve hand-edited scenes and give explicit update instructions when necessary. Keep existing service/delivery coordinates functional. Adapt the proposed scenery around old saved construction, or retain the old layout for those saves with a documented explicit opt-in; never silently obstruct a factory.

MULTIPLE AGENTS

Use separate agents for (1) layout/ground/lighting, (2) original workshop/prop/texturing work, and (3) independent integration/compatibility review. Agree on file ownership before concurrent edits. Main owns shared contracts, active scene integration, occupancy/persistence and final verification. Use existing licensed assets; no required paid services or account setup.

DEFINITION OF DONE

Deliver implemented changes and actual comparable entrance, receiving, workshop and overhead views. Show that workshop identity, ground regions and grouped scrap have improved while a large contiguous building floor remains. Test delivery/sales, player movement/headroom, placement/rotation, belt access and an older saved line. Run appropriate source/native checks. Profile when Unity is available.

If Unity is unavailable, render real project assets at the proposed transforms, label those renders accurately, document the missing native checks and supply a local capture checklist. Do not use generated concepts as proof of implementation. Source test totals alone do not establish map quality.

Update PROJECT_HANDOFF.md with implementation, checks and remaining visual differences. Do the work directly, do not stop at a proposal. Report the files changed, how to open/update the map and any unverified Unity behavior. Do not claim the creator has accepted the visuals.
