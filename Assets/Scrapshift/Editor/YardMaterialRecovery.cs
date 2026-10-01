using UnityEditor;
using UnityEngine;
namespace Scrapshift
{
    // Runs after URP's material postprocessors. Repairs missing bindings, never overwrites a valid custom map/tint.
    [InitializeOnLoad] public sealed class YardMaterialRecovery : AssetPostprocessor
    {
        static bool pending;
        static YardMaterialRecovery(){Queue();}
        static void Queue(){if(pending)return;pending=true;EditorApplication.delayCall+=Recover;}
        static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
        {
            foreach(var path in imported)if(path.StartsWith("Assets/Scrapshift/",System.StringComparison.Ordinal)&&
                (path.EndsWith(".mat",System.StringComparison.Ordinal)||path.EndsWith("Materials.asset",System.StringComparison.Ordinal))){Queue();return;}
        }
        [MenuItem("Scrapshift/Repair Missing Material Bindings")]
        public static void Recover()
        {
            pending=false;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}
            var catalog=AssetDatabase.LoadAssetAtPath<YardMaterialCatalog>("Assets/Scrapshift/Resources/ScrapshiftRendering/Materials.asset");
            if(catalog==null||catalog.entries==null)return;
            SurfaceTextureSampling.Ensure();
            int count=0;
            foreach(var entry in catalog.entries)
                if(entry!=null && YardMaterialBindings.Repair(entry.material,entry))
                {EditorUtility.SetDirty(entry.material);AssetDatabase.SaveAssetIfDirty(entry.material);count++;}
            if(count>0)Debug.Log("Scrapshift restored material bindings/render state for "+count+" materials. Valid custom maps, colors and surface values were retained.");
        }
    }
}
