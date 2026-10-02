using System;
using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactConstructionTests
    {
        [TestCaseSource(typeof(CompactConstructionScenarios),nameof(CompactConstructionScenarios.Names))]
        public void ConstructionRules(string name) { CompactConstructionScenarios.Run(name); }

        [Test] public void PreviewCancelIsFreeAndConfirmationRevalidatesCurrentCash()
        {
            var state=new CompactYardState{money=1000};var rules=new CompactRules();
            var c=new ConstructionModel(state,rules);var build=new CompactBuildMode(c);
            build.Begin(EquipmentKind.Generator);build.UpdatePreview(new Vector3(3.22f,4,1.18f));
            Assert.AreEqual(new Vector3(3,0,1),build.PreviewPosition);Assert.IsTrue(build.Valid);
            build.Rotate();Assert.AreEqual(90,build.Yaw);build.Cancel();
            Assert.AreEqual(1000,state.money);Assert.AreEqual(0,state.equipment.Count);Assert.IsFalse(build.Active);
            build.Begin(EquipmentKind.Generator);build.UpdatePreview(new Vector3(3,0,1));Assert.IsTrue(build.Valid);
            state.money=0;Assert.IsFalse(build.Confirm());Assert.AreEqual(0,state.equipment.Count);Assert.IsTrue(build.Active);
            state.money=1000;Assert.IsTrue(build.Confirm());Assert.IsFalse(build.Active);Assert.AreEqual(1,state.equipment.Count);
        }

        [Test] public void LegacySevenActionJsonMigratesWithoutResettingGameplayOrLook()
        {
            string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"scrapshift-build-controls-"+Guid.NewGuid());
            string path=System.IO.Path.Combine(directory,"controls-v1.json");
            try
            {
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllText(path,"{\"version\":1,\"bindings\":[\"W\",\"S\",\"A\",\"D\",\"B\",\"R\",\"Mouse1\"],\"sensitivity\":3.5,\"invertY\":true}");
                string notice;var p=ControlSettingsStore.Read(path,out notice);
                Assert.AreEqual("B",p.Binding(ControlAction.Interact));Assert.AreEqual("R",p.Binding(ControlAction.Drop));
                Assert.AreEqual("Mouse1",p.Binding(ControlAction.ManualWork));Assert.AreEqual(3.5f,p.sensitivity);Assert.IsTrue(p.invertY);
                Assert.AreEqual("F2",p.Binding(ControlAction.BuildToggle));Assert.AreEqual("F3",p.Binding(ControlAction.BuildRotate));
                StringAssert.Contains("preserved",notice);ControlSettingsStore.Write(path,p);
                var again=ControlSettingsStore.Read(path,out notice);Assert.AreEqual(JsonUtility.ToJson(p),JsonUtility.ToJson(again));
            }
            finally{if(System.IO.Directory.Exists(directory))System.IO.Directory.Delete(directory,true);}
        }

        [Test] public void ConstructionConnectionsAndPlacementsRoundTripInUnityJson()
        {
            var state=new CompactYardState{money=1000};var rules=new CompactRules();var c=new ConstructionModel(state,rules);
            Assert.IsTrue(c.Place(EquipmentKind.Generator,-3,4,90));int generator=c.LastPlacedId;
            Assert.IsTrue(c.Place(EquipmentKind.Tier1Scrapper,3,4,180));Assert.IsTrue(c.Connect(generator,c.LastPlacedId));
            var restored=JsonUtility.FromJson<CompactYardState>(JsonUtility.ToJson(state));ConstructionModel.Validate(restored,rules);
            var resumed=new ConstructionModel(restored,rules);Assert.IsTrue(resumed.PowerFor(c.LastPlacedId).powered);
            Assert.AreEqual(90,restored.equipment[0].yaw);Assert.AreEqual(state.money,restored.money);Assert.AreEqual(1,restored.powerLinks.Count);
        }
    }
}
