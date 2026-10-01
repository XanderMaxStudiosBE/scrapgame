using System;
using System.IO;
using NUnit.Framework;
namespace Scrapshift.Tests
{
    public sealed class PresentationTests
    {
        [TestCaseSource(typeof(PresentationScenarios), nameof(PresentationScenarios.Names))]
        public void PreferencesAndPresets(string scenario) { PresentationScenarios.Run(scenario); }
        [Test]
        public void PreferencesPersistSeparatelyAndRecoverBackup()
        {
            string dir = Path.Combine(Path.GetTempPath(), "scrapshift-presentation-" + Guid.NewGuid());
            string path = Path.Combine(dir, "presentation.json");
            try
            {
                var p = new PresentationPreferences { graphics = GraphicsPreset.Laptop, masterVolume = .25f, fieldOfView = 80 };
                PresentationStore.Write(path, p);
                var restored = PresentationStore.Read(path, out _);
                Assert.AreEqual(GraphicsPreset.Laptop, restored.graphics); Assert.AreEqual(.25f, restored.masterVolume); Assert.AreEqual(80, restored.fieldOfView);
                p.graphics = GraphicsPreset.Detailed; PresentationStore.Write(path, p); File.WriteAllText(path, "invalid");
                restored = PresentationStore.Read(path, out string notice);
                Assert.AreEqual(GraphicsPreset.Laptop, restored.graphics); Assert.That(notice, Does.Contain("backup"));
                Assert.IsFalse(File.Exists(Path.Combine(dir, "yard-v1.json"))); Assert.IsFalse(File.Exists(Path.Combine(dir, "controls-v1.json")));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
    }
}
