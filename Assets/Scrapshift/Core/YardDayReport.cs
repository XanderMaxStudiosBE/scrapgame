using System;
namespace Scrapshift
{
    public struct YardDayReport
    {
        public readonly int dayIndex,income,openingCash,closingCash,partsSpent,equipmentSpent;
        public readonly int wireLoads,copperSold,copperDelivered,appliancesTested,appliancesSalvaged,appliancesSold,appliancesDelivered,copperOrders,restorationOrders;
        public readonly bool detailsComplete,totalsCapped;
        public long Spending { get { return (long)partsSpent+equipmentSpent; } }
        public int CashChange { get { return closingCash-openingCash; } }
        internal YardDayReport(int day,int income,int opening,int closing,int parts,int equipment,
            int wire,int soldCopper,int deliveredCopper,int tested,int salvaged,int soldAppliances,int deliveredAppliances,int copperOrders,int restorationOrders,bool complete,bool capped)
        {
            dayIndex=day;this.income=income;openingCash=opening;closingCash=closing;partsSpent=parts;equipmentSpent=equipment;
            wireLoads=wire;copperSold=soldCopper;copperDelivered=deliveredCopper;appliancesTested=tested;appliancesSalvaged=salvaged;
            appliancesSold=soldAppliances;appliancesDelivered=deliveredAppliances;this.copperOrders=copperOrders;this.restorationOrders=restorationOrders;detailsComplete=complete;totalsCapped=capped;
        }
        public static void Validate(YardState s)
        {
            if(s.dayOpeningCash<0 || s.partsSpentToday<0 || s.equipmentSpentToday<0 || s.wireLoadsToday<0 || s.copperSoldToday<0 || s.copperDeliveredToday<0 ||
                s.appliancesTestedToday<0 || s.appliancesSalvagedToday<0 || s.appliancesSoldToday<0 || s.appliancesDeliveredToday<0 || s.copperOrdersToday<0 || s.restorationOrdersToday<0 ||
                s.lastDayIndex<0 || s.lastDayIncome<0 || s.lastDayOpeningCash<0 || s.lastDayClosingCash<0 || s.lastDayPartsSpent<0 || s.lastDayEquipmentSpent<0 ||
                s.lastDayWireLoads<0 || s.lastDayCopperSold<0 || s.lastDayCopperDelivered<0 || s.lastDayAppliancesTested<0 || s.lastDayAppliancesSalvaged<0 ||
                s.lastDayAppliancesSold<0 || s.lastDayAppliancesDelivered<0 || s.lastDayCopperOrders<0 || s.lastDayRestorationOrders<0 ||
                (s.hasLastDayReport && s.lastDayIndex>=s.dayIndex))throw new ArgumentException("Invalid day report.");
        }
    }
    public sealed partial class YardModel
    {
        public YardDayReport Today { get { return new YardDayReport(State.dayIndex,State.incomeToday,State.dayOpeningCash,State.money,State.partsSpentToday,State.equipmentSpentToday,
            State.wireLoadsToday,State.copperSoldToday,State.copperDeliveredToday,State.appliancesTestedToday,State.appliancesSalvagedToday,State.appliancesSoldToday,State.appliancesDeliveredToday,State.copperOrdersToday,State.restorationOrdersToday,State.dailyDetailsComplete,State.dailyTotalsCapped); } }
        public YardDayReport LastDay
        {
            get
            {
                if(!State.hasLastDayReport)throw new InvalidOperationException("No day has been closed yet.");
                return new YardDayReport(State.lastDayIndex,State.lastDayIncome,State.lastDayOpeningCash,State.lastDayClosingCash,State.lastDayPartsSpent,State.lastDayEquipmentSpent,
                    State.lastDayWireLoads,State.lastDayCopperSold,State.lastDayCopperDelivered,State.lastDayAppliancesTested,State.lastDayAppliancesSalvaged,State.lastDayAppliancesSold,State.lastDayAppliancesDelivered,State.lastDayCopperOrders,State.lastDayRestorationOrders,State.lastDayDetailsComplete,State.lastDayTotalsCapped);
            }
        }
        void CountToday(ref int count,int amount)
        {
            long sum=(long)count+amount;
            if(sum>int.MaxValue)State.dailyTotalsCapped=true;
            count=(int)Math.Min(int.MaxValue,sum);
        }
        void RecordIncome(int amount)
        { CountToday(ref State.incomeToday,amount);State.milestones|=YardMilestone.EarnedIncome; }
        void RecordExpense(int amount,bool equipment)
        { if(equipment)CountToday(ref State.equipmentSpentToday,amount);else CountToday(ref State.partsSpentToday,amount); }
        void CloseDayReport()
        {
            State.hasLastDayReport=true;State.lastDayIndex=State.dayIndex;State.lastDayIncome=State.incomeToday;
            State.lastDayOpeningCash=State.dayOpeningCash;State.lastDayClosingCash=State.money;
            State.lastDayPartsSpent=State.partsSpentToday;State.lastDayEquipmentSpent=State.equipmentSpentToday;
            State.lastDayWireLoads=State.wireLoadsToday;State.lastDayCopperSold=State.copperSoldToday;State.lastDayCopperDelivered=State.copperDeliveredToday;
            State.lastDayAppliancesTested=State.appliancesTestedToday;State.lastDayAppliancesSalvaged=State.appliancesSalvagedToday;State.lastDayAppliancesSold=State.appliancesSoldToday;State.lastDayAppliancesDelivered=State.appliancesDeliveredToday;
            State.lastDayCopperOrders=State.copperOrdersToday;State.lastDayRestorationOrders=State.restorationOrdersToday;
            State.lastDayDetailsComplete=State.dailyDetailsComplete;State.lastDayTotalsCapped=State.dailyTotalsCapped;
            State.dayOpeningCash=State.money;State.incomeToday=State.partsSpentToday=State.equipmentSpentToday=0;
            State.wireLoadsToday=State.copperSoldToday=State.copperDeliveredToday=State.appliancesTestedToday=State.appliancesSalvagedToday=State.appliancesSoldToday=State.appliancesDeliveredToday=State.copperOrdersToday=State.restorationOrdersToday=0;
            State.dailyDetailsComplete=true;State.dailyTotalsCapped=false;
        }
    }
}
