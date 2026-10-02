using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactStorageTests
    {
        [TestCaseSource(typeof(CompactStorageScenarios),nameof(CompactStorageScenarios.Names))]
        public void PortedStorageAndTierTwo(string name){CompactStorageScenarios.Run(name);}
        [Test]
        public void ActualJsonRetainsBufferedInputsFilteringAndInFlightLineage()
        {
            var rules=new CompactRules();var m=new ScrappingModel(rules);var storage=new EquipmentState{id=m.State.nextId++,kind=EquipmentKind.Storage,x=5,z=5,filterKind=(int)PartKind.Copper,routeCursor=2};
            m.State.equipment.Add(storage);storage.contents.Add(new CompactStack{id=m.State.nextId++,kind=PartKind.Copper,quantity=3,xpEligible=false});
            var tier=new EquipmentState{id=m.State.nextId++,kind=EquipmentKind.Tier2Scrapper,x=-5,z=6};m.State.equipment.Add(tier);
            var belt=new ConveyorLink{id=m.State.nextId++,fromId=storage.id,toId=tier.id,paidPrice=24,launchRemaining=.3f};m.State.belts.Add(belt);
            belt.items.Add(new ConveyorItem{id=m.State.nextId++,kind=PartKind.Wire,xpEligible=true,progress=.45f});
            var restored=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(m.State)));
            Assert.AreEqual(3,restored.StoredUnits(storage.id));Assert.IsFalse(restored.FindEquipment(storage.id).contents[0].xpEligible);
            Assert.AreEqual((int)PartKind.Copper,restored.FindEquipment(storage.id).filterKind);Assert.AreEqual(2,restored.FindEquipment(storage.id).routeCursor);
            Assert.AreEqual(.45f,restored.State.belts[0].items[0].progress);Assert.IsTrue(restored.State.belts[0].items[0].xpEligible);
        }
        [Test]
        public void TrackedBalanceEnablesOnlyImplementedProductionStages()
        {
            var rules=Resources.Load<CompactBalance>("ScrapshiftCompact/Balance").PreparedRules;
            Assert.AreEqual(1.2f,rules.beltSpeed);Assert.AreEqual(.65f,rules.beltSpacing);Assert.AreEqual(8,rules.beltCapacity);Assert.AreEqual(64,rules.maxBelts);
            foreach(var kind in new[]{EquipmentKind.Storage,EquipmentKind.Tier2Scrapper,EquipmentKind.Conveyor,EquipmentKind.Splitter,EquipmentKind.Merger})
            {Assert.IsTrue(rules.Equipment(kind).available);Assert.AreEqual(10,rules.Equipment(kind).unlockLevel);}
            Assert.IsFalse(rules.Equipment(EquipmentKind.ExportStation).available);
        }
        [Test]
        public void ActualSerializedLegacyBalanceGetsOnlyMissingAutomationDefaults()
        {
            var asset=ScriptableObject.CreateInstance<CompactBalance>();
            try
            {
                var authored=new CompactRules{beltSpeed=2,beltSpacing=0,beltCapacity=0,beltMaxLength=0,maxBelts=0};
                authored.Equipment(EquipmentKind.Storage).name="My unavailable storage";authored.Equipment(EquipmentKind.Storage).available=false;
                asset.rules=JsonUtility.FromJson<CompactRules>(JsonUtility.ToJson(authored));
                var prepared=asset.PreparedRules;Assert.AreEqual(2,prepared.beltSpeed);Assert.AreEqual(.65f,prepared.beltSpacing);Assert.AreEqual(8,prepared.beltCapacity);
                Assert.IsFalse(prepared.Equipment(EquipmentKind.Storage).available);Assert.AreEqual("My unavailable storage",prepared.Equipment(EquipmentKind.Storage).name);
            }
            finally{Object.DestroyImmediate(asset);}
        }
        [Test]
        public void ActualJsonRetainsExplicitBatchAndRetiresItsSourceIds()
        {
            var rules=new CompactRules();var m=new ScrappingModel(rules);var storage=new EquipmentState{id=m.State.nextId++,kind=EquipmentKind.Storage,x=5,z=5};m.State.equipment.Add(storage);
            int selected=0;for(int i=0;i<25;i++){var unit=new CompactStack{id=m.State.nextId++,kind=PartKind.Copper,quantity=1,xpEligible=true};storage.contents.Add(unit);if(i==0)selected=unit.id;}
            Assert.IsTrue(m.WithdrawBatch(storage.id,selected));
            var restored=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(m.State)));
            Assert.AreEqual(selected,restored.Carried.id);Assert.AreEqual(25,restored.Carried.quantity);Assert.AreEqual(0,restored.StoredUnits(storage.id));
            Assert.IsTrue(restored.Sell());Assert.AreEqual(50,restored.State.experience);Assert.IsFalse(restored.WithdrawBatch(storage.id,selected));Assert.IsFalse(restored.Sell());
        }
    }
}
