using System;

namespace Scrapshift.Tests
{
    public static class GuidanceScenarios
    {
        public static readonly string[] Names = { "DynamicHints", "StationStates", "StateBasedObjectives", "GuidancePreservesMaterials" };
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static StationHint Hint(YardModel m, TargetKind target) { return YardGuidance.Hint(m, target, 0, "F", "Right mouse"); }
        public static void Run(string name)
        {
            var m = new YardModel(new YardRules());
            switch (name)
            {
                case "DynamicHints":
                    Check(Hint(m, TargetKind.Supply).text.Contains("[F]"), "interact label");
                    m.AcquireWire(); m.LoadBench();
                    Check(Hint(m, TargetKind.Bench).text.Contains("[Right mouse]") && !Hint(m, TargetKind.Bench).text.Contains("LMB"), "manual binding");
                    Check(YardGuidance.Objective(m, "F", "Right mouse", "R").Contains("[Right mouse]"), "tutorial binding"); break;
                case "StationStates":
                    Check(Hint(m, TargetKind.Supply).canUse && !Hint(m, TargetKind.Sell).canUse && !Hint(m, TargetKind.Bench).canUse, "new game");
                    m.AcquireWire(); Check(!Hint(m, TargetKind.Supply).canUse && !Hint(m, TargetKind.Sell).canUse && Hint(m, TargetKind.Bench).canUse, "wire and full hands");
                    m.LoadBench(); Check(Hint(m, TargetKind.Bench).canUse, "loaded bench");
                    for (int i=0;i<4;i++) m.WorkBench(); Check(Hint(m, TargetKind.Bench).canUse && Hint(m,TargetKind.Bench).text.Contains("collect 3"), "bench output");
                    m.CollectBench(); Check(!Hint(m,TargetKind.Bench).canUse && Hint(m,TargetKind.Sell).text.Contains("€12"), "finished copper");
                    Check(!Hint(m, TargetKind.Machine).canUse && Hint(m, TargetKind.Machine).text.Contains("€36"), "unaffordable");
                    m.State.money=36; Check(Hint(m, TargetKind.Machine).canUse, "affordable"); m.BuyMachine();
                    Check(!Hint(m, TargetKind.Machine).canUse, "wrong input copper"); m.Sell();
                    Check(!Hint(m, TargetKind.Machine).canUse && Hint(m, TargetKind.Machine).text.Contains("DELIVERY"), "idle empty");
                    m.AcquireWire(); Check(Hint(m, TargetKind.Machine).canUse && Hint(m, TargetKind.Machine).text.Contains("front opening"), "idle wire");
                    m.FeedMachine(); Check(!Hint(m, TargetKind.Machine).canUse && Hint(m, TargetKind.Machine).text.Contains("Processing"), "busy");
                    m.Tick(5); Check(Hint(m, TargetKind.Machine).canUse, "ready"); m.AcquireWire();
                    Check(!Hint(m, TargetKind.Machine).canUse && Hint(m, TargetKind.Machine).text.Contains("Put down"), "output full hands"); break;
                case "StateBasedObjectives":
                    Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("DELIVERY"), "first wire");
                    m.AcquireWire(); Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("WORKBENCH"), "load");
                    m.LoadBench(); Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("[Mouse4]"), "strip");
                    m.AcquireWire(); Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("[R]"), "drop spare");
                    m.Drop(0,.3f,0); for(int i=0;i<4;i++)m.WorkBench();
                    Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("Collect"), "collect");
                    m.CollectBench(); Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("BUYER"), "sell");
                    m.Sell(); m.State.money=36; Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("Buy"), "buy");
                    m.BuyMachine(); var resumed=new YardModel(m.Rules,m.State);
                    Check(YardGuidance.Objective(resumed,"F","Mouse4","R").Contains("powered stripper"), "owned save");
                    m.AcquireWire(); Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("front opening"), "feed"); break;
                case "GuidancePreservesMaterials":
                    m.AcquireWire(); int id=m.Carried.id;
                    for(int i=0;i<100;i++) { foreach(TargetKind target in Enum.GetValues(typeof(TargetKind))) YardGuidance.Hint(m,target,id,"F","Mouse1"); YardGuidance.Objective(m,"F","Mouse1","R"); }
                    Check(m.Carried.id==id && m.State.items.Count==1 && m.State.money==0 && !m.State.machineOwned, "presentation must be read-only"); break;
                default: throw new Exception("Unknown guidance case");
            }
            YardModel.Validate(m.State);
        }
    }
}
