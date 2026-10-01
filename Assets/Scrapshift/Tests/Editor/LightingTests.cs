using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Scrapshift.Tests
{
    public sealed class LightingTests
    {
        [Test]
        public void TrackedProfileHasReadableSkyAndBoundedValues()
        {
            var profile=Resources.Load<YardLightingProfile>("ScrapshiftLighting/CozyAfternoon");
            Assert.NotNull(profile);profile.Validate();
            Assert.AreEqual(profile.zenith,profile.SkyColor(Vector3.up));
            Assert.AreEqual(profile.horizon,profile.SkyColor(Vector3.right));
            Assert.AreEqual(profile.ground,profile.SkyColor(Vector3.down));
            Assert.Less(profile.sunColor.r-profile.sunColor.b,.25f,"Warm daylight avoids a strong orange cast");
            var invalid=Object.Instantiate(profile);
            try
            {
                invalid.sunIntensity=float.NaN;Assert.Throws<System.ArgumentException>(invalid.Validate);
                invalid.sunIntensity=profile.sunIntensity;invalid.skyFill=new Color(-1,0,0);Assert.Throws<System.ArgumentException>(invalid.Validate);
            }
            finally{Object.DestroyImmediate(invalid);}
        }
        [TestCase("YardSky","Scrapshift/Cozy Yard Sky")]
        [TestCase("ContactShade","Scrapshift/Soft Contact Shade")]
        public void LightingShadersAreTrackedAndCompile(string name,string shader)
        {
            var material=Resources.Load<Material>("ScrapshiftLighting/"+name);
            Assert.NotNull(material);Assert.NotNull(material.shader);Assert.AreEqual(shader,material.shader.name);
            Assert.IsFalse(ShaderUtil.ShaderHasError(material.shader));
        }
        [Test]
        public void LightingUsesFourBoundedLightsAndRestoresSceneGlobals()
        {
            var root=new GameObject("Lighting lifecycle test");YardLighting lighting=null;
            var oldSky=RenderSettings.skybox;var oldSun=RenderSettings.sun;var oldReflection=RenderSettings.customReflectionTexture;
            var oldReflectionMode=RenderSettings.defaultReflectionMode;float oldReflectionIntensity=RenderSettings.reflectionIntensity;
            var oldMode=RenderSettings.ambientMode;var oldFill=RenderSettings.ambientSkyColor;var oldSide=RenderSettings.ambientEquatorColor;var oldGround=RenderSettings.ambientGroundColor;float oldAmbient=RenderSettings.ambientIntensity;
            bool oldFog=RenderSettings.fog;var oldFogMode=RenderSettings.fogMode;var oldFogColor=RenderSettings.fogColor;float oldFogStart=RenderSettings.fogStartDistance,oldFogEnd=RenderSettings.fogEndDistance;
            var tracked=Resources.Load<Material>("ScrapshiftLighting/YardSky");
            try
            {
                var camera=root.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;
                var profile=Resources.Load<YardLightingProfile>("ScrapshiftLighting/CozyAfternoon");
                lighting=YardLighting.Build(root.transform,camera,profile);
                Assert.AreEqual(4,root.GetComponentsInChildren<Light>().Length);
                Assert.AreEqual(LightType.Directional,lighting.Sun.type);Assert.AreEqual(LightShadows.Soft,lighting.Sun.shadows);
                Assert.AreEqual(3,lighting.WorkLights.Length);
                foreach(var light in lighting.WorkLights)
                {Assert.AreEqual(LightType.Spot,light.type);Assert.AreEqual(LightShadows.None,light.shadows);Assert.LessOrEqual(light.range,5);Assert.Greater(Vector3.Dot(light.transform.forward,Vector3.down),.99f);}
                Assert.AreNotSame(tracked,RenderSettings.skybox,"Shared material is not changed");
                Assert.AreEqual(CameraClearFlags.Skybox,camera.clearFlags);
                Assert.AreEqual(profile.horizon,RenderSettings.fogColor);
                Assert.AreEqual(DefaultReflectionMode.Custom,RenderSettings.defaultReflectionMode);
                Assert.AreEqual(32,RenderSettings.customReflectionTexture.width);
                Assert.AreEqual(AmbientMode.Trilight,RenderSettings.ambientMode);
                Assert.IsEmpty(lighting.GetComponentsInChildren<Collider>(),"Fixtures must not block interaction rays");
                lighting.Dispose();lighting.Dispose();
                Assert.AreSame(oldSky,RenderSettings.skybox);Assert.AreSame(oldSun,RenderSettings.sun);Assert.AreSame(oldReflection,RenderSettings.customReflectionTexture);
                Assert.AreEqual(oldReflectionMode,RenderSettings.defaultReflectionMode);Assert.AreEqual(oldReflectionIntensity,RenderSettings.reflectionIntensity);
                Assert.AreEqual(oldMode,RenderSettings.ambientMode);Assert.AreEqual(oldFill,RenderSettings.ambientSkyColor);Assert.AreEqual(oldSide,RenderSettings.ambientEquatorColor);Assert.AreEqual(oldGround,RenderSettings.ambientGroundColor);Assert.AreEqual(oldAmbient,RenderSettings.ambientIntensity);
                Assert.AreEqual(oldFog,RenderSettings.fog);Assert.AreEqual(oldFogMode,RenderSettings.fogMode);Assert.AreEqual(oldFogColor,RenderSettings.fogColor);Assert.AreEqual(oldFogStart,RenderSettings.fogStartDistance);Assert.AreEqual(oldFogEnd,RenderSettings.fogEndDistance);
                Assert.AreEqual(CameraClearFlags.SolidColor,camera.clearFlags);Assert.AreEqual(Color.magenta,camera.backgroundColor);
            }
            finally{if(lighting!=null)lighting.Dispose();Object.DestroyImmediate(root);}
        }
        [Test]
        public void SoftShadowCapabilityUpgradeIsIdempotentAndKeepsPipelineValues()
        {
            var renderer=ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
            var pipeline=UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.Create(renderer);
            try
            {
                pipeline.renderScale=.85f;pipeline.shadowDistance=29;pipeline.maxAdditionalLightsCount=2;
                Assert.IsTrue(PresetShadowSupport.Enable(pipeline));Assert.IsTrue(pipeline.supportsSoftShadows);
                Assert.IsFalse(PresetShadowSupport.Enable(pipeline),"idempotent migration");
                Assert.AreEqual(.85f,pipeline.renderScale);Assert.AreEqual(29,pipeline.shadowDistance);Assert.AreEqual(2,pipeline.maxAdditionalLightsCount);
            }
            finally{Object.DestroyImmediate(pipeline);Object.DestroyImmediate(renderer);}
        }
        [Test]
        public void ApplianceGroundingFollowsRotatedRowsInOneOwnedMesh()
        {
            var root=new GameObject("Rotated grounding test");
            try
            {
                Assert.IsTrue(YardWorldDressing.TryPlace("ApplianceRow",root.transform,new Vector3(12,0,-3),out GameObject row));
                row.transform.localRotation=Quaternion.Euler(0,90,0);
                var shade=YardContactShadows.Build(root.transform);var mesh=shade.GetComponent<MeshFilter>().sharedMesh;
                Assert.AreEqual(25,mesh.vertexCount);Assert.That(mesh.bounds.size.x,Is.InRange(1.6f,1.8f));Assert.That(mesh.bounds.size.z,Is.InRange(4.5f,4.7f));
                Assert.IsEmpty(shade.GetComponentsInChildren<Collider>());Assert.IsEmpty(shade.GetComponentsInChildren<Light>());
                Assert.AreSame(mesh,shade.GetComponent<ProceduralMeshOwner>().mesh);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void ContactGroundingIsOneSmallMeshWithoutCollisionOrLights()
        {
            var root=new GameObject("Contact grounding test");
            try
            {
                YardProps.Workbench(root.transform,new Vector3(-2.5f,0,4));YardProps.Delivery(root.transform,new Vector3(-7,0,2));
                var electronics=YardProps.Delivery(root.transform,YardBootstrap.StationPosition(YardLandmark.RadioSupply));
                electronics.GetComponent<InteractionTarget>().kind=TargetKind.RadioSupply;
                GameObject car;Assert.IsTrue(AuthoredYardProps.TryPlace("WornHatchback",root.transform,new Vector3(5,0,0),out car));
                var shade=YardContactShadows.Build(root.transform);
                Assert.NotNull(shade);Assert.IsEmpty(shade.GetComponentsInChildren<Collider>());Assert.IsEmpty(shade.GetComponentsInChildren<Light>());
                Assert.AreEqual(1,shade.GetComponentsInChildren<MeshRenderer>().Length);
                var renderer=shade.GetComponent<MeshRenderer>();Assert.AreEqual(ShadowCastingMode.Off,renderer.shadowCastingMode);Assert.IsFalse(renderer.receiveShadows);
                var mesh=shade.GetComponent<MeshFilter>().sharedMesh;Assert.Less(mesh.vertexCount,225);Assert.AreEqual(mesh.vertexCount,mesh.colors.Length);
                Assert.IsTrue(mesh.bounds.Contains(YardBootstrap.StationPosition(YardLandmark.RadioSupply)+Vector3.up*.006f),"Electronics crate receives contact grounding");
                foreach(var vertex in mesh.vertices){Assert.IsFalse(float.IsNaN(vertex.x));Assert.AreEqual(.006f,vertex.y,.00001f);}
                foreach(var color in mesh.colors){Assert.GreaterOrEqual(color.a,0);Assert.LessOrEqual(color.a,.25f);}
                Assert.AreSame(mesh,shade.GetComponent<ProceduralMeshOwner>().mesh);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
