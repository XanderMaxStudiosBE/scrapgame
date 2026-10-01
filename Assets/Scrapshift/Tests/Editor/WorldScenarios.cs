using System;
namespace Scrapshift.Tests
{
    public static class WorldScenarios
    {
        public static readonly string[] Names = { "ExpandedBoundsKeepOldAndNewPositions", "WorldDistrictNavigation", "RemoteSalvageUsesExistingInventory" };
        public static void Run(string scenario)
        {
            switch (scenario)
            {
                case "ExpandedBoundsKeepOldAndNewPositions":
                    foreach (float x in new[] { -10f, 0, 10, -30, 31, -46, 46 })
                        Check(YardWorldLayout.ClampX(x) == x, "valid old or expanded X must survive load/drop");
                    foreach (float z in new[] { -8f, 0, 8, 19, -22, -38, 38 })
                        Check(YardWorldLayout.ClampZ(z) == z, "valid old or expanded Z must survive load/drop");
                    Check(YardWorldLayout.ClampX(999) == YardWorldLayout.SafeX && YardWorldLayout.ClampX(-999) == -YardWorldLayout.SafeX, "X perimeter guard");
                    Check(YardWorldLayout.ClampZ(999) == YardWorldLayout.SafeZ && YardWorldLayout.ClampZ(-999) == -YardWorldLayout.SafeZ, "Z perimeter guard");
                    break;
                case "WorldDistrictNavigation":
                    Check(YardWorldLayout.Area(0, -6) == "Workshop yard", "old spawn remains hub");
                    Check(YardWorldLayout.Area(-30, -6) == "Vehicle salvage", "west source");
                    Check(YardWorldLayout.Area(-33, 19) == "Vehicle salvage", "northwest source");
                    Check(YardWorldLayout.Area(31, -22) == "Metal sorting", "east source");
                    Check(YardWorldLayout.Area(0, 30) == "Loading & container storage", "north");
                    Check(YardWorldLayout.Area(-16, -30) == "Entry & yard office", "south");
                    break;
                case "RemoteSalvageUsesExistingInventory":
                    var m = new YardModel(new YardRules());
                    Check(m.AcquireWire(), "shared source transaction");
                    Check(!m.AcquireWire(), "another source cannot bypass hands-full guard");
                    int id = m.Carried.id;
                    Check(m.Drop(-30, .25f, -6), "drop in expanded yard");
                    var restored = new YardModel(m.Rules, m.State);
                    Check(restored.Find(id).x == -30 && restored.Find(id).z == -6, "version-one state retains remote item");
                    Check(restored.PickUp(id) && restored.LoadBench(), "remote wire works in existing bench");
                    for (int i = 0; i < restored.Rules.manualStrokes; i++) Check(restored.WorkBench(), "strip");
                    Check(restored.CollectBench() && restored.Sell(), "sell");
                    Check(restored.State.money == 12 && restored.State.items.Count == 0, "no duplicated material or money");
                    break;
                default: throw new ArgumentException(scenario);
            }
        }
        static void Check(bool condition, string detail) { if (!condition) throw new Exception(detail); }
    }
}
