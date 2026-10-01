using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class BusinessTests
    {
        [TestCaseSource(typeof(RestorationOrderScenarios),nameof(RestorationOrderScenarios.Names))]
        public void RestorationRequests(string scenario){RestorationOrderScenarios.Run(scenario);}
        [TestCaseSource(typeof(DayReportScenarios),nameof(DayReportScenarios.Names))]
        public void DayReceipts(string scenario){DayReportScenarios.Run(scenario);}
        [Test]
        public void BothPartialRequestsAndDayReceiptSurviveActualJson()
        {
            string directory=Path.Combine(Path.GetTempPath(),"scrapshift-requests-"+Guid.NewGuid());
            string path=Path.Combine(directory,"yard.json");
            try
            {
                var m=new YardModel(new YardRules());m.State.money=100;m.State.orderIndex=1;m.State.commissionIndex=2;m.AcceptOrder();m.AcceptCommission();
                BusinessScenarioTools.Copper(m);m.DeliverOrder();BusinessScenarioTools.Repair(m,false);m.DeliverCommission();m.AdvanceDay();
                SaveStore.Write(path,m.State);
                var resumed=new YardModel(new YardRules{commissionBonusPerItem=21,fanSalePrice=57},SaveStore.Read(path,out _));
                Assert.AreEqual(JsonUtility.ToJson(m.State),JsonUtility.ToJson(resumed.State));
                Assert.AreEqual(104,resumed.CommissionReward);Assert.AreEqual(1,resumed.State.commissionDelivered);Assert.AreEqual(3,resumed.State.orderDelivered);
                Assert.AreEqual(8,resumed.LastDay.partsSpent);Assert.AreEqual(1,resumed.LastDay.appliancesDelivered);Assert.AreEqual(0,resumed.LastDay.restorationOrders);
                BusinessScenarioTools.Repair(resumed,false);Assert.IsTrue(resumed.DeliverCommission());Assert.IsFalse(resumed.DeliverCommission());
                Assert.AreEqual(192,resumed.State.money);Assert.AreEqual(1,resumed.Today.restorationOrders);
                BusinessScenarioTools.Copper(resumed);Assert.IsTrue(resumed.DeliverOrder());Assert.AreEqual(226,resumed.State.money);
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test]
        public void FirstSaveHasFullLedgerWhileOldJsonKeepsUnknownEarlierDetails()
        {
            string directory=Path.Combine(Path.GetTempPath(),"scrapshift-legacy-receipt-"+Guid.NewGuid());
            string path=Path.Combine(directory,"yard.json");
            try
            {
                var fresh=new YardModel(new YardRules(),SaveStore.Read(path,out _));Assert.IsTrue(fresh.Today.detailsComplete);
                Directory.CreateDirectory(directory);
                File.WriteAllText(path,"{\"version\":1,\"money\":29,\"nextId\":1,\"items\":[],\"dayIndex\":8,\"incomeToday\":47}");
                var old=new YardModel(new YardRules(),SaveStore.Read(path,out _));
                Assert.IsFalse(old.State.commissionAccepted);Assert.IsFalse(old.Today.detailsComplete);
                Assert.AreEqual(47,old.Today.income);Assert.AreEqual(29,old.Today.closingCash);old.AdvanceDay();SaveStore.Write(path,old.State);
                var resumed=new YardModel(old.Rules,SaveStore.Read(path,out _));
                Assert.IsFalse(resumed.LastDay.detailsComplete);Assert.IsTrue(resumed.Today.detailsComplete);Assert.AreEqual(29,resumed.Today.openingCash);
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test]
        public void InvalidNewRequestMetadataRecoversBackupWithoutConsumingIt()
        {
            string directory=Path.Combine(Path.GetTempPath(),"scrapshift-request-recovery-"+Guid.NewGuid());
            string path=Path.Combine(directory,"yard.json");
            try
            {
                var m=new YardModel(new YardRules());m.AcceptCommission();SaveStore.Write(path,m.State);SaveStore.Write(path,m.State);
                string backup=File.ReadAllText(path+".bak");m.State.commissionReward=0;string invalid=JsonUtility.ToJson(m.State);
                File.WriteAllText(path,invalid);var state=SaveStore.Read(path,out string notice);
                Assert.IsTrue(notice.Contains("backup"));Assert.IsTrue(state.commissionAccepted);Assert.AreEqual(52,state.commissionReward);
                Assert.AreEqual(invalid,File.ReadAllText(path));Assert.AreEqual(backup,File.ReadAllText(path+".bak"));
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test]
        public void BoardShowsBothRequestsAndRaycastsAtEitherNote()
        {
            var root=new GameObject("Neighbourhood request board test");
            try
            {
                var visual=YardBusinessVisual.Build(root.transform);var m=new YardModel(new YardRules());m.AcceptCommission();visual.Refresh(m);
                Assert.IsNotEmpty(visual.orderText.text);Assert.IsTrue(visual.commissionText.text.Contains("€52"));Assert.IsTrue(visual.commissionText.text.Contains("DELIVER TESTED ITEMS"));
                Assert.AreEqual(3,root.GetComponentsInChildren<InteractionTarget>().Length);
                Physics.SyncTransforms();
                foreach(float x in new[]{-2f,-1.15f,1.15f,2f})
                {
                    Vector3 face=YardBusinessVisual.OrderPosition+new Vector3(x,1.9f,-.2f);
                    Assert.IsTrue(Physics.Raycast(face+Vector3.back*2,Vector3.forward,out RaycastHit hit,3.2f));
                    Assert.AreEqual(TargetKind.OrderBoard,hit.collider.GetComponentInParent<InteractionTarget>().kind);
                }
                var dropped=YardItemVisual.Create(MaterialKind.Copper,root.transform);
                dropped.transform.position=YardBusinessVisual.OrderPosition+new Vector3(2,.15f,0);
                dropped.AddComponent<InteractionTarget>().kind=TargetKind.LooseItem;Physics.SyncTransforms();
                Assert.IsTrue(Physics.Raycast(dropped.transform.position+Vector3.back*2,Vector3.forward,out RaycastHit loose,3.2f));
                Assert.AreEqual(TargetKind.LooseItem,loose.collider.GetComponentInParent<InteractionTarget>().kind,"Old dropped copper outside the original footprint stays reachable beneath the wider note");
                m.State.money=100;BusinessScenarioTools.Repair(m,false);m.DeliverCommission();visual.Refresh(m);
                Assert.IsTrue(visual.commissionText.text.Contains("Rowan"));Assert.IsTrue(visual.commissionText.text.Contains("€44"));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void ReturningFromStoredReceiptKeepsPauseAndOpeningNewDayResumes()
        {
            var root=new GameObject("Receipt pause test");float priorTime=Time.timeScale;
            var priorLock=Cursor.lockState;bool priorVisible=Cursor.visible;
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            try
            {
                var game=root.AddComponent<PrototypeGame>();var m=new YardModel(new YardRules());m.AdvanceDay();
                typeof(PrototypeGame).GetProperty("Model").SetValue(game,m);
                Action<string,bool> set=(name,value)=>typeof(PrototypeGame).GetField(name,flags).SetValue(game,value);
                Func<string,bool> get=name=>(bool)typeof(PrototypeGame).GetField(name,flags).GetValue(game);
                var pause=typeof(PrototypeGame).GetMethod("SetPaused",flags);var close=typeof(PrototypeGame).GetMethod("CloseDayReportMenu",flags);
                pause.Invoke(game,new object[]{true});set("dayReportOpen",true);set("reportStartsNewDay",false);close.Invoke(game,null);
                Assert.IsTrue(get("paused"));Assert.IsTrue(get("dayOpen"));Assert.IsFalse(get("dayReportOpen"));Assert.AreEqual(0,Time.timeScale);
                set("dayOpen",false);set("dayReportOpen",true);set("reportStartsNewDay",true);close.Invoke(game,null);
                Assert.IsFalse(get("paused"));Assert.IsFalse(get("dayOpen"));Assert.IsFalse(get("dayReportOpen"));Assert.AreEqual(1,Time.timeScale);
                Assert.AreEqual(1,m.State.dayIndex,"Review/resume never advances or rewards another day");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);Time.timeScale=priorTime;Cursor.lockState=priorLock;Cursor.visible=priorVisible;}
        }
    }
}
