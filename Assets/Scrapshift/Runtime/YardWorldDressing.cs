using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrapshift
{
    [Serializable] public sealed class YardSceneryProp
    {
        public string model, district;
        public int anchor;
        public float x, y, z, yaw, sx, sy, sz;
        public Vector3 Position
        {
            get
            {
                var point = new Vector3(x, y, z);
                if (anchor != 0)
                {
                    var station = YardNavigation.Get((YardLandmark)anchor);
                    point += new Vector3(station.x, 0, station.z);
                }
                return point;
            }
        }
    }
    [Serializable] public sealed class YardSceneryLayout
    {
        public int version;
        public YardSceneryProp[] props;
        public void Validate()
        {
            if (version != 1 || props == null || props.Length > 128) throw new ArgumentException("Invalid scenery layout size/version");
            foreach (var p in props)
            {
                if (p == null || !YardWorldDressing.IsWorldModel(p.model) || !YardWorldDressing.IsDistrict(p.district) || p.anchor < 0 || p.anchor > YardNavigation.Destinations.Length)
                    throw new ArgumentException("Invalid scenery model, district or anchor");
                foreach (float n in new[] { p.x, p.y, p.z, p.yaw, p.sx, p.sy, p.sz })
                    if (float.IsNaN(n) || float.IsInfinity(n)) throw new ArgumentException("Non-finite scenery transform");
                if (Math.Abs(p.x) > 90 || Math.Abs(p.z) > 90 || p.y < 0 || p.y > 20 || Math.Abs(p.yaw) > 360 ||
                    p.sx < .1f || p.sx > 3 || p.sy < .1f || p.sy > 3 || p.sz < .1f || p.sz > 3)
                    throw new ArgumentException("Scenery transform exceeds bounded world budget");
            }
        }
    }
    // Data-driven, startup-only scenery. It has no inventory, physics, interaction or light components.
    public static class YardWorldDressing
    {
        static Material material;
        static YardSceneryLayout layout;
        static bool triedLayout;
        public static bool IsWorldModel(string name)
        {
            switch (name)
            {
                case "WorkshopDetails": case "ToolWall": case "RepairTools": case "ApplianceRow": case "SalvageShelter":
                case "WeedClump": case "OfficeDetails": case "IndustrialWorks": case "PoplarTree": case "FluorescentFixture": case "FencePost": case "TealContainer": return true;
                default: return false;
            }
        }
        public static bool IsDistrict(string name)
        {
            switch (name)
            {
                case "Workshop surroundings": case "Restoration surroundings": case "North loading district": case "West vehicle salvage":
                case "East metal sorting": case "South entry district": case "Distant landscape": return true;
                default: return false;
            }
        }
        public static YardSceneryLayout LoadLayout()
        {
            if (triedLayout) return layout;
            triedLayout = true;
            var data = Resources.Load<TextAsset>("ScrapshiftWorld/WorldDressing");
            try
            {
                if (data == null) throw new ArgumentException("Missing world dressing resource");
                var candidate = JsonUtility.FromJson<YardSceneryLayout>(data.text);
                if (candidate == null) throw new ArgumentException("Empty world dressing resource");
                candidate.Validate(); layout = candidate;
            }
            catch (ArgumentException e) { Debug.LogWarning("World dressing unavailable: " + e.Message + ". Existing yard remains accessible."); }
            return layout;
        }
        public static bool TryPlace(string name, Transform parent, Vector3 position, out GameObject instance)
        {
            instance = null;
            if (!IsWorldModel(name)) return false;
            if (material == null) material = Resources.Load<Material>("ScrapshiftWorld/WorldProps");
            if (material == null || !AuthoredYardProps.TryPlace(name, parent, position, out instance)) return false;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
                if (name == "WeedClump" || name == "FencePost" || name == "RepairTools" || name == "FluorescentFixture") renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            return true;
        }
        public static void BuildDistrict(Transform parent)
        {
            var data = LoadLayout();
            if (data == null) return;
            foreach (var p in data.props)
                if (p.district == parent.name && TryPlace(p.model, parent, p.Position, out GameObject prop))
                {
                    prop.transform.localRotation = Quaternion.Euler(0, p.yaw, 0);
                    prop.transform.localScale = new Vector3(p.sx, p.sy, p.sz);
                }
        }
        public static void FenceVisual(GameObject boundary, Vector3 size)
        {
            var wire = Resources.Load<Material>("ScrapshiftWorld/WireFence");
            if (wire == null) return;
            bool alongX = size.x > size.z; float width = alongX ? size.x : size.z;
            var go = new GameObject("Open chain-link mesh"); go.transform.SetParent(boundary.transform, false);
            // SurfaceBox stores world-sized vertices at unit transform; retain its collider exactly.
            var mesh = new Mesh { name = "Metre-scaled chain-link" };
            mesh.vertices = alongX ? new[] { new Vector3(-width/2,-size.y/2,0), new Vector3(-width/2,size.y/2,0), new Vector3(width/2,size.y/2,0), new Vector3(width/2,-size.y/2,0) }
                : new[] { new Vector3(0,-size.y/2,-width/2), new Vector3(0,size.y/2,-width/2), new Vector3(0,size.y/2,width/2), new Vector3(0,-size.y/2,width/2) };
            mesh.uv = new[] { Vector2.zero, new Vector2(0,size.y), new Vector2(width,size.y), new Vector2(width,0) };
            mesh.triangles = new[] { 0,1,2,0,2,3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<ProceduralMeshOwner>().mesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = wire; renderer.shadowCastingMode = ShadowCastingMode.Off;
            boundary.GetComponent<MeshRenderer>().enabled = false;
            int spans = Mathf.CeilToInt(width / 4);
            for (int i=0; i<=spans; i++)
            {
                float offset = -width/2 + width*i/spans;
                TryPlace("FencePost", boundary.transform, alongX ? new Vector3(offset,-size.y/2,0) : new Vector3(0,-size.y/2,offset), out _);
            }
        }
    }
}
