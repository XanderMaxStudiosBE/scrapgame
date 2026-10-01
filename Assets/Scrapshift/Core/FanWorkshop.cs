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
        void RecordIncome(int amount) { State.incomeToday = (int)Math.Min(int.MaxValue, (long)State.incomeToday + amount); State.milestones|=YardMilestone.EarnedIncome; }
        public bool AcquireFan()
        {
            if (Carried != null || OccupiedBundles >= Capacity || State.nextId == int.MaxValue || State.fansTakenToday >= Rules.fanDailyLimit) return false;
            var item=Create(MaterialKind.BrokenFan, 1);
            item.applianceFault=(ApplianceFault)(((long)State.dayIndex+State.fansTakenToday)%3);
            State.fansTakenToday++; return true;
        }
        public bool AcquireRadio()
        {
            if(Carried!=null || OccupiedBundles>=Capacity || State.nextId==int.MaxValue || State.radiosTakenToday>=Rules.radioDailyLimit)return false;
            var item=Create(MaterialKind.BrokenRadio,1);
            item.applianceFault=(ApplianceFault)(((long)State.dayIndex+State.radiosTakenToday)%3);
            State.radiosTakenToday++; return true;
        }
        // Historical Fan method/state names are retained for source and version-one save compatibility.
        // The same reserved restoration-bench slot now accepts either appliance; it never runs two jobs.
        public bool LoadFan()
        {
            var item = Carried;
            if (State.fanStage != FanStage.Empty || item == null || !ApplianceRecipe.IsBroken(item.kind)) return false;
            State.benchAppliance=item.kind==MaterialKind.BrokenRadio?RepairAppliance.PortableRadio:RepairAppliance.DeskFan;
            State.benchFault=item.applianceFault;
            State.items.Remove(item); State.carriedId = 0; State.fanStage = FanStage.AwaitingInspection; return true;
        }
        public bool InspectFan()
        {
            if (State.fanStage != FanStage.AwaitingInspection) return false;
            State.fanStage = FanStage.Diagnosed; return true;
        }
        public bool BeginFanRepair()
        {
            if (State.fanStage != FanStage.Diagnosed || State.money < CurrentRepair.partsPrice) return false;
            State.money -= CurrentRepair.partsPrice; State.fanStage = FanStage.Repairing; return true;
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
            if (State.fanStrokes >= (repairing ? FanRepairSteps : FanSalvageSteps))
            {
                CompleteFanWork(repairing);
            }
            return true;
        }
        void CompleteFanWork(bool repairing)
        {
            State.fanStage = repairing ? FanStage.ReadyToTest : FanStage.CopperReady;
            State.fanStrokes = 0;
            if (!repairing)
            {
                State.milestones|=YardMilestone.RecoveredCopper;
                if(State.benchAppliance==RepairAppliance.PortableRadio)State.radiosDismantled=Math.Min(int.MaxValue-1,State.radiosDismantled)+1;
                else State.fansDismantled = Math.Min(int.MaxValue - 1, State.fansDismantled) + 1;
            }
        }
        public bool TestFan()
        {
            if (State.fanStage != FanStage.ReadyToTest || Carried != null) return false;
            State.fanStage = FanStage.Tested;
            State.milestones|=YardMilestone.RestoredAppliance;
            if(State.benchAppliance==RepairAppliance.PortableRadio)State.radiosRepaired=Math.Min(int.MaxValue-1,State.radiosRepaired)+1;
            else State.fansRepaired = Math.Min(int.MaxValue - 1, State.fansRepaired) + 1;
            return true;
        }
        public bool CollectFan()
        {
            if (!CanCollectOutput ||
                (State.fanStage != FanStage.Tested && State.fanStage != FanStage.CopperReady)) return false;
            bool restored = State.fanStage == FanStage.Tested;
            var kind=State.benchAppliance==RepairAppliance.PortableRadio?MaterialKind.RestoredRadio:MaterialKind.RestoredFan;
            var item=Create(restored ? kind : MaterialKind.Copper, restored ? 1 : CurrentRepair.copperYield);
            if(restored)item.applianceFault=State.benchFault;
            State.fanStage = FanStage.Empty; State.benchAppliance=RepairAppliance.DeskFan; State.benchFault=ApplianceFault.MainComponent; return true;
        }
        public bool AdvanceDay()
        {
            if (State.dayIndex == int.MaxValue) return false;
            // Powered work finishes overnight once; hand-work and all material locations are retained.
            if (State.machineRemaining > 0) Tick(State.machineRemaining);
            State.dayIndex++; State.fansTakenToday = 0; State.radiosTakenToday=0; State.incomeToday = 0; return true;
        }
    }
}
