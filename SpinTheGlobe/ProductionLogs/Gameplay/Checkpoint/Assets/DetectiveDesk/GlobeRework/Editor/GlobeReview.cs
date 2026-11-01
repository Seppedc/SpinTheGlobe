using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;

[InitializeOnLoad]
public static class GlobeReview
{
    static Transform[] moving; static Quaternion[] rotations; static float started; static int frame; static bool? background;
    static GlobeReview(){EditorApplication.update+=Tick;}
    public static void FinalInspection()
    {
        var s=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||s.isDirty)throw new Exception("Expected clean Edit Mode");
        GlobeIntegration.Snapshot("ProductionLogs/GlobeRework/after-runtime.json");
        var root=GlobeIntegration.Root;var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();var textures=new JArray();
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{GlobeIntegration.Dir+"Textures"})) {var path=AssetDatabase.GUIDToAssetPath(guid);var ti=(TextureImporter)AssetImporter.GetAtPath(path);var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);textures.Add(new JObject{{"path",path},{"width",t.width},{"height",t.height},{"sRGB",ti.sRGBTexture},{"type",ti.textureType.ToString()},{"max_size",ti.maxTextureSize}});}
        var mech=new SerializedObject(root.GetComponent("DetectiveGlobeMechanism"));var refs=new JObject();foreach(var name in new[]{"rotatingSphere","rpmNeedle"})refs[name]=mech.FindProperty(name).objectReferenceInstanceIDValue;
        File.WriteAllText("ProductionLogs/GlobeRework/final-unity-inspection.json",new JObject{{"edit_mode",true},{"scene_clean",!s.isDirty},{"scene",s.path},{"renderer_count",renderers.Length},{"mesh_count",renderers.Length},{"missing_materials",renderers.Sum(r=>r.sharedMaterials.Count(m=>!m))},{"triangles",renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3)},{"root_world_position",root.position.ToString("F7")},{"root_scale",root.localScale.ToString("F7")},{"bindings",refs},{"textures",textures}}.ToString());
    }
    public static void Capture()
    {
        var camera=Camera.main;var oldTarget=camera.targetTexture;float aspect=camera.aspect;var active=RenderTexture.active;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isActiveAndEnabled).ToArray();
        var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var planes=canvases.Select(c=>c.planeDistance).ToArray();
        var rects=canvases.SelectMany(c=>c.GetComponentsInChildren<RectTransform>(true)).Distinct().ToArray();var states=rects.Select(r=>EditorJsonUtility.ToJson(r)).ToArray();
        var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try {for(int i=0;i<canvases.Length;i++)if(modes[i]==RenderMode.ScreenSpaceOverlay){canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=1;}
            camera.targetTexture=rt;camera.aspect=1920f/1080;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();var bytes=tex.EncodeToPNG();File.WriteAllBytes(GlobeIntegration.Dir+"preview.png",bytes);File.WriteAllBytes("ProductionLogs/GlobeRework/after-game-camera.png",bytes);
        }finally {camera.targetTexture=oldTarget;camera.aspect=aspect;RenderTexture.active=active;for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=planes[i];}for(int i=0;i<rects.Length;i++)EditorJsonUtility.FromJsonOverwrite(states[i],rects[i]);UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    public static void StartRuntime(){if(SceneManager.GetActiveScene().isDirty)throw new Exception("Save validated scene first");SessionState.SetBool("GlobeRework.Runtime",true);EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!SessionState.GetBool("GlobeRework.Runtime",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        // The relay leaves the editor unfocused. This affects only this Play session.
        if(!background.HasValue)background=Application.runInBackground;
        Application.runInBackground=true;
        File.WriteAllText("ProductionLogs/GlobeRework/runtime-progress.json",new JObject{{"frame",Time.frameCount},{"time",Time.time},{"started",started},{"has_sample",moving!=null}}.ToString());
        if(Time.frameCount<5)return;
        try
        {
            var root=GlobeIntegration.Root;var mech=new SerializedObject(root.GetComponent("DetectiveGlobeMechanism"));
            if(moving==null){moving=new[]{(Transform)mech.FindProperty("rotatingSphere").objectReferenceValue,(Transform)mech.FindProperty("gears").GetArrayElementAtIndex(0).objectReferenceValue,(Transform)mech.FindProperty("gears").GetArrayElementAtIndex(1).objectReferenceValue,(Transform)mech.FindProperty("gears").GetArrayElementAtIndex(2).objectReferenceValue};rotations=moving.Select(t=>t.localRotation).ToArray();started=Time.time;frame=Time.frameCount;return;}
            if(Time.time-started<2||Time.frameCount-frame<30)return;
            float elapsed=Time.time-started;var delta=moving.Select((t,i)=>Quaternion.Angle(rotations[i],t.localRotation)).ToArray();var ratios=new[]{1f,1.6f,1.2f,1.37142857f};bool motion=true;for(int i=0;i<4;i++)motion&=Mathf.Abs(delta[i]-7*elapsed*ratios[i])<.5f;
            var needle=(Transform)mech.FindProperty("rpmNeedle").objectReferenceValue;float angle=Mathf.DeltaAngle(0,needle.localEulerAngles.z);
            var interact=new SerializedObject(root.GetComponent("DeskInteractable"));var game=(Component)interact.FindProperty("game").objectReferenceValue;var gs=new SerializedObject(game);bool binding=gs.FindProperty("globeRotor").objectReferenceValue==moving[0];
            game.SendMessage("InspectGlobe",SendMessageOptions.RequireReceiver);var hint=gs.FindProperty("hintText").objectReferenceValue;string text=new SerializedObject(hint).FindProperty("m_Text").stringValue;
            bool callback=text.StartsWith("Mechanical globe")&&text.Contains("7");
            File.WriteAllText("ProductionLogs/GlobeRework/runtime-validation.json",new JObject{{"status",motion&&binding&&callback&&Mathf.Abs(angle-72)<.5?"PASS":"FAIL"},{"elapsed_seconds",elapsed},{"frames",Time.frameCount-frame},{"rotation_degrees",new JArray(delta)},{"expected_abs_ratios",new JArray(ratios)},{"needle_angle",angle},{"game_sphere_binding",binding},{"InspectGlobe_callback",callback},{"hud_text",text},{"physical_mouse_click_tested",false}}.ToString());
        }catch(Exception e){File.WriteAllText("ProductionLogs/GlobeRework/runtime-validation.json",new JObject{{"status","FAIL"},{"error",e.ToString()}}.ToString());}
        if(background.HasValue)Application.runInBackground=background.Value;
        SessionState.SetBool("GlobeRework.Runtime",false);EditorApplication.isPlaying=false;
    }
}
