using System;
using System.Collections.Generic;
namespace Scrapshift.Tests
{
    public static class NavigationScenarios
    {
        public static readonly string[] Names = { "DestinationCatalog", "NavigationBearings", "NavigationFollowsWireLoop", "NavigationFindsLocalSupply", "NavigationParallelJobsAndStorage", "NavigationSelectionIsReadOnly", "GuidanceIdentifiesFanMaterials", "GuidanceRetainsBlockedFanOutput" };
        static void Check(bool value,string message) { if(!value)throw new Exception(message); }
        static void Route(YardModel m,YardLandmark expected)
        { Check(YardNavigation.Recommend(m,0,-6).landmark==expected,"Expected "+expected); }
        public static void Run(string scenario)
        {
            var m=new YardModel(new YardRules());
            switch(scenario)
            {
                case "DestinationCatalog":
                    var seen=new HashSet<YardLandmark>();
                    foreach(var destination in YardNavigation.Destinations)
                    {
                        Check(seen.Add(destination.landmark),"unique landmark");
                        Check(destination.x==YardWorldLayout.ClampX(destination.x) && destination.z==YardWorldLayout.ClampZ(destination.z),"inside safe yard");
                        Check(YardNavigation.Get(destination.landmark).name==destination.name,"lookup identity");
                        Check(destination.Distance(destination.x,destination.z)==0,"metre coordinate");
                    }
                    Check(seen.Count==14 && !seen.Contains(YardLandmark.Automatic),"all physical destinations");break;
                case "NavigationBearings":
                    var bearingPoint=new YardDestination(YardLandmark.Bench,"North",0,10);
                    Check(bearingPoint.Distance(0,0)==10,"metres");
                    Check(bearingPoint.Direction(0,0,0)=="Ahead","north facing");
                    Check(bearingPoint.Direction(0,0,90)=="Left","east facing");
                    Check(bearingPoint.Direction(0,0,-90)=="Right","west facing");
                    Check(bearingPoint.Direction(0,0,180)=="Behind","south facing");
                    Check(bearingPoint.Direction(0,0,720)=="Ahead" && bearingPoint.Direction(0,0,-720)=="Ahead","unbounded saved yaw wraps");
                    Check(bearingPoint.Direction(0,7,180)=="Nearby","arrival takes priority over bearing");
                    Check(bearingPoint.Direction(0,0,335)=="Ahead" && bearingPoint.Direction(0,0,-335)=="Ahead","inclusive forward cone");break;
                case "NavigationFollowsWireLoop":
                    Route(m,YardLandmark.Delivery);m.AcquireWire();Route(m,YardLandmark.Bench);
                    m.LoadBench();Route(m,YardLandmark.Bench);
                    while(m.State.benchLoaded)m.WorkBench();Route(m,YardLandmark.Bench);
                    m.CollectBench();Route(m,YardLandmark.Buyer);m.Sell();
                    m.State.money=m.Rules.machinePrice;Route(m,YardLandmark.Machine);m.BuyMachine();
                    m.AcquireWire();Route(m,YardLandmark.Machine);m.FeedMachine();
                    m.Tick(m.MachineSeconds);Route(m,YardLandmark.Machine);m.CollectMachine();Route(m,YardLandmark.Buyer);break;
                case "NavigationFindsLocalSupply":
                    foreach(var landmark in new[]{YardLandmark.Delivery,YardLandmark.VehicleWire,YardLandmark.SalvageWire,YardLandmark.SortingWire})
                    {
                        var point=YardNavigation.Get(landmark);
                        Check(YardNavigation.NearestWire(point.x+.5f,point.z-.5f).landmark==landmark,"nearest source "+landmark);
                        Check(YardNavigation.Recommend(m,point.x,point.z).landmark==landmark,"empty yard source "+landmark);
                    }
                    break;
                case "NavigationParallelJobsAndStorage":
                    m.State.money=100;m.BuyMachine();m.AcquireWire();m.FeedMachine();m.AcquireFan();Route(m,YardLandmark.FanBench);
                    m.LoadFan();m.InspectFan();m.BeginFanRepair();Route(m,YardLandmark.FanBench);
                    Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("[Mouse4]"),"powered work cannot hide hand job");
                    m.Tick(m.MachineSeconds);m.CollectMachine();m.Sell();Route(m,YardLandmark.FanBench);
                    while(m.State.fanStage==FanStage.Repairing)m.WorkFan();m.TestFan();m.CollectFan();Route(m,YardLandmark.Buyer);m.Sell();
                    m.AcquireWire();m.Store(MaterialKind.Wire);Route(m,YardLandmark.WireStorage);m.Retrieve(MaterialKind.Wire);m.LoadBench();
                    while(m.State.benchLoaded)m.WorkBench();m.CollectBench();m.Store(MaterialKind.Copper);m.AcceptOrder();Route(m,YardLandmark.CopperStorage);
                    m.Retrieve(MaterialKind.Copper);Route(m,YardLandmark.Orders);break;
                case "NavigationSelectionIsReadOnly":
                    var navigator=new YardNavigator();m.AcquireWire();int id=m.Carried.id;
                    navigator.Select(YardLandmark.FanSupply);
                    for(int i=0;i<100;i++)
                    {
                        var point=navigator.Resolve(m,i-50,i-50);point.Direction(i-50,i-50,i*90);point.Distance(i-50,i-50);
                        Check(point.landmark==YardLandmark.FanSupply,"explicit destination remains tracked");
                    }
                    Check(m.Carried.id==id && m.State.items.Count==1 && m.State.money==0,"navigation cannot move/spend material");
                    bool rejected=false;try{navigator.Select((YardLandmark)999);}catch(ArgumentOutOfRangeException){rejected=true;}
                    Check(rejected && navigator.Selected==YardLandmark.FanSupply,"invalid selection preserves route");
                    navigator.Select(YardLandmark.Automatic);Check(navigator.Resolve(m,0,0).landmark==YardLandmark.Bench,"automatic restored");break;
                case "GuidanceIdentifiesFanMaterials":
                    m.State.money=100;m.AcquireFan();
                    foreach(var target in new[]{TargetKind.Bench,TargetKind.Machine})
                    { var hint=YardGuidance.Hint(m,target,0,"F","Mouse4"); if(target==TargetKind.Machine){m.BuyMachine();hint=YardGuidance.Hint(m,target,0,"F","Mouse4");} Check(!hint.canUse && hint.text.Contains("broken fan") && hint.text.Contains("restoration"),"broken fan hint"); }
                    m.LoadFan();m.InspectFan();m.BeginFanRepair();while(m.State.fanStage==FanStage.Repairing)m.WorkFan();m.TestFan();m.CollectFan();
                    foreach(var target in new[]{TargetKind.Bench,TargetKind.Machine})
                    { var hint=YardGuidance.Hint(m,target,0,"F","Mouse4");Check(!hint.canUse && hint.text.Contains("fan is tested") && hint.text.Contains("buyer"),"tested fan hint"); }
                    break;
                case "GuidanceRetainsBlockedFanOutput":
                    m.AcquireFan();m.LoadFan();m.InspectFan();m.BeginFanDismantle();while(m.State.fanStage==FanStage.Dismantling)m.WorkFan();
                    m.State.nextId=int.MaxValue;
                    var blocked=YardGuidance.Hint(m,TargetKind.FanBench,0,"F","Mouse4");
                    Check(!blocked.canUse && blocked.text.Contains("retained safely") && !blocked.text.Contains("[F] collect"),"no impossible collection prompt");
                    Check(!m.CollectFan() && m.State.fanStage==FanStage.CopperReady,"protected output");
                    m.State.nextId=1;m.AcquireWire();
                    Check(!YardGuidance.Hint(m,TargetKind.FanBench,0,"F","Mouse4").canUse,"full hands block collection");break;
                default: throw new Exception("Unknown navigation scenario");
            }
        }
    }
}
