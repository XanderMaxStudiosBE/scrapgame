using System.Collections.Generic;
using UnityEngine;

namespace Scrapshift
{
    // Original FBX meshes share one URP atlas material. Imported meshes are never runtime-owned.
    public static class AuthoredYardProps
    {
        static readonly Dictionary<string, GameObject> Models = new Dictionary<string, GameObject>();
        static readonly HashSet<string> Missing = new HashSet<string>();
        public static bool TryPlace(string name, Transform parent, Vector3 position, out GameObject instance)
        {
            instance = null;
            if (!Models.TryGetValue(name, out GameObject model) || model == null)
            {
                model = Resources.Load<GameObject>("ScrapshiftProps/" + name);
                if (model != null) Models[name] = model;
            }
            var atlas = YardMaterialBindings.Load("ScrapshiftMaterials/PropAtlas",parent);
            if (model == null || atlas == null)
            {
                if (Missing.Add(name)) Debug.LogWarning("Authored yard prop unavailable: " + name + ". Allow the tracked FBX/material assets to import; using procedural fallback.");
                return false;
            }
            instance = Object.Instantiate(model, parent, false);
            instance.name = name; instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.identity; instance.transform.localScale = Vector3.one;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = atlas;
                if (slots.Length == 0) renderer.sharedMaterial = atlas;
                else renderer.sharedMaterials = slots;
            }
            return true;
        }
    }
}
