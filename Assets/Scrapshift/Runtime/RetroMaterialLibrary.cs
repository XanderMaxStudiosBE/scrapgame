using System.Collections.Generic;
using UnityEngine;

namespace Scrapshift
{
    public enum RetroSurface
    {
        RustPaint, DarkMetal, CorrugatedMetal, WeatheredWood, Gravel, Copper, WireInsulation
    }

    /// <summary>Shared tracked URP materials. Resources references keep their shaders in player builds.</summary>
    public static class RetroMaterialLibrary
    {
        static readonly Dictionary<RetroSurface, Material> Materials = new Dictionary<RetroSurface, Material>();

        public static Material Get(RetroSurface surface)
        {
            if (Materials.TryGetValue(surface, out Material material) && material != null)
                return material;

            material = Resources.Load<Material>("ScrapshiftMaterials/" + surface);
            if (material == null)
                Debug.LogError("Missing Scrapshift material " + surface + ". Restore the tracked Resources assets or run Scrapshift / Validate Retro Materials.");
            else
                Materials[surface] = material;
            return material;
        }
    }
}
