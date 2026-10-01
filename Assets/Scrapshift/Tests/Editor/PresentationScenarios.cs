using System;
namespace Scrapshift.Tests
{
    public static class PresentationScenarios
    {
        public static readonly string[] Names = { "PresentationDefaults", "PresentationPresetCosts", "PresentationInvalidPreferences" };
        public static void Run(string name)
        {
            switch(name)
            {
                case "PresentationDefaults":
                    var defaults = new PresentationPreferences(); defaults.Validate();
                    Check(defaults.graphics == GraphicsPreset.Balanced && defaults.frameLimit == 60 && defaults.fieldOfView == 72, "Preserve comfortable defaults");
                    Check(defaults.masterVolume > 0 && defaults.effectsVolume > 0 && defaults.ambienceVolume > 0, "Sound enabled by default"); break;
                case "PresentationPresetCosts":
                    var p = new PresentationPreferences { graphics = GraphicsPreset.Laptop };
                    float lowScale = p.RenderScale, lowShadows = p.ShadowDistance;
                    p.graphics = GraphicsPreset.Balanced; Check(p.RenderScale > lowScale && p.ShadowDistance > lowShadows, "Balanced budget");
                    p.graphics = GraphicsPreset.Detailed; Check(p.RenderScale == 1 && p.ShadowDistance > 35, "Detailed budget");
                    p.frameLimit = 30; p.fieldOfView = 55; p.masterVolume = 0; p.Validate();
                    p.frameLimit = 120; p.fieldOfView = 95; p.masterVolume = 1; p.Validate(); break;
                case "PresentationInvalidPreferences":
                    Reject(new PresentationPreferences { version = 2 }); Reject(new PresentationPreferences { graphics = (GraphicsPreset)3 });
                    Reject(new PresentationPreferences { frameLimit = 0 }); Reject(new PresentationPreferences { fieldOfView = float.NaN });
                    Reject(new PresentationPreferences { fieldOfView = 96 }); Reject(new PresentationPreferences { masterVolume = float.PositiveInfinity });
                    Reject(new PresentationPreferences { ambienceVolume = -.1f }); Reject(new PresentationPreferences { effectsVolume = 1.1f }); break;
                default: throw new ArgumentException(name);
            }
        }
        static void Check(bool condition, string message) { if(!condition) throw new Exception(message); }
        static void Reject(PresentationPreferences p) { try { p.Validate(); } catch(ArgumentException) { return; } throw new Exception("Invalid preferences accepted"); }
    }
}
