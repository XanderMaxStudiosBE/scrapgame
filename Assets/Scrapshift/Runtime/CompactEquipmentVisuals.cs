using UnityEngine;

namespace Scrapshift.Compact
{
    /// <summary>Original, metre-sized views. Inventory and work progress remain in the core model.</summary>
    public static class CompactEquipmentVisuals
    {
        public static Vector3 GeneratorPowerSocket(CompactRules rules)
        {
            var definition=rules.Equipment(EquipmentKind.Generator);
            return new Vector3(.45f*definition.width/1.75f,.75f,-.55f*definition.depth/1.25f);
        }

        public static Vector3 Tier1PowerSocket(CompactRules rules)
        {
            var definition=rules.Equipment(EquipmentKind.Tier1Scrapper);
            return new Vector3(-.9f*definition.width/2.4f,.9f,-.29f*definition.depth/2.3f);
        }

        public static GameObject Build(EquipmentKind kind, Transform parent, Vector3 position, float yaw = 0, bool colliders = true, CompactRules rules = null)
        {
            if(kind==EquipmentKind.PrimaryScrapper || kind==EquipmentKind.ExportStation)
                return CompactIndustryVisuals.BuildEquipment(kind,parent,position,yaw,colliders,rules);
            if(kind==EquipmentKind.Storage || kind==EquipmentKind.Tier2Scrapper || kind==EquipmentKind.Splitter || kind==EquipmentKind.Merger)
                return CompactAutomationVisuals.BuildEquipment(kind,parent,position,yaw,colliders,rules);
            var root = Root(kind.ToString(), parent, position, yaw);
            switch (kind)
            {
                case EquipmentKind.Workbench:
                    Model("Workbench", root.transform);
                    YardWorldDressing.TryPlace("RepairTools", root.transform, new Vector3(.35f,1.14f,.1f), out _);
                    Collision(root, new Vector3(0,.72f,0), new Vector3(2.55f,1.44f,1.35f), colliders);
                    break;
                case EquipmentKind.Generator:
                    Model("CompactGenerator",root.transform);
                    Collision(root, new Vector3(0,.62f,0), new Vector3(1.75f,1.24f,1.25f), colliders);
                    break;
                case EquipmentKind.Tier1Scrapper:
                    Model("CompactTier1Scrapper",root.transform);
                    Collision(root, new Vector3(0,1.0f,-.04f), new Vector3(2.4f,2,2.3f), colliders);
                    break;
                default:
                    Model("PoweredStripper",root.transform);
                    Collision(root,new Vector3(0,.7f,0),new Vector3(3.2f,1.4f,2.0f),colliders);
                    break;
            }
            if(rules!=null)
            {
                var definition=rules.Equipment(kind);
                if(definition!=null)
                {
                    foreach(var box in root.GetComponents<BoxCollider>())box.size=new Vector3(definition.width,box.size.y,definition.depth);
                    string model=kind==EquipmentKind.Workbench?"Workbench":kind==EquipmentKind.Generator?"CompactGenerator":"CompactTier1Scrapper";
                    float width=kind==EquipmentKind.Workbench?2.55f:kind==EquipmentKind.Generator?1.75f:2.4f;
                    float depth=kind==EquipmentKind.Workbench?1.35f:kind==EquipmentKind.Generator?1.25f:2.3f;
                    var body=root.transform.Find(model);
                    if(body!=null)body.localScale=new Vector3(definition.width/width,1,definition.depth/depth);
                    if(kind==EquipmentKind.Workbench)
                    {
                        var tools=root.transform.Find("RepairTools");
                        if(tools!=null)
                        {
                            tools.localScale=new Vector3(definition.width/width,1,definition.depth/depth);
                            tools.localPosition=new Vector3(.35f*definition.width/width,1.14f,.1f*definition.depth/depth);
                        }
                    }
                }
            }
            if(kind==EquipmentKind.Workbench || kind==EquipmentKind.Tier1Scrapper)
                CompactAutomationVisuals.BuildPorts(kind,root.transform,rules ?? new CompactRules(),colliders);
            if(kind==EquipmentKind.Workbench || kind==EquipmentKind.Generator || kind==EquipmentKind.Tier1Scrapper)
                CompactAutomationVisuals.BuildMachineDetails(kind,root.transform,rules ?? new CompactRules());
            if(colliders)
            {
                if(kind==EquipmentKind.Workbench)
                {
                    var definition=(rules ?? new CompactRules()).Equipment(kind);
                    YardSignText.Plate(root.transform,"MANUAL WORKBENCH",new Vector3(0,.94f,-definition.depth*.5f-.025f),Mathf.Min(1.55f,definition.width*.65f),.24f);
                }
                else if(kind==EquipmentKind.Generator)
                {
                    var definition=(rules ?? new CompactRules()).Equipment(kind);
                    YardSignText.Plate(root.transform,"GENERATOR",new Vector3(-.29f*definition.width/1.75f,1.04f,-.285f*definition.depth/1.25f),Mathf.Min(.72f,definition.width*.42f),.15f);
                }
                else if(kind==EquipmentKind.Tier1Scrapper)
                {
                    var definition=(rules ?? new CompactRules()).Equipment(kind);
                    YardSignText.Plate(root.transform,"SCRAPPER / TIER 1",new Vector3(0,1.68f,-.47f*definition.depth/2.3f),Mathf.Min(1.30f,definition.width*.54f),.22f);
                }
            }
            return root;
        }

        public static GameObject BuildPart(PartKind kind, Transform parent, Vector3 position, bool colliders = true)
        {
            var root = Root(kind.ToString(),parent,position,0);
            Vector3 size;
            switch (kind)
            {
                case PartKind.Wire: Model("WireBundle",root.transform);size=new Vector3(.62f,.23f,.64f);break;
                case PartKind.Copper: Model("CopperBundle",root.transform);size=new Vector3(.55f,.23f,.64f);break;
                case PartKind.Motor: Model("CompactMotor",root.transform);size=new Vector3(.50f,.43f,.62f);break;
                case PartKind.Compressor: Model("CompactCompressor",root.transform);size=new Vector3(.53f,.45f,.46f);break;
                case PartKind.Plastic: Model("CompactPlasticFragments",root.transform);size=new Vector3(.55f,.18f,.50f);break;
                case PartKind.Insulation: Model("CompactInsulationCoil",root.transform);size=new Vector3(.53f,.15f,.49f);break;
                case PartKind.BrokenFan: case PartKind.RestoredFan:
                    Model("SalvageFan",root.transform);size=new Vector3(.8f,1.0f,.5f);break;
                case PartKind.BrokenRadio: case PartKind.RestoredRadio:
                    Model("PortableRadio",root.transform);size=new Vector3(.66f,.64f,.26f);break;
                default:
                    size = new Vector3(.57f,.25f,.49f);
                    var surface = kind==PartKind.BodyMetal ? RetroSurface.RustPaint : RetroSurface.DarkMetal;
                    // A few recoverable sheets and curled strips, kept in one portable stack.
                    for(int i=0;i<3;i++)
                    {
                        var sheet=YardGeometry.SurfaceBox("Recovered sheet",root.transform,new Vector3((i%2)*.025f,.035f+i*.055f,0),new Vector3(.51f,.065f,.43f),surface,false);
                        sheet.transform.localRotation=Quaternion.Euler(i*3,i*9,0);
                    }
                    break;
            }
            Collision(root,new Vector3(0,size.y*.5f,kind==PartKind.Motor ? -.08f : 0),size,colliders);
            return root;
        }

        public static GameObject BuildScrap(ScrapObjectKind kind, Transform parent, Vector3 position, float yaw = 0, bool colliders = true)
        {
            var root = Root(kind==ScrapObjectKind.Car ? "Old car" : "Old refrigerator",parent,position,yaw);
            if(kind==ScrapObjectKind.Car)
            {
                Model("WornHatchback",root.transform);
                Collision(root,new Vector3(0,.65f,0),new Vector3(2.4f,.90f,4.7f),colliders);
                Collision(root,new Vector3(0,1.30f,.15f),new Vector3(1.90f,.70f,2.05f),colliders);
            }
            else
            {
                Model("CompactRefrigerator",root.transform);
                Collision(root,new Vector3(0,.94f,-.05f),new Vector3(.85f,1.88f,.95f),colliders);
            }
            return root;
        }

        static GameObject Root(string name,Transform parent,Vector3 position,float yaw)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);
            root.transform.localPosition=position;root.transform.localRotation=Quaternion.Euler(0,yaw,0);
            return root;
        }
        static void Model(string name,Transform root)
        {
            if(AuthoredYardProps.TryPlace(name,root,Vector3.zero,out _))return;
            // Import problems stay visible in the Console; fallback preserves reachability.
            YardGeometry.SurfaceBox(name+" import fallback",root,new Vector3(0,.3f,0),new Vector3(.7f,.6f,.6f),RetroSurface.DarkMetal,false);
        }
        static void Collision(GameObject root,Vector3 centre,Vector3 size,bool enabled)
        {
            if(!enabled)return;
            var box=root.AddComponent<BoxCollider>();box.center=centre;box.size=size;
        }
    }
}
