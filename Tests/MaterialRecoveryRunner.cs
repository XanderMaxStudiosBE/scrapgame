// Executes real YardMaterialBindings against a narrow material API adapter.
// Deliberately models engine-default samplers distinct from whiteTexture; this is NOT Unity rendering/import.
using System;
using System.Collections.Generic;
using Scrapshift;
namespace UnityEngine {
 public enum HideFlags { DontSave }
 [Flags] public enum MaterialGlobalIlluminationFlags {None=0,RealtimeEmissive=1,BakedEmissive=2,EmissiveIsBlack=4,AnyEmissive=3}
 public enum FindObjectsSortMode {None}
 public class Object {public static T[] FindObjectsByType<T>(FindObjectsSortMode sort){return new T[0];}public int GetInstanceID(){return GetHashCode();} public string name; public HideFlags hideFlags; public static void Destroy(Object value){} public static void DestroyImmediate(Object value){} }
 public class Component:Object { public GameObject gameObject; public T GetComponent<T>() where T:class {return null;} }
 public class MonoBehaviour:Component {}
 public class GameObject:Object {public bool activeInHierarchy; public T AddComponent<T>() where T:new(){return new T();} }
 public class Transform:Component { public Transform root {get{return this;}} }
 public class ScriptableObject:Object {}
 public class CreateAssetMenuAttribute:Attribute {public string menuName;}
 public static class Application {public static bool isPlaying=false;public static string unityVersion="adapter",dataPath;}
 public static class Resources {public static Object catalog; public static T Load<T>(string path) where T:class {return catalog as T;} }
 public class Texture:Object {public int width=64,height=64;}
 public class Texture2D:Texture { public static readonly Texture2D whiteTexture=new Texture2D{name="whiteTexture"}; }
 public class Shader:Object {public bool isSupported=true; public readonly Texture2D defaultWhite=new Texture2D{name="Default-White"}; public readonly Texture2D legacyWhite=new Texture2D{name="Legacy-Default-White"}; }
 public struct Vector2 {
  public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}
  public static Vector2 one {get{return new Vector2(1,1);}} public static Vector2 zero {get{return new Vector2(0,0);}}
  public static bool operator==(Vector2 a,Vector2 b){return a.x==b.x&&a.y==b.y;}public static bool operator!=(Vector2 a,Vector2 b){return !(a==b);}
  public override bool Equals(object b){return b is Vector2&&this==(Vector2)b;} public override int GetHashCode(){return x.GetHashCode()^y.GetHashCode();}
 }
 public struct Color {
  public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
  public float maxColorComponent {get{return Math.Max(r,Math.Max(g,b));}}
  public static bool operator==(Color a,Color b){return a.r==b.r&&a.g==b.g&&a.b==b.b&&a.a==b.a;}public static bool operator!=(Color a,Color b){return !(a==b);}
  public override bool Equals(object b){return b is Color&&this==(Color)b;}public override int GetHashCode(){return r.GetHashCode()^g.GetHashCode();}
 }
 public class Material:Object {
  public Shader shader;public MaterialGlobalIlluminationFlags globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
  public int renderQueue=2000;public string GetTag(string name,bool fallback){return "Opaque";}
  public string[] shaderKeywords {get{var result=new string[keywords.Count];keywords.CopyTo(result);return result;}}
  readonly Dictionary<string,Texture> textures=new Dictionary<string,Texture>();
  readonly Dictionary<string,float> floats=new Dictionary<string,float>();
  readonly Dictionary<string,Color> colors=new Dictionary<string,Color>();
  readonly Dictionary<string,Vector2> scales=new Dictionary<string,Vector2>(),offsets=new Dictionary<string,Vector2>();
  readonly HashSet<string> keywords=new HashSet<string>();
  public Material(Shader shader){this.shader=shader;}
  public Material(Material source){shader=source.shader;globalIlluminationFlags=source.globalIlluminationFlags; foreach(var pair in source.textures)textures[pair.Key]=pair.Value;foreach(var pair in source.floats)floats[pair.Key]=pair.Value;foreach(var pair in source.colors)colors[pair.Key]=pair.Value;foreach(var pair in source.scales)scales[pair.Key]=pair.Value;foreach(var pair in source.offsets)offsets[pair.Key]=pair.Value;foreach(var keyword in source.keywords)keywords.Add(keyword);}
  public bool HasProperty(string property){return true;}
  public Texture GetTexture(string property){Texture value;return textures.TryGetValue(property,out value)?value:property=="_MainTex"?shader.legacyWhite:shader.defaultWhite;}
  public void SetTexture(string property,Texture value){textures[property]=value;}
  public Vector2 GetTextureScale(string property){Vector2 value;return scales.TryGetValue(property,out value)?value:Vector2.one;}
  public Vector2 GetTextureOffset(string property){Vector2 value;return offsets.TryGetValue(property,out value)?value:Vector2.zero;}
  public void SetTextureScale(string property,Vector2 value){scales[property]=value;}public void SetTextureOffset(string property,Vector2 value){offsets[property]=value;}
  public float GetFloat(string property){float value;if(floats.TryGetValue(property,out value))return value;return property=="_WorkflowMode"||property=="_SpecularHighlights"||property=="_EnvironmentReflections"||property=="_SrcBlend"||property=="_ZWrite"||property=="_BlendModePreserveSpecular"?1:0;}
  public void SetFloat(string property,float value){floats[property]=value;}
  public Color GetColor(string property){Color value;if(colors.TryGetValue(property,out value))return value;return property=="_EmissionColor"?new Color(0,0,0):new Color(1,1,1);}
  public void SetColor(string property,Color value){colors[property]=value;}
  public bool IsKeywordEnabled(string keyword){return keywords.Contains(keyword);}public void EnableKeyword(string keyword){keywords.Add(keyword);}public void DisableKeyword(string keyword){keywords.Remove(keyword);}
 }
}
namespace UnityEngine.Rendering {public enum BlendMode {One=1,SrcAlpha=5,OneMinusSrcAlpha=10}}
class MaterialRecoveryRunner {
 static void Check(bool result,string message){if(!result)throw new Exception(message);}
 static void Main(){
  var shader=new UnityEngine.Shader{name="Universal Render Pipeline/Lit"};
  var entry=new YardMaterialEntry{resource="ScrapshiftMaterials/PropAtlas",albedo=new UnityEngine.Texture2D{name="Original atlas"}};
  var material=new UnityEngine.Material(shader);var tint=new UnityEngine.Color(.4f,.5f,.6f);
  material.SetColor("_BaseColor",tint);material.SetFloat("_Smoothness",.21f);material.SetTextureScale("_BaseMap",new UnityEngine.Vector2(2,3));
  Check(!YardMaterialBindings.Missing(material.GetTexture("_BaseMap")),"fixture must reproduce the old missed placeholder");
  Check(YardMaterialBindings.NeedsRepair(material,entry),"default sampler must need repair");
  Check(YardMaterialBindings.Repair(material,entry),"repair must change default sampler");
  Check(material.GetTexture("_BaseMap")==entry.albedo,"albedo recovery");
  Check(!material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"),"default metallic sampler must not enable mask variant");
  Check(material.GetColor("_BaseColor")==tint&&material.GetFloat("_Smoothness")==.21f&&material.GetTextureScale("_BaseMap")==new UnityEngine.Vector2(2,3),"preserve custom surface values");
  Check(!YardMaterialBindings.NeedsRepair(material,entry)&&!YardMaterialBindings.Repair(material,entry),"idempotent repair");
  Console.WriteLine("PASS default sampler recovery, unassigned mask exclusion, preserved surface values and idempotence");
  var upgraded=new UnityEngine.Material(shader);upgraded.SetTexture("_BaseMap",shader.legacyWhite);
  Check(shader.defaultWhite!=shader.legacyWhite,"fixture requires distinct modern and legacy defaults");
  Check(YardMaterialBindings.NeedsRepair(upgraded,entry),"copied legacy placeholder must need repair");
  YardMaterialBindings.Repair(upgraded,entry);
  Check(upgraded.GetTexture("_BaseMap")==entry.albedo&&upgraded.GetTexture("_MainTex")==entry.albedo,"recover both copied legacy placeholders");
  Check(!YardMaterialBindings.NeedsRepair(upgraded,entry),"cross-slot recovery must be idempotent");
  Console.WriteLine("PASS distinct legacy default copied into modern albedo recovers original texture");
  var world=new YardMaterialEntry{albedo=entry.albedo,metallicGloss=new UnityEngine.Texture2D{name="Mask"},emission=new UnityEngine.Texture2D{name="Localized emission"}};
  var worldMaterial=new UnityEngine.Material(shader){globalIlluminationFlags=UnityEngine.MaterialGlobalIlluminationFlags.BakedEmissive};worldMaterial.SetColor("_EmissionColor",new UnityEngine.Color(1,1,1));
  YardMaterialBindings.Repair(worldMaterial,world);
  Check(worldMaterial.GetTexture("_MetallicGlossMap")==world.metallicGloss&&worldMaterial.GetTexture("_EmissionMap")==world.emission,"recover independent mask/emission");
  Check(worldMaterial.IsKeywordEnabled("_METALLICSPECGLOSSMAP")&&worldMaterial.IsKeywordEnabled("_EMISSION"),"valid map variants");
  Console.WriteLine("PASS mask/emission recovery with authored variants");
  // Interleave actual recovery with the public Unity/URP emission protocol, including explicit disable.
  // Before the flag-based fix, disabled flags + white color oscillate on every validation.
  foreach(int flags in new[]{0,1,2,4,5,6,7})foreach(bool black in new[]{false,true})
  {
   var roundTrip=new UnityEngine.Material(shader){globalIlluminationFlags=(UnityEngine.MaterialGlobalIlluminationFlags)flags};
   roundTrip.SetColor("_EmissionColor",black?new UnityEngine.Color(0,0,0):new UnityEngine.Color(1,1,1));
   YardMaterialBindings.Repair(roundTrip,world);
   for(int cycle=0;cycle<4;cycle++)
   {
    ValidateUrpEmission(roundTrip);
    Check(!YardMaterialBindings.NeedsRepair(roundTrip,world)&&!YardMaterialBindings.Repair(roundTrip,world),"URP emission round trip must be idle: "+flags+" / black "+black);
   }
   if(flags==4)Check(roundTrip.globalIlluminationFlags==UnityEngine.MaterialGlobalIlluminationFlags.EmissiveIsBlack&&!roundTrip.IsKeywordEnabled("_EMISSION"),"preserve intentional emission disable");
  }
  Console.WriteLine("PASS 14 GI/color combinations remain idle across four URP emission validation cycles");
  var custom=new UnityEngine.Texture2D{name=shader.defaultWhite.name};var customMaterial=new UnityEngine.Material(shader);
  customMaterial.SetTexture("_BaseMap",custom);customMaterial.SetTexture("_MainTex",custom);customMaterial.SetTexture("_MetallicGlossMap",custom);
  YardMaterialBindings.Repair(customMaterial,entry);
  Check(customMaterial.GetTexture("_BaseMap")==custom&&customMaterial.GetTexture("_MetallicGlossMap")==custom,"same-name user maps must remain");
  Check(customMaterial.IsKeywordEnabled("_METALLICSPECGLOSSMAP"),"custom gloss map remains enabled");Console.WriteLine("PASS same-name custom maps remain intact");
  var legacy=new UnityEngine.Material(shader);legacy.SetTexture("_MainTex",custom);legacy.SetTextureScale("_MainTex",new UnityEngine.Vector2(3,2));
  YardMaterialBindings.Repair(legacy,entry);Check(legacy.GetTexture("_BaseMap")==custom&&legacy.GetTextureScale("_BaseMap")==new UnityEngine.Vector2(3,2),"preserve valid legacy map/UV");Console.WriteLine("PASS valid legacy map/UV takes precedence");
  var spec=new UnityEngine.Material(shader);spec.SetFloat("_WorkflowMode",0);spec.SetTexture("_SpecGlossMap",custom);
  YardMaterialBindings.Repair(spec,world);Check(spec.GetFloat("_WorkflowMode")==0&&spec.GetTexture("_SpecGlossMap")==custom,"preserve specular workflow");Check(YardMaterialBindings.Missing(spec,"_MetallicGlossMap"),"no metallic workflow injection");Console.WriteLine("PASS custom specular workflow");
  var ground=new UnityEngine.Material(shader);ground.SetFloat("_Surface",1);ground.EnableKeyword("_ALPHAPREMULTIPLY_ON");
  var groundEntry=new YardMaterialEntry{albedo=entry.albedo,straightAlpha=true};YardMaterialBindings.Repair(ground,groundEntry);
  Check(ground.GetFloat("_SrcBlend")==5&&ground.GetFloat("_DstBlend")==10&&ground.GetFloat("_ZWrite")==0&&!ground.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"),"straight-alpha ground state");Console.WriteLine("PASS straight-alpha ground recovery");
  var other=new UnityEngine.Material(new UnityEngine.Shader{name="Custom sky"});Check(!YardMaterialBindings.Repair(other,entry),"do not rewrite custom shader");Console.WriteLine("PASS custom shader retained");
 }
 // Reference behavior: UnityCsReference 6000.3 MaterialEditor.FixupEmissiveFlag, URP BaseShaderGUI.SetMaterialKeywords.
 // The supplied EditMode regression uses these real public APIs instead of this narrow adapter.
 static void ValidateUrpEmission(UnityEngine.Material material)
 {
  var flags=material.globalIlluminationFlags;
  if((flags&UnityEngine.MaterialGlobalIlluminationFlags.BakedEmissive)!=0&&material.GetColor("_EmissionColor").maxColorComponent==0)
   flags|=UnityEngine.MaterialGlobalIlluminationFlags.EmissiveIsBlack;
  else if(flags!=UnityEngine.MaterialGlobalIlluminationFlags.EmissiveIsBlack)flags&=UnityEngine.MaterialGlobalIlluminationFlags.AnyEmissive;
  material.globalIlluminationFlags=flags;
  if((flags&UnityEngine.MaterialGlobalIlluminationFlags.AnyEmissive)!=0)material.EnableKeyword("_EMISSION");else material.DisableKeyword("_EMISSION");
 }
}
