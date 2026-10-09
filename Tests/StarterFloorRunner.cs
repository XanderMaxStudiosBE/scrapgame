// Source declaration arithmetic with narrow value adapters; not Unity colliders or rendering.
using System;
using System.Collections.Generic;
using System.Globalization;
using Scrapshift.Compact;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 up {get{return new Vector3(0,1,0);}}
        public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
        public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}
    }
    public struct Bounds
    {
        public Vector3 center,size;
        public Bounds(Vector3 center,Vector3 size){this.center=center;this.size=size;}
        public Vector3 min {get{return new Vector3(center.x-size.x*.5f,center.y-size.y*.5f,center.z-size.z*.5f);}}
        public Vector3 max {get{return new Vector3(center.x+size.x*.5f,center.y+size.y*.5f,center.z+size.z*.5f);}}
    }
}

internal struct StarterFloorRect
{
    public readonly double minX,minZ,maxX,maxZ;
    public StarterFloorRect(double x,double z,double width,double depth,double yaw,double padding)
    {
        double c=Math.Abs(Math.Cos(yaw*Math.PI/180)),s=Math.Abs(Math.Sin(yaw*Math.PI/180));
        double halfX=(c*width+s*depth)*.5+padding,halfZ=(s*width+c*depth)*.5+padding;
        minX=Math.Max(-ConstructionModel.HalfWidth,x-halfX);maxX=Math.Min(ConstructionModel.HalfWidth,x+halfX);
        minZ=Math.Max(-ConstructionModel.HalfDepth,z-halfZ);maxZ=Math.Min(ConstructionModel.HalfDepth,z+halfZ);
    }
    public double Area {get{return Math.Max(0,maxX-minX)*Math.Max(0,maxZ-minZ);}}
}

internal static class StarterFloorRunner
{
    static string Number(double value){return value.ToString("0.00",CultureInfo.InvariantCulture);}
    static void Add(List<StarterFloorRect> rectangles,double x,double z,double width,double depth,double yaw,double padding)
    {
        var rectangle=new StarterFloorRect(x,z,width,depth,yaw,padding);
        if(rectangle.Area>0)rectangles.Add(rectangle);
    }
    static double Sum(List<StarterFloorRect> rectangles)
    {double result=0;foreach(var rectangle in rectangles)result+=rectangle.Area;return result;}
    static double Union(List<StarterFloorRect> rectangles)
    {
        var edges=new List<double>();
        foreach(var rectangle in rectangles){edges.Add(rectangle.minX);edges.Add(rectangle.maxX);}
        edges.Sort();double result=0;
        for(int i=1;i<edges.Count;i++)
        {
            if(edges[i]==edges[i-1])continue;
            double x=(edges[i]+edges[i-1])*.5;
            var covering=new List<StarterFloorRect>();
            foreach(var rectangle in rectangles)if(x>rectangle.minX&&x<rectangle.maxX)covering.Add(rectangle);
            covering.Sort((a,b)=>a.minZ.CompareTo(b.minZ));
            double occupied=0,end=double.NegativeInfinity;
            foreach(var rectangle in covering)
            {occupied+=Math.Max(0,rectangle.maxZ-Math.Max(end,rectangle.minZ));end=Math.Max(end,rectangle.maxZ);}
            result+=(edges[i]-edges[i-1])*occupied;
        }
        return result;
    }
    static void Main()
    {
        var rules=new CompactRules();var model=new ScrappingModel(rules);
        var patches=CompactYardClutter.Describe();
        double yardArea=4d*ConstructionModel.HalfWidth*ConstructionModel.HalfDepth;
        Console.WriteLine("Actual stock declarations: "+patches.Length+" patches; fresh yard: "+model.State.equipment.Count+" equipment, "+model.State.scrap.Count+" scrap; floor: "+Number(yardArea)+" m2.");
        foreach(double padding in new[]{0d,.15,.4})
        {
            // Add every protected rectangle and every stock declaration, including hidden stock.
            // Deliberately double-count overlaps for a conservative lower bound on free space.
            var rectangles=new List<StarterFloorRect>(StarterFloorFixedReservations.Describe());
            double fixedArea=Sum(rectangles);
            foreach(var equipment in model.State.equipment)
            {
                var definition=rules.Equipment(equipment.kind);
                Add(rectangles,equipment.x,equipment.z,definition.width,definition.depth,equipment.yaw,padding);
            }
            foreach(var scrap in model.State.scrap)
                Add(rectangles,scrap.x,scrap.z,scrap.kind==ScrapObjectKind.Car?2.4:1.2,scrap.kind==ScrapObjectKind.Car?4.7:1.2,0,padding);
            double freshArea=Sum(rectangles)-fixedArea;int inside=0;
            foreach(var patch in patches)
            {
                if(patch.outside)continue;
                inside++;
                Add(rectangles,patch.footprint.center.x,patch.footprint.center.z,patch.footprint.size.x,patch.footprint.size.z,0,padding);
            }
            double occupied=Sum(rectangles),free=yardArea-occupied,declaredFree=yardArea-Union(rectangles);
            if(free<yardArea*.5)throw new Exception("Conservative starter floor falls below 50% with "+Number(padding)+" m padding per side.");
            Console.WriteLine("PASS padding "+Number(padding)+" m: "+inside+" inside stock footprints; protected "+Number(fixedArea)+" m2; fresh equipment/scrap "+Number(freshArea)+" m2; conservative free >= "+Number(free)+" m2 ("+Number(free/yardArea*100)+"%); exact declaration-union free "+Number(declaredFree)+" m2.");
        }
        Console.WriteLine("Scope: actual fresh Core state, extracted protected construction footprints and compiled stock declarations; not native colliders, renderer envelopes or usable walking/build paths.");
    }
}
