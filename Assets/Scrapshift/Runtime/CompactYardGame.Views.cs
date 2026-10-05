using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

namespace Scrapshift.Compact
{
    public sealed partial class CompactYardGame
    {
        sealed class EntityView {public GameObject root,details;public string key,geometryKey;public CompactEquipmentViewStamp equipmentStamp;}
        readonly Dictionary<int,EntityView> views=new Dictionary<int,EntityView>();
        readonly HashSet<int> viewActive=new HashSet<int>();
        readonly List<int> staleViews=new List<int>();
        GameObject cables,ghost;
        Material ghostMaterial;
        bool lastPreviewValid;
        string cableKey="";
        readonly Collider[] placementHits=new Collider[32];
        static readonly ProfilerMarker ViewsMarker=new ProfilerMarker("Scrapshift.Compact.SyncViews");
        static readonly ProfilerMarker HudMarker=new ProfilerMarker("Scrapshift.Compact.RefreshHud");
        void SyncViews(){using(ViewsMarker.Auto())SyncViewsNow();}
        void SyncViewsNow()
        {
            RebuildPower();
            var active=viewActive;active.Clear();
            foreach(var item in Model.State.items)
            {
                active.Add(item.id);bool held=item.id==Model.State.carriedId;
                string key="item:"+item.kind+":"+item.quantity+":"+item.x+":"+item.y+":"+item.z+":"+held;
                if(Unchanged(item.id,key))continue;
                var go=CompactEquipmentVisuals.BuildPart(item.kind,held?player.view.transform:transform,
                    held?new Vector3(.44f,-.37f,.8f):new Vector3(item.x,Mathf.Max(0,item.y-.25f),item.z),!held);
                if(held){SetLayer(go,2);go.transform.localScale=Vector3.one*.65f;}
                else AttachTarget(go,CompactTargetKind.Item,item.id);
                views[item.id]=new EntityView{root=go,key=key};
            }
            foreach(var scrap in Model.State.scrap)
            {
                active.Add(scrap.id);
                string geometryKey="scrap:"+scrap.kind+":"+scrap.x+":"+scrap.z;
                GameObject go;
                if(views.TryGetValue(scrap.id,out EntityView previous)&&previous.root!=null&&previous.geometryKey==geometryKey)go=previous.root;
                else
                {
                    RemoveView(scrap.id);
                    go=CompactEquipmentVisuals.BuildScrap(scrap.kind,transform,new Vector3(scrap.x,0,scrap.z));
                    AttachTarget(go,CompactTargetKind.LargeScrap,scrap.id);
                    views[scrap.id]=new EntityView{root=go,geometryKey=geometryKey};
                }
                CompactWorkVisuals.ApplyScrapProgress(go,scrap,Model.Rules);
            }
            foreach(var equipment in Model.State.equipment)
            {
                active.Add(equipment.id);
                var definition=Model.Rules.Equipment(equipment.kind);
                var power=equipment.kind==EquipmentKind.Tier1Scrapper||equipment.kind==EquipmentKind.Tier2Scrapper?PowerStatus(equipment.id):null;
                GameObject go;
                if(views.TryGetValue(equipment.id,out EntityView previous)&&previous.root!=null&&previous.equipmentStamp!=null&&previous.equipmentStamp.GeometryMatches(equipment,definition))
                {
                    go=previous.root;CompactIndustryVisuals.SyncJob(go,equipment,Model.Rules);
                    if(previous.equipmentStamp.DetailsMatch(equipment,power!=null&&power.powered,power!=null&&power.overloaded))
                    {previous.equipmentStamp.Capture(equipment,definition,power!=null&&power.powered,power!=null&&power.overloaded);continue;}
                    if(previous.details!=null){previous.details.SetActive(false);DestroyOwnedView(previous.details);}
                }
                else
                {
                    RemoveView(equipment.id);
                    go=CompactEquipmentVisuals.Build(equipment.kind,transform,new Vector3(equipment.x,0,equipment.z),equipment.yaw,true,Model.Rules);
                    AttachTarget(go,CompactTargetKind.Equipment,equipment.id);
                    CompactWorkVisuals.PrepareEquipment(go,equipment.kind,Model.Rules);
                    CompactIndustryVisuals.SyncJob(go,equipment,Model.Rules);
                }
                var details=new GameObject("Current contents and work status");details.transform.SetParent(go.transform,false);
                if(equipment.kind==EquipmentKind.Tier1Scrapper||equipment.kind==EquipmentKind.Tier2Scrapper)
                {
                    Color status=equipment.job!=null&&equipment.job.ready?new Color(.35f,.67f,.42f):power.overloaded?YardGeometry.Rust:
                        !power.powered?YardGeometry.Charcoal:equipment.job==null?new Color(.35f,.67f,.42f):new Color(.83f,.62f,.24f);
                    Vector3 indicator=equipment.kind==EquipmentKind.Tier2Scrapper?CompactAutomationVisuals.Tier2PowerSocket(Model.Rules)+Vector3.up*.25f:new Vector3(-.9f,1.25f,-.28f);
                    YardGeometry.Box("Power and work indicator",details.transform,indicator,new Vector3(.075f,.06f,.025f),status,false);
                }
                if(equipment.job!=null)
                {
                    var job=equipment.job;
                    if(!job.ready)CompactEquipmentVisuals.BuildPart(job.input,details.transform,equipment.kind==EquipmentKind.Tier1Scrapper?
                        new Vector3(0,1.95f,.2f):equipment.kind==EquipmentKind.Tier2Scrapper?new Vector3(0,.75f,-Model.Rules.Equipment(equipment.kind).depth*.5f):new Vector3(0,1.17f,-.25f),false).transform.localScale=Vector3.one*.6f;
                    else
                    {
                        int slot=0;
                        foreach(var output in job.yields)if(output.quantity>0)
                        {
                            var outputView=CompactEquipmentVisuals.BuildPart(output.kind,details.transform,
                                new Vector3((slot++-1)*.45f,equipment.kind==EquipmentKind.Workbench?1.17f:.70f,equipment.kind==EquipmentKind.Tier1Scrapper?-.78f:equipment.kind==EquipmentKind.Tier2Scrapper?Model.Rules.Equipment(equipment.kind).depth*.5f:-.1f),false);
                            outputView.transform.localScale=Vector3.one*.5f;
                        }
                    }
                }
                for(int slot=0;slot<Mathf.Min(3,equipment.contents.Count);slot++)
                {
                    var part=CompactEquipmentVisuals.BuildPart(equipment.contents[slot].kind,details.transform,new Vector3((slot-1)*.35f,.78f,0),false);
                    part.transform.localScale=Vector3.one*.4f;
                }
                var stamp=previous!=null&&previous.root==go&&previous.equipmentStamp!=null?previous.equipmentStamp:new CompactEquipmentViewStamp();
                stamp.Capture(equipment,definition,power!=null&&power.powered,power!=null&&power.overloaded);
                views[equipment.id]=new EntityView{root=go,details=details,equipmentStamp=stamp};
            }
            staleViews.Clear();foreach(var pair in views)if(!active.Contains(pair.Key))staleViews.Add(pair.Key);
            foreach(int id in staleViews){RemoveView(id);}
            SyncCables();SyncTransportGeometry();UpdateTransportViews();SyncDressing();hudDirty=true;
        }
        void StepWorkViews()
        {
            foreach(var pair in views)if(pair.Value.root!=null)CompactWorkVisuals.StepPulse(pair.Value.root,Time.deltaTime);
            foreach(var equipment in Model.State.equipment)
                if(views.TryGetValue(equipment.id,out EntityView view)&&view.root!=null)
                {
                    bool industryRunning=equipment.kind==EquipmentKind.PrimaryScrapper&&IsWorkingMachine(equipment)||
                        equipment.kind==EquipmentKind.ExportStation&&equipment.industry!=null&&equipment.industry.enabled&&Industry.DispatchQuote(equipment.id).allowed;
                    CompactIndustryVisuals.Step(view.root,equipment,industryRunning,Time.deltaTime,Time.time);
                    CompactWorkVisuals.AnimateEquipment(view.root,equipment,
                        (equipment.kind==EquipmentKind.Tier1Scrapper||equipment.kind==EquipmentKind.Tier2Scrapper)&&
                        equipment.job!=null&&!equipment.job.ready&&PowerStatus(equipment.id).powered&&
                        Model.ProcessingBlockReason(equipment.id)=="Processing",Time.time);
                }
        }
        void PulseWork(int id)
        {if(views.TryGetValue(id,out EntityView view)&&view.root!=null)CompactWorkVisuals.Pulse(view.root);}
        bool Unchanged(int id,string key)
        {
            if(views.TryGetValue(id,out EntityView view)&&view.root!=null&&view.key==key)return true;
            RemoveView(id);return false;
        }
        void RemoveView(int id)
        {
            if(!views.TryGetValue(id,out EntityView view))return;
            if(view.root!=null){foreach(var c in view.root.GetComponentsInChildren<Collider>())c.enabled=false;DestroyOwnedView(view.root);}
            views.Remove(id);
        }
        void RefreshProcessingViews()
        {
            foreach(var equipment in Model.State.equipment)
            {
                var power=equipment.kind==EquipmentKind.Tier1Scrapper||equipment.kind==EquipmentKind.Tier2Scrapper?PowerStatus(equipment.id):null;
                if(!views.TryGetValue(equipment.id,out EntityView view)||view.root==null||view.equipmentStamp==null||
                    !view.equipmentStamp.Matches(equipment,Model.Rules.Equipment(equipment.kind),power!=null&&power.powered,power!=null&&power.overloaded))
                {SyncViews();return;}
            }
        }
        static void AttachTarget(GameObject root,CompactTargetKind kind,int id)
        {var t=root.AddComponent<CompactInteractionTarget>();t.kind=kind;t.id=id;}
        static void SetLayer(GameObject go,int layer)
        {foreach(var node in go.GetComponentsInChildren<Transform>())node.gameObject.layer=layer;}
        void SyncCables()
        {
            string key="";
            foreach(var link in Model.State.powerLinks)
            {
                var a=Model.FindEquipment(link.a);var b=Model.FindEquipment(link.b);
                key+=link.a+":"+link.b+":"+a.x+":"+a.z+":"+a.yaw+":"+b.x+":"+b.z+":"+b.yaw+";";
            }
            if(key==cableKey)return; cableKey=key;
            if(cables!=null)DestroyOwnedView(cables);cables=new GameObject("Player-built power cables");cables.transform.SetParent(transform,false);
            foreach(var link in Model.State.powerLinks)
            {
                var a=Model.FindEquipment(link.a);var b=Model.FindEquipment(link.b);
                Vector3 start=PowerPort(a),end=PowerPort(b);
                var line=new GameObject("Power "+link.a+" to "+link.b).AddComponent<LineRenderer>();line.transform.SetParent(cables.transform,false);
                line.sharedMaterial=YardGeometry.PaletteMaterial(new Color(.74f,.48f,.12f));line.widthMultiplier=.045f;
                line.useWorldSpace=false;line.positionCount=4;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
                line.SetPositions(new[]{start,new Vector3(start.x,.06f,start.z),new Vector3(end.x,.06f,end.z),end});
            }
        }
        Vector3 PowerPort(EquipmentState item)
        {
            Vector3 socket=item.kind==EquipmentKind.Generator?new Vector3(.45f,.75f,-.55f):
                item.kind==EquipmentKind.PrimaryScrapper?CompactIndustryVisuals.PrimaryPowerSocket(Model.Rules):
                item.kind==EquipmentKind.ExportStation?CompactIndustryVisuals.ExportPowerSocket(Model.Rules):
                item.kind==EquipmentKind.Tier2Scrapper?CompactAutomationVisuals.Tier2PowerSocket(Model.Rules):new Vector3(-.9f,.9f,-.29f);
            return new Vector3(item.x,0,item.z)+Quaternion.Euler(0,item.yaw,0)*socket;
        }
        bool IsWorkingMachine(EquipmentState equipment)
        {
            if(equipment.kind!=EquipmentKind.PrimaryScrapper&&equipment.kind!=EquipmentKind.Tier1Scrapper&&equipment.kind!=EquipmentKind.Tier2Scrapper)return false;
            if(!PowerStatus(equipment.id).powered)return false;
            if(equipment.kind==EquipmentKind.PrimaryScrapper)
                return equipment.industry!=null&&equipment.industry.primary!=null&&
                    Model.StoredUnits(equipment.id)+CompactIndustryModel.ReservedUnits(equipment)<=Model.Rules.Equipment(equipment.kind).outputCapacity&&
                    (long)Model.State.nextId+CompactIndustryModel.ReservedSlots(equipment)<=int.MaxValue;
            return (equipment.kind==EquipmentKind.Tier1Scrapper||equipment.kind==EquipmentKind.Tier2Scrapper)&&
                equipment.job!=null&&!equipment.job.ready&&Model.ProcessingBlockReason(equipment.id)=="Processing";
        }
        void StepSound()
        {
            EquipmentState nearest=null;float nearestDistance=float.MaxValue;
            var listener=player.transform.position;
            foreach(var equipment in Model.State.equipment)
            {
                if(!IsWorkingMachine(equipment))continue;
                float dx=equipment.x-listener.x,dz=equipment.z-listener.z;
                float distance=dx*dx+dz*dz;
                if(distance<nearestDistance||distance==nearestDistance&&(nearest==null||equipment.id<nearest.id))
                {nearest=equipment;nearestDistance=distance;}
            }
            sounds.StepCompact(nearest!=null,nearest==null?Vector3.zero:new Vector3(nearest.x,1,nearest.z));
        }
        void BeginBuild(EquipmentKind kind,int movingId=0)
        {
            if(kind==EquipmentKind.Conveyor&&movingId==0){BeginBelt();return;}
            if(Model.State.carriedId!=0){Tell("Put down your carried component before building.");return;}
            if(movingId!=0&&!build.BeginMove(movingId)){Tell(build.Reason);return;}
            if(movingId==0)build.Begin(kind);
            page=Page.None;Pause(false);CreateGhost();Tell("Choose a position. Nothing is spent until placement is confirmed.");
        }
        void CreateGhost()
        {
            DestroyGhost();
            ghost=CompactEquipmentVisuals.Build(build.SelectedKind,transform,build.PreviewPosition,build.Yaw,false,Model.Rules);SetLayer(ghost,2);
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");
            ghostMaterial=new Material(shader){name="Private construction preview",hideFlags=HideFlags.DontSave};
            ghostMaterial.SetFloat("_Surface",1);ghostMaterial.SetFloat("_Blend",0);ghostMaterial.SetFloat("_ZWrite",0);
            ghostMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);ghostMaterial.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ghostMaterial.renderQueue=(int)RenderQueue.Transparent;
            foreach(var r in ghost.GetComponentsInChildren<Renderer>())
            {
                var materials=r.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=ghostMaterial;r.sharedMaterials=materials;
                r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            }
            lastPreviewValid=false;ghostMaterial.SetColor("_BaseColor",new Color(.86f,.26f,.13f,.45f));
        }
        void UpdateBuild()
        {
            if(controls.Pressed(ControlAction.BuildToggle)){CancelBuild();return;}
            if(controls.Pressed(ControlAction.BuildRotate))build.Rotate();
            var ray=player.view.ViewportPointToRay(new Vector3(.5f,.5f));
            var ground=new Plane(Vector3.up,Vector3.zero);
            previewHit=ground.Raycast(ray,out float distance)&&distance<=12&&distance>=0;
            if(previewHit)build.UpdatePreview(ray.GetPoint(distance));
            bool avoidsPlayer=PreviewAvoidsPlayer();bool avoidsWorld=previewHit&&PreviewAvoidsWorld();
            previewClear=previewHit&&build.Valid&&avoidsPlayer&&avoidsWorld;
            buildReason=!previewHit?"Look at the ground within 12 metres.":!build.Valid?build.Reason:!avoidsPlayer?"Step clear of the equipment footprint.":!avoidsWorld?"Blocked by fixed scenery or another object.":"Clear / ready to place";
            if(ghost!=null)
            {
                ghost.SetActive(previewHit);ghost.transform.localPosition=build.PreviewPosition;ghost.transform.localRotation=Quaternion.Euler(0,build.Yaw,0);
                if(lastPreviewValid!=previewClear){lastPreviewValid=previewClear;ghostMaterial.SetColor("_BaseColor",previewClear?new Color(.34f,.72f,.52f,.45f):new Color(.86f,.26f,.13f,.45f));}
            }
            if(controls.Pressed(ControlAction.Interact))
            {
                if(!previewClear){Tell(buildReason);return;}
                if(build.Confirm())
                {Tell(Construction.LastMessage);sounds.Play(YardSound.Tool);DestroyGhost();SyncViews();Save();controls.SuppressUntilRelease();}
                else Tell(build.Reason);
            }
        }
        bool PreviewAvoidsPlayer()
        {
            var d=Model.Rules.Equipment(build.SelectedKind);
            Vector3 local=Quaternion.Euler(0,-build.Yaw,0)*(player.transform.position-build.PreviewPosition);
            return Mathf.Abs(local.x)>d.width*.5f+.35f||Mathf.Abs(local.z)>d.depth*.5f+.35f;
        }
        bool PreviewAvoidsWorld()
        {
            var d=Model.Rules.Equipment(build.SelectedKind);
            int count=Physics.OverlapBoxNonAlloc(build.PreviewPosition+Vector3.up*1.05f,new Vector3(d.width*.5f,1,d.depth*.5f),
                placementHits,Quaternion.Euler(0,build.Yaw,0),~(1<<2),QueryTriggerInteraction.Ignore);
            if(count==placementHits.Length)return false;
            for(int i=0;i<count;i++)
            {
                var owner=placementHits[i].GetComponentInParent<CompactInteractionTarget>();
                if(owner!=null&&owner.kind==CompactTargetKind.Equipment&&owner.id==build.MovingId)continue;
                return false;
            }
            return true;
        }
        void CancelBuild()
        {bool conveyor=beltStage>0;beltStage=0;DestroyBeltPreview();build.Cancel();DestroyGhost();Tell(conveyor?"Conveyor cancelled; nothing spent.":build.Reason);controls.SuppressUntilRelease();hudDirty=true;}
        void DestroyGhost()
        {DestroyOwnedView(ghost);DestroyOwnedView(ghostMaterial);ghost=null;ghostMaterial=null;}
        static void DestroyOwnedView(Object value)
        {if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void LateUpdate()
        {if(Model!=null&&(hudDirty||(!paused&&Time.unscaledTime>=nextHud))){RefreshHud();nextHud=Time.unscaledTime+.1f;hudDirty=false;}}
        void RefreshHud(){using(HudMarker.Auto())RefreshHudNow();}
        void RefreshHudNow()
        {
            hudTitle="€"+Model.State.money+"    •    LEVEL "+Model.Level+"    •    "+Model.State.experience+" XP";
            var carried=Model.FindItem(Model.State.carriedId);
            hudHeld=carried==null?"":Model.Rules.Part(carried.kind).name+" ×"+carried.quantity+"   ["+controls.Label(ControlAction.Drop)+"] put down";
            hudHint=Hint();
            RefreshGuidance();
            RefreshTargetProgress();
            if(!saveFailed&&lastSaveAt>=0)
            {float elapsed=Time.unscaledTime-lastSaveAt;saveStatus=elapsed<5?"Saved just now":elapsed<60?"Saved "+elapsed.ToString("0")+"s ago":"Saved "+(elapsed/60).ToString("0")+"m ago";}
        }
        string Hint()
        {
            if(target==null)return "";string interact="["+controls.Label(ControlAction.Interact)+"] ";string work="["+controls.Label(ControlAction.ManualWork)+"] ";
            switch(target.kind)
            {
                case CompactTargetKind.Shop:return interact+"Buy / place equipment";
                case CompactTargetKind.Sales:return interact+"Material prices / sell carried goods";
                case CompactTargetKind.Delivery:return interact+"Buy replacement scrap";
                case CompactTargetKind.Wire:return interact+"Take renewable wiring (free)";
                case CompactTargetKind.Item:
                    var item=Model.FindItem(target.id);return item==null?"":interact+"Pick up "+Model.Rules.Part(item.kind).name+" ×"+item.quantity;
                case CompactTargetKind.LargeScrap:
                    var scrap=Model.FindScrap(target.id);if(scrap==null)return "";
                    if(!scrap.inspected)return interact+"Inspect "+Model.Rules.LargeRecipe(scrap.kind).name;
                    return scrap.strokes<scrap.requiredStrokes?work+ScrapStage(scrap)+" / "+scrap.strokes+" of "+scrap.requiredStrokes+" • "+interact+"details":interact+"Collect dismantled components";
                case CompactTargetKind.Equipment:
                    var gear=Model.FindEquipment(target.id);if(gear==null)return "";
                    if(gear.kind==EquipmentKind.PrimaryScrapper)return interact+"Whole-object intake / "+Industry.Status(gear.id);
                    if(gear.kind==EquipmentKind.ExportStation&&Model.State.carriedId==0)return interact+"Material dispatch / "+Industry.Status(gear.id);
                    if(Model.State.carriedId!=0)return interact+(gear.kind==EquipmentKind.Storage||gear.kind==EquipmentKind.Tier2Scrapper||gear.kind==EquipmentKind.Splitter||gear.kind==EquipmentKind.Merger||gear.kind==EquipmentKind.ExportStation?"Deposit into ":"Load ")+Model.Rules.Equipment(gear.kind).name;
                    if(gear.job==null)return interact+Model.Rules.Equipment(gear.kind).name+" / manage";
                    if(gear.job.ready)return interact+"Collect output / manage";
                    if(gear.kind==EquipmentKind.Workbench)return work+"Process component / "+gear.job.strokes+" of "+gear.job.requiredStrokes;
                    return PowerStatus(gear.id).powered?Model.ProcessingBlockReason(gear.id)+" / "+gear.job.remaining.ToString("0.0")+"s • "+interact+"details":PowerStatus(gear.id).reason+" • "+interact+"connections";
            }
            return "";
        }
        string ScrapStage(LargeScrapJob scrap)
        {return Model.ScrapWorkStage(scrap.id);}
    }
}
