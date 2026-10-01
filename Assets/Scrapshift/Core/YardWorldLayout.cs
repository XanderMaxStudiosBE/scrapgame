namespace Scrapshift
{
    // Shared by terrain, player restoration, item placement and navigation.
    public static class YardWorldLayout
    {
        public const float HalfWidth = 48;
        public const float HalfDepth = 40;
        public const float SafeX = HalfWidth - 1.5f;
        public const float SafeZ = HalfDepth - 1.5f;
        public sealed class SalvageSite
        {
            public readonly float x, z;
            public readonly string name;
            public SalvageSite(float x, float z, string name) { this.x = x; this.z = z; this.name = name; }
        }
        public static readonly SalvageSite[] SalvageSites =
        {
            new SalvageSite(-30, -6, "VEHICLE SALVAGE"),
            new SalvageSite(-33, 19, "SALVAGE WIRE"),
            new SalvageSite(31, -22, "SORTING BAY")
        };
        public static float ClampX(float x) { return System.Math.Max(-SafeX, System.Math.Min(SafeX, x)); }
        public static float ClampZ(float z) { return System.Math.Max(-SafeZ, System.Math.Min(SafeZ, z)); }
        public static string Area(float x, float z)
        {
            if (z < -22) return "Entry & yard office";
            if (z > 22) return "Loading & container storage";
            if (x < -16) return "Vehicle salvage";
            if (x > 16) return "Metal sorting";
            return "Workshop yard";
        }
    }
}
