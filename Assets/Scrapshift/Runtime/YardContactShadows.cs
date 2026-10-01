using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Scrapshift
{
    // One small static mesh for contact grounding. No depth texture, fullscreen AO or extra shadow-casting lights.
    public static class YardContactShadows
    {
        const int Segments=12;
        public static GameObject Build(Transform parent)
        {
            var material=Resources.Load<Material>("ScrapshiftLighting/ContactShade");
            if(material==null){Debug.LogWarning("Missing contact-shade material. Reimport tracked lighting assets.");return null;}
            var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            // A bounded startup scan finds actual placements, including a tuned/manually positioned station.
            foreach(var target in parent.GetComponentsInChildren<InteractionTarget>())
            {
                var p=parent.InverseTransformPoint(target.transform.position);
                switch(target.kind)
                {
                    case TargetKind.Bench:case TargetKind.FanBench:
                        Patch(vertices,colors,triangles,p,1.25f,.75f,.08f);
                        foreach(float x in new[]{-.97f,.97f})foreach(float z in new[]{-.47f,.47f})Patch(vertices,colors,triangles,p+new Vector3(x,0,z),.22f,.22f,.23f);
                        break;
                    case TargetKind.Supply:case TargetKind.FanSupply:case TargetKind.RadioSupply:Patch(vertices,colors,triangles,p,1.3f,.85f,.17f);break;
                    case TargetKind.Machine:Patch(vertices,colors,triangles,p,1.3f,.83f,.16f);break;
                    case TargetKind.Sell:Patch(vertices,colors,triangles,p,1.45f,.85f,.13f);break;
                    case TargetKind.WireStorage:case TargetKind.CopperStorage:Patch(vertices,colors,triangles,p,1.25f,.9f,.14f);break;
                    case TargetKind.DayBoard:Patch(vertices,colors,triangles,p,.8f,.45f,.13f);break;
                    case TargetKind.OrderBoard:
                        foreach(float x in new[]{-2.2f,2.2f})Patch(vertices,colors,triangles,p+new Vector3(x,0,.06f),.25f,.25f,.2f);
                        break;
                }
            }
            foreach(var prop in parent.GetComponentsInChildren<Transform>())
            {
                // FBX imports can retain an identically named mesh beneath their prefab root.
                if(prop.parent!=null&&prop.parent.name==prop.name)continue;
                var p=parent.InverseTransformPoint(prop.position);
                switch(prop.name)
                {
                    case "WornHatchback":case "RustyHatchback":Patch(vertices,colors,triangles,p,1.28f,2.5f,.19f);break;
                    case "ShippingContainer":Patch(vertices,colors,triangles,p,4.75f,2.25f,.15f);break;
                    case "SortingSkip":Patch(vertices,colors,triangles,p,2.2f,2.2f,.15f);break;
                    case "YardOffice":Patch(vertices,colors,triangles,p,4.3f,3.3f,.14f);break;
                    case "PalletBundle":Patch(vertices,colors,triangles,p,1.4f,1,.16f);break;
                }
            }
            var mesh=new Mesh{name="Static yard contact grounding"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var root=new GameObject("Soft static contact shadows");root.transform.SetParent(parent,false);
            root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<ProceduralMeshOwner>().mesh=mesh;
            var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
            return root;
        }
        static void Patch(List<Vector3> vertices,List<Color> colors,List<int> triangles,Vector3 center,float radiusX,float radiusZ,float opacity)
        {
            center.y+=.006f;int start=vertices.Count;vertices.Add(center);colors.Add(new Color(1,1,1,opacity));
            for(int ring=0;ring<2;ring++)for(int i=0;i<Segments;i++)
            {
                float angle=i*Mathf.PI*2/Segments,radius=ring==0?.58f:1;
                vertices.Add(center+new Vector3(Mathf.Cos(angle)*radiusX*radius,0,Mathf.Sin(angle)*radiusZ*radius));
                colors.Add(new Color(1,1,1,ring==0?opacity*.65f:0));
            }
            for(int i=0;i<Segments;i++)
            {
                int next=(i+1)%Segments,a=start+1+i,b=start+1+next,c=a+Segments,d=b+Segments;
                triangles.Add(start);triangles.Add(b);triangles.Add(a);
                triangles.Add(a);triangles.Add(b);triangles.Add(d);
                triangles.Add(a);triangles.Add(d);triangles.Add(c);
            }
        }
    }
}
