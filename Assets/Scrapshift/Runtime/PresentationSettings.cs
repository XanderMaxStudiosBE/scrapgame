using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Scrapshift
{
    public static class PresentationStore
    {
        public static PresentationPreferences Read(string path, out string notice)
        {
            notice = "";
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    string json = File.ReadAllText(candidate);
                    foreach (string required in new[] { "version", "graphics", "frameLimit", "fieldOfView", "masterVolume", "effectsVolume", "ambienceVolume", "warmGrade" })
                        if (!json.Contains("\"" + required + "\"")) throw new ArgumentException("Missing preference fields.");
                    var value = JsonUtility.FromJson<PresentationPreferences>(json);
                    if (value == null) throw new ArgumentException("Empty preferences.");
                    value.Validate();
                    if (candidate != path) notice = "Video/audio restored from backup.";
                    return value;
                }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException)
                { notice = "Video/audio defaults active: " + ex.Message; }
            }
            return new PresentationPreferences();
        }
        public static void Write(string path, PresentationPreferences preferences)
        {
            preferences.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(path));
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(preferences, true));
            string temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak"); else File.Move(temporary, path);
        }
    }

    // Only a private runtime clone is changed; tracked URP assets and quality levels stay intact.
    public sealed class PresentationSettings : IDisposable
    {
        readonly string path;
        readonly Camera camera;
        readonly RenderPipelineAsset originalQualityPipeline;
        readonly UniversalRenderPipelineAsset pipeline;
        readonly int originalFrameLimit, originalVSync;
        readonly bool originalPost;
        readonly Light[] lights;
        readonly LightShadows[] originalShadows;
        readonly float originalFov;
        readonly GameObject gradeObject;
        readonly VolumeProfile gradeProfile;
        bool disposed;
        public PresentationPreferences Preferences { get; private set; }
        public string Notice { get; private set; }
        public PresentationSettings(Camera camera, Transform parent)
        {
            this.camera = camera; originalFov = camera.fieldOfView;
            lights = parent.GetComponentsInChildren<Light>(); originalShadows = new LightShadows[lights.Length];
            for (int i = 0; i < lights.Length; i++) originalShadows[i] = lights[i].shadows;
            path = Path.Combine(Application.persistentDataPath, "presentation-v1.json");
            string notice; Preferences = PresentationStore.Read(path, out notice); Notice = notice;
            originalFrameLimit = Application.targetFrameRate; originalVSync = QualitySettings.vSyncCount;
            originalQualityPipeline = QualitySettings.renderPipeline;
            var source = (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if (source != null) { pipeline = UnityEngine.Object.Instantiate(source); pipeline.name = "Scrapshift runtime graphics"; pipeline.hideFlags = HideFlags.DontSave; QualitySettings.renderPipeline = pipeline; }
            var data = camera.GetUniversalAdditionalCameraData(); originalPost = data.renderPostProcessing;
            gradeObject = new GameObject("Scrapshift warm color grade"); gradeObject.transform.SetParent(parent, false);
            var volume = gradeObject.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10;
            gradeProfile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = gradeProfile;
            var color = gradeProfile.Add<ColorAdjustments>(); color.postExposure.Override(-.05f); color.contrast.Override(8); color.saturation.Override(-4);
            var tone = gradeProfile.Add<Tonemapping>(); tone.mode.Override(TonemappingMode.Neutral);
            var bloom = gradeProfile.Add<Bloom>(); bloom.threshold.Override(1.4f); bloom.intensity.Override(.08f);
            Apply();
        }
        public void Apply()
        {
            Preferences.Validate();
            Application.targetFrameRate = Preferences.frameLimit; QualitySettings.vSyncCount = 0;
            camera.fieldOfView = Preferences.fieldOfView;
            if (pipeline != null)
            {
                pipeline.renderScale = Preferences.RenderScale; pipeline.shadowDistance = Preferences.ShadowDistance;
                pipeline.shadowCascadeCount = Preferences.graphics == GraphicsPreset.Laptop ? 1 : Preferences.graphics == GraphicsPreset.Balanced ? 2 : 4;
                pipeline.cascade2Split = .35f; pipeline.cascade4Split = new Vector3(.12f,.32f,.60f);
                pipeline.maxAdditionalLightsCount = 3;
                pipeline.msaaSampleCount = Preferences.graphics == GraphicsPreset.Detailed ? 4 : 2;
                pipeline.mainLightShadowmapResolution = Preferences.graphics == GraphicsPreset.Laptop ? 512 : Preferences.graphics == GraphicsPreset.Balanced ? 1024 : 2048;

            }
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null && originalShadows[i] != LightShadows.None)
                    lights[i].shadows = Preferences.graphics == GraphicsPreset.Laptop ? LightShadows.Hard : originalShadows[i];
            bool post = Preferences.warmGrade && Preferences.graphics != GraphicsPreset.Laptop;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = post;
            gradeObject.SetActive(post);
        }
        public void Save()
        {
            Apply();
            try { PresentationStore.Write(path, Preferences); Notice = "Video/audio saved."; }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            { Notice = "Applied, but could not save: " + ex.Message; }
        }
        public void RestoreDefaults() { Preferences = new PresentationPreferences(); Save(); }
        static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (QualitySettings.renderPipeline == pipeline) QualitySettings.renderPipeline = originalQualityPipeline;
            Application.targetFrameRate = originalFrameLimit; QualitySettings.vSyncCount = originalVSync;
            if (camera != null) { camera.fieldOfView = originalFov; camera.GetUniversalAdditionalCameraData().renderPostProcessing = originalPost; }
            for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].shadows = originalShadows[i];
            if (pipeline != null) DestroyOwned(pipeline);
            var components = gradeProfile.components.ToArray();
            foreach (var component in components) DestroyOwned(component);
            DestroyOwned(gradeProfile); DestroyOwned(gradeObject);
        }
    }
}
