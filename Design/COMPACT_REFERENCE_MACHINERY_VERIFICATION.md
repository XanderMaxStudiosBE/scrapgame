# Movable workshop and production machinery — 2026-10-08

The three images in `VisualTargets-2026-10-08` are environment concepts. This pass translates their recognizable workshop/production forms into the existing original equipment assemblies; it does not copy the illustrated factory layout or grant starting machinery. Unity is unavailable, so engine import, rendering, raycast readability and desktop performance remain unverified.

## Implemented source

`CompactEquipmentVisuals`, `CompactAutomationVisuals` and `CompactIndustryVisuals` retain the authored FBXs and worn atlas. A movable bench now carries a shallow tool board with peg holes, hanging spanners/pliers, a small hood and two empty catch trays. These fittings remain within its editable equipment footprint and follow placement/rotation. The hood is an original geometry fitting, not a new light or fixed shed. The existing vise, tabletop, repair tools and coordinator-driven work tool remain. Empty decorative trays never add component inventory.

Tier1's existing low rear IN now meets a visible inclined roller throat leading toward the original high cutter hopper. Its historical OUT remains index0 at -Z; IN remains +Z. Tier2 gets service handles and chassis braces; storage gets lifting handles/boots; the generator gets a cable reel/electrical symbol; the primary's wide whole-object ramp gets alternating grip marks; dispatch gets pallet straps/tie-downs. These original fittings are at most two additional combined renderer/owned-mesh surfaces per detailed equipment, with no Rigidbody, collider, interaction identity, per-prop Update or added light. Splitters/mergers use their existing original bodies and shared port meshes.

Every physical mouth, including each junction branch, has amber IN/sage OUT guide posts, an outward-facing raised ivory role stencil, exposed rollers/bearings and a bolted sleeve. The sleeves meet the exact authoritative belt endpoint. Their deck top is .69m and roller top is .7m, matching the cargo base; the earlier collar rollers reached .744m and cut into that space. Painted lane arrows now sit just above the actual deck rather than floating .06m above it. Collar and belt rail centres both sit .4m from the lane centre.

Belts retain three combined meshes, the exact core path, .66m lane/.86m rail envelope, and no physics. Low endpoint splice sleeves match the mouth sleeve faces, with a visible transfer roller. Raised turn beds and rail gaps now appear only at a real direction change; collinear port stubs no longer create extra raised elbow pads or rail breaks. Arrows follow the actual OUT-to-IN path segments. These markings do not invent sorting/filter rules.

Workbench/generator authored bodies and bench repair tools now follow the editable width/depth, as Tier1 and the later equipment already did. Equipment name plates sit on the bench apron, generator tank, Tier1 hopper and Tier2 gearbox with constrained widths. Generator/Tier1 power socket helpers follow original body scaling; the coordinator uses these for cable endpoints and status positioning. Electrical cables remain separate from cargo belts.

No port coordinate/index, core processing rule, progression, inventory, save schema, paid job, transport progress, scene, material/texture/FBX bytes or asset GUID is changed by the machinery presentation code.

## Reproducible checks

Run:

```sh
SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-machinery-presentation.sh
```

This compiles the actual Core types, the three runtime equipment builders, port target, `YardGeometry` and `ProceduralMeshOwner` with warnings as errors. Its controlled API/value adapter exercises real hierarchy/quaternion/mesh arithmetic and owner destruction callbacks. Authored resources, fonts, shaders and lighting are explicitly substitute fixtures; it does not execute Unity import/rendering/physics or prove Unity compilation.

The final run passes 1,897,357 throwing adapter assertions: 216 real/ghost equipment builders across nine kinds, four yaws and three editable footprint variants; nine actual belt routes; independent port identity/roles and amber/sage/ivory atlas use; arrow direction, lane endpoints and item arc length; exact sleeve terminal-face vertices under a rotated/translated yard; bounded assemblies/finite UVs/outward winding; private mesh ownership versus shared source ownership; physics-free ghosts/belts; and cached industrial load/paused drive behavior. A passing assertion count is diagnostic coverage, not a game-performance result.

`CompactReferenceMachineryTests.cs` supplies twelve native EditMode invocations, including nested rotated/translated owners, every detailed kind and yaw with narrowed/deepened footprints, immutable shared original meshes, exact mouth/belt sleeve-face vertices, level decks/roller cargo clearance, each junction stencil and straight collinear-stub geometry. These engine tests are supplied and remain unrun.

## Real source preview

`Previews/ReferenceMachineryAssemblies.png` is a labelled Blender preview of the actual runtime procedural mesh export and retained tracked FBXs. It shows the manual bench, Tier1, whole-scrap/dispatch equipment and a sample purchased line. The adapter's authored envelope fixtures are excluded; Blender imports the real resources at exported hierarchy transforms. Runtime rasterized font/name-plate fixtures are omitted; the visible mesh IN/OUT stencils are real exported runtime geometry. This is a source-geometry inspection, not Unity gameplay or finished visual acceptance. Ground, lighting and materials are Blender approximations.

Reproduce it with:

```sh
SCRAPSHIFT_MONO_ROOT=/workspace/tooling/mono ./Tests/run-machinery-presentation.sh --export /tmp/scrapshift-machinery.json
blender -b --python-exit-code 1 --python ArtSource/preview_reference_machinery.py -- --source-json /tmp/scrapshift-machinery.json
```

The authored bench top remains approximately 1.1m high; the reference's approximate .9m worktop and full surrounding workshop scene are still visual acceptance comparisons. This pass preserves the original working surface/feedback arrangement and adds an equipment-attached tool board rather than fixing a large shelter over moveable machinery. The sample line is only preview composition, never a mandatory or free runtime layout.

## Local Unity acceptance

Open the existing `CompactScrapyard` with the retained save and run EditMode. At player height compare the bench/tool board, Tier1 rear throat, generator controls, junction role stencils and short connected line to the reference pack. Test all four supported rotations and editable footprints, physical mouth clicks, wrong-role rejection, custom-size cable snaps, visible cargo at both splice seams, elbow direction, pause/power/downstream-blocking and saved transit reconstruction. Confirm no hood or fittings obstruct use/placement and no decorative tray supplies items.

Profile actual renderer/shadow/collider behavior in Editor and desktop on all quality presets, including repeated view destruction. Original asset bytes and source budgets are compatibility/resource checks, not measured draw calls or FPS. Creator visual acceptance, native raycast/rendering, Unity cleanup timing and a finished desktop build remain outstanding.
