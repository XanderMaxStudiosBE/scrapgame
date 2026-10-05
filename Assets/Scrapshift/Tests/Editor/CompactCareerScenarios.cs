using System;
using System.Text;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactCareerScenarios
    {
        public static readonly string[] Names={"CareerFreshHasNoHistoricalAwards","CareerSuccessfulTransactionsOnly","CareerPartialAndSurplusDelivery",
            "CareerImportedLineageAndConservation","CareerCashAndXPOverflowAtomic","CareerPartialResumeExactlyOnce","CareerDurableRequestTuning",
            "CareerFiniteRequestsAndFreePlay","CareerLockedRequestOrdinarySale","CareerReadyJobsDoNotRepeatMetrics","CareerLegacyEvidenceWithoutTotals",
            "CareerMissingAndCustomBalanceDefaults","CareerRejectsCorruptOptionalState","CareerPowerAndBeltsStayComplete","CareerCompleteFromZero",
            "CareerDeterministicSaleLedger"};
        static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
        static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new Exception("Invalid career data was accepted.");}
        static int Bench(ScrappingModel model){return model.State.equipment[0].id;}
        static void Finish(ScrappingModel model,int id){while(!model.FindEquipment(id).job.ready)Check(model.Work(id),model.LastNotice);}
        public static CompactYardState Copy(CompactYardState source)
        {
            var copy=CompactScrappingScenarios.Copy(source);var state=source.career;if(state==null)return copy;
            copy.career=new CompactCareerState{version=state.version,completedGoals=state.completedGoals,inspectedObjects=state.inspectedObjects,
                dismantledObjects=state.dismantledObjects,manualBatches=state.manualBatches,poweredBatches=state.poweredBatches,
                materialUnitsSold=state.materialUnitsSold,saleTransactions=state.saleTransactions,salesRevenue=state.salesRevenue,
                contractsCompleted=state.contractsCompleted,contractBonuses=state.contractBonuses,completionAcknowledged=state.completionAcknowledged};
            if(state.contract!=null){var r=state.contract;copy.career.contract=new CompactContractProgress{id=r.id,index=r.index,name=r.name,customer=r.customer,
                kind=r.kind,required=r.required,delivered=r.delivered,bonus=r.bonus,minimumLevel=r.minimumLevel};}
            return copy;
        }
        static string Fingerprint(ScrappingModel model)
        {
            var s=model.State;var c=model.Career.Stats;var b=new StringBuilder();b.Append(s.money).Append(',').Append(s.experience).Append(',').Append(s.nextId).Append(',').Append(s.carriedId);
            foreach(var item in s.items)b.Append('/').Append(item.id).Append(':').Append(item.kind).Append(':').Append(item.quantity).Append(':').Append(item.xpEligible);
            b.Append('/').Append(c.completedGoals).Append(':').Append(c.inspectedObjects).Append(':').Append(c.dismantledObjects).Append(':').Append(c.manualBatches)
                .Append(':').Append(c.poweredBatches).Append(':').Append(c.materialUnitsSold).Append(':').Append(c.saleTransactions).Append(':').Append(c.salesRevenue)
                .Append(':').Append(c.contractsCompleted).Append(':').Append(c.contractBonuses).Append(':').Append(c.completionAcknowledged);
            var r=c.contract;if(r!=null)b.Append('/').Append(r.id).Append(':').Append(r.required).Append(':').Append(r.delivered).Append(':').Append(r.bonus).Append(':').Append(r.minimumLevel);
            return b.ToString();
        }
        static CompactStack Hold(ScrappingModel model,PartKind kind,int quantity,bool eligible=true)
        {
            Check(model.Carried==null,"test hands must be empty");var item=new CompactStack{id=model.State.nextId++,kind=kind,quantity=quantity,xpEligible=eligible};
            model.State.items.Add(item);model.State.carriedId=item.id;return item;
        }
        static bool Goal(ScrappingModel model,string key)
        {foreach(var goal in model.Career.Goals)if(goal.key==key)return goal.complete;throw new Exception("Unknown goal "+key);}
        static void SellOrDeliver(ScrappingModel model)
        {
            while(model.Carried!=null)
                if(model.Career.ContractQuote().allowed)Check(model.Career.DeliverContract(),model.LastNotice);else Check(model.Sell(),model.LastNotice);
        }
        static void WireCycle(ScrappingModel model)
        {
            int bench=Bench(model);Check(model.AcquireWire(),model.LastNotice);Check(model.BeginProcessing(bench),model.LastNotice);Finish(model,bench);
            Check(model.CollectOutput(bench,0),model.LastNotice);SellOrDeliver(model);Check(model.CollectOutput(bench,1),model.LastNotice);SellOrDeliver(model);
        }
        static void ScrapCycle(ScrappingModel model,ScrapObjectKind kind)
        {
            LargeScrapJob scrap=null;foreach(var candidate in model.State.scrap)if(candidate.kind==kind){scrap=candidate;break;}
            if(scrap==null){Check(model.BuyScrap(kind),model.LastNotice);foreach(var candidate in model.State.scrap)if(candidate.kind==kind)scrap=candidate;}
            Check(model.InspectScrap(scrap.id),model.LastNotice);while(scrap.strokes<scrap.requiredStrokes)Check(model.WorkScrap(scrap.id),model.LastNotice);
            for(int i=0;i<scrap.remaining.Length;i++)
            {
                Check(model.CollectScrap(scrap.id,i),model.LastNotice);
                if(model.Rules.Part(model.Carried.kind).isMaterial){SellOrDeliver(model);continue;}
                int bench=Bench(model);Check(model.BeginProcessing(bench),model.LastNotice);Finish(model,bench);var job=model.FindEquipment(bench).job;
                for(int output=0;output<job.yields.Length;output++){Check(model.CollectOutput(bench,output),model.LastNotice);SellOrDeliver(model);}
            }
        }
        public static void Run(string name)
        {
            var rules=new CompactRules();var model=new ScrappingModel(rules);int bench=Bench(model);
            switch(name)
            {
                case "CareerFreshHasNoHistoricalAwards":
                    Check(model.State.version==4 && model.State.money==8 && model.State.experience==0,"fresh balances unchanged");
                    Check(model.Career.CurrentGoal.key=="inspect" && model.Career.CurrentContract.id==1,"clear first objective and request");
                    Check(model.Career.Stats.saleTransactions==0 && model.Career.Stats.contractsCompleted==0 && !model.Career.Completed,"no historical awards");break;
                case "CareerSuccessfulTransactionsOnly":
                    int car=model.State.scrap[0].id;Check(!model.WorkScrap(car) && model.Career.Stats.dismantledObjects==0,"rejected work uncounted");
                    Check(model.InspectScrap(car) && !model.InspectScrap(car) && model.Career.Stats.inspectedObjects==1,"one inspection");
                    while(model.FindScrap(car).strokes<model.FindScrap(car).requiredStrokes)Check(model.WorkScrap(car),"dismantle");
                    Check(!model.WorkScrap(car) && model.Career.Stats.dismantledObjects==1,"one finished object");
                    Check(model.AcquireWire() && model.BeginProcessing(bench),"manual input");Finish(model,bench);
                    Check(model.Career.Stats.manualBatches==1 && !model.Work(bench),"completed manual job counted once");
                    Check(model.CollectOutput(bench,0) && model.Sell() && !model.Sell(),"one sale");
                    Check(model.Career.Stats.materialUnitsSold==3 && model.Career.Stats.salesRevenue==9 && model.Career.Stats.saleTransactions==1,"actual sale ledger");break;
                case "CareerPartialAndSurplusDelivery":
                    Hold(model,PartKind.Copper,2);string before=Fingerprint(model);var partial=model.Career.ContractQuote();
                    Check(partial.allowed && !partial.completes && partial.quantity==2 && partial.remainingAfter==4 && partial.total==6 && partial.experience==4,"partial quote");
                    Check(before==Fingerprint(model),"quote/cancel does not mutate");Check(model.Career.DeliverContract() && model.Carried==null,"partial bundle consumed");
                    int original=Hold(model,PartKind.Copper,5).id;var final=model.Career.ContractQuote();
                    Check(final.quantity==4 && final.completionBonus==6 && final.saleTotal==12 && final.total==18 && final.experience==8,"only needed units are quoted");
                    Check(model.Career.DeliverContract() && model.Carried.id==original && model.Carried.quantity==1 && model.Carried.xpEligible,"surplus identity and lineage retained");
                    Check(model.State.money==32 && model.State.experience==12 && model.Career.CurrentContract.id==2,"normal sale plus one modest bonus");
                    before=Fingerprint(model);Check(!model.Career.DeliverContract() && before==Fingerprint(model),"previous request cannot replay");break;
                case "CareerImportedLineageAndConservation":
                    int importedId=Hold(model,PartKind.Copper,10,false).id;Check(model.Career.ContractQuote().experience==0,"imported quantity has no XP");
                    Check(model.Career.DeliverContract() && model.Carried.id==importedId && model.Carried.quantity==4 && !model.Carried.xpEligible,"partial lineage conserved");
                    Check(model.State.experience==0 && !Goal(model,"sell") && model.Career.Stats.materialUnitsSold==6,"no invented recovery");
                    Check(model.Sell() && model.State.money==44 && model.State.experience==0 && model.Career.Stats.materialUnitsSold==10,"remaining cash sale conserves all ten units");break;
                case "CareerCashAndXPOverflowAtomic":
                    Hold(model,PartKind.Copper,6);model.State.money=int.MaxValue-18;string rich=Fingerprint(model);
                    Check(!model.Career.ContractQuote().allowed && !model.Career.DeliverContract() && rich==Fingerprint(model),"bonus overflow preserves every field");
                    model.State.money=0;model.State.experience=int.MaxValue;rich=Fingerprint(model);
                    Check(!model.Career.DeliverContract() && rich==Fingerprint(model),"XP overflow preserves bundle/request");
                    model.State.experience=0;model.Career.Stats.salesRevenue=int.MaxValue-1;model.Career.Stats.materialUnitsSold=int.MaxValue;
                    Check(model.Career.DeliverContract() && model.Career.Stats.salesRevenue==int.MaxValue && model.Career.Stats.materialUnitsSold==int.MaxValue,"display counters saturate without affecting actual balances");break;
                case "CareerPartialResumeExactlyOnce":
                    Hold(model,PartKind.Copper,2);Check(model.Career.DeliverContract(),"first partial");
                    model=new ScrappingModel(rules,Copy(model.State));Check(model.Career.CurrentContract.delivered==2 && model.Career.Stats.salesRevenue==6,"partial and ledger retained");
                    Hold(model,PartKind.Copper,4);Check(model.Career.DeliverContract(),"finish resumed request");
                    model=new ScrappingModel(rules,Copy(model.State));Check(model.Career.Stats.contractsCompleted==1 && model.Career.Stats.contractBonuses==6 && model.Career.CurrentContract.id==2,"completion retained once");
                    Check(!model.Career.DeliverContract() && model.State.money==32 && model.State.experience==12,"resumed completion cannot replay");break;
                case "CareerDurableRequestTuning":
                    Hold(model,PartKind.Copper,2);model.Career.DeliverContract();var changed=new CompactRules();changed.contracts[0].quantity=40;changed.contracts[0].bonus=90;changed.contracts[0].minimumLevel=12;
                    changed.contracts[1].quantity=7;changed.contracts[1].bonus=13;model=new ScrappingModel(changed,Copy(model.State));
                    Check(model.Career.CurrentContract.required==6 && model.Career.CurrentContract.bonus==6 && model.Career.CurrentContract.minimumLevel==1,"active request preserved");
                    Hold(model,PartKind.Copper,4);Check(model.Career.DeliverContract() && model.Career.CurrentContract.required==7 && model.Career.CurrentContract.bonus==13,"only next request reads edited balance");break;
                case "CareerFiniteRequestsAndFreePlay":
                    model.State.experience=rules.levelThresholds[9];int allBonuses=0;
                    while(model.Career.CurrentContract!=null){var request=model.Career.CurrentContract;allBonuses+=request.bonus;Hold(model,request.kind,request.Remaining,false);Check(model.Career.DeliverContract(),model.LastNotice);}
                    int bookCash=model.State.money;Check(model.Career.Stats.contractsCompleted==6 && model.Career.Stats.contractBonuses==allBonuses && Goal(model,"contracts"),"finite book complete");
                    Hold(model,PartKind.Copper,3,false);Check(!model.Career.DeliverContract() && model.State.money==bookCash && model.Carried.quantity==3,"finite customer rewards cannot replay");
                    Check(model.Sell() && model.State.money>bookCash,"ordinary free-play sales continue");break;
                case "CareerLockedRequestOrdinarySale":
                    var lockedRules=new CompactRules{contracts=new[]{new CompactContractDefinition(90,"Later request","Customer",PartKind.Copper,3,5,3)}};
                    model=new ScrappingModel(lockedRules);Hold(model,PartKind.Copper,3);string locked=Fingerprint(model);
                    Check(!model.Career.ContractQuote().allowed && !model.Career.DeliverContract() && locked==Fingerprint(model),"locked request stays untouched");
                    Check(model.Sell() && model.State.experience==6 && model.Career.CurrentContract.delivered==0,"normal sales remain available");break;
                case "CareerReadyJobsDoNotRepeatMetrics":
                    model.AcquireWire();model.BeginProcessing(bench);model.Work(bench);model.Work(bench);model=new ScrappingModel(rules,Copy(model.State));Finish(model,bench);
                    model=new ScrappingModel(rules,Copy(model.State));Check(model.Career.Stats.manualBatches==1 && !model.Work(bench),"manual resume does not repeat completion");
                    model.CollectOutput(bench,0);model.Sell();model.CollectOutput(bench,1);model.Sell();var machine=new EquipmentState{id=model.State.nextId++,kind=EquipmentKind.Tier1Scrapper,x=0,z=5,paidPrice=60};model.State.equipment.Add(machine);
                    model.HasPower=id=>true;model.AcquireWire();model.BeginProcessing(machine.id);model.Tick(1);
                    model=new ScrappingModel(rules,Copy(model.State));model.HasPower=id=>true;Check(model.Tick(99) && model.Career.Stats.poweredBatches==1,"one powered completion");
                    model=new ScrappingModel(rules,Copy(model.State));model.HasPower=id=>true;Check(!model.Tick(99) && model.Career.Stats.poweredBatches==1,"ready powered job never repeats");break;
                case "CareerLegacyEvidenceWithoutTotals":
                    model.State.career=null;model.State.experience=rules.levelThresholds[9];int originalLegacyXp=model.State.experience;var legacy=Copy(model.State);
                    legacy.scrap[0].inspected=true;legacy.scrap[0].requiredStrokes=6;legacy.scrap[0].strokes=6;
                    legacy.scrap[0].remaining=new[]{new PartAmount(PartKind.Motor,1)};
                    model=new ScrappingModel(rules,legacy);
                    Check(Goal(model,"inspect") && Goal(model,"dismantle") && Goal(model,"sell") && Goal(model,"level10"),"visible existing evidence inferred");
                    Check(!Goal(model,"process") && !Goal(model,"powered") && model.Career.Stats.salesRevenue==0 && model.Career.Stats.materialUnitsSold==0 && model.Career.Stats.inspectedObjects==0 && model.Career.Stats.contractsCompleted==0,"historical totals and unseen work never invented");
                    Check(model.State.money==8 && model.State.experience==originalLegacyXp,"no migration awards");break;
                case "CareerMissingAndCustomBalanceDefaults":
                    var missing=new CompactRules{contracts=null};missing.Validate();Check(missing.contracts.Length==6,"missing older field safely initialized");
                    var olderEmpty=new CompactRules{careerRulesVersion=0,contracts=new CompactContractDefinition[0]};olderEmpty.Validate();
                    Check(olderEmpty.careerRulesVersion==1 && olderEmpty.contracts.Length==6,"Unity older empty-array defaults safely initialized");
                    var custom=new CompactRules{contracts=new[]{new CompactContractDefinition(75,"Custom name","Custom buyer",PartKind.Copper,17,29,4)}};
                    Check(!custom.FillMissingCareerDefaults(),"positive custom values retained");custom.Validate();Check(custom.contracts[0].quantity==17 && custom.contracts[0].bonus==29,"no custom overwrite");
                    custom.careerRulesVersion=0;custom.Validate();Check(custom.contracts[0].quantity==17 && custom.contracts[0].bonus==29,"older version preserves custom positive definitions");
                    var disabled=new CompactRules{contracts=new CompactContractDefinition[0]};model=new ScrappingModel(disabled);Check(model.Career.CurrentContract==null && Goal(model,"contracts"),"explicit empty book respected");
                    custom.contracts[0].bonus=-1;Reject(()=>custom.Validate());break;
                case "CareerRejectsCorruptOptionalState":
                    var invalid=Copy(model.State);invalid.career.version=2;Reject(()=>ScrappingModel.Validate(invalid,rules));
                    invalid=Copy(model.State);invalid.career.materialUnitsSold=-1;Reject(()=>ScrappingModel.Validate(invalid,rules));
                    invalid=Copy(model.State);invalid.career.completedGoals=512;Reject(()=>ScrappingModel.Validate(invalid,rules));
                    invalid=Copy(model.State);invalid.career.completionAcknowledged=true;Reject(()=>ScrappingModel.Validate(invalid,rules));
                    invalid=Copy(model.State);invalid.career.contract.delivered=invalid.career.contract.required;Reject(()=>ScrappingModel.Validate(invalid,rules));
                    invalid=Copy(model.State);invalid.career.contract.kind=PartKind.Wire;Reject(()=>ScrappingModel.Validate(invalid,rules));
                    invalid=Copy(model.State);invalid.career.contract.index=1;Reject(()=>ScrappingModel.Validate(invalid,rules));
                    invalid=Copy(model.State);invalid.career=null;ScrappingModel.Validate(invalid,rules);break;
                case "CareerPowerAndBeltsStayComplete":
                    model.State.money=1000;model.State.experience=rules.levelThresholds[9];var construction=new ConstructionModel(model.State,rules);model.HasPower=id=>construction.PowerFor(id).powered;
                    Check(construction.Place(EquipmentKind.Generator,5,5,0),construction.LastMessage);int generator=construction.LastPlacedId;
                    Check(construction.Place(EquipmentKind.Tier1Scrapper,0,5,0),construction.LastMessage);int powered=construction.LastPlacedId;
                    Check(construction.Connect(generator,powered) && model.Career.RefreshProgress() && Goal(model,"power"),"power evidence recorded");
                    Check(construction.Disconnect(generator,powered) && construction.Remove(generator) && !model.Career.RefreshProgress() && Goal(model,"power"),"removal does not regress goal");
                    Check(construction.Place(EquipmentKind.Storage,-8,0,0),construction.LastMessage);int first=construction.LastPlacedId;
                    Check(construction.Place(EquipmentKind.Storage,-8,6,0),construction.LastMessage);var automation=new AutomationModel(model,construction);
                    Check(automation.Connect(first,0,construction.LastPlacedId,0,true) && model.Career.RefreshProgress() && Goal(model,"belts"),"connected route recorded");
                    Check(automation.Remove(automation.LastConnectedId) && !model.Career.RefreshProgress() && Goal(model,"belts"),"removed belt does not regress journal");break;
                case "CareerCompleteFromZero":
                    model.State.money=0;ScrapCycle(model,ScrapObjectKind.Car);ScrapCycle(model,ScrapObjectKind.Refrigerator);
                    int cycles=0;
                    while(model.Level<10 || model.Career.CurrentContract!=null)
                    {
                        Check(cycles++<1000,"finite career remains reachable");var request=model.Career.CurrentContract;
                        if(request!=null && request.minimumLevel<=model.Level && request.kind==PartKind.Plastic)ScrapCycle(model,ScrapObjectKind.Refrigerator);
                        else if(request!=null && request.minimumLevel<=model.Level && request.kind==PartKind.Steel)ScrapCycle(model,ScrapObjectKind.Car);
                        else WireCycle(model);
                    }
                    var c=new ConstructionModel(model.State,rules);model.HasPower=id=>c.PowerFor(id).powered;
                    Check(c.Place(EquipmentKind.Generator,5,5,0),c.LastMessage);int generatorId=c.LastPlacedId;
                    Check(c.Place(EquipmentKind.Tier1Scrapper,0,5,0),c.LastMessage);int tierOne=c.LastPlacedId;Check(c.Connect(generatorId,tierOne),c.LastMessage);model.Career.RefreshProgress();
                    Check(model.AcquireWire() && model.BeginProcessing(tierOne) && model.Tick(10),"real powered component completion");
                    model.CollectOutput(tierOne,0);model.Sell();model.CollectOutput(tierOne,1);model.Sell();
                    Check(c.Place(EquipmentKind.Storage,-8,0,0),c.LastMessage);int from=c.LastPlacedId;Check(c.Place(EquipmentKind.Storage,-8,6,0),c.LastMessage);
                    var a=new AutomationModel(model,c);Check(a.Connect(from,0,c.LastPlacedId,0,true),a.LastMessage);model.Career.RefreshProgress();
                    Check(model.Career.Completed && model.Career.CurrentGoal==null && model.Career.CurrentContract==null,"all career goals attained from actual work");
                    int finalCash=model.State.money,finalXP=model.State.experience;
                    Check(model.Career.AcknowledgeCompletion() && !model.Career.AcknowledgeCompletion() && model.State.money==finalCash && model.State.experience==finalXP,"completion acknowledgment is persistent and has no duplicate reward");
                    model=new ScrappingModel(rules,Copy(model.State));Check(model.Career.Completed && model.Career.Stats.completionAcknowledged,"finished career resumes in free play");
                    ConstructionModel.Validate(model.State,rules);AutomationModel.Validate(model.State,rules);WireCycle(model);Check(model.State.money>finalCash,"free play stays productive");break;
                case "CareerDeterministicSaleLedger":
                    var random=new Random(4721);int acquired=0,sold=0,xp=0,cash=0,bonuses=0;
                    for(int i=0;i<700;i++)
                    {
                        int amount=random.Next(1,8);bool eligible=random.Next(3)!=0;Hold(model,PartKind.Copper,amount,eligible);acquired+=amount;
                        while(model.Carried!=null)
                        {
                            var contract=model.Career.ContractQuote();var sale=model.SaleQuote();
                            if(contract.allowed){Check(model.Career.DeliverContract(),model.LastNotice);sold+=contract.quantity;xp+=contract.experience;cash+=contract.saleTotal;bonuses+=contract.completionBonus;}
                            else {Check(model.Sell(),model.LastNotice);sold+=sale.quantity;xp+=sale.experience;cash+=sale.total;}
                        }
                        if(i%17==0)model=new ScrappingModel(rules,Copy(model.State));
                        Check(acquired==sold && model.State.experience==xp && model.State.money==8+cash+bonuses,"money, XP and material ledger conserved across deliveries/resumes");
                        Check(model.Career.Stats.salesRevenue==cash && model.Career.Stats.contractBonuses==bonuses && model.Career.Stats.materialUnitsSold==sold,"persistent career ledger equals actual sales");
                        ScrappingModel.Validate(model.State,rules);
                    }
                    Check(model.Career.Stats.contractsCompleted==1 && model.Career.CurrentContract.kind==PartKind.Steel,"mismatched sales never satisfy later request");break;
                default:throw new ArgumentException(name);
            }
            ScrappingModel.Validate(model.State,model.Rules);
        }
    }
}
