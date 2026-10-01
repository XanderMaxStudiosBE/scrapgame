using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class FanTests
    {
        [TestCaseSource(typeof(FanScenarios), nameof(FanScenarios.Names))]
        public void RepairSalvageAndDayTransactions(string scenario) { FanScenarios.Run(scenario); }
        [Test]
        public void PaidPartialRepairAndDailyStockSurviveJsonSave()
        {
            string directory=Path.Combine(Path.GetTempPath(),"scrapshift-fan-"+Guid.NewGuid());
            string path=Path.Combine(directory,"yard.json");
            try
            {
                var m=new YardModel(new YardRules());m.State.money=8;
                m.AcquireFan();m.LoadFan();m.InspectFan();m.BeginFanRepair();m.WorkFan();
                SaveStore.Write(path,m.State);
                var restored=new YardModel(m.Rules,SaveStore.Read(path,out _));
                Assert.AreEqual(JsonUtility.ToJson(m.State),JsonUtility.ToJson(restored.State));
                Assert.AreEqual(0,restored.State.money);Assert.AreEqual(1,restored.State.fanStrokes);
                Assert.AreEqual(1,restored.State.fansTakenToday);Assert.IsFalse(restored.BeginFanRepair());
                restored.WorkFan();restored.WorkFan();restored.TestFan();restored.CollectFan();
                Assert.AreEqual(MaterialKind.RestoredFan,restored.Carried.kind);
            }
            finally {if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test]
        public void FanHeadRayResolvesTheRestorationBenchAndPausingStopsItsRotor()
        {
            var root=new GameObject("Fan station test");
            try
            {
                var visual=FanWorkbenchVisual.Build(root.transform);
                var m=new YardModel(new YardRules());m.State.money=8;
                m.AcquireFan();m.LoadFan();m.InspectFan();m.BeginFanRepair();
                for(int i=0;i<3;i++)m.WorkFan();m.TestFan();visual.Refresh(m);
                Physics.SyncTransforms();
                Vector3 head=FanWorkbenchVisual.Position+new Vector3(0,1.75f,-.05f);
                Assert.IsTrue(Physics.Raycast(head+Vector3.back*2,Vector3.forward,out RaycastHit hit,3.2f));
                Assert.AreEqual(TargetKind.FanBench,hit.collider.GetComponentInParent<InteractionTarget>().kind);
                Assert.NotNull(visual.rotor,"Authored rotor should import");
                Quaternion before=visual.rotor.localRotation;visual.Step(m,0);
                Assert.AreEqual(before,visual.rotor.localRotation,"Zero simulation delta changes no animation");
                visual.Step(m,.2f);Assert.AreNotEqual(before,visual.rotor.localRotation);
            }
            finally {UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
