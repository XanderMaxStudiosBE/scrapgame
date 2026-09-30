namespace Scrapshift
{
    public enum TargetKind { Supply, Bench, Machine, Sell, LooseItem }

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
                case TargetKind.Supply:
                    if (carried != null) return new StationHint(false, "DELIVERY • Hands full. " + emptyHands);
                    int occupied = s.items.Count + (s.benchLoaded || s.benchOutput > 0 ? 1 : 0) + (s.machineRemaining > 0 || s.machineOutput > 0 ? 1 : 0);
                    if (occupied >= r.maxBundles) return new StationHint(false, "DELIVERY • Yard storage full. Process or sell existing bundles.");
                    return new StationHint(true, "DELIVERY • " + use + "take free scrap wire");
                case TargetKind.LooseItem:
                    var item = m.Find(itemId);
                    if (item == null) return new StationHint(false, "This bundle is no longer here.");
                    string description = item.kind + " ×" + item.quantity + (item.kind == MaterialKind.Copper ? " • worth €" + (long)item.quantity * r.copperUnitPrice : " • strip to recover " + r.copperPerWire + " copper");
                    return new StationHint(carried == null, description + " • " + (carried == null ? use + "pick up" : "Hands full. " + emptyHands));
                case TargetKind.Sell:
                    if (carried == null) return new StationHint(false, "COPPER BUYER • Carry copper here to sell it.");
                    if (carried.kind != MaterialKind.Copper) return new StationHint(false, "COPPER BUYER • Wire must be stripped at the bench or machine first.");
                    long value = (long)carried.quantity * r.copperUnitPrice;
                    if ((long)s.money + value > int.MaxValue) return new StationHint(false, "COPPER BUYER • Balance limit reached; bundle retained.");
                    return new StationHint(true, "COPPER BUYER • " + use + "sell " + carried.quantity + " copper for €" + value);
                case TargetKind.Bench:
                    if (s.benchOutput > 0) return OutputHint(m, "WORKBENCH", s.benchOutput, use, emptyHands);
                    if (s.benchLoaded)
                        return carried == null ? new StationHint(true, "WORKBENCH • [" + work + "] strip wire • " + s.benchStrokes + "/" + r.manualStrokes + " strokes") :
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
            if (m.State.items.Count >= m.Rules.maxBundles) return new StationHint(false, station + " • Storage full. Sell an existing copper bundle first.");
            return new StationHint(true, station + " • " + use + "collect " + quantity + " copper");
        }
        public static string Objective(YardModel m, string interact, string work, string drop)
        {
            var s = m.State; var c = m.Carried; var r = m.Rules;
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
            if (s.benchLoaded) return "Strip the loaded wire at the WORKBENCH [" + work + "] • " + s.benchStrokes + "/" + r.manualStrokes + ".";
            if (s.machineRemaining > 0) return "The STRIPPER is working. Wait for copper or use the manual bench.";
            if (s.machineOwned) return "Take wire from DELIVERY [" + interact + "] to feed your powered stripper.";
            return "Take wire from DELIVERY [" + interact + "]. Strip and sell it; €" + (r.machinePrice - s.money) + " to your first machine.";
        }
    }
}
