using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactIntegrationTests
    {
        [TestCaseSource(typeof(CompactIntegrationScenarios),nameof(CompactIntegrationScenarios.Names))]
        public void CoreContracts(string name){CompactIntegrationScenarios.Run(name);}
        [Test]
        public void UnityJsonRoundTripRetainsPlacementsLinksCarryAndReservedOutputs()
        {
            var rules=new CompactRules();var model=new ScrappingModel(rules);model.State.money=200;
            var construction=new ConstructionModel(model.State,rules);
            Assert.IsTrue(construction.Place(EquipmentKind.Generator,4,2,90));int generator=construction.LastPlacedId;
            Assert.IsTrue(construction.Place(EquipmentKind.Tier1Scrapper,8,2,180));int machine=construction.LastPlacedId;
            Assert.IsTrue(construction.Connect(generator,machine));Assert.IsTrue(model.AcquireWire());Assert.IsTrue(model.BeginProcessing(machine));
            model.HasPower=id=>construction.PowerFor(id).powered;model.Tick(1);
            Assert.IsTrue(model.InspectScrap(model.State.scrap[0].id));Assert.IsTrue(model.WorkScrap(model.State.scrap[0].id));
            string json=JsonUtility.ToJson(model.State);CompactSaveHeader.Validate(json);
            var copy=JsonUtility.FromJson<CompactYardState>(json);CompactSaveStore.Validate(copy,rules);
            Assert.AreEqual(90,copy.equipment[1].yaw);Assert.AreEqual(1,copy.powerLinks.Count);
            Assert.AreEqual(model.FindEquipment(machine).job.remaining,copy.equipment[2].job.remaining);
            Assert.AreEqual(1,copy.scrap[0].strokes);Assert.AreEqual(6,copy.scrap[0].requiredStrokes);
        }
        [Test]
        public void ActualSaveRecoveryProtectsInvalidPrimaryAndArchivesWithoutTouchingLegacy()
        {
            string folder=Path.Combine(Path.GetTempPath(),"scrapshift-compact-"+System.Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,"yard-v2.json"),legacy=Path.Combine(folder,"yard-v1.json");
            try
            {
                var rules=new CompactRules();var state=new ScrappingModel(rules).State;File.WriteAllText(legacy,"retained legacy bytes");
                CompactSaveStore.Write(path,state,rules);state.money++;CompactSaveStore.Write(path,state,rules);
                File.WriteAllText(path,"{}");var backup=CompactSaveStore.Read(path,rules,out string notice);
                Assert.AreEqual(rules.startingMoney,backup.money);StringAssert.Contains("backup",notice);Assert.AreEqual("{}",File.ReadAllText(path));
                File.WriteAllText(path+".bak","{}");Assert.Throws<InvalidDataException>(()=>CompactSaveStore.Read(path,rules,out notice));
                Assert.AreEqual("{}",File.ReadAllText(path));CompactSaveStore.Archive(path);Assert.IsFalse(File.Exists(path));
                Assert.AreEqual("retained legacy bytes",File.ReadAllText(legacy));Assert.AreEqual(1,Directory.GetDirectories(folder).Length);
            }
            finally{Directory.Delete(folder,true);}
        }
        [Test]
        public void EveryGameplayMenuPausesTimeAndSuppressesActivationInput()
        {
            WithCoordinator((game,input)=>
            {
                Assert.IsTrue(game.Model.AcquireWire());Assert.IsTrue(game.Model.BeginProcessing(game.Model.State.equipment[0].id));
                string before=JsonUtility.ToJson(game.Model.State);
                var pageType=typeof(CompactYardGame).GetNestedType("Page",BindingFlags.NonPublic);
                foreach(string page in new[]{"Pause","Welcome","Help","Catalogue","Equipment","LargeScrap","Sales","Delivery","Import"})
                {
                    Invoke(game,"Show",System.Enum.Parse(pageType,page),0);
                    Assert.IsTrue(game.IsPaused,page);Assert.AreEqual(0,Time.timeScale,page);Assert.IsFalse(input.GameplayReady,page);
                    Assert.AreEqual(before,JsonUtility.ToJson(game.Model.State),page+" changed a job or inventory");
                }
            });
        }
        [Test]
        public void ActualBuildGhostAndCancellationSpendNothingAndConsumeInput()
        {
            WithCoordinator((game,input)=>
            {
                game.Model.State.money=200;
                Invoke(game,"BeginBuild",EquipmentKind.Generator,0);
                Assert.IsTrue(game.IsBuilding);Assert.IsFalse(game.IsPaused);Assert.AreEqual(200,game.Model.State.money);
                var ghost=(GameObject)typeof(CompactYardGame).GetField("ghost",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
                Assert.IsNotNull(ghost);Assert.IsEmpty(ghost.GetComponentsInChildren<Collider>());
                foreach(var t in ghost.GetComponentsInChildren<Transform>())Assert.AreEqual(2,t.gameObject.layer);
                Invoke(game,"CancelBuild");Assert.IsFalse(game.IsBuilding);Assert.AreEqual(200,game.Model.State.money);
                Assert.IsFalse(input.GameplayReady);Assert.AreEqual(1,game.Model.State.equipment.Count);
            });
        }
        [Test]
        public void FenceEdgePlayerCaptureStaysWithinVersionTwoSaveLimits()
        {
            WithCoordinator((game,input)=>
            {
                game.player.transform.position=new Vector3(23.65f,.9f,17.65f);Invoke(game,"CapturePlayer");
                Assert.AreEqual(23.5f,game.Model.State.playerX);Assert.AreEqual(17.5f,game.Model.State.playerZ);
                CompactSaveStore.Validate(game.Model.State,game.Model.Rules);
            });
        }
        internal static void Invoke(CompactYardGame game,string name,params object[] arguments)
        {typeof(CompactYardGame).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,arguments);}
        internal static void WithCoordinator(System.Action<CompactYardGame,PlayerInputSettings> check)
        {
            var root=new GameObject("Compact coordinator contract test");var balance=ScriptableObject.CreateInstance<CompactBalance>();
            float previousTime=Time.timeScale;var previousCursor=Cursor.lockState;bool previousVisible=Cursor.visible;
            try
            {
                var playerRoot=new GameObject("Player");playerRoot.transform.SetParent(root.transform,false);
                var player=playerRoot.AddComponent<FirstPersonController>();
                var cameraRoot=new GameObject("Camera");cameraRoot.transform.SetParent(playerRoot.transform,false);player.view=cameraRoot.AddComponent<Camera>();
                var game=root.AddComponent<CompactYardGame>();game.enabled=false;game.balance=balance;game.player=player;
                var controls=new PlayerInputSettings(Path.Combine(Path.GetTempPath(),"scrapshift-test-controls-"+System.Guid.NewGuid().ToString("N")+".json"));
                typeof(CompactYardGame).GetField("controls",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,controls);
                Invoke(game,"BindModel",new object[]{null});check(game,controls);
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(balance);Time.timeScale=previousTime;Cursor.lockState=previousCursor;Cursor.visible=previousVisible;}
        }
    }
}
