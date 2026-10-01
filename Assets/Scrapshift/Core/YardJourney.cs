using System;
namespace Scrapshift
{
    [Flags]
    public enum YardMilestone { None=0, RecoveredCopper=1, EarnedIncome=2, ServedCustomer=4, PoweredYard=8, RestoredAppliance=16, InvestedYard=32 }
    public struct YardGoal
    {
        public readonly YardMilestone milestone;
        public readonly string title, detail;
        public readonly YardLandmark destination;
        public YardGoal(YardMilestone milestone,string title,string detail,YardLandmark destination)
        { this.milestone=milestone;this.title=title;this.detail=detail;this.destination=destination; }
    }
    // Read-only journal: completed goals persist, have no deadline, and never grant extra money/material.
    public static class YardJourney
    {
        public const YardMilestone Known=YardMilestone.RecoveredCopper|YardMilestone.EarnedIncome|YardMilestone.ServedCustomer|YardMilestone.PoweredYard|YardMilestone.RestoredAppliance|YardMilestone.InvestedYard;
        public static readonly YardGoal[] Goals=
        {
            new YardGoal(YardMilestone.RecoveredCopper,"Recover your first copper","Take wire from delivery, load the stripping bench and work with empty hands.",YardLandmark.Delivery),
            new YardGoal(YardMilestone.EarnedIncome,"Earn your first income","Sell recovered copper or a tested appliance, or complete a customer request.",YardLandmark.Buyer),
            new YardGoal(YardMilestone.ServedCustomer,"Help a local customer","Accept a copper request on the customer board. Deliver over several trips if needed.",YardLandmark.Orders),
            new YardGoal(YardMilestone.PoweredYard,"Power up the yard","Buy the powered stripper. It can work while you use either hand bench.",YardLandmark.Machine),
            new YardGoal(YardMilestone.RestoredAppliance,"Give an appliance a second life","Find a fan or radio, inspect its fault, repair it and complete a power-on test.",YardLandmark.FanSupply),
            new YardGoal(YardMilestone.InvestedYard,"Make the yard your own","Visit the yard diary and invest in storage, hand tools or machine tuning.",YardLandmark.Diary)
        };
        public static YardMilestone Completed(YardState s)
        {
            var result=s.milestones;
            // Derive only established facts for legacy saves without journal fields.
            if(s.benchOutput>0 || s.machineOutput>0 || s.fanStage==FanStage.CopperReady)result|=YardMilestone.RecoveredCopper;
            foreach(var item in s.items)if(item.kind==MaterialKind.Copper)result|=YardMilestone.RecoveredCopper;
            if(s.incomeToday>0)result|=YardMilestone.EarnedIncome;
            if(s.orderIndex>0)result|=YardMilestone.ServedCustomer|YardMilestone.EarnedIncome;
            if(s.machineOwned)result|=YardMilestone.PoweredYard;
            if(s.fansRepaired>0 || s.radiosRepaired>0)result|=YardMilestone.RestoredAppliance;
            if(s.upgrades!=YardUpgrade.None)result|=YardMilestone.InvestedYard;
            return result;
        }
        public static bool HasProgress(YardState s)
        {
            return s.money>0 || s.items.Count>0 || s.benchLoaded || s.benchOutput>0 || s.machineOwned ||
                s.fanStage!=FanStage.Empty || s.dayIndex>0 || s.orderAccepted || s.orderIndex>0 ||
                s.fansTakenToday>0 || s.radiosTakenToday>0 || s.fansRepaired>0 || s.fansDismantled>0 ||
                s.radiosRepaired>0 || s.radiosDismantled>0 || s.milestones!=YardMilestone.None;
        }
    }
}
