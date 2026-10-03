using NUnit.Framework;
using UnityEngine;

namespace Scrapshift.Tests
{
    public sealed class OldTyreTests
    {
        const string Prefix = "ThirdParty/PolyHaven/OldTyre/";

        [Test]
        public void ImportedTyreHasItsOwnNormalMappedUrpMaterialAndMetreBounds()
        {
            var root = new GameObject("CC0 tyre import test");
            try
            {
                Assert.IsTrue(OldTyreVisuals.TryPlace(root.transform, new Vector3(1, .14f, 2), 43));
                var filters = root.GetComponentsInChildren<MeshFilter>();
                Assert.AreEqual(1, filters.Length);
                Assert.IsTrue(filters[0].sharedMesh.isReadable);
                Assert.AreEqual(184, filters[0].sharedMesh.triangles.Length / 3);
                var renderers = root.GetComponentsInChildren<MeshRenderer>();
                Assert.AreEqual(1, renderers.Length);
                var material = renderers[0].sharedMaterial;
                Assert.AreSame(Resources.Load<Material>(Prefix + "OldTyre"), material);
                Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name);
                Assert.AreSame(Resources.Load<Texture2D>(Prefix + "Albedo"), material.GetTexture("_BaseMap"));
                Assert.AreSame(material.GetTexture("_BaseMap"), material.GetTexture("_MainTex"));
                Assert.AreSame(Resources.Load<Texture2D>(Prefix + "Normal"), material.GetTexture("_BumpMap"));
                Assert.IsTrue(material.IsKeywordEnabled("_NORMALMAP"));
                Assert.AreSame(Resources.Load<Texture2D>(Prefix + "MetalSmoothness"), material.GetTexture("_MetallicGlossMap"));
                Assert.IsTrue(material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"));
                var bounds = renderers[0].bounds;
                Assert.AreEqual(.23f, bounds.size.y, .002f, "Flat metre-scale tyre; no axis or centimetre error");
                Assert.AreEqual(.025f, bounds.min.y, .002f);
                Assert.AreEqual(1, bounds.center.x, .002f);
                Assert.AreEqual(2, bounds.center.z, .002f);
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
                Assert.IsEmpty(root.GetComponentsInChildren<Rigidbody>());
                Assert.IsEmpty(root.GetComponentsInChildren<Light>());
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RepeatedTyresShareImportedMeshAndMaterial()
        {
            var root = new GameObject("CC0 tyre reuse test");
            try
            {
                Assert.IsTrue(OldTyreVisuals.TryPlace(root.transform, Vector3.zero, 0));
                Assert.IsTrue(OldTyreVisuals.TryPlace(root.transform, Vector3.up, 91));
                var meshes = root.GetComponentsInChildren<MeshFilter>();
                var renderers = root.GetComponentsInChildren<MeshRenderer>();
                Assert.AreEqual(2, meshes.Length);
                Assert.AreEqual(2, renderers.Length);
                Assert.AreSame(meshes[0].sharedMesh, meshes[1].sharedMesh);
                Assert.AreSame(renderers[0].sharedMaterial, renderers[1].sharedMaterial);
                Assert.IsEmpty(root.GetComponentsInChildren<ProceduralMeshOwner>(), "Imported meshes must never be destroyed as private geometry");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
