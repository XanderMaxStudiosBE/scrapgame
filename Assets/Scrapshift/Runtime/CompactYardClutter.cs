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

        // Source ceilings include optional private piston geometry and missing-model fallbacks.
        // These are mesh budgets, not measured frame-rate claims.
        public const int MaximumStockTriangles=82000,MaximumStockRenderers=460;
        public const int StockPatchCount=53,SolidStockVolumes=37;

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
            // Keep perimeter rows, then bring stock toward the first working routes.
            int[] northKinds={7,0,1,9,3};
            for(int i=0;i<5;i++)
                patches.Add(new Patch("North working stock "+i,"North working stock",new Vector3(-15+i*7.5f,0,15.8f),new Vector3(4.6f,2.6f,3.2f),northKinds[i],i,0,1.3f));
            int[] westKinds={8,3,7},eastKinds={2,9,0};
            for(int i=0;i<3;i++)
            {
                patches.Add(new Patch("West working stock "+i,"West working stock",new Vector3(-20.8f,0,-1+i*6.5f),new Vector3(3.2f,2.6f,4.6f),westKinds[i],i+5,90,1.3f));
                patches.Add(new Patch("East working stock "+i,"East working stock",new Vector3(20.8f,0,-1+i*6.5f),new Vector3(3.2f,2.6f,4.6f),eastKinds[i],i+8,-90,1.3f));
            }
            patches.Add(new Patch("Office repair spares","Service pocket offcuts",new Vector3(-22.15f,0,-14),new Vector3(1,1.1f,2.4f),5,0));
            patches.Add(new Patch("Receiving offcut drums","Service pocket offcuts",new Vector3(22.2f,0,-8.5f),new Vector3(1,1.1f,2.4f),5,1));
            // Small replaceable modules frame the office and bench at player height. None are
            // gameplay equipment; each releases its own footprint when a saved build overlaps it.
            Add(patches,"Office forecourt parts shelf","Office working pockets",-11.1f,-13,2.8f,2.2f,1.15f,10,0,180);
            Add(patches,"Office maintenance tool rack","Office working pockets",-11.1f,-10.15f,2.5f,2.5f,1,12,0,180);
            Add(patches,"Office tyre stock","Office working pockets",-11.2f,-7.4f,3.5f,1.2f,2.3f,0,12);
            Add(patches,"Office side appliance stock","Office working pockets",-21.9f,-5.8f,2.4f,2.2f,3.2f,9,12,90);
            Add(patches,"Office mixed metal skip","Office working pockets",-14,-4.8f,3,1.7f,1.9f,11,0,180);
            Add(patches,"Bench component shelf","Manual working pockets",-8.5f,-3.8f,2.8f,2.2f,1.15f,10,1,180);
            Add(patches,"Bench tool wall","Manual working pockets",-5,-6.4f,2.5f,2.5f,1,12,1,180);
            Add(patches,"Bench spare tyres","Manual working pockets",-9.2f,-6.4f,3.5f,1.2f,2.3f,0,13);
            Add(patches,"West stripped shell lot","West working pockets",-14.1f,1.5f,4.4f,2,3,8,13,0,1.2f);
            Add(patches,"West cooling parts","West working pockets",-12.5f,5,1.6f,1.4f,1,13,0,180);
            Add(patches,"West recovered cable reel","West working pockets",-16,5.3f,2.1f,1.3f,1.6f,14,0);
            Add(patches,"West oil and cable stock","West working pockets",-17,8,2.8f,1.6f,2.2f,3,14);
            Add(patches,"North stock shelf west","North working pockets",-13,11.6f,2.8f,2.2f,1.15f,10,2,180);
            Add(patches,"North metal skip west","North working pockets",-6.5f,12,3,1.7f,1.9f,11,1,180);
            Add(patches,"North stock shelf east","North working pockets",3,11.7f,2.8f,2.2f,1.15f,10,3,180);
            Add(patches,"North metal skip east","North working pockets",10.5f,12.1f,3,1.7f,1.9f,11,2,180);
            Add(patches,"East stripped shell lot","East working pockets",15,3.6f,4.4f,2,3,8,14,0,1.2f);
            Add(patches,"East appliance sorting lot","East working pockets",14.6f,8.7f,4,2.2f,2.6f,9,14,180);
            Add(patches,"East mixed metal skip","East working pockets",16,-1.2f,3,1.7f,1.9f,11,3,180);
            Add(patches,"East cooling parts","East working pockets",17.5f,7,1.6f,1.4f,1,13,1,180);
            Add(patches,"Receiving component shelf","Receiving working pockets",11.4f,-7.1f,2.8f,2.2f,1.15f,10,4,180);
            Add(patches,"Receiving metal skip","Receiving working pockets",10.8f,-11,3,1.7f,1.9f,11,4,180);
            Add(patches,"Receiving tyre stock","Receiving working pockets",16,-16.1f,3.5f,1.2f,2.3f,0,15);
            Add(patches,"Receiving spare cable reel","Receiving working pockets",11.9f,-15,2.1f,1.3f,1.6f,14,1);
            for(int i=0;i<8;i++)
                patches.Add(new Patch("Scattered ground offcuts "+i,"Flat ground offcuts",new Vector3(-15+(i%4)*10,0,-4+(i/4)*12),new Vector3(1.1f,.04f,.7f),6,i));
            return patches.ToArray();
        }

        static void Add(List<Patch> patches,string name,string sector,float x,float z,float width,float height,float depth,int kind,int variation,float yaw=0,float scale=1)
        {patches.Add(new Patch(name,sector,new Vector3(x,0,z),new Vector3(width,height,depth),kind,variation,yaw,scale));}

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
                {
                    // New combined foreground fixtures cast one solid sun shadow each; tiny
                    // separate offcuts/tyres and the bounded contact ellipse stay cheap.
                    bool fixture=renderer.name=="CompactPartsShelf" || renderer.name=="CompactMixedSkip" || renderer.name=="CompactWorkshopRack";
                    renderer.shadowCastingMode=fixture?ShadowCastingMode.On:ShadowCastingMode.Off;
                    renderer.receiveShadows=renderer.name!="Stock contact grounding";
                }
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
        {RefreshVisibility(root,occupied,null,.45f);}

        public static bool RefreshVisibility(GameObject root,IReadOnlyList<Bounds> occupied,Vector3? player,float playerRadius)
        {
            if(root==null)return false;
            var visibility=root.GetComponent<CompactClutterVisibility>();if(visibility==null)return false;
            bool waitingForPlayer=false;
            for(int i=0;i<visibility.clusters.Count;i++)
            {
                var cluster=visibility.clusters[i];if(cluster==null)continue;
                bool visible=!IsBlocked(visibility.patches[i],occupied);
                // Already visible stock remains present when approached. Only a currently
                // hidden module can be held back by the actual capsule, so removing saved
                // equipment/items cannot enable its collision through a standing player.
                if(visible && !cluster.activeSelf && player.HasValue && TouchesPlayer(visibility.patches[i],player.Value,playerRadius))
                {visible=false;waitingForPlayer=true;}
                if(cluster.activeSelf!=visible)cluster.SetActive(visible);
            }
            return waitingForPlayer;
        }

        static bool TouchesPlayer(Patch patch,Vector3 player,float radius)
        {
            var area=patch.footprint;
            float dx=Mathf.Max(area.min.x-player.x,Mathf.Max(0,player.x-area.max.x));
            float dz=Mathf.Max(area.min.z-player.z,Mathf.Max(0,player.z-area.max.z));
            return dx*dx+dz*dz<=radius*radius;
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
                    if(variation%2!=0 || !Model("CompactRadiatorRack",parent,new Vector3(.7f,.18f,.44f),new Vector3(.62f,.65f,.40f),15))
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
                    if(variation<4 || !Model("CompactCableReel",parent,new Vector3(.72f,0,-.28f),new Vector3(.82f,.95f,.82f),-12))
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
                case 8: // Missing glass/doors and bare hubs read as stripped stock, not a second playable car.
                    if(!Model("CompactSalvageShell",parent,new Vector3(-.08f,0,.23f),new Vector3(3.10f,1.30f,1.58f),90))
                    {
                        BuildPatch(parent,1,variation);
                        break;
                    }
                    if(!Model("CompactRadiatorRack",parent,new Vector3(.60f,0,-.75f),new Vector3(.75f,.72f,.36f),-8))
                        Model("CompactMotor",parent,new Vector3(.60f,0,-.75f),new Vector3(.45f,.45f,.36f),-8);
                    Ring("Loose stripped wheel rim",parent,new Vector3(-1.18f,.13f,-.75f),.30f,.18f,.20f,RetroSurface.DarkMetal);
                    var carDoor=Box("Removed car door",parent,new Vector3(-.23f,.10f,-.75f),new Vector3(.70f,.045f,.55f),RetroSurface.RustPaint);
                    carDoor.transform.localRotation=Quaternion.Euler(0,12,6);
                    break;
                case 9: // Washer drums, a discarded microwave and a timber cable reel vary the appliance row.
                    if(!Model("CompactWasherLot",parent,new Vector3(-.53f,.12f,.23f),new Vector3(1.53f,1.65f,1.08f),-8))
                    {
                        BuildPatch(parent,7,variation);
                        break;
                    }
                    Model("PalletBundle",parent,new Vector3(-.53f,0,.23f),new Vector3(1.72f,.18f,1.25f),0);
                    if(variation<12 && !Model("CompactCableReel",parent,new Vector3(.87f,0,.34f),new Vector3(.81f,.98f,.95f),-14))
                        Model("CompactInsulationCoil",parent,new Vector3(.87f,0,.34f),new Vector3(.78f,.36f,.78f),-14);
                    if(variation<12 && !Model("CompactRadiatorRack",parent,new Vector3(.22f,0,-.77f),new Vector3(.83f,.75f,.38f),-12))
                        Model("CompactMotor",parent,new Vector3(.22f,0,-.77f),new Vector3(.55f,.48f,.40f),-12);
                    Ring("Loose appliance door gasket",parent,new Vector3(-.90f,.033f,-.71f),.24f,.195f,.038f,RetroSurface.WireInsulation);
                    break;
                case 10: // A populated open shelf, rather than another empty brown crate.
                    if(!Model("CompactPartsShelf",parent,Vector3.zero,new Vector3(2.5f,2.05f,.98f),0))
                        FallbackShelf(parent,false);
                    break;
                case 11: // Unsorted metal has a readable rim, open top, bent sheets and a wheel hub.
                    if(!Model("CompactMixedSkip",parent,Vector3.zero,new Vector3(2.70f,1.50f,1.68f),0))
                        FallbackSkip(parent);
                    break;
                case 12: // Freestanding tool wall/parts drawers with no gameplay interaction.
                    if(!Model("CompactWorkshopRack",parent,Vector3.zero,new Vector3(2.22f,2.32f,.78f),0))
                        FallbackShelf(parent,true);
                    break;
                case 13:
                    if(!Model("CompactRadiatorRack",parent,Vector3.zero,new Vector3(1.15f,1.10f,.65f),0))
                        Model("CompactMotor",parent,Vector3.zero,new Vector3(.75f,.65f,.60f),0);
                    break;
                case 14:
                    if(!Model("CompactCableReel",parent,Vector3.zero,new Vector3(1.18f,1.15f,1.20f),variation==0?-12:16))
                        Model("CompactInsulationCoil",parent,Vector3.zero,new Vector3(1.18f,.50f,1.18f),0);
                    break;
                case 6:
                    var tab=Box("Discarded sheet-metal tab",parent,new Vector3(-.22f,.01f,0),new Vector3(.52f,.016f,.25f),RetroSurface.RustPaint);
                    tab.transform.localRotation=Quaternion.Euler(0,variation*17,0);
                    var cut=Box("Flat cable clip",parent,new Vector3(.32f,.012f,.12f),new Vector3(.18f,.020f,.22f),RetroSurface.DarkMetal);
                    cut.transform.localRotation=Quaternion.Euler(0,variation*13,0);
                    Ring("Discarded rubber seal",parent,new Vector3(.20f,.015f,-.11f),.14f,.105f,.020f,RetroSurface.WireInsulation);
                    break;
            }
        }

        static void FallbackShelf(Transform parent,bool tools)
        {
            foreach(float x in new[]{-.96f,.96f})foreach(float z in new[]{-.30f,.30f})
                Box("Parts shelf upright",parent,new Vector3(x,1.00f,z),new Vector3(.055f,2.00f,.055f),RetroSurface.DarkMetal);
            foreach(float y in new[]{.15f,.90f,1.66f})
                Box("Worn parts shelf",parent,new Vector3(0,y,0),new Vector3(2.10f,.065f,.72f),RetroSurface.WeatheredWood);
            if(tools)
            {
                Box("Tool wall backing",parent,new Vector3(0,1.45f,.23f),new Vector3(2.00f,1.03f,.035f),RetroSurface.WeatheredWood);
                for(int i=0;i<5;i++)
                {
                    float x=-.70f+i*.35f;
                    Box("Hanging tool shaft",parent,new Vector3(x,1.48f,.18f),new Vector3(.028f,.42f,.035f),RetroSurface.DarkMetal);
                    Box("Hanging tool head",parent,new Vector3(x,1.68f,.18f),new Vector3(.15f,.055f,.06f),RetroSurface.DarkMetal);
                }
            }
            else
                for(int i=0;i<3;i++)Box("Recovered parts case",parent,new Vector3(-.62f+i*.61f,1.84f,.02f),new Vector3(.48f,.29f,.44f),RetroSurface.RustPaint);
            Model("CompactMotor",parent,new Vector3(-.48f,.94f,0),new Vector3(.45f,.51f,.40f),15);
            Model("CompactCompressor",parent,new Vector3(.48f,.18f,0),new Vector3(.52f,.61f,.49f),0);
        }

        static void FallbackSkip(Transform parent)
        {
            Box("Mixed skip floor",parent,new Vector3(0,.12f,0),new Vector3(2.50f,.18f,1.50f),RetroSurface.DarkMetal);
            foreach(float x in new[]{-1.24f,1.24f})Box("Mixed skip side",parent,new Vector3(x,.60f,0),new Vector3(.07f,1,1.58f),RetroSurface.RustPaint);
            Box("Mixed skip rear",parent,new Vector3(0,.60f,.76f),new Vector3(2.48f,1,.07f),RetroSurface.RustPaint);
            Box("Mixed skip low front",parent,new Vector3(0,.36f,-.76f),new Vector3(2.48f,.58f,.07f),RetroSurface.CorrugatedMetal);
            for(int i=0;i<4;i++)
            {
                var panel=Box("Unsorted sheet",parent,new Vector3(-.76f+i*.5f,.57f+i*.07f,0),new Vector3(.52f,.035f,1.16f),i%2==0?RetroSurface.CorrugatedMetal:RetroSurface.RustPaint);
                panel.transform.localRotation=Quaternion.Euler(0,0,12+i*8);
            }
            Ring("Discarded skip wheel rim",parent,new Vector3(-.45f,.99f,.20f),.25f,.16f,.12f,RetroSurface.DarkMetal);
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
        static bool Model(string resource,Transform parent,Vector3 p,Vector3 maximum,float yaw)
        {
            if(!AuthoredYardProps.TryPlace(resource,parent,Vector3.zero,out GameObject model))return false;
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
            if(!found || !Finite(bounds.center) || !Finite(bounds.size) ||
               bounds.size.x<=.0001f || bounds.size.y<=.0001f || bounds.size.z<=.0001f)
            {
                if(Application.isPlaying)Object.Destroy(model);else Object.DestroyImmediate(model);
                return false;
            }
            float scale=Mathf.Min(maximum.x/Mathf.Max(.001f,bounds.size.x),Mathf.Min(maximum.y/Mathf.Max(.001f,bounds.size.y),maximum.z/Mathf.Max(.001f,bounds.size.z)));
            model.transform.localScale=Vector3.one*scale;
            model.transform.localPosition=p-new Vector3(bounds.center.x*scale,bounds.min.y*scale,bounds.center.z*scale);
            return true;
        }
        static bool Finite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) &&
                   !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
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
