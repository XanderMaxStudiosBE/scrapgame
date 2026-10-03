using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrapshift.Compact
{
    [Serializable] public sealed class CompactBackdropPlacement
    {
        public string name,model;
        public int sector;
        public float x,y,z,yaw,scale=1;
    }

    [Serializable] public sealed class CompactBackdropLayout
    {
        public int version;
        public CompactBackdropPlacement[] placements;
        public void Validate()
        {
            if(version!=1||placements==null||placements.Length>64)throw new ArgumentException("Invalid compact backdrop catalogue");
            var names=new HashSet<string>();
            foreach(var p in placements)
            {
                if(p==null||string.IsNullOrEmpty(p.name)||!names.Add(p.name)||!CompactYardBackdrop.IsModel(p.model)||p.sector<0||p.sector>5)
                    throw new ArgumentException("Invalid compact backdrop model or sector");
                foreach(float n in new[]{p.x,p.y,p.z,p.yaw,p.scale})
                    if(float.IsNaN(n)||float.IsInfinity(n))throw new ArgumentException("Non-finite compact backdrop transform");
                if(Mathf.Abs(p.x)>46||p.z< -17||p.z>43||p.y<0||p.y>4||Mathf.Abs(p.yaw)>360||p.scale<.6f||p.scale>1.2f)
                    throw new ArgumentException("Compact backdrop exceeds its scenery budget");
                if(Mathf.Abs(p.x)<24&&p.z<18)throw new ArgumentException("Backdrop anchor lies inside the playable yard");
            }
        }
    }

    // Static neighbouring scenery, never inventory/construction/physics. Each region
    // batches independently so the whole horizon is not one always-visible mesh.
    public static class CompactYardBackdrop
    {
        static bool warnedLayout;
        static readonly string[] SectorNames={"North workshop row","West service compound","East service compound","North connected woodline","West connected woodline","East connected woodline"};
        public static bool IsModel(string model)
        {
            return model=="BackdropBrickWorkshop"||model=="BackdropSawtoothWorks"||model=="BackdropTreeGroup"||
                model=="BackdropUtilityPole"||model=="BackdropOverheadSpan"||model=="TealContainer";
        }

        public static CompactBackdropLayout ReadLayout()
        {
            var source=Resources.Load<TextAsset>("ScrapshiftProps/compact_backdrop_layout");
            if(source==null)throw new InvalidOperationException("Original compact backdrop layout has not imported");
            var layout=JsonUtility.FromJson<CompactBackdropLayout>(source.text);
            if(layout==null)throw new ArgumentException("Invalid compact backdrop JSON");
            layout.Validate();return layout;
        }

        public static GameObject Build(Transform parent,bool combine=true)
        {
            var root=new GameObject("Compact industrial backdrop");root.transform.SetParent(parent,false);
            CompactBackdropLayout layout;
            try{layout=ReadLayout();}
            catch(Exception exception) when(exception is ArgumentException||exception is InvalidOperationException)
            {
                if(!warnedLayout){warnedLayout=true;Debug.LogWarning("Optional compact backdrop unavailable: "+exception.Message);}
                return root;
            }
            var material=YardMaterialBindings.Load("ScrapshiftWorld/WorldProps",root.transform);
            var sectors=new Transform[SectorNames.Length];
            foreach(var placement in layout.placements)
            {
                int index=placement.sector;
                if(sectors[index]==null)
                {
                    var group=new GameObject(SectorNames[index]);group.transform.SetParent(root.transform,false);sectors[index]=group.transform;
                }
                if(!AuthoredYardProps.TryPlace(placement.model,sectors[index],new Vector3(placement.x,placement.y,placement.z),out GameObject model))continue;
                model.name=placement.name;
                model.transform.localRotation=Quaternion.Euler(0,placement.yaw,0);
                model.transform.localScale=Vector3.one*placement.scale;
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
                {
                    if(material!=null)renderer.sharedMaterial=material;
                    renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
                }
            }
            if(combine)
                foreach(var sector in sectors)
                {
                    if(sector==null)continue;
                    var renderers=sector.GetComponentsInChildren<MeshRenderer>();
                    var objects=new GameObject[renderers.Length];for(int i=0;i<objects.Length;i++)objects[i]=renderers[i].gameObject;
                    if(objects.Length>0)StaticBatchingUtility.Combine(objects,sector.gameObject);
                }
            return root;
        }
    }
}
