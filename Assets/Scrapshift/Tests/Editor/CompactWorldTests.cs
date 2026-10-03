using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactWorldTests
    {
        [TestCase("CompactGenerator",1.1f,1.4f)]
        [TestCase("CompactTier1Scrapper",1.8f,2.1f)]
        [TestCase("CompactMotor",.38f,.46f)]
        [TestCase("CompactCompressor",.40f,.48f)]
        [TestCase("CompactRefrigerator",1.8f,1.95f)]
        [TestCase("CompactDeliveryTruck",1.8f,2.0f)]
        [TestCase("CompactPlasticFragments",.12f,.20f)]
        [TestCase("CompactInsulationCoil",.08f,.15f)]
        [TestCase("CompactBoundaryTree",7.5f,8.5f)]
        public void OriginalModelsImportInMetresWithSharedRecoveredAtlas(string name,float min,float max)
        {
            var root=new GameObject("Compact import test");
            try
            {
                Assert.IsTrue(AuthoredYardProps.TryPlace(name,root.transform,Vector3.zero,out GameObject model));
                var material=YardMaterialBindings.Load("ScrapshiftMaterials/PropAtlas",root.transform);
                var renderers=model.GetComponentsInChildren<Renderer>();Assert.IsNotEmpty(renderers);
                Bounds bounds=renderers[0].bounds;
                foreach(var renderer in renderers){bounds.Encapsulate(renderer.bounds);Assert.AreSame(material,renderer.sharedMaterial);}
                Assert.That(bounds.size.y,Is.InRange(min,max));
                Assert.IsEmpty(model.GetComponentsInChildren<Collider>());
                Assert.IsEmpty(model.GetComponentsInChildren<Rigidbody>());
                Assert.IsEmpty(model.GetComponentsInChildren<Light>());
                foreach(var mesh in model.GetComponentsInChildren<MeshFilter>())
                {Assert.IsTrue(mesh.sharedMesh.isReadable);Assert.LessOrEqual(mesh.sharedMesh.triangles.Length/3,2500);}
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void FixedWorldHasNoStarterEquipmentAndLeavesInteriorClear()
        {
            var root=new GameObject("Compact world test");
            try
            {
                var handles=CompactYardWorld.Build(root.transform,false);Physics.SyncTransforms();
                Assert.AreEqual(CompactYardWorld.ShopAnchor,handles.Shop.transform.localPosition);
                Assert.AreEqual(CompactYardWorld.SalesAnchor,handles.Sales.transform.localPosition);
                Assert.AreEqual(CompactYardWorld.DeliveryAnchor,handles.Delivery.transform.localPosition);
                Assert.AreEqual(CompactYardWorld.WireAnchor,handles.Wire.transform.localPosition);
                Assert.IsEmpty(root.GetComponentsInChildren<Rigidbody>());Assert.IsEmpty(root.GetComponentsInChildren<Light>());
                Assert.IsEmpty(root.GetComponentsInChildren<InteractionTarget>());
                Assert.IsNull(root.transform.Find("Workbench"));Assert.IsNull(root.transform.Find("Generator"));
                foreach(var c in root.GetComponentsInChildren<Collider>())
                {
                    if(c.name=="48 x 36 metre packed gravel")continue;
                    var open=new Bounds(new Vector3(0,1.1f,4),new Vector3(21,2,23));
                    Assert.IsFalse(c.bounds.Intersects(open),c.name+" blocks the open construction interior");
                    var starterCar=new Bounds(new Vector3(18,.9f,-10),new Vector3(2.4f,1.8f,4.7f));
                    var starterFridge=new Bounds(new Vector3(20,.95f,-5),new Vector3(1.2f,1.9f,1.2f));
                    Assert.IsFalse(c.bounds.Intersects(starterCar),c.name+" overlaps starting car");
                    Assert.IsFalse(c.bounds.Intersects(starterFridge),c.name+" overlaps starting refrigerator");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(EquipmentKind.Workbench,2.6f,1.4f)]
        [TestCase(EquipmentKind.Generator,1.75f,1.25f)]
        [TestCase(EquipmentKind.Tier1Scrapper,2.4f,2.3f)]
        public void PlacedAndGhostEquipmentHaveExplicitCollisionPolicy(EquipmentKind kind,float width,float depth)
        {
            var root=new GameObject("Compact equipment test");
            try
            {
                var placed=CompactEquipmentVisuals.Build(kind,root.transform,new Vector3(3,0,4),90);
                Assert.AreEqual(new Vector3(3,0,4),placed.transform.localPosition);
                Assert.That(Quaternion.Angle(placed.transform.localRotation,Quaternion.Euler(0,90,0)),Is.LessThan(.001f));
                var boxes=placed.GetComponents<BoxCollider>();Assert.AreEqual(1,boxes.Length);
                Assert.LessOrEqual(boxes[0].size.x,width);Assert.LessOrEqual(boxes[0].size.z,depth);
                Assert.IsEmpty(placed.GetComponentsInChildren<Rigidbody>());Assert.IsEmpty(placed.GetComponentsInChildren<Light>());
                var ghost=CompactEquipmentVisuals.Build(kind,root.transform,Vector3.zero,0,false);
                Assert.IsEmpty(ghost.GetComponentsInChildren<Collider>());
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(PartKind.Motor)] [TestCase(PartKind.Compressor)] [TestCase(PartKind.Wire)]
        [TestCase(PartKind.Copper)] [TestCase(PartKind.BodyMetal)] [TestCase(PartKind.Steel)]
        [TestCase(PartKind.Plastic)] [TestCase(PartKind.Insulation)]
        public void PortableComponentsUseOneCoarseColliderAndNoPhysicsSimulation(PartKind kind)
        {
            var root=new GameObject("Compact component test");
            try
            {
                var item=CompactEquipmentVisuals.BuildPart(kind,root.transform,Vector3.zero);
                Assert.AreEqual(1,item.GetComponentsInChildren<Collider>().Length);
                Assert.IsEmpty(item.GetComponentsInChildren<Rigidbody>());Assert.IsEmpty(item.GetComponentsInChildren<Light>());
                Assert.IsNotEmpty(item.GetComponentsInChildren<MeshRenderer>());
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(ScrapObjectKind.Car,2)] [TestCase(ScrapObjectKind.Refrigerator,1)]
        public void LargeScrapViewsKeepMetreScaleAndStayStationary(ScrapObjectKind kind,int boxes)
        {
            var root=new GameObject("Large scrap view test");
            try
            {
                var scrap=CompactEquipmentVisuals.BuildScrap(kind,root.transform,new Vector3(18,0,-10),90);
                Assert.AreEqual(Vector3.one,scrap.transform.localScale);
                Assert.AreEqual(boxes,scrap.GetComponents<BoxCollider>().Length);
                Assert.IsEmpty(scrap.GetComponentsInChildren<Rigidbody>());
                Assert.IsEmpty(scrap.GetComponentsInChildren<Light>());
                Assert.IsNotEmpty(scrap.GetComponentsInChildren<MeshRenderer>());
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void CompactGroundLayersStayBoundedUpwardAndOwned()
        {
            var root=new GameObject("Compact ground test");
            try
            {
                CompactYardWorld.Build(root.transform,false);
                int layers=0;
                foreach(var mesh in root.GetComponentsInChildren<MeshFilter>())
                {
                    if(!mesh.name.StartsWith("Compact dust")&&!mesh.name.StartsWith("Compact shallow")&&!mesh.name.StartsWith("Worn entry"))continue;
                    layers++;
                    Assert.AreSame(mesh.sharedMesh,mesh.GetComponent<ProceduralMeshOwner>().mesh);
                    Assert.IsNull(mesh.GetComponent<Collider>());
                    Assert.Less(mesh.sharedMesh.vertexCount,320);
                    foreach(var normal in mesh.sharedMesh.normals)Assert.Greater(normal.y,.99f);
                    foreach(var p in mesh.sharedMesh.vertices)
                    {Assert.Less(Mathf.Abs(p.x),24);Assert.LessOrEqual(Mathf.Abs(p.z),18);Assert.That(p.y,Is.InRange(.009f,.013f));}
                }
                Assert.AreEqual(3,layers,"Wear, puddles and connecting wheel lanes must be available");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
