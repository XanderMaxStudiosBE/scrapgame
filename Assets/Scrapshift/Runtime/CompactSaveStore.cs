using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Scrapshift.Compact
{
    public static class CompactSaveStore
    {
        public static void Validate(CompactYardState state,CompactRules rules)
        {
            ScrappingModel.Validate(state,rules);
            ConstructionModel.Validate(state,rules);
        }
        public static void Write(string path,CompactYardState state,CompactRules rules)
        {
            Validate(state,rules);
            string folder=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(folder))Directory.CreateDirectory(folder);
            byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(state,true));
            string temporary=path+".tmp";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
            {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
            if(File.Exists(path))
            {
                bool validPrimary=true;
                try{string json=File.ReadAllText(path);CompactSaveHeader.Validate(json);Validate(JsonUtility.FromJson<CompactYardState>(json),rules);}
                catch(Exception ex)when(ex is InvalidDataException||ex is ArgumentException){validPrimary=false;}
                // After backup recovery, retain both the good backup and unreadable primary.
                string retained=validPrimary?path+".bak":path+".unreadable-"+Guid.NewGuid().ToString("N");
                File.Replace(temporary,path,retained);
            }
            else File.Move(temporary,path);
        }
        // null means no file exists, not an unreadable or unsupported version.
        public static CompactYardState Read(string path,CompactRules rules,out string notice)
        {
            notice="A new compact yard awaits.";
            if(!File.Exists(path)&&!File.Exists(path+".bak"))return null;
            foreach(string candidate in new[]{path,path+".bak"})
            {
                if(!File.Exists(candidate))continue;
                try
                {
                    string json=File.ReadAllText(candidate);CompactSaveHeader.Validate(json);
                    var state=JsonUtility.FromJson<CompactYardState>(json);Validate(state,rules);
                    notice=candidate==path?"Compact yard restored.":"Restored compact-yard backup; unreadable latest file retained.";
                    return state;
                }
                catch(Exception ex)when(ex is IOException||ex is InvalidDataException||ex is ArgumentException||ex is UnauthorizedAccessException)
                {Debug.LogWarning("Could not load "+Path.GetFileName(candidate)+": "+ex.Message);}
            }
            throw new InvalidDataException("Compact save and backup are unreadable. They are preserved; start an archived new yard to recover.");
        }
        public static void Archive(string path)
        {
            // No deletion. Unique directory means an earlier recovery archive is never overwritten.
            string folder=path+".archive-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
            Directory.CreateDirectory(folder);
            foreach(string suffix in new[]{"",".bak",".tmp"})
                if(File.Exists(path+suffix))File.Move(path+suffix,Path.Combine(folder,Path.GetFileName(path+suffix)));
            string directory=Path.GetDirectoryName(path);
            if(!string.IsNullOrEmpty(directory)&&Directory.Exists(directory))
                foreach(string retained in Directory.GetFiles(directory,Path.GetFileName(path)+".unreadable-*"))
                    File.Move(retained,Path.Combine(folder,Path.GetFileName(retained)));
        }
    }
}
