using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift.Tests
{
    public sealed class CompactWorkVisualTests
    {
        [Test]
        public void PrivateFacesPreserveUvsAndUseInverseTransposeNormalsWithoutEditingTheSource()
        {
            var source=new Mesh();Mesh copied=null;
            try
            {
                var n=new Vector3(1,1,0).normalized;
                source.vertices=new[]{Vector3.zero,Vector3.right,Vector3.forward};
                source.normals=new[]{n,n,n};source.uv=new[]{Vector2.zero,Vector2.right,Vector2.up};source.triangles=new[]{0,1,2};
                var copy=typeof(CompactWorkVisuals).GetMethod("CopyFaces",BindingFlags.NonPublic|BindingFlags.Static);
                var matrix=Matrix4x4.Scale(new Vector3(2,1,1));
                copied=(Mesh)copy.Invoke(null,new object[]{source,new List<int>{0,1,2},matrix,Vector3.zero,"Normal test"});
                var expected=new Vector3(.5f,1,0).normalized;
                foreach(var actual in copied.normals)Assert.Less(Vector3.Distance(expected,actual),.0001f);
                CollectionAssert.AreEqual(source.uv,copied.uv);
                Assert.AreEqual(Vector3.right,source.vertices[1]);Assert.AreEqual(Vector3.right*2,copied.vertices[1]);
                CollectionAssert.AreEqual(new[]{0,1,2},source.triangles);
            }
            finally{Object.DestroyImmediate(source);if(copied!=null)Object.DestroyImmediate(copied);}
        }

        [Test]
        public void MirroredPrivateFacesRetainOutwardWindingAndNeverReverseTheSource()
        {
            var source=new Mesh();Mesh copied=null;
            try
            {
                source.vertices=new[]{Vector3.zero,Vector3.right,Vector3.forward};
                source.normals=new[]{Vector3.down,Vector3.down,Vector3.down};source.triangles=new[]{0,1,2};
                var copy=typeof(CompactWorkVisuals).GetMethod("CopyFaces",BindingFlags.NonPublic|BindingFlags.Static);
                copied=(Mesh)copy.Invoke(null,new object[]{source,new List<int>{0,1,2},Matrix4x4.Scale(new Vector3(-1,1,1)),Vector3.zero,"Winding test"});
                var p=copied.vertices;var t=copied.triangles;
                var face=Vector3.Cross(p[t[1]]-p[t[0]],p[t[2]]-p[t[0]]).normalized;
                Assert.Less(Vector3.Distance(face,copied.normals[0]),.0001f);
                CollectionAssert.AreEqual(new[]{0,1,2},source.triangles);
            }
            finally{Object.DestroyImmediate(source);if(copied!=null)Object.DestroyImmediate(copied);}
        }

        [TestCase(ScrapObjectKind.Car,"WornHatchback")]
        [TestCase(ScrapObjectKind.Refrigerator,"CompactRefrigerator")]
        public void ActualScrapProgressReplaysWithoutChangingSourceGeometryOrPhysics(ScrapObjectKind kind,string modelName)
        {
            var root=new GameObject("Scrap work presentation test");
            try
            {
                var rules=new CompactRules();
                var view=CompactEquipmentVisuals.BuildScrap(kind,root.transform,Vector3.zero);
                var model=view.transform.Find(modelName);Assert.NotNull(model,"Tracked original FBX must be imported");
                var originalFilter=model.GetComponentInChildren<MeshFilter>();var mesh=originalFilter.sharedMesh;
                var originalVertices=mesh.vertices;var originalIndices=mesh.triangles;var originalUv=mesh.uv;
                var originalMaterial=originalFilter.GetComponent<MeshRenderer>().sharedMaterial;
                int colliderCount=view.GetComponentsInChildren<Collider>().Length;
                var job=new LargeScrapJob{kind=kind};
                CompactWorkVisuals.ApplyScrapProgress(view,job,rules);
                Assert.AreSame(mesh,originalFilter.sharedMesh);
                Assert.IsFalse(originalFilter.GetComponent<MeshRenderer>().enabled,"Readable authored prop uses private cutaway groups");
                var panel=view.transform.Find("Work access panel");Assert.NotNull(panel);Assert.IsTrue(panel.gameObject.activeSelf);
                var trim=view.transform.Find("Remaining trim and glazing");Assert.NotNull(trim);Assert.IsTrue(trim.gameObject.activeSelf);
                var recipe=rules.LargeRecipe(kind);job.inspected=true;job.requiredStrokes=recipe.strokes;job.strokes=1;
                job.remaining=new PartAmount[recipe.yields.Length];
                for(int i=0;i<recipe.yields.Length;i++)job.remaining[i]=new PartAmount(recipe.yields[i].kind,recipe.yields[i].quantity);
                CompactWorkVisuals.ApplyScrapProgress(view,job,rules);
                var first=view.transform.Find("Recoverable "+recipe.yields[0].kind);Assert.IsTrue(first.gameObject.activeSelf);var initialPartPosition=first.localPosition;
                if(kind==ScrapObjectKind.Car)Assert.Greater(Quaternion.Angle(panel.localRotation,Quaternion.identity),10);
                else Assert.IsFalse(panel.gameObject.activeSelf);
                Assert.IsTrue(view.transform.Find("Opened component bay").gameObject.activeSelf);
                job.strokes=job.requiredStrokes;CompactWorkVisuals.ApplyScrapProgress(view,job,rules);
                Assert.Greater(Vector3.Distance(initialPartPosition,first.localPosition),.1f,"Released component moves clear of its mounted position");
                Assert.IsFalse(panel.gameObject.activeSelf);Assert.IsFalse(trim.gameObject.activeSelf);
                foreach(var amount in job.remaining)Assert.IsTrue(view.transform.Find("Recoverable "+amount.kind).gameObject.activeSelf);
                var owned=view.GetComponentsInChildren<ProceduralMeshOwner>(true);
                var ownedMeshes=new Mesh[owned.Length];for(int i=0;i<owned.Length;i++)ownedMeshes[i]=owned[i].mesh;
                job.remaining[0].quantity=0;CompactWorkVisuals.ApplyScrapProgress(view,job,rules);
                Assert.IsFalse(first.gameObject.activeSelf,"Collected output must cease to be displayed");
                CompactWorkVisuals.ApplyScrapProgress(view,job,rules);
                var replayOwned=view.GetComponentsInChildren<ProceduralMeshOwner>(true);
                Assert.AreEqual(owned.Length,replayOwned.Length);
                for(int i=0;i<owned.Length;i++)Assert.AreSame(ownedMeshes[i],replayOwned[i].mesh,"Progress updates reuse bounded geometry");
                CollectionAssert.AreEqual(originalVertices,mesh.vertices);CollectionAssert.AreEqual(originalIndices,mesh.triangles);CollectionAssert.AreEqual(originalUv,mesh.uv);
                Assert.AreSame(originalMaterial,originalFilter.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.AreEqual(colliderCount,view.GetComponentsInChildren<Collider>(true).Length);
                Assert.IsEmpty(view.GetComponentsInChildren<Rigidbody>(true));Assert.IsEmpty(view.GetComponentsInChildren<Light>(true));
                Assert.AreEqual(job.requiredStrokes,job.strokes,"Presentation never changes work state");
                // Reconstructed saved state produces the same visible parts without replaying clicks.
                var reloaded=CompactEquipmentVisuals.BuildScrap(kind,root.transform,Vector3.right*4);
                CompactWorkVisuals.ApplyScrapProgress(reloaded,job,rules);
                foreach(var amount in job.remaining)
                    Assert.AreEqual(view.transform.Find("Recoverable "+amount.kind).gameObject.activeSelf,reloaded.transform.Find("Recoverable "+amount.kind).gameObject.activeSelf);
                int renderers=view.GetComponentsInChildren<Renderer>(true).Length;
                Assert.LessOrEqual(renderers,16,"Each large object has a small fixed presentation budget");
            }
            finally{Object.DestroyImmediate(root);}
        }

        [TestCase(false)] [TestCase(true)]
        public void UnavailableCutawaySourcePreservesTheExistingFallbackAndStillDisplaysReadyOutputs(bool unreadable)
        {
            var root=new GameObject("Missing source fallback test");
            try
            {
                var fallback=YardGeometry.SurfaceBox("Fallback",root.transform,Vector3.up,new Vector3(.7f,.6f,.6f),RetroSurface.DarkMetal,false);
                var mesh=fallback.GetComponent<MeshFilter>().sharedMesh;
                if(unreadable){fallback.name="WornHatchback";mesh.UploadMeshData(true);}
                var rules=new CompactRules();var recipe=rules.LargeRecipe(ScrapObjectKind.Car);
                var job=new LargeScrapJob{kind=ScrapObjectKind.Car,inspected=true,requiredStrokes=recipe.strokes,strokes=recipe.strokes,remaining=recipe.yields};
                CompactWorkVisuals.ApplyScrapProgress(root,job,rules);
                Assert.IsTrue(fallback.GetComponent<MeshRenderer>().enabled);Assert.AreSame(mesh,fallback.GetComponent<MeshFilter>().sharedMesh);
                Assert.IsNull(root.transform.Find("Work access panel"));
                foreach(var amount in job.remaining)Assert.IsTrue(root.transform.Find("Recoverable "+amount.kind).gameObject.activeSelf);
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(root.GetComponentsInChildren<Light>(true));
            }
            finally{Object.DestroyImmediate(root);}
        }

        [TestCase(EquipmentKind.Tier1Scrapper)] [TestCase(EquipmentKind.Tier2Scrapper)]
        public void DriveMotionStopsWhenIdleBlockedOrReadyAndGeometryIsReused(EquipmentKind kind)
        {
            var root=new GameObject("Drive presentation test");
            try
            {
                var rules=new CompactRules();var view=CompactEquipmentVisuals.Build(kind,root.transform,Vector3.zero,0,false,rules);
                CompactWorkVisuals.PrepareEquipment(view,kind,rules);
                var drive=view.transform.Find("Visible drive timing marks");Assert.NotNull(drive);
                var state=new EquipmentState{kind=kind,job=new ProcessingJob{duration=4,remaining=3}};
                CompactWorkVisuals.AnimateEquipment(view,state,true,.1f);var turning=drive.localRotation;
                CompactWorkVisuals.AnimateEquipment(view,state,false,100);Assert.AreEqual(turning,drive.localRotation,"Blocked power retains motion pose");
                CompactWorkVisuals.AnimateEquipment(view,state,true,.2f);Assert.Greater(Quaternion.Angle(turning,drive.localRotation),1);
                var paused=drive.localRotation;CompactWorkVisuals.AnimateEquipment(view,state,true,.2f);Assert.AreEqual(paused,drive.localRotation,"Frozen gameplay time gives frozen presentation");
                state.job.ready=true;CompactWorkVisuals.AnimateEquipment(view,state,true,200);Assert.AreEqual(paused,drive.localRotation);
                state.job=null;CompactWorkVisuals.AnimateEquipment(view,state,true,300);Assert.AreEqual(paused,drive.localRotation);
                int children=view.transform.childCount;CompactWorkVisuals.PrepareEquipment(view,kind,rules);Assert.AreEqual(children,view.transform.childCount);
                Assert.IsEmpty(view.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(view.GetComponentsInChildren<Rigidbody>(true));Assert.IsEmpty(view.GetComponentsInChildren<Light>(true));
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test]
        public void BenchStrokeReturnsToRestAndZeroGameplayDeltaDoesNotAdvanceIt()
        {
            var root=new GameObject("Manual stroke test");
            try
            {
                CompactWorkVisuals.PrepareEquipment(root,EquipmentKind.Workbench,new CompactRules());
                var state=new EquipmentState{kind=EquipmentKind.Workbench,job=new ProcessingJob()};
                CompactWorkVisuals.AnimateEquipment(root,state,false,0);
                var tool=root.transform.Find("Working hand tool");Assert.IsTrue(tool.gameObject.activeSelf);var rest=tool.localPosition;
                CompactWorkVisuals.Pulse(root);CompactWorkVisuals.StepPulse(root,.1f);Assert.Greater(Vector3.Distance(rest,tool.localPosition),.02f);
                var mid=tool.localPosition;CompactWorkVisuals.StepPulse(root,0);Assert.AreEqual(mid,tool.localPosition);
                CompactWorkVisuals.StepPulse(root,1);Assert.AreEqual(rest,tool.localPosition);CompactWorkVisuals.StepPulse(root,1);Assert.AreEqual(rest,tool.localPosition);
                state.job.ready=true;CompactWorkVisuals.AnimateEquipment(root,state,false,0);Assert.IsFalse(tool.gameObject.activeSelf);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
