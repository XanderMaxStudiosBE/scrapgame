using System;
using System.Collections.Generic;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    // Actual construction, processing, transport and sales run together. These are pure
    // state checks; copying/reconstructing a state is deliberately not a Unity JSON test.
    public static class CompactIndustryIntegrationScenarios
    {
        public static readonly string[] Names={
            "IndustryPurchasedPortsAndRefundSafety", "IndustryScheduledProductionChain", "IndustryMixedApplianceSortingRoutes",
            "IndustryPowerOutageAndNetworkOverload", "IndustryExportBackpressureAndLineage",
            "IndustryReceivingFiltersCannotDeleteInputs", "IndustryDeliveryReservationAndCapacity",
            "IndustryTransportDoesNotInheritElapsedTime", "IndustryReconstructedProductionConservation",
            "IndustryPartitionedTimerAndOutputConservation", "IndustryExportOverflowKeepsTransitStock"
        };
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        sealed class Yard
        {
            public readonly CompactRules rules;
            public ScrappingModel model;
            public ConstructionModel construction;
            public AutomationModel automation;
            public int primary,storage,tier,splitter,export,generator;
            public CompactYardState State{get{return model.State;}}
            public Yard()
            {
                rules=new CompactRules{startingCars=0,startingRefrigerators=0};
                model=new ScrappingModel(rules);State.money=10000;State.experience=rules.levelThresholds[11];
                Bind();
            }
            void Bind()
            {
                construction=new ConstructionModel(State,rules);automation=new AutomationModel(model,construction);
                model.HasPower=id=>construction.PowerFor(id).powered;
            }
            public int Place(EquipmentKind kind,float x,float z,float yaw=0)
            {Check(construction.Place(kind,x,z,yaw),construction.LastMessage);return construction.LastPlacedId;}
            public ConveyorLink Link(int from,int output,int to,int input,bool xFirst=true)
            {
                Check(automation.Connect(from,output,to,input,xFirst),automation.LastMessage);
                foreach(var link in State.belts)if(link.id==automation.LastConnectedId)return link;
                throw new Exception("Connected conveyor missing.");
            }
            public void Primary()
            {
                primary=Place(EquipmentKind.PrimaryScrapper,-17,0);
                generator=Place(EquipmentKind.Generator,-9,0);
                Check(construction.Connect(generator,primary),construction.LastMessage);
            }
            public void Chain()
            {
                Primary();storage=Place(EquipmentKind.Storage,-17,7);tier=Place(EquipmentKind.Tier2Scrapper,-17,13);
                splitter=Place(EquipmentKind.Splitter,-11,13,90);export=Place(EquipmentKind.ExportStation,-5,13,90);
                int tierGenerator=Place(EquipmentKind.Generator,-9,7),exportGenerator=Place(EquipmentKind.Generator,-5,7);
                Check(construction.Connect(tierGenerator,tier) && construction.Connect(exportGenerator,export),"Dedicated machine power.");
                Link(primary,0,storage,0);Link(storage,0,tier,0);Link(tier,0,splitter,0);Link(splitter,1,export,0);
            }
            public void Tick(float delta)
            {model.Tick(delta);model.Industry.Tick(delta);automation.Tick(delta);}
            public void Advance(float seconds,float step=.1f)
            {for(float elapsed=0;elapsed<seconds-.00001f;elapsed+=step)Tick(Math.Min(step,seconds-elapsed));}
            public void Resume()
            {model=new ScrappingModel(rules,Copy(State));Bind();}
        }
        static CompactStack Put(Yard yard,int equipment,PartKind kind,int quantity,bool eligible=true)
        {
            var owner=yard.model.FindEquipment(equipment);
            var stack=new CompactStack{id=yard.State.nextId++,kind=kind,quantity=quantity,x=owner.x,z=owner.z,xpEligible=eligible};
            owner.contents.Add(stack);return stack;
        }
        static int Total(CompactYardState state,PartKind kind,bool eligible)
        {
            int count=0;
            foreach(var item in state.items)if(item.kind==kind && item.xpEligible==eligible)count+=item.quantity;
            foreach(var equipment in state.equipment)
            {
                foreach(var item in equipment.contents)if(item.kind==kind && item.xpEligible==eligible)count+=item.quantity;
                if(equipment.job!=null && equipment.job.xpEligible==eligible)
                    foreach(var amount in equipment.job.yields)if(amount.kind==kind)count+=amount.quantity;
                if(equipment.industry!=null && equipment.industry.primary!=null && equipment.industry.primary.xpEligible==eligible)
                    foreach(var amount in equipment.industry.primary.yields)if(amount.kind==kind)count+=amount.quantity;
            }
            foreach(var belt in state.belts)
                foreach(var item in belt.items)if(item.kind==kind && item.xpEligible==eligible)count+=item.quantity;
            return count;
        }
        static void Validate(Yard yard)
        {ScrappingModel.Validate(yard.State,yard.rules);ConstructionModel.Validate(yard.State,yard.rules);AutomationModel.Validate(yard.State,yard.rules);}
        static void StartOneScheduledCar(Yard yard)
        {
            Check(yard.model.Industry.SetEnabled(yard.primary,true),yard.model.Industry.LastMessage);
            yard.Tick(yard.rules.deliveryIntervalSeconds);
            Check(yard.model.FindEquipment(yard.primary).industry.primary!=null,"Standing order starts its paid whole-object job.");
            Check(yard.model.Industry.SetEnabled(yard.primary,false),"One purchase can be disabled without cancelling paid work.");
        }
        public static void Run(string name)
        {
            var yard=new Yard();var model=yard.model;var state=yard.State;string reason;
            switch(name)
            {
                case "IndustryPurchasedPortsAndRefundSafety":
                {
                    Check(state.equipment.Count==1 && model.Level==12,"Unlocks do not grant industrial equipment.");
                    state.experience=yard.rules.levelThresholds[10];int cash=state.money,next=state.nextId;
                    Check(!yard.construction.CanPlace(EquipmentKind.PrimaryScrapper,-17,0,0,0,out reason) && reason.Contains("12"),"Primary has a level gate.");
                    Check(!yard.construction.Place(EquipmentKind.ExportStation,-5,13,90) && state.money==cash && state.nextId==next,"Locked purchase is nonmutating.");
                    state.experience=yard.rules.levelThresholds[11];yard.Primary();yard.export=yard.Place(EquipmentKind.ExportStation,-5,13,90);
                    Check(AutomationModel.PortCount(EquipmentKind.PrimaryScrapper,true)==1 && AutomationModel.PortCount(EquipmentKind.PrimaryScrapper,false)==0 &&
                        AutomationModel.PortCount(EquipmentKind.ExportStation,true)==0 && AutomationModel.PortCount(EquipmentKind.ExportStation,false)==1,"Whole objects never become portable belt inputs.");
                    var primary=model.FindEquipment(yard.primary);var export=model.FindEquipment(yard.export);
                    Check(AutomationModel.Port(primary,yard.rules,true,0).z>primary.z && AutomationModel.Port(export,yard.rules,false,0).x<export.x,"Rotated industrial ports face outwards.");
                    Check(!yard.automation.CanConnect(yard.export,0,yard.primary,0,true,out reason),"Unsupported port pair is rejected.");
                    Check(model.Industry.SetEnabled(yard.primary,true) && !yard.construction.CanMove(yard.primary,out reason),"Enabled standing order cannot be moved or dismantled.");
                    Check(model.Industry.SetEnabled(yard.primary,false) && yard.construction.Disconnect(yard.generator,yard.primary),"Disable and unplug idle machine.");
                    Check(yard.construction.Move(yard.primary,-17,1,0),"Idle disconnected industrial equipment moves normally.");
                    cash=state.money;int refund=yard.construction.RefundFor(yard.primary);
                    Check(yard.construction.Remove(yard.primary) && state.money==cash+refund && !yard.construction.Remove(yard.primary),"Industrial refund occurs once.");
                    Check(model.Industry.SetEnabled(yard.export,true) && !yard.construction.Remove(yard.export),"Enabled export cannot be dismantled.");
                    Check(model.Industry.SetEnabled(yard.export,false) && yard.construction.Remove(yard.export),"Disabled empty export may be dismantled.");
                    break;
                }
                case "IndustryScheduledProductionChain":
                {
                    yard.Chain();int purchaseCash=state.money,xp=state.experience;
                    Check(model.Industry.SetEnabled(yard.export,true),"Enable automatic dispatch explicitly.");StartOneScheduledCar(yard);
                    Check(state.money==purchaseCash-yard.rules.LargeRecipe(ScrapObjectKind.Car).purchasePrice && state.experience==xp,"Intake purchases once and grants no XP.");
                    yard.Advance(240);
                    var intake=model.FindEquipment(yard.primary).industry;var dispatch=model.FindEquipment(yard.export).industry;
                    Check(intake.primary==null && intake.objectsProcessed==1 && dispatch.exportedUnits==32 && dispatch.exportedXp==42,"Car recovery reaches storage, Tier2, sorting and export once.");
                    Check(state.experience==xp+42 && state.money==purchaseCash-25+dispatch.exportedRevenue,"Only recovered final material sales change XP/cash.");
                    Check(model.Career.Stats.materialUnitsSold==32 && model.Career.Stats.salesRevenue==dispatch.exportedRevenue && model.Career.Stats.contractsCompleted==0,"Automatic sales count toward career without customer bonuses.");
                    foreach(PartKind part in Enum.GetValues(typeof(PartKind)))Check(Total(state,part,true)==0 && Total(state,part,false)==0,"Production chain drains its component/material ledger.");
                    Check(model.OccupiedSlots==0 && !model.Industry.DispatchNow(yard.export),"Empty export cannot repeat a sale.");
                    int stableCash=state.money,stableXP=state.experience;yard.Advance(60);
                    Check(state.money==stableCash && state.experience==stableXP && intake.objectsProcessed==1,"Disabled intake does not make another purchase.");
                    break;
                }
                case "IndustryPowerOutageAndNetworkOverload":
                {
                    yard.Primary();StartOneScheduledCar(yard);yard.Tick(3);
                    var job=model.FindEquipment(yard.primary).industry.primary;float remaining=job.remaining;int paid=state.money;
                    Check(yard.construction.Disconnect(yard.generator,yard.primary),"Disconnect a live primary.");yard.Tick(100);
                    Check(job.remaining==remaining && model.FindEquipment(yard.primary).industry.objectsProcessed==0 && state.money==paid,"Power outage retains paid object and partial progress.");
                    Check(!yard.construction.Move(yard.primary,-17,1,0) && !yard.construction.Remove(yard.primary),"Unplugged whole-object job remains busy.");
                    Check(yard.construction.Connect(yard.generator,yard.primary),"Reconnect primary.");yard.export=yard.Place(EquipmentKind.ExportStation,-9,7);
                    Check(yard.construction.Connect(yard.generator,yard.export) && yard.construction.PowerFor(yard.primary).overloaded,"A shared 6 kW generator cannot supply 8 kW.");
                    yard.Tick(100);Check(job.remaining==remaining,"Overload freezes all attached processing.");
                    int second=yard.Place(EquipmentKind.Generator,-5,7);Check(yard.construction.Connect(second,yard.export),"Additional purchased supply.");
                    Check(yard.construction.PowerFor(yard.primary).powered,"12 kW network resumes.");yard.Tick(remaining);
                    Check(model.FindEquipment(yard.primary).industry.primary==null && model.FindEquipment(yard.primary).industry.objectsProcessed==1 && Total(state,PartKind.Wire,true)==2,"Resume completes the exact reserved car output once.");
                    break;
                }
                case "IndustryMixedApplianceSortingRoutes":
                {
                    yard.Primary();yard.storage=yard.Place(EquipmentKind.Storage,-17,7);
                    yard.splitter=yard.Place(EquipmentKind.Splitter,-17,11);yard.tier=yard.Place(EquipmentKind.Tier2Scrapper,-17,15);
                    yard.export=yard.Place(EquipmentKind.ExportStation,-10,11,90);int finishedExport=yard.Place(EquipmentKind.ExportStation,-10,15,90);
                    int tierGen=yard.Place(EquipmentKind.Generator,-21,13),directGen=yard.Place(EquipmentKind.Generator,-5,11),finishedGen=yard.Place(EquipmentKind.Generator,-5,15);
                    Check(yard.construction.Connect(tierGen,yard.tier) && yard.construction.Connect(directGen,yard.export) && yard.construction.Connect(finishedGen,finishedExport),"Purchased power for each active production branch.");
                    yard.Link(yard.primary,0,yard.storage,0);yard.Link(yard.storage,0,yard.splitter,0);
                    yard.Link(yard.splitter,1,yard.tier,0);yard.Link(yard.splitter,2,yard.export,0);yard.Link(yard.tier,0,finishedExport,0);
                    int cash=state.money,xp=state.experience;
                    Check(model.Industry.SetPurchaseKind(yard.primary,ScrapObjectKind.Refrigerator) && model.Industry.SetEnabled(yard.primary,true),"Explicit refrigerator standing order.");
                    Check(model.Industry.SetEnabled(yard.export,true) && model.Industry.SetEnabled(finishedExport,true),"Both material branches dispatch explicitly.");
                    yard.Tick(yard.rules.deliveryIntervalSeconds);Check(model.Industry.SetEnabled(yard.primary,false),"Stop later paid purchases.");yard.Advance(200);
                    var direct=model.FindEquipment(yard.export).industry;var finished=model.FindEquipment(finishedExport).industry;
                    Check(direct.exportedUnits==2 && direct.exportedXp==2 && finished.exportedUnits==16 && finished.exportedXp==22,"Sorting sends already recovered plastic to export and components through Tier2.");
                    Check(state.experience==xp+24 && state.money==cash-15+direct.exportedRevenue+finished.exportedRevenue,"Appliance sale ledger includes only recovered materials and one purchase.");
                    Check(model.OccupiedSlots==0 && model.FindEquipment(yard.primary).industry.objectsProcessed==1 && model.Career.Stats.materialUnitsSold==18,"Mixed-output connected sorting cannot leave unprocessable plastic trapped in Tier2.");
                    break;
                }
                case "IndustryExportBackpressureAndLineage":
                {
                    yard.storage=yard.Place(EquipmentKind.Storage,0,0);yard.export=yard.Place(EquipmentKind.ExportStation,0,6);
                    int generator=yard.Place(EquipmentKind.Generator,5,6);Check(yard.construction.Connect(generator,yard.export),"Export power.");
                    var link=yard.Link(yard.storage,0,yard.export,0);yard.rules.Equipment(EquipmentKind.ExportStation).outputCapacity=1;
                    Put(yard,yard.export,PartKind.Steel,1,false);var fresh=Put(yard,yard.storage,PartKind.Copper,3,true);Put(yard,yard.storage,PartKind.Copper,2,false);
                    int xp=state.experience,cash=state.money;yard.Advance(12);
                    Check(link.items.Count>0 && link.items[0].progress==1 && Total(state,PartKind.Copper,true)==3 && Total(state,PartKind.Copper,false)==2,"Disabled full export preserves eligible and imported transit.");
                    Check(state.money==cash && state.experience==xp,"Transport grants no sales reward.");
                    Check(model.Industry.SetEnabled(yard.export,true),"Explicit dispatch enabled.");yard.Advance(60);
                    var dispatch=model.FindEquipment(yard.export).industry;
                    Check(dispatch.exportedUnits==6 && dispatch.exportedXp==6 && state.experience==xp+6,"Imported material sells for cash without XP after backpressure clears.");
                    Check(Total(state,PartKind.Copper,true)==0 && Total(state,PartKind.Copper,false)==0 && link.items.Count==0,"Blocked cargo resumes without loss or duplication.");
                    Check(model.FindItem(fresh.id)==null && model.OccupiedSlots==0,"Sold source identities cannot be withdrawn later.");
                    break;
                }
                case "IndustryReceivingFiltersCannotDeleteInputs":
                {
                    yard.storage=yard.Place(EquipmentKind.Storage,0,0);yard.export=yard.Place(EquipmentKind.ExportStation,0,6);
                    Check(model.SetFilter(yard.storage,(int)PartKind.Wire) && !yard.automation.CanConnect(yard.storage,0,yard.export,0,true,out reason),"Component-filtered source cannot target material-only export.");
                    Check(model.SetFilter(yard.storage,-1),"Unfiltered storage connection.");var link=yard.Link(yard.storage,0,yard.export,0);
                    var wire=Put(yard,yard.storage,PartKind.Wire,2);var copper=Put(yard,yard.storage,PartKind.Copper,1);yard.Advance(10);
                    Check(Total(state,PartKind.Wire,true)==2 && model.StoredUnits(yard.storage)==2 && model.StoredUnits(yard.export)==1 && link.items.Count==0,"Unsupported components remain upstream while supported material transfers.");
                    Check(model.FindEquipment(yard.storage).contents[0].id==wire.id && model.FindEquipment(yard.export).contents[0].id==copper.id,"Whole-unit transport preserves original IDs.");
                    Check(model.SetFilter(yard.export,(int)PartKind.Steel),"Material-only receiving filter.");Put(yard,yard.storage,PartKind.Copper,1);yard.Advance(10);
                    Check(Total(state,PartKind.Copper,true)==2 && model.StoredUnits(yard.storage)==3,"Destination filter retains unsupported stock instead of discarding it.");
                    Check(!model.SetFilter(yard.export,(int)PartKind.Motor) && model.FindEquipment(yard.export).filterKind==(int)PartKind.Steel,"Component cannot be configured as a saleable filter.");
                    break;
                }
                case "IndustryDeliveryReservationAndCapacity":
                {
                    yard.Primary();yard.rules.maxStacks=2;int cash=state.money,next=state.nextId;
                    Check(model.Industry.SetEnabled(yard.primary,true),"Enable standing order with tight inventory.");yard.Tick(yard.rules.deliveryIntervalSeconds);
                    Check(model.FindEquipment(yard.primary).industry.primary==null && state.money==cash && state.nextId==next,"All distinct output slots are reserved before a paid purchase.");
                    yard.rules.maxStacks=256;yard.Tick(yard.rules.deliveryIntervalSeconds);var job=model.FindEquipment(yard.primary).industry.primary;
                    Check(job!=null && model.OccupiedSlots==3 && model.Industry.SetEnabled(yard.primary,false),"Paid primary reservation fits once space returns.");
                    yard.Tick(2);float remaining=job.remaining;yard.rules.Equipment(EquipmentKind.PrimaryScrapper).outputCapacity=5;
                    yard.Tick(100);Check(job.remaining==remaining && model.FindEquipment(yard.primary).contents.Count==0,"Reduced capacity preserves running output reservation.");
                    yard.rules.Equipment(EquipmentKind.PrimaryScrapper).outputCapacity=48;yard.Tick(remaining);
                    Check(model.FindEquipment(yard.primary).industry.primary==null && model.StoredUnits(yard.primary)==6 && model.OccupiedSlots==3,"Completion replaces reservation with its exact buffers.");
                    break;
                }
                case "IndustryTransportDoesNotInheritElapsedTime":
                {
                    yard.Primary();yard.storage=yard.Place(EquipmentKind.Storage,-17,7);var first=yard.Link(yard.primary,0,yard.storage,0);
                    yard.export=yard.Place(EquipmentKind.ExportStation,-17,13);var second=yard.Link(yard.storage,0,yard.export,0);
                    Put(yard,yard.primary,PartKind.Copper,1);int xp=state.experience;
                    yard.automation.Tick(100);Check(first.items.Count==1 && first.items[0].progress==0 && second.items.Count==0,"New primary launch starts at zero after movement phase.");
                    yard.automation.Tick(100);Check(first.items.Count==0 && second.items.Count==1 && second.items[0].progress==0,"A newly received relay cannot inherit earlier elapsed time.");
                    yard.automation.Tick(100);Check(second.items.Count==0 && model.StoredUnits(yard.export)==1 && state.experience==xp,"Transfer reaches export once without XP.");
                    int builds=yard.automation.LayoutBuildCount;yard.automation.Tick(.1f);Check(yard.automation.LayoutBuildCount==builds,"Industrial stock changes preserve cached port/path layout.");
                    break;
                }
                case "IndustryReconstructedProductionConservation":
                {
                    yard.Chain();Check(model.Industry.SetEnabled(yard.export,true),"Enable export.");StartOneScheduledCar(yard);
                    yard.Tick(7.25f);var before=model.FindEquipment(yard.primary).industry.primary;int objectId=before.id;float remaining=before.remaining;
                    yard.Resume();model=yard.model;state=yard.State;
                    Check(model.FindEquipment(yard.primary).industry.primary.id==objectId && model.FindEquipment(yard.primary).industry.primary.remaining==remaining,"Object identity and partial primary timer survive reconstructed state.");
                    int xp=state.experience,cash=state.money;var random=new Random(4205);
                    for(int i=0;i<2400;i++)
                    {
                        if(i%73==0){yard.Resume();model=yard.model;state=yard.State;}
                        yard.Tick(i%41==0?0:(float)(.05+random.NextDouble()*.15));
                        int credited=model.FindEquipment(yard.export).industry.exportedXp;
                        Check(state.experience==xp+credited,"Only completed export sales award XP across reconstructions.");
                        Check(model.FindEquipment(yard.primary).industry.objectsProcessed<=1,"No reconstituted whole object completes twice.");
                        if(i%97==0)Validate(yard);
                    }
                    var dispatch=model.FindEquipment(yard.export).industry;
                    Check(dispatch.exportedUnits==32 && dispatch.exportedXp==42 && state.money==cash+dispatch.exportedRevenue,"Seeded resumed chain conserves car output and exactly credited revenue.");
                    Check(model.OccupiedSlots==0 && model.FindEquipment(yard.primary).industry.objectsProcessed==1,"Resumed completed chain leaves no duplicated reservations.");
                    break;
                }
                case "IndustryPartitionedTimerAndOutputConservation":
                {
                    yard.Primary();StartOneScheduledCar(yard);var other=new Yard();other.Primary();StartOneScheduledCar(other);
                    var primary=model.FindEquipment(yard.primary).industry.primary;float duration=primary.duration;
                    yard.Tick(duration);other.Advance(duration,.125f);
                    Check(model.FindEquipment(yard.primary).industry.objectsProcessed==1 && other.model.FindEquipment(other.primary).industry.objectsProcessed==1,"Partitioned unpaused processing completes once.");
                    foreach(PartKind part in Enum.GetValues(typeof(PartKind)))Check(Total(state,part,true)==Total(other.State,part,true),"Primary snapshot yields do not depend on elapsed partitioning.");
                    Check(state.money==other.State.money && state.experience==other.State.experience && state.nextId==other.State.nextId,"Timer partitioning cannot create fees, sale XP or extra output IDs.");
                    Validate(other);break;
                }
                case "IndustryExportOverflowKeepsTransitStock":
                {
                    yard.storage=yard.Place(EquipmentKind.Storage,0,0);yard.export=yard.Place(EquipmentKind.ExportStation,0,6);
                    int gen=yard.Place(EquipmentKind.Generator,5,6);Check(yard.construction.Connect(gen,yard.export),"Export power.");
                    var link=yard.Link(yard.storage,0,yard.export,0);Put(yard,yard.storage,PartKind.Copper,2);yard.Advance(10);
                    int next=state.nextId,xp=state.experience;state.money=int.MaxValue;
                    Check(!model.Industry.DispatchNow(yard.export) && model.StoredUnits(yard.export)==2 && state.nextId==next && state.experience==xp,"Cash overflow rejects before consuming transferred stock.");
                    state.money=100;state.experience=int.MaxValue;
                    Check(!model.Industry.DispatchNow(yard.export) && model.StoredUnits(yard.export)==2 && link.items.Count==0,"XP overflow also retains material for a later valid sale.");
                    state.experience=xp;Check(model.Industry.DispatchNow(yard.export) && model.StoredUnits(yard.export)==0 && state.experience==xp+4,"Restored valid quote sells the retained batch once.");
                    int credited=state.money;Check(!model.Industry.DispatchNow(yard.export) && state.money==credited,"Repeated sale has no stock/reward.");
                    break;
                }
                default:throw new ArgumentException(name);
            }
            Validate(yard);
        }
        static PartAmount[] Amounts(PartAmount[] source)
        {if(source==null)return null;var result=new PartAmount[source.Length];for(int i=0;i<result.Length;i++)result[i]=new PartAmount(source[i].kind,source[i].quantity);return result;}
        public static CompactYardState Copy(CompactYardState source)
        {
            var copy=CompactScrappingScenarios.Copy(source);
            for(int i=0;i<source.equipment.Count;i++)
            {
                var original=source.equipment[i].industry;if(original==null)continue;
                var restored=new IndustrialMachineState{version=original.version,enabled=original.enabled,purchaseKind=original.purchaseKind,remaining=original.remaining,
                    objectsProcessed=original.objectsProcessed,exportedUnits=original.exportedUnits,exportedRevenue=original.exportedRevenue,exportedXp=original.exportedXp};
                if(original.primary!=null)
                {
                    var job=original.primary;restored.primary=new PrimaryScrapJob{id=job.id,kind=job.kind,duration=job.duration,remaining=job.remaining,xpEligible=job.xpEligible,yields=Amounts(job.yields)};
                }
                copy.equipment[i].industry=restored;
            }
            if(source.career!=null)
            {
                var career=source.career;copy.career=new CompactCareerState{version=career.version,completedGoals=career.completedGoals,
                    inspectedObjects=career.inspectedObjects,dismantledObjects=career.dismantledObjects,manualBatches=career.manualBatches,poweredBatches=career.poweredBatches,
                    materialUnitsSold=career.materialUnitsSold,saleTransactions=career.saleTransactions,salesRevenue=career.salesRevenue,contractsCompleted=career.contractsCompleted,
                    contractBonuses=career.contractBonuses,completionAcknowledged=career.completionAcknowledged};
                if(career.contract!=null)
                {
                    var request=career.contract;copy.career.contract=new CompactContractProgress{id=request.id,index=request.index,name=request.name,customer=request.customer,
                        kind=request.kind,required=request.required,delivered=request.delivered,bonus=request.bonus,minimumLevel=request.minimumLevel};
                }
            }
            return copy;
        }
    }
}
