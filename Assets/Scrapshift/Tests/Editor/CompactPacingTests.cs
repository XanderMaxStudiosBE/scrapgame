using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactPacingTests
    {
        [TestCaseSource(typeof(CompactPacingScenarios),nameof(CompactPacingScenarios.Names))]
        public void ProgressionMigrationAndLegitimatePacing(string name){CompactPacingScenarios.Run(name);}
        [Test]
        public void ActualOldBalanceJsonWithoutVersionReceivesPacedCurve()
        {
            var source=ScriptableObject.CreateInstance<CompactBalance>();var loaded=ScriptableObject.CreateInstance<CompactBalance>();
            try
            {
                source.rules.levelThresholds=(int[])CompactPacingScenarios.PreviousCurve.Clone();
                // Preserve all other serialized tuning while recreating the genuinely absent additive field.
                string fixture=JsonUtility.ToJson(source).Replace("\"progressionRulesVersion\":0,","").Replace(",\"progressionRulesVersion\":0","");
                Assert.IsFalse(fixture.Contains("\"progressionRulesVersion\""));
                JsonUtility.FromJsonOverwrite(fixture,loaded);Assert.AreEqual(0,loaded.rules.progressionRulesVersion);
                var prepared=loaded.PreparedRules;Assert.AreEqual(1,prepared.progressionRulesVersion);
                CollectionAssert.AreEqual(CompactPacingScenarios.PacedCurve,prepared.levelThresholds);
                Assert.AreEqual(8,prepared.startingMoney);Assert.AreEqual(2,prepared.Part(PartKind.Copper).saleXp);
                Assert.IsFalse(prepared.FillMissingProgressionDefaults());
            }
            finally{Object.DestroyImmediate(source);Object.DestroyImmediate(loaded);}
        }
        [Test]
        public void ActualVersionedBalanceJsonKeepsExplicitFormerCurve()
        {
            var source=ScriptableObject.CreateInstance<CompactBalance>();var loaded=ScriptableObject.CreateInstance<CompactBalance>();
            try
            {
                source.rules.progressionRulesVersion=1;source.rules.levelThresholds=(int[])CompactPacingScenarios.PreviousCurve.Clone();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source),loaded);
                CollectionAssert.AreEqual(CompactPacingScenarios.PreviousCurve,loaded.PreparedRules.levelThresholds);
                Assert.AreEqual(1,loaded.rules.progressionRulesVersion);Assert.IsFalse(loaded.rules.FillMissingProgressionDefaults());
            }
            finally{Object.DestroyImmediate(source);Object.DestroyImmediate(loaded);}
        }
    }
}
