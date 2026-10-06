# Original starter-stock fixtures

The three original SCRAPSHIFT fixtures were built for the creator's request for stocked working routes. No downloaded or Asset Store model is embedded in this pack. The authoring helpers are the repository's original cube/cylinder/beam/ring/export helpers; the small spanner/pliers primitives are standalone here because the older world authoring script performs exports when imported.

| Resource | Triangles | Intended silhouette |
| --- | ---: | --- |
| CompactPartsShelf | 1,600 | Open angle-iron shelves, old batteries, trays, motor, cases, grease can and hanging hose |
| CompactMixedSkip | 632 | Open reinforced scrap bin, bent sheets, wheel hub, appliance controls and steel rod |
| CompactWorkshopRack | 1,380 | Freestanding tool wall, spanners/pliers/hammer/drivers, parts drawers, tins and copper lead |

All models use metre scale, grounded pivots, finite imported normals, nondegenerate faces and one retained `ScrapshiftPropAtlas.png` material. Separate `starter_stock_manifest.json` and `starter_stock_audit.json` prevent collisions with other authoring packs. Existing `.meta` GUIDs survive rebuilds; new files receive a GUID only when absent. The builder writes only these three FBXs and its own manifest; it does not regenerate the atlas or previous props.

Reproduce with Blender 4.3+:

```bash
blender -b --python-exit-code 1 --python ArtSource/build_starter_stock.py
blender -b --python-exit-code 1 --python ArtSource/audit_starter_stock.py
python3 Tests/audit-starter-stock.py
blender -b --python-exit-code 1 --python ArtSource/preview_starter_stock.py
```

`preview_starter_layout.py` reads `Design/Previews/StarterStockLayout.json`, exported from the actual C# clutter hierarchy and fresh-state occupancy using the retained cloud value adapter. Source transforms and authored FBXs are actual; Blender's lighting, ground/context and procedural texture projection are approximate. The preview visibly declares that it is not Unity gameplay. Runtime shader/importer/batching/physics and frame rate require local Unity verification.

The broader yard additionally reuses the existing original salvage and machinery models and already attributed CC0 Poly Haven OldTyre; their provenance documents remain authoritative. This pack introduces no new third-party dependency or licence purchase.
