using System;
namespace Scrapshift.Tests
{
    public static class ApplianceScenarios
    {
        public static readonly string[] Names={"RadioRepairResale", "RadioSalvageAndCapacity", "FaultRecipesAndFreeRepair", "FaultIdentityAcrossDropAndResume", "LegacyFanAndRadioBalanceMigration", "RadioGuardsAndOverflow", "RadioGuidanceAndTools", "ApplianceMetadataValidation"};
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void Finish(YardModel m){while(m.State.fanStage==FanStage.Repairing || m.State.fanStage==FanStage.Dismantling)Check(m.WorkFan(),"work");}
        static void Reject(YardState state){try{YardModel.Validate(state);}catch(ArgumentException){return;}throw new Exception("Invalid metadata accepted");}
        public static void Run(string name)
        {
            var m=new YardModel(new YardRules());
            switch(name)
            {
                case "RadioRepairResale":
                    m.State.money=6;Check(m.AcquireRadio()&&m.LoadFan()&&m.InspectFan(),"radio diagnosis");
                    Check(m.CurrentRepair.fault=="Failed capacitor" && m.CurrentRepair.partsPrice==6,"radio recipe");
                    Check(m.BeginFanRepair() && !m.BeginFanRepair() && m.State.money==0,"pay once");
                    Finish(m);Check(!m.CollectFan()&&m.State.radiosRepaired==0,"test required");
                    Check(m.TestFan() && !m.TestFan() && m.State.radiosRepaired==1 && m.State.fansRepaired==0,"radio counter once");
                    Check(m.CollectFan()&&!m.CollectFan()&&m.Carried.kind==MaterialKind.RestoredRadio,"one radio");
                    Check(m.Sell()&&!m.Sell()&&m.State.money==34&&m.State.incomeToday==34,"one resale");break;
                case "RadioSalvageAndCapacity":
                    m=new YardModel(new YardRules{maxBundles=1});m.AcquireRadio();m.LoadFan();m.InspectFan();
                    Check(!m.AcquireFan()&&!m.AcquireWire()&&!m.AcquireRadio(),"shared station reserves capacity");
                    Check(m.BeginFanDismantle()&&!m.BeginFanRepair(),"exclusive choice");Finish(m);
                    Check(m.State.radiosDismantled==1&&m.State.fansDismantled==0&&m.CollectFan(),"one salvage completion");
                    Check(m.Carried.kind==MaterialKind.Copper&&m.Carried.quantity==2&&m.Store(MaterialKind.Copper),"radio yield stores normally");
                    Check(!m.AcquireWire()&&m.Retrieve(MaterialKind.Copper)&&m.Sell()&&m.State.money==8,"protected output sale");
                    Check(!m.AcquireRadio()&&m.AdvanceDay()&&m.AcquireRadio(),"daily radio replenishes");break;
                case "FaultRecipesAndFreeRepair":
                    int[] costs={8,4,0};int[] steps={3,2,2};
                    for(int day=0;day<3;day++)
                    {
                        m=new YardModel(new YardRules(),new YardState{dayIndex=day,money=8});m.AcquireFan();m.LoadFan();m.InspectFan();
                        Check(m.CurrentRepair.partsPrice==costs[day]&&m.FanRepairSteps==steps[day],"fan fault changes choice");
                        Check(m.BeginFanRepair()&&m.State.money==8-costs[day],"exact parts charge");Finish(m);Check(m.TestFan()&&m.CollectFan()&&m.Sell(),"all faults complete");
                        Check(m.State.money==50-costs[day],"resale remains predictable");
                    }
                    m=new YardModel(new YardRules(),new YardState{dayIndex=2});m.AcquireRadio();m.LoadFan();m.InspectFan();
                    Check(m.CurrentRepair.fault=="Oxidized tuner contacts"&&m.BeginFanRepair()&&m.State.money==0,"free repair possible from zero");Finish(m);Check(m.TestFan()&&m.CollectFan()&&m.Sell()&&m.State.money==34,"cleaning gives useful choice");break;
                case "FaultIdentityAcrossDropAndResume":
                    m.State.dayIndex=1;m.State.money=3;m.AcquireRadio();int id=m.Carried.id;
                    Check(m.Carried.applianceFault==ApplianceFault.PowerLead&&m.Drop(7,.1f,8),"fault travels with dropped ID");m.AdvanceDay();
                    Check(m.PickUp(id)&&m.LoadFan()&&m.State.benchFault==ApplianceFault.PowerLead,"later day does not reroll fault");m.InspectFan();m.BeginFanRepair();m.WorkFan();
                    m=new YardModel(m.Rules,m.State);Check(m.State.benchAppliance==RepairAppliance.PortableRadio&&m.State.money==0&&!m.BeginFanRepair(),"paid partial radio retained");
                    Finish(m);m.TestFan();m.CollectFan();Check(m.Carried.applianceFault==ApplianceFault.PowerLead,"restored provenance");break;
                case "LegacyFanAndRadioBalanceMigration":
                    Check((int)MaterialKind.Wire==0 && (int)MaterialKind.Copper==1 && (int)MaterialKind.BrokenFan==2 && (int)MaterialKind.RestoredFan==3,"original saved enum IDs preserved");
                    m=new YardModel(new YardRules(),new YardState{fanStage=FanStage.Repairing,fanStrokes=1});
                    Check(m.CurrentRepair.fault=="Seized motor"&&m.FanRepairSteps==3,"legacy default metadata");Finish(m);m.TestFan();m.CollectFan();Check(m.Carried.kind==MaterialKind.RestoredFan,"legacy partial job completes");
                    var r=new YardRules{radioPartsPrice=0,radioSalePrice=51,radioCopperYield=0,radioRepairStrokes=0,radioDismantleStrokes=0,radioDailyLimit=0,fanPartsPrice=13};
                    Check(BalanceMigration.FillMissingExtensions(r)&&r.radioPartsPrice==6&&r.radioSalePrice==51&&r.fanPartsPrice==13,"only missing radio rules filled");r.Validate();Check(!BalanceMigration.FillMissingExtensions(r),"idempotent migration");break;
                case "RadioGuardsAndOverflow":
                    Check(!m.LoadFan()&&!m.InspectFan()&&!m.TestFan(),"empty bench guards");m.AcquireRadio();
                    Check(!m.Store(MaterialKind.BrokenRadio)&&!m.Sell()&&!m.AcquireFan(),"wrong storage/buyer/full hands retain radio");m.LoadFan();m.InspectFan();
                    Check(!m.BeginFanRepair()&&m.State.fanStage==FanStage.Diagnosed,"unaffordable retains diagnosis");m.State.money=6;m.BeginFanRepair();m.AcquireWire();Check(!m.WorkFan(),"hands block work");m.Drop(0,.1f,0);Finish(m);
                    m.State.nextId=int.MaxValue;m.TestFan();Check(!m.CollectFan()&&m.State.fanStage==FanStage.Tested,"ID exhaustion retains tested radio");m.State.nextId=3;m.CollectFan();m.State.money=int.MaxValue-33;
                    Check(!m.Sell()&&m.Carried!=null,"sale overflow retains radio");m.State.money=int.MaxValue-34;Check(m.Sell()&&m.State.money==int.MaxValue,"exact limit");break;
                case "RadioGuidanceAndTools":
                    m.State.money=100;m.AcquireRadio();
                    Check(YardNavigation.Recommend(m,0,0).landmark==YardLandmark.FanBench&&YardGuidance.Objective(m,"F","Mouse4","R").Contains("radio"),"radio route and actual binding");
                    Check(!YardGuidance.Hint(m,TargetKind.Bench,0,"F","Mouse4").canUse&&YardGuidance.Hint(m,TargetKind.Bench,0,"F","Mouse4").text.Contains("radio"),"correct wrong-station hint");m.LoadFan();m.InspectFan();m.BeginFanRepair();m.WorkFan();m.WorkFan();
                    Check(m.BuyUpgrade(YardUpgrade.HandTools)&&m.State.fanStage==FanStage.ReadyToTest&&m.State.radiosRepaired==0,"tools finish eligible work, explicit test retained");m.TestFan();
                    Check(StationProgress.Read(m,TargetKind.FanBench).label.Contains("34"),"radio progress price");m.CollectFan();
                    Check(YardGuidance.Hint(m,TargetKind.Sell,0,"F","Mouse4").text.Contains("[F] sell tested portable radio for €34"),"resale prompt");break;
                case "ApplianceMetadataValidation":
                    Reject(new YardState{benchAppliance=(RepairAppliance)9});Reject(new YardState{benchFault=(ApplianceFault)9});Reject(new YardState{radiosTakenToday=-1});Reject(new YardState{radiosRepaired=-1});Reject(new YardState{benchAppliance=RepairAppliance.PortableRadio});
                    m.AcquireWire();m.Carried.applianceFault=ApplianceFault.PowerLead;Reject(m.State);m.Carried.applianceFault=ApplianceFault.MainComponent;
                    m.Carried.kind=MaterialKind.BrokenRadio;m.Carried.quantity=2;Reject(m.State);m.Carried.quantity=1;m.Carried.applianceFault=(ApplianceFault)7;Reject(m.State);m.Carried.applianceFault=ApplianceFault.MainComponent;break;
                default:throw new Exception(name);
            }
            YardModel.Validate(m.State);
        }
    }
}
