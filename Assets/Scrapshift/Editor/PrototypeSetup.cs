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
        public const string CompactScenePath=Folder+"/CompactScrapyard.unity";
        [MenuItem("Scrapshift/Create or Open Prototype")]
        public static void CreateOrOpen()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Generate();
        }
        [MenuItem("Scrapshift/Create or Open Compact Yard")]
        public static void CreateOrOpenCompact(){CreateOrOpen();}
        [MenuItem("Scrapshift/Update Existing Prototype Visuals")]
        public static void UpdateExistingVisuals()
        {
            RetroMaterialSetup.EnsureMaterials();
            YardMaterialRecovery.Recover();
            PresetShadowSupport.EnsureGenerated();
            Debug.Log("Tracked materials ready. Existing YardBootstrap scenes use the updated props and Settings automatically on next Play; scene and balance edits were preserved.");
        }
        // Batch entry point: -executeMethod Scrapshift.PrototypeSetup.Generate
        public static void Generate()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            RetroMaterialSetup.EnsureMaterials();
            YardMaterialRecovery.Recover();
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
            // Existing legacy scenes are preserved byte-for-byte. Only create the missing compact scene.
            if(!File.Exists(CompactScenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var bootstrap=new GameObject("Compact Scrapshift Bootstrap").AddComponent<Compact.CompactYardBootstrap>();
                bootstrap.balance=Resources.Load<Compact.CompactBalance>("ScrapshiftCompact/Balance");
                bootstrap.legacyBalance=balance;bootstrap.logo=logo;
                bootstrap.surfaceShader=Shader.Find("Universal Render Pipeline/Lit");
                EditorSceneManager.SaveScene(scene,CompactScenePath);
            }
            else EditorSceneManager.OpenScene(CompactScenePath);
            // Default build starts in the compact yard; legacy remains an explicit playable option.
            var scenes=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(CompactScenePath,true)};
            foreach(var existing in EditorBuildSettings.scenes)if(existing.path!=CompactScenePath&&existing.path!=ScenePath)scenes.Add(existing);
            scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Compact yard ready: open Generated/CompactScrapyard and press Play. Resources/ScrapshiftCompact/Balance controls the new economy. Generated/Scrapyard, its balance and all version-one saves remain the legacy continuation.");
        }
    }
}
