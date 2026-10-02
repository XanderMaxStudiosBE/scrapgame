using System;
using System.Collections.Generic;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public static class CompactAutomationScenarios
    {
        public static readonly string[] Names={"ConveyorPortsAndCorners","ConveyorRouteSafetyAndCosts","ConveyorSocketAndFilterGuards","ConveyorMoveAndRefundGuards",
            "ConveyorStorageTransferConservation","ConveyorBackpressureAndLineage","ConveyorSplitFairness","ConveyorMergeFairness","ConveyorTier2ProcessingChain",
            "ConveyorTransportCapacityAndIdentityLimits","ConveyorSaveValidation","ConveyorLongRunAndResume","ConveyorDurableCostsAndLayoutCache","ConveyorChainedInputFilters"};
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new Exception("Invalid automation state accepted.");}
        static ScrappingModel Fresh(out ConstructionModel c,out AutomationModel a)
        {
            var m=new ScrappingModel(new CompactRules());m.State.money=100000;m.State.experience=m.Rules.levelThresholds[9];
            c=new ConstructionModel(m.State,m.Rules);a=new AutomationModel(m,c);return m;
        }
        static int Place(ConstructionModel c,EquipmentKind kind,float x,float z,float yaw=0)
        {Check(c.Place(kind,x,z,yaw),c.LastMessage);return c.LastPlacedId;}
        static ConveyorLink Link(AutomationModel a,CompactYardState s,int from,int output,int to,int input,bool xFirst=true)
        {Check(a.Connect(from,output,to,input,xFirst),a.LastMessage);foreach(var link in s.belts)if(link.id==a.LastConnectedId)return link;throw new Exception("Missing connected belt.");}
        static CompactStack Put(ScrappingModel m,int id,PartKind kind,int quantity,bool eligible=true)
        {
            var e=m.FindEquipment(id);var stack=new CompactStack{id=m.State.nextId++,kind=kind,quantity=quantity,x=e.x,z=e.z,xpEligible=eligible};
            e.contents.Add(stack);return stack;
        }
        static int Total(CompactYardState state,PartKind kind,bool eligible)
        {
            int total=0;foreach(var item in state.items)if(item.kind==kind && item.xpEligible==eligible)total+=item.quantity;
            foreach(var e in state.equipment)
            {
                foreach(var item in e.contents)if(item.kind==kind && item.xpEligible==eligible)total+=item.quantity;
                if(e.job!=null && e.job.xpEligible==eligible)foreach(var y in e.job.yields)if(y.kind==kind)total+=y.quantity;
            }
            foreach(var link in state.belts)foreach(var item in link.items)if(item.kind==kind && item.xpEligible==eligible)total+=item.quantity;
            return total;
        }
        static void Advance(AutomationModel a,float seconds){for(int i=0;i<(int)(seconds/.1f);i++)a.Tick(.1f);}
        public static void Run(string name)
        {
            ConstructionModel c;AutomationModel a;var m=Fresh(out c,out a);var s=m.State;string reason;
            switch(name)
            {
                case "ConveyorPortsAndCorners":
                    Check(AutomationModel.PortCount(EquipmentKind.Tier1Scrapper,false)==0 && AutomationModel.PortCount(EquipmentKind.Splitter,true)==3 &&
                        AutomationModel.PortCount(EquipmentKind.Merger,false)==3,"equipment port topology");
                    int start=Place(c,EquipmentKind.Storage,-8,3),end=Place(c,EquipmentKind.Storage,-2,9);
                    var belt=new ConveyorLink{fromId=start,toId=end};var path=AutomationModel.Path(belt,s,m.Rules);
                    Check(path.Length>=4 && path[1].z>path[0].z,"outward stubs kept");
                    for(int i=1;i<path.Length;i++)Check(Math.Abs(path[i].x-path[i-1].x)<.001f || Math.Abs(path[i].z-path[i-1].z)<.001f,"orthogonal route");
                    var other=new ConveyorLink{fromId=start,toId=end,bendXFirst=false};var alternative=AutomationModel.Path(other,s,m.Rules);
                    Check(path[2].x!=alternative[2].x || path[2].z!=alternative[2].z,"elbow choice changes route");
                    var t1=new EquipmentState{kind=EquipmentKind.Tier1Scrapper,x=0,z=0};Check(AutomationModel.Port(t1,m.Rules,true,0).z<0,"Tier1 tray output faces -Z");break;
                case "ConveyorRouteSafetyAndCosts":
                    int source=Place(c,EquipmentKind.Storage,-8,0),dest=Place(c,EquipmentKind.Storage,-8,6);
                    int before=s.money,next=s.nextId;Check(a.CanConnect(source,0,dest,0,true,out reason),reason);
                    Check(s.money==before && s.nextId==next && s.belts.Count==0,"preview free");
                    var route=Link(a,s,source,0,dest,0);int price=(int)Math.Ceiling(AutomationModel.Length(AutomationModel.Path(route,s,m.Rules))/2)*m.Rules.Equipment(EquipmentKind.Conveyor).price;
                    Check(route.paidPrice==price && s.money==before-price,"sectioncost paid once");
                    Check(!c.CanPlace(EquipmentKind.Generator,-8,3,0,0,out reason) && reason.Contains("conveyor"),"construction preserves track");
                    Check(!a.Connect(source,0,dest,0,true) && s.money==before-price,"repeated purchase nonmutating");
                    int blockA=Place(c,EquipmentKind.Storage,0,0),blockB=Place(c,EquipmentKind.Storage,0,9);
                    Place(c,EquipmentKind.Workbench,0,4);
                    Check(!a.CanConnect(blockA,0,blockB,0,true,out reason) && reason.Contains("crosses"),"no machine-body tunneling");
                    int nearFence=Place(c,EquipmentKind.Storage,21,15,90);
                    Check(!a.CanConnect(nearFence,0,blockB,0,true,out reason),"bounds or route length rejected");break;
                case "ConveyorSocketAndFilterGuards":
                    int src=Place(c,EquipmentKind.Storage,0,0),dst=Place(c,EquipmentKind.Tier2Scrapper,0,6);
                    Check(!a.CanConnect(src,1,dst,0,true,out reason) && !a.CanConnect(src,0,src,0,true,out reason),"unsupported/self ports");
                    Check(m.SetFilter(src,(int)PartKind.Copper) && !a.CanConnect(src,0,dst,0,true,out reason),"material-source to processing blackhole explained");
                    Check(m.SetFilter(src,(int)PartKind.Wire),"wirefilter");var link=Link(a,s,src,0,dst,0);
                    Check(!a.CanConnect(src,0,dst,0,false,out reason),"individual port occupied");
                    int second=Place(c,EquipmentKind.Storage,6,0);Check(!a.CanConnect(second,0,dst,0,true,out reason),"destination input already occupied");
                    s.experience=0;Check(!a.CanConnect(second,0,src,0,true,out reason),"level gate");
                    Check(link.items.Count==0,"no preview items invented");break;
                case "ConveyorMoveAndRefundGuards":
                    int first=Place(c,EquipmentKind.Storage,-8,0),last=Place(c,EquipmentKind.Storage,-8,6);var owned=Link(a,s,first,0,last,0);
                    Check(!c.Move(first,-6,0,0) && !c.Remove(last),"connected equipment stays intact");
                    int cash=s.money;Check(a.Remove(owned.id) && s.money==cash+owned.paidPrice/2 && !a.Remove(owned.id),"refund once");
                    Check(c.Move(first,-6,0,0),"disconnected empty equipment can move");
                    var active=Link(a,s,first,0,last,0);Put(m,first,PartKind.Copper,1);a.Tick(.1f);
                    Check(active.items.Count==1 && !a.Remove(active.id),"loaded belt cannot delete cargo");break;
                case "ConveyorStorageTransferConservation":
                    int f=Place(c,EquipmentKind.Storage,0,0),t=Place(c,EquipmentKind.Storage,0,6);Link(a,s,f,0,t,0);
                    var original=Put(m,f,PartKind.Copper,5);Put(m,f,PartKind.Copper,3,false);
                    Advance(a,20);
                    Check(m.StoredUnits(f)==0 && m.StoredUnits(t)==8,"all transported");
                    Check(Total(s,PartKind.Copper,true)==5 && Total(s,PartKind.Copper,false)==3,"quantity and lineage conserved");
                    Check(m.State.experience==m.Rules.levelThresholds[9],"transport grants no XP");
                    bool finalId=false;foreach(var stack in m.FindEquipment(t).contents)if(stack.id==original.id)finalId=true;
                    Check(finalId,"last unit retains original bundle identity");
                    ScrappingModel.Validate(s,m.Rules);ConstructionModel.Validate(s,m.Rules);AutomationModel.Validate(s,m.Rules);break;
                case "ConveyorBackpressureAndLineage":
                    int bf=Place(c,EquipmentKind.Storage,0,0),bt=Place(c,EquipmentKind.Storage,0,6);var blocked=Link(a,s,bf,0,bt,0);
                    m.Rules.Equipment(EquipmentKind.Storage).outputCapacity=4;Put(m,bt,PartKind.Steel,4,false);Put(m,bf,PartKind.Copper,4,true);
                    Advance(a,10);Check(blocked.items.Count>0 && blocked.items[0].progress==1,"blocked destination retains head at end");
                    Check(Total(s,PartKind.Copper,true)==4 && Total(s,PartKind.Steel,false)==4,"blocked conservation");
                    float progress=blocked.items[0].progress;Check(!a.Tick(0) && blocked.items[0].progress==progress,"pause time ignored");
                    Check(m.Withdraw(bt,m.FindEquipment(bt).contents[0].id) && m.Drop(8,.25f,8),"make destination space");Advance(a,10);
                    Check(m.StoredUnits(bt)==4 && blocked.items.Count==0 && m.StoredUnits(bf)==0,"backpressure resumes without loss");break;
                case "ConveyorSplitFairness":
                    int split=Place(c,EquipmentKind.Splitter,0,3),left=Place(c,EquipmentKind.Storage,-5,3,270),forward=Place(c,EquipmentKind.Storage,0,9),right=Place(c,EquipmentKind.Storage,5,3,90);
                    Link(a,s,split,0,left,0);Link(a,s,split,1,forward,0);Link(a,s,split,2,right,0);Put(m,split,PartKind.Plastic,12);
                    Advance(a,25);Check(m.StoredUnits(left)==4 && m.StoredUnits(forward)==4 && m.StoredUnits(right)==4,"splitter round robin balanced");
                    Check(Total(s,PartKind.Plastic,true)==12 && m.StoredUnits(split)==0,"branches conserve quantity");break;
                case "ConveyorMergeFairness":
                    int merge=Place(c,EquipmentKind.Merger,0,3),ml=Place(c,EquipmentKind.Storage,-5,3,90),mb=Place(c,EquipmentKind.Storage,0,-3),mr=Place(c,EquipmentKind.Storage,5,3,270);
                    int outBox=Place(c,EquipmentKind.Storage,0,9);var l=Link(a,s,ml,0,merge,0);var b=Link(a,s,mb,0,merge,1);var r=Link(a,s,mr,0,merge,2);Link(a,s,merge,0,outBox,0);
                    Put(m,ml,PartKind.Copper,4);Put(m,mb,PartKind.Plastic,4);Put(m,mr,PartKind.Steel,4);
                    m.Rules.Equipment(EquipmentKind.Merger).outputCapacity=1;Advance(a,35);
                    Check(m.StoredUnits(outBox)==12,"merger admits every input with one free slot");
                    Check(Total(s,PartKind.Copper,true)==4 && Total(s,PartKind.Plastic,true)==4 && Total(s,PartKind.Steel,true)==4,"merge conservation");
                    Check(l.items.Count+b.items.Count+r.items.Count==0,"no starving blocked input");break;
                case "ConveyorTier2ProcessingChain":
                    int raw=Place(c,EquipmentKind.Storage,0,0),processor=Place(c,EquipmentKind.Tier2Scrapper,0,6),finished=Place(c,EquipmentKind.Storage,0,12),gen=Place(c,EquipmentKind.Generator,5,6);
                    Link(a,s,raw,0,processor,0);Link(a,s,processor,0,finished,0);Check(c.Connect(gen,processor),"Tier2 generator network");
                    Check(m.SetFilter(processor,(int)PartKind.Wire),"Tier2 recipe filter selects input, not output materials");
                    m.HasPower=equipmentId=>c.PowerFor(equipmentId).powered;Put(m,raw,PartKind.Wire,3,true);Put(m,raw,PartKind.Wire,2,false);
                    for(int i=0;i<600;i++){m.Tick(.1f);a.Tick(.1f);}
                    Check(Total(s,PartKind.Wire,true)==0 && Total(s,PartKind.Wire,false)==0,"all five inputs consumed once");
                    Check(Total(s,PartKind.Copper,true)==9 && Total(s,PartKind.Insulation,true)==6 && Total(s,PartKind.Copper,false)==6 && Total(s,PartKind.Insulation,false)==4,"snapshots and XP provenance preserved");
                    Check(m.StoredUnits(finished)==25 && m.State.experience==m.Rules.levelThresholds[9],"transport/processing does not award sales XP");
                    ScrappingModel.Validate(s,m.Rules);AutomationModel.Validate(s,m.Rules);break;
                case "ConveyorTransportCapacityAndIdentityLimits":
                    int cf=Place(c,EquipmentKind.Storage,0,0),ct=Place(c,EquipmentKind.Storage,0,6);var capacity=Link(a,s,cf,0,ct,0);var bulk=Put(m,cf,PartKind.Copper,3);
                    m.Rules.maxStacks=m.OccupiedSlots;Check(!a.Tick(.1f) && bulk.quantity==3 && capacity.items.Count==0,"no extra unit record past global slot limit");
                    m.Rules.maxStacks=256;s.nextId=int.MaxValue;Check(!a.Tick(.1f) && bulk.quantity==3,"ID exhaustion never consumes source");
                    bulk.quantity=1;Check(a.Tick(.1f) && capacity.items[0].id==bulk.id,"existing unit ID can transfer without allocation");break;
                case "ConveyorSaveValidation":
                    int vf=Place(c,EquipmentKind.Storage,0,0),vt=Place(c,EquipmentKind.Storage,0,6);var v=Link(a,s,vf,0,vt,0);Put(m,vf,PartKind.Copper,2);a.Tick(.1f);
                    AutomationModel.Validate(s,m.Rules);int savedTransitId=v.items[0].id;v.items[0].id=v.id;Reject(()=>AutomationModel.Validate(s,m.Rules));v.items[0].id=savedTransitId;
                    v.items[0].quantity=2;Reject(()=>AutomationModel.Validate(s,m.Rules));v.items[0].quantity=1;
                    v.items[0].progress=float.NaN;Reject(()=>AutomationModel.Validate(s,m.Rules));v.items[0].progress=0;
                    v.toPort=1;Reject(()=>AutomationModel.Validate(s,m.Rules));v.toPort=0;
                    s.belts.Add(new ConveyorLink{id=s.nextId++,fromId=vf,toId=vt});Reject(()=>AutomationModel.Validate(s,m.Rules));s.belts.RemoveAt(1);break;
                case "ConveyorLongRunAndResume":
                    int lf=Place(c,EquipmentKind.Storage,0,0),lt=Place(c,EquipmentKind.Storage,0,6);Link(a,s,lf,0,lt,0);Put(m,lf,PartKind.Copper,70);Put(m,lf,PartKind.Plastic,40,false);
                    var random=new Random(941);int copper=70,plastic=40;
                    for(int i=0;i<1500;i++)
                    {
                        if(i%97==0){m=new ScrappingModel(m.Rules,s);c=new ConstructionModel(s,m.Rules);a=new AutomationModel(m,c);}
                        a.Tick(i%43==0?0:(float)random.NextDouble()*.2f);
                        Check(Total(s,PartKind.Copper,true)==copper && Total(s,PartKind.Plastic,false)==plastic,"seeded conservation through model reconstruction");
                        if(i%71==0){ScrappingModel.Validate(s,m.Rules);AutomationModel.Validate(s,m.Rules);}
                    }
                    Check(m.StoredUnits(lt)>0,"delivery progresses");break;
                case "ConveyorDurableCostsAndLayoutCache":
                    int cachedFrom=Place(c,EquipmentKind.Storage,0,0),cachedTo=Place(c,EquipmentKind.Storage,0,6);int savedCash=s.money,savedNext=s.nextId;
                    m.Rules.Equipment(EquipmentKind.Conveyor).price=1000000;s.money=int.MaxValue;
                    Check(!a.CanConnect(cachedFrom,0,cachedTo,0,true,out reason) && reason.Contains("durable"),"reject purchase too large to save");
                    Check(s.nextId==savedNext && s.belts.Count==0,"oversized price never mutates ownership");
                    s.money=savedCash;m.Rules.Equipment(EquipmentKind.Conveyor).price=12;
                    var cached=Link(a,s,cachedFrom,0,cachedTo,0);Put(m,cachedFrom,PartKind.Copper,2);
                    a.Tick(.1f);int builds=a.LayoutBuildCount;Advance(a,10);
                    Check(a.LayoutBuildCount==builds,"ordinary progress and inventory use one cached layout");
                    m.Rules.Equipment(EquipmentKind.Storage).depth+=.05f;a.Tick(.1f);
                    Check(a.LayoutBuildCount==builds+1,"edited rule dimensions invalidate exact cached geometry");
                    Check(a.Remove(cached.id),"empty route removal");a.Tick(.1f);Check(a.LayoutBuildCount==builds+2,"route removal invalidates port cache");
                    Place(c,EquipmentKind.Generator,8,6);a.Tick(.1f);Check(a.LayoutBuildCount==builds+3,"equipment ownership invalidates order cache");break;
                case "ConveyorChainedInputFilters":
                    int upstream=Place(c,EquipmentKind.Tier2Scrapper,0,0),downstream=Place(c,EquipmentKind.Tier2Scrapper,0,6),chainStorage=Place(c,EquipmentKind.Storage,0,12);
                    int firstGenerator=Place(c,EquipmentKind.Generator,5,0),secondGenerator=Place(c,EquipmentKind.Generator,5,6);
                    Check(m.SetFilter(upstream,(int)PartKind.Motor) && m.SetFilter(downstream,(int)PartKind.Wire),"distinct input recipe filters");
                    m.Rules.Recipe(PartKind.Motor).yields=new[]{new PartAmount(PartKind.Wire,1)};m.Rules.Validate();
                    Link(a,s,upstream,0,downstream,0);Link(a,s,downstream,0,chainStorage,0);
                    Check(c.Connect(firstGenerator,upstream) && c.Connect(secondGenerator,upstream) && c.Connect(upstream,downstream),"adequate connected supply");
                    m.HasPower=poweredId=>c.PowerFor(poweredId).powered;Put(m,upstream,PartKind.Motor,1);
                    for(int i=0;i<500;i++){m.Tick(.1f);a.Tick(.1f);}
                    Check(Total(s,PartKind.Wire,true)==0 && Total(s,PartKind.Motor,true)==0 && Total(s,PartKind.Copper,true)==3 && Total(s,PartKind.Insulation,true)==2,"upstream input filter never blocks different downstream output kind");
                    Check(m.StoredUnits(chainStorage)==5,"two timed recipes transfer their actual results");break;
                default:throw new Exception("Unknown automation scenario: "+name);
            }
        }
    }
}
