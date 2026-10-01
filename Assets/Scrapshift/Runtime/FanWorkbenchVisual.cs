using UnityEngine;
namespace Scrapshift
{
    public sealed class FanWorkbenchVisual
    {
        public static readonly Vector3 Position = new Vector3(-7, 0, 12);
        public static readonly Vector3 SupplyPosition = new Vector3(-33, 0, -19);
        public static readonly Vector3 DiaryPosition = new Vector3(6, 0, -27);
        public readonly Transform display, rotor, copper, supplyStock;
        FanWorkbenchVisual(Transform display, Transform rotor, Transform copper, Transform supplyStock)
        { this.display = display; this.rotor = rotor; this.copper = copper; this.supplyStock = supplyStock; }
        public static FanWorkbenchVisual Build(Transform parent)
        {
            var bench = YardProps.Workbench(parent, Position);
            bench.root.name = "Appliance restoration workbench";
            bench.root.GetComponent<InteractionTarget>().kind = TargetKind.FanBench;
            bench.materialDisplay.gameObject.SetActive(false);
            YardGeometry.MountedSign(parent, "RESTORATION BENCH", Position + Vector3.forward * .7f, 2.55f);
            if (AuthoredYardProps.TryPlace("WorkshopCanopy", parent, Position, out GameObject shelter))
                shelter.transform.localScale = new Vector3(.26f,.70f,.50f);
            var display = new GameObject("Fan job display").transform; display.SetParent(bench.root.transform,false); display.localPosition = new Vector3(0,1.1f,-.05f);
            Transform rotor = null;
            if (AuthoredYardProps.TryPlace("SalvageFanFrame", display, Vector3.zero, out GameObject frame))
            {
                if (AuthoredYardProps.TryPlace("FanRotor", display, new Vector3(0,.65f,0), out GameObject blades)) rotor = blades.transform;
            }
            else
            {
                var fallback = YardItemVisual.Create(MaterialKind.BrokenFan,display);
                foreach(var collider in fallback.GetComponentsInChildren<Collider>()) collider.enabled = false;
            }
            var fanHit = display.gameObject.AddComponent<BoxCollider>();
            fanHit.center = new Vector3(0,.46f,0); fanHit.size = new Vector3(.6f,.95f,.4f);
            var copper = YardGeometry.Bundle(MaterialKind.Copper, bench.root.transform).transform;
            copper.localPosition = new Vector3(0,1.22f,-.05f);
            foreach(var collider in copper.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var supply = YardProps.Delivery(parent, SupplyPosition);
            supply.name = "Appliance salvage crate"; supply.GetComponent<InteractionTarget>().kind = TargetKind.FanSupply;
            var stock = YardItemVisual.Create(MaterialKind.BrokenFan,supply.transform); stock.transform.localPosition = new Vector3(0,.5f,0);
            // Aiming at the displayed fan resolves to the supply marker on its parent.
            foreach(var collider in stock.GetComponentsInChildren<Collider>()) collider.enabled = true;
            YardGeometry.MountedSign(supply.transform,"APPLIANCE SALVAGE",new Vector3(0,0,.7f));
            var diary = YardGeometry.SurfaceBox("Yard diary stand",parent,DiaryPosition+new Vector3(0,.75f,0),new Vector3(1.3f,1.5f,.5f),RetroSurface.WeatheredWood);
            diary.AddComponent<InteractionTarget>().kind = TargetKind.DayBoard;
            YardGeometry.MountedSign(parent,"YARD DIARY",DiaryPosition+Vector3.forward*.28f,1.9f,1.65f);
            return new FanWorkbenchVisual(display,rotor,copper,stock.transform);
        }
        public void Refresh(YardModel m)
        {
            display.gameObject.SetActive(m.State.fanStage != FanStage.Empty && m.State.fanStage != FanStage.CopperReady);
            supplyStock.gameObject.SetActive(m.State.fansTakenToday < m.Rules.fanDailyLimit);
            copper.gameObject.SetActive(m.State.fanStage == FanStage.CopperReady);
        }
        public void Step(YardModel m, float seconds)
        {
            if (rotor != null && m.State.fanStage == FanStage.Tested) rotor.Rotate(0,0,480*seconds,Space.Self);
        }
    }
}
