using System;
using System.Collections.Generic;
namespace Scrapshift.Tests
{
    public static class JourneyScenarios
    {
        public static readonly string[] Names={"JourneyTracksSuccessfulWork", "JourneyRejectsPrematureGoals", "JourneySurvivesNewDay", "JourneyLegacyReadIsConservative", "JourneyGoalsAndValidation", "OpeningChapterRequiresEveryGoal", "OpeningChapterCompleteFromZero", "OpeningChapterLegacyAndResume"};
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static bool Done(YardModel m,YardMilestone goal){return (YardJourney.Completed(m.State)&goal)!=0;}
        public static void Run(string name)
        {
            var m=new YardModel(new YardRules());
            switch(name)
            {
                case "JourneyTracksSuccessfulWork":
                    Check(!YardJourney.HasProgress(m.State),"fresh yard");m.AcquireWire();m.LoadBench();
                    while(m.State.benchLoaded)m.WorkBench();Check(Done(m,YardMilestone.RecoveredCopper),"work records recovery");m.CollectBench();m.Sell();
                    Check(Done(m,YardMilestone.EarnedIncome)&&m.State.money==12,"income goal creates no bonus");m.AcquireWire();m.LoadBench();while(m.State.benchLoaded)m.WorkBench();m.CollectBench();m.AcceptOrder();m.DeliverOrder();
                    Check(Done(m,YardMilestone.ServedCustomer)&&m.State.money==30,"contract goal creates no bonus");m.AcquireWire();m.LoadBench();while(m.State.benchLoaded)m.WorkBench();m.CollectBench();m.Sell();m.BuyMachine();
                    Check(Done(m,YardMilestone.PoweredYard)&&m.State.money==6,"purchase goal creates no bonus");m.AcquireRadio();m.LoadFan();m.InspectFan();m.BeginFanRepair();while(m.State.fanStage==FanStage.Repairing)m.WorkFan();m.TestFan();
                    Check(Done(m,YardMilestone.RestoredAppliance)&&m.State.money==0,"test goal creates no bonus");break;
                case "JourneyRejectsPrematureGoals":
                    Check(!m.Sell()&&!m.DeliverOrder()&&!m.BuyMachine()&&!m.TestFan()&&!m.BuyUpgrade(YardUpgrade.HandTools),"rejected actions");
                    Check(m.State.milestones==YardMilestone.None,"rejected actions cannot mark goals");m.State.money=8;m.AcquireFan();m.LoadFan();m.InspectFan();m.BeginFanRepair();
                    while(m.State.fanStage==FanStage.Repairing)m.WorkFan();Check(!Done(m,YardMilestone.RestoredAppliance),"assembly alone not a restoration");m.TestFan();m.CollectFan();m.State.money=int.MaxValue-41;
                    Check(!m.Sell()&&!Done(m,YardMilestone.EarnedIncome),"failed sale creates no income goal");break;
                case "JourneySurvivesNewDay":
                    m.AcquireWire();m.LoadBench();while(m.State.benchLoaded)m.WorkBench();m.CollectBench();m.Sell();m.State.introSeen=true;
                    m.AdvanceDay();m=new YardModel(m.Rules,m.State);
                    Check(m.State.incomeToday==0&&Done(m,YardMilestone.RecoveredCopper)&&Done(m,YardMilestone.EarnedIncome)&&m.State.introSeen,"journal survives income reset/reconstruction");
                    m=new YardModel(m.Rules);Check(!m.State.introSeen&&YardJourney.Completed(m.State)==YardMilestone.None,"new yard resets journal");break;
                case "JourneyLegacyReadIsConservative":
                    var state=new YardState{machineOwned=true,fansRepaired=2,orderIndex=1,upgrades=YardUpgrade.HandTools};
                    var before=state.milestones;var completed=YardJourney.Completed(state);
                    Check((completed&(YardMilestone.PoweredYard|YardMilestone.RestoredAppliance|YardMilestone.ServedCustomer|YardMilestone.InvestedYard))==(YardMilestone.PoweredYard|YardMilestone.RestoredAppliance|YardMilestone.ServedCustomer|YardMilestone.InvestedYard),"known legacy facts recognized");
                    Check(state.milestones==before&&state.money==0,"read cannot alter save/economy");
                    Check((YardJourney.Completed(new YardState{money=99})&YardMilestone.RecoveredCopper)==0,"cash does not invent wire history");break;
                case "JourneyGoalsAndValidation":
                    var seen=new HashSet<YardMilestone>();
                    foreach(var goal in YardJourney.Goals){Check(seen.Add(goal.milestone)&&goal.milestone!=YardMilestone.None,"unique goal");YardNavigation.Get(goal.destination);}
                    Check(seen.Count==6,"bounded journal");
                    m.State.milestones=(YardMilestone)64;bool rejected=false;try{YardModel.Validate(m.State);}catch(ArgumentException){rejected=true;}Check(rejected,"unknown bits rejected");m.State.milestones=YardMilestone.None;
                    m.State.money=75;Check(m.BuyUpgrade(YardUpgrade.HandTools)&&Done(m,YardMilestone.InvestedYard)&&m.State.money==0,"investment goal has no bonus");break;
                case "OpeningChapterRequiresEveryGoal":
                    Check(!YardJourney.PresentOpeningCompletion(m.State),"fresh yard has no ending");
                    foreach(var goal in YardJourney.Goals)
                    {
                        var partial=new YardState{milestones=YardJourney.Known & ~goal.milestone,money=9999};
                        Check(!YardJourney.OpeningComplete(partial)&&!YardJourney.PresentOpeningCompletion(partial)&&!partial.openingChapterSeen,"each goal matters; cash is insufficient");
                    }
                    break;
                case "OpeningChapterCompleteFromZero":
                    // Play a complete route with real transactions and no injected starting money.
                    m.AcquireWire();m.LoadBench();while(m.State.benchLoaded)m.WorkBench();m.CollectBench();m.Sell();
                    m.AcceptOrder();m.AcquireWire();m.LoadBench();while(m.State.benchLoaded)m.WorkBench();m.CollectBench();m.DeliverOrder();
                    m.AcquireWire();m.LoadBench();while(m.State.benchLoaded)m.WorkBench();m.CollectBench();m.Sell();m.BuyMachine();
                    m.AcquireRadio();m.LoadFan();m.InspectFan();m.BeginFanRepair();while(m.State.fanStage==FanStage.Repairing)m.WorkFan();m.TestFan();m.CollectFan();m.Sell();
                    for(int i=0;i<3;i++){Check(m.AcquireWire()&&m.FeedMachine(),"feed powered yard");m.Tick(10);Check(m.CollectMachine()&&m.Sell(),"collect/sell powered output");}
                    Check(m.BuyUpgrade(YardUpgrade.StorageRack),"earned investment");
                    Check(YardJourney.OpeningComplete(m.State),"six earned goals complete chapter");
                    int money=m.State.money,id=m.State.nextId;var goals=m.State.milestones;
                    Check(YardJourney.PresentOpeningCompletion(m.State)&&m.State.openingChapterSeen,"completion offered once");
                    Check(!YardJourney.PresentOpeningCompletion(m.State)&&m.State.money==money&&m.State.nextId==id&&m.State.milestones==goals,"repeated ending never grants or consumes anything");
                    Check(m.AcquireWire()&&m.FeedMachine(),"free play still works");break;
                case "OpeningChapterLegacyAndResume":
                    var old=new YardState{benchOutput=3,machineOwned=true,fansRepaired=1,orderIndex=1,upgrades=YardUpgrade.StorageRack};
                    Check(!old.openingChapterSeen&&YardJourney.OpeningComplete(old),"known old-save progress counts");
                    Check(YardJourney.PresentOpeningCompletion(old),"old completed save gets recap");
                    m=new YardModel(m.Rules,old);Check(m.CollectBench()&&m.Sell(),"consume temporary legacy evidence");m.AdvanceDay();m=new YardModel(m.Rules,m.State);
                    Check(m.State.openingChapterSeen&&YardJourney.OpeningComplete(m.State)&&!YardJourney.PresentOpeningCompletion(m.State),"acknowledgement survives day and reconstruction");
                    Check(!new YardModel(m.Rules).State.openingChapterSeen,"fresh yard has its own ending");break;
                default:throw new Exception(name);
            }
            YardModel.Validate(m.State);
        }
    }
}
