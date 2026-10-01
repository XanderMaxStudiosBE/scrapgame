using System;
namespace Scrapshift
{
    public sealed partial class YardModel
    {
        public int OccupiedBundles
        {
            get
            {
                return State.items.Count + (State.benchLoaded || State.benchOutput > 0 ? 1 : 0) +
                    (State.machineRemaining > 0 || State.machineOutput > 0 ? 1 : 0) + (State.fanStage != FanStage.Empty ? 1 : 0);
            }
        }
        void RecordIncome(int amount) { State.incomeToday = (int)Math.Min(int.MaxValue, (long)State.incomeToday + amount); }
        public bool AcquireFan()
        {
            if (Carried != null || OccupiedBundles >= Rules.maxBundles || State.nextId == int.MaxValue || State.fansTakenToday >= Rules.fanDailyLimit) return false;
            Create(MaterialKind.BrokenFan, 1); State.fansTakenToday++; return true;
        }
        public bool LoadFan()
        {
            var item = Carried;
            if (State.fanStage != FanStage.Empty || item == null || item.kind != MaterialKind.BrokenFan) return false;
            State.items.Remove(item); State.carriedId = 0; State.fanStage = FanStage.AwaitingInspection; return true;
        }
        public bool InspectFan()
        {
            if (State.fanStage != FanStage.AwaitingInspection) return false;
            State.fanStage = FanStage.Diagnosed; return true;
        }
        public bool BeginFanRepair()
        {
            if (State.fanStage != FanStage.Diagnosed || State.money < Rules.fanPartsPrice) return false;
            State.money -= Rules.fanPartsPrice; State.fanStage = FanStage.Repairing; return true;
        }
        public bool BeginFanDismantle()
        {
            if (State.fanStage != FanStage.Diagnosed) return false;
            State.fanStage = FanStage.Dismantling; return true;
        }
        public bool WorkFan()
        {
            if (Carried != null || (State.fanStage != FanStage.Repairing && State.fanStage != FanStage.Dismantling)) return false;
            bool repairing = State.fanStage == FanStage.Repairing;
            State.fanStrokes++;
            if (State.fanStrokes >= (repairing ? Rules.fanRepairStrokes : Rules.fanDismantleStrokes))
            {
                State.fanStage = repairing ? FanStage.ReadyToTest : FanStage.CopperReady;
                State.fanStrokes = 0;
                if (!repairing) State.fansDismantled = Math.Min(int.MaxValue - 1, State.fansDismantled) + 1;
            }
            return true;
        }
        public bool TestFan()
        {
            if (State.fanStage != FanStage.ReadyToTest || Carried != null) return false;
            State.fanStage = FanStage.Tested; State.fansRepaired = Math.Min(int.MaxValue - 1, State.fansRepaired) + 1;
            return true;
        }
        public bool CollectFan()
        {
            if (Carried != null || State.items.Count >= Rules.maxBundles || State.nextId == int.MaxValue ||
                (State.fanStage != FanStage.Tested && State.fanStage != FanStage.CopperReady)) return false;
            bool restored = State.fanStage == FanStage.Tested;
            Create(restored ? MaterialKind.RestoredFan : MaterialKind.Copper, restored ? 1 : Rules.fanCopperYield);
            State.fanStage = FanStage.Empty; return true;
        }
        public bool AdvanceDay()
        {
            if (State.dayIndex == int.MaxValue) return false;
            // Powered work finishes overnight once; hand-work and all material locations are retained.
            if (State.machineRemaining > 0) Tick(State.machineRemaining);
            State.dayIndex++; State.fansTakenToday = 0; State.incomeToday = 0; return true;
        }
    }
}
