using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Draws a white surface pass on existing meshes without replacing their materials or colliders.</summary>
    public sealed class ObjectPulseOverlay
    {
        readonly struct Surface
        {
            public readonly MeshRenderer renderer;
            public readonly Mesh mesh;
            public Surface(MeshRenderer renderer, Mesh mesh) { this.renderer = renderer; this.mesh = mesh; }
        }

        readonly List<Surface> surfaces = new List<Surface>();
        readonly Material material;
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        static readonly int Opacity = Shader.PropertyToID("_Opacity");
        public int SurfaceCount => surfaces.Count;

        public ObjectPulseOverlay(Transform target, Material overlayMaterial)
        {
            material = overlayMaterial;
            if (!target) return;
            foreach (var renderer in target.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter && filter.sharedMesh) surfaces.Add(new Surface(renderer, filter.sharedMesh));
            }
        }

        public void Draw(float opacity, Camera camera = null)
        {
            if (!material || opacity <= 0) return;
            properties.SetFloat(Opacity, Mathf.Clamp01(opacity));
            foreach (var surface in surfaces)
            {
                var renderer = surface.renderer;
                if (!renderer || !surface.mesh || !renderer.enabled || renderer.forceRenderingOff
                    || !renderer.gameObject.activeInHierarchy) continue;
                for (int submesh = 0; submesh < surface.mesh.subMeshCount; submesh++)
                    Graphics.DrawMesh(surface.mesh, renderer.localToWorldMatrix, material,
                        renderer.gameObject.layer, camera, submesh, properties, ShadowCastingMode.Off, false,
                        null, LightProbeUsage.Off);
            }
        }
    }
}
