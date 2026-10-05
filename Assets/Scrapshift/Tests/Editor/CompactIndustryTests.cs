using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactIndustryTests
    {
        [TestCaseSource(typeof(CompactIndustryScenarios),nameof(CompactIndustryScenarios.Names))]
        public void IndustrialTransactionsAndTime(string name){CompactIndustryScenarios.Run(name);}
        [Test]
        public void ActualJsonPreservesPaidPrimaryProgressAndCommitsOutputOnce()
        {
            var rules=new CompactRules();var model=new ScrappingModel(rules);var primary=new EquipmentState{id=model.State.nextId++,kind=EquipmentKind.PrimaryScrapper,x=0,z=5,paidPrice=350};model.State.equipment.Add(primary);
            int source=model.State.scrap[0].id;Assert.IsTrue(model.Industry.FeedScrap(primary.id,source));model.HasPower=id=>true;model.Industry.Tick(7.25f);
            var resumed=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(model.State)));resumed.HasPower=id=>true;
            var restored=resumed.FindEquipment(primary.id);Assert.AreEqual(source,restored.industry.primary.id);Assert.AreEqual(16.75f,restored.industry.primary.remaining);
            resumed.Industry.Tick(16.75f);Assert.AreEqual(3,restored.contents.Count);Assert.IsNull(restored.industry.primary);
            var second=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(resumed.State)));second.HasPower=id=>true;
            Assert.IsFalse(second.Industry.Tick(100));Assert.AreEqual(1,second.FindEquipment(primary.id).industry.objectsProcessed);Assert.AreEqual(0,second.State.experience);
        }
        [Test]
        public void ActualJsonRetainsEnabledTimersExportLineageAndCarriedIdentity()
        {
            var rules=new CompactRules();var model=new ScrappingModel(rules);var exporter=new EquipmentState{id=model.State.nextId++,kind=EquipmentKind.ExportStation,x=10,z=5,paidPrice=200};model.State.equipment.Add(exporter);
            exporter.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Copper,quantity=3,x=10,z=5,xpEligible=false});Assert.IsTrue(model.AcquireWire());int carry=model.State.carriedId;
            model.HasPower=id=>true;model.Industry.SetEnabled(exporter.id,true);model.Industry.Tick(2.5f);
            var resumed=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(model.State)));resumed.HasPower=id=>true;
            Assert.AreEqual(2.5f,resumed.FindEquipment(exporter.id).industry.remaining);resumed.Industry.Tick(2.5f);
            Assert.AreEqual(carry,resumed.State.carriedId);Assert.AreEqual(0,resumed.State.experience);Assert.AreEqual(9,resumed.FindEquipment(exporter.id).industry.exportedRevenue);
            Assert.AreEqual(0,resumed.Career.Stats.contractBonuses);Assert.AreEqual(0,resumed.Career.CurrentContract.delivered);
        }
    }
}
