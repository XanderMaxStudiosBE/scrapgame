using System;

namespace Scrapshift.Compact
{
    [Serializable] public sealed class CompactContractDefinition
    {
        public int id;
        public string name,customer;
        public PartKind kind;
        public int quantity,bonus,minimumLevel=1;
        public CompactContractDefinition() { }
        public CompactContractDefinition(int id,string name,string customer,PartKind kind,int quantity,int bonus,int minimumLevel)
        {this.id=id;this.name=name;this.customer=customer;this.kind=kind;this.quantity=quantity;this.bonus=bonus;this.minimumLevel=minimumLevel;}
    }
    [Serializable] public sealed class CompactContractProgress
    {
        // A durable request snapshot: later balance edits never rewrite an active delivery.
        public int id,index;
        public string name,customer;
        public PartKind kind;
        public int required,delivered,bonus,minimumLevel;
        public int Remaining {get{return required-delivered;}}
    }
    [Serializable] public sealed class CompactCareerState
    {
        public int version=1,completedGoals;
        public int inspectedObjects,dismantledObjects,manualBatches,poweredBatches;
        public int materialUnitsSold,saleTransactions,salesRevenue,contractsCompleted,contractBonuses;
        public bool completionAcknowledged;
        public CompactContractProgress contract;
    }
    public sealed class CompactCareerGoal
    {public string key,title,detail;public bool complete;}
    public sealed class CompactContractQuote
    {
        public bool allowed,completes;
        public string reason,name,customer;
        public PartKind kind;
        public int contractId,quantity,remainingAfter,saleTotal,completionBonus,total,experience;
    }

    /// <summary>Relaxed, finite customer requests and a monotonic career journal. No clock or automatic awards.</summary>
    public sealed class CompactCareerModel
    {
        const int Inspect=1,Dismantle=2,Process=4,Sell=8,Power=16,Powered=32,LevelTen=64,Belts=128,Contracts=256;
        const int AllGoals=511;
        readonly ScrappingModel model;
        public CompactCareerState Stats {get{return model.State.career;}}
        public CompactContractProgress CurrentContract {get{return Stats.contract;}}
        public bool Completed {get{return Stats.completedGoals==AllGoals;}}
        internal CompactCareerModel(ScrappingModel model)
        {
            this.model=model;
            if(model.State.career==null)model.State.career=new CompactCareerState();
            // Only visible evidence is inferred. Historical quantities, revenue and customer rewards remain zero.
            foreach(var scrap in model.State.scrap)
                if(scrap.inspected){Mark(Inspect);if(scrap.strokes==scrap.requiredStrokes)Mark(Dismantle);}
            foreach(var equipment in model.State.equipment)
                if(equipment.job!=null && equipment.job.ready)
                {if(equipment.kind==EquipmentKind.Workbench)Mark(Process);else if(equipment.kind==EquipmentKind.Tier1Scrapper)Mark(Powered);}
            if(model.State.experience>0)Mark(Sell);
            EnsureRequest();RefreshProgress();
        }
        void Mark(int bit){Stats.completedGoals|=bit;}
        internal void RecordInspect(){Stats.inspectedObjects=Add(Stats.inspectedObjects,1);Mark(Inspect);}
        internal void RecordDismantle(){Stats.dismantledObjects=Add(Stats.dismantledObjects,1);Mark(Dismantle);}
        internal void RecordProcessing(EquipmentKind equipment)
        {
            if(equipment==EquipmentKind.Workbench){Stats.manualBatches=Add(Stats.manualBatches,1);Mark(Process);}
            else {Stats.poweredBatches=Add(Stats.poweredBatches,1);if(equipment==EquipmentKind.Tier1Scrapper)Mark(Powered);}
        }
        internal void RecordSale(PartKind kind,int quantity,int cash,bool eligible)
        {
            Stats.saleTransactions=Add(Stats.saleTransactions,1);Stats.salesRevenue=Add(Stats.salesRevenue,cash);
            if(model.Rules.Part(kind).isMaterial)
            {Stats.materialUnitsSold=Add(Stats.materialUnitsSold,quantity);if(eligible)Mark(Sell);}
            RefreshProgress();
        }
        static int Add(int value,int amount){return (int)Math.Min(int.MaxValue,(long)value+amount);}
        void EnsureRequest()
        {
            if(Stats.contract!=null || Stats.contractsCompleted>=model.Rules.contracts.Length)return;
            var definition=model.Rules.contracts[Stats.contractsCompleted];
            Stats.contract=new CompactContractProgress{id=definition.id,index=Stats.contractsCompleted,name=definition.name,customer=definition.customer,
                kind=definition.kind,required=definition.quantity,bonus=definition.bonus,minimumLevel=definition.minimumLevel};
        }
        public bool RefreshProgress()
        {
            int before=Stats.completedGoals;
            if(model.Level>=10)Mark(LevelTen);
            if(model.State.belts!=null && model.State.belts.Count>0)Mark(Belts);
            foreach(var equipment in model.State.equipment)
                if(equipment.kind==EquipmentKind.Tier1Scrapper && model.HasPower!=null && model.HasPower(equipment.id))
                {Mark(Power);break;}
            if(CurrentContract==null && Stats.contractsCompleted>=model.Rules.contracts.Length)Mark(Contracts);
            return before!=Stats.completedGoals;
        }
        public CompactCareerGoal[] Goals
        {
            get{return new[]{
                Goal("inspect","Know your scrap","Inspect a delivered car or refrigerator before taking it apart.",Inspect),
                Goal("dismantle","Open it up","Finish all dismantling stages on one delivered object.",Dismantle),
                Goal("process","Recover useful materials","Finish one component batch at your manual workbench.",Process),
                Goal("sell","Make your first recovery sale","Sell recovered materials at the office sales counter.",Sell),
                Goal("power","Put the generator to work","Place a generator and connect sufficient power to a Tier 1 scrapper.",Power),
                Goal("powered","Let the machine help","Finish one powered Tier 1 component batch.",Powered),
                Goal("level10","Build your reputation","Reach level 10 through valid recovered-material sales.",LevelTen),
                Goal("belts","Lay out your first route","Connect compatible equipment ports with a conveyor.",Belts),
                Goal("contracts","Supply the neighbourhood","Finish the customer request book. No deadlines or fees.",Contracts)
            };}
        }
        CompactCareerGoal Goal(string key,string title,string detail,int bit)
        {return new CompactCareerGoal{key=key,title=title,detail=detail,complete=(Stats.completedGoals&bit)!=0};}
        public CompactCareerGoal CurrentGoal
        {get{foreach(var goal in Goals)if(!goal.complete)return goal;return null;}}
        public bool AcknowledgeCompletion()
        {
            if(!Completed || Stats.completionAcknowledged)return false;
            Stats.completionAcknowledged=true;return true;
        }
        public CompactContractQuote ContractQuote()
        {
            var quote=new CompactContractQuote{reason="The request book is complete. Continue growing your yard in free play."};
            var request=CurrentContract;if(request==null)return quote;
            quote.contractId=request.id;quote.name=request.name;quote.customer=request.customer;quote.kind=request.kind;quote.remainingAfter=request.Remaining;
            if(model.Level<request.minimumLevel){quote.reason="This customer opens at level "+request.minimumLevel+". Ordinary material sales remain available.";return quote;}
            var held=model.Carried;
            if(held==null || held.kind!=request.kind)
            {quote.reason="Carry "+model.Rules.Part(request.kind).name+" to this request. "+request.Remaining+" units still needed.";return quote;}
            int quantity=Math.Min(held.quantity,request.Remaining);
            var sale=model.QuoteSale(quantity);
            if(!sale.allowed){quote.reason=sale.reason;return quote;}
            quote.quantity=quantity;quote.remainingAfter=request.Remaining-quantity;quote.completes=quote.remainingAfter==0;
            quote.saleTotal=sale.total;quote.completionBonus=quote.completes?request.bonus:0;quote.experience=sale.experience;
            long total=(long)quote.saleTotal+quote.completionBonus;
            if(total>int.MaxValue-(long)model.State.money){quote.reason="Delivery exceeds the cash limit; your bundle and request are unchanged.";return quote;}
            quote.total=(int)total;quote.allowed=true;
            quote.reason=quote.completes?"Completes this request; the customer bonus is paid once.":"Partial delivery; request progress is saved.";
            return quote;
        }
        public bool DeliverContract()
        {
            var quote=ContractQuote();if(!quote.allowed){model.CareerNotice(quote.reason);return false;}
            var request=CurrentContract;var sale=model.QuoteSale(quote.quantity);
            // The quote is recomputed for every action. No quoted state, reservations or rewards are written until confirmation.
            model.CommitSale(sale,quote.completionBonus);
            request.delivered+=quote.quantity;
            string notice="Delivered "+model.Rules.Part(quote.kind).name+" ×"+quote.quantity+" to "+quote.customer+" for €"+quote.saleTotal+" (+"+quote.experience+" XP).";
            if(quote.completes)
            {
                Stats.contractsCompleted++;Stats.contractBonuses=Add(Stats.contractBonuses,quote.completionBonus);Stats.contract=null;
                notice+=" Request complete: €"+quote.completionBonus+" customer bonus.";EnsureRequest();
                if(CurrentContract==null)notice+=" All customer requests complete. Your yard remains open for free play.";
            }
            else notice+=" "+request.Remaining+" units still needed.";
            RefreshProgress();model.CareerNotice(notice);return true;
        }
        public static void Validate(CompactCareerState state,CompactRules rules)
        {
            if(state==null)return;
            if(state.version!=1 || state.completedGoals<0 || state.completedGoals>AllGoals || state.inspectedObjects<0 || state.dismantledObjects<0 ||
                state.manualBatches<0 || state.poweredBatches<0 || state.materialUnitsSold<0 || state.saleTransactions<0 || state.salesRevenue<0 ||
                state.contractsCompleted<0 || state.contractsCompleted>64 || state.contractBonuses<0 || (state.completionAcknowledged && state.completedGoals!=AllGoals))
                throw new ArgumentException("Invalid career counters or journal state.");
            var request=state.contract;if(request==null)return;
            var part=rules.Part(request.kind);
            if(request.id<1 || request.index!=state.contractsCompleted || request.index>=64 || string.IsNullOrEmpty(request.name) || string.IsNullOrEmpty(request.customer) ||
                request.name.Length>120 || request.customer.Length>120 || part==null || !part.isMaterial || request.required<1 || request.required>4096 ||
                request.delivered<0 || request.delivered>=request.required || request.bonus<0 || request.bonus>100000 || request.minimumLevel<1 || request.minimumLevel>100)
                throw new ArgumentException("Invalid durable customer request snapshot.");
        }
    }
}
