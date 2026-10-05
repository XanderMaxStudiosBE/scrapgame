using UnityEngine;

namespace Scrapshift.Compact
{
    public sealed partial class CompactYardGame
    {
        bool completionPresented;
        static readonly CompactGuideServices GuideServices=new CompactGuideServices
        {
            sales=Destination("Office sales counter",CompactYardWorld.SalesAnchor),
            shop=Destination("Equipment counter",CompactYardWorld.ShopAnchor),
            delivery=Destination("Scrap delivery",CompactYardWorld.DeliveryAnchor),
            wire=Destination("Free wiring crate",CompactYardWorld.WireAnchor)
        };
        static YardDestination Destination(string name,Vector3 anchor)
        {return new YardDestination(YardLandmark.Automatic,name,anchor.x,anchor.z);}

        bool CustomerCounterContext
        {
            get
            {
                if(page==Page.Sales)return true;
                if(page!=Page.Contracts)return false;
                foreach(var frame in pageHistory)if(frame.page==Page.Sales)return true;
                return false;
            }
        }
        bool DeliverCustomerRequest()
        {
            if(!CustomerCounterContext)
            {Tell("Bring your bundle to the office sales counter to deliver this request.");return false;}
            return Act(()=>Model.Career.DeliverContract(),YardSound.Sale);
        }
        bool RefreshCareer()
        {
            bool changed=Model.Career.RefreshProgress();
            if(sessionStarted&&!paused&&!IsBuilding&&Model.Career.Completed&&
                !Model.Career.Stats.completionAcknowledged&&!completionPresented)
            {
                completionPresented=true;Show(Page.Journal);changed=true;
            }
            if(changed)hudDirty=true;
            return changed;
        }
        void RefreshGuidance()
        {
            var position=player.transform.position;
            var step=CompactGuidance.Resolve(Model,position.x,position.z,GuideServices,
                controls.Label(ControlAction.Interact),controls.Label(ControlAction.ManualWork));
            hudObjective=step.action;
            hudDirection=step.hasDestination?
                step.destination.Direction(position.x,position.z,player.Yaw)+" / "+step.destination.name+" / "+
                step.destination.Distance(position.x,position.z).ToString("0")+"m":"";
        }
        void RefreshTargetProgress()
        {
            hudProgress=-1;hudProgressLabel="";
            if(target==null||IsBuilding)return;
            if(target.kind==CompactTargetKind.LargeScrap)
            {
                var scrap=Model.FindScrap(target.id);
                if(scrap==null||!scrap.inspected||scrap.requiredStrokes<=0)return;
                hudProgress=Mathf.Clamp01((float)scrap.strokes/scrap.requiredStrokes);
                hudProgressLabel=scrap.strokes>=scrap.requiredStrokes?"Components ready to remove":
                    "Dismantling / "+scrap.strokes+" of "+scrap.requiredStrokes+" stages";
            }
            else if(target.kind==CompactTargetKind.Equipment)
            {
                var equipment=Model.FindEquipment(target.id);var job=equipment==null?null:equipment.job;
                if(equipment!=null&&equipment.kind==EquipmentKind.PrimaryScrapper&&equipment.industry!=null&&equipment.industry.primary!=null)
                {
                    var primary=equipment.industry.primary;
                    hudProgress=Mathf.Clamp01(1-primary.remaining/primary.duration);
                    hudProgressLabel=IsWorkingMachine(equipment)?"Dismantling / "+primary.remaining.ToString("0.0")+"s remaining":
                        Industry.Status(equipment.id)+" / progress retained";
                    return;
                }
                if(equipment!=null&&equipment.kind==EquipmentKind.ExportStation&&equipment.industry!=null&&equipment.industry.enabled)
                {
                    float duration=Model.Rules.Equipment(equipment.kind).processingSeconds;
                    hudProgress=Mathf.Clamp01(1-equipment.industry.remaining/duration);
                    hudProgressLabel=Industry.DispatchQuote(equipment.id).allowed?"Next dispatch / "+equipment.industry.remaining.ToString("0.0")+"s":Industry.Status(equipment.id);
                    return;
                }
                if(job==null)return;
                hudProgress=job.ready?1:equipment.kind==EquipmentKind.Workbench?
                    Mathf.Clamp01((float)job.strokes/job.requiredStrokes):Mathf.Clamp01(1-job.remaining/job.duration);
                hudProgressLabel=job.ready?"Recovered materials ready":equipment.kind==EquipmentKind.Workbench?
                    "Hand work / "+job.strokes+" of "+job.requiredStrokes+" strokes":
                    "Processing / "+job.remaining.ToString("0.0")+"s remaining";
                if(!job.ready&&equipment.kind!=EquipmentKind.Workbench)
                {
                    string blocked=Model.ProcessingBlockReason(equipment.id);
                    if(!PowerStatus(equipment.id).powered)hudProgressLabel=PowerStatus(equipment.id).reason+" / progress retained";
                    else if(blocked!="Processing")hudProgressLabel=blocked+" / progress retained";
                }
            }
        }
    }
}
