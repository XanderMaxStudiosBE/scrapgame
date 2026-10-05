using NUnit.Framework;

namespace Scrapshift.Tests
{
    public sealed class CompactIndustryGuidanceTests
    {
        [TestCaseSource(typeof(CompactIndustryGuidanceScenarios),nameof(CompactIndustryGuidanceScenarios.Names))]
        public void IndustryGuidanceReadsActualPaidWorkAndOptionalChoices(string name)
        {CompactIndustryGuidanceScenarios.Run(name);}
    }
}
