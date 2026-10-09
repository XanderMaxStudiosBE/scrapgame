using System;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    // Pure authoritative-model regressions. These do not execute Unity rays or JSON.
    public static class CompactReferenceConveyorScenarios
    {
        public static readonly string[] Names={"ReferenceEveryMouthRotates","ReferenceOccupiedOutputSelection","ReferenceSourceSelectionReadOnly",
            "ReferenceRotatedRoutePreviewAndPrice","ReferenceRoutePurchaseRechecksCash","ReferenceRotatedProcessingRequiresWorkAndPower",
            "ReferenceRotatedBlockedSaveConserves"};
        static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
        static CompactPortPoint Rotate(float x,float z,float yaw)
        {double a=yaw*Math.PI/180;return new CompactPortPoint((float)(x*Math.Cos(a)+z*Math.Sin(a)),(float)(-x*Math.Sin(a)+z*Math.Cos(a)));}
        sealed class Line
        {
            public CompactRules rules=new CompactRules{startingMoney=2000,startingCars=0,startingRefrigerators=0};
            public ScrappingModel model;public ConstructionModel construction;public AutomationModel automation;
            public int source,processor,destination,generator;
            public CompactYardState State{get{return model.State;}}
            public Line(EquipmentKind kind,float yaw,bool connect=true)
            {
                model=new ScrappingModel(rules);model.State.experience=rules.levelThresholds[4];Bind();
                if(kind==EquipmentKind.Workbench){processor=State.equipment[0].id;Check(construction.Move(processor,1,4,yaw),construction.LastMessage);}
                else processor=Place(kind,1,4,yaw);
                var offset=Rotate(0,6,yaw);bool tier1=kind==EquipmentKind.Tier1Scrapper;
                float inputSign=tier1?1:-1,storageYaw=tier1?(yaw+180)%360:yaw;
                source=Place(EquipmentKind.Storage,1+offset.x*inputSign,4+offset.z*inputSign,storageYaw);
                destination=Place(EquipmentKind.Storage,1-offset.x*inputSign,4-offset.z*inputSign,storageYaw);
                if(kind!=EquipmentKind.Workbench)
                {
                    var side=Rotate(3.5f,0,yaw);generator=Place(EquipmentKind.Generator,1+side.x,4+side.z,yaw);
                    Check(construction.Connect(generator,processor),construction.LastMessage);
                }
                if(connect)
                {
                    Check(automation.Connect(source,0,processor,0,true),automation.LastMessage);
                    Check(automation.Connect(processor,0,destination,0,true),automation.LastMessage);
                }
            }
            int Place(EquipmentKind kind,float x,float z,float yaw)
            {Check(construction.Place(kind,x,z,yaw),construction.LastMessage);return construction.LastPlacedId;}
            void Bind()
            {construction=new ConstructionModel(State,rules);automation=new AutomationModel(model,construction);model.HasPower=id=>construction.PowerFor(id).powered;}
            public void Put(int owner,PartKind kind,int quantity,bool eligible)
            {var e=model.FindEquipment(owner);e.contents.Add(new CompactStack{id=State.nextId++,kind=kind,quantity=quantity,x=e.x,z=e.z,xpEligible=eligible});}
            public void Advance(int frames)
            {for(int i=0;i<frames;i++){model.Tick(.1f);automation.Tick(.1f);}}
            public void Resume()
            {model=new ScrappingModel(rules,CompactScrappingScenarios.Copy(State));Bind();}
            public void Validate()
            {ScrappingModel.Validate(State,rules);ConstructionModel.Validate(State,rules);AutomationModel.Validate(State,rules);}
        }
        static int Total(CompactYardState state,PartKind kind,bool eligible)
        {
            int units=0;foreach(var stack in state.items)if(stack.kind==kind&&stack.xpEligible==eligible)units+=stack.quantity;
            foreach(var e in state.equipment)
            {
                foreach(var stack in e.contents)if(stack.kind==kind&&stack.xpEligible==eligible)units+=stack.quantity;
                if(e.job!=null&&e.job.xpEligible==eligible)foreach(var amount in e.job.yields)if(amount.kind==kind)units+=amount.quantity;
            }
            foreach(var belt in state.belts)foreach(var item in belt.items)if(item.kind==kind&&item.xpEligible==eligible)units+=item.quantity;
            return units;
        }
        static void CheckWireLedger(CompactYardState state)
        {
            foreach(bool eligible in new[]{true,false})
            {
                int wires=eligible?2:1;
                Check(Total(state,PartKind.Copper,eligible)+3*Total(state,PartKind.Wire,eligible)==3*wires,"Copper and pending wire retain source lineage.");
                Check(Total(state,PartKind.Insulation,eligible)+2*Total(state,PartKind.Wire,eligible)==2*wires,"Insulation and pending wire retain source lineage.");
            }
        }
        public static void Run(string name)
        {
            string reason;var rules=new CompactRules();
            switch(name)
            {
                case "ReferenceEveryMouthRotates":
                    foreach(var kind in new[]{EquipmentKind.Workbench,EquipmentKind.Tier1Scrapper,EquipmentKind.Tier2Scrapper,EquipmentKind.Storage,EquipmentKind.Splitter,EquipmentKind.Merger,EquipmentKind.PrimaryScrapper,EquipmentKind.ExportStation})
                        foreach(float yaw in new[]{0f,90f,180f,270f})foreach(bool output in new[]{false,true})
                            for(int index=0;index<AutomationModel.PortCount(kind,output);index++)
                            {
                                var e=new EquipmentState{kind=kind,x=3,z=4,yaw=yaw};var p=AutomationModel.Port(e,rules,output,index);
                                Check(CompactPortSelection.Find(e,rules,output,p.x,.7f,p.z)==index,"Every real mouth preserves its role/index at all quarter turns.");
                                Check(CompactPortSelection.Find(e,rules,!output,p.x,.7f,p.z)==-1,"Opposite-role body snapping is refused.");
                            }
                    break;
                case "ReferenceOccupiedOutputSelection":
                {
                    var state=new CompactYardState();var e=new EquipmentState{id=1,kind=EquipmentKind.Splitter,yaw=270};state.equipment.Add(e);
                    state.belts.Add(new ConveyorLink{id=2,fromId=1,fromPort=1,toId=3});
                    Check(!CompactPortSelection.CanSelectOutput(state,rules,1,1,out reason)&&reason.Contains("already"),"Attached OUT is rejected before route stage.");
                    Check(CompactPortSelection.CanSelectOutput(state,rules,1,0,out reason)&&CompactPortSelection.CanSelectOutput(state,rules,1,2,out reason),"Unoccupied splitter outputs remain independently selectable.");
                    Check(!CompactPortSelection.CanSelectOutput(state,rules,1,3,out reason)&&!CompactPortSelection.CanSelectOutput(state,rules,0,0,out reason),"Invalid index or missing source cannot start.");
                    e.kind=EquipmentKind.Generator;Check(!CompactPortSelection.CanSelectOutput(state,rules,1,0,out reason),"Power sockets cannot become item belts.");
                    e.kind=EquipmentKind.ExportStation;Check(!CompactPortSelection.CanSelectOutput(state,rules,1,0,out reason),"Material dispatch has IN only.");break;
                }
                case "ReferenceSourceSelectionReadOnly":
                {
                    var line=new Line(EquipmentKind.Tier1Scrapper,0,false);int cash=line.State.money,next=line.State.nextId;
                    var e=line.model.FindEquipment(line.processor);
                    Check(CompactPortSelection.CanSelectOutput(line.State,line.rules,e.id,0,out reason),reason);
                    e.yaw=45;Check(!CompactPortSelection.CanSelectOutput(line.State,line.rules,e.id,0,out reason),"Non-quarter output is explained before selecting.");
                    e.yaw=0;Check(!CompactPortSelection.CanSelectOutput(null,line.rules,e.id,0,out reason)&&!CompactPortSelection.CanSelectOutput(line.State,null,e.id,0,out reason),"Missing preview dependencies rejected safely.");
                    Check(line.State.money==cash&&line.State.nextId==next&&line.State.belts.Count==0,"Repeated start previews and cancellation do not spend or allocate.");line.Validate();break;
                }
                case "ReferenceRotatedRoutePreviewAndPrice":
                    foreach(float yaw in new[]{0f,90f,180f,270f})
                    {
                        var line=new Line(EquipmentKind.Tier1Scrapper,yaw,false);line.rules.Equipment(EquipmentKind.Conveyor).price=17;
                        int cash=line.State.money,next=line.State.nextId;
                        foreach(bool elbow in new[]{false,true})
                        {
                            var link=new ConveyorLink{fromId=line.processor,fromPort=0,toId=line.destination,toPort=0,bendXFirst=elbow};
                            var path=AutomationModel.Path(link,line.State,line.rules);var start=AutomationModel.Port(line.model.FindEquipment(line.processor),line.rules,true,0);
                            var end=AutomationModel.Port(line.model.FindEquipment(line.destination),line.rules,false,0);
                            Check(Math.Abs(path[0].x-start.x)<.0001f&&Math.Abs(path[0].z-start.z)<.0001f&&Math.Abs(path[path.Length-1].x-end.x)<.0001f&&Math.Abs(path[path.Length-1].z-end.z)<.0001f,"Preview uses exact mouth endpoints and saved Tier1 outlet.");
                            Check(line.automation.CanConnect(line.processor,0,line.destination,0,elbow,out reason),reason);
                            Check(reason.Contains("€"+((long)Math.Ceiling(AutomationModel.Length(path)/2)*17)),"Full routed length uses the authoritative custom price.");
                        }
                        Check(line.State.money==cash&&line.State.nextId==next&&line.State.belts.Count==0,"Changing elbow/preview never buys.");
                        Check(line.automation.Connect(line.processor,0,line.destination,0,true),line.automation.LastMessage);
                        Check(cash-line.State.money==line.State.belts[0].paidPrice,"Exactly the displayed purchase is paid once.");line.Validate();
                    }
                    break;
                case "ReferenceRoutePurchaseRechecksCash":
                {
                    var line=new Line(EquipmentKind.Tier1Scrapper,90,false);
                    Check(line.automation.CanConnect(line.processor,0,line.destination,0,true,out reason),reason);int next=line.State.nextId;
                    line.State.money=0;Check(!line.automation.Connect(line.processor,0,line.destination,0,true),"Confirm revalidates cash after a previously valid preview.");
                    Check(line.State.money==0&&line.State.nextId==next&&line.State.belts.Count==0,"Failed confirmation preserves IDs, ownership and money.");break;
                }
                case "ReferenceRotatedProcessingRequiresWorkAndPower":
                    foreach(float yaw in new[]{0f,90f,180f,270f})foreach(var kind in new[]{EquipmentKind.Workbench,EquipmentKind.Tier1Scrapper,EquipmentKind.Tier2Scrapper})
                    {
                        var line=new Line(kind,yaw);line.Put(line.source,PartKind.Wire,1,true);int xp=line.State.experience,cash=line.State.money;
                        if(kind!=EquipmentKind.Workbench)Check(line.construction.Disconnect(line.generator,line.processor),line.construction.LastMessage);
                        line.Advance(200);var e=line.model.FindEquipment(line.processor);
                        Check(line.model.StoredUnits(line.destination)==0&&line.State.belts[1].items.Count==0,"Raw intake never leaks through finished OUT.");
                        if(kind==EquipmentKind.Workbench)
                        {Check(e.job!=null&&e.job.strokes==0&&!e.job.ready,"Belts prepare but never stroke manual work.");for(int i=0;i<4;i++)Check(line.model.Work(e.id),line.model.LastNotice);}
                        else {Check(e.job==null,"Unpowered machine waits without preparing paid output.");Check(line.construction.Connect(line.generator,line.processor),line.construction.LastMessage);}
                        line.Advance(400);Check(line.model.StoredUnits(line.destination)==5&&Total(line.State,PartKind.Copper,true)==3&&Total(line.State,PartKind.Insulation,true)==2,"Every rotated purchased line recovers exact recipe yields.");
                        Check(line.State.experience==xp&&line.State.money==cash,"Transport/power/manual work awards no sales XP or cash.");line.Validate();
                    }
                    break;
                case "ReferenceRotatedBlockedSaveConserves":
                    foreach(float yaw in new[]{0f,90f,180f,270f})
                    {
                        var line=new Line(EquipmentKind.Tier1Scrapper,yaw);line.Put(line.source,PartKind.Wire,2,true);line.Put(line.source,PartKind.Wire,1,false);
                        line.Put(line.destination,PartKind.Steel,line.rules.Equipment(EquipmentKind.Storage).outputCapacity,false);
                        int cash=line.State.money,xp=line.State.experience;line.Advance(500);CheckWireLedger(line.State);
                        Check(line.State.belts[1].items.Count>0&&line.model.FindEquipment(line.processor).job!=null,"Full destination safely retains transit and a paid output batch.");
                        var savedBelt=line.State.belts[1];int transitId=savedBelt.items[0].id;float progress=savedBelt.items[0].progress,remaining=savedBelt.launchRemaining;
                        line.Resume();Check(line.State.belts[1].items[0].id==transitId&&line.State.belts[1].items[0].progress==progress&&line.State.belts[1].launchRemaining==remaining,"Model reconstruction preserves in-flight identity/progress/cooldown.");
                        CheckWireLedger(line.State);line.model.FindEquipment(line.destination).contents.Clear();line.Advance(1000);CheckWireLedger(line.State);
                        Check(line.model.StoredUnits(line.destination)==15&&line.model.QueueUnits(line.processor)==0&&line.model.FindEquipment(line.processor).job==null,"Clearing downstream stock resumes exactly three original inputs.");
                        Check(line.State.money==cash&&line.State.experience==xp,"Reconstruction/resumption adds no duplicate purchase or rewards.");line.Validate();
                    }
                    break;
                default:throw new Exception("Unknown reference conveyor scenario "+name);
            }
        }
    }
}
