using System;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactEarlyMachineScenarios
    {
        public static readonly string[] Names={
            "EarlyPortsKeepExistingOutput", "EarlyPurchasedTier1Line", "EarlyManualBenchLine", "EarlyRawInputsNeverEmit",
            "EarlyPoweredIntakeWaits", "EarlyInputFiltersRejectAtomically", "EarlyBusyInputReservations", "EarlyOneQualifiedBatch",
            "EarlyProcessingBackpressure", "EarlyFullReceivingBuffer", "EarlyNewArrivalHasFullDuration", "EarlyPauseHasNoPreparation",
            "EarlyCarriedLoopRemainsUsable", "EarlyReadOnlyLoadingQuotes", "EarlyIdentityAndSlotReservations", "EarlyResumeKeepsLineage",
            "EarlySustainedFullInputLine", "EarlyBlockedSustainedLineResumes", "EarlyManualSeparateBays", "EarlyReducedCapacityPreservesBothBays", "EarlySeparateDurableCapacity"
        };
        static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
        sealed class Yard
        {
            public CompactRules rules;
            public ScrappingModel model;
            public ConstructionModel construction;
            public AutomationModel automation;
            public int input,processor,output,generator;
            public CompactYardState State{get{return model.State;}}
            public Yard(EquipmentKind kind,bool line=true)
            {
                // A funded low-level yard isolates ownership/unlock behavior from earned sale XP.
                // No equipment is granted: construction and both actual belt purchases debit cash.
                rules=new CompactRules{startingMoney=1000,startingCars=0,startingRefrigerators=0};
                model=new ScrappingModel(rules);Bind();
                if(kind==EquipmentKind.Workbench)
                {processor=State.equipment[0].id;Check(construction.Move(processor,-8,0,0),construction.LastMessage);}
                else processor=Place(kind,-8,0);
                if(line)
                {
                    bool tier1=kind==EquipmentKind.Tier1Scrapper;
                    input=Place(EquipmentKind.Storage,-8,tier1?6:-6,tier1?180:0);
                    output=Place(EquipmentKind.Storage,-8,tier1?-6:6,tier1?180:0);
                    Check(automation.Connect(input,0,processor,0,true),automation.LastMessage);
                    Check(automation.Connect(processor,0,output,0,true),automation.LastMessage);
                }
                if(kind!=EquipmentKind.Workbench)
                {
                    generator=Place(EquipmentKind.Generator,-2,0);
                    Check(construction.Connect(generator,processor),construction.LastMessage);
                }
            }
            void Bind()
            {construction=new ConstructionModel(State,rules);automation=new AutomationModel(model,construction);model.HasPower=id=>construction.PowerFor(id).powered;}
            int Place(EquipmentKind kind,float x,float z,float yaw=0)
            {Check(construction.Place(kind,x,z,yaw),construction.LastMessage);return construction.LastPlacedId;}
            public CompactStack Put(int equipment,PartKind kind,int quantity,bool eligible=true)
            {
                var owner=model.FindEquipment(equipment);var item=new CompactStack{id=State.nextId++,kind=kind,quantity=quantity,x=owner.x,z=owner.z,xpEligible=eligible};
                owner.contents.Add(item);return item;
            }
            public CompactStack Hold(PartKind kind,int quantity,bool eligible=true)
            {var item=new CompactStack{id=State.nextId++,kind=kind,quantity=quantity,xpEligible=eligible};State.items.Add(item);State.carriedId=item.id;return item;}
            public void Tick(float delta){model.Tick(delta);automation.Tick(delta);}
            public void Advance(int frames=1200){for(int i=0;i<frames;i++)Tick(.1f);}
            public void Resume()
            {model=new ScrappingModel(rules,CompactScrappingScenarios.Copy(State));Bind();}
            public void Validate()
            {ScrappingModel.Validate(State,rules);ConstructionModel.Validate(State,rules);AutomationModel.Validate(State,rules);}
        }
        static int Total(CompactYardState state,PartKind kind,bool eligible)
        {
            int total=0;foreach(var item in state.items)if(item.kind==kind && item.xpEligible==eligible)total+=item.quantity;
            foreach(var equipment in state.equipment)
            {
                foreach(var item in equipment.contents)if(item.kind==kind && item.xpEligible==eligible)total+=item.quantity;
                if(equipment.job!=null && equipment.job.xpEligible==eligible)foreach(var amount in equipment.job.yields)if(amount.kind==kind)total+=amount.quantity;
            }
            foreach(var belt in state.belts)foreach(var item in belt.items)if(item.kind==kind && item.xpEligible==eligible)total+=item.quantity;
            return total;
        }
        public static void Run(string name)
        {
            var yard=new Yard(name=="EarlyManualBenchLine" || name=="EarlyPauseHasNoPreparation"?EquipmentKind.Workbench:EquipmentKind.Tier1Scrapper);
            var model=yard.model;var state=yard.State;var machine=model.FindEquipment(yard.processor);string reason;
            switch(name)
            {
                case "EarlyPortsKeepExistingOutput":
                {
                    foreach(var kind in new[]{EquipmentKind.Workbench,EquipmentKind.Tier1Scrapper,EquipmentKind.Tier2Scrapper})
                    {
                        var e=new EquipmentState{kind=kind,x=3,z=2};
                        Check(AutomationModel.PortCount(kind,false)==1 && AutomationModel.PortCount(kind,true)==1,"Each component processor has an intake and outlet.");
                        var input=AutomationModel.Port(e,yard.rules,false,0);var output=AutomationModel.Port(e,yard.rules,true,0);
                        Check(kind==EquipmentKind.Tier1Scrapper?input.z>2 && output.z<2:input.z<2 && output.z>2,"Opposite endpoints retain prior Tier1 output geometry.");
                        e.yaw=90;input=AutomationModel.Port(e,yard.rules,false,0);output=AutomationModel.Port(e,yard.rules,true,0);
                        Check(kind==EquipmentKind.Tier1Scrapper?input.x>3 && output.x<3:input.x<3 && output.x>3,"Ports rotate with equipment.");
                    }
                    break;
                }
                case "EarlyPurchasedTier1Line":
                    Check(model.Level==1 && state.experience==0 && state.belts.Count==2 && state.money<1000 && machine.paidPrice==60,"A purchased useful line works at level1.");
                    yard.Put(yard.input,PartKind.Wire,3);yard.Put(yard.input,PartKind.Wire,2,false);int paid=state.money;yard.Advance();
                    Check(model.StoredUnits(yard.output)==25 && Total(state,PartKind.Wire,true)==0 && Total(state,PartKind.Wire,false)==0,"Five actual inputs reach finished storage.");
                    Check(Total(state,PartKind.Copper,true)==9 && Total(state,PartKind.Insulation,true)==6 && Total(state,PartKind.Copper,false)==6 && Total(state,PartKind.Insulation,false)==4,"Source-qualified outputs remain exact.");
                    Check(state.money==paid && state.experience==0 && machine.job==null,"Transport/processing adds no rewards or recurring purchases.");break;
                case "EarlyManualBenchLine":
                    yard.Put(yard.input,PartKind.Wire,2);yard.Advance(120);Check(machine.job!=null && !machine.job.ready && machine.job.strokes==0,"Belt delivery prepares manual work, never performs it.");
                    Check(model.StoredUnits(yard.output)==0 && state.belts[1].items.Count==0,"Unworked outputs remain reserved.");
                    for(int i=0;i<4;i++)Check(model.Work(machine.id),model.LastNotice);yard.Advance(120);
                    Check(model.StoredUnits(yard.output)==5 && machine.job!=null && machine.job.strokes==0,"Ready outputs belt away, next batch still needs manual work.");
                    for(int i=0;i<4;i++)Check(model.Work(machine.id),model.LastNotice);yard.Advance(120);
                    Check(model.StoredUnits(yard.output)==10 && machine.job==null && state.experience==0,"Two manual batches recover once without transport XP.");break;
                case "EarlyRawInputsNeverEmit":
                    Check(yard.construction.Disconnect(yard.generator,yard.processor),"Disconnect power.");yard.Put(yard.processor,PartKind.Wire,2);yard.Advance(120);
                    Check(machine.job==null && model.StoredUnits(yard.processor)==2 && model.StoredUnits(yard.output)==0 && state.belts[1].items.Count==0,"Buffered raw components cannot bypass processor output.");
                    Check(yard.construction.Connect(yard.generator,yard.processor),"Restore power.");yard.Advance(250);
                    Check(model.StoredUnits(yard.output)==10 && Total(state,PartKind.Wire,true)==0,"Restored power resumes genuine conversion.");break;
                case "EarlyPoweredIntakeWaits":
                    Check(yard.construction.Disconnect(yard.generator,yard.processor),"Disconnect power.");var pending=yard.Put(yard.processor,PartKind.Motor,1);int next=state.nextId;
                    Check(!model.CanAutoBegin(machine.id,out reason) && reason.Contains("power") && !model.AutoBegin(machine.id),"Unpowered intake is explained and rejected.");
                    yard.Tick(20);Check(machine.job==null && machine.contents[0].id==pending.id && pending.quantity==1 && state.nextId==next,"Unpowered ticks retain input identity.");
                    Check(yard.construction.Connect(yard.generator,yard.processor),"Restore generator.");model.Tick(20);
                    Check(machine.job!=null && machine.job.remaining==machine.job.duration && !machine.job.ready,"New powered job does not inherit earlier delta.");break;
                case "EarlyInputFiltersRejectAtomically":
                    Check(model.SetFilter(machine.id,(int)PartKind.Motor),"Select motor recipe.");var held=yard.Hold(PartKind.Wire,1);int id=held.id,nextBefore=state.nextId;
                    Check(!model.Deposit(machine.id) && !model.BeginProcessing(machine.id) && model.Carried.id==id && machine.contents.Count==0 && state.nextId==nextBefore,"Carried wrong recipe stays intact.");
                    Check(!model.SetFilter(machine.id,(int)PartKind.Copper) && machine.filterKind==(int)PartKind.Motor,"Recovered material cannot become a processor recipe filter.");
                    model.Drop(10,.25f,0);var transit=new ConveyorItem{id=state.nextId++,kind=PartKind.Wire,progress=1};state.belts[0].items.Add(transit);yard.automation.Tick(.1f);
                    Check(state.belts[0].items.Count==1 && machine.contents.Count==0,"Already travelling wrong input waits rather than being deleted.");
                    Check(model.SetFilter(machine.id,(int)PartKind.Wire),"Choose compatible intake.");yard.automation.Tick(.1f);
                    Check(state.belts[0].items.Count==0 && machine.job!=null && machine.job.input==PartKind.Wire,"Changing filter permits the retained unit exactly once.");break;
                case "EarlyBusyInputReservations":
                    yard.Hold(PartKind.Motor,1);Check(model.BeginProcessing(machine.id),"Load existing carried batch.");var job=machine.job;
                    yard.Hold(PartKind.Wire,24);Check(model.Deposit(machine.id) && model.QueueUnits(machine.id)==24 && model.ReservedOutputUnits(machine.id)==10,"Busy intake fills its independent input bay without stealing reserved outputs.");
                    var extra=yard.Hold(PartKind.Wire,1);Check(!model.Deposit(machine.id) && model.Carried.id==extra.id && machine.job==job && model.StoredUnits(machine.id)==24,"Input queue capacity blocks new carried input atomically.");break;
                case "EarlyOneQualifiedBatch":
                    yard.rules.Recipe(PartKind.Wire).inputQuantity=2;var old=yard.Put(machine.id,PartKind.Wire,1,false);yard.Put(machine.id,PartKind.Wire,1,true);
                    Check(!model.AutoBegin(machine.id) && machine.job==null && machine.contents.Count==2,"Recovery histories cannot be combined for XP.");
                    yard.Put(machine.id,PartKind.Wire,2,true);Check(model.AutoBegin(machine.id) && machine.job.inputQuantity==2 && machine.job.xpEligible,"Exactly one qualified recipe batch starts.");
                    Check(model.StoredUnits(machine.id)==2 && old.quantity==1 && machine.contents[0].id==old.id && !model.AutoBegin(machine.id),"Remainder identity is retained and a busy job cannot start twice.");break;
                case "EarlyProcessingBackpressure":
                    yard.Put(machine.id,PartKind.Wire,1);Check(model.AutoBegin(machine.id),"Prepare powered batch.");model.Tick(1);float remaining=machine.job.remaining;yard.rules.Equipment(machine.kind).outputCapacity=4;
                    Check(!model.Tick(40) && machine.job.remaining==remaining && model.ProcessingBlockReason(machine.id).Contains("Output blocked"),"Reduced capacity retains partial processing.");
                    yard.rules.Equipment(machine.kind).outputCapacity=24;yard.Advance(150);
                    Check(model.StoredUnits(yard.output)==5 && machine.job==null,"Making space resumes exact outputs once.");break;
                case "EarlyFullReceivingBuffer":
                    yard.rules.Equipment(EquipmentKind.Storage).outputCapacity=1;yard.Put(yard.output,PartKind.Copper,1);yard.Put(machine.id,PartKind.Wire,2);yard.Advance(150);
                    int stable=Total(state,PartKind.Copper,true);Check(stable==7 && model.StoredUnits(yard.output)==1,"Full destination retains all finished output across machine and track.");
                    yard.rules.Equipment(EquipmentKind.Storage).outputCapacity=120;yard.Advance(150);
                    Check(model.StoredUnits(yard.output)==11 && Total(state,PartKind.Copper,true)==7 && machine.job==null,"Receiving capacity resumes conserved queued batches.");break;
                case "EarlyNewArrivalHasFullDuration":
                    state.belts[0].items.Add(new ConveyorItem{id=state.nextId++,kind=PartKind.Wire,progress=.999f});model.Tick(500);yard.automation.Tick(500);
                    Check(machine.job!=null && machine.job.remaining==machine.job.duration && !machine.job.ready && state.belts[1].items.Count==0,"A new arriving unit never inherits earlier processing or transport time.");break;
                case "EarlyPauseHasNoPreparation":
                    var waiting=yard.Put(machine.id,PartKind.Wire,1);int unchanged=state.nextId;Check(!model.Tick(0) && !yard.automation.Tick(0),"Paused gameplay performs no preparation or transfer.");
                    Check(machine.job==null && waiting.quantity==1 && state.nextId==unchanged,"Even an idle manual bench retains input under pause.");
                    model.Tick(.1f);Check(machine.job!=null && machine.job.strokes==0,"Resume prepares a manual batch only.");break;
                case "EarlyCarriedLoopRemainsUsable":
                    var carried=yard.Hold(PartKind.Wire,2,false);Check(model.BeginProcessing(machine.id) && state.carriedId==0 && model.FindItem(carried.id)==null,"Original whole carried stack intake remains usable.");
                    Check(machine.job.inputQuantity==2 && machine.job.yields[0].quantity==6 && machine.job.duration==8 && !machine.job.xpEligible,"Existing carried batch snapshot is preserved.");
                    model.Tick(8);Check(model.CollectOutput(machine.id,0) && model.Carried.quantity==6 && !model.Carried.xpEligible && model.SaleQuote().experience==0,"Manual collection preserves imported recovery history.");break;
                case "EarlyReadOnlyLoadingQuotes":
                    var source=yard.Hold(PartKind.Motor,1);int originalId=state.carriedId,originalNext=state.nextId;string notice=model.LastNotice;yard.rules.Equipment(machine.kind).outputCapacity=4;
                    Check(model.CanDeposit(machine.id,source,out reason) && !model.CanAutoBegin(machine.id,source,out reason) && reason.Contains("Output blocked") && !model.CanBeginProcessing(machine.id,source,out reason),"Future input quote checks complete output reservation, not just raw queue fit.");
                    Check(state.carriedId==originalId && state.nextId==originalNext && machine.contents.Count==0 && machine.job==null && model.LastNotice==notice,"All loading quotes leave ownership, notice and work unchanged.");
                    yard.rules.Equipment(machine.kind).outputCapacity=24;Check(model.CanAutoBegin(machine.id,source,out reason) && model.CanBeginProcessing(machine.id,source,out reason),"Valid full batch preview becomes available without mutation.");break;
                case "EarlyIdentityAndSlotReservations":
                    var reservedSource=yard.Put(machine.id,PartKind.Wire,1);yard.rules.maxStacks=1;int reservedId=reservedSource.id;
                    Check(!model.AutoBegin(machine.id) && machine.job==null && reservedSource.id==reservedId && reservedSource.quantity==1,"Every distinct output slot must fit before input is consumed.");
                    yard.rules.maxStacks=256;var carriedLimit=yard.Hold(PartKind.Wire,1);state.nextId=int.MaxValue;
                    Check(!model.AutoBegin(machine.id) && machine.job==null && reservedSource.quantity==1,"Identity exhaustion preserves queued input.");
                    Check(!model.BeginProcessing(machine.id) && model.Carried.id==carriedLimit.id && carriedLimit.quantity==1,"Identity exhaustion also keeps carried whole-batch input usable elsewhere.");break;
                case "EarlyResumeKeepsLineage":
                    yard.Put(yard.input,PartKind.Wire,3,false);yard.Advance(45);int cash=state.money,xp=state.experience,nextSave=state.nextId;float progress=state.belts[0].items.Count>0?state.belts[0].items[0].progress:0;
                    yard.Resume();Check(yard.State.money==cash && yard.State.experience==xp && yard.State.nextId==nextSave,"Reconstruction retains all purchase and reward state.");
                    if(yard.State.belts[0].items.Count>0)Check(yard.State.belts[0].items[0].progress==progress,"In-flight progress is preserved.");
                    yard.Advance();Check(yard.model.StoredUnits(yard.output)==15 && Total(yard.State,PartKind.Copper,false)==9 && Total(yard.State,PartKind.Insulation,false)==6 && Total(yard.State,PartKind.Wire,false)==0,"Resumed machine/belt chain converts exact original history once.");
                    Check(yard.State.money==cash && yard.State.experience==xp && Total(yard.State,PartKind.Copper,true)==0,"Reload cannot turn imported stock into recovered XP.");break;
                case "EarlySustainedFullInputLine":
                    yard.rules.maxStacks=512;yard.rules.Equipment(EquipmentKind.Storage).outputCapacity=512;yard.Put(yard.input,PartKind.Wire,80);
                    yard.Advance(8000);
                    Check(model.StoredUnits(yard.output)==400 && machine.job==null && model.QueueUnits(machine.id)==0 && model.StoredUnits(yard.input)==0,"Eighty supplied inputs finish without withdrawing jammed raw stock.");
                    Check(Total(state,PartKind.Copper,true)==240 && Total(state,PartKind.Insulation,true)==160 && Total(state,PartKind.Wire,true)==0 && state.experience==0,"Sustained full-queue conversion conserves all400 output units without transport XP.");break;
                case "EarlyBlockedSustainedLineResumes":
                    yard.rules.maxStacks=512;yard.rules.Equipment(EquipmentKind.Storage).outputCapacity=512;yard.Put(yard.input,PartKind.Wire,80);
                    yard.rules.Equipment(EquipmentKind.Storage).outputCapacity=1;yard.Put(yard.output,PartKind.Copper,1);yard.Advance(1800);
                    Check(machine.job!=null && machine.job.ready && model.QueueUnits(machine.id)==24 && state.belts[1].items.Count>0,"A blocked destination fills independent input and output bays, then stops safely.");
                    int ledgerCopper=Total(state,PartKind.Copper,true),ledgerWire=Total(state,PartKind.Wire,true);yard.Resume();
                    Check(Total(yard.State,PartKind.Copper,true)==ledgerCopper && Total(yard.State,PartKind.Wire,true)==ledgerWire,"Blocked full bays reconstruct without loss.");
                    yard.rules.Equipment(EquipmentKind.Storage).outputCapacity=512;yard.Advance(8000);
                    Check(yard.model.StoredUnits(yard.output)==401 && yard.model.QueueUnits(machine.id)==0 && yard.model.FindEquipment(machine.id).job==null,"Clearing downstream capacity resumes all80 inputs without manual queue withdrawal.");
                    Check(Total(yard.State,PartKind.Copper,true)==241 && Total(yard.State,PartKind.Insulation,true)==160 && Total(yard.State,PartKind.Wire,true)==0,"Blocked/resumed ledger retains original stock plus all output units.");break;
                case "EarlyManualSeparateBays":
                {
                    var manual=new Yard(EquipmentKind.Workbench,false);var bench=manual.model.FindEquipment(manual.processor);
                    var input=manual.Put(bench.id,PartKind.Wire,24);Check(manual.model.AutoBegin(bench.id) && input.quantity==23 && manual.model.ReservedOutputUnits(bench.id)==5,"A full manual input bay prepares independent outputs.");
                    var topUp=manual.Hold(PartKind.Wire,1);Check(manual.model.Deposit(bench.id) && manual.model.QueueUnits(bench.id)==24,"Manual input can refill while tools/output reserve remain busy.");
                    Check(manual.model.Tick(500)==false && bench.job.strokes==0,"Full queues do not automate manual tools.");
                    for(int i=0;i<4;i++)Check(manual.model.Work(bench.id),"Perform real manual strokes.");
                    Check(manual.model.QueueUnits(bench.id)==24 && manual.model.OutputQuantity(bench.id)==5 && manual.model.State.experience==0 && topUp.quantity==1,"Manual ready materials coexist with a full input queue.");
                    manual.Validate();break;
                }
                case "EarlyReducedCapacityPreservesBothBays":
                    yard.rules.maxStacks=512;yard.rules.Equipment(EquipmentKind.Storage).outputCapacity=512;yard.Hold(PartKind.Motor,1);Check(model.BeginProcessing(machine.id),"Load actual carried batch.");
                    var queue=yard.Hold(PartKind.Wire,24);Check(model.Deposit(machine.id),"Fill separate input bay.");model.Tick(1);float partial=machine.job.remaining;
                    yard.rules.Equipment(machine.kind).outputCapacity=4;var rejected=yard.Hold(PartKind.Wire,1);
                    Check(!model.CanDeposit(machine.id,out reason) && !model.Deposit(machine.id) && model.Carried.id==rejected.id && queue.quantity==24,"Reduced input capacity preserves existing queue and carried item.");
                    Check(!model.Tick(50) && machine.job.remaining==partial && model.ReservedOutputUnits(machine.id)==10,"Reduced output capacity preserves the independent partial job.");
                    Check(model.Drop(12,.25f,0),"Put rejected item safely aside.");yard.rules.Equipment(machine.kind).outputCapacity=24;yard.Advance(4000);
                    Check(model.StoredUnits(yard.output)==130 && Total(state,PartKind.Wire,true)==1 && machine.job==null && model.QueueUnits(machine.id)==0,"Restored limits process all queued stock; rejected loose item remains owned.");break;
                case "EarlySeparateDurableCapacity":
                {
                    yard.Put(machine.id,PartKind.Wire,4096);machine.job=new ProcessingJob{recipeId=1,input=PartKind.Wire,inputQuantity=1,duration=4,remaining=4,
                        yields=new[]{new PartAmount(PartKind.Copper,4096)}};
                    ScrappingModel.Validate(state,yard.rules);Check(model.QueueUnits(machine.id)==4096 && model.ReservedOutputUnits(machine.id)==4096,"Durable input and output bay limits each allow4096 units without a schema change.");
                    machine.contents[0].quantity=4097;bool invalid=false;try{ScrappingModel.Validate(state,yard.rules);}catch(ArgumentException){invalid=true;}
                    Check(invalid,"An oversized individual input bay is still rejected.");machine.contents[0].quantity=4096;
                    machine.job.yields[0].quantity=4097;invalid=false;try{ScrappingModel.Validate(state,yard.rules);}catch(ArgumentException){invalid=true;}
                    Check(invalid,"An oversized individual output bay is still rejected.");machine.job.yields[0].quantity=4096;break;
                }
                default:throw new Exception("Unknown early machine scenario: "+name);
            }
            yard.Validate();
        }
    }
}
