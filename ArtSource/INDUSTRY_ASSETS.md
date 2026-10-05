# Original industrial machinery

The Stage D pack adds two original models. Earlier models, textures, materials,
audio and GUIDs are retained. Neither model contains downloaded Asset Store art.
They reuse the existing 512px `ScrapshiftPropAtlas.png` and recovered shared
`ScrapshiftMaterials/PropAtlas` material.

| Resource model | Conservative footprint | Actual source height | Triangles | Purpose |
| --- | --- | --- | --- | --- |
| CompactPrimaryScrapper | 6 × 7m | 4.267m | 3,032 | Open scrap cradle, hydraulic gantry, visible press rods, power reservoir, finned motor, operator console and component discharge rollers |
| CompactExportStation | 3 × 2.5m | 2.05m | 1,612 | Weigh-and-dispatch platform, pallet cage, material receiving rollers, mechanical scale head and weigh docket |

The primary's southern opening is for whole scrap; its northern component port
is at 0.7m. The export station receives portable materials from the south at
0.7m. Whole cars and refrigerators are presentation instances inside the
primary's cradle, never portable conveyor items. Standing intake is contracted
machinery delivery; these assets do not implement vehicle or road AI.

From the repository root, regenerate **only this pack**:

```sh
blender -b --python-exit-code 1 --python ArtSource/build_industry_assets.py
blender -b --python-exit-code 1 --python ArtSource/audit_industry_assets.py
python3 Tests/audit-industry-assets.py
blender -b --python-exit-code 1 --python ArtSource/preview_industry_assets.py
```

The builder never writes the existing atlas or previous asset manifests. New
metadata must retain its existing GUIDs. The independent manifest stores source
dimensions and budgets; the round-trip audit stores the actual exported FBX
hashes. Packaging checks fail if binary files differ from that audit.

`Design/Previews/IndustrialMachineryAssetPreview.png` is a Blender render of the
actual exported meshes and original car/fridge using the tracked atlas. It is
an asset preview, **not Unity gameplay or evidence of engine rendering**.

`CompactIndustryVisuals.BuildEquipment` scales the authored body and physical
footprint to editable catalogue dimensions, uses the shared transport-port
geometry, and supplies actual scaled cable sockets. Real equipment has one
conservative collider; construction ghosts have none. `SyncJob` creates one
cached original whole-object view per durable primary job identity and removes
it when the job ends. Imported source meshes/materials remain untouched.
`Step` receives the coordinator's unpaused delta, game time and running flag.
Timing marks and shallow clamps freeze under pause, missing power or blocked
production. There are no per-object Update callbacks, new lights, rigidbodies
or processing/transaction logic in the presentation helper.

The supplied 12 `CompactIndustryVisualTests` cases require local Unity. Native
compilation/import, collider and targeting feel, sign/socket/port appearance,
actual paused motion, destruction lifecycle, draw calls and frame times,
shader inclusion and a desktop build remain unverified in Cloud.
