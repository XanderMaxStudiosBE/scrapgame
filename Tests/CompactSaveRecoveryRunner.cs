using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Scrapshift.Compact;

// Narrow serializer adapter for real filesystem/recovery/atomic-write branches only.
// State snapshots live in this harness; actual Unity JSON has separate, unrun engine tests.
namespace UnityEngine
{
    public static class JsonUtility
    {
        static readonly Dictionary<string,CompactYardState> snapshots=new Dictionary<string,CompactYardState>();
        static int serial;
        public static int Reads;
        public static string ToJson(object value,bool pretty)
        {
            var state=(CompactYardState)value;
            string json="{\"version\":"+state.version+",\"money\":"+state.money+",\"experience\":0,\"nextId\":1,\"carriedId\":0,\"playerX\":0,\"playerY\":1.1,\"playerZ\":-13,\"yaw\":0,\"pitch\":0,\"items\":[],\"scrap\":[],\"equipment\":[],\"powerLinks\":[],\"belts\":[],\"fixture\":"+(++serial)+"}";
            if(state.version==2)json=json.Replace(",\"belts\":[]","");
            snapshots[json]=(CompactYardState)Copy(state);return json;
        }
        public static T FromJson<T>(string json)
        {
            Reads++;if(!snapshots.ContainsKey(json))throw new ArgumentException("Unknown adapter fixture.");
            return (T)Copy(snapshots[json]);
        }
        static object Copy(object value)
        {
            if(value==null)return null;Type type=value.GetType();if(type.IsValueType||value is string)return value;
            if(value is Array array)
            {
                var result=Array.CreateInstance(type.GetElementType(),array.Length);for(int i=0;i<array.Length;i++)result.SetValue(Copy(array.GetValue(i)),i);return result;
            }
            if(value is IList list)
            {
                var result=(IList)Activator.CreateInstance(type);foreach(var item in list)result.Add(Copy(item));return result;
            }
            object copy=Activator.CreateInstance(type);foreach(var f in type.GetFields(BindingFlags.Public|BindingFlags.Instance))f.SetValue(copy,Copy(f.GetValue(value)));return copy;
        }
    }
    public static class Debug{public static void LogWarning(string message){}}
}
class CompactSaveRecoveryRunner
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Main()
    {
        string folder=Path.Combine(Path.GetTempPath(),"scrapshift-compact-recovery-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"yard-v2.json"),legacy=Path.Combine(folder,"yard-v1.json");
        try
        {
            var rules=new CompactRules();string notice;
            Check(CompactSaveStore.Read(path,rules,out notice)==null&&!File.Exists(path),"Missing files must not create a save");
            Console.WriteLine("PASS missing compact files return no saved yard without creating data");
            var model=new ScrappingModel(rules);var state=model.State;model.AcquireWire();model.BeginProcessing(state.equipment[0].id);model.Work(state.equipment[0].id);
            CompactSaveStore.Write(path,state,rules);string first=File.ReadAllText(path);state.money++;
            CompactSaveStore.Write(path,state,rules);
            Check(File.ReadAllText(path+".bak")==first&&!File.Exists(path+".tmp"),"Atomic replacement/backup failed");
            var restored=CompactSaveStore.Read(path,rules,out notice);
            Check(restored.money==rules.startingMoney+1&&restored.equipment[0].job.strokes==1,"Snapshot lost data");
            Console.WriteLine("PASS real atomic replacement preserves previous snapshot and removes temporary file");
            File.WriteAllText(path,"{}");var recovered=CompactSaveStore.Read(path,rules,out notice);
            Check(notice.Contains("backup")&&recovered.money==rules.startingMoney&&File.ReadAllText(path)=="{}","Backup recovery changed files");
            Console.WriteLine("PASS invalid primary recovers validated backup without overwriting either");
            CompactSaveStore.Write(path,recovered,rules);
            Check(File.ReadAllText(path+".bak")==first&&Directory.GetFiles(folder,"yard-v2.json.unreadable-*").Length==1,"Recovery save overwrote the good backup");
            Console.WriteLine("PASS saving recovered state retains good backup and archives the unreadable primary");
            // A header-valid future version must still fail core schema validation before recovery.
            var future=new ScrappingModel(rules).State;future.version=99;File.WriteAllText(path,UnityEngine.JsonUtility.ToJson(future,false));
            Check(CompactSaveStore.Read(path,rules,out notice).version==3&&notice.Contains("backup"),"Future version accepted");
            Console.WriteLine("PASS unsupported version is rejected and existing backup remains usable");
            File.WriteAllText(path,"{}");File.WriteAllText(path+".bak","{}");bool failed=false;
            try{CompactSaveStore.Read(path,rules,out notice);}catch(InvalidDataException){failed=true;}
            Check(failed&&File.ReadAllText(path)=="{}"&&File.ReadAllText(path+".bak")=="{}","Unreadable save was silently reset");
            Console.WriteLine("PASS unreadable primary and backup remain protected instead of resetting");
            File.WriteAllText(legacy,"legacy bytes remain");File.WriteAllText(path+".tmp","interrupted write");CompactSaveStore.Archive(path);
            string[] archive=Directory.GetDirectories(folder);Check(archive.Length==1&&!File.Exists(path)&&!File.Exists(path+".bak")&&!File.Exists(path+".tmp"),"Archive did not retain all files");
            Check(File.Exists(Path.Combine(archive[0],"yard-v2.json.tmp"))&&File.ReadAllText(legacy)=="legacy bytes remain","Legacy or interrupted write lost");
            Console.WriteLine("PASS recovery archive retains compact primary/backup/temp and leaves version one untouched");
            var previous=new ScrappingModel(rules);previous.AcquireWire();previous.BeginProcessing(previous.State.equipment[0].id);previous.Work(previous.State.equipment[0].id);
            previous.State.version=2;previous.State.belts=null;previous.State.equipment[0].filterKind=0;
            string v2=UnityEngine.JsonUtility.ToJson(previous.State,false);File.WriteAllText(path,v2);
            var upgraded=CompactSaveStore.Read(path,rules,out notice);
            Check(upgraded.version==3&&upgraded.belts.Count==0&&upgraded.equipment[0].filterKind==-1&&upgraded.equipment[0].job.strokes==1,"Version-two migration lost progress");
            Check(File.ReadAllText(path)==v2&&notice.Contains("upgraded"),"Migration must not rewrite source on read");
            CompactSaveStore.Write(path,upgraded,rules);
            Check(File.ReadAllText(path+".bak")==v2&&CompactSaveStore.Read(path,rules,out notice).version==3,"Migration backup did not retain original");
            Console.WriteLine("PASS version-two migration retains source bytes, partial work and recoverable backup on the next save");
            var transport=new ScrappingModel(rules);transport.State.money=2000;transport.State.experience=rules.levelThresholds[9];
            var layout=new ConstructionModel(transport.State,rules);
            Check(layout.Place(EquipmentKind.Storage,-4,1,0),layout.LastMessage);int source=layout.LastPlacedId;
            Check(layout.Place(EquipmentKind.Storage,-4,7,0),layout.LastMessage);int destination=layout.LastPlacedId;
            var automation=new AutomationModel(transport,layout);Check(automation.Connect(source,0,destination,0,true),automation.LastMessage);
            var moving=new ConveyorItem{id=transport.State.nextId++,kind=PartKind.Wire,xpEligible=true,progress=.4f};transport.State.belts[0].items.Add(moving);
            CompactSaveStore.Write(path,transport.State,rules);var resumed=CompactSaveStore.Read(path,rules,out notice);
            Check(resumed.belts[0].items[0].id==moving.id&&resumed.belts[0].items[0].xpEligible&&resumed.belts[0].items[0].progress==.4f,"Transport snapshot lost identity, progress or lineage");
            Console.WriteLine("PASS transport snapshot filesystem round trip retains moving identity, progress and XP lineage");
            Console.WriteLine("Scope: real CompactSaveStore and filesystem, narrow snapshot adapter; not Unity JSON.");
        }
        finally{Directory.Delete(folder,true);}
    }
}
