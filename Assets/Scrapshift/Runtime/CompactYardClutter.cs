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
            internal readonly float yaw, scale;
            internal Patch(string name, string sector, Vector3 p, Vector3 size, int kind, int variation, float yaw = 0, float scale = 1)
            {
                this.name=name;this.sector=sector;this.kind=kind;this.variation=variation;this.yaw=yaw;this.scale=scale;
                footprint=new Bounds(p+Vector3.up*size.y*.5f,size);
                outside=footprint.min.x>=24 || footprint.max.x<=-24 || footprint.min.z>=18 || footprint.max.z<=-18;
            }
        }

        public static Patch[] Describe()
        {
            var patches=new List<Patch>();
            // Far stock is secondary to larger visible stock inside the yard.
            for(int i=0;i<4;i++)
                patches.Add(new Patch("North sorted salvage "+i,"North salvage",new Vector3(-18+i*12,0,21.1f),new Vector3(4,2,2.6f),i,i));
            for(int i=0;i<2;i++)
            {
                patches.Add(new Patch("West sorted salvage "+i,"West salvage",new Vector3(-27,0,-5+i*15),new Vector3(3.4f,2,3.4f),i+1,i+4));
                patches.Add(new Patch("East sorted salvage "+i,"East salvage",new Vector3(27,0,-5+i*15),new Vector3(3.4f,2,3.4f),i+2,i+8));
            }
            // Optional stock leaves the central 34 x 24m walking view open, and hides near any saved construction.
            int[] northKinds={7,0,1,7,3};
            for(int i=0;i<5;i++)
                patches.Add(new Patch("North working stock "+i,"North working stock",new Vector3(-15+i*7.5f,0,15.8f),new Vector3(4.6f,2.6f,3.2f),northKinds[i],i,0,1.3f));
            int[] westKinds={1,3,7},eastKinds={2,7,0};
            for(int i=0;i<3;i++)
            {
                patches.Add(new Patch("West working stock "+i,"West working stock",new Vector3(-20.8f,0,-1+i*6.5f),new Vector3(3.2f,2.6f,4.6f),westKinds[i],i+5,90,1.3f));
                patches.Add(new Patch("East working stock "+i,"East working stock",new Vector3(20.8f,0,-1+i*6.5f),new Vector3(3.2f,2.6f,4.6f),eastKinds[i],i+8,-90,1.3f));
            }
            patches.Add(new Patch("Office repair spares","Service pocket offcuts",new Vector3(-22.15f,0,-14),new Vector3(1,1.1f,2.4f),5,0));
            patches.Add(new Patch("Receiving offcut drums","Service pocket offcuts",new Vector3(22.2f,0,-8.5f),new Vector3(1,1.1f,2.4f),5,1));
            for(int i=0;i<8;i++)
                patches.Add(new Patch("Scattered ground offcuts "+i,"Flat ground offcuts",new Vector3(-15+(i%4)*10,0,-4+(i/4)*12),new Vector3(1.1f,.04f,.7f),6,i));
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

        public static GameObject Build(Transform parent,IReadOnlyList<Bounds> occupied,bool combine=true,bool solidStock=false)
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
                cluster.transform.localScale=Vector3.one*patch.scale;
                BuildPatch(cluster.transform,patch.kind,patch.variation);
                if(patch.kind!=6 && patch.kind!=4)ContactGrounding(cluster.transform,patch);
                if(solidStock && !patch.outside && patch.kind!=6 && patch.kind!=4)
                {
                    // Coarse replaceable stock volume. Gameplay/build raycasts omit layer 2; the player still collides.
                    cluster.layer=2;
                    var size=patch.footprint.size;
                    if(Mathf.Abs(patch.yaw)%180>45){float swap=size.x;size.x=size.z;size.z=swap;}
                    var collider=cluster.AddComponent<BoxCollider>();collider.size=size/patch.scale;
                    collider.center=Vector3.up*patch.footprint.size.y/(2*patch.scale);
                }
                visibility.patches.Add(patch);visibility.clusters.Add(cluster);
                foreach(var renderer in cluster.GetComponentsInChildren<MeshRenderer>())
                {renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=renderer.name!="Stock contact grounding";}
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
                        {
                            var centre=new Vector3((column-1)*.86f,.14f+layer*.25f,-.30f);
                            if(!OldTyreVisuals.TryPlace(parent,centre,variation*31+column*43+layer*67))
                                Ring("Discarded tyre",parent,centre,.38f,.20f,.23f,RetroSurface.WireInsulation);
                        }
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
                        if(!LocalCarPartsVisuals.TryPlace(parent,new Vector3(0,.20f,-.45f),-20))
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
                case 7: // Recognizable discarded appliances, not tiny specks outside the fence.
                    Model("PalletBundle",parent,new Vector3(-.5f,0,.20f),new Vector3(2.3f,.24f,1.5f),0);
                    Model("CompactRefrigerator",parent,new Vector3(-1.02f,.15f,.32f),new Vector3(.80f,1.6f,.86f),-12);
                    Model("CompactRefrigerator",parent,new Vector3(-.08f,.15f,.45f),new Vector3(.74f,1.5f,.82f),9);
                    Model("CompactCompressor",parent,new Vector3(.94f,0,.45f),new Vector3(.60f,.60f,.60f),variation*29);
                    Model("CompactMotor",parent,new Vector3(.46f,0,-.54f),new Vector3(.6f,.60f,.52f),-25);
                    Ring("Recovered copper coil",parent,new Vector3(1.05f,.11f,-.50f),.42f,.25f,.18f,RetroSurface.Copper);
                    Ring("Recovered copper coil",parent,new Vector3(1.05f,.28f,-.50f),.40f,.25f,.16f,RetroSurface.Copper);
                    var door=Box("Detached appliance door",parent,new Vector3(-1.02f,.39f,-.50f),new Vector3(.64f,.065f,.90f),RetroSurface.CorrugatedMetal);
                    door.transform.localRotation=Quaternion.Euler(30,-15,0);
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

        // A bounded soft ellipse follows each pooled stock root; geometry/occupancy hide together.
        static void ContactGrounding(Transform parent,Patch patch)
        {
            var material=Resources.Load<Material>("ScrapshiftLighting/ContactShade");if(material==null)return;
            const int sides=12;
            float width=patch.footprint.size.x,depth=patch.footprint.size.z;
            if(Mathf.Abs(patch.yaw)%180>45){float swap=width;width=depth;depth=swap;}
            float rx=width*.47f/patch.scale,rz=depth*.47f/patch.scale,y=.006f/patch.scale;
            var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            vertices.Add(new Vector3(0,y,0));colors.Add(new Color(1,1,1,.20f));
            for(int ring=0;ring<2;ring++)for(int i=0;i<sides;i++)
            {
                float angle=i*Mathf.PI*2/sides,radius=ring==0?.58f:1;
                vertices.Add(new Vector3(Mathf.Cos(angle)*rx*radius,y,Mathf.Sin(angle)*rz*radius));
                colors.Add(new Color(1,1,1,ring==0?.13f:0));
            }
            for(int i=0;i<sides;i++)
            {
                int next=(i+1)%sides,a=1+i,b=1+next,c=a+sides,d=b+sides;
                triangles.Add(0);triangles.Add(b);triangles.Add(a);
                triangles.Add(a);triangles.Add(b);triangles.Add(d);
                triangles.Add(a);triangles.Add(d);triangles.Add(c);
            }
            var go=new GameObject("Stock contact grounding");go.transform.SetParent(parent,false);
            var mesh=new Mesh{name="Bounded stock contact grounding"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<ProceduralMeshOwner>().mesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
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
