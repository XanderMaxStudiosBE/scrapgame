// Reproducible source/API checks. Real core, input preferences and build mode;
// coordinator methods copied unchanged by extract-port-interaction.py.
// Unity scene/input/physics/rendering/presentation APIs below are controlled.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Scrapshift;
using Scrapshift.Compact;
using UnityEngine;

namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        public static void Destroy(Object value) { DestroyImmediate(value); }
        public static void DestroyImmediate(Object value)
        {
            if(value==null)return;value.destroyed=true;
            var root=value as GameObject;
            if(root!=null)foreach(var child in root.transform.children.ToArray())DestroyImmediate(child.gameObject);
        }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform {get{return gameObject.transform;}}
        public T GetComponentInParent<T>() where T:Component
        {
            for(var current=transform;current!=null;current=current.parent)
                foreach(var component in current.gameObject.components)if(component is T)return (T)component;
            return null;
        }
    }
    public class MonoBehaviour : Component { }
    public sealed class GameObject : Object
    {
        public string name;public bool activeSelf=true;
        public readonly Transform transform;
        public readonly List<Component> components=new List<Component>();
        public GameObject(string name="") {this.name=name;transform=new Transform(this);}
        public T AddComponent<T>() where T:Component,new()
        {var component=new T{gameObject=this};components.Add(component);return component;}
        public void SetActive(bool active){activeSelf=active;}
    }
    public sealed class Transform
    {
        public readonly GameObject gameObject;
        public Transform parent;
        public readonly List<Transform> children=new List<Transform>();
        public Vector3 localPosition,localScale=new Vector3(1,1,1);
        public float yaw;
        public Transform(GameObject gameObject){this.gameObject=gameObject;}
        public Vector3 position {get{return parent==null?localPosition:parent.TransformPoint(localPosition);}set{localPosition=parent==null?value:parent.InverseTransformPoint(value);}}
        public Vector3 lossyScale {get{return parent==null?localScale:Vector3.Scale(localScale,parent.lossyScale);}}
        public Vector3 up {get{return Vector3.up;}}
        public Vector3 forward {get{return Rotate(new Vector3(0,0,1),yaw);}}
        public void SetParent(Transform next,bool worldPositionStays)
        {var before=position;if(parent!=null)parent.children.Remove(this);parent=next;if(next!=null)next.children.Add(this);if(worldPositionStays)position=before;}
        static Vector3 Rotate(Vector3 point,float degrees)
        {double a=degrees*Math.PI/180;float s=(float)Math.Sin(a),c=(float)Math.Cos(a);return new Vector3(c*point.x+s*point.z,point.y,-s*point.x+c*point.z);}
        public Vector3 TransformPoint(Vector3 point)
        {var local=localPosition+Rotate(Vector3.Scale(point,localScale),yaw);return parent==null?local:parent.TransformPoint(local);}
        public Vector3 InverseTransformPoint(Vector3 point)
        {var local=Rotate((parent==null?point:parent.InverseTransformPoint(point))-localPosition,-yaw);return new Vector3(local.x/localScale.x,local.y/localScale.y,local.z/localScale.z);}
    }
    public struct Vector2
    {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero {get{return new Vector2();}}}
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero {get{return new Vector3();}}
        public static Vector3 up {get{return new Vector3(0,1,0);}}
        public float sqrMagnitude {get{return x*x+y*y+z*z;}}
        public float magnitude {get{return (float)Math.Sqrt(sqrMagnitude);}}
        public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
        public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
        public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}
        public static Vector3 operator /(Vector3 a,float b){return new Vector3(a.x/b,a.y/b,a.z/b);}
        public static Vector3 Scale(Vector3 a,Vector3 b){return new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);}
        public static Vector3 Cross(Vector3 a,Vector3 b){return new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);}
    }
    public struct Quaternion
    {
        public Vector3 forward,up;public float yaw;
        public static Quaternion LookRotation(Vector3 forward,Vector3 up){return new Quaternion{forward=forward,up=up};}
        public static Quaternion Euler(float x,float y,float z){return new Quaternion{yaw=y};}
        public static Vector3 operator *(Quaternion rotation,Vector3 point)
        {double a=rotation.yaw*Math.PI/180;float s=(float)Math.Sin(a),c=(float)Math.Cos(a);return new Vector3(c*point.x+s*point.z,point.y,-s*point.x+c*point.z);}
    }
    public struct Color
    {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public sealed class Material : Object {public Color color;}
    public sealed class LineRenderer : Component
    {
        public bool useWorldSpace,receiveShadows;public float widthMultiplier;public Rendering.ShadowCastingMode shadowCastingMode;
        public int positionCount;public Material sharedMaterial;public Vector3[] points;
        public void SetPositions(Vector3[] values){points=(Vector3[])values.Clone();}
    }
    public sealed class Collider : Component { }
    public struct Ray
    {public Vector3 origin,direction;public Vector3 GetPoint(float distance){return origin+direction*distance;}}
    public struct RaycastHit {public Collider collider;public Vector3 point;}
    public sealed class Camera : Component
    {public Ray ray=new Ray{direction=new Vector3(0,0,1)};public Ray ViewportPointToRay(Vector3 point){return ray;}}
    public enum QueryTriggerInteraction {Ignore}
    public static class Physics
    {
        public static bool hasHit;public static RaycastHit hit;
        public static Func<Vector3,Vector3,Collider[],Quaternion,int> overlap;
        public static readonly List<Vector3> centres=new List<Vector3>();
        public static readonly List<Vector3> extents=new List<Vector3>();
        public static bool Raycast(Ray ray,out RaycastHit result,float distance,int mask,QueryTriggerInteraction triggers)
        {result=hit;return hasHit;}
        public static int OverlapBoxNonAlloc(Vector3 centre,Vector3 halfExtents,Collider[] results,Quaternion rotation,int mask,QueryTriggerInteraction triggers)
        {centres.Add(centre);extents.Add(halfExtents);return overlap==null?0:overlap(centre,halfExtents,results,rotation);}
        public static void Reset(){hasHit=false;hit=new RaycastHit();overlap=null;centres.Clear();extents.Clear();}
    }
    public static class Mathf
    {
        public static float Round(float value){return (float)Math.Round(value);}
        public static float Clamp(float value,float min,float max){return Math.Max(min,Math.Min(max,value));}
        public static float Min(float a,float b){return Math.Min(a,b);}
        public static float Max(float a,float b){return Math.Max(a,b);}
        public static float Abs(float value){return Math.Abs(value);}
    }
    public static class Time
    {public static int frameCount=1;public static float timeScale=1,unscaledTime=1,time=1,deltaTime=.016f,unscaledDeltaTime=.016f;}
    // Covers the exact supported preference codes parsed by actual PlayerInputSettings.
    public enum KeyCode
    {
        Escape,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,
        Alpha0,Alpha1,Alpha2,Alpha3,Alpha4,Alpha5,Alpha6,Alpha7,Alpha8,Alpha9,
        Keypad0,Keypad1,Keypad2,Keypad3,Keypad4,Keypad5,Keypad6,Keypad7,Keypad8,Keypad9,
        F1,F2,F3,F4,F5,F6,F7,F8,F9,F10,F11,F12,Mouse0,Mouse1,Mouse2,Mouse3,Mouse4,Mouse5,Mouse6,
        UpArrow,DownArrow,LeftArrow,RightArrow,Space,Tab,Return,Backspace,Delete,Insert,Home,End,PageUp,PageDown,
        LeftShift,RightShift,LeftControl,RightControl,LeftAlt,RightAlt,BackQuote,Minus,Equals,LeftBracket,RightBracket,
        Backslash,Semicolon,Quote,Comma,Period,Slash,KeypadPeriod,KeypadDivide,KeypadMultiply,KeypadMinus,KeypadPlus,KeypadEnter,KeypadEquals,CapsLock,Numlock,ScrollLock,Pause,Print
    }
    public static class Input
    {
        public static readonly HashSet<KeyCode> held=new HashSet<KeyCode>(),down=new HashSet<KeyCode>();
        public static bool GetKey(KeyCode key){return held.Contains(key);}
        public static bool GetKeyDown(KeyCode key){return down.Contains(key);}
        public static void Frame(params KeyCode[] keys)
        {Time.frameCount++;Time.unscaledTime+=.016f;Time.time+=.016f;held.Clear();down.Clear();foreach(var key in keys){held.Add(key);down.Add(key);}}
    }
    public enum CursorLockMode {None,Locked}
    public static class Cursor {public static CursorLockMode lockState;public static bool visible;}
    public static class Application {public static bool isPlaying;public static string persistentDataPath=Path.GetTempPath();}
    public static class JsonUtility
    {
        public static T FromJson<T>(string json){throw new NotSupportedException("Unity JSON is outside this runner.");}
        public static string ToJson(object value,bool pretty){throw new NotSupportedException("Unity JSON is outside this runner.");}
    }
}
namespace UnityEngine.Rendering {public enum ShadowCastingMode {Off}}
namespace Scrapshift
{
    public sealed class FirstPersonController : MonoBehaviour
    {public Camera view;public int steps;public void Step(){steps++;}}
    public sealed class PresentationSettings {public void UpdatePending(float now){}}
    public sealed class SettingsMenu
    {public bool IsOpen;public void HandleEscape(){IsOpen=false;}public void UpdateCapture(){}}
    public enum YardSound {Pickup,Tool}
    public sealed class YardAudio {public void Pause(bool paused){}public void ApplyVolumes(){}public void Play(YardSound sound){}}
    public static class YardGeometry
    {
        public static readonly Color Rust=new Color(.7f,.2f,.1f);
        static readonly Dictionary<string,Material> palette=new Dictionary<string,Material>();
        public static Material PaletteMaterial(Color color)
        {string key=color.r+"/"+color.g+"/"+color.b;if(!palette.ContainsKey(key))palette.Add(key,new Material{color=color});return palette[key];}
    }
}
namespace Scrapshift.Compact
{
    public static partial class CompactAutomationVisuals {public const float ItemHeight=.7f;}
    // State shell and presentation seams only; interaction/construction branches
    // come from the freshly extracted production methods, never copied here.
    public sealed partial class CompactYardGame : MonoBehaviour
    {
        public ScrappingModel Model;public ConstructionModel Construction;public AutomationModel Automation;
        public CompactIndustryModel Industry {get{return Model.Industry;}}
        public FirstPersonController player;
        public bool IsBuilding {get{return build.Active||beltStage>0;}}
        enum Page {None,Title,Pause,Welcome,Help,Catalogue,Equipment,LargeScrap,Sales,Delivery,Import,Journal,Contracts,Credits}
        Page page=Page.None;
        struct MenuFrame {public Page page;public int selectedId;public Vector2 scroll;}
        readonly Stack<MenuFrame> pageHistory=new Stack<MenuFrame>();
        readonly Dictionary<int,CompactPowerStatus> powerCache=new Dictionary<int,CompactPowerStatus>();
        int selectedId;Vector2 menuScroll;bool paused,confirmNew,confirmRemove,hudDirty=true,previewClear;bool saveFailed=false,sessionStarted=true;
        readonly ProgressCheckpoint workCheckpoint=new ProgressCheckpoint(),passiveCheckpoint=new ProgressCheckpoint();bool refreshingPassiveCareer;
        PlayerInputSettings controls;PresentationSettings presentation=new PresentationSettings();SettingsMenu settings=new SettingsMenu();YardAudio sounds=new YardAudio();
        CompactBuildMode build;CompactInteractionTarget target;CompactConveyorPortTarget aimedPort;
        string message="",buildReason="",fpsLabel="";float messageUntil,nextPower,nextCareer,nextViews,nextSave=10000,nextStroke,fpsTime;int fpsFrames;
        int beltStage,beltFrom,beltFromPort;bool beltXFirst=true;
        LineRenderer beltPreview,beltSourcePreview,beltDestinationPreview;GameObject beltPreviewRoot;
        readonly List<LineRenderer> beltDirectionPreviews=new List<LineRenderer>(),beltCorridorPreviews=new List<LineRenderer>();
        readonly Collider[] placementHits=new Collider[64];
        GameObject cables;string cableKey="";
        public readonly List<string> menuText=new List<string>();
        void Text(string value){menuText.Add(value);}bool Button(string value,bool enabled=true){return false;}
        public void Initialize(CompactRules rules)
        {
            Model=new ScrappingModel(rules);Construction=new ConstructionModel(Model.State,rules);Automation=new AutomationModel(Model,Construction);
            build=new CompactBuildMode(Construction);controls=new PlayerInputSettings(Path.Combine(Path.GetTempPath(),"scrapshift-port-check-"+Guid.NewGuid()+".json"));
            player=new GameObject("Controlled player").AddComponent<FirstPersonController>();player.view=new GameObject("Controlled camera").AddComponent<Camera>();
        }
        void ResetIndustryReview(){}void ResetRequestReview(){}void SyncViews(){}void NoticeLevel(int oldLevel){}
        bool RefreshCareer(){return false;}bool Save(){return true;}void RebuildPower(){powerCache.Clear();}void UpdateTransportViews(){}
        void StepWorkViews(){}void RefreshProcessingViews(){}void StepSound(){}void UpdateBuild(){}void Drop(){}
        void ManualAct(int id,bool wholeObject){}void DestroyGhost(){}
        // Read state to keep diagnostics available without suppressing compiler warnings.
        public string Diagnostics {get{return message+buildReason+fpsLabel+messageUntil+selectedId+menuScroll.x+paused+confirmNew+confirmRemove+hudDirty+previewClear+saveFailed;}}
    }
}

sealed class PortInteractionRunner
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int groups;
    static void Check(bool value,string description){if(!value)throw new Exception(description);}
    static bool Near(float a,float b){return Math.Abs(a-b)<.0001f;}
    static bool Near(Vector3 a,Vector3 b){return Near(a.x,b.x)&&Near(a.y,b.y)&&Near(a.z,b.z);}
    static object Get(CompactYardGame game,string field){return typeof(CompactYardGame).GetField(field,Private).GetValue(game);}
    static void Set(CompactYardGame game,string field,object value){typeof(CompactYardGame).GetField(field,Private).SetValue(game,value);}
    static object Call(CompactYardGame game,string method,params object[] args)
    {try{return typeof(CompactYardGame).GetMethod(method,Private).Invoke(game,args);}catch(TargetInvocationException error){throw error.InnerException;}}
    static PlayerInputSettings Controls(CompactYardGame game){return (PlayerInputSettings)Get(game,"controls");}
    static CompactYardGame Fresh()
    {
        Input.Frame();Physics.Reset();Time.timeScale=1;Application.isPlaying=false;
        var rules=new CompactRules{startingMoney=1000,startingCars=0,startingRefrigerators=0};
        var game=new GameObject("Actual coordinator methods").AddComponent<CompactYardGame>();game.Initialize(rules);return game;
    }
    static EquipmentState Add(CompactYardGame game,EquipmentKind kind,float x,float z,float yaw=0)
    {var gear=new EquipmentState{id=game.Model.State.nextId++,kind=kind,x=x,z=z,yaw=yaw,paidPrice=game.Model.Rules.Equipment(kind).price};game.Model.State.equipment.Add(gear);return gear;}
    static Collider Aim(CompactYardGame game,EquipmentState gear,bool? output,int index=0)
    {
        var owner=new GameObject("Controlled equipment target");var target=owner.AddComponent<CompactInteractionTarget>();target.id=gear.id;target.kind=CompactTargetKind.Equipment;
        CompactConveyorPortTarget mouth=null;
        if(output.HasValue){mouth=owner.AddComponent<CompactConveyorPortTarget>();mouth.output=output.Value;mouth.index=index;}
        var collider=owner.AddComponent<Collider>();Set(game,"target",target);Set(game,"aimedPort",mouth);
        var point=output.HasValue?AutomationModel.Port(gear,game.Model.Rules,output.Value,index):new CompactPortPoint(gear.x,gear.z);
        Physics.hasHit=true;Physics.hit=new RaycastHit{collider=collider,point=game.transform.TransformPoint(new Vector3(point.x,.7f,point.z))};return collider;
    }
    static void Ready(CompactYardGame game)
    {Input.Frame();Controls(game).GameplayReady.ToString();Input.Frame();Check(Controls(game).GameplayReady,"release gate should reopen on the subsequent frame");}
    static string Snapshot(CompactYardGame game)
    {
        var state=game.Model.State;var text=new System.Text.StringBuilder();text.Append(state.money).Append('/').Append(state.experience).Append('/').Append(state.nextId).Append('/').Append(state.carriedId);
        foreach(var item in state.items)text.Append(" item ").Append(item.id).Append('/').Append(item.kind).Append('/').Append(item.quantity).Append('/').Append(item.xpEligible);
        foreach(var gear in state.equipment)
        {
            text.Append(" gear ").Append(gear.id).Append('/').Append(gear.x).Append('/').Append(gear.z).Append('/').Append(gear.yaw);
            foreach(var item in gear.contents)text.Append(" queue ").Append(item.id).Append('/').Append(item.kind).Append('/').Append(item.quantity);
            if(gear.job!=null){text.Append(" job ").Append(gear.job.recipeId).Append('/').Append(gear.job.strokes).Append('/').Append(gear.job.remaining);foreach(var yield in gear.job.yields)text.Append('/').Append(yield.kind).Append('/').Append(yield.quantity);}
        }
        foreach(var belt in state.belts){text.Append(" belt ").Append(belt.id).Append('/').Append(belt.paidPrice);foreach(var item in belt.items)text.Append('/').Append(item.id).Append('/').Append(item.kind).Append('/').Append(item.quantity).Append('/').Append(item.progress);}
        return text.ToString();
    }
    static void Group(string description,Action action){action();groups++;Console.WriteLine("PASS "+description);}
    static void Main()
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        Group("empty-hand OUT starts stage 2 with actual source index; body opens paused management",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];Aim(game,bench,true);string before=Snapshot(game);Call(game,"Interact");
            Check((int)Get(game,"beltStage")==2&&(int)Get(game,"beltFrom")==bench.id&&(int)Get(game,"beltFromPort")==0,"OUT should enter destination selection directly");
            Check(!(bool)Get(game,"paused")&&!Controls(game).GameplayReady,"begin should resume and suppress activation input");Check(Snapshot(game)==before,"selection must not spend or mutate inventory");
            Call(game,"CancelBuild");Aim(game,bench,null);Call(game,"Interact");Check(Get(game,"page").ToString()=="Equipment"&&(bool)Get(game,"paused"),"body should open equipment management");
        });
        Group("occupied OUT rejects a direct start without changing paid belt or cargo",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];var storage=Add(game,EquipmentKind.Storage,bench.x,bench.z+8);
            Check(game.Automation.Connect(bench.id,0,storage.id,0,true),game.Automation.LastMessage);var belt=game.Model.State.belts[0];belt.items.Add(new ConveyorItem{id=game.Model.State.nextId++,kind=PartKind.Copper,progress=.4f});
            Aim(game,bench,true);string before=Snapshot(game);Call(game,"Interact");Check((int)Get(game,"beltStage")==0&&Snapshot(game)==before,"occupied OUT must preserve cargo and purchase");
            string hint=(string)Call(game,"Hint");
            Check(hint.Contains("OUT 1")&&hint.Contains("IN 1")&&hint.Contains(game.Automation.FlowStatus(belt.id)),"occupied hint should show the owned route and its actual flow");
        });
        Group("carried OUT rejects intake; busy IN queues the same bundle and preserves paid job",()=>
        {
            var game=Fresh();var machine=Add(game,EquipmentKind.Tier1Scrapper,5,5);Check(game.Model.AcquireWire(),"wire acquisition");Check(game.Model.BeginProcessing(machine.id),game.Model.LastNotice);
            var job=machine.job;var carried=new CompactStack{id=game.Model.State.nextId++,kind=PartKind.Motor,quantity=1,xpEligible=true};game.Model.State.items.Add(carried);game.Model.State.carriedId=carried.id;
            Aim(game,machine,true);string before=Snapshot(game);Call(game,"Interact");Check(Snapshot(game)==before&&ReferenceEquals(game.Model.Carried,carried)&&ReferenceEquals(machine.job,job),"wrong mouth must keep paid job and held identity");
            Check(((string)Call(game,"Hint")).Contains("amber IN"),"carried OUT hint");Aim(game,machine,false);int xp=game.Model.State.experience;Call(game,"Interact");
            Check(game.Model.Carried==null&&ReferenceEquals(machine.contents[0],carried)&&ReferenceEquals(machine.job,job)&&game.Model.State.experience==xp,"busy IN must queue without replacing outputs or awarding XP");
        });
        Group("live machine hints show independent full IN, reserved OUT and recipe filter without changing work",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];Check(game.Model.AcquireWire(),"wire acquisition");Check(game.Model.BeginProcessing(bench.id),game.Model.LastNotice);
            int capacity=game.Model.Rules.Equipment(bench.kind).outputCapacity;
            bench.contents.Add(new CompactStack{id=game.Model.State.nextId++,kind=PartKind.Wire,quantity=capacity,xpEligible=true});bench.filterKind=(int)PartKind.Wire;
            Aim(game,bench,null);string before=Snapshot(game);string hint=(string)Call(game,"Hint");
            Check(hint.Contains("IN "+capacity+" / "+capacity+" (full)")&&hint.Contains("OUT "+game.Model.OutputQuantity(bench.id))&&hint.Contains("(reserved)"),"separate bays must not imply a full IN blocks paid OUT");
            Check(hint.Contains("IN recipe / "+game.Model.Rules.Part(PartKind.Wire).name)&&hint.Contains("["+Controls(game).Label(ControlAction.ManualWork)+"]"),"filter and actual manual control should be visible");
            Check(Snapshot(game)==before&&bench.job.strokes==0,"looking must not stroke or move inventory");
            bench.job=null;bench.filterKind=(int)PartKind.Motor;hint=(string)Call(game,"Hint");
            Check(hint.Contains(game.Model.ProcessingBlockReason(bench.id)),"idle queued machine must explain the real reservation/filter blocker");
        });
        Group("connected mouth hints name endpoints and show blocked head even with empty source and changed build gates",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];var storage=Add(game,EquipmentKind.Storage,bench.x,bench.z+8);
            Check(game.Automation.Connect(bench.id,0,storage.id,0,true),game.Automation.LastMessage);var belt=game.Model.State.belts[0];
            belt.items.Add(new ConveyorItem{id=game.Model.State.nextId++,kind=PartKind.Copper,progress=1});
            int capacity=game.Model.Rules.Equipment(storage.kind).outputCapacity;storage.contents.Add(new CompactStack{id=game.Model.State.nextId++,kind=PartKind.Copper,quantity=capacity});
            game.Model.Rules.Equipment(EquipmentKind.Conveyor).available=false;Aim(game,bench,true);string before=Snapshot(game),message=game.Automation.LastMessage;
            for(int i=0;i<20;i++)
            {
                string hint=(string)Call(game,"Hint");
                Check(hint.Contains(game.Model.Rules.Equipment(bench.kind).name+" #"+bench.id+" OUT 1")&&hint.Contains(game.Model.Rules.Equipment(storage.kind).name+" #"+storage.id+" IN 1"),"route endpoints should use actual names, IDs and ports");
                Check(hint.Contains(game.Automation.FlowStatus(belt.id))&&hint.Contains("IN full")&&!hint.Contains("unlock"),"owned belt status must remain visible independent of future purchase gates");
            }
            Check(Snapshot(game)==before&&game.Automation.LastMessage==message,"hover diagnostics must preserve transport and latest transaction notice");
            Aim(game,storage,false);Controls(game).Preferences.SetBinding(ControlAction.Interact,"F",false);
            Check(((string)Call(game,"Hint")).Contains("[F] Manage input"),"connected IN must use rebound action label");
            Check(game.Model.AcquireWire(),"wire acquisition");string carriedHint=(string)Call(game,"Hint");
            Check(carriedHint=="Buffer is full; reserved outputs also occupy capacity. Your carried item is unchanged.","carried intake refusal must take priority over passive route hint");
        });
        Group("equipment route cards separate live stopped cargo from loaded-belt dismantling eligibility",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];var storage=Add(game,EquipmentKind.Storage,bench.x,bench.z+8);
            Check(game.Automation.Connect(bench.id,0,storage.id,0,true),game.Automation.LastMessage);var belt=game.Model.State.belts[0];
            belt.items.Add(new ConveyorItem{id=game.Model.State.nextId++,kind=PartKind.Copper,progress=1});storage.contents.Add(new CompactStack{id=game.Model.State.nextId++,kind=PartKind.Steel,quantity=game.Model.Rules.Equipment(storage.kind).outputCapacity});
            string before=Snapshot(game);Call(game,"DrawAutomation",bench);string text=string.Join("\n",game.menuText.ToArray());
            Check(text.Contains("Stopped at IN / IN full")&&text.Contains("OUT 1")&&text.Contains("IN 1")&&!text.Contains("moving items"),"loaded stopped cargo must be identified as backpressure in route card");
            Check(text.Contains("empty it before dismantling")&&Snapshot(game)==before,"the distinct dismantling guard and inventory must remain intact");
        });
        Group("oversized idle IN uses actual queue fallback without auto-stroking a workbench",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];Check(game.Model.AcquireWire(),"wire acquisition");game.Model.Carried.quantity=10;Aim(game,bench,false);Call(game,"Interact");
            Check(game.Model.Carried==null&&bench.job==null&&game.Model.StoredUnits(bench.id)==10,"oversized batch should queue");game.Model.Tick(.1f);Check(bench.job!=null&&bench.job.inputQuantity==1&&bench.job.strokes==0,"manual queue should prepare one batch without performing strokes");
        });
        Group("actual rebound label invalidates the hint cache and drives physical-mouth input",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];Aim(game,bench,true);Check(((string)Call(game,"Hint")).Contains("[E]"),"default label");
            Controls(game).Preferences.SetBinding(ControlAction.Interact,"F",false);Check(((string)Call(game,"Hint")).Contains("[F] Start conveyor"),"hint must follow binding");
            Input.Frame(KeyCode.E);Call(game,"Update");Check((int)Get(game,"beltStage")==0,"old key must not start belt");Input.Frame(KeyCode.F);Call(game,"Update");Check((int)Get(game,"beltStage")==2,"new key must start belt through actual Update");
        });
        Group("configured conveyor level/availability and carried-item gates reject with no mutation",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];Aim(game,bench,true);var definition=game.Model.Rules.Equipment(EquipmentKind.Conveyor);definition.unlockLevel=5;string before=Snapshot(game);Call(game,"Interact");
            Check((int)Get(game,"beltStage")==0&&Snapshot(game)==before&&((string)Call(game,"Hint")).Contains("level 5"),"level gate");definition.unlockLevel=1;definition.available=false;Call(game,"Interact");Check((int)Get(game,"beltStage")==0&&Snapshot(game)==before,"availability gate");
            definition.available=true;Check(game.Model.AcquireWire(),"wire acquisition");before=Snapshot(game);Call(game,"BeginBelt",bench.id,0);Check((int)Get(game,"beltStage")==0&&Snapshot(game)==before,"menu/direct begin must reject carried item");
        });
        Group("catalogue start selects only OUT; indexed junction mouth selects the actual output",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];Call(game,"BeginBelt",0,0);Ready(game);Aim(game,bench,false);Input.Frame(KeyCode.E);Call(game,"UpdateBeltBuild");
            Check((int)Get(game,"beltStage")==1&&!(bool)Get(game,"previewClear")&&((string)Get(game,"buildReason")).Contains("IN mouth"),"catalogue start must reject IN as source");
            Ready(game);Aim(game,bench,true);Input.Frame(KeyCode.E);Call(game,"UpdateBeltBuild");Check((int)Get(game,"beltStage")==2&&(int)Get(game,"beltFrom")==bench.id&&!Controls(game).GameplayReady,"physical OUT should advance source-selection stage");Call(game,"CancelBuild");
            var splitter=Add(game,EquipmentKind.Splitter,7,-4);game.Model.State.experience=45;Aim(game,splitter,true,2);string before=Snapshot(game);Call(game,"Interact");
            Check((int)Get(game,"beltStage")==2&&(int)Get(game,"beltFrom")==splitter.id&&(int)Get(game,"beltFromPort")==2&&Snapshot(game)==before,"third output must retain actual source index");Check(((string)Get(game,"message")).Contains("OUT 3"),"source feedback should name selected mouth");
        });
        Group("pause, Settings and input-release gates block coordinator simulation and interaction",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];Check(game.Model.AcquireWire(),"wire acquisition");Check(game.Model.BeginProcessing(bench.id),"bench batch");Aim(game,bench,true);string before=Snapshot(game);
            Call(game,"Pause",true);Input.Frame(KeyCode.E);Call(game,"Update");Check((int)Get(game,"beltStage")==0&&Snapshot(game)==before&&Time.timeScale==0,"paused update");Call(game,"Pause",false);Call(game,"Update");Check((int)Get(game,"beltStage")==0&&Snapshot(game)==before,"held activation suppressed after resume");
            Ready(game);((SettingsMenu)Get(game,"settings")).IsOpen=true;Input.Frame(KeyCode.E);Call(game,"Update");Check((int)Get(game,"beltStage")==0&&Snapshot(game)==before,"Settings capture guard");
        });
        PreviewChecks();
        Group("actual power sockets and cable cache follow editable dimensions and rotated equipment",()=>
        {
            var game=Fresh();var generator=Add(game,EquipmentKind.Generator,0,5,270);var machine=Add(game,EquipmentKind.Tier1Scrapper,5,5,90);
            Check(game.Construction.Connect(generator.id,machine.id),game.Construction.LastMessage);Call(game,"SyncCables");var original=(GameObject)Get(game,"cables");
            var initial=(LineRenderer)original.transform.children[0].gameObject.components[0];Check(Near(initial.points[0],new Vector3(.55f,.75f,5.45f))&&Near(initial.points[3],new Vector3(4.71f,.9f,5.9f)),"rotated original cable endpoints should match actual physical sockets");
            string before=Snapshot(game);Call(game,"SyncCables");Check(ReferenceEquals(original,Get(game,"cables")),"unchanged cable endpoints should reuse owned root");
            game.Model.Rules.Equipment(EquipmentKind.Generator).width*=.8f;game.Model.Rules.Equipment(EquipmentKind.Tier1Scrapper).depth*=1.2f;Call(game,"SyncCables");var refreshed=(GameObject)Get(game,"cables");
            var cable=(LineRenderer)refreshed.transform.children[0].gameObject.components[0];Check(!ReferenceEquals(original,refreshed)&&original.destroyed&&Snapshot(game)==before,"dimension edit should refresh owned cables without changing yard economy/work");
            Check(Near(cable.points[0],new Vector3(.55f,.75f,5.36f))&&Near(cable.points[3],new Vector3(4.652f,.9f,5.9f))&&!cable.useWorldSpace&&ReferenceEquals(refreshed.transform.parent,game.transform),"dimension-scaled sockets and local cable hierarchy");
            foreach(var kind in new[]{EquipmentKind.Tier2Scrapper,EquipmentKind.PrimaryScrapper,EquipmentKind.ExportStation})
            {
                var equipment=Add(game,kind,10,5,90);var definition=game.Model.Rules.Equipment(kind);Vector3 beforeSocket=(Vector3)Call(game,"PowerPort",equipment);definition.width*=.8f;definition.depth*=1.2f;Vector3 afterSocket=(Vector3)Call(game,"PowerPort",equipment);
                Check(!Near(beforeSocket,afterSocket)&&Near(beforeSocket.y,afterSocket.y),"selected "+kind+" helper must scale its physical socket at quarter-turn orientation");
            }
        });
        Console.WriteLine("Passed "+groups+" production port/coordinator API check groups. Unity engine compilation, real input, raycasts/collision, rendering and builds remain unverified.");
    }
    static void PreviewChecks()
    {
        Group("snapped route previews actual full price, arrows and clearance, then commits once",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];var storage=Add(game,EquipmentKind.Storage,bench.x,bench.z+8);Aim(game,bench,true);Call(game,"Interact");Ready(game);Aim(game,storage,false);string before=Snapshot(game);Call(game,"UpdateBeltBuild");
            var candidate=new ConveyorLink{fromId=bench.id,toId=storage.id,bendXFirst=true};int cost=(int)Math.Ceiling(AutomationModel.Length(AutomationModel.Path(candidate,game.Model.State,game.Model.Rules))/2)*game.Model.Rules.Equipment(EquipmentKind.Conveyor).price;
            Check((bool)Get(game,"previewClear")&&((string)Get(game,"buildReason")).Contains("€"+cost),"full path quote should be valid and match actual cost");Check(Snapshot(game)==before,"preview must be read-only");
            var arrows=(List<LineRenderer>)Get(game,"beltDirectionPreviews");var corridors=(List<LineRenderer>)Get(game,"beltCorridorPreviews");Check(arrows.Count>0&&arrows.Count==corridors.Count,"route needs direction arrows and clearance corridors");
            var firstSegment=((LineRenderer)Get(game,"beltPreview")).points;var travel=firstSegment[1]-firstSegment[0];var arrow=arrows[0].points;var tip=arrow[1]-(arrow[0]+arrow[2])*.5f;
            Check(tip.x*travel.x+tip.z*travel.z>0,"arrow tip must point from OUT toward IN");Check(Near((corridors[0].points[0]-corridors[0].points[4]).magnitude,0)&&Near((corridors[0].points[2]-corridors[0].points[1]).magnitude,AutomationModel.CorridorWidth),"clearance line must close and show actual corridor width");
            int oldMoney=game.Model.State.money,oldNext=game.Model.State.nextId;Input.Frame(KeyCode.E);Call(game,"UpdateBeltBuild");Check(game.Model.State.belts.Count==1&&game.Model.State.money==oldMoney-cost&&game.Model.State.nextId==oldNext+1&&(int)Get(game,"beltStage")==0,"confirm should pay full quote and reserve one identity");
            Check(!Controls(game).GameplayReady&&Get(game,"beltPreviewRoot")==null,"commit should release preview and suppress held confirmation");
        });
        Group("no-hit and wrong-role frames clear stale validity; unaffordable route cannot commit",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];var storage=Add(game,EquipmentKind.Storage,bench.x,bench.z+8);Aim(game,bench,true);Call(game,"Interact");Ready(game);Aim(game,storage,false);Call(game,"UpdateBeltBuild");Check((bool)Get(game,"previewClear"),"initial clear route");
            Physics.hasHit=false;Input.Frame(KeyCode.E);Call(game,"UpdateBeltBuild");Check(!(bool)Get(game,"previewClear")&&game.Model.State.belts.Count==0,"no-hit must not keep valid route");
            Ready(game);Aim(game,storage,true);Call(game,"UpdateBeltBuild");Check(!(bool)Get(game,"previewClear")&&((string)Get(game,"buildReason")).Contains("OUT mouth"),"wrong role must not snap destination");
            Aim(game,storage,false);game.Model.State.money=0;string before=Snapshot(game);Input.Frame(KeyCode.E);Call(game,"UpdateBeltBuild");Check(!(bool)Get(game,"previewClear")&&Snapshot(game)==before&&((string)Get(game,"buildReason")).Contains("Not enough money"),"affordability is part of preview and confirmation");
        });
        Group("cancellation destroys only owned preview hierarchy and preserves cash, IDs, XP and cargo",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];var storage=Add(game,EquipmentKind.Storage,bench.x,bench.z+8);
            var existingSource=Add(game,EquipmentKind.Storage,-15,-4);var existingDestination=Add(game,EquipmentKind.Storage,-15,4);Check(game.Automation.Connect(existingSource.id,0,existingDestination.id,0,true),game.Automation.LastMessage);
            game.Model.State.belts[0].items.Add(new ConveyorItem{id=game.Model.State.nextId++,kind=PartKind.Copper,progress=.4f,xpEligible=true});
            Check(game.Model.AcquireWire(),"wire acquisition");Check(game.Model.BeginProcessing(bench.id),"paid manual batch");game.Model.State.experience=23;bench.contents.Add(new CompactStack{id=game.Model.State.nextId++,kind=PartKind.Wire,quantity=2,xpEligible=true});
            Aim(game,bench,true);Call(game,"Interact");Ready(game);Aim(game,storage,false);Call(game,"UpdateBeltBuild");var root=(GameObject)Get(game,"beltPreviewRoot");var preview=(LineRenderer)Get(game,"beltPreview");var material=preview.sharedMaterial;string before=Snapshot(game);
            Call(game,"CancelBuild");Check(Snapshot(game)==before&&!game.IsBuilding,"cancel preserves all state");Check(root.destroyed&&preview.gameObject.destroyed&&!material.destroyed,"destroy owned root and children; preserve shared palette material");
            Check(Get(game,"beltPreviewRoot")==null&&((List<LineRenderer>)Get(game,"beltDirectionPreviews")).Count==0&&((List<LineRenderer>)Get(game,"beltCorridorPreviews")).Count==0&&!Controls(game).GameplayReady,"cancel clears preview references and input gate");
        });
        Group("transformed yard ray hits are interpreted locally; preview and overlap follow yard transform",()=>
        {
            var game=Fresh();game.transform.localPosition=new Vector3(9,2,-7);game.transform.yaw=90;game.transform.localScale=new Vector3(2,1.5f,2);
            var bench=game.Model.State.equipment[0];var storage=Add(game,EquipmentKind.Storage,bench.x,bench.z+8);Aim(game,bench,true);Call(game,"Interact");Ready(game);
            var collider=Aim(game,storage,null);var socket=AutomationModel.Port(storage,game.Model.Rules,false,0);Physics.hit=new RaycastHit{collider=collider,point=game.transform.TransformPoint(new Vector3(socket.x,.7f,socket.z))};Call(game,"UpdateBeltBuild");
            Check((bool)Get(game,"previewClear"),"body fallback near transformed actual IN should snap");var preview=(LineRenderer)Get(game,"beltPreview");Check(!preview.useWorldSpace&&ReferenceEquals(preview.transform.parent,((GameObject)Get(game,"beltPreviewRoot")).transform),"line should stay yard-local under owned root");
            var path=AutomationModel.Path(new ConveyorLink{fromId=bench.id,toId=storage.id,bendXFirst=true},game.Model.State,game.Model.Rules);
            Check(Near(preview.points[0].x,path[0].x)&&Near(preview.points[0].z,path[0].z),"preview points should match core local route");
            var a=game.transform.TransformPoint(new Vector3(path[0].x,CompactAutomationVisuals.ItemHeight,path[0].z));var b=game.transform.TransformPoint(new Vector3(path[1].x,CompactAutomationVisuals.ItemHeight,path[1].z));
            Check(Physics.centres.Count>0&&Near(Physics.centres[0],(a+b)*.5f)&&Near(Physics.extents[0].x,AutomationModel.CorridorWidth),"world overlap should use transformed route and scaled width");
        });
        Group("obstacles, overlap saturation and newly occupied sources reject without spending",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];var storage=Add(game,EquipmentKind.Storage,bench.x,bench.z+8);Aim(game,bench,true);Call(game,"Interact");Ready(game);Aim(game,storage,false);
            var obstacle=new GameObject("Controlled blocking scenery").AddComponent<Collider>();Physics.overlap=(centre,size,results,rotation)=>{results[0]=obstacle;return 1;};string before=Snapshot(game);Input.Frame(KeyCode.E);Call(game,"UpdateBeltBuild");
            Check(!(bool)Get(game,"previewClear")&&Snapshot(game)==before,"fixed obstacle rejects");Ready(game);Physics.overlap=(centre,size,results,rotation)=>results.Length;Call(game,"UpdateBeltBuild");Check(!(bool)Get(game,"previewClear")&&Snapshot(game)==before,"saturated overlap conservatively rejects");
            Physics.overlap=null;Check(game.Automation.Connect(bench.id,0,storage.id,0,true),game.Automation.LastMessage);before=Snapshot(game);Ready(game);Call(game,"UpdateBeltBuild");Check((int)Get(game,"beltStage")==0&&Snapshot(game)==before,"occupied source after selection cancels without another transaction");
        });
        Group("confirm rechecks native-world clearance after a clear preview before spending",()=>
        {
            var game=Fresh();var bench=game.Model.State.equipment[0];var storage=Add(game,EquipmentKind.Storage,bench.x,bench.z+8);Aim(game,bench,true);Call(game,"Interact");Ready(game);Aim(game,storage,false);
            var path=AutomationModel.Path(new ConveyorLink{fromId=bench.id,toId=storage.id,bendXFirst=true},game.Model.State,game.Model.Rules);int segments=0;
            for(int i=1;i<path.Length;i++)if((new Vector3(path[i].x,0,path[i].z)-new Vector3(path[i-1].x,0,path[i-1].z)).sqrMagnitude>=.0001f)segments++;
            int calls=0;var obstacle=new GameObject("Obstacle arriving after preview").AddComponent<Collider>();Physics.overlap=(centre,size,results,rotation)=>{calls++;if(calls<=segments)return 0;results[0]=obstacle;return 1;};
            string before=Snapshot(game);Input.Frame(KeyCode.E);Call(game,"UpdateBeltBuild");Check(calls>segments&&Snapshot(game)==before&&(int)Get(game,"beltStage")==2&&!(bool)Get(game,"previewClear"),"late collision must stop commit and preserve selection");
            var material=((LineRenderer)Get(game,"beltPreview")).sharedMaterial;string reason=(string)Get(game,"buildReason");Check(Near(material.color.r,YardGeometry.Rust.r)&&reason.Contains("blocked by fixed scenery")&&!reason.Contains(" / clear"),"failed recheck should turn preview invalid and explain obstruction without claiming clearance");
        });
    }
}
