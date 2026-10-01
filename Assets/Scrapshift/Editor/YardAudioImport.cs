using UnityEditor;
using UnityEngine;
namespace Scrapshift
{
    public sealed class YardAudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/Scrapshift/Resources/ScrapshiftAudio/",System.StringComparison.Ordinal))return;
            var importer=(AudioImporter)assetImporter;
            var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings=settings;importer.forceToMono=true;
            importer.loadInBackground=false;
        }
    }
}
