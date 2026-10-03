using System;

namespace Scrapshift.Compact
{
    public struct CompactGuideServices
    {public YardDestination sales,shop,delivery,wire;}
    public sealed class CompactGuideStep
    {public string action;public YardDestination destination;public bool hasDestination;}

    // Guidance reads the live yard. It never advances jobs, awards progress or reserves inventory.
    public static class CompactGuidance
    {
        public static CompactGuideStep Resolve(ScrappingModel model,float playerX,float playerZ,CompactGuideServices services,string interactLabel,string workLabel)
        {
            if(model==null)throw new ArgumentNullException("model");
            string use="["+interactLabel+"]",work="["+workLabel+"]";
            var held=model.Carried;
            if(held!=null)return Carry(model,held,playerX,playerZ,services,use,work);
            var ready=ReadyOutput(model,playerX,playerZ,use);if(ready!=null)return ready;
            var manual=NearestEquipment(model,playerX,playerZ,e=>e.kind==EquipmentKind.Workbench && e.job!=null && !e.job.ready);
            if(manual!=null)return At(manual,model,"Use "+work+" to finish "+RecipeName(model,manual.job)+" • "+manual.job.strokes+"/"+manual.job.requiredStrokes+" strokes.");
            var blocked=NearestEquipment(model,playerX,playerZ,e=>e.job!=null && !e.job.ready && ScrappingModel.HasBuffer(e.kind) &&
                model.StoredUnits(e.id)+model.OutputQuantity(e.id)>model.Rules.Equipment(e.kind).outputCapacity);
            if(blocked!=null)return At(blocked,model,"Output blocked. Inspect "+use+" and withdraw stored materials to make room; processing progress is retained.");
            var goal=model.Career.CurrentGoal;
            if(goal!=null)
            {
                if(goal.key=="inspect")
                {
                    var scrap=NearestScrap(model,playerX,playerZ,s=>!s.inspected && CanInspect(model,s));
                    if(scrap!=null)return ScrapStep(model,scrap,use,work);
                    return Acquire(model,playerX,playerZ,services,use,work,null,"Inspect your next delivery");
                }
                if(goal.key=="dismantle")
                {
                    var scrap=NearestScrap(model,playerX,playerZ,s=>s.inspected && s.strokes<s.requiredStrokes);
                    if(scrap!=null)return ScrapStep(model,scrap,use,work);
                }
                if(goal.key=="power")return EstablishPower(model,playerX,playerZ,services,use,work);
                if(goal.key=="powered")
                {
                    var machine=NearestEquipment(model,playerX,playerZ,e=>e.kind==EquipmentKind.Tier1Scrapper);
                    if(machine==null || !Powered(model,machine))return EstablishPower(model,playerX,playerZ,services,use,work);
                    if(machine.job!=null)return Running(machine,model);
                    var input=NearestItem(model,playerX,playerZ,i=>model.Rules.Recipe(i.kind)!=null && CanLoad(model,machine,i));
                    if(input!=null)return ItemStep(model,input,use,"Feed the powered Tier 1 scrapper next");
                    return Acquire(model,playerX,playerZ,services,use,work,null,"Recover a component for your powered scrapper");
                }
                if(goal.key=="level10")return Acquire(model,playerX,playerZ,services,use,work,null,"Sell recovered materials for XP • "+model.XPToNextLevel+" XP to level "+(model.Level+1));
                if(goal.key=="belts")return FirstBelt(model,playerX,playerZ,services,use,work);
                if(goal.key=="contracts")
                {
                    var request=model.Career.CurrentContract;
                    if(request!=null && model.Level>=request.minimumLevel)
                        return Acquire(model,playerX,playerZ,services,use,work,request.kind,"Recover "+model.Rules.Part(request.kind).name+" for "+request.customer+" • "+request.Remaining+" needed");
                    return Acquire(model,playerX,playerZ,services,use,work,null,"Sell recovered materials to unlock your next customer");
                }
            }
            return Acquire(model,playerX,playerZ,services,use,work,null,goal==null?"Your yard is open • keep recovering and selling":"Continue your recovery work");
        }
        static CompactGuideStep Carry(ScrappingModel model,CompactStack held,float x,float z,CompactGuideServices services,string use,string work)
        {
            var request=model.Career.ContractQuote();
            if(request.allowed)return At(services.sales,"At the sales counter "+use+", deliver "+model.Rules.Part(held.kind).name+" ×"+request.quantity+" to "+request.customer+" • €"+request.total+" / +"+request.experience+" XP.");
            var sale=model.SaleQuote();
            if(sale.allowed)return At(services.sales,"At the sales counter "+use+", sell "+sale.name+" ×"+sale.quantity+" • €"+sale.total+" / +"+sale.experience+" XP"+(held.xpEligible?".":" • imported stock earns no new recovery XP."));
            if(model.Rules.Part(held.kind).isMaterial || held.kind==PartKind.RestoredFan || held.kind==PartKind.RestoredRadio)
                return Message(sale.reason);
            var recipe=model.Rules.Recipe(held.kind);
            if(recipe==null)return Message("This item has no component recipe. Store it or put it down to continue working.");
            var equipment=NearestEquipment(model,x,z,e=>CanLoad(model,e,held));
            if(equipment!=null)return At(equipment,model,"At "+model.Rules.Equipment(equipment.kind).name+" "+use+", "+(equipment.kind==EquipmentKind.Tier2Scrapper?"deposit":"load")+" "+model.Rules.Part(held.kind).name+" ×"+held.quantity+" to recover materials.");
            var occupied=NearestEquipment(model,x,z,e=>e.kind==EquipmentKind.Workbench && e.job!=null);
            if(occupied!=null)return At(occupied,model,"Put down your carried component, then "+(occupied.job.ready?"collect the workbench output "+use:"finish the workbench job "+work+" with empty hands")+" before loading another batch.");
            var machine=NearestEquipment(model,x,z,e=>(e.kind==EquipmentKind.Tier1Scrapper || e.kind==EquipmentKind.Tier2Scrapper) && !Powered(model,e));
            if(machine!=null)return At(machine,model,"Put down your component, then inspect "+use+" to restore generator power. Your carried item is retained.");
            return Message("This batch needs compatible input quantities and enough free output space. Store it or put it down, then collect and sell existing materials.");
        }
        static CompactGuideStep ReadyOutput(ScrappingModel model,float x,float z,string use)
        {
            var machine=NearestEquipment(model,x,z,e=>e.job!=null && e.job.ready && Positive(e.job.yields));
            if(machine!=null)
            {
                if(model.State.nextId==int.MaxValue)return Message("Recovered output is retained safely: the item identifier limit prevents collection.");
                return At(machine,model,"Collect recovered materials "+use+" from "+model.Rules.Equipment(machine.kind).name+", then take them to sales.");
            }
            var scrap=NearestScrap(model,x,z,s=>s.inspected && s.strokes>=s.requiredStrokes && Positive(s.remaining));
            if(scrap!=null)
            {
                if(model.State.nextId==int.MaxValue)return Message("Dismantled components are retained safely: the item identifier limit prevents collection.");
                return At(Destination(scrap),"Remove a finished "+model.Rules.LargeRecipe(scrap.kind).name+" component "+use+"; process components or sell recovered materials.");
            }
            return null;
        }
        static CompactGuideStep Acquire(ScrappingModel model,float x,float z,CompactGuideServices services,string use,string work,PartKind? desired,string purpose)
        {
            var item=NearestItem(model,x,z,i=>model.Rules.Part(i.kind).unitPrice>0 && (!desired.HasValue || i.kind==desired.Value));
            if(item!=null)return ItemStep(model,item,use,purpose);
            item=NearestItem(model,x,z,i=>RecipeSupplies(model,i.kind,desired) && ValidBatch(model,i));
            if(item!=null)return ItemStep(model,item,use,purpose);
            var stored=NearestEquipment(model,x,z,e=>ScrappingModel.HasBuffer(e.kind) && UsefulStored(model,e,desired));
            if(stored!=null)return At(stored,model,purpose+" • withdraw a matching material or component "+use+" from "+model.Rules.Equipment(stored.kind).name+".");
            var scrap=NearestScrap(model,x,z,s=>ScrapSupplies(model,s.kind,desired) && (s.inspected || CanInspect(model,s)));
            if(scrap!=null)return ScrapStep(model,scrap,use,work);
            if(!desired.HasValue || RecipeSupplies(model,PartKind.Wire,desired))
            {
                if(CanAcquireWire(model))return At(services.wire,purpose+" • collect free wiring "+use+", then strip it at your workbench.");
                return CapacityRecovery(model,x,z,use);
            }
            LargeScrapRecipe affordable=null;
            foreach(var candidate in model.Rules.largeRecipes)
                if(ScrapSupplies(model,candidate.kind,desired) && CanBuyScrap(model,candidate) && (affordable==null || candidate.purchasePrice<affordable.purchasePrice))affordable=candidate;
            if(affordable!=null)return At(services.delivery,purpose+" • order a "+affordable.name+" for €"+affordable.purchasePrice+" "+use+", then inspect and dismantle it.");
            if(CanAcquireWire(model))return At(services.wire,"Earn delivery money first: collect free wiring "+use+", strip it and sell the copper. Wiring cannot supply "+model.Rules.Part(desired.Value).name+".");
            return CapacityRecovery(model,x,z,use);
        }
        static CompactGuideStep EstablishPower(ScrappingModel model,float x,float z,CompactGuideServices services,string use,string work)
        {
            var machine=NearestEquipment(model,x,z,e=>e.kind==EquipmentKind.Tier1Scrapper);
            if(machine==null)return BuyEquipment(model,EquipmentKind.Tier1Scrapper,x,z,services,use,work);
            if(Powered(model,machine))return At(machine,model,"Generator power is ready. Load a supported component "+use+" to let your Tier 1 scrapper work.");
            var power=new ConstructionModel(model.State,model.Rules).PowerFor(machine.id);
            if(power.powered)return At(machine,model,"Generator power is ready. Load a supported component "+use+" to let your Tier 1 scrapper work.");
            if(power.overloaded)return At(machine,model,"Power network overloaded: "+power.demand.ToString("0.##")+" / "+power.supply.ToString("0.##")+" kW. Inspect "+use+" and disconnect idle machines or connect more generator supply.");
            var generator=NearestEquipment(model,machine.x,machine.z,e=>model.Rules.Equipment(e.kind).powerOutput>0);
            if(generator==null)return BuyEquipment(model,EquipmentKind.Generator,x,z,services,use,work);
            if(Destination(generator).Distance(machine.x,machine.z)>model.Rules.CableRange)
                return At(machine,model,"Generator is beyond "+model.Rules.CableRange+"m cable reach. Empty and disconnect equipment before moving it closer, or place a generator nearby.");
            return At(machine,model,"Inspect "+use+" and connect the nearby "+model.Rules.Equipment(generator.kind).name+" in the Power network panel.");
        }
        static CompactGuideStep BuyEquipment(ScrappingModel model,EquipmentKind kind,float x,float z,CompactGuideServices services,string use,string work)
        {
            var definition=model.Rules.Equipment(kind);
            if(!definition.available)return Acquire(model,x,z,services,use,work,null,"This equipment is unavailable in your catalogue; manual recovery remains available");
            if(model.Level<definition.unlockLevel)return Acquire(model,x,z,services,use,work,null,"Sell recovered materials to unlock "+definition.name+" at level "+definition.unlockLevel);
            if(model.State.money<definition.price)return Acquire(model,x,z,services,use,work,null,"Earn €"+(definition.price-model.State.money)+" more for "+definition.name);
            if(model.State.equipment.Count>=Math.Min(model.Rules.maxEquipment,ConstructionModel.MaximumEquipment) || model.State.nextId==int.MaxValue)
                return Message("Equipment limit reached. Empty and remove unused equipment before expanding the yard.");
            return At(services.shop,"Open the yard shop "+use+" and buy/place "+definition.name+" • €"+definition.price+". Choose a clear position inside the yard.");
        }
        static CompactGuideStep FirstBelt(ScrappingModel model,float x,float z,CompactGuideServices services,string use,string work)
        {
            var storage=NearestEquipment(model,x,z,e=>e.kind==EquipmentKind.Storage);
            if(storage==null)return BuyEquipment(model,EquipmentKind.Storage,x,z,services,use,work);
            var conveyor=model.Rules.Equipment(EquipmentKind.Conveyor);
            if(!conveyor.available)return Acquire(model,x,z,services,use,work,null,"Conveyors are unavailable in your catalogue; keep recovering materials manually");
            if(model.Level<conveyor.unlockLevel)return Acquire(model,x,z,services,use,work,null,"Reach level "+conveyor.unlockLevel+" for conveyors");
            if(model.State.money<conveyor.price)return Acquire(model,x,z,services,use,work,null,"Earn €"+(conveyor.price-model.State.money)+" more for your first conveyor section");
            var source=NearestEquipment(model,storage.x,storage.z,e=>e.id!=storage.id && AutomationModel.PortCount(e.kind,true)>0);
            if(source==null)return BuyEquipment(model,EquipmentKind.Tier1Scrapper,x,z,services,use,work);
            return At(source,model,"Inspect "+use+" and select a free output port; aim at the storage input to preview a conveyor. Its route must be clear and within "+model.Rules.beltMaxLength+"m; check the price before confirming.");
        }
        static CompactGuideStep Running(EquipmentState e,ScrappingModel model)
        {
            return At(e,model,"Tier 1 is processing • "+e.job.remaining.ToString("0.0")+"s of powered work left. Collect its output when ready.");
        }
        static CompactGuideStep CapacityRecovery(ScrappingModel model,float x,float z,string use)
        {
            var item=NearestItem(model,x,z,i=>model.Rules.Part(i.kind).unitPrice>0);
            if(item!=null)return ItemStep(model,item,use,"Make room by selling this existing item");
            return Message("Keep space for every recipe output. Finish existing jobs and collect/sell stored materials before taking more wiring.");
        }
        static bool CanLoad(ScrappingModel model,EquipmentState e,CompactStack stack)
        {
            if(e.kind!=EquipmentKind.Workbench && e.kind!=EquipmentKind.Tier1Scrapper && e.kind!=EquipmentKind.Tier2Scrapper)return false;
            var recipe=model.Rules.Recipe(stack.kind);if(recipe==null || !ValidBatch(model,stack))return false;
            if(e.kind!=EquipmentKind.Workbench && !Powered(model,e))return false;
            var definition=model.Rules.Equipment(e.kind);
            if(e.kind==EquipmentKind.Tier2Scrapper)
            {
                if(e.job!=null || (e.filterKind>=0 && e.filterKind!=(int)stack.kind) ||
                    (long)model.StoredUnits(e.id)+stack.quantity>definition.outputCapacity)return false;
                int output=0;foreach(var yield in recipe.yields)output+=yield.quantity;
                if((long)model.StoredUnits(e.id)+stack.quantity-recipe.inputQuantity+output>definition.outputCapacity ||
                    (long)model.State.nextId+output>int.MaxValue)return false;
                int removed=0,left=recipe.inputQuantity;
                foreach(var input in e.contents)
                    if(input.kind==stack.kind && input.xpEligible==stack.xpEligible && left>0)
                    {int taken=Math.Min(left,input.quantity);left-=taken;if(taken==input.quantity)removed++;}
                if(left>0 && left==stack.quantity)removed++;
                return CanReserve(model,recipe.yields.Length-removed);
            }
            if(e.job!=null)return false;
            int batches=stack.quantity/recipe.inputQuantity;long units=model.StoredUnits(e.id);
            foreach(var y in recipe.yields){long quantity=(long)y.quantity*batches;if(quantity>4096)return false;units+=quantity;}
            double duration=(double)recipe.seconds*batches*definition.processingSeconds/model.Rules.baseMachineSeconds;
            return units<=definition.outputCapacity && CanReserve(model,recipe.yields.Length-1) &&
                (long)recipe.strokes*batches<=10000 && duration>0 && duration<=86400 && !double.IsNaN(duration) && !double.IsInfinity(duration);
        }
        static bool ValidBatch(ScrappingModel model,CompactStack stack)
        {var r=model.Rules.Recipe(stack.kind);return r!=null && stack.quantity>=r.inputQuantity && stack.quantity%r.inputQuantity==0;}
        static bool CanReserve(ScrappingModel model,int extra)
        {return model.OccupiedSlots+extra<=512 && (extra<=0 || model.OccupiedSlots+extra<=model.Rules.maxStacks);}
        static bool CanInspect(ScrappingModel model,LargeScrapJob scrap)
        {return CanReserve(model,model.Rules.LargeRecipe(scrap.kind).yields.Length);}
        static bool CanAcquireWire(ScrappingModel model)
        {return model.State.nextId<int.MaxValue && CanReserve(model,Math.Max(1,model.Rules.Recipe(PartKind.Wire).yields.Length));}
        static bool CanBuyScrap(ScrappingModel model,LargeScrapRecipe recipe)
        {
            if(model.State.money<recipe.purchasePrice || model.State.nextId==int.MaxValue || model.State.scrap.Count>=Math.Min(model.Rules.maxLargeScrap,16))return false;
            foreach(var existing in model.State.scrap)if(existing.kind==recipe.kind)return false;
            float x=recipe.kind==ScrapObjectKind.Car?18:20,z=recipe.kind==ScrapObjectKind.Car?-10:-5;
            float halfX=recipe.kind==ScrapObjectKind.Car?1.4f:.8f,halfZ=recipe.kind==ScrapObjectKind.Car?2.5f:.8f;
            foreach(var item in model.State.items)if(Math.Abs(item.x-x)<halfX+.5f && Math.Abs(item.z-z)<halfZ+.5f)return false;
            return true;
        }
        static bool RecipeSupplies(ScrappingModel model,PartKind input,PartKind? desired)
        {
            var recipe=model.Rules.Recipe(input);if(recipe==null)return false;
            if(!desired.HasValue)return true;
            foreach(var yield in recipe.yields)if(yield.kind==desired.Value && yield.quantity>0)return true;
            return false;
        }
        static bool ScrapSupplies(ScrappingModel model,ScrapObjectKind kind,PartKind? desired)
        {
            if(!desired.HasValue)return true;
            foreach(var yield in model.Rules.LargeRecipe(kind).yields)
                if(yield.kind==desired.Value || RecipeSupplies(model,yield.kind,desired))return true;
            return false;
        }
        static bool UsefulStored(ScrappingModel model,EquipmentState e,PartKind? desired)
        {foreach(var item in e.contents)if((model.Rules.Part(item.kind).unitPrice>0 && (!desired.HasValue || item.kind==desired.Value)) || RecipeSupplies(model,item.kind,desired))return true;return false;}
        static bool Powered(ScrappingModel model,EquipmentState e)
        {return model.HasPower!=null && model.HasPower(e.id);}
        static bool Positive(PartAmount[] yields)
        {if(yields!=null)foreach(var yield in yields)if(yield.quantity>0)return true;return false;}
        static string RecipeName(ScrappingModel model,ProcessingJob job)
        {var recipe=model.Rules.Recipe(job.input);return recipe==null?model.Rules.Part(job.input).name:recipe.name;}
        static CompactGuideStep ScrapStep(ScrappingModel model,LargeScrapJob scrap,string use,string work)
        {
            string name=model.Rules.LargeRecipe(scrap.kind).name;
            return At(Destination(scrap),scrap.inspected?"Dismantle "+name+" with "+work+" • "+model.ScrapWorkStage(scrap.id)+" • "+scrap.strokes+"/"+scrap.requiredStrokes+".":"Inspect the delivered "+name+" "+use+" to see its recoverable components.");
        }
        static CompactGuideStep ItemStep(ScrappingModel model,CompactStack item,string use,string purpose)
        {return At(new YardDestination(YardLandmark.Automatic,model.Rules.Part(item.kind).name,item.x,item.z),purpose+" • pick up "+model.Rules.Part(item.kind).name+" ×"+item.quantity+" "+use+".");}
        static YardDestination Destination(LargeScrapJob scrap)
        {return new YardDestination(YardLandmark.Delivery,scrap.kind==ScrapObjectKind.Car?"Delivered car":"Delivered refrigerator",scrap.x,scrap.z);}
        static YardDestination Destination(EquipmentState equipment)
        {return new YardDestination(YardLandmark.Automatic,equipment.kind.ToString()+" #"+equipment.id,equipment.x,equipment.z);}
        static CompactGuideStep At(EquipmentState e,ScrappingModel model,string action)
        {return At(new YardDestination(YardLandmark.Automatic,model.Rules.Equipment(e.kind).name+" #"+e.id,e.x,e.z),action);}
        static CompactGuideStep At(YardDestination destination,string action)
        {return new CompactGuideStep{action=action,destination=destination,hasDestination=!string.IsNullOrEmpty(destination.name) && CompactRules.Finite(destination.x) && CompactRules.Finite(destination.z)};}
        static CompactGuideStep Message(string action){return new CompactGuideStep{action=action};}
        static EquipmentState NearestEquipment(ScrappingModel model,float x,float z,Func<EquipmentState,bool> accepts)
        {EquipmentState best=null;float distance=float.MaxValue;foreach(var e in model.State.equipment)if(accepts(e)){float next=Destination(e).Distance(x,z);if(next<distance){distance=next;best=e;}}return best;}
        static CompactStack NearestItem(ScrappingModel model,float x,float z,Func<CompactStack,bool> accepts)
        {CompactStack best=null;float distance=float.MaxValue;foreach(var i in model.State.items)if(i.id!=model.State.carriedId && accepts(i)){float next=new YardDestination(YardLandmark.Automatic,"item",i.x,i.z).Distance(x,z);if(next<distance){distance=next;best=i;}}return best;}
        static LargeScrapJob NearestScrap(ScrappingModel model,float x,float z,Func<LargeScrapJob,bool> accepts)
        {LargeScrapJob best=null;float distance=float.MaxValue;foreach(var s in model.State.scrap)if(accepts(s)){float next=Destination(s).Distance(x,z);if(next<distance){distance=next;best=s;}}return best;}
    }
}
