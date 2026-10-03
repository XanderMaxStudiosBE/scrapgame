using UnityEditor;
using UnityEngine;

namespace Scrapshift
{
    // Scope conversion to this CC0 asset; Asset Store packages keep their own importer settings.
    public sealed class OldTyreImport : AssetPostprocessor
    {
        const string Prefix = "Assets/Scrapshift/Resources/ThirdParty/PolyHaven/OldTyre/";

        void OnPreprocessModel()
        {
            if (assetPath != Prefix + "OldTyre.fbx") return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = true;
            importer.addCollider = false;
        }

        void OnPreprocessTexture()
        {
            if (assetPath != Prefix + "Albedo.png" && assetPath != Prefix + "Normal.png" &&
                assetPath != Prefix + "MetalSmoothness.png") return;
            var importer = (TextureImporter)assetImporter;
            bool normal = assetPath == Prefix + "Normal.png";
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = assetPath == Prefix + "Albedo.png";
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.alphaIsTransparency = false;
        }
    }
}
