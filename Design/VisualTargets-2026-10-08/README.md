# Scrapshift visual targets — 2026-10-08

These generated images are environment concept references, not screenshots of a working build. Generated with the built-in image-generation tool. Do not use them as Kickstarter prototype evidence.

## What to build

One compact 48 x 36 metre yard. Use metres and human-scale props. Create a readable, worn retro environment with ordinary daylight and warm workshop fixtures. Avoid the old row of coloured boxes, a huge industrial landscape, or an empty dark plane.

The entrance, small office/sales and delivery pocket are fixed infrastructure. The workshop equipment, generators, machines, belts and storage are freely player-placed. Shown machinery is an example purchased arrangement, never starting inventory or mandatory positions.

Keep at least half the starting yard's floor available for building and circulation. Put cosmetic scrap, weeds and parked scenery near edges rather than filling construction space. Prefer sparse large readable props over hundreds of little scattered objects. Ground wear should describe use: wheel ruts near gate, scuffed dirt around work, gravel elsewhere.

## Three views

1. `01-entrance.png`: first-person entrance composition. The office identifies the place, the sheltered manual work area is recognizable, the central open buildable space is obvious, and the far fence makes the scale legible.
2. `02-manual-workshop.png`: human-scale dismantling work. A car, fridge, tools, motor, copper winding and component trays show the component chain; this is a small practical shelter, not a giant hangar.
3. `03-automation-line.png`: example short production line. Feed bay and output tray are distinct. Belts have legs and visible travel direction, connect through a real junction, and terminate at a container port. The generator and cables show power supply. Equipment must remain moveable and purchasable.

## Visual acceptance

## Machine ports and conveyors: creator clarification

The creator reaffirmed on October 8 that machine inputs, outputs and belt construction should feel like Satisfactory: recognizable physical mouths, directional transport, visible moving cargo and a clear connection preview. Use original Scrapshift geometry and art. Read the latest PROJECT_HANDOFF.md and Design/COMPACT_EARLY_LINE_VERIFICATION.md before older milestone documents.

- Place and rotate equipment freely within the compact buildable yard. Connect a selected physical OUT mouth to another equipment's physical IN mouth; preview direction, route, clearance and total cost before confirmation. Support the current straight/elbow routing and cancellation without payment. Never replace this with a preset factory or connections through arbitrary machine-body positions.
- Workbench, Tier1 and Tier2 have separate input queues and finished-output bays. Belt cargo must meet the actual port opening, with rollers/trays and readable IN/OUT marks. Distinguish the amber IN and sage OUT. Align the visible belt endpoint to the authoritative port transform at every supported rotation.
- Supported incoming components wait in IN. Only finished recipe yields leave OUT. A workbench still needs manual strokes; a powered machine needs its connected power supply. Neither a belt nor an input queue performs manual work or grants sale XP.
- Preserve independent bay capacities, downstream blocking, exact item conservation and saved in-transit progress. A blocked output pauses safely and resumes when space returns. Junction routing/filtering must follow the real code; painted arrows are not sorting rules.
- Generator connections are power cables, not item belts. Whole cars/fridges enter suitable whole-object intake; do not force them through small component ports. Storage and export use their supported material ports.
- Preserve existing port IDs and positions, especially Tier1 OUT index0 at -Z and its new IN at +Z. Existing saved factories must remain connected. Do not reset saves or move established outlets just to match a concept image.

Current implementation uses straight/elbow routes and quarter-turn equipment rotations. Curved/freeform belt splines, lifts, arbitrary-height routes and unrestricted junction placement are not established by these images or this clarification. If expanding construction, implement it as a separately verified compatible feature rather than claiming it already works.

Relevant implementation entry points: Assets/Scrapshift/Core/CompactConstruction.cs, Assets/Scrapshift/Core/CompactPortSelection.cs, Assets/Scrapshift/Runtime/CompactConveyorPortTarget.cs and Assets/Scrapshift/Runtime/CompactEquipmentVisuals.cs. Inspect the current transport model and coordinator alongside these files before editing.

## Image interpretation

Generated illustrations contain more fine detail than the intended runtime assets. Translate their composition and recognizable forms into performant retro meshes and textures rather than copying every small object. They are not engineering diagrams: use the current machine-port definitions and recipe rules for item direction/filtering. In particular, decorative conveyor arrows in the automation illustration are inconsistent; do not reproduce those opposing arrows or infer that a splitter sorts material without filtering logic. Equipment placement shown between views is illustrative, not a surveyed world coordinate contract.

- First-person eye height approximately 1.7m; comfortable horizontal field of view around 70 degrees. Match camera position and scale before comparing detail.
- Fridges roughly 1.7m tall; ordinary car around 4m long; workbench top around 0.9m. These are visual targets, not an instruction to break existing gameplay footprints or saved placements.
- Use actual low-poly silhouettes, coarse textures, worn paint, edge wear and warm daylight. Do not replace readable geometry with plain blocks or try to achieve photorealism with excessive meshes or textures.
- Main palette remains muted olive, rust, ivory and charcoal. Outdoors must stay readable in shadow. Keep fog beyond useful gameplay distance.
- Preserve current progression: according to current AGENTS.md, basic belts/storage at level1, junctions at3, faster Tier2 at5; primary/export at12. The old overhead image's level10 labels are historical. No level numbers are specified by the new visual targets.

## Prompt to send with the images

Read these three environment concept images and this README. Improve the current compact yard to match their scale, composition, silhouettes, ground treatment and lighting. Preserve current gameplay, freely placed equipment, saves, controls/settings, metadata and progression. Do not scaffold another game or hardcode the example factory layout. Keep only infrastructure fixed, and keep the starter floor mostly open.

First inspect the current yard and identify concrete mismatches. Then implement improvements in coherent passes: boundaries/ground/light; office/workshop/scenery; recognizable production props and port connections. Reuse authored assets and textures where suitable. Preserve custom worlds and occupied player footprints; do not silently overwrite scenes or reset saves.

Use separate agents for environment/lighting, props/textures, and integration/verification if available, with non-overlapping ownership. The main agent integrates and checks dimensions, usable buildable space, interaction targets, colliders, belt endpoints and performance.

Capture actual Unity views from comparable camera positions and compare against these references. If Unity is unavailable, use clearly labeled renders of real exported assets, document differences, and leave Unity visual acceptance unverified. Do not claim generated reference images are completed implementation. Update the handoff with the files changed, checks run and remaining mismatches.
