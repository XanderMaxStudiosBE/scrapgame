using NUnit.Framework;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class WorldTests
    {
        [TestCaseSource(typeof(WorldScenarios), nameof(WorldScenarios.Names))]
        public void SharedWorldScenarios(string scenario) { WorldScenarios.Run(scenario); }

        [Test]
        public void ExpandedGroundSourcesAndWalkwaysAreAccessible()
        {
            var root = new GameObject("World layout test");
            try
            {
                // Raw geometry makes collision checks independent of rendering/batching.
                ScrapyardWorld.Build(root.transform, false);
                var ground = root.transform.Find("Ground and lanes/Packed gravel").GetComponent<BoxCollider>();
                Assert.AreEqual(new Vector3(96, .5f, 80), ground.size);
                var sources = root.GetComponentsInChildren<InteractionTarget>();
                Assert.AreEqual(3, sources.Length);
                Physics.SyncTransforms();
                foreach (var source in sources)
                {
                    Assert.AreEqual(TargetKind.Supply, source.kind);
                    Assert.IsNotEmpty(source.displayName);
                    Vector3 approach = source.transform.position + new Vector3(0, 1, -1.8f);
                    Assert.IsFalse(Physics.CheckCapsule(approach + Vector3.down * .6f, approach + Vector3.up * .6f, .3f), source.name + " approach blocked");
                    Assert.IsTrue(Physics.Raycast(approach + Vector3.up * .55f, (source.transform.position + Vector3.up * .6f - approach - Vector3.up * .55f).normalized, out RaycastHit hit, 3.2f));
                    Assert.AreSame(source, hit.collider.GetComponentInParent<InteractionTarget>(), "crate must be first interaction hit");
                }
                // Continuous main routes reach all districts and the old player spawn.
                for (int z = -36; z <= 25; z++) AssertWalkable(new Vector3(0, 1, z));
                for (int x = -44; x <= 44; x++) AssertWalkable(new Vector3(x, 1, -13));
                for (int z = -13; z <= -8; z++) AssertWalkable(new Vector3(-30, 1, z));
                for (int z = -20; z <= -13; z++) AssertWalkable(new Vector3(31, 1, z));
                for (int z = -13; z <= 19; z++) AssertWalkable(new Vector3(-42, 1, z));
                for (int x = -42; x <= -33; x++) AssertWalkable(new Vector3(x, 1, 17));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void ExpandedSaveRestoresPlayerHeightAndRemoteBundles()
        {
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "scrapshift-world-" + System.Guid.NewGuid());
            string path = System.IO.Path.Combine(directory, "yard.json");
            var playerObject = new GameObject("Saved world player");
            try
            {
                var m = new YardModel(new YardRules());
                m.AcquireWire(); m.Drop(-33, .25f, 19);
                m.State.playerX = 24; m.State.playerY = 1.7f; m.State.playerZ = 22;
                m.State.yaw = 80; m.State.pitch = -12;
                SaveStore.Write(path, m.State);
                var state = SaveStore.Read(path, out _);
                var player = playerObject.AddComponent<FirstPersonController>();
                var cameraObject = new GameObject("Test view"); cameraObject.transform.SetParent(playerObject.transform, false);
                player.view = cameraObject.AddComponent<Camera>();
                player.Restore(state);
                Assert.AreEqual(new Vector3(24, 1.7f, 22), player.transform.position, "Expanded location and dock height must survive restoration");
                Assert.AreEqual(80, player.Yaw); Assert.AreEqual(-12, player.Pitch);
                Assert.AreEqual(-33, state.items[0].x); Assert.AreEqual(19, state.items[0].z);
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
                if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true);
            }
        }
        static void AssertWalkable(Vector3 point)
        {
            Assert.IsFalse(Physics.CheckCapsule(point + Vector3.down * .6f, point + Vector3.up * .6f, .3f), "Blocked lane at " + point);
        }
    }
}
