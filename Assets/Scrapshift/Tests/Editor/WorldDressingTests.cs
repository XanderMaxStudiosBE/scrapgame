using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
namespace Scrapshift.Tests
{
    public sealed class WorldDressingTests
    {
        [TestCase("WorkshopDetails",3.8f,4.2f)]
        [TestCase("ToolWall",3,3.4f)]
        [TestCase("RepairTools",.1f,.3f)]
        [TestCase("ApplianceRow",2,2.3f)]
        [TestCase("SalvageShelter",3.2f,3.7f)]
        [TestCase("OfficeDetails",2.9f,3.2f)]
        [TestCase("IndustrialWorks",16,18)]
        [TestCase("PoplarTree",10,11)]
        [TestCase("FluorescentFixture",.5f,.8f)]
        [TestCase("FencePost",2.2f,2.4f)]
        [TestCase("TealContainer",2.7f,3.1f)]
        [TestCase("WeedClump",.3f,.5f)]
        public void WorldModelsUseMetresSharedMasksAndNoSimulation(string name,float minimumHeight,float maximumHeight)
        {
            var root = new GameObject("World import test");
            try
            {
                Assert.IsTrue(YardWorldDressing.TryPlace(name,root.transform,Vector3.zero,out GameObject model));
                Assert.IsEmpty(model.GetComponentsInChildren<Collider>()); Assert.IsEmpty(model.GetComponentsInChildren<Rigidbody>());
                Assert.IsEmpty(model.GetComponentsInChildren<Light>()); Assert.IsEmpty(model.GetComponentsInChildren<InteractionTarget>());
                var shared = Resources.Load<Material>("ScrapshiftWorld/WorldProps");
                Assert.NotNull(shared); Assert.NotNull(shared.GetTexture("_MetallicGlossMap")); Assert.NotNull(shared.GetTexture("_EmissionMap"));
                Assert.AreEqual("Universal Render Pipeline/Lit",shared.shader.name);
                var meshes = model.GetComponentsInChildren<MeshFilter>(); Assert.IsNotEmpty(meshes);
                foreach (var mesh in meshes) Assert.IsTrue(mesh.sharedMesh.isReadable);
                var renderers = model.GetComponentsInChildren<Renderer>(); Assert.IsNotEmpty(renderers);
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds); Assert.AreSame(shared,renderer.sharedMaterial);
                }
                Assert.That(bounds.size.y,Is.InRange(minimumHeight,maximumHeight));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void LayoutIsBoundedAnchoredAndRejectsInvalidData()
        {
            var text = Resources.Load<TextAsset>("ScrapshiftWorld/WorldDressing"); Assert.NotNull(text);
            var data = JsonUtility.FromJson<YardSceneryLayout>(text.text); data.Validate();
            Assert.That(data.props.Length,Is.InRange(1,128));
            var tools = Array.Find(data.props,p=>p.model=="RepairTools" && p.anchor==(int)YardLandmark.Bench);
            Assert.NotNull(tools); Assert.AreEqual(YardBootstrap.StationPosition(YardLandmark.Bench)+new Vector3(.7f,1.135f,.24f),tools.Position);
            tools.x=float.NaN; Assert.Throws<ArgumentException>(data.Validate); tools.x=.7f;
            tools.model="../Workbench"; Assert.Throws<ArgumentException>(data.Validate); tools.model="RepairTools";
            tools.anchor=99; Assert.Throws<ArgumentException>(data.Validate); tools.anchor=(int)YardLandmark.Bench;
            tools.sy=0; Assert.Throws<ArgumentException>(data.Validate); tools.sy=1;
            tools.district="unknown"; Assert.Throws<ArgumentException>(data.Validate);
        }
        [TestCase(96,2.2f,.2f)]
        [TestCase(.2f,2.2f,80)]
        [TestCase(8,2,.2f)]
        public void WireFenceRetainsOriginalBoundaryAndOwnsOnlyItsNewMesh(float x,float y,float z)
        {
            var root = new GameObject("Fence test");
            try
            {
                var size = new Vector3(x,y,z); var p = new Vector3(0,y/2,39.5f);
                var boundary = YardGeometry.SurfaceBox("Perimeter fence",root.transform,p,size,RetroSurface.CorrugatedMetal);
                var before = boundary.GetComponent<BoxCollider>(); YardWorldDressing.FenceVisual(boundary,size);
                Assert.AreSame(before,boundary.GetComponent<BoxCollider>()); Assert.AreEqual(size,before.size); Assert.AreEqual(p,boundary.transform.localPosition);
                Assert.AreEqual(1,boundary.GetComponentsInChildren<Collider>().Length); Assert.IsFalse(boundary.GetComponent<MeshRenderer>().enabled);
                var plane = boundary.transform.Find("Open chain-link mesh").GetComponent<MeshFilter>();
                Assert.AreEqual(4,plane.sharedMesh.vertexCount); Assert.AreSame(plane.sharedMesh,plane.GetComponent<ProceduralMeshOwner>().mesh);
                Assert.AreEqual(ShadowCastingMode.Off,plane.GetComponent<Renderer>().shadowCastingMode);
                var material = plane.GetComponent<Renderer>().sharedMaterial;
                Assert.AreEqual(1,material.GetFloat("_AlphaClip")); Assert.AreEqual(0,material.GetFloat("_Cull"));
                Assert.AreEqual(TextureWrapMode.Repeat,material.GetTexture("_BaseMap").wrapMode);
                Assert.IsEmpty(boundary.GetComponentsInChildren<Light>());
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void GroundLayersAreBoundedUpwardFacingAndDoNotBlockDroppedItems()
        {
            var root = new GameObject("Ground layer test");
            try
            {
                YardGroundDressing.Build(root.transform);
                var meshes = root.GetComponentsInChildren<MeshFilter>(); Assert.AreEqual(8,meshes.Length);
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>()); Assert.IsEmpty(root.GetComponentsInChildren<Light>());
                int vertices=0;
                foreach (var mesh in meshes)
                {
                    vertices+=mesh.sharedMesh.vertexCount;
                    Assert.AreSame(mesh.sharedMesh,mesh.GetComponent<ProceduralMeshOwner>().mesh);
                    foreach (var normal in mesh.sharedMesh.normals) Assert.Greater(normal.y,.99f);
                    foreach (var vertex in mesh.sharedMesh.vertices)
                    { Assert.That(vertex.y,Is.InRange(.009f,.012f)); Assert.Less(Math.Abs(vertex.x),48); Assert.Less(Math.Abs(vertex.z),40); }
                }
                Assert.Less(vertices,700,"Transparent geometry budget stays bounded");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [Test]
        public void IntegratedWorldKeepsSourcesAndCoarseCollisionVolumes()
        {
            var root = new GameObject("Integrated dressing test");
            try
            {
                ScrapyardWorld.Build(root.transform,false); Assert.AreEqual(3,root.GetComponentsInChildren<InteractionTarget>().Length);
                Assert.IsEmpty(root.GetComponentsInChildren<Light>()); Assert.IsEmpty(root.GetComponentsInChildren<Rigidbody>());
                var office=root.transform.Find("South entry district/YardOffice"); Assert.NotNull(office);
                Assert.AreEqual(new Vector3(8,3,6),office.GetComponent<BoxCollider>().size);
                Assert.AreEqual(new Vector3(-16,0,-30),office.localPosition);
                foreach (var mesh in root.GetComponentsInChildren<MeshRenderer>())
                    if (mesh.sharedMaterial==Resources.Load<Material>("ScrapshiftWorld/WorldProps") && mesh.name!="TealContainer")
                        Assert.IsEmpty(mesh.GetComponentsInChildren<Collider>(),mesh.name+" dressing must not obstruct old saves/items");
                var teal=root.transform.Find("North loading district/TealContainer"); Assert.NotNull(teal);
                Assert.AreEqual(new Vector3(9,2.8f,4),teal.GetComponent<BoxCollider>().size);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
