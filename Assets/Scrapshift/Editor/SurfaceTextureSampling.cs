using System;
using UnityEditor;
using UnityEngine;
namespace Scrapshift
{
    // Coarse source textures with mipmaps: keep their authored colors, avoid distant gravel shimmer.
    public sealed class SurfaceTextureSampling : AssetPostprocessor
    {
        const string Prefix="Assets/Scrapshift/Art/Textures/";
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(Prefix,StringComparison.Ordinal))return;
            string name=System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if(Enum.TryParse(name,out RetroSurface surface) && Enum.IsDefined(typeof(RetroSurface),surface))Configure((TextureImporter)assetImporter);
        }
        public static bool Configure(TextureImporter importer)
        {
            bool changed=importer.filterMode!=FilterMode.Bilinear || importer.wrapMode!=TextureWrapMode.Repeat ||
                !importer.mipmapEnabled || importer.anisoLevel!=2 || importer.textureCompression!=TextureImporterCompression.Uncompressed ||
                !importer.sRGBTexture || importer.maxTextureSize!=64;
            if(!changed)return false;
            importer.textureType=TextureImporterType.Default;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Repeat;
            importer.mipmapEnabled=true;importer.anisoLevel=2;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.sRGBTexture=true;importer.maxTextureSize=64;return true;
        }
        public static void Ensure()
        {
            foreach(RetroSurface surface in Enum.GetValues(typeof(RetroSurface)))
            {
                var importer=AssetImporter.GetAtPath(Prefix+surface+".png") as TextureImporter;
                if(importer!=null && Configure(importer))importer.SaveAndReimport();
            }
        }
    }
}
