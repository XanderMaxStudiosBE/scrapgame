using System;

namespace Scrapshift.Tests
{
    // Shared by the Unity NUnit wrapper and the compiler-only Cloud runner.
    public static class CoreScenarios
    {
        public static readonly string[] Names = {
            "CompleteLoop", "CarryAndDrop", "InvalidInputs", "BenchExactlyOnce", "MachineExactlyOnce",
            "PurchaseGuards", "CapacityReservesOutputs", "PartialResume", "PausedTime", "InvalidSaves", "MoneyOverflow", "RenewableSupply"
        };
        static void Check(bool result, string message) { if (!result) throw new Exception(message); }
        static void Manual(YardModel m)
        {
            Check(m.AcquireWire(), "acquire"); Check(m.LoadBench(), "load");
            for (int i = 0; i < m.Rules.manualStrokes; i++) Check(m.WorkBench(), "stroke");
            Check(m.CollectBench(), "collect");
        }
        static void Reject(Action action)
        {
            try { action(); } catch (ArgumentException) { return; }
            throw new Exception("Invalid data was accepted");
        }
        public static void Run(string name)
        {
            var rules = new YardRules(); var m = new YardModel(rules);
            switch (name)
            {
                case "CompleteLoop":
                    for (int i = 0; i < 3; i++) { Manual(m); Check(m.Sell(), "sell"); }
                    Check(m.State.money == 36 && m.BuyMachine() && m.State.money == 0, "afford after three loads");
                    Check(m.AcquireWire() && m.FeedMachine(), "feed"); m.Tick(5);
                    Check(m.CollectMachine() && m.Sell() && m.State.money == 12, "automated sale"); break;
                case "CarryAndDrop":
                    Check(m.AcquireWire(), "acquire"); int id = m.Carried.id;
                    Check(!m.AcquireWire() && !m.PickUp(id), "hands busy");
                    Check(m.Drop(2, .3f, 4) && m.Carried == null, "drop");
                    Check(m.Find(id).x == 2 && m.Find(id).z == 4 && m.PickUp(id), "persistent position and pickup");
                    Check(!m.Drop(float.NaN, 0, 0), "invalid position"); break;
                case "InvalidInputs":
                    Check(!m.Sell() && !m.LoadBench() && !m.FeedMachine() && !m.CollectBench(), "empty hands");
                    m.AcquireWire(); Check(!m.Sell() && !m.FeedMachine() && m.Carried.kind == MaterialKind.Wire, "wire retained");
                    m.LoadBench(); for (int i = 0; i < 4; i++) m.WorkBench(); m.CollectBench();
                    Check(!m.LoadBench() && m.Carried.kind == MaterialKind.Copper, "wrong bench input retained");
                    m.State.machineOwned = true; Check(!m.FeedMachine() && m.Carried != null, "wrong machine input retained"); break;
                case "BenchExactlyOnce":
                    m.AcquireWire(); Check(m.LoadBench() && !m.LoadBench(), "single consumption");
                    for (int i = 0; i < 4; i++) m.WorkBench();
                    Check(!m.WorkBench() && m.State.benchOutput == 3, "bounded result");
                    Check(m.CollectBench() && !m.CollectBench() && m.Carried.quantity == 3, "single collect");
                    Check(m.Sell() && !m.Sell() && m.State.money == 12, "single sale"); break;
                case "MachineExactlyOnce":
                    m.State.machineOwned = true; m.AcquireWire(); m.FeedMachine(); m.AcquireWire();
                    Check(!m.FeedMachine() && m.Carried != null, "busy rejects without consumption");
                    m.Tick(500); m.Tick(500); Check(m.State.machineOutput == 3 && !m.CollectMachine(), "bounded and busy hands");
                    m.Drop(0, .3f, 0); Check(m.CollectMachine() && !m.CollectMachine(), "single collect");
                    Check(m.State.items.Count == 2 && m.Carried.quantity == 3, "material conservation"); break;
                case "PurchaseGuards":
                    Check(!m.BuyMachine() && m.State.money == 0, "unaffordable");
                    m.State.money = 35; Check(!m.BuyMachine() && m.State.money == 35, "one short");
                    m.State.money = 100; Check(m.BuyMachine() && !m.BuyMachine() && m.State.money == 64, "no double charge"); break;
                case "CapacityReservesOutputs":
                    rules.maxBundles = 2; m.AcquireWire(); m.LoadBench();
                    m.AcquireWire(); m.Drop(0, 0, 0); Check(!m.AcquireWire(), "station reserves capacity");
                    for (int i = 0; i < 4; i++) m.WorkBench();
                    Check(m.CollectBench() && m.Sell(), "output never blocked by renewable supply"); break;
                case "PartialResume":
                    m.State.machineOwned = true; m.AcquireWire(); m.FeedMachine(); m.Tick(2);
                    m.AcquireWire(); m.LoadBench(); m.WorkBench();
                    // Serialization round-trip is covered separately in Unity SaveStore tests.
                    var resumed = new YardModel(rules, m.State);
                    Check(resumed.State.machineRemaining == 3 && resumed.State.benchStrokes == 1, "partial state retained");
                    resumed.Tick(3); for (int i = 0; i < 3; i++) resumed.WorkBench();
                    Check(resumed.State.machineOutput == 3 && resumed.State.benchOutput == 3, "both finish once"); break;
                case "PausedTime":
                    m.State.machineOwned = true; m.AcquireWire(); m.FeedMachine();
                    m.Tick(0); m.Tick(-1); m.Tick(float.NaN); m.Tick(float.PositiveInfinity);
                    Check(m.State.machineRemaining == 5 && m.State.machineOutput == 0, "no progress for zero/invalid delta"); break;
                case "InvalidSaves":
                    Reject(() => new YardModel(rules, new YardState { money = -1 }));
                    Reject(() => new YardModel(rules, new YardState { carriedId = 1 }));
                    Reject(() => new YardModel(rules, new YardState { machineRemaining = 3 }));
                    Reject(() => new YardModel(rules, new YardState { benchLoaded = true, benchOutput = 3 }));
                    Reject(() => new YardModel(rules, new YardState { version = 99 }));
                    Reject(() => new YardModel(new YardRules { machineSeconds = float.NaN }));
                    m.AcquireWire(); m.State.items.Add(m.Carried); Reject(() => YardModel.Validate(m.State)); break;
                case "MoneyOverflow":
                    Manual(m); m.State.money = int.MaxValue; Check(!m.Sell() && m.Carried != null && m.State.money == int.MaxValue, "no overflow or loss"); break;
                case "RenewableSupply":
                    for (int i = 0; i < 100; i++) { Manual(m); m.Sell(); }
                    Check(m.State.money == 1200 && m.State.items.Count == 0 && m.AcquireWire(), "renewable and bounded"); break;
                default: throw new Exception("Unknown scenario: " + name);
            }
            // InvalidSaves deliberately corrupts its fixture.
            if (name != "InvalidSaves") YardModel.Validate(m.State);
        }
    }
}
