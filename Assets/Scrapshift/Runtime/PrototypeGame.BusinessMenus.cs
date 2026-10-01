using UnityEngine;
namespace Scrapshift
{
    public sealed partial class PrototypeGame
    {
        bool dayReportOpen, reportStartsNewDay;
        void DrawOrders(float width,float height)
        {
            var panel=FrontPanel(width,height,"CUSTOMER REQUESTS & STORAGE",800,730);
            var copper=Model.CurrentOrder;var appliance=Model.CurrentCommission;
            DrawRequestCard(panel,42,"COPPER / "+copper.customer+" / "+copper.title,copper.note,
                Model.State.orderDelivered+" / "+copper.copper+" copper • €"+copper.reward+" on completion",
                Model.State.orderAccepted,Model.State.orderIndex<int.MaxValue,false);
            DrawRequestCard(panel,278,"RESTORATION / "+appliance.customer+" / "+appliance.title,appliance.note,
                Model.State.commissionDelivered+" / "+appliance.quantity+" "+appliance.ItemName+" • €"+Model.CommissionReward+" on completion",
                Model.State.commissionAccepted,Model.State.commissionIndex<int.MaxValue,true);
            GUI.Label(new Rect(panel.x+25,panel.y+522,panel.width-50,93),
                "No deadlines or daily fees. Carry matching items to the CUSTOMER BOARD ["+controls.Label(ControlAction.Interact)+"]. Deliveries are kept until a request is complete.\nCompleted: "+Model.State.orderIndex+" copper / "+Model.State.commissionIndex+" restoration requests.",controlLegend);
            GUI.Label(new Rect(panel.x+25,panel.y+620,panel.width-50,50),
                "Storage: "+Model.StoredBundles(MaterialKind.Wire)+" wire bundles / "+Model.StoredQuantity(MaterialKind.Copper)+" copper units • Yard "+Model.OccupiedBundles+"/"+Model.Capacity+" slots\nUse the east-side bins to store carried material or retrieve with empty hands.",controlLegend);
            if(GUI.Button(new Rect(panel.x+25,panel.yMax-55,panel.width-50,40),"Back / Escape"))ordersOpen=false;
        }
        void DrawRequestCard(Rect panel,float top,string title,string note,string progress,bool accepted,bool available,bool restoration)
        {
            float x=panel.x+20,y=panel.y+top,w=panel.width-40;
            GUI.Box(new Rect(x,y,w,220),"");
            GUI.Label(new Rect(x+14,y+8,w-28,51),title,wrappedLabel);
            GUI.Label(new Rect(x+14,y+66,w-28,58),note,controlLegend);
            GUI.Label(new Rect(x+14,y+130,w-28,30),progress,wrappedLabel);
            GUI.enabled=!accepted && available;
            string caption=accepted?"Accepted / bring matching items to the board":available?"Accept this request":"No further requests available";
            if(GUI.Button(new Rect(x+14,y+172,w-28,36),caption))
            {
                bool changed=restoration?Model.AcceptCommission():Model.AcceptOrder();
                if(changed)
                {
                    SyncViews();Save();SetPaused(false);Beep();
                    Tell(restoration?Model.CurrentCommission.customer+" request accepted. Bring "+Model.CurrentCommission.quantity+" "+Model.CurrentCommission.ItemName+" for €"+Model.CommissionReward+". No rush.":
                        Model.CurrentOrder.customer+" request accepted. Bring "+Model.CurrentOrder.copper+" copper for €"+Model.CurrentOrder.reward+". No rush.");
                }
            }
            GUI.enabled=true;
        }
        void DrawDay(float width,float height)
        {
            var panel=FrontPanel(width,height,"YARD DIARY / DAY "+((long)Model.State.dayIndex+1),720,710);
            float w=panel.width;
            if(GUI.Button(new Rect(panel.x+20,panel.y+42,(w-50)/2,35),"Day review"))investmentsOpen=false;
            if(GUI.Button(new Rect(panel.center.x+5,panel.y+42,(w-50)/2,35),"Invest in the yard"))investmentsOpen=true;
            if(investmentsOpen){DrawInvestments(panel);return;}
            DrawReportFigures(panel,Model.Today,94);
            GUI.Label(new Rect(panel.x+25,panel.y+421,w-50,56),"Fresh salvage tomorrow: "+Model.Rules.fanDailyLimit+" fans / "+Model.Rules.radioDailyLimit+" radios.\nStill available today: "+Mathf.Max(0,Model.Rules.fanDailyLimit-Model.State.fansTakenToday)+" fans / "+Mathf.Max(0,Model.Rules.radioDailyLimit-Model.State.radiosTakenToday)+" radios.",controlLegend);
            GUI.enabled=Model.State.hasLastDayReport;
            if(GUI.Button(new Rect(panel.x+25,panel.y+486,w-50,36),"Review last finished day"))
            {dayOpen=false;dayReportOpen=true;reportStartsNewDay=false;}
            GUI.enabled=true;
            GUI.Label(new Rect(panel.x+25,panel.y+534,w-50,60),"Returning tomorrow keeps items, hand jobs and requests, and finishes the current powered load. No rent or extra charges.",controlLegend);
            GUI.enabled=Model.State.dayIndex<int.MaxValue;
            if(GUI.Button(new Rect(panel.x+25,panel.y+601,w-50,40),"Finish day / return on day "+((long)Model.State.dayIndex+2)))
            {
                if(Model.AdvanceDay())
                {SyncViews();Save();dayOpen=false;dayReportOpen=true;reportStartsNewDay=true;Beep(1.3f);}
            }
            GUI.enabled=true;
            if(GUI.Button(new Rect(panel.x+25,panel.yMax-55,w-50,40),"Back / Escape"))dayOpen=false;
        }
        void DrawReportFigures(Rect panel,YardDayReport report,float top)
        {
            float x=panel.x+25,y=panel.y+top,w=panel.width-50;
            GUI.Label(new Rect(x,y,w,135),
                "Sales and request income: €"+report.income+"\nParts bought: €"+report.partsSpent+" / Equipment installed: €"+report.equipmentSpent+"\nCash on hand: €"+report.closingCash+
                (report.detailsComplete?"\nOpening cash: €"+report.openingCash+" / Cash change: "+(report.CashChange>=0?"+":"−")+"€"+System.Math.Abs((long)report.CashChange):""),wrappedLabel);
            GUI.Label(new Rect(x,y+146,w,128),
                "WORK COMPLETED\nWire loads stripped: "+report.wireLoads+" / Copper sold: "+report.copperSold+" / Delivered: "+report.copperDelivered+"\nAppliances tested: "+report.appliancesTested+" / Dismantled: "+report.appliancesSalvaged+"\nTested appliances sold: "+report.appliancesSold+" / Delivered: "+report.appliancesDelivered+"\nRequests completed: "+report.copperOrders+" copper / "+report.restorationOrders+" restoration",controlLegend);
            if(!report.detailsComplete || report.totalsCapped)
                GUI.Label(new Rect(x,y+276,w,49),!report.detailsComplete?"Earlier work and costs for this day are unavailable. The next day will have a full report.":"Some displayed totals reached their limit. Cash on hand and cash change still reflect the actual balance.",controlLegend);
        }
        void DrawDayReport(float width,float height)
        {
            var report=Model.LastDay;
            var panel=FrontPanel(width,height,"DAY "+((long)report.dayIndex+1)+" / WORK RECEIPT",720,650);
            GUI.Label(new Rect(panel.x+25,panel.y+47,panel.width-50,42),reportStartsNewDay?"The yard is ready for day "+((long)Model.State.dayIndex+1)+". Thanks for giving old things another life.":"Your last finished day. This receipt stays available after saving.",wrappedLabel);
            DrawReportFigures(panel,report,104);
            GUI.Label(new Rect(panel.x+25,panel.y+445,panel.width-50,110),
                "Fresh fans and radios arrive each morning. Your inventory, partial repairs and accepted requests stay with you. The powered stripper's overnight load is counted in this finished day.\n\nCopper and appliance requests offer different ways to help neighbours and earn your next yard improvement.",controlLegend);
            if(GUI.Button(new Rect(panel.x+25,panel.yMax-60,panel.width-50,42),reportStartsNewDay?"Open the yard / Escape":"Back to diary / Escape"))CloseDayReportMenu();
        }
        void CloseDayReportMenu()
        {
            dayReportOpen=false;
            if(reportStartsNewDay)
            {SetPaused(false);Tell("A fresh day at the yard. Fans and radios have arrived at salvage.");}
            else {dayOpen=true;investmentsOpen=false;}
        }
    }
}
