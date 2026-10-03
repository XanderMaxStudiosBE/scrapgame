using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactClutterTests
    {
        [Test]
        public void DeterministicDescriptorsKeepBulkSalvageOutsideConstructionSpace()
        {
            var first=CompactYardClutter.Describe();var second=CompactYardClutter.Describe();
            Assert.AreEqual(45,first.Length);var names=new HashSet<string>();var sectors=new HashSet<string>();
            for(int i=0;i<first.Length;i++)
            {
                var p=first[i];Assert.IsTrue(names.Add(p.name));sectors.Add(p.sector);
                Assert.AreEqual(p.name,second[i].name);Assert.AreEqual(p.footprint,second[i].footprint);
                if(p.outside)continue;
                if(p.sector.Contains("fence"))
                    Assert.IsTrue(p.footprint.min.x>=23.25f || p.footprint.max.x<=-23.25f || p.footprint.min.z>=17.25f);
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
                Assert.LessOrEqual(clutter.GetComponentsInChildren<MeshRenderer>(true).Length,260);
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

        static Transform Find(Transform root,string name)
        {
            foreach(var child in root.GetComponentsInChildren<Transform>(true))if(child.name==name)return child;
            return null;
        }
    }
}
