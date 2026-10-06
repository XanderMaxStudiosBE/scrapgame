using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Scrapshift.Compact
{
    public sealed partial class CompactYardGame
    {
        int beltStage,beltFrom,beltFromPort;
        bool beltXFirst=true;
        LineRenderer beltPreview;
        string beltViewKey="";
        GameObject beltViews;
        sealed class TransitView {public PartKind kind;public GameObject root;}
        readonly Dictionary<int,TransitView> transitViews=new Dictionary<int,TransitView>();
        readonly Dictionary<PartKind,Stack<GameObject>> transitPool=new Dictionary<PartKind,Stack<GameObject>>();
        const int MaximumSpareTransitViews=16;
        int spareTransitViews;
        readonly HashSet<int> transitActive=new HashSet<int>();
        readonly List<int> transitStale=new List<int>();
        readonly Dictionary<int,Vector3[]> transitPaths=new Dictionary<int,Vector3[]>();
        readonly Dictionary<int,float> transitLengths=new Dictionary<int,float>();
        void BeginBelt(int fromId=0,int fromPort=0)
        {
            var definition=Model.Rules.Equipment(EquipmentKind.Conveyor);
            if(!definition.available||Model.Level<definition.unlockLevel){Tell("Conveyors unlock at level "+definition.unlockLevel+".");return;}
            if(Model.Carried!=null){Tell("Put down your carried item before connecting conveyors.");return;}
            build.Cancel();DestroyGhost();DestroyBeltPreview();
            beltFrom=fromId;beltFromPort=fromPort;beltStage=fromId==0?1:2;
            beltXFirst=true;page=Page.None;Pause(false);controls.SuppressUntilRelease();hudDirty=true;
            Tell("Choose an output, then an input. Confirm with ["+controls.Label(ControlAction.Interact)+"]. Escape cancels without spending.");
        }
        void UpdateBeltBuild()
        {
            if(controls.Pressed(ControlAction.BuildToggle)){CancelBuild();return;}
            if(controls.Pressed(ControlAction.BuildRotate))beltXFirst=!beltXFirst;
            var ray=player.view.ViewportPointToRay(new Vector3(.5f,.5f));
            bool hit=Physics.Raycast(ray,out RaycastHit result,12,~(1<<2),QueryTriggerInteraction.Ignore);
            var owner=hit?result.collider.GetComponentInParent<CompactInteractionTarget>():null;
            var gear=owner!=null&&owner.kind==CompactTargetKind.Equipment?Model.FindEquipment(owner.id):null;
            var mouth=hit?result.collider.GetComponentInParent<CompactConveyorPortTarget>():null;
            bool selectingOutput=beltStage==1;
            int port=gear==null?-1:mouth!=null?
                (mouth.output==selectingOutput&&mouth.index>=0&&mouth.index<AutomationModel.PortCount(gear.kind,selectingOutput)?mouth.index:-1):
                NearestPort(gear,selectingOutput,result.point);
            previewClear=false;
            buildReason=beltStage==1?"Aim at a green OUT mouth to start.":"Aim at an amber IN mouth on another machine.";
            if(mouth!=null&&mouth.output!=selectingOutput)buildReason=selectingOutput?"That is an IN mouth. Start at a green OUT mouth.":"That is an OUT mouth. Finish at an amber IN mouth.";
            if(beltStage==1)
            {
                if(port<0){DestroyBeltPreview();return;}
                var socket=AutomationModel.Port(gear,Model.Rules,true,port);
                PreviewBelt(new[]{new Vector3(socket.x,.72f,socket.z),new Vector3(socket.x,.99f,socket.z)},true);
                buildReason=Model.Rules.Equipment(gear.kind).name+" #"+gear.id+" / output "+(port+1)+" / select start";
                if(controls.Pressed(ControlAction.Interact))
                {beltFrom=gear.id;beltFromPort=port;beltStage=2;controls.SuppressUntilRelease();hudDirty=true;}
                return;
            }
            if(port<0)
            {
                var source=Model.FindEquipment(beltFrom);
                if(source==null){CancelBuild();return;}
                var socket=AutomationModel.Port(source,Model.Rules,true,beltFromPort);
                var end=hit?result.point:ray.GetPoint(6);
                PreviewBelt(new[]{new Vector3(socket.x,.75f,socket.z),new Vector3(end.x,.75f,end.z)},false);
                return;
            }
            var link=new ConveyorLink{fromId=beltFrom,fromPort=beltFromPort,toId=gear.id,toPort=port,bendXFirst=beltXFirst};
            previewClear=Automation.CanConnect(beltFrom,beltFromPort,gear.id,port,beltXFirst,out string reason);
            var path=AutomationModel.Path(link,Model.State,Model.Rules);
            if(previewClear&&!BeltAvoidsWorld(path,beltFrom,gear.id)){previewClear=false;reason="Conveyor path is blocked by fixed scenery or loose scrap.";}
            int cost=(int)Math.Ceiling(AutomationModel.Length(path)/2)*Model.Rules.Equipment(EquipmentKind.Conveyor).price;
            buildReason=previewClear?"OUT → IN / €"+cost+" / "+AutomationModel.Length(path).ToString("0.0")+"m":reason;
            var points=new Vector3[path.Length];for(int i=0;i<points.Length;i++)points[i]=new Vector3(path[i].x,.75f,path[i].z);
            PreviewBelt(points,previewClear);
            if(controls.Pressed(ControlAction.Interact))
            {
                if(!previewClear){Tell(buildReason);return;}
                if(AutomationAct(()=>Automation.Connect(beltFrom,beltFromPort,gear.id,port,beltXFirst)))
                {beltStage=0;DestroyBeltPreview();controls.SuppressUntilRelease();hudDirty=true;}
            }
        }
        void PreviewBelt(Vector3[] points,bool valid)
        {
            if(beltPreview==null)
            {
                beltPreview=new GameObject("Conveyor port preview").AddComponent<LineRenderer>();beltPreview.transform.SetParent(transform,false);
                beltPreview.useWorldSpace=false;beltPreview.widthMultiplier=.14f;beltPreview.shadowCastingMode=ShadowCastingMode.Off;beltPreview.receiveShadows=false;
            }
            beltPreview.gameObject.SetActive(true);
            beltPreview.sharedMaterial=YardGeometry.PaletteMaterial(valid?new Color(.2f,.7f,.65f):YardGeometry.Rust);
            beltPreview.positionCount=points.Length;beltPreview.SetPositions(points);
        }
        int NearestPort(EquipmentState gear,bool output,Vector3 aim)
        {
            return CompactPortSelection.Find(gear,Model.Rules,output,aim.x,aim.y,aim.z);
        }
        bool BeltAvoidsWorld(CompactPortPoint[] path,int from,int to)
        {
            for(int i=1;i<path.Length;i++)
            {
                var a=new Vector3(path[i-1].x,.7f,path[i-1].z);var b=new Vector3(path[i].x,.7f,path[i].z);
                if((b-a).sqrMagnitude<.0001f)continue;
                int count=Physics.OverlapBoxNonAlloc((a+b)*.5f,new Vector3(.36f,.28f,(b-a).magnitude*.5f),placementHits,
                    Quaternion.LookRotation(b-a),~(1<<2),QueryTriggerInteraction.Ignore);
                if(count==placementHits.Length)return false;
                for(int j=0;j<count;j++)
                {
                    var owner=placementHits[j].GetComponentInParent<CompactInteractionTarget>();
                    if(owner!=null&&owner.kind==CompactTargetKind.Equipment&&(owner.id==from||owner.id==to))continue;
                    return false;
                }
            }
            return true;
        }
        void DestroyBeltPreview(){if(beltPreview!=null)DestroyOwnedView(beltPreview.gameObject);beltPreview=null;}
        bool AutomationAct(Func<bool> action)
        {
            bool changed=action();Tell(Automation.LastMessage);
            if(changed){sounds.Play(YardSound.Tool);SyncViews();Save();hudDirty=true;}return changed;
        }
        void DrawAutomation(EquipmentState gear)
        {
            int inputs=AutomationModel.PortCount(gear.kind,false),outputs=AutomationModel.PortCount(gear.kind,true);
            if(inputs==0&&outputs==0)return;
            if(ScrappingModel.HasBuffer(gear.kind))
            {
                int capacity=Model.Rules.Equipment(gear.kind).outputCapacity;
                if(ScrappingModel.IsComponentProcessor(gear.kind))
                {
                    Text("IN QUEUE / "+Model.StoredUnits(gear.id)+" / "+capacity+" units  •  OUT / "+Model.OutputQuantity(gear.id)+" / "+capacity+" units "+(gear.job==null?"(empty)":gear.job.ready?"(ready)":"(reserved)"));
                    Text(gear.kind==EquipmentKind.Workbench?"Belts deliver components and collect finished materials. Use your manual tools to complete each batch.":"Powered batches take components from IN. Finished materials leave OUT automatically when a belt can accept them.");
                }
                else Text("BUFFER / "+Model.StoredUnits(gear.id)+" / "+capacity+" units. Transfers grant no XP.");
                if(gear.kind!=EquipmentKind.PrimaryScrapper&&Button("Deposit carried bundle",Model.Carried!=null))Act(()=>Model.Deposit(gear.id));
                // Present matching units as a single carry bundle, keeping recovered-sale eligibility separate.
                var groups=new HashSet<int>();
                foreach(var stack in gear.contents.ToArray())
                {
                    int group=(int)stack.kind*2+(stack.xpEligible?1:0);if(!groups.Add(group))continue;
                    int quantity=0;foreach(var item in gear.contents)if(item.kind==stack.kind&&item.xpEligible==stack.xpEligible)quantity+=item.quantity;
                    int id=stack.id;
                    if(Button("Withdraw "+Model.Rules.Part(stack.kind).name+" ×"+quantity+(stack.xpEligible?" / sale XP":" / no sale XP"),Model.Carried==null))Act(()=>Model.WithdrawBatch(gear.id,id));
                }
                Text((ScrappingModel.IsComponentProcessor(gear.kind)?"INPUT RECIPE":gear.kind==EquipmentKind.ExportStation?"DISPATCH FILTER":"OUTPUT FILTER")+" / "+(gear.filterKind<0?"All supported items":Model.Rules.Part((PartKind)gear.filterKind).name));
                if(Button("Allow all supported items"))Act(()=>Model.SetFilter(gear.id,-1));
                foreach(var part in Model.Rules.parts)
                {
                    if(ScrappingModel.IsComponentProcessor(gear.kind)&&Model.Rules.Recipe(part.kind)==null)continue;
                    if(gear.kind==EquipmentKind.ExportStation&&!CompactIndustryModel.CanExportPart(Model.Rules,part.kind))continue;
                    int kind=(int)part.kind;
                    if(Button("Only "+part.name,gear.filterKind!=kind))Act(()=>Model.SetFilter(gear.id,kind));
                }
            }
            Text("CONVEYOR PORTS / "+inputs+" in / "+outputs+" out. Select an output then aim at an input. ["+controls.Label(ControlAction.BuildRotate)+"] changes the elbow; Escape cancels. Belts keep blocked items and cannot be dismantled while loaded.");
            for(int port=0;port<outputs;port++)
            {int chosen=port;var definition=Model.Rules.Equipment(EquipmentKind.Conveyor);if(Button("Connect output "+(port+1)+" conveyor / level "+definition.unlockLevel,Model.Carried==null&&definition.available&&Model.Level>=definition.unlockLevel))BeginBelt(gear.id,chosen);}
            foreach(var link in Model.State.belts.ToArray())
            {
                if(link.fromId!=gear.id&&link.toId!=gear.id)continue;
                bool allowed=Automation.CanRemove(link.id,out string reason);int id=link.id;
                Text("Belt #"+link.id+" / #"+link.fromId+" → #"+link.toId+" / "+link.items.Count+" moving items / "+reason);
                if(Button("Dismantle empty belt #"+link.id+" / refund €"+(link.paidPrice/2),allowed))AutomationAct(()=>Automation.Remove(id));
            }
        }
        void SyncTransportGeometry()
        {
            string key="";foreach(var belt in Model.State.belts)
            {
                var from=Model.FindEquipment(belt.fromId);var to=Model.FindEquipment(belt.toId);
                var a=Model.Rules.Equipment(from.kind);var b=Model.Rules.Equipment(to.kind);
                key+=belt.id+":"+belt.fromId+":"+belt.fromPort+":"+belt.toId+":"+belt.toPort+":"+belt.bendXFirst+":"+from.x+":"+from.z+":"+from.yaw+":"+to.x+":"+to.z+":"+to.yaw+":"+a.width+":"+a.depth+":"+b.width+":"+b.depth+";";
            }
            if(key==beltViewKey&&beltViews!=null)return;beltViewKey=key;
            if(beltViews!=null)DestroyOwnedView(beltViews);beltViews=new GameObject("Player conveyor network");beltViews.transform.SetParent(transform,false);
            transitPaths.Clear();transitLengths.Clear();
            foreach(var link in Model.State.belts)
            {
                CompactAutomationVisuals.BuildBelt(link,Model.State,Model.Rules,beltViews.transform);
                var path=AutomationModel.Path(link,Model.State,Model.Rules);var points=new Vector3[path.Length];
                for(int i=0;i<path.Length;i++)points[i]=new Vector3(path[i].x,.7f,path[i].z);
                transitPaths[link.id]=points;transitLengths[link.id]=AutomationModel.Length(path);
            }
        }
        void UpdateTransportViews()
        {
            transitActive.Clear();
            foreach(var link in Model.State.belts)foreach(var item in link.items)
            {
                transitActive.Add(item.id);
                if(!transitViews.TryGetValue(item.id,out TransitView view))
                {
                    GameObject root=null;if(transitPool.TryGetValue(item.kind,out Stack<GameObject> pool)&&pool.Count>0){root=pool.Pop();spareTransitViews--;}
                    if(root==null){root=CompactEquipmentVisuals.BuildPart(item.kind,transform,Vector3.zero,false);root.transform.localScale=Vector3.one*.45f;SetLayer(root,2);}
                    root.SetActive(true);view=new TransitView{kind=item.kind,root=root};transitViews[item.id]=view;
                }
                if(transitPaths.TryGetValue(link.id,out Vector3[] path))view.root.transform.localPosition=TransitPosition(path,Mathf.Clamp01(item.progress)*transitLengths[link.id]);
            }
            transitStale.Clear();foreach(var pair in transitViews)if(!transitActive.Contains(pair.Key))transitStale.Add(pair.Key);
            foreach(int id in transitStale)
            {
                var view=transitViews[id];view.root.SetActive(false);
                if(spareTransitViews<MaximumSpareTransitViews)
                {
                    if(!transitPool.TryGetValue(view.kind,out Stack<GameObject> pool)){pool=new Stack<GameObject>();transitPool[view.kind]=pool;}
                    pool.Push(view.root);spareTransitViews++;
                }
                else DestroyOwnedView(view.root);
                transitViews.Remove(id);
            }
        }
        static Vector3 TransitPosition(Vector3[] path,float remaining)
        {
            for(int i=1;i<path.Length;i++)
            {
                float length=(path[i]-path[i-1]).magnitude;
                if(remaining<=length&&length>.0001f)return path[i-1]+(path[i]-path[i-1])*(remaining/length);
                remaining-=length;
            }
            return path[path.Length-1];
        }
        void ResetTransportViews()
        {
            beltStage=0;DestroyBeltPreview();beltViewKey="";
            if(beltViews!=null)DestroyOwnedView(beltViews);beltViews=null;
            foreach(var view in transitViews.Values)DestroyOwnedView(view.root);
            foreach(var pool in transitPool.Values)foreach(var root in pool)DestroyOwnedView(root);
            transitViews.Clear();transitPool.Clear();transitActive.Clear();transitStale.Clear();
            spareTransitViews=0;
            transitPaths.Clear();transitLengths.Clear();
        }
    }
}
