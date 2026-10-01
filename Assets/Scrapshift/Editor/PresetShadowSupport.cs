using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Scrapshift
{
    // URP 17.3 defaults to no soft-shadow shader support. Enable it for our preset-capable source asset.
    // Only this capability flag is migrated; Laptop's runtime lights still use hard shadows.
    public static class PresetShadowSupport
    {
        const string PipelinePath="Assets/Scrapshift/Generated/ScrapshiftURP.asset";
        [InitializeOnLoadMethod]
        static void ScheduleCheck() { EditorApplication.delayCall+=EnsureGenerated; }
        public static void EnsureGenerated()
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if(pipeline==null||!Enable(pipeline))return;
            AssetDatabase.SaveAssetIfDirty(pipeline);
            Debug.Log("Scrapshift soft-shadow support enabled for Balanced/Detailed; Laptop retains hard shadows. Existing pipeline values and GUID were preserved.");
        }
        public static bool Enable(UniversalRenderPipelineAsset pipeline)
        {
            if(pipeline==null||pipeline.supportsSoftShadows)return false;
            // The 17.3 runtime setter is internal. Use Unity's supported editor serialization API.
            var serialized=new SerializedObject(pipeline);
            var support=serialized.FindProperty("m_SoftShadowsSupported");
            if(support==null)throw new System.InvalidOperationException("Pinned URP soft-shadow property is missing. Check the project's URP version.");
            support.boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();return true;
        }
    }
}
