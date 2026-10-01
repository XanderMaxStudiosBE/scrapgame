using UnityEngine;

namespace Scrapshift
{
    /// <summary>Walkable districts around the original working hub. No per-frame simulation or rigidbody clutter.</summary>
    public static class ScrapyardWorld
    {
        static readonly Color Sky = new Color(.69f, .72f, .68f);
        public static Color SkyColor { get { return Sky; } }

        public static void Build(Transform parent, bool combine = true)
        {
            // Small regional batches keep distant districts eligible for frustum culling.
            var lanes = Sector(parent, "Ground and lanes");
            Box(lanes, "Packed gravel", new Vector3(0, -.25f, 0), new Vector3(YardWorldLayout.HalfWidth * 2, .5f, YardWorldLayout.HalfDepth * 2), RetroSurface.Gravel);
            // Pale worn lane edges make the hub easy to find without a minimap.
            // Sparse wheel ruts break up the repeated gravel without expensive decals.
            foreach (float x in new[] { -1.15f, 1.15f })
                for (int i = 0; i < 6; i++)
                    CozyYardDetails.Accent(lanes, "Worn delivery tyre track", new Vector3(x, .006f, -33 + i * 9), new Vector3(.16f, .009f, 5), new Color32(89, 83, 67, 255));
            foreach (float x in new[] { -5f, 5f })
                Box(lanes, "North south lane edge", new Vector3(x, .012f, 0), new Vector3(.14f, .02f, 72), RetroSurface.WeatheredWood, false);
            foreach (float z in new[] { -10f, -16f })
                Box(lanes, "East west lane edge", new Vector3(0, .012f, z), new Vector3(88, .02f, .14f), RetroSurface.WeatheredWood, false);
            Batch(lanes, combine);

            var north = Sector(parent, "North loading district");
            Fence(north, new Vector3(0, 1.1f, 39.5f), new Vector3(96, 2.2f, .2f));
            // Individual container rows have accessible aisles, not a solid wall of scenery.
            for (int i = 0; i < 6; i++)
                Container(north, new Vector3(-34 + i * 13, 0, 30), i % 2 == 0 ? RetroSurface.RustPaint : RetroSurface.CorrugatedMetal);
            Box(north, "Loading platform", new Vector3(24, .3f, 22), new Vector3(15, .6f, 6), RetroSurface.DarkMetal);
            Box(north, "Lower dock step", new Vector3(24, .1f, 17.8f), new Vector3(15, .2f, .8f), RetroSurface.WeatheredWood);
            Box(north, "Upper dock step", new Vector3(24, .2f, 18.6f), new Vector3(15, .4f, .8f), RetroSurface.WeatheredWood);
            for (int i = 0; i < 4; i++) Pallet(north, new Vector3(19 + i * 3, .6f, 22));
            // A stationary gantry is a landmark, not an operable crane.
            foreach (float x in new[] { -9f, 9f }) Box(north, "Gantry leg", new Vector3(x, 4, 24), new Vector3(.6f, 8, .6f), RetroSurface.RustPaint);
            Box(north, "Gantry beam", new Vector3(0, 8, 24), new Vector3(19, .7f, .8f), RetroSurface.RustPaint);
            Box(north, "Parked hoist", new Vector3(-3, 7.3f, 24), new Vector3(1.3f, .8f, 1.2f), RetroSurface.DarkMetal, false);
            Box(north, "Hoist cable", new Vector3(-3, 5.5f, 24), new Vector3(.08f, 3, .08f), RetroSurface.DarkMetal, false);
            for (int i = 0; i < 16; i++)
                CozyYardDetails.Accent(north, "Dock safety stripe", new Vector3(17 + i * .9f, .607f, 19.3f), new Vector3(.45f, .01f, .45f), i % 2 == 0 ? CozyYardDetails.Ochre : YardGeometry.Charcoal);
            Board(north, "LOADING & STORAGE\nWORKSHOP / SOUTH", new Vector3(0, 0, 18));
            Batch(north, combine);

            var west = Sector(parent, "West vehicle salvage");
            Fence(west, new Vector3(-47.5f, 1.1f, 0), new Vector3(.2f, 2.2f, 80));
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    Car(west, new Vector3(-37 + col * 7, 0, -2 + row * 8), (row + col) % 2 == 0);
            Board(west, "VEHICLE SALVAGE\nRECOVER WIRE / RETURN TO WORKSHOP", new Vector3(-25, 0, -8));
            Batch(west, combine);

            var east = Sector(parent, "East metal sorting");
            Fence(east, new Vector3(47.5f, 1.1f, 0), new Vector3(.2f, 2.2f, 80));
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    ScrapBin(east, new Vector3(23 + col * 7, 0, -1 + row * 8), (row + col) % 2 == 0);
            // A sheltered sorting bay gives the silhouette another recognizable destination.
            foreach (float x in new[] { 20f, 42f })
                foreach (float z in new[] { -22f, -29f }) Box(east, "Sorting shelter post", new Vector3(x, 2.1f, z), new Vector3(.2f, 4.2f, .2f), RetroSurface.DarkMetal);
            Box(east, "Sorting shelter roof", new Vector3(31, 4.2f, -25.5f), new Vector3(24, .18f, 9), RetroSurface.CorrugatedMetal, false);
            for (int i = 0; i < 4; i++) Pallet(east, new Vector3(23 + i * 5, 0, -27));
            Board(east, "METAL SORTING\nWORKSHOP / WEST", new Vector3(25, 0, -8));
            Batch(east, combine);


            var south = Sector(parent, "South entry district");
            foreach (float x in new[] { -26f, 26f }) Fence(south, new Vector3(x, 1.1f, -39.5f), new Vector3(44, 2.2f, .2f));
            Fence(south, new Vector3(0, 1, -39.5f), new Vector3(8, 2, .2f));
            foreach (float x in new[] { -4.5f, 4.5f }) Box(south, "Entry gate pillar", new Vector3(x, 2.1f, -38), new Vector3(.4f, 4.2f, .4f), RetroSurface.RustPaint);
            Box(south, "Entry arch", new Vector3(0, 4.1f, -38), new Vector3(10, .55f, .4f), RetroSurface.DarkMetal);
            Building(south, new Vector3(-16, 0, -30));
            for (int i = 0; i < 3; i++) Pallet(south, new Vector3(16 + i * 5, 0, -30));
            Board(south, "SCRAPSHIFT\nWORKSHOP / NORTH", new Vector3(0, 0, -29));
            Board(south, "YARD OFFICE", new Vector3(-16, 0, -24));
            CozyYardDetails.Accent(south, "Scrapshift entry nameboard", new Vector3(0, 4.12f, -38.23f), new Vector3(8, .5f, .04f), CozyYardDetails.Sage);
            YardGeometry.Sign(south, "S C R A P S H I F T", new Vector3(0, 4.12f, -38.26f));
            Batch(south, combine);

            var hub = Sector(parent, "Workshop surroundings");
            // Existing roof and drums retain their shapes; the old enclosing walls are gone.
            YardProps.WorkshopSurroundings(hub);
            CozyYardDetails.Workshop(hub);
            Board(hub, "WORKSHOP\nWIRE > STRIP > SELL", new Vector3(0, 0, 12));
            Board(hub, "< VEHICLE SALVAGE    METAL SORTING >", new Vector3(0, 0, -18));
            Batch(hub, combine);

            foreach (var site in YardWorldLayout.SalvageSites)
                Supply(parent, new Vector3(site.x, 0, site.z), site.name);

            var horizon = Sector(parent, "Distant landscape");
            var grass = YardGeometry.Box("Outside grassland", horizon, new Vector3(0, -.65f, 0), new Vector3(180, .4f, 160), YardGeometry.Olive, false);
            grass.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2 / 16;
                Vector3 p = new Vector3(Mathf.Cos(angle) * 67, 0, Mathf.Sin(angle) * 59);
                Box(horizon, "Tree trunk", p + Vector3.up * 2, new Vector3(.7f, 4, .7f), RetroSurface.WeatheredWood, false);
                CozyYardDetails.Faceted(horizon, "Faceted tree lower crown", p + Vector3.up * (4.8f + i % 3), new Vector3(7, 5, 7), i % 2 == 0 ? YardGeometry.Olive : CozyYardDetails.Sage);
                CozyYardDetails.Faceted(horizon, "Faceted tree upper crown", p + Vector3.up * (7 + i % 3), new Vector3(5, 4.5f, 5), CozyYardDetails.Sage);
            }
            // Edge vegetation stays away from district routes and the original hub.
            for (int i = 0; i < 12; i++)
            {
                float x = i % 2 == 0 ? -44 : 44;
                CozyYardDetails.Faceted(horizon, "Fence line shrub", new Vector3(x, .6f, -32 + i * 5), new Vector3(2.5f, 1.2f, 2), YardGeometry.Olive);
            }
            Batch(horizon, combine);
        }

        static Transform Sector(Transform parent, string name)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
        }
        static void Batch(Transform sector, bool combine)
        {
            if (!combine) return;
            var geometry = new System.Collections.Generic.List<GameObject>();
            foreach (var renderer in sector.GetComponentsInChildren<MeshRenderer>())
                // Dynamic font atlas updates must retain the live TextMesh geometry.
                if (renderer.GetComponent<TextMesh>() == null) geometry.Add(renderer.gameObject);
            StaticBatchingUtility.Combine(geometry.ToArray(), sector.gameObject);
        }
        static GameObject Box(Transform p, string name, Vector3 position, Vector3 size, RetroSurface surface, bool collider = true)
        {
            var go = YardGeometry.SurfaceBox(name, p, position, size, surface, collider);
            // Tiny decorative parts contribute no useful silhouette to the shadow map.
            if (!collider && size.y < 1) go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
        static void Fence(Transform p, Vector3 position, Vector3 size)
        { Box(p, "Perimeter fence", position, size, RetroSurface.CorrugatedMetal); }
        static void Board(Transform p, string text, Vector3 ground)
        {
            // Elevated boards keep the lane underneath unobstructed.
            foreach (float x in new[] { -2.4f, 2.4f }) Box(p, "Wayfinding post", ground + new Vector3(x, 1.7f, 0), new Vector3(.12f, 3.4f, .12f), RetroSurface.DarkMetal);
            Box(p, "Wayfinding board", ground + new Vector3(0, 3, .08f), new Vector3(5, 1, .12f), RetroSurface.RustPaint, false);
            YardGeometry.Sign(p, text, ground + new Vector3(0, 3.05f, -.01f));
            YardGeometry.Sign(p, text, ground + new Vector3(0, 3.05f, .17f), 180);
        }
        static void Supply(Transform p, Vector3 pos, string label)
        {
            var crate = YardProps.Delivery(p, pos); crate.name = label + " wire crate";
            crate.GetComponent<InteractionTarget>().displayName = label;
            YardGeometry.Sign(crate.transform, label + "\nFREE SCRAP WIRE", new Vector3(0, 2.25f, 0));
            // These markers remain outside the stationary scenery batches.
        }
        static void Container(Transform p, Vector3 pos, RetroSurface surface)
        {
            Box(p, "Storage container", pos + new Vector3(0, 1.4f, 0), new Vector3(9, 2.8f, 4), surface);
            foreach (float x in new[] { -4.4f, 4.4f })
                CozyYardDetails.Accent(p, "Container corner rail", pos + new Vector3(x, 1.4f, -2.06f), new Vector3(.12f, 2.7f, .12f), CozyYardDetails.Sage);
            foreach (float x in new[] { -.12f, .12f }) Box(p, "Container door latch", pos + new Vector3(x, 1.4f, -2.04f), new Vector3(.05f, 2, .08f), RetroSurface.DarkMetal, false);
            CozyYardDetails.Accent(p, "Container stencil plate", pos + new Vector3(-2.5f, 1.7f, -2.07f), new Vector3(2.4f, .65f, .03f), CozyYardDetails.DustyBlue);
            YardGeometry.Sign(p, "SCRAP / STORAGE", pos + new Vector3(-2.5f, 1.7f, -2.10f));
        }
        static void Pallet(Transform p, Vector3 pos)
        {
            Box(p, "Pallet base", pos + Vector3.up * .12f, new Vector3(2.4f, .24f, 1.6f), RetroSurface.WeatheredWood);
            for (int i = 0; i < 3; i++) Box(p, "Bundled scrap panels", pos + new Vector3(0, .4f + i * .22f, 0), new Vector3(2, .18f, 1.3f), i % 2 == 0 ? RetroSurface.RustPaint : RetroSurface.DarkMetal);
        }
        static void Car(Transform p, Vector3 pos, bool rust)
        {
            var surface = rust ? RetroSurface.RustPaint : RetroSurface.CorrugatedMetal;
            Box(p, "Salvaged vehicle body", pos + new Vector3(0, .65f, 0), new Vector3(2.1f, .65f, 4.4f), surface);
            Box(p, "Vehicle cabin", pos + new Vector3(0, 1.25f, .1f), new Vector3(1.8f, .7f, 1.9f), surface);
            CozyYardDetails.Accent(p, "Painted vehicle roof", pos + new Vector3(0, 1.62f, .1f), new Vector3(1.82f, .08f, 1.92f), rust ? CozyYardDetails.Ochre : CozyYardDetails.DustyBlue);
            CozyYardDetails.Accent(p, "Windshield", pos + new Vector3(0, 1.27f, -.87f), new Vector3(1.5f, .48f, .035f), CozyYardDetails.Window);
            CozyYardDetails.Accent(p, "Rear window", pos + new Vector3(0, 1.27f, 1.07f), new Vector3(1.5f, .48f, .035f), CozyYardDetails.Window);
            foreach (float x in new[] { -.92f, .92f })
            {
                CozyYardDetails.Accent(p, "Vehicle side glass", pos + new Vector3(x, 1.27f, .1f), new Vector3(.035f, .48f, 1.55f), CozyYardDetails.Window);
                CozyYardDetails.Accent(p, "Vehicle window pillar", pos + new Vector3(x, 1.27f, .1f), new Vector3(.055f, .56f, .09f), YardGeometry.Charcoal);
                CozyYardDetails.Accent(p, "Vehicle door handle", pos + new Vector3(x * 1.18f, .89f, .45f), new Vector3(.035f, .06f, .18f), YardGeometry.Ivory);
            }
            foreach (float x in new[] { -.72f, .72f })
                CozyYardDetails.Accent(p, "Faded vehicle headlight", pos + new Vector3(x, .72f, -2.23f), new Vector3(.34f, .2f, .04f), CozyYardDetails.WarmWindow);
            CozyYardDetails.Accent(p, "Vehicle grille", pos + new Vector3(0, .65f, -2.23f), new Vector3(.8f, .22f, .04f), YardGeometry.Charcoal);
            foreach (float x in new[] { -1.08f, 1.08f })
                foreach (float z in new[] { -1.35f, 1.35f })
                    YardProps.Cylinder("Low poly vehicle tyre", p, pos + new Vector3(x, .4f, z), .35f, .25f, RetroSurface.WireInsulation, Quaternion.Euler(0, 0, 90));
            Box(p, "Vehicle bumper", pos + new Vector3(0, .52f, -2.23f), new Vector3(2.15f, .16f, .15f), RetroSurface.DarkMetal, false);
        }
        static void ScrapBin(Transform p, Vector3 pos, bool rust)
        {
            Box(p, "Sorting bin floor", pos + Vector3.up * .15f, new Vector3(4, .3f, 4), RetroSurface.DarkMetal);
            foreach (float x in new[] { -1.95f, 1.95f }) Box(p, "Sorting bin wall", pos + new Vector3(x, .65f, 0), new Vector3(.1f, 1.3f, 4), RetroSurface.RustPaint);
            Box(p, "Sorting bin back", pos + new Vector3(0, .65f, 1.95f), new Vector3(4, 1.3f, .1f), RetroSurface.RustPaint);
            // A low front makes the contents visible, with a single coarse collision volume.
            Box(p, "Scrap stock collision", pos + new Vector3(0, .6f, 0), new Vector3(3.6f, .8f, 3.6f), rust ? RetroSurface.RustPaint : RetroSurface.DarkMetal);
            for (int i = 0; i < 4; i++)
            {
                var scrap = Box(p, "Angled salvage panel", pos + new Vector3((i % 2 - .5f) * 1.5f, 1.1f, (i / 2 - .5f) * 1.5f), new Vector3(1.4f, .18f, 1.4f), RetroSurface.CorrugatedMetal, false);
                scrap.transform.localRotation = Quaternion.Euler(i * 7, i * 33, -i * 6);
            }
        }
        static void Building(Transform p, Vector3 pos)
        {
            Box(p, "Yard office", pos + Vector3.up * 1.5f, new Vector3(8, 3, 6), RetroSurface.WeatheredWood);
            CozyYardDetails.Office(p, pos);
            Box(p, "Office door", pos + new Vector3(-2, 1.1f, 3.02f), new Vector3(1.2f, 2.2f, .04f), RetroSurface.RustPaint, false);

        }
    }
}
