using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactScrappingTests
    {
        [TestCaseSource(typeof(CompactScrappingScenarios),nameof(CompactScrappingScenarios.Names))]
        public void CompactManualAndTierOne(string name){CompactScrappingScenarios.Run(name);}

        [Test]
        public void SnapshotSerializationRetainsPartialDismantlingAndAlreadyCollectedOutputs()
        {
            var rules=new CompactRules();var m=new ScrappingModel(rules);int car=m.State.scrap[0].id;
            m.InspectScrap(car);m.WorkScrap(car);m.WorkScrap(car);
            var resumed=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(m.State)));
            Assert.AreEqual(2,resumed.FindScrap(car).strokes);Assert.AreEqual(6,resumed.FindScrap(car).requiredStrokes);
            while(resumed.FindScrap(car).strokes<6)resumed.WorkScrap(car);resumed.CollectScrap(car,0);resumed.Drop(0,.25f,0);
            var second=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(resumed.State)));
            Assert.IsFalse(second.CollectScrap(car,0));Assert.AreEqual(1,second.State.items.Count);
        }
        [Test]
        public void TrackedBalanceLoadsWithEditableCompleteRecipes()
        {
            var balance=Resources.Load<CompactBalance>("ScrapshiftCompact/Balance");Assert.IsNotNull(balance);
            var rules=balance.PreparedRules;Assert.AreEqual(12,rules.parts.Length);Assert.AreEqual(6,rules.recipes.Length);
            Assert.AreEqual(2,rules.largeRecipes.Length);Assert.AreEqual(9,rules.equipment.Length);
            Assert.AreEqual(12,rules.BonusForLevel(2));Assert.AreEqual(45,rules.BonusForLevel(10));
            Assert.IsTrue(rules.Equipment(EquipmentKind.Conveyor).available);Assert.AreEqual(10,rules.Equipment(EquipmentKind.Conveyor).unlockLevel);
            Assert.IsFalse(rules.Equipment(EquipmentKind.ExportStation).available);
        }
        [Test]
        public void ActualJsonRetainsInterruptedPoweredRecipeSnapshot()
        {
            var rules=new CompactRules();var m=new ScrappingModel(rules);var machine=new EquipmentState{id=m.State.nextId++,kind=EquipmentKind.Tier1Scrapper,x=5,z=5,paidPrice=60};m.State.equipment.Add(machine);
            m.HasPower=id=>true;m.AcquireWire();m.BeginProcessing(machine.id);m.Tick(1.25f);
            var restored=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(m.State)));restored.HasPower=id=>true;
            Assert.AreEqual(2.75f,restored.FindEquipment(machine.id).job.remaining);restored.Tick(2.75f);Assert.IsTrue(restored.CollectOutput(machine.id,0));
            Assert.AreEqual(3,restored.Carried.quantity);Assert.IsTrue(restored.Sell());Assert.IsFalse(restored.CollectOutput(machine.id,0));
        }
    }
}
