using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Scrapshift.Compact;
using UnityEngine;

namespace Scrapshift.Tests
{
    // These call the real review/confirmation guards, outside IMGUI. Native layout remains a playtest check.
    public sealed class CompactIndustryMenuTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject root;
        CompactYardGame game;
        ScrappingModel model;
        EquipmentState primary,export;
        SettingsMenu settings;
        float previousScale;
        CursorLockMode previousCursor;
        bool previousVisible;
        object Page(string name){return Enum.Parse(typeof(CompactYardGame).GetNestedType("Page",BindingFlags.NonPublic),name);}
        object Review(string name){return Enum.Parse(typeof(CompactYardGame).GetNestedType("IndustryReviewKind",BindingFlags.NonPublic),name);}
        void Set(string name,object value){typeof(CompactYardGame).GetField(name,Private).SetValue(game,value);}
        object Get(string name){return typeof(CompactYardGame).GetField(name,Private).GetValue(game);}
        object Call(string name,params object[] args){return typeof(CompactYardGame).GetMethod(name,Private).Invoke(game,args);}
        string Snapshot(){return JsonUtility.ToJson(model.State);}
        void Inspect(EquipmentState equipment)
        {Set("page",Page("None"));Call("Show",Page("Equipment"),equipment.id);}
        bool Begin(string kind,EquipmentState equipment,int sourceId=0)
        {return (bool)Call("BeginIndustryReview",Review(kind),equipment.id,sourceId);}
        EquipmentState Equipment(EquipmentKind kind,float x,float z)
        {
            var item=new EquipmentState{id=model.State.nextId++,kind=kind,x=x,z=z,paidPrice=model.Rules.Equipment(kind).price};
            model.State.equipment.Add(item);return item;
        }
        void BufferedCopper()
        {
            export.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Copper,quantity=3,xpEligible=true,x=export.x,z=export.z});
            export.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Copper,quantity=4,xpEligible=false,x=export.x,z=export.z});
        }
        [SetUp] public void Setup()
        {
            previousScale=Time.timeScale;previousCursor=Cursor.lockState;previousVisible=Cursor.visible;
            root=new GameObject("Industry menu safety check");game=root.AddComponent<CompactYardGame>();
            model=new ScrappingModel(new CompactRules());model.HasPower=id=>true;
            typeof(CompactYardGame).GetProperty("Model").SetValue(game,model,null);
            Set("saveBlocked",true);Set("sessionStarted",true);Set("page",Page("None"));
            var input=new PlayerInputSettings(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"),"controls.json"));
            settings=new SettingsMenu(input);Set("controls",input);Set("settings",settings);
            primary=Equipment(EquipmentKind.PrimaryScrapper,0,0);export=Equipment(EquipmentKind.ExportStation,8,4);
        }
        [TearDown] public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(root);
            Time.timeScale=previousScale;Cursor.lockState=previousCursor;Cursor.visible=previousVisible;
        }
        [Test] public void OwnedIntakeReviewAndCancelDoNotBuyMoveOrConsumeTheObject()
        {
            Inspect(primary);int source=model.State.scrap[0].id;string before=Snapshot();
            Assert.IsTrue(Begin("Feed",primary,source));Assert.AreEqual(before,Snapshot());
            Call("ResetIndustryReview");Assert.IsFalse((bool)Call("ConfirmIndustryReview"));
            Assert.AreEqual(before,Snapshot());Assert.NotNull(model.FindScrap(source));
        }
        [Test] public void StandingDeliveryReviewAndCancelLeavePurchasesOff()
        {
            Inspect(primary);string before=Snapshot();
            Assert.IsTrue(Begin("StandingDelivery",primary));Assert.AreEqual(before,Snapshot());
            Call("ResetIndustryReview");Assert.IsFalse((bool)Call("ConfirmIndustryReview"));Assert.AreEqual(before,Snapshot());
            Assert.IsTrue(primary.industry==null||!primary.industry.enabled);
        }
        [Test] public void DispatchReviewAndCancelRetainStockAndRecoveredEligibility()
        {
            BufferedCopper();Inspect(export);string before=Snapshot();
            Assert.IsTrue(Begin("Dispatch",export));Assert.AreEqual(before,Snapshot());
            Assert.AreEqual(3,(int)Get("industryReviewQuantity"));
            Call("ResetIndustryReview");Assert.IsFalse((bool)Call("ConfirmIndustryReview"));Assert.AreEqual(before,Snapshot());
            Assert.IsTrue(export.contents[0].xpEligible);Assert.IsFalse(export.contents[1].xpEligible);
        }
        [Test] public void AutomaticExportReviewAndCancelCannotEnableSales()
        {
            Inspect(export);string before=Snapshot();
            Assert.IsTrue(Begin("AutomaticExport",export));Assert.AreEqual(before,Snapshot());
            Call("ResetIndustryReview");Assert.AreEqual(before,Snapshot());Assert.IsTrue(export.industry==null||!export.industry.enabled);
        }
        [Test] public void PauseAndOtherPagesCannotConfirmIndustryActions()
        {
            BufferedCopper();string before=Snapshot();
            foreach(string page in new[]{"None","Pause","Journal","Contracts","Catalogue","Sales","Credits"})
            {
                Set("page",Page(page));Set("paused",true);Set("selectedId",export.id);
                Assert.IsFalse(Begin("Dispatch",export));Assert.IsFalse((bool)Call("ConfirmIndustryReview"));
                Assert.AreEqual(before,Snapshot());
            }
        }
        [Test] public void ChangingSelectedMachineInvalidatesThePhysicalContext()
        {
            BufferedCopper();Inspect(export);Assert.IsTrue(Begin("Dispatch",export));
            string before=Snapshot();Set("selectedId",primary.id);
            Assert.IsFalse((bool)Call("ConfirmIndustryReview"));Assert.AreEqual(before,Snapshot());
        }
        [Test] public void InspectionAfterOwnedIntakePreviewRejectsTheStaleConfirmation()
        {
            Inspect(primary);int source=model.State.scrap[0].id;Assert.IsTrue(Begin("Feed",primary,source));
            Assert.IsTrue(model.InspectScrap(source));string before=Snapshot();
            Assert.IsFalse((bool)Call("ConfirmIndustryReview"));Assert.AreEqual(before,Snapshot());
            Assert.NotNull(model.FindScrap(source));Assert.IsTrue(primary.industry==null||primary.industry.primary==null);
        }
        [Test] public void ChangedDispatchQuantityRequiresANewQuoteWithoutAwardingMoneyOrXp()
        {
            BufferedCopper();Inspect(export);Assert.IsTrue(Begin("Dispatch",export));
            export.contents[0].quantity++;string before=Snapshot();
            Assert.IsFalse((bool)Call("ConfirmIndustryReview"));Assert.AreEqual(before,Snapshot());
        }
        [TestCase("price")][TestCase("interval")]
        public void ChangedStandingDeliveryTermsKeepPurchasesOff(string change)
        {
            Inspect(primary);Assert.IsTrue(Begin("StandingDelivery",primary));
            if(change=="price")model.Rules.LargeRecipe(ScrapObjectKind.Car).purchasePrice++;
            else model.Rules.deliveryIntervalSeconds++;
            string before=Snapshot();Assert.IsFalse((bool)Call("ConfirmIndustryReview"));Assert.AreEqual(before,Snapshot());
            Assert.IsTrue(primary.industry==null||!primary.industry.enabled);
        }
        [Test] public void OpeningSettingsRejectsAPendingDispatchWithoutSelling()
        {
            BufferedCopper();Inspect(export);Assert.IsTrue(Begin("Dispatch",export));string before=Snapshot();
            settings.Open();Assert.IsFalse((bool)Call("ConfirmIndustryReview"));Assert.AreEqual(before,Snapshot());
        }
        [Test] public void FocusLossCancelsPhysicalDispatchReview()
        {
            BufferedCopper();Inspect(export);Assert.IsTrue(Begin("Dispatch",export));string before=Snapshot();
            Call("OnApplicationFocus",false);Assert.AreEqual("Pause",Get("page").ToString());
            Assert.AreEqual("None",Get("industryReviewKind").ToString());
            Assert.IsFalse((bool)Call("ConfirmIndustryReview"));Assert.AreEqual(before,Snapshot());
        }
        [Test] public void BackCancelsStandingDeliveryReviewAndResumesWithoutAPurchase()
        {
            Inspect(primary);Assert.IsTrue(Begin("StandingDelivery",primary));string before=Snapshot();
            Call("Back");Assert.IsFalse(game.IsPaused);Assert.AreEqual("None",Get("industryReviewKind").ToString());
            Assert.IsFalse((bool)Call("ConfirmIndustryReview"));Assert.AreEqual(before,Snapshot());
        }
    }
}
