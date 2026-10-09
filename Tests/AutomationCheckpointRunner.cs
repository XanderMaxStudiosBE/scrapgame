// Actual runtime methods are extracted at invocation. These are controlled API,
// presentation and writer seams; no Unity engine or JSON result is claimed.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Scrapshift;
using Scrapshift.Compact;
using UnityEngine;

namespace UnityEngine
{
    public static class Debug {public static int warnings;public static void LogWarning(object message){warnings++;}}
}
namespace Scrapshift.Compact
{
    public static class CompactSaveStore
    {
        public static bool fail;
        public static int attempts;
        public static readonly List<CompactYardState> snapshots=new List<CompactYardState>();
        public static readonly List<float> times=new List<float>();
        static object Clone(object value)
        {
            if(value==null)return null;var type=value.GetType();
            if(type.IsValueType||value is string)return value;
            var array=value as Array;
            if(array!=null)
            {
                var copy=Array.CreateInstance(type.GetElementType(),array.Length);
                for(int i=0;i<array.Length;i++)copy.SetValue(Clone(array.GetValue(i)),i);return copy;
            }
            var list=value as IList;
            if(list!=null){var copy=(IList)Activator.CreateInstance(type);foreach(var element in list)copy.Add(Clone(element));return copy;}
            var result=Activator.CreateInstance(type);
            foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public))field.SetValue(result,Clone(field.GetValue(value)));
            return result;
        }
        public static void Write(string path,CompactYardState state,CompactRules rules)
        {
            attempts++;if(fail)throw new IOException("Controlled write failure.");
            ScrappingModel.Validate(state,rules);ConstructionModel.Validate(state,rules);AutomationModel.Validate(state,rules);
            snapshots.Add((CompactYardState)Clone(state));times.Add(Time.unscaledTime);
        }
        public static void Reset(){fail=false;attempts=0;snapshots.Clear();times.Clear();Debug.warnings=0;}
    }
    public static class CompactWorkVisuals {public static void ApplyScrapProgress(GameObject root,LargeScrapJob scrap,CompactRules rules){}}
    public sealed partial class CompactYardGame : MonoBehaviour
    {
        public ScrappingModel Model;public ConstructionModel Construction;public AutomationModel Automation;
        public CompactIndustryModel Industry {get{return Model.Industry;}}
        public FirstPersonController player;
        public bool IsBuilding {get{return false;}}
        enum Page {None,Title,Pause,Welcome,Help,Catalogue,Equipment,LargeScrap,Sales,Delivery,Import,Journal,Contracts,Credits}
        Page page=Page.None;
        struct MenuFrame {public Page page;public int selectedId;public Vector2 scroll;}
        readonly Stack<MenuFrame> pageHistory=new Stack<MenuFrame>();
        int selectedId;Vector2 menuScroll;bool paused,confirmNew,confirmRemove,hudDirty=true,saveFailed,sessionStarted=true,hasSave,saveBlocked=false,refreshingPassiveCareer,completionPresented;
        readonly ProgressCheckpoint workCheckpoint=new ProgressCheckpoint(),passiveCheckpoint=new ProgressCheckpoint();
        PlayerInputSettings controls;PresentationSettings presentation=new PresentationSettings();SettingsMenu settings=new SettingsMenu();YardAudio sounds=new YardAudio();
        string message="",saveStatus="",fpsLabel="";float messageUntil,nextPower,nextCareer,nextViews,nextSave=15,nextStroke,fpsTime,lastSaveAt=-1;int fpsFrames,beltStage=0;
        readonly Dictionary<int,CompactPowerStatus> powerCache=new Dictionary<int,CompactPowerStatus>();
        sealed class EntityView {public GameObject root=null;}
        readonly Dictionary<int,EntityView> views=new Dictionary<int,EntityView>();
        CompactInteractionTarget target=null;
        string SavePath {get{return "controlled-yard-v2.json";}}
        public void Initialize(CompactRules rules)
        {
            Model=new ScrappingModel(rules);Construction=new ConstructionModel(Model.State,rules);Automation=new AutomationModel(Model,Construction);
            controls=new PlayerInputSettings(Path.Combine(Path.GetTempPath(),"scrapshift-checkpoint-"+Guid.NewGuid()+".json"));
            player=new GameObject("Controlled player").AddComponent<FirstPersonController>();player.view=new GameObject("Controlled camera").AddComponent<Camera>();
            Model.HasPower=id=>PowerStatus(id).powered;
        }
        void ResetIndustryReview(){}void ResetRequestReview(){}void SyncViews(){}void NoticeLevel(int oldLevel){}
        bool Save(){return SaveNow();}void CapturePlayer(){}void UpdateTransportViews(){}
        void StepWorkViews(){}void RefreshProcessingViews(){}void StepSound(){}void UpdateBuild(){}void UpdateBeltBuild(){}void Drop(){}
        void UpdateTarget(){}void Interact(){}void Back(){}void CancelBuild(){}void PulseWork(int id){}
        public string Diagnostics {get{return message+saveStatus+fpsLabel+messageUntil+selectedId+menuScroll.x+paused+confirmNew+confirmRemove+hudDirty+saveFailed+sessionStarted+hasSave+saveBlocked+lastSaveAt+beltStage+target+views.Count;}}
    }
    public sealed class CompactInteractionTarget {public CompactTargetKind kind;public int id;}
    public enum CompactTargetKind {Shop,Sales,Delivery,Wire,Equipment,LargeScrap,Item}
}

sealed class AutomationCheckpointRunner
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int groups;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static object Get(CompactYardGame game,string name){return typeof(CompactYardGame).GetField(name,Private).GetValue(game);}
    static void Set(CompactYardGame game,string name,object value){typeof(CompactYardGame).GetField(name,Private).SetValue(game,value);}
    static object Call(CompactYardGame game,string name,params object[] args)
    {try{return typeof(CompactYardGame).GetMethod(name,Private).Invoke(game,args);}catch(TargetInvocationException error){throw error.InnerException;}}
    static ProgressCheckpoint Passive(CompactYardGame game){return (ProgressCheckpoint)Get(game,"passiveCheckpoint");}
    static ProgressCheckpoint Manual(CompactYardGame game){return (ProgressCheckpoint)Get(game,"workCheckpoint");}
    static void Frame(CompactYardGame game,float delta=.016f)
    {Input.Frame();Time.unscaledTime+=delta-.016f;Time.time+=delta-.016f;Time.deltaTime=delta;Time.unscaledDeltaTime=delta;Call(game,"Update");}
    static void Frames(CompactYardGame game,int count){for(int i=0;i<count;i++)Frame(game);}
    static CompactYardGame Fresh()
    {
        CompactSaveStore.Reset();Input.Frame();Input.held.Clear();Input.down.Clear();Time.unscaledTime=0;Time.time=0;Time.timeScale=1;Physics.Reset();
        var game=new GameObject("Actual scheduling/save contract").AddComponent<CompactYardGame>();
        game.Initialize(new CompactRules{startingMoney=1000,startingCars=0,startingRefrigerators=0});return game;
    }
    static EquipmentState Add(CompactYardGame game,EquipmentKind kind,float x,float z)
    {
        var state=new EquipmentState{id=game.Model.State.nextId++,kind=kind,x=x,z=z,paidPrice=game.Model.Rules.Equipment(kind).price};
        game.Model.State.equipment.Add(state);return state;
    }
    static EquipmentState[] Line(CompactYardGame game)
    {
        var source=Add(game,EquipmentKind.Storage,-15,-4);var destination=Add(game,EquipmentKind.Storage,-15,4);
        Check(game.Automation.Connect(source.id,0,destination.id,0,true),game.Automation.LastMessage);
        source.contents.Add(new CompactStack{id=game.Model.State.nextId++,kind=PartKind.Copper,quantity=24,xpEligible=true});
        return new[]{source,destination};
    }
    static int Units(CompactYardState state)
    {int result=0;foreach(var equipment in state.equipment)foreach(var stack in equipment.contents)result+=stack.quantity;foreach(var belt in state.belts)foreach(var item in belt.items)result+=item.quantity;return result;}
    static void Group(string title,Action test){test();groups++;Console.WriteLine("PASS "+title);}
    static void Main()
    {
        Group("continuous automation saves inside a fixed window, never once per allocated item",()=>
        {
            var game=Fresh();Line(game);Frames(game,600);
            Check(CompactSaveStore.attempts==9,"9.6 seconds of continuous transport should write nine fixed checkpoints, got "+CompactSaveStore.attempts);
            Check(CompactSaveStore.times[0]<=1.032f,"ongoing motion must not postpone the first one-second checkpoint");
            Frames(game,650);
            Check(CompactSaveStore.attempts>=16&&CompactSaveStore.attempts<=20,"busy transport spanning the periodic deadline remains bounded");
            for(int i=1;i<CompactSaveStore.times.Count;i++)Check(CompactSaveStore.times[i]-CompactSaveStore.times[i-1]>=1,"background writes must remain at least one second apart");
            Check(Units(game.Model.State)==24&&game.Model.State.experience==0,"checkpoint policy must not alter transport quantity or award XP");
            var saved=CompactSaveStore.snapshots[CompactSaveStore.snapshots.Count-1];
            var resumed=new ScrappingModel(game.Model.Rules,saved);var construction=new ConstructionModel(saved,game.Model.Rules);var automation=new AutomationModel(resumed,construction);
            for(int i=0;i<1000;i++){resumed.Tick(.016f);resumed.Industry.Tick(.016f);automation.Tick(.016f);}
            Check(Units(saved)==24&&saved.belts[0].items.Count==0,"actual reconstructed model must finish transport with conserved saved cargo");
        });
        Group("first background failure stops retries through continuing transport and periodic deadlines",()=>
        {
            var game=Fresh();Line(game);CompactSaveStore.fail=true;Frames(game,2400);
            Check(CompactSaveStore.attempts==1&&Debug.warnings==1,"ongoing simulation and periodic save must not repeatedly attempt or warn after failure");
            Check(Passive(game).Pending&&Passive(game).Failed&&!Passive(game).Due(Time.unscaledTime),"failed snapshot retains pending progress without rearming");
            Check(Units(game.Model.State)==24&&game.Model.State.belts[0].items.Count==0,"failed persistence must leave live transport usable and conserved");
            CompactSaveStore.fail=false;Check((bool)Call(game,"Save"),"explicit retry should succeed");
            Check(CompactSaveStore.attempts==2&&!Passive(game).Pending&&!Manual(game).Pending&&!(bool)Get(game,"saveFailed"),"successful retry clears both checkpoint windows and the failure latch");
            Check(Units(CompactSaveStore.snapshots[0])==24,"retry writes the complete current state");
        });
        Group("timer-only powered work joins passive checkpoints with no identity changes",()=>
        {
            var game=Fresh();var generator=Add(game,EquipmentKind.Generator,0,5);var machine=Add(game,EquipmentKind.Tier1Scrapper,5,5);
            Check(game.Construction.Connect(generator.id,machine.id),game.Construction.LastMessage);Call(game,"RebuildPower");
            Check(game.Model.AcquireWire()&&game.Model.BeginProcessing(machine.id),game.Model.LastNotice);int identity=game.Model.State.nextId;float remaining=machine.job.remaining;
            Frames(game,66);Check(CompactSaveStore.attempts==1&&game.Model.State.nextId==identity&&machine.job.remaining<remaining,"timer progress must persist even when money, XP and IDs stay unchanged");
            Check(CompactSaveStore.snapshots[0].equipment[2].job.remaining<remaining,"timer snapshot contains accepted elapsed work");
        });
        Group("automatic dispatch batches money and XP changes without per-shipment writes",()=>
        {
            var game=Fresh();game.Model.State.experience=180;var generator=Add(game,EquipmentKind.Generator,0,5);var dispatch=Add(game,EquipmentKind.ExportStation,5,5);
            dispatch.contents.Add(new CompactStack{id=game.Model.State.nextId++,kind=PartKind.Copper,quantity=3,xpEligible=true});
            Check(game.Construction.Connect(generator.id,dispatch.id),game.Construction.LastMessage);Call(game,"RebuildPower");
            Check(game.Industry.SetEnabled(dispatch.id,true),game.Industry.LastMessage);dispatch.industry.remaining=0;
            int beforeMoney=game.Model.State.money,beforeXp=game.Model.State.experience;Frame(game);
            Check(game.Model.State.money>beforeMoney&&game.Model.State.experience==beforeXp+6&&CompactSaveStore.attempts==0&&Passive(game).Pending,"automatic sale changes apply immediately but queue their snapshot");
            Frames(game,65);Check(CompactSaveStore.attempts==1&&CompactSaveStore.snapshots[0].money==game.Model.State.money,"one checkpoint contains earned dispatch money and XP");
        });
        Group("explicit purchase and sale clear both pending windows immediately",()=>
        {
            var game=Fresh();Line(game);Frames(game,20);Manual(game).Queue(Time.unscaledTime);
            Check((bool)Call(game,"Act",new Func<bool>(()=>game.Model.AcquireWire()),YardSound.Pickup),"explicit pickup");
            Check(CompactSaveStore.attempts==1&&!Passive(game).Pending&&!Manual(game).Pending,"explicit action writes without waiting for either window");
            Check(game.Model.Drop(10,.25f,-8),"clear hands for sale fixture");
            var held=new CompactStack{id=game.Model.State.nextId++,kind=PartKind.Copper,quantity=3,xpEligible=true};game.Model.State.items.Add(held);game.Model.State.carriedId=held.id;
            Check((bool)Call(game,"Act",new Func<bool>(()=>game.Model.Sell()),YardSound.Pickup)&&CompactSaveStore.attempts==2,"explicit sale commits immediately");
            Check((bool)Call(game,"Act",new Func<bool>(()=>game.Model.BuyScrap(ScrapObjectKind.Car)),YardSound.Pickup)&&CompactSaveStore.attempts==3,"explicit purchase commits immediately");
        });
        Group("pause flushes pending cargo immediately and paused frames advance nothing",()=>
        {
            var game=Fresh();Line(game);Frames(game,20);var belt=game.Model.State.belts[0];float progress=belt.items[0].progress;
            Call(game,"Pause",true);Check(CompactSaveStore.attempts==1&&!Passive(game).Pending&&Time.timeScale==0,"pause is an immediate snapshot boundary");
            Frames(game,120);Check(CompactSaveStore.attempts==1&&belt.items[0].progress==progress,"paused clocks and cargo stay unchanged");
        });
        Group("focus loss and quit preserve pending transport at their explicit boundaries",()=>
        {
            var game=Fresh();Line(game);Frames(game,20);Call(game,"OnApplicationFocus",false);
            Check(CompactSaveStore.attempts==1&&Units(CompactSaveStore.snapshots[0])==24&&!Passive(game).Pending,"focus loss flushes complete cargo once");
            Call(game,"OnApplicationFocus",false);Check(CompactSaveStore.attempts==2,"focus loss while already paused still captures current position/state once");
            game=Fresh();Line(game);Frames(game,20);CompactSaveStore.fail=true;Call(game,"OnApplicationFocus",false);
            Check(CompactSaveStore.attempts==1&&Debug.warnings==1&&Passive(game).Pending&&Units(game.Model.State)==24,"failed focus boundary attempts once and preserves live pending cargo");
            game=Fresh();Call(game,"OnApplicationFocus",false);Check(CompactSaveStore.attempts==1,"focus loss with no pending work still snapshots current position once");
            game=Fresh();Line(game);Frames(game,20);Call(game,"OnApplicationQuit");
            Check(CompactSaveStore.attempts==1&&!Passive(game).Pending&&Units(CompactSaveStore.snapshots[0])==24,"quit immediately saves pending cargo");
        });
        Group("automatic chapter pause flushes once and never retries a failed checkpoint",()=>
        {
            var game=Fresh();Line(game);Frames(game,20);game.Model.Career.Stats.completedGoals=511;game.Model.Career.Stats.contractsCompleted=game.Model.Rules.contracts.Length;game.Model.Career.Stats.contract=null;
            Call(game,"RefreshPassiveCareer");Check(CompactSaveStore.attempts==1&&!Passive(game).Pending&&(bool)Get(game,"paused"),"automatic completion pause must write once after including career changes");
            game=Fresh();Line(game);CompactSaveStore.fail=true;Frames(game,70);game.Model.Career.Stats.completedGoals=511;game.Model.Career.Stats.contractsCompleted=game.Model.Rules.contracts.Length;game.Model.Career.Stats.contract=null;
            Call(game,"RefreshPassiveCareer");Check(CompactSaveStore.attempts==1&&Passive(game).Pending&&(bool)Get(game,"paused"),"automatic completion cannot retry a reported failure");
        });
        Group("new manual work retains its established retry and terminal-save behavior",()=>
        {
            var game=Fresh();Line(game);CompactSaveStore.fail=true;Frames(game,70);CompactSaveStore.fail=false;
            var bench=game.Model.State.equipment[0];Check(game.Model.AcquireWire()&&game.Model.BeginProcessing(bench.id),game.Model.LastNotice);Call(game,"ManualAct",bench.id,false);
            Check(CompactSaveStore.attempts==1&&Manual(game).Pending,"a new stroke queues its own retry instead of writing immediately");
            for(int frame=0;frame<100&&CompactSaveStore.attempts==1;frame++)Frame(game);
            Check(CompactSaveStore.attempts==2&&!Manual(game).Pending&&!Passive(game).Pending&&!(bool)Get(game,"saveFailed"),"new manual work permits one successful retry containing both progress sources");
            while(!bench.job.ready)Call(game,"ManualAct",bench.id,false);
            Check(CompactSaveStore.attempts==3&&CompactSaveStore.snapshots[1].equipment[0].job.ready,"terminal manual output commits immediately and only once");
        });
        Console.WriteLine("Passed "+groups+" actual coordinator checkpoint groups. Unity compilation, native input/physics, JSON, disk latency and frame times remain unverified.");
    }
}
