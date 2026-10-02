using System;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactConstructionScenarios
    {
        public static readonly string[] Names = {
            "CompactConstructionCostsAndRejectedPlacement", "CompactConstructionProtectedAccess", "CompactConstructionRotatedFootprints",
            "CompactConstructionLooseAndLargeObjects", "CompactConstructionMoveAndDismantle", "CompactConstructionUnlockAndCapacity",
            "CompactPowerNetworksAndOverload", "CompactPowerConnectionsAndPreservedJobs", "CompactConstructionSaveGuards",
            "CompactPowerProcessingResumesExactlyOnce", "CompactLegacyBuildBindings", "CompactOccupiedBuildDefaults", "CompactBuildRebindCancelAndSwap", "CompactInvalidLegacyBindings"
        };
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static void Reject(Action action)
        { try { action(); } catch (ArgumentException) { return; } throw new Exception("Invalid state accepted."); }
        static int Place(ConstructionModel construction, EquipmentKind kind, float x, float z, float yaw = 0)
        { Check(construction.Place(kind,x,z,yaw),construction.LastMessage);return construction.LastPlacedId; }
        public static void Run(string name)
        {
            var rules = new CompactRules(); rules.Validate();
            var state = new CompactYardState { money = 1000 };
            var c = new ConstructionModel(state,rules); string reason;
            switch(name)
            {
                case "CompactConstructionCostsAndRejectedPlacement":
                    Check(c.CanPlace(EquipmentKind.Generator,0,0,0,0,out reason),reason);
                    Check(state.money==1000 && state.nextId==1 && state.equipment.Count==0,"preview is read-only");
                    int generator=Place(c,EquipmentKind.Generator,0,0,450);
                    Check(generator==1 && state.nextId==2 && state.money==1000-rules.Equipment(EquipmentKind.Generator).price,"purchase once");
                    Check(c.Find(generator).yaw==90,"saved rotation normalized");
                    int cash=state.money,next=state.nextId;
                    Check(!c.Place(EquipmentKind.Generator,24,0,0) && !c.Place(EquipmentKind.Generator,float.NaN,0,0),"invalid locations rejected");
                    Check(state.money==cash && state.nextId==next && state.equipment.Count==1,"failure no charge or identity mutation");
                    state.money=0;Check(!c.Place(EquipmentKind.Tier1Scrapper,5,0,0) && state.equipment.Count==1,"unaffordable rejected");break;
                case "CompactConstructionProtectedAccess":
                    Check(!c.CanPlace(EquipmentKind.Workbench,0,-14,0,0,out reason) && reason.Contains("entrance"),"entrance protected");
                    Check(!c.CanPlace(EquipmentKind.Workbench,-17,-13,0,0,out reason) && reason.Contains("office"),"office protected");
                    Check(!c.CanPlace(EquipmentKind.Generator,18,-10,0,0,out reason) && reason.Contains("delivery"),"delivery protected");
                    Check(!c.CanPlace(EquipmentKind.Workbench,0,-10.5f,0,0,out reason),"wholefootprint not justcenter protected");
                    Check(c.CanPlace(EquipmentKind.Workbench,0,-9,0,0,out reason),"openinterior accessible");break;
                case "CompactConstructionRotatedFootprints":
                    Check(!c.CanPlace(EquipmentKind.Workbench,23,8,0,0,out reason),"unrotated bench beyond fence");
                    Check(c.CanPlace(EquipmentKind.Workbench,23,8,90,0,out reason),"rotated bench fits");
                    rules.Equipment(EquipmentKind.Workbench).width=1;rules.Equipment(EquipmentKind.Workbench).depth=10;
                    Place(c,EquipmentKind.Workbench,0,3,45);
                    Check(c.CanPlace(EquipmentKind.Workbench,2,1,45,0,out reason),"parallel diagonal benches with overlapping AABBs fit");
                    Check(!c.CanPlace(EquipmentKind.Workbench,-2,1,45,0,out reason),"diagonal longaxis collision rejected");break;
                case "CompactConstructionLooseAndLargeObjects":
                    state.items.Add(new CompactStack{id=50,kind=PartKind.Motor,quantity=1,x=0,z=0});state.nextId=51;
                    Check(!c.CanPlace(EquipmentKind.Workbench,0,0,0,0,out reason) && reason.Contains("loose"),"loosepart access preserved");
                    state.carriedId=50;Check(c.CanPlace(EquipmentKind.Workbench,0,0,0,0,out reason),"carriedpart notworldobstacle");
                    state.scrap.Add(new LargeScrapJob{id=51,kind=ScrapObjectKind.Car,x=8,z=4});state.nextId=52;
                    Check(!c.CanPlace(EquipmentKind.Workbench,8,6,0,0,out reason) && reason.Contains("scrap"),"car longitudinal body protected");
                    Check(c.CanPlace(EquipmentKind.Workbench,4,4,0,0,out reason),"space outsidecar usable");break;
                case "CompactConstructionMoveAndDismantle":
                    int bench=Place(c,EquipmentKind.Workbench,-5,0);int g=Place(c,EquipmentKind.Generator,0,0);int machine=Place(c,EquipmentKind.Tier1Scrapper,5,0);
                    int paid=c.Find(g).paidPrice;int before=state.money;
                    Check(c.Move(g,1,2,90) && state.money==before && c.Find(g).paidPrice==paid,"free move preservesownership");
                    Check(c.Connect(g,machine) && !c.Move(g,0,4,0) && !c.Remove(g),"connected equipment remains");
                    Check(c.Disconnect(g,machine),"disconnect");c.Find(machine).job=new ProcessingJob();
                    Check(!c.Move(machine,6,3,0) && !c.Remove(machine),"reservedjob notdeleted");c.Find(machine).job=null;
                    c.Find(machine).contents.Add(new CompactStack{id=55,kind=PartKind.Copper,quantity=1});
                    Check(!c.Remove(machine),"storedcontents notdeleted");c.Find(machine).contents.Clear();
                    Check(c.Remove(g) && state.money==before+paid/2 && !c.Remove(g),"refundonce");
                    Check(!c.Remove(bench),"finalbench preventssoftlock");Place(c,EquipmentKind.Workbench,-5,5);
                    c.Find(bench).starter=true;Check(c.Remove(bench) && c.LastRefund==0,"startercannotmanufacture cash");break;
                case "CompactConstructionUnlockAndCapacity":
                    Check(!c.Place(EquipmentKind.Conveyor,0,0,0),"planned stage cannotbe bought");
                    state.experience=rules.levelThresholds[9];Check(!c.Place(EquipmentKind.Tier2Scrapper,0,0,0),"level10doesnotpretendplanned functional");
                    rules.Equipment(EquipmentKind.Generator).unlockLevel=10;state.experience=0;
                    Check(!c.Place(EquipmentKind.Generator,0,0,0),"editable unlock enforced");
                    state.experience=rules.levelThresholds[9];Place(c,EquipmentKind.Generator,0,0);
                    rules.maxEquipment=1;Check(!c.Place(EquipmentKind.Workbench,5,0,0),"configured equipmentlimit enforced");break;
                case "CompactPowerNetworksAndOverload":
                    int source=Place(c,EquipmentKind.Generator,0,0);int one=Place(c,EquipmentKind.Tier1Scrapper,4,0);int two=Place(c,EquipmentKind.Tier1Scrapper,8,0);
                    int three=Place(c,EquipmentKind.Tier1Scrapper,8,5);int spare=Place(c,EquipmentKind.Generator,0,5);
                    Check(!c.PowerFor(one).powered && !c.PowerFor(one).connected,"isolated consumer paused");
                    Check(c.Connect(source,one) && c.Connect(one,two),"physicalnetwork");
                    var status=c.PowerFor(two);Check(status.powered && status.supply==6 && status.demand==6,"exactcapacity powered");
                    Check(c.Connect(two,three) && c.PowerFor(one).overloaded && !c.PowerFor(one).powered && !c.PowerFor(three).powered,"allconsumers pause onoverload");
                    Check(c.Connect(source,spare) && c.PowerFor(one).powered && c.PowerFor(one).supply==12,"secondgenerator resolvesoverload");
                    state.powerLinks.Reverse();status=c.PowerFor(one);Check(status.supply==12 && status.demand==9 && status.powered,"deterministic linksorder");
                    Check(c.Disconnect(source,one) && !c.PowerFor(two).powered && c.PowerFor(two).reason.Contains("No generator"),"consumer-onlynetwork paused");break;
                case "CompactPowerConnectionsAndPreservedJobs":
                    int s=Place(c,EquipmentKind.Generator,0,0);int m=Place(c,EquipmentKind.Tier1Scrapper,4,0);int far=Place(c,EquipmentKind.Generator,20,9);
                    int manual=Place(c,EquipmentKind.Workbench,-5,0);
                    Check(!c.Connect(s,s) && !c.Connect(s,far) && !c.Connect(s,manual),"invalidportsrange/self rejected");
                    Check(c.Connect(s,m) && !c.Connect(m,s) && state.powerLinks.Count==1,"undirectedduplicate rejected");
                    var job=new ProcessingJob{remaining=2.5f,input=PartKind.Wire,inputQuantity=1};c.Find(m).job=job;
                    Check(c.Disconnect(m,s) && ReferenceEquals(c.Find(m).job,job) && job.remaining==2.5f,"disconnectpreservesreservedprogress");
                    Check(c.Connect(s,m) && ReferenceEquals(c.Find(m).job,job),"reconnectdoesnotconsume input again");break;
                case "CompactConstructionSaveGuards":
                    int a=Place(c,EquipmentKind.Generator,0,0);int b=Place(c,EquipmentKind.Tier1Scrapper,4,0);Check(c.Connect(a,b),"link");
                    ConstructionModel.Validate(state,rules);
                    state.powerLinks.Add(new PowerLink(b,a));Reject(()=>ConstructionModel.Validate(state,rules));state.powerLinks.RemoveAt(1);
                    state.powerLinks[0].b=99;Reject(()=>ConstructionModel.Validate(state,rules));state.powerLinks[0].b=b;
                    c.Find(a).x=24;Reject(()=>ConstructionModel.Validate(state,rules));c.Find(a).x=0;
                    c.Find(b).x=0;Reject(()=>ConstructionModel.Validate(state,rules));c.Find(b).x=4;
                    c.Find(b).yaw=float.NaN;Reject(()=>ConstructionModel.Validate(state,rules));break;
                case "CompactPowerProcessingResumesExactlyOnce":
                    var scrapModel=new ScrappingModel(rules);scrapModel.State.money=1000;
                    var network=new ConstructionModel(scrapModel.State,rules);scrapModel.HasPower=id=>network.PowerFor(id).powered;
                    int gen=Place(network,EquipmentKind.Generator,0,0),unit=Place(network,EquipmentKind.Tier1Scrapper,4,0);
                    Check(scrapModel.AcquireWire() && scrapModel.BeginProcessing(unit),"input loaded once");
                    var processing=scrapModel.FindEquipment(unit).job;float remaining=processing.remaining;
                    Check(!scrapModel.Tick(1) && processing.remaining==remaining,"unconnected input/progress preserved");
                    Check(network.Connect(gen,unit) && scrapModel.Tick(1) && processing.remaining==remaining-1,"power advances exact gameplay time");
                    Check(network.Disconnect(gen,unit) && !scrapModel.Tick(100) && processing.remaining==remaining-1,"disconnect pauses progress without output");
                    Check(network.Connect(gen,unit),"reconnect");rules.Equipment(EquipmentKind.Tier1Scrapper).outputCapacity=4;
                    Check(!scrapModel.Tick(100) && !processing.ready && processing.remaining==remaining-1,"blocked capacity preserves work");
                    rules.Equipment(EquipmentKind.Tier1Scrapper).outputCapacity=24;
                    Check(scrapModel.Tick(remaining-1) && processing.ready && processing.remaining==0,"resume completes original reserved batch");
                    Check(!scrapModel.Tick(100) && scrapModel.State.experience==0,"completion grants no sale XP");
                    Check(scrapModel.CollectOutput(unit,0) && scrapModel.Carried.quantity==3 && scrapModel.Sell(),"copper sold once");
                    int earned=scrapModel.State.experience;Check(!scrapModel.CollectOutput(unit,0) && scrapModel.State.experience==earned,"recollection cannot grant XP");
                    Check(scrapModel.CollectOutput(unit,1) && scrapModel.Carried.quantity==2 && scrapModel.Sell(),"insulation sold once");
                    Check(scrapModel.FindEquipment(unit).job==null && !scrapModel.CollectOutput(unit,1),"finished reservation cleared exactly once");
                    ScrappingModel.Validate(scrapModel.State,rules);ConstructionModel.Validate(scrapModel.State,rules);break;
                case "CompactLegacyBuildBindings":
                    var p=new ControlPreferences{bindings=new[]{"Z","S","Q","D","F","A","Mouse1"},sensitivity=4.75f,invertY=true};
                    Check(p.UpgradeLegacyBindings(),"oldsettings recognized");p.Validate();
                    Check(p.Binding(ControlAction.MoveForward)=="Z" && p.Binding(ControlAction.Drop)=="A" && p.Binding(ControlAction.ManualWork)=="Mouse1","oldchoices preserved");
                    Check(p.Binding(ControlAction.BuildToggle)=="B" && p.Binding(ControlAction.BuildRotate)=="R" && p.sensitivity==4.75f && p.invertY,"additivebuild/look migration");
                    Check(!p.UpgradeLegacyBindings(),"migrationidempotent");break;
                case "CompactOccupiedBuildDefaults":
                    var occupied=new ControlPreferences{bindings=new[]{"W","S","A","D","B","R","F2"}};
                    Check(occupied.UpgradeLegacyBindings(),"upgrade");occupied.Validate();
                    Check(occupied.Binding(ControlAction.Interact)=="B" && occupied.Binding(ControlAction.Drop)=="R","neversteal existing B/R");
                    Check(occupied.Binding(ControlAction.BuildToggle)=="F3" && occupied.Binding(ControlAction.BuildRotate)=="F4","stableunusedfallbacks");break;
                case "CompactBuildRebindCancelAndSwap":
                    var preferences=new ControlPreferences();var rebind=new ControlRebind(preferences);
                    rebind.Begin(ControlAction.BuildToggle);Check(!rebind.Capture("Escape"),"escape reservedduringbuild rebind");rebind.Cancel();
                    Check(preferences.Binding(ControlAction.BuildToggle)=="B","cancellationunchanged");
                    rebind.Begin(ControlAction.BuildRotate);Check(!rebind.Capture("Mouse0") && rebind.Conflict==ControlAction.ManualWork,"existingworkmouse conflict");
                    Check(rebind.Swap() && preferences.Binding(ControlAction.BuildRotate)=="Mouse0" && preferences.Binding(ControlAction.ManualWork)=="R","mouseworks inbuildandoldaction remainsbound");
                    preferences.RestoreDefaults();Check(preferences.Binding(ControlAction.BuildToggle)=="B" && preferences.Binding(ControlAction.BuildRotate)=="R","builddefaultsrestore");break;
                case "CompactInvalidLegacyBindings":
                    var bad=new ControlPreferences{bindings=new[]{"W","S","A","D","E","E","Mouse0"}};var original=bad.bindings;
                    Reject(()=>bad.UpgradeLegacyBindings());Check(ReferenceEquals(original,bad.bindings) && bad.bindings.Length==7,"invalidmigration doesnotpartially mutate");
                    bad.bindings=new[]{"W","S","A","D","E","Q","Escape"};Reject(()=>bad.UpgradeLegacyBindings());
                    bad.bindings=new[]{"W","S","A","D","E","Q","Mouse0"};bad.sensitivity=float.PositiveInfinity;Reject(()=>bad.UpgradeLegacyBindings());break;
                default:throw new Exception("Unknown compact construction scenario: "+name);
            }
        }
    }
}
