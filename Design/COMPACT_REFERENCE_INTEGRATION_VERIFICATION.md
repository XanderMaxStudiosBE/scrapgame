# Physical production lines and reference pass — October 8, 2026

This pass started from latest `main` at `6279a3d`. All three images in `VisualTargets-2026-10-08` were inspected as concepts. The checkout contains the editor setup and runtime scene builders; its generated scene is not tracked. The existing Unity 6000.3.25f1 / URP 17.3.0 configuration is retained.

Final integration review on October 9 rechecked the source runners, preserved asset metadata and the labelled Blender preview. The connected-line camera was widened to keep both machines and their real belt in view; this changes only the preview composition.

## Implemented

The office now has a continuous pitched, wall-supported awning with gutter and sheet repairs. Worn fence panels give the existing compact boundary a clearer silhouette. The existing wheel-lane texture repeats along travelled metres. Existing daytime lighting, warm counter fixtures, stock modules and ground/material resources are retained. New fixed geometry stays within the existing office reservation and boundary planes. See `COMPACT_REFERENCE_ENVIRONMENT_VERIFICATION.md` for source budgets and geometry checks.

Original equipment gains attached tool boards, open component trays, drive/service fittings and clearer mouth frames. Ports have amber IN / sage OUT paint, exposed rollers and raised role stencils, including junction mouths. Belts meet the historical mouth coordinates, with matching endpoint sleeves, a common level travel surface, visible supports and arrows that follow the actual OUT-to-IN route. Original imported meshes remain shared and unchanged; generated additions have private mesh ownership. Custom machine dimensions scale their bodies and fittings. Power cables use the scaled original sockets, and their cache refreshes when endpoint dimensions change. See `COMPACT_REFERENCE_MACHINERY_VERIFICATION.md`.

With empty hands, the actual interaction binding starts a belt directly from a physical OUT mouth. Clicking the machine body still opens its management page. Carried components remain rejected at OUT and use the existing IN processing/queue rules. Invalid, missing, occupied or non-quarter-turn sources cannot start a route. The management shortcut uses the same read-only guard.

Snapped previews display both station IDs and port numbers, complete price and route length for valid and invalid routes. Direction arrows, separate OUT/IN outlines and the full required corridor make the connection legible. Rotation switches the existing elbow; Escape or the rebound build action cancels free. Confirmation checks native world clearance again before the authoritative model rechecks geometry, ownership, money and IDs. Preview roots are reused within construction and destroyed on cancellation/completion. Belts and preview markings add no collision or transport simulation.

## Compatibility

No processing, transport, save schema, migration, economy, progression or settings backend was replaced. Workbench/Tier1/Tier2 retain independent queued inputs and reserved finished outputs. Manual strokes, generator power, filters, junction arbitration, blocked destinations, recovery eligibility and saved in-transit IDs/progress follow the existing core model. Tier1 OUT index0 remains at -Z and IN at +Z. Other port identities and coordinates are unchanged.

Basic belts/storage remain at level1, junctions at3, Tier2 at5, primary/export at12. No equipment, cash or XP is granted. Existing authored art, textures, material/profile/catalogue assets, metadata GUIDs, scenes, package configuration, legacy saves and preferences are preserved. The example factory is never installed in a fresh yard.

## Executed source checks

Reproducible runners are now in the repository, rather than depending on the missing historical supplemental adapter directory. On a host with Mono, run them directly. This Cloud workspace uses `SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono` before each shell command.

- `Tests/run-core-tests.sh`: 347 pure scenarios, including seven new reference-line groups, plus the existing filesystem/recovery, runtime-material, editor-material and occupancy groups pass with warnings as errors. Rotated manual/Tier1/Tier2 lines, full queues, blocked saved cargo and recovery lineage are covered. Serializer adapters do not execute Unity JSON.
- `Tests/run-reference-conveyor-tests.sh`: independently executes the seven new shared scenario groups across all four supported yaws.
- `Tests/run-starter-floor.sh`: compiles actual stock declarations and fresh core state, and extracts current protected construction footprints. Conservative free floor is at least 1,152.45m² of 1,728m²; padding stock/equipment/scrap by 0.4m on every side still leaves at least 934.73m² (54.09%). This is declaration arithmetic, not native walking/collision acceptance.
- `Tests/run-machinery-presentation.sh`: compiles actual equipment/port/belt builders, `YardGeometry` and private mesh cleanup against controlled hierarchy/math APIs. It checks 216 equipment/ghost builds across nine kinds, four yaws and three footprint variants, plus nine belt routes. Imported model substitutes are labelled fixtures and are excluded from source preview exports.
- `Tests/run-reference-environment.sh`: compiles the actual world builder and geometry with controlled import/material/font/backdrop/batching dependencies. Both batching settings retain 21 private cover boxes, one canopy caster, 36 screens, 41 rails and 19 existing fixed colliders. Generated envelopes, upward lane winding, metre UV travel and private cleanup pass. Native batching and authored imports are excluded from this check.
- `Tests/run-port-interaction.sh`: 15 groups compile 25 unchanged extracted production coordinator methods and five actual socket helpers, the full real core, build mode and input settings against controlled presentation/input/physics APIs. They verify direct OUT selection, wrong-role carrying, occupied sources, input queues, rebound controls, Settings/pause/release guards, full route cost, confirmation, preview cleanup, transformed route clearance and dimension-aware cable caching.
- Existing original/compact/automation/industry/salvage/stock/backdrop/ground asset audits, material bindings and texture metadata checks pass. C# syntax and whitespace checks pass. These checks do not establish engine compilation or rendering.

New native EditMode fixtures cover environment envelopes, real imported machinery/ghost geometry, port stencils/seams, route preview and physics clearance. The existing runtime fixtures also gain direct OUT/cancellation/body-management/rebound-input and custom cable-dimension cases. They are supplied for local Unity execution and remain unrun here.

Review also corrected the existing industrial presentation test's lookup of a loaded-view name containing a literal slash: it now searches direct children rather than treating the name as a transform path. Runtime object names and caching are retained.

## Next concrete task

With Unity closed, update the checkout and reopen the existing compact scene and save. If the generated scene is absent, use **Scrapshift → Create or Open Compact Yard**; the setup preserves an existing scene. Run EditMode and then play the earned manual → powered line with occupied ports, both elbows, cancelled purchases, full input/output, downstream recovery, pause/Settings, rebound controls and save/quit/reopen. Include an older factory using Tier1's original outlet and schema2/3 paid work.

Capture actual Unity views from the entrance, the live moved workbench and an actually purchased automation line at player eye height. Record camera FOV/aspect and compare the three references on all presets. Check the office sign/awning, worn ground, machine/port scale, stock approaches, stencil readability, cargo direction and shadows. Profile Editor and desktop frame times, then build and play a desktop player. Unity compilation/import, actual input/physics/audio, visual acceptance, performance and desktop build remain unverified in this Cloud session. Any Blender previews are labelled renders of source assets/geometry, not Unity screenshots or finished gameplay.
