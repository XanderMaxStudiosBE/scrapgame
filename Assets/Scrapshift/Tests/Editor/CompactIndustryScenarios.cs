using System;
using System.Text;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactIndustryScenarios
    {
        public static readonly string[] Names={"IndustryOptInAndReadOnlyConfiguration","IndustryFeedOwnedObjectOnce","IndustryRejectsPartialScrap",
            "IndustryStandingDeliveryCadence","IndustryFundingPowerAndCapacityFreeze","IndustryDisablePreservesPaidWork","IndustrySnapshotAndPartialResume",
            "IndustryCompletionCapacityAndIdentities","IndustryPrimaryReservationAndPortableGuards","IndustryDispatchGroupingAndCarryIdentity",
            "IndustryDispatchImportedLineage","IndustryDispatchQuoteCancelAndOverflow","IndustryAutoDispatchCadenceAndDisable",
            "IndustryPartitionedClockEquivalence","IndustryStableScarceFundsOrder","IndustryBoundedElapsedAtomic","IndustrySchemaAndIdentityGuards","IndustryCorruptTimerAndJobs",
            "IndustryPriceChangesPreserveStoredStock","IndustryStandingServiceNoDebtOrHistoricalCatchup","IndustryManualRecoveryFundsStandingService",
            "IndustryRejectsInvalidLiveTuning","IndustrySaleAndProcessingLedger"};
        static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
        static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new Exception("Invalid industry input was accepted.");}
        static ScrappingModel Fresh(){return new ScrappingModel(new CompactRules());}
        static EquipmentState Place(ScrappingModel model,EquipmentKind kind,float x=0,float z=5)
        {
            var equipment=new EquipmentState{id=model.State.nextId++,kind=kind,x=x,z=z,paidPrice=model.Rules.Equipment(kind).price};model.State.equipment.Add(equipment);return equipment;
        }
        static CompactStack Buffer(ScrappingModel model,EquipmentState equipment,PartKind kind,int quantity,bool eligible=true)
        {
            var stack=new CompactStack{id=model.State.nextId++,kind=kind,quantity=quantity,x=equipment.x,z=equipment.z,xpEligible=eligible};equipment.contents.Add(stack);return stack;
        }
        public static CompactYardState Copy(CompactYardState source)
        {
            var copy=CompactCareerScenarios.Copy(source);
            for(int i=0;i<source.equipment.Count;i++)
            {
                var state=source.equipment[i].industry;if(state==null)continue;
                var clone=new IndustrialMachineState{version=state.version,enabled=state.enabled,purchaseKind=state.purchaseKind,remaining=state.remaining,
                    objectsProcessed=state.objectsProcessed,exportedUnits=state.exportedUnits,exportedRevenue=state.exportedRevenue,exportedXp=state.exportedXp};
                if(state.primary!=null)
                {
                    var job=state.primary;var amounts=new PartAmount[job.yields.Length];for(int y=0;y<amounts.Length;y++)amounts[y]=new PartAmount(job.yields[y].kind,job.yields[y].quantity);
                    clone.primary=new PrimaryScrapJob{id=job.id,kind=job.kind,duration=job.duration,remaining=job.remaining,xpEligible=job.xpEligible,yields=amounts};
                }
                copy.equipment[i].industry=clone;
            }
            return copy;
        }
        static string Fingerprint(ScrappingModel model)
        {
            var state=model.State;var b=new StringBuilder();b.Append(state.version).Append(':').Append(state.money).Append(':').Append(state.experience).Append(':').Append(state.nextId).Append(':').Append(state.carriedId);
            foreach(var scrap in state.scrap)b.Append("/S:").Append(scrap.id).Append(':').Append(scrap.inspected).Append(':').Append(scrap.strokes);
            foreach(var item in state.items)b.Append("/H:").Append(item.id).Append(':').Append(item.kind).Append(':').Append(item.quantity).Append(':').Append(item.xpEligible);
            foreach(var equipment in state.equipment)
            {
                b.Append("/E:").Append(equipment.id).Append(':').Append(equipment.kind).Append(':').Append(equipment.filterKind);
                foreach(var item in equipment.contents)b.Append("/C:").Append(item.id).Append(':').Append(item.kind).Append(':').Append(item.quantity).Append(':').Append(item.xpEligible);
                var industrial=equipment.industry;if(industrial==null)continue;
                b.Append("/I:").Append(industrial.enabled).Append(':').Append(industrial.purchaseKind).Append(':').Append(industrial.remaining).Append(':').Append(industrial.objectsProcessed)
                    .Append(':').Append(industrial.exportedUnits).Append(':').Append(industrial.exportedRevenue).Append(':').Append(industrial.exportedXp);
                var job=industrial.primary;if(job==null)continue;
                b.Append("/P:").Append(job.id).Append(':').Append(job.kind).Append(':').Append(job.duration).Append(':').Append(job.remaining).Append(':').Append(job.xpEligible);
                foreach(var yield in job.yields)b.Append('/').Append(yield.kind).Append(':').Append(yield.quantity);
            }
            var career=model.Career.Stats;b.Append("/J:").Append(career.materialUnitsSold).Append(':').Append(career.salesRevenue).Append(':').Append(career.poweredBatches)
                .Append(':').Append(career.contractBonuses).Append(':').Append(career.contractsCompleted);return b.ToString();
        }
        static void Equivalent(ScrappingModel left,ScrappingModel right)
        {
            Check(left.State.money==right.State.money && left.State.experience==right.State.experience && left.State.nextId==right.State.nextId,"partitioned totals/identity sequence");
            for(int i=0;i<left.State.equipment.Count;i++)
            {
                var a=left.State.equipment[i];var b=right.State.equipment[i];Check(a.contents.Count==b.contents.Count,"partitioned stack count");
                for(int j=0;j<a.contents.Count;j++)Check(a.contents[j].id==b.contents[j].id && a.contents[j].kind==b.contents[j].kind && a.contents[j].quantity==b.contents[j].quantity && a.contents[j].xpEligible==b.contents[j].xpEligible,"partitioned outputs/lineage");
                if(a.industry==null){Check(b.industry==null,"partitioned service ownership");continue;}
                Check(Math.Abs(a.industry.remaining-b.industry.remaining)<.0001f && a.industry.objectsProcessed==b.industry.objectsProcessed && a.industry.exportedUnits==b.industry.exportedUnits,"partitioned service clocks and counters");
                Check((a.industry.primary==null)==(b.industry.primary==null),"partitioned pending jobs");
                if(a.industry.primary!=null)Check(a.industry.primary.id==b.industry.primary.id && Math.Abs(a.industry.primary.remaining-b.industry.primary.remaining)<.0001f,"partitioned processing clocks");
            }
        }
        public static void Run(string name)
        {
            var model=Fresh();var primary=Place(model,EquipmentKind.PrimaryScrapper);model.HasPower=id=>true;int firstCar=model.State.scrap[0].id;string reason;
            switch(name)
            {
                case "IndustryOptInAndReadOnlyConfiguration":
                    string baseline=Fingerprint(model);Check(!model.Industry.Tick(1000) && baseline==Fingerprint(model),"no configured service means no automatic purchases");
                    model.Industry.Status(primary.id);model.Industry.CanFeedScrap(primary.id,firstCar,out reason);model.Industry.DispatchQuote(primary.id);
                    Check(baseline==Fingerprint(model) && primary.industry==null,"status/quotes/previews do not initialize or change state");
                    Check(model.AcquireWire(),"held item");int held=model.State.carriedId;
                    Check(model.Industry.SetEnabled(primary.id,true) && primary.industry.remaining==60 && model.State.carriedId==held,"explicit enable starts full interval without changing hands");
                    Check(!model.Industry.SetEnabled(primary.id,true) && model.Industry.SetPurchaseKind(primary.id,ScrapObjectKind.Refrigerator) && model.State.carriedId==held,"configuration never borrows player inventory");break;
                case "IndustryFeedOwnedObjectOnce":
                    int next=model.State.nextId,cash=model.State.money;Check(model.Industry.FeedScrap(primary.id,firstCar),model.Industry.LastMessage);
                    Check(primary.industry.primary.id==firstCar && model.FindScrap(firstCar)==null && model.State.nextId==next && model.State.money==cash,"owned identity transferred without charge/allocation");
                    Check(!model.Industry.FeedScrap(primary.id,firstCar) && model.State.experience==0,"no duplicate source intake or free XP");
                    Check(model.Industry.Tick(24) && primary.industry.primary==null && primary.contents.Count==3 && primary.industry.objectsProcessed==1,"all outputs committed once");
                    Check(primary.contents[0].kind==PartKind.Motor && primary.contents[0].quantity==1 && primary.contents[1].quantity==2 && primary.contents[2].quantity==3 && model.State.nextId==next+3,"exact car recipe and unique output IDs");
                    Check(!model.Industry.Tick(1000) && primary.industry.objectsProcessed==1 && model.State.experience==0,"disabled delivery cannot generate more work/reward");break;
                case "IndustryRejectsPartialScrap":
                    Check(model.InspectScrap(firstCar),"inspect source");string inspected=Fingerprint(model);
                    Check(!model.Industry.CanFeedScrap(primary.id,firstCar,out reason) && !model.Industry.FeedScrap(primary.id,firstCar) && inspected==Fingerprint(model),"inspected component reservation is preserved");
                    Check(model.WorkScrap(firstCar),"manual source still works");inspected=Fingerprint(model);
                    Check(!model.Industry.FeedScrap(primary.id,firstCar) && inspected==Fingerprint(model),"partially dismantled source cannot reset to whole");break;
                case "IndustryStandingDeliveryCadence":
                    model.State.money=100;model.Industry.SetEnabled(primary.id,true);Check(model.Industry.Tick(59) && primary.industry.primary==null && model.State.money==100,"no early delivery");
                    Check(model.Industry.Tick(1) && primary.industry.primary!=null && model.State.money==75 && primary.industry.primary.remaining==24,"one charged intake at eligible60s");
                    model.Industry.Tick(24);Check(primary.industry.objectsProcessed==1 && primary.industry.remaining==60 && model.State.money==75,"busy time does not buy another object");
                    model.Industry.Tick(60);Check(primary.industry.primary!=null && model.State.money==50,"next eligible60s purchase once");break;
                case "IndustryFundingPowerAndCapacityFreeze":
                    model.State.money=0;model.Industry.SetEnabled(primary.id,true);string waiting=Fingerprint(model);
                    Check(!model.Industry.Tick(1000) && waiting==Fingerprint(model),"zero cash does not count toward purchases or create debt");
                    model.State.money=100;model.HasPower=id=>false;waiting=Fingerprint(model);Check(!model.Industry.Tick(1000) && waiting==Fingerprint(model),"power shortage preserves standing clock");
                    model.HasPower=id=>true;model.Rules.Equipment(EquipmentKind.PrimaryScrapper).outputCapacity=5;waiting=Fingerprint(model);
                    Check(!model.Industry.Tick(1000) && waiting==Fingerprint(model),"undersized buffer cannot consume standing timer/money");
                    model.Rules.Equipment(EquipmentKind.PrimaryScrapper).outputCapacity=48;model.Industry.Tick(60);Check(model.State.money==75 && primary.industry.primary!=null,"eligible work begins only after blockage clears");break;
                case "IndustryDisablePreservesPaidWork":
                    model.State.money=100;model.Industry.SetEnabled(primary.id,true);model.Industry.Tick(65);
                    Check(model.Industry.SetEnabled(primary.id,false) && model.Industry.SetPurchaseKind(primary.id,ScrapObjectKind.Refrigerator),"disable/next-kind while busy");
                    Check(primary.industry.primary.kind==ScrapObjectKind.Car && primary.industry.primary.remaining==19,"paid snapshot unaffected");
                    model.Industry.Tick(1000);Check(primary.industry.objectsProcessed==1 && primary.industry.primary==null && model.State.money==75,"paid work finishes but no later standing charge");
                    model.Industry.SetEnabled(primary.id,true);model.Industry.Tick(60);Check(primary.industry.primary.kind==ScrapObjectKind.Refrigerator && model.State.money==60,"next object uses selected kind once");break;
                case "IndustrySnapshotAndPartialResume":
                    model.Industry.FeedScrap(primary.id,firstCar);model.Industry.Tick(9);var snapshot=Copy(model.State);var tuned=new CompactRules();
                    tuned.LargeRecipe(ScrapObjectKind.Car).yields[0].quantity=5;tuned.Equipment(EquipmentKind.PrimaryScrapper).processingSeconds=2;
                    model=new ScrappingModel(tuned,snapshot);primary=model.FindEquipment(primary.id);model.HasPower=id=>true;
                    Check(primary.industry.primary.duration==24 && primary.industry.primary.remaining==15 && primary.industry.primary.yields[0].quantity==1,"duration/yields/identity snapshot survives balance edits");
                    model.Industry.Tick(15);model=new ScrappingModel(tuned,Copy(model.State));primary=model.FindEquipment(primary.id);model.HasPower=id=>true;
                    Check(primary.contents[0].quantity==1 && primary.industry.objectsProcessed==1 && !model.Industry.Tick(100),"saved completed buffer never repeats outputs");break;
                case "IndustryCompletionCapacityAndIdentities":
                    model.Industry.FeedScrap(primary.id,firstCar);model.Industry.Tick(10);model.Rules.Equipment(EquipmentKind.PrimaryScrapper).outputCapacity=5;string blocked=Fingerprint(model);
                    Check(!model.Industry.Tick(100) && blocked==Fingerprint(model),"reduced output capacity preserves paid progress");
                    model.Rules.Equipment(EquipmentKind.PrimaryScrapper).outputCapacity=48;model.State.nextId=int.MaxValue;blocked=Fingerprint(model);
                    Check(!model.Industry.Tick(100) && blocked==Fingerprint(model),"exhausted output IDs preserve paid snapshot");break;
                case "IndustryPrimaryReservationAndPortableGuards":
                    model.Industry.FeedScrap(primary.id,firstCar);Check(model.OccupiedSlots==3 && CompactIndustryModel.ReservedSlots(primary)==3 && CompactIndustryModel.ReservedUnits(primary)==6,"primary outputs occupy durable reserved slots/units");
                    Check(model.AcquireWire(),"wire keeps ordinary space");string portable=Fingerprint(model);Check(!model.Deposit(primary.id) && portable==Fingerprint(model),"portable input cannot enter primary output");
                    model.Industry.Tick(24);Check(model.OccupiedSlots==4 && primary.contents.Count==3 && model.Carried.kind==PartKind.Wire,"completion replaces reservations with equal output slots and preserves hands");break;
                case "IndustryDispatchGroupingAndCarryIdentity":
                    var exporter=Place(model,EquipmentKind.ExportStation,10,5);Buffer(model,exporter,PartKind.Copper,3);Buffer(model,exporter,PartKind.Copper,2);Buffer(model,exporter,PartKind.Copper,7,false);
                    model.AcquireWire();int heldId=model.State.carriedId;var quote=model.Industry.DispatchQuote(exporter.id);
                    Check(quote.allowed && quote.quantity==5 && quote.total==15 && quote.experience==10,"dispatch groups only same material/recovery lineage");
                    Check(model.Industry.DispatchNow(exporter.id) && model.State.carriedId==heldId && model.Carried.kind==PartKind.Wire,"dispatch never consumes/replaces player carry identity");
                    Check(exporter.contents.Count==1 && exporter.contents[0].quantity==7 && !exporter.contents[0].xpEligible,"imported group retained separately");
                    Check(model.Career.Stats.salesRevenue==15 && model.Career.Stats.materialUnitsSold==5 && model.Career.Stats.contractBonuses==0 && model.Career.CurrentContract.delivered==0,"normal sale/career path with no customer reward");break;
                case "IndustryDispatchImportedLineage":
                    var importedExport=Place(model,EquipmentKind.ExportStation,10,5);Buffer(model,importedExport,PartKind.Copper,3,false);
                    Check(model.Industry.DispatchQuote(importedExport.id).experience==0 && model.Industry.DispatchNow(importedExport.id),"imported dispatch earns only valid cash");
                    Check(model.State.experience==0 && importedExport.industry.exportedXp==0 && importedExport.industry.exportedRevenue==9,"no historical XP invention");break;
                case "IndustryDispatchQuoteCancelAndOverflow":
                    var richExport=Place(model,EquipmentKind.ExportStation,10,5);Buffer(model,richExport,PartKind.Copper,3);string quoteBefore=Fingerprint(model);
                    model.Industry.DispatchQuote(richExport.id);Check(quoteBefore==Fingerprint(model) && richExport.industry==null,"quote/cancel is read-only");
                    model.State.money=int.MaxValue;quoteBefore=Fingerprint(model);Check(!model.Industry.DispatchNow(richExport.id) && quoteBefore==Fingerprint(model),"cash overflow atomic");
                    model.State.money=0;model.State.experience=int.MaxValue;quoteBefore=Fingerprint(model);Check(!model.Industry.DispatchNow(richExport.id) && quoteBefore==Fingerprint(model),"XP overflow atomic");
                    model.State.experience=0;model.HasPower=id=>false;quoteBefore=Fingerprint(model);Check(!model.Industry.DispatchNow(richExport.id) && quoteBefore==Fingerprint(model),"manual dispatch still requires power");break;
                case "IndustryAutoDispatchCadenceAndDisable":
                    var timedExport=Place(model,EquipmentKind.ExportStation,10,5);Buffer(model,timedExport,PartKind.Copper,3);
                    Check(!model.Industry.Tick(1000) && timedExport.contents.Count==1,"no automatic sale until explicit enable");model.Industry.SetEnabled(timedExport.id,true);
                    model.Industry.Tick(4);Check(timedExport.contents.Count==1 && timedExport.industry.remaining==1,"no early dispatch");model.Industry.SetEnabled(timedExport.id,false);
                    Check(!model.Industry.Tick(1000) && timedExport.industry.remaining==1,"disabled timer preserved");model.Industry.SetEnabled(timedExport.id,true);model.Industry.Tick(1);
                    Check(timedExport.contents.Count==0 && timedExport.industry.exportedUnits==3 && model.State.experience==6,"single dispatch at full eligible cadence");
                    int exportCash=model.State.money;Check(!model.Industry.Tick(1000) && model.State.money==exportCash,"empty buffer cannot replay sale or consume nexttimer");break;
                case "IndustryPartitionedClockEquivalence":
                    model.State.money=200;var second=Place(model,EquipmentKind.PrimaryScrapper,10,5);var cadenceExport=Place(model,EquipmentKind.ExportStation,-10,5);Buffer(model,cadenceExport,PartKind.Copper,3,false);
                    model.Industry.SetEnabled(primary.id,true);model.Industry.SetPurchaseKind(second.id,ScrapObjectKind.Refrigerator);model.Industry.SetEnabled(second.id,true);model.Industry.SetEnabled(cadenceExport.id,true);
                    var divided=new ScrappingModel(model.Rules,Copy(model.State));divided.HasPower=id=>true;
                    model.Industry.Tick(252);for(int tick=0;tick<2520;tick++)divided.Industry.Tick(.1f);
                    Equivalent(model,divided);Check(primary.industry.objectsProcessed==3 && second.industry.objectsProcessed==3,"three exact cadence cycles independent of delta partition");break;
                case "IndustryBoundedElapsedAtomic":
                    model.State.money=1000;model.Industry.SetEnabled(primary.id,true);string bounded=Fingerprint(model);
                    Reject(()=>model.Industry.Tick(float.NaN));Reject(()=>model.Industry.Tick(-1));Reject(()=>model.Industry.Tick(float.PositiveInfinity));
                    Reject(()=>model.Industry.Tick(float.MaxValue));Check(bounded==Fingerprint(model),"invalid or over-budget tick rejects before money/jobs/progress change");
                    Check(model.Industry.Tick(84) && primary.industry.objectsProcessed==1,"smaller valid gameplay delta remains usable");break;
                case "IndustryStableScarceFundsOrder":
                    model.State.money=25;var later=Place(model,EquipmentKind.PrimaryScrapper,10,5);model.Industry.SetEnabled(primary.id,true);model.Industry.SetEnabled(later.id,true);
                    var reordered=new ScrappingModel(model.Rules,Copy(model.State));reordered.State.equipment.Reverse();reordered.HasPower=id=>true;
                    model.Industry.Tick(84);reordered.Industry.Tick(84);
                    Check(primary.industry.objectsProcessed==1 && later.industry.primary==null && model.State.money==0,"oldest durable machine gets scarce purchase first");
                    Check(reordered.FindEquipment(primary.id).industry.objectsProcessed==1 && reordered.FindEquipment(later.id).industry.primary==null && reordered.State.money==0,"serialized list order cannot change scarce-funds winner");break;
                case "IndustrySchemaAndIdentityGuards":
                    var old=Copy(model.State);old.version=3;Reject(()=>ScrappingModel.Validate(old,model.Rules));
                    old=Copy(model.State);old.equipment.RemoveAt(old.equipment.Count-1);old.version=3;ScrappingModel.Validate(old,model.Rules);
                    var oldModel=new ScrappingModel(model.Rules,old);Check(!oldModel.Industry.SetEnabled(old.equipment[0].id,true) && !oldModel.Industry.Tick(1000),"old schema receives no industrial state/simulation");
                    old.equipment[0].industry=new IndustrialMachineState();Reject(()=>ScrappingModel.Validate(old,model.Rules));
                    model.Industry.FeedScrap(primary.id,firstCar);var duplicate=Copy(model.State);duplicate.equipment[duplicate.equipment.Count-1].industry.primary.id=duplicate.equipment[0].id;Reject(()=>ScrappingModel.Validate(duplicate,model.Rules));
                    duplicate=Copy(model.State);duplicate.items.Add(new CompactStack{id=firstCar,kind=PartKind.Copper,quantity=1});Reject(()=>ScrappingModel.Validate(duplicate,model.Rules));break;
                case "IndustryCorruptTimerAndJobs":
                    model.Industry.FeedScrap(primary.id,firstCar);var corrupt=Copy(model.State);var bad=corrupt.equipment[corrupt.equipment.Count-1];bad.industry.remaining=float.NaN;Reject(()=>ScrappingModel.Validate(corrupt,model.Rules));
                    corrupt=Copy(model.State);bad=corrupt.equipment[corrupt.equipment.Count-1];bad.industry.primary.remaining=25;Reject(()=>ScrappingModel.Validate(corrupt,model.Rules));
                    corrupt=Copy(model.State);bad=corrupt.equipment[corrupt.equipment.Count-1];bad.industry.primary.yields[0].quantity=0;Reject(()=>ScrappingModel.Validate(corrupt,model.Rules));
                    corrupt=Copy(model.State);bad=corrupt.equipment[corrupt.equipment.Count-1];bad.industry.exportedXp=1;Reject(()=>ScrappingModel.Validate(corrupt,model.Rules));
                    corrupt=Copy(model.State);bad=corrupt.equipment[corrupt.equipment.Count-1];bad.industry.version=0;Reject(()=>ScrappingModel.Validate(corrupt,model.Rules));
                    var invalidExport=Place(model,EquipmentKind.ExportStation,10,5);Buffer(model,invalidExport,PartKind.Motor,1);Reject(()=>ScrappingModel.Validate(model.State,model.Rules));invalidExport.contents.Clear();break;
                case "IndustryPriceChangesPreserveStoredStock":
                    var freeExport=Place(model,EquipmentKind.ExportStation,10,5);Buffer(model,freeExport,PartKind.Copper,3);model.SetFilter(freeExport.id,(int)PartKind.Copper);model.Rules.Part(PartKind.Copper).unitPrice=0;
                    ScrappingModel.Validate(model.State,model.Rules);string free=Fingerprint(model);
                    Check(!model.Industry.DispatchNow(freeExport.id) && free==Fingerprint(model),"zero later-tuned saleprice preserves stored material/filter");
                    model.Rules.Part(PartKind.Copper).unitPrice=3;Check(model.Industry.DispatchNow(freeExport.id),"restored price resumes safe sale");break;
                case "IndustryStandingServiceNoDebtOrHistoricalCatchup":
                    model.State.money=0;model.Industry.SetEnabled(primary.id,true);model.Industry.Tick(5000);var saved=Copy(model.State);
                    model=new ScrappingModel(model.Rules,saved);primary=model.FindEquipment(primary.id);model.HasPower=id=>true;
                    Check(model.State.money==0 && primary.industry.primary==null && primary.industry.remaining==60 && !model.Industry.Tick(0),"elapsed blocked/load/pause does not manufacture purchases/progress");
                    model.State.money=24;Check(!model.Industry.Tick(5000) && model.State.money==24 && primary.industry.remaining==60,"one euro short stays debt-free");
                    model.State.money=25;model.Industry.Tick(84);Check(model.State.money==0 && primary.industry.objectsProcessed==1 && primary.industry.primary==null,"exact affordable purchase and actual processing");break;
                case "IndustrySaleAndProcessingLedger":
                    model.State.money=1000;model.Industry.SetEnabled(primary.id,true);model.Industry.Tick(252);
                    Check(primary.industry.objectsProcessed==3 && model.State.money==925 && model.State.experience==0 && model.Career.Stats.poweredBatches==3,"standing purchases plus realprocessing, no free processing XP");
                    var ledgerExport=Place(model,EquipmentKind.ExportStation,10,5);Buffer(model,ledgerExport,PartKind.Copper,12);Buffer(model,ledgerExport,PartKind.Steel,18,false);
                    var copper=model.Industry.DispatchQuote(ledgerExport.id);model.Industry.DispatchNow(ledgerExport.id);var steel=model.Industry.DispatchQuote(ledgerExport.id);model.Industry.DispatchNow(ledgerExport.id);
                    Check(model.State.money==925+copper.total+steel.total && model.State.experience==24 && ledgerExport.industry.exportedXp==24,"export cash/XP equals ordinary live quotes");
                    Check(model.Career.Stats.materialUnitsSold==30 && model.Career.Stats.salesRevenue==copper.total+steel.total && model.Career.Stats.contractBonuses==0,"sale/career ledger exact and customer requests untouched");break;
                case "IndustryManualRecoveryFundsStandingService":
                    model.State.money=0;model.Industry.SetEnabled(primary.id,true);int bench=model.State.equipment[0].id;
                    for(int cycle=0;cycle<3;cycle++)
                    {
                        Check(model.AcquireWire() && model.BeginProcessing(bench),"free manual input while standing service is unfunded");
                        while(!model.FindEquipment(bench).job.ready)Check(model.Work(bench),"manual processing");
                        Check(model.CollectOutput(bench,0) && model.Sell() && model.CollectOutput(bench,1) && model.Sell(),"real material sales fund machinery");model.Industry.Tick(100);
                    }
                    Check(model.State.money==8 && model.State.experience==24 && primary.industry.objectsProcessed==1 && model.Career.Stats.salesRevenue==33,"zero-cash manual path funds one actual charged delivery without free XP or debt");break;
                case "IndustryRejectsInvalidLiveTuning":
                    model.Rules.LargeRecipe(ScrapObjectKind.Car).yields[0].quantity=-1;string invalidTune=Fingerprint(model);
                    Check(!model.Industry.FeedScrap(primary.id,firstCar) && invalidTune==Fingerprint(model),"invalid changed yield cannot consume owned object");model.Rules.LargeRecipe(ScrapObjectKind.Car).yields[0].quantity=1;
                    model.Rules.Equipment(EquipmentKind.PrimaryScrapper).processingSeconds=0;Check(!model.Industry.FeedScrap(primary.id,firstCar) && primary.industry==null,"invalid duration cannot create paid job");model.Rules.Equipment(EquipmentKind.PrimaryScrapper).processingSeconds=24;
                    model.Rules.deliveryIntervalSeconds=float.NaN;Check(!model.Industry.SetEnabled(primary.id,true) && primary.industry==null,"invalid interval cannot persist service state");model.Rules.deliveryIntervalSeconds=60;
                    var badIntervalExport=Place(model,EquipmentKind.ExportStation,10,5);Buffer(model,badIntervalExport,PartKind.Copper,3);model.Rules.Equipment(EquipmentKind.ExportStation).processingSeconds=float.PositiveInfinity;invalidTune=Fingerprint(model);
                    Check(!model.Industry.DispatchNow(badIntervalExport.id) && invalidTune==Fingerprint(model),"invalid new dispatch interval cannot credit cash then corrupt saved timer");model.Rules.Equipment(EquipmentKind.ExportStation).processingSeconds=5;
                    model.Rules.Part(PartKind.Copper).unitPrice=-1;Check(!model.Industry.DispatchNow(badIntervalExport.id) && invalidTune==Fingerprint(model),"negative live price cannot consume stock or make cash negative");model.Rules.Part(PartKind.Copper).unitPrice=3;
                    model.Rules.Part(PartKind.Copper).saleXp=-1;Check(!model.Industry.DispatchNow(badIntervalExport.id) && invalidTune==Fingerprint(model),"negative live XP cannot consume stock or corrupt experience");model.Rules.Part(PartKind.Copper).saleXp=2;break;
                default:throw new ArgumentException(name);
            }
            ScrappingModel.Validate(model.State,model.Rules);
        }
    }
}
