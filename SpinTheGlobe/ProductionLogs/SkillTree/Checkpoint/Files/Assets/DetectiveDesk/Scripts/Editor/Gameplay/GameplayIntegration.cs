using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DetectiveDesk.Gameplay;

namespace DetectiveDesk.Editor
{
    public static class GameplayIntegration
    {
        const string Log = "ProductionLogs/Gameplay/";
        const string Data = "Assets/DetectiveDesk/Gameplay/Data/";
        static Transform[] All => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        static Transform Find(string name) => All.First(t => t.name == name);
        static T Add<T>(GameObject g) where T : Component => g.GetComponent<T>() ?? g.AddComponent<T>();
        static GameObject Child(string name, Transform parent) { var g = new GameObject(name); g.transform.SetParent(parent, false); return g; }
        static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
        static Material Mat(string name, Color color)
        {
            string path = Data + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(m, path); }
            return m;
        }
        static LineRenderer Line(string name, Transform parent, Material material, float width, bool world = true)
        {
            var l = Child(name, parent).AddComponent<LineRenderer>(); l.sharedMaterial = material; l.widthMultiplier = width;
            l.useWorldSpace = world; l.positionCount = 0; l.numCapVertices = 3; l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false;
            return l;
        }
        static Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, int fontSize = 20)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); g.transform.SetParent(parent, false);
            var t = g.GetComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = fontSize; t.color = new Color(.89f,.79f,.58f); t.text = text; t.raycastTarget = false;
            var r = t.rectTransform; r.anchorMin = r.anchorMax = new Vector2(.5f,.5f); r.anchoredPosition = position; r.sizeDelta = size; return t;
        }
        static Button Button(Transform parent, string name, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(Button)); g.transform.SetParent(parent,false);
            var r=g.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.anchoredPosition=position; r.sizeDelta=new Vector2(112,42);
            g.GetComponent<UnityEngine.UI.Image>().color=new Color(.32f,.25f,.13f,.95f); var b=g.GetComponent<Button>(); b.targetGraphic=g.GetComponent<UnityEngine.UI.Image>(); UnityEventTools.AddPersistentListener(b.onClick,action);
            Label("Caption",g.transform,Vector2.zero,new Vector2(105,35),name,17).alignment=TextAnchor.MiddleCenter; return b;
        }
        public static void Baseline()
        {
            var list = new List<object>();
            foreach(var t in All) foreach(var c in t.GetComponents<Component>()) if(c)
                list.Add(new {id=GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(), path=PathOf(t), type=c.GetType().FullName, json=EditorJsonUtility.ToJson(c), refs=References(c)});
            File.WriteAllText(Log+"integration-components-before.json",JsonConvert.SerializeObject(list));
        }
        static Dictionary<string,string> References(Component c)
        {
            var d=new Dictionary<string,string>(); var s=new SerializedObject(c); var p=s.GetIterator();
            while(p.Next(true)) if(p.propertyType==SerializedPropertyType.ObjectReference) d[p.propertyPath]=p.objectReferenceValue?GlobalObjectId.GetGlobalObjectIdSlow(p.objectReferenceValue).ToString():"";
            return d;
        }
        public static void Setup()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new Exception("Setup requires clean Edit Mode; unsaved edits preserved.");
            if(UnityEngine.Object.FindFirstObjectByType<GameManager>()) throw new Exception("Gameplay already wired; do not duplicate.");
            Baseline();
            Directory.CreateDirectory(Data); Directory.CreateDirectory("Assets/DetectiveDesk/Gameplay/References");
            foreach(var f in Directory.GetFiles(Log+"Data")) File.Copy(f,"Assets/DetectiveDesk/Gameplay/References/"+System.IO.Path.GetFileName(f),true);
            AssetDatabase.Refresh();
            var config=ScriptableObject.CreateInstance<UpgradeConfig>(); AssetDatabase.CreateAsset(config,Data+"UpgradeConfig.asset");
            var root=Find("DetectiveOffice"); var legacy=root.GetComponent<DetectiveGame>(); var gm=Add<GameManager>(root.gameObject); gm.legacy=legacy; gm.config=config;
            var globeRoot=Find("MechanicalGlobe"); var globe=Add<GlobeController>(globeRoot.gameObject); gm.globe=globe; globe.game=gm; globe.rotor=legacy.globeRotor; globe.gameCamera=Camera.main; globe.dragCollider=globeRoot.GetComponent<Collider>();
            if(!globe.dragCollider) throw new Exception("Original globe collider missing");
            var logic=Child("GameplayIntegration",root); var map=logic.AddComponent<WallMapManager>(); gm.map=map; map.game=gm; map.globe=globe; map.gameCamera=Camera.main;
            map.runtimeRoot=Child("RuntimeRoot",root).transform; map.mapSurface=Find("CLEAN MAP - replace texture here"); map.muzzle=Find("MuzzleTip"); map.launcherVisual=Find("Recoil_Slide_Pivot");
            map.mapBottomLeft=new Vector3(-3.01f,1.235547f,1.95349991f); map.mapBottomRight=new Vector3(4.11f,1.235547f,1.95349991f); map.mapTopLeft=new Vector3(-3.01f,4.364453f,1.95349991f);
            map.pinPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DetectiveDesk/Prefabs/CollectiblePin.prefab").GetComponent<PinSkin>();
            var brass=Mat("MeridianBrass",new Color(.53f,.34f,.12f)); var red=Mat("CrimsonYarn",new Color(.38f,.025f,.02f)); var border=Mat("CountryOutline",new Color(.35f,.30f,.22f)); var glue=Mat("DarkGlue",new Color(.035f,.03f,.025f)); var gold=Mat("GoldSeams",new Color(1,.59f,.1f));
            var meridian=logic.AddComponent<MeridianTrigger>(); map.meridian=meridian; meridian.globe=globe; meridian.map=map; meridian.wireMaterial=brass;
            meridian.stationaryWire=Line("Stationary front meridian",logic.transform,brass,.003f); meridian.primeRibbon=Line("Prime meridian ribbon",globe.rotor,red,.0025f,false);
            meridian.SendMessage("Start");
            var yarn=logic.AddComponent<YarnRenderer>(); map.yarn=yarn; yarn.line=Line("Pin yarn",logic.transform,red,.005f);
            var dust=Child("Cork dust",logic.transform).AddComponent<ParticleSystem>(); dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); var main=dust.main; main.playOnAwake=false; main.loop=false; main.duration=.3f; main.startLifetime=.35f; main.startSpeed=.12f; main.startSize=.009f; main.startColor=new Color(.56f,.36f,.16f); main.maxParticles=128; main.simulationSpace=ParticleSystemSimulationSpace.World; main.gravityModifier=.25f;
            var emission=dust.emission; emission.enabled=false; var shape=dust.shape; shape.enabled=false; dust.GetComponent<ParticleSystemRenderer>().sharedMaterial=Mat("CorkDust",new Color(.56f,.36f,.16f)); map.corkDust=dust;
            var audio=logic.AddComponent<Gameplay.AudioManager>(); gm.audioManager=audio; audio.source=logic.AddComponent<AudioSource>(); audio.source.playOnAwake=false; audio.source.spatialBlend=0;
            gm.archive=UnityEngine.Object.FindFirstObjectByType<DeskAssetPresentation>(); gm.pinSlots=UnityEngine.Object.FindObjectsByType<PinCofferSlot>(FindObjectsSortMode.None).OrderBy(s=>s.SlotNumber).ToArray();
            var countries=JObject.Parse(File.ReadAllText(Log+"Data/countries.json"))["countries"]; var targets=new List<CountryTarget>();
            foreach(var item in countries)
            {
                var data=ScriptableObject.CreateInstance<CountryData>(); JsonUtility.FromJsonOverwrite(item.ToString(),data); AssetDatabase.CreateAsset(data,Data+data.id+".asset");
                var target=Child(data.id,logic.transform).AddComponent<CountryTarget>(); target.data=data; target.map=map; targets.Add(target);
                foreach(var ring in data.boundaries)
                {
                    var points=new List<Vector3>(); int part=0;
                    Action flush=()=> {if(points.Count>1) { var l=Line(data.id+" boundary "+part++,target.transform,border,.002f); l.SetPositions(points.ToArray()); l.positionCount=points.Count; l.SetPositions(points.ToArray()); } points.Clear();};
                    for(int i=0;i<ring.points.Length;i++) {var p=ring.points[i]; if(i>0 && Mathf.Abs(p.x-ring.points[i-1].x)>180) flush(); if(p.y < map.minimumLatitude || p.y>map.maximumLatitude) {flush();continue;} points.Add(map.GeographicPoint(p.y,p.x));} flush();
                }
            }
            map.targets=targets.ToArray();
            var figures=new List<FigurinePedestal>();
            for(int i=0;i<2;i++) {var slot=Find("StatueCollectibleSlot_"+i); var bust=slot.GetComponentsInChildren<Transform>(true).First(t=>t.name.EndsWith("_BustModule")&&t.gameObject.activeInHierarchy); figures.Add(ConfigureFigure(slot,bust,gm,glue));}
            var third=Child("StatueCollectibleSlot_2",Find("GameplayObjects")).transform;
            var originalBase=Find("StatueCollectibleSlot_0").GetComponentsInChildren<Transform>(true).First(t=>t.name=="DisplayBase_01");
            var baseClone=UnityEngine.Object.Instantiate(originalBase.gameObject,third).transform; baseClone.name="Commissioner display base"; baseClone.rotation=originalBase.rotation; baseClone.localScale=originalBase.lossyScale;
            baseClone.position=originalBase.position+new Vector3(.70f,0,.70f);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DetectiveDesk/AssetProduction/CollectibleStatues/20261031_011441_91ecff05/CollectibleStatues_Review.prefab");
            var source=prefab.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Commissioner_Assembly");
            var exemplar=Find("StatueCollectibleSlot_0").GetComponentsInChildren<Transform>(true).First(t=>t.name=="Detective_Assembly"&&t.gameObject.activeInHierarchy);
            var commissioner=UnityEngine.Object.Instantiate(source.gameObject,third).transform; commissioner.name="Commissioner_Assembly"; commissioner.gameObject.SetActive(true); commissioner.position=exemplar.position+new Vector3(.70f,0,.70f); commissioner.rotation=exemplar.rotation; commissioner.localScale=exemplar.lossyScale;
            var thirdBust=commissioner.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Commissioner_BustModule"); figures.Add(ConfigureFigure(third,thirdBust,gm,glue));
            var box=third.gameObject.AddComponent<BoxCollider>(); box.center=new Vector3(-2.85f,1.35f,.4f); box.size=new Vector3(.55f,.95f,.55f); var interact=third.gameObject.AddComponent<DeskInteractable>(); interact.game=legacy; interact.action=DeskInteractable.Action.Statue;
            gm.figurines=figures.ToArray();
            var panel=legacy.upgradesPanel; var pr=panel.GetComponent<RectTransform>(); pr.sizeDelta=new Vector2(720,600);
            foreach(Transform t in panel.transform) {if(t.name.Contains("CLOSE")) {var rt=t.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f); rt.anchoredPosition=new Vector2(0,-255);} else t.gameObject.SetActive(false);}
            var ui=Child("Gameplay controls",panel.transform); ui.AddComponent<RectTransform>(); var ur=ui.GetComponent<RectTransform>(); ur.anchorMin=Vector2.zero;ur.anchorMax=Vector2.one;ur.offsetMin=ur.offsetMax=Vector2.zero;
            var gp=ui.AddComponent<GameplayPanel>(); gp.game=gm; gm.panel=gp;
            Label("Heading",ui.transform,new Vector2(0,258),new Vector2(640,40),"WORKSHOP / RESTORATIONS",26);
            gp.summary=Label("Summary",ui.transform,new Vector2(0,211),new Vector2(640,35),"",19);
            gp.upgradeLabels=new Text[3];gp.figurineLabels=new Text[3]; gp.upgradeButtons=new Button[3];gp.figurineButtons=new Button[3];
            UnityEngine.Events.UnityAction[] calls={gp.BuySpeed,gp.BuyMotor,gp.BuyDurability,gp.RestoreFirst,gp.RestoreSecond,gp.RestoreThird};
            for(int i=0;i<6;i++) {float y=151-i*62; var text=Label("Row "+i,ui.transform,new Vector2(-61,y),new Vector2(488,45),"",19);var b=Button(ui.transform,i<3?"UPGRADE":"RESTORE",new Vector2(262,y),calls[i]); if(i<3){gp.upgradeLabels[i]=text;gp.upgradeButtons[i]=b;}else{gp.figurineLabels[i-3]=text;gp.figurineButtons[i-3]=b;}}
            Label("Controls",ui.transform,new Vector2(0,-216),new Vector2(650,44),"Drag left to right / choose a coffer color / pin a country\nIncome shown per minute is an estimate from rotations.",16); gp.Refresh();
            var tach=logic.AddComponent<TachometerUI>(); tach.globe=globe; tach.gaugeFace=All.First(t=>t.name=="RPMGauge_PrintedDial"&&t.parent.name=="GlobeRework_Static").GetComponent<Renderer>();
            ConfigureTachometer(tach);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.WriteAllText(Log+"integration-result.json",JsonConvert.SerializeObject(new {status="WIRED_AWAITING_VALIDATION",countries=8,figurines=3,slots=gm.pinSlots.Length,scene=SceneManager.GetActiveScene().path},Formatting.Indented));
        }
        public static void ConfigureTachometer(TachometerUI tach)
        {
            tach.needle=tach.globe.GetComponent<DetectiveGlobeMechanism>().rpmNeedle;
            var module=Find("GlobeRework_Needle");
            // Restore the baseline visual-child identity pose. Runtime angles belong
            // exclusively to the original mechanism's pivot, whose Edit Mode pose stays intact.
            module.localPosition=Vector3.zero;module.localRotation=Quaternion.identity;module.localScale=Vector3.one;
            var existing=All.FirstOrDefault(t=>t.name=="Gauge RPM label");
            var canvas=existing?existing.GetComponent<Canvas>():Child("Gauge RPM label",tach.transform).AddComponent<Canvas>();
            canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=tach.globe.gameCamera;
            var cr=canvas.GetComponent<RectTransform>();cr.sizeDelta=new Vector2(180,28);cr.position=tach.gaugeFace.bounds.center+new Vector3(.46f,-.025f,-.05f);cr.rotation=tach.globe.gameCamera.transform.rotation;cr.localScale=Vector3.one*.0035f;
            if(!tach.speedText)tach.speedText=Label("Actual globe RPM",canvas.transform,Vector2.zero,new Vector2(180,28),"0.0 / 5.0 RPM",16);
            else {tach.speedText.transform.SetParent(canvas.transform,false);var r=tach.speedText.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(180,28);r.localRotation=Quaternion.identity;r.localScale=Vector3.one;}
            tach.speedText.alignment=TextAnchor.MiddleCenter;tach.speedText.fontSize=16;tach.speedText.raycastTarget=false;
            EditorUtility.SetDirty(tach);
        }
        static FigurinePedestal ConfigureFigure(Transform slot,Transform bust,GameManager game,Material seamMaterial)
        {
            var f=Add<FigurinePedestal>(slot.gameObject); f.game=game;f.bust=bust;f.bustRenderers=bust.GetComponentsInChildren<Renderer>(true);f.scatterFrame=slot; f.glueMaterial=seamMaterial; f.goldSeamMaterial=AssetDatabase.LoadAssetAtPath<Material>(Data+"GoldSeams.mat");
            var seat=bust.parent.GetComponentsInChildren<Renderer>().First(r=>r.name.EndsWith("_BustSeat"));f.scatterCenter=slot.InverseTransformPoint(new Vector3(seat.bounds.center.x,seat.bounds.max.y,seat.bounds.center.z));
            var seams=new List<LineRenderer>();
            for(int i=0;i<3;i++) {var l=Line("Restoration seam "+i,bust,seamMaterial,.0028f,false);l.positionCount=3;l.SetPositions(new[]{new Vector3(-.016f+i*.014f,-.017f,.035f),new Vector3(-.01f+i*.011f,-.019f,.059f),new Vector3(-.015f+i*.013f,-.016f,.088f)}); l.enabled=false;seams.Add(l);} f.restorationSeams=seams.ToArray();return f;
        }
        public static void Capture(string path)
        {
            var camera=Camera.main;var oldTarget=camera.targetTexture;float oldAspect=camera.aspect;var oldActive=RenderTexture.active; var canvas=Find("GameUI").GetComponent<Canvas>(); var mode=canvas.renderMode;var cam=canvas.worldCamera;float plane=canvas.planeDistance;
            var rects=canvas.GetComponentsInChildren<RectTransform>(true).ToDictionary(r=>r,r=>EditorJsonUtility.ToJson(r));
            var rt=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try {camera.targetTexture=rt;camera.aspect=1920f/1080;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.3f;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally {canvas.renderMode=mode;canvas.worldCamera=cam;canvas.planeDistance=plane;camera.targetTexture=oldTarget;camera.aspect=oldAspect;RenderTexture.active=oldActive;foreach(var r in rects)if(r.Key)EditorJsonUtility.FromJsonOverwrite(r.Value,r.Key);UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
        public static void Preservation()
        {
            var issues=new List<string>();int checkedCount=0;var before=JArray.Parse(File.ReadAllText(Log+"integration-components-before.json"));
            foreach(var item in before)
            {
                GlobalObjectId id;GlobalObjectId.TryParse((string)item["id"],out id);var c=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Component;
                string path=(string)item["path"],type=(string)item["type"];
                if(!c){issues.Add("Missing "+path+" "+type);continue;}checkedCount++;
                if(path.Contains("/Upgrades panel"))continue;
                var old=JObject.Parse((string)item["json"]);var now=JObject.Parse(EditorJsonUtility.ToJson(c));
                // New additive children change only parent child lists.
                old.Remove("m_Children");now.Remove("m_Children");
                if(!JToken.DeepEquals(old,now))issues.Add("Serialized change "+path+" "+type);
                var refs=item["refs"].ToObject<Dictionary<string,string>>();var current=References(c);
                foreach(var r in refs)if(!r.Key.StartsWith("m_Children")&&current.ContainsKey(r.Key)&&current[r.Key]!=r.Value)issues.Add("Reference change "+path+" "+r.Key);
            }
            int missing=All.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));int missingMat=All.SelectMany(t=>t.GetComponents<Renderer>()).Sum(r=>r.sharedMaterials.Count(m=>!m));
            File.WriteAllText(Log+"preservation-validation.json",JsonConvert.SerializeObject(new {status=issues.Count==0&&missing==0&&missingMat==0?"PASS":"FAIL",originalComponentsChecked=checkedCount,issues,missingScripts=missing,missingMaterials=missingMat,play=EditorApplication.isPlaying,dirty=SceneManager.GetActiveScene().isDirty},Formatting.Indented));
            Debug.Log("Preservation: "+issues.Count+" differences / "+checkedCount+" original components; missing scripts="+missing+", materials="+missingMat);
        }
    }
}
