using System;
namespace Scrapshift
{
    public enum GraphicsPreset { Laptop = 0, Balanced = 1, Detailed = 2 }
    [Serializable]
    public sealed class PresentationPreferences
    {
        public int version = 1;
        public GraphicsPreset graphics = GraphicsPreset.Balanced;
        public int frameLimit = 60;
        public float fieldOfView = 72;
        public float masterVolume = .8f, effectsVolume = .8f, ambienceVolume = .45f;
        public bool warmGrade = true;
        public bool showFrameRate;
        public float RenderScale { get { return graphics == GraphicsPreset.Laptop ? .75f : graphics == GraphicsPreset.Balanced ? .9f : 1; } }
        public float ShadowDistance { get { return graphics == GraphicsPreset.Laptop ? 20 : graphics == GraphicsPreset.Balanced ? 35 : 50; } }
        public void Validate()
        {
            if (version != 1 || graphics < GraphicsPreset.Laptop || graphics > GraphicsPreset.Detailed ||
                (frameLimit != 30 && frameLimit != 60 && frameLimit != 120) || !InRange(fieldOfView, 55, 95) ||
                !InRange(masterVolume, 0, 1) || !InRange(effectsVolume, 0, 1) || !InRange(ambienceVolume, 0, 1))
                throw new ArgumentException("Invalid video/audio preferences.");
        }
        static bool InRange(float value, float min, float max) { return !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max; }
    }
}
