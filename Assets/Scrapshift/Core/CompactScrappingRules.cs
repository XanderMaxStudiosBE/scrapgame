using System;
using System.Collections.Generic;

namespace Scrapshift.Compact
{
    [Serializable] public sealed class PartDefinition
    {
        public PartKind kind;
        public string name;
        public int unitPrice, saleXp;
        public bool isMaterial;
        public PartDefinition() { }
        public PartDefinition(PartKind kind, string name, int price, int xp, bool material)
        { this.kind=kind; this.name=name; unitPrice=price; saleXp=xp; isMaterial=material; }
    }
    [Serializable] public sealed class ComponentRecipe
    {
        public int id;
        public string name;
        public PartKind input;
        public int inputQuantity=1, strokes=4;
        public float seconds=4;
        public PartAmount[] yields;
        public ComponentRecipe() { }
        public ComponentRecipe(int id, string name, PartKind input, int strokes, float seconds, params PartAmount[] yields)
        { this.id=id; this.name=name; this.input=input; this.strokes=strokes; this.seconds=seconds; this.yields=yields; }
    }
    [Serializable] public sealed class LargeScrapRecipe
    {
        public ScrapObjectKind kind;
        public string name;
        public int purchasePrice, strokes;
        public string[] workStages;
        public PartAmount[] yields;
        public LargeScrapRecipe() { }
        public LargeScrapRecipe(ScrapObjectKind kind, string name, int price, string[] stages, params PartAmount[] yields)
        { this.kind=kind; this.name=name; purchasePrice=price; strokes=stages.Length; workStages=stages; this.yields=yields; }
    }
    [Serializable] public sealed class CompactRules
    {
        public int startingMoney=8, startingCars=1, startingRefrigerators=1;
        public int renewableWireQuantity=1, maxStacks=256, maxLargeScrap=8, maxEquipment=64;
        public float CableRange=12, baseMachineSeconds=4;
        public int[] levelThresholds={0,30,80,160,280,440,650,930,1290,1750,2320,3010};
        // Integer-euro quotes need 12% for a common €9 copper bundle to gain its first €1.
        public int[] saleBonusPercents={0,12,16,20,24,28,32,36,40,45,50,55};
        public PartDefinition[] parts={
            new PartDefinition(PartKind.Wire,"Wiring",0,0,false),
            new PartDefinition(PartKind.Copper,"Copper",3,2,true),
            new PartDefinition(PartKind.BrokenFan,"Broken fan",0,0,false),
            new PartDefinition(PartKind.RestoredFan,"Tested fan",42,0,false),
            new PartDefinition(PartKind.BrokenRadio,"Broken radio",0,0,false),
            new PartDefinition(PartKind.RestoredRadio,"Tested radio",34,0,false),
            new PartDefinition(PartKind.Motor,"Motor",0,0,false),
            new PartDefinition(PartKind.BodyMetal,"Metal casing",0,0,false),
            new PartDefinition(PartKind.Steel,"Steel",1,1,true),
            new PartDefinition(PartKind.Plastic,"Plastic",1,1,true),
            new PartDefinition(PartKind.Insulation,"Insulation",1,1,true),
            new PartDefinition(PartKind.Compressor,"Compressor",0,0,false)
        };
        public ComponentRecipe[] recipes={
            new ComponentRecipe(100,"Strip wiring",PartKind.Wire,4,4,new PartAmount(PartKind.Copper,3),new PartAmount(PartKind.Insulation,2)),
            new ComponentRecipe(200,"Open motor",PartKind.Motor,5,6,new PartAmount(PartKind.Copper,4),new PartAmount(PartKind.Steel,6)),
            new ComponentRecipe(300,"Cut metal casing",PartKind.BodyMetal,3,3,new PartAmount(PartKind.Steel,4)),
            new ComponentRecipe(400,"Open compressor",PartKind.Compressor,5,6,new PartAmount(PartKind.Copper,3),new PartAmount(PartKind.Steel,4)),
            new ComponentRecipe(500,"Salvage fan",PartKind.BrokenFan,4,5,new PartAmount(PartKind.Copper,3),new PartAmount(PartKind.Steel,2),new PartAmount(PartKind.Plastic,2)),
            new ComponentRecipe(600,"Salvage radio",PartKind.BrokenRadio,4,5,new PartAmount(PartKind.Copper,2),new PartAmount(PartKind.Plastic,3))
        };
        public LargeScrapRecipe[] largeRecipes={
            new LargeScrapRecipe(ScrapObjectKind.Car,"Scrap car",25,new[]{"Release wiring","Remove battery leads","Unbolt motor","Lift motor free","Remove body panels","Separate usable components"},
                new PartAmount(PartKind.Motor,1),new PartAmount(PartKind.Wire,2),new PartAmount(PartKind.BodyMetal,3)),
            new LargeScrapRecipe(ScrapObjectKind.Refrigerator,"Scrap refrigerator",15,new[]{"Remove rear cover","Release compressor","Remove wiring","Separate casing and plastic"},
                new PartAmount(PartKind.Compressor,1),new PartAmount(PartKind.Wire,1),new PartAmount(PartKind.BodyMetal,1),new PartAmount(PartKind.Plastic,2))
        };
        public EquipmentDefinition[] equipment={
            new EquipmentDefinition{kind=EquipmentKind.Workbench,name="Manual workbench",price=24,available=true,width=2.6f,depth=1.4f,outputCapacity=24},
            new EquipmentDefinition{kind=EquipmentKind.Generator,name="Generator",price=45,available=true,width=1.75f,depth=1.25f,powerOutput=6},
            new EquipmentDefinition{kind=EquipmentKind.Tier1Scrapper,name="Tier 1 scrapper",price=60,available=true,width=2.4f,depth=2.3f,powerDemand=3,processingSeconds=4,outputCapacity=24},
            new EquipmentDefinition{kind=EquipmentKind.Storage,name="Ported storage — later stage",price=70,unlockLevel=10,width=3,depth=2.5f,outputCapacity=120},
            new EquipmentDefinition{kind=EquipmentKind.Tier2Scrapper,name="Tier 2 scrapper — later stage",price=160,unlockLevel=10,width=3.2f,depth=2.5f,powerDemand=5,processingSeconds=2,outputCapacity=48},
            new EquipmentDefinition{kind=EquipmentKind.Conveyor,name="Conveyor — later stage",price=12,unlockLevel=10,width=1,depth=2},
            new EquipmentDefinition{kind=EquipmentKind.Splitter,name="Splitter — later stage",price=30,unlockLevel=10,width=1.5f,depth=1.5f},
            new EquipmentDefinition{kind=EquipmentKind.Merger,name="Merger — later stage",price=30,unlockLevel=10,width=1.5f,depth=1.5f},
            new EquipmentDefinition{kind=EquipmentKind.ExportStation,name="Export station — later stage",price=200,unlockLevel=12,width=3,depth=2.5f,powerDemand=2}
        };

        public int MaxLevel { get { return levelThresholds.Length; } }
        public int LevelForExperience(int experience)
        {
            int level=1;
            while(level<levelThresholds.Length && experience>=levelThresholds[level]) level++;
            return level;
        }
        public int LevelForXP(int experience) { return LevelForExperience(experience); }
        public int BonusForLevel(int level) { return saleBonusPercents[Math.Max(0,Math.Min(level-1,saleBonusPercents.Length-1))]; }
        public PartDefinition Part(PartKind kind) { foreach(var p in parts) if(p.kind==kind) return p; return null; }
        public ComponentRecipe Recipe(PartKind input) { foreach(var r in recipes) if(r.input==input) return r; return null; }
        public LargeScrapRecipe LargeRecipe(ScrapObjectKind kind) { foreach(var r in largeRecipes) if(r.kind==kind) return r; return null; }
        public EquipmentDefinition Equipment(EquipmentKind kind) { foreach(var e in equipment) if(e.kind==kind) return e; return null; }
        public void Validate()
        {
            if(startingMoney<0 || startingMoney>1000000 || startingCars<0 || startingCars>4 || startingRefrigerators<0 || startingRefrigerators>4 || startingCars+startingRefrigerators>4 ||
                renewableWireQuantity<1 || renewableWireQuantity>64 || maxStacks<1 || maxStacks>512 || maxLargeScrap<1 || maxLargeScrap>16 ||
                maxEquipment<1 || maxEquipment>128 || !Finite(CableRange) || CableRange<1 || CableRange>60 ||
                !Finite(baseMachineSeconds) || baseMachineSeconds<=0 || baseMachineSeconds>3600)
                throw new ArgumentException("Invalid compact-yard starting supplies or limits.");
            if(levelThresholds==null || saleBonusPercents==null || levelThresholds.Length<10 || levelThresholds.Length>100 ||
                saleBonusPercents.Length!=levelThresholds.Length || levelThresholds[0]!=0)
                throw new ArgumentException("Experience curve must start at zero and contain at least ten levels.");
            for(int i=0;i<levelThresholds.Length;i++)
                if(levelThresholds[i]<0 || (i>0 && levelThresholds[i]<=levelThresholds[i-1]) || saleBonusPercents[i]<0 || saleBonusPercents[i]>100 ||
                    (i>0 && saleBonusPercents[i]<saleBonusPercents[i-1])) throw new ArgumentException("Invalid experience thresholds or sale bonuses.");
            if(parts==null || parts.Length!=12) throw new ArgumentException("Every compact item needs a definition.");
            var kinds=new HashSet<PartKind>();
            foreach(var p in parts)
                if(p==null || !Enum.IsDefined(typeof(PartKind),p.kind) || !kinds.Add(p.kind) || string.IsNullOrEmpty(p.name) ||
                    p.unitPrice<0 || p.unitPrice>100000 || p.saleXp<0 || p.saleXp>10000 || (!p.isMaterial && p.saleXp!=0))
                    throw new ArgumentException("Invalid item prices or experience.");
            if(recipes==null || recipes.Length<1 || recipes.Length>64) throw new ArgumentException("Missing component recipes.");
            var recipeIds=new HashSet<int>(); var inputs=new HashSet<PartKind>();
            foreach(var r in recipes)
            {
                if(r==null || r.id<=0 || !recipeIds.Add(r.id) || !inputs.Add(r.input) || Part(r.input)==null ||
                    string.IsNullOrEmpty(r.name) || r.inputQuantity<1 || r.inputQuantity>64 || r.strokes<1 || r.strokes>100 ||
                    !Finite(r.seconds) || r.seconds<=0 || r.seconds>3600) throw new ArgumentException("Invalid component recipe.");
                ValidateYields(r.yields,false);
            }
            if(largeRecipes==null || largeRecipes.Length!=2) throw new ArgumentException("Car and refrigerator recipes are required.");
            var largeKinds=new HashSet<ScrapObjectKind>();
            foreach(var r in largeRecipes)
            {
                if(r==null || !Enum.IsDefined(typeof(ScrapObjectKind),r.kind) || !largeKinds.Add(r.kind) || string.IsNullOrEmpty(r.name) ||
                    r.purchasePrice<0 || r.purchasePrice>100000 || r.strokes<1 || r.strokes>100 || r.workStages==null ||
                    r.workStages.Length!=r.strokes) throw new ArgumentException("Invalid large-scrap recipe.");
                foreach(string stage in r.workStages) if(string.IsNullOrEmpty(stage)) throw new ArgumentException("Missing work-stage label.");
                ValidateYields(r.yields,false);
            }
            if(equipment==null || equipment.Length!=9) throw new ArgumentException("Missing equipment catalogue entries.");
            var equipmentKinds=new HashSet<EquipmentKind>();
            foreach(var e in equipment)
                if(e==null || !Enum.IsDefined(typeof(EquipmentKind),e.kind) || !equipmentKinds.Add(e.kind) || string.IsNullOrEmpty(e.name) ||
                    e.price<0 || e.price>1000000 || e.unlockLevel<1 || e.unlockLevel>MaxLevel ||
                    !Finite(e.width) || !Finite(e.depth) || e.width<=0 || e.width>10 || e.depth<=0 || e.depth>10 ||
                    !Finite(e.powerOutput) || !Finite(e.powerDemand) || e.powerOutput<0 || e.powerOutput>10000 || e.powerDemand<0 || e.powerDemand>10000 ||
                    !Finite(e.processingSeconds) || e.processingSeconds<=0 || e.processingSeconds>3600 || e.outputCapacity<1 || e.outputCapacity>4096 ||
                    (e.available && e.kind!=EquipmentKind.Workbench && e.kind!=EquipmentKind.Generator && e.kind!=EquipmentKind.Tier1Scrapper))
                    throw new ArgumentException("Invalid equipment settings or an unavailable automation stage enabled.");
            var bench=Equipment(EquipmentKind.Workbench);var generator=Equipment(EquipmentKind.Generator);var tierOne=Equipment(EquipmentKind.Tier1Scrapper);
            if(bench.powerDemand!=0 || bench.powerOutput!=0 || generator.powerDemand!=0 || generator.powerOutput<=0 ||
                tierOne.powerOutput!=0 || tierOne.powerDemand<=0 || Recipe(PartKind.Wire)==null)
                throw new ArgumentException("A manual workbench, renewable wiring recipe and functional generator/machine power are required.");
        }
        internal static bool Finite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
        internal static void ValidateYields(PartAmount[] yields, bool allowZero)
        {
            if(yields==null || yields.Length<1 || yields.Length>12) throw new ArgumentException("Invalid yield slots.");
            var kinds=new HashSet<PartKind>();
            foreach(var y in yields)
                if(y==null || !Enum.IsDefined(typeof(PartKind),y.kind) || !kinds.Add(y.kind) || y.quantity<(allowZero?0:1) || y.quantity>4096)
                    throw new ArgumentException("Invalid yield quantity or duplicate output.");
        }
    }
}
