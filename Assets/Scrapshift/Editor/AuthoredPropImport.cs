using UnityEditor;
using UnityEngine;

namespace Scrapshift
{
    // Only original generated props use this policy; player-edited materials and other models are untouched.
    public sealed class AuthoredPropImport : AssetPostprocessor
    {
        const string Prefix = "Assets/Scrapshift/Resources/ScrapshiftProps/";
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Prefix, System.StringComparison.Ordinal)) return;
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
            importer.importTangents = ModelImporterTangents.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            // Runtime regional static batching needs readable source vertices.
            importer.isReadable = true;
            importer.addCollider = false;
        }
        void OnPreprocessTexture()
        {
            if (assetPath != Prefix + "ScrapshiftPropAtlas.png") return;
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.isReadable = false;
        }
    }
}
