using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrapshift.Compact
{
    public sealed class CompactWorldHandles
    {
        public GameObject Shop, Sales, Delivery, Wire;
    }

    /// <summary>Fixed infrastructure only. No player equipment, inventory, physics clutter or frame updates.</summary>
    public static class CompactYardWorld
    {
        public const float HalfWidth=24,HalfDepth=18;
        public static readonly Vector3 OfficeAnchor=new Vector3(-17,0,-14);
        public static readonly Vector3 ShopAnchor=new Vector3(-19,0,-10.25f);
        public static readonly Vector3 SalesAnchor=new Vector3(-15,0,-10.25f);
        public static readonly Vector3 DeliveryAnchor=new Vector3(15,0,-13);
        public static readonly Vector3 WireAnchor=new Vector3(15,0,-7);
        public static readonly Vector3 DeliveryLightAnchor=new Vector3(21,3,-14.5f);

        public static CompactWorldHandles Build(Transform parent,bool combine=true)
        {
            var ground=Sector(parent,"Compact ground");
            YardGeometry.SurfaceBox("48 x 36 metre packed gravel",ground,new Vector3(0,-.2f,0),new Vector3(48,.4f,36),RetroSurface.Gravel);
            GroundLayers(ground);
            Batch(ground,combine);

            var north=Sector(parent,"Compact north boundary");
            Fence(north,new Vector3(0,1.05f,18),new Vector3(48,2.1f,.18f));
            BoundaryDress(north,true,18);
            Batch(north,combine);
            var west=Sector(parent,"Compact west boundary");
            Fence(west,new Vector3(-24,1.05f,0),new Vector3(.18f,2.1f,36));
            BoundaryDress(west,false,-24);
            Batch(west,combine);
            var east=Sector(parent,"Compact east boundary");
            Fence(east,new Vector3(24,1.05f,0),new Vector3(.18f,2.1f,36));
            BoundaryDress(east,false,24);
            Batch(east,combine);
            var south=Sector(parent,"Compact entrance");
            foreach(float x in new[]{-13f,13f})Fence(south,new Vector3(x,1.05f,-18),new Vector3(22,2.1f,.18f));
            // Gate is closed scenery; the clear approach remains protected for future deliveries.
            Fence(south,new Vector3(0,1,-18),new Vector3(4,2,.12f));
            foreach(float x in new[]{-2.15f,2.15f})
                YardGeometry.SurfaceBox("Entry gate upright",south,new Vector3(x,1.5f,-18),new Vector3(.18f,3,.18f),RetroSurface.DarkMetal);
            Label(south,"SCRAPSHIFT",new Vector3(0,2.75f,-18.16f),0,3.6f,.48f);
            Label(south,"SCRAPSHIFT",new Vector3(0,2.75f,-18.08f),180,3.6f,.48f);
            Batch(south,combine);

            var office=Sector(parent,"Compact office and sales");
            if(AuthoredYardProps.TryPlace("YardOffice",office,OfficeAnchor,out GameObject building))
                building.transform.localRotation=Quaternion.Euler(0,180,0);
            else YardGeometry.SurfaceBox("Office import fallback",office,OfficeAnchor+Vector3.up*1.5f,new Vector3(8,3,6),RetroSurface.WeatheredWood,false);
            var officeCollision=new GameObject("Fixed office collision");officeCollision.transform.SetParent(office,false);officeCollision.transform.localPosition=OfficeAnchor;
            var body=officeCollision.AddComponent<BoxCollider>();body.center=new Vector3(0,1.5f,0);body.size=new Vector3(8,3,6);
            if(YardWorldDressing.TryPlace("OfficeDetails",office,OfficeAnchor,out GameObject trim))trim.transform.localRotation=Quaternion.Euler(0,180,0);
            Label(office,"SCRAPSHIFT / OFFICE",OfficeAnchor+new Vector3(0,2.56f,3.19f),180,4.2f,.42f);
            Label(office,"YARD SHOP",ShopAnchor+new Vector3(0,2.02f,-.65f),180,1.9f,.40f);
            Label(office,"MATERIAL SALES",SalesAnchor+new Vector3(0,2.02f,-.65f),180,2.1f,.40f);
            // Shallow covered counter lamps reuse the existing four-light lighting budget.
            foreach(float x in new[]{-19f,-15f})
            {
                YardGeometry.SurfaceBox("Counter rain hood",office,new Vector3(x,3.34f,-10.75f),new Vector3(2.3f,.075f,1.1f),RetroSurface.CorrugatedMetal,false);
                Fixture(office,new Vector3(x,2.69f,-10.6f));
            }
            var handles=new CompactWorldHandles();
            handles.Shop=Counter(office,"Equipment and scrap shop",ShopAnchor,"YARD SHOP",false);
            handles.Sales=Counter(office,"Recovered-material buyer",SalesAnchor,"MATERIAL SALES",true);
            Batch(office,combine);

            var delivery=Sector(parent,"Compact receiving bay");
            if(AuthoredYardProps.TryPlace("CompactDeliveryTruck",delivery,new Vector3(20.5f,0,-15),out GameObject truck))
            {
                truck.transform.localRotation=Quaternion.Euler(0,180,0);
                var box=truck.AddComponent<BoxCollider>();box.center=new Vector3(0,.90f,0);box.size=new Vector3(2.25f,1.8f,4.5f);
            }
            foreach(float x in new[]{19f,23f})foreach(float z in new[]{-17.2f,-13f})
                YardGeometry.SurfaceBox("Receiving canopy post",delivery,new Vector3(x,1.825f,z),new Vector3(.09f,3.65f,.09f),RetroSurface.DarkMetal);
            YardGeometry.SurfaceBox("Receiving rain canopy",delivery,new Vector3(21,3.67f,-15.1f),new Vector3(4.4f,.08f,4.65f),RetroSurface.CorrugatedMetal,false);
            Fixture(delivery,DeliveryLightAnchor);
            handles.Delivery=Target(delivery,"Scrap deliveries",DeliveryAnchor,new Vector3(1.3f,.70f,.70f));
            YardGeometry.SurfaceBox("Delivery crate floor",handles.Delivery.transform,new Vector3(0,.09f,0),new Vector3(1.30f,.16f,.70f),RetroSurface.WeatheredWood,false);
            foreach(float x in new[]{-.63f,.63f})YardGeometry.SurfaceBox("Delivery crate side",handles.Delivery.transform,new Vector3(x,.30f,0),new Vector3(.055f,.50f,.70f),RetroSurface.WeatheredWood,false);
            YardGeometry.SurfaceBox("Delivery crate back",handles.Delivery.transform,new Vector3(0,.30f,-.32f),new Vector3(1.3f,.50f,.055f),RetroSurface.WeatheredWood,false);
            YardGeometry.SurfaceBox("Delivery crate lid",handles.Delivery.transform,new Vector3(0,.40f,0),new Vector3(1.45f,.10f,.80f),RetroSurface.WeatheredWood,false);
            Label(handles.Delivery.transform,"DELIVERIES",new Vector3(0,.47f,.37f),180,1.18f,.27f);
            handles.Wire=Target(delivery,"Renewable wire offcuts",WireAnchor,new Vector3(2.3f,1.05f,1.3f));
            AuthoredYardProps.TryPlace("WireCrate",handles.Wire.transform,Vector3.zero,out _);
            Label(handles.Wire.transform,"FREE WIRE OFFCUTS",new Vector3(0,.83f,.70f),180,2.05f,.31f);
            Batch(delivery,combine);

            var outside=Sector(parent,"Compact neighbouring landscape");
            var land=YardGeometry.SurfaceBox("Outside verge",outside,new Vector3(0,-.50f,0),new Vector3(95,.35f,78),RetroSurface.Gravel,false);
            land.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var road=YardGeometry.SurfaceBox("Neighbourhood service road",outside,new Vector3(0,-.008f,-23),new Vector3(90,.016f,7),RetroSurface.DarkMetal,false);
            road.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            for(int i=0;i<13;i++)
                YardGeometry.SurfaceBox("Faded service-road marking",outside,new Vector3(-42+i*7,.003f,-23),new Vector3(2.6f,.008f,.09f),RetroSurface.CorrugatedMetal,false);
            for(int i=0;i<15;i++)
            {
                float angle=i*Mathf.PI*2/15;
                var p=new Vector3(Mathf.Cos(angle)*31,0,Mathf.Sin(angle)*25);
                // Keep the entrance and service-road skyline open.
                if(p.z< -18 && Mathf.Abs(p.x)<17)continue;
                if(AuthoredYardProps.TryPlace("CompactBoundaryTree",outside,p,out GameObject tree))
                {
                    tree.transform.localRotation=Quaternion.Euler(0,i*47,0);
                    tree.transform.localScale=Vector3.one*(.8f+(i%3)*.14f);
                }
            }
            if(YardWorldDressing.TryPlace("IndustrialWorks",outside,new Vector3(9,0,42),out GameObject factory))factory.transform.localRotation=Quaternion.Euler(0,10,0);
            Batch(outside,combine);
            return handles;
        }

        static GameObject Counter(Transform parent,string name,Vector3 anchor,string label,bool scale)
        {
            var root=Target(parent,name,anchor,new Vector3(1.9f,.92f,.70f));
            if(scale && AuthoredYardProps.TryPlace("BuyingScale",root.transform,new Vector3(0,0,-.1f),out GameObject model))
            {model.transform.localRotation=Quaternion.Euler(0,180,0);model.transform.localScale=Vector3.one*.65f;}
            else
            {
                YardGeometry.SurfaceBox("Counter wood top",root.transform,new Vector3(0,.93f,-.1f),new Vector3(1.9f,.11f,.70f),RetroSurface.WeatheredWood,false);
                foreach(float x in new[]{-.72f,.72f})YardGeometry.SurfaceBox("Counter steel leg",root.transform,new Vector3(x,.44f,-.1f),new Vector3(.065f,.88f,.55f),RetroSurface.DarkMetal,false);
                YardGeometry.SurfaceBox("Purchase clipboard",root.transform,new Vector3(.25f,1.0f,-.05f),new Vector3(.26f,.02f,.33f),RetroSurface.WeatheredWood,false);
            }
            Label(root.transform,label,new Vector3(0,.78f,.275f),180,1.48f,.25f);
            return root;
        }
        static GameObject Target(Transform parent,string name,Vector3 p,Vector3 size)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.localPosition=p;
            var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,size.y*.5f,-.12f);box.size=size;
            return root;
        }
        static void Fixture(Transform parent,Vector3 position)
        {YardWorldDressing.TryPlace("FluorescentFixture",parent,position,out _);}
        static void Label(Transform parent,string text,Vector3 p,float yaw,float width,float height)
        {YardSignText.Plate(parent,text,p,width,height,yaw);}
        static void Fence(Transform parent,Vector3 p,Vector3 size)
        {
            var boundary=YardGeometry.SurfaceBox("Fixed fence",parent,p,size,RetroSurface.CorrugatedMetal);
            YardWorldDressing.FenceVisual(boundary,size);
            bool alongX=size.x>size.z;
            YardGeometry.SurfaceBox("Weathered lower fence sheets",parent,new Vector3(p.x,.31f,p.z),alongX ? new Vector3(size.x,.62f,.09f) : new Vector3(.09f,.62f,size.z),RetroSurface.CorrugatedMetal,false);
        }
        static void BoundaryDress(Transform parent,bool north,float edge)
        {
            int count=north ? 10 : 7;
            for(int i=0;i<count;i++)
            {
                float v=north ? -21+i*4.6f : -13+i*4.6f;
                var p=north ? new Vector3(v,0,edge- .35f) : new Vector3(edge>0 ? edge-.35f : edge+.35f,0,v);
                if(YardWorldDressing.TryPlace("WeedClump",parent,p,out GameObject plant))plant.transform.localRotation=Quaternion.Euler(0,i*61,0);
            }
            // Salvage dressing stays in a 0.75m boundary strip, never in interior construction space.
            if(north)
            {
                foreach(float x in new[]{-22.5f,22.5f})
                {
                    if(AuthoredYardProps.TryPlace("PalletBundle",parent,new Vector3(x,0,17.55f),out GameObject pallet))
                        pallet.transform.localScale=new Vector3(.46f,.62f,.40f);
                }
            }
        }
        static Transform Sector(Transform parent,string name)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        static void Batch(Transform parent,bool combine)
        {
            if(!combine)return;
            var objects=new List<GameObject>();
            foreach(var renderer in parent.GetComponentsInChildren<MeshRenderer>())
                if(renderer.enabled && renderer.GetComponent<TextMesh>()==null)objects.Add(renderer.gameObject);
            if(objects.Count>0)StaticBatchingUtility.Combine(objects.ToArray(),parent.gameObject);
        }

        static void GroundLayers(Transform parent)
        {
            var wear=new GroundLayer();var wet=new GroundLayer();
            // Fixed receiving/entry tyre trails are purely surface detail, not machine pads.
            wear.Patch(-.85f,-13,.40f,4.5f,1,0,.009f);
            wear.Patch(.85f,-13,.40f,4.5f,1,0,.009f);
            wear.Patch(7,-8,.40f,10,1,60,.009f);
            wear.Patch(8.4f,-7.3f,.40f,10,1,60,.009f);
            for(int i=0;i<12;i++)wear.Patch(-18+(i%4)*12,-5+(i/4)*9,2.8f,1.65f,i%4==0 ? 3 : 0,i*47,.010f);
            wet.Patch(4,-11,1.5f,.60f,2,25,.012f);
            wet.Patch(-17,8,1.0f,.65f,2,-10,.012f);
            wear.Build(parent,"Compact dust, moss and tyre wear","ScrapshiftWorld/GroundWear");
            wet.Build(parent,"Compact shallow rough puddles","ScrapshiftWorld/RoughPuddles");
        }
        sealed class GroundLayer
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<int> triangles=new List<int>();
            public void Patch(float x,float z,float rx,float rz,int tile,float yaw,float y)
            {
                int start=vertices.Count;const int sides=12;
                float c=Mathf.Cos(yaw*Mathf.Deg2Rad),s=Mathf.Sin(yaw*Mathf.Deg2Rad);
                var origin=new Vector2(tile%2*.5f,tile/2*.5f);
                vertices.Add(new Vector3(x,y,z));uv.Add(origin+Vector2.one*.25f);
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,u=Mathf.Cos(a),v=Mathf.Sin(a);
                    float variation=1+.04f*Mathf.Sin(i*2.7f+x);
                    vertices.Add(new Vector3(x+c*u*rx*variation+s*v*rz*variation,y,z-s*u*rx*variation+c*v*rz*variation));
                    uv.Add(origin+new Vector2(.25f+u*.24f,.25f+v*.24f));
                    triangles.Add(start);triangles.Add(start+1+(i+1)%sides);triangles.Add(start+1+i);
                }
            }
            public void Build(Transform parent,string name,string resource)
            {
                var material=YardMaterialBindings.Load(resource,parent);if(material==null)return;
                var root=new GameObject(name);root.transform.SetParent(parent,false);
                var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<ProceduralMeshOwner>().mesh=mesh;
                var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
    }
}
