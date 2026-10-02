using System;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactIntegrationScenarios
    {
        public static readonly string[] Names={"Legacy cash/portable IDs and source preserved","Legacy manual partial job exact yield/no historical XP","Legacy powered progress requires new connected generator","Legacy ready outputs collected once","Legacy custom work steps retained","Maximum legacy inventory stays clear of fixed infrastructure","Compact headers require actual root fields","Compact headers reject truncation and nested impostors","Import rejects invalid source before producing a state"};
        public static void Run(string name)
        {
            switch(name)
            {
                case "Legacy cash/portable IDs and source preserved":Portable();break;
                case "Legacy manual partial job exact yield/no historical XP":Manual();break;
                case "Legacy powered progress requires new connected generator":Powered();break;
                case "Legacy ready outputs collected once":Ready();break;
                case "Legacy custom work steps retained":Custom();break;
                case "Maximum legacy inventory stays clear of fixed infrastructure":MaximumInventory();break;
                case "Compact headers require actual root fields":Header();break;
                case "Compact headers reject truncation and nested impostors":Malformed();break;
                case "Import rejects invalid source before producing a state":Invalid();break;
                default:throw new ArgumentException(name);
            }
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new Exception("Invalid input was accepted");}
        static CompactYardState Import(YardState old,YardRules oldRules=null)
        {return CompactLegacyImport.Convert(old,oldRules??new YardRules(),new CompactRules(),"{\"version\":1,\"full-source\":true}");}
        static void Portable()
        {
            var old=new YardState{money=83,nextId=9,carriedId=8,orderAccepted=true,orderDelivered=1};
            old.items.Add(new ScrapItem{id=7,kind=MaterialKind.Copper,quantity=5,x=38,y=.25f,z=20});
            old.items.Add(new ScrapItem{id=8,kind=MaterialKind.Wire,quantity=1,x=-39,y=.25f,z=-20});
            var compact=Import(old);var model=new ScrappingModel(new CompactRules(),compact);
            Check(compact.money==83&&compact.experience==0&&compact.scrap.Count==0,"Invented gifts/cash/XP");
            Check(model.Carried.kind==PartKind.Wire&&model.Carried.quantity==1,"Carry identity/quantity lost");
            Check(compact.importedLegacy&&compact.legacySnapshot.Contains("full-source")&&compact.legacyNotice.Contains("escrow"),"Incomplete disclosure/snapshot");
            Check(old.items[0].x==38&&old.items[1].id==8&&old.money==83&&old.orderDelivered==1,"Legacy source mutated");
            Check(!compact.items[0].xpEligible&&!compact.items[1].xpEligible,"Historical eligibility invented");
        }
        static void Manual()
        {
            var old=new YardState{benchLoaded=true,benchStrokes=2};var s=Import(old);var m=new ScrappingModel(new CompactRules(),s);int id=s.equipment[0].id;
            Check(m.Work(id)&&m.Work(id),"Partial work did not resume");
            Check(m.CollectOutput(id,0)&&m.Carried.quantity==3&&m.Carried.kind==PartKind.Copper,"Legacy output changed");
            Check(!m.CollectOutput(id,0)&&m.Sell()&&s.money==9&&s.experience==0,"Repeated output or historical XP");
            Check(old.benchLoaded&&old.benchStrokes==2,"Legacy partial job changed");
        }
        static void Powered()
        {
            var old=new YardState{money=100,machineOwned=true,machineRemaining=2,machinePendingYield=7};
            var s=Import(old);var rules=new CompactRules();var m=new ScrappingModel(rules,s);var c=new ConstructionModel(s,rules);m.HasPower=id=>c.PowerFor(id).powered;
            var machine=s.equipment[1];Check(machine.job.requiredStrokes==0&&machine.job.remaining==2,"Timed job changed");
            m.Tick(1);Check(machine.job.remaining==2,"Free initial power invented");
            Check(c.Place(EquipmentKind.Generator,3,0,0)&&c.Connect(machine.id,c.LastPlacedId),"Cannot connect imported machine");
            m.Tick(1);Check(machine.job.remaining==1,"Connected machine did not advance");
            Check(c.Disconnect(machine.id,c.LastPlacedId),"Disconnect failed");m.Tick(5);Check(machine.job.remaining==1,"Outage lost progress");
            Check(c.Connect(machine.id,c.LastPlacedId),"Reconnect failed");m.Tick(1);
            Check(m.CollectOutput(machine.id,0)&&m.Carried.quantity==7,"Legacy exact yield lost");
            Check(s.money==55&&old.machineRemaining==2&&old.money==100,"Import changed source or generator cost");
        }
        static void Ready()
        {
            var old=new YardState{machineOwned=true,benchOutput=5,machineOutput=8};var s=Import(old);var m=new ScrappingModel(new CompactRules(),s);
            Check(m.CollectOutput(s.equipment[0].id,0)&&m.Carried.quantity==5&&m.Sell(),"Manual ready output lost");
            Check(m.CollectOutput(s.equipment[1].id,0)&&m.Carried.quantity==8&&m.Sell(),"Powered ready output lost");
            Check(s.money==39&&s.experience==0&&!m.CollectOutput(s.equipment[1].id,0),"Ready output duplicated");
        }
        static void Custom()
        {
            var old=new YardState{benchLoaded=true,benchStrokes=3,upgrades=YardUpgrade.HandTools};
            var s=Import(old,new YardRules{manualStrokes=7,copperPerWire=9});var m=new ScrappingModel(new CompactRules(),s);int id=s.equipment[0].id;
            Check(s.equipment[0].job.requiredStrokes==6,"Custom effective steps changed");
            Check(m.Work(id)&&m.Work(id)&&m.Work(id)&&m.CollectOutput(id,0)&&m.Carried.quantity==9,"Custom progress/yield changed");
        }
        static void MaximumInventory()
        {
            var old=new YardState{nextId=101};
            for(int i=1;i<=100;i++)old.items.Add(new ScrapItem{id=i,kind=MaterialKind.Wire,quantity=1});
            var s=Import(old);Check(s.items.Count==100,"Legacy capacity was discarded");
            foreach(var item in s.items)
            {
                Check(item.x>=14&&item.x<=20&&item.z>=-2&&item.z<=15,"Imported bundle outside clear gravel");
                Check(!(item.x>=13.5f&&item.x<=16.5f&&item.z>=-8&&item.z<=-6),"Imported bundle inside wire crate");
                foreach(var other in s.items)if(other.id!=item.id)
                    Check(Math.Abs(other.x-item.x)>=1.49f||Math.Abs(other.z-item.z)>=.84f,"Imported bundles overlap");
            }
        }
        const string ValidHeader="{\"version\":2,\"money\":0,\"experience\":0,\"nextId\":1,\"carriedId\":0,\"playerX\":0,\"playerY\":1.1,\"playerZ\":-13,\"yaw\":0,\"pitch\":0,\"items\":[],\"scrap\":[],\"equipment\":[],\"powerLinks\":[]}";
        static void Header()
        {
            CompactSaveHeader.Validate(ValidHeader);
            Reject(()=>CompactSaveHeader.Validate(ValidHeader.Replace("\"money\":0,","")));
            Reject(()=>CompactSaveHeader.Validate(ValidHeader.Replace("\"money\":0","\"money\":null")));
            Reject(()=>CompactSaveHeader.Validate(ValidHeader.Replace("\"money\":0","\"money\":0,\"money\":0")));
            Reject(()=>CompactSaveHeader.Validate("{\"legacySnapshot\":"+ValidHeader+"}"));
            Reject(()=>CompactSaveHeader.Validate("{\"legacySnapshot\":\"\\\"money\\\":0\"}"));
        }
        static void Malformed()
        {
            Reject(()=>CompactSaveHeader.Validate(ValidHeader.Substring(0,ValidHeader.Length-1)));
            Reject(()=>CompactSaveHeader.Validate("{"+ValidHeader+"}"));Reject(()=>CompactSaveHeader.Validate(""));
        }
        static void Invalid()
        {
            Reject(()=>Import(new YardState{machineRemaining=4,machinePendingYield=2}));
            Reject(()=>CompactLegacyImport.Convert(new YardState(),new YardRules(),new CompactRules(),""));
        }
    }
}
