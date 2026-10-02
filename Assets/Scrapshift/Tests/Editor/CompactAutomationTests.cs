using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactAutomationTests
    {
        [TestCaseSource(typeof(CompactAutomationScenarios),nameof(CompactAutomationScenarios.Names))]
        public void AutomationRules(string name){CompactAutomationScenarios.Run(name);}
        [Test]public void InFlightUnitsAndCooldownResumeThroughUnityJsonWithoutDuplicateOutput()
        {
            var rules=new CompactRules();var m=new ScrappingModel(rules);m.State.money=10000;m.State.experience=rules.levelThresholds[9];
            var c=new ConstructionModel(m.State,rules);var a=new AutomationModel(m,c);
            Assert.IsTrue(c.Place(EquipmentKind.Storage,0,0,0));int from=c.LastPlacedId;
            Assert.IsTrue(c.Place(EquipmentKind.Storage,0,6,0));int to=c.LastPlacedId;
            Assert.IsTrue(a.Connect(from,0,to,0,true));m.FindEquipment(from).contents.Add(new CompactStack{id=m.State.nextId++,kind=PartKind.Copper,quantity=2,xpEligible=true});
            a.Tick(.1f);a.Tick(.25f);var state=JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(m.State));
            ScrappingModel.Validate(state,rules);ConstructionModel.Validate(state,rules);AutomationModel.Validate(state,rules);
            var resumed=new ScrappingModel(rules,state);var again=new AutomationModel(resumed,new ConstructionModel(state,rules));
            for(int i=0;i<150;i++)again.Tick(.1f);
            Assert.AreEqual(2,resumed.StoredUnits(to));Assert.AreEqual(0,resumed.StoredUnits(from));Assert.AreEqual(0,state.belts[0].items.Count);
        }
    }
}
