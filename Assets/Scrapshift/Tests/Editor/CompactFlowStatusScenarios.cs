using System;
using System.Collections;
using System.Reflection;
using System.Text;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactFlowStatusScenarios
    {
        public static readonly string[] Names={"FlowStatusReadOnly","FlowStatusEmptyAndUnavailable","FlowStatusStockFilters",
            "FlowStatusChangedTransitFilter","FlowStatusFullBeforeAndAtArrival","FlowStatusSpacingAndBeltCapacity",
            "FlowStatusIndependentProcessorBays","FlowStatusMixedRecoveredOutputs","FlowStatusRecordLimits",
            "FlowStatusIdentityAndFinalStockReuse","FlowStatusSplitterBranches","FlowStatusPoweredAndPrimaryWait"};
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        static bool Has(AutomationModel automation,ConveyorLink link,string text)
        {return automation.FlowStatus(link.id).IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0;}
        static ScrappingModel Fresh(out ConstructionModel construction,out AutomationModel automation)
        {
            var model=new ScrappingModel(new CompactRules());model.State.money=100000;model.State.experience=model.Rules.levelThresholds[11];
            construction=new ConstructionModel(model.State,model.Rules);automation=new AutomationModel(model,construction);return model;
        }
        static EquipmentState Place(ScrappingModel model,ConstructionModel construction,EquipmentKind kind,float x,float z,float yaw=0)
        {Check(construction.Place(kind,x,z,yaw),construction.LastMessage);return model.FindEquipment(construction.LastPlacedId);}
        static ConveyorLink Link(ScrappingModel model,AutomationModel automation,EquipmentState source,int output,EquipmentState destination,int input)
        {
            Check(automation.Connect(source.id,output,destination.id,input,true),automation.LastMessage);
            foreach(var link in model.State.belts)if(link.id==automation.LastConnectedId)return link;throw new Exception("Missing route.");
        }
        static CompactStack Put(ScrappingModel model,EquipmentState equipment,PartKind kind,int quantity,bool eligible=true)
        {
            var item=new CompactStack{id=model.State.nextId++,kind=kind,quantity=quantity,x=equipment.x,z=equipment.z,xpEligible=eligible};
            equipment.contents.Add(item);return item;
        }
        static void Advance(AutomationModel automation,float seconds)
        {for(int i=0;i<(int)(seconds/.1f);i++)automation.Tick(.1f);}
        static string Snapshot(object value){var text=new StringBuilder();Fingerprint(value,text);return text.ToString();}
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
            ConstructionModel construction;AutomationModel automation;var model=Fresh(out construction,out automation);
            switch(name)
            {
                case "FlowStatusReadOnly":
                {
                    var source=Place(model,construction,EquipmentKind.Storage,0,0);var destination=Place(model,construction,EquipmentKind.Storage,0,6);
                    var link=Link(model,automation,source,0,destination,0);Put(model,source,PartKind.Copper,3,false);
                    for(int phase=0;phase<3;phase++)
                    {
                        string state=Snapshot(model.State),rules=Snapshot(model.Rules),message=automation.LastMessage,notice=model.LastNotice,powerMessage=construction.LastMessage;
                        int builds=automation.LayoutBuildCount,connected=automation.LastConnectedId;
                        for(int i=0;i<100;i++)automation.FlowStatus(link.id);
                        Check(state==Snapshot(model.State) && rules==Snapshot(model.Rules) && message==automation.LastMessage && notice==model.LastNotice && powerMessage==construction.LastMessage,
                            "Repeated queries preserve stock, lineage, cash, IDs, timers, recipes and action messages.");
                        Check(builds==automation.LayoutBuildCount && connected==automation.LastConnectedId,"Queries do not rebuild the simulation layout cache or change connection identity.");
                        if(phase==0)automation.Tick(.1f);
                        if(phase==1){Put(model,destination,PartKind.Steel,model.Rules.Equipment(destination.kind).outputCapacity,false);Advance(automation,10);}
                    }
                    Check(Has(automation,link,"Stopped at IN"),"Read-only coverage includes a stopped head.");break;
                }
                case "FlowStatusEmptyAndUnavailable":
                {
                    var source=Place(model,construction,EquipmentKind.Storage,0,0);var destination=Place(model,construction,EquipmentKind.Storage,0,6);
                    var link=Link(model,automation,source,0,destination,0);
                    Check(Has(automation,link,"OUT empty"),"Empty stock does not imply movement.");
                    Check(automation.FlowStatus(-1).Contains("unavailable"),"Removed or stale route is described safely.");
                    model.State.equipment.Remove(destination);Check(Has(automation,link,"endpoint unavailable"),"Missing endpoint does not dereference a route path.");
                    model.State.equipment.Add(destination);link.toPort=8;Check(Has(automation,link,"port unavailable"),"Stale physical role is described safely.");break;
                }
                case "FlowStatusStockFilters":
                {
                    var source=Place(model,construction,EquipmentKind.Storage,0,0);var destination=Place(model,construction,EquipmentKind.Tier1Scrapper,0,6,180);
                    var link=Link(model,automation,source,0,destination,0);Put(model,source,PartKind.Copper,1);Put(model,source,PartKind.Wire,1);
                    Check(model.SetFilter(source.id,(int)PartKind.Motor),model.LastNotice);
                    Check(Has(automation,link,"OUT filter holds"),"Source filter retains unmatched contents.");
                    Check(model.SetFilter(source.id,(int)PartKind.Copper),model.LastNotice);
                    Check(Has(automation,link,"IN cannot accept"),"Matching source stock can still be incompatible with processor IN.");
                    Check(model.SetFilter(source.id,-1),model.LastNotice);
                    Check(Has(automation,link,"OUT ready"),"Selection skips earlier incompatible copper and finds wiring.");
                    Check(model.SetFilter(destination.id,(int)PartKind.Motor),model.LastNotice);
                    Check(Has(automation,link,"IN cannot accept") && !automation.Tick(.1f),"Destination input recipe filter shares the actual launch guard.");
                    Check(model.SetFilter(destination.id,(int)PartKind.Wire),model.LastNotice);
                    Check(automation.Tick(.1f) && link.items.Count==1 && link.items[0].kind==PartKind.Wire && source.contents[0].kind==PartKind.Copper,
                        "Restoring destination filter launches wiring while incompatible copper stays stored.");break;
                }
                case "FlowStatusChangedTransitFilter":
                {
                    var source=Place(model,construction,EquipmentKind.Storage,0,0);var destination=Place(model,construction,EquipmentKind.Tier1Scrapper,0,6,180);
                    var link=Link(model,automation,source,0,destination,0);Put(model,source,PartKind.Wire,1);Check(automation.Tick(.1f),"Initial wiring launches.");
                    Check(source.contents.Count==0 && model.SetFilter(destination.id,(int)PartKind.Motor),"Source is empty before destination filter changes.");
                    Check(Has(automation,link,"Items travelling") && Has(automation,link,"IN cannot accept Wiring") && !Has(automation,link,"Stopped"),"A changed filter warns about arrival while cargo still travels.");
                    Advance(automation,10);Check(link.items[0].progress==1 && Has(automation,link,"Stopped at IN"),"An incompatible head is stopped even with no remaining source stock.");
                    Check(model.SetFilter(destination.id,(int)PartKind.Wire),model.LastNotice);Check(Has(automation,link,"Ready at IN"),"A compatible waiting head will receive on the next transport tick.");
                    Check(automation.Tick(.1f) && link.items.Count==0 && model.QueueUnits(destination.id)==1,"Filter recovery resumes the original unit without loss.");break;
                }
                case "FlowStatusFullBeforeAndAtArrival":
                {
                    var source=Place(model,construction,EquipmentKind.Storage,0,0);var destination=Place(model,construction,EquipmentKind.Storage,0,6);
                    var link=Link(model,automation,source,0,destination,0);model.Rules.Equipment(EquipmentKind.Storage).outputCapacity=2;
                    Put(model,source,PartKind.Copper,1,false);Put(model,destination,PartKind.Steel,2);
                    Check(Has(automation,link,"IN full") && Has(automation,link,"incoming items") && !Has(automation,link,"Stopped"),"An empty belt can still launch into a full destination.");
                    Check(automation.Tick(.1f) && link.items.Count==1,"Full IN does not veto launching.");int identity=link.items[0].id;
                    Check(Has(automation,link,"Items travelling") && Has(automation,link,"IN full") && !Has(automation,link,"Stopped"),"Full destination does not imply cargo has stopped before arrival.");
                    Advance(automation,10);Check(Has(automation,link,"Stopped at IN") && link.items[0].id==identity && !link.items[0].xpEligible,"Backpressure preserves original ineligible cargo.");
                    Check(model.Withdraw(destination.id,destination.contents[0].id) && model.Drop(8,.25f,8),model.LastNotice);
                    Check(Has(automation,link,"Ready at IN"),"Cleared destination exposes a ready arrival.");Check(automation.Tick(.1f),"Cleared destination resumes.");
                    Check(link.items.Count==0 && destination.contents[0].id==identity && destination.contents[0].quantity==1 && !destination.contents[0].xpEligible,"Resume preserves quantity, lineage and identity.");break;
                }
                case "FlowStatusSpacingAndBeltCapacity":
                {
                    var source=Place(model,construction,EquipmentKind.Storage,0,0);var destination=Place(model,construction,EquipmentKind.Storage,0,6);
                    var link=Link(model,automation,source,0,destination,0);Put(model,source,PartKind.Copper,4);
                    Check(automation.Tick(.1f) && Has(automation,link,"Items travelling") && Has(automation,link,"belt spacing") && !Has(automation,link,"Stopped"),"Normal launch cooldown is described as moving with spacing.");
                    link.launchRemaining=0;Check(Has(automation,link,"belt spacing"),"The physical tail spacing is an independent launch guard.");
                    link.items[0].progress=.3f;Check(Has(automation,link,"OUT ready"),"Enough spacing makes another launch eligible.");
                    model.Rules.beltCapacity=1;Check(Has(automation,link,"room on the belt") && Has(automation,link,"Items travelling"),"Capacity limits upstream launches without falsely claiming stopped cargo.");break;
                }
                case "FlowStatusIndependentProcessorBays":
                {
                    var source=Place(model,construction,EquipmentKind.Storage,0,0);var destination=Place(model,construction,EquipmentKind.Tier1Scrapper,0,6,180);
                    var link=Link(model,automation,source,0,destination,0);model.HasPower=id=>id==destination.id;
                    Put(model,destination,PartKind.Wire,1);Check(model.AutoBegin(destination.id),model.LastNotice);
                    int capacity=model.Rules.Equipment(destination.kind).outputCapacity;Put(model,destination,PartKind.Wire,capacity-1);Put(model,source,PartKind.Wire,1);
                    Check(model.ReservedOutputUnits(destination.id)==5 && model.IntakeCapacityUnits(destination.id)==capacity-1,"Paid OUT reservation is separate from IN.");
                    Check(Has(automation,link,"OUT ready") && !Has(automation,link,"IN full"),"A paid processor output reservation does not fill the independent intake bay.");
                    Check(automation.Tick(.1f) && automation.Tick(10) && model.QueueUnits(destination.id)==capacity,"The last IN slot receives while OUT remains reserved.");
                    float remaining=destination.job.remaining;Check(model.Tick(.5f) && destination.job.remaining==remaining-.5f,"Full independent IN does not block ongoing processing.");break;
                }
                case "FlowStatusMixedRecoveredOutputs":
                {
                    var source=Place(model,construction,EquipmentKind.Workbench,0,0);var destination=Place(model,construction,EquipmentKind.ExportStation,0,6);
                    var link=Link(model,automation,source,0,destination,0);Put(model,source,PartKind.Motor,1);Check(model.AutoBegin(source.id),model.LastNotice);
                    Check(Has(automation,link,"manual work"),"A manual job never promises automatic output before its strokes.");
                    while(!source.job.ready)Check(model.Work(source.id),model.LastNotice);
                    Check(model.SetFilter(source.id,(int)PartKind.Wire) && model.SetFilter(destination.id,(int)PartKind.Steel),model.LastNotice);
                    Check(source.job.yields[0].kind==PartKind.Copper && source.job.yields[1].kind==PartKind.Steel && Has(automation,link,"OUT ready"),
                        "Recipe filter does not suppress ready materials and later supported steel skips copper.");
                    Check(automation.Tick(.1f) && link.items[0].kind==PartKind.Steel && source.job.yields[0].quantity==4,"Actual launch agrees with status ordering.");
                    Advance(automation,30);Check(source.job!=null && source.job.yields[0].quantity==4 && source.job.yields[1].quantity==0 && Has(automation,link,"remaining recovered output"),
                        "Once steel drains, unsupported copper is retained and explained.");break;
                }
                case "FlowStatusRecordLimits":
                {
                    var source=Place(model,construction,EquipmentKind.Storage,0,0);var destination=Place(model,construction,EquipmentKind.Storage,0,6);
                    var link=Link(model,automation,source,0,destination,0);var stock=Put(model,source,PartKind.Copper,2);
                    model.Rules.maxStacks=model.OccupiedSlots;Check(Has(automation,link,"inventory space") && !automation.Tick(.1f) && stock.quantity==2,"Splitting a stack requires a free record and never consumes blocked stock.");
                    stock.quantity=1;Check(Has(automation,link,"OUT ready") && automation.Tick(.1f) && link.items[0].id==stock.id,"The final existing stock unit reuses its record.");
                    var bench=Place(model,construction,EquipmentKind.Workbench,7,0);var store=Place(model,construction,EquipmentKind.Storage,7,6);
                    var outputRoute=Link(model,automation,bench,0,store,0);model.Rules.maxStacks=256;Put(model,bench,PartKind.Wire,1);Check(model.AutoBegin(bench.id),model.LastNotice);
                    while(!bench.job.ready)Check(model.Work(bench.id),model.LastNotice);
                    model.Rules.maxStacks=model.OccupiedSlots;Check(Has(automation,outputRoute,"inventory space"),"Splitting a reserved output also needs a record.");
                    bench.job.yields[0].quantity=1;Check(Has(automation,outputRoute,"OUT ready"),"The last unit of a reserved yield needs no additional record.");break;
                }
                case "FlowStatusIdentityAndFinalStockReuse":
                {
                    var source=Place(model,construction,EquipmentKind.Storage,0,0);var destination=Place(model,construction,EquipmentKind.Storage,0,6);
                    var link=Link(model,automation,source,0,destination,0);var stock=Put(model,source,PartKind.Copper,2);model.State.nextId=int.MaxValue;
                    Check(Has(automation,link,"safe item identities") && !automation.Tick(.1f) && stock.quantity==2,"A split unit cannot consume stock without a safe new identity.");
                    stock.quantity=1;Check(Has(automation,link,"OUT ready") && automation.Tick(.1f) && link.items[0].id==stock.id,"Final stock unit can transport using its old identity at the ID limit.");
                    model.State.nextId=10000;var bench=Place(model,construction,EquipmentKind.Workbench,7,0);var store=Place(model,construction,EquipmentKind.Storage,7,6);
                    var outputRoute=Link(model,automation,bench,0,store,0);Put(model,bench,PartKind.Wire,1);Check(model.AutoBegin(bench.id),model.LastNotice);
                    while(!bench.job.ready)Check(model.Work(bench.id),model.LastNotice);bench.job.yields[0].quantity=1;model.State.nextId=int.MaxValue;
                    Check(Has(automation,outputRoute,"safe item identities"),"Even the final reserved output unit needs an identity, unlike existing stock.");break;
                }
                case "FlowStatusSplitterBranches":
                {
                    var splitter=Place(model,construction,EquipmentKind.Splitter,0,3);
                    var left=Place(model,construction,EquipmentKind.Storage,-5,3,270);var forward=Place(model,construction,EquipmentKind.Tier1Scrapper,0,9,180);
                    var right=Place(model,construction,EquipmentKind.Storage,5,3,90);
                    var l=Link(model,automation,splitter,0,left,0);var f=Link(model,automation,splitter,1,forward,0);var r=Link(model,automation,splitter,2,right,0);
                    Put(model,splitter,PartKind.Copper,3);Put(model,left,PartKind.Steel,model.Rules.Equipment(left.kind).outputCapacity);
                    Check(Has(automation,l,"IN full") && Has(automation,f,"IN cannot accept") && Has(automation,r,"OUT ready"),"Each splitter mouth reports its own destination, rather than inheriting another branch's blockage.");
                    Check(automation.Tick(.1f) && l.items.Count==1 && f.items.Count==0 && r.items.Count==1,"Actual split route selection respects compatibility and can stage cargo toward full IN.");break;
                }
                case "FlowStatusPoweredAndPrimaryWait":
                {
                    model.HasPower=id=>construction.PowerFor(id).powered;
                    for(int i=0;i<2;i++)
                    {
                        EquipmentKind kind=i==0?EquipmentKind.Tier1Scrapper:EquipmentKind.Tier2Scrapper;
                        var processor=Place(model,construction,kind,i*7,0,i==0?180:0);
                        var destination=Place(model,construction,EquipmentKind.Storage,i*7,6);
                        var generator=Place(model,construction,EquipmentKind.Generator,i*7+3,-3);
                        var link=Link(model,automation,processor,0,destination,0);
                        Check(construction.Connect(generator.id,processor.id),construction.LastMessage);
                        Put(model,processor,PartKind.Wire,1);Check(model.AutoBegin(processor.id),model.LastNotice);
                        var paid=processor.job;float remaining=paid.remaining;int capacity=model.Rules.Equipment(kind).outputCapacity;
                        Check(Has(automation,link,"waiting for processing"),"A powered unfinished paid batch waits for processing.");
                        Check(construction.Disconnect(generator.id,processor.id),construction.LastMessage);
                        Check(Has(automation,link,"Insufficient or disconnected power") && !Has(automation,link,"waiting for processing"),"Connected OUT names actual power interruption.");
                        string state=Snapshot(model.State),rules=Snapshot(model.Rules),notice=model.LastNotice,message=automation.LastMessage,powerMessage=construction.LastMessage;
                        for(int query=0;query<50;query++)automation.FlowStatus(link.id);
                        Check(state==Snapshot(model.State) && rules==Snapshot(model.Rules) && notice==model.LastNotice && message==automation.LastMessage && powerMessage==construction.LastMessage,
                            "Power feedback preserves the entire save, paid snapshot and action notices.");
                        model.Tick(.25f);Check(ReferenceEquals(paid,processor.job) && paid.remaining==remaining,"A queried power interruption does not consume the paid batch or advance work.");
                        Check(construction.Connect(generator.id,processor.id),construction.LastMessage);
                        Check(Has(automation,link,"waiting for processing") && !Has(automation,link,"disconnected power"),"Power restoration resumes the processing description.");
                        Check(model.Tick(.25f) && paid.remaining==remaining-.25f,"Power restoration advances the same paid work.");
                        model.Rules.Equipment(kind).outputCapacity=4;
                        Check(Has(automation,link,"Output blocked") && Has(automation,link,"progress is preserved"),"Reduced OUT capacity explains interrupted paid work.");
                        state=Snapshot(model.State);rules=Snapshot(model.Rules);
                        for(int query=0;query<50;query++)automation.FlowStatus(link.id);
                        Check(state==Snapshot(model.State) && rules==Snapshot(model.Rules),"Capacity feedback never rewrites reserved output or live tuning.");
                        model.Rules.Equipment(kind).outputCapacity=capacity;model.Tick(paid.remaining);
                        Check(ReferenceEquals(paid,processor.job) && paid.ready && Has(automation,link,"OUT ready"),"Restored capacity completes the original snapshot and exposes launchable OUT.");
                    }
                    var primary=Place(model,construction,EquipmentKind.PrimaryScrapper,-12,4);
                    var output=Place(model,construction,EquipmentKind.Storage,-12,13);
                    var primaryGenerator=Place(model,construction,EquipmentKind.Generator,-18,4);
                    var primaryLink=Link(model,automation,primary,0,output,0);
                    int owned=model.State.scrap[0].id,money=model.State.money;
                    Check(model.Industry.FeedScrap(primary.id,owned),model.Industry.LastMessage);
                    var whole=primary.industry.primary;float pending=whole.remaining;
                    Check(model.State.money==money && whole.id==owned && Has(automation,primaryLink,"Paid object preserved") && !Has(automation,primaryLink,"OUT empty"),
                        "A pending owned object is already reserved and its empty OUT reports the power wait.");
                    string saved=Snapshot(model.State),savedRules=Snapshot(model.Rules),industryMessage=model.Industry.LastMessage,lastNotice=model.LastNotice,lastMessage=automation.LastMessage;
                    for(int query=0;query<50;query++)automation.FlowStatus(primaryLink.id);
                    Check(saved==Snapshot(model.State) && savedRules==Snapshot(model.Rules) && industryMessage==model.Industry.LastMessage && lastNotice==model.LastNotice && lastMessage==automation.LastMessage,
                        "Primary waiting feedback preserves the save, paid output snapshot and industrial notices.");
                    Check(!model.Industry.Tick(.25f) && whole.remaining==pending,"The unpowered primary retains its paid duration.");
                    Check(construction.Connect(primaryGenerator.id,primary.id),construction.LastMessage);
                    Check(Has(automation,primaryLink,"seconds remaining") && !Has(automation,primaryLink,"OUT empty"),"Powered pending primary reports its actual work.");
                    Check(model.Industry.Tick(.25f) && whole.remaining==pending-.25f && ReferenceEquals(whole,primary.industry.primary),"Primary power recovery advances the original paid snapshot.");
                    int primaryCapacity=model.Rules.Equipment(primary.kind).outputCapacity;model.Rules.Equipment(primary.kind).outputCapacity=5;
                    Check(Has(automation,primaryLink,"Primary blocked") && Has(automation,primaryLink,"progress are preserved"),"A reduced primary buffer explains why its already reserved object is waiting.");
                    saved=Snapshot(model.State);savedRules=Snapshot(model.Rules);
                    for(int query=0;query<50;query++)automation.FlowStatus(primaryLink.id);
                    Check(saved==Snapshot(model.State) && savedRules==Snapshot(model.Rules),"Blocked primary feedback preserves the paid object and reduced catalogue tuning.");
                    model.Rules.Equipment(primary.kind).outputCapacity=primaryCapacity;
                    Put(model,primary,PartKind.Wire,1);Check(Has(automation,primaryLink,"OUT ready"),"Previously finished primary stock can leave while a new paid object is processing.");break;
                }
                default:throw new Exception("Unknown flow status scenario: "+name);
            }
        }
    }
}
