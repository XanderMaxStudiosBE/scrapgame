using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class ProgressionTests
    {
        [Test]
        public void StoredBundlesAndPartialOrderRoundTripInTheGameplaySave()
        {
            string directory = Path.Combine(Path.GetTempPath(), "scrapshift-business-" + Guid.NewGuid());
            string path = Path.Combine(directory, "yard.json");
            try
            {
                var m = new YardModel(new YardRules()); m.State.orderIndex = 1; m.AcceptOrder();
                m.AcquireWire(); m.LoadBench(); for (int i = 0; i < 4; i++) m.WorkBench();
                m.CollectBench(); m.DeliverOrder(); m.AcquireWire(); m.Store(MaterialKind.Wire);
                SaveStore.Write(path, m.State);
                var restored = new YardModel(m.Rules, SaveStore.Read(path, out _));
                Assert.AreEqual(JsonUtility.ToJson(m.State), JsonUtility.ToJson(restored.State));
                Assert.AreEqual(3, restored.State.orderDelivered);
                Assert.AreEqual(1, restored.StoredBundles(MaterialKind.Wire));
                Assert.IsTrue(restored.Retrieve(MaterialKind.Wire));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
        [Test]
        public void OlderVersionOneSaveDefaultsToEmptyStorageAndNoOrder()
        {
            string directory = Path.Combine(Path.GetTempPath(), "scrapshift-legacy-business-" + Guid.NewGuid());
            string path = Path.Combine(directory, "yard.json");
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(path, "{\"version\":1,\"money\":12,\"nextId\":2,\"carriedId\":1,\"items\":[{\"id\":1,\"kind\":0,\"quantity\":1,\"x\":0,\"y\":0,\"z\":0}]}");
                var restored = new YardModel(new YardRules(), SaveStore.Read(path, out _));
                Assert.AreEqual(12, restored.State.money); Assert.NotNull(restored.Carried);
                Assert.AreEqual(StorageSlot.None, restored.Carried.storage);
                Assert.IsFalse(restored.State.orderAccepted); Assert.AreEqual(0, restored.State.orderIndex);
                Assert.AreEqual(0, restored.StoredBundles(MaterialKind.Wire));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
        [Test]
        public void BusinessStationCollidersAndDecorationsResolveToTheirMarkers()
        {
            var root = new GameObject("Business station test");
            try
            {
                var visual = YardBusinessVisual.Build(root.transform);
                visual.Refresh(new YardModel(new YardRules()));
                var markers = root.GetComponentsInChildren<InteractionTarget>();
                Assert.AreEqual(3, markers.Length);
                foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                    if (collider.enabled) Assert.NotNull(collider.GetComponentInParent<InteractionTarget>());
                Assert.IsNotEmpty(visual.orderText.text);
                Assert.AreEqual(4, visual.wireStock.childCount);
                Assert.AreEqual(4, visual.copperStock.childCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [TestCaseSource(typeof(ProgressionScenarios), nameof(ProgressionScenarios.Names))]
        public void StorageAndCustomerOrders(string scenario) { ProgressionScenarios.Run(scenario); }
    }
}
