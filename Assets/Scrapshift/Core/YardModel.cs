using System;
using System.Collections.Generic;

namespace Scrapshift
{
    // All material/economy mutations happen here; presentation never creates outputs.
    public sealed partial class YardModel
    {
        public const int MaxSavedItems = 100;
        // Collecting replaces an already-reserved station slot, even after capacity is tuned down.
        public bool CanCollectOutput { get { return Carried == null && State.items.Count < MaxSavedItems && State.nextId < int.MaxValue; } }
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
        public ScrapItem Find(int id)
        {
            if (id <= 0) return null;
            for (int i = 0; i < State.items.Count; i++) if (State.items[i].id == id) return State.items[i];
            return null;
        }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        public static void Validate(YardState s)
        {
            if (s == null || s.version != 1 || s.items == null || s.items.Count > MaxSavedItems || s.money < 0 || s.nextId < 1 ||
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
            if (s.orderIndex < 0 || s.orderDelivered < 0 ||
                (!s.orderAccepted && s.orderDelivered != 0) ||
                (s.orderAccepted && (s.orderIndex == int.MaxValue || s.orderDelivered >= CustomerOrders.At(s.orderIndex).copper)))
                throw new ArgumentException("Inconsistent customer order.");
            if ((int)s.fanStage < 0 || (int)s.fanStage > (int)FanStage.CopperReady ||
                s.fanStrokes < 0 || s.fanStrokes > 100 || s.fansRepaired < 0 || s.fansDismantled < 0 ||
                s.dayIndex < 0 || s.fansTakenToday < 0 || s.fansTakenToday > 100 || s.incomeToday < 0 ||
                (s.fanStage != FanStage.Repairing && s.fanStage != FanStage.Dismantling && s.fanStrokes != 0))
                throw new ArgumentException("Invalid repair or working-day state.");
            if((int)s.benchAppliance<0 || (int)s.benchAppliance>1 || (int)s.benchFault<0 || (int)s.benchFault>2 ||
                s.radiosTakenToday<0 || s.radiosTakenToday>100 || s.radiosRepaired<0 || s.radiosDismantled<0 ||
                (s.fanStage==FanStage.Empty && (s.benchAppliance!=RepairAppliance.DeskFan || s.benchFault!=ApplianceFault.MainComponent)))
                throw new ArgumentException("Invalid appliance metadata.");
            if ((s.upgrades & ~(YardUpgrade.StorageRack | YardUpgrade.HandTools | YardUpgrade.MachineTuning)) != 0 ||
                ((s.upgrades & YardUpgrade.MachineTuning) != 0 && !s.machineOwned) || (s.milestones & ~YardJourney.Known)!=0)
                throw new ArgumentException("Invalid yard upgrade state.");
            var ids = new HashSet<int>();
            foreach (var item in s.items)
                if (item == null || item.id < 1 || item.id >= s.nextId || !ids.Add(item.id) ||
                    ((int)item.kind<0 || (int)item.kind>(int)MaterialKind.RestoredRadio) || item.quantity < 1 || item.quantity > 100 ||
                    ((int)item.applianceFault<0 || (int)item.applianceFault>2) || (!ApplianceRecipe.IsAppliance(item.kind) && item.applianceFault!=ApplianceFault.MainComponent) ||
                    (item.kind != MaterialKind.Copper && item.quantity != 1) ||
                    (item.storage != StorageSlot.None && item.storage != StorageSlot.Wire && item.storage != StorageSlot.Copper) ||
                    (item.storage == StorageSlot.Wire && item.kind != MaterialKind.Wire) ||
                    (item.storage == StorageSlot.Copper && item.kind != MaterialKind.Copper) ||
                    (item.id == s.carriedId && item.storage != StorageSlot.None) || !Finite(item.x) || !Finite(item.y) || !Finite(item.z))
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
            if (Carried != null || OccupiedBundles >= Capacity || State.nextId == int.MaxValue) return false;
            Create(MaterialKind.Wire, 1); return true;
        }
        public bool PickUp(int id)
        {
            var item = Find(id);
            if (Carried != null || item == null || item.storage != StorageSlot.None) return false;
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
            if (State.benchStrokes >= WireWorkSteps)
            {
                CompleteBenchWork();
            }
            return true;
        }
        void CompleteBenchWork()
        {
            State.benchLoaded = false; State.benchStrokes = 0; State.benchOutput = Rules.copperPerWire;
            State.milestones|=YardMilestone.RecoveredCopper;
        }
        public bool CollectBench()
        {
            if (!CanCollectOutput || State.benchOutput == 0) return false;
            Create(MaterialKind.Copper, State.benchOutput); State.benchOutput = 0; return true;
        }
        public bool Sell()
        {
            var item = Carried;
            if (item == null || (item.kind != MaterialKind.Copper && !ApplianceRecipe.IsRestored(item.kind))) return false;
            long value = SaleValue(item);
            long money = (long)State.money + value;
            if (money > int.MaxValue) return false;
            RecordIncome((int)value);
            State.money = (int)money; State.items.Remove(item); State.carriedId = 0; return true;
        }
        public int StoredBundles(MaterialKind kind)
        {
            if (kind != MaterialKind.Wire && kind != MaterialKind.Copper) return 0;
            StorageSlot slot = kind == MaterialKind.Wire ? StorageSlot.Wire : StorageSlot.Copper;
            int count = 0;
            foreach (var item in State.items) if (item.storage == slot) count++;
            return count;
        }
        public int StoredQuantity(MaterialKind kind)
        {
            if (kind != MaterialKind.Wire && kind != MaterialKind.Copper) return 0;
            StorageSlot slot = kind == MaterialKind.Wire ? StorageSlot.Wire : StorageSlot.Copper;
            int total = 0;
            foreach (var item in State.items) if (item.storage == slot) total += item.quantity;
            return total;
        }
        public bool Store(MaterialKind kind)
        {
            if (kind != MaterialKind.Wire && kind != MaterialKind.Copper) return false;
            var item = Carried;
            if (item == null || item.kind != kind) return false;
            item.storage = kind == MaterialKind.Wire ? StorageSlot.Wire : StorageSlot.Copper;
            State.carriedId = 0;
            return true;
        }
        public bool Retrieve(MaterialKind kind)
        {
            if (kind != MaterialKind.Wire && kind != MaterialKind.Copper) return false;
            if (Carried != null) return false;
            StorageSlot slot = kind == MaterialKind.Wire ? StorageSlot.Wire : StorageSlot.Copper;
            var item = State.items.Find(candidate => candidate.storage == slot);
            if (item == null) return false;
            item.storage = StorageSlot.None; State.carriedId = item.id;
            return true;
        }
        public CustomerOrder CurrentOrder { get { return CustomerOrders.At(State.orderIndex); } }
        public bool AcceptOrder()
        {
            if (State.orderAccepted || State.orderIndex == int.MaxValue) return false;
            State.orderAccepted = true; return true;
        }
        public bool DeliverOrder()
        {
            var item = Carried;
            if (!State.orderAccepted || item == null || item.kind != MaterialKind.Copper) return false;
            var order = CurrentOrder;
            int delivered = System.Math.Min(item.quantity, order.copper - State.orderDelivered);
            bool complete = State.orderDelivered + delivered == order.copper;
            long payment = (long)State.money + order.reward;
            // A failed payment never consumes the final material or advances the order.
            if (complete && payment > int.MaxValue) return false;
            item.quantity -= delivered;
            if (item.quantity == 0) { State.items.Remove(item); State.carriedId = 0; }
            if (complete)
            {
                RecordIncome(order.reward);
                State.money = (int)payment; State.orderIndex++;
                State.milestones|=YardMilestone.ServedCustomer;
                State.orderAccepted = false; State.orderDelivered = 0;
            }
            else State.orderDelivered += delivered;
            return true;
        }
        public bool BuyMachine()
        {
            if (State.machineOwned || State.money < Rules.machinePrice) return false;
            State.money -= Rules.machinePrice; State.machineOwned = true; State.milestones|=YardMilestone.PoweredYard; return true;
        }
        public bool FeedMachine()
        {
            if (!State.machineOwned || State.machineRemaining > 0 || State.machineOutput != 0 || !ConsumeWire()) return false;
            State.machineRemaining = MachineSeconds; State.machinePendingYield = Rules.copperPerWire; return true;
        }
        public void Tick(float seconds)
        {
            if (!Finite(seconds) || seconds <= 0 || State.machineRemaining <= 0) return;
            State.machineRemaining = Math.Max(0, State.machineRemaining - seconds);
            if (State.machineRemaining == 0)
            {
                State.machineOutput = State.machinePendingYield; State.machinePendingYield = 0;
                State.milestones|=YardMilestone.RecoveredCopper;
            }
        }
        public bool CollectMachine()
        {
            if (!CanCollectOutput || State.machineOutput == 0) return false;
            Create(MaterialKind.Copper, State.machineOutput); State.machineOutput = 0; return true;
        }
    }
}
