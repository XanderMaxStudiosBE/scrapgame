using System;

namespace Scrapshift.Compact
{
    // Compare only data represented by geometry. Timers, hand strokes, stack
    // identities and quantities do not rebuild an unchanged machine display.
    internal sealed class CompactEquipmentViewStamp
    {
        EquipmentKind kind;
        float x,z,yaw,width,depth;
        bool hasJob,ready,powered,overloaded;
        PartKind input;
        PartKind[] outputs=Array.Empty<PartKind>(),contents=Array.Empty<PartKind>();
        int primaryId;
        ScrapObjectKind primaryKind;

        internal bool GeometryMatches(EquipmentState equipment,EquipmentDefinition definition)
        {
            return kind==equipment.kind&&x==equipment.x&&z==equipment.z&&yaw==equipment.yaw&&
                width==definition.width&&depth==definition.depth;
        }
        internal bool DetailsMatch(EquipmentState equipment,bool hasPower,bool powerOverloaded)
        {
            bool showPower=equipment.kind==EquipmentKind.Tier1Scrapper||equipment.kind==EquipmentKind.Tier2Scrapper;
            if(showPower&&(powered!=hasPower||overloaded!=powerOverloaded))return false;
            var job=equipment.job;
            if(hasJob!=(job!=null))return false;
            if(job!=null)
            {
                if(ready!=job.ready||input!=job.input)return false;
                int count=0;
                if(job.ready)foreach(var output in job.yields)if(output.quantity>0)
                {if(count>=outputs.Length||outputs[count++]!=output.kind)return false;}
                if(count!=outputs.Length)return false;
            }
            int visible=Math.Min(3,equipment.contents.Count);
            if(contents.Length!=visible)return false;
            for(int i=0;i<visible;i++)if(contents[i]!=equipment.contents[i].kind)return false;
            return true;
        }
        internal bool Matches(EquipmentState equipment,EquipmentDefinition definition,bool hasPower,bool powerOverloaded)
        {
            if(!GeometryMatches(equipment,definition)||!DetailsMatch(equipment,hasPower,powerOverloaded))return false;
            var primary=equipment.industry==null?null:equipment.industry.primary;
            return primaryId==(primary==null?0:primary.id)&&(primary==null||primaryKind==primary.kind);
        }
        internal void Capture(EquipmentState equipment,EquipmentDefinition definition,bool hasPower,bool powerOverloaded)
        {
            kind=equipment.kind;x=equipment.x;z=equipment.z;yaw=equipment.yaw;width=definition.width;depth=definition.depth;
            powered=hasPower;overloaded=powerOverloaded;
            var job=equipment.job;hasJob=job!=null;ready=job!=null&&job.ready;input=job==null?default(PartKind):job.input;
            int count=0;if(job!=null&&job.ready)foreach(var output in job.yields)if(output.quantity>0)count++;
            if(outputs.Length!=count)outputs=new PartKind[count];
            count=0;if(job!=null&&job.ready)foreach(var output in job.yields)if(output.quantity>0)outputs[count++]=output.kind;
            count=Math.Min(3,equipment.contents.Count);if(contents.Length!=count)contents=new PartKind[count];
            for(int i=0;i<count;i++)contents[i]=equipment.contents[i].kind;
            var primary=equipment.industry==null?null:equipment.industry.primary;
            primaryId=primary==null?0:primary.id;primaryKind=primary==null?default(ScrapObjectKind):primary.kind;
        }
    }
}
