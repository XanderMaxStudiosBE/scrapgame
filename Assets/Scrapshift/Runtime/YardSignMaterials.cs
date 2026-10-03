using System.Collections.Generic;
using UnityEngine;

namespace Scrapshift
{
    /// <summary>One private font material per font/world, with atlas updates and bounded lifetime.</summary>
    [ExecuteAlways]
    public sealed class YardSignMaterials : MonoBehaviour
    {
        readonly Dictionary<Font, Material> materials = new Dictionary<Font, Material>();
        [SerializeField] List<TextMesh> labels = new List<TextMesh>();
        bool disposed;
        bool missingShaderReported;
        bool subscribed;
#if UNITY_EDITOR
        bool reloadSubscribed;
#endif

        static YardSignMaterials Owner(Transform parent)
        {
            var root = parent.root;
            var owner = root.GetComponent<YardSignMaterials>();
            return owner != null ? owner : root.gameObject.AddComponent<YardSignMaterials>();
        }

        public static Material Get(Transform parent, Font font)
        {
            return Owner(parent).MaterialFor(font);
        }

        public static Material Bind(Transform parent, TextMesh label)
        {
            var owner = Owner(parent);
            // Equipment rebuilds destroy their lettering while the world owner survives.
            for (int i = owner.labels.Count - 1; i >= 0; i--)
                if (owner.labels[i] == null) owner.labels.RemoveAt(i);
            if (!owner.labels.Contains(label)) owner.labels.Add(label);
            return owner.MaterialFor(label.font);
        }

        void Awake() { Subscribe(); }
        void OnEnable()
        {
            Subscribe(); RebindLabels();
            foreach (var font in materials.Keys) RefreshAtlas(font);
        }
        void OnDisable() { Font.textureRebuilt -= RefreshAtlas; subscribed = false; }

        void Subscribe()
        {
            if (!subscribed) { Font.textureRebuilt += RefreshAtlas; subscribed = true; }
#if UNITY_EDITOR
            if (!reloadSubscribed) { UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Dispose; reloadSubscribed = true; }
#endif
            disposed = false;
        }

        void RebindLabels()
        {
            for (int i = labels.Count - 1; i >= 0; i--)
            {
                var label = labels[i];
                if (label == null) { labels.RemoveAt(i); continue; }
                label.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(label.font);
            }
        }

        Material MaterialFor(Font font)
        {
            bool reactivate = disposed;
            Subscribe();
            if (reactivate) RebindLabels();
            if (materials.TryGetValue(font, out Material material) && material != null) return material;
            var template = Resources.Load<Material>("ScrapshiftSigns/WorldSignText");
            if (template == null || template.shader == null || !template.shader.isSupported)
            {
                if (!missingShaderReported)
                {
                    missingShaderReported = true;
                    Debug.LogWarning("World sign shader unavailable. Using Unity's builtin font material until the tracked sign assets import.");
                }
                return font.material;
            }
            material = new Material(template) { name = "Private world sign lettering", hideFlags = HideFlags.DontSave };
            material.SetTexture("_MainTex", font.material.mainTexture);
            materials.Add(font, material);
            return material;
        }

        // Font atlas growth can replace the texture. This runs only on font rebuild, never each frame.
        public void RefreshAtlas(Font font)
        {
            if (disposed || font == null || !materials.TryGetValue(font, out Material material) || material == null) return;
            material.SetTexture("_MainTex", font.material.mainTexture);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; Font.textureRebuilt -= RefreshAtlas;
            subscribed = false;
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Dispose; reloadSubscribed = false;
#endif
            foreach (var material in materials.Values)
                if (material != null) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            materials.Clear();
        }
        void OnDestroy() { Dispose(); }
    }
}
