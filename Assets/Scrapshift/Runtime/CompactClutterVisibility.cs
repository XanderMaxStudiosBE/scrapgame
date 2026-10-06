using System.Collections.Generic;
using UnityEngine;

namespace Scrapshift.Compact
{
    // Startup-populated registry: gameplay transactions and pending player-clearance checks reuse these roots.
    public sealed class CompactClutterVisibility : MonoBehaviour
    {
        internal readonly List<CompactYardClutter.Patch> patches=new List<CompactYardClutter.Patch>();
        internal readonly List<GameObject> clusters=new List<GameObject>();
    }
}
