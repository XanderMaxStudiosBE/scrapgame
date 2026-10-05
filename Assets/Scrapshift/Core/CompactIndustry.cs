using System;
using System.Collections.Generic;

namespace Scrapshift.Compact
{
    [Serializable] public sealed class PrimaryScrapJob
    {
        public int id;
        public ScrapObjectKind kind;
        public float duration,remaining;
        public bool xpEligible;
        public PartAmount[] yields;
    }
    [Serializable] public sealed class IndustrialMachineState
    {
        public int version=1;
        public bool enabled;
        public ScrapObjectKind purchaseKind;
        public float remaining;
        public PrimaryScrapJob primary;
        public int objectsProcessed,exportedUnits,exportedRevenue,exportedXp;
    }

    /// <summary>Explicit standing deliveries, whole-object processing and material dispatch. Gameplay time only.</summary>
    public sealed class CompactIndustryModel
    {
        public const int MaximumEventsPerTick=8192;
        readonly ScrappingModel model;
        // Keep sub-float countdown precision between frames. Serialized float clocks remain authoritative after a load/edit.
        readonly Dictionary<object,double> clocks=new Dictionary<object,double>();
        readonly List<EquipmentState> machines=new List<EquipmentState>();
        EquipmentState[] observed=new EquipmentState[0];
        int[] observedIds=new int[0];
        public string LastMessage {get;private set;}
        public CompactIndustryModel(ScrappingModel model)
        {if(model==null)throw new ArgumentNullException("model");this.model=model;LastMessage="Industry is opt-in. Manual work and ordinary sales remain available.";}
        public static bool IsIndustrial(EquipmentKind kind)
        {return kind==EquipmentKind.PrimaryScrapper || kind==EquipmentKind.ExportStation;}
        public static bool CanExportPart(CompactRules rules,PartKind kind)
        {var part=rules.Part(kind);return part!=null && part.isMaterial && part.unitPrice>0;}
        public static int ReservedUnits(EquipmentState equipment)
        {
            int total=0;var job=equipment==null || equipment.industry==null?null:equipment.industry.primary;
            if(job!=null)foreach(var yield in job.yields)total+=yield.quantity;return total;
        }
        public static int ReservedSlots(EquipmentState equipment)
        {var job=equipment==null || equipment.industry==null?null:equipment.industry.primary;return job==null?0:job.yields.Length;}
        bool Fail(string reason){LastMessage=reason;model.CareerNotice(reason);return false;}
        bool Supported(EquipmentState equipment)
        {return model.State.version==4 && equipment!=null && IsIndustrial(equipment.kind);}
        bool Powered(EquipmentState equipment){return model.HasPower!=null && model.HasPower(equipment.id);}
        string ScrapName(ScrapObjectKind kind)
        {var recipe=model.Rules.LargeRecipe(kind);return recipe==null?kind.ToString():recipe.name;}
        IndustrialMachineState Ensure(EquipmentState equipment)
        {
            if(equipment.industry==null)equipment.industry=new IndustrialMachineState{remaining=Interval(equipment)};
            return equipment.industry;
        }
        float Interval(EquipmentState equipment)
        {return equipment.kind==EquipmentKind.PrimaryScrapper?model.Rules.deliveryIntervalSeconds:model.Rules.Equipment(equipment.kind).processingSeconds;}
        bool ValidInterval(EquipmentState equipment)
        {float interval=Interval(equipment);return CompactRules.Finite(interval) && interval>0 && interval<=86400;}
        public bool SetEnabled(int equipmentId,bool enabled)
        {
            var equipment=model.FindEquipment(equipmentId);if(!Supported(equipment))return Fail("Choose placed schema-4 industrial equipment.");
            if((equipment.industry!=null && equipment.industry.enabled==enabled) || (equipment.industry==null && !enabled))return Fail("This service is already "+(enabled?"enabled.":"disabled."));
            if(enabled && !ValidInterval(equipment))return Fail("Correct the invalid industrial interval before enabling this service.");
            Ensure(equipment).enabled=enabled;
            LastMessage=equipment.kind==EquipmentKind.PrimaryScrapper?(enabled?"Standing deliveries enabled. Purchases require power, money and reserved capacity.":"Standing deliveries disabled. The already paid object can finish with power."):
                (enabled?"Automatic material dispatch enabled. Each dispatch requires power and a valid sale quote.":"Automatic dispatch disabled. Stored materials remain available.");
            model.CareerNotice(LastMessage);return true;
        }
        public bool SetPurchaseKind(int equipmentId,ScrapObjectKind kind)
        {
            var equipment=model.FindEquipment(equipmentId);
            if(!Supported(equipment) || equipment.kind!=EquipmentKind.PrimaryScrapper)return Fail("Choose a primary scrapper for standing deliveries.");
            if(!Enum.IsDefined(typeof(ScrapObjectKind),kind))return Fail("Choose a car or refrigerator.");
            if((equipment.industry==null?ScrapObjectKind.Car:equipment.industry.purchaseKind)==kind)return Fail("That delivery kind is already selected.");
            if(equipment.industry==null && !ValidInterval(equipment))return Fail("Correct the invalid standing-delivery interval before configuring this service.");
            Ensure(equipment).purchaseKind=kind;LastMessage="Next standing delivery: "+model.Rules.LargeRecipe(kind).name+". Paid processing and its saved yields are unchanged.";
            model.CareerNotice(LastMessage);return true;
        }
        bool CanIntake(EquipmentState equipment,ScrapObjectKind kind,bool purchase,out string reason)
        {
            reason="Choose a primary scrapper.";
            if(!Supported(equipment) || equipment.kind!=EquipmentKind.PrimaryScrapper)return false;
            if(equipment.industry!=null && equipment.industry.primary!=null){reason="This primary already holds one paid object.";return false;}
            var recipe=model.Rules.LargeRecipe(kind);if(recipe==null){reason="Choose a supported whole-object recipe.";return false;}
            var definition=model.Rules.Equipment(equipment.kind);
            if(!ValidInterval(equipment) || !CompactRules.Finite(definition.processingSeconds) || definition.processingSeconds<=0 || definition.processingSeconds>86400 || definition.outputCapacity<1 || definition.outputCapacity>4096)
            {reason="Correct invalid primary duration, delivery interval or output capacity before intake.";return false;}
            try{CompactRules.ValidateYields(recipe.yields,false);}catch(ArgumentException){reason="Correct the invalid whole-object yields before intake. The owned object and money are unchanged.";return false;}
            if(recipe.purchasePrice<0 || recipe.purchasePrice>100000){reason="Correct the invalid whole-object purchase price before intake.";return false;}
            if(purchase && model.State.money<recipe.purchasePrice){reason="Waiting for €"+recipe.purchasePrice+". No debt or delivery fee is charged.";return false;}
            long units=model.StoredUnits(equipment.id);foreach(var yield in recipe.yields)units+=yield.quantity;
            if(units>model.Rules.Equipment(equipment.kind).outputCapacity || units>4096){reason="Drain the primary buffer before reserving every component output.";return false;}
            if((long)model.OccupiedSlots+recipe.yields.Length>Math.Min(512,model.Rules.maxStacks)){reason="No room to reserve every primary output stack.";return false;}
            if((long)model.State.nextId+recipe.yields.Length+(purchase?1:0)>int.MaxValue){reason="No safe output identities remain; the whole object is unchanged.";return false;}
            reason="Intact scrap can be fed. Generator power advances its processing.";return true;
        }
        public bool CanFeedScrap(int equipmentId,int scrapId,out string reason)
        {
            var scrap=model.FindScrap(scrapId);reason="Choose an owned receiving-area car or refrigerator.";
            if(scrap==null)return false;
            if(scrap.inspected || scrap.strokes!=0 || scrap.requiredStrokes!=0 || (scrap.remaining!=null && scrap.remaining.Length!=0))
            {reason="Finish partially inspected/dismantled scrap manually. Primary intake accepts intact uninspected objects only.";return false;}
            return CanIntake(model.FindEquipment(equipmentId),scrap.kind,false,out reason);
        }
        PrimaryScrapJob Snapshot(EquipmentState equipment,int id,ScrapObjectKind kind)
        {
            var source=model.Rules.LargeRecipe(kind).yields;var yields=new PartAmount[source.Length];
            for(int i=0;i<source.Length;i++)yields[i]=new PartAmount(source[i].kind,source[i].quantity);
            float duration=model.Rules.Equipment(equipment.kind).processingSeconds;
            return new PrimaryScrapJob{id=id,kind=kind,duration=duration,remaining=duration,xpEligible=true,yields=yields};
        }
        public bool FeedScrap(int equipmentId,int scrapId)
        {
            string reason;if(!CanFeedScrap(equipmentId,scrapId,out reason))return Fail(reason);
            var equipment=model.FindEquipment(equipmentId);var scrap=model.FindScrap(scrapId);var job=Snapshot(equipment,scrap.id,scrap.kind);
            Ensure(equipment).primary=job;model.State.scrap.Remove(scrap);
            LastMessage="Fed owned "+model.Rules.LargeRecipe(scrap.kind).name+" without another purchase. Its identity and outputs are reserved.";
            model.CareerNotice(LastMessage);return true;
        }
        CompactStack DispatchSource(EquipmentState equipment,out int quantity)
        {
            quantity=0;CompactStack selected=null;
            foreach(var item in equipment.contents)
            {
                var part=model.Rules.Part(item.kind);
                if(part==null || !part.isMaterial || (equipment.filterKind>=0 && (int)item.kind!=equipment.filterKind))continue;
                if(selected==null)selected=item;
                if(item.kind==selected.kind && item.xpEligible==selected.xpEligible)quantity+=item.quantity;
            }
            return selected;
        }
        public CompactSaleQuote DispatchQuote(int equipmentId)
        {
            var equipment=model.FindEquipment(equipmentId);
            if(!Supported(equipment) || equipment.kind!=EquipmentKind.ExportStation)return new CompactSaleQuote{reason="Choose an export station."};
            if(!ValidInterval(equipment))return new CompactSaleQuote{reason="Correct the invalid dispatch interval before selling. Stored materials are preserved."};
            int quantity;var selected=DispatchSource(equipment,out quantity);
            if(selected==null)return new CompactSaleQuote{reason="Waiting for saleable materials matching the export filter."};
            var quote=model.QuoteMaterialSale(selected.kind,quantity,selected.xpEligible);
            if(!Powered(equipment)){quote.allowed=false;quote.reason="Connect sufficient generator power before dispatch. Stock and sale eligibility are preserved.";}
            return quote;
        }
        public bool DispatchNow(int equipmentId)
        {
            var quote=DispatchQuote(equipmentId);if(!quote.allowed)return Fail(quote.reason);
            var equipment=model.FindEquipment(equipmentId);int quantity;var source=DispatchSource(equipment,out quantity);bool eligible=source.xpEligible;
            if(!model.CommitBufferSale(equipment,quote,eligible))return Fail("The dispatch stock changed; review a fresh quote.");
            var state=Ensure(equipment);state.exportedUnits=Add(state.exportedUnits,quote.quantity);state.exportedRevenue=Add(state.exportedRevenue,quote.total);state.exportedXp=Add(state.exportedXp,quote.experience);
            SetClock(state,Interval(equipment));
            LastMessage="Exported "+quote.name+" ×"+quote.quantity+" for €"+quote.total+" (+"+quote.experience+" XP). No customer completion bonus.";
            model.CareerNotice(LastMessage);return true;
        }
        static int Add(int value,int amount){return (int)Math.Min(int.MaxValue,(long)value+amount);}
        List<EquipmentState> Machines()
        {
            bool same=observed.Length==model.State.equipment.Count;
            if(same)for(int i=0;i<observed.Length;i++)if(!ReferenceEquals(observed[i],model.State.equipment[i]) || observedIds[i]!=model.State.equipment[i].id){same=false;break;}
            if(same)return machines;
            observed=model.State.equipment.ToArray();observedIds=new int[observed.Length];machines.Clear();
            for(int i=0;i<observed.Length;i++){observedIds[i]=observed[i].id;if(IsIndustrial(observed[i].kind))machines.Add(observed[i]);}
            // Scarce funds and simultaneous completions resolve by durable ID, independent of serialized collection order.
            machines.Sort((first,second)=>first.id.CompareTo(second.id));return machines;
        }
        bool CanComplete(EquipmentState equipment)
        {
            return (long)model.StoredUnits(equipment.id)+ReservedUnits(equipment)<=model.Rules.Equipment(equipment.kind).outputCapacity &&
                (long)model.State.nextId+ReservedSlots(equipment)<=int.MaxValue;
        }
        void Complete(EquipmentState equipment)
        {
            var state=equipment.industry;var job=state.primary;var output=new List<CompactStack>();int nextId=model.State.nextId;
            foreach(var yield in job.yields)output.Add(new CompactStack{id=nextId++,kind=yield.kind,quantity=yield.quantity,x=equipment.x,z=equipment.z,xpEligible=job.xpEligible});
            equipment.contents.AddRange(output);model.State.nextId=nextId;clocks.Remove(job);state.primary=null;state.objectsProcessed=Add(state.objectsProcessed,1);
            model.Career.RecordDismantle();model.Career.RecordProcessing(EquipmentKind.PrimaryScrapper);
            LastMessage="Primary processed "+ScrapName(job.kind)+" once. Components are in its output buffer; processing awards no sale XP.";
            model.CareerNotice(LastMessage);
        }
        void Purchase(EquipmentState equipment)
        {
            var state=equipment.industry;var kind=state.purchaseKind;var recipe=model.Rules.LargeRecipe(kind);var job=Snapshot(equipment,model.State.nextId,kind);
            model.State.money-=recipe.purchasePrice;model.State.nextId++;state.primary=job;SetClock(state,Interval(equipment));
            LastMessage="Standing delivery: "+recipe.name+" bought for €"+recipe.purchasePrice+" and reserved in the primary.";model.CareerNotice(LastMessage);
        }
        double Clock(object owner,float visible)
        {
            double value;if(!clocks.TryGetValue(owner,out value) || (float)value!=visible){value=visible;clocks[owner]=value;}return value;
        }
        void SetClock(IndustrialMachineState state,double value){clocks[state]=value;state.remaining=(float)value;}
        void SetClock(PrimaryScrapJob job,double value){clocks[job]=value;job.remaining=(float)value;}
        bool Advancing(EquipmentState equipment,out object owner,out double remaining)
        {
            owner=null;remaining=0;var state=equipment.industry;if(state==null || !Powered(equipment))return false;
            if(equipment.kind==EquipmentKind.PrimaryScrapper)
            {
                if(state.primary!=null)
                {if(!CanComplete(equipment))return false;owner=state.primary;remaining=Clock(state.primary,state.primary.remaining);return remaining>0;}
                string reason;if(!state.enabled || !CanIntake(equipment,state.purchaseKind,true,out reason))return false;
            }
            else if(!state.enabled || !DispatchQuote(equipment.id).allowed)return false;
            owner=state;remaining=Clock(state,state.remaining);return remaining>0;
        }
        void CheckTickBudget(float delta,List<EquipmentState> activeMachines)
        {
            double transitions=0;
            foreach(var equipment in activeMachines)
            {
                if(!IsIndustrial(equipment.kind) || equipment.industry==null || !Powered(equipment))continue;
                var state=equipment.industry;
                if(equipment.kind==EquipmentKind.PrimaryScrapper)
                {
                    if(state.primary!=null)transitions++;
                    if(state.enabled && ValidInterval(equipment) && model.Rules.Equipment(equipment.kind).processingSeconds>0)
                        transitions+=2*(Math.Ceiling(delta/((double)model.Rules.deliveryIntervalSeconds+model.Rules.Equipment(equipment.kind).processingSeconds))+1);
                }
                else if(state.enabled && ValidInterval(equipment))transitions+=Math.Ceiling(delta/model.Rules.Equipment(equipment.kind).processingSeconds)+1;
                if(transitions>MaximumEventsPerTick)throw new ArgumentOutOfRangeException("delta","Industrial catch-up exceeds the per-call event budget. Split elapsed gameplay into smaller ticks; state is unchanged.");
            }
        }
        public bool Tick(float delta)
        {
            if(!CompactRules.Finite(delta) || delta<0)throw new ArgumentOutOfRangeException("delta","Elapsed gameplay time must be finite and nonnegative.");
            if(delta==0 || model.State.version!=4)return false;
            var activeMachines=Machines();CheckTickBudget(delta,activeMachines);double left=delta;bool changed=false;int events=0;
            while(true)
            {
                foreach(var equipment in activeMachines)
                {
                    var state=equipment.industry;if(!IsIndustrial(equipment.kind) || state==null || !Powered(equipment))continue;
                    if(equipment.kind==EquipmentKind.PrimaryScrapper)
                    {
                        if(state.primary!=null && Clock(state.primary,state.primary.remaining)==0 && CanComplete(equipment)){Complete(equipment);events++;changed=true;}
                        string reason;
                        if(state.primary==null && state.enabled && Clock(state,state.remaining)==0 && CanIntake(equipment,state.purchaseKind,true,out reason))
                        {Purchase(equipment);events++;changed=true;}
                    }
                    else if(state.enabled && Clock(state,state.remaining)==0 && DispatchQuote(equipment.id).allowed)
                    {DispatchNow(equipment.id);events++;changed=true;}
                }
                if(left<=0)break;
                double step=left;bool advancing=false;
                foreach(var equipment in activeMachines)
                {if(!IsIndustrial(equipment.kind))continue;object owner;double remaining;if(Advancing(equipment,out owner,out remaining)){advancing=true;step=Math.Min(step,remaining);}}
                if(!advancing)break;
                foreach(var equipment in activeMachines)
                {
                    if(!IsIndustrial(equipment.kind))continue;object owner;double remaining;if(!Advancing(equipment,out owner,out remaining))continue;
                    double next=Math.Max(0,remaining-step);if(owner is PrimaryScrapJob)SetClock((PrimaryScrapJob)owner,next);else SetClock((IndustrialMachineState)owner,next);
                }
                changed=true;left-=step;
                if(events>MaximumEventsPerTick)throw new InvalidOperationException("Industrial event estimate failed; report this simulation fault.");
            }
            if(clocks.Count>model.State.equipment.Count*2+16)
            {
                var live=new HashSet<object>();foreach(var equipment in model.State.equipment)if(equipment.industry!=null){live.Add(equipment.industry);if(equipment.industry.primary!=null)live.Add(equipment.industry.primary);}
                var stale=new List<object>();foreach(var key in clocks.Keys)if(!live.Contains(key))stale.Add(key);foreach(var key in stale)clocks.Remove(key);
            }
            return changed;
        }
        public string Status(int equipmentId)
        {
            var equipment=model.FindEquipment(equipmentId);if(!Supported(equipment))return "Industrial equipment unavailable.";
            var state=equipment.industry;
            if(equipment.kind==EquipmentKind.ExportStation)
            {
                var quote=DispatchQuote(equipmentId);if(!quote.allowed)return quote.reason;
                return state!=null && state.enabled?"Automatic dispatch in "+Math.Ceiling(state.remaining)+" seconds: "+quote.name+" ×"+quote.quantity+".":"Automatic dispatch disabled. A powered manual dispatch is available.";
            }
            if(state!=null && state.primary!=null)
            {
                if(!Powered(equipment))return "Paid object preserved; connect sufficient generator power.";
                if(!CanComplete(equipment))return "Primary blocked: drain output capacity or restore safe output identities. Paid object and progress are preserved.";
                return ScrapName(state.primary.kind)+": "+Math.Ceiling(state.primary.remaining)+" seconds remaining. Standing deliveries "+(state.enabled?"enabled.":"disabled.");
            }
            if(state==null || !state.enabled)return "Standing deliveries disabled. Feed an intact owned car/fridge or enable purchases explicitly.";
            if(!Powered(equipment))return "Standing delivery paused: insufficient or disconnected power.";
            string reason;if(!CanIntake(equipment,state.purchaseKind,true,out reason))return reason;
            return "Next "+model.Rules.LargeRecipe(state.purchaseKind).name+" purchase in "+Math.Ceiling(state.remaining)+" eligible idle seconds.";
        }
        internal static int ValidateEquipment(EquipmentState equipment,int schema,int nextId,HashSet<int> ids)
        {
            if(!IsIndustrial(equipment.kind))
            {if(equipment.industry!=null)throw new ArgumentException("Industry state belongs only to primary/export equipment.");return 0;}
            if(schema!=4)throw new ArgumentException("Industrial equipment requires schema 4.");
            if(equipment.job!=null)throw new ArgumentException("Industrial equipment cannot contain a portable component-processing job.");
            var state=equipment.industry;if(state==null)return 0;
            if(state.version!=1 || !Enum.IsDefined(typeof(ScrapObjectKind),state.purchaseKind) || !CompactRules.Finite(state.remaining) || state.remaining<0 || state.remaining>86400 ||
                state.objectsProcessed<0 || state.exportedUnits<0 || state.exportedRevenue<0 || state.exportedXp<0)
                throw new ArgumentException("Invalid industrial service timer, configuration or counters.");
            if(equipment.kind==EquipmentKind.ExportStation && (state.primary!=null || state.objectsProcessed!=0))throw new ArgumentException("An export station cannot hold whole scrap.");
            if(equipment.kind==EquipmentKind.PrimaryScrapper && (state.exportedUnits!=0 || state.exportedRevenue!=0 || state.exportedXp!=0))throw new ArgumentException("A primary cannot contain export sales counters.");
            var job=state.primary;if(job==null)return 0;
            if(job.id<1 || job.id>=nextId || !ids.Add(job.id) || !Enum.IsDefined(typeof(ScrapObjectKind),job.kind) ||
                !CompactRules.Finite(job.duration) || job.duration<=0 || job.duration>86400 || !CompactRules.Finite(job.remaining) || job.remaining<0 || job.remaining>job.duration)
                throw new ArgumentException("Invalid primary whole-object identity or processing snapshot.");
            CompactRules.ValidateYields(job.yields,false);int units=0;foreach(var yield in job.yields)units+=yield.quantity;
            if(units>4096)throw new ArgumentException("Primary outputs exceed the durable unit limit.");return job.yields.Length;
        }
    }
}
