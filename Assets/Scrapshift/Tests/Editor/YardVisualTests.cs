using NUnit.Framework;
using UnityEngine;

namespace Scrapshift.Tests
{
    public sealed class YardVisualTests
    {
        GameObject root;

        [SetUp]
        public void CreateRoot() { root = new GameObject("Geometry test yard"); }

        [TearDown]
        public void DestroyRoot()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void StationCollidersResolveTheirStationMarker()
        {
            var delivery = YardProps.Delivery(root.transform, new Vector3(-7, 0, 2));
            var bench = YardProps.Workbench(root.transform, new Vector3(-2.5f, 0, 4));
            var buyer = YardProps.Buyer(root.transform, new Vector3(2.2f, 0, 4));
            AssertStation(delivery, TargetKind.Supply);
            AssertStation(bench.root, TargetKind.Bench);
            AssertStation(buyer, TargetKind.Sell);
            var machine = WireStripperVisual.Build(root.transform, new Vector3(7, 0, 2));
            AssertStation(machine.root, TargetKind.Machine);
            Assert.AreEqual(Vector3.one, delivery.transform.localScale);
            Assert.AreEqual(Vector3.one, bench.root.transform.localScale);
            Assert.AreEqual(Vector3.one, buyer.transform.localScale);
        }

        [Test]
        public void BenchMaterialAndDeliveryStockDoNotBlockInteractionRays()
        {
            var bench = YardProps.Workbench(root.transform, Vector3.zero);
            Assert.IsEmpty(bench.materialDisplay.GetComponentsInChildren<Collider>());
            Assert.NotNull(bench.materialDisplay.GetComponent<Renderer>());
            var delivery = YardProps.Delivery(root.transform, new Vector3(4, 0, 0));
            foreach (var part in delivery.GetComponentsInChildren<Transform>())
                if (part.name == "Visible scrap wire")
                    Assert.IsEmpty(part.GetComponentsInChildren<Collider>());
        }

        [Test]
        public void SurfaceBoxesUseMetreUvsAndOutwardTriangles()
        {
            var box = YardGeometry.SurfaceBox("Nonuniform textured test box", root.transform, Vector3.zero,
                new Vector3(2, 3, 4), RetroSurface.RustPaint);
            var mesh = box.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(Vector3.one, box.transform.localScale);
            Assert.AreEqual(new Vector3(2, 3, 4), box.GetComponent<BoxCollider>().size);
            var vertices = mesh.vertices; var uv = mesh.uv;
            for (int face = 0; face < 6; face++)
            {
                int first = face * 4;
                Assert.AreEqual(Vector3.Distance(vertices[first], vertices[first + 1]),
                    Vector2.Distance(uv[first], uv[first + 1]), .0001f, "Horizontal texel density");
                Assert.AreEqual(Vector3.Distance(vertices[first + 1], vertices[first + 2]),
                    Vector2.Distance(uv[first + 1], uv[first + 2]), .0001f, "Vertical texel density");
            }
            AssertOutwardTriangles(mesh);
        }

        [Test]
        public void OriginalLowPolyDrumMeshesHaveOutwardSideAndCapTriangles()
        {
            YardProps.WorkshopSurroundings(root.transform);
            int drums = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.name != "Salvage drum") continue;
                drums++;
                var mesh = filter.sharedMesh;
                Assert.AreEqual(12 * 4 * 3, mesh.triangles.Length, "Twelve sides, two side and two cap triangles each");
                AssertOutwardTriangles(mesh);
            }
            Assert.AreEqual(3, drums);
        }

        [Test]
        public void FacetedVegetationHasOutwardFacesAndNoInteractionColliders()
        {
            var shrub = CozyYardDetails.Faceted(root.transform, "Test shrub", Vector3.zero, new Vector3(3, 2, 4), CozyYardDetails.Sage);
            Assert.IsEmpty(shrub.GetComponentsInChildren<Collider>());
            var mesh = shrub.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(16 * 3, mesh.triangles.Length);
            AssertOutwardTriangles(mesh);
            CozyYardDetails.Workshop(root.transform);
            foreach (var collider in root.GetComponentsInChildren<Collider>())
                Assert.IsFalse(collider.enabled, "Workshop accents must not obstruct station rays or saved items");
        }

        static void AssertStation(GameObject station, TargetKind expected)
        {
            var marker = station.GetComponent<InteractionTarget>();
            Assert.AreEqual(expected, marker.kind);
            var colliders = station.GetComponentsInChildren<Collider>();
            Assert.IsNotEmpty(colliders);
            Physics.SyncTransforms();
            foreach (var collider in colliders)
            {
                Assert.AreSame(marker, collider.GetComponentInParent<InteractionTarget>());
                Vector3 minimum = station.transform.InverseTransformPoint(collider.bounds.min);
                Vector3 maximum = station.transform.InverseTransformPoint(collider.bounds.max);
                Assert.GreaterOrEqual(minimum.x, -1.2501f, "Preserve original station collision footprint");
                Assert.LessOrEqual(maximum.x, 1.2501f, "Preserve original station collision footprint");
                Assert.GreaterOrEqual(minimum.z, -.6501f, "Preserve original station collision footprint");
                Assert.LessOrEqual(maximum.z, .6501f, "Preserve original station collision footprint");
            }
        }

        static void AssertOutwardTriangles(Mesh mesh)
        {
            var vertices = mesh.vertices; var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                Assert.Greater(Vector3.Dot(normal, (a + b + c) / 3), 0, "Outward triangle " + i / 3 + " in " + mesh.name);
            }
        }
    }
}
