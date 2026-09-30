using NUnit.Framework;
namespace Scrapshift.Tests
{
    public sealed class GuidanceTests
    {
        [TestCaseSource(typeof(GuidanceScenarios), nameof(GuidanceScenarios.Names))]
        public void CurrentStateGuidance(string scenario) { GuidanceScenarios.Run(scenario); }
    }
}
