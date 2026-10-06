using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Scrapshift.Compact;
using UnityEngine;

namespace Scrapshift.Tests
{
    // Native hierarchy/input/audio contracts. These do not measure frame times.
    // Gameplay writes are blocked; preference fixtures use unique temporary paths.
    public sealed class CompactRuntimeEfficiencyTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        GameObject root;
        CompactYardGame game;
        CompactBalance balance;
        PlayerInputSettings input;
        PresentationSettings presentation;
        YardAudio sounds;
        string folder;
        float previousScale;
        CursorLockMode previousCursor;
        bool previousVisible;
        ScrappingModel Model {get{return game.Model;}}
        object Get(string name){return typeof(CompactYardGame).GetField(name,Private).GetValue(game);}
        void Set(string name,object value){typeof(CompactYardGame).GetField(name,Private).SetValue(game,value);}
        object Call(string name,params object[] args){return typeof(CompactYardGame).GetMethod(name,Private).Invoke(game,args);}
        GameObject View(int id,string member)
        {
            var entity=((IDictionary)Get("views"))[id];
            return (GameObject)entity.GetType().GetField(member).GetValue(entity);
        }
        ProgressCheckpoint Checkpoint {get{return (ProgressCheckpoint)Get("workCheckpoint");}}
        EquipmentState Add(EquipmentKind kind,float x,float z)
        {
            var equipment=new EquipmentState{id=Model.State.nextId++,kind=kind,x=x,z=z,paidPrice=Model.Rules.Equipment(kind).price};
            Model.State.equipment.Add(equipment);return equipment;
        }
        EquipmentState PoweredMachine(float x=5,float z=5)
        {
            var generator=Add(EquipmentKind.Generator,0,z);var machine=Add(EquipmentKind.Tier1Scrapper,x,z);
            Assert.IsTrue(game.Construction.Connect(generator.id,machine.id),game.Construction.LastMessage);
            Call("SyncViews");return machine;
        }
        [SetUp] public void Setup()
        {
            previousScale=Time.timeScale;previousCursor=Cursor.lockState;previousVisible=Cursor.visible;
            folder=Path.Combine(Path.GetTempPath(),"scrapshift-efficiency-"+Guid.NewGuid().ToString("N"));
            root=new GameObject("Compact runtime efficiency contract");
            var person=new GameObject("Player");person.transform.SetParent(root.transform,false);
            person.AddComponent<CharacterController>();var player=person.AddComponent<FirstPersonController>();
            var camera=new GameObject("Camera");camera.transform.SetParent(person.transform,false);player.view=camera.AddComponent<Camera>();
            game=root.AddComponent<CompactYardGame>();game.enabled=false;game.player=player;
            balance=ScriptableObject.CreateInstance<CompactBalance>();game.balance=balance;
            input=new PlayerInputSettings(Path.Combine(folder,"controls.json"));player.controls=input;
            presentation=new PresentationSettings(player.view,root.transform,Path.Combine(folder,"presentation.json"));
            sounds=new YardAudio(root.transform,player,presentation);
            Set("controls",input);Set("presentation",presentation);Set("settings",new SettingsMenu(input,presentation));Set("sounds",sounds);
            Set("saveBlocked",true);Set("sessionStarted",true);
            Set("page",Enum.Parse(typeof(CompactYardGame).GetNestedType("Page",BindingFlags.NonPublic),"None"));
            Call("BindModel",new object[]{null});Call("RestorePlayer");Call("SyncViews");
        }
        [TearDown] public void Cleanup()
        {
            // YardAudio.Dispose schedules native destruction for Play mode. EditMode
            // fixtures destroy its private root immediately without invoking audio playback cleanup.
            if(sounds!=null)
            {
                if(game!=null)Set("sounds",null);
                var audioRoot=(GameObject)typeof(YardAudio).GetField("root",Private).GetValue(sounds);
                if(audioRoot!=null)UnityEngine.Object.DestroyImmediate(audioRoot);
            }
            if(presentation!=null)presentation.Dispose();
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            if(balance!=null)UnityEngine.Object.DestroyImmediate(balance);
            if(folder!=null&&Directory.Exists(folder))Directory.Delete(folder,true);
            Time.timeScale=previousScale;Cursor.lockState=previousCursor;Cursor.visible=previousVisible;
        }
        [Test] public void ManualWorkbenchStrokeKeepsLoadedGeometryAndQueuesCheckpoint()
        {
            var bench=Model.State.equipment[0];
            Assert.IsTrue(Model.AcquireWire());Assert.IsTrue(Model.BeginProcessing(bench.id));Call("SyncViews");
            var body=View(bench.id,"root");var details=View(bench.id,"details");float started=Time.unscaledTime;
            Call("ManualAct",bench.id,false);Call("RefreshProcessingViews");
            Assert.AreEqual(1,bench.job.strokes);Assert.IsTrue(Checkpoint.Pending);
            Assert.IsFalse(Checkpoint.Due(started+.99f));Assert.IsTrue(Checkpoint.Due(started+1));
            Assert.AreSame(body,View(bench.id,"root"));Assert.AreSame(details,View(bench.id,"details"));
        }
        [Test] public void ManualDismantlingKeepsShellAndRetainsPartialProgress()
        {
            int id=Model.State.scrap[0].id;Assert.IsTrue(Model.InspectScrap(id));Call("SyncViews");var shell=View(id,"root");
            Call("ManualAct",id,true);Call("ManualAct",id,true);Call("SyncViews");
            Assert.AreEqual(2,Model.FindScrap(id).strokes);Assert.IsTrue(Checkpoint.Pending);
            Assert.AreSame(shell,View(id,"root"));
        }
        [Test] public void ManualCompletionReplacesOutputDetailsWithoutReplacingBench()
        {
            var bench=Model.State.equipment[0];Assert.IsTrue(Model.AcquireWire());Assert.IsTrue(Model.BeginProcessing(bench.id));Call("SyncViews");
            var body=View(bench.id,"root");var inputDetails=View(bench.id,"details");
            for(int i=0;i<bench.job.requiredStrokes;i++)Call("ManualAct",bench.id,false);
            Assert.IsTrue(bench.job.ready);Assert.AreSame(body,View(bench.id,"root"));Assert.AreNotSame(inputDetails,View(bench.id,"details"));
        }
        [Test] public void TimerProgressKeepsGeometryUntilReadyOutputsAppear()
        {
            var machine=PoweredMachine();Assert.IsTrue(Model.AcquireWire());Assert.IsTrue(Model.BeginProcessing(machine.id));Call("SyncViews");
            var body=View(machine.id,"root");var details=View(machine.id,"details");float remaining=machine.job.remaining;
            Assert.IsTrue(Model.Tick(.2f));Call("RefreshProcessingViews");
            Assert.Less(machine.job.remaining,remaining);Assert.AreSame(body,View(machine.id,"root"));Assert.AreSame(details,View(machine.id,"details"));
            Model.Tick(20);Call("RefreshProcessingViews");Assert.IsTrue(machine.job.ready);
            Assert.AreSame(body,View(machine.id,"root"));Assert.AreNotSame(details,View(machine.id,"details"));
        }
        [Test] public void BufferQuantityKeepsGeometryAndDepletionRefreshesOnlyDetails()
        {
            var storage=Add(EquipmentKind.Storage,-8,5);storage.contents.Add(new CompactStack{id=Model.State.nextId++,kind=PartKind.Copper,quantity=5});Call("SyncViews");
            var body=View(storage.id,"root");var details=View(storage.id,"details");storage.contents[0].quantity=4;Call("RefreshProcessingViews");
            Assert.AreSame(body,View(storage.id,"root"));Assert.AreSame(details,View(storage.id,"details"));
            storage.contents.Clear();Call("RefreshProcessingViews");
            Assert.AreSame(body,View(storage.id,"root"));Assert.AreNotSame(details,View(storage.id,"details"));
        }
        [Test] public void PausedUpdatePreservesJobsWorkAndSuppressedActivationInput()
        {
            var machine=PoweredMachine();Assert.IsTrue(Model.AcquireWire());Assert.IsTrue(Model.BeginProcessing(machine.id));Call("SyncViews");
            var bench=Model.State.equipment[0];Assert.IsTrue(Model.AcquireWire());Assert.IsTrue(Model.BeginProcessing(bench.id));Call("ManualAct",bench.id,false);
            float remaining=machine.job.remaining;int strokes=bench.job.strokes;Call("Pause",true);Call("Update");
            Assert.IsTrue(game.IsPaused);Assert.AreEqual(0,Time.timeScale);Assert.IsFalse(input.GameplayReady);
            Assert.AreEqual(remaining,machine.job.remaining);Assert.AreEqual(strokes,bench.job.strokes);Assert.IsTrue(Checkpoint.Pending);
        }
        [Test] public void SettingsCaptureUpdatePreservesJobsEvenIfPauseFlagIsClear()
        {
            var machine=PoweredMachine();Assert.IsTrue(Model.AcquireWire());Assert.IsTrue(Model.BeginProcessing(machine.id));Call("SyncViews");
            float remaining=machine.job.remaining;var settings=(SettingsMenu)Get("settings");settings.Open();Call("Update");
            Assert.IsTrue(settings.IsOpen);Assert.AreEqual(remaining,machine.job.remaining);Assert.IsFalse(input.GameplayReady);
        }
        [Test] public void MotorSourceUsesNearestPoweredRunningJobAndStableTie()
        {
            var generator=Add(EquipmentKind.Generator,0,4);var left=Add(EquipmentKind.Tier1Scrapper,-10,4);var right=Add(EquipmentKind.Tier1Scrapper,10,4);
            Assert.IsTrue(game.Construction.Connect(generator.id,left.id));Assert.IsTrue(game.Construction.Connect(generator.id,right.id));Call("SyncViews");
            Assert.IsTrue(Model.AcquireWire());Assert.IsTrue(Model.BeginProcessing(left.id));Assert.IsTrue(Model.AcquireWire());Assert.IsTrue(Model.BeginProcessing(right.id));Call("SyncViews");
            var motor=(AudioSource)typeof(YardAudio).GetField("machine",Private).GetValue(sounds);
            game.player.transform.position=new Vector3(9,1.1f,4);Call("StepSound");Assert.AreEqual(new Vector3(10,1,4),motor.transform.position);
            Model.State.equipment.Reverse();Call("StepSound");Assert.AreEqual(new Vector3(10,1,4),motor.transform.position);
            game.player.transform.position=new Vector3(0,1.1f,4);Call("StepSound");Assert.AreEqual(new Vector3(-10,1,4),motor.transform.position);
            game.player.transform.position=new Vector3(9,1.1f,4);right.contents.Add(new CompactStack{id=Model.State.nextId++,kind=PartKind.Wire,quantity=24});
            Call("StepSound");Assert.AreEqual(new Vector3(10,1,4),motor.transform.position,"A full IN bay does not block an already reserved OUT batch");
            right.job.yields[0].quantity=25; // Paid snapshot output can exceed a subsequently reduced capacity.
            Call("StepSound");Assert.AreEqual(new Vector3(-10,1,4),motor.transform.position);
        }
        [Test] public void ActualOutputMouthRejectsCarriedIntakeWhileBusyInputQueuesIt()
        {
            var machine=PoweredMachine();Assert.IsTrue(Model.AcquireWire());Assert.IsTrue(Model.BeginProcessing(machine.id));
            var carried=new CompactStack{id=Model.State.nextId++,kind=PartKind.Motor,quantity=1,xpEligible=true};Model.State.items.Add(carried);Model.State.carriedId=carried.id;
            var owner=new GameObject("Test targeted mouth");owner.transform.SetParent(root.transform,false);
            var target=owner.AddComponent<CompactInteractionTarget>();target.kind=CompactTargetKind.Equipment;target.id=machine.id;
            var mouth=owner.AddComponent<CompactConveyorPortTarget>();mouth.output=true;Set("target",target);Set("aimedPort",mouth);
            var paidJob=machine.job;int xp=Model.State.experience;Call("Interact");
            Assert.AreSame(carried,Model.Carried);Assert.AreSame(paidJob,machine.job);Assert.IsEmpty(machine.contents);
            mouth.output=false;Call("Interact");Assert.IsNull(Model.Carried);Assert.AreSame(paidJob,machine.job);
            Assert.AreSame(carried,machine.contents[0]);Assert.AreEqual(xp,Model.State.experience);
        }
        [Test] public void LargeCarriedBatchQueuesInsteadOfFailingAnOversizedDirectRecipe()
        {
            var bench=Model.State.equipment[0];Assert.IsTrue(Model.AcquireWire());Model.Carried.quantity=10;
            Assert.IsFalse(Model.CanBeginProcessing(bench.id,out _));Assert.IsTrue(Model.CanDeposit(bench.id,out _));
            var owner=new GameObject("Test bench target");owner.transform.SetParent(root.transform,false);
            var target=owner.AddComponent<CompactInteractionTarget>();target.kind=CompactTargetKind.Equipment;target.id=bench.id;Set("target",target);
            Call("Interact");Assert.IsNull(Model.Carried);Assert.IsNull(bench.job);Assert.AreEqual(10,Model.StoredUnits(bench.id));
            Model.Tick(.1f);Assert.IsNotNull(bench.job);Assert.AreEqual(1,bench.job.inputQuantity);Assert.AreEqual(0,bench.job.strokes);
        }
    }
}
