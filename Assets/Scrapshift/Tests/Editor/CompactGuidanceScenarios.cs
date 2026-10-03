using System;
using System.Collections;
using System.Reflection;
using System.Text;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactGuidanceScenarios
    {
        public static readonly string[] Names={"CompactGuideFreshInspection","CompactGuideNearestMovedBench","CompactGuidePoweredAndBusyInputs",
            "CompactGuideTier2FilterAndCapacity","CompactGuideActiveManualAndReady","CompactGuideBlockedOutput","CompactGuideFullHandsBeforeOutput",
            "CompactGuideEligibleCustomerDelivery","CompactGuideImportedCashOnly","CompactGuideLockedRequestSale","CompactGuidePlasticAndSteelSources",
            "CompactGuideAffordableDelivery","CompactGuideZeroCashRenewable","CompactGuidePowerShoppingAndConnection","CompactGuidePowerRangeAndOverload",
            "CompactGuideUnavailableAndLimits","CompactGuideFirstConveyor","CompactGuideReadOnly"};
        static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
        static bool Has(CompactGuideStep step,string value){return step.action.IndexOf(value,StringComparison.OrdinalIgnoreCase)>=0;}
        static CompactGuideServices Services()
        {return new CompactGuideServices{sales=new YardDestination(YardLandmark.Buyer,"Sales counter",-16,-11),
            shop=new YardDestination(YardLandmark.Automatic,"Yard shop",-12,-12),
            delivery=new YardDestination(YardLandmark.Delivery,"Receiving office",18,-12),
            wire=new YardDestination(YardLandmark.Delivery,"Wire offcuts",16,-7)};}
        static ScrappingModel Empty()
        {return new ScrappingModel(new CompactRules{startingCars=0,startingRefrigerators=0});}
        static CompactGuideStep Guide(ScrappingModel model,float x=0,float z=0)
        {return CompactGuidance.Resolve(model,x,z,Services(),"Return","Right mouse");}
        static void At(CompactGuideStep guide,float x,float z,string reason)
        {Check(guide.hasDestination && guide.destination.x==x && guide.destination.z==z,reason+": "+guide.action);}
        static EquipmentState AddEquipment(ScrappingModel model,EquipmentKind kind,float x,float z)
        {var e=new EquipmentState{id=model.State.nextId++,kind=kind,x=x,z=z};model.State.equipment.Add(e);return e;}
        static CompactStack AddItem(ScrappingModel model,PartKind kind,int quantity,float x,float z,bool held=false,bool eligible=true)
        {var i=new CompactStack{id=model.State.nextId++,kind=kind,quantity=quantity,x=x,z=z,xpEligible=eligible};model.State.items.Add(i);if(held)model.State.carriedId=i.id;return i;}
        static void Goal(ScrappingModel model,string key)
        {
            string[] keys={"inspect","dismantle","process","sell","power","powered","level10","belts","contracts"};
            for(int i=0;i<keys.Length;i++)if(keys[i]==key){model.Career.Stats.completedGoals=(1<<i)-1;return;}
            throw new Exception("Unknown test goal");
        }
        static EquipmentState LoadWire(ScrappingModel model,EquipmentState bench)
        {AddItem(model,PartKind.Wire,1,0,0,true);Check(model.BeginProcessing(bench.id),model.LastNotice);return bench;}
        static void Finish(ScrappingModel model,EquipmentState bench)
        {while(!bench.job.ready)Check(model.Work(bench.id),model.LastNotice);}
        static string Snapshot(object value)
        {
            var text=new StringBuilder();Fingerprint(value,text);return text.ToString();
        }
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
                case "CompactGuideFreshInspection":
                {
                    var model=new ScrappingModel(new CompactRules());var scrap=model.State.scrap[1];scrap.x=7;scrap.z=3;
                    var guide=Guide(model,6,3);At(guide,7,3,"Inspect the closest actual delivered object");
                    Check(Has(guide,"Inspect") && Has(guide,"[Return]"),"Inspection uses actual rebound binding");
                    Check(guide.destination.Direction(6,3,0)=="Nearby","Existing direction helper applies to compact destination");break;
                }
                case "CompactGuideNearestMovedBench":
                {
                    var model=Empty();var original=model.State.equipment[0];original.x=-12;original.z=9;
                    var near=AddEquipment(model,EquipmentKind.Workbench,8,5);AddItem(model,PartKind.Motor,1,0,0,true);
                    At(Guide(model,7,4),near.x,near.z,"Moved nearest bench guides actual saved position");
                    near.x=-16;near.z=7;At(Guide(model,-12,8),original.x,original.z,"New movement is reflected immediately");break;
                }
                case "CompactGuidePoweredAndBusyInputs":
                {
                    var model=Empty();var bench=model.State.equipment[0];LoadWire(model,bench);
                    var powered=AddEquipment(model,EquipmentKind.Tier1Scrapper,5,5);var unpowered=AddEquipment(model,EquipmentKind.Tier1Scrapper,0,1);
                    model.HasPower=id=>id==powered.id;AddItem(model,PartKind.Motor,1,0,0,true);
                    At(Guide(model),5,5,"Nearest busy/unpowered equipment is skipped");
                    model.Rules.Equipment(EquipmentKind.Tier1Scrapper).outputCapacity=1;
                    var guide=Guide(model);At(guide,bench.x,bench.z,"Oversized outputs are not promised");
                    Check(Has(guide,"Put down") && Has(guide,"[Right mouse]"),"Busy work needs empty hands and actual work label");
                    Check(unpowered.job==null,"Guidance does not load the unpowered machine");break;
                }
                case "CompactGuideTier2FilterAndCapacity":
                {
                    var model=Empty();var machine=AddEquipment(model,EquipmentKind.Tier2Scrapper,1,2);model.HasPower=id=>id==machine.id;
                    AddItem(model,PartKind.Motor,1,0,0,true);At(Guide(model),1,2,"Nearby usable powered Tier 2 accepts a supported input");
                    machine.filterKind=(int)PartKind.Wire;At(Guide(model),-5,-4,"Wrong filter is skipped");
                    machine.filterKind=-1;model.Rules.Equipment(EquipmentKind.Tier2Scrapper).outputCapacity=4;
                    At(Guide(model),-5,-4,"Input fitting a buffer is insufficient when its automatic output cannot fit");
                    model.Rules.Equipment(EquipmentKind.Tier2Scrapper).outputCapacity=48;
                    machine.filterKind=-1;machine.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Wire,quantity=48});
                    At(Guide(model),-5,-4,"Full buffer is skipped");break;
                }
                case "CompactGuideActiveManualAndReady":
                {
                    var model=Empty();var bench=model.State.equipment[0];bench.x=9;bench.z=2;LoadWire(model,bench);
                    var guide=Guide(model);At(guide,9,2,"Active manual work guides its actual location");Check(Has(guide,"[Right mouse]") && Has(guide,"0/4"),"Active job displays work action and progress");
                    Finish(model,bench);guide=Guide(model);Check(Has(guide,"Collect") && Has(guide,"[Return]"),"Ready output replaces manual-work prompt");break;
                }
                case "CompactGuideBlockedOutput":
                {
                    var model=Empty();var machine=AddEquipment(model,EquipmentKind.Tier2Scrapper,7,7);model.HasPower=id=>true;
                    machine.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Wire,quantity=1});Check(model.AutoBegin(machine.id),model.LastNotice);
                    machine.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Copper,quantity=48});
                    var guide=Guide(model);At(guide,7,7,"Blocked Tier 2 output directs to its buffer");
                    Check(Has(guide,"withdraw") && Has(guide,"retained"),"Explain meaningful capacity recovery without discarding progress");
                    model.State.nextId=int.MaxValue;machine.job.ready=true;guide=Guide(model);
                    Check(!guide.hasDestination && Has(guide,"identifier limit"),"Identity exhaustion does not promise collectible output");break;
                }
                case "CompactGuideFullHandsBeforeOutput":
                {
                    var model=Empty();var bench=model.State.equipment[0];LoadWire(model,bench);Finish(model,bench);
                    AddItem(model,PartKind.Wire,1,0,0,true);var guide=Guide(model);
                    Check(Has(guide,"Put down") && Has(guide,"collect"),"Carried component must be put down before ready output is collected");
                    Check(model.Carried.kind==PartKind.Wire && bench.job.yields[0].quantity==3,"Ready and carried inputs are preserved");break;
                }
                case "CompactGuideEligibleCustomerDelivery":
                {
                    var model=Empty();AddItem(model,PartKind.Copper,2,0,0,true);var guide=Guide(model);var quote=model.Career.ContractQuote();
                    At(guide,-16,-11,"Customer delivery uses actual sales anchor");
                    Check(Has(guide,"deliver") && Has(guide,quote.customer) && Has(guide,"+4 XP") && Has(guide,"€6"),"Quote is actual partial delivery, not fabricated completion bonus");
                    Check(model.Career.CurrentContract.delivered==0,"Guidance does not commit partial delivery");break;
                }
                case "CompactGuideImportedCashOnly":
                {
                    var model=Empty();AddItem(model,PartKind.Steel,2,0,0,true,false);var guide=Guide(model);
                    Check(Has(guide,"sell") && Has(guide,"+0 XP") && Has(guide,"imported stock"),"Imported material cash sale advertises no new XP");
                    model.Carried.kind=PartKind.Copper;guide=Guide(model);Check(Has(guide,"deliver") && Has(guide,"+0 XP"),"Matching imported copper can supply customers without XP");break;
                }
                case "CompactGuideLockedRequestSale":
                {
                    var model=Empty();model.Career.CurrentContract.minimumLevel=3;AddItem(model,PartKind.Copper,3,0,0,true);
                    var guide=Guide(model);Check(Has(guide,"sell") && !Has(guide,"deliver"),"Locked customer retains ordinary sales");
                    model.State.money=int.MaxValue;guide=Guide(model);Check(!guide.hasDestination && Has(guide,"limit"),"Overflow never promises a sale");break;
                }
                case "CompactGuidePlasticAndSteelSources":
                {
                    var model=new ScrappingModel(new CompactRules());Goal(model,"contracts");model.Career.CurrentContract.kind=PartKind.Plastic;
                    var guide=Guide(model);At(guide,20,-5,"Plastic request selects fridge rather than nonplastic car/wire");
                    model.Career.CurrentContract.kind=PartKind.Steel;guide=Guide(model,18,-10);At(guide,18,-10,"Steel request selects car body/motor chain");
                    model.State.scrap.Clear();AddItem(model,PartKind.BrokenRadio,1,7,4);model.Career.CurrentContract.kind=PartKind.Plastic;
                    At(Guide(model),7,4,"Plastic request can choose a supported electronic salvage recipe");break;
                }
                case "CompactGuideAffordableDelivery":
                {
                    var model=Empty();Goal(model,"contracts");model.Career.CurrentContract.kind=PartKind.Plastic;model.State.money=15;
                    var guide=Guide(model);At(guide,18,-12,"Affordable suitable scrap is bought through actual delivery anchor");Check(Has(guide,"€15") && Has(guide,"refrigerator"),"Request does not suggest an unsuitable car");
                    model.State.money=14;guide=Guide(model);At(guide,16,-7,"Unaffordable delivery starts from renewable funds");
                    Check(Has(guide,"Earn delivery money") && Has(guide,"cannot supply Plastic") && !Has(guide,"order a"),"Wire is funding, not falsely a plastic recipe");break;
                }
                case "CompactGuideZeroCashRenewable":
                {
                    var model=Empty();model.State.money=0;Goal(model,"power");var guide=Guide(model);
                    At(guide,16,-7,"Zero-money recovery stays available at caller wire anchor");Check(Has(guide,"free wiring") && Has(guide,"€60 more"),"Clear funding purpose without unaffordable purchase");
                    model.Rules.maxStacks=1;guide=Guide(model);Check(!guide.hasDestination && Has(guide,"space"),"Finite reserved output headroom is respected");break;
                }
                case "CompactGuidePowerShoppingAndConnection":
                {
                    var model=Empty();Goal(model,"power");model.State.money=105;var guide=Guide(model);
                    At(guide,-12,-12,"No owned scrapper sends player to real shop");Check(Has(guide,"Tier 1") && Has(guide,"€60"),"Offer available paid Tier 1");
                    var machine=AddEquipment(model,EquipmentKind.Tier1Scrapper,4,4);guide=Guide(model);Check(Has(guide,"Generator") && Has(guide,"€45"),"Unpowered scrapper needs generator purchase");
                    AddEquipment(model,EquipmentKind.Generator,8,4);guide=Guide(model);At(guide,4,4,"Connect at actual machine location");Check(Has(guide,"Power network") && Has(guide,"[Return]"),"Actionable connection route uses rebound inspect");
                    Check(machine.job==null && model.State.powerLinks.Count==0,"Prompt is not a purchase or cable transaction");break;
                }
                case "CompactGuidePowerRangeAndOverload":
                {
                    var model=Empty();Goal(model,"power");var a=AddEquipment(model,EquipmentKind.Tier1Scrapper,8,4);var g=AddEquipment(model,EquipmentKind.Generator,-10,4);
                    var guide=Guide(model);Check(Has(guide,"beyond 12m") && !Has(guide,"connect the nearby"),"Out-of-range generator is not presented as connectable");
                    g.x=12;var b=AddEquipment(model,EquipmentKind.Tier2Scrapper,8,8);model.State.powerLinks.Add(new PowerLink(a.id,g.id));model.State.powerLinks.Add(new PowerLink(b.id,g.id));
                    guide=Guide(model);Check(Has(guide,"overloaded") && Has(guide,"8 / 6 kW"),"Actual shared demand drives overloaded guidance");break;
                }
                case "CompactGuideUnavailableAndLimits":
                {
                    var model=Empty();Goal(model,"power");model.State.money=1000;model.Rules.Equipment(EquipmentKind.Tier1Scrapper).available=false;
                    var guide=Guide(model);At(guide,16,-7,"Unavailable catalogue leaves manual loop");Check(Has(guide,"unavailable") && !Has(guide,"buy/place"),"Unavailable stage is not purchasable");
                    model.Rules.Equipment(EquipmentKind.Tier1Scrapper).available=true;model.Rules.maxEquipment=1;
                    guide=Guide(model);Check(!guide.hasDestination && Has(guide,"Equipment limit"),"Construction ownership capacity is honored");break;
                }
                case "CompactGuideFirstConveyor":
                {
                    var model=Empty();model.State.experience=model.Rules.levelThresholds[9];Goal(model,"belts");model.State.money=100;
                    var guide=Guide(model);Check(Has(guide,"Ported storage") && Has(guide,"€70"),"First route needs actual purchased storage");
                    AddEquipment(model,EquipmentKind.Storage,5,6);AddEquipment(model,EquipmentKind.Tier1Scrapper,5,2);guide=Guide(model);
                    At(guide,5,2,"Guide to a compatible owned conveyor output");Check(Has(guide,"output port") && Has(guide,"preview") && Has(guide,"price"),"A preview accounts for length-priced route rather than promising flat cost");break;
                }
                case "CompactGuideReadOnly":
                {
                    var model=new ScrappingModel(new CompactRules());var bench=model.State.equipment[0];LoadWire(model,bench);Finish(model,bench);
                    var machine=AddEquipment(model,EquipmentKind.Tier1Scrapper,8,2);model.HasPower=id=>id==machine.id;
                    string before=Snapshot(model.State),rules=Snapshot(model.Rules),notice=model.LastNotice;var held=model.Carried;
                    for(int i=0;i<100;i++)Guide(model,i%20-10,i%12-6);
                    Check(before==Snapshot(model.State) && rules==Snapshot(model.Rules) && notice==model.LastNotice && ReferenceEquals(held,model.Carried),"Repeated guidance preserves all saved state, recipes and transient notice");
                    model.State.carriedId=AddItem(model,PartKind.Copper,3,0,0,false,false).id;before=Snapshot(model.State);notice=model.LastNotice;
                    for(int i=0;i<100;i++)Guide(model);
                    Check(before==Snapshot(model.State) && notice==model.LastNotice,"Contract quote guidance never commits inventory, IDs, request or money");
                    model.State.carriedId=0;model.State.items.Clear();bench.job=null;Goal(model,"power");model.HasPower=null;
                    before=Snapshot(model.State);notice=model.LastNotice;
                    for(int i=0;i<100;i++)Guide(model);
                    Check(before==Snapshot(model.State) && notice==model.LastNotice,"Power-network explanation is also completely read-only");break;
                }
                default:throw new Exception("Unknown compact guidance scenario: "+name);
            }
        }
    }
}
