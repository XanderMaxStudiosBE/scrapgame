// Controlled API/value adapter for executing the ACTUAL runtime builders outside Unity.
// Quaternion/hierarchy/mesh arithmetic and destruction callbacks run here. Authored FBX,
// materials, fonts and world dressing are explicitly labelled fixtures, never engine imports.
// This does not verify Unity compilation, import, rendering, physics, or destruction timing.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using Scrapshift;
using Scrapshift.Compact;

namespace UnityEngine
{
    public class Object
    {
        public virtual string name { get; set; }
        public bool destroyed;
        public static void Destroy(Object item) { DestroyImmediate(item); }
        public static void DestroyImmediate(Object item)
        {
            if (item == null || item.destroyed) return;
            var gameObject = item as GameObject;
            if (gameObject != null)
            {
                foreach (var child in new List<Transform>(gameObject.transform.children)) DestroyImmediate(child.gameObject);
                foreach (var component in gameObject.components)
                {
                    var callback = component.GetType().GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (callback != null) callback.Invoke(component, null);
                    component.destroyed = true;
                }
                gameObject.transform.SetParent(null, false);
            }
            item.destroyed = true;
        }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public override string name { get { return gameObject.name; } set { gameObject.name = value; } }
        public T GetComponent<T>() where T : Component { return gameObject.GetComponent<T>(); }
        public T[] GetComponents<T>() where T : Component { return gameObject.GetComponents<T>(); }
        public T[] GetComponentsInChildren<T>() where T : Component { return gameObject.GetComponentsInChildren<T>(); }
    }
    public class MonoBehaviour : Component { }
    public sealed class Transform : Component
    {
        public Vector3 localPosition;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 localScale = Vector3.one;
        public Transform parent;
        public readonly List<Transform> children = new List<Transform>();
        public Vector3 position { get { return TransformPoint(Vector3.zero); } }
        public Vector3 forward { get { return TransformDirection(Vector3.forward); } }
        public Quaternion rotation { get { return parent == null ? localRotation : parent.rotation * localRotation; } }
        public void SetParent(Transform value, bool worldPositionStays)
        {
            var oldPosition = position; var oldRotation = rotation;
            if (parent != null) parent.children.Remove(this);
            parent = value; if (parent != null) parent.children.Add(this);
            if (worldPositionStays)
            {
                localPosition = parent == null ? oldPosition : parent.InverseTransformPoint(oldPosition);
                localRotation = parent == null ? oldRotation : Quaternion.Inverse(parent.rotation) * oldRotation;
            }
        }
        public Transform Find(string path)
        {
            var split = path.Split('/'); Transform result = this;
            foreach (var part in split)
            {
                Transform next = null;
                foreach (var child in result.children) if (!child.destroyed && child.name == part) { next = child; break; }
                if (next == null) return null; result = next;
            }
            return result;
        }
        public Vector3 TransformPoint(Vector3 point)
        {
            var result = localPosition + localRotation * Vector3.Scale(point, localScale);
            return parent == null ? result : parent.TransformPoint(result);
        }
        public Vector3 InverseTransformPoint(Vector3 point)
        {
            var result = Quaternion.Inverse(localRotation) * ((parent == null ? point : parent.InverseTransformPoint(point)) - localPosition);
            return new Vector3(result.x / localScale.x, result.y / localScale.y, result.z / localScale.z);
        }
        public Vector3 TransformDirection(Vector3 direction)
        {
            var result = localRotation * direction; return parent == null ? result : parent.TransformDirection(result);
        }
    }
    public sealed class GameObject : Object
    {
        public readonly List<Component> components = new List<Component>();
        public readonly Transform transform;
        public int layer;
        public bool activeSelf = true;
        public GameObject(string objectName = "GameObject")
        {
            name = objectName; transform = new Transform { gameObject = this }; components.Add(transform);
        }
        public T AddComponent<T>() where T : Component, new()
        { var value = new T { gameObject = this }; components.Add(value); return value; }
        public T GetComponent<T>() where T : Component
        { foreach (var value in components) if (value is T && !value.destroyed) return (T)value; return null; }
        public T[] GetComponents<T>() where T : Component
        { var results = new List<T>(); foreach (var value in components) if (value is T && !value.destroyed) results.Add((T)value); return results.ToArray(); }
        public T[] GetComponentsInChildren<T>() where T : Component
        {
            var results = new List<T>(GetComponents<T>());
            foreach (var child in transform.children) if (!child.destroyed) results.AddRange(child.GetComponentsInChildren<T>());
            return results.ToArray();
        }
        public void SetActive(bool value) { activeSelf = value; }
        public static GameObject CreatePrimitive(PrimitiveType kind)
        {
            var result = new GameObject("Controlled primitive fixture"); result.AddComponent<MeshRenderer>();
            result.AddComponent<MeshFilter>().sharedMesh = FixtureMesh.Box(Vector3.one);
            result.AddComponent<BoxCollider>().size = Vector3.one; return result;
        }
    }
    public enum PrimitiveType { Cube }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float xValue, float yValue) { x = xValue; y = yValue; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public static Vector2 up { get { return new Vector2(0, 1); } }
        public static Vector2 right { get { return new Vector2(1, 0); } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator *(Vector2 a, float b) { return new Vector2(a.x * b, a.y * b); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float xValue, float yValue, float zValue) { x = xValue; y = yValue; z = zValue; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 down { get { return new Vector3(0, -1, 0); } }
        public static Vector3 right { get { return new Vector3(1, 0, 0); } }
        public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 back { get { return new Vector3(0, 0, -1); } }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z); } }
        public Vector3 normalized { get { return magnitude > 1e-10f ? this / magnitude : zero; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator -(Vector3 a) { return zero - a; }
        public static Vector3 operator *(Vector3 a, float b) { return new Vector3(a.x * b, a.y * b, a.z * b); }
        public static Vector3 operator *(float a, Vector3 b) { return b * a; }
        public static Vector3 operator /(Vector3 a, float b) { return a * (1 / b); }
        public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vector3 Cross(Vector3 a, Vector3 b) { return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
        public static float Distance(Vector3 a, Vector3 b) { return (a - b).magnitude; }
        public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "({0},{1},{2})", x, y, z); }
    }
    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float a, float b, float c, float d) { x = a; y = b; z = c; w = d; }
        public static Quaternion identity { get { return new Quaternion(0, 0, 0, 1); } }
        static Quaternion Normal(Quaternion q)
        { float n = (float)Math.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w); return new Quaternion(q.x / n, q.y / n, q.z / n, q.w / n); }
        public static Quaternion operator *(Quaternion a, Quaternion b)
        { return new Quaternion(a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y, a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x, a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w, a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z); }
        public static Vector3 operator *(Quaternion q, Vector3 v)
        { var u = new Vector3(q.x, q.y, q.z); return v + Vector3.Cross(u, v) * (2 * q.w) + Vector3.Cross(u, Vector3.Cross(u, v)) * 2; }
        public static Quaternion Inverse(Quaternion q)
        { float n = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w; return new Quaternion(-q.x / n, -q.y / n, -q.z / n, q.w / n); }
        public static Quaternion AngleAxis(float degrees, Vector3 axis)
        { axis = axis.normalized; float half = degrees * (float)Math.PI / 360; return new Quaternion(axis.x * (float)Math.Sin(half), axis.y * (float)Math.Sin(half), axis.z * (float)Math.Sin(half), (float)Math.Cos(half)); }
        public static Quaternion Euler(float x, float y, float z)
        { return AngleAxis(y, Vector3.up) * AngleAxis(x, Vector3.right) * AngleAxis(z, Vector3.forward); }
        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            from = from.normalized; to = to.normalized; float d = Vector3.Dot(from, to);
            if (d > .999999f) return identity;
            if (d < -.999999f)
            { var axis = Vector3.Cross(from, Vector3.right); if (axis.magnitude < .001f) axis = Vector3.Cross(from, Vector3.up); return AngleAxis(180, axis); }
            var cross = Vector3.Cross(from, to); return Normal(new Quaternion(cross.x, cross.y, cross.z, 1 + d));
        }
        public static Quaternion LookRotation(Vector3 direction)
        {
            // Runtime builders look along horizontal machine/route axes. This also
            // handles non-horizontal axes through an orthonormal basis.
            var f = direction.normalized; var r = Vector3.Cross(Vector3.up, f).normalized;
            if (r.magnitude < .001f) r = Vector3.Cross(Vector3.forward, f).normalized;
            var u = Vector3.Cross(f, r); float trace = r.x + u.y + f.z;
            if (trace > 0)
            { float s = (float)Math.Sqrt(trace + 1) * 2; return Normal(new Quaternion((u.z - f.y) / s, (f.x - r.z) / s, (r.y - u.x) / s, s / 4)); }
            if (r.x > u.y && r.x > f.z)
            { float s = (float)Math.Sqrt(1 + r.x - u.y - f.z) * 2; return Normal(new Quaternion(s / 4, (u.x + r.y) / s, (f.x + r.z) / s, (u.z - f.y) / s)); }
            if (u.y > f.z)
            { float s = (float)Math.Sqrt(1 + u.y - r.x - f.z) * 2; return Normal(new Quaternion((u.x + r.y) / s, s / 4, (f.y + u.z) / s, (f.x - r.z) / s)); }
            { float s = (float)Math.Sqrt(1 + f.z - r.x - u.y) * 2; return Normal(new Quaternion((f.x + r.z) / s, (f.y + u.z) / s, s / 4, (r.y - u.x) / s)); }
        }
        public static float Angle(Quaternion a, Quaternion b)
        { float d = Math.Abs(a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w); return (float)(Math.Acos(Math.Min(1, d)) * 360 / Math.PI); }
    }
    public struct Bounds
    {
        public Vector3 center, size;
        public Vector3 min { get { return center - size * .5f; } }
        public Vector3 max { get { return center + size * .5f; } }
    }
    public sealed class Mesh : Object
    {
        public Vector3[] vertices = new Vector3[0], normals = new Vector3[0];
        public Vector2[] uv = new Vector2[0];
        public int[] triangles = new int[0];
        public Bounds bounds;
        public int vertexCount { get { return vertices.Length; } }
        public bool isReadable { get { return true; } }
        public int subMeshCount { get { return 1; } }
        public void SetVertices(List<Vector3> values) { vertices = values.ToArray(); }
        public void SetUVs(int channel, List<Vector2> values) { uv = values.ToArray(); }
        public void SetTriangles(List<int> values, int submesh) { triangles = values.ToArray(); }
        public void RecalculateNormals()
        {
            normals = new Vector3[vertices.Length];
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                var n = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).normalized;
                normals[a] = normals[a] + n; normals[b] = normals[b] + n; normals[c] = normals[c] + n;
            }
            for (int i = 0; i < normals.Length; i++) normals[i] = normals[i].normalized;
        }
        public void RecalculateBounds()
        {
            if (vertices.Length == 0) return;
            var min = vertices[0]; var max = min;
            foreach (var v in vertices)
            {
                min = new Vector3(Math.Min(min.x, v.x), Math.Min(min.y, v.y), Math.Min(min.z, v.z));
                max = new Vector3(Math.Max(max.x, v.x), Math.Max(max.y, v.y), Math.Max(max.z, v.z));
            }
            bounds = new Bounds { center = (min + max) * .5f, size = max - min };
        }
    }
    public sealed class MeshFilter : Component { public Mesh sharedMesh; }
    public class Renderer : Component
    { public Material sharedMaterial; public Rendering.ShadowCastingMode shadowCastingMode = Rendering.ShadowCastingMode.On; public bool receiveShadows = true, enabled = true; }
    public sealed class MeshRenderer : Renderer { }
    public class Collider : Component { public bool enabled = true, isTrigger; }
    public sealed class BoxCollider : Collider { public Vector3 center, size; }
    public sealed class Rigidbody : Component { }
    public sealed class Light : Component { }
    public sealed class TextMesh : Component { }
    public static class StaticBatchingUtility { public static int calls; public static void Combine(GameObject[] objects, GameObject root) { calls++; } }
    public sealed class Material : Object
    { public Color color; public Material(Shader shader) { } public void SetFloat(string key, float value) { } }
    public sealed class Shader : Object { public static Shader Find(string value) { return new Shader { name = value }; } }
    public struct Color { public float r, g, b, a; public Color(float red, float green, float blue, float alpha) { r = red; g = green; b = blue; a = alpha; } }
    public struct Color32
    {
        public byte r, g, b, a; public Color32(byte red, byte green, byte blue, byte alpha) { r = red; g = green; b = blue; a = alpha; }
        public static implicit operator Color(Color32 value) { return new Color(value.r / 255f, value.g / 255f, value.b / 255f, value.a / 255f); }
    }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Deg2Rad = (float)Math.PI / 180;
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static int Min(int a, int b) { return Math.Min(a, b); }
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static int Max(int a, int b) { return Math.Max(a, b); }
        public static int CeilToInt(float value) { return (int)Math.Ceiling(value); }
        public static float Abs(float value) { return Math.Abs(value); }
        public static float Sin(float value) { return (float)Math.Sin(value); }
        public static float Cos(float value) { return (float)Math.Cos(value); }
        public static float Sqrt(float value) { return (float)Math.Sqrt(value); }
        public static float Clamp01(float value) { return Math.Max(0, Math.Min(1, value)); }
    }
    public static class Application { public static bool isPlaying; }
    public static class Debug { public static void LogWarning(string message) { Console.WriteLine("CONTROLLED WARNING: " + message); } }
    namespace Rendering { public enum ShadowCastingMode { Off, On } }
    static class FixtureMesh
    {
        public static Mesh Box(Vector3 size)
        {
            var mesh = new Mesh { name = "CONTROLLED authored envelope fixture; not FBX import" };
            var v = new List<Vector3>(); var uv = new List<Vector2>();
            foreach (float x in new[] { -.5f, .5f }) foreach (float y in new[] { 0f, 1f }) foreach (float z in new[] { -.5f, .5f })
            { v.Add(Vector3.Scale(new Vector3(x, y, z), size)); uv.Add(Vector2.zero); }
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(new List<int> { 0, 1, 3, 0, 3, 2, 4, 6, 7, 4, 7, 5 }, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
namespace Scrapshift
{
    public enum RetroSurface { RustPaint, DarkMetal, CorrugatedMetal, WeatheredWood, Gravel, Copper, WireInsulation }
    public sealed class InteractionTarget : MonoBehaviour { public TargetKind kind; }
    public sealed class AuthoredFixtureMarker : MonoBehaviour { public string resourceName; }
    public sealed class SignFixtureMarker : MonoBehaviour { public string text; public float width, height; }
    public static class YardMaterialBindings
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Material Load(string resource, Transform root)
        { if (!materials.ContainsKey(resource)) materials[resource] = new Material(null) { name = resource }; return materials[resource]; }
    }
    public static class AuthoredYardProps
    {
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public static bool TryPlace(string model, Transform parent, Vector3 position, out GameObject result)
        {
            result = new GameObject(model); result.transform.SetParent(parent, false); result.transform.localPosition = position;
            result.AddComponent<AuthoredFixtureMarker>().resourceName = model;
            if (!meshes.ContainsKey(model)) meshes[model] = FixtureMesh.Box(ModelSize(model));
            result.AddComponent<MeshFilter>().sharedMesh = meshes[model];
            result.AddComponent<MeshRenderer>().sharedMaterial = YardMaterialBindings.Load("ScrapshiftMaterials/PropAtlas", parent);
            return true;
        }
        public static Vector3 ModelSize(string model)
        {
            switch (model)
            {
                case "Workbench": return new Vector3(2.55f, 1.44f, 1.35f);
                case "CompactGenerator": return new Vector3(1.75f, 1.24f, 1.25f);
                case "CompactTier1Scrapper": return new Vector3(2.4f, 2, 2.3f);
                case "CompactPortedStorage": return new Vector3(3.04f, 2.25f, 2.62f);
                case "CompactTier2Scrapper": return new Vector3(3.2f, 2.25f, 2.56f);
                case "CompactSplitter": case "CompactMerger": return new Vector3(1.5f, 1, 1.5f);
                case "CompactPrimaryScrapper": return new Vector3(6, 4.3f, 7);
                case "CompactExportStation": return new Vector3(3, 2.05f, 2.5f);
                case "WornHatchback": return new Vector3(2.4f, 1.65f, 4.7f);
                case "CompactRefrigerator": return new Vector3(.85f, 1.88f, .95f);
                default: return new Vector3(.5f, .5f, .5f);
            }
        }
    }
    public static class YardWorldDressing
    {
        public static bool TryPlace(string name, Transform parent, Vector3 position, out GameObject result) { return AuthoredYardProps.TryPlace(name, parent, position, out result); }
        public static void FenceVisual(GameObject fence, Vector3 size) { }
    }
    public static class YardSignText
    {
        public static void Plate(Transform parent, string text, Vector3 position, float width, float height, float yaw = 0)
        { var sign = new GameObject("CONTROLLED font sign fixture"); sign.transform.SetParent(parent, false); sign.transform.localPosition = position;
          sign.transform.localRotation = Quaternion.Euler(0, yaw, 0);
          var marker = sign.AddComponent<SignFixtureMarker>(); marker.text = text; marker.width = width; marker.height = height; }
        public static void CreateLabel(Transform parent, string text, Vector3 position, float yaw)
        { Plate(parent, text, position, 1, .2f); }
    }
}
namespace Scrapshift.Compact
{
    // Ground material creation, backdrop imports, chain-link replacement, font
    // meshes and static batching remain narrow fixtures for the world runner.
    public sealed class CompactGroundSurface : MonoBehaviour
    {
        public Material Create(string material, string texture, Vector2 tiling)
        { return YardMaterialBindings.Load(material + " / CONTROLLED texture " + texture, transform); }
    }
    public static class CompactYardBackdrop { public static void Build(Transform parent, bool combine) { } }
}

public static class MachineryPresentationRunner
{
    static readonly EquipmentKind[] equipmentKinds = { EquipmentKind.Workbench, EquipmentKind.Generator, EquipmentKind.Tier1Scrapper,
        EquipmentKind.Storage, EquipmentKind.Tier2Scrapper, EquipmentKind.Splitter, EquipmentKind.Merger, EquipmentKind.PrimaryScrapper, EquipmentKind.ExportStation };
    static int checks, equipmentViews, beltViews;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static void Near(Vector3 a, Vector3 b, string message, float tolerance = .001f) { Check(Vector3.Distance(a, b) < tolerance, message + ": " + a + " vs " + b); }
    static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    static int PortCount(EquipmentKind kind) { return AutomationModel.PortCount(kind, false) + AutomationModel.PortCount(kind, true); }
    static Mesh MeshAt(GameObject root, string name)
    { var node = root.transform.Find(name); Check(node != null, "Missing generated surface: " + name); return node.GetComponent<MeshFilter>().sharedMesh; }
    static Transform DirectChild(Transform parent, string name)
    { foreach (var child in parent.children) if (!child.destroyed && child.name == name) return child; return null; }
    static void HasVertexInOwner(Transform owner, MeshFilter filter, Vector3 expected, string message)
    {
        bool found = false;
        foreach (var vertex in filter.sharedMesh.vertices)
            if (Vector3.Distance(owner.InverseTransformPoint(filter.transform.TransformPoint(vertex)), expected) < .0002f) found = true;
        Check(found, message + " " + filter.name);
    }
    static void ValidateMeshes(GameObject root)
    {
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = filter.sharedMesh; Check(mesh != null, "No mesh on " + filter.name);
            Check(mesh.vertexCount > 0 && mesh.vertexCount < 25000, "Per mesh vertex budget: " + filter.name);
            Check(mesh.uv.Length == mesh.vertexCount, "UV count " + filter.name);
            Check(mesh.normals.Length == mesh.vertexCount, "Normal count " + filter.name);
            Check(mesh.triangles.Length % 3 == 0, "Triangle stride " + filter.name);
            var fixture = filter.GetComponent<AuthoredFixtureMarker>();
            if (fixture == null) Check(filter.GetComponent<ProceduralMeshOwner>() != null && filter.GetComponent<ProceduralMeshOwner>().mesh == mesh, "Private mesh owner " + filter.name);
            else Check(filter.GetComponent<ProceduralMeshOwner>() == null, "Imported fixture must remain shared/unowned");
            foreach (int index in mesh.triangles) Check(index >= 0 && index < mesh.vertexCount, "Triangle index " + filter.name);
            foreach (var v in mesh.vertices)
            {
                Check(Finite(v.x) && Finite(v.y) && Finite(v.z), "Nonfinite vertex " + filter.name);
                Check(v.x >= mesh.bounds.min.x - .001f && v.x <= mesh.bounds.max.x + .001f && v.y >= mesh.bounds.min.y - .001f && v.y <= mesh.bounds.max.y + .001f && v.z >= mesh.bounds.min.z - .001f && v.z <= mesh.bounds.max.z + .001f, "Recalculated bounds " + filter.name);
            }
        }
        Check(root.GetComponentsInChildren<Rigidbody>().Length == 0, "No rigidbodies on static view");
        Check(root.GetComponentsInChildren<Light>().Length == 0, "No per machine light");
    }
    sealed class Arrow { public Vector3 centre, direction; }
    static List<Arrow> Arrows(Mesh mesh)
    {
        var results = new List<Arrow>();
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            int ai = mesh.triangles[i], bi = mesh.triangles[i + 1], ci = mesh.triangles[i + 2];
            var a = mesh.vertices[ai]; var b = mesh.vertices[bi]; var c = mesh.vertices[ci];
            // Arrowhead uniquely uses .22 length/.26 base. Its tail triangles and
            // stencil boxes have other edge lengths and cannot satisfy this shape.
            if (Math.Abs(Vector3.Distance(b, c) - .26f) > .0005f || Math.Abs(Vector3.Distance(a, (b + c) * .5f) - .22f) > .0005f || Math.Abs(a.y - b.y) > .0005f || Math.Abs(b.y - c.y) > .0005f) continue;
            var direction = (a - (b + c) * .5f).normalized;
            Check(Vector3.Cross(b - a, c - a).normalized.y > .99f, "Arrowhead must face up");
            results.Add(new Arrow { centre = a - direction * .22f, direction = direction });
        }
        return results;
    }
    static void ValidateEquipment()
    {
        foreach (var kind in equipmentKinds) foreach (float yaw in new[] { 0f, 90f, 180f, 270f }) foreach (float size in new[] { 1f, .78f, 1.22f })
        {
            var owner = new GameObject("Controlled equipment fixture"); var rules = new CompactRules();
            owner.transform.localPosition = new Vector3(17, 2, -9); owner.transform.localRotation = Quaternion.Euler(0, 31, 0);
            var definition = rules.Equipment(kind); definition.width *= size; definition.depth *= size == 1 ? 1 : (size < 1 ? .84f : 1.13f);
            var state = new EquipmentState { kind = kind, x = 3, z = 4, yaw = yaw };
            var root = CompactEquipmentVisuals.Build(kind, owner.transform, new Vector3(state.x, 0, state.z), yaw, true, rules); equipmentViews++;
            ValidateMeshes(root); var body = root.GetComponent<BoxCollider>(); Check(body != null, "Equipment body collider " + kind);
            Check(Math.Abs(body.size.x - definition.width) < .001f && Math.Abs(body.size.z - definition.depth) < .001f, "Editable body footprint " + kind);
            Check(root.GetComponentsInChildren<Collider>().Length == 1 + PortCount(kind), "Exactly body plus port selection colliders " + kind);
            Check(root.GetComponentsInChildren<CompactConveyorPortTarget>().Length == PortCount(kind), "Exact IN/OUT target count " + kind);
            var arrowList = PortCount(kind) > 0 ? Arrows(MeshAt(root, "Painted port directions")) : new List<Arrow>();
            Check(arrowList.Count == PortCount(kind), "One genuine direction arrow per physical mouth " + kind);
            foreach (bool output in new[] { false, true }) for (int index = 0; index < AutomationModel.PortCount(kind, output); index++)
            {
                var marker = root.transform.Find((output ? "Output port " : "Input port ") + index); Check(marker != null, "Port marker " + kind);
                var core = AutomationModel.Port(state, rules, output, index); Near(marker.position, owner.transform.TransformPoint(new Vector3(core.x, CompactAutomationVisuals.ItemHeight, core.z)), "Exact rotated core mouth " + kind);
                var target = marker.GetComponent<CompactConveyorPortTarget>(); Check(target != null && target.output == output && target.index == index, "Stable IN/OUT identity " + kind);
                Check(marker.GetComponentsInChildren<MeshRenderer>().Length == 0, "Ports share batched surfaces");
                var portDirection = new Vector3(marker.localPosition.x, 0, marker.localPosition.z).normalized;
                Near(marker.localRotation * Vector3.forward, portDirection, "Mouth faces out " + kind);
                int matches = 0; foreach (var arrow in arrowList)
                {
                    var mouthCentre = marker.localPosition - portDirection * .23f;
                    if (Math.Abs(arrow.centre.x - mouthCentre.x) < .001f && Math.Abs(arrow.centre.z - mouthCentre.z) < .001f)
                    { Near(arrow.direction, output ? portDirection : -portDirection, "IN/OUT travel arrow " + kind);
                      Check(Math.Abs(arrow.centre.y - (CompactAutomationVisuals.TravelSurfaceHeight + .002f)) < .0001f, "Port arrow sits on deck below cargo"); matches++; }
                }
                Check(matches == 1, "Arrow located at actual mouth " + kind);
                var deck = MeshAt(root, "Port transport collars"); var side = Vector3.Cross(Vector3.up, portDirection);
                foreach (float sign in new[] { -1f, 1f })
                {
                    bool found = false; var expected = marker.localPosition + side * (sign * CompactAutomationVisuals.TrackWidth * .5f) + Vector3.up * (CompactAutomationVisuals.TravelSurfaceHeight - CompactAutomationVisuals.ItemHeight);
                    foreach (var vertex in deck.vertices) if (Vector3.Distance(vertex, expected) < .0002f) found = true;
                    Check(found, "Actual collar travel deck reaches exact mouth corner");
                }
                foreach (var vertex in deck.vertices) Check(vertex.y <= CompactAutomationVisuals.TravelSurfaceHeight + .0001f, "Collar deck stays below carried part");
                var frame = MeshAt(root, "Worn port-collar rails");
                foreach (float rollerOffset in new[] { .105f, .345f })
                {
                    bool topFound = false;
                    foreach (var vertex in frame.vertices)
                    {
                        var local = marker.InverseTransformPoint(root.transform.TransformPoint(vertex));
                        if (Math.Abs(local.x) > .37f || Math.Abs(local.z + rollerOffset) > .025f || local.y < -.05f || local.y > .01f) continue;
                        Check(local.y <= .0001f, "Exposed mouth roller cannot penetrate carried part"); if (Math.Abs(local.y) < .0001f) topFound = true;
                    }
                    Check(topFound, "Actual roller top meets authoritative cargo base " + kind + " yaw " + yaw + " size " + size + " output " + output + " index " + index + " offset " + rollerOffset);
                }
            }
            var assembly = root.transform.Find("Original working assembly");
            if (kind != EquipmentKind.Splitter && kind != EquipmentKind.Merger)
            {
                Check(assembly != null, "Detailed machinery working assembly " + kind);
                int triangles = 0; int meshCount = assembly.GetComponentsInChildren<MeshFilter>().Length;
                Check(meshCount > 0 && meshCount <= 2, "Small batched machine detail assembly");
                foreach (var filter in assembly.GetComponentsInChildren<MeshFilter>())
                {
                    triangles += filter.sharedMesh.triangles.Length / 3;
                    foreach (var vertex in filter.sharedMesh.vertices)
                    {
                        var local = root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        Check(Math.Abs(local.x) <= definition.width * .5f + .001f && Math.Abs(local.z) <= definition.depth * .5f + .001f, "Working fittings stay inside editable purchased footprint " + kind);
                    }
                }
                Check(triangles < 1400, "Machine detail triangle budget");
                int rootsBefore = root.transform.children.Count; CompactAutomationVisuals.BuildMachineDetails(kind, root.transform, rules);
                Check(root.transform.children.Count == rootsBefore && assembly.GetComponentsInChildren<MeshFilter>().Length == meshCount, "Working assembly preparation is idempotent");
            }
            var ghost = CompactEquipmentVisuals.Build(kind, owner.transform, Vector3.zero, yaw, false, rules); equipmentViews++;
            ValidateMeshes(ghost); Check(ghost.GetComponentsInChildren<Collider>().Length == 0, "Ghost has zero colliders " + kind);
            Check(ghost.GetComponentsInChildren<CompactConveyorPortTarget>().Length == 0, "Ghost has zero selectable mouths " + kind);
            var privateMeshes = new List<Mesh>(); var privateIdentity = new HashSet<Mesh>();
            foreach (var view in owner.GetComponentsInChildren<ProceduralMeshOwner>())
            { privateMeshes.Add(view.mesh); Check(privateIdentity.Add(view.mesh), "Each runtime surface owns its own distinct mesh"); }
            var sharedMeshes = new List<Mesh>(); foreach (var fixture in owner.GetComponentsInChildren<AuthoredFixtureMarker>()) sharedMeshes.Add(fixture.GetComponent<MeshFilter>().sharedMesh);
            UnityEngine.Object.DestroyImmediate(owner);
            foreach (var mesh in privateMeshes) Check(mesh.destroyed, "Actual owner callback disposes private mesh");
            foreach (var mesh in sharedMeshes) Check(!mesh.destroyed, "Shared authored fixture remains intact");
        }
    }
    static void ValidateBelts()
    {
        foreach (float yaw in new[] { 0f, 90f, 180f, 270f }) foreach (bool xFirst in new[] { false, true })
        {
            var rules = new CompactRules(); var state = new CompactYardState();
            state.equipment.Add(new EquipmentState { id = 1, kind = EquipmentKind.Tier1Scrapper, x = -4, z = -3, yaw = yaw });
            state.equipment.Add(new EquipmentState { id = 2, kind = EquipmentKind.Storage, x = 6, z = 8, yaw = (yaw + 180) % 360 });
            var link = new ConveyorLink { id = 3, fromId = 1, toId = 2, bendXFirst = xFirst };
            var owner = new GameObject("Controlled route fixture");
            owner.transform.localPosition = new Vector3(9, 1, -12); owner.transform.localRotation = Quaternion.Euler(0, 23, 0);
            var sourceView = CompactEquipmentVisuals.Build(state.equipment[0].kind, owner.transform, new Vector3(-4, 0, -3), yaw, false, rules);
            var destinationView = CompactEquipmentVisuals.Build(state.equipment[1].kind, owner.transform, new Vector3(6, 0, 8), (yaw + 180) % 360, false, rules);
            var root = CompactAutomationVisuals.BuildBelt(link, state, rules, owner.transform); beltViews++;
            ValidateMeshes(root); Check(root.GetComponentsInChildren<MeshFilter>().Length == 3, "Three batched belt surfaces");
            Check(root.GetComponentsInChildren<Collider>().Length == 0, "Belts remain walkable, no collider");
            var path = AutomationModel.Path(link, state, rules); var track = MeshAt(root, "Rubber travel surface"); var arrows = Arrows(MeshAt(root, "Painted conveyor direction"));
            Check(arrows.Count > 0, "Belt has actual route direction arrows");
            foreach (var arrow in arrows)
            {
                bool followsRoute = false;
                for (int i = 1; i < path.Length; i++)
                {
                    var start = new Vector3(path[i - 1].x, arrow.centre.y, path[i - 1].z); var end = new Vector3(path[i].x, arrow.centre.y, path[i].z);
                    var direction = (end - start).normalized; float along = Vector3.Dot(arrow.centre - start, direction);
                    float off = Math.Abs(Vector3.Cross(arrow.centre - start, direction).y);
                    if (off < .001f && along >= -.001f && along <= Vector3.Distance(start, end) + .001f && Vector3.Dot(direction, arrow.direction) > .999f) followsRoute = true;
                }
                Check(followsRoute, "Arrow follows existing route segment; no decorative branch");
            }
            for (int endpoint = 0; endpoint < 2; endpoint++)
            {
                int index = endpoint == 0 ? 0 : path.Length - 1; var at = new Vector3(path[index].x, .69f, path[index].z);
                var direction = endpoint == 0 ? new Vector3(path[1].x - path[0].x, 0, path[1].z - path[0].z).normalized : new Vector3(path[index].x - path[index - 1].x, 0, path[index].z - path[index - 1].z).normalized;
                var side = Vector3.Cross(Vector3.up, direction);
                foreach (float sign in new[] { -1f, 1f })
                {
                    bool exists = false; foreach (var v in track.vertices) if (Vector3.Distance(v, at + side * (CompactAutomationVisuals.TrackWidth * .5f * sign)) < .002f) exists = true;
                    Check(exists, "Actual deck reaches exact core endpoint at rail-side corner");
                }
                Near(CompactAutomationVisuals.ItemPosition(link, state, rules, endpoint), new Vector3(at.x, .7f, at.z), "Cargo exact core endpoint");
                var equipmentView = endpoint == 0 ? sourceView : destinationView;
                var marker = equipmentView.transform.Find(endpoint == 0 ? "Output port 0" : "Input port 0");
                var endpointInOwner = owner.transform.InverseTransformPoint(marker.position);
                var sleeveSide = Quaternion.Inverse(owner.transform.rotation) * marker.TransformDirection(Vector3.right);
                var beltSteel = root.transform.Find("Belt steel frame, rollers and supports").GetComponent<MeshFilter>();
                var collarSteel = equipmentView.transform.Find("Worn port-collar rails").GetComponent<MeshFilter>();
                foreach (float sign in new[] { -1f, 1f }) foreach (float edge in new[] { -.0425f, .0425f }) foreach (float height in new[] { .61f, .74f })
                {
                    var sleeveFace = endpointInOwner + sleeveSide * (sign * .40f + edge) + Vector3.up * (height - CompactAutomationVisuals.ItemHeight);
                    HasVertexInOwner(owner.transform, beltSteel, sleeveFace, "Belt sleeve exact terminal face");
                    HasVertexInOwner(owner.transform, collarSteel, sleeveFace, "Machine sleeve exact matching terminal face");
                }
            }
            float length = AutomationModel.Length(path), traversed = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (i > 0) traversed += Vector3.Distance(new Vector3(path[i - 1].x, 0, path[i - 1].z), new Vector3(path[i].x, 0, path[i].z));
                Near(CompactAutomationVisuals.ItemPosition(link, state, rules, traversed / length), new Vector3(path[i].x, .7f, path[i].z), "Cargo traverses actual elbow by arc length");
            }
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>()) foreach (var v in filter.sharedMesh.vertices) Check(v.y >= -.001f && v.y <= .82f, "Ground-level belt height envelope");
            UnityEngine.Object.DestroyImmediate(owner);
        }
        var straightOwner = new GameObject("Controlled straight route fixture"); var straightRules = new CompactRules(); var straightState = new CompactYardState();
        straightState.equipment.Add(new EquipmentState { id = 1, kind = EquipmentKind.Tier1Scrapper });
        straightState.equipment.Add(new EquipmentState { id = 2, kind = EquipmentKind.Storage, z = -8, yaw = 180 });
        var straightBelt = CompactAutomationVisuals.BuildBelt(new ConveyorLink { id = 3, fromId = 1, toId = 2 }, straightState, straightRules, straightOwner.transform); beltViews++;
        foreach (var vertex in MeshAt(straightBelt, "Rubber travel surface").vertices)
            Check(vertex.y <= CompactAutomationVisuals.TravelSurfaceHeight + .0001f, "Collinear port stubs cannot create raised elbow plates");
        foreach (var normal in MeshAt(straightBelt, "Painted conveyor direction").normals) Check(normal.y > .99f, "Every straight belt paint face points up");
        UnityEngine.Object.DestroyImmediate(straightOwner);
    }
    static void ValidateIndustry()
    {
        var rules = new CompactRules(); var owner = new GameObject("Controlled industry fixture");
        var equipment = new EquipmentState { id = 7, kind = EquipmentKind.PrimaryScrapper, industry = new IndustrialMachineState { enabled = true,
            primary = new PrimaryScrapJob { id = 17, kind = ScrapObjectKind.Car, duration = 24, remaining = 12, xpEligible = true, yields = new[] { new PartAmount(PartKind.Wire, 3) } } } };
        var root = CompactIndustryVisuals.BuildEquipment(equipment.kind, owner.transform, Vector3.zero, 0, true, rules);
        CompactIndustryVisuals.SyncJob(root, equipment, rules); var load = DirectChild(root.transform, "Loaded whole scrap / 17"); Check(load != null, "Cached whole-object load");
        int meshes = root.GetComponentsInChildren<MeshFilter>().Length;
        for (int i = 0; i < 12; i++) CompactIndustryVisuals.SyncJob(root, equipment, rules);
        Check(DirectChild(root.transform, "Loaded whole scrap / 17") == load && root.GetComponentsInChildren<MeshFilter>().Length == meshes, "Stable industrial job identity retains view");
        Check(load.GetComponentsInChildren<Collider>().Length == 0 && load.GetComponentsInChildren<Rigidbody>().Length == 0, "Decorative load has no physics");
        var drive = root.transform.Find("Industrial timing mark"); var clamp = root.transform.Find("Left hydraulic clamp"); Check(drive != null && clamp != null, "Industrial timing/clamp details");
        var rest = drive.localRotation; var clampRest = clamp.localPosition;
        CompactIndustryVisuals.Step(root, equipment, false, .2f, 1); Check(Quaternion.Angle(rest, drive.localRotation) < .05f, "Paused drive frozen"); Near(clampRest, clamp.localPosition, "Paused clamp frozen");
        CompactIndustryVisuals.Step(root, equipment, true, .2f, 1); Check(Quaternion.Angle(rest, drive.localRotation) > 1, "Powered drive moves"); Check(Vector3.Distance(clampRest, clamp.localPosition) > .001f, "Powered clamp moves");
        rest = drive.localRotation; clampRest = clamp.localPosition;
        CompactIndustryVisuals.Step(root, equipment, false, 4, 5); CompactIndustryVisuals.Step(root, equipment, true, float.NaN, 5); CompactIndustryVisuals.Step(root, equipment, true, .2f, float.PositiveInfinity);
        Check(Quaternion.Angle(rest, drive.localRotation) < .05f, "Invalid/blocked drive frozen"); Near(clampRest, clamp.localPosition, "Invalid/blocked clamp frozen");
        Check(equipment.industry.primary.remaining == 12 && equipment.contents.Count == 0 && equipment.industry.objectsProcessed == 0, "Visual helpers never process/supply cargo");
        equipment.industry.primary = null; CompactIndustryVisuals.SyncJob(root, equipment, rules); Check(DirectChild(root.transform, "Loaded whole scrap / 17") == null && load.destroyed, "Cleared job disposes cached load");
        Check(typeof(CompactIndustryVisuals).GetMethod("Update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null, "No per-object Update");
        UnityEngine.Object.DestroyImmediate(owner);
    }
    static string Escape(string value) { return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\""; }
    static string Number(float value) { return value.ToString("R", CultureInfo.InvariantCulture); }
    static string V(Vector3 value) { return "[" + Number(value.x) + "," + Number(value.y) + "," + Number(value.z) + "]"; }
    static string PathName(Transform value)
    { return value.parent == null ? value.name : PathName(value.parent) + "/" + value.name; }
    static string TransformJson(Transform value)
    {
        var origin = value.position;
        return "\"position\":" + V(origin) + ",\"basisX\":" + V(value.TransformPoint(Vector3.right) - origin) + ",\"basisY\":" + V(value.TransformPoint(Vector3.up) - origin) + ",\"basisZ\":" + V(value.TransformPoint(Vector3.forward) - origin);
    }
    static void Export(string output)
    {
        var owner = new GameObject("Actual runtime source mesh preview"); var rules = new CompactRules();
        for (int i = 0; i < equipmentKinds.Length; i++) CompactEquipmentVisuals.Build(equipmentKinds[i], owner.transform, new Vector3((i % 3) * 9 - 9, 0, (i / 3) * 11 - 11), 0, true, rules);
        var line = new GameObject("Actual connected source line"); line.transform.SetParent(owner.transform, false); line.transform.localPosition = new Vector3(26, 0, 0);
        var state = new CompactYardState(); state.equipment.Add(new EquipmentState { id = 1, kind = EquipmentKind.Tier1Scrapper, x = 0, z = 0 });
        state.equipment.Add(new EquipmentState { id = 2, kind = EquipmentKind.Storage, x = 6, z = -7, yaw = 180 });
        foreach (var e in state.equipment) CompactEquipmentVisuals.Build(e.kind, line.transform, new Vector3(e.x, 0, e.z), e.yaw, true, rules);
        CompactAutomationVisuals.BuildBelt(new ConveyorLink { id = 3, fromId = 1, toId = 2, bendXFirst = true }, state, rules, line.transform);
        var builder = new StringBuilder("{\n\"evidence\":\"Actual runtime builder mesh export via controlled Unity API/value adapter. Authored FBX, materials, fonts, imports, lighting, rendering and physics unverified. Authored markers require real tracked FBX import; fixture meshes excluded.\",\n\"meshes\":["); bool first = true;
        foreach (var filter in owner.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.GetComponent<AuthoredFixtureMarker>() != null) continue;
            if (!first) builder.Append(','); first = false; var mesh = filter.sharedMesh;
            builder.Append("\n{\"name\":").Append(Escape(PathName(filter.transform))).Append(",\"material\":").Append(Escape(filter.GetComponent<MeshRenderer>().sharedMaterial.name)).Append(",\"vertices\":[");
            for (int i = 0; i < mesh.vertices.Length; i++) { if (i > 0) builder.Append(','); builder.Append(V(filter.transform.TransformPoint(mesh.vertices[i]))); }
            builder.Append("],\"uv\":["); for (int i = 0; i < mesh.uv.Length; i++) { if (i > 0) builder.Append(','); builder.Append('[').Append(Number(mesh.uv[i].x)).Append(',').Append(Number(mesh.uv[i].y)).Append(']'); }
            builder.Append("],\"triangles\":["); for (int i = 0; i < mesh.triangles.Length; i++) { if (i > 0) builder.Append(','); builder.Append(mesh.triangles[i]); } builder.Append("]}");
        }
        builder.Append("],\n\"authoredMarkers\":["); first = true;
        foreach (var marker in owner.GetComponentsInChildren<AuthoredFixtureMarker>())
        {
            if (!first) builder.Append(','); first = false;
            builder.Append("\n{\"name\":").Append(Escape(marker.resourceName)).Append(",\"hierarchy\":").Append(Escape(PathName(marker.transform))).Append(',').Append(TransformJson(marker.transform)).Append('}');
        }
        builder.Append("],\n\"fontMarkers\":["); first = true;
        foreach (var marker in owner.GetComponentsInChildren<SignFixtureMarker>())
        {
            if (!first) builder.Append(','); first = false;
            builder.Append("\n{\"text\":").Append(Escape(marker.text)).Append(",\"width\":").Append(Number(marker.width)).Append(",\"height\":").Append(Number(marker.height)).Append(',').Append(TransformJson(marker.transform)).Append('}');
        }
        builder.Append("]\n}\n"); File.WriteAllText(output, builder.ToString()); UnityEngine.Object.DestroyImmediate(owner);
        Console.WriteLine("Exported actual procedural source meshes and separate authored/font fixture markers: " + output);
    }
    public static int Main(string[] args)
    {
        try
        {
            ValidateEquipment(); ValidateBelts(); ValidateIndustry();
            Console.WriteLine("PASS machinery presentation: " + equipmentViews + " actual equipment/ghost builders across 9 equipment kinds, 4 yaws and 3 footprint variants; " + beltViews + " actual belt routes; " + checks + " throwing adapter assertions. NOT Unity engine evidence.");
            if (args.Length == 2 && args[0] == "--export") Export(args[1]);
            else if (args.Length != 0) throw new ArgumentException("Usage: run-machinery-presentation.sh [--export /absolute/output.json]");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
}
