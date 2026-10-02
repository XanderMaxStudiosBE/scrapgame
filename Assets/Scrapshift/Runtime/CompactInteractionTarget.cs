using UnityEngine;
namespace Scrapshift.Compact
{
    public enum CompactTargetKind {Shop,Sales,Delivery,Wire,Equipment,LargeScrap,Item}
    public sealed class CompactInteractionTarget : MonoBehaviour
    {public CompactTargetKind kind;public int id;}
}
