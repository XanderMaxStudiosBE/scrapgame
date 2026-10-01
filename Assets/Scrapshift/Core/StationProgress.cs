namespace Scrapshift
{
    // Read-only presentation; completion and rewards remain owned by YardModel.
    public struct StationProgress
    {
        public readonly string label;
        public readonly float fraction;
        public bool Visible { get { return label != null; } }
        StationProgress(string label, int done, int total)
        { this.label = label; fraction = total > 0 ? (float)done / total : 0; }
        public static StationProgress Read(YardModel model, TargetKind target)
        {
            var s = model.State;
            if (target == TargetKind.Bench)
            {
                if (s.benchOutput > 0) return new StationProgress("COPPER READY / " + s.benchOutput + " copper", 1, 1);
                if (s.benchLoaded) return new StationProgress("STRIPPING / " + s.benchStrokes + " of " + model.WireWorkSteps + " strokes", s.benchStrokes, model.WireWorkSteps);
            }
            if (target == TargetKind.FanBench)
            {
                switch (s.fanStage)
                {
                    case FanStage.Repairing: return new StationProgress("FITTING MOTOR / " + s.fanStrokes + " of " + model.FanRepairSteps + " steps", s.fanStrokes, model.FanRepairSteps);
                    case FanStage.Dismantling: return new StationProgress("RECOVERING COPPER / " + s.fanStrokes + " of " + model.FanSalvageSteps + " steps", s.fanStrokes, model.FanSalvageSteps);
                    case FanStage.ReadyToTest: return new StationProgress("MOTOR FITTED / Power-on test still required", 1, 1);
                    case FanStage.Tested: return new StationProgress("TEST PASSED / Resale value EUR " + model.Rules.fanSalePrice, 1, 1);
                    case FanStage.CopperReady: return new StationProgress("SALVAGE READY / " + model.Rules.fanCopperYield + " copper", 1, 1);
                }
            }
            return default(StationProgress);
        }
    }
}
