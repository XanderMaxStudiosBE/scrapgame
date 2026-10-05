using System;
using System.Collections.Generic;

namespace Scrapshift.Compact
{
    public sealed class CompactSaleQuote
    {
        public PartKind kind;
        public string name, reason;
        public int quantity, unitPrice, bonusPercent, baseTotal, bonusTotal, total, experience;
        public bool allowed;
    }

    /// <summary>Authoritative staged scrapping. Timed equipment never consults wall-clock time.</summary>
    public sealed class ScrappingModel
    {
        public CompactRules Rules { get; private set; }
        public CompactYardState State { get; private set; }
        public CompactCareerModel Career { get; private set; }
        public CompactIndustryModel Industry { get; private set; }
        public string LastNotice { get; private set; }
        // Missing or disconnected power safely pauses machines, including immediately after load.
        public Func<int,bool> HasPower;
        public int Level { get { return Rules.LevelForExperience(State.experience); } }
        public int SaleBonusPercent { get { return Rules.BonusForLevel(Level); } }
        public int XPToNextLevel { get { return Level>=Rules.MaxLevel?0:Rules.levelThresholds[Level]-State.experience; } }
        public float LevelFraction
        {
            get
            {
                if(Level>=Rules.MaxLevel) return 1;
                int start=Rules.levelThresholds[Level-1],end=Rules.levelThresholds[Level];
                return (float)(State.experience-start)/(end-start);
            }
        }
        public CompactStack Carried { get { return FindItem(State.carriedId); } }
        public int OccupiedSlots
        {
            get
            {
                int count=State.items.Count;
                foreach(var e in State.equipment) { count+=e.contents.Count; if(e.job!=null) count+=PositiveSlots(e.job.yields);count+=CompactIndustryModel.ReservedSlots(e); }
                foreach(var s in State.scrap) if(s.inspected) count+=PositiveSlots(s.remaining);
                if(State.belts!=null)foreach(var belt in State.belts)count+=belt.items.Count;
                return count;
            }
        }
        public ScrappingModel(CompactRules rules, CompactYardState state=null)
        {
            if(rules==null) throw new ArgumentNullException("rules");
            rules.Validate(); Rules=rules;
            State=state??FreshState(rules);
            Validate(State,rules);
            Career=new CompactCareerModel(this);
            Industry=new CompactIndustryModel(this);
            LastNotice="Inspect delivered scrap, or collect renewable wiring.";
        }
        static CompactYardState FreshState(CompactRules rules)
        {
            var s=new CompactYardState{money=rules.startingMoney};
            s.equipment.Add(new EquipmentState{id=s.nextId++,kind=EquipmentKind.Workbench,x=-5,z=-4,starter=true});
            for(int i=0;i<rules.startingCars;i++) s.scrap.Add(new LargeScrapJob{id=s.nextId++,kind=ScrapObjectKind.Car,x=18-i*3,z=-10});
            for(int i=0;i<rules.startingRefrigerators;i++) s.scrap.Add(new LargeScrapJob{id=s.nextId++,kind=ScrapObjectKind.Refrigerator,x=20-i*2,z=-5});
            return s;
        }
        public CompactStack FindItem(int id) { if(id<=0)return null; foreach(var s in State.items)if(s.id==id)return s;return null; }
        public EquipmentState FindEquipment(int id) { foreach(var e in State.equipment)if(e.id==id)return e;return null; }
        public LargeScrapJob FindScrap(int id) { foreach(var s in State.scrap)if(s.id==id)return s;return null; }
        bool Fail(string reason) { LastNotice=reason;return false; }
        bool CanAllocate { get { return State.nextId>0 && State.nextId<int.MaxValue; } }
        bool CanReserve(int additionalSlots)
        {
            return OccupiedSlots+additionalSlots<=512 && (additionalSlots<=0 || OccupiedSlots+additionalSlots<=Rules.maxStacks);
        }
        static int PositiveSlots(PartAmount[] amounts)
        { int count=0;if(amounts!=null)foreach(var a in amounts){if(a==null)throw new ArgumentException("Missing output slot.");if(a.quantity>0)count++;}return count; }
        static int OutputUnits(PartAmount[] amounts)
        { int count=0;if(amounts!=null)foreach(var a in amounts){if(a==null)throw new ArgumentException("Missing output slot.");count+=a.quantity;}return count; }
        static PartAmount[] CloneYields(PartAmount[] source, int batches=1)
        {
            var copy=new PartAmount[source.Length];
            for(int i=0;i<copy.Length;i++)copy[i]=new PartAmount(source[i].kind,source[i].quantity*batches);
            return copy;
        }
        CompactStack Give(PartKind kind,int quantity,float x,float z,bool eligible)
        {
            var stack=new CompactStack{id=State.nextId++,kind=kind,quantity=quantity,x=x,z=z,xpEligible=eligible};
            State.items.Add(stack);State.carriedId=stack.id;return stack;
        }
        public bool AcquireWire()
        {
            if(State.carriedId!=0) return Fail("Put down or process your carried item first.");
            // Leave enough headroom to turn this raw stack into its distinct saleable outputs.
            // Otherwise a yard filled only with renewable raw wire can never process its first batch.
            int requiredSlots=Math.Max(1,Rules.Recipe(PartKind.Wire).yields.Length);
            if(!CanAllocate || !CanReserve(requiredSlots))return Fail("Keep space to process the wiring; collect and sell an existing output first.");
            Give(PartKind.Wire,Rules.renewableWireQuantity,20,-5,true);
            LastNotice="Collected renewable wiring. Strip it at a workbench; no purchase required.";return true;
        }
        public bool Pickup(int id)
        {
            if(State.carriedId!=0)return Fail("Your hands are full.");
            var item=FindItem(id);if(item==null)return Fail("That item is no longer available.");
            State.carriedId=id;LastNotice="Picked up "+Rules.Part(item.kind).name+" ×"+item.quantity+".";return true;
        }
        public bool Drop(float x,float y,float z)
        {
            var held=Carried;if(held==null)return Fail("Your hands are empty.");
            if(!CompactRules.Finite(x) || !CompactRules.Finite(y) || !CompactRules.Finite(z) || Math.Abs(x)>23.5f || Math.Abs(z)>17.5f || y<0 || y>3)
                return Fail("Choose a clear drop point inside the yard.");
            held.x=x;held.y=y;held.z=z;State.carriedId=0;LastNotice="Item put down.";return true;
        }
        public int RequiredScrapStrokes(int id)
        {var s=FindScrap(id);return s==null?0:s.requiredStrokes>0?s.requiredStrokes:Rules.LargeRecipe(s.kind).strokes;}
        public string ScrapWorkStage(int id)
        {
            var s=FindScrap(id);if(s==null)return "Scrap cleared";
            if(!s.inspected)return "Inspect before dismantling";
            if(s.strokes>=s.requiredStrokes)return "Components ready to remove";
            var r=Rules.LargeRecipe(s.kind);
            return r.strokes==s.requiredStrokes?r.workStages[s.strokes]:"Dismantle stage "+(s.strokes+1)+" / "+s.requiredStrokes;
        }
        public bool InspectScrap(int id)
        {
            var s=FindScrap(id);if(s==null)return Fail("That scrap object is no longer available.");
            if(State.carriedId!=0)return Fail("Put down your carried item before inspecting.");
            if(s.inspected)return Fail("Already inspected; begin the next dismantling stage.");
            var recipe=Rules.LargeRecipe(s.kind);
            if(!CanReserve(recipe.yields.Length))return Fail("Make room for the components before dismantling.");
            s.remaining=CloneYields(recipe.yields);s.requiredStrokes=recipe.strokes;s.inspected=true;
            Career.RecordInspect();
            LastNotice="Inspected "+recipe.name+". "+recipe.strokes+" dismantling stages; recovered components are reserved.";return true;
        }
        public bool WorkScrap(int id)
        {
            var s=FindScrap(id);if(s==null)return Fail("That scrap object is no longer available.");
            if(State.carriedId!=0)return Fail("Put down your carried item to use the manual tools.");
            if(!s.inspected)return Fail("Inspect this object first.");
            if(s.strokes>=s.requiredStrokes)return Fail("Dismantling is complete; remove its components.");
            s.strokes++;if(s.strokes==s.requiredStrokes)Career.RecordDismantle();LastNotice=ScrapWorkStage(id);return true;
        }
        public bool CollectScrap(int id,int outputIndex)
        {
            var s=FindScrap(id);
            if(s==null || !s.inspected || s.strokes<s.requiredStrokes)return Fail("Finish inspecting and dismantling before removing components.");
            if(State.carriedId!=0)return Fail("Your hands are full.");
            if(outputIndex<0 || outputIndex>=s.remaining.Length || s.remaining[outputIndex].quantity<=0)return Fail("That component has already been removed.");
            if(!CanAllocate)return Fail("Item identifier limit reached; the component remains in the scrap object.");
            var output=s.remaining[outputIndex];Give(output.kind,output.quantity,s.x,s.z,true);output.quantity=0;
            if(PositiveSlots(s.remaining)==0)State.scrap.Remove(s);
            LastNotice="Removed "+Rules.Part(output.kind).name+". Carry components to the workbench; materials can be sold.";return true;
        }
        public bool BuyScrap(ScrapObjectKind kind)
        {
            if(!Enum.IsDefined(typeof(ScrapObjectKind),kind))return Fail("Unknown scrap supply.");
            var recipe=Rules.LargeRecipe(kind);
            if(State.money<recipe.purchasePrice)return Fail("Not enough money. Renewable wiring remains free.");
            if(State.scrap.Count>=Rules.maxLargeScrap || State.scrap.Count>=16 || !CanAllocate)return Fail("The delivery area is full.");
            foreach(var s in State.scrap)if(s.kind==kind)return Fail("Clear the existing "+recipe.name+" before ordering another.");
            float x=kind==ScrapObjectKind.Car?18:20,z=kind==ScrapObjectKind.Car?-10:-5;
            float halfX=kind==ScrapObjectKind.Car?1.4f:.8f,halfZ=kind==ScrapObjectKind.Car?2.5f:.8f;
            foreach(var s in State.items)if(s.id!=State.carriedId && Math.Abs(s.x-x)<halfX+.5f && Math.Abs(s.z-z)<halfZ+.5f)
                return Fail("Clear loose items from the delivery bay first.");
            // Purchase only whole nonportable objects; their components require genuine work.
            State.money-=recipe.purchasePrice;State.scrap.Add(new LargeScrapJob{id=State.nextId++,kind=kind,x=x,z=z});
            LastNotice=recipe.name+" delivered for €"+recipe.purchasePrice+". Inspect it at the receiving bay.";return true;
        }
        public bool BeginProcessing(int equipmentId)
        {
            var equipment=FindEquipment(equipmentId);var held=Carried;
            if(equipment==null || (equipment.kind!=EquipmentKind.Workbench && equipment.kind!=EquipmentKind.Tier1Scrapper))
                return Fail("This equipment cannot process portable components.");
            if(equipment.job!=null)return Fail("Finish and collect the existing output before loading another item.");
            if(held==null)return Fail("Carry a supported component to the input area.");
            var recipe=Rules.Recipe(held.kind);
            if(recipe==null || held.quantity<recipe.inputQuantity || held.quantity%recipe.inputQuantity!=0)
                return Fail("No compatible whole-batch recipe for this carried item.");
            int batches=held.quantity/recipe.inputQuantity;long outputUnits=0;
            foreach(var y in recipe.yields)
            {
                long q=(long)y.quantity*batches;if(q>4096)return Fail("This batch is too large; its input is unchanged.");outputUnits+=q;
            }
            var definition=Rules.Equipment(equipment.kind);
            long storedUnits=0;foreach(var item in equipment.contents)storedUnits+=item.quantity;
            if(outputUnits+storedUnits>definition.outputCapacity)return Fail("Output capacity is too small for this batch; input stays in your hands.");
            if(!CanReserve(recipe.yields.Length-1))return Fail("No room to reserve every output; collect and sell existing materials.");
            long strokes=(long)recipe.strokes*batches;
            double duration=(double)recipe.seconds*batches*definition.processingSeconds/Rules.baseMachineSeconds;
            if(strokes>10000 || double.IsNaN(duration) || double.IsInfinity(duration) || duration<=0 || duration>86400)
                return Fail("This batch exceeds processing limits; input is unchanged.");
            bool powered=equipment.kind==EquipmentKind.Tier1Scrapper;
            bool supplied=powered && HasPower!=null && HasPower(equipmentId);
            var job=new ProcessingJob{recipeId=recipe.id,input=held.kind,inputQuantity=held.quantity,requiredStrokes=powered?0:(int)strokes,
                duration=(float)duration,remaining=powered?(float)duration:0,xpEligible=held.xpEligible,yields=CloneYields(recipe.yields,batches)};
            equipment.job=job;State.items.Remove(held);State.carriedId=0;
            LastNotice=powered?"Loaded "+recipe.name+". "+(supplied?"Processing with generator power.":"Connect sufficient generator power to begin."):
                "Loaded "+recipe.name+". Complete "+job.requiredStrokes+" manual tool strokes.";return true;
        }
        public static bool HasBuffer(EquipmentKind kind)
        {return kind==EquipmentKind.Storage || kind==EquipmentKind.Tier2Scrapper || kind==EquipmentKind.Splitter || kind==EquipmentKind.Merger ||
            kind==EquipmentKind.PrimaryScrapper || kind==EquipmentKind.ExportStation;}
        public int StoredUnits(int equipmentId)
        {var e=FindEquipment(equipmentId);int count=0;if(e!=null)foreach(var item in e.contents)count+=item.quantity;return count;}
        public bool Deposit(int equipmentId)
        {
            var e=FindEquipment(equipmentId);var held=Carried;
            if(e==null || !HasBuffer(e.kind))return Fail("Choose storage, a Tier 2 input buffer or a conveyor junction.");
            if(held==null)return Fail("Carry a component or material to deposit it.");
            if(e.kind==EquipmentKind.PrimaryScrapper)return Fail("Primary intake takes intact whole scrap; portable items cannot enter its output buffer.");
            if(e.kind==EquipmentKind.ExportStation && (!CompactIndustryModel.CanExportPart(Rules,held.kind) || (e.filterKind>=0 && (int)held.kind!=e.filterKind)))
                return Fail("Export input requires saleable materials matching its filter. Components must be processed first.");
            if(e.kind==EquipmentKind.Tier2Scrapper && (Rules.Recipe(held.kind)==null || (e.filterKind>=0 && (int)held.kind!=e.filterKind)))
                return Fail("The Tier 2 input needs a supported component matching its recipe filter.");
            int reserved=(e.job==null?0:OutputUnits(e.job.yields))+CompactIndustryModel.ReservedUnits(e);
            if((long)StoredUnits(equipmentId)+reserved+held.quantity>Rules.Equipment(e.kind).outputCapacity)
                return Fail("Buffer is full; reserved machine outputs also occupy capacity. Your carried item is unchanged.");
            State.items.Remove(held);e.contents.Add(held);held.x=e.x;held.z=e.z;held.y=.25f;State.carriedId=0;
            LastNotice="Deposited "+Rules.Part(held.kind).name+" ×"+held.quantity+". Transfers award no sale experience.";return true;
        }
        public bool Withdraw(int equipmentId,int stackId)
        {
            var e=FindEquipment(equipmentId);
            if(e==null || !HasBuffer(e.kind))return Fail("That equipment has no accessible buffer.");
            if(State.carriedId!=0)return Fail("Your hands are full.");
            CompactStack found=null;foreach(var item in e.contents)if(item.id==stackId){found=item;break;}
            if(found==null)return Fail("That stored stack is no longer available.");
            e.contents.Remove(found);State.items.Add(found);State.carriedId=found.id;
            LastNotice="Withdrew "+Rules.Part(found.kind).name+" ×"+found.quantity+". Its recovery history is unchanged.";return true;
        }
        public bool WithdrawBatch(int equipmentId,int stackId)
        {
            var e=FindEquipment(equipmentId);
            if(e==null || !HasBuffer(e.kind))return Fail("That equipment has no accessible buffer.");
            if(State.carriedId!=0)return Fail("Your hands are full.");
            CompactStack selected=null;foreach(var item in e.contents)if(item.id==stackId){selected=item;break;}
            if(selected==null)return Fail("That stored stack is no longer available.");
            long quantity=0;
            foreach(var item in e.contents)
                if(item.kind==selected.kind && item.xpEligible==selected.xpEligible)
                {
                    if(item.quantity<1)return Fail("A stored quantity is invalid; no items were moved.");
                    quantity+=item.quantity;
                    if(quantity>4096)return Fail("This matching batch exceeds the 4096-unit stack limit; withdraw individual stacks instead.");
                }
            // Preserve the chosen identity and retire the other source records once, without reusing IDs.
            for(int i=e.contents.Count-1;i>=0;i--)
                if(e.contents[i].kind==selected.kind && e.contents[i].xpEligible==selected.xpEligible)e.contents.RemoveAt(i);
            selected.quantity=(int)quantity;State.items.Add(selected);State.carriedId=selected.id;
            LastNotice="Withdrew matching "+Rules.Part(selected.kind).name+" ×"+selected.quantity+". Recovery histories remain separate; transfers award no XP.";return true;
        }
        public bool SetFilter(int equipmentId,int kind)
        {
            var e=FindEquipment(equipmentId);
            if(e==null || !HasBuffer(e.kind))return Fail("This equipment has no material or recipe filter.");
            if(kind< -1 || (kind>=0 && !Enum.IsDefined(typeof(PartKind),kind)))return Fail("Choose a supported item filter or All.");
            if(e.kind==EquipmentKind.Tier2Scrapper && kind>=0 && Rules.Recipe((PartKind)kind)==null)
                return Fail("Choose a supported component recipe for Tier 2.");
            if(e.kind==EquipmentKind.ExportStation && kind>=0 && !CompactIndustryModel.CanExportPart(Rules,(PartKind)kind))
                return Fail("Choose a saleable material filter for export, or All materials.");
            if(e.filterKind==kind)return Fail("This filter is already selected.");
            e.filterKind=kind;
            LastNotice=kind<0?"Filter: all compatible items.":"Filter: "+Rules.Part((PartKind)kind).name+". Existing contents and jobs are preserved.";return true;
        }
        public bool AutoBegin(int equipmentId)
        {
            var e=FindEquipment(equipmentId);
            if(e==null || e.kind!=EquipmentKind.Tier2Scrapper)return Fail("Automatic intake requires a Tier 2 scrapper.");
            if(e.job!=null)return Fail("Finish and clear the reserved Tier 2 output before the next batch.");
            if(!CanAllocate)return Fail("No safe output identities remain; buffered input is preserved.");
            foreach(var source in e.contents)
            {
                if(e.filterKind>=0 && (int)source.kind!=e.filterKind)continue;
                var recipe=Rules.Recipe(source.kind);if(recipe==null)continue;
                int available=0;foreach(var input in e.contents)if(input.kind==source.kind && input.xpEligible==source.xpEligible)available+=input.quantity;
                if(available<recipe.inputQuantity)continue;
                int removedSlots=0,left=recipe.inputQuantity;
                foreach(var input in e.contents)
                    if(input.kind==source.kind && input.xpEligible==source.xpEligible && left>0)
                    {int consumed=Math.Min(left,input.quantity);left-=consumed;if(consumed==input.quantity)removedSlots++;}
                int outputUnits=OutputUnits(recipe.yields),afterUnits=StoredUnits(equipmentId)-recipe.inputQuantity+outputUnits;
                if(afterUnits>Rules.Equipment(e.kind).outputCapacity || !CanReserve(recipe.yields.Length-removedSlots) ||
                    (long)State.nextId+outputUnits>int.MaxValue)continue;
                double duration=(double)recipe.seconds*Rules.Equipment(e.kind).processingSeconds/Rules.baseMachineSeconds;
                if(double.IsNaN(duration) || double.IsInfinity(duration) || duration<=0 || duration>86400)continue;
                // Build the complete durable result before consuming one source-qualified recipe batch.
                var job=new ProcessingJob{recipeId=recipe.id,input=source.kind,inputQuantity=recipe.inputQuantity,duration=(float)duration,
                    remaining=(float)duration,xpEligible=source.xpEligible,yields=CloneYields(recipe.yields)};
                var inputKind=source.kind;bool eligible=source.xpEligible;left=recipe.inputQuantity;
                for(int i=0;i<e.contents.Count && left>0;)
                {
                    var input=e.contents[i];if(input.kind!=inputKind || input.xpEligible!=eligible){i++;continue;}
                    int consumed=Math.Min(left,input.quantity);input.quantity-=consumed;left-=consumed;
                    if(input.quantity==0)e.contents.RemoveAt(i);else i++;
                }
                e.job=job;LastNotice="Tier 2 reserved one "+recipe.name+" batch. Generator power advances processing.";return true;
            }
            return Fail("Waiting for a compatible full input batch and enough reserved output capacity.");
        }
        public bool Work(int equipmentId)
        {
            var e=FindEquipment(equipmentId);
            if(e==null || e.kind!=EquipmentKind.Workbench || e.job==null)return Fail("Load a component into a manual workbench first.");
            if(State.carriedId!=0)return Fail("Put down your carried item before using the tools.");
            if(e.job.ready)return Fail("Output is ready; collect it before working again.");
            e.job.strokes++;if(e.job.strokes==e.job.requiredStrokes){e.job.ready=true;Career.RecordProcessing(e.kind);}
            LastNotice=e.job.ready?"Recovered materials are ready to collect.":"Manual work "+e.job.strokes+" / "+e.job.requiredStrokes+".";return true;
        }
        public int OutputQuantity(int equipmentId)
        {var e=FindEquipment(equipmentId);return e==null || e.job==null?0:OutputUnits(e.job.yields);}
        public string ProcessingBlockReason(int equipmentId)
        {
            var e=FindEquipment(equipmentId);if(e==null)return "Equipment unavailable";
            if(e.job==null)return "Awaiting input";
            if(e.job.ready)return "Collect recovered output";
            long stored=0;foreach(var item in e.contents)stored+=item.quantity;
            if(stored+OutputUnits(e.job.yields)>Rules.Equipment(e.kind).outputCapacity)return "Output blocked: make space; processing progress is preserved";
            if((e.kind==EquipmentKind.Tier1Scrapper || e.kind==EquipmentKind.Tier2Scrapper) && (HasPower==null || !HasPower(equipmentId)))return "Insufficient or disconnected power; processing progress is preserved";
            return e.kind==EquipmentKind.Workbench?"Manual work required":"Processing";
        }
        public bool Tick(float delta)
        {
            if(!CompactRules.Finite(delta) || delta<0)throw new ArgumentOutOfRangeException("delta","Elapsed gameplay time must be finite and nonnegative.");
            if(delta==0)return false;
            bool changed=false;
            foreach(var e in State.equipment)
            {
                if((e.kind!=EquipmentKind.Tier1Scrapper && e.kind!=EquipmentKind.Tier2Scrapper) || e.job==null || e.job.ready || HasPower==null || !HasPower(e.id))continue;
                long stored=0;foreach(var item in e.contents)stored+=item.quantity;
                if(stored+OutputUnits(e.job.yields)>Rules.Equipment(e.kind).outputCapacity)continue;
                e.job.remaining=Math.Max(0,e.job.remaining-delta);if(e.job.remaining==0){e.job.ready=true;Career.RecordProcessing(e.kind);}changed=true;
            }
            return changed;
        }
        public bool CollectOutput(int equipmentId,int outputIndex)
        {
            var e=FindEquipment(equipmentId);
            if(e==null || e.job==null || !e.job.ready)return Fail("Finish processing before collecting output.");
            if(State.carriedId!=0)return Fail("Your hands are full.");
            if(outputIndex<0 || outputIndex>=e.job.yields.Length || e.job.yields[outputIndex].quantity<=0)
                return Fail("That output has already been collected.");
            if(!CanAllocate)return Fail("Item identifier limit reached; output remains reserved in the equipment.");
            var output=e.job.yields[outputIndex];Give(output.kind,output.quantity,e.x,e.z,e.job.xpEligible);output.quantity=0;
            if(PositiveSlots(e.job.yields)==0)e.job=null;
            LastNotice="Collected "+Rules.Part(output.kind).name+" ×"+Carried.quantity+". Sale experience is awarded only when sold.";return true;
        }
        public CompactSaleQuote SaleQuote()
        {return QuoteSale(Carried==null?0:Carried.quantity);}
        internal CompactSaleQuote QuoteSale(int quantity)
        {
            var held=Carried;if(held==null)return new CompactSaleQuote{reason="Carry recovered materials to sell."};
            var q=new CompactSaleQuote();
            if(quantity<1 || quantity>held.quantity){q.reason="Choose a valid quantity from your carried bundle.";return q;}
            return QuoteSale(held.kind,quantity,held.xpEligible,false);
        }
        internal CompactSaleQuote QuoteMaterialSale(PartKind kind,int quantity,bool eligible)
        {return QuoteSale(kind,quantity,eligible,true);}
        CompactSaleQuote QuoteSale(PartKind kind,int quantity,bool eligible,bool materialOnly)
        {
            var q=new CompactSaleQuote{kind=kind,quantity=quantity};var part=Rules.Part(kind);
            if(part==null || quantity<1 || quantity>4096){q.reason="Choose a supported sale bundle within the durable unit limit.";return q;}
            if(part.unitPrice<0 || part.unitPrice>100000 || part.saleXp<0 || part.saleXp>10000)
            {q.reason="Correct invalid sale prices or experience tuning before selling. Your stock is unchanged.";return q;}
            q.name=part.name;q.unitPrice=part.unitPrice;q.bonusPercent=SaleBonusPercent;
            bool restored=!materialOnly && (kind==PartKind.RestoredFan || kind==PartKind.RestoredRadio);
            if((!part.isMaterial && !restored) || part.unitPrice==0){q.reason="Dismantle this component into saleable materials first.";return q;}
            long baseTotal=(long)part.unitPrice*quantity,bonus=baseTotal*q.bonusPercent/100,total=baseTotal+bonus;
            long xp=eligible && part.isMaterial?(long)part.saleXp*quantity:0;
            if(total>int.MaxValue || total>int.MaxValue-(long)State.money || xp>int.MaxValue-(long)State.experience)
            {q.reason="Sale exceeds the cash or experience limit; your item is unchanged.";return q;}
            q.baseTotal=(int)baseTotal;q.bonusTotal=(int)bonus;q.total=(int)total;q.experience=(int)xp;q.allowed=true;
            q.reason=eligible?"Valid recovered-material sale":"Existing/imported item: cash sale, no new recovery XP";return q;
        }
        public bool CanSell(out string reason) {var q=SaleQuote();reason=q.reason;return q.allowed;}
        public bool CanSell() {return SaleQuote().allowed;}
        public bool Sell()
        {
            var quote=SaleQuote();if(!quote.allowed)return Fail(quote.reason);
            int oldLevel=Level;CommitSale(quote,0);
            LastNotice="Sold "+quote.name+" ×"+quote.quantity+" for €"+quote.total+" (+"+quote.experience+" XP).";
            if(Level>oldLevel)
            {
                LastNotice+=" Level "+Level+": "+SaleBonusPercent+"% reputation bonus.";
                foreach(var e in Rules.equipment)if(e.unlockLevel>oldLevel && e.unlockLevel<=Level)
                    LastNotice+=" "+e.name+(e.available?" is now purchasable.":" level requirement reached; unavailable in this version.");
            }
            return true;
        }
        internal void CommitSale(CompactSaleQuote quote,int completionBonus)
        {
            var held=Carried;bool eligible=held.xpEligible;
            held.quantity-=quote.quantity;
            if(held.quantity==0){State.items.Remove(held);State.carriedId=0;}
            CreditSale(quote,completionBonus,eligible);
        }
        void CreditSale(CompactSaleQuote quote,int completionBonus,bool eligible)
        {State.money+=quote.total+completionBonus;State.experience+=quote.experience;Career.RecordSale(quote.kind,quote.quantity,quote.total,eligible);}
        internal bool CommitBufferSale(EquipmentState equipment,CompactSaleQuote quote,bool eligible)
        {
            long quantity=0;foreach(var item in equipment.contents)if(item.kind==quote.kind && item.xpEligible==eligible)quantity+=item.quantity;
            if(!quote.allowed || quantity!=quote.quantity)return false;
            var fresh=QuoteMaterialSale(quote.kind,quote.quantity,eligible);
            if(!fresh.allowed || fresh.total!=quote.total || fresh.experience!=quote.experience)return false;
            for(int i=equipment.contents.Count-1;i>=0;i--)if(equipment.contents[i].kind==quote.kind && equipment.contents[i].xpEligible==eligible)equipment.contents.RemoveAt(i);
            CreditSale(fresh,0,eligible);return true;
        }
        internal void CareerNotice(string notice){LastNotice=notice;}
        public static void Validate(CompactYardState s, CompactRules rules)
        {
            if(s==null || rules==null)throw new ArgumentException("Missing compact yard or rules.");
            rules.Validate();
            CompactCareerModel.Validate(s.career,rules);
            if((s.version!=2 && s.version!=3 && s.version!=4) || s.money<0 || s.experience<0 || s.nextId<1 || s.carriedId<0 || s.items==null || s.scrap==null || s.equipment==null || s.powerLinks==null ||
                (s.version>=3 && s.belts==null) || (s.belts!=null && (s.belts.Count>128 || (s.version==2 && s.belts.Count>0))) ||
                s.items.Count>512 || s.scrap.Count>16 || s.equipment.Count>128 || s.powerLinks.Count>256 ||
                !CompactRules.Finite(s.playerX) || !CompactRules.Finite(s.playerY) || !CompactRules.Finite(s.playerZ) ||
                Math.Abs(s.playerX)>23.5f || Math.Abs(s.playerZ)>17.5f || s.playerY<.1f || s.playerY>5 ||
                !CompactRules.Finite(s.yaw) || Math.Abs(s.yaw)>360000 || !CompactRules.Finite(s.pitch) || Math.Abs(s.pitch)>89)
                throw new ArgumentException("Invalid compact-yard state.");
            if(s.importedLegacy && string.IsNullOrEmpty(s.legacySnapshot))throw new ArgumentException("Imported progress requires its recoverable source snapshot.");
            var ids=new HashSet<int>();int occupied=0;
            foreach(var item in s.items){ValidateStack(item,s.nextId,ids);occupied++;}
            if(s.carriedId!=0){bool found=false;foreach(var item in s.items)if(item.id==s.carriedId)found=true;if(!found)throw new ArgumentException("Carried item is missing.");}
            foreach(var large in s.scrap)
            {
                if(large==null || !ids.Add(large.id) || large.id<1 || large.id>=s.nextId || !Enum.IsDefined(typeof(ScrapObjectKind),large.kind) ||
                    !CompactRules.Finite(large.x) || !CompactRules.Finite(large.z) || Math.Abs(large.x)>22 || Math.Abs(large.z)>15.5f ||
                    large.strokes<0 || large.requiredStrokes<0 || large.requiredStrokes>100)
                    throw new ArgumentException("Invalid large-scrap object.");
                if(!large.inspected)
                {
                    if(large.strokes!=0 || large.requiredStrokes!=0 || (large.remaining!=null && large.remaining.Length!=0))throw new ArgumentException("Uninspected scrap has processing data.");
                }
                else
                {
                    CompactRules.ValidateYields(large.remaining,true);
                    if(large.requiredStrokes<1 || large.strokes>large.requiredStrokes || PositiveSlots(large.remaining)==0)
                        throw new ArgumentException("Invalid dismantling progress or exhausted object.");
                    if(large.strokes<large.requiredStrokes)foreach(var output in large.remaining)if(output.quantity==0)throw new ArgumentException("An unfinished object has already yielded a component.");
                    occupied+=PositiveSlots(large.remaining);
                }
            }
            bool hasBench=false;
            foreach(var e in s.equipment)
            {
                if(e==null || !ids.Add(e.id) || e.id<1 || e.id>=s.nextId || !Enum.IsDefined(typeof(EquipmentKind),e.kind) ||
                    !CompactRules.Finite(e.x) || !CompactRules.Finite(e.z) || !CompactRules.Finite(e.yaw) || Math.Abs(e.x)>24 || Math.Abs(e.z)>18 ||
                    Math.Abs(e.yaw)>360000 || e.paidPrice<0 || e.paidPrice>1000000 || e.contents==null || e.contents.Count>512 ||
                    e.kind==EquipmentKind.Conveyor || (s.version<4 && CompactIndustryModel.IsIndustrial(e.kind)) ||
                    (s.version==2 && e.kind!=EquipmentKind.Workbench && e.kind!=EquipmentKind.Generator && e.kind!=EquipmentKind.Tier1Scrapper) ||
                    (s.version>=3 && (e.filterKind< -1 || e.filterKind>11 || (!HasBuffer(e.kind) && e.filterKind!=-1))) || e.routeCursor<0)
                    throw new ArgumentException("Invalid equipment identity, placement or contents.");
                if(e.kind==EquipmentKind.Workbench)hasBench=true;
                if(e.kind==EquipmentKind.ExportStation && e.filterKind>=0 && !rules.Part((PartKind)e.filterKind).isMaterial)
                    throw new ArgumentException("An export filter must select a material.");
                long stored=0;foreach(var item in e.contents)
                {
                    ValidateStack(item,s.nextId,ids);stored+=item.quantity;occupied++;
                    if(e.kind==EquipmentKind.ExportStation && !rules.Part(item.kind).isMaterial)throw new ArgumentException("Export buffers contain materials only.");
                }
                occupied+=CompactIndustryModel.ValidateEquipment(e,s.version,s.nextId,ids);
                if(stored>4096 || stored+(e.job==null?0:OutputUnits(e.job.yields))+CompactIndustryModel.ReservedUnits(e)>4096)throw new ArgumentException("Stored contents and reserved output exceed the durable safety limit.");
                if(e.job==null)continue;
                var j=e.job;CompactRules.ValidateYields(j.yields,true);
                if((e.kind!=EquipmentKind.Workbench && e.kind!=EquipmentKind.Tier1Scrapper && e.kind!=EquipmentKind.Tier2Scrapper) || j.recipeId<1 || !Enum.IsDefined(typeof(PartKind),j.input) ||
                    j.inputQuantity<1 || j.inputQuantity>4096 || j.requiredStrokes<0 || j.requiredStrokes>10000 || j.strokes<0 || j.strokes>j.requiredStrokes ||
                    !CompactRules.Finite(j.duration) || j.duration<=0 || j.duration>86400 || !CompactRules.Finite(j.remaining) || j.remaining<0 || j.remaining>j.duration ||
                    PositiveSlots(j.yields)==0 || OutputUnits(j.yields)>4096)
                    throw new ArgumentException("Invalid durable processing snapshot.");
                if(e.kind==EquipmentKind.Workbench && (j.requiredStrokes<1 || j.remaining!=0 || j.ready!=(j.strokes==j.requiredStrokes)))
                    throw new ArgumentException("Manual job progress is inconsistent.");
                if((e.kind==EquipmentKind.Tier1Scrapper || e.kind==EquipmentKind.Tier2Scrapper) && (j.requiredStrokes!=0 || j.strokes!=0 || j.ready!=(j.remaining==0)))
                    throw new ArgumentException("Powered job progress is inconsistent.");
                if(!j.ready)foreach(var output in j.yields)if(output.quantity==0)throw new ArgumentException("An unfinished job has already produced output.");
                occupied+=PositiveSlots(j.yields);
            }
            if(s.belts!=null)foreach(var belt in s.belts)
            {
                if(belt==null || belt.id<1 || belt.id>=s.nextId || !ids.Add(belt.id) || belt.fromId<1 || belt.toId<1 || belt.fromId==belt.toId ||
                    belt.fromPort<0 || belt.toPort<0 || belt.paidPrice<0 || belt.paidPrice>1000000 || !CompactRules.Finite(belt.launchRemaining) ||
                    belt.launchRemaining<0 || belt.launchRemaining>30 || belt.items==null || belt.items.Count>32)
                    throw new ArgumentException("Invalid conveyor identity, metadata or durable capacity.");
                foreach(var item in belt.items)
                {
                    if(item==null || item.id<1 || item.id>=s.nextId || !ids.Add(item.id) || !Enum.IsDefined(typeof(PartKind),item.kind) || item.quantity!=1 ||
                        !CompactRules.Finite(item.progress) || item.progress<0 || item.progress>1)
                        throw new ArgumentException("Invalid conveyor item or duplicate identity.");
                    occupied++;
                }
            }
            if(!hasBench)throw new ArgumentException("A compact yard must retain one manual workbench.");
            if(occupied>512)throw new ArgumentException("Portable inventory and reserved outputs exceed the safety limit.");
        }
        static void ValidateStack(CompactStack item,int nextId,HashSet<int> ids)
        {
            if(item==null || item.id<1 || item.id>=nextId || !ids.Add(item.id) || !Enum.IsDefined(typeof(PartKind),item.kind) || item.quantity<1 || item.quantity>4096 ||
                !CompactRules.Finite(item.x) || !CompactRules.Finite(item.y) || !CompactRules.Finite(item.z) || Math.Abs(item.x)>24 || Math.Abs(item.z)>18 || item.y<0 || item.y>3)
                throw new ArgumentException("Invalid portable stack or duplicate identifier.");
        }
    }
}
