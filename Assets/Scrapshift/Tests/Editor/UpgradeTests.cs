using NUnit.Framework;
namespace Scrapshift.Tests
{
    public sealed class UpgradeTests
    {
        [TestCaseSource(typeof(UpgradeScenarios),nameof(UpgradeScenarios.Names))]
        public void BusinessInvestments(string scenario){UpgradeScenarios.Run(scenario);}
    }
}
