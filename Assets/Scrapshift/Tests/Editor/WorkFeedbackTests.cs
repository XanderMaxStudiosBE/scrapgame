using NUnit.Framework;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class WorkFeedbackTests
    {
        [TestCaseSource(typeof(WorkFeedbackScenarios),nameof(WorkFeedbackScenarios.Names))]
        public void ProgressFromTransactions(string name) { WorkFeedbackScenarios.Run(name); }
        [Test]
        public void WireDisplayUsesBundlesAndSettlesToolWithoutColliders()
        {
            var root = new GameObject("Workbench feedback test");
            try
            {
                var bench=YardProps.Workbench(root.transform,Vector3.zero);
                var visual=new WorkbenchJobVisual(bench.materialDisplay);
                var model=new YardModel(new YardRules());
                visual.Refresh(model);
                Assert.IsFalse(bench.materialDisplay.GetComponent<Renderer>().enabled);
                Assert.IsFalse(visual.wire.gameObject.activeSelf);
                model.AcquireWire();model.LoadBench();visual.Refresh(model);
                Assert.IsTrue(visual.wire.gameObject.activeSelf);
                Assert.IsTrue(visual.tool.gameObject.activeSelf);
                foreach(var collider in bench.materialDisplay.GetComponentsInChildren<Collider>(true))Assert.IsFalse(collider.enabled);
                Assert.IsFalse(visual.copper.gameObject.activeSelf);
                model.WorkBench();visual.Refresh(model);
                Assert.IsTrue(visual.copper.gameObject.activeSelf,"copper emerges during stripping");
                Assert.Less(visual.wire.localScale.x,1);
                Vector3 rest=visual.tool.localPosition;
                visual.Pulse();visual.Step(.11f);
                Assert.Greater(Vector3.Distance(rest,visual.tool.localPosition),.1f);
                Vector3 held=visual.tool.localPosition;visual.Step(0);
                Assert.AreEqual(held,visual.tool.localPosition,"zero delta holds paused pose");
                visual.Step(.22f);Assert.Less(Vector3.Distance(rest,visual.tool.localPosition),.001f);
                while(model.State.benchLoaded)model.WorkBench();visual.Refresh(model);
                Assert.IsFalse(visual.wire.gameObject.activeSelf);
                Assert.IsTrue(visual.copper.gameObject.activeSelf);
                Assert.IsFalse(visual.tool.gameObject.activeSelf);
                model.CollectBench();visual.Refresh(model);
                Assert.IsFalse(visual.copper.gameObject.activeSelf);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void FanWorkFeedbackLeavesWireBenchStill()
        {
            var root=new GameObject("Independent bench pulses");
            try
            {
                var bench=YardProps.Workbench(root.transform,Vector3.zero);
                var wire=new WorkbenchJobVisual(bench.materialDisplay);
                var fan=FanWorkbenchVisual.Build(root.transform);
                var model=new YardModel(new YardRules());
                Quaternion rest=fan.display.localRotation;Vector3 wireRest=wire.tool.localPosition;
                fan.Pulse();fan.Step(model,.11f);wire.Step(.11f);
                Assert.Greater(Quaternion.Angle(rest,fan.display.localRotation),1);
                Assert.AreEqual(wireRest,wire.tool.localPosition);
                fan.Step(model,.22f);
                Assert.Less(Quaternion.Angle(rest,fan.display.localRotation),.01f);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
