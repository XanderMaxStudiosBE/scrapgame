using System;
namespace Scrapshift.Tests
{
    public static class WorkFeedbackScenarios
    {
        public static readonly string[] Names = { "WireFeedbackTracksTransactions", "FanFeedbackRequiresTest", "FeedbackResumesUpgradedJobs" };
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run(string name)
        {
            var m = new YardModel(new YardRules());
            switch (name)
            {
                case "WireFeedbackTracksTransactions":
                    Check(!StationProgress.Read(m,TargetKind.Bench).Visible,"empty bench");
                    m.AcquireWire(); m.LoadBench();
                    Check(StationProgress.Read(m,TargetKind.Bench).fraction==0,"initial progress");
                    m.WorkBench();
                    Check(StationProgress.Read(m,TargetKind.Bench).fraction==1f/m.WireWorkSteps,"one stroke");
                    m.AcquireWire();
                    Check(!m.WorkBench(),"full hands reject work");
                    Check(StationProgress.Read(m,TargetKind.Bench).fraction==1f/m.WireWorkSteps,"rejected stroke unchanged");
                    m.Drop(0,.3f,0);
                    while(m.State.benchLoaded)m.WorkBench();
                    var ready=StationProgress.Read(m,TargetKind.Bench);
                    Check(ready.fraction==1 && ready.label.Contains(m.State.benchOutput+" copper"),"actual output");
                    m.CollectBench();
                    Check(!StationProgress.Read(m,TargetKind.Bench).Visible,"collected clears");
                    Check(!StationProgress.Read(m,TargetKind.Sell).Visible,"unrelated station"); break;
                case "FanFeedbackRequiresTest":
                    m.State.money=100;m.AcquireFan();m.LoadFan();m.InspectFan();m.BeginFanRepair();
                    m.WorkFan(); Check(StationProgress.Read(m,TargetKind.FanBench).fraction==1f/m.FanRepairSteps,"repair progress");
                    while(m.State.fanStage==FanStage.Repairing)m.WorkFan();
                    Check(StationProgress.Read(m,TargetKind.FanBench).label.Contains("test still required"),"no premature test success");
                    Check(m.State.fansRepaired==0,"presentation does not test");
                    m.TestFan();Check(StationProgress.Read(m,TargetKind.FanBench).label.Contains("TEST PASSED"),"tested status");
                    m.CollectFan();Check(!StationProgress.Read(m,TargetKind.FanBench).Visible,"collected clears");break;
                case "FeedbackResumesUpgradedJobs":
                    m.State.money=100;m.BuyUpgrade(YardUpgrade.HandTools);m.AcquireFan();m.LoadFan();m.InspectFan();m.BeginFanDismantle();m.WorkFan();
                    var resumed=new YardModel(m.Rules,m.State);
                    var progress=StationProgress.Read(resumed,TargetKind.FanBench);
                    Check(progress.fraction==1f/resumed.FanSalvageSteps && progress.label.Contains("1 of "+resumed.FanSalvageSteps),"saved upgraded threshold");
                    for(int i=0;i<100;i++)StationProgress.Read(resumed,TargetKind.FanBench);
                    Check(resumed.State.fanStrokes==1 && resumed.State.fansDismantled==0,"read-only");
                    while(resumed.State.fanStage==FanStage.Dismantling)resumed.WorkFan();
                    Check(StationProgress.Read(resumed,TargetKind.FanBench).label.Contains(resumed.Rules.fanCopperYield+" copper"),"salvage reward");break;
                default: throw new Exception("Unknown work feedback scenario");
            }
        }
    }
}
