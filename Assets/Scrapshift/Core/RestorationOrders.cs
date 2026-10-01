using System;
namespace Scrapshift
{
    public sealed class RestorationOrder
    {
        public readonly string customer, title, note;
        public readonly MaterialKind kind;
        public readonly int quantity;
        public string ItemName { get { return kind==MaterialKind.RestoredRadio?"tested radio":"tested fan"; } }
        public RestorationOrder(string customer,string title,string note,MaterialKind kind,int quantity)
        { this.customer=customer;this.title=title;this.note=note;this.kind=kind;this.quantity=quantity; }
    }
    // Independent from the unchanged five copper orders, preserving every old order identifier.
    public static class RestorationOrders
    {
        public const int MaxReward=40000;
        static readonly RestorationOrder[] Orders=
        {
            new RestorationOrder("Marta's Cafe","A breeze for the counter","An old desk fan would keep our little counter comfortable. Please test it before bringing it over.",MaterialKind.RestoredFan,1),
            new RestorationOrder("Rowan's Garage","Company in the workshop","A working portable radio would brighten long afternoons in the garage.",MaterialKind.RestoredRadio,1),
            new RestorationOrder("Neighbourhood Hall","Something for the summer","We could use two reliable fans for our community room. Separate deliveries are welcome.",MaterialKind.RestoredFan,2),
            new RestorationOrder("Local Repair Club","Radios with another story","Two tested radios for our shared workshop benches. Take as many yard days as you need.",MaterialKind.RestoredRadio,2)
        };
        public static RestorationOrder At(int completed)
        { if(completed<0)throw new ArgumentOutOfRangeException("completed");return Orders[completed%Orders.Length]; }
        public static int Quote(RestorationOrder order,YardRules rules)
        { return ((order.kind==MaterialKind.RestoredFan?rules.fanSalePrice:rules.radioSalePrice)+rules.commissionBonusPerItem)*order.quantity; }
        public static void Validate(YardState s)
        {
            if(s.commissionIndex<0 || s.commissionDelivered<0 || s.commissionReward<0 || s.commissionReward>MaxReward ||
                (!s.commissionAccepted && (s.commissionDelivered!=0 || s.commissionReward!=0)) ||
                (s.commissionAccepted && (s.commissionIndex==int.MaxValue || s.commissionReward==0 || s.commissionDelivered>=At(s.commissionIndex).quantity)))
                throw new ArgumentException("Inconsistent restoration request.");
        }
    }
    public sealed partial class YardModel
    {
        public RestorationOrder CurrentCommission { get { return RestorationOrders.At(State.commissionIndex); } }
        public int CommissionReward { get { return State.commissionAccepted?State.commissionReward:RestorationOrders.Quote(CurrentCommission,Rules); } }
        public bool CanDeliverCommission { get { return State.commissionAccepted && Carried!=null && Carried.kind==CurrentCommission.kind; } }
        public bool AcceptCommission()
        {
            if(State.commissionAccepted || State.commissionIndex==int.MaxValue)return false;
            State.commissionReward=CommissionReward;State.commissionAccepted=true;return true;
        }
        public bool DeliverCommission()
        {
            if(!CanDeliverCommission)return false;
            bool complete=State.commissionDelivered+1==CurrentCommission.quantity;
            long payment=(long)State.money+State.commissionReward;
            if(complete && payment>int.MaxValue)return false;
            var item=Carried;State.items.Remove(item);State.carriedId=0;
            CountToday(ref State.appliancesDeliveredToday,1);
            if(complete)
            {
                RecordIncome(State.commissionReward);CountToday(ref State.restorationOrdersToday,1);
                State.money=(int)payment;State.commissionIndex++;
                State.commissionAccepted=false;State.commissionDelivered=State.commissionReward=0;
                State.milestones|=YardMilestone.ServedCustomer;
            }
            else State.commissionDelivered++;
            return true;
        }
    }
}
