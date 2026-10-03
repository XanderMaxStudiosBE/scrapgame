using UnityEngine;

namespace Scrapshift
{
    // The editor builds this resource locally from licensed Store art. Public checkouts use the fallback.
    public static class LocalCarPartsVisuals
    {
        public const string PistonResource = "LocalScrapshiftProps/Piston";
        public const int MaximumTriangles = 1500;

        public static bool IsUsable(Mesh mesh)
        {
            if (mesh == null || !mesh.isReadable || mesh.vertexCount == 0 || mesh.vertexCount > MaximumTriangles * 3 || mesh.subMeshCount != 1 ||
                mesh.GetTopology(0) != MeshTopology.Triangles || mesh.GetIndexCount(0) < 3 ||
                mesh.GetIndexCount(0) > MaximumTriangles * 3) return false;
            var bounds = mesh.bounds;
            var size = bounds.size;
            return Finite(size.x) && Finite(size.y) && Finite(size.z) &&
                Finite(bounds.center.x) && Finite(bounds.center.y) && Finite(bounds.center.z) &&
                size.x > 0 && size.x <= .601f && size.y > 0 && size.y <= .601f && size.z > 0 && size.z <= .551f &&
                Mathf.Abs(bounds.center.x) <= .001f && Mathf.Abs(bounds.center.z) <= .001f && Mathf.Abs(bounds.min.y) <= .001f;
        }

        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        public static bool TryPlace(Transform parent, Vector3 basePosition, float yaw)
        {
            var mesh = Resources.Load<Mesh>(PistonResource);
            if (!IsUsable(mesh)) return false;
            var material = YardMaterialBindings.Load("ScrapshiftMaterials/DarkMetal", parent);
            if (material == null) return false;
            var instance = new GameObject("Imported piston spare / local Store asset");
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = basePosition;
            instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            instance.AddComponent<MeshRenderer>().sharedMaterial = material;
            return true;
        }
    }
}
