using System;
namespace Scrapshift
{
    // Bounded recipes, derived from editable balance. Faults travel with their item/job through saves.
    public struct ApplianceRecipe
    {
        public readonly string name, fault, observation, repairAction, workAction, testResult;
        public readonly int partsPrice, salePrice, copperYield, repairSteps, salvageSteps;
        ApplianceRecipe(string name, string fault, string observation, string repairAction, string workAction,
            string testResult, int partsPrice, int salePrice, int copperYield, int repairSteps, int salvageSteps)
        {
            this.name=name; this.fault=fault; this.observation=observation; this.repairAction=repairAction;
            this.workAction=workAction; this.testResult=testResult; this.partsPrice=partsPrice;
            this.salePrice=salePrice; this.copperYield=copperYield; this.repairSteps=repairSteps; this.salvageSteps=salvageSteps;
        }
        public static bool IsBroken(MaterialKind kind) { return kind==MaterialKind.BrokenFan || kind==MaterialKind.BrokenRadio; }
        public static bool IsRestored(MaterialKind kind) { return kind==MaterialKind.RestoredFan || kind==MaterialKind.RestoredRadio; }
        public static bool IsAppliance(MaterialKind kind) { return IsBroken(kind) || IsRestored(kind); }
        public static ApplianceRecipe Read(RepairAppliance appliance, ApplianceFault fault, YardRules rules)
        {
            bool radio=appliance==RepairAppliance.PortableRadio;
            if((int)appliance<0 || (int)appliance>1 || (int)fault<0 || (int)fault>2)throw new ArgumentException("Unknown appliance recipe.");
            int parts=radio?rules.radioPartsPrice:rules.fanPartsPrice;
            int steps=radio?rules.radioRepairStrokes:rules.fanRepairStrokes;
            string diagnosis, observation, action, work;
            switch(fault)
            {
                case ApplianceFault.PowerLead:
                    diagnosis="Loose power lead"; observation="The lead is frayed at its connection. The rest of the appliance is reusable.";
                    action="Replace and secure the lead"; work="secure power lead";
                    parts=Math.Max(1,parts/2); steps=Math.Max(1,steps-1); break;
                case ApplianceFault.DirtyMechanism:
                    diagnosis=radio?"Oxidized tuner contacts":"Dust-clogged bearings";
                    observation=radio?"The tuner crackles, but the board is intact. Clean the contacts and retest reception.":"Dust has packed around the shaft. Cleaning and a little oil can free the bearings.";
                    action=radio?"Clean the tuner contacts":"Clean and oil the bearings";
                    work=radio?"clean tuner contacts":"clean and oil bearings";
                    parts=0; steps=Math.Max(1,steps-1); break;
                default:
                    diagnosis=radio?"Failed capacitor":"Seized motor";
                    observation=radio?"The supply capacitor has failed. The speaker and tuner can be kept.":"The motor has seized. The guard, base and wiring are reusable.";
                    action=radio?"Fit a replacement capacitor":"Fit replacement motor";
                    work=radio?"fit capacitor":"fit motor"; break;
            }
            return new ApplianceRecipe(radio?"portable radio":"desk fan",diagnosis,observation,action,work,
                radio?"Clear reception. The speaker and tuner pass the listening test.":"The fan runs smoothly. Motor, guard and wiring pass the power-on test.",
                parts,radio?rules.radioSalePrice:rules.fanSalePrice,radio?rules.radioCopperYield:rules.fanCopperYield,
                steps,radio?rules.radioDismantleStrokes:rules.fanDismantleStrokes);
        }
    }
    public sealed partial class YardModel
    {
        public ApplianceRecipe CurrentRepair { get { return ApplianceRecipe.Read(State.benchAppliance,State.benchFault,Rules); } }
        public long SaleValue(ScrapItem item)
        {
            if(item==null)return 0;
            if(item.kind==MaterialKind.Copper)return (long)item.quantity*Rules.copperUnitPrice;
            if(item.kind==MaterialKind.RestoredFan)return Rules.fanSalePrice;
            return item.kind==MaterialKind.RestoredRadio?Rules.radioSalePrice:0;
        }
    }
}
