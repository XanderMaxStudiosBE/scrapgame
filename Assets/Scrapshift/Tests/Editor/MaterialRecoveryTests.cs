using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
namespace Scrapshift.Tests
{
    public sealed class MaterialRecoveryTests
    {
        [TestCase("ScrapshiftMaterials/RustPaint")][TestCase("ScrapshiftMaterials/DarkMetal")]
        [TestCase("ScrapshiftMaterials/CorrugatedMetal")][TestCase("ScrapshiftMaterials/WeatheredWood")]
        [TestCase("ScrapshiftMaterials/Gravel")][TestCase("ScrapshiftMaterials/Copper")]
        [TestCase("ScrapshiftMaterials/WireInsulation")][TestCase("ScrapshiftMaterials/PropAtlas")]
        [TestCase("ScrapshiftWorld/WorldProps")][TestCase("ScrapshiftWorld/GroundWear")]
        [TestCase("ScrapshiftWorld/RoughPuddles")][TestCase("ScrapshiftWorld/WireFence")]
        public void CatalogRetainsDirectTextureReferencesIndependentOfMaterialBindings(string resource)
        {
            var catalog=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials");Assert.NotNull(catalog);
            var entry=catalog.Find(resource);Assert.NotNull(entry);Assert.NotNull(entry.albedo);
            Assert.AreSame(Resources.Load<Material>(resource),entry.material);
            if(resource.EndsWith("WorldProps")){Assert.NotNull(entry.metallicGloss);Assert.NotNull(entry.emission);}
            Assert.AreEqual(resource.EndsWith("GroundWear")||resource.EndsWith("RoughPuddles"),entry.straightAlpha);
        }
        [TestCase(false)][TestCase(true)]
        public void EmptyBindingsRecoverWithoutChangingTintRoughnessOrUvAndSecondRepairIsIdle(bool engineWhite)
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftWorld/WorldProps");
            var material=new Material(entry.material);
            try
            {
                Texture missing=engineWhite?Texture2D.whiteTexture:null;
                material.SetTexture("_BaseMap",missing);material.SetTexture("_MainTex",null);
                material.SetTexture("_MetallicGlossMap",missing);material.SetTexture("_EmissionMap",missing);
                var tint=new Color(.4f,.5f,.6f,.7f);var scale=new Vector2(2,3);var offset=new Vector2(.2f,.1f);
                material.SetColor("_BaseColor",tint);material.SetFloat("_Smoothness",.21f);
                material.SetTextureScale("_BaseMap",scale);material.SetTextureOffset("_BaseMap",offset);
                Assert.IsTrue(YardMaterialBindings.NeedsRepair(material,entry));Assert.IsTrue(YardMaterialBindings.Repair(material,entry));
                Assert.AreSame(entry.albedo,material.GetTexture("_BaseMap"));Assert.AreSame(entry.metallicGloss,material.GetTexture("_MetallicGlossMap"));
                Assert.AreSame(entry.emission,material.GetTexture("_EmissionMap"));Assert.AreEqual(tint,material.GetColor("_BaseColor"));
                Assert.AreEqual(.21f,material.GetFloat("_Smoothness"));Assert.AreEqual(scale,material.GetTextureScale("_BaseMap"));Assert.AreEqual(offset,material.GetTextureOffset("_BaseMap"));
                Assert.AreEqual(scale,material.GetTextureScale("_MainTex"));Assert.AreEqual(offset,material.GetTextureOffset("_MainTex"));
                Assert.IsFalse(YardMaterialBindings.NeedsRepair(material,entry));Assert.IsFalse(YardMaterialBindings.Repair(material,entry));
            }
            finally{Object.DestroyImmediate(material);}
        }
        [TestCase("ScrapshiftMaterials/PropAtlas")][TestCase("ScrapshiftWorld/WorldProps")]
        public void ShaderDefaultSamplersRecoverAndDoNotEnableAnUnassignedGlossMap(string resource)
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find(resource);
            var defaults=new Material(entry.material.shader);var material=new Material(entry.material);
            try
            {
                foreach(var property in new[]{"_BaseMap","_MainTex","_MetallicGlossMap","_SpecGlossMap","_EmissionMap"})
                    material.SetTexture(property,defaults.GetTexture(property));
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                Assert.IsTrue(YardMaterialBindings.NeedsRepair(material,entry));
                YardMaterialBindings.Repair(material,entry);
                Assert.AreSame(entry.albedo,material.GetTexture("_BaseMap"));
                Assert.AreEqual(entry.metallicGloss!=null,material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"),"Only an authored metallic mask enables its variant");
                if(entry.metallicGloss!=null)Assert.AreSame(entry.metallicGloss,material.GetTexture("_MetallicGlossMap"));
                if(entry.emission!=null)Assert.AreSame(entry.emission,material.GetTexture("_EmissionMap"));
                Assert.IsFalse(YardMaterialBindings.NeedsRepair(material,entry));
            }
            finally{Object.DestroyImmediate(defaults);Object.DestroyImmediate(material);}
        }
        [Test]
        public void LegacyDefaultCopiedIntoModernAlbedoRestoresOriginalTexture()
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftMaterials/PropAtlas");
            var defaults=new Material(entry.material.shader);var material=new Material(entry.material);
            try
            {
                material.SetTexture("_BaseMap",defaults.GetTexture("_MainTex"));material.SetTexture("_MainTex",defaults.GetTexture("_MainTex"));
                Assert.IsTrue(YardMaterialBindings.NeedsRepair(material,entry));YardMaterialBindings.Repair(material,entry);
                Assert.AreSame(entry.albedo,material.GetTexture("_BaseMap"));Assert.AreSame(entry.albedo,material.GetTexture("_MainTex"));
                Assert.IsFalse(YardMaterialBindings.NeedsRepair(material,entry));
            }
            finally{Object.DestroyImmediate(defaults);Object.DestroyImmediate(material);}
        }
        [Test]
        public void UserTextureNamedLikeTheEngineDefaultIsPreserved()
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftMaterials/PropAtlas");
            var defaults=new Material(entry.material.shader);var material=new Material(entry.material);
            var custom=new Texture2D(2,2){name=defaults.GetTexture("_BaseMap")!=null?defaults.GetTexture("_BaseMap").name:"Default-White"};
            try
            {
                material.SetTexture("_BaseMap",custom);material.SetTexture("_MainTex",custom);
                YardMaterialBindings.Repair(material,entry);
                Assert.AreSame(custom,material.GetTexture("_BaseMap"));Assert.AreSame(custom,material.GetTexture("_MainTex"));
                Assert.IsFalse(YardMaterialBindings.Missing(material,"_BaseMap"));
            }
            finally{Object.DestroyImmediate(defaults);Object.DestroyImmediate(material);Object.DestroyImmediate(custom);}
        }
        [Test]
        public void ValidCustomMapsAndAdditiveBlendRemainIntact()
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftWorld/RoughPuddles");
            var material=new Material(entry.material);var custom=new Texture2D(2,2);
            try
            {
                material.SetTexture("_BaseMap",custom);material.SetTexture("_MetallicGlossMap",custom);material.SetTexture("_EmissionMap",custom);
                material.SetFloat("_Surface",1);material.SetFloat("_Blend",2);material.SetFloat("_SrcBlend",1);material.SetFloat("_DstBlend",1);material.SetFloat("_ZWrite",1);
                YardMaterialBindings.Repair(material,entry);
                foreach(var prop in new[]{"_BaseMap","_MetallicGlossMap","_EmissionMap"})Assert.AreSame(custom,material.GetTexture(prop));
                Assert.AreEqual(2,material.GetFloat("_Blend"));Assert.AreEqual(1,material.GetFloat("_DstBlend"));Assert.AreEqual(1,material.GetFloat("_ZWrite"));
            }
            finally{Object.DestroyImmediate(material);Object.DestroyImmediate(custom);}
        }
        [Test]
        public void CustomSpecularWorkflowRetainsItsOwnGlossMap()
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftWorld/WorldProps");
            var material=new Material(entry.material);var custom=new Texture2D(2,2);
            try
            {
                material.SetFloat("_WorkflowMode",0);material.SetTexture("_SpecGlossMap",custom);material.SetTexture("_MetallicGlossMap",null);
                YardMaterialBindings.Repair(material,entry);Assert.AreEqual(0,material.GetFloat("_WorkflowMode"));Assert.AreSame(custom,material.GetTexture("_SpecGlossMap"));
                Assert.IsNull(material.GetTexture("_MetallicGlossMap"));Assert.IsTrue(material.IsKeywordEnabled("_SPECULAR_SETUP"));Assert.IsTrue(material.IsKeywordEnabled("_METALLICSPECGLOSSMAP"));
                Assert.IsFalse(YardMaterialBindings.NeedsRepair(material,entry));
            }
            finally{Object.DestroyImmediate(material);Object.DestroyImmediate(custom);}
        }
        [Test]
        public void ValidLegacyAlbedoTakesPriorityOverCatalogFallback()
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftMaterials/PropAtlas");
            var material=new Material(entry.material);var custom=new Texture2D(2,2);
            try
            {
                material.SetTexture("_BaseMap",null);material.SetTexture("_MainTex",custom);
                material.SetTextureScale("_MainTex",new Vector2(3,2));material.SetTextureOffset("_MainTex",new Vector2(.1f,.3f));
                Assert.IsTrue(YardMaterialBindings.Repair(material,entry));Assert.AreSame(custom,material.GetTexture("_BaseMap"));
                Assert.AreEqual(new Vector2(3,2),material.GetTextureScale("_BaseMap"));Assert.AreEqual(new Vector2(.1f,.3f),material.GetTextureOffset("_BaseMap"));
            }
            finally{Object.DestroyImmediate(material);Object.DestroyImmediate(custom);}
        }
        [Test]
        public void GroundTransparencyRepairsEvenWithIntactAlbedo()
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftWorld/GroundWear");
            var material=new Material(entry.material);
            try
            {
                material.SetTexture("_BaseMap",entry.albedo);material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.SetFloat("_BlendModePreserveSpecular",1);material.SetFloat("_SrcBlend",1);material.SetFloat("_ZWrite",1);
                Assert.IsTrue(YardMaterialBindings.NeedsRepair(material,entry));YardMaterialBindings.Repair(material,entry);
                Assert.IsFalse(material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"));Assert.IsTrue(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"));
                Assert.AreEqual(0,material.GetFloat("_BlendModePreserveSpecular"));Assert.AreEqual((float)BlendMode.SrcAlpha,material.GetFloat("_SrcBlend"));
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha,material.GetFloat("_DstBlend"));Assert.AreEqual(0,material.GetFloat("_ZWrite"));
                Assert.IsFalse(YardMaterialBindings.NeedsRepair(material,entry));
            }
            finally{Object.DestroyImmediate(material);}
        }
        [Test]
        public void RuntimeRecoveryCachesOneOwnedCopyAndDoesNotEditTheAsset()
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftMaterials/PropAtlas");
            var snapshot=new Material(entry.material);var root=new GameObject("Material lifetime test");Material recovered=null;
            try
            {
                entry.material.SetTexture("_BaseMap",null);entry.material.SetTexture("_MainTex",null);
                recovered=YardMaterialBindings.Load(entry.resource,root.transform);
                Assert.AreNotSame(entry.material,recovered);Assert.AreSame(entry.albedo,recovered.GetTexture("_BaseMap"));
                Assert.IsNull(entry.material.GetTexture("_BaseMap"));Assert.AreSame(recovered,YardMaterialBindings.Load(entry.resource,root.transform));
                Object.DestroyImmediate(root);root=null;Assert.IsTrue(recovered==null,"private copy dies with its world");
            }
            finally{entry.material.CopyPropertiesFromMaterial(snapshot);Object.DestroyImmediate(snapshot);if(root!=null)Object.DestroyImmediate(root);}
        }
        [Test]
        public void MaterialKeywordsFollowCutoutSpecularAndReflectionValues()
        {
            var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftWorld/WireFence");var material=new Material(entry.material);
            try
            {
                material.SetFloat("_AlphaClip",1);material.SetFloat("_SpecularHighlights",0);material.SetFloat("_EnvironmentReflections",0);
                material.DisableKeyword("_ALPHATEST_ON");YardMaterialBindings.Repair(material,entry);
                Assert.IsTrue(material.IsKeywordEnabled("_ALPHATEST_ON"));Assert.IsTrue(material.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF"));Assert.IsTrue(material.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF"));
                material.SetFloat("_AlphaClip",0);material.SetFloat("_SpecularHighlights",1);material.SetFloat("_EnvironmentReflections",1);YardMaterialBindings.Repair(material,entry);
                Assert.IsFalse(material.IsKeywordEnabled("_ALPHATEST_ON"));Assert.IsFalse(material.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF"));Assert.IsFalse(material.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF"));
            }
            finally{Object.DestroyImmediate(material);}
        }
        [Test]
        public void CustomShaderIsNotRewrittenByRecovery()
        {
            var material=new Material(Resources.Load<Material>("ScrapshiftLighting/YardSky"));
            try
            {
                var entry=Resources.Load<YardMaterialCatalog>("ScrapshiftRendering/Materials").Find("ScrapshiftMaterials/PropAtlas");
                string shader=material.shader.name;Assert.IsFalse(YardMaterialBindings.Repair(material,entry));Assert.IsFalse(YardMaterialBindings.NeedsRepair(material,entry));Assert.AreEqual(shader,material.shader.name);
            }
            finally{Object.DestroyImmediate(material);}
        }
    }
}
