using System;
using Scrapshift.Compact;
namespace Scrapshift.Tests
{
    public static class CompactStageCIntegrationScenarios
    {
        public static readonly string[] Names={"V2MigrationPreservesManualProgress","InvalidV2MigrationIsNonmutating","V3HeaderRequiresTransportList","V3HeaderRejectsNullTransport","V2HeaderRemainsReadable"};
        public static void Run(string name)
        {
            switch(name)
            {
                case "V2MigrationPreservesManualProgress":
                {
                    var rules=new CompactRules();var model=new ScrappingModel(rules);model.AcquireWire();model.BeginProcessing(model.State.equipment[0].id);model.Work(model.State.equipment[0].id);
                    var s=model.State;s.version=2;s.belts=null;s.equipment[0].filterKind=0;int cash=s.money,next=s.nextId;
                    if(!CompactSaveMigration.Upgrade(s,rules)||s.version!=4||s.belts==null||s.belts.Count!=0||s.money!=cash||s.nextId!=next||s.equipment[0].job.strokes!=1||s.equipment[0].filterKind!=-1)
                        throw new Exception("Version-two upgrade altered earning/manual progress.");
                    if(CompactSaveMigration.Upgrade(s,rules))throw new Exception("Upgrade repeats.");
                    ScrappingModel.Validate(s,rules);ConstructionModel.Validate(s,rules);AutomationModel.Validate(s,rules);break;
                }
                case "InvalidV2MigrationIsNonmutating":
                {
                    var rules=new CompactRules();var s=new ScrappingModel(rules).State;s.version=2;s.money=-1;s.belts=null;s.equipment[0].filterKind=0;
                    Reject(()=>CompactSaveMigration.Upgrade(s,rules));
                    if(s.version!=2||s.belts!=null||s.equipment[0].filterKind!=0)throw new Exception("Invalid source was partly upgraded.");break;
                }
                case "V3HeaderRequiresTransportList":Reject(()=>CompactSaveHeader.Validate(Header.Replace("\"version\":2","\"version\":3")));break;
                case "V3HeaderRejectsNullTransport":Reject(()=>CompactSaveHeader.Validate(Header.Replace("\"version\":2","\"version\":3").Replace("\"items\":[]","\"belts\":null,\"items\":[]")));break;
                case "V2HeaderRemainsReadable":CompactSaveHeader.Validate(Header);break;
                default:throw new ArgumentException(name);
            }
        }
        static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new Exception("Invalid input accepted.");}
        const string Header="{\"version\":2,\"money\":0,\"experience\":0,\"nextId\":1,\"carriedId\":0,\"playerX\":0,\"playerY\":1.1,\"playerZ\":-13,\"yaw\":0,\"pitch\":0,\"items\":[],\"scrap\":[],\"equipment\":[],\"powerLinks\":[]}";
    }
}
