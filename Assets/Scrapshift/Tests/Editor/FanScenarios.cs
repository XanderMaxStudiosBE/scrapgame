using System;
namespace Scrapshift.Tests
{
    public static class FanScenarios
    {
        public static readonly string[] Names = { "FanRepairToResale", "FanDismantleToCopper", "FanChoicesAndChargesAreGuarded", "FanCapacityAndStorageGuards", "FanDailySupplyAndOvernightMachine", "FanPartialResume", "FanInvalidStates", "FanSaleOverflow", "FanBindingGuidance", "ItemIdExhaustionRetainsOutputs" };
        static void Check(bool result, string message) { if (!result) throw new Exception(message); }
        static void Loaded(YardModel m) { Check(m.AcquireFan() && m.LoadFan() && m.InspectFan(), "acquire/load/inspect"); }
        static void Reject(YardState state)
        {
            try { YardModel.Validate(state); } catch (ArgumentException) { return; }
            throw new Exception("invalid fan state accepted");
        }
        public static void Run(string name)
        {
            var m = new YardModel(new YardRules());
            switch (name)
            {
                case "FanRepairToResale":
                    m.State.money = 12; Loaded(m); Check(m.BeginFanRepair() && m.State.money == 4, "motor paid once");
                    for (int i = 0; i < 3; i++) Check(m.WorkFan(), "motor assembly");
                    Check(m.State.fanStage == FanStage.ReadyToTest && !m.CollectFan(), "test required before resale");
                    Check(m.TestFan() && !m.TestFan() && m.State.fansRepaired == 1, "one test completion");
                    Check(m.CollectFan() && m.Carried.kind == MaterialKind.RestoredFan && !m.CollectFan(), "one restored item");
                    Check(m.Sell() && !m.Sell() && m.State.money == 46 && m.State.incomeToday == 42, "one resale, motor cost retained"); break;
                case "FanDismantleToCopper":
                    Loaded(m); Check(m.BeginFanDismantle() && !m.BeginFanRepair(), "exclusive salvage choice");
                    for (int i = 0; i < 4; i++) Check(m.WorkFan(), "salvage stroke");
                    Check(!m.WorkFan() && m.State.fansDismantled == 1 && m.CollectFan(), "bounded yield");
                    Check(m.Carried.kind == MaterialKind.Copper && m.Carried.quantity == 3 && m.Sell() && m.State.money == 12, "recover and sell copper"); break;
                case "FanChoicesAndChargesAreGuarded":
                    Check(!m.BeginFanRepair() && !m.TestFan(), "empty guards"); Loaded(m);
                    Check(!m.BeginFanRepair() && m.State.money == 0 && m.State.fanStage == FanStage.Diagnosed, "insufficient parts money retains fan");
                    m.State.money = 8; Check(m.BeginFanRepair() && !m.BeginFanRepair() && m.State.money == 0 && !m.BeginFanDismantle(), "no double motor charge or branch switch");
                    m.AcquireWire(); Check(!m.WorkFan(), "full hands prevent hand work"); m.Drop(0,.3f,0);
                    Check(m.WorkFan(), "resume after freeing hands"); break;
                case "FanCapacityAndStorageGuards":
                    m = new YardModel(new YardRules { maxBundles = 1 }); Loaded(m);
                    Check(!m.AcquireWire() && !m.AcquireFan(), "fan bench reserves one slot");
                    m.BeginFanDismantle(); for (int i = 0; i < 4; i++) m.WorkFan();
                    Check(m.CollectFan() && m.State.items.Count == 1 && m.Store(MaterialKind.Copper), "reserved output can collect/store");
                    Check(!m.AcquireWire(), "stored copper still counts toward capacity");
                    m.Retrieve(MaterialKind.Copper); m.Sell(); m.AcquireFan();
                    Check(!m.Store(MaterialKind.BrokenFan) && !m.Retrieve(MaterialKind.BrokenFan) && m.Carried != null, "fan cannot corrupt a material-only bin"); break;
                case "FanDailySupplyAndOvernightMachine":
                    for (int i = 0; i < 2; i++) { m.AcquireFan(); m.Drop(i,.3f,0); }
                    Check(!m.AcquireFan() && m.State.fansTakenToday == 2, "daily supply limit");
                    m.State.machineOwned = true; m.AcquireWire(); m.FeedMachine(); m.Tick(2);
                    m.AcceptOrder(); m.State.playerX = 31; Check(m.AdvanceDay(), "next day");
                    Check(m.State.dayIndex == 1 && m.State.fansTakenToday == 0 && m.State.machineOutput == 3 && m.State.machineRemaining == 0, "new supply and one overnight output");
                    Check(m.State.items.Count == 2 && m.State.orderAccepted && m.State.playerX == 31 && m.AcquireFan(), "inventory/order/location retained"); break;
                case "FanPartialResume":
                    m.State.money = 8; Loaded(m); m.BeginFanRepair(); m.WorkFan();
                    var restored = new YardModel(m.Rules, m.State);
                    Check(restored.State.fanStrokes == 1 && restored.State.money == 0 && !restored.BeginFanRepair(), "paid partial work retained");
                    restored.WorkFan(); restored.WorkFan(); restored.TestFan(); restored.CollectFan();
                    Check(restored.Carried.kind == MaterialKind.RestoredFan && restored.State.fansRepaired == 1, "finish exactly once"); break;
                case "FanInvalidStates":
                    Reject(new YardState { fanStage = (FanStage)88 }); Reject(new YardState { fanStrokes = 1 });
                    Reject(new YardState { fansTakenToday = -1 }); Reject(new YardState { dayIndex = -1 });
                    Reject(new YardState { incomeToday = -1 });
                    m.AcquireFan(); m.Carried.quantity = 2; Reject(m.State);
                    m.Carried.quantity = 1; m.State.dayIndex = int.MaxValue; Check(!m.AdvanceDay(), "bounded day index"); break;
                case "FanSaleOverflow":
                    m.State.money = 8; Loaded(m); m.BeginFanRepair(); for (int i = 0; i < 3; i++) m.WorkFan();
                    m.TestFan(); m.CollectFan(); m.State.money = int.MaxValue - 41;
                    Check(!m.Sell() && m.Carried != null && m.State.incomeToday == 0, "failed payment retains appliance");
                    m.State.money = int.MaxValue - 42; Check(m.Sell() && m.State.money == int.MaxValue, "exact-limit sale"); break;
                case "FanBindingGuidance":
                    m.AcquireFan(); Check(YardGuidance.Objective(m,"F","Mouse4","R").Contains("RESTORATION BENCH [F]"), "loaded objective binding");
                    m.LoadFan(); m.InspectFan(); m.BeginFanDismantle();
                    Check(YardGuidance.Hint(m,TargetKind.FanBench,0,"F","Mouse4").text.Contains("[Mouse4] dismantle"), "manual binding"); break;
                case "ItemIdExhaustionRetainsOutputs":
                    m.State.nextId = int.MaxValue; Check(!m.AcquireWire() && !m.AcquireFan(), "renewable input bounded without throwing");
                    m.State.benchOutput = 3; Check(!m.CollectBench() && m.State.benchOutput == 3, "bench output retained");
                    m.State.fanStage = FanStage.Tested; Check(!m.CollectFan() && m.State.fanStage == FanStage.Tested, "fan output retained"); break;
                default: throw new Exception(name);
            }
            YardModel.Validate(m.State);
        }
    }
}
