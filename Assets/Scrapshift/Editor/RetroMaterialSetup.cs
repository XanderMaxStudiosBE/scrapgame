using System;
using UnityEditor;
using UnityEngine;

namespace Scrapshift
{
    /// <summary>Repairs missing assets without replacing existing materials or their GUIDs.</summary>
    public static class RetroMaterialSetup
    {
        const string TextureRoot = "Assets/Scrapshift/Art/Textures/";
        const string MaterialRoot = "Assets/Scrapshift/Resources/ScrapshiftMaterials";

        [MenuItem("Scrapshift/Validate Retro Materials")]
        public static void EnsureMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit is unavailable. Resolve the project's pinned URP package before creating materials.");

            EnsureFolder("Assets/Scrapshift", "Resources");
            EnsureFolder("Assets/Scrapshift/Resources", "ScrapshiftMaterials");
            foreach (RetroSurface surface in Enum.GetValues(typeof(RetroSurface)))
            {
                string texturePath = TextureRoot + surface + ".png";
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer == null)
                    throw new InvalidOperationException("Missing tracked retro texture: " + texturePath);

                if(SurfaceTextureSampling.Configure(importer))importer.SaveAndReimport();

                string materialPath = MaterialRoot + "/" + surface + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material != null)
                    continue; // Preserve user-edited material values, texture assignments, and shader choice.

                material = new Material(shader) { name = surface.ToString() };
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", .08f);
                material.SetFloat("_Metallic", surface == RetroSurface.Copper ? .25f : 0f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Retro textures validated. Existing material edits were preserved.");
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
