using System;
using System.Collections.Generic;
using System.Text;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactScrappingScenarios
    {
        public static readonly string[] Names={"CompactFreshSetup","CompactInspectBeforeWork","CompactCarChainConservation","CompactFridgeChain",
            "CompactPartialSnapshotResume","CompactManualDurableRecipeTuning","CompactSalesXPAndNoCollectionXP","CompactLevelTwoSaleImprovement","CompactLevelPriceBonusUnlock",
            "CompactRejectBuyResell","CompactRenewableZeroMoney","CompactOutputCapacityGuards","CompactReservedOutputSurvivesCapacityReduction",
            "CompactPowerPauseResumeAndBackpressure","CompactTimedSnapshotsResume","CompactFailedTransactionsNonmutating","CompactOverflowAndIds",
            "CompactInvalidData","CompactInvalidBalance","CompactLegacyPortableSalesSalvage","CompactFullStartingProgression",
            "CompactDeterministicMaterialLedger","CompactAllocationLimitOutputsRetained","CompactPurchasedScrapStages","CompactPartialLargeRecipeTuning"};
        static void Check(bool v,string reason){if(!v)throw new Exception(reason);}
        static void Invalid(Action action,string reason){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,reason);}
        static ScrappingModel Fresh(){return new ScrappingModel(new CompactRules());}
        static int Bench(ScrappingModel m){foreach(var e in m.State.equipment)if(e.kind==EquipmentKind.Workbench)return e.id;throw new Exception("No bench");}
        static LargeScrapJob Large(ScrappingModel m,ScrapObjectKind kind){foreach(var s in m.State.scrap)if(s.kind==kind)return s;throw new Exception("No large scrap");}
        static void FinishManual(ScrappingModel m,int equipmentId)
        {var e=m.FindEquipment(equipmentId);while(!e.job.ready)Check(m.Work(equipmentId),"manual work failed");}
        static void FinishLarge(ScrappingModel m,int id)
        {Check(m.InspectScrap(id),"inspect");var s=m.FindScrap(id);while(s.strokes<s.requiredStrokes)Check(m.WorkScrap(id),"large work");}
        static int[] DrainProcessed(ScrappingModel m,int bench)
        {
            var result=new int[12];var job=m.FindEquipment(bench).job;int count=job.yields.Length;
            for(int i=0;i<count;i++)
            {
                if(job.yields[i].quantity==0)continue;
                Check(m.CollectOutput(bench,i),"collect output");var item=m.Carried;result[(int)item.kind]+=item.quantity;
                Check(m.Sell(),"sell output");Check(!m.CollectOutput(bench,i),"duplicate collection");
            }
            return result;
        }
        static int[] ProcessLarge(ScrappingModel m,ScrapObjectKind kind)
        {
            var large=Large(m,kind);FinishLarge(m,large.id);int count=large.remaining.Length;var total=new int[12];
            for(int i=0;i<count;i++)
            {
                Check(m.CollectScrap(large.id,i),"remove component");
                if(m.Rules.Part(m.Carried.kind).isMaterial){total[(int)m.Carried.kind]+=m.Carried.quantity;Check(m.Sell(),"sell direct material");continue;}
                int bench=Bench(m);Check(m.BeginProcessing(bench),"load component");FinishManual(m,bench);
                var recovered=DrainProcessed(m,bench);for(int j=0;j<total.Length;j++)total[j]+=recovered[j];
            }
            Check(m.FindScrap(large.id)==null,"exhausted object must be cleared");return total;
        }
        static EquipmentState AddMachine(ScrappingModel m)
        {var e=new EquipmentState{id=m.State.nextId++,kind=EquipmentKind.Tier1Scrapper,x=5,z=5,paidPrice=60};m.State.equipment.Add(e);return e;}
        static PartAmount[] CloneAmounts(PartAmount[] a){if(a==null)return null;var c=new PartAmount[a.Length];for(int i=0;i<a.Length;i++)c[i]=new PartAmount(a[i].kind,a[i].quantity);return c;}
        static CompactStack CloneStack(CompactStack s){return new CompactStack{id=s.id,kind=s.kind,quantity=s.quantity,x=s.x,y=s.y,z=s.z,xpEligible=s.xpEligible};}
        public static CompactYardState Copy(CompactYardState s)
        {
            var c=new CompactYardState{version=s.version,money=s.money,experience=s.experience,nextId=s.nextId,carriedId=s.carriedId,
                playerX=s.playerX,playerY=s.playerY,playerZ=s.playerZ,yaw=s.yaw,pitch=s.pitch,welcomeSeen=s.welcomeSeen,
                importedLegacy=s.importedLegacy,legacySnapshot=s.legacySnapshot,legacyNotice=s.legacyNotice};
            foreach(var item in s.items)c.items.Add(CloneStack(item));
            foreach(var large in s.scrap)c.scrap.Add(new LargeScrapJob{id=large.id,kind=large.kind,x=large.x,z=large.z,inspected=large.inspected,
                strokes=large.strokes,requiredStrokes=large.requiredStrokes,remaining=CloneAmounts(large.remaining)});
            foreach(var e in s.equipment)
            {
                var copy=new EquipmentState{id=e.id,kind=e.kind,x=e.x,z=e.z,yaw=e.yaw,paidPrice=e.paidPrice,starter=e.starter,filterKind=e.filterKind,routeCursor=e.routeCursor};
                foreach(var item in e.contents)copy.contents.Add(CloneStack(item));
                if(e.job!=null){var j=e.job;copy.job=new ProcessingJob{recipeId=j.recipeId,input=j.input,inputQuantity=j.inputQuantity,requiredStrokes=j.requiredStrokes,
                    strokes=j.strokes,duration=j.duration,remaining=j.remaining,ready=j.ready,xpEligible=j.xpEligible,yields=CloneAmounts(j.yields)};}
                c.equipment.Add(copy);
            }
            foreach(var link in s.powerLinks)c.powerLinks.Add(new PowerLink(link.a,link.b));
            if(s.belts==null)c.belts=null;
            else foreach(var belt in s.belts)
            {
                var copy=new ConveyorLink{id=belt.id,fromId=belt.fromId,fromPort=belt.fromPort,toId=belt.toId,toPort=belt.toPort,
                    paidPrice=belt.paidPrice,bendXFirst=belt.bendXFirst,launchRemaining=belt.launchRemaining};
                foreach(var item in belt.items)copy.items.Add(new ConveyorItem{id=item.id,kind=item.kind,quantity=item.quantity,xpEligible=item.xpEligible,progress=item.progress});
                c.belts.Add(copy);
            }
            return c;
        }
        static string Fingerprint(CompactYardState s)
        {
            var b=new StringBuilder();b.Append(s.version).Append(',').Append(s.money).Append(',').Append(s.experience).Append(',').Append(s.nextId).Append(',').Append(s.carriedId);
            foreach(var i in s.items)AppendStack(b,i);
            foreach(var large in s.scrap){b.Append("/L:").Append(large.id).Append(':').Append(large.kind).Append(':').Append(large.x).Append(':').Append(large.z)
                .Append(':').Append(large.inspected).Append(':').Append(large.strokes).Append(':').Append(large.requiredStrokes);AppendAmounts(b,large.remaining);}
            foreach(var e in s.equipment)
            {
                b.Append("/E:").Append(e.id).Append(':').Append(e.kind).Append(':').Append(e.x).Append(':').Append(e.z).Append(':').Append(e.yaw).Append(':').Append(e.paidPrice).Append(':').Append(e.starter);
                foreach(var i in e.contents)AppendStack(b,i);
                if(e.job!=null){var j=e.job;b.Append("/J:").Append(j.recipeId).Append(':').Append(j.input).Append(':').Append(j.inputQuantity).Append(':').Append(j.requiredStrokes)
                    .Append(':').Append(j.strokes).Append(':').Append(j.duration).Append(':').Append(j.remaining).Append(':').Append(j.ready).Append(':').Append(j.xpEligible);AppendAmounts(b,j.yields);}
            }
            foreach(var link in s.powerLinks)b.Append("/P:").Append(link.a).Append(':').Append(link.b);return b.ToString();
        }
        static void AppendStack(StringBuilder b,CompactStack i){b.Append("/I:").Append(i.id).Append(':').Append(i.kind).Append(':').Append(i.quantity).Append(':').Append(i.x).Append(':').Append(i.y).Append(':').Append(i.z).Append(':').Append(i.xpEligible);}
        static void AppendAmounts(StringBuilder b,PartAmount[] a){if(a==null){b.Append("/null");return;}foreach(var y in a)b.Append('/').Append(y.kind).Append(':').Append(y.quantity);}
        public static void Run(string name)
        {
            var m=Fresh();int bench=Bench(m);
            switch(name)
            {
                case "CompactFreshSetup":
                    Check(m.State.money==8 && m.State.experience==0 && m.Level==1 && m.State.equipment.Count==1 && m.State.scrap.Count==2,"modest manual start");
                    Check(m.State.equipment[0].starter && m.State.equipment[0].kind==EquipmentKind.Workbench && m.State.equipment[0].x==-5,"movable starter bench");
                    Check(!m.Pickup(m.State.scrap[0].id) && m.State.carriedId==0,"whole car never portable");break;
                case "CompactInspectBeforeWork":
                    int car=Large(m,ScrapObjectKind.Car).id;string before=Fingerprint(m.State);
                    Check(!m.WorkScrap(car) && !m.CollectScrap(car,0) && before==Fingerprint(m.State),"inspection first, no premature yields");
                    Check(m.InspectScrap(car) && m.OccupiedSlots==3,"reserve components");Check(!m.InspectScrap(car),"inspection once");
                    Check(m.ScrapWorkStage(car)=="Release wiring" && m.WorkScrap(car) && m.ScrapWorkStage(car)=="Remove battery leads","readable stages");break;
                case "CompactCarChainConservation":
                    var carTotals=ProcessLarge(m,ScrapObjectKind.Car);
                    Check(carTotals[(int)PartKind.Copper]==10 && carTotals[(int)PartKind.Steel]==18 && carTotals[(int)PartKind.Insulation]==4,"car hierarchical yields");
                    Check(m.State.items.Count==0 && m.State.experience==42,"sale XP equals material units");break;
                case "CompactFridgeChain":
                    var fridgeTotals=ProcessLarge(m,ScrapObjectKind.Refrigerator);
                    Check(fridgeTotals[(int)PartKind.Copper]==6 && fridgeTotals[(int)PartKind.Steel]==8 && fridgeTotals[(int)PartKind.Insulation]==2 && fridgeTotals[(int)PartKind.Plastic]==2,"fridge component hierarchy");break;
                case "CompactPartialSnapshotResume":
                    int objectId=Large(m,ScrapObjectKind.Car).id;m.InspectScrap(objectId);m.WorkScrap(objectId);m.WorkScrap(objectId);
                    var resumed=new ScrappingModel(m.Rules,Copy(m.State));Check(resumed.FindScrap(objectId).strokes==2,"partial dismantle retained");
                    while(resumed.FindScrap(objectId).strokes<6)resumed.WorkScrap(objectId);resumed.CollectScrap(objectId,0);
                    resumed.Drop(1,.25f,1);var again=new ScrappingModel(m.Rules,Copy(resumed.State));
                    Check(!again.CollectScrap(objectId,0) && again.FindScrap(objectId).remaining[0].quantity==0,"collected component never repeats");break;
                case "CompactManualDurableRecipeTuning":
                    m.AcquireWire();m.BeginProcessing(bench);m.Work(bench);m.Work(bench);
                    var changed=new CompactRules();changed.Recipe(PartKind.Wire).strokes=1;changed.Recipe(PartKind.Wire).yields[0].quantity=9;
                    m=new ScrappingModel(changed,Copy(m.State));Check(m.FindEquipment(bench).job.requiredStrokes==4,"saved threshold retained");FinishManual(m,bench);
                    Check(m.CollectOutput(bench,0) && m.Carried.quantity==3,"saved exact yields retained");break;
                case "CompactSalesXPAndNoCollectionXP":
                    m.AcquireWire();m.BeginProcessing(bench);FinishManual(m,bench);m.CollectOutput(bench,0);
                    Check(m.State.experience==0,"work/collection gives no XP");int stack=m.State.carriedId;m.Drop(0,.25f,0);m.Pickup(stack);
                    Check(m.State.experience==0 && m.SaleQuote().experience==6,"movement gives no XP");
                    Check(m.Sell() && m.State.experience==6 && m.State.money==17 && !m.Sell() && !m.CollectOutput(bench,0),"one valid sale only");break;
                case "CompactLevelTwoSaleImprovement":
                    m.AcquireWire();m.BeginProcessing(bench);FinishManual(m,bench);m.CollectOutput(bench,0);
                    var firstLevel=m.SaleQuote();Check(firstLevel.baseTotal==9 && firstLevel.bonusTotal==0 && firstLevel.total==9,"ordinary level-one copper bundle");
                    m.State.experience=m.Rules.levelThresholds[1];var secondLevel=m.SaleQuote();
                    Check(m.Level==2 && secondLevel.quantity==3 && secondLevel.unitPrice==3 && secondLevel.bonusPercent==12 && secondLevel.baseTotal==9 && secondLevel.bonusTotal==1 && secondLevel.total==10,"level two gives a visible extra euro on a common sale");
                    Check(secondLevel.experience==firstLevel.experience && secondLevel.experience==6,"reputation changes price, never recovery XP");
                    int preSaleCash=m.State.money,preSaleXP=m.State.experience;
                    Check(m.Sell() && m.State.money==preSaleCash+10 && m.State.experience==preSaleXP+6,"quoted increase is paid once");
                    Check(!m.Sell() && m.State.money==preSaleCash+10,"sale cannot repeat the level reward");
                    m.CollectOutput(bench,1);var smallQuote=m.SaleQuote();Check(smallQuote.baseTotal==2 && smallQuote.bonusTotal==0 && smallQuote.total==2,"small bonuses still use consistent floor rounding");break;
                case "CompactLevelPriceBonusUnlock":
                    m.State.experience=m.Rules.levelThresholds[8];m.AcquireWire();m.BeginProcessing(bench);FinishManual(m,bench);m.CollectOutput(bench,0);
                    m.Carried.quantity=100;m.State.experience=m.Rules.levelThresholds[9]-200;
                    var quote=m.SaleQuote();Check(quote.baseTotal==300 && quote.bonusPercent==40 && quote.bonusTotal==120 && quote.total==420,"separately explained level bonus");
                    Check(m.Sell() && m.Level==10 && m.LastNotice.Contains("is now purchasable") && m.Rules.Equipment(EquipmentKind.Conveyor).available,"unlock grants purchase eligibility without gifting equipment");
                    Check(m.LevelFraction==0 && m.XPToNextLevel>0,"visible progress reset");break;
                case "CompactRejectBuyResell":
                    Check(m.AcquireWire() && !m.CanSell() && !m.Sell() && m.State.experience==0,"raw wire cannot sell for XP");
                    m.Drop(0,.25f,0);int scrapId=Large(m,ScrapObjectKind.Car).id;FinishLarge(m,scrapId);m.CollectScrap(scrapId,0);
                    Check(!m.Sell() && m.State.experience==0,"raw motor cannot immediately resell");break;
                case "CompactRenewableZeroMoney":
                    m.State.money=0;
                    for(int i=0;i<20;i++){Check(m.AcquireWire(),"renewable supply");Check(m.BeginProcessing(bench),"renewable load");FinishManual(m,bench);DrainProcessed(m,bench);}
                    Check(m.State.money>=220 && m.State.experience==160,"zero money can recover and fund equipment");break;
                case "CompactOutputCapacityGuards":
                    m.Rules.Equipment(EquipmentKind.Workbench).outputCapacity=4;m.AcquireWire();string capBefore=Fingerprint(m.State);
                    Check(!m.BeginProcessing(bench) && capBefore==Fingerprint(m.State),"undersized output never consumes input");
                    m.Rules.Equipment(EquipmentKind.Workbench).outputCapacity=24;m.Carried.quantity=10;capBefore=Fingerprint(m.State);
                    Check(!m.BeginProcessing(bench) && capBefore==Fingerprint(m.State),"oversized batch rejected atomically");break;
                case "CompactReservedOutputSurvivesCapacityReduction":
                    m.AcquireWire();m.BeginProcessing(bench);FinishManual(m,bench);
                    var lower=new CompactRules{maxStacks=1};lower.Equipment(EquipmentKind.Workbench).outputCapacity=1;
                    m=new ScrappingModel(lower,Copy(m.State));Check(m.OccupiedSlots==2 && !m.AcquireWire(),"reduced capacity blocks new items");
                    Check(m.CollectOutput(bench,0) && m.Sell() && m.CollectOutput(bench,1) && m.Sell(),"reserved outputs remain collectable");break;
                case "CompactPowerPauseResumeAndBackpressure":
                    var machine=AddMachine(m);m.AcquireWire();m.BeginProcessing(machine.id);float time=machine.job.remaining;
                    Check(!m.Tick(1) && machine.job.remaining==time,"default no power safe");bool supplied=true;m.HasPower=id=>supplied;
                    Check(m.Tick(1) && machine.job.remaining==time-1,"connected processing");supplied=false;Check(!m.Tick(99) && machine.job.remaining==time-1,"outage preserves timer");
                    supplied=true;m.Rules.Equipment(EquipmentKind.Tier1Scrapper).outputCapacity=4;Check(!m.Tick(99) && machine.job.remaining==time-1 && m.ProcessingBlockReason(machine.id).Contains("Output blocked"),"backpressure preserves input and time");
                    m.Rules.Equipment(EquipmentKind.Tier1Scrapper).outputCapacity=24;Check(m.Tick(99) && machine.job.ready,"recovered power/capacity resumes once");
                    DrainProcessed(m,machine.id);Check(!m.Tick(10) && m.State.experience==8,"one completed powered batch");break;
                case "CompactTimedSnapshotsResume":
                    var timer=AddMachine(m);m.HasPower=id=>true;m.AcquireWire();m.BeginProcessing(timer.id);m.Tick(1.5f);
                    var tuned=new CompactRules();tuned.Equipment(EquipmentKind.Tier1Scrapper).processingSeconds=1;tuned.Recipe(PartKind.Wire).yields[0].quantity=7;
                    m=new ScrappingModel(tuned,Copy(m.State));m.HasPower=id=>true;Check(m.FindEquipment(timer.id).job.remaining==2.5f && m.FindEquipment(timer.id).job.duration==4,"running snapshot survives tuning");
                    m.Tick(2.5f);m.CollectOutput(timer.id,0);Check(m.Carried.quantity==3,"saved powered yield unchanged");break;
                case "CompactFailedTransactionsNonmutating":
                    string pristine=Fingerprint(m.State);
                    Check(!m.BuyScrap(ScrapObjectKind.Car) && !m.BuyScrap((ScrapObjectKind)99) && !m.CollectOutput(bench,0) && !m.Sell() && pristine==Fingerprint(m.State),"failed pristine operations nonmutating");
                    m.AcquireWire();string holding=Fingerprint(m.State);
                    Check(!m.AcquireWire() && !m.Drop(float.NaN,0,0) && !m.Drop(25,0,0) && !m.Pickup(12345) && !m.InspectScrap(m.State.scrap[0].id) && holding==Fingerprint(m.State),"failed holding actions nonmutating");
                    Invalid(()=>m.Tick(float.PositiveInfinity),"nonfinite time rejected");Invalid(()=>m.Tick(-1),"negative time rejected");Check(holding==Fingerprint(m.State),"bad timer unchanged");break;
                case "CompactOverflowAndIds":
                    m.AcquireWire();m.BeginProcessing(bench);FinishManual(m,bench);m.CollectOutput(bench,0);m.State.money=int.MaxValue;
                    string rich=Fingerprint(m.State);Check(!m.Sell() && rich==Fingerprint(m.State),"cash overflow cannot consume item");
                    m.State.money=0;m.State.experience=int.MaxValue;rich=Fingerprint(m.State);Check(!m.Sell() && rich==Fingerprint(m.State),"XP overflow cannot consume item");
                    m.Drop(0,.25f,0);m.State.nextId=int.MaxValue;rich=Fingerprint(m.State);Check(!m.AcquireWire() && rich==Fingerprint(m.State),"identifier overflow rejected");break;
                case "CompactInvalidData":
                    var invalid=Copy(m.State);invalid.nextId=1;Invalid(()=>ScrappingModel.Validate(invalid,m.Rules),"ID bounds");
                    invalid=Copy(m.State);invalid.equipment[0].id=invalid.scrap[0].id;Invalid(()=>ScrappingModel.Validate(invalid,m.Rules),"global duplicate IDs");
                    invalid=Copy(m.State);invalid.playerX=float.NaN;Invalid(()=>ScrappingModel.Validate(invalid,m.Rules),"nonfinite position");
                    invalid=Copy(m.State);invalid.carriedId=999;Invalid(()=>ScrappingModel.Validate(invalid,m.Rules),"missing carry");
                    invalid=Copy(m.State);invalid.equipment.Clear();Invalid(()=>ScrappingModel.Validate(invalid,m.Rules),"missing renewable-loop workbench");
                    invalid=Copy(m.State);invalid.equipment.Add(new EquipmentState{id=invalid.nextId++,kind=EquipmentKind.Conveyor,x=0,z=0});Invalid(()=>ScrappingModel.Validate(invalid,m.Rules),"unsupported future machine");
                    m.AcquireWire();m.BeginProcessing(bench);invalid=Copy(m.State);invalid.equipment[0].job.ready=true;Invalid(()=>ScrappingModel.Validate(invalid,m.Rules),"inconsistent readiness");
                    invalid=Copy(m.State);invalid.equipment[0].job.yields[0].quantity=0;Invalid(()=>ScrappingModel.Validate(invalid,m.Rules),"premature output");break;
                case "CompactInvalidBalance":
                    var bad=new CompactRules();bad.recipes[0].yields[0].quantity=-1;Invalid(()=>bad.Validate(),"negative yield");
                    bad=new CompactRules();bad.saleBonusPercents[1]=-1;Invalid(()=>bad.Validate(),"negative bonus");
                    bad=new CompactRules();bad.levelThresholds[2]=bad.levelThresholds[1];Invalid(()=>bad.Validate(),"nonincreasing curve");
                    bad=new CompactRules();bad.recipes[1].id=bad.recipes[0].id;Invalid(()=>bad.Validate(),"unstable recipe IDs");
                    bad=new CompactRules();bad.Equipment(EquipmentKind.ExportStation).available=true;Invalid(()=>bad.Validate(),"cannot pretend export stage works");break;
                case "CompactLegacyPortableSalesSalvage":
                    m.State.items.Add(new CompactStack{id=m.State.nextId++,kind=PartKind.RestoredFan,quantity=1});m.State.carriedId=m.State.items[0].id;
                    Check(m.SaleQuote().total==42 && m.SaleQuote().experience==0 && m.Sell(),"existing tested appliance remains saleable");
                    m.State.items.Add(new CompactStack{id=m.State.nextId++,kind=PartKind.BrokenRadio,quantity=1});m.State.carriedId=m.State.items[0].id;
                    Check(m.BeginProcessing(bench),"legacy broken radio has salvage recipe");FinishManual(m,bench);DrainProcessed(m,bench);Check(m.State.experience==0,"legacy lineage remains XP-ineligible");break;
                case "CompactFullStartingProgression":
                    ProcessLarge(m,ScrapObjectKind.Car);ProcessLarge(m,ScrapObjectKind.Refrigerator);
                    int earnings=m.State.money;Check(earnings==93,"initial scrap earns a modest positive cash buffer including earned level bonuses");
                    // Small further manual work provides a guaranteed affordable path independent of purchase order.
                    for(int i=0;i<2;i++){m.AcquireWire();m.BeginProcessing(bench);FinishManual(m,bench);DrainProcessed(m,bench);}
                    Check(m.State.money>=m.Rules.Equipment(EquipmentKind.Generator).price+m.Rules.Equipment(EquipmentKind.Tier1Scrapper).price,"first powered progression affordable");break;
                case "CompactDeterministicMaterialLedger":
                    var rng=new Random(718);int acquired=0,soldCopper=0,soldInsulation=0;long salesXP=0;
                    for(int i=0;i<8000;i++)
                    {
                        switch(rng.Next(9))
                        {
                            case 0:if(m.AcquireWire())acquired++;break;
                            case 1:m.BeginProcessing(bench);break;
                            case 2:m.Work(bench);break;
                            case 3:m.CollectOutput(bench,0);break;
                            case 4:m.CollectOutput(bench,1);break;
                            case 5:var q=m.SaleQuote();if(m.Sell()){if(q.kind==PartKind.Copper)soldCopper+=q.quantity;if(q.kind==PartKind.Insulation)soldInsulation+=q.quantity;salesXP+=q.experience;}break;
                            case 6:if(m.Carried!=null)m.Drop(0,.25f,0);break;
                            case 7:if(m.State.items.Count>0)m.Pickup(m.State.items[rng.Next(m.State.items.Count)].id);break;
                            case 8:m=new ScrappingModel(m.Rules,Copy(m.State));break;
                        }
                        ScrappingModel.Validate(m.State,m.Rules);
                        int wire=0,copper=soldCopper,insulation=soldInsulation;
                        foreach(var item in m.State.items){if(item.kind==PartKind.Wire)wire+=item.quantity;if(item.kind==PartKind.Copper)copper+=item.quantity;if(item.kind==PartKind.Insulation)insulation+=item.quantity;}
                        foreach(var e in m.State.equipment)if(e.job!=null)foreach(var y in e.job.yields){if(y.kind==PartKind.Copper)copper+=y.quantity;if(y.kind==PartKind.Insulation)insulation+=y.quantity;}
                        Check(copper==3*(acquired-wire) && insulation==2*(acquired-wire),"material ledger conserved through partial processing/resume");
                        Check(m.State.experience==salesXP,"XP equals completed valid sales");
                    }
                    Check(acquired>10 && soldCopper>10,"ledger exercised productive cycles");break;
                case "CompactAllocationLimitOutputsRetained":
                    m.AcquireWire();m.BeginProcessing(bench);FinishManual(m,bench);m.State.nextId=int.MaxValue;
                    string limit=Fingerprint(m.State);Check(!m.CollectOutput(bench,0) && limit==Fingerprint(m.State),"collect failure preserves output");break;
                case "CompactPurchasedScrapStages":
                    ProcessLarge(m,ScrapObjectKind.Car);m.State.money=100;int xpBefore=m.State.experience;
                    Check(m.BuyScrap(ScrapObjectKind.Car) && m.State.money==75 && m.State.experience==xpBefore,"buy does not award XP");
                    int purchased=Large(m,ScrapObjectKind.Car).id;Check(!m.Pickup(purchased) && !m.CollectScrap(purchased,0),"purchased whole object requires work");
                    Check(!m.BuyScrap(ScrapObjectKind.Car) && m.State.money==75,"occupied bay no double charge");break;
                case "CompactPartialLargeRecipeTuning":
                    int refrigerator=Large(m,ScrapObjectKind.Refrigerator).id;m.InspectScrap(refrigerator);m.WorkScrap(refrigerator);
                    var newData=new CompactRules();var largeRule=newData.LargeRecipe(ScrapObjectKind.Refrigerator);largeRule.strokes=1;largeRule.workStages=new[]{"Open"};largeRule.yields[0].quantity=3;
                    m=new ScrappingModel(newData,Copy(m.State));Check(m.RequiredScrapStrokes(refrigerator)==4,"large threshold snapshotted");
                    while(m.FindScrap(refrigerator).strokes<4)m.WorkScrap(refrigerator);m.CollectScrap(refrigerator,0);Check(m.Carried.quantity==1,"large original yield snapshot");break;
                default:throw new Exception(name);
            }
            ScrappingModel.Validate(m.State,m.Rules);
        }
    }
}
