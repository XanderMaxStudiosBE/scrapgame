using System;

namespace Scrapshift.Compact
{
    public static class CompactPortSelection
    {
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
