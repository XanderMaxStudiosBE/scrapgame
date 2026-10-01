using System;
namespace Scrapshift.Tests
{
    public static class DayReportScenarios
    {
        public static readonly string[] Names={"DailyMoneyAndWorkLedger", "RejectedActionsLeaveDailyLedger", "DayClosureCountsOvernightWorkAndRetainsJobs", "LegacyDayDetailStartsAtNextBoundary", "DailyCountersAndReportBounds", "ReceiptInspectionIsReadOnly"};
        static void Check(bool value,string message){BusinessScenarioTools.Check(value,message);}
        public static void Run(string name)
        {
            var m=new YardModel(new YardRules());
            switch(name)
            {
                case "DailyMoneyAndWorkLedger":
                    // Every euro is earned through normal transactions, from zero cash.
                    m.AcceptOrder();BusinessScenarioTools.Copper(m);m.DeliverOrder();
                    BusinessScenarioTools.Copper(m);m.Sell();m.AcceptCommission();BusinessScenarioTools.Repair(m,false);m.DeliverCommission();
                    BusinessScenarioTools.Repair(m,true);m.Sell();m.BuyMachine();m.BuyUpgrade(YardUpgrade.StorageRack);
                    var day=m.Today;
                    Check(day.income==116&&day.partsSpent==14&&day.equipmentSpent==96&&day.closingCash==6&&day.openingCash==0&&day.CashChange==6,"earned income minus actual costs matches cash");
                    Check(day.Spending==110&&day.wireLoads==2&&day.copperDelivered==3&&day.copperSold==3,"wire units and real costs");
                    Check(day.appliancesTested==2&&day.appliancesSold==1&&day.appliancesDelivered==1&&day.copperOrders==1&&day.restorationOrders==1&&day.detailsComplete,"ordinary sales/requests/work separately counted");break;
                case "RejectedActionsLeaveDailyLedger":
                    m.AcceptCommission();m.AcquireFan();m.LoadFan();m.InspectFan();string before=BusinessScenarioTools.Digest(m.State);
                    Check(!m.BeginFanRepair()&&!m.BuyMachine()&&!m.BuyUpgrade(YardUpgrade.HandTools)&&before==BusinessScenarioTools.Digest(m.State),"unaffordable purchases have no costs");
                    m.BeginFanDismantle();while(m.State.fanStage==FanStage.Dismantling)m.WorkFan();m.CollectFan();m.Sell();
                    Check(!m.Sell()&&!m.WorkFan()&&!m.TestFan()&&!m.CollectFan()&&m.Today.appliancesSalvaged==1&&m.Today.copperSold==3&&m.Today.income==12,"no duplicate work/output/income");break;
                case "DayClosureCountsOvernightWorkAndRetainsJobs":
                    m.State.money=100;m.BuyMachine();m.AcquireWire();m.FeedMachine();m.AcquireFan();m.LoadFan();m.InspectFan();m.BeginFanRepair();m.WorkFan();m.AcceptCommission();
                    Check(m.AdvanceDay(),"close real working day");var closed=m.LastDay;
                    Check(closed.dayIndex==0&&closed.wireLoads==1&&closed.equipmentSpent==36&&closed.partsSpent==8&&closed.closingCash==56,"overnight load belongs to closed day");
                    Check(m.Today.income==0&&m.Today.Spending==0&&m.Today.wireLoads==0&&m.Today.openingCash==56&&m.Today.detailsComplete,"fresh daily ledger");
                    Check(m.State.machineOutput==3&&m.State.fanStage==FanStage.Repairing&&m.State.fanStrokes==1&&m.State.commissionAccepted&&m.State.fansTakenToday==0,"jobs/output/request protected");
                    m=new YardModel(m.Rules,BusinessScenarioTools.Copy(m.State));m.CollectMachine();m.Sell();m.AdvanceDay();
                    Check(m.LastDay.dayIndex==1&&m.LastDay.wireLoads==0&&m.LastDay.income==12&&m.LastDay.closingCash==68,"new day sells old output without duplicate processing");
                    Check(closed.closingCash==56&&closed.wireLoads==1,"receipt snapshot immutable");break;
                case "LegacyDayDetailStartsAtNextBoundary":
                    m=new YardModel(new YardRules(),new YardState{money=29,dayIndex=8,incomeToday=47});
                    Check(!m.Today.detailsComplete&&!m.State.hasLastDayReport,"old save has unknown earlier costs/work");
                    m.AdvanceDay();Check(!m.LastDay.detailsComplete&&m.LastDay.income==47&&m.LastDay.closingCash==29,"legacy income/cash retained without invented profit");
                    BusinessScenarioTools.Copper(m);m.Sell();m.AdvanceDay();
                    Check(m.LastDay.detailsComplete&&m.LastDay.openingCash==29&&m.LastDay.closingCash==41&&m.LastDay.CashChange==12,"following day complete and reconciled");break;
                case "DailyCountersAndReportBounds":
                    m.State.incomeToday=int.MaxValue-1;m.State.wireLoadsToday=int.MaxValue;m.State.copperSoldToday=int.MaxValue;
                    BusinessScenarioTools.Copper(m);m.Sell();
                    Check(m.Today.income==int.MaxValue&&m.Today.wireLoads==int.MaxValue&&m.Today.copperSold==int.MaxValue&&m.Today.totalsCapped&&m.State.money==12,"counters saturate without cash inflation");
                    m.State.partsSpentToday=int.MaxValue;m.State.equipmentSpentToday=int.MaxValue;Check(m.Today.Spending==4294967294L,"combined costs use wide sum");
                    m.AdvanceDay();Check(m.LastDay.totalsCapped&&!m.Today.totalsCapped,"saved cap notice resets next day");
                    BusinessScenarioTools.Reject(new YardState{partsSpentToday=-1});BusinessScenarioTools.Reject(new YardState{appliancesDeliveredToday=-1});
                    BusinessScenarioTools.Reject(new YardState{lastDayAppliancesDelivered=-1});BusinessScenarioTools.Reject(new YardState{hasLastDayReport=true,dayIndex=1,lastDayIndex=1});
                    m.State.dayIndex=int.MaxValue;string digest=BusinessScenarioTools.Digest(m.State);Check(!m.AdvanceDay()&&digest==BusinessScenarioTools.Digest(m.State),"day overflow does not close/reward twice");break;
                case "ReceiptInspectionIsReadOnly":
                    m.State.money=100;m.State.commissionIndex=2;m.AcceptCommission();BusinessScenarioTools.Repair(m,false);m.DeliverCommission();m.AdvanceDay();
                    string state=BusinessScenarioTools.Digest(m.State);for(int i=0;i<100;i++)
                    {
                        var receipt=m.LastDay;var today=m.Today;
                        Check(receipt.appliancesDelivered==1&&receipt.restorationOrders==0&&receipt.income==0&&today.income==0,"partial delivery gives work credit without early payment");
                    }
                    Check(state==BusinessScenarioTools.Digest(m.State)&&m.State.commissionDelivered==1,"review cannot mutate escrow, finances or inventory");break;
                default:throw new Exception(name);
            }
            YardModel.Validate(m.State);
        }
    }
}
