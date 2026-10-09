# Reproducible reference presentation checks

These runners compile and execute the actual tracked runtime builders against a
controlled C# Unity API/value adapter. They are source geometry and branch checks,
not Unity compilation, import, rendering, collision, static batching or build evidence.
The native `CompactReferenceMachineryTests` and `CompactReferenceEnvironmentTests`
remain separate Unity EditMode cases.

Run from the repository root with `mcs` and `mono` on PATH:

```sh
Tests/run-machinery-presentation.sh
Tests/run-reference-environment.sh
Tests/run-machinery-presentation.sh --export /tmp/scrapshift-machinery.json
blender -b --python-exit-code 1 --python ArtSource/preview_reference_machinery.py -- --source-json /tmp/scrapshift-machinery.json
```

For an unpacked Mono toolchain, set `SCRAPSHIFT_MONO_ROOT` to its containing
directory (the directory with `usr/bin/mono-sgen` and `usr/lib/mono/4.5/mcs.exe`).
The Blender montage also needs Pillow available to `python3`.

The machinery runner executes all nine equipment kinds, four right-angle yaws,
three editable footprint variants, real/ghost views, eight bent belt routes and
one straight route. It checks exact authoritative mouth positions and roles,
actual arrow winding/direction, deck/roller heights, matching machine/belt sleeve
terminal faces, fitting bounds, idempotent bounded assemblies, cargo arc length,
industrial cached load/motion gates, private mesh identity and owner cleanup.

The environment runner executes actual `CompactYardWorld.Build` with combining
off and on. It checks the retained anchors/19 infrastructure colliders, 21
private cover boxes inside the office reservation, one cover roof caster,
36 boundary screens/41 rails, upward wheel-lane triangles, metre-based texture
travel and private cleanup. Authored fixture bounds are excluded from acceptance.

The adapter substitutes authored FBX envelopes, material loading, fonts and world
dressing. Ground texture material creation, backdrop, chain-link replacement and
static batching use narrow fixtures. Destruction callbacks execute immediately;
Unity's deferred frame timing is unverified. The export explicitly excludes
authored envelope meshes and writes separate authored model markers. The Blender
script imports the actual tracked FBXs at those marker transforms, alongside
actual runtime mesh vertices/UVs/triangles. Its lighting/materials/ground/fonts
are approximate; every panel labels itself as a Blender source preview.

The generated preview is `Design/Previews/ReferenceMachineryAssemblies.png`.
It is a review artifact, not gameplay or proof of creator visual acceptance.
