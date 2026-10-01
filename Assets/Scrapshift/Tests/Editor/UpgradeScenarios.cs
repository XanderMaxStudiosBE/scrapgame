using System;
namespace Scrapshift.Tests
{
    public static class UpgradeScenarios
    {
        public static readonly string[] Names={"UpgradePurchaseGuards","StorageUpgradePreservesCapacity","ToolUpgradeReducesWork","MachineTuningPreservesRunningLoad","UpgradeLimitsAndResume","InvalidUpgradeStates", "LegacyBalanceExtensions", "ToolsCompleteEligiblePartialJobs"};
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run(string name)
        {
            var m=new YardModel(new YardRules());
            switch(name)
            {
                case "UpgradePurchaseGuards":
                    Check(!m.BuyUpgrade(YardUpgrade.StorageRack),"unaffordable");m.State.money=200;
                    Check(!m.BuyUpgrade(YardUpgrade.MachineTuning)&&m.State.money==200,"machine required");
                    Check(!m.BuyUpgrade(YardUpgrade.None)&&!m.BuyUpgrade((YardUpgrade)3),"one known upgrade at a time");
                    Check(m.BuyUpgrade(YardUpgrade.StorageRack)&&!m.BuyUpgrade(YardUpgrade.StorageRack)&&m.State.money==140,"no double charge");break;
                case "StorageUpgradePreservesCapacity":
                    m=new YardModel(new YardRules{maxBundles=1});m.AcquireWire();m.Store(MaterialKind.Wire);
                    Check(!m.AcquireWire(),"initial capacity");m.State.money=60;Check(m.BuyUpgrade(YardUpgrade.StorageRack),"rack purchase");
                    Check(m.Capacity==13&&m.StoredBundles(MaterialKind.Wire)==1&&m.AcquireWire(),"capacity increases without changing existing item");break;
                case "ToolUpgradeReducesWork":
                    m.State.money=75;m.BuyUpgrade(YardUpgrade.HandTools);m.AcquireWire();m.LoadBench();
                    for(int i=0;i<3;i++)Check(m.WorkBench(),"wire step");
                    Check(m.State.benchOutput==3&&!m.WorkBench()&&m.FanRepairSteps==2&&m.FanSalvageSteps==3,"fewer steps same output");
                    Check(m.Rules.manualStrokes==4&&m.Rules.fanRepairStrokes==3,"balance data never mutated");break;
                case "MachineTuningPreservesRunningLoad":
                    m.State.machineOwned=true;m.State.money=120;m.AcquireWire();m.FeedMachine();m.Tick(1);
                    m.BuyUpgrade(YardUpgrade.MachineTuning);Check(m.State.machineRemaining==4,"existing timer remains saved/consistent");
                    m.Tick(4);m.CollectMachine();m.Sell();m.AcquireWire();m.FeedMachine();
                    Check(Math.Abs(m.State.machineRemaining-3)<.0001&&m.Rules.machineSeconds==5,"future load faster without balance mutation");break;
                case "UpgradeLimitsAndResume":
                    m=new YardModel(new YardRules{manualStrokes=1,fanRepairStrokes=1,fanDismantleStrokes=1,maxBundles=95});m.State.money=200;
                    m.BuyUpgrade(YardUpgrade.HandTools);m.BuyUpgrade(YardUpgrade.StorageRack);
                    var restored=new YardModel(m.Rules,m.State);
                    Check(restored.Capacity==100&&restored.WireWorkSteps==1&&restored.FanRepairSteps==1&&restored.Owns(YardUpgrade.StorageRack),"bounds and restored flags");break;
                case "InvalidUpgradeStates":
                    foreach(var state in new[]{new YardState{upgrades=(YardUpgrade)8},new YardState{upgrades=YardUpgrade.MachineTuning}})
                    {
                        bool rejected=false;try{YardModel.Validate(state);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid ownership accepted");
                    }break;
                case "LegacyBalanceExtensions":
                    var old = new YardRules { copperPerWire=5, manualStrokes=7, maxBundles=18,
                        fanPartsPrice=0,fanSalePrice=55,fanCopperYield=0,fanRepairStrokes=0,fanDismantleStrokes=0,fanDailyLimit=0,
                        storageUpgradePrice=0,toolsUpgradePrice=0,tuningUpgradePrice=0 };
                    Check(BalanceMigration.FillMissingExtensions(old),"legacy fields should migrate");old.Validate();
                    Check(old.copperPerWire==5&&old.manualStrokes==7&&old.maxBundles==18&&old.fanSalePrice==55,"existing tuning retained");
                    Check(old.fanPartsPrice==8&&old.storageUpgradePrice==60&&!BalanceMigration.FillMissingExtensions(old),"idempotent defaults");
                    old.toolsUpgradePrice=-1;BalanceMigration.FillMissingExtensions(old);
                    bool invalid=false;try{old.Validate();}catch(ArgumentException){invalid=true;}Check(invalid,"negative authored values still rejected");break;
                case "ToolsCompleteEligiblePartialJobs":
                    m.State.money=100;m.AcquireWire();m.LoadBench();for(int i=0;i<3;i++)m.WorkBench();
                    m.AcquireFan();m.LoadFan();m.InspectFan();m.BeginFanRepair();m.WorkFan();m.WorkFan();
                    Check(m.BuyUpgrade(YardUpgrade.HandTools),"upgrade during concurrent hand work");
                    Check(!m.State.benchLoaded&&m.State.benchOutput==3&&m.State.fanStage==FanStage.ReadyToTest,"completed thresholds apply immediately");
                    Check(m.State.fansRepaired==0&&!m.WorkBench()&&!m.WorkFan(),"testing stays explicit and outputs stay exactly once");
                    Check(m.TestFan()&&m.State.fansRepaired==1&&m.CollectFan(),"one tested fan");
                    var salvage=new YardModel(new YardRules());salvage.State.money=75;
                    salvage.AcquireFan();salvage.LoadFan();salvage.InspectFan();salvage.BeginFanDismantle();
                    for(int i=0;i<3;i++)salvage.WorkFan();salvage.BuyUpgrade(YardUpgrade.HandTools);
                    Check(salvage.State.fanStage==FanStage.CopperReady&&salvage.State.fansDismantled==1&&!salvage.WorkFan(),"salvage completion exactly once");break;
                default:throw new Exception(name);
            }
            YardModel.Validate(m.State);
        }
    }
}
