using NUnit.Framework;

namespace Scrapshift.Tests
{
    public sealed class CompactFlowStatusTests
    {
        [TestCaseSource(typeof(CompactFlowStatusScenarios),nameof(CompactFlowStatusScenarios.Names))]
        public void ConveyorOperatingFeedback(string name){CompactFlowStatusScenarios.Run(name);}
    }
}
