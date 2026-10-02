using System;

namespace Scrapshift.Compact
{
    // Explicit compatibility import. The old file/scene remains the full continuation path.
    // Historical XP is unknown, so no XP or eligibility is invented for old inventory.
    public static class CompactLegacyImport
    {
        public const string Explanation="Import copies cash, portable inventory and wire-bench/machine progress. Old repairs, upgrades, customer escrow and daily records stay playable in the unchanged legacy yard and are retained in a full snapshot. No historical XP is invented. The old machine becomes a placed Tier 1 scrapper and needs a purchased generator and cable.";
        public static CompactYardState Convert(YardState source,YardRules oldRules,CompactRules rules,string snapshot)
        {
            YardModel.Validate(source);oldRules.Validate();rules.Validate();
            if(string.IsNullOrEmpty(snapshot))throw new ArgumentException("Legacy source snapshot is required.");
            var oldModel=new YardModel(oldRules,source);
            var state=new ScrappingModel(rules).State;
            state.scrap.Clear();state.items.Clear();state.money=source.money;state.experience=0;
            state.importedLegacy=true;state.legacySnapshot=snapshot;state.legacyNotice=Explanation;
            state.welcomeSeen=false;
            var bench=state.equipment[0];
            if(source.benchLoaded || source.benchOutput>0)
                bench.job=Snapshot(oldModel,source.benchLoaded?oldRules.copperPerWire:source.benchOutput,
                    source.benchLoaded?source.benchStrokes:oldModel.WireWorkSteps,!source.benchLoaded,0);
            if(source.machineOwned)
            {
                var machine=new EquipmentState{id=state.nextId++,kind=EquipmentKind.Tier1Scrapper,x=5,z=-4,paidPrice=oldRules.machinePrice};
                if(source.machineRemaining>0 || source.machineOutput>0)
                    machine.job=Snapshot(oldModel,source.machineRemaining>0?source.machinePendingYield:source.machineOutput,
                        0,source.machineRemaining<=0,source.machineRemaining,true);
                state.equipment.Add(machine);
            }
            foreach(var item in source.items)
            {
                int id=state.nextId++;
                // Arrange on open gravel north of the fixed wire crate; quantities stay unchanged.
                int slot=state.items.Count;
                state.items.Add(new CompactStack{id=id,kind=(PartKind)(int)item.kind,quantity=item.quantity,
                    x=14+(slot%5)*1.5f,y=.25f,z=-2+(slot/5)*.85f,xpEligible=false});
                if(item.id==source.carriedId)state.carriedId=id;
            }
            // Many legacy bundles still fit the compact yard, but never move anything beyond its fence.
            foreach(var item in state.items)if(item.z>16)throw new ArgumentException("Too much legacy inventory for receiving space; continue the legacy yard first.");
            ScrappingModel.Validate(state,rules);ConstructionModel.Validate(state,rules);return state;
        }
        static ProcessingJob Snapshot(YardModel oldModel,int yield,int strokes,bool ready,float remaining,bool powered=false)
        {
            return new ProcessingJob{recipeId=100,input=PartKind.Wire,inputQuantity=1,
                requiredStrokes=powered?0:oldModel.WireWorkSteps,strokes=strokes,duration=Math.Max(oldModel.MachineSeconds,remaining),
                remaining=remaining,ready=ready,xpEligible=false,yields=new[]{new PartAmount(PartKind.Copper,yield)}};
        }
    }
}
