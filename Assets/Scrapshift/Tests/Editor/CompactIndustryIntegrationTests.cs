using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactIndustryIntegrationTests
    {
        [TestCaseSource(typeof(CompactIndustryIntegrationScenarios),"Names")]
        public void SharedRegression(string name){CompactIndustryIntegrationScenarios.Run(name);}
        [Test]
        public void ActualJsonPreservesWholeObjectReservationAndPartialProgress()
        {
            var rules=new CompactRules();var model=new ScrappingModel(rules);model.State.money=2000;model.State.experience=rules.levelThresholds[11];
            var construction=new ConstructionModel(model.State,rules);
            Assert.IsTrue(construction.Place(EquipmentKind.PrimaryScrapper,-17,0,0),construction.LastMessage);int primary=construction.LastPlacedId;
            Assert.IsTrue(construction.Place(EquipmentKind.Generator,-9,0,0),construction.LastMessage);
            Assert.IsTrue(construction.Connect(construction.LastPlacedId,primary));model.HasPower=id=>construction.PowerFor(id).powered;
            int ownedObject=model.State.scrap[0].id;
            Assert.IsTrue(model.Industry.FeedScrap(primary,ownedObject),model.Industry.LastMessage);model.Industry.Tick(7.25f);
            var original=model.FindEquipment(primary).industry.primary;int next=model.State.nextId,cash=model.State.money;
            var saved=JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(model.State));CompactSaveStore.Validate(saved,rules);
            var resumed=new ScrappingModel(rules,saved);var resumedConstruction=new ConstructionModel(saved,rules);
            resumed.HasPower=id=>resumedConstruction.PowerFor(id).powered;var restored=resumed.FindEquipment(primary).industry.primary;
            Assert.AreEqual(ownedObject,restored.id);Assert.AreEqual(original.duration,restored.duration);Assert.AreEqual(original.remaining,restored.remaining);
            Assert.AreEqual(3,restored.yields.Length);Assert.IsTrue(restored.xpEligible);Assert.AreEqual(model.OccupiedSlots,resumed.OccupiedSlots);
            Assert.AreEqual(next,saved.nextId);Assert.AreEqual(cash,saved.money);Assert.IsNull(resumed.FindScrap(ownedObject));
            resumed.Industry.Tick(restored.remaining);Assert.IsNull(resumed.FindEquipment(primary).industry.primary);
            Assert.AreEqual(6,resumed.StoredUnits(primary));Assert.AreEqual(1,resumed.FindEquipment(primary).industry.objectsProcessed);
            resumed.Industry.Tick(100);Assert.AreEqual(6,resumed.StoredUnits(primary));Assert.AreEqual(1,resumed.FindEquipment(primary).industry.objectsProcessed);
        }
        [Test]
        public void ActualJsonRetainsExportTimerFilterTransitLineageAndCreditsOnce()
        {
            var rules=new CompactRules{startingCars=0,startingRefrigerators=0};var model=new ScrappingModel(rules);var state=model.State;
            state.money=2000;state.experience=rules.levelThresholds[11];var construction=new ConstructionModel(state,rules);
            Assert.IsTrue(construction.Place(EquipmentKind.Storage,0,0,0));int storage=construction.LastPlacedId;
            Assert.IsTrue(construction.Place(EquipmentKind.ExportStation,0,6,0));int export=construction.LastPlacedId;
            Assert.IsTrue(construction.Place(EquipmentKind.Generator,5,6,0));Assert.IsTrue(construction.Connect(construction.LastPlacedId,export));
            model.HasPower=id=>construction.PowerFor(id).powered;var automation=new AutomationModel(model,construction);
            Assert.IsTrue(automation.Connect(storage,0,export,0,true),automation.LastMessage);
            model.FindEquipment(storage).contents.Add(new CompactStack{id=state.nextId++,kind=PartKind.Copper,quantity=2,xpEligible=false});
            model.FindEquipment(storage).contents.Add(new CompactStack{id=state.nextId++,kind=PartKind.Copper,quantity=1,xpEligible=true});
            model.FindEquipment(export).contents.Add(new CompactStack{id=state.nextId++,kind=PartKind.Copper,quantity=1,xpEligible=false});
            automation.Tick(.1f);automation.Tick(.25f);
            Assert.IsTrue(model.SetFilter(export,(int)PartKind.Copper));Assert.IsTrue(model.Industry.SetEnabled(export,true));model.Industry.Tick(1.25f);
            var saved=JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(state));CompactSaveStore.Validate(saved,rules);
            Assert.AreEqual(state.belts[0].items[0].id,saved.belts[0].items[0].id);Assert.AreEqual(state.belts[0].items[0].progress,saved.belts[0].items[0].progress);
            Assert.AreEqual(3.75f,model.FindEquipment(export).industry.remaining);
            Assert.AreEqual(model.FindEquipment(export).industry.remaining,saved.equipment[2].industry.remaining);Assert.AreEqual((int)PartKind.Copper,saved.equipment[2].filterKind);
            var resumed=new ScrappingModel(rules,saved);var resumedConstruction=new ConstructionModel(saved,rules);
            resumed.HasPower=id=>resumedConstruction.PowerFor(id).powered;var resumedAutomation=new AutomationModel(resumed,resumedConstruction);int xp=saved.experience;
            for(int i=0;i<600;i++){resumed.Tick(.1f);resumed.Industry.Tick(.1f);resumedAutomation.Tick(.1f);}
            Assert.AreEqual(4,resumed.FindEquipment(export).industry.exportedUnits);Assert.AreEqual(2,resumed.FindEquipment(export).industry.exportedXp);
            Assert.AreEqual(xp+2,saved.experience);Assert.AreEqual(0,resumed.OccupiedSlots);
            int cash=saved.money;Assert.IsFalse(resumed.Industry.DispatchNow(export));Assert.AreEqual(cash,saved.money);
        }
    }
}
