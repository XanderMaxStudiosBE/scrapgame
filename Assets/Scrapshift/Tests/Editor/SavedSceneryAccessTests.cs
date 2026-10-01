using NUnit.Framework;
using UnityEngine;
namespace Scrapshift.Tests
{
    public sealed class SavedSceneryAccessTests
    {
        [TestCase(true)][TestCase(false)]
        public void NewSceneryHidesForSavedPlayerOrDroppedItemWithoutMutatingSave(bool player)
        {
            var yard=new GameObject("Saved access test");
            try
            {
                var prop=new GameObject("ApplianceRow");prop.transform.SetParent(yard.transform,false);
                prop.transform.localPosition=new Vector3(12,0,-7);prop.transform.localRotation=Quaternion.Euler(0,90,0);
                var box=prop.AddComponent<BoxCollider>();box.center=new Vector3(0,1,0);box.size=new Vector3(4,2,1);
                var footprint=prop.AddComponent<AddedSceneryFootprint>();Assert.IsTrue(footprint.Covers(12,-8.5f,.3f));Assert.IsFalse(footprint.Covers(14,-7,.3f));
                var state=new YardState();if(player){state.playerX=12;state.playerZ=-8.5f;}else state.items.Add(new ScrapItem{id=1,quantity=3,kind=MaterialKind.Copper,x=12,y=.2f,z=-8.5f});
                string before=JsonUtility.ToJson(state);AddedSceneryFootprint.PreserveSavedAccess(yard.transform,state);
                Assert.IsFalse(prop.activeSelf);Assert.AreEqual(before,JsonUtility.ToJson(state));
            }
            finally{Object.DestroyImmediate(yard);}
        }
        [Test]
        public void StoredAndCarriedMetadataDoesNotHideUnoccupiedScenery()
        {
            var yard=new GameObject("Carried access test");
            try
            {
                var prop=new GameObject("ApplianceRow");prop.transform.SetParent(yard.transform,false);prop.transform.localPosition=new Vector3(12,0,-7);
                var box=prop.AddComponent<BoxCollider>();box.size=new Vector3(4,2,1);prop.AddComponent<AddedSceneryFootprint>();
                var state=new YardState{carriedId=1};state.items.Add(new ScrapItem{id=1,x=12,z=-7});state.items.Add(new ScrapItem{id=2,x=12,z=-7,storage=StorageSlot.Copper});
                AddedSceneryFootprint.PreserveSavedAccess(yard.transform,state);Assert.IsTrue(prop.activeSelf);
            }
            finally{Object.DestroyImmediate(yard);}
        }
    }
}
