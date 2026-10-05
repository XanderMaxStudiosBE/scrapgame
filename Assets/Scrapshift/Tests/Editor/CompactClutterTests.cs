using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactClutterTests
    {
        [Test]
        public void DeterministicStockKeepsCentralFloorOpenAndFitsInsidePerimeter()
        {
            var first=CompactYardClutter.Describe();var second=CompactYardClutter.Describe();
            Assert.AreEqual(29,first.Length);var names=new HashSet<string>();var sectors=new HashSet<string>();
            for(int i=0;i<first.Length;i++)
            {
                var p=first[i];Assert.IsTrue(names.Add(p.name));sectors.Add(p.sector);
                Assert.AreEqual(p.name,second[i].name);Assert.AreEqual(p.footprint,second[i].footprint);
                if(p.outside)continue;
                if(p.sector.Contains("working stock"))
                {
                    Assert.GreaterOrEqual(p.footprint.min.x,-24);Assert.LessOrEqual(p.footprint.max.x,24);
                    Assert.GreaterOrEqual(p.footprint.min.z,-18);Assert.LessOrEqual(p.footprint.max.z,18);
                    Assert.IsTrue(p.footprint.max.x<=-17 || p.footprint.min.x>=17 || p.footprint.min.z>=14,
                        p.name+" must leave the central walking and construction view open");
                    Assert.GreaterOrEqual(p.footprint.size.y,2);
                }
                else if(p.sector=="Flat ground offcuts")Assert.LessOrEqual(p.footprint.max.y,.04f);
                else Assert.AreEqual("Service pocket offcuts",p.sector);
            }
            Assert.AreEqual(8,sectors.Count);
        }

        [Test]
        public void OccupancyUsesFootprintsAndClearanceEvenWhenObjectHeightsDiffer()
        {
            var patch=CompactYardClutter.Describe()[0];
            var high=new Bounds(patch.footprint.center+Vector3.up*100,new Vector3(.2f,.2f,.2f));
            Assert.IsTrue(CompactYardClutter.IsBlocked(patch,new[]{high}));
            var near=new Bounds(new Vector3(patch.footprint.max.x+.29f,0,patch.footprint.center.z),new Vector3(.1f,1,.1f));
            Assert.IsTrue(CompactYardClutter.IsBlocked(patch,new[]{near}));
            var far=new Bounds(new Vector3(patch.footprint.max.x+.51f,0,patch.footprint.center.z),new Vector3(.1f,1,.1f));
            Assert.IsFalse(CompactYardClutter.IsBlocked(patch,new[]{far}));
            foreach(var p in CompactYardClutter.Describe())Assert.AreEqual(!p.outside,CompactYardClutter.IsBlocked(p,null));
        }

        [Test]
        public void BuiltGeometryFitsDeclaredBoundsAndHasNoGameplayOrPhysicsComponents()
        {
            var yard=new GameObject("Clutter containment test");yard.transform.position=new Vector3(300,0,100);
            try
            {
                var clutter=CompactYardClutter.Build(yard.transform,new Bounds[0],false);
                Assert.IsEmpty(clutter.GetComponentsInChildren<Collider>(true));
                Assert.IsEmpty(clutter.GetComponentsInChildren<Rigidbody>(true));
                Assert.IsEmpty(clutter.GetComponentsInChildren<Light>(true));
                Assert.IsEmpty(clutter.GetComponentsInChildren<CompactInteractionTarget>(true));
                Assert.LessOrEqual(clutter.GetComponentsInChildren<MeshRenderer>(true).Length,228,
                    "Recognizable stock must stay below the previous 203-renderer budget plus 25");
                int triangles=0;
                foreach(var filter in clutter.GetComponentsInChildren<MeshFilter>(true))triangles+=filter.sharedMesh.triangles.Length/3;
                Assert.LessOrEqual(triangles,45012,"Keep stock below the previous 33012 triangles plus 12000, including one optional local piston");
                foreach(var patch in CompactYardClutter.Describe())
                {
                    var cluster=Find(clutter.transform,patch.name);Assert.NotNull(cluster,patch.name);
                    foreach(var filter in cluster.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var b=filter.sharedMesh.bounds;
                        for(int i=0;i<8;i++)
                        {
                            var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                            var point=yard.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                            var envelope=patch.footprint;envelope.Expand(.002f);
                            Assert.IsTrue(envelope.Contains(point),patch.name+" / "+filter.name+" exceeds envelope at "+point);
                        }
                    }
                }
            }
            finally{Object.DestroyImmediate(yard);}
        }

        [TestCase(false)][TestCase(true)]
        public void RefreshHidesAndRestoresPooledClustersWithoutRebuildingMeshes(bool combine)
        {
            var yard=new GameObject("Clutter occupancy refresh test");
            try
            {
                var occupied=new[]{new Bounds(Vector3.zero,new Vector3(200,200,200))};
                var root=CompactYardClutter.Build(yard.transform,occupied,combine);
                var meshes=root.GetComponentsInChildren<MeshFilter>(true);Assert.IsNotEmpty(meshes);
                foreach(var patch in CompactYardClutter.Describe())Assert.IsFalse(Find(root.transform,patch.name).gameObject.activeSelf);
                CompactYardClutter.RefreshVisibility(root,new Bounds[0]);
                foreach(var patch in CompactYardClutter.Describe())Assert.IsTrue(Find(root.transform,patch.name).gameObject.activeSelf);
                CollectionAssert.AreEqual(meshes,root.GetComponentsInChildren<MeshFilter>(true));
                CompactYardClutter.RefreshVisibility(root,occupied);
                foreach(var patch in CompactYardClutter.Describe())Assert.IsFalse(Find(root.transform,patch.name).gameObject.activeSelf);
            }
            finally{Object.DestroyImmediate(yard);}
        }

        [Test]
        public void SavedEquipmentOccupancyHidesNearbyStockAndRestoresItWithoutDeletingCandidates()
        {
            var yard=new GameObject("Working stock occupancy test");
            try
            {
                var root=CompactYardClutter.Build(yard.transform,new Bounds[0],false);
                var occupied=new[]{new Bounds(new Vector3(-15,1,15.8f),new Vector3(2,2,2))};
                CompactYardClutter.RefreshVisibility(root,occupied);
                Assert.IsFalse(Find(root.transform,"North working stock 0").gameObject.activeSelf);
                Assert.IsTrue(Find(root.transform,"North working stock 4").gameObject.activeSelf);
                var hidden=Find(root.transform,"North working stock 0");
                CompactYardClutter.RefreshVisibility(root,new Bounds[0]);
                Assert.AreSame(hidden,Find(root.transform,"North working stock 0"));
                Assert.IsTrue(hidden.gameObject.activeSelf);
            }
            finally{Object.DestroyImmediate(yard);}
        }

        [TestCase("CompactSalvageShell",1596)]
        [TestCase("CompactWasherLot",1648)]
        [TestCase("CompactCableReel",1620)]
        [TestCase("CompactRadiatorRack",780)]
        public void NewRecognizableSalvageUsesReadableOriginalImportedMeshesWithoutOwnedSourceGeometry(string resource,int expectedTriangles)
        {
            var yard=new GameObject("Original salvage import test");
            try
            {
                Assert.IsTrue(AuthoredYardProps.TryPlace(resource,yard.transform,Vector3.zero,out GameObject instance),resource);
                var filters=instance.GetComponentsInChildren<MeshFilter>();Assert.AreEqual(1,filters.Length);
                var mesh=filters[0].sharedMesh;Assert.NotNull(mesh);Assert.IsTrue(mesh.isReadable);
                Assert.AreEqual(expectedTriangles,mesh.triangles.Length/3);Assert.LessOrEqual(expectedTriangles,3500);
                Assert.Greater(mesh.vertexCount,0);Assert.AreEqual(mesh.vertexCount,mesh.normals.Length);
                foreach(var vertex in mesh.vertices)
                    Assert.IsFalse(float.IsNaN(vertex.x)||float.IsNaN(vertex.y)||float.IsNaN(vertex.z)||
                                   float.IsInfinity(vertex.x)||float.IsInfinity(vertex.y)||float.IsInfinity(vertex.z),resource);
                Assert.IsEmpty(instance.GetComponentsInChildren<ProceduralMeshOwner>(),"Imported meshes remain Unity-owned");
                Assert.IsEmpty(instance.GetComponentsInChildren<Collider>());
                Assert.IsEmpty(instance.GetComponentsInChildren<Rigidbody>());
                Assert.IsEmpty(instance.GetComponentsInChildren<Light>());
                Assert.AreSame(YardMaterialBindings.Load("ScrapshiftMaterials/PropAtlas",yard.transform),filters[0].GetComponent<MeshRenderer>().sharedMaterial);
                Object.DestroyImmediate(instance);
                Assert.IsTrue(mesh!=null,"Destroying a stock view must retain its shared imported source mesh");
            }
            finally{Object.DestroyImmediate(yard);}
        }

        [Test]
        public void PerimeterStockVariesRecognizableSilhouettesWithinTheSamePooledRoots()
        {
            var yard=new GameObject("Recognizable salvage composition test");
            try
            {
                var root=CompactYardClutter.Build(yard.transform,new Bounds[0],false,true);
                Assert.AreEqual(1,Count(root,"CompactSalvageShell"));
                Assert.AreEqual(2,Count(root,"CompactWasherLot"));
                Assert.AreEqual(5,Count(root,"CompactCableReel"));
                Assert.AreEqual(5,Count(root,"CompactRadiatorRack"));
                Assert.AreEqual(8,Count(root,"Discarded rubber seal"));
                Assert.AreEqual(29,CompactYardClutter.Describe().Length);
                foreach(var patch in CompactYardClutter.Describe())Assert.NotNull(Find(root.transform,patch.name));
                Assert.AreEqual(13,root.GetComponentsInChildren<BoxCollider>(true).Length);
                var shell=Find(root.transform,"CompactSalvageShell");
                Assert.AreEqual("West working stock 0",shell.parent.name);
                var occupied=new[]{new Bounds(new Vector3(-20.8f,1,-1),new Vector3(2,2,2))};
                CompactYardClutter.RefreshVisibility(root,occupied);
                Assert.IsFalse(shell.gameObject.activeInHierarchy,"A saved build releases both the stock and existing collision");
                CompactYardClutter.RefreshVisibility(root,new Bounds[0]);
                Assert.AreSame(shell,Find(root.transform,"CompactSalvageShell"));
                Assert.IsTrue(shell.gameObject.activeInHierarchy);
            }
            finally{Object.DestroyImmediate(yard);}
        }

        [Test]
        public void MissingAuthoredStockReturnsFailureAndLeavesNoPhantomModel()
        {
            var yard=new GameObject("Missing optional stock test");
            try
            {
                var place=typeof(CompactYardClutter).GetMethod("Model",BindingFlags.Static|BindingFlags.NonPublic);
                Assert.NotNull(place);
                Assert.IsFalse((bool)place.Invoke(null,new object[]{"IntentionallyMissingStock",yard.transform,Vector3.zero,Vector3.one,0f}));
                Assert.AreEqual(0,yard.transform.childCount);
                Assert.IsEmpty(yard.GetComponentsInChildren<MeshFilter>(true));
            }
            finally{Object.DestroyImmediate(yard);}
        }

        [Test]
        public void MeshlessAndZeroExtentStockReturnsFailureWithoutOwningOrDestroyingItsSource()
        {
            const string resource="IntentionallyEmptySalvageSource";
            var yard=new GameObject("Invalid optional stock test");var source=new GameObject(resource);var mesh=new Mesh();
            var cache=(Dictionary<string,GameObject>)typeof(AuthoredYardProps).GetField("Models",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            try
            {
                Assert.IsFalse(cache.ContainsKey(resource));cache.Add(resource,source);
                var place=typeof(CompactYardClutter).GetMethod("Model",BindingFlags.Static|BindingFlags.NonPublic);
                var parameters=new object[]{resource,yard.transform,Vector3.zero,Vector3.one,0f};
                Assert.IsFalse((bool)place.Invoke(null,parameters));Assert.AreEqual(0,yard.transform.childCount);
                source.AddComponent<MeshFilter>().sharedMesh=mesh;source.AddComponent<MeshRenderer>();
                Assert.IsFalse((bool)place.Invoke(null,parameters));Assert.AreEqual(0,yard.transform.childCount);
                Assert.IsTrue(source!=null && mesh!=null,"Fallback must dispose only its private failed instance");
                Assert.AreSame(mesh,source.GetComponent<MeshFilter>().sharedMesh);
            }
            finally{cache.Remove(resource);Object.DestroyImmediate(yard);Object.DestroyImmediate(source);Object.DestroyImmediate(mesh);}
        }

        [Test]
        public void StockContactGroundingIsSoftContainedOwnedAndFacesUpward()
        {
            var yard=new GameObject("Stock contact grounding test");
            try
            {
                var root=CompactYardClutter.Build(yard.transform,new Bounds[0],false);int count=0;
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    if(filter.name!="Stock contact grounding")continue;
                    count++;Assert.NotNull(filter.GetComponent<ProceduralMeshOwner>());
                    var mesh=filter.sharedMesh;var vertices=mesh.vertices;var colors=mesh.colors;
                    Assert.AreEqual(vertices.Length,colors.Length);
                    bool softEdge=false;
                    foreach(var color in colors){Assert.That(color.a,Is.InRange(0,.22f));if(color.a==0)softEdge=true;}
                    Assert.IsTrue(softEdge);
                    foreach(var vertex in vertices)Assert.AreEqual(.006f,yard.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex)).y,.00001f);
                    var triangles=mesh.triangles;
                    for(int i=0;i<triangles.Length;i+=3)
                        Assert.Greater(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).y,0);
                    Assert.AreSame(Resources.Load<Material>("ScrapshiftLighting/ContactShade"),filter.GetComponent<MeshRenderer>().sharedMaterial);
                    Assert.IsEmpty(filter.GetComponents<Collider>());
                }
                Assert.AreEqual(21,count);
            }
            finally{Object.DestroyImmediate(yard);}
        }

        [Test]
        public void SolidStockUsesThirteenReplaceableIgnoreRaycastVolumesWithMatchingEnvelopes()
        {
            var yard=new GameObject("Solid stock envelope test");
            try
            {
                var root=CompactYardClutter.Build(yard.transform,new Bounds[0],false,true);
                var colliders=root.GetComponentsInChildren<BoxCollider>(true);Assert.AreEqual(13,colliders.Length);
                Assert.IsEmpty(root.GetComponentsInChildren<Rigidbody>(true));Assert.IsEmpty(root.GetComponentsInChildren<CompactInteractionTarget>(true));
                Physics.SyncTransforms();
                foreach(var patch in CompactYardClutter.Describe())
                {
                    var cluster=Find(root.transform,patch.name);var collider=cluster.GetComponent<BoxCollider>();
                    if(patch.outside || patch.sector=="Flat ground offcuts"){Assert.IsNull(collider);continue;}
                    Assert.NotNull(collider);Assert.AreEqual(2,cluster.gameObject.layer);
                    Assert.Less(Vector3.Distance(patch.footprint.center,collider.bounds.center),.001f);
                    Assert.Less(Vector3.Distance(patch.footprint.size,collider.bounds.size),.001f);
                    var occupied=new[]{new Bounds(patch.footprint.center,new Vector3(.1f,1,.1f))};
                    CompactYardClutter.RefreshVisibility(root,occupied);
                    Assert.IsFalse(collider.gameObject.activeInHierarchy);
                    CompactYardClutter.RefreshVisibility(root,new Bounds[0]);Assert.IsTrue(collider.gameObject.activeInHierarchy);
                }
            }
            finally{Object.DestroyImmediate(yard);}
        }

        [Test]
        public void PlayerControllerCollidesWithStockThenCanCrossAfterOccupancyHidesIt()
        {
            var yard=new GameObject("Solid stock controller test");yard.transform.position=new Vector3(1000,0,1000);
            try
            {
                Assert.IsFalse(Physics.GetIgnoreLayerCollision(2,2),"Player and replaceable stock require IgnoreRaycast layer self-collision in the project matrix.");
                var root=CompactYardClutter.Build(yard.transform,new Bounds[0],false,true);
                var actor=new GameObject("Test player");actor.transform.SetParent(yard.transform,false);actor.layer=2;
                actor.transform.localPosition=new Vector3(-15,1,11.5f);
                var controller=actor.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.3f;
                Physics.SyncTransforms();controller.Move(Vector3.forward*4);
                Assert.Less(actor.transform.localPosition.z,14.2f,"The stock front must block the player capsule.");
                CompactYardClutter.RefreshVisibility(root,new[]{new Bounds(new Vector3(-15,1,15.8f),new Vector3(2,2,2))});
                Physics.SyncTransforms();controller.Move(Vector3.forward*2.5f);
                Assert.Greater(actor.transform.localPosition.z,14.2f,"Hidden stock must release its collision volume.");
            }
            finally{Object.DestroyImmediate(yard);}
        }

        static Transform Find(Transform root,string name)
        {
            foreach(var child in root.GetComponentsInChildren<Transform>(true))if(child.name==name)return child;
            return null;
        }
        static int Count(GameObject root,string name)
        {
            int count=0;foreach(var child in root.GetComponentsInChildren<Transform>(true))if(child.name==name)count++;
            return count;
        }
    }
}
