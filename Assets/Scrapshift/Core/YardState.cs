using System;
using System.Collections.Generic;

namespace Scrapshift
{
    public enum MaterialKind { Wire, Copper, BrokenFan, RestoredFan, BrokenRadio, RestoredRadio }
    public enum RepairAppliance { DeskFan, PortableRadio }
    public enum ApplianceFault { MainComponent, PowerLead, DirtyMechanism }
    public enum FanStage { Empty, AwaitingInspection, Diagnosed, Repairing, ReadyToTest, Tested, Dismantling, CopperReady }
    [Flags]
    public enum YardUpgrade { None = 0, StorageRack = 1, HandTools = 2, MachineTuning = 4 }
    public enum StorageSlot { None, Wire, Copper }

    [Serializable]
    public sealed class ScrapItem
    {
        public int id;
        public MaterialKind kind;
        public int quantity;
        public float x, y, z;
        public StorageSlot storage;
        public ApplianceFault applianceFault;
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
        public FanStage fanStage;
        public int fanStrokes, fansRepaired, fansDismantled;
        public int dayIndex, fansTakenToday, incomeToday;
        public YardUpgrade upgrades;
        // Zero values preserve the original seized-motor fan in old version-one saves.
        public RepairAppliance benchAppliance;
        public ApplianceFault benchFault;
        public int radiosTakenToday, radiosRepaired, radiosDismantled;
        public bool introSeen;
        public YardMilestone milestones;
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
        public int fanPartsPrice = 8, fanSalePrice = 42, fanCopperYield = 3;
        public int fanRepairStrokes = 3, fanDismantleStrokes = 4, fanDailyLimit = 2;
        public int storageUpgradePrice = 60, toolsUpgradePrice = 75, tuningUpgradePrice = 120;
        public int radioPartsPrice = 6, radioSalePrice = 34, radioCopperYield = 2;
        public int radioRepairStrokes = 3, radioDismantleStrokes = 3, radioDailyLimit = 1;

        public void Validate()
        {
            if (copperPerWire < 1 || copperPerWire > 100 || copperUnitPrice < 1 || copperUnitPrice > 10000 ||
                machinePrice < 1 || manualStrokes < 1 || manualStrokes > 100 ||
                float.IsNaN(machineSeconds) || float.IsInfinity(machineSeconds) || machineSeconds <= 0 || maxBundles < 1 || maxBundles > 100 ||
                fanPartsPrice < 1 || fanPartsPrice > 10000 || fanSalePrice < 1 || fanSalePrice > 10000 ||
                fanCopperYield < 1 || fanCopperYield > 100 || fanRepairStrokes < 1 || fanRepairStrokes > 100 ||
                fanDismantleStrokes < 1 || fanDismantleStrokes > 100 || fanDailyLimit < 1 || fanDailyLimit > 100 || storageUpgradePrice < 1 || storageUpgradePrice > 10000 ||
                toolsUpgradePrice < 1 || toolsUpgradePrice > 10000 || tuningUpgradePrice < 1 || tuningUpgradePrice > 10000 ||
                radioPartsPrice < 1 || radioPartsPrice > 10000 || radioSalePrice < 1 || radioSalePrice > 10000 ||
                radioCopperYield < 1 || radioCopperYield > 100 || radioRepairStrokes < 1 || radioRepairStrokes > 100 ||
                radioDismantleStrokes < 1 || radioDismantleStrokes > 100 || radioDailyLimit < 1 || radioDailyLimit > 100)
                throw new ArgumentException("Invalid prototype balance values.");
        }
    }
}
