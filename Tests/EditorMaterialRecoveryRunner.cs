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
 public class TextureImporter:AssetImporter {public string textureType,alphaSource;public bool sRGBTexture;}
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
  public static Action<Material> onSave;
  static readonly Dictionary<UnityEngine.Object,string> paths=new Dictionary<UnityEngine.Object,string>();
  public static void Register(UnityEngine.Object value,string path){paths[value]=path;}
  public static T LoadAssetAtPath<T>(string path)where T:class {return catalog as T;}
  public static string GetAssetPath(UnityEngine.Object value){string path;return value!=null&&paths.TryGetValue(value,out path)?path:"";}
  public static bool TryGetGUIDAndLocalFileIdentifier(UnityEngine.Object value,out string guid,out long localId)
  {string path=GetAssetPath(value);guid="test-guid-"+path;localId=2800000;return path.Length>0;}
  public static void SaveAssetIfDirty(UnityEngine.Object value){saves++;if(onSave!=null)onSave((Material)value);Import(GetAssetPath(value));}
  public static void Import(string path)
  {
   typeof(YardMaterialRecovery).GetMethod("OnPostprocessAllAssets",BindingFlags.NonPublic|BindingFlags.Static)
    .Invoke(null,new object[]{new[]{path},new string[0],new string[0],new string[0]});
  }
 }
 public static class EditorUtility {public static void SetDirty(UnityEngine.Object value){}}
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
 }
}
