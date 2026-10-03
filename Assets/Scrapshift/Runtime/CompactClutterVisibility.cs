using System.Collections.Generic;
using UnityEngine;

namespace Scrapshift.Compact
{
    // Startup-populated registry only: refreshed by gameplay transactions, never by a per-frame callback.
    public sealed class CompactClutterVisibility : MonoBehaviour
    {
        internal readonly List<CompactYardClutter.Patch> patches=new List<CompactYardClutter.Patch>();
        internal readonly List<GameObject> clusters=new List<GameObject>();
    }
}
