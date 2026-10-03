# Local Junkyard Car Parts integration — 2026-10-03

Inspected the creator's `fghfgfhfg.unitypackage` as data, without importing or executing its scripts. Of 143 asset paths, 141 are existing Scrapshift project files. The remaining paths are the `Assets/Junk Car Parts/Models` folder and **PistonSmooth.obj**. No other Store models, prefabs, MTL files or textures are included. The OBJ references a missing `PistonSmooth.mtl`; it is not a complete imported pack. The creator reports more packs installed locally, but those files remain unavailable here.

The provided OBJ was inspected privately outside the checkout. It has 644 positions, 1,250 UVs, 2,150 normals and 1,288 triangles across three object/material groups, with finite values and valid face indices. None of those vendor source files, previews or uploaded game scripts are committed. The current implementation and original assets are preserved.

## Local setup and behavior

After pulling with Unity closed, open the existing compact scene. With Play stopped, run **Scrapshift → Enable Imported Car Parts**, then press Play. This requires the locally imported `Junkyard Car Parts` piston. Source lookup uses its original GUID so folder renames work; the original OBJ path is a fallback. Missing models produce a bounded menu warning and leave the original prop available.

The editor temporarily enables the source model's CPU Read/Write flag, combines triangle submeshes in root coordinates, preserves their normals/UVs, fits the geometry uniformly into a .6m height/.6 × .55m envelope, centres it horizontally and sets its bottom to zero. It validates finite transformed vertices, at most 1,500 triangles/4,500 vertices and one readable combined submesh. The generated Mesh goes to the already ignored `Assets/Scrapshift/Generated/Resources/LocalScrapshiftProps/Piston.asset`. An existing local output is retained, including invalid/custom assets; it is not overwritten. Readability is restored in `finally`, with an explicit warning if reimport cannot restore the setting. Model import flags apart from temporary readability are not deliberately changed.

Runtime loads that optional Mesh and uses the existing recovered **URP DarkMetal** material. It replaces one original CompactMotor in the **office spares pocket**, keeping the compressor/pallet and existing occupancy/collision/visibility pooling. Missing, unreadable, empty, oversized or malformed local geometry retains the original motor. Resources are looked up again on the next world build, so a previous miss is not cached permanently. The shared imported mesh belongs to Unity and gets no runtime destruction owner. There is no simulation, physics, collider, extra light, material clone or per-frame callback on the piston.

The supplied geometry fits approximately .300 × .600 × .300m; at -20° its footprint is approximately .384m square, inside the original service envelope. It is intentionally large enough to read as workshop stock and still needs visual acceptance. The one combined renderer replaces one prior renderer. Estimated total clutter rises from 34,860 to **35,452 triangles**, adding 592 (old motor 696 → piston 1,288); the native budget guard is now 36,000. A different accepted local mesh can have at most 1,500 triangles, keeping the estimate below that limit. No FPS improvement is claimed.

## Executed checks and limits

- Independent package/model review confirms contents, finite indexed OBJ geometry, absent material dependencies, uniform scale, yawed footprint and budget arithmetic.
- Runtime/helper/editor/clutter code compiles with Mono warnings-as-errors against narrow Unity/API adapters. Actual `PrepareModel` normalization executes on the supplied OBJ positions/counts against mesh-value adapters: floor/centre/scale and source preservation pass. Invalid/missing/unreadable/excess geometry rejection and missing-resource fallback pass. Actual menu missing-source, retained-file and Play-mode guards pass with controlled asset APIs and a real temporary filesystem. These are not Unity import or native asset database/batching execution.
- Five changed/new C# syntax parses, existing original/CC0 tyre packaging checks and metadata completeness pass. Previous tracked art/material/meta/scene/data files remain unchanged.
- Git ignore checks cover the known Store source folder, its sibling meta, and the generated mesh/meta. Public staging contains setup/runtime/tests/docs only; private OBJ/package content is absent.
- Three supplied Unity tests cover child/root transform normalization/source preservation, preallocation triangle-budget rejection, and optional native Resources fallback/reuse/no-physics. Updated clutter budget/containment/batched occupancy checks must run locally. All native engine tests remain **unrun in Cloud**.

Required Unity checks: compile/import, run the menu, verify actual OBJ triangulation/normals/UVs, restore source Read/Write setting, save/restart and confirm generated Mesh readability/Resources inclusion, inspect the office piston/material/scale, test stock hide/reappear and static batching, then verify player build inclusion and Editor/player frame times. Existing gameplay/save/input behavior should remain unchanged. No finished game, runtime rendering or local visual acceptance is claimed.

## Sharing additional packs

In Unity 6, open **Window → Package Management → Package Manager → My Assets**. Send a screenshot of that list first so useful scrapyard packs can be selected. Appliances, worn car parts, barrels/pallets, industrial tools and fence/yard stock are priorities. Export selected vendor prefabs or folders as `.unitypackage` with dependencies; uncheck `Assets/Scrapshift` to avoid exporting the whole game assembly again. Keep files under 32 MiB per upload; large packs can be split into smaller prefab selections.

Keep vendor sources local/private: this GitHub repository is public, and Store “free” is not CC0. The known `Assets/Junk Car Parts` and `Assets/RetroStyleGames/LastGuns` source folders/sibling metadata and all Generated output are ignored. Other imported pack folders require their own scoped ignore entries before broad staging; their folder paths have not yet been supplied. Whole-project/Library/Temp uploads are unnecessary. No additional purchase is required for this integration.

The creator subsequently supplied a Package Manager screenshot listing **Industry Props**, **Low poly junkyard models pack**, **Junkyard Car Parts**, **Post Apocalyptic Motorcycle 3D Model Rigged Off Road**, **Metal Plates**, **Doomsday Pickup Truck - Game-Ready Post-Apocalyptic Car 3D Model**, **PSX Shader Kit**, **Simple Retro Car**, **Retro Cartoon Cars**, **DS Retro Television Set**, **ToonTastic - Electronic Devices**, and **8-bit SFX & UI Sound**. This is evidence of the local library list, not Cloud file availability, license/purchase totals or engine compatibility. Prioritize the first three for useful worn industrial props; inspect television, metal-plate and rusty-vehicle geometry/materials before adding them. Other packs remain unintegrated.

After the generic compilation banner and pasted `Gadd420.BuggiController` source, the creator supplied the actual error:

```text
Assets\RetroStyleGames\LastGuns\Base\Scripts\Buggy\BuggiController.cs(50,16): error CS1069: The type name 'WheelCollider' could not be found in the namespace 'UnityEngine'. This type has been forwarded to assembly 'UnityEngine.VehiclesModule' ... Enable the built in package 'Vehicles' in the Package Manager window to fix this error.
```

The controller also uses Unity 6 `Rigidbody.linearVelocity`/`linearDamping` and vendor helpers `Input_Manager`/`Input_Compat`. The helper files were not supplied and are absent from the Cloud checkout; no missing-helper error has been reported, so do not fabricate replacements.

The project manifest lacked `com.unity.modules.vehicles`, confirming the reported error's cause. [Unity 6000.3 documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.modules.vehicles.html) confirms that this built-in module supplies `WheelCollider`; enabled it at the standard built-in `1.0.0` package version, alongside the existing Physics module. Unity/pipeline versions and game input remain unchanged. JSON/preservation checks verify the additive declaration, but Unity is unavailable here: package resolution and compilation after the fix remain unverified. Additional Console errors, if any, require their own messages.

A later Console screenshot also shows CS1069 for `BikeController.cs` and `BuggiAntiRollBar.cs`, all referring to the same missing `WheelCollider` type. They share this module dependency; separate vendor script edits are unnecessary. The repeated Play-mode blocker messages are consequences of compilation failure.

Close Unity, pull the update, reopen and allow package resolution/compilation. If red errors remain, obtain the first error's full path/code/message from **Window → General → Console**. Do not fabricate input helper classes, delete packs or change pipelines based on the banner alone. The local piston setup cannot be used until Unity compilation succeeds.
