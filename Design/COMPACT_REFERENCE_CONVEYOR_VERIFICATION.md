# Physical conveyor construction — October 8, 2026

Read the current handoff, `COMPACT_EARLY_LINE_VERIFICATION.md` and the three images/README in `VisualTargets-2026-10-08` first. The images are concept references. This pass improves the existing purchased, freely placed conveyor network; it adds no fixed example factory and no new transport topology.

## Concrete differences and implementation

The earlier source-selection preview coloured every OUT green, even an already connected OUT. Management-menu source shortcuts bypassed this selection stage. A snapped but invalid route omitted its total cost and length, and the preview line did not show transport direction or the required corridor. Native ray/obstacle coordinates also assumed the yard coordinator was at the world origin.

- `CompactPortSelection.CanSelectOutput` is a shared read-only source guard. Physical-mouth shortcuts, the source-selection stage, an ongoing route and per-OUT management buttons check source existence, index, quarter-turn rotation and existing source belt. Occupied outputs explain how to choose a junction output or dismantle the empty existing belt. Generator power connections and IN-only dispatch cannot become item-belt sources.
- A selected source mouth retains a sage outline, and a snapped destination has an amber outline. Every actual route leg has an OUT-to-IN arrow and a thin outline of the authoritative 0.94m corridor. A rejected route turns red. No target leaves a red cursor guide and removes the former destination/arrow/corridor presentation; it cannot purchase a stale route.
- Every snapped preview shows source/destination IDs and indices, complete routed length, and the configured full price, including rejected routes. Invalid previews keep the blocking reason. No preview, elbow change or cancellation spends cash or consumes IDs. Confirm rechecks native world obstruction and then the authoritative model's complete connection validation before paying.
- Mouth-body fallback converts native ray hits into yard-local coordinates. Native obstruction queries transform the authoritative route into world space and cover the entire corridor, including endpoint clearance. Cached cargo paths and preview heights use `CompactAutomationVisuals.ItemHeight`, preserving agreement with machinery ports.
- Temporary lines share the existing palette material cache, cast no shadows and add no collider/Rigidbody/per-object update. Arrows/corridor lines are reused within construction; cancellation destroys their owning preview root. The current path has at most four legs, so the preview uses at most eleven temporary line renderers.

Main integrates the direct empty-hand interaction on the physical OUT mouth, matching rebound-control hints and the existing body-management/cargo role rules. Machine meshes, mouth geometry and actual belt rendering are owned by the machinery pass.

## Compatibility

No port ID/coordinate, save version, paid job/output, in-flight item, progression gate, price, power rule, input capacity or processing algorithm is changed. Tier1 OUT remains index0 on local -Z; its IN remains +Z. Quarter turns and straight/elbow routes remain the supported scope. Processor contents remain input queues and paid job yields remain separate output reservations. Manual work still needs strokes; powered work still needs cables. Transfers still award no sale XP. Custom catalogue settings and old saved belts remain authoritative.

## Executed source evidence

`SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono bash Tests/run-reference-conveyor-tests.sh` passes seven focused pure-C# groups with warnings treated as errors:

1. Every supported equipment kind, every real port index and all four yaws retain exact IN/OUT selection; opposite-role snapping is refused.
2. An occupied splitter output is rejected while its other outputs remain selectable; invalid IDs/indices, generator power equipment and IN-only dispatch are refused.
3. Output selection/dependency failures are read-only, including non-quarter rotation.
4. Both route elbow choices at every quarter turn retain exact existing mouth endpoints, use a custom price, leave cash/IDs untouched during preview and debit the purchased cost once.
5. A previously valid route cannot confirm after cash changes; failed confirmation leaves ownership/IDs/cash intact.
6. Workbench, Tier1 and Tier2 lines at all four yaws retain raw input without finished output until actual manual work/power. Every finished wire yield is exact and grants no transfer rewards.
7. Full downstream storage at all four yaws blocks safely. Model reconstruction preserves in-flight identity/progress/cooldown and paid yields/source lineage; clearing storage produces exactly fifteen outputs from three original wires with no extra cash/XP. This is explicit field reconstruction, not native Unity JSON serialization.

The integrated `Tests/run-core-tests.sh` also passes all 347 pure scenarios, including these seven, followed by the existing filesystem/recovery, material, editor and occupancy groups. The two new Unity test metadata GUIDs are unique and `git diff --check` passes.

`CompactReferenceConveyorTests` supplies the seven shared groups and five native fixtures: exact transformed preview endpoints/direction/corridor/pooling/destruction, plus actual Physics clearance at four yaws. The native fixtures are unrun in Cloud.

`SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono bash Tests/run-port-interaction.sh` passes fifteen actual coordinator/source-API groups against 25 extracted actual methods, plus five actual power-socket/cable helpers. These include physical OUT interaction with rebound controls, occupied/carried/gated source refusal, manual intake/paid-job preservation, complete route price, OUT-to-IN arrows and corridor outlines, no-hit stale validity removal, confirmation's second world-obstruction check, translated/rotated/scaled yard coordinates and preview/resource cleanup. A same-click obstruction now replaces the earlier clear status while retaining IDs/price/length and the red preview. Controlled APIs do not verify Unity rendering, hardware input or native physics.

## Local Unity acceptance still required

1. Open the existing scene/save in Unity 6000.3.25f1 and run the EditMode tests without resetting/regenerating. Inspect all physical mouths and belt endpoints at every quarter turn, including the historical Tier1 OUT.
2. Empty-handed, aim at an actual OUT and interact with the configured binding. Confirm that occupied OUT refuses, wrong-role IN/body aiming behaves correctly, and carried inputs refuse OUT without loss.
3. Snap to a compatible IN. Check both elbow choices, source/destination outlines, directional arrows, complete price/length and visible clearance. Test wrong role, filtered input, occupied input, insufficient cash, fixed/loose obstacles, route limit and Escape with unchanged money/IDs.
4. Confirm a purchased route meets each visible mouth and its moving cargo stays on the lane. Fill downstream storage, pause/Settings/save/quit/reopen and clear stock: production should resume with conserved eligibility and no repeated outputs. Manual work and missing power must still stop production.
5. Judge the preview at player height across presentation presets; inspect overlap/contrast, cleanup after repeated cancels and native capsule/physics clearance. Profile locally if temporary preview renderers affect frame times.

Unity import/compilation, line-renderer appearance, native input/rays/physics, native JSON save round-trips and Editor/player performance remain unverified here.
