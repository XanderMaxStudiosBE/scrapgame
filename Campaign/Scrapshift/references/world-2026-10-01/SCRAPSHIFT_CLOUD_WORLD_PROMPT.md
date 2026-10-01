Continue SCRAPSHIFT in this existing repository and implement the concept world shown in the three attached reference images. Deliver actual Unity environment code and original game assets, integrated with the existing playable systems. Work through implementation and verification; a plan or concept render alone does not complete this task.

Read AGENTS.md, PROJECT_HANDOFF.md, README.md, GAME_FINISHING_PLAN.md, CLOUD_TASK_PROMPT.md and ArtSource/README.md first. Inspect the current checkout and existing changes before editing. The repository and its current implementation are the source of truth.

VISUAL REFERENCES

The attachments are AI-generated environment concepts, not screenshots of the current game:
1. 01-werf-overzicht.png: overall yard atmosphere and spatial composition.
2. 02-werkplaats.png: the workbench, repair area, recognizable tools and workshop dressing.
3. 03-schrootstraat.png: a readable salvage lane, appliances, containers and scenery.

Use them as a coherent art direction. They are ambitious visual targets; translate their useful qualities into an achievable original indie Unity environment. Prioritize recognizable objects, purposeful layout, tactile surfaces, warm lighting and clear navigation. Preserve the existing gameplay station coordinates and saved-world compatibility instead of reproducing the pictures' layout literally.

If the attachments are unavailable, look for these filenames in the repository. Continue the initial code/asset inspection while identifying the missing references. Do not claim to have matched images you cannot inspect. Keep any added reference copies under Campaign/Scrapshift/references/ with clear concept-art provenance.

THE REQUEST

Make the existing scrapyard feel like this world when the player opens the normal Scrapshift scene:
- A compact, inviting European industrial scrapyard with late-1990s character.
- A corrugated-metal workshop with believable support posts, roof edges, gutters, fixtures and clearly separate workstations.
- Worn timber workbenches, tool storage, a visible repair area, cable bundles, copper trays, fan/radio props and the powered wire stripper.
- A small weathered yard office with readable windows and a welcoming interior suggestion.
- Sorted appliance rows, pallets, low scrap bins and faded teal containers around clear walking lanes.
- A restrained industrial boundary of fences, trees and distant brick buildings that gives the yard context.
- Worn cream, dusty teal, faded olive, rust-orange accents, copper, galvanized steel and warm wood. Retain color separation; rust is localized rather than an orange coating on everything.
- Layered gravel, restrained tire wear, a few weeds and small rough puddles where appropriate. Ground detail must remain inexpensive and navigable.
- Soft daylight, readable workshop shade, practical warm lamps and grounded objects.

Aim for worn retro realism with believable, slightly chunky forms and restrained texture detail. Use original assets. Retro Rewind is a broad reference for warmth and readable everyday objects; do not copy its assets, characters or video-store setting.

IMPLEMENTATION PATH

Start by comparing each reference with the existing environment and identify the largest visible gaps. Then implement the workshop hub to a consistent standard and apply that language to the office, salvage lanes and boundary. Finish a coherent integrated pass across these areas.

Inspect and extend the existing integration points rather than adding an independent demo:
- Runtime/YardBootstrap.cs, YardProps.cs, ScrapyardWorld.cs, CozyYardDetails.cs and YardGeometry.cs.
- AuthoredYardProps.cs, RetroMaterialLibrary.cs and the scoped import/editor setup.
- YardLighting.cs, YardLightingProfile.cs, YardContactShadows.cs and the existing CozyAfternoon profile.
- Core/YardNavigation.cs and YardWorldLayout.cs for authoritative destinations, bounds and saved placement.
- ArtSource/ for reproducible original mesh/texture authoring.

Reuse good existing models. Improve or add original meshes where a recognizable shape is missing; exposed placeholder cubes are insufficient for prominent props. Supply exported assets and reproducible authoring source. Do not globally regenerate or overwrite unrelated FBXs just to add a decoration. Preserve .meta GUIDs and targeted material edits; update audits/manifest records for changed assets.

Give materials distinct roughness, restrained color variation and meaningful wear. Prefer reuse, atlas variants or a small justified set of shared materials over one private material per prop. Keep decorative dressing separate from authoritative interactive items. A decorative fan or cable coil must not create inventory or imply an interaction it does not support.

Existing bootstrap scenes must receive the changes through the established builders/setup path. Provide a non-destructive update route for any additional serialized references. Keep the player's ordinary Start/Continue flow working without manual scene reconstruction.

COMPATIBILITY AND SCOPE

Preserve Unity 6000.3.25f1 and URP 17.3.0 unless repository evidence establishes a different authorized baseline. Do not upgrade the engine, migrate input systems or add paid dependencies.

Preserve working wire/manual/powered processing, orders, fan/radio faults and repair/salvage, day refresh, upgrades, navigation, controls, menus, preferences and saves. Keep exactly-once outputs/payments and existing item IDs/version-one save compatibility.

Preserve station approaches, raycast targets, coarse colliders, map markers and navigation coordinates. Place new decoration around the existing stations. Do not trap saved player positions or dropped items behind new solid props. Keep moving rollers, appliance rotors, status lamps and station outputs independently mutable.

Preserve existing scenes, tuned Balance assets, customized materials and saves. Avoid unrelated cleanup. No vehicles, conveyors, cranes, new appliance types or new economy systems are required. Scenery stays scenery.

Use portable relative paths. Keep runtime generation bounded, batch compatible stationary decoration by district, and avoid per-frame world scans, physics on every scrap fragment and uncontrolled realtime lights. Maintain readable Laptop and Balanced presets. Use cheap surface/detail treatments before adding expensive reflections or lighting. Preserve lighting/pipeline cleanup after Play. Do not claim an FPS improvement without measurement.

VERIFICATION

Run checks appropriate to the actual changes:
- Tests/run-core-tests.sh using the available Mono environment.
- Tests/audit-original-assets.py.
- Blender round-trip scale, UV, geometry, triangle-budget and hash audits after any mesh exports; verify original .meta GUID preservation.
- Relevant source/syntax checks and git diff --check.
- Meaningful regression checks for changed collision bounds, station access, geometry/material lifecycle or other behavior where applicable.

Historical reports mention 82 core scenarios, three filesystem recovery checks and 149 supplied Unity EditMode cases. Reinspect the current suite and report only checks you actually run.

If Unity is available, import and compile with the pinned editor, run the relevant EditMode suite, exercise the normal Start/Continue scene, and inspect the three reference viewpoints plus station approaches. Check readable signs/HUD, appliance interaction, save/reopen, pause, graphics switching and cleanup. Capture actual Unity images, build a desktop player if the environment supports it, and measure representative CPU/GPU frame times before making performance claims.

If Unity is unavailable, complete the source/assets and all available checks. Render useful previews from the actual exported assets where possible, clearly labelled as Blender asset/layout previews. Document the exact remaining local Unity import, shader, collision, gameplay, save and performance checks. An unavailable editor is not a reason to stop after writing a roadmap, and source/Blender checks are not evidence of a verified playable build.

DELIVERABLES AND COMPLETION

Deliver the integrated environment changes, original exported assets with authoring source, updated packaging/audit records, and concise non-destructive local setup/verification instructions. Update PROJECT_HANDOFF.md and README.md with what is actually implemented, checks run, remaining issues and the next concrete local test. Keep concept art, Blender previews and real Unity screenshots clearly distinguished.

Before finishing, review the resulting diff and fix issues you discover. Report the visible changes by yard area, the actual check results and material limitations. The work is complete when the visual pass is integrated in the existing scene and available checks pass; visual acceptance and engine/player verification must be reported according to the evidence.

Proceed autonomously with routine implementation decisions. Stay focused on making this specific world usable in the existing game. Do not publish a campaign, invent release commitments or describe the game as finished without a verified build and creator approval.
