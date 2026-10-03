using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Scrapshift
{
    public static class ImportedCarPartsSetup
    {
        const string SourceGuid = "3d4d8d964bef0ef45a6997eec12b5ccb";
        const string SourcePath = "Assets/Junk Car Parts/Models/PistonSmooth.obj";
        const string Folder = "Assets/Scrapshift/Generated/Resources/LocalScrapshiftProps";
        public const string OutputPath = Folder + "/Piston.asset";

        [MenuItem("Scrapshift/Enable Imported Car Parts")]
        public static void Enable()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play mode before enabling imported car parts.");
                return;
            }
            if (File.Exists(OutputPath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(OutputPath);
                Debug.Log(LocalCarPartsVisuals.IsUsable(existing)
                    ? "Imported piston is already enabled. Its local mesh was retained; press Play."
                    : "Existing local Piston.asset was retained. Check its mesh/import settings before enabling it.");
                return;
            }
            string sourcePath = AssetDatabase.GUIDToAssetPath(SourceGuid);
            if (string.IsNullOrEmpty(sourcePath) || !sourcePath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase)) sourcePath = SourcePath;
            var importer = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning("Piston model not found. Import Junkyard Car Parts locally, then run Scrapshift → Enable Imported Car Parts.");
                return;
            }
            bool wasReadable = importer.isReadable;
            Mesh prepared = null;
            try
            {
                if (!wasReadable) { importer.isReadable = true; importer.SaveAndReimport(); }
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source == null) throw new ArgumentException("The installed piston did not import as a model.");
                prepared = PrepareModel(source);
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(prepared, OutputPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Imported piston enabled in the office spares pocket. Local generated mesh uses existing URP metal; press Play. Keep Store sources and Generated files private.");
            }
            catch (Exception error)
            {
                Debug.LogWarning("Imported car parts setup could not finish: " + error.Message + ". Original yard props remain available.");
            }
            finally
            {
                if (prepared != null && !AssetDatabase.Contains(prepared)) UnityEngine.Object.DestroyImmediate(prepared);
                // Only a temporary CPU-read flag changes; restore the creator's original model import setting.
                if (!wasReadable)
                {
                    try
                    {
                        var current = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
                        if (current != null && current.isReadable) { current.isReadable = false; current.SaveAndReimport(); }
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("Could not restore the piston model's original Read/Write import setting: " + error.Message +
                            ". Check the source model Inspector. The prepared local mesh and original yard remain available.");
                    }
                }
            }
        }

        // Geometry-only conversion: no source hierarchy/scripts, materials, colliders or lights are cloned.
        public static Mesh PrepareModel(GameObject source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var parts = new List<CombineInstance>();
            long indices = 0, verticesToCopy = 0;
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable) throw new ArgumentException("Source meshes must be readable while preparing the piston.");
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    if (mesh.GetTopology(submesh) != MeshTopology.Triangles) throw new ArgumentException("Piston must use triangle geometry.");
                    indices += mesh.GetIndexCount(submesh);
                    if (indices > LocalCarPartsVisuals.MaximumTriangles * 3) throw new ArgumentException("Piston exceeds the 1,500-triangle scenery limit.");
                    verticesToCopy += mesh.vertexCount;
                    if (verticesToCopy > LocalCarPartsVisuals.MaximumTriangles * 3) throw new ArgumentException("Piston exceeds the 4,500-vertex scenery limit.");
                    parts.Add(new CombineInstance { mesh = mesh, subMeshIndex = submesh,
                        transform = source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                }
            }
            if (indices == 0) throw new ArgumentException("Piston has no visible mesh geometry.");
            var result = new Mesh { name = "Local piston spare / licensed Store geometry" };
            try
            {
                result.CombineMeshes(parts.ToArray(), true, true);
                result.RecalculateBounds();
                var bounds = result.bounds;
                var size = bounds.size;
                if (size.x <= 0 || size.y <= 0 || size.z <= 0) throw new ArgumentException("Piston bounds are empty.");
                float scale = Mathf.Min(.6f / size.x, Mathf.Min(.6f / size.y, .55f / size.z));
                Vector3 origin = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                var vertices = result.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var vertex = (vertices[i] - origin) * scale;
                    if (float.IsNaN(vertex.x) || float.IsInfinity(vertex.x) || float.IsNaN(vertex.y) || float.IsInfinity(vertex.y) ||
                        float.IsNaN(vertex.z) || float.IsInfinity(vertex.z)) throw new ArgumentException("Piston geometry contains non-finite vertices.");
                    vertices[i] = vertex;
                }
                result.vertices = vertices;
                result.RecalculateBounds();
                if (!LocalCarPartsVisuals.IsUsable(result)) throw new ArgumentException("Prepared piston failed size or geometry validation.");
                return result;
            }
            catch { UnityEngine.Object.DestroyImmediate(result); throw; }
        }
    }
}
