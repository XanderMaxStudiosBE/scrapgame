namespace Scrapshift
{
    public enum TargetKind { Supply, Bench, Machine, Sell, LooseItem, WireStorage, CopperStorage, OrderBoard, FanSupply, FanBench, DayBoard }

    public sealed class StationHint
    {
        public readonly bool canUse;
        public readonly string text;
        public StationHint(bool canUse, string text) { this.canUse = canUse; this.text = text; }
    }

    // Presentation derives from the authoritative yard, so dropped items and restored saves need no tutorial migration.
    public static class YardGuidance
    {
        public static StationHint Hint(YardModel m, TargetKind target, int itemId, string interact, string work)
        {
            var s = m.State; var carried = m.Carried; var r = m.Rules;
            string use = "[" + interact + "] ";
            string emptyHands = "Put down or sell your carried bundle first.";
            switch (target)
            {
                case TargetKind.DayBoard:
                    return new StationHint(true, "YARD DIARY • " + use + "review the day / return tomorrow");
                case TargetKind.FanSupply:
                    if (carried != null) return new StationHint(false, "APPLIANCE SALVAGE • Hands full. " + emptyHands);
                    if (s.fansTakenToday >= r.fanDailyLimit) return new StationHint(false, "APPLIANCE SALVAGE • Today's fans collected. More arrive tomorrow; wire remains available.");
                    if (m.OccupiedBundles >= m.Capacity || s.nextId == int.MaxValue) return new StationHint(false, "APPLIANCE SALVAGE • Yard capacity reached. Process, sell or finish existing work.");
                    return new StationHint(true, "APPLIANCE SALVAGE • " + use + "take broken desk fan • " + (r.fanDailyLimit - s.fansTakenToday) + " available today");
                case TargetKind.FanBench:
                    if (s.fanStage == FanStage.Empty)
                        return new StationHint(carried != null && carried.kind == MaterialKind.BrokenFan, "RESTORATION BENCH • " +
                            (carried != null && carried.kind == MaterialKind.BrokenFan ? use + "place broken desk fan" : "Bring a broken desk fan from appliance salvage."));
                    if (s.fanStage == FanStage.ReadyToTest)
                        return new StationHint(carried == null, "RESTORATION BENCH • " + (carried == null ? use + "power on and test repaired fan" : emptyHands));
                    if (s.fanStage == FanStage.Tested || s.fanStage == FanStage.CopperReady)
                        return new StationHint(carried == null && s.items.Count < m.Capacity && s.nextId < int.MaxValue, "RESTORATION BENCH • " +
                            (carried == null ? use + (s.fanStage == FanStage.Tested ? "collect tested fan • worth €" + r.fanSalePrice : "collect " + r.fanCopperYield + " copper") : emptyHands));
                    if (s.fanStage == FanStage.Repairing || s.fanStage == FanStage.Dismantling)
                        return new StationHint(carried == null, "RESTORATION BENCH • " + (carried == null ? "[" + work + "] " +
                            (s.fanStage == FanStage.Repairing ? "fit motor" : "dismantle") + " • " + s.fanStrokes + "/" +
                            (s.fanStage == FanStage.Repairing ? m.FanRepairSteps : m.FanSalvageSteps) + " • " + use + "inspect job" : emptyHands));
                    return new StationHint(true, "RESTORATION BENCH • " + use + (s.fanStage == FanStage.AwaitingInspection ? "inspect broken fan" : "choose repair or salvage"));
                case TargetKind.WireStorage:
                case TargetKind.CopperStorage:
                    MaterialKind storedKind = target == TargetKind.WireStorage ? MaterialKind.Wire : MaterialKind.Copper;
                    string bin = storedKind == MaterialKind.Wire ? "WIRE STORAGE" : "COPPER STORAGE";
                    int stored = m.StoredBundles(storedKind);
                    if (carried != null)
                        return new StationHint(carried.kind == storedKind, bin + " • " +
                            (carried.kind == storedKind ? use + "store carried bundle • " + stored + " stored" : "This bin holds " + storedKind.ToString().ToLowerInvariant() + "."));
                    return new StationHint(stored > 0, bin + " • " + (stored > 0 ? use + "take one bundle • " + m.StoredQuantity(storedKind) + " units stored" : "Empty. Bring " + storedKind.ToString().ToLowerInvariant() + " here to store it."));
                case TargetKind.OrderBoard:
                    var order = m.CurrentOrder;
                    if (!s.orderAccepted)
                        return new StationHint(s.orderIndex < int.MaxValue, "CUSTOMER BOARD • " + use + "accept " + order.copper + " copper for €" + order.reward + " • no deadline");
                    int remaining = order.copper - s.orderDelivered;
                    if (carried == null || carried.kind != MaterialKind.Copper)
                        return new StationHint(false, "CUSTOMER BOARD • " + order.customer + " needs " + remaining + " more copper. Carry a copper bundle here.");
                    int amount = System.Math.Min(remaining, carried.quantity);
                    if (amount == remaining && (long)s.money + order.reward > int.MaxValue)
                        return new StationHint(false, "CUSTOMER BOARD • Balance limit reached; copper retained.");
                    return new StationHint(true, "CUSTOMER BOARD • " + use + "deliver " + amount + " copper • " + s.orderDelivered + "/" + order.copper + " • €" + order.reward + " on completion");
                case TargetKind.Supply:
                    if (carried != null) return new StationHint(false, "DELIVERY • Hands full. " + emptyHands);
                    if (m.OccupiedBundles >= m.Capacity || s.nextId == int.MaxValue) return new StationHint(false, "DELIVERY • Yard storage full. Process or sell existing bundles.");
                    return new StationHint(true, "DELIVERY • " + use + "take free scrap wire");
                case TargetKind.LooseItem:
                    var item = m.Find(itemId);
                    if (item == null) return new StationHint(false, "This bundle is no longer here.");
                    string description = item.kind == MaterialKind.BrokenFan ? "Broken desk fan • inspect at the restoration bench" :
                        item.kind == MaterialKind.RestoredFan ? "Tested desk fan • worth €" + r.fanSalePrice :
                        item.kind + " ×" + item.quantity + (item.kind == MaterialKind.Copper ? " • worth €" + (long)item.quantity * r.copperUnitPrice : " • strip to recover " + r.copperPerWire + " copper");
                    return new StationHint(carried == null, description + " • " + (carried == null ? use + "pick up" : "Hands full. " + emptyHands));
                case TargetKind.Sell:
                    if (carried == null) return new StationHint(false, "COPPER BUYER • Carry copper here to sell it.");
                    if (carried.kind != MaterialKind.Copper && carried.kind != MaterialKind.RestoredFan)
                        return new StationHint(false, "SCRAP BUYER • " + (carried.kind == MaterialKind.BrokenFan ? "Inspect, repair or dismantle this fan at the restoration bench first." : "Wire must be stripped at the bench or machine first."));
                    long value = carried.kind == MaterialKind.Copper ? (long)carried.quantity * r.copperUnitPrice : r.fanSalePrice;
                    if ((long)s.money + value > int.MaxValue) return new StationHint(false, "COPPER BUYER • Balance limit reached; bundle retained.");
                    return new StationHint(true, "SCRAP BUYER • " + use + "sell " + (carried.kind == MaterialKind.RestoredFan ? "tested desk fan" : carried.quantity + " copper") + " for €" + value);
                case TargetKind.Bench:
                    if (s.benchOutput > 0) return OutputHint(m, "WORKBENCH", s.benchOutput, use, emptyHands);
                    if (s.benchLoaded)
                        return carried == null ? new StationHint(true, "WORKBENCH • [" + work + "] strip wire • " + s.benchStrokes + "/" + m.WireWorkSteps + " strokes") :
                            new StationHint(false, "WORKBENCH • Wire loaded. Empty your hands before stripping.");
                    if (carried == null) return new StationHint(false, "WORKBENCH • Get wire from DELIVERY, then place it here.");
                    if (carried.kind != MaterialKind.Wire) return new StationHint(false, "WORKBENCH • Copper is already stripped. Take it to the buyer.");
                    return new StationHint(true, "WORKBENCH • " + use + "place scrap wire");
                case TargetKind.Machine:
                    if (!s.machineOwned)
                    {
                        if (s.money < r.machinePrice) return new StationHint(false, "POWERED STRIPPER • Costs €" + r.machinePrice + " • earn €" + (r.machinePrice - s.money) + " more by selling copper");
                        return new StationHint(true, "POWERED STRIPPER • " + use + "buy for €" + r.machinePrice);
                    }
                    if (s.machineRemaining > 0) return new StationHint(false, "POWERED STRIPPER • Processing • " + s.machineRemaining.ToString("0.0") + "s left • wait for the output tray");
                    if (s.machineOutput > 0) return OutputHint(m, "OUTPUT TRAY", s.machineOutput, use, emptyHands);
                    if (carried == null) return new StationHint(false, "POWERED STRIPPER • Idle. Take wire from DELIVERY, then feed the front opening.");
                    if (carried.kind != MaterialKind.Wire) return new StationHint(false, "POWERED STRIPPER • Copper is finished material. Sell it at the buyer.");
                    return new StationHint(true, "POWERED STRIPPER • " + use + "feed wire into the front opening");
                default: return new StationHint(false, "Look at a station within reach.");
            }
        }
        static StationHint OutputHint(YardModel m, string station, int quantity, string use, string emptyHands)
        {
            if (m.Carried != null) return new StationHint(false, station + " • Copper ready. " + emptyHands);
            if (m.State.items.Count >= m.Capacity || m.State.nextId == int.MaxValue) return new StationHint(false, station + " • Storage full. Sell an existing copper bundle first.");
            return new StationHint(true, station + " • " + use + "collect " + quantity + " copper");
        }
        public static string Objective(YardModel m, string interact, string work, string drop)
        {
            var s = m.State; var c = m.Carried; var r = m.Rules;
            if (c != null && c.kind == MaterialKind.BrokenFan) return "Place the fan on the RESTORATION BENCH [" + interact + "] to inspect it.";
            if (c != null && c.kind == MaterialKind.RestoredFan) return "Sell your tested fan at the BUYER [" + interact + "] • €" + r.fanSalePrice + ".";
            if (s.orderAccepted && c != null && c.kind == MaterialKind.Copper) return "Deliver copper to the CUSTOMER BOARD [" + interact + "] • " + s.orderDelivered + "/" + m.CurrentOrder.copper + ".";
            if (c != null && c.kind == MaterialKind.Copper) return "Sell your copper at the BUYER [" + interact + "].";
            if (!s.machineOwned && s.money >= r.machinePrice) return "Buy the POWERED STRIPPER for €" + r.machinePrice + " [" + interact + "].";
            if (c != null)
            {
                if (s.machineOwned && s.machineRemaining == 0 && s.machineOutput == 0) return "Feed wire into the STRIPPER's front opening [" + interact + "].";
                if (!s.benchLoaded && s.benchOutput == 0) return "Place wire on the WORKBENCH [" + interact + "].";
                return "Drop the spare wire [" + drop + "] to free your hands for the station.";
            }
            if (s.benchOutput > 0) return "Collect copper from the WORKBENCH [" + interact + "], then sell it.";
            if (s.machineOutput > 0) return "Collect copper from the STRIPPER output tray [" + interact + "], then sell it.";
            if (s.benchLoaded) return "Strip the loaded wire at the WORKBENCH [" + work + "] • " + s.benchStrokes + "/" + m.WireWorkSteps + ".";
            if (s.machineRemaining > 0) return "The STRIPPER is working. Wait for copper or use the manual bench.";
            if (s.fanStage == FanStage.ReadyToTest) return "Power on and test your repaired fan at the RESTORATION BENCH [" + interact + "].";
            if (s.fanStage == FanStage.Tested || s.fanStage == FanStage.CopperReady) return "Collect your finished restoration-bench output [" + interact + "].";
            if (s.fanStage == FanStage.Repairing || s.fanStage == FanStage.Dismantling) return "Work on the fan at the RESTORATION BENCH [" + work + "].";
            if (s.fanStage == FanStage.AwaitingInspection || s.fanStage == FanStage.Diagnosed) return "Inspect the RESTORATION BENCH [" + interact + "] and choose repair or salvage.";
            if (s.orderAccepted && m.StoredBundles(MaterialKind.Copper) > 0) return "Take copper from COPPER STORAGE [" + interact + "] for your customer order.";
            if (m.StoredBundles(MaterialKind.Wire) > 0) return "Take wire from WIRE STORAGE [" + interact + "] to process it.";
            if (s.orderAccepted) return "Strip wire for " + m.CurrentOrder.customer + " • " + s.orderDelivered + "/" + m.CurrentOrder.copper + " copper delivered.";
            if (s.machineOwned && (m.CanBuyUpgrade(YardUpgrade.StorageRack) || m.CanBuyUpgrade(YardUpgrade.HandTools) || m.CanBuyUpgrade(YardUpgrade.MachineTuning)))
                return "Improve your yard at the YARD DIARY [" + interact + "] • Investments.";
            if (s.machineOwned) return "Feed your powered stripper with DELIVERY wire, or restore a fan from APPLIANCE SALVAGE.";
            return "Take wire from DELIVERY [" + interact + "]. Strip and sell it; €" + (r.machinePrice - s.money) + " to your first machine.";
        }
    }
}
