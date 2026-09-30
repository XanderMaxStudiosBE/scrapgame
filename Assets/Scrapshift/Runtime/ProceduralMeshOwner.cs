using UnityEngine;

namespace Scrapshift
{
    // Only attach to a mesh created by these builders; imported/shared meshes are never owned here.
    public sealed class ProceduralMeshOwner : MonoBehaviour
    {
        public Mesh mesh;
        void OnDestroy()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }
    }
}
