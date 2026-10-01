using System;
namespace Scrapshift.Tests
{
    public static class ProgressionScenarios
    {
        public static readonly string[] Names = { "StorageConservesItems", "StorageRespectsCapacity", "OrdersPayExactlyOnce", "PartialOrderAndSurplus", "ProgressionResume", "ProgressionInvalidStates", "OrderOverflowRetainsFinalCopper", "StorageOrderHints" };
        static void Check(bool result, string message) { if (!result) throw new Exception(message); }
        static void Copper(YardModel m)
        {
            Check(m.AcquireWire() && m.LoadBench(), "wire input");
            for (int i = 0; i < m.Rules.manualStrokes; i++) Check(m.WorkBench(), "stroke");
            Check(m.CollectBench(), "copper output");
        }
        static void Reject(YardState state)
        {
            try { YardModel.Validate(state); } catch (ArgumentException) { return; }
            throw new Exception("invalid progression accepted");
        }
        public static void Run(string name)
        {
            var m = new YardModel(new YardRules());
            switch (name)
            {
                case "StorageConservesItems":
                    m.AcquireWire(); int id = m.Carried.id;
                    Check(!m.Store(MaterialKind.Copper), "wrong bin keeps carried item");
                    Check(m.Store(MaterialKind.Wire) && !m.Store(MaterialKind.Wire), "one deposit");
                    Check(!m.PickUp(id), "stored item cannot be picked up through a world id");
                    Check(m.StoredBundles(MaterialKind.Wire) == 1 && m.StoredQuantity(MaterialKind.Wire) == 1 && m.State.items.Count == 1, "same item counted once");
                    Check(m.Retrieve(MaterialKind.Wire) && m.Carried.id == id && !m.Retrieve(MaterialKind.Wire), "retrieve original id once");
                    Check(m.StoredBundles(MaterialKind.Wire) == 0 && m.State.nextId == 2, "no invented item ids"); break;
                case "StorageRespectsCapacity":
                    m = new YardModel(new YardRules { maxBundles = 2 });
                    m.AcquireWire(); m.LoadBench(); m.AcquireWire(); m.Store(MaterialKind.Wire);
                    Check(!m.AcquireWire(), "stored bundle and bench reserve capacity");
                    for (int i = 0; i < 4; i++) m.WorkBench();
                    Check(m.CollectBench() && m.Store(MaterialKind.Copper), "output fits reserved slot");
                    Check(!m.AcquireWire(), "storage is not an unlimited capacity bypass");
                    Check(m.Retrieve(MaterialKind.Copper) && m.Sell() && m.AcquireWire(), "selling frees capacity"); break;
                case "OrdersPayExactlyOnce":
                    Check(!m.DeliverOrder() && m.AcceptOrder() && !m.AcceptOrder(), "accept once");
                    Copper(m); Check(m.DeliverOrder(), "complete first contract");
                    Check(m.State.money == 18 && m.State.orderIndex == 1 && !m.State.orderAccepted && m.State.orderDelivered == 0, "paid and advanced once");
                    Check(!m.DeliverOrder() && m.State.money == 18 && m.State.items.Count == 0, "no second payment");
                    Check(m.CurrentOrder.copper == 6 && m.AcceptOrder(), "next contract requires more work"); break;
                case "PartialOrderAndSurplus":
                    m.State.orderIndex = 1; m.AcceptOrder(); Copper(m); m.DeliverOrder();
                    Check(m.State.money == 0 && m.State.orderDelivered == 3 && m.State.orderAccepted, "partial material escrow without payment");
                    Copper(m); m.DeliverOrder(); Check(m.State.money == 34 && m.State.orderIndex == 2, "full payment once");
                    m.AcceptOrder(); m.State.orderDelivered = 8; Copper(m); int surplusId = m.Carried.id;
                    Check(m.DeliverOrder() && m.Carried.id == surplusId && m.Carried.quantity == 2 && m.State.money == 84, "consume only one needed unit; keep two in original bundle");
                    Check(m.Sell() && m.State.money == 92, "surplus still sellable"); break;
                case "ProgressionResume":
                    m.State.orderIndex = 1; m.AcceptOrder(); Copper(m); m.DeliverOrder();
                    m.AcquireWire(); m.Store(MaterialKind.Wire);
                    var restored = new YardModel(m.Rules, m.State);
                    Check(restored.State.orderAccepted && restored.State.orderDelivered == 3 && restored.StoredBundles(MaterialKind.Wire) == 1, "partial state retained");
                    restored.Retrieve(MaterialKind.Wire); restored.LoadBench();
                    for (int i = 0; i < 4; i++) restored.WorkBench();
                    restored.CollectBench(); restored.DeliverOrder();
                    Check(restored.State.money == 34 && restored.State.orderIndex == 2, "restored order finishes once"); break;
                case "ProgressionInvalidStates":
                    Reject(new YardState { orderDelivered = 1 });
                    Reject(new YardState { orderAccepted = true, orderDelivered = 3 });
                    Reject(new YardState { orderIndex = -1 });
                    Reject(new YardState { orderIndex = int.MaxValue, orderAccepted = true });
                    m.AcquireWire(); m.Carried.storage = StorageSlot.Wire; Reject(m.State);
                    m.State.carriedId = 0; m.State.items[0].storage = StorageSlot.Copper; Reject(m.State);
                    m.State.items[0].storage = (StorageSlot)99; Reject(m.State);
                    m.State.items.Clear(); m.State.orderIndex = int.MaxValue;
                    Check(!m.AcceptOrder(), "bounded sequence"); break;
                case "OrderOverflowRetainsFinalCopper":
                    m.State.orderIndex = 1; m.AcceptOrder(); Copper(m); m.DeliverOrder();
                    Copper(m); m.State.money = int.MaxValue - 33; int finalId = m.Carried.id;
                    Check(!m.DeliverOrder() && m.Carried.id == finalId && m.Carried.quantity == 3 && m.State.orderDelivered == 3, "failed final payment keeps final material and escrow");
                    m.State.money = int.MaxValue - 34;
                    Check(m.DeliverOrder() && m.State.money == int.MaxValue && m.State.orderIndex == 2, "exact limit succeeds"); break;
                case "StorageOrderHints":
                    m.AcquireWire();
                    Check(YardGuidance.Hint(m, TargetKind.WireStorage, 0, "F", "Mouse4").text.Contains("[F] store"), "rebound deposit prompt");
                    Check(!YardGuidance.Hint(m, TargetKind.CopperStorage, 0, "F", "Mouse4").canUse, "wrong-bin prompt");
                    m.Store(MaterialKind.Wire);
                    Check(YardGuidance.Objective(m, "F", "Mouse4", "R").Contains("WIRE STORAGE"), "stored stock guidance");
                    m.AcceptOrder(); m.Retrieve(MaterialKind.Wire); m.LoadBench();
                    for (int i = 0; i < 4; i++) m.WorkBench(); m.CollectBench();
                    Check(YardGuidance.Objective(m, "F", "Mouse4", "R").Contains("CUSTOMER BOARD [F]"), "contract guidance uses current binding"); break;
                default: throw new Exception(name);
            }
            YardModel.Validate(m.State);
        }
    }
}
