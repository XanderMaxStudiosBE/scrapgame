using UnityEngine;
namespace Scrapshift
{
    // Only new foreground scenery is eligible. Original collision, stations and saved entities never move.
    public sealed class AddedSceneryFootprint : MonoBehaviour
    {
        public Bounds WorldFootprint()
        {
            bool first=true;var result=new Bounds();
            foreach(var box in GetComponents<BoxCollider>())
                foreach(float x in new[]{-.5f,.5f})foreach(float y in new[]{-.5f,.5f})foreach(float z in new[]{-.5f,.5f})
                {
                    var point=transform.TransformPoint(box.center+Vector3.Scale(box.size,new Vector3(x,y,z)));
                    if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);
                }
            return result;
        }
        public bool Covers(float x,float z,float padding)
        {return Covers(WorldFootprint(),x,z,padding);}
        static bool Covers(Bounds b,float x,float z,float padding)
        {return x>=b.min.x-padding && x<=b.max.x+padding && z>=b.min.z-padding && z<=b.max.z+padding;}
        public static void PreserveSavedAccess(Transform yard,YardState state)
        {
            foreach(var scenery in yard.GetComponentsInChildren<AddedSceneryFootprint>())
            {
                var bounds=scenery.WorldFootprint();bool occupied=Covers(bounds,state.playerX,state.playerZ,.8f);
                if(!occupied)foreach(var item in state.items)
                    if(item.id!=state.carriedId && item.storage==StorageSlot.None && Covers(bounds,item.x,item.z,.55f)){occupied=true;break;}
                if(occupied)scenery.gameObject.SetActive(false);
            }
        }
    }
}
