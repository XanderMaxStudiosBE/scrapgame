using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Scrapshift.Tests
{
    public sealed class PreferenceWriteTests
    {
        [TestCaseSource(typeof(PreferenceWriteScenarios), nameof(PreferenceWriteScenarios.Names))]
        public void DeferredWriting(string name) { PreferenceWriteScenarios.Run(name); }

        static string Folder() { return Path.Combine(Path.GetTempPath(), "scrapshift-preferences-" + Guid.NewGuid().ToString("N")); }
        [Test]
        public void LookPreviewAppliesNowWritesAfterIdleAndFlushesOnMenuClose()
        {
            string folder = Folder(), path = Path.Combine(folder, "controls.json");
            try
            {
                var input = new PlayerInputSettings(path);
                input.PreviewLook(3, false, 10); input.PreviewLook(4.5f, true, 10.2f);
                Assert.AreEqual(4.5f, input.Sensitivity); Assert.IsTrue(input.InvertY); Assert.IsTrue(input.HasPendingWrite);
                input.UpdatePending(10.5f); Assert.IsFalse(File.Exists(path));
                input.UpdatePending(10.7f); Assert.IsTrue(File.Exists(path)); Assert.IsFalse(input.HasPendingWrite);
                var loaded = ControlSettingsStore.Read(path, out _); Assert.AreEqual(4.5f, loaded.sensitivity); Assert.IsTrue(loaded.invertY);
                var menu = new SettingsMenu(input); menu.Open(); input.PreviewLook(6, false, 20); menu.HandleEscape();
                loaded = ControlSettingsStore.Read(path, out _); Assert.AreEqual(6, loaded.sensitivity); Assert.IsFalse(loaded.invertY);
                Assert.IsFalse(menu.IsOpen); Assert.IsFalse(input.HasPendingWrite);
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test]
        public void RebindSetLookAndDefaultsStillSaveImmediatelyIncludingPendingValues()
        {
            string folder = Folder(), path = Path.Combine(folder, "controls.json");
            try
            {
                var input = new PlayerInputSettings(path); input.PreviewLook(4, true, 10);
                var rebind = new ControlRebind(input.Preferences); rebind.Begin(ControlAction.Interact); Assert.IsTrue(rebind.Capture("F")); input.Save();
                var loaded = ControlSettingsStore.Read(path, out _); Assert.AreEqual("F", loaded.Binding(ControlAction.Interact)); Assert.AreEqual(4, loaded.sensitivity);
                Assert.IsTrue(loaded.invertY); Assert.IsFalse(input.HasPendingWrite);
                input.PreviewLook(5, false, 20); input.SetLook(6, true);
                loaded = ControlSettingsStore.Read(path, out _); Assert.AreEqual(6, loaded.sensitivity); Assert.IsTrue(loaded.invertY);
                input.PreviewLook(7, true, 30); input.RestoreDefaults();
                loaded = ControlSettingsStore.Read(path, out _); Assert.AreEqual("E", loaded.Binding(ControlAction.Interact)); Assert.AreEqual(2, loaded.sensitivity);
                Assert.IsFalse(loaded.invertY); Assert.IsFalse(input.HasPendingWrite);
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test]
        public void FailedLookWriteRetainsLiveValuesWithoutRepeatedAutomaticRetries()
        {
            string folder = Folder(), path = Path.Combine(folder, "controls.json");
            try
            {
                Directory.CreateDirectory(folder); File.WriteAllText(path + ".tmp", "blocking temporary file");
                var input = new PlayerInputSettings(path); input.PreviewLook(4, true, 10);
                using (var locked = new FileStream(path + ".tmp", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    input.UpdatePending(11); Assert.IsTrue(input.HasPendingWrite); StringAssert.Contains("could not save", input.Notice);
                    Assert.AreEqual(4, input.Sensitivity); Assert.IsTrue(input.InvertY);
                }
                input.UpdatePending(30); Assert.IsFalse(File.Exists(path), "A failed automatic write waits for an explicit retry or another edit.");
                Assert.IsTrue(input.FlushPending()); Assert.IsFalse(input.HasPendingWrite);
                var loaded = ControlSettingsStore.Read(path, out _); Assert.AreEqual(4, loaded.sensitivity); Assert.IsTrue(loaded.invertY);
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test]
        public void VideoAudioPreviewAppliesImmediatelyAndFinalValuesFlushOnDispose()
        {
            string folder = Folder(), path = Path.Combine(folder, "presentation.json");
            var root = new GameObject("Deferred presentation check"); PresentationSettings settings = null;
            var oldPipeline = QualitySettings.renderPipeline; int oldFrame = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount;
            try
            {
                var camera = root.AddComponent<Camera>(); settings = new PresentationSettings(camera, root.transform, path);
                settings.Preferences.fieldOfView = 81; settings.Preferences.frameLimit = 30; settings.Preview(10, true);
                Assert.AreEqual(81, camera.fieldOfView); Assert.AreEqual(30, Application.targetFrameRate); Assert.IsFalse(File.Exists(path));
                camera.fieldOfView = 79; settings.Preferences.masterVolume = .35f; settings.Preview(10.2f, false);
                Assert.AreEqual(79, camera.fieldOfView, "An audio preview does not reapply unrelated camera/pipeline settings.");
                settings.UpdatePending(10.5f); Assert.IsFalse(File.Exists(path));
                settings.Dispose(); Assert.IsTrue(File.Exists(path)); Assert.IsFalse(settings.HasPendingWrite);
                var loaded = PresentationStore.Read(path, out _); Assert.AreEqual(81, loaded.fieldOfView); Assert.AreEqual(.35f, loaded.masterVolume);
                Assert.AreEqual(oldPipeline, QualitySettings.renderPipeline); Assert.AreEqual(oldFrame, Application.targetFrameRate);
            }
            finally
            {
                if (settings != null) settings.Dispose(); QualitySettings.renderPipeline = oldPipeline;
                Application.targetFrameRate = oldFrame; QualitySettings.vSyncCount = oldVsync;
                UnityEngine.Object.DestroyImmediate(root); if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }
        [Test]
        public void ClosingSettingsFlushesControlsAndPresentationTogether()
        {
            string folder = Folder(), controlsPath = Path.Combine(folder, "controls.json"), videoPath = Path.Combine(folder, "presentation.json");
            var root = new GameObject("Settings final flush check"); PresentationSettings presentation = null;
            try
            {
                var input = new PlayerInputSettings(controlsPath); presentation = new PresentationSettings(root.AddComponent<Camera>(), root.transform, videoPath);
                var menu = new SettingsMenu(input, presentation); menu.Open(); input.PreviewLook(3.5f, true, 10);
                presentation.Preferences.effectsVolume = .2f; presentation.Preview(10, false); menu.Close();
                Assert.IsFalse(input.HasPendingWrite); Assert.IsFalse(presentation.HasPendingWrite);
                Assert.AreEqual(3.5f, ControlSettingsStore.Read(controlsPath, out _).sensitivity);
                Assert.AreEqual(.2f, PresentationStore.Read(videoPath, out _).effectsVolume);
            }
            finally { if (presentation != null) presentation.Dispose(); UnityEngine.Object.DestroyImmediate(root); if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test]
        public void ClosingSettingsAttemptsBothStoresWhenControlsWriteFails()
        {
            string folder = Folder(), controlsPath = Path.Combine(folder, "controls.json"), videoPath = Path.Combine(folder, "presentation.json");
            var root = new GameObject("Independent settings flush check"); PresentationSettings presentation = null;
            try
            {
                Directory.CreateDirectory(folder); File.WriteAllText(controlsPath + ".tmp", "blocked");
                var input = new PlayerInputSettings(controlsPath); presentation = new PresentationSettings(root.AddComponent<Camera>(), root.transform, videoPath);
                var menu = new SettingsMenu(input, presentation); menu.Open(); input.PreviewLook(4, true, 10);
                presentation.Preferences.ambienceVolume = .25f; presentation.Preview(10, false);
                using (var locked = new FileStream(controlsPath + ".tmp", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    menu.Close(); Assert.IsFalse(menu.IsOpen); Assert.IsTrue(input.HasPendingWrite);
                    StringAssert.Contains("could not save", input.Notice); Assert.IsFalse(presentation.HasPendingWrite);
                    Assert.AreEqual(.25f, PresentationStore.Read(videoPath, out _).ambienceVolume);
                }
                Assert.IsTrue(input.FlushPending()); Assert.AreEqual(4, ControlSettingsStore.Read(controlsPath, out _).sensitivity);
            }
            finally { if (presentation != null) presentation.Dispose(); UnityEngine.Object.DestroyImmediate(root); if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
    }
}
