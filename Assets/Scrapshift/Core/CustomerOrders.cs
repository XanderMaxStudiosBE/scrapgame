namespace Scrapshift
{
    public sealed class CustomerOrder
    {
        public readonly string customer, title, note;
        public readonly int copper, reward;
        public CustomerOrder(string customer, string title, string note, int copper, int reward)
        { this.customer = customer; this.title = title; this.note = note; this.copper = copper; this.reward = reward; }
    }

    // A compact repeating board; no deadlines, penalties, randomness or changing save identifiers.
    public static class CustomerOrders
    {
        static readonly CustomerOrder[] Orders =
        {
            new CustomerOrder("Corner Repair Shop", "A little copper goes a long way", "We are fixing the neighbourhood's old lamps. Clean copper would help.", 3, 18),
            new CustomerOrder("Mira's Workshop", "Restock the workbench", "My next batch of repairs needs a small supply of recovered copper.", 6, 34),
            new CustomerOrder("Community Garden", "Something useful from something old", "We are building plant labels and small fittings. Nothing needs to be new.", 9, 50),
            new CustomerOrder("Canal Street Electric", "Weekend repair supply", "A tidy lot of copper for a busy weekend at the shop, please.", 12, 66),
            new CustomerOrder("Local Makers Club", "A bigger batch", "Our members are making a shared project. We would love to use salvaged material.", 18, 98)
        };
        public static CustomerOrder At(int completed)
        {
            if (completed < 0) throw new System.ArgumentOutOfRangeException("completed");
            return Orders[completed % Orders.Length];
        }
    }
}
