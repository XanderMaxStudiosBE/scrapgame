using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
namespace Scrapshift.Tests
{
    public static class IntegrationScenarios
    {
        public static readonly string[] Names={"MixedYardConservation", "BusinessProgressionFromZero", "ReducedCapacityPreservesReservedOutputs", "RestorationRequestsFromZero"};
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run(string name)
        {
            if(name=="MixedYardConservation")Fuzz();
            else if(name=="BusinessProgressionFromZero")Progression();
            else if(name=="ReducedCapacityPreservesReservedOutputs")ReducedCapacity();
            else if(name=="RestorationRequestsFromZero")RestorationProgression();
            else throw new Exception(name);
        }
        static void ReducedCapacity()
        {
            var m=new YardModel(new YardRules{maxBundles=4});m.State.machineOwned=true;
            m.AcquireWire();m.LoadBench();m.AcquireWire();m.FeedMachine();
            m.AcquireFan();m.LoadFan();m.InspectFan();m.BeginFanDismantle();for(int i=0;i<4;i++)m.WorkFan();
            m.AcquireWire();m.Drop(0,.1f,0);for(int i=0;i<4;i++)m.WorkBench();m.Tick(5);
            Check(m.OccupiedBundles==4,"saved station reservations and loose wire");
            m=new YardModel(new YardRules{maxBundles=1},Copy(m.State));
            Check(!m.AcquireWire()&&!m.AcquireFan(),"lower capacity blocks fresh material");
            Check(YardGuidance.Hint(m,TargetKind.Bench,0,"F","Right mouse").canUse,"collection hint matches reserved output");
            Check(m.CollectBench()&&m.Sell(),"reserved bench output survives tuning");
            Check(m.CollectMachine()&&m.Sell(),"reserved machine output survives tuning");
            Check(YardGuidance.Hint(m,TargetKind.FanBench,0,"F","Right mouse").canUse,"fan collection hint");
            Check(m.CollectFan()&&m.Sell()&&m.State.money==36,"reserved fan material and money intact");
            Check(m.OccupiedBundles==1&&!m.AcquireWire(),"one old wire remains");
            m.PickUp(m.State.items[0].id);m.LoadBench();for(int i=0;i<4;i++)m.WorkBench();
            Check(m.CollectBench()&&m.Sell()&&m.AcquireWire(),"normal acquisition resumes after reducing old stock");
            YardModel.Validate(m.State);
        }
        static long Material(YardModel m)
        {
            long total=(m.State.benchLoaded?m.Rules.copperPerWire:0)+m.State.benchOutput+m.State.machineOutput+m.State.machinePendingYield;
            if(m.State.fanStage!=FanStage.Empty)total+=m.State.benchAppliance==RepairAppliance.PortableRadio?m.Rules.radioCopperYield:m.Rules.fanCopperYield;
            foreach(var item in m.State.items)
                total+=item.kind==MaterialKind.Copper?item.quantity:item.kind==MaterialKind.Wire?m.Rules.copperPerWire:(item.kind==MaterialKind.BrokenRadio || item.kind==MaterialKind.RestoredRadio?m.Rules.radioCopperYield:m.Rules.fanCopperYield);
            return total;
        }
        static string Fingerprint(YardState state)
        {
            var text=new StringBuilder();
            foreach(var field in typeof(YardState).GetFields())if(field.Name!="items")text.Append(field.Name).Append('=').Append(field.GetValue(state)).Append(';');
            foreach(var item in state.items)foreach(var field in typeof(ScrapItem).GetFields())text.Append(field.GetValue(item)).Append(';');
            return text.ToString();
        }
        static YardState Copy(YardState state)
        {
            var copy=new YardState();
            foreach(var field in typeof(YardState).GetFields())if(field.Name!="items")field.SetValue(copy,field.GetValue(state));
            copy.items=new List<ScrapItem>();
            foreach(var item in state.items)
            {
                var next=new ScrapItem();foreach(var field in typeof(ScrapItem).GetFields())field.SetValue(next,field.GetValue(item));copy.items.Add(next);
            }
            return copy;
        }
        static void Fuzz()
        {
            var random=new Random(7138);
            var m=new YardModel(new YardRules{copperPerWire=5,fanCopperYield=4,maxBundles=9,manualStrokes=2,fanRepairStrokes=2,fanDismantleStrokes=2});
            long acquired=0,consumed=0,expectedMoney=0;
            for(int i=0;i<20000;i++)
            {
                string before=Fingerprint(m.State);bool success=false;int action=random.Next(28);
                ScrapItem held=m.Carried;int money=m.State.money;
                switch(action)
                {
                    case 0:success=m.AcquireWire();if(success)acquired+=m.Rules.copperPerWire;break;
                    case 1:success=m.AcquireFan();if(success)acquired+=m.Rules.fanCopperYield;break;
                    case 2:success=m.LoadBench();break;
                    case 3:success=m.WorkBench();break;
                    case 4:success=m.CollectBench();break;
                    case 5:success=m.FeedMachine();break;
                    case 6:success=m.CollectMachine();break;
                    case 7:success=m.Sell();if(success){bool radio=held.kind==MaterialKind.RestoredRadio;consumed+=held.kind==MaterialKind.Copper?held.quantity:radio?m.Rules.radioCopperYield:m.Rules.fanCopperYield;expectedMoney+=held.kind==MaterialKind.Copper?held.quantity*m.Rules.copperUnitPrice:radio?m.Rules.radioSalePrice:m.Rules.fanSalePrice;}break;
                    case 8:success=m.Drop(random.Next(-40,40),.1f,random.Next(-30,30));break;
                    case 9:success=m.PickUp(m.State.items.Count==0?0:m.State.items[random.Next(m.State.items.Count)].id);break;
                    case 10:success=m.Store(MaterialKind.Wire);break;
                    case 11:success=m.Store(MaterialKind.Copper);break;
                    case 12:success=m.Retrieve(MaterialKind.Wire);break;
                    case 13:success=m.Retrieve(MaterialKind.Copper);break;
                    case 14:success=m.AcceptOrder();break;
                    case 15:
                        int count=held==null?0:held.quantity,reward=m.CurrentOrder.reward;
                        success=m.DeliverOrder();
                        if(success){consumed+=count-(m.Carried==null?0:m.Carried.quantity);if(!m.State.orderAccepted)expectedMoney+=reward;}break;
                    case 16:success=m.BuyMachine();if(success)expectedMoney-=m.Rules.machinePrice;break;
                    case 17:
                        var upgrade=(YardUpgrade)(1<<random.Next(3));success=m.BuyUpgrade(upgrade);if(success)expectedMoney-=m.UpgradePrice(upgrade);break;
                    case 18:success=m.LoadFan();break;
                    case 19:success=m.InspectFan();break;
                    case 20:
                        int parts=m.State.benchAppliance==RepairAppliance.PortableRadio?m.Rules.radioPartsPrice:m.Rules.fanPartsPrice;
                        if(m.State.benchFault==ApplianceFault.PowerLead)parts=Math.Max(1,parts/2);
                        else if(m.State.benchFault==ApplianceFault.DirtyMechanism)parts=0;
                        success=m.BeginFanRepair();if(success)expectedMoney-=parts;break;
                    case 21:success=m.BeginFanDismantle();break;
                    case 22:success=m.WorkFan();break;
                    case 23:success=m.TestFan();break;
                    case 24:success=m.CollectFan();break;
                    case 25:success=m.AcquireRadio();if(success)acquired+=m.Rules.radioCopperYield;break;
                    case 26:success=m.AcceptCommission();break;
                    case 27:
                        int agreed=m.State.commissionReward;
                        success=m.DeliverCommission();
                        if(success){consumed+=held.kind==MaterialKind.RestoredRadio?m.Rules.radioCopperYield:m.Rules.fanCopperYield;if(!m.State.commissionAccepted)expectedMoney+=agreed;}break;
                }
                Check(success || before==Fingerprint(m.State),"Rejected action mutated state at step "+i+" action "+action);
                if(i%7==0)m.Tick(.5f);if(i%59==0)m.AdvanceDay();
                if(i%83==0)m=new YardModel(m.Rules,Copy(m.State)); // Model reconstruction; actual JSON is tested in Unity.
                YardModel.Validate(m.State);
                Check(m.State.money==expectedMoney,"Money ledger mismatch at "+i+" before €"+money);
                Check(Material(m)+consumed==acquired,"Material lost/duplicated at "+i);
                Check(m.OccupiedBundles<=m.Capacity,"Capacity exceeded at "+i);
            }
            Check(acquired>50 && consumed>20,"Mixed operations must make progress");
        }
        static void Progression()
        {
            var m=new YardModel(new YardRules());
            for(int day=0;day<12;day++)
            {
                // A new yard earns its first machine entirely through hand work.
                for(int wire=0;wire<3;wire++)
                {
                    Check(m.AcquireWire(),"renewable wire");
                    if(m.State.machineOwned){Check(m.FeedMachine(),"feed");m.Tick(10);Check(m.CollectMachine(),"machine output");}
                    else{Check(m.LoadBench(),"bench");while(m.State.benchLoaded)Check(m.WorkBench(),"hand stroke");Check(m.CollectBench(),"bench output");}
                    if(!m.State.orderAccepted)m.AcceptOrder();
                    Check(m.DeliverOrder(),"customer delivery");if(m.Carried!=null)Check(m.Sell(),"surplus sale");
                    if(!m.State.machineOwned && m.State.money>=m.Rules.machinePrice)Check(m.BuyMachine(),"first machine");
                }
                for(int fan=0;fan<2;fan++)
                {
                    Check(m.AcquireFan()&&m.LoadFan()&&m.InspectFan(),"daily fan delivery");
                    if(m.State.money>=m.CurrentRepair.partsPrice)
                    {Check(m.BeginFanRepair(),"motor purchase");while(m.State.fanStage==FanStage.Repairing)Check(m.WorkFan(),"repair stroke");Check(m.TestFan(),"explicit test");}
                    else{Check(m.BeginFanDismantle(),"affordable salvage");while(m.State.fanStage==FanStage.Dismantling)Check(m.WorkFan(),"salvage stroke");}
                    Check(m.CollectFan()&&m.Sell(),"fan recovery/resale");
                }
                foreach(var upgrade in new[]{YardUpgrade.StorageRack,YardUpgrade.HandTools,YardUpgrade.MachineTuning})if(m.CanBuyUpgrade(upgrade))Check(m.BuyUpgrade(upgrade),"investment");
                Check(m.AdvanceDay(),"next day");YardModel.Validate(m.State);
            }
            Check(m.State.machineOwned&&m.Owns(YardUpgrade.StorageRack|YardUpgrade.HandTools|YardUpgrade.MachineTuning),"all upgrades earned from zero");
            Check(m.State.orderIndex>5&&m.State.fansRepaired>10&&m.State.dayIndex==12,"repeat business progression");
            Check(m.State.money>0&&m.State.items.Count==0,"clean retained economy");
        }
        static void RestorationProgression()
        {
            var m=new YardModel(new YardRules());
            m.AcceptOrder();BusinessScenarioTools.Copper(m);Check(m.DeliverOrder()&&m.State.money==18,"first copper request funds parts");
            int costs=0;
            for(int request=0;request<4;request++)
            {
                Check(m.AcceptCommission(),"accept next neighbour request");
                bool radio=m.CurrentCommission.kind==MaterialKind.RestoredRadio;
                int count=m.CurrentCommission.quantity;
                for(int item=0;item<count;item++)
                {
                    if((radio?m.State.radiosTakenToday:m.State.fansTakenToday)>=(radio?m.Rules.radioDailyLimit:m.Rules.fanDailyLimit))Check(m.AdvanceDay(),"replenish only exhausted stock");
                    int cash=m.State.money;BusinessScenarioTools.Repair(m,radio);costs+=cash-m.State.money;
                    Check(m.DeliverCommission(),"tested appliance delivered");
                    m=new YardModel(m.Rules,Copy(m.State));
                }
            }
            Check(costs==25&&m.State.money==281&&m.State.dayIndex==2,"default route earns every euro and covers actual parts");
            Check(m.State.commissionIndex==4&&m.State.orderIndex==1&&m.State.items.Count==0,"all four restoration requests and original copper order complete");
            Check(m.CurrentCommission.customer=="Marta's Cafe"&&m.State.fansRepaired==3&&m.State.radiosRepaired==3,"repeatable queue, correct tested counts");
            YardModel.Validate(m.State);
        }
    }
}
