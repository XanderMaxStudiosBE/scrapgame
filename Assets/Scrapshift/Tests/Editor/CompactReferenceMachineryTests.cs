using System;
using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    /// <summary>Engine geometry/import checks. Cloud adapters do not execute these Unity cases.</summary>
    public sealed class CompactReferenceMachineryTests
    {
        static readonly EquipmentKind[] DetailedKinds={EquipmentKind.Workbench,EquipmentKind.Generator,
            EquipmentKind.Tier1Scrapper,EquipmentKind.Tier2Scrapper,EquipmentKind.Storage,
            EquipmentKind.PrimaryScrapper,EquipmentKind.ExportStation};

        [Test]
        public void MovableWorkingAssembliesFitEveryEditableFootprintAndKeepGhostsPhysicsFree()
        {
            var owner=new GameObject("Reference machinery footprint fixture");
            try
            {
                owner.transform.position=new Vector3(17,2,-9);owner.transform.rotation=Quaternion.Euler(0,31,0);
                foreach(var kind in DetailedKinds)foreach(float yaw in new[]{0f,90f,180f,270f})
                {
                    var rules=new CompactRules();var definition=rules.Equipment(kind);
                    definition.width*=.72f;definition.depth*=1.18f;
                    var view=CompactEquipmentVisuals.Build(kind,owner.transform,new Vector3(3,0,4),yaw,false,rules);
                    var assembly=view.transform.Find("Original working assembly");Assert.NotNull(assembly);
                    int meshCount=assembly.GetComponentsInChildren<MeshFilter>().Length;
                    Assert.That(meshCount,Is.InRange(1,2));
                    int triangles=0;
                    foreach(var filter in assembly.GetComponentsInChildren<MeshFilter>())
                    {
                        var mesh=filter.sharedMesh;triangles+=mesh.triangles.Length/3;
                        Assert.AreSame(mesh,filter.GetComponent<ProceduralMeshOwner>().mesh);
                        foreach(var vertex in mesh.vertices)
                        {
                            var local=view.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                            Assert.LessOrEqual(Mathf.Abs(local.x),definition.width*.5f+.001f,kind+" fitting exceeds paid width");
                            Assert.LessOrEqual(Mathf.Abs(local.z),definition.depth*.5f+.001f,kind+" fitting exceeds paid depth");
                        }
                    }
                    Assert.Less(triangles,1400,"Attached details must remain a small original mesh assembly");
                    CompactAutomationVisuals.BuildMachineDetails(kind,view.transform,rules);
                    Assert.AreEqual(meshCount,assembly.GetComponentsInChildren<MeshFilter>().Length,"Repeated preparation retains the same roots/meshes");
                    Assert.IsEmpty(view.GetComponentsInChildren<Collider>());
                    Assert.IsEmpty(view.GetComponentsInChildren<Rigidbody>());
                    Assert.IsEmpty(view.GetComponentsInChildren<Light>());
                    Assert.IsEmpty(view.GetComponentsInChildren<CompactConveyorPortTarget>());
                    UnityEngine.Object.DestroyImmediate(view);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        [TestCase(EquipmentKind.Workbench,"Workbench",2.55f,1.35f)]
        [TestCase(EquipmentKind.Generator,"CompactGenerator",1.75f,1.25f)]
        [TestCase(EquipmentKind.Tier1Scrapper,"CompactTier1Scrapper",2.4f,2.3f)]
        public void OriginalEarlyEquipmentScalesWithItsCatalogueWithoutChangingImportedMesh(EquipmentKind kind,string name,float width,float depth)
        {
            var owner=new GameObject("Original machine scaling fixture");
            try
            {
                var source=Resources.Load<GameObject>("ScrapshiftProps/"+name);Assert.NotNull(source);
                var sourceMesh=source.GetComponentsInChildren<MeshFilter>()[0].sharedMesh;
                var vertices=sourceMesh.vertices;
                var rules=new CompactRules();var definition=rules.Equipment(kind);
                definition.width*=.73f;definition.depth*=.83f;
                var view=CompactEquipmentVisuals.Build(kind,owner.transform,Vector3.zero,90,false,rules);
                var model=view.transform.Find(name);Assert.NotNull(model);
                Assert.AreEqual(new Vector3(definition.width/width,1,definition.depth/depth),model.localScale);
                Assert.AreSame(sourceMesh,model.GetComponentsInChildren<MeshFilter>()[0].sharedMesh);
                CollectionAssert.AreEqual(vertices,sourceMesh.vertices,"Runtime dressing must not edit the authored FBX mesh");
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        [Test]
        public void BeltAndBothMouthSleevesShareExactEndpointFacesAtEveryRotation()
        {
            var owner=new GameObject("Actual endpoint splice fixture");
            try
            {
                owner.transform.position=new Vector3(9,1,-12);owner.transform.rotation=Quaternion.Euler(0,23,0);
                foreach(float yaw in new[]{0f,90f,180f,270f})
                {
                    var rules=new CompactRules();rules.Equipment(EquipmentKind.Tier1Scrapper).depth*=1.13f;
                    rules.Equipment(EquipmentKind.Storage).depth*=.87f;
                    var state=new CompactYardState();
                    var from=new EquipmentState{id=1,kind=EquipmentKind.Tier1Scrapper,x=2,z=3,yaw=yaw};
                    var to=new EquipmentState{id=2,kind=EquipmentKind.Storage,x=7,z=-5,yaw=yaw+180};
                    state.equipment.Add(from);state.equipment.Add(to);
                    var source=CompactEquipmentVisuals.Build(from.kind,owner.transform,new Vector3(from.x,0,from.z),from.yaw,false,rules);
                    var destination=CompactEquipmentVisuals.Build(to.kind,owner.transform,new Vector3(to.x,0,to.z),to.yaw,false,rules);
                    var link=new ConveyorLink{id=3,fromId=1,toId=2,bendXFirst=true};
                    var belt=CompactAutomationVisuals.BuildBelt(link,state,rules,owner.transform);
                    var beltSteel=belt.transform.Find("Belt steel frame, rollers and supports").GetComponent<MeshFilter>();
                    CheckSplice(owner.transform,beltSteel,source.transform.Find("Worn port-collar rails").GetComponent<MeshFilter>(),
                        source.transform.Find("Output port 0"));
                    CheckSplice(owner.transform,beltSteel,destination.transform.Find("Worn port-collar rails").GetComponent<MeshFilter>(),
                        destination.transform.Find("Input port 0"));
                    Assert.IsEmpty(belt.GetComponentsInChildren<Collider>());
                    Assert.IsEmpty(belt.GetComponentsInChildren<Rigidbody>());
                    UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(destination);UnityEngine.Object.DestroyImmediate(belt);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        static void CheckSplice(Transform owner,MeshFilter belt,MeshFilter collar,Transform marker)
        {
            var endpoint=owner.InverseTransformPoint(marker.position);
            var side=owner.InverseTransformDirection(marker.right);
            foreach(float sign in new[]{-1f,1f})foreach(float edge in new[]{-.0425f,.0425f})foreach(float height in new[]{.61f,.74f})
            {
                var expected=endpoint+side*(sign*.40f+edge)+Vector3.up*(height-CompactAutomationVisuals.ItemHeight);
                AssertHasVertex(owner,belt,expected);AssertHasVertex(owner,collar,expected);
            }
        }

        static void AssertHasVertex(Transform owner,MeshFilter filter,Vector3 expected)
        {
            float nearest=float.MaxValue;
            foreach(var vertex in filter.sharedMesh.vertices)
                nearest=Mathf.Min(nearest,Vector3.Distance(owner.InverseTransformPoint(filter.transform.TransformPoint(vertex)),expected));
            Assert.Less(nearest,.0002f,filter.name+" leaves a gap at an authoritative mouth sleeve");
        }

        [TestCase(EquipmentKind.Workbench)] [TestCase(EquipmentKind.Tier1Scrapper)]
        [TestCase(EquipmentKind.Tier2Scrapper)] [TestCase(EquipmentKind.Storage)]
        public void CollarsHaveMatchingTravelDecksAndExposedRollersBelowCargoBase(EquipmentKind kind)
        {
            var owner=new GameObject("Port lane fixture");
            try
            {
                var rules=new CompactRules();var view=CompactEquipmentVisuals.Build(kind,owner.transform,Vector3.zero,0,false,rules);
                var deck=view.transform.Find("Port transport collars").GetComponent<MeshFilter>().sharedMesh;
                foreach(var vertex in deck.vertices)Assert.LessOrEqual(vertex.y,CompactAutomationVisuals.TravelSurfaceHeight+.0001f);
                var frame=view.transform.Find("Worn port-collar rails").GetComponent<MeshFilter>().sharedMesh;
                foreach(bool output in new[]{false,true})
                {
                    var marker=view.transform.Find((output?"Output port ":"Input port ")+"0");
                    foreach(float offset in new[]{.105f,.345f})
                    {
                        bool top=false;
                        foreach(var vertex in frame.vertices)
                        {
                            var local=marker.InverseTransformPoint(view.transform.TransformPoint(vertex));
                            if(Mathf.Abs(local.x)>.37f || Mathf.Abs(local.z+offset)>.025f || local.y<-.05f || local.y>.01f)continue;
                            Assert.LessOrEqual(local.y,.0001f,"Raised roller must not cut into the carried part");
                            if(Mathf.Abs(local.y)<.0001f)top=true;
                        }
                        Assert.IsTrue(top,"Exposed roller top meets the actual cargo base");
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        [TestCase(EquipmentKind.Splitter)] [TestCase(EquipmentKind.Merger)]
        public void EachJunctionMouthHasARaisedIvoryRoleStencilFacingOutward(EquipmentKind kind)
        {
            var owner=new GameObject("Junction stencil fixture");
            try
            {
                var rules=new CompactRules();var view=CompactEquipmentVisuals.Build(kind,owner.transform,Vector3.zero,270,false,rules);
                var mesh=view.transform.Find("Painted port directions").GetComponent<MeshFilter>().sharedMesh;
                var vertices=mesh.vertices;var uv=mesh.uv;
                foreach(bool output in new[]{false,true})for(int port=0;port<AutomationModel.PortCount(kind,output);port++)
                {
                    var marker=view.transform.Find((output?"Output port ":"Input port ")+port);int strokes=0;
                    for(int i=0;i<vertices.Length;i++)
                    {
                        if(uv[i].x<.55f || uv[i].x>.70f || uv[i].y<.80f || uv[i].y>.95f)continue;
                        var local=marker.InverseTransformPoint(view.transform.TransformPoint(vertices[i]));
                        if(Mathf.Abs(local.x)<.20f && local.y>.50f && local.y<.69f && local.z>-.212f && local.z<-.197f)strokes++;
                    }
                    Assert.Greater(strokes,50,"Every junction IN/OUT needs readable raised stencil strokes on its outward face");
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        [Test]
        public void StraightRoutesKeepOneLevelDeckThroughCollinearPortStubs()
        {
            var owner=new GameObject("Straight belt fixture");
            try
            {
                var rules=new CompactRules();var state=new CompactYardState();
                state.equipment.Add(new EquipmentState{id=1,kind=EquipmentKind.Tier1Scrapper});
                state.equipment.Add(new EquipmentState{id=2,kind=EquipmentKind.Storage,z=-8,yaw=180});
                var link=new ConveyorLink{id=3,fromId=1,toId=2};
                var belt=CompactAutomationVisuals.BuildBelt(link,state,rules,owner.transform);
                var track=belt.transform.Find("Rubber travel surface").GetComponent<MeshFilter>().sharedMesh;
                foreach(var vertex in track.vertices)Assert.LessOrEqual(vertex.y,CompactAutomationVisuals.TravelSurfaceHeight+.0001f,
                    "A collinear endpoint stub must not create a raised elbow plate");
                var marks=belt.transform.Find("Painted conveyor direction").GetComponent<MeshFilter>().sharedMesh;
                foreach(var normal in marks.normals)Assert.Greater(normal.y,.99f);
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }
    }
}
