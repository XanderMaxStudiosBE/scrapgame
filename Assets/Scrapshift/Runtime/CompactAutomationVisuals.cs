using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrapshift.Compact
{
    /// <summary>Static views follow the authoritative port/path geometry; no transport simulation lives here.</summary>
    public static class CompactAutomationVisuals
    {
        public const float ItemHeight=.7f;
        public const float TravelSurfaceHeight=.69f;
        public const float TrackWidth=.66f;
        public const float RailEnvelope=.86f;

        public static Vector3 Tier2PowerSocket(CompactRules rules)
        {
            var definition=rules.Equipment(EquipmentKind.Tier2Scrapper);
            return new Vector3(-.90f*definition.width/3.2f,.90f,-1.055f*definition.depth/2.56f);
        }

        public static GameObject BuildEquipment(EquipmentKind kind,Transform parent,Vector3 position,float yaw=0,bool colliders=true,CompactRules rules=null)
        {
            rules=rules ?? new CompactRules();
            var definition=rules.Equipment(kind);
            if(definition==null)throw new ArgumentException("Unknown automation equipment.",nameof(kind));
            string model;float width,depth,height;
            switch(kind)
            {
                case EquipmentKind.Storage:model="CompactPortedStorage";width=3.04f;depth=2.62f;height=2.25f;break;
                case EquipmentKind.Tier2Scrapper:model="CompactTier2Scrapper";width=3.2f;depth=2.56f;height=2.25f;break;
                case EquipmentKind.Splitter:model="CompactSplitter";width=depth=1.5f;height=1;break;
                case EquipmentKind.Merger:model="CompactMerger";width=depth=1.5f;height=1;break;
                default:throw new ArgumentException("This equipment has no stage-C view.",nameof(kind));
            }
            var root=new GameObject(kind.ToString());root.transform.SetParent(parent,false);
            root.transform.localPosition=position;root.transform.localRotation=Quaternion.Euler(0,yaw,0);
            if(AuthoredYardProps.TryPlace(model,root.transform,Vector3.zero,out GameObject body))
                body.transform.localScale=new Vector3(definition.width/width,1,definition.depth/depth);
            else
                Debug.LogWarning("Automation mesh missing: "+model+". Allow the tracked original FBX to import.");
            if(colliders)
            {
                var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,height*.5f,0);
                box.size=new Vector3(definition.width,height,definition.depth);
            }
            BuildPorts(kind,root.transform,rules,colliders);
            BuildMachineDetails(kind,root.transform,rules);
            if(colliders)
            {
                float labelY=height>.99f ? height-.46f : .82f;
                string label=kind==EquipmentKind.Tier2Scrapper ? "SCRAPPER / TIER 2" : kind.ToString().ToUpperInvariant();
                if(kind==EquipmentKind.Tier2Scrapper)
                    YardSignText.Plate(root.transform,label,new Vector3(0,1.91f,-.435f*definition.depth/2.56f),Mathf.Min(1.55f,definition.width*.50f),.22f);
                else
                    YardSignText.Plate(root.transform,label,new Vector3(0,labelY,-definition.depth*.5f-.075f),Mathf.Min(1.8f,definition.width*.82f),.29f);
            }
            return root;
        }

        // Port markers and their mouths follow the core, including Tier1's saved -Z outlet.
        // One static mesh per surface is shared across every port on this equipment.
        public static void BuildPorts(EquipmentKind kind,Transform root,CompactRules rules,bool colliders=false)
        {
            var frame=new Geometry();var deck=new Geometry();var markings=new Geometry();
            var equipment=new EquipmentState{kind=kind};
            for(int outputIndex=0;outputIndex<2;outputIndex++)
            {
                bool output=outputIndex==1;
                for(int i=0;i<AutomationModel.PortCount(kind,output);i++)
                {
                    var p=AutomationModel.Port(equipment,rules,output,i);
                    var point=new Vector3(p.x,ItemHeight,p.z);
                    var direction=new Vector3(p.x,0,p.z).normalized;
                    var marker=new GameObject((output ? "Output port " : "Input port ")+i);
                    marker.transform.SetParent(root,false);marker.transform.localPosition=point;
                    var rotation=Quaternion.LookRotation(direction);
                    marker.transform.localRotation=rotation;
                    if(colliders)
                    {
                        var target=marker.AddComponent<CompactConveyorPortTarget>();target.output=output;target.index=i;
                        var hit=marker.AddComponent<BoxCollider>();hit.center=new Vector3(0,.15f,-.15f);
                        hit.size=new Vector3(.85f,.80f,.40f);
                    }
                    // The collar bridges the .35m gap between the machine footprint and
                    // authoritative belt endpoint. No face closes the transport opening.
                    float collarLength=kind==EquipmentKind.Tier1Scrapper ? .95f : kind==EquipmentKind.Workbench ? .85f : .52f;
                    var collar=point-direction*(collarLength*.5f);
                    deck.Box(collar+Vector3.down*.055f,new Vector3(TrackWidth,.09f,collarLength),rotation);
                    frame.Box(collar+Vector3.down*.14f,new Vector3(.80f,.10f,collarLength),rotation);
                    var side=Vector3.Cross(Vector3.up,direction);
                    bool junction=kind==EquipmentKind.Splitter || kind==EquipmentKind.Merger;
                    int paint=output ? 11 : 12; // Worn sage outlet / amber receiving mouth.
                    foreach(float sign in new[]{-1f,1f})
                    {
                        frame.Box(collar+side*(sign*.40f),new Vector3(.06f,.12f,collarLength),rotation);
                        markings.Box(collar+side*(sign*.40f)+Vector3.up*.068f,new Vector3(.055f,.025f,collarLength-.02f),rotation,paint);
                        var upright=point-direction*.30f+side*(sign*.40f)+Vector3.up*.235f;
                        frame.Box(upright,new Vector3(.07f,.47f,.15f),rotation);
                        markings.Box(upright+direction*.078f,new Vector3(.074f,.37f,.012f),rotation,paint);
                        if(!junction)
                        {
                            var support=point-direction*.45f+side*(sign*.32f);
                            frame.Box(support+Vector3.down*.42f,new Vector3(.065f,.55f,.09f),rotation);
                            frame.Box(support+Vector3.down*.665f,new Vector3(.14f,.06f,.18f),rotation);
                        }
                    }
                    // Exposed rollers and a painted direction arrow identify the actual
                    // receiving/finished tray from player height, even before a belt exists.
                    foreach(float along in new[]{.105f,.345f})
                    {
                        var roller=point-direction*along-Vector3.up*.024f;
                        frame.Cylinder(roller,side,.024f,.73f,8);
                        foreach(float sign in new[]{-1f,1f})
                            frame.Box(roller+side*(sign*.385f),new Vector3(.045f,.07f,.08f),rotation);
                    }
                    markings.Arrow(point-direction*.23f-Vector3.up*.008f,output ? direction : -direction);
                    // End bearing blocks and a bolted sleeve meet the belt at exactly
                    // its authoritative endpoint. The lane itself stays open.
                    foreach(float sign in new[]{-1f,1f})
                    {
                        var sleeve=point-direction*.035f+side*(sign*.40f)-Vector3.up*.025f;
                        frame.Box(sleeve,new Vector3(.085f,.13f,.07f),rotation);
                        markings.Box(sleeve+direction*.036f,new Vector3(.050f,.07f,.007f),rotation,2);
                    }
                    var header=point-direction*.30f+Vector3.up*.49f;
                    frame.Box(header,new Vector3(.85f,.07f,.15f),rotation);
                    markings.Box(header+direction*.078f,new Vector3(.83f,.073f,.012f),rotation,paint);
                    PortWord(markings,header+Vector3.up*.105f+direction*.08f,rotation,output);
                }
            }
            frame.Build(root,"Worn port-collar rails","ScrapshiftMaterials/DarkMetal");
            deck.Build(root,"Port transport collars","ScrapshiftMaterials/WireInsulation");
            markings.Build(root,"Painted port directions","ScrapshiftMaterials/PropAtlas");
        }

        // These are equipment-attached original fittings, never fixed yard scenery
        // or free inventory. Two combined meshes keep the authoring detail bounded.
        public static void BuildMachineDetails(EquipmentKind kind,Transform root,CompactRules rules)
        {
            if(root.Find("Original working assembly")!=null)return;
            var definition=rules.Equipment(kind);
            if(definition==null)return;
            float width,depth;
            var steel=new Geometry();var paint=new Geometry();
            switch(kind)
            {
                case EquipmentKind.Workbench:
                    width=2.55f;depth=1.35f;
                    // An attached tool board and shallow task-light hood give the
                    // movable bench a workshop silhouette inside its paid footprint.
                    foreach(float x in new[]{-1.08f,1.08f})
                        steel.Box(new Vector3(x,1.48f,.53f),new Vector3(.055f,.80f,.065f),Quaternion.identity);
                    paint.Box(new Vector3(0,1.58f,.53f),new Vector3(2.22f,.60f,.055f),Quaternion.identity,6);
                    foreach(float y in new[]{1.32f,1.84f})
                        steel.Box(new Vector3(0,y,.49f),new Vector3(2.26f,.035f,.035f),Quaternion.identity);
                    for(int x=0;x<9;x++)for(int y=0;y<4;y++)
                        paint.Box(new Vector3(-.93f+x*.23f,1.38f+y*.13f,.498f),new Vector3(.014f,.014f,.008f),Quaternion.identity,5);
                    paint.Box(new Vector3(0,1.91f,.50f),new Vector3(2.30f,.055f,.26f),Quaternion.Euler(-7,0,0),0);
                    paint.Box(new Vector3(0,1.872f,.438f),new Vector3(1.88f,.026f,.036f),Quaternion.identity,8);
                    // Recognizable hanging spanners/pliers use retained worn metal
                    // and rust tiles; empty catch trays leave work state authoritative.
                    foreach(float x in new[]{-.76f,-.31f})
                    {
                        steel.Box(new Vector3(x,1.61f,.473f),new Vector3(.030f,.25f,.022f),Quaternion.Euler(0,0,x<-.5f?8:-8));
                        foreach(float side in new[]{-1f,1f})
                            steel.Box(new Vector3(x+side*.038f,1.752f,.473f),new Vector3(.026f,.072f,.023f),Quaternion.Euler(0,0,side*18));
                        steel.Cylinder(new Vector3(x,1.481f,.473f),Vector3.forward,.037f,.025f,8);
                    }
                    foreach(float sign in new[]{-1f,1f})
                    {
                        paint.Box(new Vector3(.36f+sign*.033f,1.565f,.470f),new Vector3(.035f,.20f,.023f),Quaternion.Euler(0,0,sign*-14),3);
                        steel.Box(new Vector3(.36f+sign*.025f,1.70f,.470f),new Vector3(.028f,.09f,.022f),Quaternion.Euler(0,0,sign*15));
                    }
                    OpenTray(steel,paint,new Vector3(.77f,1.145f,.18f),.57f,.34f,12);
                    OpenTray(steel,paint,new Vector3(-.77f,1.145f,-.34f),.56f,.36f,11);
                    break;
                case EquipmentKind.Generator:
                    width=1.75f;depth=1.25f;
                    // A cable reel and electrical symbol distinguish power from cargo.
                    steel.Cylinder(new Vector3(.78f,.46f,.26f),Vector3.right,.16f,.065f,12);
                    steel.Cylinder(new Vector3(.78f,.46f,.26f),Vector3.right,.12f,.18f,12);
                    paint.Box(new Vector3(-.40f,.73f,-.512f),new Vector3(.18f,.20f,.009f),Quaternion.identity,12);
                    foreach(var ends in new[]{new[]{new Vector3(-.365f,.802f,-.520f),new Vector3(-.428f,.721f,-.520f)},
                        new[]{new Vector3(-.428f,.721f,-.520f),new Vector3(-.375f,.741f,-.520f)},
                        new[]{new Vector3(-.375f,.741f,-.520f),new Vector3(-.433f,.658f,-.520f)}})
                        steel.Beam(ends[0],ends[1],.018f);
                    break;
                case EquipmentKind.Tier1Scrapper:
                    width=2.4f;depth=2.3f;
                    // The historical .7m intake meets a visible rear lift throat,
                    // which feeds the existing high cutter hopper without relocating IN.
                    var lower=new Vector3(0,.69f,.99f);var upper=new Vector3(0,1.46f,.66f);
                    var slope=(upper-lower).normalized;var liftRotation=Quaternion.LookRotation(slope);
                    float liftLength=Vector3.Distance(lower,upper);
                    steel.Box((lower+upper)*.5f,new Vector3(.72f,.055f,liftLength),liftRotation);
                    foreach(float sign in new[]{-1f,1f})
                    {
                        paint.Box((lower+upper)*.5f+Vector3.right*(sign*.39f),new Vector3(.065f,.15f,liftLength+.06f),liftRotation,12);
                        steel.Box(new Vector3(sign*.39f,1.05f,.98f),new Vector3(.065f,.68f,.065f),Quaternion.identity);
                    }
                    for(int i=0;i<4;i++)
                        steel.Cylinder(lower+(upper-lower)*((i+.5f)/4),Vector3.right,.027f,.69f,8);
                    paint.Box(new Vector3(0,1.42f,.69f),new Vector3(.72f,.065f,.13f),Quaternion.identity,12);
                    break;
                case EquipmentKind.Tier2Scrapper:
                    width=3.2f;depth=2.56f;
                    foreach(float x in new[]{-1.15f,1.15f})
                    {
                        steel.Beam(new Vector3(x,.24f,-.80f),new Vector3(x,.60f,.55f),.06f);
                        paint.Box(new Vector3(x,.52f,-1.02f),new Vector3(.22f,.14f,.05f),Quaternion.identity,3);
                    }
                    // Side service handles and a restrained raised warning plate.
                    foreach(float x in new[]{-1.31f,1.31f})
                    {
                        steel.Box(new Vector3(x,1.10f,.14f),new Vector3(.07f,.032f,.34f),Quaternion.identity);
                        foreach(float z in new[]{-.015f,.295f})
                            steel.Box(new Vector3(x,1.10f,z),new Vector3(.07f,.09f,.035f),Quaternion.identity);
                    }
                    break;
                case EquipmentKind.Storage:
                    width=3.04f;depth=2.62f;
                    foreach(float x in new[]{-1.40f,1.40f})
                    {
                        steel.Box(new Vector3(x,1.84f,0),new Vector3(.09f,.035f,.62f),Quaternion.identity);
                        foreach(float z in new[]{-.27f,.27f})
                            steel.Box(new Vector3(x,1.80f,z),new Vector3(.09f,.11f,.04f),Quaternion.identity);
                        paint.Box(new Vector3(x,.35f,-1.18f),new Vector3(.17f,.14f,.06f),Quaternion.identity,3);
                    }
                    break;
                case EquipmentKind.PrimaryScrapper:
                    width=6;depth=7;
                    // The whole-object ramp remains much wider than a component mouth.
                    foreach(float x in new[]{-1.12f,1.12f})
                        for(int i=0;i<4;i++)
                            paint.Box(new Vector3(x,.346f,-3.32f+i*.10f),new Vector3(.16f,.008f,.048f),Quaternion.identity,i%2==0?12:4);
                    steel.Box(new Vector3(0,.47f,-3.0f),new Vector3(2.56f,.08f,.06f),Quaternion.identity);
                    break;
                case EquipmentKind.ExportStation:
                    width=3;depth=2.5f;
                    // Empty straps and tie-down hooks support the shipping silhouette.
                    foreach(float x in new[]{-.72f,.72f})
                    {
                        paint.Box(new Vector3(x,.676f,.54f),new Vector3(.044f,.015f,.99f),Quaternion.identity,12);
                        steel.Box(new Vector3(x,.704f,1.02f),new Vector3(.10f,.055f,.07f),Quaternion.identity);
                    }
                    break;
                default:return;
            }
            var assembly=new GameObject("Original working assembly");assembly.transform.SetParent(root,false);
            assembly.transform.localScale=new Vector3(definition.width/width,1,definition.depth/depth);
            steel.Build(assembly.transform,"Working assembly steel","ScrapshiftMaterials/DarkMetal");
            paint.Build(assembly.transform,"Working assembly worn fittings","ScrapshiftMaterials/PropAtlas");
        }

        static void OpenTray(Geometry steel,Geometry paint,Vector3 centre,float width,float depth,int tile)
        {
            steel.Box(centre,new Vector3(width,.045f,depth),Quaternion.identity);
            foreach(float sign in new[]{-1f,1f})
            {
                paint.Box(centre+new Vector3(sign*(width*.5f-.018f),.042f,0),new Vector3(.035f,.10f,depth),Quaternion.identity,tile);
                paint.Box(centre+new Vector3(0,.042f,sign*(depth*.5f-.018f)),new Vector3(width,.10f,.035f),Quaternion.identity,tile);
            }
        }

        static void PortWord(Geometry geometry,Vector3 centre,Quaternion rotation,bool output)
        {
            // Industrial stencil strokes live in the same atlas mesh as the arrows.
            // This adds no font atlas, material instance, floating label or extra renderer.
            geometry.Box(centre,new Vector3(.36f,.14f,.024f),rotation,4);
            string word=output ? "OUT" : "IN";
            for(int i=0;i<word.Length;i++)
            {
                // Viewed from outside a +Z-facing mouth, readable right is local
                // -X. Reverse both stencil layout and strokes on the outer face.
                var at=centre+rotation*new Vector3(-(i-(word.Length-1)*.5f)*.088f,0,.016f);
                switch(word[i])
                {
                    case 'I':Stencil(geometry,at,rotation,0,-.04f,0,.04f);break;
                    case 'N':
                        Stencil(geometry,at,rotation,-.026f,-.04f,-.026f,.04f);
                        Stencil(geometry,at,rotation,.026f,-.04f,.026f,.04f);
                        Stencil(geometry,at,rotation,-.026f,.04f,.026f,-.04f);break;
                    case 'O':
                        Stencil(geometry,at,rotation,-.026f,-.04f,-.026f,.04f);
                        Stencil(geometry,at,rotation,.026f,-.04f,.026f,.04f);
                        Stencil(geometry,at,rotation,-.026f,.04f,.026f,.04f);
                        Stencil(geometry,at,rotation,-.026f,-.04f,.026f,-.04f);break;
                    case 'U':
                        Stencil(geometry,at,rotation,-.026f,-.04f,-.026f,.04f);
                        Stencil(geometry,at,rotation,.026f,-.04f,.026f,.04f);
                        Stencil(geometry,at,rotation,-.026f,-.04f,.026f,-.04f);break;
                    case 'T':
                        Stencil(geometry,at,rotation,-.030f,.04f,.030f,.04f);
                        Stencil(geometry,at,rotation,0,-.04f,0,.04f);break;
                }
            }
        }
        static void Stencil(Geometry geometry,Vector3 centre,Quaternion rotation,float ax,float ay,float bx,float by)
        {
            var a=new Vector3(-ax,ay,0);var b=new Vector3(-bx,by,0);
            geometry.Box(centre+rotation*((a+b)*.5f),new Vector3(.015f,(b-a).magnitude+.008f,.005f),
                rotation*Quaternion.FromToRotation(Vector3.up,(b-a).normalized),14);
        }

        // Belts intentionally have no collider: the player can cross ground-level lines without a jump action.
        public static GameObject BuildBelt(ConveyorLink link,CompactYardState state,CompactRules rules,Transform parent)
        {
            var points=AutomationModel.Path(link,state,rules);
            var root=new GameObject("Conveyor "+link.id);root.transform.SetParent(parent,false);
            var frame=new Geometry();var track=new Geometry();var markings=new Geometry();
            for(int i=1;i<points.Length;i++)
            {
                var start=new Vector3(points[i-1].x,0,points[i-1].z);
                var end=new Vector3(points[i].x,0,points[i].z);
                float length=Vector3.Distance(start,end);if(length<.001f)continue;
                var direction=(end-start)/length;var side=Vector3.Cross(Vector3.up,direction);
                var rotation=Quaternion.LookRotation(direction);var centre=(start+end)*.5f;
                track.Box(centre+Vector3.up*(TravelSurfaceHeight-.045f),new Vector3(TrackWidth,.09f,length),rotation);
                frame.Box(centre+Vector3.up*.56f,new Vector3(.80f,.10f,length),rotation);
                bool turnStart=i>1 && IsTurn(points[i-2],points[i-1],points[i]);
                bool turnEnd=i<points.Length-1 && IsTurn(points[i-1],points[i],points[i+1]);
                float trimStart=turnStart?Mathf.Min(.40f,length*.45f):0;
                float trimEnd=turnEnd?Mathf.Min(.40f,length*.45f):0;
                float railLength=length-trimStart-trimEnd;
                if(railLength>.02f)
                    foreach(float sign in new[]{-1f,1f})
                        frame.Box(centre+direction*((trimStart-trimEnd)*.5f)+side*(sign*.40f)+Vector3.up*.72f,
                            new Vector3(.06f,.12f,railLength),rotation);
                int rollers=Math.Max(1,(int)Math.Ceiling(length/.60f));
                for(int j=0;j<rollers;j++)
                {
                    var p=start+direction*((j+.5f)*length/rollers);
                    frame.Cylinder(p+Vector3.up*.594f,side,.048f,.73f,8);
                }
                int supports=Math.Max(1,(int)Math.Ceiling(length/2.0f));
                for(int j=0;j<supports;j++)
                {
                    var p=start+direction*((j+.5f)*length/supports);
                    foreach(float sign in new[]{-1f,1f})
                    {
                        frame.Box(p+side*(sign*.33f)+Vector3.up*.27f,new Vector3(.065f,.54f,.09f),rotation);
                        frame.Box(p+side*(sign*.33f)+Vector3.up*.045f,new Vector3(.18f,.075f,.22f),rotation);
                    }
                    frame.Box(p+Vector3.up*.17f,new Vector3(.72f,.055f,.075f),rotation);
                }
                int arrows=Math.Max(1,(int)Math.Ceiling(length/1.15f));
                for(int j=0;j<arrows;j++)markings.Arrow(start+direction*((j+.5f)*length/arrows)+Vector3.up*(TravelSurfaceHeight+.002f),direction);
                // Short contrasting rail caps make the black travel lane readable
                // without a scrolling material or per-conveyor update component.
                int bands=Math.Max(1,(int)Math.Ceiling(length/1.8f));
                for(int j=0;j<bands;j++)
                {
                    var p=start+direction*((j+.5f)*length/bands)+Vector3.up*.785f;
                    foreach(float sign in new[]{-1f,1f})
                        markings.TopPatch(p+side*(sign*.40f),direction,.035f,Mathf.Min(.26f,length*.40f),12);
                }
                if(turnEnd)
                {
                    // Slightly higher corner bed avoids overlapping top faces at orthogonal elbows.
                    track.Box(end+Vector3.up*.652f,new Vector3(TrackWidth,.09f,TrackWidth),Quaternion.identity);
                    frame.Box(end+Vector3.up*.56f,new Vector3(.80f,.10f,.80f),Quaternion.identity);
                    frame.Cylinder(end+Vector3.up*.53f,Vector3.up,.37f,.07f,12);
                }
                if(i==1)BeltSplice(frame,markings,start,direction);
                if(i==points.Length-1)BeltSplice(frame,markings,end,-direction);
            }
            frame.Build(root.transform,"Belt steel frame, rollers and supports","ScrapshiftMaterials/DarkMetal");
            track.Build(root.transform,"Rubber travel surface","ScrapshiftMaterials/WireInsulation");
            markings.Build(root.transform,"Painted conveyor direction","ScrapshiftMaterials/PropAtlas");
            return root;
        }

        static bool IsTurn(CompactPortPoint a,CompactPortPoint b,CompactPortPoint c)
        {
            var incoming=new Vector3(b.x-a.x,0,b.z-a.z).normalized;
            var outgoing=new Vector3(c.x-b.x,0,c.z-b.z).normalized;
            return Vector3.Dot(incoming,outgoing)<.999f;
        }

        static void BeltSplice(Geometry frame,Geometry markings,Vector3 endpoint,Vector3 inward)
        {
            var side=Vector3.Cross(Vector3.up,inward);var rotation=Quaternion.LookRotation(inward);
            foreach(float sign in new[]{-1f,1f})
            {
                var sleeve=endpoint+inward*.035f+side*(sign*.40f)+Vector3.up*.675f;
                frame.Box(sleeve,new Vector3(.085f,.13f,.07f),rotation);
                markings.TopPatch(sleeve+Vector3.up*.066f,inward,.024f,.042f,2);
            }
            frame.Cylinder(endpoint+inward*.105f+Vector3.up*.676f,side,.024f,.73f,8);
        }

        public static Vector3 ItemPosition(ConveyorLink link,CompactYardState state,CompactRules rules,float progress)
        {
            var points=AutomationModel.Path(link,state,rules);
            if(points.Length==0)return new Vector3(0,ItemHeight,0);
            if(float.IsNaN(progress)||float.IsInfinity(progress))progress=0;
            float remaining=AutomationModel.Length(points)*Mathf.Clamp01(progress);
            for(int i=1;i<points.Length;i++)
            {
                float x=points[i].x-points[i-1].x,z=points[i].z-points[i-1].z;
                float length=Mathf.Sqrt(x*x+z*z);
                if(length>.0001f && remaining<=length)
                {
                    float t=remaining/length;
                    return new Vector3(points[i-1].x+x*t,ItemHeight,points[i-1].z+z*t);
                }
                remaining-=length;
            }
            var last=points[points.Length-1];return new Vector3(last.x,ItemHeight,last.z);
        }

        sealed class Geometry
        {
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();
            readonly List<int> triangles=new List<int>();
            public void Beam(Vector3 a,Vector3 b,float width)
            {
                var span=b-a;
                Box((a+b)*.5f,new Vector3(width,width,span.magnitude),Quaternion.LookRotation(span));
            }
            public void Box(Vector3 centre,Vector3 size,Quaternion rotation,int tile=-1)
            {
                Face(centre,rotation,new Vector3(0,0,size.z*.5f),Vector3.right,Vector3.up,size.x,size.y,tile);
                Face(centre,rotation,new Vector3(0,0,-size.z*.5f),Vector3.left,Vector3.up,size.x,size.y,tile);
                Face(centre,rotation,new Vector3(size.x*.5f,0,0),Vector3.back,Vector3.up,size.z,size.y,tile);
                Face(centre,rotation,new Vector3(-size.x*.5f,0,0),Vector3.forward,Vector3.up,size.z,size.y,tile);
                Face(centre,rotation,new Vector3(0,size.y*.5f,0),Vector3.right,Vector3.back,size.x,size.z,tile);
                Face(centre,rotation,new Vector3(0,-size.y*.5f,0),Vector3.right,Vector3.forward,size.x,size.z,tile);
            }
            void Face(Vector3 centre,Quaternion rotation,Vector3 offset,Vector3 right,Vector3 up,float width,float height,int tile)
            {
                int n=vertices.Count;var x=right*width*.5f;var y=up*height*.5f;
                vertices.Add(centre+rotation*(offset-x-y));vertices.Add(centre+rotation*(offset+x-y));
                vertices.Add(centre+rotation*(offset+x+y));vertices.Add(centre+rotation*(offset-x+y));
                if(tile<0)
                {uv.Add(Vector2.zero);uv.Add(new Vector2(width,0));uv.Add(new Vector2(width,height));uv.Add(new Vector2(0,height));}
                else
                {
                    float u=(tile%4)*.25f+.055f,v=(tile/4)*.25f+.055f;
                    uv.Add(new Vector2(u,v));uv.Add(new Vector2(u+.14f,v));uv.Add(new Vector2(u+.14f,v+.14f));uv.Add(new Vector2(u,v+.14f));
                }
                triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
            }
            public void Cylinder(Vector3 centre,Vector3 axis,float radius,float length,int sides)
            {
                var rotation=Quaternion.FromToRotation(Vector3.up,axis);
                for(int i=0;i<sides;i++)
                {
                    float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                    var first=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                    var second=new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                    int n=vertices.Count;
                    vertices.Add(centre+rotation*(first+Vector3.down*length*.5f));
                    vertices.Add(centre+rotation*(first+Vector3.up*length*.5f));
                    vertices.Add(centre+rotation*(second+Vector3.up*length*.5f));
                    vertices.Add(centre+rotation*(second+Vector3.down*length*.5f));
                    uv.Add(Vector2.zero);uv.Add(new Vector2(0,length));uv.Add(new Vector2(radius,length));uv.Add(new Vector2(radius,0));
                    triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
                    Triangle(centre+rotation*Vector3.up*length*.5f,vertices[n+2],vertices[n+1],false);
                    Triangle(centre+rotation*Vector3.down*length*.5f,vertices[n],vertices[n+3],false);
                }
            }
            public void Arrow(Vector3 p,Vector3 forward)
            {
                var side=Vector3.Cross(Vector3.up,forward);
                Triangle(p+forward*.22f,p+side*.13f,p-side*.13f,true);
                var a=p-side*.04f-forward*.18f;var b=p+side*.04f-forward*.18f;
                var c=p+side*.04f;var d=p-side*.04f;
                Triangle(a,c,b,true);Triangle(a,d,c,true);
            }
            public void TopPatch(Vector3 p,Vector3 forward,float halfWidth,float length,int tile)
            {
                var side=Vector3.Cross(Vector3.up,forward);int n=vertices.Count;
                vertices.Add(p-side*halfWidth-forward*length*.5f);vertices.Add(p-side*halfWidth+forward*length*.5f);
                vertices.Add(p+side*halfWidth+forward*length*.5f);vertices.Add(p+side*halfWidth-forward*length*.5f);
                float u=(tile%4)*.25f+.06f,v=(tile/4)*.25f+.06f;
                uv.Add(new Vector2(u,v));uv.Add(new Vector2(u,v+.13f));uv.Add(new Vector2(u+.13f,v+.13f));uv.Add(new Vector2(u+.13f,v));
                triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
            }
            void Triangle(Vector3 a,Vector3 b,Vector3 c,bool atlas)
            {
                int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
                // Tile 14 is restrained ivory paint in the existing PropAtlas.
                uv.Add(atlas ? new Vector2(.625f,.875f) : Vector2.zero);
                uv.Add(atlas ? new Vector2(.65f,.90f) : Vector2.up);
                uv.Add(atlas ? new Vector2(.60f,.90f) : Vector2.right);
                triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
            }
            public void Build(Transform parent,string name,string resource)
            {
                if(vertices.Count==0)return;
                var go=new GameObject(name);go.transform.SetParent(parent,false);
                var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<ProceduralMeshOwner>().mesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=YardMaterialBindings.Load(resource,parent);
                renderer.shadowCastingMode=resource.EndsWith("PropAtlas",StringComparison.Ordinal) ? ShadowCastingMode.Off : ShadowCastingMode.On;
            }
        }
    }
}
