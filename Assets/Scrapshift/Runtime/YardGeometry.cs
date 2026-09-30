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
            if (!Materials.TryGetValue(color, out Material material) || material == null)
            {
                material = new Material(SurfaceShader != null ? SurfaceShader : Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color; material.SetFloat("_Smoothness", .08f);
                Materials[color] = material;
            }
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.GetComponent<Collider>().enabled = collider;
            return go;
        }
        public static GameObject Bundle(MaterialKind kind, Transform parent)
        {
            var root = new GameObject(kind == MaterialKind.Wire ? "Insulated wire bundle" : "Recovered copper bundle");
            root.transform.SetParent(parent, false);
            for (int i = 0; i < 4; i++)
                Box("Strand", root.transform, new Vector3((i - 1.5f) * .13f, 0, 0), new Vector3(.10f, .17f, .55f), kind == MaterialKind.Wire ? Charcoal : Copper, false);
            var collider = root.AddComponent<BoxCollider>(); collider.size = new Vector3(.55f, .2f, .6f);
            return root;
        }
        public static void Sign(Transform parent, string text, Vector3 position, float yaw = 0)
        {
            var sign = new GameObject(text);
            sign.transform.SetParent(parent, false); sign.transform.localPosition = position;
            sign.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var mesh = sign.AddComponent<TextMesh>(); mesh.text = text; mesh.fontSize = 60; mesh.characterSize = .075f;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sign.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.color = Ivory;
        }
        public static GameObject Station(Transform parent, string name, TargetKind kind, Vector3 position, Color color)
        {
            var station = Box(name, parent, position, new Vector3(2.5f, 1.2f, 1.3f), color);
            var target = station.AddComponent<InteractionTarget>(); target.kind = kind;
            return station;
        }
    }
}
