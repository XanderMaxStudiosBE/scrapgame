using System;
using System.Collections.Generic;
namespace Scrapshift.Compact
{
    public static class CompactSaveMigration
    {
        // Call only after validating the original schema. No gifts, IDs, inputs or XP are changed.
        public static bool Upgrade(CompactYardState state,CompactRules rules)
        {
            if(state==null)throw new ArgumentNullException("state");
            if(state.version!=2&&state.version!=3)return false;
            ScrappingModel.Validate(state,rules);ConstructionModel.Validate(state,rules);
            if(state.version==2)
            {
                state.belts=new List<ConveyorLink>();
                foreach(var equipment in state.equipment){equipment.filterKind=-1;equipment.routeCursor=0;}
            }
            else AutomationModel.Validate(state,rules);
            state.version=4;return true;
        }
    }
}
