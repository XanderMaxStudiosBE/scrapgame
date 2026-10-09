using System;
using System.Collections.Generic;

namespace Scrapshift.Compact
{
    public struct CompactPortPoint
    {
        public float x,z;
        public CompactPortPoint(float x,float z){this.x=x;this.z=z;}
    }

    // Quantity simulation; transit views are pooled by the runtime coordinator.
    public sealed class AutomationModel
    {
        public const float CorridorWidth=.94f,PortOffset=.35f,StubLength=.5f;
        public const int MaximumBelts=128,MaximumItemsPerBelt=32,MaximumPaidPrice=1000000;
        readonly ScrappingModel model;
        readonly ConstructionModel construction;
        CompactYardState State{get{return model.State;}}
        CompactRules Rules{get{return model.Rules;}}
        public string LastMessage{get;private set;}
        public int LastConnectedId{get;private set;}
        readonly List<ConveyorLink> orderedLinks=new List<ConveyorLink>();
        readonly List<EquipmentState> orderedEquipment=new List<EquipmentState>();
        readonly Dictionary<int,float> routeLengths=new Dictionary<int,float>();
        readonly Dictionary<int,ConveyorLink[]> inputLinks=new Dictionary<int,ConveyorLink[]>(),outputLinks=new Dictionary<int,ConveyorLink[]>();
        EquipmentStamp[] equipmentStamps;LinkStamp[] linkStamps;DefinitionStamp[] definitionStamps;
        public int LayoutBuildCount{get;private set;}
        struct EquipmentStamp
        {
            public EquipmentState item;public int id;public EquipmentKind kind;public float x,z,yaw;
            public bool Matches(EquipmentState e){return ReferenceEquals(item,e) && id==e.id && kind==e.kind && x==e.x && z==e.z && yaw==e.yaw;}
        }
        struct LinkStamp
        {
            public ConveyorLink link;public int id,fromId,fromPort,toId,toPort;public bool bend;
            public bool Matches(ConveyorLink e){return ReferenceEquals(link,e) && id==e.id && fromId==e.fromId && fromPort==e.fromPort && toId==e.toId && toPort==e.toPort && bend==e.bendXFirst;}
        }
        struct DefinitionStamp
        {
            public EquipmentDefinition definition;public EquipmentKind kind;public float width,depth;
            public bool Matches(EquipmentDefinition e){return ReferenceEquals(definition,e) && kind==e.kind && width==e.width && depth==e.depth;}
        }
        bool LayoutMatches()
        {
            if(equipmentStamps==null || equipmentStamps.Length!=State.equipment.Count || linkStamps.Length!=State.belts.Count || definitionStamps.Length!=Rules.equipment.Length)return false;
            for(int i=0;i<equipmentStamps.Length;i++)if(!equipmentStamps[i].Matches(State.equipment[i]))return false;
            for(int i=0;i<linkStamps.Length;i++)if(!linkStamps[i].Matches(State.belts[i]))return false;
            for(int i=0;i<definitionStamps.Length;i++)if(!definitionStamps[i].Matches(Rules.equipment[i]))return false;
            return true;
        }
        void EnsureLayout()
        {
            if(LayoutMatches())return;
            orderedLinks.Clear();orderedLinks.AddRange(State.belts);orderedLinks.Sort((a,b)=>a.id.CompareTo(b.id));
            orderedEquipment.Clear();orderedEquipment.AddRange(State.equipment);orderedEquipment.Sort((a,b)=>a.id.CompareTo(b.id));
            routeLengths.Clear();inputLinks.Clear();outputLinks.Clear();
            foreach(var equipment in orderedEquipment)
            {inputLinks.Add(equipment.id,new ConveyorLink[PortCount(equipment.kind,false)]);outputLinks.Add(equipment.id,new ConveyorLink[PortCount(equipment.kind,true)]);}
            foreach(var link in orderedLinks)
            {routeLengths.Add(link.id,Length(Path(link,State,Rules)));inputLinks[link.toId][link.toPort]=link;outputLinks[link.fromId][link.fromPort]=link;}
            equipmentStamps=new EquipmentStamp[State.equipment.Count];linkStamps=new LinkStamp[State.belts.Count];definitionStamps=new DefinitionStamp[Rules.equipment.Length];
            for(int i=0;i<equipmentStamps.Length;i++){var e=State.equipment[i];equipmentStamps[i]=new EquipmentStamp{item=e,id=e.id,kind=e.kind,x=e.x,z=e.z,yaw=e.yaw};}
            for(int i=0;i<linkStamps.Length;i++){var e=State.belts[i];linkStamps[i]=new LinkStamp{link=e,id=e.id,fromId=e.fromId,fromPort=e.fromPort,toId=e.toId,toPort=e.toPort,bend=e.bendXFirst};}
            for(int i=0;i<definitionStamps.Length;i++){var e=Rules.equipment[i];definitionStamps[i]=new DefinitionStamp{definition=e,kind=e.kind,width=e.width,depth=e.depth};}
            LayoutBuildCount++;
        }
        public AutomationModel(ScrappingModel model,ConstructionModel construction)
        {
            if(model==null || construction==null || !ReferenceEquals(model.State,construction.State))throw new ArgumentException("Automation requires the same authoritative yard.");
            this.model=model;this.construction=construction;LastMessage="";
        }
        static bool Reject(string message,out string reason){reason=message;return false;}
        bool Reject(string message){LastMessage=message;return false;}
        public static int PortCount(EquipmentKind kind,bool output)
        {
            if(ScrappingModel.IsComponentProcessor(kind))return 1;
            if(kind==EquipmentKind.PrimaryScrapper)return output?1:0;
            if(kind==EquipmentKind.ExportStation)return output?0:1;
            if(kind==EquipmentKind.Storage)return 1;
            if(kind==EquipmentKind.Splitter)return output?3:1;
            if(kind==EquipmentKind.Merger)return output?1:3;
            return 0;
        }
        static CompactPortPoint Direction(EquipmentState e,bool output,int index)
        {
            if(index<0 || index>=PortCount(e.kind,output))throw new ArgumentException("Unsupported conveyor port.");
            float dx=0,dz=output?1:-1;
            // Keep old Tier 1 output links on -Z; the newly added input is opposite.
            if(e.kind==EquipmentKind.Tier1Scrapper)dz=output?-1:1;
            if((e.kind==EquipmentKind.Splitter && output) || (e.kind==EquipmentKind.Merger && !output))
            {
                if(index==0){dx=-1;dz=0;}else if(index==2){dx=1;dz=0;}
            }
            double angle=e.yaw*Math.PI/180;float c=(float)Math.Cos(angle),s=(float)Math.Sin(angle);
            return new CompactPortPoint(dx*c+dz*s,-dx*s+dz*c);
        }
        public static CompactPortPoint Port(EquipmentState e,CompactRules rules,bool output,int index)
        {
            if(e==null || rules==null)throw new ArgumentException("Port equipment is missing.");
            var d=rules.Equipment(e.kind);var direction=Direction(e,output,index);
            bool side=((e.kind==EquipmentKind.Splitter && output) || (e.kind==EquipmentKind.Merger && !output)) && index!=1;
            float radius=(side?d.width:d.depth)*.5f+PortOffset;
            return new CompactPortPoint(e.x+direction.x*radius,e.z+direction.z*radius);
        }
        static EquipmentState Find(CompactYardState state,int id)
        {foreach(var e in state.equipment)if(e.id==id)return e;return null;}
        static float Distance(CompactPortPoint a,CompactPortPoint b)
        {double dx=a.x-b.x,dz=a.z-b.z;return (float)Math.Sqrt(dx*dx+dz*dz);}
        static void Add(List<CompactPortPoint> points,CompactPortPoint p)
        {if(points.Count==0 || Distance(points[points.Count-1],p)>.0001f)points.Add(p);}
        public static CompactPortPoint[] Path(ConveyorLink link,CompactYardState state,CompactRules rules)
        {
            if(link==null || state==null || rules==null)throw new ArgumentException("Conveyor route is missing.");
            var from=Find(state,link.fromId);var to=Find(state,link.toId);
            if(from==null || to==null)throw new ArgumentException("Conveyor endpoint is missing.");
            var a=Port(from,rules,true,link.fromPort);var b=Port(to,rules,false,link.toPort);
            var da=Direction(from,true,link.fromPort);var db=Direction(to,false,link.toPort);
            var sa=new CompactPortPoint(a.x+da.x*StubLength,a.z+da.z*StubLength);
            var sb=new CompactPortPoint(b.x+db.x*StubLength,b.z+db.z*StubLength);
            var points=new List<CompactPortPoint>();Add(points,a);Add(points,sa);
            Add(points,link.bendXFirst?new CompactPortPoint(sb.x,sa.z):new CompactPortPoint(sa.x,sb.z));
            Add(points,sb);Add(points,b);return points.ToArray();
        }
        public static float Length(CompactPortPoint[] points)
        {float length=0;if(points!=null)for(int i=1;i<points.Length;i++)length+=Distance(points[i-1],points[i]);return length;}
        internal static ConstructionModel.Footprint Segment(CompactPortPoint a,CompactPortPoint b)
        {return new ConstructionModel.Footprint((a.x+b.x)*.5f,(a.z+b.z)*.5f,Math.Abs(a.x-b.x)+CorridorWidth,Math.Abs(a.z-b.z)+CorridorWidth,0);}
        static bool QuarterTurn(float yaw)
        {return CompactRules.Finite(yaw) && Math.Abs(yaw/90-(float)Math.Round(yaw/90))<.00001f;}
        static bool LinesCross(CompactPortPoint a,CompactPortPoint b,CompactPortPoint c,CompactPortPoint d)
        {
            bool ah=Math.Abs(a.z-b.z)<.001f,ch=Math.Abs(c.z-d.z)<.001f;
            if(ah==ch)
            {
                if(ah && Math.Abs(a.z-c.z)>.001f)return false;
                if(!ah && Math.Abs(a.x-c.x)>.001f)return false;
                float a0=ah?a.x:a.z,a1=ah?b.x:b.z,c0=ah?c.x:c.z,c1=ah?d.x:d.z;
                return Math.Min(Math.Max(a0,a1),Math.Max(c0,c1))>=Math.Max(Math.Min(a0,a1),Math.Min(c0,c1))-.0001f;
            }
            if(!ah)return LinesCross(c,d,a,b);
            return c.x>=Math.Min(a.x,b.x)-.0001f && c.x<=Math.Max(a.x,b.x)+.0001f && a.z>=Math.Min(c.z,d.z)-.0001f && a.z<=Math.Max(c.z,d.z)+.0001f;
        }
        static bool Geometry(ConveyorLink link,CompactYardState state,CompactRules rules,bool checkLoose,out string reason)
        {
            var points=Path(link,state,rules);
            for(int i=1;i<points.Length;i++)
            {
                var a=points[i-1];var b=points[i];
                if(!CompactRules.Finite(a.x) || !CompactRules.Finite(a.z) || !CompactRules.Finite(b.x) || !CompactRules.Finite(b.z) ||
                    (Math.Abs(a.x-b.x)>.001f && Math.Abs(a.z-b.z)>.001f))return Reject("Rotate conveyor equipment to quarter turns before connecting.",out reason);
                var corridor=Segment(a,b);
                if(!corridor.InBounds())return Reject("The entire conveyor must stay inside the fence.",out reason);
                if(ConstructionModel.Protected(corridor,out reason))return false;
                if(i>1)
                {
                    var previous=points[i-2];float dx=a.x-previous.x,dz=a.z-previous.z,nx=b.x-a.x,nz=b.z-a.z;
                    if(Math.Abs(dx*nz-dz*nx)<.0001f && dx*nx+dz*nz<0)return Reject("Conveyor doubles back on its own stub; try the other elbow.",out reason);
                }
                for(int earlier=1;earlier<i-1;earlier++)if(LinesCross(points[earlier-1],points[earlier],a,b))
                    return Reject("Conveyor intersects itself; try the other elbow.",out reason);
                foreach(var equipment in state.equipment)
                {
                    // Only the outward collar may enter its own endpoint by the rail thickness.
                    if((i==1 && equipment.id==link.fromId) || (i==points.Length-1 && equipment.id==link.toId))continue;
                    var d=rules.Equipment(equipment.kind);
                    if(corridor.Overlaps(new ConstructionModel.Footprint(equipment.x,equipment.z,d.width,d.depth,equipment.yaw)))
                        return Reject("Conveyor crosses "+d.name+"; try the other elbow or move equipment.",out reason);
                }
                foreach(var scrap in state.scrap)
                    if(corridor.Overlaps(new ConstructionModel.Footprint(scrap.x,scrap.z,scrap.kind==ScrapObjectKind.Car?2.4f:1.2f,scrap.kind==ScrapObjectKind.Car?4.7f:1.2f,0)))
                        return Reject("Conveyor crosses a dismantling object.",out reason);
                if(checkLoose)foreach(var item in state.items)
                    if(item.id!=state.carriedId && corridor.Overlaps(new ConstructionModel.Footprint(item.x,item.z,.8f,.8f,0)))
                        return Reject("Move the loose component before routing here.",out reason);
                if(state.belts!=null)foreach(var other in state.belts)
                {
                    if(other==null)return Reject("Existing conveyor metadata is invalid.",out reason);
                    if(other.id==link.id)continue;
                    var otherPoints=Path(other,state,rules);
                    for(int j=1;j<otherPoints.Length;j++)if(corridor.Overlaps(Segment(otherPoints[j-1],otherPoints[j])))
                        return Reject("Conveyors cannot cross or share track; use a splitter or merger.",out reason);
                }
            }
            reason="Clear conveyor route";return true;
        }
        static bool DestinationSupports(EquipmentState destination,CompactRules rules,PartKind kind)
        {
            return ScrappingModel.SupportsBufferedInput(destination,rules,kind);
        }
        public bool CanConnect(int fromId,int fromPort,int toId,int toPort,bool xFirst,out string reason)
        {
            var from=construction.Find(fromId);var to=construction.Find(toId);var d=Rules.Equipment(EquipmentKind.Conveyor);
            if(from==null || to==null || fromId==toId || fromPort<0 || fromPort>=PortCount(from.kind,true) || toPort<0 || toPort>=PortCount(to.kind,false))
                return Reject("Choose a different equipment output and compatible input port.",out reason);
            if(!QuarterTurn(from.yaw) || !QuarterTurn(to.yaw))return Reject("Rotate both endpoints to quarter turns before connecting.",out reason);
            if(!d.available || model.Level<d.unlockLevel)return Reject("Conveyors require level "+d.unlockLevel+".",out reason);
            if(State.belts!=null)
            {
                if(State.belts.Count>=Math.Min(MaximumBelts,Rules.maxBelts))return Reject("Conveyor limit reached.",out reason);
                foreach(var link in State.belts)if((link.fromId==fromId && link.fromPort==fromPort) || (link.toId==toId && link.toPort==toPort))
                    return Reject("This port already has a conveyor; use another junction port.",out reason);
            }
            if(!ScrappingModel.IsComponentProcessor(from.kind) && from.filterKind>=0 && !DestinationSupports(to,Rules,(PartKind)from.filterKind))return Reject("The selected source filter has no compatible destination recipe; choose storage or change filters.",out reason);
            var candidate=new ConveyorLink{fromId=fromId,fromPort=fromPort,toId=toId,toPort=toPort,bendXFirst=xFirst};
            float length=Length(Path(candidate,State,Rules));
            if(!CompactRules.Finite(length) || length<=.05f || length>Rules.beltMaxLength)return Reject("Conveyor exceeds the "+Rules.beltMaxLength+"m route limit.",out reason);
            if(!Geometry(candidate,State,Rules,true,out reason))return false;
            long cost=(long)Math.Ceiling(length/2)*d.price;
            if(cost>MaximumPaidPrice)return Reject("Conveyor cost exceeds the durable purchase limit; reduce catalogue price or route length.",out reason);
            if(State.money<cost)return Reject("Not enough money: conveyor costs €"+cost+".",out reason);
            if(State.nextId<=0 || State.nextId==int.MaxValue)return Reject("No safe conveyor identity available.",out reason);
            reason="Conveyor "+length.ToString("0.0")+"m — €"+cost;return true;
        }
        public bool Connect(int fromId,int fromPort,int toId,int toPort,bool xFirst)
        {
            string reason;if(!CanConnect(fromId,fromPort,toId,toPort,xFirst,out reason))return Reject(reason);
            var link=new ConveyorLink{id=State.nextId,fromId=fromId,fromPort=fromPort,toId=toId,toPort=toPort,bendXFirst=xFirst};
            link.paidPrice=(int)((long)Math.Ceiling(Length(Path(link,State,Rules))/2)*Rules.Equipment(EquipmentKind.Conveyor).price);
            if(State.belts==null)State.belts=new List<ConveyorLink>();
            State.money-=link.paidPrice;State.nextId++;State.belts.Add(link);LastConnectedId=link.id;
            LastMessage="Conveyor connected for €"+link.paidPrice+".";return true;
        }
        ConveyorLink FindLink(int id){if(State.belts!=null)foreach(var link in State.belts)if(link.id==id)return link;return null;}
        public bool CanRemove(int linkId,out string reason)
        {
            var link=FindLink(linkId);if(link==null)return Reject("Conveyor no longer exists.",out reason);
            if(link.items==null || link.items.Count>0)return Reject("Conveyor carries items; empty it before dismantling.",out reason);
            if((long)State.money+link.paidPrice/2>int.MaxValue)return Reject("Cash limit prevents this refund.",out reason);
            reason="Disconnect empty conveyor; refund €"+(link.paidPrice/2)+".";return true;
        }
        public bool Remove(int linkId)
        {
            string reason;if(!CanRemove(linkId,out reason))return Reject(reason);
            var link=FindLink(linkId);State.money+=link.paidPrice/2;State.belts.Remove(link);LastMessage=reason;return true;
        }
        bool Receive(ConveyorLink link)
        {
            if(link.items.Count==0 || link.items[0].progress<1)return false;
            var destination=construction.Find(link.toId);var item=link.items[0];
            if(destination==null || !DestinationSupports(destination,Rules,item.kind) || model.IntakeCapacityUnits(destination.id)>=Rules.Equipment(destination.kind).outputCapacity)return false;
            destination.contents.Add(new CompactStack{id=item.id,kind=item.kind,quantity=1,x=destination.x,z=destination.z,xpEligible=item.xpEligible});
            link.items.RemoveAt(0);return true;
        }
        bool BeltReadyToLaunch(ConveyorLink link,float length,out string reason)
        {
            if(link.launchRemaining>0)return Reject("OUT waiting for belt spacing",out reason);
            if(link.items.Count>=Math.Min(MaximumItemsPerBelt,Rules.beltCapacity))return Reject("OUT waiting for room on the belt",out reason);
            if(link.items.Count>0 && link.items[link.items.Count-1].progress*length<Rules.beltSpacing-.00001f)
                return Reject("OUT waiting for belt spacing",out reason);
            reason="";return true;
        }
        bool SelectLaunchOutput(EquipmentState source,EquipmentState destination,out CompactStack stack,out PartAmount output,out string reason)
        {
            stack=null;output=null;
            if(ScrappingModel.IsComponentProcessor(source.kind))
            {
                if(source.job==null)return Reject("OUT awaiting a processed batch",out reason);
                if(!source.job.ready)return Reject(source.kind==EquipmentKind.Workbench?"OUT waiting for manual work":"OUT waiting for processing",out reason);
                // Machine recipe filters select inputs, never suppress the resulting materials.
                foreach(var amount in source.job.yields)if(amount.quantity>0 && DestinationSupports(destination,Rules,amount.kind)){output=amount;break;}
                if(output==null)return Reject("IN cannot accept the remaining recovered output; review its filter or input role",out reason);
            }
            else
            {
                bool matching=false;
                foreach(var candidate in source.contents)
                {
                    if(source.filterKind>=0 && source.filterKind!=(int)candidate.kind)continue;
                    matching=true;
                    if(DestinationSupports(destination,Rules,candidate.kind)){stack=candidate;break;}
                }
                if(stack==null)
                {
                    if(source.contents.Count==0)return Reject("OUT empty",out reason);
                    return Reject(matching?"IN cannot accept the selected stock; review its filter or input role":"OUT filter holds the stored items",out reason);
                }
            }
            reason="";return true;
        }
        bool CanAllocateTransit(CompactStack stack,PartAmount output,out string reason)
        {
            int additional=stack!=null?(stack.quantity==1?0:1):(output.quantity==1?0:1);
            if(additional>0 && (model.OccupiedSlots+additional>Rules.maxStacks || model.OccupiedSlots+additional>512))
                return Reject("OUT waiting for inventory space; collect and sell existing stock",out reason);
            if(!(stack!=null && stack.quantity==1) && (State.nextId<=0 || State.nextId==int.MaxValue))
                return Reject("OUT waiting for safe item identities; stock retained",out reason);
            reason="";return true;
        }
        /// <summary>Read-only operating feedback. Queries neither advance cargo nor change action
        /// notices, saved state or the simulation's layout cache.</summary>
        public string FlowStatus(int beltId)
        {
            var link=FindLink(beltId);if(link==null)return "Conveyor unavailable.";
            var source=construction.Find(link.fromId);var destination=construction.Find(link.toId);
            if(source==null || destination==null)return "Conveyor endpoint unavailable; cargo retained.";
            if(link.fromPort<0 || link.fromPort>=PortCount(source.kind,true) || link.toPort<0 || link.toPort>=PortCount(destination.kind,false))
                return "Conveyor endpoint port unavailable; cargo retained.";
            if(link.items==null)return "Conveyor contents unavailable.";
            var head=link.items.Count>0?link.items[0]:null;
            if(link.items.Count>0 && head==null)return "Conveyor cargo unavailable.";
            if(head!=null)
            {
                string intake=null;
                if(!DestinationSupports(destination,Rules,head.kind))intake="IN cannot accept "+Rules.Part(head.kind).name+"; review its filter or input role";
                else if(model.IntakeCapacityUnits(destination.id)>=Rules.Equipment(destination.kind).outputCapacity)intake="IN full";
                if(intake!=null)return head.progress>=1?"Stopped at IN / "+intake+". Cargo retained.":"Items travelling / "+intake+"; will wait at IN.";
                if(head.progress>=1)return "Ready at IN / "+link.items.Count+" item(s) on belt.";
            }
            string moving=head==null?"":"Items travelling / ";
            CompactStack stack;PartAmount output;string reason;
            if(!SelectLaunchOutput(source,destination,out stack,out output,out reason))
            {
                if((source.kind==EquipmentKind.Tier1Scrapper || source.kind==EquipmentKind.Tier2Scrapper) && source.job!=null && !source.job.ready)
                {
                    string processing=model.ProcessingBlockReason(source.id);
                    if(processing!="Processing")return moving+"OUT waiting / "+processing+".";
                }
                if(source.kind==EquipmentKind.PrimaryScrapper && source.contents.Count==0 && source.industry!=null && source.industry.primary!=null)
                    return moving+"OUT waiting / "+model.Industry.Status(source.id);
                return moving+reason+".";
            }
            float length=Length(Path(link,State,Rules));
            if(!BeltReadyToLaunch(link,length,out reason))return moving+reason+".";
            if(!CanAllocateTransit(stack,output,out reason))return moving+reason+".";
            if(model.IntakeCapacityUnits(destination.id)>=Rules.Equipment(destination.kind).outputCapacity)
                return "IN full / incoming items will wait at IN.";
            return head==null?"OUT ready to feed IN.":"Items travelling / OUT ready to feed IN.";
        }
        bool Launch(EquipmentState source,ConveyorLink link,float length)
        {
            string reason;if(!BeltReadyToLaunch(link,length,out reason))return false;
            var destination=construction.Find(link.toId);if(destination==null)return false;
            CompactStack stack;PartAmount output;
            if(!SelectLaunchOutput(source,destination,out stack,out output,out reason) || !CanAllocateTransit(stack,output,out reason))return false;
            bool reuse=stack!=null && stack.quantity==1;
            var transit=new ConveyorItem{id=reuse?stack.id:State.nextId,kind=stack!=null?stack.kind:output.kind,quantity=1,
                xpEligible=stack!=null?stack.xpEligible:source.job.xpEligible,progress=0};
            if(!reuse)State.nextId++;
            if(stack!=null){stack.quantity--;if(stack.quantity==0)source.contents.Remove(stack);}
            else
            {
                output.quantity--;bool empty=true;foreach(var y in source.job.yields)if(y.quantity>0){empty=false;break;}
                if(empty)source.job=null;
            }
            link.items.Add(transit);link.launchRemaining=Rules.beltSpacing/Rules.beltSpeed;return true;
        }
        public bool Tick(float delta)
        {
            if(!CompactRules.Finite(delta) || delta<0)throw new ArgumentOutOfRangeException("delta");
            if(delta==0 || State.belts==null)return false;
            EnsureLayout();bool changed=false;
            // Move only items present at frame start. A new transfer cannot inherit earlier elapsed time.
            foreach(var link in orderedLinks)
            {
                float length=routeLengths[link.id];
                float old=link.launchRemaining;link.launchRemaining=Math.Max(0,old-delta);changed|=old!=link.launchRemaining;
                float ceiling=1;
                foreach(var item in link.items)
                {
                    float previous=item.progress;
                    item.progress=Math.Max(previous,Math.Min(ceiling,previous+delta*Rules.beltSpeed/length));
                    ceiling=item.progress-Rules.beltSpacing/length;changed|=previous!=item.progress;
                }
            }
            // Merger input rotates fairly, even when one free slot repeatedly becomes available.
            foreach(var destination in orderedEquipment)
            {
                int ports=PortCount(destination.kind,false);if(ports==0)continue;
                int start=destination.kind==EquipmentKind.Merger?destination.routeCursor%ports:0;
                for(int offset=0;offset<ports;offset++)
                {
                    int port=(start+offset)%ports;
                    var link=inputLinks[destination.id][port];
                    if(link!=null && Receive(link)){changed=true;if(destination.kind==EquipmentKind.Merger)destination.routeCursor=(port+1)%ports;}
                }
            }
            foreach(var source in orderedEquipment)
            {
                int ports=PortCount(source.kind,true);if(ports==0)continue;
                int start=source.kind==EquipmentKind.Splitter?source.routeCursor%ports:0;
                for(int offset=0;offset<ports;offset++)
                {
                    int port=(start+offset)%ports;
                    var link=outputLinks[source.id][port];
                    if(link!=null && Launch(source,link,routeLengths[link.id])){changed=true;if(source.kind==EquipmentKind.Splitter)source.routeCursor=(port+1)%ports;}
                }
            }
            if(model.PrepareBufferedJobs())changed=true;
            return changed;
        }
        public static void Validate(CompactYardState state,CompactRules rules)
        {
            if(state==null || rules==null || state.equipment==null || state.scrap==null || state.items==null)throw new ArgumentException("Missing automation state.");
            if(state.belts==null){if(state.version==2)return;throw new ArgumentException("Missing conveyor collection.");}
            if(state.belts.Count>MaximumBelts)throw new ArgumentException("Too many conveyors.");
            var identities=new HashSet<int>();var outputs=new HashSet<string>();var inputs=new HashSet<string>();
            foreach(var link in state.belts)
            {
                if(link==null || link.id<=0 || link.id>=state.nextId || !identities.Add(link.id) || link.fromId==link.toId || link.paidPrice<0 || link.paidPrice>MaximumPaidPrice ||
                    !CompactRules.Finite(link.launchRemaining) || link.launchRemaining<0 || link.launchRemaining>30 || link.items==null || link.items.Count>MaximumItemsPerBelt)
                    throw new ArgumentException("Invalid conveyor metadata.");
                var source=Find(state,link.fromId);var destination=Find(state,link.toId);
                if(source==null || destination==null || !QuarterTurn(source.yaw) || !QuarterTurn(destination.yaw) ||
                    link.fromPort<0 || link.fromPort>=PortCount(source.kind,true) || link.toPort<0 || link.toPort>=PortCount(destination.kind,false) ||
                    !outputs.Add(link.fromId+":"+link.fromPort) || !inputs.Add(link.toId+":"+link.toPort))throw new ArgumentException("Invalid or occupied conveyor ports.");
                float length=Length(Path(link,state,rules));string reason;
                if(!CompactRules.Finite(length) || length<=.05f || length>60 || !Geometry(link,state,rules,false,out reason))throw new ArgumentException("Invalid conveyor geometry.");
                float preceding=2;
                foreach(var item in link.items)
                {
                    if(item==null || item.id<=0 || item.id>=state.nextId || !identities.Add(item.id) || !Enum.IsDefined(typeof(PartKind),item.kind) ||
                        item.quantity!=1 || !CompactRules.Finite(item.progress) || item.progress<0 || item.progress>1 || preceding-item.progress<=.000001f)
                        throw new ArgumentException("Invalid conveyor item or ordering.");
                    preceding=item.progress;
                }
            }
        }
    }
}
