using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>Refreshes existing baked visual assets, preserving prefab and scene references.</summary>
    public static class PinCofferProductionPipeline
    {
        public const string SourcePath = "Assets/DetectiveDesk/Art/Props/PinCoffer/Models/PinCoffer.fbx";
        public const string PrefabPath = "Assets/DetectiveDesk/Art/Props/PinCoffer/Prefabs/DetectivePinCoffer.prefab";
        const string MeshFolder = "Assets/DetectiveDesk/Art/Props/PinCoffer/Meshes/";

        [MenuItem("Tools/Detective Desk/Blender Assets/Refresh Pin Coffer From Blender")]
        public static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before refreshing production meshes.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!source || !prefab) throw new InvalidOperationException("Missing canonical FBX or existing coffer prefab.");
            var sources = source.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.name);
            var targets = prefab.GetComponentsInChildren<MeshFilter>(false);
            if (sources.Count != 74 || targets.Length != 74 ||
                prefab.GetComponentsInChildren<PinCofferSlot>(true).Length != 12)
                throw new InvalidOperationException("Unexpected coffer part/slot count; inspect before updating.");

            // Build and validate every replacement before changing any persistent asset.
            var planned = new List<KeyValuePair<Mesh, Mesh>>();
            try
            {
                foreach (var target in targets)
                {
                    if (!sources.TryGetValue(target.name, out var original) || !target.sharedMesh ||
                        !AssetDatabase.GetAssetPath(target.sharedMesh).StartsWith(MeshFolder, StringComparison.Ordinal))
                        throw new InvalidOperationException("Unmatched or unexpected visual: " + target.name);
                    var renderer = target.GetComponent<Renderer>();
                    if (!renderer || renderer.sharedMaterials.Length != original.sharedMesh.subMeshCount ||
                        renderer.sharedMaterials.Any(m => !m || !m.shader ||
                            m.shader.name != "Universal Render Pipeline/Lit" || !m.shader.isSupported))
                        throw new InvalidOperationException("Invalid preserved materials: " + target.name);
                    planned.Add(new KeyValuePair<Mesh, Mesh>(target.sharedMesh, Bake(original, source.transform)));
                }
                int changed = 0;
                foreach (var pair in planned)
                {
                    if (Equivalent(pair.Key, pair.Value)) continue;
                    Undo.RegisterCompleteObjectUndo(pair.Key, "Refresh coffer visual from canonical Blender source");
                    EditorUtility.CopySerialized(pair.Value, pair.Key);
                    EditorUtility.SetDirty(pair.Key);
                    AssetDatabase.SaveAssetIfDirty(pair.Key);
                    changed++;
                }
                Debug.Log("PinCoffer: verified 74 canonical meshes; refreshed " + changed +
                    " existing mesh assets in place. Prefab, transforms, materials, colliders, scripts and slots retained.");
            }
            finally
            {
                foreach (var pair in planned) UnityEngine.Object.DestroyImmediate(pair.Value);
            }
        }

        static Mesh Bake(MeshFilter original, Transform sourceRoot)
        {
            var src = original.sharedMesh;
            using (var data = MeshUtility.AcquireReadOnlyMeshData(src))
            {
                var meshData = data[0];
                using (var vertices = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp))
                using (var normals = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp))
                using (var tangents = new NativeArray<Vector4>(meshData.vertexCount, Allocator.Temp))
                {
                    meshData.GetVertices(vertices);
                    var matrix = sourceRoot.worldToLocalMatrix * original.transform.localToWorldMatrix;
                    // Existing prefab parts already hold the authored pivot translations and insert lift.
                    // Bake only the source linear transform, leaving every gameplay Transform untouched.
                    matrix.SetColumn(3, new Vector4(0, 0, 0, 1));
                    var normalMatrix = matrix.inverse.transpose;
                    var transformedVertices = vertices.ToArray();
                    for (int i = 0; i < transformedVertices.Length; i++) transformedVertices[i] = matrix.MultiplyVector(transformedVertices[i]);
                    var mesh = new Mesh { name = original.name, indexFormat = src.indexFormat };
                    mesh.vertices = transformedVertices;
                    if (meshData.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal))
                    {
                        meshData.GetNormals(normals);
                        var transformedNormals = normals.ToArray();
                        for (int i = 0; i < transformedNormals.Length; i++) transformedNormals[i] = normalMatrix.MultiplyVector(transformedNormals[i]).normalized;
                        mesh.normals = transformedNormals;
                    }
                    if (meshData.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
                    {
                        meshData.GetTangents(tangents);
                        var transformedTangents = tangents.ToArray();
                        for (int i = 0; i < transformedTangents.Length; i++)
                        {
                            var t = transformedTangents[i];
                            var xyz = matrix.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                            transformedTangents[i] = new Vector4(xyz.x, xyz.y, xyz.z, t.w * Mathf.Sign(matrix.determinant));
                        }
                        mesh.tangents = transformedTangents;
                    }
                    for (int channel = 0; channel < 8; channel++)
                    {
                        var attribute = (UnityEngine.Rendering.VertexAttribute)((int)UnityEngine.Rendering.VertexAttribute.TexCoord0 + channel);
                        if (!meshData.HasVertexAttribute(attribute)) continue;
                        using (var uvs = new NativeArray<Vector2>(meshData.vertexCount, Allocator.Temp))
                        { meshData.GetUVs(channel, uvs); mesh.SetUVs(channel, uvs); }
                    }
                    mesh.subMeshCount = meshData.subMeshCount;
                    for (int s = 0; s < meshData.subMeshCount; s++)
                    {
                        var desc = meshData.GetSubMesh(s);
                        if (desc.topology != MeshTopology.Triangles) throw new InvalidOperationException("Expected triangle geometry.");
                        using (var indices = new NativeArray<int>(desc.indexCount, Allocator.Temp))
                        {
                            meshData.GetIndices(indices, s);
                            var transformedIndices = indices.ToArray();
                            if (matrix.determinant < 0)
                                for (int i = 0; i < transformedIndices.Length; i += 3)
                                { int tmp = transformedIndices[i + 1]; transformedIndices[i + 1] = transformedIndices[i + 2]; transformedIndices[i + 2] = tmp; }
                            mesh.SetIndices(transformedIndices, MeshTopology.Triangles, s);
                        }
                    }
                    mesh.RecalculateBounds();
                    return mesh;
                }
            }
        }

        static bool Equivalent(Mesh existing, Mesh generated)
        {
            if (existing.vertexCount != generated.vertexCount || existing.subMeshCount != generated.subMeshCount) return false;
            using (var data = MeshUtility.AcquireReadOnlyMeshData(existing))
            using (var vertices = new NativeArray<Vector3>(existing.vertexCount, Allocator.Temp))
            using (var normals = new NativeArray<Vector3>(existing.vertexCount, Allocator.Temp))
            {
                data[0].GetVertices(vertices);
                var other = generated.vertices;
                for (int i = 0; i < vertices.Length; i++) if ((vertices[i] - other[i]).sqrMagnitude > 1e-14f) return false;
                if (data[0].HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal))
                {
                    data[0].GetNormals(normals); var otherNormals = generated.normals;
                    if (otherNormals.Length != normals.Length) return false;
                    for (int i = 0; i < normals.Length; i++) if ((normals[i] - otherNormals[i]).sqrMagnitude > 1e-10f) return false;
                }
                for (int s = 0; s < existing.subMeshCount; s++)
                {
                    var desc = data[0].GetSubMesh(s);
                    var otherIndices = generated.GetIndices(s);
                    if (desc.indexCount != otherIndices.Length) return false;
                    using (var indices = new NativeArray<int>(desc.indexCount, Allocator.Temp))
                    {
                        data[0].GetIndices(indices, s);
                        for (int i = 0; i < indices.Length; i++) if (indices[i] != otherIndices[i]) return false;
                    }
                }
                for (int c = 0; c < 8; c++)
                {
                    var attr = (UnityEngine.Rendering.VertexAttribute)((int)UnityEngine.Rendering.VertexAttribute.TexCoord0 + c);
                    bool has = data[0].HasVertexAttribute(attr);
                    var otherUVs = new List<Vector2>(); generated.GetUVs(c, otherUVs);
                    if (has != (otherUVs.Count > 0)) return false;
                    if (!has) continue;
                    using (var uvs = new NativeArray<Vector2>(existing.vertexCount, Allocator.Temp))
                    {
                        data[0].GetUVs(c, uvs);
                        for (int i = 0; i < uvs.Length; i++) if ((uvs[i] - otherUVs[i]).sqrMagnitude > 1e-12f) return false;
                    }
                }
                return true;
            }
        }
    }
}
