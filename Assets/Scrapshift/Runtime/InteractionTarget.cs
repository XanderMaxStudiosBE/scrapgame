using UnityEngine;

namespace Scrapshift
{
    public enum TargetKind { Supply, Bench, Machine, Sell, LooseItem }
    public sealed class InteractionTarget : MonoBehaviour
    {
        public TargetKind kind;
        public int itemId;
    }
}
