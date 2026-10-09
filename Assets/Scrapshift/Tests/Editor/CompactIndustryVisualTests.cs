using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    /// <summary>Actual Unity import/presentation cases. They are supplied, not executed in Cloud.</summary>
    public sealed class CompactIndustryVisualTests
    {
        [TestCase(EquipmentKind.PrimaryScrapper,"CompactPrimaryScrapper",4500)]
        [TestCase(EquipmentKind.ExportStation,"CompactExportStation",2500)]
        public void ActualOriginalModelsFitEditableFootprintsAndUseOneRecoveredAtlas(EquipmentKind kind,string name,int budget)
        {
            var owner=new GameObject("Industrial asset import test");
            try
            {
                var rules=new CompactRules();var definition=rules.Equipment(kind);
                definition.width*=.85f;definition.depth*=.90f;
                var root=CompactIndustryVisuals.BuildEquipment(kind,owner.transform,new Vector3(2,0,3),90,true,rules);
                var model=root.transform.Find(name);Assert.NotNull(model,"Tracked original FBX must finish importing");
                var material=YardMaterialBindings.Load("ScrapshiftMaterials/PropAtlas",owner.transform);
                int triangles=0;
                foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=filter.sharedMesh;Assert.IsTrue(mesh.isReadable);triangles+=mesh.triangles.Length/3;
                    foreach(var vertex in mesh.vertices)
                    {
                        var p=root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        Assert.LessOrEqual(Mathf.Abs(p.x),definition.width*.5f+.01f);
                        Assert.LessOrEqual(Mathf.Abs(p.z),definition.depth*.5f+.01f);
                        Assert.That(p.y,Is.InRange(-.001f,kind==EquipmentKind.PrimaryScrapper?4.5f:2.4f));
                    }
                    Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);
                    Assert.AreEqual(1,mesh.subMeshCount);
                }
                Assert.Greater(triangles,500);Assert.LessOrEqual(triangles,budget);
                foreach(var renderer in model.GetComponentsInChildren<Renderer>())Assert.AreSame(material,renderer.sharedMaterial);
                Assert.AreEqual(1+AutomationModel.PortCount(kind,false)+AutomationModel.PortCount(kind,true),root.GetComponentsInChildren<Collider>().Length);
                var collider=root.GetComponent<BoxCollider>();Assert.AreEqual(definition.width,collider.size.x);Assert.AreEqual(definition.depth,collider.size.z);
                Assert.IsEmpty(root.GetComponentsInChildren<Rigidbody>());Assert.IsEmpty(root.GetComponentsInChildren<Light>());
                Assert.IsNull(typeof(CompactIndustryVisuals).GetMethod("Update",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance));
                var ghost=CompactIndustryVisuals.BuildEquipment(kind,owner.transform,Vector3.zero,0,false,rules);
                Assert.IsEmpty(ghost.GetComponentsInChildren<Collider>());
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        [TestCase(EquipmentKind.PrimaryScrapper,0)] [TestCase(EquipmentKind.PrimaryScrapper,90)]
        [TestCase(EquipmentKind.ExportStation,180)] [TestCase(EquipmentKind.ExportStation,270)]
        public void IndustrialPortMarkersMatchAuthoritativeRotatedPathPoints(EquipmentKind kind,float yaw)
        {
            var owner=new GameObject("Industrial port geometry test");
            try
            {
                var rules=new CompactRules();rules.Equipment(kind).width*=1.2f;rules.Equipment(kind).depth*=.8f;
                var state=new EquipmentState{kind=kind,x=4,z=3,yaw=yaw};
                var root=CompactIndustryVisuals.BuildEquipment(kind,owner.transform,new Vector3(state.x,0,state.z),yaw,false,rules);
                for(int category=0;category<2;category++)
                {
                    bool output=category==1;
                    for(int i=0;i<AutomationModel.PortCount(kind,output);i++)
                    {
                        var marker=root.transform.Find((output?"Output port ":"Input port ")+i);Assert.NotNull(marker);
                        var point=AutomationModel.Port(state,rules,output,i);
                        Assert.Less(Vector3.Distance(marker.position,new Vector3(point.x,.7f,point.z)),.001f);
                    }
                }
                Assert.AreEqual(kind==EquipmentKind.PrimaryScrapper?0:1,AutomationModel.PortCount(kind,false));
                Assert.AreEqual(kind==EquipmentKind.PrimaryScrapper?1:0,AutomationModel.PortCount(kind,true));
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        [TestCase(EquipmentKind.PrimaryScrapper)] [TestCase(EquipmentKind.ExportStation)]
        public void CableSocketTracksTheAuthoredPowerPanelAfterDefinitionScaling(EquipmentKind kind)
        {
            var owner=new GameObject("Industrial power-socket test");
            try
            {
                var rules=new CompactRules();rules.Equipment(kind).width*=1.1f;rules.Equipment(kind).depth*=.9f;
                var root=CompactIndustryVisuals.BuildEquipment(kind,owner.transform,Vector3.zero,0,false,rules);
                var socket=kind==EquipmentKind.PrimaryScrapper?CompactIndustryVisuals.PrimaryPowerSocket(rules):CompactIndustryVisuals.ExportPowerSocket(rules);
                string name=kind==EquipmentKind.PrimaryScrapper?"CompactPrimaryScrapper":"CompactExportStation";
                var model=root.transform.Find(name);Assert.NotNull(model);
                float nearest=float.MaxValue;
                foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
                    foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        var local=root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        nearest=Mathf.Min(nearest,Vector3.Distance(local,socket));
                    }
                Assert.Less(nearest,.085f,"Cable endpoint must meet the imported socket rather than an arbitrary collider edge");
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        static EquipmentState Loaded(ScrapObjectKind kind)
        {
            return new EquipmentState{id=7,kind=EquipmentKind.PrimaryScrapper,industry=new IndustrialMachineState{
                enabled=true,primary=new PrimaryScrapJob{id=17,kind=kind,duration=24,remaining=12,xpEligible=true,
                    yields=new[]{new PartAmount(PartKind.Wire,3),new PartAmount(PartKind.BodyMetal,2)}}}};
        }

        static Transform DirectChild(Transform parent,string name)
        {
            // Loaded view names contain a literal slash, which Transform.Find treats as a path.
            for(int i=0;i<parent.childCount;i++)
                if(parent.GetChild(i).name==name)return parent.GetChild(i);
            return null;
        }

        [TestCase(ScrapObjectKind.Car)] [TestCase(ScrapObjectKind.Refrigerator)]
        public void DurableLoadedObjectHasOneCachedViewNoPhysicsAndNoGameplayMutations(ScrapObjectKind kind)
        {
            var owner=new GameObject("Loaded industrial object test");
            try
            {
                var rules=new CompactRules();rules.Equipment(EquipmentKind.PrimaryScrapper).width*=.7f;
                rules.Equipment(EquipmentKind.PrimaryScrapper).depth*=.7f;
                var root=CompactIndustryVisuals.BuildEquipment(EquipmentKind.PrimaryScrapper,owner.transform,Vector3.zero,0,true,rules);
                var equipment=Loaded(kind);string before=JsonUtility.ToJson(equipment);
                CompactIndustryVisuals.SyncJob(root,equipment,rules);
                var loaded=DirectChild(root.transform,"Loaded whole scrap / 17");Assert.NotNull(loaded);
                int meshCount=root.GetComponentsInChildren<MeshFilter>().Length;
                for(int i=0;i<12;i++)CompactIndustryVisuals.SyncJob(root,equipment,rules);
                Assert.AreSame(loaded,DirectChild(root.transform,"Loaded whole scrap / 17"));
                Assert.AreEqual(meshCount,root.GetComponentsInChildren<MeshFilter>().Length);
                Assert.IsEmpty(loaded.GetComponentsInChildren<Collider>());Assert.IsEmpty(loaded.GetComponentsInChildren<Rigidbody>());
                foreach(var filter in loaded.GetComponentsInChildren<MeshFilter>())
                    foreach(var vertex in filter.sharedMesh.vertices)
                    {
                        var p=root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                        Assert.Less(Mathf.Abs(p.x),rules.Equipment(equipment.kind).width*.5f);
                        Assert.Less(Mathf.Abs(p.z),rules.Equipment(equipment.kind).depth*.5f);
                    }
                Assert.AreEqual(before,JsonUtility.ToJson(equipment));
                equipment.industry.primary=null;CompactIndustryVisuals.SyncJob(root,equipment,rules);
                Assert.IsNull(DirectChild(root.transform,"Loaded whole scrap / 17"));
                Assert.IsEmpty(equipment.contents);Assert.AreEqual(0,equipment.industry.objectsProcessed);
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        [Test]
        public void PrimaryDriveAndClampsFreezeWhenPausedUnpoweredOrBlocked()
        {
            var owner=new GameObject("Industrial motion gate test");
            try
            {
                var rules=new CompactRules();var equipment=Loaded(ScrapObjectKind.Car);
                var root=CompactIndustryVisuals.BuildEquipment(equipment.kind,owner.transform,Vector3.zero,0,true,rules);
                CompactIndustryVisuals.SyncJob(root,equipment,rules);
                var drive=root.transform.Find("Industrial timing mark");var clamp=root.transform.Find("Left hydraulic clamp");
                var initialDrive=drive.localRotation;var initialClamp=clamp.localPosition;
                CompactIndustryVisuals.Step(root,equipment,false,.2f,1);
                Assert.AreEqual(initialDrive,drive.localRotation);Assert.AreEqual(initialClamp,clamp.localPosition);
                string before=JsonUtility.ToJson(equipment);
                CompactIndustryVisuals.Step(root,equipment,true,.2f,1);
                Assert.Greater(Quaternion.Angle(initialDrive,drive.localRotation),1);
                Assert.Greater(Vector3.Distance(initialClamp,clamp.localPosition),.001f);
                var runningDrive=drive.localRotation;var runningClamp=clamp.localPosition;
                CompactIndustryVisuals.Step(root,equipment,false,4,5);
                CompactIndustryVisuals.Step(root,equipment,true,float.NaN,5);
                CompactIndustryVisuals.Step(root,equipment,true,.2f,float.PositiveInfinity);
                Assert.AreEqual(runningDrive,drive.localRotation);Assert.AreEqual(runningClamp,clamp.localPosition);
                Assert.AreEqual(before,JsonUtility.ToJson(equipment));
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }

        [Test]
        public void EmptyOrDisabledExportDoesNotAnimateOrGainInventory()
        {
            var owner=new GameObject("Export motion gate test");
            try
            {
                var rules=new CompactRules();var equipment=new EquipmentState{id=8,kind=EquipmentKind.ExportStation,
                    industry=new IndustrialMachineState{enabled=true}};
                var root=CompactIndustryVisuals.BuildEquipment(equipment.kind,owner.transform,Vector3.zero,0,true,rules);
                var drive=root.transform.Find("Industrial timing mark");var initial=drive.localRotation;
                CompactIndustryVisuals.Step(root,equipment,true,.2f,1);Assert.AreEqual(initial,drive.localRotation);
                equipment.contents.Add(new CompactStack{id=18,kind=PartKind.Copper,quantity=2,xpEligible=true});
                equipment.industry.enabled=false;CompactIndustryVisuals.Step(root,equipment,true,.2f,1);Assert.AreEqual(initial,drive.localRotation);
                equipment.industry.enabled=true;string before=JsonUtility.ToJson(equipment);
                CompactIndustryVisuals.Step(root,equipment,true,.2f,1);Assert.Greater(Quaternion.Angle(initial,drive.localRotation),1);
                Assert.AreEqual(before,JsonUtility.ToJson(equipment));
            }
            finally{UnityEngine.Object.DestroyImmediate(owner);}
        }
    }
}
