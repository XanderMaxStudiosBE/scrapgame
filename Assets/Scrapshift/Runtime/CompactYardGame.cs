using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Scrapshift.Compact
{
    // One coordinator reuses the existing controller, preferences, theme, lighting and soundscape.
    // Plain core models are authoritative; views never manufacture or transfer inventory.
    public sealed partial class CompactYardGame : MonoBehaviour
    {
        public CompactBalance balance;
        public PrototypeBalance legacyBalance;
        public FirstPersonController player;
        public Texture2D logo;
        public YardLighting lighting;
        public ScrappingModel Model { get; private set; }
        public ConstructionModel Construction { get; private set; }
        public AutomationModel Automation { get; private set; }
        public bool IsPaused { get { return paused; } }
        public bool IsBuilding { get { return (build!=null&&build.Active)||beltStage>0; } }
        enum Page {None,Title,Pause,Welcome,Help,Catalogue,Equipment,LargeScrap,Sales,Delivery,Import}
        Page page=Page.Title,backPage;
        int selectedId;
        bool paused,saveBlocked,hasSave,sessionStarted,confirmNew,confirmRemove;
        PlayerInputSettings controls;
        PresentationSettings presentation;
        SettingsMenu settings;
        YardAudio sounds;
        CompactBuildMode build;
        readonly YardInterfaceTheme theme=new YardInterfaceTheme();
        CompactInteractionTarget target;
        string message="",saveNotice="",hudTitle="",hudHint="",hudHeld="",hudObjective="",buildReason="";
        float messageUntil,nextSave,nextStroke,nextHud,nextViews,nextPower;
        readonly Dictionary<int,CompactPowerStatus> powerCache=new Dictionary<int,CompactPowerStatus>();
        bool hudDirty=true,previewHit,previewClear;
        float fpsTime;int fpsFrames;string fpsLabel="Measuring frames…";
        string SavePath {get{return Path.Combine(Application.persistentDataPath,"yard-v2.json");}}
        string LegacyPath {get{return Path.Combine(Application.persistentDataPath,"yard-v1.json");}}
        void Start()
        {
            if(balance==null){Debug.LogError("Missing compact balance asset. Reimport ScrapshiftCompact/Balance.");enabled=false;return;}
            controls=new PlayerInputSettings();presentation=new PresentationSettings(player.view,transform);
            settings=new SettingsMenu(controls,presentation);player.controls=controls;
            sounds=new YardAudio(transform,player,presentation);
            CompactYardState state=null;
            try{state=CompactSaveStore.Read(SavePath,balance.PreparedRules,out saveNotice);hasSave=state!=null;}
            catch(Exception ex){saveBlocked=true;saveNotice=ex.Message;Debug.LogWarning(ex);}
            BindModel(state);RestorePlayer();SyncViews();Pause(true);Tell(saveNotice);
            nextSave=Time.unscaledTime+15;
        }
        void BindModel(CompactYardState state)
        {
            ResetTransportViews();
            Model=new ScrappingModel(balance.PreparedRules,state);
            Construction=new ConstructionModel(Model.State,Model.Rules);
            Automation=new AutomationModel(Model,Construction);
            RebuildPower();Model.HasPower=id=>PowerStatus(id).powered;
            build=new CompactBuildMode(Construction);
        }
        void RestorePlayer()
        {
            var s=Model.State;
            player.Restore(new YardState{playerX=s.playerX,playerY=s.playerY,playerZ=s.playerZ,yaw=s.yaw,pitch=s.pitch});
        }
        void Pause(bool value)
        {
            paused=value;Time.timeScale=value?0:1;
            if(sounds!=null)sounds.Pause(value);
            if(controls!=null)controls.SuppressUntilRelease();
            Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;
            hudDirty=true;
        }
        void Show(Page next,int id=0)
        {backPage=page;page=next;selectedId=id;menuScroll=Vector2.zero;confirmNew=false;confirmRemove=false;Pause(true);}
        void Back()
        {page=backPage;backPage=Page.None;confirmNew=false;confirmRemove=false;Pause(page!=Page.None);}
        void BeginYard()
        {
            if(saveBlocked){Tell("The unreadable save is protected. Choose an archived new yard.");return;}
            sessionStarted=true;page=Model.State.welcomeSeen?Page.None:Page.Welcome;
            Pause(page!=Page.None);if(page==Page.None)Save();
        }
        void FinishWelcome(){Model.State.welcomeSeen=true;page=Page.None;Pause(false);Save();}
        void Update()
        {
            if(Model==null)return;
            sounds.ApplyVolumes();
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                if(settings.IsOpen){settings.HandleEscape();controls.SuppressUntilRelease();hudDirty=true;}
                else if(IsBuilding){CancelBuild();}
                else if(page==Page.Welcome)FinishWelcome();
                else if(page==Page.None)Show(Page.Pause);
                else if(page==Page.Pause){page=Page.None;Pause(false);}
                else if(page==Page.Title&&confirmNew){confirmNew=false;controls.SuppressUntilRelease();}
                else if(page!=Page.Title)Back();
                return;
            }
            if(settings.IsOpen){settings.UpdateCapture();return;}
            if(paused||!controls.GameplayReady)return;
            player.Step();
            if(Time.unscaledTime>=nextPower){RebuildPower();nextPower=Time.unscaledTime+1;}
            Model.Tick(Time.deltaTime);
            Automation.Tick(Time.deltaTime);UpdateTransportViews();
            if(Time.unscaledTime>=nextViews){RefreshProcessingViews();nextViews=Time.unscaledTime+.2f;}
            StepSound();
            fpsTime+=Time.unscaledDeltaTime;fpsFrames++;
            if(fpsTime>=1){fpsLabel=(fpsFrames/fpsTime).ToString("0")+" fps / "+(fpsTime/fpsFrames*1000).ToString("0.0")+" ms";fpsTime=0;fpsFrames=0;}
            if(Time.unscaledTime>=nextSave){Save();nextSave=Time.unscaledTime+15;}
            if(IsBuilding){if(beltStage>0)UpdateBeltBuild();else UpdateBuild();return;}
            UpdateTarget();
            if(controls.Pressed(ControlAction.BuildToggle)){Show(Page.Catalogue);return;}
            if(controls.Pressed(ControlAction.Interact)){Interact();if(paused)return;}
            if(controls.Pressed(ControlAction.Drop))Drop();
            if(controls.Pressed(ControlAction.ManualWork)&&Time.time>=nextStroke)
            {
                nextStroke=Time.time+.14f;
                if(target!=null&&target.kind==CompactTargetKind.LargeScrap)Act(()=>Model.WorkScrap(target.id),YardSound.Tool);
                else if(target!=null&&target.kind==CompactTargetKind.Equipment)Act(()=>Model.Work(target.id),YardSound.Tool);
            }
        }
        void UpdateTarget()
        {
            CompactInteractionTarget next=null;
            if(Physics.Raycast(player.view.ViewportPointToRay(new Vector3(.5f,.5f)),out RaycastHit hit,3.2f,~(1<<2),QueryTriggerInteraction.Ignore))
                next=hit.collider.GetComponentInParent<CompactInteractionTarget>();
            if(next!=target){target=next;hudDirty=true;}
        }
        void Interact()
        {
            if(target==null)return;
            int id=target.id;
            switch(target.kind)
            {
                case CompactTargetKind.Shop:Show(Page.Catalogue);break;
                case CompactTargetKind.Sales:Show(Page.Sales);break;
                case CompactTargetKind.Delivery:Show(Page.Delivery);break;
                case CompactTargetKind.Wire:Act(()=>Model.AcquireWire());break;
                case CompactTargetKind.Item:Act(()=>Model.Pickup(id));break;
                case CompactTargetKind.LargeScrap:
                    if(Model.FindScrap(id)!=null&&!Model.FindScrap(id).inspected)Act(()=>Model.InspectScrap(id));
                    Show(Page.LargeScrap,id);break;
                case CompactTargetKind.Equipment:
                    if(Model.State.carriedId!=0)
                    {
                        var equipment=Model.FindEquipment(id);
                        bool buffer=equipment!=null&&(equipment.kind==EquipmentKind.Storage||equipment.kind==EquipmentKind.Tier2Scrapper||equipment.kind==EquipmentKind.Splitter||equipment.kind==EquipmentKind.Merger);
                        Act(()=>buffer?Model.Deposit(id):Model.BeginProcessing(id));
                    }
                    else Show(Page.Equipment,id);
                    break;
            }
        }
        bool Act(Func<bool> action,YardSound sound=YardSound.Pickup)
        {
            int oldLevel=Model.Level;bool changed=action();Tell(Model.LastNotice);
            if(changed){sounds.Play(sound);SyncViews();NoticeLevel(oldLevel);Save();}
            hudDirty=true;return changed;
        }
        void NoticeLevel(int oldLevel)
        {
            if(Model.Level<=oldLevel)return;
            string unlock="";
            foreach(var d in Model.Rules.equipment)if(d.unlockLevel>oldLevel&&d.unlockLevel<=Model.Level)
                unlock+=" "+d.name+" ("+(d.available?"available in catalogue":"planned production stage")+").";
            Tell("Level "+Model.Level+" reached! Material sale bonus "+Model.SaleBonusPercent+"%."+unlock);
        }
        void Drop()
        {
            if(Model.State.carriedId==0)return;
            Vector3 origin=player.view.transform.position+player.view.transform.forward*1.1f;
            origin.y=2.5f;
            if(!Physics.Raycast(origin,Vector3.down,out RaycastHit hit,4,~(1<<2),QueryTriggerInteraction.Ignore))
            {Tell("Choose a clear patch of ground.");return;}
            if(hit.normal.y<.8f||hit.point.y>.2f){Tell("Drop onto clear gravel, away from equipment.");return;}
            Vector3 point=hit.point+Vector3.up*.25f;
            if(Physics.CheckBox(point,new Vector3(.33f,.18f,.33f),Quaternion.identity,~(1<<2),QueryTriggerInteraction.Ignore))
            {Tell("Move away from nearby scrap before dropping.");return;}
            Act(()=>Model.Drop(point.x,point.y,point.z));
        }
        void Tell(string text){message=text??"";messageUntil=Time.unscaledTime+7;hudDirty=true;}
        void CapturePlayer()
        {
            Vector3 p=player.transform.position;var s=Model.State;
            s.playerX=Mathf.Clamp(p.x,-23.5f,23.5f);s.playerY=Mathf.Clamp(p.y,1.1f,5);
            s.playerZ=Mathf.Clamp(p.z,-17.5f,17.5f);s.yaw=(player.Yaw%360+360)%360;s.pitch=player.Pitch;
        }
        void RebuildPower()
        {
            powerCache.Clear();foreach(var equipment in Model.State.equipment)
                if(Model.Rules.Equipment(equipment.kind).powerOutput>0||Model.Rules.Equipment(equipment.kind).powerDemand>0)
                    powerCache[equipment.id]=Construction.PowerFor(equipment.id);
        }
        CompactPowerStatus PowerStatus(int id)
        {if(powerCache.TryGetValue(id,out CompactPowerStatus status))return status;return Construction.PowerFor(id);}
        bool Save()
        {
            if(Model==null||saveBlocked||(!sessionStarted&&!hasSave))return false;
            CapturePlayer();
            try{CompactSaveStore.Write(SavePath,Model.State,Model.Rules);hasSave=true;return true;}
            catch(Exception ex){Tell("SAVE FAILED: "+ex.Message);Debug.LogWarning(ex);return false;}
        }
        void OnApplicationFocus(bool focused)
        {
            if(focused||Model==null)return;
            if(IsBuilding)CancelBuild();settings.Close();page=sessionStarted?Page.Pause:Page.Title;Pause(true);Save();
        }
        void OnApplicationQuit(){Save();}
        void OnDestroy()
        {
            theme.Dispose();if(presentation!=null)presentation.Dispose();if(sounds!=null)sounds.Dispose();
            if(lighting!=null)lighting.Dispose();DestroyGhost();DestroyBeltPreview();Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
    }
}
