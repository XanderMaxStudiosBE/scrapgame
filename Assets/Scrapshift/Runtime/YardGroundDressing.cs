using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Scrapshift
{
    // Two low-cost meshes per region. Layers receive the existing sun/fog; they add no physics or lights.
    public static class YardGroundDressing
    {
        sealed class Layer
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> triangles = new List<int>();
            public void Patch(float x, float z, float rx, float rz, int tile, float yaw, float height)
            {
                const int sides = 12; int start = vertices.Count;
                float c = Mathf.Cos(yaw * Mathf.Deg2Rad), s = Mathf.Sin(yaw * Mathf.Deg2Rad);
                Vector2 origin = new Vector2(tile % 2 * .5f, tile / 2 * .5f);
                vertices.Add(new Vector3(x,height,z)); uv.Add(origin + new Vector2(.25f,.25f));
                for (int i=0; i<sides; i++)
                {
                    float a = i*Mathf.PI*2/sides, r = 1 + .05f*Mathf.Sin(i*2.1f+x);
                    float u = Mathf.Cos(a), v = Mathf.Sin(a); float dx = u*rx*r, dz = v*rz*r;
                    vertices.Add(new Vector3(x+c*dx+s*dz,height,z-s*dx+c*dz));
                    uv.Add(origin + new Vector2(.25f+u*.24f,.25f+v*.24f));
                    // +Y normals. No coincident opaque faces and no geometry beneath the floor.
                    triangles.Add(start); triangles.Add(start+1+(i+1)%sides); triangles.Add(start+1+i);
                }
            }
            public void Build(Transform parent, string name, Material material)
            {
                if (material == null || vertices.Count == 0) return;
                var go = new GameObject(name); go.transform.SetParent(parent,false);
                var mesh = new Mesh { name = name + " / bounded ground layer" };
                mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<ProceduralMeshOwner>().mesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        public static void Build(Transform parent)
        {
            var wear = Resources.Load<Material>("ScrapshiftWorld/GroundWear");
            var wet = Resources.Load<Material>("ScrapshiftWorld/RoughPuddles");
            for (int region=0; region<4; region++)
            {
                var root = new GameObject("Ground detail region " + region).transform; root.SetParent(parent,false);
                var dirt = new Layer(); var puddles = new Layer();
                float cx = region%2 == 0 ? -24 : 24, cz = region/2 == 0 ? -20 : 20;
                for (int i=0; i<9; i++)
                {
                    float x = cx + (i%3-1)*12 + Mathf.Sin(i*4+region)*2;
                    float z = cz + (i/3-1)*10;
                    dirt.Patch(x,z,3.5f+(i%3),2.3f,i%4==0 ? 3 : 0,i*37,.009f);
                }
                // Small irregular wet patches, rather than a glossy plane covering the yard.
                puddles.Patch(cx-4,cz+4,2.3f,1.0f,2,region*29,.012f);
                puddles.Patch(cx+9,cz-7,1.4f,.65f,2,region*43,.012f);
                // Wheel wear follows the loading/workshop and cross-yard routes.
                float trackX = region%2==0 ? -1.15f : 1.15f;
                dirt.Patch(trackX,cz, .45f,8,1,0,.010f);
                dirt.Patch(cx,-13, .45f,13,1,90,.010f);
                dirt.Build(root,"Dust, moss and delivery tyre wear",wear); puddles.Build(root,"Rough shallow puddles",wet);
            }
        }
    }
}
