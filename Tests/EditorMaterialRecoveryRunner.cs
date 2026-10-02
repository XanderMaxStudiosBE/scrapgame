// Executes the actual editor recovery callbacks with a controlled import queue and asset registry.
// This verifies bounded retries/report output, not Unity import, GPU rendering or shader compilation.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Scrapshift;
using UnityEngine;
using UnityEditor;
namespace UnityEngine
{
 public static class Debug
 {
  public static readonly List<string> logs=new List<string>(),warnings=new List<string>();
  public static void Log(object value){logs.Add(value.ToString());}
  public static void LogWarning(object value){warnings.Add(value.ToString());}
 }
 public static class GUIUtility {public static string systemCopyBuffer;}
 public static class SystemInfo {public static string graphicsDeviceType="adapter",graphicsDeviceName="no GPU",graphicsDeviceVersion="none";}
 public static class QualitySettings {public static UnityEngine.Rendering.RenderPipelineAsset renderPipeline;public static string activeColorSpace="Linear";}
 public static class RenderSettings {public static string ambientMode="Skybox";public static float ambientIntensity=1;}
 public class Renderer:Component {public bool enabled;public Material[] sharedMaterials;}
 public class Light:Component {public bool enabled;public string type,shadows;public float intensity;}
}
namespace UnityEngine.Rendering
{
 public class RenderPipelineAsset:UnityEngine.Object{}
 public static class GraphicsSettings {public static RenderPipelineAsset defaultRenderPipeline;}
}
namespace UnityEngine.SceneManagement
{
 public struct Scene {public string path;}
 public static class SceneManager {public static Scene GetActiveScene(){return new Scene{path="adapter scene"};}}
}
namespace UnityEditor
{
 public class InitializeOnLoadAttribute:Attribute{}
 public class MenuItemAttribute:Attribute {public MenuItemAttribute(string path){}}
 public class AssetPostprocessor{}
 public class AssetImporter {public static AssetImporter GetAtPath(string path){return null;}}
 public class TextureImporter:AssetImporter {public string textureType,textureShape,alphaSource;public bool sRGBTexture;}
 [Flags] public enum ImportAssetOptions {ForceUpdate=1,ForceSynchronousImport=8}
 public enum PlayModeStateChange {EnteredPlayMode}
 public static class EditorApplication
 {
  public static bool isCompiling,isUpdating;
  public static Action delayCall;
  public static event Action<PlayModeStateChange> playModeStateChanged;
  public static void EnterPlay(){if(playModeStateChanged!=null)playModeStateChanged(PlayModeStateChange.EnteredPlayMode);}
  public static int Pump()
  {
   int count=0;
   while(delayCall!=null){if(++count>20)throw new Exception("Unbounded editor import queue");var callback=delayCall;delayCall=null;callback();}
   return count;
  }
 }
 public static class AssetDatabase
 {
  public static YardMaterialCatalog catalog;
  public static int saves;
  public static int catalogSaves,textureImports;
  public static Action<Material> onSave;
  static readonly Dictionary<UnityEngine.Object,string> paths=new Dictionary<UnityEngine.Object,string>();
  static readonly Dictionary<string,UnityEngine.Object> assets=new Dictionary<string,UnityEngine.Object>();
  public static readonly Dictionary<string,UnityEngine.Object> pendingAssets=new Dictionary<string,UnityEngine.Object>();
  public static void Register(UnityEngine.Object value,string path){paths[value]=path;assets[path]=value;}
  public static T LoadAssetAtPath<T>(string path)where T:class {UnityEngine.Object value;return typeof(T)==typeof(YardMaterialCatalog)?catalog as T:assets.TryGetValue(path,out value)?value as T:null;}
  public static string GetAssetPath(UnityEngine.Object value){string path;return value!=null&&paths.TryGetValue(value,out path)?path:"";}
  public static bool TryGetGUIDAndLocalFileIdentifier(UnityEngine.Object value,out string guid,out long localId)
  {string path=GetAssetPath(value);guid="test-guid-"+path;localId=2800000;return path.Length>0;}
  public static void SaveAssetIfDirty(UnityEngine.Object value)
  {
   if(value is Material){saves++;if(onSave!=null)onSave((Material)value);}else catalogSaves++;
   Import(GetAssetPath(value));
  }
  public static void ImportAsset(string path,ImportAssetOptions options)
  {
   textureImports++;UnityEngine.Object value;if(pendingAssets.TryGetValue(path,out value))Register(value,path);
   Import(path);
  }
  public static void Import(string path)
  {
   typeof(YardMaterialRecovery).GetMethod("OnPostprocessAllAssets",BindingFlags.NonPublic|BindingFlags.Static)
    .Invoke(null,new object[]{new[]{path},new string[0],new string[0],new string[0]});
  }
 }
 public static class EditorUtility
 {
  public static string savePath="";
  public static string SaveFilePanel(string title,string directory,string defaultName,string extension){return savePath;}
  public static void SetDirty(UnityEngine.Object value){}
 }
 public static class ShaderUtil {public static bool ShaderHasError(Shader shader){return false;}}
}
namespace Scrapshift {public static class SurfaceTextureSampling {public static void Ensure(){}}}
class EditorMaterialRecoveryRunner
{
 static void Check(bool result,string message){if(!result)throw new Exception(message);}
 static void Main()
 {
  string directory=Path.Combine(Path.GetTempPath(),"scrapshift-editor-recovery-"+Guid.NewGuid().ToString("N"));
  Application.dataPath=Path.Combine(directory,"Assets");Directory.CreateDirectory(Application.dataPath);
  try{Run();}finally{Directory.Delete(directory,true);}
 }
 static void Run()
 {
  var shader=new Shader{name="Universal Render Pipeline/Lit"};var albedo=new Texture2D{name="Original atlas"};var mask=new Texture2D{name="Mask"};
  AssetDatabase.Register(albedo,"Assets/Scrapshift/atlas.png");AssetDatabase.Register(mask,"Assets/Scrapshift/mask.png");
  var material=new Material(shader){name="Test material"};
  string materialPath="Assets/Scrapshift/Resources/ScrapshiftWorld/WorldProps.mat";
  AssetDatabase.Register(material,materialPath);
  var entry=new YardMaterialEntry{resource="ScrapshiftWorld/WorldProps",material=material,albedo=albedo,metallicGloss=mask};
  var catalog=new YardMaterialCatalog{entries=new[]{entry}};AssetDatabase.catalog=catalog;Resources.catalog=catalog;
  YardMaterialRecovery.Recover();int ticks=EditorApplication.Pump();
  Check(AssetDatabase.saves==1&&ticks==1&&Debug.warnings.Count==0,"healthy self-import must converge after one save");
  Check(Debug.logs[0].Contains(entry.resource)&&Debug.logs[0].Contains("_BaseMap"),"repair log must name changed material and property");
  Console.WriteLine("PASS actual editor recovery coalesces healthy self-import and names changed properties");

  material.SetColor("_EmissionColor",new Color(1,1,1));material.EnableKeyword("_EMISSION");
  AssetDatabase.onSave=value=>
  {
   value.EnableKeyword("_EMISSION"); // A conflicting importer restores the faulty state after every save.
   var reloaded=new Texture2D{name="Mask reloaded"};AssetDatabase.Register(reloaded,"Assets/Scrapshift/mask.png");
   value.SetTexture("_MetallicGlossMap",reloaded); // New native object, same persistent asset reference.
  };
  int savesBefore=AssetDatabase.saves;GUIUtility.systemCopyBuffer="user clipboard";
  AssetDatabase.Import(materialPath);ticks=EditorApplication.Pump();
  Check(AssetDatabase.saves==savesBefore+1&&ticks==2&&Debug.warnings.Count==1,"conflicting import must stop after one automatic attempt");
  Check(Debug.warnings[0].Contains(entry.resource)&&Debug.warnings[0].Contains("keywords"),"conflict must identify material and changing property");
  Check(GUIUtility.systemCopyBuffer=="user clipboard","automatic conflict report must preserve clipboard");
  string reportPath=Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/ScrapshiftRenderingReport.txt"));
  Check(File.ReadAllText(reportPath).Contains("Emission flags:"),"conflict must save the full report including GI flags");
  AssetDatabase.Import(materialPath);EditorApplication.Pump();
  Check(AssetDatabase.saves==savesBefore+1&&Debug.warnings.Count==1,"repeated blocked import must remain quiet");
  Console.WriteLine("PASS actual editor conflict is bounded and deduplicated across persistent texture reloads");

  entry.straightAlpha=true;AssetDatabase.Import("Assets/Scrapshift/Resources/ScrapshiftRendering/Materials.asset");EditorApplication.Pump();
  Check(AssetDatabase.saves==savesBefore+2&&Debug.warnings.Count==2,"changed catalogue policy must permit a new repair attempt");
  YardMaterialRecovery.Recover();EditorApplication.Pump();
  Check(AssetDatabase.saves==savesBefore+3&&Debug.warnings.Count==3,"explicit manual retry must allow one attempt then stop again");
  Console.WriteLine("PASS changed catalogue and explicit retries work without restarting an automatic loop");

  File.Delete(reportPath);EditorApplication.EnterPlay();
  Check(File.Exists(reportPath)&&GUIUtility.systemCopyBuffer=="user clipboard","Play snapshot must save evidence without replacing clipboard");
  YardMaterialRecovery.Diagnose();
  Check(GUIUtility.systemCopyBuffer==File.ReadAllText(reportPath)&&GUIUtility.systemCopyBuffer.StartsWith("SCRAPSHIFT rendering report\n"),"explicit Diagnose must copy the complete saved report");
  Console.WriteLine("PASS Play snapshot and explicit report clipboard/file output");

  string selectedPath=Path.GetFullPath(Path.Combine(Application.dataPath,"../Desktop/ScrapshiftRenderingReport.txt"));
  EditorUtility.savePath=selectedPath;int unchangedSaves=AssetDatabase.saves;
  YardMaterialRecovery.SaveRenderingReport();
  string exported=File.ReadAllText(selectedPath);
  Check(exported==GUIUtility.systemCopyBuffer&&exported.Contains("Catalog ScrapshiftWorld/WorldProps")&&exported.Contains("Emission flags:"),"selected export must contain complete material data and match clipboard");
  Check(AssetDatabase.saves==unchangedSaves,"report export must not save/change materials");
  Console.WriteLine("PASS full report exports to a selected persistent location without material writes");
  EditorUtility.savePath="";GUIUtility.systemCopyBuffer="clipboard before cancel";int logCount=Debug.logs.Count;
  YardMaterialRecovery.SaveRenderingReport();
  Check(GUIUtility.systemCopyBuffer=="clipboard before cancel"&&File.ReadAllText(selectedPath)==exported&&Debug.logs.Count==logCount,"cancel must leave clipboard/files/logs untouched");
  Console.WriteLine("PASS export cancellation leaves clipboard and files intact");
  CheckTextureSources(shader);
 }
 static void CheckTextureSources(Shader shader)
 {
  const string root="Assets/Scrapshift/Resources/ScrapshiftWorld/";
  var albedo=new Texture2D{name="Recovered world atlas"};var mask=new Texture2D{name="Recovered mask"};var glow=new Texture2D{name="Recovered localized glow"};
  foreach(string name in new[]{"WorldAtlas","WorldMetalGloss","WorldGlow","Unavailable"})
  {
   string file=Path.GetFullPath(Path.Combine(Application.dataPath,"..",root+name+".png"));
   Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file,"controlled adapter texture source");
  }
  AssetDatabase.pendingAssets[root+"WorldAtlas.png"]=albedo;AssetDatabase.pendingAssets[root+"WorldMetalGloss.png"]=mask;
  AssetDatabase.pendingAssets[root+"WorldGlow.png"]=glow;
  var material=new Material(shader){name="World source recovery",globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive};
  material.SetColor("_EmissionColor",new Color(1,1,1));var tint=new Color(.4f,.5f,.6f);material.SetColor("_BaseColor",tint);
  var entry=new YardMaterialEntry{resource="ScrapshiftWorld/WorldProps",material=material,albedoPath=root+"WorldAtlas.png",metallicGlossPath=root+"WorldMetalGloss.png",emissionPath=root+"WorldGlow.png"};
  var catalog=new YardMaterialCatalog{entries=new[]{entry}};AssetDatabase.catalog=catalog;Resources.catalog=catalog;AssetDatabase.onSave=null;
  AssetDatabase.Register(material,root+"WorldProps.mat");AssetDatabase.Register(catalog,"Assets/Scrapshift/Resources/ScrapshiftRendering/Materials.asset");
  int saves=AssetDatabase.saves,catalogSaves=AssetDatabase.catalogSaves,imports=AssetDatabase.textureImports;
  YardMaterialRecovery.Recover();int ticks=EditorApplication.Pump();
  Check(ticks==1&&AssetDatabase.textureImports==imports+3,"missing imported sources must force one import per path and converge");
  Check(entry.albedo==albedo&&entry.metallicGloss==mask&&entry.emission==glow,"catalogue null references recover from actual texture paths");
  Check(AssetDatabase.catalogSaves==catalogSaves+1&&AssetDatabase.saves==saves+1,"save rebound catalogue and repaired material once");
  Check(material.GetTexture("_BaseMap")==albedo&&material.GetTexture("_MetallicGlossMap")==mask&&material.GetTexture("_EmissionMap")==glow,"material must receive recovered source maps");
  Check(material.GetColor("_BaseColor")==tint,"preserve tint during source recovery");
  Console.WriteLine("PASS null catalogue/imported textures recover by path and repair material with bounded imports");
  var custom=new Texture2D{name="Valid custom albedo"};entry.albedo=custom;
  AssetDatabase.Import(root+"WorldAtlas.png");EditorApplication.Pump();
  Check(entry.albedo==custom&&material.GetTexture("_BaseMap")==albedo&&AssetDatabase.textureImports==imports+3,"valid catalogue/material maps survive source import");
  Console.WriteLine("PASS source import preserves valid custom catalogue and material references");
  var failed=new YardMaterialEntry{resource="Missing source",material=new Material(shader),albedoPath=root+"Unavailable.png"};
  catalog.entries=new[]{failed};int warnings=Debug.warnings.Count;imports=AssetDatabase.textureImports;
  YardMaterialRecovery.Recover();EditorApplication.Pump();
  Check(AssetDatabase.textureImports==imports+1&&Debug.warnings.Count==warnings+1&&failed.albedo==null,"unavailable source must attempt once and warn once");
  AssetDatabase.Import(root+"Unavailable.png");EditorApplication.Pump();
  Check(AssetDatabase.textureImports==imports+1&&Debug.warnings.Count==warnings+1,"unavailable source must not create a retry loop");
  YardMaterialRecovery.Diagnose();
  Check(GUIUtility.systemCopyBuffer.Contains("ERROR: albedo catalogue texture is unavailable.")&&GUIUtility.systemCopyBuffer.Contains("BaseMap matches loaded catalogue texture: False"),"two null references must be reported as missing rather than matching");
  Console.WriteLine("PASS failed source import stays bounded and report correctly identifies null reference failure");
 }
}
