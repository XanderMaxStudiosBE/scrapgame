using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace Scrapshift
{
    // Runs after URP's material postprocessors. Repairs missing bindings, never overwrites a valid custom map/tint.
    [InitializeOnLoad] public sealed class YardMaterialRecovery : AssetPostprocessor
    {
        static bool pending;
        static YardMaterialRecovery(){Queue();}
        static void Queue(){if(pending)return;pending=true;EditorApplication.delayCall+=Recover;}
        static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
        {
            foreach(var path in imported)if(path.StartsWith("Assets/Scrapshift/",System.StringComparison.Ordinal)&&
                (path.EndsWith(".mat",System.StringComparison.Ordinal)||path.EndsWith("Materials.asset",System.StringComparison.Ordinal))){Queue();return;}
        }
        [MenuItem("Scrapshift/Repair Missing Material Bindings")]
        public static void Recover()
        {
            pending=false;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}
            var catalog=AssetDatabase.LoadAssetAtPath<YardMaterialCatalog>("Assets/Scrapshift/Resources/ScrapshiftRendering/Materials.asset");
            if(catalog==null||catalog.entries==null)return;
            SurfaceTextureSampling.Ensure();
            int count=0;
            foreach(var entry in catalog.entries)
                if(entry!=null && YardMaterialBindings.Repair(entry.material,entry))
                {EditorUtility.SetDirty(entry.material);AssetDatabase.SaveAssetIfDirty(entry.material);count++;}
            if(count>0)Debug.Log("Scrapshift restored material bindings/render state for "+count+" materials. Valid custom maps, colors and surface values were retained.");
        }
        // Reports imported asset bindings AND the materials actually assigned in the running scene.
        // Leaves materials/gameplay untouched; copies and saves a report for local diagnosis.
        [MenuItem("Scrapshift/Diagnose Rendering")]
        public static void Diagnose()
        {
            var report=new StringBuilder("SCRAPSHIFT rendering report\n");
            var pipeline=QualitySettings.renderPipeline!=null?QualitySettings.renderPipeline:GraphicsSettings.defaultRenderPipeline;
            report.AppendLine("Unity "+Application.unityVersion+" / scene "+SceneManager.GetActiveScene().path);
            report.AppendLine("Graphics: "+SystemInfo.graphicsDeviceType+" / "+SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceVersion);
            report.AppendLine("Pipeline: "+(pipeline!=null?pipeline.name:"none")+" / color space: "+QualitySettings.activeColorSpace);
            report.AppendLine("Ambient: "+RenderSettings.ambientMode+" / intensity "+RenderSettings.ambientIntensity);
            var catalog=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials");
            if(catalog==null || catalog.entries==null)report.AppendLine("ERROR: original material catalogue unavailable.");
            else foreach(var entry in catalog.entries)
            {
                if(entry==null)continue;
                report.AppendLine("Catalog "+entry.resource+" / albedo: "+TextureDescription(entry.albedo));
                DescribeMaterial(report,entry.material);
                if(entry.material!=null && entry.material.HasProperty("_BaseMap"))
                    report.AppendLine("  BaseMap matches catalogue: "+(entry.material.GetTexture("_BaseMap")==entry.albedo));
            }
            var seen=new HashSet<Material>();
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy)continue;
                foreach(var material in renderer.sharedMaterials)
                {
                    if(material!=null && !seen.Add(material))continue;
                    report.AppendLine("Scene renderer: "+renderer.name);DescribeMaterial(report,material);
                }
            }
            int lightCount=0;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if(light.enabled && light.gameObject.activeInHierarchy)
                {lightCount++;report.AppendLine("Light: "+light.name+" / "+light.type+" / intensity "+light.intensity+" / shadows "+light.shadows);}
            report.AppendLine("Active lights: "+lightCount+" (generated yard normally has one sun and three task lights).");
            string text=report.ToString();GUIUtility.systemCopyBuffer=text;
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/ScrapshiftRenderingReport.txt"));
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,text);
                Debug.Log(text+"\nReport copied to clipboard and saved to "+path);
            }
            catch(System.Exception ex)when(ex is IOException||ex is System.UnauthorizedAccessException)
            {Debug.Log(text+"\nReport copied to clipboard; file could not be saved: "+ex.Message);}
        }
        static void DescribeMaterial(StringBuilder report,Material material)
        {
            if(material==null){report.AppendLine("  ERROR: material unavailable.");return;}
            report.AppendLine("  Material "+material.name+" / shader "+(material.shader!=null?material.shader.name:"missing")+" / "+AssetDatabase.GetAssetPath(material));
            report.AppendLine("  Render queue: "+material.renderQueue+" / tag: "+material.GetTag("RenderType",false));
            if(material.shader!=null)report.AppendLine("  Shader supported: "+material.shader.isSupported+" / shader errors: "+ShaderUtil.ShaderHasError(material.shader));
            foreach(var property in new[]{"_BaseMap","_MainTex","_MetallicGlossMap","_SpecGlossMap","_EmissionMap"})
                if(material.HasProperty(property))report.AppendLine("  "+property+": "+TextureDescription(material.GetTexture(property))+" / unassigned-default: "+YardMaterialBindings.Missing(material,property));
            foreach(var property in new[]{"_BaseColor","_EmissionColor"})
                if(material.HasProperty(property))report.AppendLine("  "+property+": "+material.GetColor(property));
            foreach(var property in new[]{"_Smoothness","_Metallic","_WorkflowMode","_Surface","_Blend","_SrcBlend","_DstBlend","_ZWrite","_AlphaClip"})
                if(material.HasProperty(property))report.AppendLine("  "+property+": "+material.GetFloat(property));
            report.AppendLine("  Keywords: "+string.Join(", ",material.shaderKeywords));
        }
        static string TextureDescription(Texture texture)
        {
            if(texture==null)return "null";
            string path=AssetDatabase.GetAssetPath(texture);
            string description=texture.name+" ["+texture.width+"x"+texture.height+"] / "+path;
            var importer=string.IsNullOrEmpty(path)?null:AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null)description+=" / import: "+importer.textureType+" / alpha: "+importer.alphaSource+" / sRGB: "+importer.sRGBTexture;
            return description;
        }
    }
}
