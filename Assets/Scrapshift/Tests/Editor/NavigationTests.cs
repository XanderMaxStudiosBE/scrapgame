using NUnit.Framework;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class NavigationTests
    {
        [TestCaseSource(typeof(NavigationScenarios),nameof(NavigationScenarios.Names))]
        public void NavigationFromCurrentYard(string scenario) { NavigationScenarios.Run(scenario); }
        [Test]
        public void BuiltStationsUseNavigationCoordinatesAndMarkers()
        {
            var root=new GameObject("Navigation station alignment");
            try
            {
                var business=YardBusinessVisual.Build(root.transform);
                var fan=FanWorkbenchVisual.Build(root.transform);
                YardProps.Delivery(root.transform,YardBootstrap.StationPosition(YardLandmark.Delivery));
                YardProps.Workbench(root.transform,YardBootstrap.StationPosition(YardLandmark.Bench));
                YardProps.Buyer(root.transform,YardBootstrap.StationPosition(YardLandmark.Buyer));
                WireStripperVisual.Build(root.transform,YardBootstrap.StationPosition(YardLandmark.Machine));
                var targets=root.GetComponentsInChildren<InteractionTarget>();
                var kinds=new[]{TargetKind.Supply,TargetKind.Bench,TargetKind.Sell,TargetKind.Machine,TargetKind.WireStorage,TargetKind.CopperStorage,TargetKind.OrderBoard,TargetKind.FanSupply,TargetKind.FanBench,TargetKind.DayBoard};
                var landmarks=new[]{YardLandmark.Delivery,YardLandmark.Bench,YardLandmark.Buyer,YardLandmark.Machine,YardLandmark.WireStorage,YardLandmark.CopperStorage,YardLandmark.Orders,YardLandmark.FanSupply,YardLandmark.FanBench,YardLandmark.Diary};
                for(int i=0;i<kinds.Length;i++)
                {
                    InteractionTarget found=null;
                    foreach(var target in targets)if(target.kind==kinds[i])found=target;
                    Assert.NotNull(found,kinds[i].ToString());
                    var destination=YardNavigation.Get(landmarks[i]);
                    Assert.AreEqual(destination.x,found.transform.position.x);
                    Assert.AreEqual(destination.z,found.transform.position.z);
                }
                Assert.AreEqual(YardBootstrap.StationPosition(YardLandmark.FanBench)+new Vector3(0,1.1f,-.05f),fan.display.position);
                Assert.NotNull(business.orderText);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
