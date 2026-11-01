using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace DetectiveDesk.Editor
{
    /// <summary>Only new queue outputs and an isolated review scene are writable here.</summary>
    public static class AssetQueueReview
    {
        const string ReviewPath = "Assets/DetectiveDesk/AssetReview/AssetReview.unity";
        const int ReviewLayer = 31;
        [Serializable] class Job { public string slug, pipeline, output_path, run_id; public bool smoke_test; }
        [Serializable] class Worker { public string run_id; }
        [Serializable] class Queue { public Worker active_worker; }
        [Serializable] class Manifest { public string kind, prefab_path; public string[] asset_paths; }
        [Serializable] class PreflightResult { public string status, scene_path, snapshot_sha256; public int console_errors; }
        [Serializable] class ReviewResult
        {
            public string status, source_sha256, prefab_path, screenshot_path, screenshot_sha256, review_scene;
            public bool protected_scene_unchanged;
            public int console_errors, mesh_count, renderer_count, missing_materials;
        }
        static string Project => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string Safe(string path)
        {
            var full = Path.GetFullPath(Path.Combine(Project, path));
            if (!full.StartsWith(Project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Path outside project: " + path);
            return full;
        }
        static string Sha(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static string SceneSnapshot(Scene scene)
        {
            var rows = new List<string> { scene.path + "|dirty=" + scene.isDirty };
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    foreach (var c in t.GetComponents<Component>())
                        if (c) rows.Add(c.GetEntityId() + "|" + EditorJsonUtility.ToJson(c));
            rows.Sort(StringComparer.Ordinal);
            return string.Join("\n", rows);
        }
        static int ConsoleErrors()
        {
            // Read-only access to Unity's native console; never clears existing diagnostics.
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
            var method = type?.GetMethod("GetCountsByType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("Cannot verify Unity Console counts on this editor version.");
            object[] args = { 0, 0, 0 }; method.Invoke(null, args);
            return (int)args[0];
        }
        static Job LoadJob(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Project-wide Unity Play Mode or compilation is active.");
            var job = JsonUtility.FromJson<Job>(File.ReadAllText(Safe(path)));
            if (job == null || string.IsNullOrEmpty(job.output_path) ||
                !job.output_path.StartsWith("Assets/DetectiveDesk/AssetProduction/", StringComparison.Ordinal))
                throw new InvalidOperationException("Only isolated queue output directories are allowed.");
            Safe(job.output_path);
            var queue = JsonUtility.FromJson<Queue>(File.ReadAllText(Safe("asset_queue.json")));
            if (queue?.active_worker == null || queue.active_worker.run_id != job.run_id)
                throw new InvalidOperationException("Job is not the coordinator's sole active worker.");
            if (ConsoleErrors() != 0) throw new InvalidOperationException("Project-wide Console errors; queue must stop before importing.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == ReviewPath) throw new InvalidOperationException("AssetReview scene already open; preserving that editor session.");
                if (scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Any(t => t.gameObject.layer == ReviewLayer))
                    throw new InvalidOperationException("Review isolation layer is used by an open scene; refusing mixed capture.");
            }
            return job;
        }
        public static void Preflight(string jobFile)
        {
            var job = LoadJob(jobFile); var scene = SceneManager.GetActiveScene();
            Directory.CreateDirectory(Safe(job.output_path));
            var text = SceneSnapshot(scene);
            File.WriteAllText(Safe(job.output_path + "/protected-scene-before.txt"), text);
            File.WriteAllText(Safe(job.output_path + "/unity-preflight.json"), JsonUtility.ToJson(new PreflightResult
            { status = "PASS", scene_path = scene.path, snapshot_sha256 = Sha(Encoding.UTF8.GetBytes(text)), console_errors = 0 }, true));
            Debug.Log("Asset queue preflight passed; original scene preserved, no compilation/Console errors.");
        }
        public static void Review(string jobFile)
        {
            var mutexName = "Local\\DetectiveDeskAssetReview_" + Sha(Encoding.UTF8.GetBytes(Project)).Substring(0, 16);
            using (var mutex = new Mutex(false, mutexName))
            {
                bool acquired;
                try { acquired = mutex.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) throw new InvalidOperationException("Another Unity review writer is active.");
                try { ReviewInternal(jobFile); } finally { mutex.ReleaseMutex(); }
            }
        }
        static void ReviewInternal(string jobFile)
        {
            var job = LoadJob(jobFile);var folder = job.output_path;
            var originalScene = SceneManager.GetActiveScene();
            var before = File.ReadAllText(Safe(folder + "/protected-scene-before.txt"));
            if (SceneSnapshot(originalScene) != before) throw new InvalidOperationException("Protected scene changed since preflight; refusing review.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Safe(folder + "/review_manifest.json")));
            if (manifest?.asset_paths == null || manifest.asset_paths.Length == 0) throw new InvalidOperationException("No review assets.");
            foreach (var path in manifest.asset_paths)
                if (!path.StartsWith(folder + "/", StringComparison.Ordinal) || !File.Exists(Safe(path)))
                    throw new InvalidOperationException("Review input outside this job's output folder.");
            if (!manifest.prefab_path.StartsWith(folder + "/", StringComparison.Ordinal)) throw new InvalidOperationException("Prefab output outside job.");
            var sourcePath = manifest.asset_paths[0];
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader || !shader.isSupported) throw new InvalidOperationException("Project-wide URP/Lit shader unavailable.");
            if (manifest.kind == "model")
            {
                var imported = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (!imported) throw new InvalidOperationException("Model did not import.");
                if (!AssetDatabase.IsValidFolder(folder + "/Materials")) AssetDatabase.CreateFolder(folder, "Materials");
                var importer = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
                importer.importCameras = false; importer.importLights = false; importer.importAnimation = false;
                foreach (var old in imported.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct())
                {
                    if (!old) throw new InvalidOperationException("Imported missing material.");
                    var name = string.Concat(old.name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                    var path = folder + "/Materials/" + name + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!material)
                    {
                        material = new Material(shader) { name = old.name };
                        material.SetColor("_BaseColor", old.HasProperty("_BaseColor") ? old.GetColor("_BaseColor") : old.color);
                        material.SetFloat("_Metallic", old.HasProperty("_Metallic") ? old.GetFloat("_Metallic") : 0);
                        material.SetFloat("_Smoothness", old.HasProperty("_Smoothness") ? old.GetFloat("_Smoothness") : .45f);
                        AssetDatabase.CreateAsset(material, path);
                    }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), old.name), material);
                }
                importer.SaveAndReimport();
            }
            var scene = default(Scene); Texture2D texture = null; RenderTexture render = null;
            var priorRender = RenderTexture.active; ReviewResult report = null;
            try
            {
                Directory.CreateDirectory(Safe("Assets/DetectiveDesk/AssetReview"));
                scene = File.Exists(Safe(ReviewPath)) ? EditorSceneManager.OpenScene(ReviewPath, OpenSceneMode.Additive)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                foreach (var old in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(old);
                GameObject root;
                if (manifest.kind == "texture")
                {
                    var image = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
                    if (!image) throw new InvalidOperationException("Texture import failed.");
                    root = GameObject.CreatePrimitive(PrimitiveType.Quad);root.name = job.slug + "_TextureReview";
                    var textureMaterial = new Material(shader);textureMaterial.SetTexture("_BaseMap", image);
                    AssetDatabase.CreateAsset(textureMaterial, folder + "/TextureReview.mat");
                    root.GetComponent<Renderer>().sharedMaterial = textureMaterial;
                }
                else if (manifest.kind == "material")
                {
                    root = GameObject.CreatePrimitive(PrimitiveType.Sphere);root.name = job.slug + "_MaterialReview";
                    root.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                }
                else
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                    if (!asset) throw new InvalidOperationException("Provide a standalone prefab/model or a material preview manifest.");
                    root = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                }
                root.name = job.slug + "_Review";
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ReviewLayer;
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                int missing = renderers.SelectMany(r => r.sharedMaterials).Count(m => !m || !m.shader || !m.shader.isSupported || m.shader.name == "Hidden/InternalErrorShader");
                if (missing != 0) throw new InvalidOperationException("Missing or pink/unsupported materials.");
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0) throw new InvalidOperationException("Broken review prefab script references.");
                var bounds = new Bounds(Vector3.zero, Vector3.one);
                if (renderers.Length > 0) { bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds); }
                float span = Mathf.Max(.05f, bounds.size.x, bounds.size.y, bounds.size.z);
                var cameraObject = new GameObject("AssetReviewCamera");cameraObject.layer = ReviewLayer;
                var camera = cameraObject.AddComponent<Camera>();camera.cullingMask = 1 << ReviewLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;camera.backgroundColor = new Color(.025f, .03f, .04f);
                camera.transform.position = bounds.center + new Vector3(1.1f, .8f, -1.5f) * span;
                camera.transform.LookAt(bounds.center);camera.nearClipPlane = span * .001f;camera.farClipPlane = span * 20;
                camera.orthographic = true;camera.orthographicSize = span * .72f;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                { canvas.renderMode = RenderMode.ScreenSpaceCamera;canvas.worldCamera = camera;canvas.planeDistance = span; }
                for (int i = 0; i < 2; i++)
                {
                    var obj = new GameObject("ReviewLight" + i);obj.layer = ReviewLayer;
                    var light = obj.AddComponent<Light>();light.type = LightType.Directional;light.intensity = i == 0 ? 1.7f : .7f;
                    light.cullingMask = 1 << ReviewLayer;light.transform.rotation = Quaternion.Euler(i == 0 ? 35 : 20, i == 0 ? -35 : 130, 0);
                }
                RenderSettings.ambientLight = new Color(.18f, .18f, .18f);
                PrefabUtility.SaveAsPrefabAsset(root, manifest.prefab_path);
                if (!EditorSceneManager.SaveScene(scene, ReviewPath)) throw new InvalidOperationException("Review scene save failed.");
                render = new RenderTexture(1200, 900, 24, RenderTextureFormat.ARGB32);camera.targetTexture = render;
                camera.aspect = 1200f / 900f;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active = render;
                texture = new Texture2D(1200, 900, TextureFormat.RGB24, false);texture.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0);texture.Apply();
                string screenshot = folder + "/preview.png";File.WriteAllBytes(Safe(screenshot), texture.EncodeToPNG());camera.targetTexture = null;
                report = new ReviewResult { status = "PASS", source_sha256 = Sha(File.ReadAllBytes(Safe(sourcePath))),
                    prefab_path = manifest.prefab_path, screenshot_path = screenshot, screenshot_sha256 = Sha(File.ReadAllBytes(Safe(screenshot))),
                    review_scene = ReviewPath, mesh_count = root.GetComponentsInChildren<MeshFilter>(true).Length,
                    renderer_count = renderers.Length, missing_materials = missing, console_errors = 0 };
            }
            finally
            {
                RenderTexture.active = priorRender;
                if (texture) UnityEngine.Object.DestroyImmediate(texture);
                if (render) { render.Release();UnityEngine.Object.DestroyImmediate(render); }
                if (originalScene.IsValid()) SceneManager.SetActiveScene(originalScene);
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
            if (SceneSnapshot(originalScene) != before) throw new InvalidOperationException("Protected gameplay scene snapshot changed; queue stopped.");
            if (ConsoleErrors() != 0) throw new InvalidOperationException("Console errors during review; evidence not accepted.");
            report.protected_scene_unchanged = true;
            File.WriteAllText(Safe(folder + "/unity-validation.json"), JsonUtility.ToJson(report, true));
            Debug.Log("Asset queue isolated review PASS; no visual approval implied; original gameplay scene unchanged.");
        }
    }
}
