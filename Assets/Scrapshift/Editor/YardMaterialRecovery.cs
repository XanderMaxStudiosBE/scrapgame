using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
namespace Scrapshift
{
    // Runs after URP's material postprocessors. Repairs missing bindings, never overwrites a valid custom map/tint.
    [InitializeOnLoad] public sealed class YardMaterialRecovery : AssetPostprocessor
    {
        static bool pending;
        static readonly Dictionary<string,HashSet<string>> attemptedStates=new Dictionary<string,HashSet<string>>();
        static readonly Dictionary<string,string> lastChanges=new Dictionary<string,string>();
        static readonly HashSet<string> reportedConflicts=new HashSet<string>();
        static readonly HashSet<string> textureImportAttempts=new HashSet<string>();
        static readonly HashSet<string> reportedMissingTextures=new HashSet<string>();
        static YardMaterialRecovery(){Queue();EditorApplication.playModeStateChanged+=OnPlayModeChanged;}
        static void Queue(){if(pending)return;pending=true;EditorApplication.delayCall+=RecoverAutomatically;}
        static void OnPlayModeChanged(PlayModeStateChange state)
        {if(state==PlayModeStateChange.EnteredPlayMode)CaptureReport(false);}
        static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
        {
            foreach(var path in imported)if(path.StartsWith("Assets/Scrapshift/",System.StringComparison.Ordinal)&&
                (path.EndsWith(".mat",System.StringComparison.Ordinal)||path.EndsWith(".png",System.StringComparison.Ordinal)||path.EndsWith("Materials.asset",System.StringComparison.Ordinal))){Queue();return;}
        }
        [MenuItem("Scrapshift/Repair Missing Material Bindings")]
        public static void Recover()
        {
            // An explicit retry is allowed; automatic imports never retry an already repaired input.
            attemptedStates.Clear();lastChanges.Clear();reportedConflicts.Clear();
            textureImportAttempts.Clear();reportedMissingTextures.Clear();
            EditorApplication.delayCall-=RecoverAutomatically;pending=false;RecoverAutomatically();
        }
        static void RecoverAutomatically()
        {
            pending=false;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}
            var catalog=AssetDatabase.LoadAssetAtPath<YardMaterialCatalog>("Assets/Scrapshift/Resources/ScrapshiftRendering/Materials.asset");
            if(catalog==null||catalog.entries==null)return;
            ResolveCatalogTextures(catalog);
            SurfaceTextureSampling.Ensure();
            var changes=new StringBuilder();
            foreach(var entry in catalog.entries)
            {
                if(entry==null || entry.material==null)continue;
                var before=CaptureState(entry.material);
                string fingerprint=TextureIdentity(entry.albedo)+"|"+TextureIdentity(entry.metallicGloss)+"|"+
                    TextureIdentity(entry.emission)+"|"+entry.straightAlpha+"\n"+Fingerprint(before);
                if(!attemptedStates.TryGetValue(entry.resource,out HashSet<string> attempts))
                {attempts=new HashSet<string>();attemptedStates[entry.resource]=attempts;}
                if(attempts.Contains(fingerprint))
                {
                    if(reportedConflicts.Add(entry.resource+"\n"+fingerprint))
                    {
                        Debug.LogWarning("SCRAPSHIFT automatic material repair stopped for "+entry.resource+
                            ": the same pre-repair state returned after saving. Automatic retries stopped to avoid an import loop. Last attempted changes:\n"+lastChanges[entry.resource]+
                            "\nUse Scrapshift → Diagnose Rendering to copy the report. Manual Repair can retry once.");
                        CaptureReport(false);
                    }
                    continue;
                }
                if(!YardMaterialBindings.Repair(entry.material,entry))continue;
                attempts.Add(fingerprint);
                string detail=ChangedProperties(before,CaptureState(entry.material));lastChanges[entry.resource]=detail;
                changes.AppendLine(entry.resource+":\n"+detail);
                EditorUtility.SetDirty(entry.material);AssetDatabase.SaveAssetIfDirty(entry.material);
            }
            if(changes.Length>0)Debug.Log("SCRAPSHIFT material repair (valid custom maps and surface values retained):\n"+changes);
        }
        static void ResolveCatalogTextures(YardMaterialCatalog catalog)
        {
            int count=0;
            foreach(var entry in catalog.entries)
            {
                if(entry==null)continue;
                var albedo=ResolveTexture(entry.albedo,entry.albedoPath);var mask=ResolveTexture(entry.metallicGloss,entry.metallicGlossPath);
                var glow=ResolveTexture(entry.emission,entry.emissionPath);
                if(albedo!=entry.albedo){entry.albedo=albedo;count++;}
                if(mask!=entry.metallicGloss){entry.metallicGloss=mask;count++;}
                if(glow!=entry.emission){entry.emission=glow;count++;}
            }
            if(count==0)return;
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("SCRAPSHIFT restored "+count+" missing catalogue texture references from their original asset paths.");
        }
        static Texture2D ResolveTexture(Texture2D existing,string path)
        {
            if(existing!=null || string.IsNullOrEmpty(path))return existing;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null && SourceFileExists(path) && textureImportAttempts.Add(path))
            {
                // Repair the imported texture first: assigning another null reference cannot fix the material.
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            if(texture==null && reportedMissingTextures.Add(path))
                Debug.LogWarning("SCRAPSHIFT texture unavailable after import: "+path+". Save Rendering Report includes its file/importer details.");
            return texture;
        }
        static bool SourceFileExists(string path)
        {return path.StartsWith("Assets/",System.StringComparison.Ordinal) && File.Exists(Path.Combine(Application.dataPath,"..",path));}
        // Exact property snapshots keep the guard independent of keyword order and rounded Inspector text.
        static Dictionary<string,string> CaptureState(Material material)
        {
            var state=new Dictionary<string,string>();
            state["shader"]=material.shader!=null?material.shader.name:"missing";
            state["globalIlluminationFlags"]=((int)material.globalIlluminationFlags).ToString(CultureInfo.InvariantCulture);
            state["renderQueue"]=material.renderQueue.ToString(CultureInfo.InvariantCulture);
            var keywords=material.shaderKeywords;System.Array.Sort(keywords,System.StringComparer.Ordinal);
            state["keywords"]=string.Join(", ",keywords);
            foreach(var property in new[]{"_BaseMap","_MainTex","_MetallicGlossMap","_SpecGlossMap","_EmissionMap"})
                if(material.HasProperty(property))
                {
                    var texture=material.GetTexture(property);var scale=material.GetTextureScale(property);var offset=material.GetTextureOffset(property);
                    state[property]=TextureIdentity(texture);
                    state[property+" UV"]=Number(scale.x)+","+Number(scale.y)+" / "+Number(offset.x)+","+Number(offset.y);
                }
            foreach(var property in new[]{"_BaseColor","_Color","_EmissionColor"})
                if(material.HasProperty(property))
                {var color=material.GetColor(property);state[property]=Number(color.r)+","+Number(color.g)+","+Number(color.b)+","+Number(color.a);}
            foreach(var property in new[]{"_Smoothness","_Glossiness","_GlossMapScale","_GlossyReflections","_Metallic","_WorkflowMode","_Surface","_Blend","_BlendModePreserveSpecular","_SrcBlend","_DstBlend","_SrcBlendAlpha","_DstBlendAlpha","_ZWrite","_AlphaClip","_SpecularHighlights","_EnvironmentReflections","_EmissionEnabled"})
                if(material.HasProperty(property))state[property]=Number(material.GetFloat(property));
            return state;
        }
        static string Number(float value){return value.ToString("R",CultureInfo.InvariantCulture);}
        static string TextureIdentity(Texture texture)
        {
            if(texture==null)return "null";
            if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture,out string guid,out long localId))
                return AssetDatabase.GetAssetPath(texture)+" ["+guid+":"+localId+"]";
            return texture.name+" [transient #"+texture.GetInstanceID()+"]";
        }
        static string Fingerprint(Dictionary<string,string> state)
        {
            var keys=new List<string>(state.Keys);keys.Sort(System.StringComparer.Ordinal);var result=new StringBuilder();
            foreach(var key in keys)result.AppendLine(key+"="+state[key]);return result.ToString();
        }
        static string ChangedProperties(Dictionary<string,string> before,Dictionary<string,string> after)
        {
            var result=new StringBuilder();
            foreach(var pair in before)if(after.TryGetValue(pair.Key,out string value) && value!=pair.Value)
                result.AppendLine("  "+pair.Key+": "+pair.Value+" → "+value);
            return result.ToString().TrimEnd();
        }
        // Reports imported asset bindings AND the materials actually assigned in the running scene.
        // Leaves materials/gameplay untouched; copies and saves a report for local diagnosis.
        [MenuItem("Scrapshift/Diagnose Rendering")]
        public static void Diagnose()
        {CaptureReport(true);}
        [MenuItem("Scrapshift/Save Rendering Report...")]
        public static void SaveRenderingReport()
        {
            string path=EditorUtility.SaveFilePanel("Save SCRAPSHIFT rendering report","","ScrapshiftRenderingReport","txt");
            if(!string.IsNullOrEmpty(path))CaptureReport(true,path);
        }
        static void CaptureReport(bool copy,string outputPath=null)
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
                DescribeSource(report,"albedo",entry.albedo,entry.albedoPath);
                DescribeSource(report,"metallicGloss",entry.metallicGloss,entry.metallicGlossPath);
                DescribeSource(report,"emission",entry.emission,entry.emissionPath);
                DescribeMaterial(report,entry.material);
                if(entry.material!=null && entry.material.HasProperty("_BaseMap"))
                    report.AppendLine("  BaseMap matches loaded catalogue texture: "+(entry.albedo!=null && entry.material.GetTexture("_BaseMap")==entry.albedo));
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
            string text=report.ToString();if(copy)GUIUtility.systemCopyBuffer=text;
            string path=Path.GetFullPath(outputPath??Path.Combine(Application.dataPath,"../Temp/ScrapshiftRenderingReport.txt"));
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,text);
                Debug.Log(copy?text+"\nReport copied to clipboard and saved to "+path:
                    "SCRAPSHIFT rendering snapshot saved to "+path+". Use Scrapshift → Diagnose Rendering to copy the full report.");
            }
            catch(System.Exception ex)when(ex is IOException||ex is System.UnauthorizedAccessException)
            {Debug.Log(text+"\n"+(copy?"Report copied to clipboard; ":"")+"file could not be saved: "+ex.Message);}
        }
        static void DescribeMaterial(StringBuilder report,Material material)
        {
            if(material==null){report.AppendLine("  ERROR: material unavailable.");return;}
            report.AppendLine("  Material "+material.name+" / shader "+(material.shader!=null?material.shader.name:"missing")+" / "+AssetDatabase.GetAssetPath(material));
            report.AppendLine("  Render queue: "+material.renderQueue+" / tag: "+material.GetTag("RenderType",false));
            report.AppendLine("  Emission flags: "+material.globalIlluminationFlags+" ("+(int)material.globalIlluminationFlags+")");
            if(material.shader!=null)report.AppendLine("  Shader supported: "+material.shader.isSupported+" / shader errors: "+ShaderUtil.ShaderHasError(material.shader));
            foreach(var property in new[]{"_BaseMap","_MainTex","_MetallicGlossMap","_SpecGlossMap","_EmissionMap"})
                if(material.HasProperty(property))report.AppendLine("  "+property+": "+TextureDescription(material.GetTexture(property))+" / unassigned-default: "+YardMaterialBindings.Missing(material,property));
            foreach(var property in new[]{"_BaseColor","_EmissionColor"})
                if(material.HasProperty(property))report.AppendLine("  "+property+": "+material.GetColor(property));
            foreach(var property in new[]{"_Smoothness","_Metallic","_WorkflowMode","_Surface","_Blend","_SrcBlend","_DstBlend","_ZWrite","_AlphaClip"})
                if(material.HasProperty(property))report.AppendLine("  "+property+": "+material.GetFloat(property));
            report.AppendLine("  Keywords: "+string.Join(", ",material.shaderKeywords));
        }
        static void DescribeSource(StringBuilder report,string slot,Texture2D reference,string path)
        {
            if(string.IsNullOrEmpty(path))return;
            var importer=AssetImporter.GetAtPath(path);var loaded=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            report.AppendLine("  "+slot+" source: "+path+" / file exists: "+SourceFileExists(path)+
                " / importer: "+(importer!=null?importer.GetType().Name:"missing")+" / loaded by path: "+TextureDescription(loaded));
            if(loaded!=null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(loaded,out string guid,out long localId))
                report.AppendLine("  "+slot+" imported object: GUID "+guid+" / local file ID "+localId);
            if(importer is TextureImporter textureImporter)
                report.AppendLine("  "+slot+" texture import: "+textureImporter.textureType+" / shape: "+textureImporter.textureShape);
            if(reference==null)report.AppendLine("  ERROR: "+slot+" catalogue texture is unavailable.");
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
