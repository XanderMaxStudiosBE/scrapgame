namespace Scrapshift
{
    public static class BalanceMigration
    {
        // Older Unity assets can deserialize newly added numeric fields as zero.
        // Zero was never valid for these new rules; retain every existing positive/custom value.
        public static bool FillMissingExtensions(YardRules rules)
        {
            if(rules==null)return false;
            var defaults=new YardRules();bool changed=false;
            Fill(ref rules.fanPartsPrice,defaults.fanPartsPrice,ref changed);
            Fill(ref rules.fanSalePrice,defaults.fanSalePrice,ref changed);
            Fill(ref rules.fanCopperYield,defaults.fanCopperYield,ref changed);
            Fill(ref rules.fanRepairStrokes,defaults.fanRepairStrokes,ref changed);
            Fill(ref rules.fanDismantleStrokes,defaults.fanDismantleStrokes,ref changed);
            Fill(ref rules.fanDailyLimit,defaults.fanDailyLimit,ref changed);
            Fill(ref rules.storageUpgradePrice,defaults.storageUpgradePrice,ref changed);
            Fill(ref rules.toolsUpgradePrice,defaults.toolsUpgradePrice,ref changed);
            Fill(ref rules.tuningUpgradePrice,defaults.tuningUpgradePrice,ref changed);
            Fill(ref rules.radioPartsPrice,defaults.radioPartsPrice,ref changed);
            Fill(ref rules.radioSalePrice,defaults.radioSalePrice,ref changed);
            Fill(ref rules.radioCopperYield,defaults.radioCopperYield,ref changed);
            Fill(ref rules.radioRepairStrokes,defaults.radioRepairStrokes,ref changed);
            Fill(ref rules.radioDismantleStrokes,defaults.radioDismantleStrokes,ref changed);
            Fill(ref rules.radioDailyLimit,defaults.radioDailyLimit,ref changed);
            Fill(ref rules.commissionBonusPerItem,defaults.commissionBonusPerItem,ref changed);
            return changed;
        }
        static void Fill(ref int value,int fallback,ref bool changed){if(value==0){value=fallback;changed=true;}}
    }
}
