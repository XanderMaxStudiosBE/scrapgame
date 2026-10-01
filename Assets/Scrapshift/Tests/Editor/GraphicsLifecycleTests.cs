using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Scrapshift.Tests
{
    public sealed class GraphicsLifecycleTests
    {
        [Test]
        public void PresetsUseAPrivatePipelineAndRestoreEditorGlobals()
        {
            var original=QualitySettings.renderPipeline;int frame=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
            var root=new GameObject("Graphics lifecycle test");
            var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();
            var source=UniversalRenderPipelineAsset.Create(renderer);
            PresentationSettings settings=null;
            try
            {
                source.renderScale=1;source.shadowDistance=47;
                QualitySettings.renderPipeline=source;Application.targetFrameRate=93;QualitySettings.vSyncCount=2;
                var camera=root.AddComponent<Camera>();camera.fieldOfView=69;
                var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;
                var light=root.AddComponent<Light>();light.type=LightType.Directional;light.shadows=LightShadows.Soft;
                settings=new PresentationSettings(camera,root.transform);
                settings.Preferences.graphics=GraphicsPreset.Laptop;settings.Preferences.frameLimit=30;settings.Preferences.fieldOfView=80;settings.Apply();
                var clone=QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                Assert.NotNull(clone);Assert.AreNotSame(source,clone);Assert.AreEqual(.75f,clone.renderScale);Assert.AreEqual(20,clone.shadowDistance);
                Assert.AreEqual(1,source.renderScale);Assert.AreEqual(47,source.shadowDistance,"Source asset stays intact");
                Assert.AreEqual(30,Application.targetFrameRate);Assert.AreEqual(0,QualitySettings.vSyncCount);Assert.AreEqual(80,camera.fieldOfView);
                Assert.IsFalse(data.renderPostProcessing);Assert.AreEqual(LightShadows.Hard,light.shadows);
                settings.Dispose();settings.Dispose();
                Assert.AreSame(source,QualitySettings.renderPipeline);Assert.AreEqual(93,Application.targetFrameRate);Assert.AreEqual(2,QualitySettings.vSyncCount);
                Assert.AreEqual(69,camera.fieldOfView);Assert.IsFalse(data.renderPostProcessing);Assert.AreEqual(LightShadows.Soft,light.shadows);
            }
            finally
            {
                if(settings!=null)settings.Dispose();QualitySettings.renderPipeline=original;Application.targetFrameRate=frame;QualitySettings.vSyncCount=vsync;
                Object.DestroyImmediate(root);Object.DestroyImmediate(source);Object.DestroyImmediate(renderer);
            }
        }
        [Test]
        public void AuthoredStationsKeepWorkingPartsAndOriginalMarkers()
        {
            var root=new GameObject("Authored station test");
            try
            {
                var stripper=WireStripperVisual.Build(root.transform,Vector3.zero);
                Assert.NotNull(stripper.root.transform.Find("PoweredStripper"));
                Assert.AreEqual(TargetKind.Machine,stripper.root.GetComponent<InteractionTarget>().kind);
                var collision=stripper.root.GetComponent<BoxCollider>();Assert.NotNull(collision);
                Assert.AreEqual(new Vector3(2.35f,1.20f,1.22f),collision.size);
                Assert.AreEqual(new Vector3(.58f,1.19f,-.755f),stripper.rotor.localPosition);
                Assert.AreEqual(new Vector3(-.61f,.76f,-.96f),stripper.outputDisplay.localPosition);
                Assert.NotNull(stripper.additionalRoller);Assert.NotNull(stripper.statusLamp);
                Assert.IsFalse(stripper.outputDisplay.gameObject.activeSelf);Assert.IsFalse(stripper.feedDisplay.gameObject.activeSelf);
                var buyer=YardProps.Buyer(root.transform,new Vector3(4,0,0));
                Assert.NotNull(buyer.transform.Find("BuyingScale"));Assert.AreEqual(TargetKind.Sell,buyer.GetComponent<InteractionTarget>().kind);
                foreach(var collider in buyer.GetComponentsInChildren<Collider>())Assert.AreEqual(TargetKind.Sell,collider.GetComponentInParent<InteractionTarget>().kind);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
