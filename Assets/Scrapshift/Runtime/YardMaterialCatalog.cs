using System;
using UnityEngine;
namespace Scrapshift
{
    [Serializable] public sealed class YardMaterialEntry
    {
        public string resource;
        public Material material;
        public Texture2D albedo, metallicGloss, emission;
        public bool straightAlpha;
    }
    // Direct texture references survive URP's legacy material upgrade and keep textures in player builds.
    [CreateAssetMenu(menuName="Scrapshift/Material bindings")]
    public sealed class YardMaterialCatalog : ScriptableObject
    {
        public YardMaterialEntry[] entries;
        public YardMaterialEntry Find(string resource)
        {
            if(entries!=null)foreach(var entry in entries)if(entry!=null && entry.resource==resource)return entry;
            return null;
        }
    }
}
