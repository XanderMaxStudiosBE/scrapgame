using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scrapshift.Compact
{
    // Read-only yard-local envelopes. Dressing never becomes inventory or a construction restriction.
    public static class CompactDressingOccupancy
    {
        public static void Collect(CompactYardState state,CompactRules rules,Vector3 player,List<Bounds> result)
        {
            if(state==null || rules==null || result==null)throw new ArgumentNullException("state/rules/result");
            result.Clear();
            Add(result,-17,-14,8,6); // Office body; keep its frontage available to the counters.
            Add(result,-19,-10.37f,1.9f,.70f);Add(result,-15,-10.37f,1.9f,.70f);
            Add(result,20.5f,-15,2.25f,4.5f); // Parked delivery truck.
            Add(result,15,-13.12f,1.3f,.70f);Add(result,15,-7.12f,2.3f,1.3f);
            foreach(float x in new[]{19f,23f})foreach(float z in new[]{-17.2f,-13f})Add(result,x,z,.09f,.09f);
            // Fence and ground envelopes are deliberately omitted: they span the whole boundary/yard.
            foreach(var equipment in state.equipment)
            {
                var definition=rules.Equipment(equipment.kind);
                double angle=equipment.yaw*Math.PI/180;
                float c=(float)Math.Abs(Math.Cos(angle)),s=(float)Math.Abs(Math.Sin(angle));
                Add(result,equipment.x,equipment.z,c*definition.width+s*definition.depth,s*definition.width+c*definition.depth);
            }
            foreach(var scrap in state.scrap)
                Add(result,scrap.x,scrap.z,scrap.kind==ScrapObjectKind.Car?2.4f:1.2f,scrap.kind==ScrapObjectKind.Car?4.7f:1.2f);
            foreach(var item in state.items)if(item.id!=state.carriedId)Add(result,item.x,item.z,.8f,.8f);
            foreach(var belt in state.belts)
            {
                var path=AutomationModel.Path(belt,state,rules);
                for(int i=1;i<path.Length;i++)Add(result,(path[i-1].x+path[i].x)*.5f,(path[i-1].z+path[i].z)*.5f,
                    Math.Abs(path[i].x-path[i-1].x)+AutomationModel.CorridorWidth,
                    Math.Abs(path[i].z-path[i-1].z)+AutomationModel.CorridorWidth);
            }
            Add(result,player.x,player.z,.9f,.9f);
        }
        static void Add(List<Bounds> result,float x,float z,float width,float depth)
        {result.Add(new Bounds(new Vector3(x,1.5f,z),new Vector3(width,3,depth)));}
    }
}
