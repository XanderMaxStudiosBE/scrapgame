using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrapshift
{
    // Static accents share palette materials and never block walking or interaction rays.
    public static class CozyYardDetails
    {
        public static readonly Color Sage = new Color32(116, 137, 108, 255);
        public static readonly Color DustyBlue = new Color32(99, 132, 139, 255);
        public static readonly Color Ochre = new Color32(185, 151, 89, 255);
        public static readonly Color Window = new Color32(73, 100, 101, 255);
        public static readonly Color WarmWindow = new Color32(221, 181, 113, 255);

        public static GameObject Accent(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            var go = YardGeometry.Box(name, parent, position, size, color, false);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        public static void Office(Transform parent, Vector3 pos)
        {
            // Two sloping textured sheets make a pitched roof without a heavyweight model.
            foreach (float side in new[] { -1f, 1f })
            {
                var roof = YardGeometry.SurfaceBox("Office pitched roof", parent, pos + new Vector3(side * 2.25f, 3.65f, 0),
                    new Vector3(4.75f, .16f, 7), RetroSurface.CorrugatedMetal, false);
                roof.transform.localRotation = Quaternion.Euler(0, 0, -side * 15);
                Accent(parent, "Office roof fascia", pos + new Vector3(side * 2.25f, 3.65f, 3.53f), new Vector3(4.75f, .20f, .10f), Sage)
                    .transform.localRotation = roof.transform.localRotation;
            }
            Accent(parent, "Office roof ridge", pos + new Vector3(0, 4.24f, 0), new Vector3(.2f, .18f, 7.1f), YardGeometry.Rust);
            Accent(parent, "Office foundation trim", pos + new Vector3(0, .2f, 3.05f), new Vector3(8, .28f, .1f), Sage);
            WindowFrame(parent, pos + new Vector3(1.5f, 1.7f, 3.08f), 2, 1.1f, true);
            Accent(parent, "Office door frame", pos + new Vector3(-2, 2.27f, 3.08f), new Vector3(1.4f, .12f, .12f), YardGeometry.Ivory);
            foreach (float x in new[] { -2.66f, -1.34f })
                Accent(parent, "Office door jamb", pos + new Vector3(x, 1.14f, 3.08f), new Vector3(.1f, 2.28f, .12f), YardGeometry.Ivory);
            Accent(parent, "Door handle", pos + new Vector3(-1.58f, 1.1f, 3.1f), new Vector3(.08f, .22f, .08f), Ochre);
            Accent(parent, "Office porch awning", pos + new Vector3(-2, 2.65f, 3.8f), new Vector3(2.8f, .15f, 1.8f), Sage)
                .transform.localRotation = Quaternion.Euler(7, 0, 0);
            Plant(parent, pos + new Vector3(-3.25f, 0, 3.8f));
            Plant(parent, pos + new Vector3(3.15f, 0, 3.8f));
            YardGeometry.Sign(parent, "SCRAPSHIFT / YARD OFFICE", pos + new Vector3(0, 2.65f, 3.16f), 180);
        }

        public static void WindowFrame(Transform parent, Vector3 pos, float width, float height, bool warm)
        {
            Accent(parent, "Window glass", pos, new Vector3(width, height, .04f), warm ? WarmWindow : Window);
            foreach (float x in new[] { -width / 2, 0, width / 2 })
                Accent(parent, "Window mullion", pos + new Vector3(x, 0, .04f), new Vector3(.07f, height + .1f, .07f), YardGeometry.Ivory);
            foreach (float y in new[] { -height / 2, height / 2 })
                Accent(parent, "Window frame", pos + new Vector3(0, y, .04f), new Vector3(width + .1f, .07f, .07f), YardGeometry.Ivory);
        }

        public static void Workshop(Transform parent)
        {
            bool detailed = YardWorldDressing.TryPlace("ToolWall", parent, new Vector3(-2.5f, 0, 5.45f), out _);
            if (!detailed)
            {
                YardGeometry.SurfaceBox("Workshop rear tool board", parent, new Vector3(-2.5f, 1.7f, 5.45f), new Vector3(4, 1.6f, .10f), RetroSurface.WeatheredWood, false);
                for (int i=0; i<5; i++)
                {
                    float x = -4+i*.65f;
                    Accent(parent,"Hanging tool handle",new Vector3(x,1.8f,5.36f),new Vector3(.065f,.45f,.07f),i%2==0 ? Ochre : DustyBlue);
                    Accent(parent,"Hanging tool head",new Vector3(x,2.04f,5.34f),new Vector3(.25f,.12f,.09f),YardGeometry.Charcoal);
                }
                Accent(parent,"Workshop shelf",new Vector3(-2.5f,1.22f,5.18f),new Vector3(3.8f,.10f,.42f),Sage);
            }
            Accent(parent, "Workshop coffee mug", new Vector3(-3.9f, detailed ? 1.26f : 1.37f, detailed ? 5.25f : 5.15f), new Vector3(.14f, .2f, .14f), YardGeometry.Ivory);
            if(!AuthoredYardProps.TryPlace("PortableRadio",parent,new Vector3(-1.2f,detailed ? 1.16f : 1.27f,detailed ? 5.25f : 5.1f),out GameObject shelfRadio))
            {
                Accent(parent,"Workshop radio",new Vector3(-1.2f,1.43f,5.14f),new Vector3(.6f,.3f,.22f),DustyBlue);
                Accent(parent,"Radio speaker",new Vector3(-1.35f,1.43f,5.01f),new Vector3(.24f,.21f,.02f),YardGeometry.Charcoal);
                Accent(parent,"Radio dial",new Vector3(-1.04f,1.43f,5.0f),new Vector3(.15f,.1f,.025f),WarmWindow);
            }
            Plant(parent, new Vector3(-5.4f, 0, 5.8f));
        }

        public static void Plant(Transform parent, Vector3 pos)
        {
            YardProps.Cylinder("Terracotta plant pot", parent, pos + Vector3.up * .18f, .22f, .36f, RetroSurface.RustPaint, Quaternion.identity);
            Faceted(parent, "Low poly potted shrub", pos + Vector3.up * .62f, new Vector3(.7f, .65f, .7f), Sage);
        }

        // Sixteen flat triangles; UVs use metres and each face owns its vertices/normals.
        public static GameObject Faceted(Transform parent, string name, Vector3 pos, Vector3 size, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4, b = (i + 1) * Mathf.PI / 4;
                var va = new Vector3(Mathf.Cos(a) * size.x / 2, 0, Mathf.Sin(a) * size.z / 2);
                var vb = new Vector3(Mathf.Cos(b) * size.x / 2, 0, Mathf.Sin(b) * size.z / 2);
                Triangle(vertices, uv, triangles, Vector3.up * size.y / 2, vb, va);
                Triangle(vertices, uv, triangles, Vector3.down * size.y / 2, va, vb);
            }
            var mesh = new Mesh { name = name + " / sixteen flat triangles" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<ProceduralMeshOwner>().mesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = YardGeometry.PaletteMaterial(color);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }
        static void Triangle(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int first = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            uv.Add(Vector2.zero); uv.Add(new Vector2(Vector3.Distance(a, b), 0));
            uv.Add(new Vector2(0, Vector3.Distance(a, c)));
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
        }
    }
}
