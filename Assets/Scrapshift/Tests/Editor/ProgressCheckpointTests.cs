using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class ProgressCheckpointTests
    {
        [TestCaseSource(typeof(ProgressCheckpointScenarios),"Names")]
        public void SharedRegression(string name){ProgressCheckpointScenarios.Run(name);}
        [Test]
        public void ActualJsonCheckpointRetainsLatestPartialDismantling()
        {
            var model=new ScrappingModel(new CompactRules());var checkpoint=new ProgressCheckpoint();int car=model.State.scrap[0].id;
            Assert.IsTrue(model.InspectScrap(car));
            for(int i=0;i<4;i++){Assert.IsTrue(model.WorkScrap(car));checkpoint.Queue(10+i*.2f);}
            Assert.IsFalse(checkpoint.Due(10.99f));Assert.IsTrue(checkpoint.Due(11));
            string persisted=JsonUtility.ToJson(model.State);checkpoint.RecordAttempt(true);
            var restored=new ScrappingModel(model.Rules,JsonUtility.FromJson<CompactYardState>(persisted));
            Assert.AreEqual(4,restored.FindScrap(car).strokes);Assert.AreEqual(6,restored.FindScrap(car).requiredStrokes);
            Assert.AreEqual(3,restored.FindScrap(car).remaining.Length);Assert.AreEqual(model.State.nextId,restored.State.nextId);
            Assert.AreEqual(model.State.money,restored.State.money);Assert.AreEqual(0,restored.State.experience);
            Assert.IsFalse(checkpoint.Pending);Assert.IsFalse(checkpoint.Due(100));
        }
    }
}
