using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Scrapshift
{
    // Private recovery copies belong to a world root; asset materials and intentional texture edits stay intact.
    public sealed class YardMaterialBindings : MonoBehaviour
    {
        readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        readonly List<Material> owned=new List<Material>();
        static readonly Dictionary<string,Texture> litDefaults=new Dictionary<string,Texture>();
        public static Material Load(string resource,Transform parent)
        {
            var root=parent.root;
            var scope=root.GetComponent<YardMaterialBindings>();
            if(scope==null)scope=root.gameObject.AddComponent<YardMaterialBindings>();
            if(scope.materials.TryGetValue(resource,out Material cached)&&cached!=null)return cached;
            var catalog=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials");
            var entry=catalog!=null?catalog.Find(resource):null;
            var tracked=Resources.Load<Material>(resource);
            if(tracked==null)return null;
            var result=tracked;
            if(entry!=null && NeedsRepair(tracked,entry))
            {
                result=new Material(tracked){name=tracked.name+" / recovered runtime bindings",hideFlags=HideFlags.DontSave};
                Repair(result,entry);scope.owned.Add(result);
            }
            scope.materials[resource]=result;return result;
        }
        public static bool Missing(Texture texture){return texture==null || texture==Texture2D.whiteTexture;}
        // A shader's unassigned sampler can be a different built-in object from whiteTexture.
        // Compare objects, never names/pixels: a user's small or white texture is still an intentional map.
        public static bool Missing(Material material,string property)
        {
            if(material==null || !material.HasProperty(property))return true;
            var texture=material.GetTexture(property);
            if(Missing(texture))return true;
            if(material.shader==null || material.shader.name!="Universal Render Pipeline/Lit")return false;
            if(texture==LitDefault(material,property))return true;
            // URP's V1 upgrader copies _MainTex into _BaseMap. A legacy default may
            // differ from BaseMap's default object and is still an unassigned map.
            if(property=="_BaseMap")return texture==LitDefault(material,"_MainTex");
            if(property=="_MainTex")return texture==LitDefault(material,"_BaseMap");
            return false;
        }
        static Texture LitDefault(Material material,string property)
        {
            if(!litDefaults.TryGetValue(property,out Texture defaultTexture))
            {
                var probe=new Material(material.shader){hideFlags=HideFlags.DontSave};
                try{defaultTexture=probe.GetTexture(property);litDefaults[property]=defaultTexture;}
                finally{DestroyOwned(probe);}
            }
            return defaultTexture;
        }
        static bool NeedsTexture(Material material,string property,Texture expected)
        {return expected!=null && material.HasProperty(property) && Missing(material,property);}
        static bool KeywordMismatch(Material material,string keyword,bool value){return material.IsKeywordEnabled(keyword)!=value;}
        public static bool NeedsRepair(Material material,YardMaterialEntry entry)
        {
            if(material==null||entry==null||material.shader==null||material.shader.name!="Universal Render Pipeline/Lit")return false;
            return NeedsTexture(material,"_BaseMap",entry.albedo)||(!Specular(material) && NeedsTexture(material,"_MetallicGlossMap",entry.metallicGloss))||NeedsTexture(material,"_EmissionMap",entry.emission)||
                KeywordMismatch(material,"_ALPHATEST_ON",material.GetFloat("_AlphaClip")>=.5f)||
                KeywordMismatch(material,"_SURFACE_TYPE_TRANSPARENT",material.GetFloat("_Surface")>=1)||
                KeywordMismatch(material,"_SPECULARHIGHLIGHTS_OFF",material.GetFloat("_SpecularHighlights")==0)||
                KeywordMismatch(material,"_ENVIRONMENTREFLECTIONS_OFF",material.GetFloat("_EnvironmentReflections")==0)||
                KeywordMismatch(material,"_SPECULAR_SETUP",Specular(material))||
                KeywordMismatch(material,"_METALLICSPECGLOSSMAP",HasGlossMap(material)||(!Specular(material)&&entry.metallicGloss!=null))||
                material.globalIlluminationFlags!=EmissionFlags(material)||KeywordMismatch(material,"_EMISSION",EmissionEnabled(material))||
                (StraightAlpha(material,entry) && (material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON") || material.GetFloat("_BlendModePreserveSpecular")!=0 ||
                    material.GetFloat("_SrcBlend")!=(float)BlendMode.SrcAlpha || material.GetFloat("_DstBlend")!=(float)BlendMode.OneMinusSrcAlpha ||
                    material.GetFloat("_SrcBlendAlpha")!=(float)BlendMode.One || material.GetFloat("_DstBlendAlpha")!=(float)BlendMode.OneMinusSrcAlpha || material.GetFloat("_ZWrite")!=0));
        }
        static bool SetTextureIfMissing(Material material,string property,Texture expected)
        {if(!NeedsTexture(material,property,expected))return false;material.SetTexture(property,expected);return true;}
        static bool SetKeyword(Material material,string keyword,bool enabled)
        {
            if(!KeywordMismatch(material,keyword,enabled))return false;
            if(enabled)material.EnableKeyword(keyword);else material.DisableKeyword(keyword);return true;
        }
        public static bool Repair(Material material,YardMaterialEntry entry)
        {
            if(material==null||entry==null||material.shader==null||material.shader.name!="Universal Render Pipeline/Lit")return false;
            Texture legacy=material.HasProperty("_MainTex")?material.GetTexture("_MainTex"):null;
            bool legacyMissing=Missing(material,"_MainTex");
            bool fromLegacy=!legacyMissing && NeedsTexture(material,"_BaseMap",legacy);
            Vector2 legacyScale=fromLegacy?material.GetTextureScale("_MainTex"):Vector2.one,legacyOffset=fromLegacy?material.GetTextureOffset("_MainTex"):Vector2.zero;
            bool changed=SetTextureIfMissing(material,"_BaseMap",legacyMissing?entry.albedo:legacy);
            if(fromLegacy){material.SetTextureScale("_BaseMap",legacyScale);material.SetTextureOffset("_BaseMap",legacyOffset);}
            if(!Specular(material))changed|=SetTextureIfMissing(material,"_MetallicGlossMap",entry.metallicGloss);
            changed|=SetTextureIfMissing(material,"_EmissionMap",entry.emission);
            changed|=SetKeyword(material,"_ALPHATEST_ON",material.GetFloat("_AlphaClip")>=.5f);
            changed|=SetKeyword(material,"_SURFACE_TYPE_TRANSPARENT",material.GetFloat("_Surface")>=1);
            changed|=SetKeyword(material,"_SPECULARHIGHLIGHTS_OFF",material.GetFloat("_SpecularHighlights")==0);
            changed|=SetKeyword(material,"_ENVIRONMENTREFLECTIONS_OFF",material.GetFloat("_EnvironmentReflections")==0);
            changed|=SetKeyword(material,"_SPECULAR_SETUP",Specular(material));
            changed|=SetKeyword(material,"_METALLICSPECGLOSSMAP",HasGlossMap(material));
            var emissionFlags=EmissionFlags(material);
            if(material.globalIlluminationFlags!=emissionFlags){material.globalIlluminationFlags=emissionFlags;changed=true;}
            changed|=SetKeyword(material,"_EMISSION",EmissionEnabled(material));
            // These transparent layers use straight alpha; specular must fade with their irregular edge.
            if(StraightAlpha(material,entry))
            {
                changed|=SetKeyword(material,"_ALPHAPREMULTIPLY_ON",false);
                changed|=Float(material,"_BlendModePreserveSpecular",0);
                changed|=Float(material,"_SrcBlend",(float)BlendMode.SrcAlpha);changed|=Float(material,"_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                changed|=Float(material,"_SrcBlendAlpha",(float)BlendMode.One);changed|=Float(material,"_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
                changed|=Float(material,"_ZWrite",0);
            }
            // Keep legacy aliases synchronized so a later URP V1 upgrade cannot erase current values.
            if(material.HasProperty("_MainTex") && (material.GetTexture("_MainTex")!=material.GetTexture("_BaseMap") || material.GetTextureScale("_MainTex")!=material.GetTextureScale("_BaseMap") || material.GetTextureOffset("_MainTex")!=material.GetTextureOffset("_BaseMap")))
            {material.SetTexture("_MainTex",material.GetTexture("_BaseMap"));material.SetTextureScale("_MainTex",material.GetTextureScale("_BaseMap"));material.SetTextureOffset("_MainTex",material.GetTextureOffset("_BaseMap"));changed=true;}
            if(material.HasProperty("_Color") && material.GetColor("_Color")!=material.GetColor("_BaseColor")){material.SetColor("_Color",material.GetColor("_BaseColor"));changed=true;}
            changed|=Float(material,"_Glossiness",material.GetFloat("_Smoothness"));changed|=Float(material,"_GlossMapScale",material.GetFloat("_Smoothness"));
            changed|=Float(material,"_GlossyReflections",material.GetFloat("_EnvironmentReflections"));
            return changed;
        }
        // Match Unity 6000.3 MaterialEditor.FixupEmissiveFlag and URP BaseShaderGUI.
        // Exactly EmissiveIsBlack means deliberately disabled, even with a non-black saved color.
        static MaterialGlobalIlluminationFlags EmissionFlags(Material material)
        {
            var flags=material.globalIlluminationFlags;
            if(!material.HasProperty("_EmissionColor"))return flags;
            if((flags&MaterialGlobalIlluminationFlags.BakedEmissive)!=0 && material.GetColor("_EmissionColor").maxColorComponent==0)
                flags|=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            else if(flags!=MaterialGlobalIlluminationFlags.EmissiveIsBlack)flags&=MaterialGlobalIlluminationFlags.AnyEmissive;
            return flags;
        }
        static bool EmissionEnabled(Material material)
        {
            return (EmissionFlags(material)&MaterialGlobalIlluminationFlags.AnyEmissive)!=0 ||
                (material.HasProperty("_EmissionEnabled") && material.GetFloat("_EmissionEnabled")>=.5f);
        }
        static bool Specular(Material material){return material.GetFloat("_WorkflowMode")==0;}
        static bool HasGlossMap(Material material){return !Missing(material,Specular(material)?"_SpecGlossMap":"_MetallicGlossMap");}
        static bool StraightAlpha(Material material,YardMaterialEntry entry)
        {return entry.straightAlpha && material.GetFloat("_Surface")>=1 && material.GetFloat("_Blend")==0;}
        static bool Float(Material material,string name,float value)
        {if(!material.HasProperty(name)||material.GetFloat(name)==value)return false;material.SetFloat(name,value);return true;}
        static void DestroyOwned(Object value)
        {if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        public void Dispose()
        {
            foreach(var material in owned)DestroyOwned(material);
            owned.Clear();materials.Clear();
        }
        void OnDestroy(){Dispose();}
    }
}
