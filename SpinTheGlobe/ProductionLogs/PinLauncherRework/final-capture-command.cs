using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
internal class CommandScript : IRunCommand {
 public void Execute(ExecutionResult result) {
 if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Must capture Edit Mode");
 var cam=Camera.main;var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
 var saved=new Dictionary<UnityEngine.Object,string>();saved[cam]=EditorJsonUtility.ToJson(cam);
 var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
 foreach(var c in canvases){saved[c]=EditorJsonUtility.ToJson(c);foreach(var t in c.GetComponentsInChildren<RectTransform>(true))saved[t]=EditorJsonUtility.ToJson(t);}
 RenderTexture rt=null;Texture2D tex=null;
 try{
 rt=new RenderTexture(1920,1080,24);rt.Create();cam.targetTexture=rt;cam.aspect=1920f/1080;
 foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;}
 Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
 tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();var bytes=tex.EncodeToPNG();
 File.WriteAllBytes("Assets/DetectiveDesk/PinLauncherRework/preview.png",bytes);File.WriteAllBytes("ProductionLogs/PinLauncherRework/after-game-camera.png",bytes);
 } finally {
 cam.targetTexture=oldTarget;
 foreach(var p in saved.Where(p=>p.Key is Camera||p.Key is Canvas))EditorJsonUtility.FromJsonOverwrite(p.Value,p.Key);
 foreach(var p in saved.Where(p=>p.Key is RectTransform))EditorJsonUtility.FromJsonOverwrite(p.Value,p.Key);
 RenderTexture.active=oldActive;if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(tex!=null)UnityEngine.Object.DestroyImmediate(tex);
 }
 foreach(var p in saved)if(EditorJsonUtility.ToJson(p.Key)!=p.Value)throw new Exception("Restoration mismatch "+p.Key.name);
 result.Log("1920x1080 final capture complete; camera, canvases and all affected RectTransforms restored exactly");
 }
}