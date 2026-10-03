using NUnit.Framework;

namespace Scrapshift.Tests
{
    public sealed class CompactGuidanceTests
    {
        [TestCaseSource(typeof(CompactGuidanceScenarios),nameof(CompactGuidanceScenarios.Names))]
        public void GuidanceUsesLiveYardWithoutTransactions(string name){CompactGuidanceScenarios.Run(name);}
    }
}
