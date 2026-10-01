using NUnit.Framework;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class PresentationVisualTests
    {
        [TestCase("GravelStep")][TestCase("ToolStroke")][TestCase("Pickup")][TestCase("Sale")]
        [TestCase("StripperLoop")][TestCase("FanLoop")][TestCase("YardAmbience")]
        public void OriginalSoundsImportAtBoundedMonoSize(string name)
        {
            var clip=Resources.Load<AudioClip>("ScrapshiftAudio/"+name);
            Assert.NotNull(clip);Assert.AreEqual(1,clip.channels);Assert.AreEqual(22050,clip.frequency);
            Assert.Greater(clip.length,.1f);Assert.LessOrEqual(clip.length,16.01f);
            float[] samples=new float[clip.samples];Assert.IsTrue(clip.GetData(samples,0));
            float peak=0;foreach(float sample in samples){Assert.IsFalse(float.IsNaN(sample));peak=Mathf.Max(peak,Mathf.Abs(sample));}
            Assert.Greater(peak,.001f);Assert.LessOrEqual(peak,.71f);
        }
        [Test]
        public void MountedSignsDoNotBlockStationInteractions()
        {
            var root=new GameObject("Mounted sign test");
            try
            {
                YardGeometry.MountedSign(root.transform,"TEST STATION",Vector3.zero);
                Assert.IsNotEmpty(root.GetComponentsInChildren<MeshFilter>());
                Assert.IsNotEmpty(root.GetComponentsInChildren<TextMesh>());
                foreach(var collider in root.GetComponentsInChildren<Collider>())Assert.IsFalse(collider.enabled);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
