using System;
using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactAutomationVisualTests
    {
        [TestCase(EquipmentKind.Storage,"CompactPortedStorage")]
        [TestCase(EquipmentKind.Tier2Scrapper,"CompactTier2Scrapper")]
        [TestCase(EquipmentKind.Splitter,"CompactSplitter")]
        [TestCase(EquipmentKind.Merger,"CompactMerger")]
        public void ActualModelsShareRecoveredAtlasAndFitEditableFootprints(EquipmentKind kind,string modelName)
        {
            var root=new GameObject("Automation equipment import test");
            try
            {
                var rules=new CompactRules();var definition=rules.Equipment(kind);
                definition.width*=.9f;definition.depth*=.8f;
                var equipment=CompactEquipmentVisuals.Build(kind,root.transform,new Vector3(3,0,4),90,true,rules);
                var collider=equipment.GetComponent<BoxCollider>();Assert.NotNull(collider);
                Assert.AreEqual(definition.width,collider.size.x);Assert.AreEqual(definition.depth,collider.size.z);
                Assert.AreEqual(1,equipment.GetComponentsInChildren<Collider>().Length);
                Assert.IsEmpty(equipment.GetComponentsInChildren<Rigidbody>());Assert.IsEmpty(equipment.GetComponentsInChildren<Light>());
                var model=equipment.transform.Find(modelName);Assert.NotNull(model,"Original FBX must be imported");
                var shared=YardMaterialBindings.Load("ScrapshiftMaterials/PropAtlas",root.transform);
                foreach(var renderer in model.GetComponentsInChildren<Renderer>())Assert.AreSame(shared,renderer.sharedMaterial);
                foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
                {
                    Assert.IsTrue(filter.sharedMesh.isReadable);
                    Assert.LessOrEqual(filter.sharedMesh.triangles.Length/3,2500);
                    foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        var local=equipment.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        Assert.LessOrEqual(Mathf.Abs(local.x),definition.width*.5f+.05f);
                        Assert.LessOrEqual(Mathf.Abs(local.z),definition.depth*.5f+.05f);
                    }
                }
                var ghost=CompactEquipmentVisuals.Build(kind,root.transform,Vector3.zero,0,false,rules);
                Assert.IsEmpty(ghost.GetComponentsInChildren<Collider>());
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        [TestCase(EquipmentKind.Storage,0)] [TestCase(EquipmentKind.Storage,90)]
        [TestCase(EquipmentKind.Tier2Scrapper,180)] [TestCase(EquipmentKind.Splitter,90)]
        [TestCase(EquipmentKind.Merger,270)] [TestCase(EquipmentKind.Tier1Scrapper,0)]
        public void PhysicalPortMarkersFollowAuthoritativeRotatedCoordinates(EquipmentKind kind,float yaw)
        {
            var root=new GameObject("Automation port test");
            try
            {
                var rules=new CompactRules();var state=new EquipmentState{kind=kind,x=3,z=4,yaw=yaw};
                rules.Equipment(kind).width*=1.1f;rules.Equipment(kind).depth*=1.2f;
                var equipment=CompactEquipmentVisuals.Build(kind,root.transform,new Vector3(state.x,0,state.z),yaw,false,rules);
                for(int category=0;category<2;category++)
                {
                    bool output=category==1;
                    for(int i=0;i<AutomationModel.PortCount(kind,output);i++)
                    {
                        var marker=equipment.transform.Find((output ? "Output port " : "Input port ")+i);Assert.NotNull(marker);
                        var expected=AutomationModel.Port(state,rules,output,i);
                        Assert.Less(Vector3.Distance(marker.position,new Vector3(expected.x,.7f,expected.z)),.001f);
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        static void Route(out CompactYardState state,out CompactRules rules,out ConveyorLink link)
        {
            rules=new CompactRules();state=new CompactYardState();
            state.equipment.Add(new EquipmentState{id=1,kind=EquipmentKind.Tier2Scrapper,x=0,z=0});
            state.equipment.Add(new EquipmentState{id=2,kind=EquipmentKind.Storage,x=4,z=8});
            link=new ConveyorLink{id=3,fromId=1,toId=2,bendXFirst=true};
        }
        [Test]
        public void BeltMeshesHaveBoundedGeometryUpwardMarksAndNoPhysics()
        {
            var root=new GameObject("Belt geometry test");
            try
            {
                Route(out CompactYardState state,out CompactRules rules,out ConveyorLink link);
                var belt=CompactAutomationVisuals.BuildBelt(link,state,rules,root.transform);
                Assert.IsEmpty(belt.GetComponentsInChildren<Collider>());Assert.IsEmpty(belt.GetComponentsInChildren<Rigidbody>());
                Assert.IsEmpty(belt.GetComponentsInChildren<Light>());
                var meshes=belt.GetComponentsInChildren<MeshFilter>();Assert.AreEqual(3,meshes.Length);
                int vertices=0;
                foreach(var filter in meshes)
                {
                    vertices+=filter.sharedMesh.vertexCount;
                    Assert.AreSame(filter.sharedMesh,filter.GetComponent<ProceduralMeshOwner>().mesh);
                    foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        Assert.IsFalse(float.IsNaN(vertex.x)||float.IsInfinity(vertex.x));
                        Assert.IsFalse(float.IsNaN(vertex.y)||float.IsInfinity(vertex.y));
                        Assert.IsFalse(float.IsNaN(vertex.z)||float.IsInfinity(vertex.z));
                        Assert.That(vertex.y,Is.InRange(0,.80f));
                    }
                    if(filter.name=="Painted conveyor direction")
                        foreach(var normal in filter.sharedMesh.normals)Assert.Greater(normal.y,.99f);
                }
                Assert.Less(vertices,6000,"One short line must remain within its static mesh budget");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void TransitPositionFollowsTheActualElbowByArcLength()
        {
            Route(out CompactYardState state,out CompactRules rules,out ConveyorLink link);
            var points=AutomationModel.Path(link,state,rules);
            float length=AutomationModel.Length(points),traversed=0;
            for(int i=0;i<points.Length;i++)
            {
                if(i>0)
                {
                    float dx=points[i].x-points[i-1].x,dz=points[i].z-points[i-1].z;
                    traversed+=Mathf.Sqrt(dx*dx+dz*dz);
                }
                var position=CompactAutomationVisuals.ItemPosition(link,state,rules,traversed/length);
                Assert.Less(Vector3.Distance(position,new Vector3(points[i].x,.7f,points[i].z)),.001f);
            }
            var beginning=CompactAutomationVisuals.ItemPosition(link,state,rules,-1);
            var end=CompactAutomationVisuals.ItemPosition(link,state,rules,2);
            Assert.AreEqual(new Vector3(points[0].x,.7f,points[0].z),beginning);
            Assert.AreEqual(new Vector3(points[points.Length-1].x,.7f,points[points.Length-1].z),end);
        }
    }
}
