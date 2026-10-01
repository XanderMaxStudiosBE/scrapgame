using UnityEngine;

namespace Scrapshift
{
    /// <summary>Original, low-poly station geometry; transactions remain in PrototypeGame.</summary>
    public static class WireStripperVisual
    {
        public sealed class Result
        {
            public GameObject root;
            public Transform rotor, additionalRoller, outputDisplay, feedDisplay;
            public Renderer statusLamp;
        }

        /// <param name="position">Ground anchor. The input and output face negative local Z.</param>
        public static Result Build(Transform parent, Vector3 position)
        {
            var result = new Result();
            result.root = new GameObject("Powered wire stripper / input and output");
            var root = result.root.transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            result.root.AddComponent<InteractionTarget>().kind = TargetKind.Machine;

            // Preserve the original collision footprint regardless of model import/fallback.
            var chassis = result.root.AddComponent<BoxCollider>();
            chassis.center = new Vector3(0, .60f, .04f); chassis.size = new Vector3(2.35f, 1.20f, 1.22f);
            if (!AuthoredYardProps.TryPlace("PoweredStripper", root, Vector3.zero, out GameObject authored))
                BuildLegacyChassis(root);
            result.rotor = Roller("Upper driven feed wheel", root, new Vector3(.58f, 1.19f, -.755f), .15f, .17f);
            result.additionalRoller = Roller("Lower feed wheel", root, new Vector3(.58f, .90f, -.755f), .12f, .17f);

            result.outputDisplay = new GameObject("Collectable copper / visual only").transform;
            result.outputDisplay.SetParent(root, false);
            result.outputDisplay.localPosition = new Vector3(-.61f, .76f, -.96f);
            for (int i = 0; i < 5; i++)
                Part("Recovered copper strand", result.outputDisplay, new Vector3((i - 2) * .105f, 0, 0), new Vector3(.065f, .10f, .40f), RetroSurface.Copper);
            result.feedDisplay = new GameObject("Wire in feed opening / visual only").transform;
            result.feedDisplay.SetParent(root, false);
            result.feedDisplay.localPosition = new Vector3(.58f, 1.035f, -.90f);
            for (int i = 0; i < 3; i++)
                Part("Wire in rollers", result.feedDisplay, new Vector3((i - 1) * .10f, 0, 0), new Vector3(.06f, .075f, .48f), RetroSurface.WireInsulation);
            result.outputDisplay.gameObject.SetActive(false);
            result.feedDisplay.gameObject.SetActive(false);

            result.statusLamp = YardGeometry.Box("Status lamp", root, new Vector3(.90f, 1.44f, -.546f), new Vector3(.17f, .11f, .04f), YardGeometry.Ivory, false).GetComponent<Renderer>();
            Label(root, "WIRE IN", new Vector3(.58f, .67f, -.665f));
            Label(root, "COPPER OUT", new Vector3(-.61f, .57f, -1.34f));
            Label(root, "POWER", new Vector3(.40f, 1.44f, -.548f));
            return result;
        }

        static void BuildLegacyChassis(Transform root)
        {
            // One solid chassis collider, resolved through the parent's station marker.
            Part("Heavy chassis", root, new Vector3(0, .60f, .04f), new Vector3(2.35f, 1.20f, 1.22f), RetroSurface.RustPaint);
            Part("Raised top plate", root, new Vector3(0, 1.26f, .04f), new Vector3(2.48f, .10f, 1.36f), RetroSurface.DarkMetal);
            Part("Lower plinth", root, new Vector3(0, .12f, .04f), new Vector3(2.54f, .22f, 1.44f), RetroSurface.DarkMetal);
            Part("Mismatched service door", root, new Vector3(-.61f, .69f, -.618f), new Vector3(.92f, .84f, .045f), RetroSurface.CorrugatedMetal);
            Part("Input panel", root, new Vector3(.58f, .8f, -.626f), new Vector3(1.14f, .99f, .055f), RetroSurface.RustPaint);
            for (int i = 0; i < 4; i++)
                Part("Service cooling vent", root, new Vector3(-.60f, .69f + i * .11f, -.651f), new Vector3(.59f, .035f, .022f), RetroSurface.DarkMetal);
            foreach (float x in new[] { -1.02f, 1.02f })
                foreach (float z in new[] { -.51f, .57f })
                    Part("Bolted rubber foot", root, new Vector3(x, .055f, z), new Vector3(.3f, .11f, .29f), RetroSurface.WireInsulation);

            // The dark recess, wide funnel edges and fluted wheels establish a visible feed mouth.
            Part("Black feed opening", root, new Vector3(.58f, 1.03f, -.672f), new Vector3(.92f, .38f, .05f), RetroSurface.DarkMetal);
            Part("Feed funnel left", root, new Vector3(.07f, 1.05f, -.75f), new Vector3(.09f, .50f, .23f), RetroSurface.DarkMetal);
            Part("Feed funnel right", root, new Vector3(1.09f, 1.05f, -.75f), new Vector3(.09f, .50f, .23f), RetroSurface.DarkMetal);
            Part("Feed lip", root, new Vector3(.58f, .82f, -.80f), new Vector3(1.10f, .055f, .4f), RetroSurface.DarkMetal);
            // Exposed side-mounted motor and cable make the upgrade read as powered machinery.
            var motor = new GameObject("Exposed electric motor").transform;
            motor.SetParent(root, false);
            motor.localPosition = new Vector3(-1.35f, .83f, .14f);
            Cylinder("Motor housing", motor, Vector3.zero, .25f, .6f, RetroSurface.DarkMetal);
            Cylinder("Motor end cap", motor, new Vector3(0, 0, -.33f), .29f, .06f, RetroSurface.RustPaint);
            for (int i = 0; i < 6; i++)
                Cylinder("Motor cooling fin", motor, new Vector3(0, 0, -.24f + .09f * i), .275f, .025f, RetroSurface.DarkMetal);
            Part("Motor mount", root, new Vector3(-1.3f, .51f, .14f), new Vector3(.5f, .12f, .73f), RetroSurface.DarkMetal);
            Part("Electrical junction box", root, new Vector3(-.82f, 1.4f, .30f), new Vector3(.33f, .22f, .36f), RetroSurface.DarkMetal);
            Part("Power cable riser", root, new Vector3(-1.03f, 1.11f, .25f), new Vector3(.035f, .45f, .035f), RetroSurface.WireInsulation);
            Part("Power cable to motor", root, new Vector3(-1.17f, .89f, .25f), new Vector3(.31f, .035f, .035f), RetroSurface.WireInsulation);

            // Offset output tray stays visible from the same face as the input.
            Part("Output chute", root, new Vector3(-.61f, .81f, -.68f), new Vector3(.66f, .10f, .22f), RetroSurface.DarkMetal);
            Part("Copper collection tray", root, new Vector3(-.61f, .66f, -.96f), new Vector3(.91f, .07f, .72f), RetroSurface.DarkMetal);
            Part("Tray left edge", root, new Vector3(-1.04f, .74f, -.96f), new Vector3(.045f, .16f, .72f), RetroSurface.RustPaint);
            Part("Tray right edge", root, new Vector3(-.18f, .74f, -.96f), new Vector3(.045f, .16f, .72f), RetroSurface.RustPaint);
            Part("Tray front edge", root, new Vector3(-.61f, .72f, -1.3f), new Vector3(.91f, .11f, .045f), RetroSurface.RustPaint);
            Part("Status indicator bezel", root, new Vector3(.90f, 1.43f, -.45f), new Vector3(.27f, .20f, .16f), RetroSurface.DarkMetal);
            for (int i = 0; i < 4; i++)
                Part("Front panel bolt", root, new Vector3(i < 2 ? -.98f : 1.07f, i % 2 == 0 ? .33f : 1.19f, -.671f), new Vector3(.05f, .05f, .025f), RetroSurface.DarkMetal);
        }

        static GameObject Part(string name, Transform parent, Vector3 position, Vector3 size, RetroSurface surface, bool collider = false)
        {
            return YardGeometry.SurfaceBox(name, parent, position, size, surface, collider);
        }

        static Transform Roller(string name, Transform parent, Vector3 position, float radius, float depth)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            if (AuthoredYardProps.TryPlace("FeedRoller", pivot, Vector3.zero, out GameObject wheel))
            {
                wheel.transform.localScale = new Vector3(radius / .15f, radius / .15f, depth / .17f);
                return pivot;
            }
            Cylinder("Fluted steel roller", pivot, Vector3.zero, radius, depth, RetroSurface.DarkMetal);
            // Uneven ivory notches clearly communicate rotation without glossy reflections.
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * .25f;
                var notch = YardGeometry.Box("Roller tooth", pivot, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -depth * .53f), new Vector3(.045f, .045f, .035f), i % 2 == 0 ? YardGeometry.Ivory : YardGeometry.Charcoal, false);
                notch.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            }
            Cylinder("Axle copper bushing", pivot, new Vector3(0, 0, -depth * .6f), .043f, .025f, RetroSurface.Copper);
            return pivot;
        }

        // Twelve-sided cylinder along local Z. UV coordinates use metres, matching SurfaceBox.
        static void Cylinder(string name, Transform parent, Vector3 position, float radius, float depth, RetroSurface surface)
        {
            const int sides = 12;
            var vertices = new Vector3[(sides + 1) * 2 + sides * 6];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[sides * 12];
            for (int i = 0; i <= sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                for (int end = 0; end < 2; end++)
                {
                    int index = i * 2 + end;
                    vertices[index] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, (end - .5f) * depth);
                    uv[index] = new Vector2(i * Mathf.PI * 2 * radius / sides, end * depth);
                }
            }
            int t = 0, v = (sides + 1) * 2;
            for (int i = 0; i < sides; i++)
            {
                int a = i * 2;
                triangles[t++] = a; triangles[t++] = a + 2; triangles[t++] = a + 1;
                triangles[t++] = a + 2; triangles[t++] = a + 3; triangles[t++] = a + 1;
                for (int end = 0; end < 2; end++)
                {
                    float z = (end - .5f) * depth;
                    vertices[v] = new Vector3(0, 0, z);
                    vertices[v + 1] = vertices[a + end];
                    vertices[v + 2] = vertices[a + 2 + end];
                    uv[v] = Vector2.zero;
                    uv[v + 1] = new Vector2(vertices[v + 1].x, vertices[v + 1].y);
                    uv[v + 2] = new Vector2(vertices[v + 2].x, vertices[v + 2].y);
                    triangles[t++] = v; triangles[t++] = end == 0 ? v + 2 : v + 1; triangles[t++] = end == 0 ? v + 1 : v + 2;
                    v += 3;
                }
            }
            var mesh = new Mesh { name = name + " / metre UV" };
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<ProceduralMeshOwner>().mesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = RetroMaterialLibrary.Get(surface);
        }

        static void Label(Transform parent, string text, Vector3 position)
        {
            var plate = YardGeometry.Box(text + " plate", parent, position + new Vector3(0, 0, .007f), new Vector3(.62f, .135f, .018f), YardGeometry.Charcoal, false);
            YardGeometry.Sign(parent, text, position + new Vector3(0, 0, -.018f));
            var label = parent.GetChild(parent.childCount - 1).GetComponent<TextMesh>();
            label.characterSize = .020f;
            plate.name = text + " / readable label";
        }
    }
}
