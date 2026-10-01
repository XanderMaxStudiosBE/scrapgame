using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class JourneyTests
    {
        [TestCaseSource(typeof(JourneyScenarios),nameof(JourneyScenarios.Names))]
        public void FirstDayJournal(string scenario){JourneyScenarios.Run(scenario);}
        [Test]
        public void JournalAndIntroductionSurviveActualJson()
        {
            var state=new YardState{introSeen=true,milestones=YardMilestone.RecoveredCopper|YardMilestone.EarnedIncome,dayIndex=2};
            var restored=JsonUtility.FromJson<YardState>(JsonUtility.ToJson(state));YardModel.Validate(restored);
            Assert.IsTrue(restored.introSeen);Assert.AreEqual(state.milestones,restored.milestones);
        }
        [Test]
        public void OldJsonHasNoInventedIntroOrGoals()
        {
            var state=JsonUtility.FromJson<YardState>("{\"version\":1,\"nextId\":1,\"items\":[]}");YardModel.Validate(state);
            Assert.IsFalse(state.introSeen);Assert.AreEqual(YardMilestone.None,YardJourney.Completed(state));
        }
        [Test]
        public void TitleAndPristineWelcomePauseSimulationWithoutWriting()
        {
            float previousScale=Time.timeScale;var previousLock=Cursor.lockState;bool previousVisible=Cursor.visible;
            var root=new GameObject("Front menu isolation");
            try
            {
                var game=root.AddComponent<PrototypeGame>();
                typeof(PrototypeGame).GetProperty("Model").SetValue(game,new YardModel(new YardRules()));
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(PrototypeGame).GetMethod("InitializeFrontEnd",flags).Invoke(game,null);
                Assert.AreEqual(0,Time.timeScale);Assert.IsTrue(Cursor.visible);
                Assert.IsTrue((bool)typeof(PrototypeGame).GetField("titleOpen",flags).GetValue(game));
                // A pristine yard opens the welcome before the branch that saves/resumes gameplay.
                typeof(PrototypeGame).GetMethod("BeginYard",flags).Invoke(game,null);
                Assert.AreEqual(0,Time.timeScale);
                Assert.IsTrue((bool)typeof(PrototypeGame).GetField("introOpen",flags).GetValue(game));
                Assert.IsFalse(game.Model.State.introSeen);
            }
            finally{Object.DestroyImmediate(root);Time.timeScale=previousScale;Cursor.lockState=previousLock;Cursor.visible=previousVisible;}
        }
    }
}
