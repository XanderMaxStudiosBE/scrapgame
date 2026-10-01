# SCRAPSHIFT original asset source

The user chose worn retro realism and supplied Retro Rewind - Video Store Simulator as a style reference. SCRAPSHIFT uses its own industrial subjects and original meshes/textures. No reference-game assets, screenshots, trademarks or characters are embedded in the game.

`build_yard_assets.py` authors and exports thirteen metre-scale FBX props (including separate fan frame/rotor meshes) in Blender 4.3.2, builds a shared 512 × 512 worn palette atlas, records triangle counts, and renders a contact sheet of those actual source meshes. The preview is **not Unity gameplay** and its studio light does not validate runtime lighting. Each prop currently has one shared atlas material and 176–1,684 triangles; coarse runtime collision volumes are assigned separately.

From the repository root:

```sh
blender --background --python-exit-code 1 --python ArtSource/build_yard_assets.py
blender --background --python-exit-code 1 --python ArtSource/audit_yard_assets.py
```

Generation overwrites the authored mesh/atlas/preview outputs and manifest, preserving existing `.meta` files. It never edits scenes, balance assets or user saves. Blender is an authoring dependency, not a runtime or local Unity requirement; exported FBX/PNG files are tracked. The script disables denoising because this Cloud Blender build has no OpenImageDenoise.

The audit reimports exported FBX through Blender and verifies metre dimensions, triangle counts, finite vertices and atlas UV bounds. Its JSON is evidence for **Blender round-trip only**, not Unity import. Unity's scoped `AuthoredPropImport` configures model axis conversion/readability, disables imported animation/lights/colliders/materials, and imports the atlas with bilinear filtering/mips. `AuthoredYardProps` assigns the tracked URP atlas material and keeps imported mesh ownership with Unity. The existing procedural builders remain as a clearly logged fallback; a successful Unity import should use authored meshes.

The thirteen Unity asset-import tests require real FBX imports, URP materials, source vertices for runtime batching, metre-scale vertical bounds and no unintended model colliders. Run them locally before claiming engine compatibility or performance. Inspect roof joins, glazing placement, hull/wheel shape, shadow readability, visual collision alignment and preserved station approaches.
