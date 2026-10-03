using System;
using NUnit.Framework;
using UnityEngine;

namespace Scrapshift.Tests
{
    public sealed class LocalCarPartsTests
    {
        [Test]
        public void PreparationCombinesChildTransformsAndFitsWithoutChangingSourceMeshes()
        {
            var source = new GameObject("Source piston geometry test");
            var original = BoxMesh();
            Mesh prepared = null;
            try
            {
                source.transform.position = new Vector3(200, -10, 100);
                source.transform.rotation = Quaternion.Euler(0, 53, 0);
                source.transform.localScale = new Vector3(2, 3, 4);
                var first = new GameObject("Head"); first.transform.SetParent(source.transform, false);
                first.transform.localPosition = Vector3.up;
                first.AddComponent<MeshFilter>().sharedMesh = original;
                var second = new GameObject("Rod"); second.transform.SetParent(source.transform, false);
                second.transform.localPosition = new Vector3(.6f, -2, .4f);
                second.transform.localRotation = Quaternion.Euler(0, 31, 0);
                second.transform.localScale = new Vector3(.5f, 2, .5f);
                second.AddComponent<MeshFilter>().sharedMesh = original;
                var before = original.vertices;
                prepared = ImportedCarPartsSetup.PrepareModel(source);
                Assert.IsTrue(LocalCarPartsVisuals.IsUsable(prepared));
                Assert.AreEqual(24, prepared.triangles.Length / 3);
                Assert.AreEqual(1, prepared.subMeshCount);
                Assert.AreEqual(.6f, prepared.bounds.size.y, .001f);
                Assert.AreEqual(0, prepared.bounds.min.y, .001f);
                Assert.AreEqual(0, prepared.bounds.center.x, .001f);
                Assert.AreEqual(0, prepared.bounds.center.z, .001f);
                CollectionAssert.AreEqual(before, original.vertices, "Installed source geometry is never edited");
                Assert.AreEqual(new Vector3(200, -10, 100), source.transform.position);
                Assert.AreEqual(new Vector3(.6f, -2, .4f), second.transform.localPosition);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(original);
                if (prepared != null) UnityEngine.Object.DestroyImmediate(prepared);
            }
        }

        [Test]
        public void PreparationRejectsExcessGeometryBeforeCreatingAnOutput()
        {
            var source = new GameObject("Piston budget test");
            var mesh = new Mesh();
            try
            {
                mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
                var indices = new int[(LocalCarPartsVisuals.MaximumTriangles + 1) * 3];
                for (int i = 0; i < indices.Length; i++) indices[i] = i % 3;
                mesh.triangles = indices;
                source.AddComponent<MeshFilter>().sharedMesh = mesh;
                var error = Assert.Throws<ArgumentException>(() => ImportedCarPartsSetup.PrepareModel(source));
                StringAssert.Contains("1,500-triangle", error.Message);
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void OptionalResourceUsesSharedGeometryOrLeavesFallbackAvailable()
        {
            var root = new GameObject("Optional local piston test");
            try
            {
                var mesh = Resources.Load<Mesh>(LocalCarPartsVisuals.PistonResource);
                if (!LocalCarPartsVisuals.IsUsable(mesh))
                {
                    Assert.IsFalse(LocalCarPartsVisuals.TryPlace(root.transform, Vector3.zero, -20));
                    Assert.AreEqual(0, root.transform.childCount, "Unavailable private art must leave no partial prop");
                    return;
                }
                Assert.IsTrue(LocalCarPartsVisuals.TryPlace(root.transform, new Vector3(0, .2f, 0), -20));
                Assert.IsTrue(LocalCarPartsVisuals.TryPlace(root.transform, Vector3.right, 0));
                var filters = root.GetComponentsInChildren<MeshFilter>();
                var renderers = root.GetComponentsInChildren<MeshRenderer>();
                Assert.AreEqual(2, filters.Length); Assert.AreEqual(2, renderers.Length);
                Assert.AreSame(mesh, filters[0].sharedMesh); Assert.AreSame(mesh, filters[1].sharedMesh);
                Assert.AreSame(renderers[0].sharedMaterial, renderers[1].sharedMaterial);
                Assert.AreEqual("Universal Render Pipeline/Lit", renderers[0].sharedMaterial.shader.name);
                Assert.AreEqual(.2f, renderers[0].bounds.min.y, .002f);
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>());
                Assert.IsEmpty(root.GetComponentsInChildren<Rigidbody>());
                Assert.IsEmpty(root.GetComponentsInChildren<Light>());
                Assert.IsEmpty(root.GetComponentsInChildren<ProceduralMeshOwner>());
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static Mesh BoxMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new[] {
                new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f),
                new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f) };
            mesh.triangles = new[] { 0,3,2,0,2,1, 4,5,6,4,6,7, 0,1,5,0,5,4, 3,7,6,3,6,2, 0,4,7,0,7,3, 1,2,6,1,6,5 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
