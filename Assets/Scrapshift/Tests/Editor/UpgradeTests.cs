using NUnit.Framework;
using UnityEngine;
using System;
using System.IO;
namespace Scrapshift.Tests
{
    public sealed class UpgradeTests
    {
        [TestCaseSource(typeof(UpgradeScenarios),nameof(UpgradeScenarios.Names))]
        public void BusinessInvestments(string scenario){UpgradeScenarios.Run(scenario);}
        [Test]
        public void LegacySerializedBalanceRetainsExistingTuning()
        {
            var balance=ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                balance.rules=JsonUtility.FromJson<YardRules>("{\"copperPerWire\":5,\"copperUnitPrice\":6,\"machinePrice\":50,\"manualStrokes\":7,\"machineSeconds\":8,\"maxBundles\":12}");
                var prepared=balance.PreparedRules;prepared.Validate();
                Assert.AreEqual(5,prepared.copperPerWire);Assert.AreEqual(6,prepared.copperUnitPrice);Assert.AreEqual(50,prepared.machinePrice);
                Assert.AreEqual(7,prepared.manualStrokes);Assert.AreEqual(8,prepared.machineSeconds);Assert.AreEqual(12,prepared.maxBundles);
                Assert.AreEqual(8,prepared.fanPartsPrice);Assert.AreEqual(60,prepared.storageUpgradePrice);
            }
            finally{UnityEngine.Object.DestroyImmediate(balance);}
        }
        [Test]
        public void SavedReservedOutputsRemainCollectableAfterBalanceCapacityReduction()
        {
            string dir=Path.Combine(Path.GetTempPath(),"scrapshift-capacity-"+Guid.NewGuid());
            try
            {
                var m=new YardModel(new YardRules{maxBundles=2});m.State.machineOwned=true;
                m.AcquireWire();m.FeedMachine();m.AcquireWire();m.Store(MaterialKind.Wire);m.Tick(5);
                string path=Path.Combine(dir,"yard.json");SaveStore.Write(path,m.State);
                var loaded=new YardModel(new YardRules{maxBundles=1},SaveStore.Read(path,out _));
                Assert.IsFalse(loaded.AcquireWire());Assert.IsTrue(loaded.CollectMachine());Assert.IsTrue(loaded.Sell());
                Assert.AreEqual(12,loaded.State.money);Assert.AreEqual(1,loaded.StoredBundles(MaterialKind.Wire));
                Assert.AreEqual(1,loaded.OccupiedBundles);Assert.IsFalse(loaded.AcquireWire());
            }
            finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test]
        public void UpgradeFlagsAndTunedPartialLoadSurviveActualJson()
        {
            string dir=Path.Combine(Path.GetTempPath(),"scrapshift-upgrades-"+Guid.NewGuid());
            try
            {
                var m=new YardModel(new YardRules());m.State.money=1000;
                m.BuyMachine();m.BuyUpgrade(YardUpgrade.StorageRack);m.BuyUpgrade(YardUpgrade.HandTools);m.BuyUpgrade(YardUpgrade.MachineTuning);
                m.AcquireWire();m.FeedMachine();m.Tick(1);
                string path=Path.Combine(dir,"yard.json");SaveStore.Write(path,m.State);
                var loaded=new YardModel(m.Rules,SaveStore.Read(path,out _));
                Assert.AreEqual(JsonUtility.ToJson(m.State),JsonUtility.ToJson(loaded.State));
                Assert.AreEqual(36,loaded.Capacity);Assert.AreEqual(3,loaded.WireWorkSteps);Assert.AreEqual(2,loaded.State.machineRemaining);
                int money=loaded.State.money;Assert.IsFalse(loaded.BuyUpgrade(YardUpgrade.HandTools));Assert.AreEqual(money,loaded.State.money);
                loaded.Tick(2);Assert.AreEqual(3,loaded.State.machineOutput);
            }
            finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
    }
}
