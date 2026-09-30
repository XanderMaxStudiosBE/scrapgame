using System;
using System.Collections.Generic;

namespace Scrapshift
{
    // All material/economy mutations happen here; presentation never creates outputs.
    public sealed class YardModel
    {
        public YardState State { get; private set; }
        public YardRules Rules { get; private set; }
        public ScrapItem Carried { get { return Find(State.carriedId); } }
        public YardModel(YardRules rules, YardState state = null)
        {
            rules.Validate();
            Rules = rules;
            State = state ?? new YardState();
            Validate(State);
        }
        public ScrapItem Find(int id) { return State.items.Find(item => item.id == id); }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        public static void Validate(YardState s)
        {
            if (s == null || s.version != 1 || s.items == null || s.items.Count > 100 || s.money < 0 || s.nextId < 1 ||
                s.benchStrokes < 0 || s.benchStrokes > 100 || s.benchOutput < 0 || s.benchOutput > 100 ||
                s.machineOutput < 0 || s.machineOutput > 100 || s.machinePendingYield < 0 || s.machinePendingYield > 100 ||
                !Finite(s.machineRemaining) || s.machineRemaining < 0 || s.carriedId < 0 ||
                !Finite(s.playerX) || !Finite(s.playerY) || !Finite(s.playerZ) || !Finite(s.yaw) || !Finite(s.pitch))
                throw new ArgumentException("Invalid save data.");
            if ((!s.benchLoaded && s.benchStrokes != 0) || (s.benchLoaded && s.benchOutput != 0) ||
                ((s.machineRemaining > 0) != (s.machinePendingYield > 0)) ||
                (s.machineRemaining > 0 && s.machineOutput != 0) ||
                (!s.machineOwned && (s.machineRemaining != 0 || s.machineOutput != 0)))
                throw new ArgumentException("Inconsistent station state.");
            var ids = new HashSet<int>();
            foreach (var item in s.items)
                if (item == null || item.id < 1 || item.id >= s.nextId || !ids.Add(item.id) ||
                    (item.kind != MaterialKind.Wire && item.kind != MaterialKind.Copper) || item.quantity < 1 || item.quantity > 100 ||
                    (item.kind == MaterialKind.Wire && item.quantity != 1) || !Finite(item.x) || !Finite(item.y) || !Finite(item.z))
                    throw new ArgumentException("Invalid item state.");
            if (s.carriedId != 0 && !ids.Contains(s.carriedId)) throw new ArgumentException("Missing carried item.");
        }
        ScrapItem Create(MaterialKind kind, int quantity)
        {
            if (State.nextId == int.MaxValue) throw new InvalidOperationException("Item ID limit reached.");
            var item = new ScrapItem { id = State.nextId++, kind = kind, quantity = quantity };
            State.items.Add(item);
            State.carriedId = item.id;
            return item;
        }
        public bool AcquireWire()
        {
            int occupied = State.items.Count + (State.benchLoaded || State.benchOutput > 0 ? 1 : 0) + (State.machineRemaining > 0 || State.machineOutput > 0 ? 1 : 0);
            if (Carried != null || occupied >= Rules.maxBundles) return false;
            Create(MaterialKind.Wire, 1); return true;
        }
        public bool PickUp(int id)
        {
            if (Carried != null || Find(id) == null) return false;
            State.carriedId = id; return true;
        }
        public bool Drop(float x, float y, float z)
        {
            var item = Carried;
            if (item == null || !Finite(x) || !Finite(y) || !Finite(z)) return false;
            item.x = x; item.y = y; item.z = z; State.carriedId = 0; return true;
        }
        bool ConsumeWire()
        {
            var item = Carried;
            if (item == null || item.kind != MaterialKind.Wire) return false;
            State.items.Remove(item); State.carriedId = 0; return true;
        }
        public bool LoadBench()
        {
            if (State.benchLoaded || State.benchOutput != 0 || !ConsumeWire()) return false;
            State.benchLoaded = true; State.benchStrokes = 0; return true;
        }
        public bool WorkBench()
        {
            if (!State.benchLoaded || Carried != null) return false;
            State.benchStrokes++;
            if (State.benchStrokes >= Rules.manualStrokes)
            {
                State.benchLoaded = false; State.benchStrokes = 0; State.benchOutput = Rules.copperPerWire;
            }
            return true;
        }
        public bool CollectBench()
        {
            if (Carried != null || State.benchOutput == 0 || State.items.Count >= Rules.maxBundles) return false;
            Create(MaterialKind.Copper, State.benchOutput); State.benchOutput = 0; return true;
        }
        public bool Sell()
        {
            var item = Carried;
            if (item == null || item.kind != MaterialKind.Copper) return false;
            long money = (long)State.money + (long)item.quantity * Rules.copperUnitPrice;
            if (money > int.MaxValue) return false;
            State.money = (int)money; State.items.Remove(item); State.carriedId = 0; return true;
        }
        public bool BuyMachine()
        {
            if (State.machineOwned || State.money < Rules.machinePrice) return false;
            State.money -= Rules.machinePrice; State.machineOwned = true; return true;
        }
        public bool FeedMachine()
        {
            if (!State.machineOwned || State.machineRemaining > 0 || State.machineOutput != 0 || !ConsumeWire()) return false;
            State.machineRemaining = Rules.machineSeconds; State.machinePendingYield = Rules.copperPerWire; return true;
        }
        public void Tick(float seconds)
        {
            if (!Finite(seconds) || seconds <= 0 || State.machineRemaining <= 0) return;
            State.machineRemaining = Math.Max(0, State.machineRemaining - seconds);
            if (State.machineRemaining == 0)
            {
                State.machineOutput = State.machinePendingYield; State.machinePendingYield = 0;
            }
        }
        public bool CollectMachine()
        {
            if (Carried != null || State.machineOutput == 0 || State.items.Count >= Rules.maxBundles) return false;
            Create(MaterialKind.Copper, State.machineOutput); State.machineOutput = 0; return true;
        }
    }
}
