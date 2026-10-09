using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactReferenceEnvironmentTests
    {
        [Test]
        public void ReferencePassRetainsAnchorsCollisionAndHalfTheStartingFloor()
        {
            var root=new GameObject("Reference environment floor test");
            try
            {
                var handles=CompactYardWorld.Build(root.transform,false);Physics.SyncTransforms();
                Assert.AreEqual(24,CompactYardWorld.HalfWidth);Assert.AreEqual(18,CompactYardWorld.HalfDepth);
                Assert.AreEqual(new Vector3(-17,0,-14),CompactYardWorld.OfficeAnchor);
                Assert.AreEqual(new Vector3(-19,0,-10.25f),handles.Shop.transform.localPosition);
                Assert.AreEqual(new Vector3(-15,0,-10.25f),handles.Sales.transform.localPosition);
                Assert.AreEqual(new Vector3(15,0,-13),handles.Delivery.transform.localPosition);
                Assert.AreEqual(new Vector3(15,0,-7),handles.Wire.transform.localPosition);
                Assert.IsEmpty(root.GetComponentsInChildren<Rigidbody>());Assert.IsEmpty(root.GetComponentsInChildren<Light>());
                var rules=new CompactRules();var model=new ScrappingModel(rules);var occupied=new List<Bounds>();
                CompactDressingOccupancy.Collect(model.State,rules,new Vector3(0,1.1f,-13),occupied);
                // Include the existing construction service reservations, all fresh scrap/gear,
                // every interior stock module (even flat or normally hidden ones) and perimeter.
                // Counting only completely clear 0.5m cells rounds blocked space outward.
                occupied.Add(new Bounds(new Vector3(0,1,-14.5f),new Vector3(4,2,7)));
                occupied.Add(new Bounds(new Vector3(-17,1,-13.5f),new Vector3(12,2,7)));
                occupied.Add(new Bounds(new Vector3(18,1,-10),new Vector3(10,2,14)));
                foreach(var collider in root.GetComponentsInChildren<Collider>())
                    if(collider.name!="48 x 36 metre packed gravel")occupied.Add(collider.bounds);
                foreach(var patch in CompactYardClutter.Describe())if(!patch.outside)occupied.Add(patch.footprint);
                float free=0;
                for(float x=-23.75f;x<24;x+=.5f)for(float z=-17.75f;z<18;z+=.5f)
                {
                    bool blocked=false;
                    foreach(var area in occupied)
                    {
                        if(x-.25f<=area.max.x && x+.25f>=area.min.x && z-.25f<=area.max.z && z+.25f>=area.min.z)
                        {blocked=true;break;}
                    }
                    if(!blocked)free+=.25f;
                }
                Assert.GreaterOrEqual(free,864,"At least half the 1,728m2 floor stays available for construction/circulation");
                TestContext.WriteLine("Conservatively clear initial floor: "+free+" / 1728 m2");
                Assert.AreEqual(1,model.State.equipment.Count);Assert.AreEqual(rules.startingMoney,model.State.money);Assert.AreEqual(0,model.State.experience);
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test]
        public void OfficeCoverAndScreensStayWithinExistingReservedSpaceWithoutNewCollision()
        {
            var root=new GameObject("Reference environment envelope test");
            try
            {
                CompactYardWorld.Build(root.transform,false);Physics.SyncTransforms();
                var cover=root.transform.Find("Compact office and sales/Office service rain cover");
                Assert.NotNull(cover);Assert.IsEmpty(cover.GetComponentsInChildren<Collider>());
                var coverMeshes=cover.GetComponentsInChildren<MeshFilter>();Assert.AreEqual(21,coverMeshes.Length);
                int roofCasters=0,screens=0,rails=0;
                foreach(var filter in coverMeshes)
                {
                    var bounds=filter.GetComponent<MeshRenderer>().bounds;
                    Assert.GreaterOrEqual(bounds.min.x,-23);Assert.LessOrEqual(bounds.max.x,-11);
                    Assert.GreaterOrEqual(bounds.min.z,-17);Assert.LessOrEqual(bounds.max.z,-10);
                    Assert.LessOrEqual(bounds.max.y,3.3f);
                    Assert.AreSame(filter.sharedMesh,filter.GetComponent<ProceduralMeshOwner>().mesh);
                    Assert.AreEqual(12,filter.sharedMesh.triangles.Length/3);
                    if(filter.GetComponent<MeshRenderer>().shadowCastingMode!=ShadowCastingMode.Off)roofCasters++;
                }
                Assert.AreEqual(1,roofCasters);
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    if(renderer.name=="Boundary salvaged screen panel")
                    {
                        screens++;Assert.IsNull(renderer.GetComponent<Collider>());
                        var bounds=renderer.bounds;
                        Assert.LessOrEqual(bounds.max.y,2.1f);
                        bool withinBoundary=Mathf.Abs(bounds.center.x)>=23.9f || Mathf.Abs(bounds.center.z)>=17.9f;
                        Assert.IsTrue(withinBoundary,"Screens stay inside the historical boundary envelopes");
                        Assert.AreEqual(12,renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3);
                    }
                    if(renderer.name=="Boundary screen fixing rail" || renderer.name=="Boundary top support rail")
                    {rails++;Assert.AreEqual(ShadowCastingMode.Off,renderer.shadowCastingMode);Assert.IsNull(renderer.GetComponent<Collider>());}
                }
                Assert.AreEqual(36,screens);Assert.AreEqual(41,rails);
                Assert.AreEqual(19,root.GetComponentsInChildren<Collider>().Length,"Retain ground, six boundary spans, two gate uprights, office, two counters, truck, four canopy posts and two receiving targets");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test]
        public void WornRoutesUseMetreTextureTravelAndUpwardPrivateGeometry()
        {
            var root=new GameObject("Reference environment route test");
            try
            {
                CompactYardWorld.Build(root.transform,false);
                var lane=root.transform.Find("Compact ground/Worn entry and receiving wheel lanes");Assert.NotNull(lane);
                var filter=lane.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;
                Assert.AreSame(mesh,lane.GetComponent<ProceduralMeshOwner>().mesh);Assert.IsNull(lane.GetComponent<Collider>());
                Assert.AreEqual(24,mesh.vertexCount);Assert.AreEqual(18,mesh.triangles.Length/3);
                foreach(var normal in mesh.normals)Assert.Greater(normal.y,.99f);
                var vertices=mesh.vertices;var uv=mesh.uv;
                for(int route=0;route<3;route++)
                {
                    int start=route*8;Assert.AreEqual(0,uv[start].y);Assert.AreEqual(0,uv[start].x);Assert.AreEqual(1,uv[start+1].x);
                    float distance=0;
                    for(int i=1;i<4;i++)
                    {
                        var previous=(vertices[start+(i-1)*2]+vertices[start+(i-1)*2+1])*.5f;
                        var current=(vertices[start+i*2]+vertices[start+i*2+1])*.5f;
                        distance+=Vector3.Distance(previous,current);
                        Assert.That(uv[start+i*2].y,Is.EqualTo(distance/2.5f).Within(.0001f));
                        Assert.AreEqual(uv[start+i*2].y,uv[start+i*2+1].y);
                    }
                    Assert.Greater(uv[start+6].y,1,"A tiny tread map must repeat rather than stretch across the route");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
