using UnityEngine;

namespace Scrapshift
{
    public sealed class YardBusinessVisual
    {
        public readonly Transform wireStock, copperStock;
        public readonly TextMesh orderText;
        public static readonly Vector3 WirePosition = new Vector3(14, 0, 2);
        public static readonly Vector3 CopperPosition = new Vector3(14, 0, 7);
        public static readonly Vector3 OrderPosition = new Vector3(7, 0, 12);

        YardBusinessVisual(Transform wire, Transform copper, TextMesh text)
        { wireStock = wire; copperStock = copper; orderText = text; }

        public static YardBusinessVisual Build(Transform parent)
        {
            Transform wire = Storage(parent, WirePosition, MaterialKind.Wire);
            Transform copper = Storage(parent, CopperPosition, MaterialKind.Copper);
            var board = new GameObject("Customer order board").transform;
            board.SetParent(parent, false); board.localPosition = OrderPosition;
            board.gameObject.AddComponent<InteractionTarget>().kind = TargetKind.OrderBoard;
            var collider = board.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 1.6f, 0); collider.size = new Vector3(2.8f, 2.8f, .3f);
            YardGeometry.SurfaceBox("Customer notice board", board, new Vector3(0, 1.9f, 0), new Vector3(2.8f, 1.8f, .15f), RetroSurface.WeatheredWood, false);
            foreach (float x in new[] { -1.2f, 1.2f })
                YardGeometry.SurfaceBox("Notice board post", board, new Vector3(x, 1.5f, .06f), new Vector3(.13f, 3, .13f), RetroSurface.DarkMetal, false);
            CozyYardDetails.Accent(board, "Pinned customer note", new Vector3(0, 1.9f, -.095f), new Vector3(2.45f, 1.55f, .015f), YardGeometry.Ivory);
            YardGeometry.Sign(board, "CUSTOMER ORDERS", new Vector3(0, 3.12f, -.12f));
            var status = new GameObject("Live order note").transform; status.SetParent(board, false); status.localPosition = new Vector3(0, 1.9f, -.12f);
            var text = status.gameObject.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 48; text.characterSize = .028f;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = YardGeometry.Charcoal;
            text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return new YardBusinessVisual(wire, copper, text);
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
            YardGeometry.Sign(bin, kind == MaterialKind.Wire ? "WIRE STORAGE\nSTORE / TAKE ONE BUNDLE" : "COPPER STORAGE\nSTORE / TAKE ONE BUNDLE", new Vector3(0, 2.05f, 0));
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
            UpdateStock(wireStock, model.StoredBundles(MaterialKind.Wire));
            UpdateStock(copperStock, model.StoredBundles(MaterialKind.Copper));
            var order = model.CurrentOrder;
            orderText.text = order.customer + "\n" + (model.State.orderAccepted ? model.State.orderDelivered + " / " : "") +
                order.copper + " COPPER\nREWARD / €" + order.reward + "\n" + (model.State.orderAccepted ? "DELIVER COPPER HERE" : "ACCEPT HERE / NO DEADLINE");
        }
        static void UpdateStock(Transform stock, int count)
        {
            for (int i = 0; i < stock.childCount; i++) stock.GetChild(i).gameObject.SetActive(i < count);
        }
    }
}
