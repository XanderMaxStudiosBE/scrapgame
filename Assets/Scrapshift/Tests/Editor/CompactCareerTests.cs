using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactCareerTests
    {
        [TestCaseSource(typeof(CompactCareerScenarios),nameof(CompactCareerScenarios.Names))]
        public void CareerAndCustomerTransactions(string name){CompactCareerScenarios.Run(name);}
        [Test]
        public void ActualJsonPreservesPartialRequestAndSalesLedger()
        {
            var rules=new CompactRules();var model=new ScrappingModel(rules);
            var stack=new CompactStack{id=model.State.nextId++,kind=PartKind.Copper,quantity=2,xpEligible=true};model.State.items.Add(stack);model.State.carriedId=stack.id;
            Assert.IsTrue(model.Career.DeliverContract());
            var resumed=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(model.State)));
            Assert.AreEqual(2,resumed.Career.CurrentContract.delivered);Assert.AreEqual(6,resumed.Career.Stats.salesRevenue);
            Assert.AreEqual(4,resumed.State.experience);Assert.AreEqual(0,resumed.Career.Stats.contractsCompleted);
            var final=new CompactStack{id=resumed.State.nextId++,kind=PartKind.Copper,quantity=5,xpEligible=true};resumed.State.items.Add(final);resumed.State.carriedId=final.id;
            Assert.IsTrue(resumed.Career.DeliverContract());var second=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(resumed.State)));
            Assert.AreEqual(1,second.Career.Stats.contractsCompleted);Assert.AreEqual(6,second.Career.Stats.contractBonuses);
            Assert.AreEqual(1,second.Carried.quantity);Assert.IsFalse(second.Career.DeliverContract());
        }
        [Test]
        public void ActualOlderJsonAddsNoHistoricalCareerTotals()
        {
            var rules=new CompactRules();var original=new ScrappingModel(rules).State;original.experience=30;
            string json=JsonUtility.ToJson(original).Replace(",\"career\":"+JsonUtility.ToJson(original.career),"");
            Assert.IsFalse(json.Contains("\"career\""));
            var resumed=new ScrappingModel(rules,JsonUtility.FromJson<CompactYardState>(json));
            Assert.AreEqual(8,resumed.State.money);Assert.AreEqual(30,resumed.State.experience);
            Assert.AreEqual(0,resumed.Career.Stats.salesRevenue);Assert.AreEqual(0,resumed.Career.Stats.saleTransactions);
            Assert.AreEqual(0,resumed.Career.Stats.contractsCompleted);Assert.AreEqual(0,resumed.Career.CurrentContract.delivered);
        }
        [Test]
        public void TrackedOlderBalanceReceivesEditableRequestDefaults()
        {
            var balance=Resources.Load<CompactBalance>("ScrapshiftCompact/Balance");Assert.IsNotNull(balance);
            var rules=balance.PreparedRules;Assert.AreEqual(6,rules.contracts.Length);Assert.AreEqual(20,rules.contracts[5].bonus);
            Assert.AreEqual(10,rules.contracts[5].minimumLevel);
        }
    }
}
