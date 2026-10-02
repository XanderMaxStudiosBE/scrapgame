using System;
using UnityEngine;
namespace Scrapshift
{
    [Serializable] public sealed class YardMaterialEntry
    {
        public string resource;
        public Material material;
        public Texture2D albedo, metallicGloss, emission;
        public string albedoPath, metallicGlossPath, emissionPath;
        public bool straightAlpha;
        // Path fallback also works in a player for original textures under Resources.
        // Return a descriptor copy rather than altering the loaded catalogue asset at runtime.
        public YardMaterialEntry ResolveRuntimeTextures()
        {
            var color=Resolve(albedo,albedoPath);var mask=Resolve(metallicGloss,metallicGlossPath);var glow=Resolve(emission,emissionPath);
            if(color==albedo && mask==metallicGloss && glow==emission)return this;
            return new YardMaterialEntry{resource=resource,material=material,albedo=color,metallicGloss=mask,emission=glow,
                albedoPath=albedoPath,metallicGlossPath=metallicGlossPath,emissionPath=emissionPath,straightAlpha=straightAlpha};
        }
        static Texture2D Resolve(Texture2D existing,string path)
        {
            if(existing!=null || string.IsNullOrEmpty(path))return existing;
            int index=path.IndexOf("/Resources/",StringComparison.Ordinal);
            if(index<0 || !path.EndsWith(".png",StringComparison.OrdinalIgnoreCase))return null;
            return Resources.Load<Texture2D>(path.Substring(index+11,path.Length-index-15));
        }
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
