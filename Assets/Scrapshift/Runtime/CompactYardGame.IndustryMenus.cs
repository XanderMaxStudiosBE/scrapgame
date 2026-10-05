using System;
using System.Globalization;
using UnityEngine;

namespace Scrapshift.Compact
{
    public sealed partial class CompactYardGame
    {
        enum IndustryReviewKind { None, Feed, StandingDelivery, AutomaticExport, Dispatch }
        IndustryReviewKind industryReviewKind;
        int industryReviewMachine,industryReviewScrap,industryReviewQuantity,industryReviewTotal,industryReviewXp;
        int industryReviewUnitPrice,industryReviewBonus,industryReviewPrice;
        PartKind industryReviewMaterial;
        ScrapObjectKind industryReviewObject;
        float industryReviewInterval;
        string industryReviewFeedTerms;

        void ResetIndustryReview()
        {
            industryReviewKind=IndustryReviewKind.None;industryReviewMachine=0;industryReviewScrap=0;
            industryReviewFeedTerms=null;
        }
        bool IndustryEquipmentContext(int id)
        {
            if(Model==null||!paused||page!=Page.Equipment||selectedId!=id||settings!=null&&settings.IsOpen)return false;
            var equipment=Model.FindEquipment(id);
            return equipment!=null&&(equipment.kind==EquipmentKind.PrimaryScrapper||equipment.kind==EquipmentKind.ExportStation);
        }
        bool RequireIndustryEquipment(int id)
        {
            if(IndustryEquipmentContext(id))return true;
            ResetIndustryReview();Tell("Inspect this machine in the yard before confirming its work.");return false;
        }
        string IndustryFeedTerms(EquipmentState equipment,LargeScrapJob source)
        {
            var recipe=Model.Rules.LargeRecipe(source.kind);
            string terms=((int)source.kind)+":"+Model.Rules.Equipment(equipment.kind).processingSeconds.ToString("R",CultureInfo.InvariantCulture);
            foreach(var yield in recipe.yields)terms+=":"+(int)yield.kind+"="+yield.quantity;
            return terms;
        }
        bool BeginIndustryReview(IndustryReviewKind kind,int machineId,int scrapId=0)
        {
            ResetIndustryReview();if(!RequireIndustryEquipment(machineId))return false;
            var equipment=Model.FindEquipment(machineId);var state=equipment.industry;
            if(kind==IndustryReviewKind.Feed)
            {
                string reason="This station cannot take a whole object.";
                if(equipment.kind!=EquipmentKind.PrimaryScrapper||!Model.Industry.CanFeedScrap(machineId,scrapId,out reason))
                {Tell(equipment.kind==EquipmentKind.PrimaryScrapper?reason:"This station cannot take a whole object.");return false;}
                var source=Model.FindScrap(scrapId);industryReviewObject=source.kind;
                industryReviewFeedTerms=IndustryFeedTerms(equipment,source);industryReviewScrap=scrapId;
            }
            else if(kind==IndustryReviewKind.Dispatch)
            {
                if(equipment.kind!=EquipmentKind.ExportStation){Tell("Only an export station can dispatch materials.");return false;}
                var quote=Model.Industry.DispatchQuote(machineId);
                if(!quote.allowed){Tell(quote.reason);return false;}
                industryReviewMaterial=quote.kind;industryReviewQuantity=quote.quantity;industryReviewTotal=quote.total;
                industryReviewXp=quote.experience;industryReviewUnitPrice=quote.unitPrice;industryReviewBonus=quote.bonusPercent;
            }
            else if(kind==IndustryReviewKind.StandingDelivery)
            {
                if(equipment.kind!=EquipmentKind.PrimaryScrapper){Tell("Standing deliveries need a primary scrapper.");return false;}
                if(state!=null&&state.enabled){Tell("Standing deliveries are already on.");return false;}
                industryReviewObject=state==null?ScrapObjectKind.Car:state.purchaseKind;
                industryReviewPrice=Model.Rules.LargeRecipe(industryReviewObject).purchasePrice;
                industryReviewInterval=Model.Rules.deliveryIntervalSeconds;
            }
            else if(kind==IndustryReviewKind.AutomaticExport)
            {
                if(equipment.kind!=EquipmentKind.ExportStation){Tell("Automatic dispatch needs an export station.");return false;}
                if(state!=null&&state.enabled){Tell("Automatic dispatch is already on.");return false;}
                industryReviewInterval=Model.Rules.Equipment(equipment.kind).processingSeconds;
            }
            else return false;
            industryReviewMachine=machineId;industryReviewKind=kind;return true;
        }
        bool ConfirmIndustryReview()
        {
            int id=industryReviewMachine;var kind=industryReviewKind;
            if(kind==IndustryReviewKind.None||!RequireIndustryEquipment(id))return false;
            var equipment=Model.FindEquipment(id);var state=equipment.industry;
            if(kind==IndustryReviewKind.Feed)
            {
                int sourceId=industryReviewScrap;var source=Model.FindScrap(sourceId);
                if(source==null||source.kind!=industryReviewObject||IndustryFeedTerms(equipment,source)!=industryReviewFeedTerms||
                    !Model.Industry.CanFeedScrap(id,sourceId,out string reason))
                {ResetIndustryReview();Tell("This intake changed. Review the object again before loading it.");return false;}
                ResetIndustryReview();return IndustryAct(()=>Model.Industry.FeedScrap(id,sourceId));
            }
            if(kind==IndustryReviewKind.Dispatch)
            {
                var quote=Model.Industry.DispatchQuote(id);
                if(!quote.allowed||quote.kind!=industryReviewMaterial||quote.quantity!=industryReviewQuantity||quote.total!=industryReviewTotal||
                    quote.experience!=industryReviewXp||quote.unitPrice!=industryReviewUnitPrice||quote.bonusPercent!=industryReviewBonus)
                {ResetIndustryReview();Tell("The dispatch quote changed. Review the current material before confirming.");return false;}
                ResetIndustryReview();return IndustryAct(()=>Model.Industry.DispatchNow(id));
            }
            if(kind==IndustryReviewKind.StandingDelivery)
            {
                var selected=state==null?ScrapObjectKind.Car:state.purchaseKind;
                if(equipment.kind!=EquipmentKind.PrimaryScrapper||state!=null&&state.enabled||selected!=industryReviewObject||
                    Model.Rules.LargeRecipe(selected).purchasePrice!=industryReviewPrice||Model.Rules.deliveryIntervalSeconds!=industryReviewInterval)
                {ResetIndustryReview();Tell("The standing-delivery terms changed. Review the current price and timing again.");return false;}
            }
            else if(kind==IndustryReviewKind.AutomaticExport)
            {
                if(equipment.kind!=EquipmentKind.ExportStation||state!=null&&state.enabled||
                    Model.Rules.Equipment(equipment.kind).processingSeconds!=industryReviewInterval)
                {ResetIndustryReview();Tell("The dispatch timing changed. Review automatic dispatch again.");return false;}
            }
            ResetIndustryReview();return IndustryAct(()=>Model.Industry.SetEnabled(id,true));
        }
        bool DisableIndustry(int id)
        {
            if(!RequireIndustryEquipment(id))return false;
            ResetIndustryReview();return IndustryAct(()=>Model.Industry.SetEnabled(id,false));
        }
        bool SelectIndustryPurchase(int id,ScrapObjectKind kind)
        {
            if(!RequireIndustryEquipment(id))return false;
            var equipment=Model.FindEquipment(id);
            if(equipment.kind!=EquipmentKind.PrimaryScrapper)return false;
            ResetIndustryReview();return IndustryAct(()=>Model.Industry.SetPurchaseKind(id,kind));
        }
        static bool IsIndustryEquipment(EquipmentState equipment)
        {return equipment!=null&&(equipment.kind==EquipmentKind.PrimaryScrapper||equipment.kind==EquipmentKind.ExportStation);}
        void DrawIndustry(EquipmentState equipment)
        {
            if(!IsIndustryEquipment(equipment))return;
            if(industryReviewMachine!=0&&industryReviewMachine!=equipment.id)ResetIndustryReview();
            if(equipment.kind==EquipmentKind.PrimaryScrapper)DrawPrimaryIndustry(equipment);
            else DrawExportIndustry(equipment);
        }
        void DrawPrimaryIndustry(EquipmentState equipment)
        {
            var state=equipment.industry;var definition=Model.Rules.Equipment(equipment.kind);
            var job=state==null?null:state.primary;
            BeginCard("Whole-object recovery / "+Model.Industry.Status(equipment.id));
            Text("This machine takes an intact car or refrigerator, then recovers components into its output buffer. Whole objects are loaded directly; you never carry one or put it on a component belt.");
            if(job!=null)
            {
                Progress(job.duration>0?1-job.remaining/job.duration:0,
                    Model.Rules.LargeRecipe(job.kind).name+" / "+job.remaining.ToString("0.0")+"s work left");
                Text("This object is already loaded. Recover: "+YieldText(job.yields));
                Text("Power cuts and a full output buffer keep the object and its progress safe. Menus pause the work.");
            }
            else Text("Ready for one intact object. You can load scrap you already own, or explicitly turn on recurring deliveries below.");
            Text("Output: "+Model.StoredUnits(equipment.id)+" / "+definition.outputCapacity+" component units"+
                (job!=null?"  •  Space reserved for "+IndustryYieldUnits(job.yields)+" incoming units":"")+
                "\nProcessing time per object: "+definition.processingSeconds.ToString("0.#")+"s  •  Power: "+definition.powerDemand+" kW");
            if(state!=null)GUILayout.Label(state.objectsProcessed+" objects recovered by this machine",small);
            EndCard();
            Section("LOAD SCRAP YOU ALREADY OWN","Choose an intact, uninspected object from receiving. Loading it does not buy it again. A loaded object stays in the machine until its components are recovered.");
            bool any=false;
            foreach(var source in Model.State.scrap.ToArray())
            {
                any=true;int sourceId=source.id;
                string name=Model.Rules.LargeRecipe(source.kind).name;
                bool allowed=Model.Industry.CanFeedScrap(equipment.id,sourceId,out string reason);
                if(industryReviewKind==IndustryReviewKind.Feed&&industryReviewMachine==equipment.id&&industryReviewScrap==sourceId)
                {
                    BeginCard("Confirm loading / "+name);
                    Text("Load this owned object directly into the primary scrapper. No additional purchase charge. Its components will come from this object's recovery recipe.");
                    if(Primary("Confirm load / no extra charge",allowed))ConfirmIndustryReview();
                    if(Button("Cancel / keep it at receiving"))ResetIndustryReview();EndCard();
                }
                else if(Button("Review loading "+name+" / owned",allowed))BeginIndustryReview(IndustryReviewKind.Feed,equipment.id,sourceId);
                if(!allowed)GUILayout.Label(reason,small);
            }
            if(!any)Text("No whole objects are waiting at receiving. Buy one there, or choose a standing delivery for this machine.");
            DrawStandingDelivery(equipment);
        }
        void DrawStandingDelivery(EquipmentState equipment)
        {
            var state=equipment.industry;bool running=state!=null&&state.enabled;
            var selected=state==null?ScrapObjectKind.Car:state.purchaseKind;
            Section("STANDING DELIVERIES / "+(running?"ON":"OFF"));
            Text("A recurring delivery buys one selected object when this machine has power, enough room and enough money. Each object costs the quoted price. There is no acceptance fee or debt.");
            foreach(var recipe in Model.Rules.largeRecipes)
            {
                var kind=recipe.kind;
                if(Button((kind==selected?"Selected: ":"Select ")+recipe.name+" / €"+recipe.purchasePrice+" each",kind!=selected))
                    SelectIndustryPurchase(equipment.id,kind);
            }
            var choice=Model.Rules.LargeRecipe(selected);
            Text(choice.name+" / €"+choice.purchasePrice+" per object\nDelivery interval: "+Model.Rules.deliveryIntervalSeconds.ToString("0.#")+"s while the yard runs. The timer also waits for the machine to be ready.");
            if(running)
            {
                Text("Next delivery check: "+state.remaining.ToString("0.0")+"s. "+Model.Industry.Status(equipment.id));
                if(Button("Turn off standing deliveries"))DisableIndustry(equipment.id);
                Text("Turning deliveries off stops new purchases. An object already paid for or manually loaded will still finish when it has power.");
            }
            else if(industryReviewKind==IndustryReviewKind.StandingDelivery&&industryReviewMachine==equipment.id)
            {
                BeginCard("Approve recurring purchases");
                Text("Buy "+Model.Rules.LargeRecipe(industryReviewObject).name+" at €"+industryReviewPrice+" each, with a "+industryReviewInterval.ToString("0.#")+"s delivery interval. Purchases stop while money, power or room is missing. You can turn this off at any time.");
                if(Primary("Confirm / turn on standing deliveries"))ConfirmIndustryReview();
                if(Button("Cancel / keep deliveries off"))ResetIndustryReview();EndCard();
            }
            else if(Button("Review standing delivery terms…"))BeginIndustryReview(IndustryReviewKind.StandingDelivery,equipment.id);
        }
        void DrawExportIndustry(EquipmentState equipment)
        {
            var state=equipment.industry;var definition=Model.Rules.Equipment(equipment.kind);
            BeginCard("Material dispatch / "+Model.Industry.Status(equipment.id));
            Text("Deposit saleable materials or connect a material conveyor to this station. Components must be processed before they can be exported. Dispatch keeps each material's existing sale-XP eligibility.");
            Text("Stored: "+Model.StoredUnits(equipment.id)+" / "+definition.outputCapacity+" material units  •  Power: "+definition.powerDemand+" kW\nDispatch interval: "+definition.processingSeconds.ToString("0.#")+"s");
            var quote=Model.Industry.DispatchQuote(equipment.id);
            if(quote.quantity>0)
            {
                GUILayout.Label(quote.name+" ×"+quote.quantity,hudHeading);
                Text("€"+quote.unitPrice+" per unit  •  Base €"+quote.baseTotal+"  •  Level bonus €"+quote.bonusTotal+
                    "\nDispatch total €"+quote.total+"  •  "+quote.experience+" sale XP");
            }
            Text(quote.reason);
            GUILayout.Label("XP comes from eligible recovered materials. Transport and dispatch do not make previously ineligible stock eligible. Customer-request bonuses are separate.",small);
            if(industryReviewKind==IndustryReviewKind.Dispatch&&industryReviewMachine==equipment.id)
            {
                Text("Confirm exporting "+industryReviewQuantity+" "+Model.Rules.Part(industryReviewMaterial).name+" for €"+industryReviewTotal+" and "+industryReviewXp+" sale XP. Other materials stay stored.");
                if(Primary("Confirm dispatch / €"+industryReviewTotal,quote.allowed))ConfirmIndustryReview();
                if(Button("Cancel / keep the materials"))ResetIndustryReview();
            }
            else if(Button("Review a dispatch now",quote.allowed))BeginIndustryReview(IndustryReviewKind.Dispatch,equipment.id);
            EndCard();
            bool running=state!=null&&state.enabled;
            Section("AUTOMATIC EXPORT / "+(running?"ON":"OFF"));
            Text("Sell each supported buffered material group at the ordinary material rate. Dispatch respects the selected output filter and waits safely if power or sale capacity is missing. No customer-request bonus is paid here.");
            if(running)
            {
                Text("Next dispatch check: "+state.remaining.ToString("0.0")+"s. "+Model.Industry.Status(equipment.id));
                if(Button("Turn off automatic export"))DisableIndustry(equipment.id);
            }
            else if(industryReviewKind==IndustryReviewKind.AutomaticExport&&industryReviewMachine==equipment.id)
            {
                BeginCard("Approve automatic material sales");
                Text("Dispatch eligible buffered materials every "+industryReviewInterval.ToString("0.#")+"s while powered and ready. Each delivery uses its current material price and level bonus. You can turn automatic export off at any time.");
                if(Primary("Confirm / turn on automatic export"))ConfirmIndustryReview();
                if(Button("Cancel / keep automatic export off"))ResetIndustryReview();EndCard();
            }
            else if(Button("Review automatic export…"))BeginIndustryReview(IndustryReviewKind.AutomaticExport,equipment.id);
            Section("THIS STATION'S RECEIPT");
            Text(state==null?"No material shipments yet.":state.exportedUnits+" material units dispatched\n€"+state.exportedRevenue+" sale income  •  "+state.exportedXp+" sale XP");
            GUILayout.Label("This receipt stays with the equipment when you save. Reviewing it never pays or consumes another shipment.",small);
        }
        static long IndustryYieldUnits(PartAmount[] yields)
        {long total=0;if(yields!=null)foreach(var part in yields)if(part!=null&&part.quantity>0)total+=part.quantity;return total;}
    }
}
