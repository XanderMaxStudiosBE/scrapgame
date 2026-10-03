// Actual occupancy collection with narrow value-type adapters; not Unity geometry, rendering or JSON.
using System;
using System.Collections.Generic;
using Scrapshift.Compact;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    }
    public struct Bounds
    {
        public Vector3 center,size;
        public Bounds(Vector3 center,Vector3 size){this.center=center;this.size=size;}
    }
}
class DressingOccupancyRunner
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static UnityEngine.Bounds At(List<UnityEngine.Bounds> bounds,float x,float z)
    {foreach(var b in bounds)if(Near(b.center.x,x)&&Near(b.center.z,z))return b;throw new Exception("Missing envelope at "+x+", "+z);}
    static void Main()
    {
        var rules=new CompactRules();var state=new CompactYardState();var bounds=new List<UnityEngine.Bounds>();
        var player=new UnityEngine.Vector3(0,1.1f,-13);
        CompactDressingOccupancy.Collect(state,rules,player,bounds);
        Check(bounds.Count==11,"only fixed service objects and player should occupy an empty yard");
        var office=At(bounds,-17,-14);Check(office.size.x==8&&office.size.z==6,"office bounds match fixed body");
        Check(Near(At(bounds,20.5f,-15).size.z,4.5f),"truck footprint must be protected");
        foreach(var b in bounds)Check(b.size.x<48&&b.size.z<36,"ground or a long fence must not hide all dressing");
        CompactDressingOccupancy.Collect(state,rules,player,bounds);Check(bounds.Count==11,"collection clears and reuses its output list");
        Console.WriteLine("PASS service geometry and player envelopes omit yard-wide ground/fences and reuse output");

        var generator=new EquipmentState{id=1,kind=EquipmentKind.Generator,x=3,z=5,yaw=90};state.equipment.Add(generator);
        CompactDressingOccupancy.Collect(state,rules,player,bounds);
        var rotated=At(bounds,3,5);var definition=rules.Equipment(EquipmentKind.Generator);
        Check(Near(rotated.size.x,definition.depth)&&Near(rotated.size.z,definition.width),"rotation must swap rectangular dimensions");
        generator.yaw=45;CompactDressingOccupancy.Collect(state,rules,player,bounds);rotated=At(bounds,3,5);
        float diagonal=(definition.width+definition.depth)*(float)Math.Sqrt(.5);
        Check(Near(rotated.size.x,diagonal)&&Near(rotated.size.z,diagonal),"diagonal equipment needs conservative projected bounds");
        Check(generator.yaw==45&&state.equipment.Count==1,"occupancy must not move equipment or alter ownership");
        Console.WriteLine("PASS rotated and diagonally placed equipment project conservatively without state changes");

        state.scrap.Add(new LargeScrapJob{id=2,kind=ScrapObjectKind.Car,x=11,z=6});
        state.scrap.Add(new LargeScrapJob{id=3,kind=ScrapObjectKind.Refrigerator,x=14,z=6});
        state.items.Add(new CompactStack{id=4,x=-4,z=7});state.items.Add(new CompactStack{id=5,x=-7,z=4});state.carriedId=5;
        int count=bounds.Count;CompactDressingOccupancy.Collect(state,rules,player,bounds);
        Check(bounds.Count==count+3,"large scrap and loose item are included but carried item is excluded");
        Check(Near(At(bounds,11,6).size.z,4.7f)&&Near(At(bounds,14,6).size.x,1.2f),"large dismantling envelopes retained");
        Check(At(bounds,-4,7).size.x==.8f&&state.carriedId==5&&state.items.Count==2,"loose item remains accessible and unchanged");
        Console.WriteLine("PASS loose components and dismantling objects protected while carried view is excluded");

        state.equipment.Add(new EquipmentState{id=6,kind=EquipmentKind.Tier1Scrapper,x=6,z=5,yaw=0});
        state.equipment.Add(new EquipmentState{id=7,kind=EquipmentKind.Storage,x=12,z=10,yaw=0});
        state.belts.Add(new ConveyorLink{id=8,fromId=6,toId=7,fromPort=0,toPort=0,bendXFirst=true});
        var route=AutomationModel.Path(state.belts[0],state,rules);
        CompactDressingOccupancy.Collect(state,rules,player,bounds);
        foreach(var p in route)
        {
            bool covered=false;
            foreach(var b in bounds)if(Math.Abs(p.x-b.center.x)<=b.size.x*.5f+.0001f&&Math.Abs(p.z-b.center.z)<=b.size.z*.5f+.0001f)covered=true;
            Check(covered,"entire conveyor route, including endpoint stubs and elbow, needs protection");
        }
        Check(state.belts.Count==1&&state.equipment.Count==3&&state.items.Count==2,"route collection must not mutate model lists");
        Console.WriteLine("PASS full conveyor route envelopes protect endpoints, stubs and elbow without changing transport state");
    }
}
