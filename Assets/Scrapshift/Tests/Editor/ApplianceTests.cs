using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class ApplianceTests
    {
        [TestCaseSource(typeof(ApplianceScenarios),nameof(ApplianceScenarios.Names))]
        public void ApplianceTransactions(string scenario){ApplianceScenarios.Run(scenario);}
        [Test]
        public void RadioFaultAndPaidPartialJobSurviveActualJson()
        {
            string directory=Path.Combine(Path.GetTempPath(),"scrapshift-radio-"+Guid.NewGuid());
            string path=Path.Combine(directory,"yard.json");
            try
            {
                var m=new YardModel(new YardRules(),new YardState{dayIndex=1,money=3});
                m.AcquireRadio();m.LoadFan();m.InspectFan();m.BeginFanRepair();m.WorkFan();m.AcquireFan();m.Drop(2,.02f,3);
                SaveStore.Write(path,m.State);
                var resumed=new YardModel(m.Rules,SaveStore.Read(path,out _));
                Assert.AreEqual(JsonUtility.ToJson(m.State),JsonUtility.ToJson(resumed.State));
                Assert.AreEqual(RepairAppliance.PortableRadio,resumed.State.benchAppliance);
                Assert.AreEqual(ApplianceFault.PowerLead,resumed.State.benchFault);
                Assert.AreEqual(1,resumed.State.radiosTakenToday);Assert.AreEqual(0,resumed.State.money);
                Assert.IsFalse(resumed.BeginFanRepair());Assert.IsTrue(resumed.WorkFan());Assert.IsTrue(resumed.TestFan());Assert.IsTrue(resumed.CollectFan());
                Assert.AreEqual(MaterialKind.RestoredRadio,resumed.Carried.kind);
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test]
        public void OldJsonDefaultsToOriginalFanRecipe()
        {
            var state=JsonUtility.FromJson<YardState>("{\"version\":1,\"nextId\":1,\"items\":[],\"fanStage\":3,\"fanStrokes\":1}");
            var m=new YardModel(new YardRules(),state);
            Assert.AreEqual("Seized motor",m.CurrentRepair.fault);Assert.AreEqual(3,m.FanRepairSteps);
            m.WorkFan();m.WorkFan();m.TestFan();m.CollectFan();Assert.AreEqual(MaterialKind.RestoredFan,m.Carried.kind);
        }
        [Test]
        public void RadioDisplayAndSupplyRouteToTheirStations()
        {
            var root=new GameObject("Radio station check");
            try
            {
                var visual=FanWorkbenchVisual.Build(root.transform);var m=new YardModel(new YardRules());
                m.AcquireRadio();m.LoadFan();visual.Refresh(m);
                Assert.IsTrue(visual.radioDisplay.gameObject.activeSelf);Assert.IsFalse(visual.display.gameObject.activeSelf);
                Assert.IsFalse(visual.radioStock.gameObject.activeSelf,"Today's only radio is taken");
                Assert.AreEqual(TargetKind.FanBench,visual.radioDisplay.GetComponentInParent<InteractionTarget>().kind);
                Assert.AreEqual(TargetKind.RadioSupply,visual.radioStock.GetComponentInParent<InteractionTarget>().kind);
                Physics.SyncTransforms();
                Vector3 face=FanWorkbenchVisual.Position+new Vector3(0,1.3f,-.05f);
                Assert.IsTrue(Physics.Raycast(face+Vector3.back*2,Vector3.forward,out RaycastHit hit,3.2f));
                Assert.AreEqual(TargetKind.FanBench,hit.collider.GetComponentInParent<InteractionTarget>().kind);
                m.State.money=6;m.InspectFan();m.BeginFanRepair();m.WorkFan();visual.Pulse();visual.Step(m,.08f);
                Assert.AreNotEqual(Quaternion.identity,visual.radioDisplay.localRotation,"Successful work feedback");
                while(m.State.fanStage==FanStage.Repairing)m.WorkFan();m.TestFan();m.CollectFan();visual.Refresh(m);
                Assert.AreEqual(Quaternion.identity,visual.radioDisplay.localRotation,"Collection clears an interrupted pulse");
                Assert.IsFalse(visual.radioDisplay.gameObject.activeSelf);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
