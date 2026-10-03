using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactBackdropTests
    {
        [Test]
        public void CatalogueRejectsInteriorAndUnboundedTransforms()
        {
            var layout=CompactYardBackdrop.ReadLayout();Assert.AreEqual(23,layout.placements.Length);
            var item=layout.placements[0];float x=item.x,z=item.z;
            item.x=0;item.z=0;Assert.Throws<ArgumentException>(()=>layout.Validate());item.x=x;item.z=z;
            item.scale=float.NaN;Assert.Throws<ArgumentException>(()=>layout.Validate());item.scale=1;
            item.model="Workbench";Assert.Throws<ArgumentException>(()=>layout.Validate());
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualImportedBackdropIsExteriorStaticAndUsesRecoveredWorldAtlas(bool combine)
        {
            var host=new GameObject("Backdrop import and batching check");
            try
            {
                host.transform.position=new Vector3(103,4,-71);host.transform.rotation=Quaternion.Euler(0,31,0);
                var backdrop=CompactYardBackdrop.Build(host.transform,combine);
                Assert.AreEqual(6,backdrop.transform.childCount);
                Assert.IsEmpty(backdrop.GetComponentsInChildren<Collider>());Assert.IsEmpty(backdrop.GetComponentsInChildren<Rigidbody>());
                Assert.IsEmpty(backdrop.GetComponentsInChildren<Light>());Assert.IsEmpty(backdrop.GetComponentsInChildren<MonoBehaviour>(),"Scenery must not install per-frame or gameplay components");
                var material=YardMaterialBindings.Load("ScrapshiftWorld/WorldProps",backdrop.transform);Assert.NotNull(material);
                var renderers=backdrop.GetComponentsInChildren<MeshRenderer>();Assert.AreEqual(23,renderers.Length);
                foreach(var renderer in renderers)
                {
                    Assert.AreSame(material,renderer.sharedMaterial);
                    Assert.AreEqual(ShadowCastingMode.Off,renderer.shadowCastingMode);
                    if(combine)Assert.IsTrue(renderer.isPartOfStaticBatch,"Imported scenery must participate in its regional static batch");
                }
                // Unbatched geometry verifies imported vertices in yard coordinates;
                // the batched case verifies renderer membership and non-empty bounds.
                int triangles=0;
                foreach(Transform sector in backdrop.transform)
                    foreach(Transform instance in sector)
                    {
                        bool found=false;var bounds=new Bounds();
                        if(!combine)
                            foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())
                            {
                                Assert.IsTrue(filter.sharedMesh.isReadable);triangles+=filter.sharedMesh.triangles.Length/3;
                                foreach(var vertex in filter.sharedMesh.vertices)
                                {
                                    var point=backdrop.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                                    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                                }
                            }
                        else
                            foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                            {
                                // world AABBs under rotated hosts are conservative; the
                                // actual imported pre-batch case above verifies envelopes.
                                Assert.IsTrue(renderer.bounds.size.sqrMagnitude>0);found=true;
                            }
                        Assert.IsTrue(found,instance.name+" must contain imported geometry");
                        if(!combine)
                        {
                            Assert.IsTrue(bounds.max.x< -24||bounds.min.x>24||bounds.min.z>18,instance.name+" intrudes player construction area");
                            Assert.Greater(bounds.min.z,-18,instance.name+" intrudes southern entrance/road");
                            Assert.LessOrEqual(bounds.max.y,10.6f);Assert.LessOrEqual(bounds.max.z,46);
                            Assert.GreaterOrEqual(bounds.min.x,-48);Assert.LessOrEqual(bounds.max.x,48);
                        }
                    }
                if(!combine)Assert.LessOrEqual(triangles,40000);
            }
            finally{UnityEngine.Object.DestroyImmediate(host);}
        }
    }
}
