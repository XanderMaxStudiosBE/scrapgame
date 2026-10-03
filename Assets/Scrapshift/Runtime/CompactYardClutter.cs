using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrapshift.Compact
{
    /// <summary>Optional static salvage scenery. Bounds/occupancy use yard-local metres and ignore height.</summary>
    public static class CompactYardClutter
    {
        public sealed class Patch
        {
            public readonly string name, sector;
            public readonly Bounds footprint;
            public readonly bool outside;
            internal readonly int kind, variation;
            internal readonly float yaw;
            internal Patch(string name, string sector, Vector3 p, Vector3 size, int kind, int variation, float yaw = 0)
            {
                this.name=name;this.sector=sector;this.kind=kind;this.variation=variation;this.yaw=yaw;
                footprint=new Bounds(p+Vector3.up*size.y*.5f,size);
                outside=footprint.min.x>=24 || footprint.max.x<=-24 || footprint.min.z>=18 || footprint.max.z<=-18;
            }
        }

        public static Patch[] Describe()
        {
            var patches=new List<Patch>();
            // Separate culling regions outside the fence; no scenery over the entrance road.
            for(int i=0;i<4;i++)
            {
                patches.Add(new Patch("North sorted salvage "+i,"North salvage",new Vector3(-18+i*12,0,21.1f),new Vector3(4.0f,2.0f,2.6f),i,i));
                patches.Add(new Patch("West sorted salvage "+i,"West salvage",new Vector3(-27,0,-8+i*7),new Vector3(3.4f,2.0f,3.4f),(i+1)%4,i+4));
                patches.Add(new Patch("East sorted salvage "+i,"East salvage",new Vector3(27,0,-8+i*7),new Vector3(3.4f,2.0f,3.4f),(i+2)%4,i+8));
            }
            for(int i=0;i<3;i++)
                patches.Add(new Patch("North overflow salvage "+i,"North salvage",new Vector3(-12+i*12,0,21.1f),new Vector3(3.4f,2.0f,2.6f),i+1,i+12));
            patches.Add(new Patch("West pallet and tyre stock","West salvage",new Vector3(-27,0,-15),new Vector3(3.4f,2.0f,3.4f),0,15));
            patches.Add(new Patch("East pipe offcut stock","East salvage",new Vector3(27,0,-15),new Vector3(3.4f,2.0f,3.4f),2,16));
            for(int i=0;i<4;i++)
                patches.Add(new Patch("Fence cable and rim offcuts "+i,"North fence offcuts",new Vector3(-16+i*10,0,17.65f),new Vector3(2.4f,.8f,.55f),4,i));
            for(int i=0;i<3;i++)
            {
                patches.Add(new Patch("West fence offcuts "+i,"West fence offcuts",new Vector3(-23.65f,0,-3+i*7),new Vector3(.55f,.8f,2.4f),4,i,90));
                patches.Add(new Patch("East fence offcuts "+i,"East fence offcuts",new Vector3(23.65f,0,-3+i*7),new Vector3(.55f,.8f,2.4f),4,i+3,90));
            }
            patches.Add(new Patch("Office repair spares","Service pocket offcuts",new Vector3(-22.15f,0,-14),new Vector3(1.0f,1.1f,2.4f),5,0));
            patches.Add(new Patch("Receiving offcut drums","Service pocket offcuts",new Vector3(22.2f,0,-8.5f),new Vector3(1.0f,1.1f,2.4f),5,1));
            // Small flat discarded tabs add wear without reserving construction space or obstructing feet.
            for(int i=0;i<16;i++)
                patches.Add(new Patch("Scattered ground offcuts "+i,"Flat ground offcuts",new Vector3(-15+(i%4)*10,0,-6+(i/4)*5),new Vector3(1.1f,.04f,.7f),6,i));
            return patches.ToArray();
        }

        public static bool IsBlocked(Patch patch,IReadOnlyList<Bounds> occupied)
        {
            if(occupied==null)return !patch.outside;
            var area=patch.footprint;
            foreach(var other in occupied)
            {
                const float clearance=.4f;
                if(area.min.x<=other.max.x+clearance && area.max.x>=other.min.x-clearance &&
                    area.min.z<=other.max.z+clearance && area.max.z>=other.min.z-clearance)return true;
            }
            return false;
        }

        public static GameObject Build(Transform parent,IReadOnlyList<Bounds> occupied,bool combine=true)
        {
            var root=new GameObject("Optional sorted salvage clutter");root.transform.SetParent(parent,false);
            var visibility=root.AddComponent<CompactClutterVisibility>();
            var sectors=new Dictionary<string,Transform>();
            foreach(var patch in Describe())
            {
                if(!sectors.TryGetValue(patch.sector,out Transform sector))
                {
                    var group=new GameObject(patch.sector);group.transform.SetParent(root.transform,false);
                    sector=group.transform;sectors.Add(patch.sector,sector);
                }
                var cluster=new GameObject(patch.name);cluster.transform.SetParent(sector,false);
                cluster.transform.localPosition=new Vector3(patch.footprint.center.x,0,patch.footprint.center.z);
                cluster.transform.localRotation=Quaternion.Euler(0,patch.yaw,0);
                BuildPatch(cluster.transform,patch.kind,patch.variation);
                visibility.patches.Add(patch);visibility.clusters.Add(cluster);
                foreach(var renderer in cluster.GetComponentsInChildren<MeshRenderer>())
                {renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;}
            }
            if(combine)
                foreach(var sector in sectors.Values)
                {
                    var renderers=sector.GetComponentsInChildren<MeshRenderer>();
                    var objects=new GameObject[renderers.Length];for(int i=0;i<objects.Length;i++)objects[i]=renderers[i].gameObject;
                    if(objects.Length>0)StaticBatchingUtility.Combine(objects,sector.gameObject);
                }
            RefreshVisibility(root,occupied);
            return root;
        }

        public static void RefreshVisibility(GameObject root,IReadOnlyList<Bounds> occupied)
        {
            if(root==null)return;
            var visibility=root.GetComponent<CompactClutterVisibility>();if(visibility==null)return;
            for(int i=0;i<visibility.clusters.Count;i++)
            {
                var cluster=visibility.clusters[i];if(cluster==null)continue;
                bool visible=!IsBlocked(visibility.patches[i],occupied);
                if(cluster.activeSelf!=visible)cluster.SetActive(visible);
            }
        }

        static void BuildPatch(Transform parent,int kind,int variation)
        {
            switch(kind)
            {
                case 0: // Recognizable tyre stock, rusty rims and a wood pallet.
                    Model("PalletBundle",parent,new Vector3(0,0,.45f),new Vector3(1.4f,.25f,1.15f),0);
                    for(int column=0;column<3;column++)
                        for(int layer=0;layer<2+column%2;layer++)
                            Ring("Discarded tyre",parent,new Vector3((column-1)*.86f,.14f+layer*.25f,-.30f),.38f,.20f,.23f,RetroSurface.WireInsulation);
                    Ring("Bare wheel rim",parent,new Vector3(1.38f,.17f,.38f),.31f,.19f,.25f,RetroSurface.DarkMetal);
                    break;
                case 1: // Open metal sorting bin: broad bent panels produce a visible silhouette.
                    Box("Scrap bin floor",parent,new Vector3(0,.13f,0),new Vector3(2.6f,.16f,1.8f),RetroSurface.DarkMetal);
                    foreach(float x in new[]{-1.25f,1.25f})Box("Sorting bin side",parent,new Vector3(x,.45f,0),new Vector3(.10f,.75f,1.8f),RetroSurface.RustPaint);
                    for(int i=0;i<4;i++)
                    {
                        var panel=Box("Bent appliance panel",parent,new Vector3(-.8f+i*.53f,.55f+i%2*.1f,0),new Vector3(.68f,.045f,1.4f),i%2==0?RetroSurface.RustPaint:RetroSurface.CorrugatedMetal);
                        panel.transform.localRotation=Quaternion.Euler(0,(i-2)*9,15+i*4);
                    }
                    Model("CompactMotor",parent,new Vector3(.7f,.18f,.44f),new Vector3(.46f,.48f,.40f),35);
                    break;
                case 2: // Exposed pipe rack with offcuts and a compact appliance compressor.
                    foreach(float x in new[]{-.8f,.8f})Box("Pipe cradle",parent,new Vector3(x,.15f,0),new Vector3(.12f,.3f,1.25f),RetroSurface.RustPaint);
                    for(int i=0;i<5;i++)
                        YardProps.Cylinder("Recovered steel pipe",parent,new Vector3(0,.34f+(i/3)*.25f,-.4f+(i%3)*.36f),.13f,2.6f,RetroSurface.DarkMetal,Quaternion.Euler(0,0,90));
                    Model("CompactCompressor",parent,new Vector3(.8f,0,.86f),new Vector3(.5f,.50f,.45f),variation*29);
                    break;
                case 3: // Salvage drums, stacked pallet and substantial cable coils.
                    Model("PalletBundle",parent,new Vector3(-.60f,0,0),new Vector3(1.4f,.25f,1.4f),0);
                    foreach(float z in new[]{-.48f,.48f})
                    {
                        YardProps.Cylinder("Used salvage drum",parent,new Vector3(-.58f,.67f,z),.33f,.88f,RetroSurface.RustPaint,Quaternion.identity);
                        Ring("Drum rolled lip",parent,new Vector3(-.58f,1.11f,z),.335f,.30f,.035f,RetroSurface.DarkMetal);
                    }
                    Model("CompactInsulationCoil",parent,new Vector3(.72f,0,-.34f),new Vector3(.75f,.4f,.75f),variation*23);
                    Model("CompactMotor",parent,new Vector3(.73f,0,.53f),new Vector3(.5f,.52f,.42f),variation*17);
                    break;
                case 4: // Low fence remnants fit inside a 0.75m strip and hide near player equipment.
                    for(int i=0;i<3;i++)
                    {
                        var panel=Box("Fence-side metal offcut",parent,new Vector3(-.72f+i*.65f,.05f+i*.055f,0),new Vector3(.58f,.08f,.38f),i%2==0?RetroSurface.RustPaint:RetroSurface.DarkMetal);
                        panel.transform.localRotation=Quaternion.Euler(0,variation%2==0?8:-8,0);
                    }
                    Model("CompactInsulationCoil",parent,new Vector3(.60f,.18f,0),new Vector3(.38f,.28f,.38f),0);
                    break;
                case 5: // Protected service pockets, not shop/receiving approach routes.
                    Model("PalletBundle",parent,new Vector3(0,0,0),new Vector3(.8f,.20f,1.5f),0);
                    if(variation==0)
                    {
                        Model("CompactMotor",parent,new Vector3(0,.20f,-.45f),new Vector3(.60f,.60f,.55f),-20);
                        Model("CompactCompressor",parent,new Vector3(0,.20f,.30f),new Vector3(.60f,.65f,.55f),15);
                    }
                    else
                    {
                        foreach(float z in new[]{-.48f,.48f})
                            YardProps.Cylinder("Cable offcut drum",parent,new Vector3(0,.61f,z),.32f,.82f,RetroSurface.RustPaint,Quaternion.identity);
                        Model("CompactInsulationCoil",parent,new Vector3(0,1.02f,.48f),new Vector3(.40f,.08f,.40f),0);
                    }
                    break;
                case 6:
                    var tab=Box("Discarded sheet-metal tab",parent,new Vector3(-.22f,.01f,0),new Vector3(.52f,.016f,.25f),RetroSurface.RustPaint);
                    tab.transform.localRotation=Quaternion.Euler(0,variation*17,0);
                    var cut=Box("Flat cable clip",parent,new Vector3(.32f,.012f,.12f),new Vector3(.18f,.020f,.22f),RetroSurface.DarkMetal);
                    cut.transform.localRotation=Quaternion.Euler(0,variation*13,0);
                    var strip=Box("Discarded rubber offcut",parent,new Vector3(.20f,.015f,-.11f),new Vector3(.38f,.018f,.08f),RetroSurface.WireInsulation);
                    strip.transform.localRotation=Quaternion.Euler(0,-variation*19,0);
                    break;
            }
        }

        static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,RetroSurface surface)
        {return YardGeometry.SurfaceBox(name,parent,p,size,surface,false);}

        // Fit imported geometry after yaw, making our occupancy envelope independent of FBX authoring extents.
        static void Model(string resource,Transform parent,Vector3 p,Vector3 maximum,float yaw)
        {
            if(!AuthoredYardProps.TryPlace(resource,parent,Vector3.zero,out GameObject model))return;
            model.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var filters=model.GetComponentsInChildren<MeshFilter>();
            bool found=false;var bounds=new Bounds();
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;if(mesh==null)continue;
                Bounds local=mesh.bounds;
                for(int i=0;i<8;i++)
                {
                    Vector3 corner=local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    Vector3 point=parent.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                }
            }
            if(!found)return;
            float scale=Mathf.Min(maximum.x/Mathf.Max(.001f,bounds.size.x),Mathf.Min(maximum.y/Mathf.Max(.001f,bounds.size.y),maximum.z/Mathf.Max(.001f,bounds.size.z)));
            model.transform.localScale=Vector3.one*scale;
            model.transform.localPosition=p-new Vector3(bounds.center.x*scale,bounds.min.y*scale,bounds.center.z*scale);
        }

        static void Ring(string name,Transform parent,Vector3 p,float outer,float inner,float height,RetroSurface surface)
        {
            const int sides=12;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 oa=new Vector3(Mathf.Cos(a)*outer,0,Mathf.Sin(a)*outer),ob=new Vector3(Mathf.Cos(b)*outer,0,Mathf.Sin(b)*outer);
                Vector3 ia=oa*(inner/outer),ib=ob*(inner/outer),up=Vector3.up*height;
                Quad(vertices,uv,triangles,oa,oa+up,ob+up,ob);
                Quad(vertices,uv,triangles,ib,ib+up,ia+up,ia);
                Quad(vertices,uv,triangles,oa+up,ia+up,ib+up,ob+up);
                Quad(vertices,uv,triangles,ob,ib,ia,oa);
            }
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p-Vector3.up*height*.5f;
            var mesh=new Mesh{name=name+" / hollow twelve-sided ring"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<ProceduralMeshOwner>().mesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=YardMaterialBindings.Load("ScrapshiftMaterials/"+surface,parent);
        }
        static void Quad(List<Vector3> v,List<Vector2> uv,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int i=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);
            uv.Add(Vector2.zero);uv.Add(new Vector2(0,Vector3.Distance(a,b)));uv.Add(new Vector2(Vector3.Distance(a,d),Vector3.Distance(a,b)));uv.Add(new Vector2(Vector3.Distance(a,d),0));
            t.Add(i);t.Add(i+1);t.Add(i+2);t.Add(i);t.Add(i+2);t.Add(i+3);
        }
    }

}
