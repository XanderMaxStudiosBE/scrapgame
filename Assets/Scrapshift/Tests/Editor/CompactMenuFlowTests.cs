using System;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using Scrapshift.Compact;
using UnityEngine;

namespace Scrapshift.Tests
{
    public sealed class CompactMenuFlowTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject root;
        CompactYardGame game;
        ScrappingModel model;
        float previousScale;
        CursorLockMode previousCursor;
        bool previousVisible;
        object Page(string name){return Enum.Parse(typeof(CompactYardGame).GetNestedType("Page",BindingFlags.NonPublic),name);}
        void Set(string name,object value){typeof(CompactYardGame).GetField(name,Private).SetValue(game,value);}
        object Get(string name){return typeof(CompactYardGame).GetField(name,Private).GetValue(game);}
        object Call(string name,params object[] args){return typeof(CompactYardGame).GetMethod(name,Private).Invoke(game,args);}
        bool AtCounter(){return (bool)typeof(CompactYardGame).GetProperty("CustomerCounterContext",Private).GetValue(game,null);}
        [SetUp] public void Setup()
        {
            previousScale=Time.timeScale;previousCursor=Cursor.lockState;previousVisible=Cursor.visible;
            root=new GameObject("Compact menu flow check");game=root.AddComponent<CompactYardGame>();
            model=new ScrappingModel(new CompactRules());
            typeof(CompactYardGame).GetProperty("Model").SetValue(game,model,null);
            Set("saveBlocked",true);Set("sessionStarted",true);Set("page",Page("None"));
            var controls=new PlayerInputSettings(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"),"controls.json"));
            Set("controls",controls);Set("settings",new SettingsMenu(controls));
        }
        [TearDown] public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(root);
            Time.timeScale=previousScale;Cursor.lockState=previousCursor;Cursor.visible=previousVisible;
        }
        [Test] public void NestedMenusKeepEquipmentSelectionAndPauseUntilLastBack()
        {
            Call("Show",Page("Equipment"),17);Set("menuScroll",new Vector2(3,42));
            Call("Show",Page("Contracts"),0);
            Assert.IsTrue(game.IsPaused);Assert.AreEqual(0,Time.timeScale);
            Call("Back");Assert.AreEqual("Equipment",Get("page").ToString());
            Assert.AreEqual(17,(int)Get("selectedId"));Assert.AreEqual(new Vector2(3,42),(Vector2)Get("menuScroll"));
            Assert.IsTrue(game.IsPaused);
            Call("Back");Assert.AreEqual("None",Get("page").ToString());
            Assert.IsFalse(game.IsPaused);Assert.AreEqual(1,Time.timeScale);
        }
        [Test] public void ReviewingRequestsFromPauseCannotDeliverOrEarnMoney()
        {
            var item=new CompactStack{id=model.State.nextId++,kind=PartKind.Copper,quantity=3,xpEligible=true};
            model.State.items.Add(item);model.State.carriedId=item.id;
            Call("Show",Page("Pause"),0);Call("Show",Page("Contracts"),0);
            int money=model.State.money,xp=model.State.experience;
            Assert.IsFalse(AtCounter());Assert.IsFalse((bool)Call("DeliverCustomerRequest"));
            Assert.AreEqual(money,model.State.money);Assert.AreEqual(xp,model.State.experience);
            Assert.AreEqual(3,item.quantity);Assert.AreEqual(0,model.Career.Stats.contractsCompleted);
        }
        [Test] public void SalesCounterContextSurvivesNestedRequestReviewAndEndsOnResume()
        {
            Call("Show",Page("Sales"),0);Assert.IsTrue(AtCounter());
            Call("Show",Page("Contracts"),0);Assert.IsTrue(AtCounter());
            Call("Back");Assert.IsTrue(AtCounter());
            Call("Back");Assert.IsFalse(AtCounter());Assert.IsFalse(game.IsPaused);
            Call("Show",Page("Contracts"),0);Assert.IsFalse(AtCounter());
        }
        [Test] public void CompletedChapterEscapeAcknowledgesOnceWithoutAwardingRewards()
        {
            model.Career.Stats.completedGoals=511;model.Career.Stats.contractsCompleted=model.Rules.contracts.Length;
            model.Career.Stats.contract=null;
            int money=model.State.money,xp=model.State.experience;
            Call("RefreshCareer");Assert.AreEqual("Journal",Get("page").ToString());Assert.IsTrue(game.IsPaused);
            Call("Back");Assert.IsTrue(model.Career.Stats.completionAcknowledged);Assert.IsFalse(game.IsPaused);
            Call("RefreshCareer");Assert.AreEqual("None",Get("page").ToString());
            Assert.AreEqual(money,model.State.money);Assert.AreEqual(xp,model.State.experience);
        }
        [Test] public void FocusLossEndsCounterContextBeforePauseRequestReview()
        {
            Call("Show",Page("Sales"),0);Call("Show",Page("Contracts"),0);Assert.IsTrue(AtCounter());
            Set("selectedId",17);Set("menuScroll",new Vector2(3,42));Set("requestConfirmId",1);
            Call("OnApplicationFocus",false);
            Assert.AreEqual("Pause",Get("page").ToString());Assert.IsTrue(game.IsPaused);
            Assert.AreEqual(0,(int)Get("selectedId"));Assert.AreEqual(Vector2.zero,(Vector2)Get("menuScroll"));
            Assert.AreEqual(0,(int)Get("requestConfirmId"));
            Call("Show",Page("Journal"),0);Call("Show",Page("Contracts"),0);
            Assert.IsFalse(AtCounter());Assert.IsFalse((bool)Call("DeliverCustomerRequest"));
            Call("Back");Assert.AreEqual("Journal",Get("page").ToString());
            Call("Back");Assert.AreEqual("Pause",Get("page").ToString());
        }
        [Test] public void FocusLossKeepsFirstShiftIntroductionUntilExplicitContinue()
        {
            Set("page",Page("Welcome"));Call("OnApplicationFocus",false);
            Assert.AreEqual("Welcome",Get("page").ToString());Assert.IsTrue(game.IsPaused);
            Assert.IsFalse(model.State.welcomeSeen);
            Call("FinishWelcome");Assert.IsTrue(model.State.welcomeSeen);Assert.IsFalse(game.IsPaused);
        }
    }
}
