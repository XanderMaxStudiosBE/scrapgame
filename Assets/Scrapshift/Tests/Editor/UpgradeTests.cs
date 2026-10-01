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
