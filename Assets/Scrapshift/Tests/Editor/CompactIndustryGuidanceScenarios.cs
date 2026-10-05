using System;
using System.Collections;
using System.Reflection;
using System.Text;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactIndustryGuidanceScenarios
    {
        public static readonly string[] Names={"IndustryGuideCarriedCustomerPriority","IndustryGuideManualAndReadyPriority",
            "IndustryGuideCustomerRecoveryPriority","IndustryGuidePaidPrimaryPower","IndustryGuidePaidNetworkOverload",
            "IndustryGuideNearestMovedPaidPrimary","IndustryGuideSnapshotProgress","IndustryGuidePrimaryBackpressure",
            "IndustryGuidePrimaryComponentsReady","IndustryGuideExportQuoteAndEligibility","IndustryGuideExportPowerAndFilter",
            "IndustryGuideStandingFundsAndTiming","IndustryGuideOwnedIntactFeed","IndustryGuideOptionalUnlockAndPrices",
            "IndustryGuideOptionalUnavailableAndLimits","IndustryGuideReadOnly"};
        static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
        static bool Has(CompactGuideStep step,string text){return step.action.IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0;}
        static ScrappingModel Empty()
        {return new ScrappingModel(new CompactRules{startingCars=0,startingRefrigerators=0});}
        static CompactGuideStep Guide(ScrappingModel model,float x=0,float z=0)
        {
            var services=new CompactGuideServices{sales=new YardDestination(YardLandmark.Buyer,"Office sales",-16,-11),
                shop=new YardDestination(YardLandmark.Automatic,"Equipment counter",-12,-12),
                delivery=new YardDestination(YardLandmark.Delivery,"Receiving",18,-12),
                wire=new YardDestination(YardLandmark.Delivery,"Free wire",16,-7)};
            return CompactGuidance.Resolve(model,x,z,services,"Return","Right mouse");
        }
        static void At(CompactGuideStep step,float x,float z,string reason)
        {Check(step.hasDestination && step.destination.x==x && step.destination.z==z,reason+": "+step.action);}
        static EquipmentState Equipment(ScrappingModel model,EquipmentKind kind,float x,float z)
        {
            var equipment=new EquipmentState{id=model.State.nextId++,kind=kind,x=x,z=z,paidPrice=model.Rules.Equipment(kind).price};
            model.State.equipment.Add(equipment);return equipment;
        }
        static CompactStack Stack(ScrappingModel model,PartKind kind,int quantity,bool eligible=true)
        {return new CompactStack{id=model.State.nextId++,kind=kind,quantity=quantity,xpEligible=eligible};}
        static void Held(ScrappingModel model,PartKind kind,int quantity,bool eligible=true)
        {var stack=Stack(model,kind,quantity,eligible);model.State.items.Add(stack);model.State.carriedId=stack.id;}
        static LargeScrapJob Owned(ScrappingModel model,ScrapObjectKind kind=ScrapObjectKind.Car)
        {var source=new LargeScrapJob{id=model.State.nextId++,kind=kind,x=18,z=-10};model.State.scrap.Add(source);return source;}
        static void Feed(ScrappingModel model,EquipmentState primary)
        {Check(model.Industry.FeedScrap(primary.id,Owned(model).id),model.Industry.LastMessage);}
        static void Completed(ScrappingModel model,int level=12)
        {
            model.State.experience=model.Rules.levelThresholds[level-1];model.Career.Stats.completedGoals=511;
            model.Career.Stats.contract=null;model.Career.Stats.contractsCompleted=model.Rules.contracts.Length;
        }
        static string Snapshot(object value)
        {var text=new StringBuilder();Fingerprint(value,text);return text.ToString();}
        static void Fingerprint(object value,StringBuilder text)
        {
            if(value==null){text.Append("null;");return;}
            var type=value.GetType();
            if(type.IsPrimitive || type.IsEnum || value is string){text.Append(type.Name).Append(':').Append(value).Append(';');return;}
            var sequence=value as IEnumerable;
            if(sequence!=null){text.Append('[');foreach(var item in sequence)Fingerprint(item,text);text.Append(']');return;}
            var fields=type.GetFields(BindingFlags.Instance|BindingFlags.Public);Array.Sort(fields,(a,b)=>string.CompareOrdinal(a.Name,b.Name));
            text.Append(type.Name).Append('{');foreach(var field in fields){text.Append(field.Name).Append('=');Fingerprint(field.GetValue(value),text);}text.Append('}');
        }
        public static void Run(string name)
        {
            switch(name)
            {
                case "IndustryGuideCarriedCustomerPriority":
                {
                    var model=Empty();var primary=Equipment(model,EquipmentKind.PrimaryScrapper,5,5);Feed(model,primary);
                    Held(model,PartKind.Copper,2);var step=Guide(model);
                    At(step,-16,-11,"Carried customer material keeps the physical sales route ahead of a paid power interruption");
                    Check(Has(step,"deliver") && Has(step,"[Return]") && Has(step,"+4 XP"),"Customer terms and rebound action remain exact");break;
                }
                case "IndustryGuideManualAndReadyPriority":
                {
                    var model=Empty();var primary=Equipment(model,EquipmentKind.PrimaryScrapper,5,5);Feed(model,primary);
                    var bench=model.State.equipment[0];Held(model,PartKind.Wire,1);Check(model.BeginProcessing(bench.id),model.LastNotice);
                    var step=Guide(model);At(step,bench.x,bench.z,"Active manual work stays ahead of optional industry");
                    Check(Has(step,"[Right mouse]") && Has(step,"strokes"),"Actual manual binding/stage shown");
                    while(!bench.job.ready)Check(model.Work(bench.id),model.LastNotice);
                    step=Guide(model);At(step,bench.x,bench.z,"Ready manual materials keep collection priority");Check(Has(step,"Collect"),"Ready manual job can be completed before power work");break;
                }
                case "IndustryGuideCustomerRecoveryPriority":
                {
                    var model=Empty();model.Career.Stats.completedGoals=255;
                    model.Career.CurrentContract.kind=PartKind.Plastic;var source=Owned(model,ScrapObjectKind.Refrigerator);source.x=11;source.z=-5;
                    var primary=Equipment(model,EquipmentKind.PrimaryScrapper,2,3);Feed(model,primary);
                    var step=Guide(model);At(step,11,-5,"Active customer recovery keeps its suitable fridge route");
                    Check(Has(step,"Inspect") && !Has(step,"Paid object"),"Industry does not displace the customer's useful scrap source");break;
                }
                case "IndustryGuidePaidPrimaryPower":
                {
                    var model=Empty();var primary=Equipment(model,EquipmentKind.PrimaryScrapper,8,5);Feed(model,primary);
                    var generator=Equipment(model,EquipmentKind.Generator,-10,5);var step=Guide(model);
                    At(step,8,5,"Unpowered paid object directs to its moved machine");
                    Check(Has(step,"retained") && Has(step,"6 kW") && Has(step,"beyond 12m") && Has(step,"[Return]") && !Has(step,"move"),"No busy-machine move or running-power promise");
                    generator.x=10;model.State.powerLinks.Add(new PowerLink(primary.id,generator.id));
                    step=Guide(model);Check(!Has(step,"being recovered"),"An absent runtime HasPower callback does not falsely promise processing");
                    model.HasPower=id=>new ConstructionModel(model.State,model.Rules).PowerFor(id).powered;
                    Check(Has(Guide(model),"being recovered"),"Actual power allows paid-job progress guidance");break;
                }
                case "IndustryGuidePaidNetworkOverload":
                {
                    var model=Empty();var primary=Equipment(model,EquipmentKind.PrimaryScrapper,4,4);Feed(model,primary);
                    var generator=Equipment(model,EquipmentKind.Generator,10,4);var exporter=Equipment(model,EquipmentKind.ExportStation,4,9);
                    model.State.powerLinks.Add(new PowerLink(primary.id,generator.id));model.State.powerLinks.Add(new PowerLink(exporter.id,generator.id));
                    model.HasPower=id=>new ConstructionModel(model.State,model.Rules).PowerFor(id).powered;
                    var step=Guide(model);At(step,4,4,"Interrupted paid job uses its actual network");
                    Check(Has(step,"overloaded") && Has(step,"8 / 6 kW") && Has(step,"retained"),"Combined true demand drives recovery advice");break;
                }
                case "IndustryGuideNearestMovedPaidPrimary":
                {
                    var model=Empty();var far=Equipment(model,EquipmentKind.PrimaryScrapper,-13,8);Feed(model,far);
                    var near=Equipment(model,EquipmentKind.PrimaryScrapper,9,6);Feed(model,near);model.HasPower=id=>false;
                    At(Guide(model,8,5),9,6,"Nearest interrupted primary chosen from actual positions");
                    near.x=18;near.z=12;At(Guide(model,-12,7),-13,8,"Moved/saved positions are reflected without a fixed anchor");break;
                }
                case "IndustryGuideSnapshotProgress":
                {
                    var model=Empty();var primary=Equipment(model,EquipmentKind.PrimaryScrapper,7,4);model.HasPower=id=>true;Feed(model,primary);
                    model.Industry.Tick(2.5f);model.Rules.Equipment(primary.kind).processingSeconds=90;
                    var step=Guide(model);At(step,7,4,"Active job has a concrete machine destination");
                    Check(Has(step,"21.5s") && Has(step,"off") && !Has(step,"90"),"Uses saved paid-job progress after tuning rather than a new recipe duration");break;
                }
                case "IndustryGuidePrimaryBackpressure":
                {
                    var model=Empty();var primary=Equipment(model,EquipmentKind.PrimaryScrapper,7,4);model.HasPower=id=>true;Feed(model,primary);
                    primary.contents.Add(Stack(model,PartKind.Wire,48));var step=Guide(model);
                    At(step,7,4,"Output reservation backpressure directs to the held object's own buffer");
                    Check(Has(step,"blocked") && Has(step,"drain") && Has(step,"retained"),"No second intake or lost progress is promised");break;
                }
                case "IndustryGuidePrimaryComponentsReady":
                {
                    var model=Empty();var primary=Equipment(model,EquipmentKind.PrimaryScrapper,7,4);model.HasPower=id=>true;Feed(model,primary);
                    model.Industry.Tick(24);var step=Guide(model);At(step,7,4,"Actual recovered output replaces waiting/intake guidance");
                    Check(Has(step,"components") && Has(step,"withdraw") && Has(step,"processing"),"Outputs need component recovery before material sale");break;
                }
                case "IndustryGuideExportQuoteAndEligibility":
                {
                    var model=Empty();var exporter=Equipment(model,EquipmentKind.ExportStation,-8,9);model.HasPower=id=>true;
                    exporter.contents.Add(Stack(model,PartKind.Copper,3));exporter.contents.Add(Stack(model,PartKind.Copper,4,false));
                    var quote=model.Industry.DispatchQuote(exporter.id);var step=Guide(model);At(step,-8,9,"Material quote directs to moved physical dispatch equipment");
                    Check(Has(step,"Copper ×3") && Has(step,"€"+quote.total) && Has(step,"+6 sale XP") && Has(step,"explicitly approved"),"Preview is exact recovered eligibility group and auto remains off");
                    Check(model.Industry.DispatchNow(exporter.id),model.Industry.LastMessage);step=Guide(model);
                    Check(Has(step,"Copper ×4") && Has(step,"+0 sale XP"),"Remaining ineligible group gains no new recovery XP");break;
                }
                case "IndustryGuideExportPowerAndFilter":
                {
                    var model=Empty();var exporter=Equipment(model,EquipmentKind.ExportStation,-8,9);exporter.contents.Add(Stack(model,PartKind.Copper,3));
                    var step=Guide(model);At(step,-8,9,"Unpowered exports keep physical stock and guide power");
                    Check(Has(step,"2 kW") && Has(step,"retained") && !Has(step,"ready for dispatch"),"No unpowered sale is promised");
                    model.HasPower=id=>true;exporter.filterKind=(int)PartKind.Steel;step=Guide(model);
                    Check(Has(step,"filter") && !Has(step,"ready for dispatch"),"Rejected filter does not fabricate a copper quote");
                    exporter.filterKind=-1;model.State.money=int.MaxValue;step=Guide(model);
                    Check(Has(step,"limit") && !Has(step,"ready for dispatch"),"Sale overflow advertises preservation rather than payment");break;
                }
                case "IndustryGuideStandingFundsAndTiming":
                {
                    var model=Empty();var primary=Equipment(model,EquipmentKind.PrimaryScrapper,5,8);model.HasPower=id=>true;
                    model.Rules.LargeRecipe(ScrapObjectKind.Refrigerator).purchasePrice=37;model.Rules.deliveryIntervalSeconds=17;
                    Check(model.Industry.SetPurchaseKind(primary.id,ScrapObjectKind.Refrigerator),model.Industry.LastMessage);
                    Check(model.Industry.SetEnabled(primary.id,true),model.Industry.LastMessage);model.State.money=12;
                    var step=Guide(model);At(step,5,8,"Cash-paused selected standing service identifies the actual machine");
                    Check(Has(step,"€37") && Has(step,"€25 more") && Has(step,"no fee or debt"),"Funding advice uses selected actual price without purchasing");
                    model.State.money=50;model.Industry.Tick(2);step=Guide(model);
                    Check(Has(step,"15 eligible idle seconds") && Has(step,"€37"),"Eligible idle countdown and editable interval are actual");
                    model.HasPower=id=>false;step=Guide(model);Check(Has(step,"without buying") && !Has(step,"15 eligible"),"Power gate outranks delivery countdown");break;
                }
                case "IndustryGuideOwnedIntactFeed":
                {
                    var model=Empty();Completed(model);var near=Equipment(model,EquipmentKind.PrimaryScrapper,10,6);var source=Owned(model);
                    var step=Guide(model);At(step,10,6,"Whole-object intake directs to primary rather than carrying a car");
                    Check(Has(step,"owned") && Has(step,"No second purchase") && Has(step,"never carried"),"Owned intake can be reviewed even while power is disconnected");
                    Check(model.InspectScrap(source.id),model.LastNotice);step=Guide(model);
                    Check(!Has(step,"No second purchase") && Has(step,"power"),"Partially inspected objects cannot be promised as primary input");
                    Owned(model);model.Rules.Part(PartKind.Insulation).unitPrice=0;model.Rules.Validate();near.contents.Add(Stack(model,PartKind.Insulation,48));
                    Equipment(model,EquipmentKind.PrimaryScrapper,-15,8);step=Guide(model,9,5);
                    At(step,-15,8,"A nearer full intake is skipped in favour of an actually usable owned machine");
                    Check(Has(step,"No second purchase"),"Capacity-safe intake still reviews an owned whole object without charging");break;
                }
                case "IndustryGuideOptionalUnlockAndPrices":
                {
                    var model=Empty();Completed(model,11);model.State.money=1000;var step=Guide(model);
                    Check(!Has(step,"Optional whole-object") && !Has(step,"buy/place"),"No industrial upgrade is forced before its real unlock");
                    model.Rules.Equipment(EquipmentKind.PrimaryScrapper).unlockLevel=11;model.Rules.Equipment(EquipmentKind.PrimaryScrapper).price=417;
                    step=Guide(model);At(step,-12,-12,"Finished career offers newly unlocked optional primary at the real shop");
                    Check(Has(step,"Optional") && Has(step,"€417") && Has(step,"start off"),"No hardcoded purchase cost or automatic opt-in");
                    var primary=Equipment(model,EquipmentKind.PrimaryScrapper,10,6);model.HasPower=id=>true;
                    model.Rules.Equipment(EquipmentKind.ExportStation).unlockLevel=11;model.Rules.Equipment(EquipmentKind.ExportStation).price=263;
                    step=Guide(model);At(step,-12,-12,"Owned powered primary can extend to real export equipment");
                    Check(Has(step,"€263") && Has(step,"processed first") && Has(step,"starts off"),"Export stays an explicit paid optional next step");
                    Equipment(model,EquipmentKind.ExportStation,-8,9);model.Rules.deliveryIntervalSeconds=19;
                    step=Guide(model);At(step,primary.x,primary.z,"Owned completed line guides its actual primary controls");
                    Check(Has(step,"19 eligible idle seconds") && Has(step,"explicit approval"),"Setup uses real tuned cadence and no service grant");break;
                }
                case "IndustryGuideOptionalUnavailableAndLimits":
                {
                    var model=Empty();Completed(model);model.State.money=1000;model.Rules.Equipment(EquipmentKind.PrimaryScrapper).available=false;
                    var step=Guide(model);Check(!Has(step,"buy/place") && Has(step,"free wiring"),"Unavailable primary keeps ordinary renewable work");
                    model.Rules.Equipment(EquipmentKind.PrimaryScrapper).available=true;model.Rules.maxEquipment=1;
                    step=Guide(model);Check(!step.hasDestination && Has(step,"Equipment limit"),"Optional owned capacity cannot promise a purchase");
                    model.Rules.maxEquipment=64;model.State.money=400;model.Rules.Equipment(EquipmentKind.PrimaryScrapper).price=417;
                    step=Guide(model);At(step,16,-7,"Optional unaffordable primary offers actual renewable funding route");
                    Check(Has(step,"€17 more") && !Has(step,"buy/place"),"No unaffordable transaction is promised");break;
                }
                case "IndustryGuideReadOnly":
                {
                    var model=Empty();Completed(model);var primary=Equipment(model,EquipmentKind.PrimaryScrapper,10,6);var exporter=Equipment(model,EquipmentKind.ExportStation,-8,9);
                    model.HasPower=id=>true;Feed(model,primary);exporter.contents.Add(Stack(model,PartKind.Copper,3));
                    foreach(int state in new[]{0,1,2,3,4})
                    {
                        if(state==1)model.HasPower=id=>false;
                        if(state==2){model.HasPower=id=>true;model.Industry.Tick(24);}
                        if(state==3){primary.contents.Clear();exporter.contents.Clear();model.Industry.SetEnabled(primary.id,true);model.State.money=0;}
                        if(state==4){model.State.money=100;primary.industry.enabled=false;Owned(model);}
                        string before=Snapshot(model.State),rules=Snapshot(model.Rules),notice=model.LastNotice,industryNotice=model.Industry.LastMessage;
                        for(int i=0;i<80;i++)Guide(model,i%32-16,i%20-10);
                        Check(before==Snapshot(model.State) && rules==Snapshot(model.Rules) && notice==model.LastNotice && industryNotice==model.Industry.LastMessage,
                            "Guidance never changes paid jobs/timers/lineage/IDs/rules/quotes/career/money or either transient notice");
                    }
                    break;
                }
                default:throw new Exception("Unknown industrial guidance scenario: "+name);
            }
        }
    }
}
