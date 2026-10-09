// Executes actual CompactYardWorld/YardGeometry source against the controlled
// API/value adapters in MachineryPresentationRunner. Authored/import/font/ground
// material/batching/backdrop/fence-texture fixtures do not count as acceptance.
// No Unity import, render, physics, static-batch or frame-destruction claims.
using System;
using System.Collections.Generic;
using Scrapshift;
using Scrapshift.Compact;
using UnityEngine;
using UnityEngine.Rendering;

public static class ReferenceEnvironmentRunner
{
    static int checks;
    static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    static void Near(Vector3 a, Vector3 b, string message) { Check(Vector3.Distance(a, b) < .0002f, message); }
    static Bounds WorldBounds(MeshFilter filter)
    {
        var min = filter.transform.TransformPoint(filter.sharedMesh.vertices[0]); var max = min;
        foreach (var vertex in filter.sharedMesh.vertices)
        {
            var v = filter.transform.TransformPoint(vertex);
            min = new Vector3(Math.Min(min.x, v.x), Math.Min(min.y, v.y), Math.Min(min.z, v.z));
            max = new Vector3(Math.Max(max.x, v.x), Math.Max(max.y, v.y), Math.Max(max.z, v.z));
        }
        return new Bounds { center = (min + max) * .5f, size = max - min };
    }
    static void BuildCase(bool combine)
    {
        var root = new GameObject("Actual compact environment source fixture"); int priorBatchCalls = StaticBatchingUtility.calls;
        var handles = CompactYardWorld.Build(root.transform, combine);
        Near(CompactYardWorld.OfficeAnchor, new Vector3(-17, 0, -14), "Preserved fixed office");
        Near(handles.Shop.transform.localPosition, new Vector3(-19, 0, -10.25f), "Preserved equipment counter");
        Near(handles.Sales.transform.localPosition, new Vector3(-15, 0, -10.25f), "Preserved sales counter");
        Near(handles.Delivery.transform.localPosition, new Vector3(15, 0, -13), "Preserved delivery target");
        Near(handles.Wire.transform.localPosition, new Vector3(15, 0, -7), "Preserved free-wire target");
        Check(CompactYardWorld.HalfWidth == 24 && CompactYardWorld.HalfDepth == 18, "Preserved 48 x 36 metre yard");
        Check(root.GetComponentsInChildren<Collider>().Length == 19, "Retained 19 existing infrastructure colliders");
        Check(root.GetComponentsInChildren<Rigidbody>().Length == 0, "No environment rigidbodies");
        Check(root.GetComponentsInChildren<Light>().Length == 0, "No new environment lights");
        Check(combine ? StaticBatchingUtility.calls > priorBatchCalls : StaticBatchingUtility.calls == priorBatchCalls, "Actual world obeys combine switch; batching itself controlled");
        var cover = root.transform.Find("Compact office and sales/Office service rain cover");
        Check(cover != null, "Practical office cover exists"); Check(cover.GetComponentsInChildren<Collider>().Length == 0, "Cover adds zero collision");
        var coverFilters = cover.GetComponentsInChildren<MeshFilter>(); Check(coverFilters.Length == 21, "Bounded 21-box office service cover"); int casters = 0;
        foreach (var filter in coverFilters)
        {
            Check(filter.GetComponent<AuthoredFixtureMarker>() == null, "Cover accepted from actual procedural source");
            var bounds = WorldBounds(filter);
            Check(bounds.min.x >= -23 && bounds.max.x <= -11 && bounds.min.z >= -17 && bounds.max.z <= -10 && bounds.max.y <= 3.3f, "Cover stays within historical fixed office reservation");
            Check(filter.sharedMesh.triangles.Length / 3 == 12, "Cover box triangle envelope");
            Check(filter.GetComponent<ProceduralMeshOwner>().mesh == filter.sharedMesh, "Cover owns private mesh");
            if (filter.GetComponent<MeshRenderer>().shadowCastingMode != ShadowCastingMode.Off) casters++;
        }
        Check(casters == 1, "Only the awning roof adds a caster");
        int screens = 0, rails = 0;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.name == "Boundary salvaged screen panel")
            {
                screens++; Check(renderer.GetComponent<Collider>() == null, "Screens add no collision");
                var filter = renderer.GetComponent<MeshFilter>(); var bounds = WorldBounds(filter);
                Check(bounds.max.y <= 2.1f && (Math.Abs(bounds.center.x) >= 23.9f || Math.Abs(bounds.center.z) >= 17.9f), "Screens remain within retained boundary envelopes");
                Check(filter.sharedMesh.triangles.Length / 3 == 12 && filter.GetComponent<ProceduralMeshOwner>().mesh == filter.sharedMesh, "Private bounded screen box");
            }
            if (renderer.name == "Boundary screen fixing rail" || renderer.name == "Boundary top support rail")
            { rails++; Check(renderer.shadowCastingMode == ShadowCastingMode.Off && renderer.GetComponent<Collider>() == null, "Screen support rails add no casters or collision"); }
        }
        Check(screens == 36 && rails == 41, "Exact bounded 36 screen / 41 rail budget");
        var lane = root.transform.Find("Compact ground/Worn entry and receiving wheel lanes"); Check(lane != null, "Actual worn service routes generated");
        var mesh = lane.GetComponent<MeshFilter>().sharedMesh;
        Check(mesh.vertexCount == 24 && mesh.triangles.Length / 3 == 18, "Three four-point service strips stay bounded");
        Check(lane.GetComponent<ProceduralMeshOwner>().mesh == mesh && lane.GetComponent<Collider>() == null, "Private decorative lane ownership");
        foreach (var normal in mesh.normals) Check(normal.y > .99f, "Actual route winding faces upward");
        for (int route = 0; route < 3; route++)
        {
            int start = route * 8; Check(mesh.uv[start].y == 0 && mesh.uv[start].x == 0 && mesh.uv[start + 1].x == 1, "Lane shoulders start at expected texture coordinates");
            float distance = 0;
            for (int i = 1; i < 4; i++)
            {
                var a = (mesh.vertices[start + (i - 1) * 2] + mesh.vertices[start + (i - 1) * 2 + 1]) * .5f;
                var b = (mesh.vertices[start + i * 2] + mesh.vertices[start + i * 2 + 1]) * .5f;
                distance += Vector3.Distance(a, b);
                Check(Math.Abs(mesh.uv[start + i * 2].y - distance / 2.5f) < .0001f && mesh.uv[start + i * 2].y == mesh.uv[start + i * 2 + 1].y, "Worn texture repeats per travelled metre");
            }
            Check(mesh.uv[start + 6].y > 1, "Route does not stretch one tiny tread texture over the yard");
        }
        var privateMeshes = new HashSet<Mesh>(); var sharedMeshes = new HashSet<Mesh>();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.GetComponent<AuthoredFixtureMarker>() != null) { sharedMeshes.Add(filter.sharedMesh); continue; }
            Check(filter.GetComponent<ProceduralMeshOwner>().mesh == filter.sharedMesh && privateMeshes.Add(filter.sharedMesh), "Every actual procedural surface has distinct private ownership");
            foreach (var vertex in filter.sharedMesh.vertices)
                Check(!float.IsNaN(vertex.x) && !float.IsInfinity(vertex.x) && !float.IsNaN(vertex.y) && !float.IsInfinity(vertex.y) && !float.IsNaN(vertex.z) && !float.IsInfinity(vertex.z), "World generated finite geometry");
        }
        UnityEngine.Object.DestroyImmediate(root);
        foreach (var owned in privateMeshes) Check(owned.destroyed, "Actual source owner callback destroys environment mesh");
        foreach (var shared in sharedMeshes) Check(!shared.destroyed, "Imported fixture mesh remains untouched");
    }
    public static int Main()
    {
        try
        {
            BuildCase(false); BuildCase(true);
            Console.WriteLine("PASS reference environment: actual CompactYardWorld/YardGeometry with combine off/on; 21 cover boxes, 1 added roof caster, 36 boundary screens, 41 rails, retained 19 colliders, upward routes, metre UV travel and private cleanup; " + checks + " throwing adapter assertions. NOT Unity engine evidence.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
}
