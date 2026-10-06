using UnityEngine;

namespace Scrapshift.Compact
{
    // A physical mouth is selectable separately from the machine body during belt construction.
    public sealed class CompactConveyorPortTarget : MonoBehaviour
    {
        public bool output;
        public int index;
    }
}
