using UnityEngine;

namespace Scrapshift
{
    [CreateAssetMenu(menuName = "Scrapshift/Prototype Balance")]
    public sealed class PrototypeBalance : ScriptableObject
    {
        public YardRules rules = new YardRules();
        public YardRules PreparedRules
        {
            get
            {
                if (rules == null) rules = new YardRules();
                BalanceMigration.FillMissingExtensions(rules);
                return rules;
            }
        }
    }
}
