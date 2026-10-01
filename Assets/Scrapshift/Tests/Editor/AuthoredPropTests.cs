using NUnit.Framework;
using UnityEngine;

namespace Scrapshift.Tests
{
    public sealed class AuthoredPropTests
    {
        [TestCase("WornHatchback", 1, 2)]
        [TestCase("RustyHatchback", 1, 2)]
        [TestCase("YardOffice", 4, 6)]
        [TestCase("ShippingContainer", 2, 4)]
        [TestCase("WorkshopCanopy", 4, 6)]
        [TestCase("SortingSkip", 1, 2)]
        [TestCase("SalvageFan", .6f, 1.2f)]
        [TestCase("SalvageFanFrame", .6f, 1.2f)]
        [TestCase("FanRotor", .2f, .6f)]
        [TestCase("PortableRadio", .5f, .7f)]
        [TestCase("Workbench", .9f, 1.5f)]
        [TestCase("WireCrate", .7f, 1.3f)]
        [TestCase("PalletBundle", .6f, 1.2f)]
        [TestCase("StorageRack", 2.4f, 3)]
        [TestCase("PoweredStripper", 1.4f, 1.7f)]
        [TestCase("BuyingScale", 1.7f, 2.1f)]
        [TestCase("FeedRoller", .3f, .4f)]
        [TestCase("WireBundle", .05f, .2f)]
        [TestCase("CopperBundle", .03f, .2f)]
        public void ImportedPropsHaveReadableMetreScaleMeshesAndUrpMaterials(string name, float minimumHeight, float maximumHeight)
        {
            var root = new GameObject("Authored prop test");
            try
            {
                Assert.IsTrue(AuthoredYardProps.TryPlace(name, root.transform, Vector3.zero, out GameObject model), "Tracked model must import, not use fallback");
                var meshes = model.GetComponentsInChildren<MeshFilter>(); Assert.IsNotEmpty(meshes);
                foreach (var filter in meshes)
                {
                    Assert.IsTrue(filter.sharedMesh.isReadable, "Runtime batching needs readable source mesh");
                    Assert.Greater(filter.sharedMesh.vertexCount, 0);
                }
                var renderers = model.GetComponentsInChildren<Renderer>(); Assert.IsNotEmpty(renderers);
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    Assert.AreEqual("Universal Render Pipeline/Lit", renderer.sharedMaterial.shader.name);
                    Assert.NotNull(renderer.sharedMaterial.mainTexture);
                }
                Assert.GreaterOrEqual(bounds.size.y, minimumHeight, "Height/axis conversion");
                Assert.LessOrEqual(bounds.size.y, maximumHeight, "Centimetre/metre conversion");
                Assert.IsEmpty(model.GetComponentsInChildren<Collider>(), "Builders assign explicit coarse collision volumes");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
