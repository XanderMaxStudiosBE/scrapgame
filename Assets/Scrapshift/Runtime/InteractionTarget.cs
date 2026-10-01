using UnityEngine;

namespace Scrapshift
{
    public sealed class InteractionTarget : MonoBehaviour
    {
        public TargetKind kind;
        public int itemId;
        public string displayName;
    }
}
