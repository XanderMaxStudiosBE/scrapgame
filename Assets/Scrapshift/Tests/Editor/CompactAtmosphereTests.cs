using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactAtmosphereTests
    {
        [Test]
        public void CompactProfileReplacesOnlyUntunedLegacyDefault()
        {
            var legacy=Resources.Load<YardLightingProfile>("ScrapshiftLighting/CozyAfternoon");
            var compact=Resources.Load<YardLightingProfile>("ScrapshiftLighting/CompactAfternoon");
            Assert.NotNull(legacy);Assert.NotNull(compact);compact.Validate();
            Assert.AreSame(compact,CompactYardBootstrap.ResolveLightingProfile(null));
            Assert.AreSame(compact,CompactYardBootstrap.ResolveLightingProfile(legacy));
            var custom=Object.Instantiate(legacy);
            try
            {
                custom.lampIntensity=1.6f;
                Assert.AreSame(custom,CompactYardBootstrap.ResolveLightingProfile(custom));
                Assert.AreEqual(1.6f,custom.lampIntensity);Assert.AreEqual(3.4f,legacy.lampIntensity);
            }
            finally{Object.DestroyImmediate(custom);}
        }

        [Test]
        public void CompactDaylightFacesOfficeAndKeepsFourLightBudget()
        {
            var root=new GameObject("Compact daylight test");YardLighting lighting=null;
            try
            {
                var camera=root.AddComponent<Camera>();
                var profile=Resources.Load<YardLightingProfile>("ScrapshiftLighting/CompactAfternoon");
                lighting=YardLighting.Build(root.transform,camera,profile,new[]{
                    CompactYardWorld.ShopAnchor+new Vector3(0,2.65f,-.35f),
                    CompactYardWorld.SalesAnchor+new Vector3(0,2.65f,-.35f),CompactYardWorld.DeliveryLightAnchor});
                Assert.AreEqual(4,root.GetComponentsInChildren<Light>().Length);
                Assert.Greater(Vector3.Dot(-lighting.Sun.transform.forward,Vector3.forward),.35f,"Office frontage receives daylight");
                foreach(var lamp in lighting.WorkLights)
                {
                    Assert.LessOrEqual(lamp.intensity,1.1f);Assert.LessOrEqual(lamp.spotAngle,80);
                    Assert.Less(lamp.innerSpotAngle,50);Assert.LessOrEqual(lamp.range,5);
                    Assert.AreEqual(LightShadows.None,lamp.shadows);
                    Assert.Greater(Vector3.Dot(lamp.transform.forward,Vector3.down),.99f);
                }
            }
            finally{if(lighting!=null)lighting.Dispose();Object.DestroyImmediate(root);}
        }

        [Test]
        public void GroundCopiesKeepTrackedMapsAndOwnTheirLifetime()
        {
            var root=new GameObject("Compact surface lifecycle test");
            try
            {
                var source=YardMaterialBindings.Load("ScrapshiftMaterials/Gravel",root.transform);
                var oldMap=source.GetTexture("_BaseMap");var oldScale=source.GetTextureScale("_BaseMap");var oldColor=source.GetColor("_BaseColor");
                var owner=root.AddComponent<CompactGroundSurface>();
                var packed=owner.Create("ScrapshiftMaterials/Gravel","ScrapshiftWorld/PackedGravel",new Vector2(.4f,.4f));
                var lane=owner.Create("ScrapshiftWorld/GroundWear","ScrapshiftWorld/WheelLane",Vector2.one);
                Assert.NotNull(packed);Assert.NotNull(lane);Assert.AreNotSame(source,packed);
                Assert.AreSame(Resources.Load<Texture2D>("ScrapshiftWorld/PackedGravel"),packed.GetTexture("_BaseMap"));
                Assert.AreSame(packed.GetTexture("_BaseMap"),packed.GetTexture("_MainTex"));
                Assert.AreEqual(new Vector2(.4f,.4f),packed.GetTextureScale("_BaseMap"));
                Assert.AreEqual(packed.GetTextureScale("_BaseMap"),packed.GetTextureScale("_MainTex"));
                Assert.AreEqual(oldMap,source.GetTexture("_BaseMap"));Assert.AreEqual(oldScale,source.GetTextureScale("_BaseMap"));Assert.AreEqual(oldColor,source.GetColor("_BaseColor"));
                Assert.AreEqual(1,lane.GetFloat("_Surface"));Assert.AreEqual(0,lane.GetFloat("_ZWrite"));
                Assert.IsNull(owner.Create("ScrapshiftMaterials/Gravel","ScrapshiftWorld/UnavailableTexture",Vector2.one));
                owner.Dispose();owner.Dispose();Assert.IsTrue(packed==null);Assert.IsTrue(lane==null);
                Assert.AreEqual(oldMap,source.GetTexture("_BaseMap"));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
