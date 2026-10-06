using System;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    /// <summary>Progression migration and a transaction-only route through all purchased stages.</summary>
    public static class CompactPacingScenarios
    {
        public static readonly int[] PreviousCurve={0,30,80,160,280,440,650,930,1290,1750,2320,3010};
        public static readonly int[] PacedCurve={0,20,45,80,125,185,265,365,490,640,820,1040};
        public static readonly string[] Names={"PacingFreshRulesAndEarlierTransport","PacingLegacyDefaultMigratesOnce",
            "PacingEveryCustomThresholdRetained","PacingExplicitPreviousCurveRetained","PacingInvalidTuningStillRejected",
            "PacingEarnedSaveDoesNotReceiveAwards","PacingFreshTransactionsThroughIndustrialOwnership",
            "PacingCarsAndFridgesCanFundEveryStage","PacingOrdinarySalesNeedNoCustomerBonuses"};
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void Same(int[] expected,int[] actual,string message)
        {
            Check(actual!=null && expected.Length==actual.Length,message);
            for(int i=0;i<expected.Length;i++)Check(expected[i]==actual[i],message+" at level "+(i+1));
        }
        static void Reject(CompactRules rules)
        {
            bool failed=false;try{rules.Validate();}catch(ArgumentException){failed=true;}
            Check(failed,"invalid tuning must remain visible, not silently repaired");
        }
        static CompactRules PreviousRules(){return new CompactRules{levelThresholds=(int[])PreviousCurve.Clone(),progressionRulesVersion=1};}
        public static void Run(string name)
        {
            switch(name)
            {
                case "PacingFreshRulesAndEarlierTransport":
                    var fresh=new CompactRules();fresh.Validate();Same(PacedCurve,fresh.levelThresholds,"fresh paced curve");
                    Check(fresh.progressionRulesVersion==1 && fresh.startingMoney==8,"no starting cash grant");
                    Check(fresh.LevelForExperience(639)==9 && fresh.LevelForExperience(640)==10 && fresh.LevelForExperience(1039)==11 && fresh.LevelForExperience(1040)==12,"threshold boundaries");
                    Check(fresh.Equipment(EquipmentKind.Tier2Scrapper).unlockLevel==5 && fresh.Equipment(EquipmentKind.PrimaryScrapper).unlockLevel==12 && fresh.Equipment(EquipmentKind.ExportStation).unlockLevel==12,"faster processing upgrades at five; industrial equipment still earned at twelve");
                    Check(fresh.Part(PartKind.Copper).saleXp==2 && fresh.Part(PartKind.Steel).saleXp==1 && fresh.Part(PartKind.Plastic).saleXp==1,"material sale XP unchanged");break;
                case "PacingLegacyDefaultMigratesOnce":
                    var legacy=PreviousRules();legacy.progressionRulesVersion=0;int[] former=legacy.levelThresholds;
                    Check(legacy.FillMissingProgressionDefaults(),"missing version upgraded");Same(PacedCurve,legacy.levelThresholds,"exact old default upgraded");
                    Same(PreviousCurve,former,"previous array is not mutated through other references");
                    Check(legacy.progressionRulesVersion==1 && !legacy.FillMissingProgressionDefaults(),"upgrade is idempotent");
                    legacy.Validate();Check(!legacy.FillMissingAutomationDefaults(),"normal prepared-rules entry remains stable");break;
                case "PacingEveryCustomThresholdRetained":
                    // An edit at any positive threshold, including an otherwise exact former curve, is authoritative.
                    for(int index=1;index<PreviousCurve.Length;index++)
                    {
                        var custom=PreviousRules();custom.progressionRulesVersion=0;custom.levelThresholds[index]++;
                        int[] customArray=custom.levelThresholds;int[] expected=(int[])customArray.Clone();custom.Validate();
                        Same(expected,custom.levelThresholds,"custom legacy curve retained");
                        Check(ReferenceEquals(customArray,custom.levelThresholds) && custom.progressionRulesVersion==1,"custom curve preserved and stamped");
                    }
                    var pacedCustom=new CompactRules();pacedCustom.levelThresholds[9]=641;pacedCustom.FillMissingAutomationDefaults();
                    Check(pacedCustom.levelThresholds[9]==641,"custom current threshold retained");
                    var longer=new CompactRules{levelThresholds=new int[13],saleBonusPercents=new int[13]};
                    Array.Copy(PreviousCurve,longer.levelThresholds,12);longer.levelThresholds[12]=4000;
                    Array.Copy(new CompactRules().saleBonusPercents,longer.saleBonusPercents,12);longer.saleBonusPercents[12]=60;
                    int[] longerArray=longer.levelThresholds;longer.Validate();
                    Check(longer.MaxLevel==13 && longer.levelThresholds[9]==1750 && longer.levelThresholds[12]==4000 && ReferenceEquals(longerArray,longer.levelThresholds),"longer custom positive curve retained");break;
                case "PacingExplicitPreviousCurveRetained":
                    var explicitOld=PreviousRules();explicitOld.Validate();Same(PreviousCurve,explicitOld.levelThresholds,"version-one explicit former curve retained");
                    Check(!explicitOld.FillMissingProgressionDefaults() && explicitOld.LevelForExperience(640)==6,"explicit old pacing remains selectable");break;
                case "PacingInvalidTuningStillRejected":
                    var missing=new CompactRules{levelThresholds=null};Reject(missing);
                    var negative=new CompactRules();negative.levelThresholds[1]=-1;Reject(negative);
                    var duplicate=new CompactRules();duplicate.levelThresholds[2]=duplicate.levelThresholds[1];Reject(duplicate);
                    var truncated=new CompactRules{levelThresholds=new[]{0,20,45}};Reject(truncated);
                    var unknown=new CompactRules{progressionRulesVersion=2};Reject(unknown);
                    var oldUnknown=new CompactRules{progressionRulesVersion=-1};Reject(oldUnknown);break;
                case "PacingEarnedSaveDoesNotReceiveAwards":
                    var earned=new Journey(PreviousRules(),true);earned.Opening();
                    var snapshot=CompactIndustryScenarios.Copy(earned.Model.State);snapshot.version=3;
                    int money=snapshot.money,xp=snapshot.experience,nextId=snapshot.nextId,goals=snapshot.career.completedGoals;
                    int sales=snapshot.career.saleTransactions,bonuses=snapshot.career.contractBonuses,revenue=snapshot.career.salesRevenue,delivered=snapshot.career.contract.delivered;
                    var upgradedRules=PreviousRules();upgradedRules.progressionRulesVersion=0;
                    var resumed=new ScrappingModel(upgradedRules,snapshot);
                    Check(earned.Model.Level==2 && resumed.Level==3,"same earned XP uses the prepared curve");
                    Check(resumed.State.money==money && resumed.State.experience==xp && resumed.State.nextId==nextId && resumed.State.carriedId==0,"migration changes no money, XP or identities");
                    Check(resumed.Career.Stats.completedGoals==goals && resumed.Career.Stats.saleTransactions==sales && resumed.Career.Stats.contractBonuses==bonuses && resumed.Career.Stats.salesRevenue==revenue,"earned journal and sales totals retained");
                    Check(resumed.Career.CurrentContract.id==3 && resumed.Career.CurrentContract.delivered==delivered,"actual partial request retained without free delivery");break;
                case "PacingFreshTransactionsThroughIndustrialOwnership":
                    var wire=new Journey(new CompactRules(),true);wire.Opening();
                    Check(wire.Model.State.experience==66 && wire.Model.State.money==107 && wire.Actions==81 && wire.ManualStrokes==34 && wire.DismantleStrokes==10,"legitimate manual opening funds powered equipment");
                    wire.StageB();Check(wire.Model.State.money==2 && wire.EquipmentSpend==105,"generator and Tier1 are paid, with no gift");
                    wire.ReachLevelTen("wire");Check(wire.Model.State.experience==646 && wire.Model.State.money==887 && wire.FreeWire==53 && wire.Actions==522,"first automation unlock requires actual sales");
                    wire.StageC();Check(wire.Model.State.money==494 && wire.EquipmentSpend==498,"working source, Tier2, output, generator and both belts purchased");
                    wire.FinishWithFactory();Check(wire.Model.State.experience==1046 && wire.Model.State.money==1325 && wire.Actions==642 && wire.FreeWire==103,"actual factory batches earn industrial level");
                    Check(Math.Abs(wire.MachineSeconds-532.9)<.001 && wire.Model.Career.Stats.contractsCompleted==6 && wire.Model.Career.Stats.contractBonuses==64,"finite requests give cash only and machine time is simulated");
                    wire.StageD();Check(wire.Model.State.money==730 && wire.EquipmentSpend==1093 && wire.Actions==647,"paid powered industrial ownership remains affordable");
                    Check(wire.Model.Career.Completed && wire.Model.Career.CurrentContract==null && wire.Model.Career.CurrentGoal==null,"real transactions complete the chapter and leave free play");
                    wire.ValidateLedger();break;
                case "PacingCarsAndFridgesCanFundEveryStage":
                    foreach(string preference in new[]{"cars","fridges"})
                    {
                        var mixed=new Journey(new CompactRules(),true);mixed.Opening();mixed.StageB();mixed.ReachLevelTen(preference);mixed.StageC();mixed.FinishWithFactory();mixed.StageD();
                        Check(mixed.Model.Level==12 && mixed.Model.Career.Completed && mixed.Model.State.money>=400,"both paid whole-object preferences fund every stage without loans");
                        Check(mixed.EquipmentSpend==1093 && mixed.ScrapSpend>0,"all hardware and replacement objects paid");mixed.ValidateLedger();
                    }
                    break;
                case "PacingOrdinarySalesNeedNoCustomerBonuses":
                    var ordinary=new Journey(new CompactRules(),false);ordinary.Opening();
                    Check(ordinary.Model.State.experience==66 && ordinary.Model.State.money==93 && ordinary.Model.Career.Stats.contractBonuses==0,"normal sales keep unchanged starting transactions");
                    ordinary.Wire();ordinary.StageB();Check(ordinary.Model.State.money==0 && ordinary.Model.State.experience==74,"one additional manual wire funds power without requests");
                    ordinary.Wire();Check(ordinary.Model.State.money>0 && ordinary.Model.State.experience==82 && ordinary.Model.Career.Stats.poweredBatches==1,"zero cash powered player can recover through renewable wiring");
                    ordinary.ValidateLedger();break;
                default:throw new Exception("Unknown pacing scenario "+name);
            }
        }

        sealed class Journey
        {
            public readonly ScrappingModel Model;
            readonly ConstructionModel construction;
            readonly bool deliverRequests;
            AutomationModel automation;
            int processor,generator,source,tierTwo,output;
            public int Actions,ManualStrokes,DismantleStrokes,FreeWire,EquipmentSpend,ScrapSpend;
            int earnedXp,saleRevenue,requestBonuses;
            public double MachineSeconds;
            public Journey(CompactRules rules,bool requests)
            {
                Model=new ScrappingModel(rules);construction=new ConstructionModel(Model.State,rules);deliverRequests=requests;
                Model.HasPower=id=>construction.PowerFor(id).powered;processor=Model.State.equipment[0].id;
                Check(Model.State.money==8 && Model.State.experience==0,"fresh journey begins with the real defaults");
            }
            void Action(bool success,string purpose){Check(success,purpose+": "+Model.LastNotice+" / "+construction.LastMessage);Actions++;}
            void Sale()
            {
                while(Model.Carried!=null)
                {
                    int beforeCash=Model.State.money,beforeXp=Model.State.experience;
                    var contract=Model.Career.ContractQuote();int cash,xp,bonus;
                    if(deliverRequests && contract.allowed)
                    {cash=contract.total;xp=contract.experience;bonus=contract.completionBonus;Action(Model.Career.DeliverContract(),"customer delivery");}
                    else
                    {var quote=Model.SaleQuote();cash=quote.total;xp=quote.experience;bonus=0;Action(Model.Sell(),"ordinary sale");}
                    Check(Model.State.money-beforeCash==cash && Model.State.experience-beforeXp==xp,"only quoted normal sale XP and cash are credited");
                    earnedXp+=xp;saleRevenue+=cash-bonus;requestBonuses+=bonus;
                }
            }
            void Process()
            {
                int beforeXp=Model.State.experience;Action(Model.BeginProcessing(processor),"load component");var equipment=Model.FindEquipment(processor);
                if(equipment.kind==EquipmentKind.Workbench)
                    while(!equipment.job.ready){Action(Model.Work(processor),"manual processing");ManualStrokes++;}
                else
                {
                    float time=equipment.job.remaining;MachineSeconds+=time;
                    Check(Model.Tick(time) && equipment.job.ready,"connected paid machine finishes the loaded component");
                }
                Check(Model.State.experience==beforeXp,"processing itself grants no XP");var job=equipment.job;
                for(int i=0;i<job.yields.Length;i++){Action(Model.CollectOutput(processor,i),"collect recovered material");Sale();}
            }
            public void Wire(){Action(Model.AcquireWire(),"renewable wire");FreeWire++;Process();}
            void Scrap(ScrapObjectKind kind)
            {
                LargeScrapJob whole=null;foreach(var candidate in Model.State.scrap)if(candidate.kind==kind){whole=candidate;break;}
                if(whole==null)
                {
                    int cash=Model.State.money;Action(Model.BuyScrap(kind),"pay for replacement scrap");ScrapSpend+=cash-Model.State.money;
                    foreach(var candidate in Model.State.scrap)if(candidate.kind==kind)whole=candidate;
                }
                Action(Model.InspectScrap(whole.id),"inspect object");
                while(whole.strokes<whole.requiredStrokes){Action(Model.WorkScrap(whole.id),"dismantle object");DismantleStrokes++;}
                for(int i=0;i<whole.remaining.Length;i++)
                {Action(Model.CollectScrap(whole.id,i),"collect component");if(Model.Rules.Part(Model.Carried.kind).isMaterial)Sale();else Process();}
            }
            public void Opening(){Scrap(ScrapObjectKind.Car);Scrap(ScrapObjectKind.Refrigerator);}
            int Place(EquipmentKind kind,float x,float z)
            {int before=Model.State.money;Action(construction.Place(kind,x,z,0),"buy and place "+kind);EquipmentSpend+=before-Model.State.money;return construction.LastPlacedId;}
            public void StageB()
            {generator=Place(EquipmentKind.Generator,5,5);processor=Place(EquipmentKind.Tier1Scrapper,0,5);Action(construction.Connect(generator,processor),"connect Tier1 power");Model.Career.RefreshProgress();}
            public void ReachLevelTen(string preference)
            {
                int guard=0;
                while(Model.Level<10)
                {
                    Check(guard++<500,"reachable level-ten route");var request=Model.Career.CurrentContract;
                    if(deliverRequests && request!=null && request.minimumLevel<=Model.Level && (request.kind==PartKind.Plastic || request.kind==PartKind.Steel))
                    {
                        var kind=request.kind==PartKind.Plastic?ScrapObjectKind.Refrigerator:ScrapObjectKind.Car;
                        if(Model.State.money<Model.Rules.LargeRecipe(kind).purchasePrice)Wire();else Scrap(kind);
                    }
                    else if(preference=="cars" && Model.State.money>=Model.Rules.LargeRecipe(ScrapObjectKind.Car).purchasePrice)Scrap(ScrapObjectKind.Car);
                    else if(preference=="fridges" && Model.State.money>=Model.Rules.LargeRecipe(ScrapObjectKind.Refrigerator).purchasePrice)Scrap(ScrapObjectKind.Refrigerator);
                    else Wire();
                }
            }
            public void StageC()
            {
                source=Place(EquipmentKind.Storage,20,-1.5f);tierTwo=Place(EquipmentKind.Tier2Scrapper,20,5);output=Place(EquipmentKind.Storage,20,11);
                int power=Place(EquipmentKind.Generator,13,5);Action(construction.Connect(power,tierTwo),"connect Tier2 power");automation=new AutomationModel(Model,construction);
                int before=Model.State.money;Action(automation.Connect(source,0,tierTwo,0,true),"purchase feed conveyor");Action(automation.Connect(tierTwo,0,output,0,true),"purchase output conveyor");
                EquipmentSpend+=before-Model.State.money;Model.Career.RefreshProgress();
            }
            void FactoryBatch(int count)
            {
                for(int i=0;i<count;i++){Action(Model.AcquireWire(),"factory wire");FreeWire++;Action(Model.Deposit(source),"fill source storage");}
                int ticks=0,xp=Model.State.experience;
                while(Model.StoredUnits(output)!=count*5 || Model.StoredUnits(source)!=0 || Model.FindEquipment(tierTwo).job!=null || Model.State.belts[0].items.Count!=0 || Model.State.belts[1].items.Count!=0)
                {Model.Tick(.1f);automation.Tick(.1f);MachineSeconds+=.1;Check(++ticks<50000,"factory route drains its paid conveyors");}
                Check(Model.State.experience==xp,"conveyors and machine completion grant no free XP");
                while(Model.FindEquipment(output).contents.Count>0)
                {Action(Model.WithdrawBatch(output,Model.FindEquipment(output).contents[0].id),"withdraw material batch");Sale();}
            }
            public void FinishWithFactory()
            {
                int batches=0;
                while(Model.Level<12 || Model.Career.CurrentContract!=null)
                {
                    int units=(Model.Rules.levelThresholds[11]-Model.State.experience+7)/8;
                    FactoryBatch(Math.Max(1,Math.Min(24,units)));Check(++batches<100,"finite request book and industrial unlock reachable");
                }
            }
            public void StageD()
            {
                int primary=Place(EquipmentKind.PrimaryScrapper,-7,6),exporter=Place(EquipmentKind.ExportStation,8,10),power=Place(EquipmentKind.Generator,-4,0);
                Action(construction.Connect(power,primary),"connect primary power");Action(construction.Connect(generator,exporter),"use spare generator output for export");
                Check(construction.PowerFor(primary).powered && construction.PowerFor(exporter).powered,"industrial machines have sufficient purchased power");
                Check(Model.FindEquipment(primary).industry==null && Model.FindEquipment(exporter).industry==null,"buying machinery starts no standing delivery or automatic sale");
            }
            public void ValidateLedger()
            {
                Check(Model.State.money==8+saleRevenue+requestBonuses-EquipmentSpend-ScrapSpend,"cash conserved through actual purchases and quoted sales");
                Check(Model.State.experience==earnedXp && Model.Career.Stats.salesRevenue==saleRevenue && Model.Career.Stats.contractBonuses==requestBonuses,"no XP gifts, duplicate customer bonus or invented historical revenue");
                ScrappingModel.Validate(Model.State,Model.Rules);ConstructionModel.Validate(Model.State,Model.Rules);AutomationModel.Validate(Model.State,Model.Rules);
            }
        }
    }
}
