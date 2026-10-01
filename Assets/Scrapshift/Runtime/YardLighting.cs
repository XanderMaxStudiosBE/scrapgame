using UnityEngine;
using UnityEngine.Rendering;
namespace Scrapshift
{
    // Startup-only lighting. Owns private sky/reflection/lamp assets and restores the scene's globals on exit.
    public sealed class YardLighting : MonoBehaviour
    {
        public Light Sun { get; private set; }
        public Light[] WorkLights { get; private set; }
        Material sky,bulbMaterial;
        Cubemap reflection;
        Camera view;
        Material previousSky;
        Light previousSun;
        Texture previousReflection;
        DefaultReflectionMode previousReflectionMode;
        AmbientMode previousAmbientMode;
        Color previousSkyFill,previousSideFill,previousGroundFill,previousFog,previousBackground;
        float previousReflectionIntensity,previousAmbientIntensity,previousFogStart,previousFogEnd;
        bool previousFogEnabled,initialized,disposed;
        FogMode previousFogMode;
        CameraClearFlags previousClear;
        public static YardLighting Build(Transform parent,Camera camera,YardLightingProfile profile)
        {
            profile.Validate();
            var owner=new GameObject("Cozy yard lighting");owner.transform.SetParent(parent,false);
            var lighting=owner.AddComponent<YardLighting>();lighting.Initialize(camera,profile);return lighting;
        }
        void Initialize(Camera camera,YardLightingProfile profile)
        {
            view=camera;
            previousSky=RenderSettings.skybox;previousSun=RenderSettings.sun;previousReflection=RenderSettings.customReflectionTexture;
            previousReflectionMode=RenderSettings.defaultReflectionMode;previousReflectionIntensity=RenderSettings.reflectionIntensity;
            previousAmbientMode=RenderSettings.ambientMode;previousAmbientIntensity=RenderSettings.ambientIntensity;
            previousSkyFill=RenderSettings.ambientSkyColor;previousSideFill=RenderSettings.ambientEquatorColor;previousGroundFill=RenderSettings.ambientGroundColor;
            previousFogEnabled=RenderSettings.fog;previousFogMode=RenderSettings.fogMode;previousFog=RenderSettings.fogColor;
            previousFogStart=RenderSettings.fogStartDistance;previousFogEnd=RenderSettings.fogEndDistance;
            previousClear=view.clearFlags;previousBackground=view.backgroundColor;initialized=true;
            Sun=Source("Soft afternoon daylight",LightType.Directional,Vector3.zero,profile.sunColor,profile.sunIntensity);
            Sun.transform.localRotation=Quaternion.Euler(profile.sunElevation,profile.sunAzimuth,0);
            Sun.shadows=LightShadows.Soft;Sun.shadowStrength=.82f;Sun.shadowBias=.05f;Sun.shadowNormalBias=.35f;
            RenderSettings.sun=Sun;
            var trackedSky=Resources.Load<Material>("ScrapshiftLighting/YardSky");
            if(trackedSky!=null)
            {
                sky=new Material(trackedSky){name="Private cozy yard sky",hideFlags=HideFlags.DontSave};
                sky.SetColor("_ZenithColor",profile.zenith);sky.SetColor("_HorizonColor",profile.horizon);sky.SetColor("_GroundColor",profile.ground);
                sky.SetVector("_SunDirection",-Sun.transform.forward);
                RenderSettings.skybox=sky;view.clearFlags=CameraClearFlags.Skybox;
            }
            else { Debug.LogWarning("Missing tracked yard sky material. Using the profile horizon until import completes.");view.clearFlags=CameraClearFlags.SolidColor; }
            view.backgroundColor=profile.horizon;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientIntensity=1;
            RenderSettings.ambientSkyColor=profile.skyFill;RenderSettings.ambientEquatorColor=profile.sideFill;RenderSettings.ambientGroundColor=profile.groundFill;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=profile.horizon;
            RenderSettings.fogStartDistance=profile.fogStart;RenderSettings.fogEndDistance=profile.fogEnd;
            // A tiny deterministic environment map gives copper/paint a restrained reflection without realtime probes.
            reflection=Reflection(profile);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture=reflection;RenderSettings.reflectionIntensity=.55f;
            var bench=YardBootstrap.StationPosition(YardLandmark.Bench);
            var buyer=YardBootstrap.StationPosition(YardLandmark.Buyer);
            var fan=YardBootstrap.StationPosition(YardLandmark.FanBench);
            WorkLights=new[]{Spot("Stripping bench task light",bench+new Vector3(0,2.68f,-.35f),profile),Spot("Buyer task light",buyer+new Vector3(0,2.68f,-.35f),profile),Spot("Restoration task light",fan+new Vector3(0,2.5f,-.25f),profile)};
            var trackedBulb=Resources.Load<Material>("ScrapshiftMaterials/PropAtlas");
            if(trackedBulb!=null)
            {
                bulbMaterial=new Material(trackedBulb){name="Private warm lamp diffuser",hideFlags=HideFlags.DontSave};
                bulbMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);bulbMaterial.SetColor("_BaseColor",profile.lampColor);
                bulbMaterial.EnableKeyword("_EMISSION");bulbMaterial.SetColor("_EmissionColor",profile.lampColor*1.15f);
                foreach(var lamp in WorkLights)
                {
                    bool detailed=YardWorldDressing.TryPlace("FluorescentFixture",lamp.transform,new Vector3(0,0,-.04f),out GameObject fixture);
                    if(detailed)
                    {
                        fixture.transform.localRotation=Quaternion.Euler(-90,0,0);
                        float ceiling=lamp.transform.localPosition.y<2.6f ? 3.28f : 4.28f;
                        float length=ceiling-lamp.transform.localPosition.y-.64f;
                        foreach(float x in new[]{-.34f,.34f})
                        {
                            var wire=YardGeometry.SurfaceBox("Ceiling suspension",lamp.transform,new Vector3(x,0,-.64f-length*.5f),new Vector3(.009f,.009f,length),RetroSurface.DarkMetal,false);
                            wire.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                        }
                    }
                    else
                    {
                        var housing=YardGeometry.SurfaceBox("Pendant shade",lamp.transform,new Vector3(0,0,-.04f),new Vector3(.42f,.28f,.09f),RetroSurface.DarkMetal,false);
                        housing.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                    }
                    var diffuser=YardGeometry.SurfaceBox("Warm diffuser",lamp.transform,new Vector3(0,0,.015f),detailed ? new Vector3(.80f,.15f,.015f) : new Vector3(.31f,.19f,.025f),RetroSurface.DarkMetal,false);
                    var renderer=diffuser.GetComponent<Renderer>();renderer.sharedMaterial=bulbMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
                }
            }
        }
        Light Source(string name,LightType type,Vector3 position,Color color,float intensity)
        {
            var light=new GameObject(name).AddComponent<Light>();light.transform.SetParent(transform,false);light.transform.localPosition=position;
            light.type=type;light.color=color;light.intensity=intensity;light.shadows=LightShadows.None;return light;
        }
        Light Spot(string name,Vector3 position,YardLightingProfile profile)
        {
            var light=Source(name,LightType.Spot,position,profile.lampColor,profile.lampIntensity);
            light.transform.localRotation=Quaternion.Euler(90,0,0);light.range=5;light.spotAngle=100;light.innerSpotAngle=65;
            return light;
        }
        static Cubemap Reflection(YardLightingProfile profile)
        {
            const int n=32;var map=new Cubemap(n,TextureFormat.RGBA32,true){name="Private yard sky reflection",hideFlags=HideFlags.DontSave};
            for(int face=0;face<6;face++)
            {
                var colors=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                {
                    float u=(x+.5f)*2/n-1,v=(y+.5f)*2/n-1;Vector3 direction;
                    switch((CubemapFace)face)
                    {
                        case CubemapFace.PositiveX:direction=new Vector3(1,-v,-u);break;
                        case CubemapFace.NegativeX:direction=new Vector3(-1,-v,u);break;
                        case CubemapFace.PositiveY:direction=new Vector3(u,1,v);break;
                        case CubemapFace.NegativeY:direction=new Vector3(u,-1,-v);break;
                        case CubemapFace.PositiveZ:direction=new Vector3(u,-v,1);break;
                        default:direction=new Vector3(-u,-v,-1);break;
                    }
                    colors[y*n+x]=profile.SkyColor(direction);
                }
                map.SetPixels(colors,(CubemapFace)face);
            }
            map.Apply(true,true);return map;
        }
        static void DestroyOwned(Object value) { if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value); }
        public void Dispose()
        {
            if(disposed||!initialized)return;disposed=true;
            // Avoid overriding a different scene that has taken ownership of global lighting.
            if(RenderSettings.sun==Sun)
            {
                RenderSettings.sun=previousSun;RenderSettings.skybox=previousSky;
                RenderSettings.defaultReflectionMode=previousReflectionMode;RenderSettings.customReflectionTexture=previousReflection;RenderSettings.reflectionIntensity=previousReflectionIntensity;
                RenderSettings.ambientMode=previousAmbientMode;RenderSettings.ambientIntensity=previousAmbientIntensity;
                RenderSettings.ambientSkyColor=previousSkyFill;RenderSettings.ambientEquatorColor=previousSideFill;RenderSettings.ambientGroundColor=previousGroundFill;
                RenderSettings.fog=previousFogEnabled;RenderSettings.fogMode=previousFogMode;RenderSettings.fogColor=previousFog;
                RenderSettings.fogStartDistance=previousFogStart;RenderSettings.fogEndDistance=previousFogEnd;
            }
            if(view!=null){view.clearFlags=previousClear;view.backgroundColor=previousBackground;}
            DestroyOwned(sky);DestroyOwned(reflection);DestroyOwned(bulbMaterial);
        }
        void OnDestroy() { Dispose(); }
    }
}
