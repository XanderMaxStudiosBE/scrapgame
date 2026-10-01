using UnityEngine;

namespace Scrapshift
{
    public sealed class YardBusinessVisual
    {
        public readonly Transform wireStock, copperStock, rack;
        public readonly TextMesh orderText, commissionText;
        public static readonly Vector3 WirePosition = YardBootstrap.StationPosition(YardLandmark.WireStorage);
        public static readonly Vector3 CopperPosition = YardBootstrap.StationPosition(YardLandmark.CopperStorage);
        public static readonly Vector3 OrderPosition = YardBootstrap.StationPosition(YardLandmark.Orders);

        YardBusinessVisual(Transform wire, Transform copper, TextMesh text, TextMesh commission, Transform rack)
        { wireStock = wire; copperStock = copper; orderText = text; commissionText=commission; this.rack = rack; }

        public static YardBusinessVisual Build(Transform parent)
        {
            Transform wire = Storage(parent, WirePosition, MaterialKind.Wire);
            Transform copper = Storage(parent, CopperPosition, MaterialKind.Copper);
            var board = new GameObject("Customer order board").transform;
            board.SetParent(parent, false); board.localPosition = OrderPosition;
            board.gameObject.AddComponent<InteractionTarget>().kind = TargetKind.OrderBoard;
            var collider = board.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 1.6f, 0); collider.size = new Vector3(2.8f, 2.8f, .3f);
            // Keep the original low footprint. The wider notice surface leaves previously
            // dropped material outside that footprint targetable underneath its raised wings.
            var noticeCollider=board.gameObject.AddComponent<BoxCollider>();
            noticeCollider.center=new Vector3(0,1.9f,0);noticeCollider.size=new Vector3(4.8f,1.8f,.3f);
            YardGeometry.SurfaceBox("Customer notice board", board, new Vector3(0, 1.9f, 0), new Vector3(4.8f, 1.8f, .15f), RetroSurface.WeatheredWood, false);
            foreach (float x in new[] { -2.2f, 2.2f })
                YardGeometry.SurfaceBox("Notice board post", board, new Vector3(x, 1.5f, .06f), new Vector3(.13f, 3, .13f), RetroSurface.DarkMetal, false);
            var text=CustomerNote(board,-1.15f,"Copper customer note");
            var commission=CustomerNote(board,1.15f,"Restoration customer note");
            YardGeometry.MountedSign(board,"NEIGHBOURHOOD REQUESTS",new Vector3(0,0,-.12f),3.12f,4.3f);
            var rack=new GameObject("Storage upgrade rack").transform; rack.SetParent(parent,false);rack.localPosition=new Vector3(14,0,12);
            if(!AuthoredYardProps.TryPlace("StorageRack",rack,Vector3.zero,out GameObject rackModel))
            {
                foreach(float x in new[]{-1.1f,1.1f})YardGeometry.SurfaceBox("Rack upright",rack,new Vector3(x,1.3f,0),new Vector3(.12f,2.6f,1.2f),RetroSurface.DarkMetal,false);
                foreach(float y in new[]{.25f,1.35f,2.45f})YardGeometry.SurfaceBox("Rack shelf",rack,new Vector3(0,y,0),new Vector3(2.4f,.10f,1.2f),RetroSurface.WeatheredWood,false);
            }
            return new YardBusinessVisual(wire, copper, text,commission,rack);
        }
        static TextMesh CustomerNote(Transform board,float x,string name)
        {
            CozyYardDetails.Accent(board,name,new Vector3(x,1.9f,-.095f),new Vector3(2.05f,1.55f,.015f),YardGeometry.Ivory);
            // Small worn tape strips pin each useful request to the shared wooden board.
            foreach(float edge in new[]{-.68f,.68f})
                CozyYardDetails.Accent(board,"Note tape",new Vector3(x+edge,2.65f,-.112f),new Vector3(.2f,.13f,.012f),new Color(.67f,.64f,.49f));
            var status=new GameObject(name+" live text").transform;
            status.SetParent(board,false);status.localPosition=new Vector3(x,1.9f,-.125f);
            var text=status.gameObject.AddComponent<TextMesh>();
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=48;text.characterSize=.022f;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=YardGeometry.Charcoal;
            text.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            return text;
        }
        static Transform Storage(Transform parent, Vector3 pos, MaterialKind kind)
        {
            var bin = new GameObject(kind + " storage bin").transform;
            bin.SetParent(parent, false); bin.localPosition = pos;
            bin.gameObject.AddComponent<InteractionTarget>().kind = kind == MaterialKind.Wire ? TargetKind.WireStorage : TargetKind.CopperStorage;
            RetroSurface surface = kind == MaterialKind.Wire ? RetroSurface.WeatheredWood : RetroSurface.RustPaint;
            YardGeometry.SurfaceBox("Storage bin base", bin, new Vector3(0, .12f, 0), new Vector3(2.5f, .24f, 1.5f), surface);
            foreach (float x in new[] { -1.18f, 1.18f })
                YardGeometry.SurfaceBox("Storage bin side", bin, new Vector3(x, .55f, 0), new Vector3(.14f, .86f, 1.5f), surface);
            YardGeometry.SurfaceBox("Storage bin back", bin, new Vector3(0, .55f, .67f), new Vector3(2.5f, .86f, .16f), surface);
            YardGeometry.SurfaceBox("Storage bin front", bin, new Vector3(0, .35f, -.67f), new Vector3(2.5f, .46f, .16f), surface);
            YardGeometry.MountedSign(bin, kind == MaterialKind.Wire ? "WIRE STORAGE" : "COPPER STORAGE", new Vector3(0, 0, .78f), 2.05f);
            var stock = new GameObject("Stored bundle display").transform; stock.SetParent(bin, false);
            for (int i = 0; i < 4; i++)
            {
                var bundle = YardGeometry.Bundle(kind, stock);
                bundle.transform.localPosition = new Vector3((i % 2 - .5f) * .7f, .43f + i / 2 * .22f, .02f);
                foreach (var c in bundle.GetComponentsInChildren<Collider>()) c.enabled = false;
                bundle.SetActive(false);
            }
            return stock;
        }
        public void Refresh(YardModel model)
        {
            rack.gameObject.SetActive(model.Owns(YardUpgrade.StorageRack));
            UpdateStock(wireStock, model.StoredBundles(MaterialKind.Wire));
            UpdateStock(copperStock, model.StoredBundles(MaterialKind.Copper));
            var order = model.CurrentOrder;
            orderText.text = order.customer + "\n" + (model.State.orderAccepted ? model.State.orderDelivered + " / " : "") +
                order.copper + " COPPER\nREWARD / €" + order.reward + "\n" + (model.State.orderAccepted ? "DELIVER COPPER HERE" : "VIEW HERE / NO DEADLINE");
            var request=model.CurrentCommission;
            commissionText.text=request.customer+"\n"+(model.State.commissionAccepted?model.State.commissionDelivered+" / ":"")+request.quantity+
                (request.kind==MaterialKind.RestoredRadio?" TESTED RADIO":" TESTED FAN")+(request.quantity==1?"":"S")+"\nREWARD / €"+model.CommissionReward+"\n"+
                (model.State.commissionAccepted?"DELIVER TESTED ITEMS HERE":"VIEW HERE / NO DEADLINE");
        }
        static void UpdateStock(Transform stock, int count)
        {
            for (int i = 0; i < stock.childCount; i++) stock.GetChild(i).gameObject.SetActive(i < count);
        }
    }
}
