using UnityEngine;

namespace Scrapshift
{
    // Poly Haven's CC0 tyre uses its own UVs/maps; the original yard palette cannot shade it.
    public static class OldTyreVisuals
    {
        const string Prefix = "ThirdParty/PolyHaven/OldTyre/";

        public static bool TryPlace(Transform parent, Vector3 centre, float yaw)
        {
            var model = Resources.Load<GameObject>(Prefix + "OldTyre");
            var material = Resources.Load<Material>(Prefix + "OldTyre");
            if (model == null || material == null) return false;

            var instance = Object.Instantiate(model, parent, false);
            instance.name = "Discarded tyre / Poly Haven";
            // Exported mesh is centred in X/Z, rests at Y=0 and measures .6 x .165 x .6m.
            instance.transform.localPosition = centre - Vector3.up * .115f;
            instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            instance.transform.localScale = new Vector3(.76f / .6f, .23f / .165f, .76f / .6f);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = material;
            return true;
        }
    }
}
