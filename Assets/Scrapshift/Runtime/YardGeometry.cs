using UnityEngine;
using System.Collections.Generic;

namespace Scrapshift
{
    public static class YardGeometry
    {
        public static Shader SurfaceShader;
        static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        public static readonly Color Ivory = new Color32(232, 221, 196, 255);
        public static readonly Color Rust = new Color32(183, 89, 53, 255);
        public static readonly Color Olive = new Color32(105, 115, 91, 255);
        public static readonly Color Charcoal = new Color32(32, 41, 39, 255);
        public static readonly Color Copper = new Color32(211, 124, 66, 255);
        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Color color, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = PaletteMaterial(color);
            go.GetComponent<Collider>().enabled = collider;
            return go;
        }
        public static Material PaletteMaterial(Color color)
        {
            if (!Materials.TryGetValue(color, out Material material) || material == null)
            {
                material = new Material(SurfaceShader != null ? SurfaceShader : Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color; material.SetFloat("_Smoothness", .08f);
                Materials[color] = material;
            }
            return material;
        }
        // Independent face UVs use metres rather than Transform scale, keeping the same texel density on every prop.
        public static GameObject SurfaceBox(string name, Transform parent, Vector3 position, Vector3 size, RetroSurface surface, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var mesh = new Mesh { name = name + " / metre UVs" };
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            Vector3 h = size * .5f;
            AddFace(vertices, uv, triangles, new Vector3(0,0,h.z), Vector3.right, Vector3.up, size.x, size.y);
            AddFace(vertices, uv, triangles, new Vector3(0,0,-h.z), Vector3.left, Vector3.up, size.x, size.y);
            AddFace(vertices, uv, triangles, new Vector3(h.x,0,0), Vector3.back, Vector3.up, size.z, size.y);
            AddFace(vertices, uv, triangles, new Vector3(-h.x,0,0), Vector3.forward, Vector3.up, size.z, size.y);
            AddFace(vertices, uv, triangles, new Vector3(0,h.y,0), Vector3.right, Vector3.back, size.x, size.z);
            AddFace(vertices, uv, triangles, new Vector3(0,-h.y,0), Vector3.right, Vector3.forward, size.x, size.z);
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<ProceduralMeshOwner>().mesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = RetroMaterialLibrary.Get(surface);
            if (collider) go.AddComponent<BoxCollider>().size = size;
            return go;
        }
        static void AddFace(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, Vector3 centre, Vector3 right, Vector3 up, float width, float height)
        {
            int offset = vertices.Count;
            Vector3 horizontal = right * width * .5f, vertical = up * height * .5f;
            vertices.Add(centre-horizontal-vertical); vertices.Add(centre+horizontal-vertical);
            vertices.Add(centre+horizontal+vertical); vertices.Add(centre-horizontal+vertical);
            uv.Add(Vector2.zero); uv.Add(new Vector2(width, 0)); uv.Add(new Vector2(width, height)); uv.Add(new Vector2(0, height));
            triangles.Add(offset); triangles.Add(offset+1); triangles.Add(offset+2);
            triangles.Add(offset); triangles.Add(offset+2); triangles.Add(offset+3);
        }
        public static GameObject Bundle(MaterialKind kind, Transform parent)
        {
            var root = new GameObject(kind == MaterialKind.Wire ? "Insulated wire bundle" : "Recovered copper bundle");
            root.transform.SetParent(parent, false);
            if (AuthoredYardProps.TryPlace(kind == MaterialKind.Wire ? "WireBundle" : "CopperBundle", root.transform, Vector3.zero, out GameObject authored))
            {
                var authoredCollider = root.AddComponent<BoxCollider>(); authoredCollider.size = new Vector3(.55f, .2f, .6f);
                return root;
            }
            for (int i = 0; i < 4; i++)
                SurfaceBox("Strand", root.transform, new Vector3((i - 1.5f) * .13f, 0, 0), new Vector3(.10f, .17f, .55f), kind == MaterialKind.Wire ? RetroSurface.WireInsulation : RetroSurface.Copper, false);
            var collider = root.AddComponent<BoxCollider>(); collider.size = new Vector3(.55f, .2f, .6f);
            return root;
        }
        public static void Sign(Transform parent, string text, Vector3 position, float yaw = 0)
        {
            var sign = new GameObject(text);
            sign.transform.SetParent(parent, false); sign.transform.localPosition = position;
            sign.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var mesh = sign.AddComponent<TextMesh>(); mesh.text = text; mesh.fontSize = 60; mesh.characterSize = .035f;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sign.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.color = Ivory;
        }
        public static void MountedSign(Transform parent, string text, Vector3 ground, float height = 2.25f, float width = 2.8f)
        {
            var root = new GameObject("Mounted yard sign").transform;
            root.SetParent(parent, false); root.localPosition = ground;
            SurfaceBox("Painted signboard", root, new Vector3(0, height, .07f), new Vector3(width, .56f, .09f), RetroSurface.DarkMetal, false);
            foreach (float x in new[] { -width * .38f, width * .38f })
                SurfaceBox("Sign support", root, new Vector3(x, height * .5f, .13f), new Vector3(.06f, height, .06f), RetroSurface.DarkMetal, false);
            Sign(root, text, new Vector3(0, height, .01f));
            var label = root.GetChild(root.childCount - 1).GetComponent<TextMesh>();
            label.characterSize = .027f;
            // Decorative only; targeting retains the station's original collision footprint.
        }
        public static GameObject Station(Transform parent, string name, TargetKind kind, Vector3 position, Color color)
        {
            var station = Box(name, parent, position, new Vector3(2.5f, 1.2f, 1.3f), color);
            var target = station.AddComponent<InteractionTarget>(); target.kind = kind;
            return station;
        }
    }
}
