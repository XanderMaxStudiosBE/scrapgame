using System.IO;
using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;
namespace Scrapshift.Tests
{
    public sealed class CompactStageCIntegrationTests
    {
        [TestCaseSource(typeof(CompactStageCIntegrationScenarios),"Names")]
        public void SharedRegression(string name){CompactStageCIntegrationScenarios.Run(name);}
        [Test]
        public void ActualJsonVersionTwoReadRetainsOriginalFileAndJob()
        {
            var rules=new CompactRules();var model=new ScrappingModel(rules);model.AcquireWire();model.BeginProcessing(model.State.equipment[0].id);model.Work(model.State.equipment[0].id);
            var s=model.State;s.version=2;s.belts=new System.Collections.Generic.List<ConveyorLink>();
            string directory=Path.Combine(Path.GetTempPath(),"scrapshift-v2-migration-"+System.Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"yard-v2.json");string original=JsonUtility.ToJson(s).Replace(",\"belts\":[]","");
            try
            {
                File.WriteAllText(path,original);var current=CompactSaveStore.Read(path,rules,out string notice);
                Assert.AreEqual(3,current.version);Assert.AreEqual(1,current.equipment[0].job.strokes);Assert.AreEqual(-1,current.equipment[0].filterKind);
                Assert.AreEqual(original,File.ReadAllText(path));Assert.IsTrue(notice.Contains("upgraded"));
                CompactSaveStore.Write(path,current,rules);Assert.AreEqual(original,File.ReadAllText(path+".bak"));
                Assert.AreEqual(3,CompactSaveStore.Read(path,rules,out notice).version);
            }
            finally{Directory.Delete(directory,true);}
        }
        [Test]
        public void ActualTransportJsonPreservesIdentityLineageAndProgress()
        {
            var rules=new CompactRules();var model=new ScrappingModel(rules);var s=model.State;s.money=2000;s.experience=rules.levelThresholds[9];
            var construction=new ConstructionModel(s,rules);
            Assert.IsTrue(construction.Place(EquipmentKind.Storage,-4,1,0));int source=construction.LastPlacedId;
            Assert.IsTrue(construction.Place(EquipmentKind.Storage,-4,7,0));int destination=construction.LastPlacedId;
            var automation=new AutomationModel(model,construction);Assert.IsTrue(automation.Connect(source,0,destination,0,true),automation.LastMessage);
            var belt=s.belts[0];belt.items.Add(new ConveyorItem{id=s.nextId++,kind=PartKind.Wire,xpEligible=true,progress=.4f});
            var restored=JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(s));CompactSaveStore.Validate(restored,rules);
            Assert.AreEqual(belt.id,restored.belts[0].id);Assert.AreEqual(belt.items[0].id,restored.belts[0].items[0].id);
            Assert.AreEqual(.4f,restored.belts[0].items[0].progress);Assert.IsTrue(restored.belts[0].items[0].xpEligible);
        }
        [Test]
        public void CancellingPortSelectionSpendsNothingAndConsumesInput()
        {
            CompactIntegrationTests.WithCoordinator((game,input)=>
            {
                game.Model.State.money=2000;game.Model.State.experience=game.Model.Rules.levelThresholds[9];
                string before=JsonUtility.ToJson(game.Model.State);
                CompactIntegrationTests.Invoke(game,"BeginBuild",EquipmentKind.Conveyor,0);
                Assert.IsTrue(game.IsBuilding);Assert.IsFalse(game.IsPaused);Assert.AreEqual(before,JsonUtility.ToJson(game.Model.State));
                CompactIntegrationTests.Invoke(game,"CancelBuild");
                Assert.IsFalse(game.IsBuilding);Assert.IsFalse(input.GameplayReady);Assert.AreEqual(before,JsonUtility.ToJson(game.Model.State));
            });
        }
        [Test]
        public void ProcessingAndBufferChangesRetainMachineBodyAndCollider()
        {
            CompactIntegrationTests.WithCoordinator((game,input)=>
            {
                var equipment=game.Model.State.equipment[0];CompactIntegrationTests.Invoke(game,"SyncViews");
                var body=EquipmentTarget(game,equipment.id);var collider=body.GetComponentInChildren<Collider>();
                Assert.IsNotNull(collider);Assert.IsTrue(game.Model.AcquireWire());Assert.IsTrue(game.Model.BeginProcessing(equipment.id));
                CompactIntegrationTests.Invoke(game,"SyncViews");Assert.AreSame(body,EquipmentTarget(game,equipment.id));
                Assert.IsTrue(game.Model.Work(equipment.id));CompactIntegrationTests.Invoke(game,"SyncViews");
                Assert.AreSame(body,EquipmentTarget(game,equipment.id));Assert.AreSame(collider,body.GetComponentInChildren<Collider>());
                game.Model.State.money=2000;game.Model.State.experience=game.Model.Rules.levelThresholds[9];
                Assert.IsTrue(game.Construction.Place(EquipmentKind.Storage,-4,1,0));int storage=game.Construction.LastPlacedId;
                CompactIntegrationTests.Invoke(game,"SyncViews");var storageBody=EquipmentTarget(game,storage);
                Assert.IsTrue(game.Model.AcquireWire());Assert.IsTrue(game.Model.Deposit(storage));CompactIntegrationTests.Invoke(game,"SyncViews");
                Assert.AreSame(storageBody,EquipmentTarget(game,storage));
            });
        }
        static CompactInteractionTarget EquipmentTarget(CompactYardGame game,int id)
        {
            foreach(var target in game.GetComponentsInChildren<CompactInteractionTarget>())
                if(target.kind==CompactTargetKind.Equipment&&target.id==id)return target;
            Assert.Fail("Missing equipment view #"+id);return null;
        }
        [Test]
        public void SwitchingTransportKindsKeepsIdleVisualPoolBounded()
        {
            CompactIntegrationTests.WithCoordinator((game,input)=>
            {
                var s=game.Model.State;s.money=2000;s.experience=game.Model.Rules.levelThresholds[9];
                Assert.IsTrue(game.Construction.Place(EquipmentKind.Storage,-4,1,0));int source=game.Construction.LastPlacedId;
                Assert.IsTrue(game.Construction.Place(EquipmentKind.Storage,-4,7,0));int destination=game.Construction.LastPlacedId;
                Assert.IsTrue(game.Automation.Connect(source,0,destination,0,true));CompactIntegrationTests.Invoke(game,"SyncViews");
                var link=s.belts[0];
                foreach(PartKind kind in System.Enum.GetValues(typeof(PartKind)))
                {
                    link.items.Clear();for(int i=0;i<game.Model.Rules.beltCapacity;i++)link.items.Add(new ConveyorItem{id=s.nextId++,kind=kind,progress=(float)(game.Model.Rules.beltCapacity-i)/game.Model.Rules.beltCapacity});
                    CompactIntegrationTests.Invoke(game,"UpdateTransportViews");
                }
                link.items.Clear();CompactIntegrationTests.Invoke(game,"UpdateTransportViews");
                int idle=(int)typeof(CompactYardGame).GetField("spareTransitViews",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(game);
                Assert.Greater(idle,0);Assert.LessOrEqual(idle,32,"Idle views must not grow with every transported kind.");
            });
        }
    }
}
