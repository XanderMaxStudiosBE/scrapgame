using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Scrapshift.Tests
{
    public sealed class PrototypeTests
    {
        [TestCaseSource(typeof(CoreScenarios), nameof(CoreScenarios.Names))]
        public void CoreRules(string scenario) { CoreScenarios.Run(scenario); }

        [Test]
        public void SaveRoundTripPreservesEveryMaterialLocationAndProgress()
        {
            var model = new YardModel(new YardRules()); model.State.machineOwned = true; model.State.money = 17;
            model.AcquireWire(); model.FeedMachine(); model.Tick(2);
            model.AcquireWire(); model.LoadBench(); model.WorkBench();
            model.AcquireWire(); model.Drop(2, .3f, -2); model.AcquireWire();
            string directory = Path.Combine(Path.GetTempPath(), "scrapshift-" + Guid.NewGuid());
            string path = Path.Combine(directory, "save.json");
            try
            {
                SaveStore.Write(path, model.State);
                var restored = SaveStore.Read(path, out _);
                Assert.AreEqual(JsonUtility.ToJson(model.State), JsonUtility.ToJson(restored));
                var resumed = new YardModel(model.Rules, restored);
                resumed.Tick(3);
                Assert.AreEqual(3, resumed.State.machineOutput);
                Assert.AreEqual(2, resumed.State.items.Count);
                Assert.NotNull(resumed.Carried);
                Assert.AreEqual(1, resumed.State.benchStrokes);
                SaveStore.Write(path, resumed.State);
                Assert.IsTrue(File.Exists(path + ".bak"));
                Assert.AreEqual(3, SaveStore.Read(path, out _).machineOutput);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
        [Test]
        public void CorruptPrimaryUsesBackupAndDoubleCorruptionRefusesReset()
        {
            string directory = Path.Combine(Path.GetTempPath(), "scrapshift-" + Guid.NewGuid());
            string path = Path.Combine(directory, "save.json");
            try
            {
                SaveStore.Write(path, new YardState { money = 12 });
                SaveStore.Write(path, new YardState { money = 24 });
                File.WriteAllText(path, "{}");
                LogAssert.Expect(LogType.Warning, "Could not load save.json: Missing save fields.");
                Assert.AreEqual(12, SaveStore.Read(path, out _).money);
                File.WriteAllText(path + ".bak", "{}");
                LogAssert.Expect(LogType.Warning, "Could not load save.json: Missing save fields.");
                LogAssert.Expect(LogType.Warning, "Could not load save.json.bak: Missing save fields.");
                Assert.Throws<InvalidDataException>(() => SaveStore.Read(path, out _));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
