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
            BuildPorts(kind,root.transform,rules);
            if(colliders)
            {
                float labelY=height>.99f ? height-.46f : .82f;
                string label=kind==EquipmentKind.Tier2Scrapper ? "TIER 2" : kind.ToString().ToUpperInvariant();
                YardGeometry.Sign(root.transform,label,new Vector3(0,labelY,-definition.depth*.5f-.075f));
                root.transform.GetChild(root.transform.childCount-1).GetComponent<TextMesh>().characterSize=.023f;
            }
            return root;
        }

        // Tier1 also reuses this adapter so its existing front tray meets the new -Z output port.
        public static void BuildPorts(EquipmentKind kind,Transform root,CompactRules rules)
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
                    marker.transform.localRotation=Quaternion.LookRotation(direction);
                    // A .45m collar overlaps the existing tray slightly and ends exactly at the port.
                    var collar=point-direction*.225f;
                    deck.Box(collar+Vector3.down*.06f,new Vector3(TrackWidth,.07f,.45f),Quaternion.LookRotation(direction));
                    var side=Vector3.Cross(Vector3.up,direction);
                    foreach(float sign in new[]{-1f,1f})
                        frame.Box(collar+side*(sign*.385f),new Vector3(.06f,.10f,.45f),Quaternion.LookRotation(direction));
                    markings.Arrow(point+Vector3.up*.005f,output ? direction : -direction);
                }
            }
            frame.Build(root,"Worn port-collar rails","ScrapshiftMaterials/DarkMetal");
            deck.Build(root,"Port transport collars","ScrapshiftMaterials/WireInsulation");
            markings.Build(root,"Painted port directions","ScrapshiftMaterials/PropAtlas");
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
                track.Box(centre+Vector3.up*.645f,new Vector3(TrackWidth,.09f,length),rotation);
                frame.Box(centre+Vector3.up*.56f,new Vector3(.80f,.10f,length),rotation);
                float trimStart=i==1 ? 0 : .40f,trimEnd=i==points.Length-1 ? 0 : .40f;
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
                int arrows=Math.Max(1,(int)Math.Ceiling(length/1.5f));
                for(int j=0;j<arrows;j++)markings.Arrow(start+direction*((j+.5f)*length/arrows)+Vector3.up*.704f,direction);
                if(i<points.Length-1)
                {
                    // Slightly higher corner bed avoids overlapping top faces at orthogonal elbows.
                    track.Box(end+Vector3.up*.652f,new Vector3(TrackWidth,.09f,TrackWidth),Quaternion.identity);
                    frame.Box(end+Vector3.up*.56f,new Vector3(.80f,.10f,.80f),Quaternion.identity);
                }
            }
            frame.Build(root.transform,"Belt steel frame, rollers and supports","ScrapshiftMaterials/DarkMetal");
            track.Build(root.transform,"Rubber travel surface","ScrapshiftMaterials/WireInsulation");
            markings.Build(root.transform,"Painted conveyor direction","ScrapshiftMaterials/PropAtlas");
            return root;
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
            public void Box(Vector3 centre,Vector3 size,Quaternion rotation)
            {
                Face(centre,rotation,new Vector3(0,0,size.z*.5f),Vector3.right,Vector3.up,size.x,size.y);
                Face(centre,rotation,new Vector3(0,0,-size.z*.5f),Vector3.left,Vector3.up,size.x,size.y);
                Face(centre,rotation,new Vector3(size.x*.5f,0,0),Vector3.back,Vector3.up,size.z,size.y);
                Face(centre,rotation,new Vector3(-size.x*.5f,0,0),Vector3.forward,Vector3.up,size.z,size.y);
                Face(centre,rotation,new Vector3(0,size.y*.5f,0),Vector3.right,Vector3.back,size.x,size.z);
                Face(centre,rotation,new Vector3(0,-size.y*.5f,0),Vector3.right,Vector3.forward,size.x,size.z);
            }
            void Face(Vector3 centre,Quaternion rotation,Vector3 offset,Vector3 right,Vector3 up,float width,float height)
            {
                int n=vertices.Count;var x=right*width*.5f;var y=up*height*.5f;
                vertices.Add(centre+rotation*(offset-x-y));vertices.Add(centre+rotation*(offset+x-y));
                vertices.Add(centre+rotation*(offset+x+y));vertices.Add(centre+rotation*(offset-x+y));
                uv.Add(Vector2.zero);uv.Add(new Vector2(width,0));uv.Add(new Vector2(width,height));uv.Add(new Vector2(0,height));
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
