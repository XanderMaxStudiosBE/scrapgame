using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    // Shared by native EditMode tests and the controlled source/value runner.
    // The latter checks visibility decisions and retained identity, never native physics.
    public static class CompactDressingVisibilityCases
    {
        public static readonly string[] Names={"ActiveBenchStockSurvivesWork","RestoredSpawnRemainsClear",
            "RemovedEquipmentWaitsForPlayer","RemovedLooseItemWaitsForPlayer","CapsuleCornerDoesNotHideClearStock"};
        public static readonly string[] CoordinatorNames={"CoordinatorBenchWorkKeepsStock","CoordinatorRestoredSpawnPollsClearance",
            "CoordinatorRemovedEquipmentPollsClearance","CoordinatorRemovedLooseItemPollsClearance"};
        static void Check(bool condition,string reason){if(!condition)throw new Exception(reason);}
        static Transform Find(GameObject root,string name)
        {
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))if(transform.name==name)return transform;
            throw new Exception("Missing stock: "+name);
        }
        static void SameMeshes(MeshFilter[] before,GameObject root)
        {
            var after=root.GetComponentsInChildren<MeshFilter>(true);Check(before.Length==after.Length,"Visibility cannot create/remove mesh views");
            for(int i=0;i<before.Length;i++)Check(ReferenceEquals(before[i],after[i]),"Visibility must retain pooled mesh identity");
        }
        public static void Run(string scenario)
        {
            var yard=new GameObject("Dressing visibility / "+scenario);
            try
            {
                var rules=new CompactRules();var model=new ScrappingModel(rules);var occupied=new List<Bounds>();
                var spawn=new Vector3(0,1.1f,-13);var working=new Vector3(-5,1.1f,-5.5f);var inside=new Vector3(-5,1.1f,-6.4f);
                const float radius=.38f;
                if(scenario=="RestoredSpawnRemainsClear")spawn=inside;
                if(scenario=="RemovedEquipmentWaitsForPlayer")
                    model.State.equipment.Add(new EquipmentState{id=model.State.nextId++,kind=EquipmentKind.Storage,x=inside.x,z=inside.z});
                else if(scenario=="RemovedLooseItemWaitsForPlayer")
                    model.State.items.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Wire,quantity=1,x=inside.x,z=inside.z});
                CompactDressingOccupancy.Collect(model.State,rules,spawn,occupied);
                var root=CompactYardClutter.Build(yard.transform,occupied,false,true);
                var toolWall=Find(root,"Bench tool wall");var before=root.GetComponentsInChildren<MeshFilter>(true);
                switch(scenario)
                {
                    case "ActiveBenchStockSurvivesWork":
                        var patches=CompactYardClutter.Describe();Check(patches.Length==53,"Fresh fixture must keep all 53 stock modules");
                        var active=new bool[patches.Length];
                        for(int i=0;i<patches.Length;i++)active[i]=Find(root,patches[i].name).gameObject.activeSelf;
                        Check(toolWall.gameObject.activeSelf,"Fresh tool wall must be visible");
                        CompactDressingOccupancy.Collect(model.State,rules,working,occupied);
                        CompactYardClutter.Patch wall=null;foreach(var patch in patches)if(patch.name=="Bench tool wall")wall=patch;
                        Check(CompactYardClutter.IsBlocked(wall,occupied),"Regression position must reproduce the old player-clearance hide");
                        Check(model.AcquireWire() && model.BeginProcessing(model.State.equipment[0].id),"Actual manual recipe starts");
                        while(!model.State.equipment[0].job.ready)Check(model.Work(model.State.equipment[0].id),"Actual manual strokes complete");
                        CompactDressingOccupancy.Collect(model.State,rules,working,occupied,protectPlayer:false);
                        Check(!CompactYardClutter.RefreshVisibility(root,occupied,working,radius),"Working near active stock must not defer it");
                        for(int i=0;i<patches.Length;i++)Check(active[i]==Find(root,patches[i].name).gameObject.activeSelf,patches[i].name+" changed after bench work");
                        Check(model.State.money==rules.startingMoney && model.State.experience==0,"Cosmetic refresh cannot award cash/XP");
                        break;
                    case "RestoredSpawnRemainsClear":
                        Check(!toolWall.gameObject.activeSelf,"The restored spawn must start with clear stock collision");
                        CompactDressingOccupancy.Collect(model.State,rules,inside,occupied,protectPlayer:false);
                        Check(CompactYardClutter.RefreshVisibility(root,occupied,inside,radius),"Spawn module must stay hidden inside the capsule");
                        Check(!toolWall.gameObject.activeSelf,"A normal refresh must not enable a collider through the restored player");
                        Check(!CompactYardClutter.RefreshVisibility(root,occupied,working,radius),"Deferred spawn stock must release after the player clears");
                        Check(toolWall.gameObject.activeSelf,"Clear spawn module must restore");
                        break;
                    case "RemovedEquipmentWaitsForPlayer":
                    case "RemovedLooseItemWaitsForPlayer":
                        Check(!toolWall.gameObject.activeSelf,"Saved occupied stock module must be hidden");
                        var collider=toolWall.GetComponent<BoxCollider>();Check(collider!=null,"The regression must protect a real stock collider");
                        if(scenario=="RemovedEquipmentWaitsForPlayer")model.State.equipment.RemoveAt(model.State.equipment.Count-1);
                        else model.State.items.Clear();
                        CompactDressingOccupancy.Collect(model.State,rules,inside,occupied,protectPlayer:false);
                        Check(CompactYardClutter.RefreshVisibility(root,occupied,inside,radius),"Removed saved obstacle must still wait for the player");
                        Check(!toolWall.gameObject.activeSelf,"Removed saved obstacle cannot re-enable collision through the player");
                        Check(CompactYardClutter.RefreshVisibility(root,occupied,new Vector3(-5,1.1f,-5.7f),radius),"Capsule touching the stock edge must remain protected");
                        Check(!CompactYardClutter.RefreshVisibility(root,occupied,working,radius),"Walking clear must release deferred stock");
                        Check(toolWall.gameObject.activeSelf && ReferenceEquals(collider,toolWall.GetComponent<BoxCollider>()),"Cleared stock must restore its retained collider");
                        break;
                    case "CapsuleCornerDoesNotHideClearStock":
                        CompactYardClutter.Patch cornerWall=null;
                        foreach(var patch in CompactYardClutter.Describe())if(patch.name=="Bench tool wall")cornerWall=patch;
                        CompactYardClutter.RefreshVisibility(root,new[]{cornerWall.footprint});Check(!toolWall.gameObject.activeSelf,"Corner fixture must begin hidden");
                        CompactDressingOccupancy.Collect(model.State,rules,spawn,occupied,protectPlayer:false);
                        var corner=cornerWall.footprint.max;
                        Check(!CompactYardClutter.RefreshVisibility(root,occupied,new Vector3(corner.x+radius*.8f,1.1f,corner.z+radius*.8f),radius),
                            "A diagonal gap outside the capsule must not use the old square-plus-clearance exclusion");
                        Check(toolWall.gameObject.activeSelf,"A genuinely clear corner can restore");
                        break;
                    default:throw new ArgumentException("Unknown visibility scenario: "+scenario);
                }
                SameMeshes(before,root);
            }
            finally{UnityEngine.Object.DestroyImmediate(yard);}
        }

        static object Field(CompactYardGame game,string name)
        {return typeof(CompactYardGame).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);}
        static void Call(CompactYardGame game,string name)
        {typeof(CompactYardGame).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);}

        public static void RunCoordinator(string scenario)
        {
            var yard=new GameObject("Dressing coordinator / "+scenario);
            try
            {
                var game=yard.AddComponent<CompactYardGame>();var model=new ScrappingModel(new CompactRules());
                typeof(CompactYardGame).GetProperty("Model").GetSetMethod(true).Invoke(game,new object[]{model});
                var actor=new GameObject("Dressing test player");actor.transform.SetParent(yard.transform,false);
                actor.transform.localPosition=new Vector3(0,1.1f,-13);
                var capsule=actor.AddComponent<CharacterController>();capsule.height=1.8f;capsule.radius=.3f;capsule.skinWidth=.08f;
                game.player=actor.AddComponent<FirstPersonController>();
                Call(game,"SyncDressing");
                var root=(GameObject)Field(game,"yardClutter");var wall=Find(root,"Bench tool wall");
                var before=root.GetComponentsInChildren<MeshFilter>(true);
                Check(wall.gameObject.activeSelf,"Fresh coordinator stock must be visible");
                if(scenario=="CoordinatorBenchWorkKeepsStock")
                {
                    var patches=CompactYardClutter.Describe();var active=new bool[patches.Length];
                    for(int i=0;i<patches.Length;i++)active[i]=Find(root,patches[i].name).gameObject.activeSelf;
                    actor.transform.localPosition=new Vector3(-5,1.1f,-5.5f);
                    Check(model.AcquireWire() && model.BeginProcessing(model.State.equipment[0].id),"Coordinator fixture starts actual recipe");
                    while(!model.State.equipment[0].job.ready)Check(model.Work(model.State.equipment[0].id),"Coordinator fixture completes actual work");
                    Call(game,"SyncDressing");Call(game,"SyncDressing");Call(game,"RefreshDressingPlayerClearance");
                    for(int i=0;i<patches.Length;i++)Check(active[i]==Find(root,patches[i].name).gameObject.activeSelf,"Coordinator changed "+patches[i].name+" after working");
                    Check(!(bool)Field(game,"dressingPlayerClearancePending"),"An active working pocket must not keep player clearance polling pending");
                }
                else if(scenario=="CoordinatorRestoredSpawnPollsClearance")
                {
                    actor.transform.localPosition=new Vector3(-5,1.1f,-6.4f);
                    Call(game,"RequireDressingSpawnClearance");Call(game,"SyncDressing");
                    Check(!wall.gameObject.activeSelf,"Restoring into an existing pooled module must hide its collision");
                    Call(game,"RefreshDressingPlayerClearance");
                    Check(!wall.gameObject.activeSelf,"Unmoved restored player must retain spawn clearance");
                    actor.transform.localPosition=new Vector3(-5,1.1f,-5.7f);Call(game,"RefreshDressingPlayerClearance");
                    Check(!wall.gameObject.activeSelf && (bool)Field(game,"dressingPlayerClearancePending"),"Actual capsule edge must remain protected");
                    actor.transform.localPosition=new Vector3(-5,1.1f,-5.5f);Call(game,"RefreshDressingPlayerClearance");
                    Check(wall.gameObject.activeSelf && !(bool)Field(game,"dressingPlayerClearancePending"),"Existing scheduled refresh restores stock after leaving the capsule zone");
                }
                else if(scenario=="CoordinatorRemovedEquipmentPollsClearance" || scenario=="CoordinatorRemovedLooseItemPollsClearance")
                {
                    bool equipment=scenario=="CoordinatorRemovedEquipmentPollsClearance";
                    if(equipment)model.State.equipment.Add(new EquipmentState{id=model.State.nextId++,kind=EquipmentKind.Storage,x=-5,z=-6.4f});
                    else model.State.items.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Wire,quantity=1,x=-5,z=-6.4f});
                    Call(game,"SyncDressing");Check(!wall.gameObject.activeSelf,"An added saved object must hide its overlapping stock module");
                    actor.transform.localPosition=new Vector3(-5,1.1f,-6.4f);
                    if(equipment)model.State.equipment.RemoveAt(model.State.equipment.Count-1);else model.State.items.Clear();
                    Call(game,"SyncDressing");
                    Check(!wall.gameObject.activeSelf && (bool)Field(game,"dressingPlayerClearancePending"),"Changed occupancy cannot restore the module through the actual player");
                    // Only the existing periodic clearance hook is called; no gameplay transaction follows.
                    actor.transform.localPosition=new Vector3(-5,1.1f,-5.5f);Call(game,"RefreshDressingPlayerClearance");
                    Check(wall.gameObject.activeSelf && !(bool)Field(game,"dressingPlayerClearancePending"),"Walking clear restores retained stock without another transaction");
                    Call(game,"RefreshDressingPlayerClearance");Check(wall.gameObject.activeSelf,"Cleared polling must remain stable");
                }
                else throw new ArgumentException("Unknown dressing coordinator case: "+scenario);
                SameMeshes(before,root);Check(ReferenceEquals(root,Field(game,"yardClutter")),"The coordinator must retain the stock root");
            }
            finally{UnityEngine.Object.DestroyImmediate(yard);}
        }
    }
}
