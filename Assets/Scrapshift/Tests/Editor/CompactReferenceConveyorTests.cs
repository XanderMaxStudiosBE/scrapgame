using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Scrapshift.Compact;
using UnityEngine;

namespace Scrapshift.Tests
{
    // Native renderer/physics acceptance fixtures. Supplied for Unity, unrun in Cloud.
    public sealed class CompactReferenceConveyorTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        float previousScale;CursorLockMode previousCursor;bool previousVisible;
        [SetUp] public void PreservePresentationState()
        {previousScale=Time.timeScale;previousCursor=Cursor.lockState;previousVisible=Cursor.visible;}
        [TearDown] public void RestorePresentationState()
        {Time.timeScale=previousScale;Cursor.lockState=previousCursor;Cursor.visible=previousVisible;}
        static object Get(CompactYardGame game,string name){return typeof(CompactYardGame).GetField(name,Private).GetValue(game);}
        static object Call(CompactYardGame game,string name,params object[] args){return typeof(CompactYardGame).GetMethod(name,Private).Invoke(game,args);}

        [TestCaseSource(typeof(CompactReferenceConveyorScenarios),nameof(CompactReferenceConveyorScenarios.Names))]
        public void AuthoritativeReferenceRegressions(string name){CompactReferenceConveyorScenarios.Run(name);}

        [Test]
        public void PreviewKeepsExactEndpointsShowsDirectionAndReusesItsTemporaryViews()
        {
            var root=new GameObject("Reference conveyor preview test");
            try
            {
                root.transform.position=new Vector3(1000,2,1000);root.transform.rotation=Quaternion.Euler(0,90,0);
                var game=root.AddComponent<CompactYardGame>();game.enabled=false;
                var points=new[]{new Vector3(1,.78f,2),new Vector3(1,.78f,6),new Vector3(4,.78f,6)};
                Call(game,"PreviewBelt",points,true,true);
                var preview=(LineRenderer)Get(game,"beltPreview");
                Assert.IsFalse(preview.useWorldSpace);Assert.AreEqual(points[0],preview.GetPosition(0));Assert.AreEqual(points[2],preview.GetPosition(2));
                Assert.AreEqual(root.transform.TransformPoint(points[0]),preview.transform.TransformPoint(preview.GetPosition(0)),"Displayed endpoint follows the translated/rotated yard.");
                var arrows=(IList)Get(game,"beltDirectionPreviews");var corridors=(IList)Get(game,"beltCorridorPreviews");
                Assert.AreEqual(2,arrows.Count);Assert.AreEqual(2,corridors.Count);
                for(int i=0;i<2;i++)
                {
                    var arrow=(LineRenderer)arrows[i];var forward=(points[i+1]-points[i]).normalized;
                    var basePoint=(arrow.GetPosition(0)+arrow.GetPosition(2))*.5f;
                    Assert.Greater(Vector3.Dot(arrow.GetPosition(1)-basePoint,forward),0,"Every arrow points from OUT towards IN.");
                    var corridor=(LineRenderer)corridors[i];Assert.AreEqual(AutomationModel.CorridorWidth,(corridor.GetPosition(1)-corridor.GetPosition(2)).magnitude,.0001f);
                }
                var views=root.GetComponentsInChildren<LineRenderer>(true);
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(root.GetComponentsInChildren<Rigidbody>(true));
                Call(game,"PreviewBelt",points,false,true);
                CollectionAssert.AreEqual(views,root.GetComponentsInChildren<LineRenderer>(true),"Invalidating the route reuses renderer identities.");
                Call(game,"PreviewBelt",new[]{points[0],points[2]},false,false);
                foreach(LineRenderer arrow in arrows)Assert.IsFalse(arrow.gameObject.activeSelf);
                Assert.IsFalse(((LineRenderer)Get(game,"beltDestinationPreview")).gameObject.activeSelf,"Unsnapped cursor cannot retain a former destination marker.");
                Call(game,"DestroyBeltPreview");Assert.IsEmpty(root.GetComponentsInChildren<LineRenderer>(true));Assert.IsNull(Get(game,"beltPreviewRoot"));
            }
            finally{Object.DestroyImmediate(root);}
        }

        [TestCase(0)] [TestCase(90)] [TestCase(180)] [TestCase(270)]
        public void NativeWorldClearanceUsesWholeCorridorAndTransformedYard(float yaw)
        {
            var root=new GameObject("Reference conveyor world clearance test");
            try
            {
                root.transform.position=new Vector3(1000,2,1000);root.transform.rotation=Quaternion.Euler(0,yaw,0);
                var game=root.AddComponent<CompactYardGame>();game.enabled=false;
                var obstacle=new GameObject("Obstacle at outer rail clearance");obstacle.transform.SetParent(root.transform,false);
                obstacle.transform.localPosition=new Vector3(.43f,CompactAutomationVisuals.ItemHeight,1.5f);
                obstacle.AddComponent<BoxCollider>().size=new Vector3(.08f,.2f,.3f);
                var path=new[]{new CompactPortPoint(0,0),new CompactPortPoint(0,3)};Physics.SyncTransforms();
                Assert.IsFalse((bool)Call(game,"BeltAvoidsWorld",path,1,2),"A thin obstacle outside the former .36m half-width still blocks the .94m required corridor.");
                obstacle.transform.localPosition=new Vector3(1,CompactAutomationVisuals.ItemHeight,1.5f);Physics.SyncTransforms();
                Assert.IsTrue((bool)Call(game,"BeltAvoidsWorld",path,1,2),"Walking/building clear of the route allows the same translated/rotated corridor.");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
