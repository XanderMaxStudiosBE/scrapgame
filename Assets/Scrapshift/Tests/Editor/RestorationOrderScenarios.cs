using System;
using System.Collections.Generic;
using System.Text;
namespace Scrapshift.Tests
{
    internal static class BusinessScenarioTools
    {
        public static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static YardState Copy(YardState state)
        {
            var copy=new YardState();
            foreach(var field in typeof(YardState).GetFields())if(field.Name!="items")field.SetValue(copy,field.GetValue(state));
            copy.items=new List<ScrapItem>();
            foreach(var item in state.items)
            {
                var next=new ScrapItem();foreach(var field in typeof(ScrapItem).GetFields())field.SetValue(next,field.GetValue(item));copy.items.Add(next);
            }
            return copy;
        }
        public static string Digest(YardState state)
        {
            var text=new StringBuilder();
            foreach(var field in typeof(YardState).GetFields())if(field.Name!="items")text.Append(field.Name).Append('=').Append(field.GetValue(state)).Append(';');
            foreach(var item in state.items)foreach(var field in typeof(ScrapItem).GetFields())text.Append(field.GetValue(item)).Append(';');
            return text.ToString();
        }
        public static void Repair(YardModel m,bool radio)
        {
            Check((radio?m.AcquireRadio():m.AcquireFan())&&m.LoadFan()&&m.InspectFan()&&m.BeginFanRepair(),"acquire and pay for repair");
            while(m.State.fanStage==FanStage.Repairing)Check(m.WorkFan(),"repair work");
            Check(m.TestFan()&&m.CollectFan(),"explicit test and collect");
        }
        public static void Copper(YardModel m)
        {
            Check(m.AcquireWire()&&m.LoadBench(),"load renewable wire");
            while(m.State.benchLoaded)Check(m.WorkBench(),"strip wire");
            Check(m.CollectBench(),"collect copper");
        }
        public static void Reject(YardState s)
        {try{YardModel.Validate(s);}catch(ArgumentException){return;}throw new Exception("Invalid state accepted");}
    }
    public static class RestorationOrderScenarios
    {
        public static readonly string[] Names={"FanRequestPaysOnce", "RadioAndSeparateFanDeliveries", "RequestGuardsPreserveItems", "AcceptedRewardSurvivesBalanceChanges", "RequestOverflowRetainsFinalAppliance", "CopperAndRestorationResumeIndependently", "RequestGuidanceUsesActualBindings", "RequestBoundsAndBalanceMigration"};
        static void Check(bool value,string message){BusinessScenarioTools.Check(value,message);}
        public static void Run(string name)
        {
            var m=new YardModel(new YardRules());
            switch(name)
            {
                case "FanRequestPaysOnce":
                    m.State.money=100;Check(m.AcceptCommission()&&!m.AcceptCommission()&&m.CommissionReward==52,"accept without payment");
                    BusinessScenarioTools.Repair(m,false);Check(m.State.money==92&&m.DeliverCommission(),"deliver tested fan");
                    Check(!m.DeliverCommission()&&m.State.money==144&&m.State.items.Count==0&&m.State.commissionIndex==1,"payout/item exactly once");
                    Check(m.Today.income==52&&m.Today.restorationOrders==1&&m.Today.appliancesDelivered==1&&m.Today.appliancesSold==0,"request receipt separate from ordinary resale");break;
                case "RadioAndSeparateFanDeliveries":
                    m.State.money=100;m.State.commissionIndex=1;m.AcceptCommission();BusinessScenarioTools.Repair(m,true);m.DeliverCommission();
                    Check(m.State.money==138&&m.State.commissionIndex==2,"radio earns premium");m.AcceptCommission();
                    BusinessScenarioTools.Repair(m,false);int cash=m.State.money;Check(m.DeliverCommission()&&m.State.money==cash&&m.State.commissionDelivered==1,"first fan retained in request without payment");
                    m.AdvanceDay();m=new YardModel(m.Rules,BusinessScenarioTools.Copy(m.State));
                    Check(m.State.commissionAccepted&&m.CommissionReward==104&&m.State.commissionDelivered==1,"no expiry across days");
                    BusinessScenarioTools.Repair(m,false);cash=m.State.money;Check(m.DeliverCommission()&&!m.DeliverCommission()&&m.State.money==cash+104,"only final delivery pays");break;
                case "RequestGuardsPreserveItems":
                    m=new YardModel(new YardRules{maxBundles=1});m.State.money=100;m.AcquireFan();int id=m.Carried.id;
                    Check(!m.DeliverCommission()&&m.AcceptCommission()&&!m.DeliverCommission()&&m.Carried.id==id,"broken fan never taken");
                    Check(!m.AcquireRadio()&&!m.AcquireWire(),"full hands/capacity protected");m.LoadFan();m.InspectFan();m.BeginFanRepair();while(m.State.fanStage==FanStage.Repairing)m.WorkFan();m.TestFan();m.CollectFan();
                    Check(!m.AcquireWire()&&m.DeliverCommission()&&m.AcquireRadio(),"delivery frees exactly one reserved slot");
                    m.LoadFan();m.InspectFan();m.BeginFanRepair();while(m.State.fanStage==FanStage.Repairing)m.WorkFan();m.TestFan();m.CollectFan();
                    id=m.Carried.id;Check(!m.DeliverCommission()&&m.Carried.id==id,"no active request retains radio");
                    m.State.commissionIndex=0;m.AcceptCommission();Check(!m.DeliverCommission()&&m.Carried.id==id,"wrong appliance retains radio");break;
                case "AcceptedRewardSurvivesBalanceChanges":
                    m.State.money=100;m.AcceptCommission();BusinessScenarioTools.Repair(m,false);
                    m=new YardModel(new YardRules{fanSalePrice=100,commissionBonusPerItem=25},BusinessScenarioTools.Copy(m.State));
                    Check(m.CommissionReward==52&&m.DeliverCommission()&&m.State.money==144,"agreed quote retained after rules change");
                    Check(m.CommissionReward==59,"next available radio uses new bonus");break;
                case "RequestOverflowRetainsFinalAppliance":
                    m.State.money=100;m.AcceptCommission();BusinessScenarioTools.Repair(m,false);m.State.money=int.MaxValue-51;
                    string before=BusinessScenarioTools.Digest(m.State);
                    Check(!m.DeliverCommission()&&before==BusinessScenarioTools.Digest(m.State),"failed payment entirely nonmutating");
                    Check(!YardGuidance.Hint(m,TargetKind.OrderBoard,0,"F","Mouse4").canUse,"hint agrees with guard");
                    m.State.money=int.MaxValue-52;Check(m.DeliverCommission()&&m.State.money==int.MaxValue,"exact balance limit pays");
                    m=new YardModel(new YardRules());m.State.money=100;m.State.commissionIndex=2;m.AcceptCommission();BusinessScenarioTools.Repair(m,false);
                    m.State.money=int.MaxValue;Check(m.DeliverCommission()&&m.State.commissionDelivered==1,"nonfinal delivery does not try to pay");
                    BusinessScenarioTools.Repair(m,false);m.State.money=int.MaxValue-103;before=BusinessScenarioTools.Digest(m.State);
                    Check(!m.DeliverCommission()&&before==BusinessScenarioTools.Digest(m.State),"final delivery overflow retains earlier escrow and held fan");
                    m.State.money=int.MaxValue-104;Check(m.DeliverCommission()&&m.State.money==int.MaxValue,"two-item final payout at exact limit");break;
                case "CopperAndRestorationResumeIndependently":
                    m.State.money=100;m.State.orderIndex=1;m.State.commissionIndex=2;m.AcceptOrder();m.AcceptCommission();
                    BusinessScenarioTools.Copper(m);m.DeliverOrder();BusinessScenarioTools.Repair(m,false);m.DeliverCommission();
                    m=new YardModel(m.Rules,BusinessScenarioTools.Copy(m.State));
                    Check(m.State.orderDelivered==3&&m.State.commissionDelivered==1&&m.State.orderAccepted&&m.State.commissionAccepted,"both escrows resume");
                    BusinessScenarioTools.Copper(m);m.DeliverOrder();Check(m.State.orderIndex==2&&m.State.commissionIndex==2,"old copper IDs/reward unchanged");
                    BusinessScenarioTools.Repair(m,false);m.DeliverCommission();Check(m.State.money==226&&m.State.commissionIndex==3&&m.Today.copperOrders==1&&m.Today.restorationOrders==1,"independent exactly-once payments");break;
                case "RequestGuidanceUsesActualBindings":
                    m.AcceptCommission();Check(YardNavigation.Recommend(m,0,0).landmark==YardLandmark.FanSupply,"fan request sends to western source");
                    m.State.money=100;BusinessScenarioTools.Repair(m,false);
                    Check(YardNavigation.Recommend(m,0,0).landmark==YardLandmark.Orders&&YardGuidance.Objective(m,"F","Mouse4","R").Contains("CUSTOMER BOARD [F]"),"matching tested item goes to customer");
                    Check(YardGuidance.Hint(m,TargetKind.OrderBoard,0,"F","Mouse4").text.Contains("[F] deliver tested fan"),"actual interact binding");m.DeliverCommission();m.AcceptCommission();
                    Check(YardNavigation.Recommend(m,0,0).landmark==YardLandmark.RadioSupply,"radio request sends east");m.State.radiosTakenToday=1;
                    Check(YardNavigation.Recommend(m,0,0).landmark==YardLandmark.Diary&&YardGuidance.Objective(m,"F","Mouse4","R").Contains("[F]"),"empty stock retains relaxed request");
                    Check(YardGuidance.Hint(m,TargetKind.OrderBoard,0,"F","Mouse4").canUse,"empty hands can view and accept other request");break;
                case "RequestBoundsAndBalanceMigration":
                    BusinessScenarioTools.Reject(new YardState{commissionIndex=-1});BusinessScenarioTools.Reject(new YardState{commissionDelivered=1});
                    BusinessScenarioTools.Reject(new YardState{commissionAccepted=true});BusinessScenarioTools.Reject(new YardState{commissionAccepted=true,commissionReward=52,commissionDelivered=1});
                    BusinessScenarioTools.Reject(new YardState{commissionAccepted=true,commissionReward=52,commissionIndex=int.MaxValue});
                    BusinessScenarioTools.Reject(new YardState{commissionAccepted=true,commissionReward=40001});
                    m.State.commissionIndex=int.MaxValue;Check(!m.AcceptCommission(),"request sequence cannot overflow");
                    var r=new YardRules{commissionBonusPerItem=0,fanSalePrice=50};Check(BalanceMigration.FillMissingExtensions(r)&&r.commissionBonusPerItem==10&&r.fanSalePrice==50,"only zero missing bonus filled");
                    r.commissionBonusPerItem=17;Check(!BalanceMigration.FillMissingExtensions(r),"positive custom bonus preserved");
                    m=new YardModel(new YardRules{fanSalePrice=10000,commissionBonusPerItem=10000});m.State.commissionIndex=2;m.AcceptCommission();Check(m.CommissionReward==RestorationOrders.MaxReward,"maximum validated quote bounded");
                    r.commissionBonusPerItem=-1;try{r.Validate();throw new Exception("negative bonus accepted");}catch(ArgumentException){}break;
                default:throw new Exception(name);
            }
            YardModel.Validate(m.State);
        }
    }
}
