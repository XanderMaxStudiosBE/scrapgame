using UnityEngine;

namespace Scrapshift
{
    /// <summary>Original low-poly yard props. Dimensions are metres; textured boxes repeat once per metre.</summary>
    public static class YardProps
    {
        public sealed class BenchVisual
        {
            public GameObject root;
            public Transform materialDisplay;
        }

        public static GameObject Delivery(Transform parent, Vector3 groundPosition)
        {
            var root = StationRoot("Wire delivery crate", parent, groundPosition, TargetKind.Supply);
            Box("Crate bottom", root.transform, new Vector3(0, .18f, 0), new Vector3(2.3f, .22f, 1.3f), RetroSurface.WeatheredWood);
            // Open top and low front make the supplied wire visible from standing height.
            Box("Crate back", root.transform, new Vector3(0, .65f, .57f), new Vector3(2.3f, .85f, .16f), RetroSurface.WeatheredWood);
            Box("Crate front", root.transform, new Vector3(0, .43f, -.57f), new Vector3(2.3f, .45f, .16f), RetroSurface.WeatheredWood);
            foreach (float x in new[] { -1.07f, 1.07f })
                Box("Crate side", root.transform, new Vector3(x, .65f, 0), new Vector3(.16f, .85f, 1.3f), RetroSurface.WeatheredWood);
            foreach (float x in new[] { -.78f, .78f })
                Box("Metal reinforcing strap", root.transform, new Vector3(x, .43f, -.68f), new Vector3(.10f, .48f, .025f), RetroSurface.DarkMetal, false);
            for (int i = 0; i < 5; i++)
            {
                var bundle = new GameObject("Visible scrap wire").transform;
                bundle.SetParent(root.transform, false);
                bundle.localPosition = new Vector3((i - 2) * .37f, .54f + (i % 2) * .13f, -.02f);
                bundle.localRotation = Quaternion.Euler(0, (i % 2 == 0 ? -12 : 12), 0);
                for (int strand = 0; strand < 3; strand++)
                {
                    Box("Insulated wire", bundle, new Vector3((strand - 1) * .075f, 0, 0), new Vector3(.055f, .07f, .65f), RetroSurface.WireInsulation, false);
                    Box("Cut copper end", bundle, new Vector3((strand - 1) * .075f, 0, -.33f), new Vector3(.044f, .048f, .025f), RetroSurface.Copper, false);
                }
                Box("Bundle tie", bundle, Vector3.zero, new Vector3(.25f, .095f, .06f), RetroSurface.RustPaint, false);
            }
            return root;
        }

        public static BenchVisual Workbench(Transform parent, Vector3 groundPosition)
        {
            var root = StationRoot("Manual stripping workbench", parent, groundPosition, TargetKind.Bench);
            foreach (float x in new[] { -.97f, .97f })
                foreach (float z in new[] { -.47f, .47f })
                    Box("Workbench leg", root.transform, new Vector3(x, .5f, z), new Vector3(.14f, 1, .14f), RetroSurface.DarkMetal);
            Box("Lower tool shelf", root.transform, new Vector3(0, .28f, 0), new Vector3(2.05f, .1f, 1.05f), RetroSurface.WeatheredWood);
            Box("Heavy wooden tabletop", root.transform, new Vector3(0, 1.01f, 0), new Vector3(2.5f, .16f, 1.3f), RetroSurface.WeatheredWood);
            Box("Stripping work plate", root.transform, new Vector3(0, 1.105f, -.10f), new Vector3(1.1f, .035f, .75f), RetroSurface.DarkMetal, false);
            // A vise and a hand stripping tool distinguish this table from the buyer's scale.
            Box("Vise base", root.transform, new Vector3(-.86f, 1.13f, .23f), new Vector3(.44f, .1f, .35f), RetroSurface.DarkMetal, false);
            foreach (float z in new[] { .08f, .32f })
                Box("Vise jaw", root.transform, new Vector3(-.86f, 1.26f, z), new Vector3(.38f, .18f, .08f), RetroSurface.RustPaint, false);
            Cylinder("Vise screw", root.transform, new Vector3(-.86f, 1.2f, -.05f), .028f, .32f, RetroSurface.DarkMetal, Quaternion.Euler(90, 0, 0));
            var pliers = new GameObject("Hand stripping pliers").transform;
            pliers.SetParent(root.transform, false); pliers.localPosition = new Vector3(.84f, 1.12f, -.25f); pliers.localRotation = Quaternion.Euler(0, -25, 0);
            foreach (float x in new[] { -.04f, .04f })
            {
                Box("Tool grip", pliers, new Vector3(x, 0, -.1f), new Vector3(.045f, .045f, .25f), RetroSurface.WireInsulation, false);
                Box("Tool jaw", pliers, new Vector3(x, 0, .075f), new Vector3(.04f, .04f, .12f), RetroSurface.DarkMetal, false);
            }
            var display = Box("Bench wire and copper", root.transform, new Vector3(0, 1.25f, -.1f), Vector3.one, RetroSurface.WireInsulation, false);
            display.transform.localScale = new Vector3(.75f, .17f, .45f);
            return new BenchVisual { root = root, materialDisplay = display.transform };
        }

        public static GameObject Buyer(Transform parent, Vector3 groundPosition)
        {
            var root = StationRoot("Copper buying scale", parent, groundPosition, TargetKind.Sell);
            Box("Buying counter pedestal", root.transform, new Vector3(-.45f, .52f, 0), new Vector3(1.3f, 1.04f, 1.1f), RetroSurface.CorrugatedMetal);
            Box("Countertop", root.transform, new Vector3(-.45f, 1.08f, 0), new Vector3(1.5f, .12f, 1.3f), RetroSurface.DarkMetal);
            Box("Scale housing", root.transform, new Vector3(-.45f, 1.23f, -.05f), new Vector3(.78f, .20f, .65f), RetroSurface.RustPaint);
            Box("Scale platform", root.transform, new Vector3(-.45f, 1.36f, -.05f), new Vector3(.95f, .06f, .78f), RetroSurface.DarkMetal);
            Box("Scale display post", root.transform, new Vector3(-.45f, 1.49f, .39f), new Vector3(.09f, .65f, .09f), RetroSurface.DarkMetal);
            Box("Scale display housing", root.transform, new Vector3(-.45f, 1.78f, .34f), new Vector3(.55f, .3f, .14f), RetroSurface.RustPaint);
            Box("Scale display face", root.transform, new Vector3(-.45f, 1.78f, .26f), new Vector3(.44f, .2f, .02f), RetroSurface.WireInsulation, false);
            // Open collection bin on the right: the warm metal immediately identifies the buyer.
            var bin = new GameObject("Copper collection bin").transform;
            bin.SetParent(root.transform, false); bin.localPosition = new Vector3(.8f, 0, .08f);
            Box("Bin base", bin, new Vector3(0, .09f, 0), new Vector3(.9f, .18f, 1), RetroSurface.DarkMetal);
            foreach (float x in new[] { -.43f, .43f })
                Box("Bin side", bin, new Vector3(x, .45f, 0), new Vector3(.08f, .75f, 1), RetroSurface.RustPaint);
            foreach (float z in new[] { -.46f, .46f })
                Box("Bin end", bin, new Vector3(0, .45f, z), new Vector3(.9f, .75f, .08f), RetroSurface.RustPaint);
            for (int i = 0; i < 6; i++)
            {
                var scrap = Box("Sold copper stock", bin, new Vector3((i % 3 - 1) * .23f, .55f + (i / 3) * .1f, (i / 3 - .5f) * .28f), new Vector3(.16f, .10f, .50f), RetroSurface.Copper, false);
                scrap.transform.localRotation = Quaternion.Euler(0, (i % 2 == 0 ? -18 : 18), 0);
            }
            return root;
        }

        public static void Surroundings(Transform parent)
        {
            ScrapyardWorld.Build(parent);
        }

        public static void WorkshopSurroundings(Transform parent)
        {
            foreach (float x in new[] { -4.5f, -.5f })
                Box("Workshop rear pillar", parent, new Vector3(x, 1.6f, 5.2f), new Vector3(.15f, 3.2f, .15f), RetroSurface.DarkMetal);
            var roof = Box("Corrugated workshop roof", parent, new Vector3(-2.5f, 3.18f, 4.2f), new Vector3(4.6f, .12f, 3.2f), RetroSurface.CorrugatedMetal, false);
            roof.transform.localRotation = Quaternion.Euler(-5, 0, 0);
            Box("Workshop front beam", parent, new Vector3(-2.5f, 3.03f, 2.66f), new Vector3(4.6f, .16f, .13f), RetroSurface.RustPaint, false);
            // Clutter stays at the perimeter, leaving the four station approaches clear.
            for (int i = 0; i < 8; i++)
            {
                var scrap = Box("Stacked salvage panel", parent, new Vector3(-9 + i % 3 * .6f, .23f + i / 3 * .3f, 7 + i % 2 * .5f), new Vector3(.72f, .36f, .64f), i % 2 == 0 ? RetroSurface.RustPaint : RetroSurface.DarkMetal, false);
                scrap.transform.localRotation = Quaternion.Euler(0, i * 17, 0);
            }
            for (int i = 0; i < 3; i++)
            {
                Cylinder("Salvage drum", parent, new Vector3(9.3f, .5f, 6.7f + i * .8f), .34f, 1, i % 2 == 0 ? RetroSurface.RustPaint : RetroSurface.DarkMetal, Quaternion.identity);
            }
        }

        static GameObject StationRoot(string name, Transform parent, Vector3 position, TargetKind kind)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.localPosition = position;
            root.AddComponent<InteractionTarget>().kind = kind;
            return root;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, RetroSurface surface, bool collider = true)
        {
            return YardGeometry.SurfaceBox(name, parent, position, size, surface, collider);
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 position, float radius, float length, RetroSurface surface, Quaternion rotation)
        {
            const int sides = 12;
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localRotation = rotation;
            var vertices = new System.Collections.Generic.List<Vector3>();
            var uv = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 lowerA = new Vector3(Mathf.Cos(a) * radius, -length * .5f, Mathf.Sin(a) * radius);
                Vector3 lowerB = new Vector3(Mathf.Cos(b) * radius, -length * .5f, Mathf.Sin(b) * radius);
                Vector3 upperA = lowerA + Vector3.up * length, upperB = lowerB + Vector3.up * length;
                int index = vertices.Count;
                vertices.Add(lowerA); vertices.Add(upperA); vertices.Add(upperB); vertices.Add(lowerB);
                float u = a * radius, v = b * radius;
                uv.Add(new Vector2(u, 0)); uv.Add(new Vector2(u, length)); uv.Add(new Vector2(v, length)); uv.Add(new Vector2(v, 0));
                triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
                triangles.Add(index); triangles.Add(index + 2); triangles.Add(index + 3);
                AddCap(vertices, uv, triangles, upperA, upperB, length * .5f, true);
                AddCap(vertices, uv, triangles, lowerA, lowerB, -length * .5f, false);
            }
            var mesh = new Mesh { name = name + " / twelve sides, metre UVs" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<ProceduralMeshOwner>().mesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = RetroMaterialLibrary.Get(surface);
            return go;
        }

        static void AddCap(System.Collections.Generic.List<Vector3> vertices, System.Collections.Generic.List<Vector2> uv,
            System.Collections.Generic.List<int> triangles, Vector3 a, Vector3 b, float y, bool top)
        {
            int index = vertices.Count;
            vertices.Add(new Vector3(0, y, 0)); vertices.Add(a); vertices.Add(b);
            uv.Add(Vector2.zero); uv.Add(new Vector2(a.x, a.z)); uv.Add(new Vector2(b.x, b.z));
            triangles.Add(index); triangles.Add(index + (top ? 2 : 1)); triangles.Add(index + (top ? 1 : 2));
        }
    }
}
