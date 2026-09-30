using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Scrapshift.Tests
{
    public sealed class ControlSettingsTests
    {
        [TestCaseSource(typeof(ControlScenarios), nameof(ControlScenarios.Names))]
        public void ControlRules(string name) { ControlScenarios.Run(name); }

        [Test]
        public void EverySupportedCodeIsARealLegacyKeyAndEscapeIsReserved()
        {
            foreach (string code in ControlPreferences.SupportedCodes)
                Assert.IsTrue(Enum.IsDefined(typeof(KeyCode), code), "Missing KeyCode: " + code);
            Assert.IsFalse(ControlPreferences.IsSupported("Escape"));
        }
        [Test]
        public void RuntimeLabelsAndMenuBackReflectCurrentSettings()
        {
            string path = Path.Combine(Path.GetTempPath(), "scrapshift-controls-" + Guid.NewGuid(), "controls-v1.json");
            var input = new PlayerInputSettings(path);
            var menu = new SettingsMenu(input);
            StringAssert.Contains("Up arrow", input.Label(ControlAction.MoveForward));
            input.Preferences.SetBinding(ControlAction.ManualWork, "F", false);
            Assert.AreEqual("F", input.Label(ControlAction.ManualWork));
            menu.Open(); Assert.IsTrue(menu.IsOpen);
            Assert.IsTrue(menu.HandleEscape()); Assert.IsFalse(menu.IsOpen);
            Assert.IsFalse(menu.HandleEscape());
        }
        [Test]
        public void PreferencesRoundTripAndRestoreDoNotTouchGameplaySave()
        {
            string directory = Path.Combine(Path.GetTempPath(), "scrapshift-controls-" + Guid.NewGuid());
            string path = Path.Combine(directory, "controls-v1.json"), yard = Path.Combine(directory, "yard-v1.json");
            try
            {
                Directory.CreateDirectory(directory); File.WriteAllText(yard, "untouched gameplay save");
                var p = new ControlPreferences(); p.SetBinding(ControlAction.Interact, "F", false); p.sensitivity = 3.75f; p.invertY = true;
                ControlSettingsStore.Write(path, p);
                var restored = ControlSettingsStore.Read(path, out _);
                Assert.AreEqual(JsonUtility.ToJson(p), JsonUtility.ToJson(restored));
                restored.RestoreDefaults(); ControlSettingsStore.Write(path, restored);
                Assert.AreEqual("E", ControlSettingsStore.Read(path, out _).Binding(ControlAction.Interact));
                Assert.AreEqual("untouched gameplay save", File.ReadAllText(yard));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
        [Test]
        public void BadPreferencesRecoverBackupOrPreserveWorkingDefaults()
        {
            string directory = Path.Combine(Path.GetTempPath(), "scrapshift-controls-" + Guid.NewGuid());
            string path = Path.Combine(directory, "controls-v1.json");
            try
            {
                var p = new ControlPreferences(); p.SetBinding(ControlAction.Drop, "G", false); ControlSettingsStore.Write(path, p);
                p.SetBinding(ControlAction.Drop, "H", false); ControlSettingsStore.Write(path, p);
                File.WriteAllText(path, "{}");
                string notice; Assert.AreEqual("G", ControlSettingsStore.Read(path, out notice).Binding(ControlAction.Drop));
                StringAssert.Contains("backup", notice);
                File.WriteAllText(path + ".bak", "{}");
                var defaults = ControlSettingsStore.Read(path, out notice); defaults.Validate();
                Assert.AreEqual("Q", defaults.Binding(ControlAction.Drop)); StringAssert.Contains("defaults", notice);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
