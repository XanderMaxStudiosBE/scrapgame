using System;

namespace Scrapshift.Compact
{
    public static class CompactPortSelection
    {
        // Used by both physical-mouth clicks and the management-menu shortcut.
        // Selection is read-only; the full route is still checked again at purchase.
        public static bool CanSelectOutput(CompactYardState state,CompactRules rules,int id,int index,out string reason)
        {
            EquipmentState equipment=null;
            if(state!=null && state.equipment!=null)
                foreach(var candidate in state.equipment)if(candidate!=null && candidate.id==id){equipment=candidate;break;}
            if(equipment==null || rules==null || index<0 || index>=AutomationModel.PortCount(equipment.kind,true))
            {reason="Choose a physical OUT mouth on existing equipment.";return false;}
            var definition=rules.Equipment(equipment.kind);
            if(definition==null){reason="The output equipment definition is unavailable.";return false;}
            if(!CompactRules.Finite(equipment.yaw) || Math.Abs(equipment.yaw/90-(float)Math.Round(equipment.yaw/90))>=.00001f)
            {reason="Rotate the output equipment to a quarter turn before connecting.";return false;}
            if(state.belts!=null)foreach(var belt in state.belts)
                if(belt!=null && belt.fromId==id && belt.fromPort==index)
                {reason="This OUT already has a conveyor; choose another junction output or dismantle its empty belt.";return false;}
            reason=definition.name+" #"+id+" / OUT "+(index+1)+" / select start";
            return true;
        }

        // Machine-body fallback: aim close to the actual mouth, never snap across a whole machine.
        public static int Find(EquipmentState equipment,CompactRules rules,bool output,float x,float y,float z)
        {
            if(equipment==null || rules==null || !CompactRules.Finite(x) || !CompactRules.Finite(y) || !CompactRules.Finite(z) || y<0 || y>1.8f)return -1;
            float nearest=1;int selected=-1;bool selectedOutput=false;
            for(int side=0;side<2;side++)
            {
                bool candidateOutput=side==1;
                for(int index=0;index<AutomationModel.PortCount(equipment.kind,candidateOutput);index++)
                {
                    var port=AutomationModel.Port(equipment,rules,candidateOutput,index);
                    float dx=x-port.x,dz=z-port.z,distance=dx*dx+dz*dz;
                    if(distance>=nearest)continue;
                    nearest=distance;selected=index;selectedOutput=candidateOutput;
                }
            }
            return selectedOutput==output?selected:-1;
        }
    }
}
