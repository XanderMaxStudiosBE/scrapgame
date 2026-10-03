using System.Collections.Generic;
using UnityEngine;

namespace Scrapshift.Compact
{
    // Two startup-created surface copies. Never writes to recovered/shared materials or textures.
    public sealed class CompactGroundSurface : MonoBehaviour
    {
        readonly List<Material> owned=new List<Material>(2);
        bool disposed;
        public Material Create(string source,string texture,Vector2 scale)
        {
            var original=YardMaterialBindings.Load(source,transform);
            var map=Resources.Load<Texture2D>(texture);
            if(original==null || map==null)return null; // Imported originals remain a usable fallback.
            var material=new Material(original){name="Private compact ground / "+map.name,hideFlags=HideFlags.DontSave};
            material.SetTexture("_BaseMap",map);material.SetTextureScale("_BaseMap",scale);
            material.SetTexture("_MainTex",map);material.SetTextureScale("_MainTex",scale);
            material.SetColor("_BaseColor",Color.white);material.SetColor("_Color",Color.white);
            owned.Add(material);disposed=false;return material;
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            foreach(var material in owned)
                if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
            owned.Clear();
        }
        void OnDestroy(){Dispose();}
    }
}
