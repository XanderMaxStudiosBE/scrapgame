using System;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactStorageScenarios
    {
        public static readonly string[] Names={"StorageDepositWithdrawIdentity","StorageCapacityAtomic","StorageNoXPLineage","StorageFiltersPersist",
            "StorageTier2SingleBatch","StorageTier2InputRecipeFilter","StorageTier2SameLineageBatch","StorageTier2PauseOutputBackpressure",
            "StorageTier2SnapshotsResume","StorageTier2ReserveSlotLimit","StorageTier2OutputReservation","StorageTransitIdentityValidation","StorageVersion2Compatibility",
            "StorageLevel10NoFreeEquipment","StorageInvalidBeltTuning","StorageTier2NoIdPreservation","StorageDurableContentsLowerCapacity",
            "StorageLegacyBalanceMigration","StorageMixedCustomBalanceMigration","StorageNegativeBalanceMigration",
            "StorageRawWireCapacityRecovery","StorageBatchWithdrawalLineage","StorageBatchWithdrawalAtomic","StorageBatchWithdrawalResume"};
        static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
        static void Invalid(Action action,string reason){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,reason);}
        static EquipmentState Add(ScrappingModel m,EquipmentKind kind,float x=6,float z=6)
        {var e=new EquipmentState{id=m.State.nextId++,kind=kind,x=x,z=z,paidPrice=m.Rules.Equipment(kind).price};m.State.equipment.Add(e);return e;}
        static CompactStack Held(ScrappingModel m,PartKind kind,int quantity,bool eligible=true)
        {var s=new CompactStack{id=m.State.nextId++,kind=kind,quantity=quantity,xpEligible=eligible};m.State.items.Add(s);m.State.carriedId=s.id;return s;}
        public static void Run(string name)
        {
            var rules=new CompactRules();var m=new ScrappingModel(rules);var storage=Add(m,EquipmentKind.Storage);var tier=Add(m,EquipmentKind.Tier2Scrapper,0,7);
            switch(name)
            {
                case "StorageDepositWithdrawIdentity":
                    var original=Held(m,PartKind.Motor,2);int id=original.id,next=m.State.nextId,slots=m.OccupiedSlots;
                    Check(m.Deposit(storage.id) && m.State.carriedId==0 && m.State.items.Count==0 && m.StoredUnits(storage.id)==2,"whole stack deposit");
                    Check(ReferenceEquals(storage.contents[0],original) && storage.contents[0].id==id && m.State.nextId==next && m.OccupiedSlots==slots,"identity and accounting preserved");
                    Check(m.Withdraw(storage.id,id) && ReferenceEquals(m.Carried,original) && m.Carried.quantity==2 && m.StoredUnits(storage.id)==0,"whole stack withdrawal");
                    Check(!m.Withdraw(storage.id,id) && m.Carried.id==id,"no repeated withdrawal");break;
                case "StorageCapacityAtomic":
                    rules.Equipment(EquipmentKind.Storage).outputCapacity=2;var first=Held(m,PartKind.Copper,2);m.Deposit(storage.id);var second=Held(m,PartKind.Steel,1);
                    int beforeId=m.State.nextId;
                    Check(!m.Deposit(storage.id) && m.Carried.id==second.id && m.StoredUnits(storage.id)==2 && storage.contents[0].id==first.id && m.State.nextId==beforeId,"full buffer rejects without consuming or splitting");
                    Check(!m.Withdraw(storage.id,first.id) && m.Carried.id==second.id,"full hands cannot withdraw");
                    Check(!m.Deposit(m.State.equipment[0].id) && m.Carried.id==second.id,"bench not generic storage");break;
                case "StorageNoXPLineage":
                    var legacy=Held(m,PartKind.Copper,3,false);m.Deposit(storage.id);m.Withdraw(storage.id,legacy.id);
                    Check(m.State.experience==0 && !m.Carried.xpEligible && m.SaleQuote().experience==0 && m.Sell(),"legacy lineage survives storage/sale");
                    var recovered=Held(m,PartKind.Copper,3);m.Deposit(storage.id);m.Withdraw(storage.id,recovered.id);
                    Check(m.State.experience==0 && m.Sell() && m.State.experience==6,"only valid final sale rewards recovery");break;
                case "StorageFiltersPersist":
                    Check(m.SetFilter(storage.id,(int)PartKind.Copper),"set storage outgoing filter");Held(m,PartKind.Steel,4);Check(m.Deposit(storage.id),"outgoing filter does not discard another material");
                    storage.routeCursor=2;m=new ScrappingModel(rules,CompactScrappingScenarios.Copy(m.State));storage=m.FindEquipment(storage.id);
                    Check(storage.filterKind==(int)PartKind.Copper && storage.routeCursor==2 && m.StoredUnits(storage.id)==4,"filters/cursor/contents retained");
                    Check(!m.SetFilter(storage.id,99) && storage.filterKind==(int)PartKind.Copper && m.StoredUnits(storage.id)==4,"invalid filter nonmutating");
                    Check(m.SetFilter(storage.id,-1) && m.StoredUnits(storage.id)==4,"filter cancellation retains contents");break;
                case "StorageTier2SingleBatch":
                    var many=Held(m,PartKind.Wire,3);int wireId=many.id;Check(m.Deposit(tier.id) && m.AutoBegin(tier.id),"reserve buffered batch");
                    Check(tier.job.inputQuantity==1 && tier.contents.Count==1 && tier.contents[0].id==wireId && tier.contents[0].quantity==2,"one input unit consumed, remainder identity retained");
                    Check(tier.job.duration==2 && tier.job.yields[0].quantity==3 && tier.job.yields[1].quantity==2 && m.State.experience==0,"faster recipe snapshot, no free outputs/XP");
                    Check(!m.AutoBegin(tier.id) && tier.contents[0].quantity==2,"in-flight job cannot consume twice");
                    Check(!m.Tick(100) && tier.job.remaining==2,"automatic reservation does not imply free generator power");break;
                case "StorageTier2InputRecipeFilter":
                    Check(m.SetFilter(tier.id,(int)PartKind.Motor),"choose supported recipe");var wrong=Held(m,PartKind.Wire,1);
                    Check(!m.Deposit(tier.id) && m.Carried.id==wrong.id && tier.contents.Count==0,"wrong recipe input remains carried");m.Drop(2,.25f,2);
                    var motor=Held(m,PartKind.Motor,1);Check(m.Deposit(tier.id) && m.AutoBegin(tier.id) && tier.job.input==motor.kind,"filtered component intake");
                    Check(!m.SetFilter(tier.id,(int)PartKind.Copper) && tier.filterKind==(int)PartKind.Motor,"material cannot be configured as unsupported processing recipe");break;
                case "StorageTier2SameLineageBatch":
                    rules.Recipe(PartKind.Wire).inputQuantity=2;
                    var old=Held(m,PartKind.Wire,1,false);m.Deposit(tier.id);var fresh=Held(m,PartKind.Wire,1,true);m.Deposit(tier.id);
                    Check(!m.AutoBegin(tier.id) && tier.contents.Count==2 && m.StoredUnits(tier.id)==2,"cannot combine old and recovered units into XP-eligible batch");
                    Held(m,PartKind.Wire,1,true);m.Deposit(tier.id);Check(m.AutoBegin(tier.id) && tier.job.xpEligible && tier.job.inputQuantity==2,"aggregate compatible source-qualified stacks");
                    Check(tier.contents.Count==1 && tier.contents[0].id==old.id && !tier.contents[0].xpEligible && tier.contents[0].quantity==1,"unrelated lineage untouched");
                    m.HasPower=e=>true;m.Tick(2);m.CollectOutput(tier.id,0);Check(m.Carried.xpEligible && m.SaleQuote().experience==6,"eligibility follows consumed batch only");
                    Check(fresh.id!=old.id,"setup has distinct source IDs");break;
                case "StorageTier2PauseOutputBackpressure":
                    Held(m,PartKind.Motor,1);m.Deposit(tier.id);m.AutoBegin(tier.id);float remaining=tier.job.remaining;
                    Check(!m.Tick(1) && tier.job.remaining==remaining,"unpowered retains progress");bool power=true;m.HasPower=e=>power;m.Tick(.5f);power=false;
                    Check(!m.Tick(30) && tier.job.remaining==remaining-.5f,"outage pauses partial job");power=true;rules.Equipment(EquipmentKind.Tier2Scrapper).outputCapacity=9;
                    Check(!m.Tick(30) && tier.job.remaining==remaining-.5f && m.ProcessingBlockReason(tier.id).Contains("Output blocked"),"blocked output pauses without loss");
                    rules.Equipment(EquipmentKind.Tier2Scrapper).outputCapacity=48;m.Tick(30);Check(tier.job.ready && tier.job.yields[0].quantity==4,"resume produces once");break;
                case "StorageTier2SnapshotsResume":
                    Held(m,PartKind.Wire,2,false);m.Deposit(tier.id);m.AutoBegin(tier.id);m.HasPower=e=>true;m.Tick(.75f);
                    int tierId=tier.id;var altered=new CompactRules();altered.Recipe(PartKind.Wire).yields[0].quantity=9;altered.Equipment(EquipmentKind.Tier2Scrapper).processingSeconds=8;
                    m=new ScrappingModel(altered,CompactScrappingScenarios.Copy(m.State));tier=m.FindEquipment(tierId);m.HasPower=e=>true;
                    Check(tier.job.duration==2 && tier.job.remaining==1.25f && m.StoredUnits(tier.id)==1,"timer/reservedinput/remainder persist through tuning");
                    m.Tick(1.25f);m.CollectOutput(tier.id,0);Check(m.Carried.quantity==3 && !m.Carried.xpEligible && m.SaleQuote().experience==0,"snapshot yield and lineage retained");break;
                case "StorageTier2ReserveSlotLimit":
                    rules.maxStacks=1;Held(m,PartKind.Wire,1);m.Deposit(tier.id);int originalId=tier.contents[0].id;
                    Check(!m.AutoBegin(tier.id) && tier.job==null && tier.contents[0].id==originalId && tier.contents[0].quantity==1,"reserve every output slot before consuming input");
                    rules.maxStacks=2;Check(m.AutoBegin(tier.id) && m.OccupiedSlots==2,"exact output reservations fit");break;
                case "StorageTier2OutputReservation":
                    var bulk=Held(m,PartKind.Motor,39);m.Deposit(tier.id);Check(m.AutoBegin(tier.id),"batch reserves output capacity before processing");
                    Check(tier.contents[0].id==bulk.id && m.StoredUnits(tier.id)==38 && m.OutputQuantity(tier.id)==10,"buffer remainder plus exact yields fill the combined capacity");
                    var extra=Held(m,PartKind.Wire,1);Check(!m.Deposit(tier.id) && m.Carried.id==extra.id && extra.quantity==1 && m.StoredUnits(tier.id)==38,"incoming input cannot occupy reserved output capacity");break;
                case "StorageTransitIdentityValidation":
                    var link=new ConveyorLink{id=m.State.nextId++,fromId=storage.id,toId=tier.id,paidPrice=24};
                    var flight=new ConveyorItem{id=m.State.nextId++,kind=PartKind.Wire,progress=.4f};link.items.Add(flight);m.State.belts.Add(link);
                    Check(m.OccupiedSlots==1,"transport occupies inventory safety slot");ScrappingModel.Validate(m.State,rules);
                    var copy=CompactScrappingScenarios.Copy(m.State);Check(copy.belts[0].items[0].progress==.4f,"transport progress deep-copied");
                    copy.belts[0].items[0].id=storage.id;Invalid(()=>ScrappingModel.Validate(copy,rules),"transport/equipment duplicate identity rejected");
                    copy=CompactScrappingScenarios.Copy(m.State);copy.belts[0].id=tier.id;Invalid(()=>ScrappingModel.Validate(copy,rules),"belt/equipment duplicate identity rejected");
                    copy=CompactScrappingScenarios.Copy(m.State);copy.belts[0].items[0].quantity=2;Invalid(()=>ScrappingModel.Validate(copy,rules),"transport units must be single records");
                    copy=CompactScrappingScenarios.Copy(m.State);copy.belts[0].items[0].progress=float.NaN;Invalid(()=>ScrappingModel.Validate(copy,rules),"nonfinite progress rejected");break;
                case "StorageVersion2Compatibility":
                    var previous=new ScrappingModel(rules).State;previous.version=2;previous.belts=null;previous.equipment[0].filterKind=0;
                    ScrappingModel.Validate(previous,rules);Check(new ScrappingModel(rules,previous).OccupiedSlots==0,"old missing transport/filter fields accepted before explicit main migration");
                    previous.equipment.Add(new EquipmentState{id=previous.nextId++,kind=EquipmentKind.Storage,x=5,z=5});
                    Invalid(()=>ScrappingModel.Validate(previous,rules),"v2 cannot contain new storage");
                    previous.equipment.RemoveAt(previous.equipment.Count-1);previous.belts=new System.Collections.Generic.List<ConveyorLink>{new ConveyorLink{id=previous.nextId++,fromId=1,toId=2}};
                    Invalid(()=>ScrappingModel.Validate(previous,rules),"v2 cannot contain transport");break;
                case "StorageLevel10NoFreeEquipment":
                    var freshYard=new ScrappingModel(rules);int owned=freshYard.State.equipment.Count;freshYard.State.experience=rules.levelThresholds[9];
                    foreach(var kind in new[]{EquipmentKind.Storage,EquipmentKind.Tier2Scrapper,EquipmentKind.Conveyor,EquipmentKind.Splitter,EquipmentKind.Merger})
                        Check(rules.Equipment(kind).available && rules.Equipment(kind).unlockLevel==10 && rules.Equipment(kind).price>0,"purchased Stage C catalogue");
                    Check(freshYard.Level==10 && freshYard.State.equipment.Count==owned && rules.Equipment(EquipmentKind.ExportStation).available && rules.Equipment(EquipmentKind.ExportStation).unlockLevel==12,"levels do not spawn free gear/export");break;
                case "StorageInvalidBeltTuning":
                    var bad=new CompactRules{beltSpeed=0};Invalid(()=>bad.Validate(),"zero speed");bad=new CompactRules{beltSpacing=float.NaN};Invalid(()=>bad.Validate(),"nonfinite spacing");
                    bad=new CompactRules{beltCapacity=33};Invalid(()=>bad.Validate(),"bounded representation budget");bad=new CompactRules{beltMaxLength=49};Invalid(()=>bad.Validate(),"bounded reachable links");
                    bad=new CompactRules{maxBelts=0};Invalid(()=>bad.Validate(),"bounded number of links");break;
                case "StorageTier2NoIdPreservation":
                    Held(m,PartKind.Motor,1);m.Deposit(tier.id);int sourceId=tier.contents[0].id;m.State.nextId=int.MaxValue;
                    Check(!m.AutoBegin(tier.id) && tier.job==null && tier.contents[0].id==sourceId && tier.contents[0].quantity==1,"no IDs cannot consume reserved components");break;
                case "StorageDurableContentsLowerCapacity":
                    Held(m,PartKind.Copper,5);m.Deposit(storage.id);int storedId=storage.contents[0].id;
                    var small=new CompactRules();small.Equipment(EquipmentKind.Storage).outputCapacity=1;
                    m=new ScrappingModel(small,CompactScrappingScenarios.Copy(m.State));Check(m.StoredUnits(storage.id)==5 && m.Withdraw(storage.id,storedId) && m.Carried.quantity==5,"existing contents remain withdrawable after tuning capacity down");break;
                case "StorageLegacyBalanceMigration":
                    var legacyRules=new CompactRules{beltSpeed=0,beltSpacing=0,beltMaxLength=0,beltCapacity=0,maxBelts=0};
                    foreach(var kind in new[]{EquipmentKind.Storage,EquipmentKind.Tier2Scrapper,EquipmentKind.Conveyor,EquipmentKind.Splitter,EquipmentKind.Merger})
                    {var d=legacyRules.Equipment(kind);d.name+=" — later stage";d.available=false;}
                    legacyRules.Equipment(EquipmentKind.Tier2Scrapper).price=237;legacyRules.Equipment(EquipmentKind.Tier2Scrapper).unlockLevel=11;
                    Check(legacyRules.FillMissingAutomationDefaults(),"missing legacy extensions filled");legacyRules.Validate();
                    Check(legacyRules.beltSpeed==1.2f && legacyRules.beltSpacing==.65f && legacyRules.beltMaxLength==12 && legacyRules.beltCapacity==8 && legacyRules.maxBelts==64,"new defaults restored");
                    Check(legacyRules.Equipment(EquipmentKind.Tier2Scrapper).available && legacyRules.Equipment(EquipmentKind.Tier2Scrapper).price==237 && legacyRules.Equipment(EquipmentKind.Tier2Scrapper).unlockLevel==11,"enable only known historical catalogue placeholder, preserving tuning");
                    Check(!legacyRules.FillMissingAutomationDefaults(),"migration idempotent");break;
                case "StorageMixedCustomBalanceMigration":
                    var custom=new CompactRules{beltSpeed=2.5f,beltSpacing=0,beltCapacity=17,beltMaxLength=20,maxBelts=25};
                    custom.Equipment(EquipmentKind.Storage).name="Owner's locked container";custom.Equipment(EquipmentKind.Storage).available=false;custom.Equipment(EquipmentKind.Storage).price=91;
                    custom.Recipe(PartKind.Motor).yields[0].quantity=7;custom.saleBonusPercents[1]=13;
                    Check(custom.FillMissingAutomationDefaults(),"single missing field migrated");custom.Validate();
                    Check(custom.beltSpeed==2.5f && custom.beltCapacity==17 && custom.beltMaxLength==20 && custom.maxBelts==25 && custom.beltSpacing==.65f,"all positive belt tuning retained");
                    Check(!custom.Equipment(EquipmentKind.Storage).available && custom.Equipment(EquipmentKind.Storage).price==91 && custom.Equipment(EquipmentKind.Storage).name=="Owner's locked container" && custom.Recipe(PartKind.Motor).yields[0].quantity==7 && custom.saleBonusPercents[1]==13,"custom catalogue/recipe/curve retained");break;
                case "StorageNegativeBalanceMigration":
                    var negative=new CompactRules{beltSpeed=-1,beltSpacing=0};Check(negative.FillMissingAutomationDefaults() && negative.beltSpeed==-1 && negative.beltSpacing==.65f,"negative author value never silently replaced");
                    Invalid(()=>negative.Validate(),"negative tuning still reported after migration");break;
                case "StorageRawWireCapacityRecovery":
                    var tightRules=new CompactRules{maxStacks=3};var tight=new ScrappingModel(tightRules);int firstWire=0;
                    for(int i=0;i<2;i++){Check(tight.AcquireWire(),"renewable input fits with production headroom");if(i==0)firstWire=tight.State.carriedId;tight.Drop(i,.25f,0);}
                    Check(!tight.AcquireWire() && tight.OccupiedSlots==2 && tight.State.carriedId==0,"cannot fill the last output-expansion slot with raw input");
                    int benchId=tight.State.equipment[0].id;Check(tight.Pickup(firstWire) && tight.BeginProcessing(benchId),"full raw supply still has a route to saleable materials");
                    for(int i=0;i<4;i++)tight.Work(benchId);Check(tight.CollectOutput(benchId,0) && tight.Sell() && tight.CollectOutput(benchId,1) && tight.Sell(),"one production cycle frees capacity");
                    Check(tight.State.money==19 && tight.State.experience==8 && tight.AcquireWire(),"player recovers funds and can collect more input");
                    ScrappingModel.Validate(tight.State,tightRules);break;
                case "StorageBatchWithdrawalLineage":
                    var selectedBatch=new CompactStack{id=m.State.nextId++,kind=PartKind.Copper,quantity=3,xpEligible=true};storage.contents.Add(selectedBatch);
                    var otherRecovered=new CompactStack{id=m.State.nextId++,kind=PartKind.Copper,quantity=2,xpEligible=true};storage.contents.Add(otherRecovered);
                    var unearnedBatch=new CompactStack{id=m.State.nextId++,kind=PartKind.Copper,quantity=4,xpEligible=false};storage.contents.Add(unearnedBatch);
                    var otherMaterial=new CompactStack{id=m.State.nextId++,kind=PartKind.Steel,quantity=1,xpEligible=true};storage.contents.Add(otherMaterial);
                    int nextBeforeBatch=m.State.nextId,cashBeforeBatch=m.State.money;
                    Check(m.WithdrawBatch(storage.id,selectedBatch.id) && ReferenceEquals(m.Carried,selectedBatch) && m.Carried.quantity==5,"matching recovered material bundled under selected ID");
                    Check(storage.contents.Count==2 && storage.contents[0].id==unearnedBatch.id && storage.contents[1].id==otherMaterial.id && m.StoredUnits(storage.id)==5,"different lineage/material remain intact");
                    Check(m.State.nextId==nextBeforeBatch && m.State.experience==0 && m.State.money==cashBeforeBatch && m.SaleQuote().experience==10,"bundling retires IDs without reward or minted quantity");
                    Check(m.Sell() && m.State.experience==10 && m.WithdrawBatch(storage.id,unearnedBatch.id) && !m.Carried.xpEligible && m.SaleQuote().experience==0,"eligible and imported materials sell separately");break;
                case "StorageBatchWithdrawalAtomic":
                    var guarded=new CompactStack{id=m.State.nextId++,kind=PartKind.Copper,quantity=3,xpEligible=true};storage.contents.Add(guarded);
                    var heldGuard=Held(m,PartKind.Wire,1);Check(!m.WithdrawBatch(storage.id,guarded.id) && m.Carried.id==heldGuard.id && guarded.quantity==3 && storage.contents.Count==1,"full hands leave all sources untouched");
                    m.Drop(0,.25f,0);Check(!m.WithdrawBatch(storage.id,999999) && storage.contents.Count==1 && guarded.quantity==3,"missing selected ID cannot collect a batch");
                    var tooMany=new CompactStack{id=m.State.nextId++,kind=PartKind.Copper,quantity=4094,xpEligible=true};storage.contents.Add(tooMany);int beforeOverflow=m.State.nextId;
                    Check(!m.WithdrawBatch(storage.id,guarded.id) && guarded.quantity==3 && tooMany.quantity==4094 && storage.contents.Count==2 && m.State.carriedId==0 && m.State.nextId==beforeOverflow,"sum overflow checks precede every mutation");
                    storage.contents.Remove(tooMany);break;
                case "StorageBatchWithdrawalResume":
                    int selectedId=0;for(int i=0;i<25;i++){var unit=new CompactStack{id=m.State.nextId++,kind=PartKind.Copper,quantity=1,xpEligible=true};storage.contents.Add(unit);if(i==0)selectedId=unit.id;}
                    Check(m.WithdrawBatch(storage.id,selectedId) && m.Carried.quantity==25 && storage.contents.Count==0,"conveyor unit stacks become one explicit carried batch");
                    int batchNext=m.State.nextId;m=new ScrappingModel(rules,CompactScrappingScenarios.Copy(m.State));
                    Check(m.Carried.id==selectedId && m.Carried.quantity==25 && m.State.nextId==batchNext && m.SaleQuote().experience==50,"batch identity, quantity and lineage survive copied-state resume");
                    Check(!m.WithdrawBatch(storage.id,selectedId) && m.Sell() && m.State.experience==50 && !m.Sell() && !m.WithdrawBatch(storage.id,selectedId),"retired sources and completed batch sale cannot repeat");break;
                default:throw new Exception(name);
            }
            ScrappingModel.Validate(m.State,m.Rules);
        }
    }
}
