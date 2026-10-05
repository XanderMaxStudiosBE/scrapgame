using System;
using System.Collections.Generic;

namespace Scrapshift.Compact
{
    public sealed class CompactPowerStatus
    {
        public bool connected, powered, overloaded;
        public float supply, demand;
        public string reason;
    }

    // All ownership and power changes happen here; placement previews are read-only.
    public sealed class ConstructionModel
    {
        public const float HalfWidth = 24, HalfDepth = 18, Clearance = .15f;
        public const int MaximumEquipment = 128, MaximumLinks = 256;
        readonly CompactYardState state;
        readonly CompactRules rules;
        public string LastMessage { get; private set; }
        public int LastPlacedId { get; private set; }
        public int LastRefund { get; private set; }
        public ConstructionModel(CompactYardState state, CompactRules rules)
        {
            if (state == null || rules == null) throw new ArgumentNullException("state/rules");
            this.state = state; this.rules = rules; LastMessage = "";
        }
        public CompactYardState State { get { return state; } }
        public CompactRules Rules { get { return rules; } }
        public EquipmentState Find(int id)
        {
            foreach (var item in state.equipment) if (item.id == id) return item;
            return null;
        }
        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        static bool Supported(EquipmentDefinition d)
        {
            return d != null && Finite(d.width) && Finite(d.depth) && d.width > 0 && d.depth > 0 &&
                d.width <= HalfWidth * 2 && d.depth <= HalfDepth * 2;
        }
        static float Angle(float yaw) { return (yaw % 360 + 360) % 360; }
        static bool Refuse(string message, out string reason) { reason = message; return false; }
        bool Refuse(string message) { LastMessage = message; return false; }
        public bool CanPlace(EquipmentKind kind, float x, float z, float yaw, int ignoreId, out string reason)
        {
            var d = rules.Equipment(kind);
            if (kind == EquipmentKind.Conveyor)
                return Refuse("Build conveyors between equipment ports.", out reason);
            if (!Supported(d)) return Refuse("Equipment footprint is unavailable.", out reason);
            if (!Finite(x) || !Finite(z) || !Finite(yaw) || Math.Abs(yaw) > 360000)
                return Refuse("Choose a finite yard position and rotation.", out reason);
            if (ignoreId == 0)
            {
                if (!d.available) return Refuse("This production stage is planned and cannot be purchased yet.", out reason);
                if (rules.LevelForExperience(state.experience) < d.unlockLevel)
                    return Refuse("Requires level " + d.unlockLevel + ".", out reason);
                if (d.price < 0 || state.money < d.price) return Refuse("Not enough money: costs €" + d.price + ".", out reason);
                if (state.equipment.Count >= Math.Min(MaximumEquipment, rules.maxEquipment)) return Refuse("Equipment limit reached.", out reason);
                if (state.nextId <= 0 || state.nextId == int.MaxValue) return Refuse("No safe equipment identity available.", out reason);
            }
            else
            {
                var existing = Find(ignoreId);
                if (existing == null || existing.kind != kind) return Refuse("That equipment no longer exists.", out reason);
                if (!CanMove(ignoreId, out reason)) return false;
            }
            var candidate = new Footprint(x, z, d.width + Clearance, d.depth + Clearance, yaw);
            if (!candidate.InBounds()) return Refuse("Keep the whole footprint inside the fence.", out reason);
            if (Protected(candidate, out reason)) return false;
            foreach (var other in state.equipment)
            {
                if (other.id == ignoreId) continue;
                var definition = rules.Equipment(other.kind);
                if (!Supported(definition)) return Refuse("An existing equipment footprint is invalid.", out reason);
                if (candidate.Overlaps(new Footprint(other.x, other.z, definition.width + Clearance, definition.depth + Clearance, other.yaw)))
                    return Refuse("Blocked by " + definition.name + ".", out reason);
            }
            foreach (var scrap in state.scrap)
            {
                float width = scrap.kind == ScrapObjectKind.Car ? 2.4f : 1.2f;
                float depth = scrap.kind == ScrapObjectKind.Car ? 4.7f : 1.2f;
                if (candidate.Overlaps(new Footprint(scrap.x, scrap.z, width, depth, 0)))
                    return Refuse("Keep dismantling scrap accessible.", out reason);
            }
            foreach (var item in state.items)
                if (item.id != state.carriedId && candidate.Overlaps(new Footprint(item.x, item.z, .8f, .8f, 0)))
                    return Refuse("Move the loose component before building here.", out reason);
            if (state.belts != null) foreach (var belt in state.belts)
            {
                var path = AutomationModel.Path(belt,state,rules);
                for (int i = 1; i < path.Length; i++)
                    if (candidate.Overlaps(AutomationModel.Segment(path[i-1],path[i])))
                        return Refuse("Disconnect the empty conveyor before building over its route.",out reason);
            }
            reason = "Clear footprint"; return true;
        }
        internal static bool Protected(Footprint candidate, out string reason)
        {
            if (candidate.Overlaps(new Footprint(0, -14.5f, 4, 7, 0)))
            { reason = "Keep the entrance corridor clear."; return true; }
            if (candidate.Overlaps(new Footprint(-17, -13.5f, 12, 7, 0)))
            { reason = "Keep office, shop and sales access clear."; return true; }
            if (candidate.Overlaps(new Footprint(18, -10, 10, 14, 0)))
            { reason = "Keep the scrap delivery area clear."; return true; }
            reason = ""; return false;
        }
        public bool Place(EquipmentKind kind, float x, float z, float yaw)
        {
            string reason; if (!CanPlace(kind, x, z, yaw, 0, out reason)) return Refuse(reason);
            var d = rules.Equipment(kind);
            // Validation is complete before changing money, identity or ownership.
            var item = new EquipmentState { id = state.nextId, kind = kind, x = x, z = z, yaw = Angle(yaw), paidPrice = d.price };
            state.money -= d.price; state.nextId++; state.equipment.Add(item);
            LastPlacedId = item.id; LastMessage = d.name + " placed for €" + d.price + "."; return true;
        }
        public bool CanMove(int id, out string reason)
        {
            var item = Find(id);
            if (item == null) return Refuse("That equipment no longer exists.", out reason);
            if (item.industry != null && (item.industry.enabled || item.industry.primary != null))
                return Refuse("Disable automatic intake or dispatch and finish the whole-object job before moving or dismantling it.", out reason);
            if (item.job != null || item.contents == null || item.contents.Count > 0)
                return Refuse("Collect all output and empty the equipment before moving or dismantling it.", out reason);
            foreach (var link in state.powerLinks)
                if (link.a == id || link.b == id) return Refuse("Disconnect power cables before moving or dismantling it.", out reason);
            if (state.belts != null) foreach (var belt in state.belts)
                if (belt.fromId == id || belt.toId == id) return Refuse("Disconnect empty conveyors before moving or dismantling it.",out reason);
            reason = "Empty and disconnected"; return true;
        }
        public bool Move(int id, float x, float z, float yaw)
        {
            var item = Find(id);
            if (item == null) return Refuse("That equipment no longer exists.");
            string reason; if (!CanPlace(item.kind, x, z, yaw, id, out reason)) return Refuse(reason);
            item.x = x; item.z = z; item.yaw = Angle(yaw);
            LastMessage = rules.Equipment(item.kind).name + " moved."; return true;
        }
        public int RefundFor(int id)
        {
            var item = Find(id); return item == null || item.starter ? 0 : Math.Max(0, item.paidPrice / 2);
        }
        public bool CanRemove(int id, out string reason)
        {
            if (!CanMove(id, out reason)) return false;
            var item = Find(id);
            if (item.kind == EquipmentKind.Workbench)
            {
                int benches = 0; foreach (var owned in state.equipment) if (owned.kind == EquipmentKind.Workbench) benches++;
                if (benches < 2) return Refuse("Keep one manual workbench so the renewable scrap loop remains available; you can move it.", out reason);
            }
            if ((long)state.money + RefundFor(id) > int.MaxValue) return Refuse("Cash limit prevents this refund.", out reason);
            reason = "Refund €" + RefundFor(id); return true;
        }
        public bool Remove(int id)
        {
            string reason; if (!CanRemove(id, out reason)) return Refuse(reason);
            int refund = RefundFor(id); var item = Find(id);
            state.equipment.Remove(item); state.money += refund; LastRefund = refund;
            LastMessage = "Equipment dismantled; refund €" + refund + "."; return true;
        }
        bool HasPort(EquipmentState item)
        {
            var d = item == null ? null : rules.Equipment(item.kind);
            return d != null && (d.powerOutput > 0 || d.powerDemand > 0);
        }
        static float Distance(EquipmentState a, EquipmentState b)
        {
            double dx = a.x - b.x, dz = a.z - b.z; return (float)Math.Sqrt(dx * dx + dz * dz);
        }
        public bool CanConnect(int a, int b, out string reason)
        {
            var first = Find(a); var second = Find(b);
            if (a == b || !HasPort(first) || !HasPort(second)) return Refuse("Select two different generator or powered-machine ports.", out reason);
            if (!Finite(rules.CableRange) || rules.CableRange <= 0 || Distance(first, second) > rules.CableRange)
                return Refuse("Cable exceeds the " + rules.CableRange + "m connection range.", out reason);
            foreach (var link in state.powerLinks)
                if ((link.a == a && link.b == b) || (link.a == b && link.b == a)) return Refuse("These ports are already connected.", out reason);
            if (state.powerLinks.Count >= MaximumLinks) return Refuse("Cable limit reached.", out reason);
            reason = "Connect power cable"; return true;
        }
        public bool Connect(int a, int b)
        {
            string reason; if (!CanConnect(a, b, out reason)) return Refuse(reason);
            state.powerLinks.Add(new PowerLink(Math.Min(a, b), Math.Max(a, b)));
            LastMessage = "Power cable connected."; return true;
        }
        public bool CanDisconnect(int a, int b, out string reason)
        {
            foreach (var link in state.powerLinks)
                if ((link.a == a && link.b == b) || (link.a == b && link.b == a)) { reason = "Disconnect power cable"; return true; }
            return Refuse("These ports are not connected.", out reason);
        }
        public bool Disconnect(int a, int b)
        {
            for (int i = 0; i < state.powerLinks.Count; i++)
            {
                var link = state.powerLinks[i];
                if ((link.a == a && link.b == b) || (link.a == b && link.b == a))
                { state.powerLinks.RemoveAt(i); LastMessage = "Power cable disconnected; processing progress is preserved."; return true; }
            }
            return Refuse("These ports are not connected.");
        }
        public CompactPowerStatus PowerFor(int id)
        {
            var item = Find(id); var result = new CompactPowerStatus();
            if (!HasPort(item)) { result.reason = "Manual equipment does not require power."; return result; }
            var network = new HashSet<int> { id }; var pending = new Queue<int>(); pending.Enqueue(id);
            while (pending.Count > 0)
            {
                int current = pending.Dequeue();
                foreach (var link in state.powerLinks)
                {
                    int next = link.a == current ? link.b : link.b == current ? link.a : 0;
                    if (next != 0 && network.Add(next)) pending.Enqueue(next);
                }
            }
            // Stable identity ordering makes sums independent of saved connection order.
            var ordered = new List<int>(network); ordered.Sort(); double supply = 0, demand = 0;
            foreach (int member in ordered)
            {
                var equipment = Find(member); var d = equipment == null ? null : rules.Equipment(equipment.kind);
                if (d != null) { supply += d.powerOutput; demand += d.powerDemand; }
            }
            result.supply = (float)supply; result.demand = (float)demand;
            result.connected = network.Count > 1;
            result.overloaded = supply > 0 && demand > supply + .0001;
            result.powered = supply > 0 && !result.overloaded;
            if (supply <= 0) result.reason = result.connected ? "No generator on this network; processing paused." : "Unconnected; connect a generator to begin processing.";
            else if (result.overloaded) result.reason = "Network overloaded: " + result.demand.ToString("0.##") + " / " + result.supply.ToString("0.##") + " kW; all connected machines paused.";
            else result.reason = "Network power " + result.demand.ToString("0.##") + " / " + result.supply.ToString("0.##") + " kW" + (result.connected ? "." : "; no cables connected.");
            return result;
        }
        public static void Validate(CompactYardState state, CompactRules rules)
        {
            if (state == null || rules == null || state.equipment == null || state.powerLinks == null ||
                state.equipment.Count > MaximumEquipment || state.powerLinks.Count > MaximumLinks)
                throw new ArgumentException("Invalid construction state.");
            var model = new ConstructionModel(state, rules); var identities = new HashSet<int>();
            foreach (var item in state.equipment)
            {
                if (item == null || item.id <= 0 || !identities.Add(item.id) || !Finite(item.x) || !Finite(item.z) ||
                    !Finite(item.yaw) || item.yaw < 0 || item.yaw >= 360 || item.paidPrice < 0 || item.contents == null)
                    throw new ArgumentException("Invalid equipment placement metadata.");
                var d = rules.Equipment(item.kind);
                if (!Supported(d) || !Finite(d.powerOutput) || !Finite(d.powerDemand) || d.powerOutput < 0 || d.powerDemand < 0)
                    throw new ArgumentException("Invalid equipment definition.");
                var footprint = new Footprint(item.x, item.z, d.width + Clearance, d.depth + Clearance, item.yaw); string reason;
                if (!footprint.InBounds() || Protected(footprint, out reason)) throw new ArgumentException("Invalid saved equipment position.");
            }
            for (int i = 0; i < state.equipment.Count; i++)
                for (int j = i + 1; j < state.equipment.Count; j++)
                {
                    var a = state.equipment[i]; var b = state.equipment[j];
                    var da = rules.Equipment(a.kind); var db = rules.Equipment(b.kind);
                    if (new Footprint(a.x, a.z, da.width + Clearance, da.depth + Clearance, a.yaw).Overlaps(
                        new Footprint(b.x, b.z, db.width + Clearance, db.depth + Clearance, b.yaw)))
                        throw new ArgumentException("Saved equipment footprints overlap.");
                }
            var pairs = new HashSet<string>();
            foreach (var link in state.powerLinks)
            {
                if (link == null || link.a == link.b || !model.HasPort(model.Find(link.a)) || !model.HasPort(model.Find(link.b)) ||
                    !Finite(rules.CableRange) || rules.CableRange <= 0 || Distance(model.Find(link.a), model.Find(link.b)) > rules.CableRange ||
                    !pairs.Add(Math.Min(link.a, link.b) + ":" + Math.Max(link.a, link.b)))
                    throw new ArgumentException("Invalid saved power connection.");
            }
        }
        internal struct Footprint
        {
            readonly float x, z, hx, hz, c, s;
            public Footprint(float x, float z, float width, float depth, float yaw)
            {
                this.x = x; this.z = z; hx = width * .5f; hz = depth * .5f;
                double a = yaw * Math.PI / 180; c = (float)Math.Cos(a); s = (float)Math.Sin(a);
            }
            public bool InBounds()
            {
                float ex = Math.Abs(c) * hx + Math.Abs(s) * hz, ez = Math.Abs(s) * hx + Math.Abs(c) * hz;
                return Math.Abs(x) + ex <= HalfWidth && Math.Abs(z) + ez <= HalfDepth;
            }
            bool Separated(Footprint other, float ax, float az)
            {
                float distance = Math.Abs((other.x - x) * ax + (other.z - z) * az);
                float radius = hx * Math.Abs(c * ax - s * az) + hz * Math.Abs(s * ax + c * az);
                float otherRadius = other.hx * Math.Abs(other.c * ax - other.s * az) + other.hz * Math.Abs(other.s * ax + other.c * az);
                return distance >= radius + otherRadius - .00001f;
            }
            public bool Overlaps(Footprint other)
            {
                return !Separated(other, c, -s) && !Separated(other, s, c) &&
                    !Separated(other, other.c, -other.s) && !Separated(other, other.s, other.c);
            }
        }
    }
}
