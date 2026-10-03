using UnityEngine;

namespace Scrapshift.Compact
{
    public sealed partial class CompactYardGame
    {
        int requestConfirmId,requestConfirmQuantity,requestConfirmTotal;
        void ResetRequestReview(){requestConfirmId=0;}

        void DrawJournal()
        {
            var career=Model.Career;var goals=career.Goals;int finished=0;
            foreach(var goal in goals)if(goal.complete)finished++;
            Section("THE STARTER YARD",career.Completed?
                "You have turned a little receiving yard into a working scrapyard. This chapter is complete; the yard is still yours to grow.":
                "Small jobs become a steady workshop. Follow the next step or take a detour; your journal keeps track as you work.");
            Progress(goals.Length==0?1:(float)finished/goals.Length,finished+" / "+goals.Length+" journal milestones");
            var next=career.CurrentGoal;
            if(next!=null)
            {
                BeginCard("Next up / "+next.title);Text(next.detail);
                GUILayout.Label("["+controls.Label(ControlAction.Interact)+"] inspect / use  •  ["+
                    controls.Label(ControlAction.ManualWork)+"] work  •  ["+controls.Label(ControlAction.BuildToggle)+"] equipment",small);
                EndCard();
            }
            else DrawChapterRecap();
            Section("THE WORK YOU HAVE DONE");
            foreach(var goal in goals)
            {
                BeginCard((goal.complete?"DONE  /  ":"TO DO  /  ")+goal.title);
                Text(goal.detail);EndCard();
            }
            Section("NEIGHBOURS AT THE COUNTER");
            var request=career.CurrentContract;
            Text(request==null?"Every current neighbourhood request is complete. Your recovered materials can still be sold at the counter.":
                request.customer+" needs "+Model.Rules.Part(request.kind).name+". "+request.delivered+" / "+request.required+" delivered; €"+request.bonus+" extra on the final delivery.");
            if(Button("Open neighbourhood requests"))Show(Page.Contracts);
            if(Button("Equipment catalogue"))Show(Page.Catalogue);
        }

        void DrawChapterRecap()
        {
            var stats=Model.Career.Stats;
            BeginCard("A yard that works for you");
            Text("You learned to dismantle, recover, sell and build. Keep experimenting with your layout, finish requests and make your next production line a little smarter.");
            GUILayout.Label("RECORDED IN YOUR JOURNAL",eyebrow);
            Text(stats.dismantledObjects+" objects dismantled  •  "+stats.manualBatches+" hand-worked loads  •  "+stats.poweredBatches+" powered loads\n"+
                stats.materialUnitsSold+" material units sold  •  €"+stats.salesRevenue+" sale income\n"+
                stats.contractsCompleted+" customer requests finished  •  €"+stats.contractBonuses+" request bonuses");
            if(Primary("Keep building my yard"))
            {
                if(Model.Career.AcknowledgeCompletion())Save();
                page=Page.None;Pause(false);
            }
            EndCard();
        }

        void DrawContracts()
        {
            Section("GOOD MATERIALS, CLOSE TO HOME","Neighbours need the useful materials you recover. Deliver a little at a time, or bring a full bundle. There is no acceptance fee or deadline.");
            DrawCustomerOffer(true);
            Section("HOW THE COUNTER WORKS");
            Text("Matching material earns the same sale value and eligible sale XP as a direct sale. The last delivery adds the customer's quoted bonus. Surplus stays in your hands; you can sell it or keep it for the next job.");
            Text("Bring a recovered material bundle to the office sales counter to deliver it. You can check the request and its quote from your journal or Pause. Reviewing or cancelling a quote never consumes anything.");
            GUILayout.Label(Model.Career.Stats.contractsCompleted+" requests completed  •  €"+Model.Career.Stats.contractBonuses+" bonuses earned",small);
        }

        void DrawCustomerOffer(bool expanded)
        {
            var request=Model.Career.CurrentContract;
            if(request==null)
            {
                requestConfirmId=0;
                BeginCard("All neighbourhood requests complete");
                Text("Your neighbours have what they need. Keep selling recovered materials at the counter and building the yard your way.");EndCard();return;
            }
            var quote=Model.Career.ContractQuote();
            if(requestConfirmId!=quote.contractId||requestConfirmQuantity!=quote.quantity||requestConfirmTotal!=quote.total)
                requestConfirmId=0;
            BeginCard(request.customer+" / "+request.name);
            Text(Model.Rules.Part(request.kind).name+" ×"+request.required+" requested  •  Completion bonus €"+request.bonus);
            Progress((float)request.delivered/request.required,request.delivered+" / "+request.required+" delivered  •  "+request.Remaining+" still needed");
            if(Model.Level<request.minimumLevel)
                Text("Opens at level "+request.minimumLevel+". Sell recovered materials to build your reputation.");
            else if(quote.allowed&&CustomerCounterContext)
            {
                Text("Deliver "+quote.quantity+" now for €"+quote.saleTotal+" material value"+
                    (quote.completionBonus>0?" + €"+quote.completionBonus+" completion bonus":"")+".\n"+
                    quote.experience+" sale XP  •  "+(quote.completes?"This finishes the request.":quote.remainingAfter+" more needed afterwards."));
                if(requestConfirmId==quote.contractId)
                {
                    GUILayout.Label("CONFIRM THIS DELIVERY",eyebrow);
                    Text("Use "+quote.quantity+" of your carried "+Model.Rules.Part(quote.kind).name+" and receive €"+quote.total+". Remaining material stays carried.");
                    if(Primary("Confirm delivery / €"+quote.total))
                    {
                        DeliverCustomerRequest();requestConfirmId=0;
                    }
                    if(Button("Cancel / keep my materials"))requestConfirmId=0;
                }
                else if(Button("Review delivery / "+quote.quantity+" units / €"+quote.total))
                {
                    requestConfirmId=quote.contractId;requestConfirmQuantity=quote.quantity;requestConfirmTotal=quote.total;
                }
            }
            else
            {
                if(quote.allowed)
                    Text("Your carried material can deliver "+quote.quantity+" units for €"+quote.total+". Bring it to the office sales counter to confirm the delivery.");
                else Text(quote.reason);
                if(expanded&&Model.Carried!=null)
                    GUILayout.Label("Carried: "+Model.Rules.Part(Model.Carried.kind).name+" ×"+Model.Carried.quantity,small);
            }
            EndCard();
        }

        void DrawCredits()
        {
            Section("SCRAPSHIFT","An independent first-person scrapyard simulator. Start with your bare hands; build a yard that works for you.");
            BeginCard("The workshop");
            Text("Game design, development and original scrapyard art: the SCRAPSHIFT project. Built with Unity and the Universal Render Pipeline.");
            Text("Original workshop sounds, coarse textures and low-poly props give the yard its worn, quiet character.");EndCard();
            BeginCard("Asset thanks");
            Text("Old Tyre by MP, shared through Poly Haven under CC0. Adapted for the yard's existing tyre piles.");
            Text("Additional asset packs imported on your machine retain their original creators' licenses.");EndCard();
            Section("THANK YOU FOR TAKING A SHIFT","For playing, testing and helping this little scrapyard grow.");
        }
    }
}
