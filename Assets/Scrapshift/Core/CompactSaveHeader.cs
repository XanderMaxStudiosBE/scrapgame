using System;
using System.Collections.Generic;
using System.Globalization;

namespace Scrapshift.Compact
{
    // Check required ROOT fields before Unity fills absent primitive fields with zero.
    // Strings (including an archived legacy JSON snapshot) cannot supply these keys.
    public static class CompactSaveHeader
    {
        static readonly string[] Required={"version","money","experience","nextId","carriedId","playerX","playerY","playerZ","yaw","pitch","items","scrap","equipment","powerLinks"};
        public static void Validate(string json)
        {
            if(string.IsNullOrWhiteSpace(json))throw new ArgumentException("Empty compact save.");
            if(!json.TrimStart().StartsWith("{",StringComparison.Ordinal)||!json.TrimEnd().EndsWith("}",StringComparison.Ordinal))
                throw new ArgumentException("Compact save must be a JSON object.");
            int depth=0,version=0;bool quoted=false,escaped=false;var keys=new HashSet<string>();
            int start=0;
            for(int i=0;i<json.Length;i++)
            {
                char c=json[i];
                if(quoted)
                {
                    if(escaped){escaped=false;continue;}
                    if(c=='\\'){escaped=true;continue;}
                    if(c!='\"')continue;
                    quoted=false;
                    int next=i+1;while(next<json.Length&&char.IsWhiteSpace(json[next]))next++;
                    if(depth==1&&next<json.Length&&json[next]==':')
                    {
                        string key=json.Substring(start,i-start);
                        if(!keys.Add(key))throw new ArgumentException("Duplicate compact-save field: "+key+".");
                        CheckValue(json,next+1,key);
                        if(key=="version")
                        {
                            int end=next+1;while(end<json.Length&&json[end]!=','&&json[end]!='}')end++;
                            version=int.Parse(json.Substring(next+1,end-next-1).Trim(),CultureInfo.InvariantCulture);
                        }
                    }
                    continue;
                }
                if(c=='\"'){quoted=true;start=i+1;}
                else if(c=='{'||c=='[')depth++;
                else if(c=='}'||c==']'){depth--;if(depth<0)throw new ArgumentException("Malformed compact save.");}
            }
            if(quoted||depth!=0)throw new ArgumentException("Incomplete compact save.");
            foreach(string key in Required)if(!keys.Contains(key))throw new ArgumentException("Missing compact-save field: "+key+".");
            if(version>=3&&!keys.Contains("belts"))throw new ArgumentException("Missing compact-save field: belts.");
        }
        static void CheckValue(string json,int start,string key)
        {
            if(Array.IndexOf(Required,key)<0&&key!="belts")return;
            while(start<json.Length&&char.IsWhiteSpace(json[start]))start++;
            if(key=="items"||key=="scrap"||key=="equipment"||key=="powerLinks"||key=="belts")
            {
                if(start>=json.Length||json[start]!='[')throw new ArgumentException("Expected compact-save list: "+key+".");
                return;
            }
            int end=start;while(end<json.Length&&json[end]!=','&&json[end]!='}')end++;
            string token=json.Substring(start,end-start).Trim();
            bool integer=key=="version"||key=="money"||key=="experience"||key=="nextId"||key=="carriedId";
            if(integer)
            {if(!int.TryParse(token,NumberStyles.Integer,CultureInfo.InvariantCulture,out int parsed))throw new ArgumentException("Expected integer compact-save field: "+key+".");}
            else if(!double.TryParse(token,NumberStyles.Float,CultureInfo.InvariantCulture,out double number)||double.IsNaN(number)||double.IsInfinity(number))
                throw new ArgumentException("Expected finite compact-save number: "+key+".");
        }
    }
}
