# Original retro surface textures

Seven original procedural **64 × 64 RGB PNGs**, with tracked Unity import metadata and corresponding shared URP Lit materials under `Assets/Scrapshift/Resources/ScrapshiftMaterials`. No external photographs, downloaded texture packs, paid assets, or generated campaign illustrations are used.

| Surface | Intended use |
| --- | --- |
| RustPaint | Mismatched olive-painted machine panels worn through to rust |
| DarkMetal | Motors, rollers, tool heads, fence fittings |
| CorrugatedMetal | Workshop cladding and roof, vertical repeating ribs |
| WeatheredWood | Crate planks, bench top, selling counter, vertical grain |
| Gravel | Packed gravel yard and small earth patches |
| Copper | Recovered material bundles and visible stripped wire |
| WireInsulation | Dark insulated wire and cables |

The muted palette starts from ivory `#E8DDC4`, rust `#B75935`, olive `#69735B`, and charcoal `#202927`. Contrast is deliberately restrained. All noise fields wrap periodically and opposite edge pixels match exactly. Rib and grain patterns run along the texture's V axis.

Textures import as sRGB, uncompressed, **Point** filtered, **Repeat** wrapped, without mipmaps. At 64 texels per metre, each tile represents roughly one metre of surface. Use geometry-scaled face UVs (one repeat per metre) for boxes, fences, ground, and workshop panels, rather than stretching one tile across every primitive. Small props may use a modest minimum UV span so strands remain legible. The original twelve-sided cylinders also use metre-based circumference/length UVs, avoiding stretched primitive mappings. Base material scale is `(1, 1)` so shared materials are never modified per object. Texture detail is intentionally subtle enough to remain readable without optional screen pixelation.

`RetroMaterialLibrary.Get(RetroSurface)` loads the tracked Resources material once and shares it. Do not mutate its texture scale or color for an individual prop. Resources assets reference the actual URP shader, retaining that shader in player builds. The URP Lit GUID `933532a4fcc9baf4fa0491de14d08ed7` was checked against Unity Technologies' official Graphics repository `Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader.meta`. The project's pinned URP package remains unchanged.

**Scrapshift → Validate Retro Materials** checks import settings and recreates a missing material using the imported URP Lit shader. It preserves existing material values, assignments, shader choices, and asset GUIDs. Existing hand-edited materials are not reset.

The optional `generate_textures.py` source uses deterministic seeded, periodic noise and analytic patterns. Run it with Python 3 and Pillow to intentionally regenerate PNG pixels; it does not rewrite materials or metadata. Pillow is an authoring dependency only, not a Unity or player runtime requirement.

Cloud checks validate PNG resolution/format, matching edges, shader/texture GUID references, and metadata. Unity is unavailable here: import, shader rendering, point-filter readability, physical texel density, distant shimmer, and desktop shader inclusion still require local editor/build inspection.
