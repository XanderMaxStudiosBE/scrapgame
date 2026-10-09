using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Scrapshift.Compact
{
    /// <summary>Original industrial equipment and bounded, coordinator-driven presentation only.</summary>
    public sealed class CompactIndustryVisuals : MonoBehaviour
    {
        const float PrimaryWidth=6,PrimaryDepth=7,ExportWidth=3,ExportDepth=2.5f;
        Transform load,drive,leftClamp,rightClamp;
        Vector3 leftRest,rightRest;
        int loadId;
        ScrapObjectKind loadKind;
        EquipmentKind viewKind;
        bool prepared;
        float driveAngle;

        public static Vector3 PrimaryPowerSocket(CompactRules rules)
        {
            var definition=rules.Equipment(EquipmentKind.PrimaryScrapper);
            return new Vector3(2.56f*definition.width/PrimaryWidth,.89f,-.478f*definition.depth/PrimaryDepth);
        }

        public static Vector3 ExportPowerSocket(CompactRules rules)
        {
            var definition=rules.Equipment(EquipmentKind.ExportStation);
            return new Vector3(1.18f*definition.width/ExportWidth,.94f,-.643f*definition.depth/ExportDepth);
        }

        public static GameObject BuildEquipment(EquipmentKind kind,Transform parent,Vector3 position,float yaw=0,
            bool colliders=true,CompactRules rules=null)
        {
            rules=rules ?? new CompactRules();
            var definition=rules.Equipment(kind);
            if(definition==null)throw new ArgumentException("Unknown industrial equipment.",nameof(kind));
            string model;float width,depth,height;
            switch(kind)
            {
                case EquipmentKind.PrimaryScrapper:model="CompactPrimaryScrapper";width=PrimaryWidth;depth=PrimaryDepth;height=4.3f;break;
                case EquipmentKind.ExportStation:model="CompactExportStation";width=ExportWidth;depth=ExportDepth;height=2.05f;break;
                default:throw new ArgumentException("This equipment has no Stage D view.",nameof(kind));
            }
            var root=new GameObject(kind.ToString());root.transform.SetParent(parent,false);
            root.transform.localPosition=position;root.transform.localRotation=Quaternion.Euler(0,yaw,0);
            if(AuthoredYardProps.TryPlace(model,root.transform,Vector3.zero,out GameObject body))
                body.transform.localScale=new Vector3(definition.width/width,1,definition.depth/depth);
            else Debug.LogWarning("Industrial mesh missing: "+model+". Allow the tracked original FBX to import.");
            if(colliders)
            {
                var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,height*.5f,0);
                box.size=new Vector3(definition.width,height,definition.depth);
                if(kind==EquipmentKind.PrimaryScrapper)
                    YardSignText.Plate(root.transform,"PRIMARY / WHOLE SCRAP",new Vector3(0,3.77f,-1.96f*definition.depth/depth),Mathf.Min(1.95f,definition.width*.72f),.25f);
                else
                    YardSignText.Plate(root.transform,"WEIGH & DISPATCH",new Vector3(0,1.17f,-definition.depth*.5f-.045f),Mathf.Min(1.55f,definition.width*.72f),.23f);
                var view=root.AddComponent<CompactIndustryVisuals>();view.Prepare(kind,rules);
            }
            CompactAutomationVisuals.BuildPorts(kind,root.transform,rules,colliders);
            CompactAutomationVisuals.BuildMachineDetails(kind,root.transform,rules);
            return root;
        }

        void Prepare(EquipmentKind kind,CompactRules rules)
        {
            if(prepared)return;
            prepared=true;viewKind=kind;
            var definition=rules.Equipment(kind);
            float sx=definition.width/(kind==EquipmentKind.PrimaryScrapper?PrimaryWidth:ExportWidth);
            float sz=definition.depth/(kind==EquipmentKind.PrimaryScrapper?PrimaryDepth:ExportDepth);
            // A restrained timing mark rotates over the authored motor face. It
            // uses a private owned mesh and a recovered shared surface material.
            Vector3 point=kind==EquipmentKind.PrimaryScrapper?new Vector3(2.41f*sx,1.50f,.175f*sz):new Vector3(1.16f*sx,1.84f,-.687f*sz);
            var node=new GameObject("Industrial timing mark");node.transform.SetParent(transform,false);node.transform.localPosition=point;
            drive=node.transform;
            var mark=YardGeometry.SurfaceBox("Mechanical timing stripe",drive,new Vector3(0,.076f,0),new Vector3(.018f,.14f,.007f),RetroSurface.CorrugatedMetal,false);
            NoShadow(mark);
            if(kind==EquipmentKind.PrimaryScrapper)
            {
                leftRest=new Vector3(-1.32f*sx,.79f,-.48f*sz);rightRest=new Vector3(1.32f*sx,.79f,-.48f*sz);
                leftClamp=YardGeometry.SurfaceBox("Left hydraulic clamp",transform,leftRest,new Vector3(.14f*sx,.33f,1.09f*sz),RetroSurface.DarkMetal,false).transform;
                rightClamp=YardGeometry.SurfaceBox("Right hydraulic clamp",transform,rightRest,new Vector3(.14f*sx,.33f,1.09f*sz),RetroSurface.DarkMetal,false).transform;
                NoShadow(leftClamp.gameObject);NoShadow(rightClamp.gameObject);
            }
        }

        public static void SyncJob(GameObject root,EquipmentState equipment,CompactRules rules)
        {
            if(root==null || equipment==null || rules==null)return;
            if(equipment.kind!=EquipmentKind.PrimaryScrapper && equipment.kind!=EquipmentKind.ExportStation)return;
            var view=root.GetComponent<CompactIndustryVisuals>();
            if(view==null){view=root.AddComponent<CompactIndustryVisuals>();view.Prepare(equipment.kind,rules);}
            var job=equipment.industry!=null?equipment.industry.primary:null;
            if(equipment.kind!=EquipmentKind.PrimaryScrapper || job==null)
            {
                view.ClearLoad();return;
            }
            if(view.load!=null && view.loadId==job.id && view.loadKind==job.kind)return;
            view.ClearLoad();view.loadId=job.id;view.loadKind=job.kind;
            string model=job.kind==ScrapObjectKind.Car?"WornHatchback":"CompactRefrigerator";
            if(AuthoredYardProps.TryPlace(model,root.transform,new Vector3(0,.46f,-.10f),out GameObject loaded))
            {
                var definition=rules.Equipment(equipment.kind);
                // A resized catalogue footprint still contains the loaded object
                // and bed. The source mesh remains untouched and never gains a
                // collider, Rigidbody, item identity, or processing component.
                float scale=Mathf.Min(1,Mathf.Min(definition.width/PrimaryWidth,definition.depth/PrimaryDepth));
                loaded.transform.localScale=Vector3.one*scale;loaded.name="Loaded whole scrap / "+job.id;
                view.load=loaded.transform;
            }
        }

        void ClearLoad()
        {
            if(load!=null)
            {
                load.gameObject.SetActive(false);
                if(Application.isPlaying)Destroy(load.gameObject);else DestroyImmediate(load.gameObject);
            }
            load=null;loadId=0;
        }

        // No Update lives on this component. The single gameplay coordinator
        // calls this only during unpaused simulation, with authoritative power
        // and backpressure status. A blocked machine retains its exact pose.
        public static void Step(GameObject root,EquipmentState equipment,bool running,float delta,float gameplayTime)
        {
            if(root==null || equipment==null || !running || !(delta>0) || float.IsInfinity(delta) || float.IsNaN(delta) ||
                float.IsInfinity(gameplayTime) || float.IsNaN(gameplayTime))return;
            var view=root.GetComponent<CompactIndustryVisuals>();if(view==null || !view.prepared)return;
            if(view.viewKind!=equipment.kind)return;
            if(equipment.kind==EquipmentKind.PrimaryScrapper && (equipment.industry==null || equipment.industry.primary==null))return;
            if(equipment.kind==EquipmentKind.ExportStation && (equipment.industry==null || !equipment.industry.enabled || equipment.contents.Count==0))return;
            view.driveAngle=(view.driveAngle+Mathf.Min(delta,.25f)*130)%360;
            if(view.drive!=null)view.drive.localRotation=Quaternion.Euler(0,0,view.driveAngle);
            if(view.leftClamp!=null)
            {
                float travel=(Mathf.Sin(gameplayTime*2.8f)+1)*.035f;
                view.leftClamp.localPosition=view.leftRest+Vector3.right*travel;
                view.rightClamp.localPosition=view.rightRest+Vector3.left*travel;
            }
        }

        static void NoShadow(GameObject root)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        }
    }
}
