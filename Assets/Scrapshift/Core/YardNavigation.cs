using System;
namespace Scrapshift
{
    public enum YardLandmark { Automatic, Delivery, Bench, Buyer, Machine, WireStorage, CopperStorage, Orders, FanSupply, FanBench, Diary, VehicleWire, SalvageWire, SortingWire }
    public struct YardDestination
    {
        public readonly YardLandmark landmark;
        public readonly string name, mapLabel;
        public readonly float x, z;
        public YardDestination(YardLandmark landmark, string name, float x, float z)
        { this.landmark=landmark; this.name=name; this.x=x; this.z=z; mapLabel=((int)landmark).ToString("00")+"  "+name; }
        public float Distance(float playerX, float playerZ)
        { double dx=(double)x-playerX,dz=(double)z-playerZ; return (float)Math.Sqrt(dx*dx+dz*dz); }
        public string Direction(float playerX, float playerZ, float yaw)
        {
            if (Distance(playerX,playerZ)<=3.2f) return "Nearby";
            double bearing=Math.Atan2((double)x-playerX,(double)z-playerZ)*180/Math.PI;
            double angle=(bearing-yaw)%360;
            if(angle>180)angle-=360;if(angle < -180)angle+=360;
            if(Math.Abs(angle)<=25)return "Ahead";
            if(Math.Abs(angle)>=155)return "Behind";
            return angle>0 ? "Right" : "Left";
        }
    }
    // The same metre coordinates build stations and guide players. No pathfinding or world scans.
    public static class YardNavigation
    {
        public static readonly YardDestination[] Destinations =
        {
            new YardDestination(YardLandmark.Delivery,"Wire delivery",-7,2),
            new YardDestination(YardLandmark.Bench,"Stripping bench",-2.5f,4),
            new YardDestination(YardLandmark.Buyer,"Scrap buyer",2.2f,4),
            new YardDestination(YardLandmark.Machine,"Powered stripper",7,2),
            new YardDestination(YardLandmark.WireStorage,"Wire storage",14,2),
            new YardDestination(YardLandmark.CopperStorage,"Copper storage",14,7),
            new YardDestination(YardLandmark.Orders,"Customer board",7,12),
            new YardDestination(YardLandmark.FanSupply,"Appliance salvage",-33,-19),
            new YardDestination(YardLandmark.FanBench,"Restoration bench",-7,12),
            new YardDestination(YardLandmark.Diary,"Yard diary",6,-27),
            new YardDestination(YardLandmark.VehicleWire,"Vehicle wire crate",YardWorldLayout.SalvageSites[0].x,YardWorldLayout.SalvageSites[0].z),
            new YardDestination(YardLandmark.SalvageWire,"Salvage wire crate",YardWorldLayout.SalvageSites[1].x,YardWorldLayout.SalvageSites[1].z),
            new YardDestination(YardLandmark.SortingWire,"Sorting wire crate",YardWorldLayout.SalvageSites[2].x,YardWorldLayout.SalvageSites[2].z)
        };
        public static YardDestination Get(YardLandmark landmark)
        {
            int index=(int)landmark-1;
            if(index<0||index>=Destinations.Length)throw new ArgumentOutOfRangeException("landmark");
            return Destinations[index];
        }
        public static YardDestination NearestWire(float x, float z)
        {
            var nearest=Get(YardLandmark.Delivery);
            foreach(var point in Destinations)
                if((point.landmark==YardLandmark.VehicleWire||point.landmark==YardLandmark.SalvageWire||point.landmark==YardLandmark.SortingWire) && point.Distance(x,z)<nearest.Distance(x,z)) nearest=point;
            return nearest;
        }
        public static YardDestination Recommend(YardModel m, float x, float z)
        {
            var s=m.State;var c=m.Carried;
            if(c!=null && c.kind==MaterialKind.BrokenFan)return Get(YardLandmark.FanBench);
            if(c!=null && c.kind==MaterialKind.RestoredFan)return Get(YardLandmark.Buyer);
            if(c!=null && c.kind==MaterialKind.Copper)return Get(s.orderAccepted?YardLandmark.Orders:YardLandmark.Buyer);
            if(!s.machineOwned && s.money>=m.Rules.machinePrice)return Get(YardLandmark.Machine);
            if(c!=null)
            {
                if(s.machineOwned && s.machineRemaining==0 && s.machineOutput==0)return Get(YardLandmark.Machine);
                return Get(YardLandmark.Bench);
            }
            if(s.benchOutput>0)return Get(YardLandmark.Bench);
            if(s.machineOutput>0)return Get(YardLandmark.Machine);
            if(s.benchLoaded)return Get(YardLandmark.Bench);
            if(s.fanStage!=FanStage.Empty)return Get(YardLandmark.FanBench);
            if(s.orderAccepted && m.StoredBundles(MaterialKind.Copper)>0)return Get(YardLandmark.CopperStorage);
            if(m.StoredBundles(MaterialKind.Wire)>0)return Get(YardLandmark.WireStorage);
            if(s.orderAccepted || s.machineRemaining>0)return NearestWire(x,z);
            if(s.machineOwned && (m.CanBuyUpgrade(YardUpgrade.StorageRack)||m.CanBuyUpgrade(YardUpgrade.HandTools)||m.CanBuyUpgrade(YardUpgrade.MachineTuning)))return Get(YardLandmark.Diary);
            return NearestWire(x,z);
        }
    }
    public sealed class YardNavigator
    {
        public YardLandmark Selected { get; private set; }
        public void Select(YardLandmark landmark)
        { if(landmark!=YardLandmark.Automatic)YardNavigation.Get(landmark);Selected=landmark; }
        public YardDestination Resolve(YardModel model,float x,float z)
        { return Selected==YardLandmark.Automatic?YardNavigation.Recommend(model,x,z):YardNavigation.Get(Selected); }
    }
}
