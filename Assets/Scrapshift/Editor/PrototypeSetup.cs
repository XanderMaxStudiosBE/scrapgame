using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Scrapshift
{
    public static class PrototypeSetup
    {
        const string Folder = "Assets/Scrapshift/Generated";
        const string ScenePath = Folder + "/Scrapyard.unity";
        [MenuItem("Scrapshift/Create or Open Prototype")]
        public static void CreateOrOpen()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Generate();
        }
        [MenuItem("Scrapshift/Update Existing Prototype Visuals")]
        public static void UpdateExistingVisuals()
        {
            RetroMaterialSetup.EnsureMaterials();
            PresetShadowSupport.EnsureGenerated();
            Debug.Log("Tracked materials ready. Existing YardBootstrap scenes use the updated props and Settings automatically on next Play; scene and balance edits were preserved.");
        }
        // Batch entry point: -executeMethod Scrapshift.PrototypeSetup.Generate
        public static void Generate()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            RetroMaterialSetup.EnsureMaterials();
            var balance = AssetDatabase.LoadAssetAtPath<PrototypeBalance>(Folder + "/Balance.asset");
            if (balance == null)
            {
                balance = ScriptableObject.CreateInstance<PrototypeBalance>();
                AssetDatabase.CreateAsset(balance, Folder + "/Balance.asset");
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder + "/ScrapshiftURP.asset");
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Folder + "/ScrapshiftRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "Scrapshift URP";
                AssetDatabase.CreateAsset(pipeline, Folder + "/ScrapshiftURP.asset");
            }
            PresetShadowSupport.EnsureGenerated();
            GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
            PlayerSettings.companyName = "XanderMaxStudiosBE"; PlayerSettings.productName = "Scrapshift";
            // Legacy input keeps this small prototype free of an additional input package.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null && input.intValue != 0) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
            Texture2D logo = null;
            const string source = "Campaign/Scrapshift/assets/scrapshift-logo.png";
            string destination = Folder + "/scrapshift-logo.png";
            if (File.Exists(source) && !File.Exists(destination)) { File.Copy(source, destination); AssetDatabase.ImportAsset(destination); }
            logo = AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var bootstrap = new GameObject("Scrapshift Bootstrap").AddComponent<YardBootstrap>();
                bootstrap.balance = balance; bootstrap.logo = logo;
                bootstrap.surfaceShader = Shader.Find("Universal Render Pipeline/Lit");
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else EditorSceneManager.OpenScene(ScenePath);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Scrapshift prototype ready (existing bootstrap scenes receive updated runtime props without scene replacement). Open Generated/Scrapyard and press Play. Balance.asset controls prices and processing.");
        }
    }
}
