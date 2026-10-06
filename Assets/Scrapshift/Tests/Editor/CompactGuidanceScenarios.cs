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
            "CompactGuideTier2FilterAndCapacity","CompactGuideActiveManualAndReady","CompactGuideBlockedOutput","CompactGuideSeparateInputOutputBays","CompactGuideFullHandsBeforeOutput",
            "CompactGuideEligibleCustomerDelivery","CompactGuideImportedCashOnly","CompactGuideLockedRequestSale","CompactGuidePlasticAndSteelSources",
            "CompactGuideAffordableDelivery","CompactGuideZeroCashRenewable","CompactGuidePowerShoppingAndConnection","CompactGuidePowerRangeAndOverload",
            "CompactGuideUnavailableAndLimits","CompactGuideFirstConveyor","CompactGuideEarlyPoweredLine","CompactGuideCustomLineGate",
            "CompactGuideQueuedInputs","CompactGuideBufferedPreparation","CompactGuideAutomaticOutput","CompactGuideFilteredLaterOutput","CompactGuideOutputRouteBlocks","CompactGuideReadOnly"};
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
                    Check(Has(guide,"queue") && Has(guide,"[Return]"),"Busy manual bench can queue compatible input without promising an impossible powered batch");
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
                    model.Rules.Equipment(machine.kind).outputCapacity=4;
                    var guide=Guide(model);At(guide,7,7,"A reduced OUT bay retains an already reserved five-unit Tier 2 output");
                    Check(Has(guide,"capacity was reduced")&&Has(guide,"Restore its capacity")&&Has(guide,"retained")&&!Has(guide,"withdraw"),"Withdrawing queued inputs cannot fix an independently undersized reserved output bay");
                    model.State.nextId=int.MaxValue;machine.job.ready=true;guide=Guide(model);
                    Check(!guide.hasDestination && Has(guide,"identifier limit"),"Identity exhaustion does not promise collectible output");break;
                }
                case "CompactGuideSeparateInputOutputBays":
                {
                    var model=Empty();Goal(model,"powered");var machine=AddEquipment(model,EquipmentKind.Tier1Scrapper,6,3);model.HasPower=id=>id==machine.id;
                    AddItem(model,PartKind.Wire,1,0,0,true);Check(model.BeginProcessing(machine.id),model.LastNotice);
                    machine.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Wire,quantity=24,xpEligible=true});
                    var guide=Guide(model);At(guide,6,3,"A full IN bay does not turn an independently valid running OUT batch into blocked guidance");
                    Check(Has(guide,"is processing")&&!Has(guide,"blocked")&&model.QueueUnits(machine.id)==24&&model.ReservedOutputUnits(machine.id)==5,"Running description honors separate intake and reserved-output capacities");
                    string before=Snapshot(model.State),notice=model.LastNotice;
                    for(int i=0;i<50;i++)Guide(model);
                    Check(before==Snapshot(model.State)&&notice==model.LastNotice,"Repeated full-IN guidance retains the running batch, queue, money and identifiers");
                    float remaining=machine.job.remaining;Check(model.Tick(.5f)&&machine.job.remaining==remaining-.5f,"Actual processing agrees with guidance and advances through a full independent IN bay");

                    model=Empty();var bench=model.State.equipment[0];model.Rules.Recipe(PartKind.Motor).yields=new[]{new PartAmount(PartKind.Wire,1)};
                    AddItem(model,PartKind.Motor,1,0,0,true);Check(model.BeginProcessing(bench.id),model.LastNotice);Finish(model,bench);
                    machine=AddEquipment(model,EquipmentKind.Tier1Scrapper,bench.x,bench.z-6);model.HasPower=id=>id==machine.id;
                    AddItem(model,PartKind.Wire,1,0,0,true);Check(model.BeginProcessing(machine.id),model.LastNotice);
                    machine.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Wire,quantity=23,xpEligible=true});
                    var route=new ConveyorLink{id=model.State.nextId++,fromId=bench.id,fromPort=0,toId=machine.id,toPort=0};model.State.belts.Add(route);
                    guide=Guide(model);Check(Has(guide,"feed OUT automatically")&&!Has(guide,"IN is full"),"An incoming component can use the last IN slot while a five-unit OUT batch is reserved");
                    before=Snapshot(model.State);notice=model.LastNotice;for(int i=0;i<50;i++)Guide(model);
                    Check(before==Snapshot(model.State)&&notice==model.LastNotice,"Separate destination-bay guidance never changes transport or stock");
                    var automation=new AutomationModel(model,new ConstructionModel(model.State,model.Rules));
                    Check(automation.Tick(.1f)&&route.items.Count==1,"Real transport launches the compatible custom component output");
                    Check(automation.Tick(10)&&route.items.Count==0&&model.QueueUnits(machine.id)==24&&model.ReservedOutputUnits(machine.id)==5,"Real intake receives into its free slot while preserving the separate paid output reservation");break;
                }
                case "CompactGuideFullHandsBeforeOutput":
                {
                    var model=Empty();var bench=model.State.equipment[0];LoadWire(model,bench);Finish(model,bench);
                    AddItem(model,PartKind.Wire,1,0,0,true);var guide=Guide(model);
                    Check(Has(guide,"queue") && Has(guide,"manual collection"),"Carried component may be queued while ready output stays available for collection");
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
                    At(guide,5,2,"Guide to a compatible owned conveyor output");Check(Has(guide,"OUT port") && Has(guide,"preview") && Has(guide,"price"),"A preview accounts for length-priced route rather than promising flat cost");break;
                }
                case "CompactGuideEarlyPoweredLine":
                {
                    var model=Empty();Goal(model,"belts");model.State.money=200;
                    var storage=AddEquipment(model,EquipmentKind.Storage,-4,-2);
                    var machine=AddEquipment(model,EquipmentKind.Tier1Scrapper,2,3);model.HasPower=id=>id==machine.id;
                    var guide=Guide(model);At(guide,machine.x,machine.z,"First powered line uses a powered Tier 1 output ahead of the closer manual bench");
                    Check(model.Level==1 && Has(guide,"OUT port") && Has(guide,"storage IN") && !Has(guide,"level 10"),"Earned money buys a usable early line without a late belt gate");
                    model.State.belts.Add(new ConveyorLink{id=model.State.nextId++,fromId=machine.id,fromPort=0,toId=storage.id,toPort=0});
                    guide=Guide(model);At(guide,model.State.equipment[0].x,model.State.equipment[0].z,"Already connected powered output falls back to a free bench port");
                    Check(Has(guide,"manual strokes") && Has(guide,"[Right mouse]"),"Bench ports do not promise automatic manual labor");break;
                }
                case "CompactGuideCustomLineGate":
                {
                    var model=Empty();Goal(model,"level10");model.State.money=200;
                    model.Rules.Equipment(EquipmentKind.Storage).unlockLevel=4;model.Rules.Equipment(EquipmentKind.Conveyor).unlockLevel=3;
                    var guide=Guide(model);Check(Has(guide,"production line at level 4") && Has(guide,"free wiring") && !Has(guide,"buy/place"),"Custom later line gates stay authoritative in the historical journal slot");
                    model.State.experience=model.Rules.levelThresholds[3];guide=Guide(model);
                    Check(Has(guide,"Ported storage") && Has(guide,"€70"),"Actual configured gate, rather than literal level ten, makes a purchased line available");break;
                }
                case "CompactGuideQueuedInputs":
                {
                    var model=Empty();var bench=model.State.equipment[0];LoadWire(model,bench);
                    var machine=AddEquipment(model,EquipmentKind.Tier1Scrapper,1,2);model.HasPower=id=>id==machine.id;
                    AddItem(model,PartKind.Wire,1,0,0,true);Check(model.BeginProcessing(machine.id),model.LastNotice);
                    AddItem(model,PartKind.Motor,1,0,0,true);var guide=Guide(model);
                    At(guide,1,2,"Busy powered machine can receive another component into its real IN buffer");
                    Check(Has(guide,"queue in its IN buffer") && Has(guide,"[Return]"),"Queued guidance uses immediate supported interaction and actual rebound binding");
                    machine.filterKind=(int)PartKind.Wire;guide=Guide(model);At(guide,bench.x,bench.z,"Recipe filtering prevents a misleading motor intake prompt");
                    Check(machine.contents.Count==0 && model.Carried.kind==PartKind.Motor,"Guidance never queues or filters stock itself");break;
                }
                case "CompactGuideBufferedPreparation":
                {
                    var model=Empty();var bench=model.State.equipment[0];
                    bench.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Wire,quantity=1,xpEligible=true});
                    var guide=Guide(model);At(guide,bench.x,bench.z,"Buffered manual recipe is explained before another acquisition");
                    Check(Has(guide,"prepare the next recipe") && Has(guide,"[Right mouse]") && bench.job==null,"Read-only prompt prepares nothing and preserves manual work");
                    bench.contents.Clear();var machine=AddEquipment(model,EquipmentKind.Tier1Scrapper,6,3);
                    machine.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Motor,quantity=1,xpEligible=true});
                    guide=Guide(model);At(guide,6,3,"Unpowered queued machine points to its actual location");
                    Check(Has(guide,"Queued components are retained") && Has(guide,"Power network"),"Restore power before new purchases or replacement inputs");
                    model.HasPower=id=>id==machine.id;model.Rules.Equipment(machine.kind).outputCapacity=4;
                    guide=Guide(model);Check(Has(guide,"Output blocked") && Has(guide,"Queued stock is retained"),"Impossible queued output directs to safe capacity recovery");break;
                }
                case "CompactGuideAutomaticOutput":
                {
                    var model=Empty();var bench=model.State.equipment[0];LoadWire(model,bench);Finish(model,bench);
                    var storage=AddEquipment(model,EquipmentKind.Storage,3,4);
                    model.State.belts.Add(new ConveyorLink{id=model.State.nextId++,fromId=bench.id,fromPort=0,toId=storage.id,toPort=0});
                    var guide=Guide(model);At(guide,bench.x,bench.z,"Finished connected batch describes its real automatic output route");
                    Check(Has(guide,"feed OUT automatically") && Has(guide,"IN") && Has(guide,"[Return]"),"Connected output is not presented as requiring manual carrying");
                    string before=Snapshot(model.State),notice=model.LastNotice;
                    for(int i=0;i<50;i++)Guide(model);
                    Check(before==Snapshot(model.State)&&notice==model.LastNotice,"Automatic flow guidance neither transfers stock nor changes source notice");break;
                }
                case "CompactGuideOutputRouteBlocks":
                {
                    var model=Empty();var bench=model.State.equipment[0];LoadWire(model,bench);Finish(model,bench);
                    var receiver=AddEquipment(model,EquipmentKind.Tier1Scrapper,3,4);
                    var route=new ConveyorLink{id=model.State.nextId++,fromId=bench.id,fromPort=0,toId=receiver.id,toPort=0};model.State.belts.Add(route);
                    var guide=Guide(model);At(guide,3,4,"Material-to-component route explains the destination's actual incompatible intake");
                    Check(Has(guide,"cannot accept Copper") && Has(guide,"storage") && Has(guide,"retained"),"Material output cannot be sent to another component processor as though it were a supported recipe");
                    receiver.kind=EquipmentKind.Storage;receiver.contents.Add(new CompactStack{id=model.State.nextId++,kind=PartKind.Steel,quantity=model.Rules.Equipment(EquipmentKind.Storage).outputCapacity});
                    guide=Guide(model);Check(Has(guide,"Receiving IN is full") && Has(guide,"wait safely") && Has(guide,"withdraw"),"Downstream fullness gives actionable backpressure guidance without loss");
                    receiver.contents.Clear();receiver.kind=EquipmentKind.ExportStation;receiver.filterKind=(int)PartKind.Steel;
                    guide=Guide(model);Check(Has(guide,"cannot accept Copper") && Has(guide,"item or recipe filter"),"Material export filter conflicts are visible and preserve outputs");
                    receiver.filterKind=(int)PartKind.Copper;route.items.Add(new ConveyorItem{id=model.State.nextId++,kind=PartKind.Insulation,progress=.9f});
                    guide=Guide(model);Check(Has(guide,"cannot accept Insulation"),"A blocked leading transit item is identified ahead of newly ready compatible output");break;
                }
                case "CompactGuideFilteredLaterOutput":
                {
                    var model=Empty();var bench=model.State.equipment[0];
                    AddItem(model,PartKind.Motor,1,0,0,true);Check(model.BeginProcessing(bench.id),model.LastNotice);Finish(model,bench);
                    var receiver=AddEquipment(model,EquipmentKind.ExportStation,bench.x,bench.z-6);receiver.yaw=180;receiver.filterKind=(int)PartKind.Steel;
                    var route=new ConveyorLink{id=model.State.nextId++,fromId=bench.id,fromPort=0,toId=receiver.id,toPort=0};model.State.belts.Add(route);
                    Check(bench.job.yields[0].kind==PartKind.Copper&&bench.job.yields[0].quantity==4&&bench.job.yields[1].kind==PartKind.Steel,"Motor output orders copper before steel for a real filtered selection case");
                    var guide=Guide(model);At(guide,bench.x,bench.z,"A filtered receiver can accept a later material output from the same ready job");
                    Check(Has(guide,"feed OUT automatically")&&!Has(guide,"cannot accept"),"Copper remaining ahead of steel does not falsely block the usable steel route");
                    string before=Snapshot(model.State),notice=model.LastNotice;
                    for(int i=0;i<50;i++)Guide(model);
                    Check(before==Snapshot(model.State)&&notice==model.LastNotice,"Filtered output guidance never launches a transit item or consumes either yield");
                    var automation=new AutomationModel(model,new ConstructionModel(model.State,model.Rules));
                    Check(automation.Tick(.1f)&&route.items.Count==1&&route.items[0].kind==PartKind.Steel,"Actual transport agrees with guidance and launches supported steel past copper");
                    Check(bench.job.yields[0].quantity==4&&bench.job.yields[1].quantity==5,"One launched steel unit leaves all incompatible copper and the remaining steel reserved");break;
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
