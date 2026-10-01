using System;
using System.IO;
using Scrapshift;
// Narrow adapter for the recovery branch only. It does not implement or verify Unity JSON serialization.
namespace UnityEngine
{
    public static class JsonUtility
    {
        public static int Reads;
        public static T FromJson<T>(string json)
        {
            Reads++;
            if (json != "{\"version\":1,\"items\":[]}") throw new ArgumentException("Unexpected fixture");
            return (T)(object)new YardState { money=42 };
        }
        public static string ToJson(object value,bool pretty) { throw new NotSupportedException("Serialization is outside this harness"); }
    }
    public static class Debug { public static void LogWarning(string text) { } }
}
class Check
{
    static void Main()
    {
        string directory=Path.Combine(Path.GetTempPath(),"scrapshift-recovery-"+Guid.NewGuid());Directory.CreateDirectory(directory);
        string path=Path.Combine(directory,"yard.json"),backup=path+".bak";
        try
        {
            File.WriteAllText(path,"{}");File.WriteAllText(backup,"{\"version\":1,\"items\":[]}");
            string notice;var state=SaveStore.Read(path,out notice);
            if(state.money!=42||!notice.Contains("backup")||UnityEngine.JsonUtility.Reads!=1)throw new Exception("Missing-field primary did not use backup");
            if(File.ReadAllText(path)!="{}"||File.ReadAllText(backup)!="{\"version\":1,\"items\":[]}")throw new Exception("Recovery modified files");
            Console.WriteLine("PASS missing-field primary tries validated backup without overwriting files");
            File.WriteAllText(backup,"{}");bool rejected=false;
            try{SaveStore.Read(path,out notice);}catch(InvalidDataException ex){rejected=ex.Message.Contains("Save and backup");}
            if(!rejected||File.ReadAllText(path)!="{}"||File.ReadAllText(backup)!="{}")throw new Exception("Invalid files must remain protected");
            Console.WriteLine("PASS both missing-field files reach protected final failure");
            File.Delete(path);File.WriteAllText(backup,"{\"version\":1,\"items\":[]}");
            if(SaveStore.Read(path,out notice).money!=42||File.Exists(path))throw new Exception("Backup-only recovery failed");
            Console.WriteLine("PASS missing primary recovers backup without creating a new save");
            Console.WriteLine("Scope: real SaveStore control flow and files, test adapter after field checks; not Unity JSON");
        }
        finally { Directory.Delete(directory,true); }
    }
}
