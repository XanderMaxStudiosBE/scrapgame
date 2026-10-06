using System;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactRoutingScenarios
    {
        public static readonly string[] Names={"RoutingBasicLineAvailableImmediately","RoutingOldDefaultGatesMigrateOnce","RoutingCustomGatesAndTuningRetained","RoutingVersionedFormerGatesRetained","RoutingInvalidVersionRejected","RoutingJournalUsesActualGates","RoutingPortsRespectDirectionAndSide","RoutingRotatedPortsAndAimGuards"};
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static CompactRules Former()
        {
            var rules=new CompactRules();
            foreach(var kind in new[]{EquipmentKind.Storage,EquipmentKind.Conveyor,EquipmentKind.Splitter,EquipmentKind.Merger,EquipmentKind.Tier2Scrapper})rules.Equipment(kind).unlockLevel=10;
            return rules;
        }
        public static void Run(string name)
        {
            var rules=new CompactRules();
            switch(name)
            {
                case "RoutingBasicLineAvailableImmediately":
                    var model=new ScrappingModel(rules);model.State.money=300;
                    var build=new ConstructionModel(model.State,rules);var transport=new AutomationModel(model,build);
                    Check(build.Place(EquipmentKind.Storage,0,1,0),build.LastMessage);int source=build.LastPlacedId;
                    Check(build.Place(EquipmentKind.Tier1Scrapper,0,6,180),build.LastMessage);int destination=build.LastPlacedId;
                    Check(transport.Connect(source,0,destination,0,true),transport.LastMessage);
                    Check(model.Level==1&&model.State.experience==0&&model.State.money<300,"early construction is purchased, no XP or free hardware");
                    Check(rules.Equipment(EquipmentKind.Splitter).unlockLevel==3&&rules.Equipment(EquipmentKind.Tier2Scrapper).unlockLevel==5,"sorting and faster processing remain earned upgrades");break;
                case "RoutingOldDefaultGatesMigrateOnce":
                    rules=Former();rules.Equipment(EquipmentKind.Storage).price=93;rules.Equipment(EquipmentKind.Tier2Scrapper).processingSeconds=1.75f;
                    Check(rules.FillMissingAutomationDefaults(),"legacy defaults prepared");
                    Check(rules.routingRulesVersion==1&&rules.Equipment(EquipmentKind.Storage).unlockLevel==1&&rules.Equipment(EquipmentKind.Conveyor).unlockLevel==1,"old early line gates upgraded");
                    Check(rules.Equipment(EquipmentKind.Splitter).unlockLevel==3&&rules.Equipment(EquipmentKind.Merger).unlockLevel==3&&rules.Equipment(EquipmentKind.Tier2Scrapper).unlockLevel==5,"advanced defaults upgraded");
                    Check(rules.Equipment(EquipmentKind.Storage).price==93&&rules.Equipment(EquipmentKind.Tier2Scrapper).processingSeconds==1.75f,"valid custom statistics retained");
                    Check(!rules.FillMissingAutomationDefaults(),"migration idempotent");break;
                case "RoutingCustomGatesAndTuningRetained":
                    rules=Former();rules.Equipment(EquipmentKind.Storage).unlockLevel=7;rules.Equipment(EquipmentKind.Conveyor).name="Hand tuned belt";
                    rules.FillMissingAutomationDefaults();Check(rules.Equipment(EquipmentKind.Storage).unlockLevel==7&&rules.Equipment(EquipmentKind.Conveyor).unlockLevel==10,"edited gates and renamed catalogues retained");break;
                case "RoutingVersionedFormerGatesRetained":
                    rules=Former();rules.routingRulesVersion=1;rules.Validate();Check(rules.Equipment(EquipmentKind.Conveyor).unlockLevel==10&&!rules.FillMissingRoutingDefaults(),"explicit custom old gates authoritative");break;
                case "RoutingInvalidVersionRejected":
                    rules.routingRulesVersion=2;bool rejected=false;try{rules.Validate();}catch(ArgumentException){rejected=true;}Check(rejected,"unknown routing version surfaced");break;
                case "RoutingJournalUsesActualGates":
                    rules.routingRulesVersion=1;rules.Equipment(EquipmentKind.Storage).unlockLevel=4;
                    model=new ScrappingModel(rules);Check(model.Career.RoutingUnlockLevel==4,"journal follows edited requirements");
                    CompactCareerGoal goal=null;foreach(var candidate in model.Career.Goals)if(candidate.key=="level10")goal=candidate;
                    Check(goal!=null&&!goal.complete&&goal.detail.Contains("level 4"),"old journal key remains but description uses actual gate");
                    model.State.experience=rules.levelThresholds[3];model.Career.RefreshProgress();
                    foreach(var candidate in model.Career.Goals)if(candidate.key=="level10")Check(candidate.complete,"readiness completes at actual level");break;
                case "RoutingPortsRespectDirectionAndSide":
                    var equipment=new EquipmentState{kind=EquipmentKind.Tier1Scrapper};
                    var input=AutomationModel.Port(equipment,rules,false,0);var output=AutomationModel.Port(equipment,rules,true,0);
                    Check(output.z<0&&input.z>0,"legacy output preserved, intake opposite");
                    Check(CompactPortSelection.Find(equipment,rules,true,output.x,.7f,output.z)==0,"output mouth selected");
                    Check(CompactPortSelection.Find(equipment,rules,false,output.x,.7f,output.z)==-1,"output mouth cannot snap to distant input");
                    Check(CompactPortSelection.Find(equipment,rules,false,input.x,.7f,input.z)==0,"input mouth selected");
                    Check(CompactPortSelection.Find(equipment,rules,true,0,.7f,0)==-1,"centre of machine does not select mouth");break;
                case "RoutingRotatedPortsAndAimGuards":
                    equipment=new EquipmentState{kind=EquipmentKind.Splitter,x=5,z=-2,yaw=90};
                    for(int i=0;i<3;i++){output=AutomationModel.Port(equipment,rules,true,i);Check(CompactPortSelection.Find(equipment,rules,true,output.x,.7f,output.z)==i,"all rotated junction mouths selectable");}
                    Check(CompactPortSelection.Find(equipment,rules,true,float.NaN,.7f,0)==-1&&CompactPortSelection.Find(equipment,rules,true,0,3,0)==-1,"bad aim and machine roof rejected");break;
                default:throw new Exception("Unknown routing scenario "+name);
            }
        }
    }
}
