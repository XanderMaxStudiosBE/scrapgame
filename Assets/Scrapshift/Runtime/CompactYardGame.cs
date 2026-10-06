using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Profiling;

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
        public CompactIndustryModel Industry { get { return Model==null?null:Model.Industry; } }
        public bool IsPaused { get { return paused; } }
        public bool IsBuilding { get { return (build!=null&&build.Active)||beltStage>0; } }
        enum Page {None,Title,Pause,Welcome,Help,Catalogue,Equipment,LargeScrap,Sales,Delivery,Import,Journal,Contracts,Credits}
        Page page=Page.Title;
        struct MenuFrame {public Page page;public int selectedId;public Vector2 scroll;}
        readonly Stack<MenuFrame> pageHistory=new Stack<MenuFrame>();
        int selectedId;
        bool paused,saveBlocked,hasSave,sessionStarted,confirmNew,confirmRemove;
        PlayerInputSettings controls;
        PresentationSettings presentation;
        SettingsMenu settings;
        YardAudio sounds;
        CompactBuildMode build;
        readonly YardInterfaceTheme theme=new YardInterfaceTheme();
        CompactInteractionTarget target;
        CompactConveyorPortTarget aimedPort;
        string message="",saveNotice="",hudTitle="",hudHint="",hudHeld="",hudObjective="",hudDirection="",buildReason="";
        string hudProgressLabel="",saveStatus="Not saved yet";
        float hudProgress=-1,lastSaveAt=-1,nextCareer;
        static readonly ProfilerMarker SaveMarker=new ProfilerMarker("Scrapshift.Compact.Save");
        bool saveFailed;
        readonly ProgressCheckpoint workCheckpoint=new ProgressCheckpoint();
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
            if(hasSave)saveStatus="Saved yard loaded";
            nextSave=Time.unscaledTime+15;
        }
        void BindModel(CompactYardState state)
        {
            ResetTransportViews();
            Model=new ScrappingModel(balance.PreparedRules,state);
            Construction=new ConstructionModel(Model.State,Model.Rules);
            Automation=new AutomationModel(Model,Construction);
            RebuildPower();Model.HasPower=id=>PowerStatus(id).powered;
            Model.Career.RefreshProgress();completionPresented=false;
            lastSaveAt=-1;saveFailed=false;saveStatus="Not saved yet";workCheckpoint.RecordAttempt(true);
            build=new CompactBuildMode(Construction);
        }
        void RestorePlayer()
        {
            var s=Model.State;
            player.Restore(new YardState{playerX=s.playerX,playerY=s.playerY,playerZ=s.playerZ,yaw=s.yaw,pitch=s.pitch});
            RequireDressingSpawnClearance();
        }
        void Pause(bool value)
        {
            if(value&&!paused)SavePendingWork();
            paused=value;Time.timeScale=value?0:1;
            if(!value){pageHistory.Clear();ResetIndustryReview();}
            if(sounds!=null)sounds.Pause(value);
            if(controls!=null)controls.SuppressUntilRelease();
            Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;
            hudDirty=true;
        }
        void Show(Page next,int id=0)
        {
            ResetIndustryReview();ResetRequestReview();
            pageHistory.Push(new MenuFrame{page=page,selectedId=selectedId,scroll=menuScroll});
            page=next;selectedId=id;menuScroll=Vector2.zero;confirmNew=false;confirmRemove=false;Pause(true);
        }
        void Back()
        {
            ResetIndustryReview();ResetRequestReview();
            if(page==Page.Journal&&Model.Career.AcknowledgeCompletion())Save();
            if(pageHistory.Count>0)
            {var previous=pageHistory.Pop();page=previous.page;selectedId=previous.selectedId;menuScroll=previous.scroll;}
            else page=sessionStarted?Page.None:Page.Title;
            confirmNew=false;confirmRemove=false;Pause(page!=Page.None);
        }
        void BeginYard()
        {
            ResetIndustryReview();ResetRequestReview();
            if(saveBlocked){Tell("The unreadable save is protected. Choose an archived new yard.");return;}
            sessionStarted=true;page=Model.State.welcomeSeen?Page.None:Page.Welcome;
            Pause(page!=Page.None);if(page==Page.None)Save();
        }
        void FinishWelcome(){Model.State.welcomeSeen=true;page=Page.None;Pause(false);Save();}
        void Update()
        {
            if(Model==null)return;
            controls.UpdatePending(Time.unscaledTime);presentation.UpdatePending(Time.unscaledTime);
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
            if(controls.Pressed(ControlAction.Journal)){Show(Page.Journal);return;}
            player.Step();
            if(Time.unscaledTime>=nextPower){RebuildPower();nextPower=Time.unscaledTime+1;}
            int previousMoney=Model.State.money,previousXp=Model.State.experience,previousNextId=Model.State.nextId,previousLevel=Model.Level;
            Model.Tick(Time.deltaTime);
            Industry.Tick(Time.deltaTime);
            Automation.Tick(Time.deltaTime);UpdateTransportViews();
            if(Model.State.money!=previousMoney||Model.State.experience!=previousXp||Model.State.nextId!=previousNextId)
            {SyncViews();NoticeLevel(previousLevel);RefreshCareer();Save();hudDirty=true;}
            if(paused)return;
            UpdateWorkCheckpoint();
            StepWorkViews();
            if(Time.unscaledTime>=nextCareer)
            {if(RefreshCareer())Save();nextCareer=Time.unscaledTime+.5f;if(paused)return;}
            if(Time.unscaledTime>=nextViews){RefreshProcessingViews();nextViews=Time.unscaledTime+.2f;}
            StepSound();
            fpsTime+=Time.unscaledDeltaTime;fpsFrames++;
            if(fpsTime>=1){fpsLabel=(fpsFrames/fpsTime).ToString("0")+" fps / "+(fpsTime/fpsFrames*1000).ToString("0.0")+" ms";fpsTime=0;fpsFrames=0;}
            if(Time.unscaledTime>=nextSave){if(!saveFailed)Save();nextSave=Time.unscaledTime+15;}
            if(IsBuilding){if(beltStage>0)UpdateBeltBuild();else UpdateBuild();return;}
            UpdateTarget();
            if(controls.Pressed(ControlAction.BuildToggle)){Show(Page.Catalogue);return;}
            if(controls.Pressed(ControlAction.Interact)){Interact();if(paused)return;}
            if(controls.Pressed(ControlAction.Drop))Drop();
            if(controls.Pressed(ControlAction.ManualWork)&&Time.time>=nextStroke)
            {
                nextStroke=Time.time+.14f;
                if(target!=null&&target.kind==CompactTargetKind.LargeScrap)
                {int id=target.id;ManualAct(id,true);}
                else if(target!=null&&target.kind==CompactTargetKind.Equipment)
                {int id=target.id;ManualAct(id,false);}
            }
        }
        void UpdateTarget()
        {
            CompactInteractionTarget next=null;
            CompactConveyorPortTarget nextPort=null;
            if(Physics.Raycast(player.view.ViewportPointToRay(new Vector3(.5f,.5f)),out RaycastHit hit,3.2f,~(1<<2),QueryTriggerInteraction.Ignore))
            {
                next=hit.collider.GetComponentInParent<CompactInteractionTarget>();
                nextPort=hit.collider.GetComponentInParent<CompactConveyorPortTarget>();
            }
            if(next!=target||nextPort!=aimedPort){target=next;aimedPort=nextPort;hudDirty=true;}
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
                    var inspected=Model.FindEquipment(id);
                    if(inspected!=null&&(inspected.kind==EquipmentKind.PrimaryScrapper||inspected.kind==EquipmentKind.Generator)){Show(Page.Equipment,id);break;}
                    if(Model.State.carriedId!=0)
                    {
                        if(aimedPort!=null&&aimedPort.output){Tell("That is the OUT mouth. Take components to the amber IN mouth.");break;}
                        var equipment=Model.FindEquipment(id);
                        bool direct=equipment!=null&&(equipment.kind==EquipmentKind.Workbench||equipment.kind==EquipmentKind.Tier1Scrapper)&&equipment.job==null&&equipment.contents.Count==0;
                        if(direct&&!Model.CanBeginProcessing(id,out _)&&Model.CanDeposit(id,out _))direct=false;
                        bool buffer=equipment!=null&&ScrappingModel.HasBuffer(equipment.kind)&&!direct;
                        Act(()=>buffer?Model.Deposit(id):Model.BeginProcessing(id));
                    }
                    else Show(Page.Equipment,id);
                    break;
            }
        }
        bool Act(Func<bool> action,YardSound sound=YardSound.Pickup)
        {
            int oldLevel=Model.Level;bool changed=action();Tell(Model.LastNotice);
            if(changed){sounds.Play(sound);SyncViews();NoticeLevel(oldLevel);RefreshCareer();Save();}
            hudDirty=true;return changed;
        }
        bool IndustryAct(Func<bool> action)
        {
            int oldLevel=Model.Level;bool changed=action();Tell(Industry.LastMessage);
            if(changed){sounds.Play(YardSound.Tool);SyncViews();NoticeLevel(oldLevel);RefreshCareer();Save();}
            hudDirty=true;return changed;
        }
        void ManualAct(int id,bool wholeObject)
        {
            int oldLevel=Model.Level;
            bool changed=wholeObject?Model.WorkScrap(id):Model.Work(id);Tell(Model.LastNotice);
            if(!changed)return;
            sounds.Play(YardSound.Tool);PulseWork(id);
            var scrap=wholeObject?Model.FindScrap(id):null;
            var equipment=wholeObject?null:Model.FindEquipment(id);
            bool complete=wholeObject? scrap==null||scrap.strokes>=scrap.requiredStrokes:
                equipment==null||equipment.job==null||equipment.job.ready;
            if(complete){SyncViews();NoticeLevel(oldLevel);RefreshCareer();Save();}
            else
            {
                if(wholeObject&&views.TryGetValue(id,out EntityView view)&&view.root!=null)
                    CompactWorkVisuals.ApplyScrapProgress(view.root,scrap,Model.Rules);
                workCheckpoint.Queue(Time.unscaledTime);
            }
            hudDirty=true;
        }
        void UpdateWorkCheckpoint(){if(workCheckpoint.Due(Time.unscaledTime))Save();}
        void SavePendingWork(){if(workCheckpoint.Pending)Save();}
        void NoticeLevel(int oldLevel)
        {
            if(Model.Level<=oldLevel)return;
            int unlocked=0;
            foreach(var d in Model.Rules.equipment)if(d.unlockLevel>oldLevel&&d.unlockLevel<=Model.Level)
                if(d.available)unlocked++;
            Tell("Level "+Model.Level+" reached! Material sale bonus +"+Model.SaleBonusPercent+"%."+
                (unlocked>0?" "+unlocked+" equipment types unlocked to buy. ["+controls.Label(ControlAction.BuildToggle)+"] Catalogue.":""));
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
        {using(SaveMarker.Auto())return SaveNow();}
        bool SaveNow()
        {
            if(Model==null||saveBlocked||(!sessionStarted&&!hasSave))return false;
            CapturePlayer();
            try{CompactSaveStore.Write(SavePath,Model.State,Model.Rules);workCheckpoint.RecordAttempt(true);hasSave=true;lastSaveAt=Time.unscaledTime;saveFailed=false;saveStatus="Saved just now";return true;}
            catch(Exception ex){workCheckpoint.RecordAttempt(false);saveFailed=true;saveStatus="Save failed / retry from Pause";Tell("SAVE FAILED: "+ex.Message);Debug.LogWarning(ex);return false;}
        }
        void OnApplicationFocus(bool focused)
        {
            if(focused||Model==null)return;
            bool welcoming=page==Page.Welcome;
            if(IsBuilding)CancelBuild();settings.Close();
            pageHistory.Clear();selectedId=0;menuScroll=Vector2.zero;confirmNew=false;confirmRemove=false;ResetRequestReview();ResetIndustryReview();
            if(controls!=null)controls.FlushPending();if(presentation!=null)presentation.FlushPending();
            page=welcoming?Page.Welcome:sessionStarted?Page.Pause:Page.Title;Pause(true);Save();
        }
        void OnApplicationQuit(){if(controls!=null)controls.FlushPending();if(presentation!=null)presentation.FlushPending();Save();}
        void OnDestroy()
        {
            if(controls!=null)controls.FlushPending();if(presentation!=null)presentation.FlushPending();
            theme.Dispose();if(presentation!=null)presentation.Dispose();if(sounds!=null)sounds.Dispose();
            if(lighting!=null)lighting.Dispose();DestroyGhost();DestroyBeltPreview();Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
    }
}
