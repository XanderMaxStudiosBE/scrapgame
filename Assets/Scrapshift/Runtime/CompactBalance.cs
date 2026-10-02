using UnityEngine;

namespace Scrapshift.Compact
{
    [CreateAssetMenu(menuName="Scrapshift/Compact Yard Balance")]
    public sealed class CompactBalance : ScriptableObject
    {
        public CompactRules rules=new CompactRules();
        public CompactRules PreparedRules
        {
            get
            {
                if(rules==null)rules=new CompactRules();
                // Custom prices, yields and curve remain authoritative; invalid tuning is surfaced.
                rules.Validate();return rules;
            }
        }
    }
}
