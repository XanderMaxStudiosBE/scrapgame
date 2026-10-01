using System;
using System.Collections.Generic;

namespace Scrapshift
{
    public enum MaterialKind { Wire, Copper }
    public enum StorageSlot { None, Wire, Copper }

    [Serializable]
    public sealed class ScrapItem
    {
        public int id;
        public MaterialKind kind;
        public int quantity;
        public float x, y, z;
        public StorageSlot storage;
    }

    // Plain data is the authoritative inventory, including carried and dropped bundles.
    [Serializable]
    public sealed class YardState
    {
        public int version = 1;
        public int money;
        public bool machineOwned;
        public int nextId = 1;
        public int carriedId;
        public List<ScrapItem> items = new List<ScrapItem>();
        public bool benchLoaded;
        public int benchStrokes;
        public int benchOutput;
        public float machineRemaining;
        public int machinePendingYield;
        public int machineOutput;
        public float playerX = 0, playerY = 1.1f, playerZ = -6;
        public float yaw, pitch;
        // Additive version-one fields: absent fields in older saves mean empty storage/no order.
        public bool orderAccepted;
        public int orderIndex, orderDelivered;
    }

    [Serializable]
    public sealed class YardRules
    {
        public int copperPerWire = 3;
        public int copperUnitPrice = 4;
        public int machinePrice = 36;
        public int manualStrokes = 4;
        public float machineSeconds = 5;
        public int maxBundles = 24;

        public void Validate()
        {
            if (copperPerWire < 1 || copperPerWire > 100 || copperUnitPrice < 1 || copperUnitPrice > 10000 ||
                machinePrice < 1 || manualStrokes < 1 || manualStrokes > 100 ||
                float.IsNaN(machineSeconds) || float.IsInfinity(machineSeconds) || machineSeconds <= 0 || maxBundles < 1 || maxBundles > 100)
                throw new ArgumentException("Invalid prototype balance values.");
        }
    }
}
