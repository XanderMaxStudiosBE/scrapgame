using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactRoutingTests
    {
        [TestCaseSource(typeof(CompactRoutingScenarios),nameof(CompactRoutingScenarios.Names))]
        public void EarlyProductionLineAndCompatibleRules(string name){CompactRoutingScenarios.Run(name);}
        [Test]
        public void ImportedDefaultBalanceContainsEarlyTransport()
        {
            var balance=Resources.Load<CompactBalance>("ScrapshiftCompact/Balance");Assert.IsNotNull(balance);
            Assert.AreEqual(1,balance.PreparedRules.Equipment(EquipmentKind.Conveyor).unlockLevel);
            Assert.AreEqual(1,balance.PreparedRules.Equipment(EquipmentKind.Storage).unlockLevel);
            Assert.AreEqual(5,balance.PreparedRules.Equipment(EquipmentKind.Tier2Scrapper).unlockLevel);
        }
        [Test]
        public void AbsentRoutingVersionInActualJsonMigratesDefaultGates()
        {
            var source=ScriptableObject.CreateInstance<CompactBalance>();var loaded=ScriptableObject.CreateInstance<CompactBalance>();
            try
            {
                source.rules.Equipment(EquipmentKind.Conveyor).unlockLevel=10;
                string json=JsonUtility.ToJson(source).Replace("\"routingRulesVersion\":0,","").Replace(",\"routingRulesVersion\":0","");
                Assert.IsFalse(json.Contains("\"routingRulesVersion\""));JsonUtility.FromJsonOverwrite(json,loaded);
                Assert.AreEqual(0,loaded.rules.routingRulesVersion);Assert.AreEqual(1,loaded.PreparedRules.Equipment(EquipmentKind.Conveyor).unlockLevel);
                Assert.AreEqual(1,loaded.rules.routingRulesVersion);
            }
            finally{Object.DestroyImmediate(source);Object.DestroyImmediate(loaded);}
        }
    }
}
