using System;
namespace Scrapshift
{
    public sealed partial class YardModel
    {
        public bool Owns(YardUpgrade upgrade) { return (State.upgrades & upgrade) == upgrade && upgrade != YardUpgrade.None; }
        public int Capacity { get { return Math.Min(100, Rules.maxBundles + (Owns(YardUpgrade.StorageRack) ? 12 : 0)); } }
        public int WireWorkSteps { get { return Math.Max(1, Rules.manualStrokes - (Owns(YardUpgrade.HandTools) ? 1 : 0)); } }
        public int FanRepairSteps { get { return Math.Max(1, Rules.fanRepairStrokes - (Owns(YardUpgrade.HandTools) ? 1 : 0)); } }
        public int FanSalvageSteps { get { return Math.Max(1, Rules.fanDismantleStrokes - (Owns(YardUpgrade.HandTools) ? 1 : 0)); } }
        public float MachineSeconds { get { return Rules.machineSeconds * (Owns(YardUpgrade.MachineTuning) ? .6f : 1); } }
        public int UpgradePrice(YardUpgrade upgrade)
        {
            switch(upgrade)
            {
                case YardUpgrade.StorageRack:return Rules.storageUpgradePrice;
                case YardUpgrade.HandTools:return Rules.toolsUpgradePrice;
                case YardUpgrade.MachineTuning:return Rules.tuningUpgradePrice;
                default:return 0;
            }
        }
        public bool CanBuyUpgrade(YardUpgrade upgrade)
        {
            int price=UpgradePrice(upgrade);
            return price>0 && !Owns(upgrade) && State.money>=price &&
                (upgrade!=YardUpgrade.MachineTuning || State.machineOwned) &&
                (upgrade!=YardUpgrade.StorageRack || Rules.maxBundles<100);
        }
        public bool BuyUpgrade(YardUpgrade upgrade)
        {
            if(!CanBuyUpgrade(upgrade))return false;
            State.money-=UpgradePrice(upgrade);State.upgrades|=upgrade;return true;
        }
    }
}
