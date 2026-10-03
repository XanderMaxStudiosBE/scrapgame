using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrapshift
{
    /// <summary>Stationary industrial lettering. Local -Z is the readable front.</summary>
    public static class YardSignText
    {
        const int RasterSize = 64;
        public static TextMesh CreateLabel(Transform parent, string text, Vector3 position, float yaw = 0)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            font.RequestCharactersInTexture(text ?? string.Empty, RasterSize, FontStyle.Bold);
            var label = new GameObject(string.IsNullOrEmpty(text) ? "Painted lettering" : text);
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var mesh = label.AddComponent<TextMesh>();
            mesh.font = font; mesh.fontSize = RasterSize; mesh.fontStyle = FontStyle.Bold;
            mesh.characterSize = .035f; mesh.richText = false;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.lineSpacing = 1.08f; mesh.color = YardGeometry.Ivory; mesh.text = text ?? string.Empty;
            var renderer = label.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = YardSignMaterials.Bind(parent, mesh);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return mesh;
        }

        /// <summary>Fit actual glyph bounds in label-local metres, independent of parent scale/yaw.</summary>
        public static void Fit(TextMesh label, float width, float height, float maxCharacterSize = .035f)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (!Positive(width) || !Positive(height) || !Positive(maxCharacterSize))
                throw new ArgumentOutOfRangeException(nameof(width), "Sign dimensions and character size must be finite and positive.");
            label.font.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
            label.characterSize = maxCharacterSize;
            Vector3 size = label.GetComponent<MeshRenderer>().localBounds.size;
            if (size.x <= 0 || size.y <= 0) return; // Empty labels retain a safe size for later dynamic text.
            float fit = Mathf.Min(1, Mathf.Min(width / size.x, height / size.y));
            label.characterSize = maxCharacterSize * fit;
        }

        /// <summary>Thin bolted ivory plate. The last child of parent remains the direct TextMesh.</summary>
        public static TextMesh Plate(Transform parent, string text, Vector3 position, float width,
            float height = .36f, float yaw = 0)
        {
            if (!Positive(width) || !Positive(height)) throw new ArgumentOutOfRangeException(nameof(width));
            var frame = new GameObject("Bolted painted sign plate").transform;
            frame.SetParent(parent, false); frame.localPosition = position;
            frame.localRotation = Quaternion.Euler(0, yaw, 0);
            PaintBox("Worn metal rim", frame, Vector3.zero, new Vector3(width, height, .045f), YardGeometry.Rust);
            PaintBox("Aged ivory paint", frame, new Vector3(0, 0, -.026f),
                new Vector3(width * .95f, height * .88f, .012f), YardGeometry.Ivory);
            // Small dark washers/fasteners, safely outside the lettering inset. No interaction colliders.
            float bolt = Mathf.Min(.025f, height * .075f);
            foreach (float x in new[] { -width * .455f, width * .455f })
                foreach (float y in new[] { -height * .36f, height * .36f })
                    PaintBox("Dark corner fastener", frame, new Vector3(x, y, -.034f),
                        new Vector3(bolt, bolt, .007f), YardGeometry.Charcoal);
            var label = CreateLabel(parent, text,
                position + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, -.039f), yaw);
            label.color = YardGeometry.Charcoal;
            Fit(label, width * .80f, height * .67f);
            return label;
        }

        static void PaintBox(string name, Transform parent, Vector3 position, Vector3 size, Color color)
        {
            // SurfaceBox creates no Collider component when decorative, and owns its mesh lifetime.
            var box = YardGeometry.SurfaceBox(name, parent, position, size, RetroSurface.DarkMetal, false);
            box.GetComponent<MeshRenderer>().sharedMaterial = YardGeometry.PaletteMaterial(color);
        }

        static bool Positive(float value) { return value > 0 && !float.IsInfinity(value) && !float.IsNaN(value); }
    }
}
