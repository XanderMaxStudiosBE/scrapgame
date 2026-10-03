using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrapshift.Compact
{
    /// <summary>Cached presentation only. The coordinator supplies saved work state and unpaused time.</summary>
    public sealed class CompactWorkVisuals : MonoBehaviour
    {
        const float PulseDuration=.22f;
        const int MaximumSourceTriangles=6000;
        Transform[] panels;
        Transform[] components;
        PartKind[] componentKinds;
        Vector3[] componentRest;
        Transform pocket,tool,drive;
        Vector3 toolRest;
        float pulseAge=PulseDuration,driveAngle;
        bool scrapPrepared,equipmentPrepared,cutaway;

        static CompactWorkVisuals Cache(GameObject root)
        {
            if(root==null)return null;
            var view=root.GetComponent<CompactWorkVisuals>();
            return view!=null?view:root.AddComponent<CompactWorkVisuals>();
        }

        public static void ApplyScrapProgress(GameObject root,LargeScrapJob job,CompactRules rules)
        {
            if(root==null || job==null || rules==null)return;
            var view=Cache(root);
            if(!view.scrapPrepared)view.PrepareScrap(job.kind);
            int required=job.requiredStrokes>0?job.requiredStrokes:rules.LargeRecipe(job.kind).strokes;
            float progress=job.inspected && required>0?Mathf.Clamp01((float)job.strokes/required):0;
            bool opened=job.inspected && job.strokes>0;
            bool ready=opened && job.strokes>=required;
            if(view.panels!=null)
            {
                // All original faces remain in the closed state; only the appropriate stage opens them.
                if(job.kind==ScrapObjectKind.Car)
                {
                    view.panels[1].gameObject.SetActive(!ready);
                    view.panels[1].localRotation=Quaternion.Euler(opened?Mathf.Lerp(16,32,progress):0,0,0);
                    view.panels[2].gameObject.SetActive(progress<.8f);
                }
                else
                {
                    view.panels[1].gameObject.SetActive(!opened);
                    view.panels[2].gameObject.SetActive(progress<.75f);
                }
            }
            if(view.pocket!=null)view.pocket.gameObject.SetActive(opened && view.cutaway);
            for(int i=0;i<view.components.Length;i++)
            {
                Vector3 offset=Vector3.zero;
                if(view.cutaway && job.kind==ScrapObjectKind.Car && view.componentKinds[i]==PartKind.Motor && progress>=.66f)
                    offset=Vector3.up*(ready?.23f:.18f);
                if(view.cutaway && job.kind==ScrapObjectKind.Refrigerator && (ready || (view.componentKinds[i]==PartKind.Compressor && progress>=.5f)))
                    offset=new Vector3(0,0,ready?-.16f:-.10f);
                view.components[i].localPosition=view.componentRest[i]+offset;
                bool present=true;
                if(ready && job.remaining!=null)
                {
                    present=false;
                    foreach(var amount in job.remaining)
                        if(amount!=null && amount.kind==view.componentKinds[i] && amount.quantity>0){present=true;break;}
                }
                bool visible=ready || (opened && view.cutaway &&
                    (view.componentKinds[i]==PartKind.Motor || view.componentKinds[i]==PartKind.Compressor ||
                     (view.componentKinds[i]==PartKind.Wire && progress>=.3f)));
                view.components[i].gameObject.SetActive(present && visible);
            }
            view.tool.gameObject.SetActive(job.inspected && !ready);
        }

        void PrepareScrap(ScrapObjectKind kind)
        {
            scrapPrepared=true;
            string name=kind==ScrapObjectKind.Car?"WornHatchback":"CompactRefrigerator";
            var model=transform.Find(name);
            panels=new Transform[3];
            Vector3 pivot=kind==ScrapObjectKind.Car?new Vector3(0,.88f,-1.07f):Vector3.zero;
            cutaway=TryPartition(model,kind,pivot,out panels);
            if(!cutaway)panels=null; // Missing/unreadable meshes retain their original rendering and collider.
            pocket=new GameObject("Opened component bay").transform;pocket.SetParent(transform,false);
            if(cutaway)
            {
                if(kind==ScrapObjectKind.Car)
                    Surface("Engine bay floor",pocket,new Vector3(0,.64f,-1.62f),new Vector3(1.40f,.035f,.92f));
                else
                {
                    Surface("Inner rear lining",pocket,new Vector3(0,.95f,.26f),new Vector3(.66f,1.54f,.025f));
                    Surface("Inner bottom lining",pocket,new Vector3(0,.17f,-.04f),new Vector3(.66f,.025f,.55f));
                }
            }
            componentKinds=kind==ScrapObjectKind.Car?
                new[]{PartKind.Motor,PartKind.Wire,PartKind.BodyMetal}:
                new[]{PartKind.Compressor,PartKind.Wire,PartKind.BodyMetal,PartKind.Plastic};
            components=new Transform[componentKinds.Length];
            componentRest=new Vector3[components.Length];
            for(int i=0;i<components.Length;i++)
            {
                Vector3 position=kind==ScrapObjectKind.Car?
                    new[]{new Vector3(-.15f,.70f,-1.54f),new Vector3(.43f,.75f,-1.58f),new Vector3(0,.92f,1.42f)}[i]:
                    new[]{new Vector3(-.12f,.20f,-.14f),new Vector3(.12f,.73f,-.15f),new Vector3(-.10f,1.02f,-.13f),new Vector3(.1f,1.47f,-.13f)}[i];
                if(!cutaway)position=kind==ScrapObjectKind.Car?new Vector3(position.x,.96f,position.z):new Vector3(.49f,position.y,-.20f);
                var part=CompactEquipmentVisuals.BuildPart(componentKinds[i],transform,position,false);
                part.name="Recoverable "+componentKinds[i];part.transform.localScale=Vector3.one*(kind==ScrapObjectKind.Car?.85f:cutaway?.48f:.23f);
                NoShadow(part);components[i]=part.transform;componentRest[i]=position;
            }
            toolRest=kind==ScrapObjectKind.Car?new Vector3(.68f,.93f,-1.53f):new Vector3(.16f,.52f,-.32f);
            tool=BuildTool(transform,toolRest);
        }

        static GameObject Surface(string name,Transform parent,Vector3 centre,Vector3 size)
        {
            var result=YardGeometry.SurfaceBox(name,parent,centre,size,RetroSurface.DarkMetal,false);
            NoShadow(result);return result;
        }

        static bool TryPartition(Transform model,ScrapObjectKind kind,Vector3 pivot,out Transform[] groups)
        {
            groups=null;
            if(model==null)return false;
            var filters=model.GetComponentsInChildren<MeshFilter>();
            if(filters.Length!=1)return false;
            var filter=filters[0];var source=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();
            if(source==null || !source.isReadable || source.subMeshCount!=1 || source.GetTopology(0)!=MeshTopology.Triangles || renderer==null ||
                source.GetIndexCount(0)>MaximumSourceTriangles*3 || source.vertexCount>18000)return false;
            var vertices=source.vertices;var indices=source.triangles;
            var matrix=model.parent.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            if(float.IsNaN(matrix.determinant) || float.IsInfinity(matrix.determinant) || Mathf.Abs(matrix.determinant)<.000000000001f)return false;
            var buckets=new[]{new List<int>(),new List<int>(),new List<int>()};
            var uv=source.uv;
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=matrix.MultiplyPoint3x4(vertices[indices[i]]);var b=matrix.MultiplyPoint3x4(vertices[indices[i+1]]);var c=matrix.MultiplyPoint3x4(vertices[indices[i+2]]);
                int group=Classify(kind,a,matrix.determinant<0?c:b,matrix.determinant<0?b:c,uv.Length==vertices.Length?uv[indices[i]]:Vector2.zero);
                buckets[group].Add(indices[i]);buckets[group].Add(indices[i+1]);buckets[group].Add(indices[i+2]);
            }
            if(buckets[0].Count==0 || buckets[1].Count==0 || buckets[2].Count==0)return false;
            groups=new Transform[3];
            string[] names={"Retained scrap shell","Work access panel","Remaining trim and glazing"};
            for(int group=0;group<3;group++)
            {
                var node=new GameObject(names[group]);node.transform.SetParent(model.parent,false);
                Vector3 origin=group==1?pivot:Vector3.zero;node.transform.localPosition=origin;
                var mesh=CopyFaces(source,buckets[group],matrix,origin,names[group]);
                node.AddComponent<MeshFilter>().sharedMesh=mesh;node.AddComponent<ProceduralMeshOwner>().mesh=mesh;
                var copy=node.AddComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;
                copy.shadowCastingMode=renderer.shadowCastingMode;copy.receiveShadows=renderer.receiveShadows;
                groups[group]=node.transform;
            }
            renderer.enabled=false;
            return true;
        }

        static int Classify(ScrapObjectKind kind,Vector3 a,Vector3 b,Vector3 c,Vector2 uv)
        {
            float maxZ=Mathf.Max(a.z,Mathf.Max(b.z,c.z)),minY=Mathf.Min(a.y,Mathf.Min(b.y,c.y));
            float maxY=Mathf.Max(a.y,Mathf.Max(b.y,c.y));
            if(kind==ScrapObjectKind.Car)
            {
                // Original atlas tile 7 contains glazing; body, tyres and wheels are never mistaken for it.
                if(uv.x>.75f && uv.y>.25f && uv.y<.5f)return 2;
                if(maxZ< -1.07f && minY>.75f && Vector3.Cross(b-a,c-a).y>.00001f)return 1;
            }
            else
            {
                // Includes the case front behind each enamel door, avoiding an opaque block behind the opening.
                if(maxZ<-.33f && minY<.2f && maxY>1.7f)return 1;
                if(maxZ<-.34f)
                {
                    if(maxY<1.415f)return 1;
                    return 2;
                }
            }
            return 0;
        }

        static Mesh CopyFaces(Mesh source,List<int> indices,Matrix4x4 matrix,Vector3 pivot,string name)
        {
            var positions=source.vertices;var normals=source.normals;var uv=source.uv;
            var normalMatrix=matrix.inverse.transpose;
            var verts=new List<Vector3>(indices.Count);var tex=new List<Vector2>(indices.Count);var normal=new List<Vector3>(indices.Count);var triangles=new List<int>(indices.Count);
            foreach(int index in indices)
            {
                verts.Add(matrix.MultiplyPoint3x4(positions[index])-pivot);
                tex.Add(uv.Length==positions.Length?uv[index]:Vector2.zero);
                if(normals.Length==positions.Length)normal.Add(normalMatrix.MultiplyVector(normals[index]).normalized);
                triangles.Add(triangles.Count);
            }
            if(matrix.determinant<0)
                for(int i=0;i<triangles.Count;i+=3)
                {int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
            var mesh=new Mesh{name=name+" / private work faces"};mesh.SetVertices(verts);mesh.SetUVs(0,tex);mesh.SetTriangles(triangles,0);
            if(normal.Count==verts.Count)mesh.SetNormals(normal);else mesh.RecalculateNormals();
            mesh.RecalculateBounds();return mesh;
        }

        public static void PrepareEquipment(GameObject root,EquipmentKind kind,CompactRules rules)
        {
            if(root==null || (kind!=EquipmentKind.Workbench && kind!=EquipmentKind.Tier1Scrapper && kind!=EquipmentKind.Tier2Scrapper))return;
            var view=Cache(root);if(view.equipmentPrepared)return;
            view.equipmentPrepared=true;
            if(kind==EquipmentKind.Workbench)
            {
                view.toolRest=new Vector3(.43f,1.18f,-.25f);view.tool=BuildTool(root.transform,view.toolRest);
                view.tool.gameObject.SetActive(false);
            }
            else
            {
                view.drive=new GameObject("Visible drive timing marks").transform;view.drive.SetParent(root.transform,false);
                if(kind==EquipmentKind.Tier1Scrapper)
                {
                    float width=rules!=null?rules.Equipment(kind).width/2.4f:1,depth=rules!=null?rules.Equipment(kind).depth/2.3f:1;
                    view.drive.localPosition=new Vector3(0,1.52f,.04f*depth);
                    Surface("Turning cutter mark",view.drive,new Vector3(0,.105f,0),new Vector3(1.1f*width,.018f,.055f));
                }
                else
                {
                    float width=rules!=null?rules.Equipment(kind).width/3.2f:1,depth=rules!=null?rules.Equipment(kind).depth/2.56f:1;
                    view.drive.localPosition=new Vector3(-1.515f*width,1.16f,.18f*depth);
                    Surface("Drive end timing mark",view.drive,Vector3.zero,new Vector3(.01f,.035f,.36f));
                }
            }
        }

        public static void AnimateEquipment(GameObject root,EquipmentState equipment,bool running,float gameplayTime)
        {
            if(root==null || equipment==null || float.IsNaN(gameplayTime) || float.IsInfinity(gameplayTime))return;
            var view=root.GetComponent<CompactWorkVisuals>();if(view==null || !view.equipmentPrepared)return;
            if(view.tool!=null)view.tool.gameObject.SetActive(equipment.job!=null && !equipment.job.ready);
            if(view.drive==null)return;
            if(running && equipment.job!=null && !equipment.job.ready)view.driveAngle=Mathf.Repeat(gameplayTime*480,360);
            view.drive.localRotation=Quaternion.Euler(view.driveAngle,0,0);
        }

        public static void Pulse(GameObject root)
        {
            if(root==null)return;var view=root.GetComponent<CompactWorkVisuals>();if(view!=null)view.pulseAge=0;
        }

        public static void StepPulse(GameObject root,float delta)
        {
            if(root==null || delta<=0 || float.IsNaN(delta) || float.IsInfinity(delta))return;
            var view=root.GetComponent<CompactWorkVisuals>();if(view==null || view.tool==null || view.pulseAge>=PulseDuration)return;
            view.pulseAge=Mathf.Min(PulseDuration,view.pulseAge+delta);
            float stroke=Mathf.Sin(view.pulseAge/PulseDuration*Mathf.PI);
            if(view.pulseAge>=PulseDuration)stroke=0;
            view.tool.localPosition=view.toolRest+new Vector3(-.11f,.07f,.02f)*stroke;
            view.tool.localRotation=Quaternion.Euler(0,-25+20*stroke,-16*stroke);
        }

        static Transform BuildTool(Transform parent,Vector3 position)
        {
            var tool=new GameObject("Working hand tool").transform;tool.SetParent(parent,false);tool.localPosition=position;tool.localRotation=Quaternion.Euler(0,-25,0);
            var handle=YardGeometry.SurfaceBox("Worn tool grip",tool,new Vector3(0,0,-.10f),new Vector3(.048f,.04f,.21f),RetroSurface.RustPaint,false);
            NoShadow(handle);Surface("Steel tool jaw",tool,new Vector3(0,0,.065f),new Vector3(.075f,.025f,.13f));
            return tool;
        }

        static void NoShadow(GameObject root)
        {foreach(var renderer in root.GetComponentsInChildren<Renderer>()){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}}
    }
}
