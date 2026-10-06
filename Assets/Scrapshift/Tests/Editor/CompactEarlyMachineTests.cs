using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactEarlyMachineTests
    {
        [TestCaseSource(typeof(CompactEarlyMachineScenarios),"Names")]
        public void SharedRegression(string name){CompactEarlyMachineScenarios.Run(name);}
        [Test]
        public void ActualJsonRetainsTier1IntakePartialJobAndExistingOutputLink()
        {
            var rules=new CompactRules{startingMoney=1000,startingCars=0,startingRefrigerators=0};var model=new ScrappingModel(rules);var state=model.State;
            var construction=new ConstructionModel(state,rules);
            Assert.IsTrue(construction.Place(EquipmentKind.Tier1Scrapper,-8,0,0),construction.LastMessage);int machine=construction.LastPlacedId;
            Assert.IsTrue(construction.Place(EquipmentKind.Storage,-8,6,180));int input=construction.LastPlacedId;
            Assert.IsTrue(construction.Place(EquipmentKind.Storage,-8,-6,180));int output=construction.LastPlacedId;
            Assert.IsTrue(construction.Place(EquipmentKind.Generator,-2,0,0));Assert.IsTrue(construction.Connect(construction.LastPlacedId,machine));
            model.HasPower=id=>construction.PowerFor(id).powered;var automation=new AutomationModel(model,construction);
            Assert.IsTrue(automation.Connect(input,0,machine,0,true),automation.LastMessage);
            Assert.IsTrue(automation.Connect(machine,0,output,0,true),automation.LastMessage);
            Assert.Less(AutomationModel.Port(model.FindEquipment(machine),rules,true,0).z,0,"Existing Tier1 output remains on -Z.");
            model.FindEquipment(machine).contents.Add(new CompactStack{id=state.nextId++,kind=PartKind.Wire,quantity=1,xpEligible=false});
            model.FindEquipment(input).contents.Add(new CompactStack{id=state.nextId++,kind=PartKind.Wire,quantity=1,xpEligible=true});
            Assert.IsTrue(model.AutoBegin(machine));model.Tick(.75f);automation.Tick(.1f);automation.Tick(.4f);
            float transit=state.belts[0].items[0].progress;int transitId=state.belts[0].items[0].id;int cash=state.money;
            var saved=JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(state));CompactSaveStore.Validate(saved,rules);
            var resumed=new ScrappingModel(rules,saved);var power=new ConstructionModel(saved,rules);resumed.HasPower=id=>power.PowerFor(id).powered;
            var transport=new AutomationModel(resumed,power);
            Assert.AreEqual(3.25f,resumed.FindEquipment(machine).job.remaining);Assert.IsFalse(resumed.FindEquipment(machine).job.xpEligible);
            Assert.AreEqual(transitId,saved.belts[0].items[0].id);Assert.AreEqual(transit,saved.belts[0].items[0].progress);
            for(int i=0;i<600;i++){resumed.Tick(.1f);transport.Tick(.1f);}
            Assert.AreEqual(10,resumed.StoredUnits(output));Assert.AreEqual(cash,saved.money);Assert.AreEqual(0,saved.experience);Assert.IsNull(resumed.FindEquipment(machine).job);
            int oldUnits=0,newUnits=0;foreach(var item in resumed.FindEquipment(output).contents)if(item.xpEligible)newUnits+=item.quantity;else oldUnits+=item.quantity;
            Assert.AreEqual(5,oldUnits);Assert.AreEqual(5,newUnits);
        }
        [Test]
        public void ActualJsonKeepsBufferedManualWorkAndDoesNotPerformStrokesOnResume()
        {
            var rules=new CompactRules{startingCars=0,startingRefrigerators=0};var model=new ScrappingModel(rules);int bench=model.State.equipment[0].id;
            model.FindEquipment(bench).contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Wire,quantity=2,xpEligible=false});
            Assert.IsTrue(model.AutoBegin(bench));Assert.IsTrue(model.Work(bench));Assert.IsTrue(model.Work(bench));
            var saved=JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(model.State));CompactSaveStore.Validate(saved,rules);
            var resumed=new ScrappingModel(rules,saved);Assert.IsFalse(resumed.Tick(500));
            Assert.AreEqual(2,resumed.FindEquipment(bench).job.strokes);Assert.AreEqual(1,resumed.StoredUnits(bench));Assert.IsFalse(resumed.FindEquipment(bench).job.ready);
            Assert.IsTrue(resumed.Work(bench));Assert.IsTrue(resumed.Work(bench));Assert.IsTrue(resumed.CollectOutput(bench,0));
            Assert.AreEqual(3,resumed.Carried.quantity);Assert.IsFalse(resumed.Carried.xpEligible);Assert.AreEqual(0,resumed.SaleQuote().experience);
            Assert.IsTrue(resumed.Sell());Assert.IsTrue(resumed.CollectOutput(bench,1));Assert.IsTrue(resumed.Sell());
            Assert.IsNull(resumed.FindEquipment(bench).job);Assert.IsTrue(resumed.Tick(.1f));
            Assert.AreEqual(0,resumed.FindEquipment(bench).job.strokes);Assert.AreEqual(0,resumed.StoredUnits(bench));Assert.AreEqual(0,saved.experience);
        }
        [Test]
        public void ActualJsonRetainsIndependentFullInputAndOutputBays()
        {
            var rules=new CompactRules{startingMoney=1000,startingCars=0,startingRefrigerators=0};
            rules.Equipment(EquipmentKind.Tier1Scrapper).outputCapacity=4096;
            rules.Recipe(PartKind.Wire).yields=new[]{new PartAmount(PartKind.Copper,4096)};
            var model=new ScrappingModel(rules);var construction=new ConstructionModel(model.State,rules);
            Assert.IsTrue(construction.Place(EquipmentKind.Tier1Scrapper,-8,0,0));int machine=construction.LastPlacedId;
            Assert.IsTrue(construction.Place(EquipmentKind.Generator,-2,0,0));Assert.IsTrue(construction.Connect(construction.LastPlacedId,machine));
            model.HasPower=id=>construction.PowerFor(id).powered;
            model.FindEquipment(machine).contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Wire,quantity=4096,xpEligible=false});
            Assert.IsTrue(model.AutoBegin(machine));model.Tick(.5f);
            Assert.AreEqual(4095,model.QueueUnits(machine));Assert.AreEqual(4096,model.ReservedOutputUnits(machine));
            var saved=JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(model.State));CompactSaveStore.Validate(saved,rules);
            var resumed=new ScrappingModel(rules,saved);resumed.HasPower=id=>true;
            Assert.AreEqual(4095,resumed.QueueUnits(machine));Assert.AreEqual(4096,resumed.ReservedOutputUnits(machine));
            Assert.AreEqual(3.5f,resumed.FindEquipment(machine).job.remaining);Assert.IsFalse(resumed.FindEquipment(machine).job.xpEligible);
            resumed.Tick(3.5f);Assert.IsTrue(resumed.FindEquipment(machine).job.ready);Assert.AreEqual(4095,resumed.QueueUnits(machine));
            Assert.IsTrue(resumed.CollectOutput(machine,0));Assert.AreEqual(4096,resumed.Carried.quantity);Assert.IsFalse(resumed.Carried.xpEligible);
            Assert.AreEqual(0,saved.experience);Assert.AreEqual(4095,resumed.QueueUnits(machine));
        }
    }
}
