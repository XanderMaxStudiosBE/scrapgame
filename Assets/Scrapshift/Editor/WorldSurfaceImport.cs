using UnityEditor;
using UnityEngine;
namespace Scrapshift
{
    public sealed class WorldSurfaceImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            const string prefix = "Assets/Scrapshift/Resources/ScrapshiftWorld/";
            if (!assetPath.StartsWith(prefix, System.StringComparison.Ordinal) || !assetPath.EndsWith(".png", System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default; importer.textureShape = TextureImporterShape.Texture2D;
            importer.maxTextureSize = 512; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear; importer.wrapMode = assetPath.EndsWith("ChainLink.png", System.StringComparison.Ordinal) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.mipmapEnabled = true; importer.sRGBTexture = !assetPath.EndsWith("WorldMetalGloss.png", System.StringComparison.Ordinal);
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = !assetPath.EndsWith("WorldMetalGloss.png", System.StringComparison.Ordinal);
            importer.isReadable = false;
        }
    }
}
