using NUnit.Framework;
namespace Scrapshift.Tests
{
    public sealed class IntegrationTests
    {
        [TestCaseSource(typeof(IntegrationScenarios),nameof(IntegrationScenarios.Names))]
        public void MixedProgressionAndConservation(string scenario){IntegrationScenarios.Run(scenario);}
    }
}
