using NUnit.Framework;

namespace Scrapshift.Tests
{
    public sealed class CompactDressingVisibilityTests
    {
        [TestCase("ActiveBenchStockSurvivesWork")]
        [TestCase("RestoredSpawnRemainsClear")]
        [TestCase("RemovedEquipmentWaitsForPlayer")]
        [TestCase("RemovedLooseItemWaitsForPlayer")]
        [TestCase("CapsuleCornerDoesNotHideClearStock")]
        public void StockVisibilityProtectsOnlyRestoredOrStillOccupiedPlayerSpace(string scenario)
        {CompactDressingVisibilityCases.Run(scenario);}

        [TestCase("CoordinatorBenchWorkKeepsStock")]
        [TestCase("CoordinatorRestoredSpawnPollsClearance")]
        [TestCase("CoordinatorRemovedEquipmentPollsClearance")]
        [TestCase("CoordinatorRemovedLooseItemPollsClearance")]
        public void CoordinatorCachesStableStockAndRestoresDeferredModulesWithoutAnotherTransaction(string scenario)
        {CompactDressingVisibilityCases.RunCoordinator(scenario);}
    }
}
