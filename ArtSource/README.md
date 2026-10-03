# SCRAPSHIFT original asset source

The original authored packs below remain intact. The separately credited **CC0 Old Tyre by MP/Poly Haven** is now used in compact scenery. Its sources/license/checksums are under `ThirdParty/PolyHaven/OldTyre`; run `blender --background --python-exit-code 1 --python ArtSource/prepare_old_tyre.py` to re-export only that 184-triangle mesh, then `python3 Tests/audit-old-tyre.py` for source/mask/material packaging checks. This third-party prop uses its own 1K UV maps instead of the original shared palette. See [integration and native checks](../Design/OLD_TYRE_VERIFICATION.md). Store pack files stay local/private; they are not part of the original authored pack.

The user chose worn retro realism and supplied Retro Rewind - Video Store Simulator as a style reference. SCRAPSHIFT uses its own industrial subjects and original meshes/textures. No reference-game assets, screenshots, trademarks or characters are embedded in the game.

`build_yard_assets.py` authors and exports nineteen metre-scale FBX props (including separate fan frame/rotor meshes) in Blender 4.3.2, builds a shared 512 × 512 worn palette atlas, records triangle counts, and renders a contact sheet of those actual source meshes. The preview is **not Unity gameplay** and its studio light does not validate runtime lighting. Each prop currently has one shared atlas material and 176–1,684 triangles; coarse runtime collision volumes are assigned separately.

From the repository root:

```sh
blender --background --python-exit-code 1 --python ArtSource/build_yard_assets.py
blender --background --python-exit-code 1 --python ArtSource/audit_yard_assets.py
```

Generation overwrites the authored mesh/atlas/preview outputs and manifest, preserving existing `.meta` files. It never edits scenes, balance assets or user saves. Blender is an authoring dependency, not a runtime or local Unity requirement; exported FBX/PNG files are tracked. The script disables denoising because this Cloud Blender build has no OpenImageDenoise.

The audit reimports exported FBX through Blender and verifies metre dimensions, triangle counts, finite vertices and atlas UV bounds. Its JSON is evidence for **Blender round-trip only**, not Unity import. Unity's scoped `AuthoredPropImport` configures model axis conversion/readability, disables imported animation/lights/colliders/materials, and imports the atlas with bilinear filtering/mips. `AuthoredYardProps` assigns the tracked URP atlas material and keeps imported mesh ownership with Unity. The existing procedural builders remain as a clearly logged fallback; a successful Unity import should use authored meshes.

The nineteen Unity asset-import tests require real FBX imports, URP materials, source vertices for runtime batching, metre-scale vertical bounds and no unintended model colliders. Run them locally before claiming engine compatibility or performance. Inspect roof joins, glazing placement, hull/wheel shape, shadow readability, visual collision alignment and preserved station approaches.

## Original soundscape

Run `python3 ArtSource/build_yard_audio.py` to regenerate eight deterministic mono PCM sounds: gravel steps, tool scrape/click, bundle handling, sale chime, stripper motor, desk-fan air and outdoor breeze with sparse bird chirps and an original radio listening-test phrase. These are original mathematical synthesis, with no sampled audio. Clips are 22,050 Hz, 0.18–16 seconds, bounded below clipping and tracked in Resources. Four runtime sources provide effects, nearby stripper/appliance loops and gentle ambience. No audio is synthesized per frame. Listening/mixing and import require Unity verification.

`blender --background --python-exit-code 1 --python ArtSource/render_workshop_details.py` reimports exported workbench, stripper chassis, buyer scale, fan and bundle FBXs, assigns their original atlas and renders a close-up inspection sheet. It does not modify models or any Unity scene. Both previews use studio lighting and are explicitly asset previews, not gameplay. Separate rotor meshes are integrated by runtime code.

The Blender audit records each FBX SHA-256. `python3 Tests/audit-original-assets.py` uses the standard library to verify matching audit hashes, binary FBX headers, budgets, atlas dimensions, PCM samples, complete/unique metadata and assembly JSON. Re-exporting any FBX invalidates its previous audit hash until the Blender import audit is run again. This portable packaging check does not replace Unity import/rendering.

## Workshop lighting composition study

`blender --background --python-exit-code 1 --python ArtSource/render_yard_lighting_study.py` reimports original exported meshes and the real atlas/gravel texture, using colors from the tracked CozyAfternoon profile. It writes `Assets/Scrapshift/Art/Previews/YardLightingStudy.png`. It is a **Blender composition study, not Unity gameplay or shader/FPS verification**. Blender lights/tone mapping and a broad area fill approximate ambient readability; they do not execute the runtime sky/contact shader, URP presets or exactly reproduce the Unity rig. The additional study fill is not a runtime light.

## Portable radio

`blender --background --python-exit-code 1 --python ArtSource/build_yard_assets.py -- --only PortableRadio` exports only the selected original model, retaining other FBXs and their manifest entries. The common deterministic atlas is still regenerated; existing metadata stays intact. Run the FBX audit after any export. The new radio has 1,308 triangles, worn blue/cream housing, slatted speaker, amber tuning scale, knobs, handle and aerial.

`blender --background --python-exit-code 1 --python ArtSource/render_appliance_details.py` reimports the actual fan/radio FBXs and atlas to render `ApplianceDetails.png`. It is an inspected Blender asset preview, not Unity gameplay, collision or lighting evidence. The older workshop/contact sheets are unchanged and do not contain the new portable radio. Unity import/collision/audio tests are supplied but remain unrun in Cloud.

## Additive concept-world pack

`build_world_surfaces.py` authors five new deterministic PNGs under `Resources/ScrapshiftWorld`: the worn-color WorldAtlas, linear metallic/smoothness mask, localized warm-glass emission, irregular ground-layer atlas and cutout chain-link wire. It writes missing shared URP materials while retaining any existing material edits. It never rewrites legacy textures/materials. Regenerating the new pack's PNGs will overwrite texture edits, so retain intentional texture changes before rerunning.

`build_world_assets.py` imports the reusable original geometry helpers without running the legacy exporter or saving its atlas. It exports only the twelve new metre-scale models: WorkshopDetails, ToolWall, RepairTools, ApplianceRow, SalvageShelter, OfficeDetails, IndustrialWorks, PoplarTree, FluorescentFixture, FencePost, TealContainer and WeedClump. TealContainer reuses the proven container geometry with the new blue palette. Other models are newly authored here. Each has one atlas material; 72–3,708 triangles, 13,364 across all twelve unique meshes. Existing manifest entries and `.meta` files survive targeted exports. `-- --only ToolWall` limits regeneration to that model; run the round-trip audit after every export.

```sh
python3 ArtSource/build_world_surfaces.py
blender --background --python-exit-code 1 --python ArtSource/build_world_assets.py
blender --background --python-exit-code 1 --python ArtSource/audit_yard_assets.py
python3 Tests/audit-original-assets.py
python3 Tests/audit-world-assets.py
MESA_SHADER_CACHE_DIR=/tmp/scrapshift-mesa blender --background --python-exit-code 1 --python ArtSource/render_world_pass.py
```

The final command reimports actual FBXs, source textures and the same `WorldDressing.json` used by runtime builders, then renders four labelled Blender previews. Use `-- WorldOverview`, `-- WorkshopWorldDetails` `-- ApplianceLane` or `-- OfficeWorldDetails` to render only one view. Blender Eevee software rendering and a broad preview-only fill approximate the unoccluded Unity ambient lighting; URP sky/fog/contact shader, UI and tone mapping are not executed. Engine rendering, import axes, collisions and FPS require local Unity checks. Some existing signs/dynamic stock/contact details are omitted from the composition study. The resource layout is decorative data, independent of gameplay saves. See `WORLD_PASS_VERIFICATION.md` for source constraints and the actual local acceptance checklist.

## Screenshot-driven recovery and authoring

`python3 ArtSource/build_material_catalog.py` regenerates twelve direct original-material texture bindings independently of material saved properties, retaining metadata/GUIDs. It never edits original material files. New world materials generated by `build_world_surfaces.py` use `material_compatibility.py` to serialize public URP legacy aliases and straight-alpha factors; existing tuned materials remain skipped. Run `python3 Tests/audit-material-bindings.py` to check references, rejection of corrupt GUIDs, alias preservation and fresh generation in an isolated checkout. These are data tests, not Unity upgrader execution.

`render_world_pass.py -- EyeHeightForecourt` reimports actual meshes/textures and the current shared layout at eye height. It uses Blender’s approximate lighting, not Unity/Laptop shaders or HUD; the image carries that disclosure. FINISHING_PASS_VERIFICATION.md distinguishes the original material-loss hypothesis from the unrun engine tests and real visual/FPS checks.

## Compact automation machinery

`build_automation_assets.py` authors four original metre-scale models: CompactPortedStorage, CompactTier2Scrapper, CompactSplitter and CompactMerger. They reuse the existing PropAtlas without regenerating old models or textures. The independent automation manifest records 5,824 triangles total (maximum 2,184/model), one shared material each. Preserve tracked metadata and rerun the round-trip audit after any re-export.

```sh
blender --background --python-exit-code 1 --python ArtSource/build_automation_assets.py
blender --background --python-exit-code 1 --python ArtSource/audit_automation_assets.py
python3 Tests/audit-automation-assets.py
```

Runtime conveyor tracks/supports/rollers/arrows use three combined owned meshes per link, following the same core port path used by simulation. These are independent of the model pack's triangle counts. Unity import/material/axes and full-network performance remain local checks; see Design/COMPACT_STAGE_C_VERIFICATION.md.
