using UnityEngine;

namespace Scrapshift
{
    [CreateAssetMenu(menuName = "Scrapshift/Prototype Balance")]
    public sealed class PrototypeBalance : ScriptableObject
    {
        public YardRules rules = new YardRules();
    }
}
