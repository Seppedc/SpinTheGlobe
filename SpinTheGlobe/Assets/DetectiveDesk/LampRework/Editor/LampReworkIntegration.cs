using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace DetectiveDesk.Editor
{
    // Scoped integration/review tooling. Never rebuilds the canonical Blender source.
    public static class LampReworkIntegration
    {
        const string Out = "Assets/DetectiveDesk/LampRework/";
        const string Logs = "ProductionLogs/LampRework/";
        const string Source = "Assets/DetectiveDesk/ArtSource/Props/BankersLamp/";
        const string RootPath = "DetectiveOffice/Environment/Generated_BankersLamp";
        public const string PrefabPath = Out + "DetectiveBankersLamp_Visual.prefab";
        static FileStream guard;
        static JObject baseline;
        static Dictionary<string,string> exact = new Dictionary<string,string>();
        static Dictionary<string,string> referenceExact = new Dictionary<string,string>();
        static Dictionary<string,string> gameObjects = new Dictionary<string,string>();
        public static string Id(UnityEngine.Object o) { return o ? GlobalObjectId.GetGlobalObjectIdSlow(o).ToString() : ""; }
        static UnityEngine.Object Resolve(string id) { GlobalObjectId g; return GlobalObjectId.TryParse(id,out g) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(g) : null; }
        static JObject Serialized(UnityEngine.Object o) { return JObject.Parse(Regex.Replace(EditorJsonUtility.ToJson(o), "\"instanceID\"\\s*:\\s*-?\\d+", "\"instanceID\":0")); }
        static Dictionary<string,string> Refs(UnityEngine.Object o)
        {
            var r = new Dictionary<string,string>(); var s=new SerializedObject(o); var p=s.GetIterator();
            while(p.Next(true)) if(p.propertyType==SerializedPropertyType.ObjectReference) r[p.propertyPath]=Id(p.objectReferenceValue);
            return r;
        }
        public static void Begin()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Editor busy");
            guard=new FileStream(Logs+"unity-writer.lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            try {
                baseline=JObject.Parse(File.ReadAllText(Logs+"scene-components-before.json"));
                var differences=new JArray(); int count=0;
                foreach(var record in baseline["objects"])
                {
                    var go=Resolve((string)record["identity"]["globalId"]) as GameObject;
                    if(!go) throw new Exception("Missing original object "+record["identity"]);
                    gameObjects[Id(go)]=EditorJsonUtility.ToJson(go);
                    if(go.activeSelf!=(bool)record["activeSelf"] || go.layer!=(int)record["layer"] || go.tag!=(string)record["tag"]) differences.Add("GameObject flags: "+go.name);
                    foreach(var c in record["components"])
                    {
                        var id=(string)c["identity"]["globalId"]; var obj=Resolve(id);
                        if(!obj) throw new Exception("Missing original component "+id);
                        exact[id]=EditorJsonUtility.ToJson(obj); referenceExact[id]=JObject.FromObject(Refs(obj)).ToString(); count++;
                        if(!JToken.DeepEquals(c["serialized"],Serialized(obj))) differences.Add("Serialized: "+c["identity"]["path"]+" "+obj.GetType().FullName);
                        var refs=Refs(obj);
                        foreach(var rr in c["objectReferences"])
                        {
                            var path=(string)rr["property"]; string current; refs.TryGetValue(path,out current);
                            var expected=rr["reference"].Type==JTokenType.Null ? "" : (string)rr["reference"]["globalId"];
                            if((current??"")!=expected) differences.Add("Reference: "+c["identity"]["path"]+" "+path);
                        }
                    }
                }
                if(count!=3930 || gameObjects.Count!=1336) throw new Exception("Unexpected baseline counts");
                var scene=SceneManager.GetActiveScene();
                var copy=Out+"DetectiveOffice_PreIntegration_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+".unity";
                if(!EditorSceneManager.SaveScene(scene,copy,true)) throw new Exception("Scene copy failed");
                File.WriteAllText(Logs+"preintegration-check.json",new JObject { ["objects"]=gameObjects.Count,["components"]=count,["intervening_differences"]=differences,["scene_copy"]=copy,["dirty"]=scene.isDirty }.ToString());
                File.WriteAllText(Logs+"integration-exact-baseline.json",JObject.FromObject(new {components=exact,references=referenceExact,gameObjects=gameObjects}).ToString());
                if(differences.Count>0) throw new Exception("Intervening differences detected; preserved scene copy; inspect preintegration-check.json");
            } catch { Release(); throw; }
        }
        public static void Release() { if(guard!=null){guard.Dispose();guard=null;} }
        public static void Resume()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new Exception("Editor busy");
            guard=new FileStream(Logs+"unity-writer.lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            var saved=JObject.Parse(File.ReadAllText(Logs+"integration-exact-baseline.json"));
            exact=saved["components"].ToObject<Dictionary<string,string>>();referenceExact=saved["references"].ToObject<Dictionary<string,string>>();gameObjects=saved["gameObjects"].ToObject<Dictionary<string,string>>();
        }
        public static void ValidateAndSave()
        {
var root=GameObject.Find("DetectiveOffice/Environment/Generated_BankersLamp").transform;var visual=root.Find("DetectiveBankersLamp_Visual");var model=visual.Find("BankersLamp");
var renderers=visual.GetComponentsInChildren<MeshRenderer>(true);var meshes=visual.GetComponentsInChildren<MeshFilter>(true);
var materials=new JArray();int missing=0; bool materialsPass=true;
var spec=(JObject)JObject.Parse(File.ReadAllText("Assets/DetectiveDesk/LampRework/generation-validation.json"))["materials"];
foreach(var item in spec.Properties()){
var path="Assets/DetectiveDesk/LampRework/Materials/"+item.Name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){missing++;materialsPass=false;continue;}
var maps=new JArray();bool pass=m.shader.name=="Universal Render Pipeline/Lit"&&m.shader.isSupported;
foreach(var t in ((JObject)item.Value["textures"]).Properties().Where(t=>t.Name!="Roughness")){
var prop=t.Name=="BaseColor"?"_BaseMap":t.Name=="Normal"?"_BumpMap":"_MetallicGlossMap";var tex=m.GetTexture(prop);var tp=tex?AssetDatabase.GetAssetPath(tex):"";var importer=AssetImporter.GetAtPath(tp) as TextureImporter;
bool good=tp=="Assets/DetectiveDesk/ArtSource/Props/BankersLamp/"+(string)t.Value&&importer&&importer.sRGBTexture==(t.Name=="BaseColor")&&importer.textureType==(t.Name=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default);
if(t.Name=="Normal")good&=m.IsKeywordEnabled("_NORMALMAP");if(t.Name=="MetallicSmoothness")good&=m.IsKeywordEnabled("_METALLICSPECGLOSSMAP")&&m.GetFloat("_Smoothness")==1;
maps.Add(new JObject{["role"]=t.Name,["path"]=tp,["sRGB"]=importer?importer.sRGBTexture:false,["importer_type"]=importer?importer.textureType.ToString():"missing",["pass"]=good});pass&=good;
}
bool emissive=item.Value["emission_linear"]!=null;pass&=m.IsKeywordEnabled("_EMISSION")==emissive;
var bc=item.Value["base_color_linear"];var actual=m.GetColor("_BaseColor");pass&=Mathf.Abs(actual.r-(float)bc[0])<.0001f&&Mathf.Abs(actual.g-(float)bc[1])<.0001f&&Mathf.Abs(actual.b-(float)bc[2])<.0001f;
if(((JObject)item.Value["textures"]).Count==0)pass&=Mathf.Abs(m.GetFloat("_Smoothness")-(1-(float)item.Value["roughness"]))<.0001f;
materials.Add(new JObject{["name"]=item.Name,["path"]=path,["shader"]=m.shader.name,["maps"]=maps,["keywords"]=JArray.FromObject(m.shaderKeywords),["smoothness"]=m.GetFloat("_Smoothness"),["emission"]=JArray.FromObject(new[]{m.GetColor("_EmissionColor").r,m.GetColor("_EmissionColor").g,m.GetColor("_EmissionColor").b}),["pass"]=pass});materialsPass&=pass;
}
foreach(var r in renderers){if(r.sharedMaterials.Any(m=>!m))missing+=r.sharedMaterials.Count(m=>!m);if(r.sharedMaterials.Length!=r.GetComponent<MeshFilter>().sharedMesh.subMeshCount)materialsPass=false;}
var bounds=renderers.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});var transforms=model.GetComponentsInChildren<Transform>(true);
var shade=transforms.Single(t=>t.name=="Shade_Pivot");var chain=transforms.Single(t=>t.name=="PullChain_Pivot");var socket=transforms.Single(t=>t.name=="BulbSocket_Pivot");
var hood=renderers.Single(r=>r.name=="Shade_GreenGlass");var light=visual.GetComponentsInChildren<Light>(true).Single();
var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
int missingScripts=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
int missingPrefabs=all.Count(t=>PrefabUtility.GetPrefabInstanceStatus(t.gameObject)==PrefabInstanceStatus.MissingAsset);
int oldEnabled=root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>!r.transform.IsChildOf(visual)&&r.enabled);
bool pivots=chain.parent==model&&socket.parent==model&&shade.parent==model&&Mathf.Abs(Mathf.DeltaAngle(shade.localEulerAngles.x,10))<.001f;
bool axes=Quaternion.Angle(visual.localRotation*model.localRotation,Quaternion.identity)<.001f&&Vector3.Dot(root.TransformDirection(Vector3.up).normalized,Vector3.back)>.9999f;
bool placement=Mathf.Abs(bounds.min.y-.815f)<.0002f&&Mathf.Abs(bounds.size.y-1.5749123f)<.002f;
bool lampLight=light.type==LightType.Spot&&Vector3.Dot(light.transform.forward,Vector3.down)>.9999f&&light.intensity==.8f&&light.range==2.3f&&light.spotAngle==80&&light.innerSpotAngle==55&&light.useColorTemperature&&light.colorTemperature==3200&&light.color==Color.white&&light.shadows==LightShadows.None&&Vector3.Distance(light.transform.position,transforms.Single(t=>t.name=="LampLightOrigin").position)<.0001f;
LampReworkIntegration.Preservation();
var data=new JObject{["mesh_count"]=meshes.Length,["triangles"]=meshes.Sum(m=>m.sharedMesh.triangles.Length/3),["nonempty_meshes"]=meshes.All(m=>m.sharedMesh&&m.sharedMesh.vertexCount>0),["missing_materials"]=missing,["materials"]=materials,["materials_pass"]=materialsPass,["shade_slots"]=JArray.FromObject(hood.sharedMaterials.Select(m=>m.name).ToArray()),["shade_three_slots_pass"]=hood.sharedMaterials.Length==3,["pivot_tests_pass"]=pivots,["axis_compensation_pass"]=axes,["placement_pass"]=placement,["bounds_min"]=JArray.FromObject(new[]{bounds.min.x,bounds.min.y,bounds.min.z}),["bounds_max"]=JArray.FromObject(new[]{bounds.max.x,bounds.max.y,bounds.max.z}),["old_active_renderers"]=oldEnabled,["new_active_renderers"]=renderers.Count(r=>r.enabled&&r.gameObject.activeInHierarchy),["missing_scripts"]=missingScripts,["missing_prefab_assets"]=missingPrefabs,["scene_light_count"]=all.Sum(t=>t.GetComponents<Light>().Length),["new_light_pass"]=lampLight,["new_light"]=new JObject{["type"]=light.type.ToString(),["intensity"]=light.intensity,["range"]=light.range,["spot_angle"]=light.spotAngle,["inner_angle"]=light.innerSpotAngle,["temperature"]=light.colorTemperature,["use_temperature"]=light.useColorTemperature,["shadows"]=light.shadows.ToString(),["world_position"]=JArray.FromObject(new[]{light.transform.position.x,light.transform.position.y,light.transform.position.z}),["world_direction"]=JArray.FromObject(new[]{light.transform.forward.x,light.transform.forward.y,light.transform.forward.z})},["root_position"]=JArray.FromObject(new[]{root.localPosition.x,root.localPosition.y,root.localPosition.z}),["root_euler"]=JArray.FromObject(new[]{root.localEulerAngles.x,root.localEulerAngles.y,root.localEulerAngles.z}),["root_scale"]=JArray.FromObject(new[]{root.localScale.x,root.localScale.y,root.localScale.z})};
bool allPass=materialsPass&&missing==0&&pivots&&axes&&placement&&lampLight&&missingScripts==0&&missingPrefabs==0&&oldEnabled==0&&meshes.Length==127;
data["pass"]=allPass;File.WriteAllText("ProductionLogs/LampRework/unity-checks.json",data.ToString());
if(!allPass)throw new Exception("Lamp checks failed; see unity-checks.json");
AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
LampReworkIntegration.Preservation();Debug.Log("All lamp mesh/material/pivot/light/bounds/preservation checks passed. Scene saved after temporary state restoration. No Play Mode entered.");

        }

        static Color ColorOf(JToken a) { return new Color((float)a[0],(float)a[1],(float)a[2],a.Count()>3?(float)a[3]:1); }
        public static void Materials()
        {
            if(guard==null) throw new Exception("Writer guard required");
            Directory.CreateDirectory(Out+"Materials"); AssetDatabase.Refresh();
            var specs=(JObject)JObject.Parse(File.ReadAllText(Out+"generation-validation.json"))["materials"];
            var shader=Shader.Find("Universal Render Pipeline/Lit"); if(!shader)throw new Exception("URP Lit missing");
            foreach(var item in specs.Properties())
            {
                var spec=item.Value; string path=Out+"Materials/"+item.Name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);} mat.shader=shader;
                mat.SetColor("_BaseColor",ColorOf(spec["base_color_linear"]));
                mat.SetFloat("_Metallic",(float)spec["metallic"]);mat.SetFloat("_Smoothness",1-(float)spec["roughness"]);
                mat.SetFloat("_Surface",0); mat.SetFloat("_BumpScale",(float)spec["normal_strength"]);
                foreach(var tex in ((JObject)spec["textures"]).Properties())
                {
                    if(tex.Name=="Roughness")continue;
                    string tp=Source+(string)tex.Value; var importer=AssetImporter.GetAtPath(tp) as TextureImporter;
                    if(!importer) throw new Exception("Texture importer missing: "+tp);
                    bool normal=tex.Name=="Normal"; bool srgb=tex.Name=="BaseColor";
                    if(importer.sRGBTexture!=srgb || importer.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default))
                    { importer.sRGBTexture=srgb;importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.SaveAndReimport(); }
                    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(tp); if(!texture)throw new Exception("Texture missing");
                    mat.SetTexture(normal?"_BumpMap":srgb?"_BaseMap":"_MetallicGlossMap",texture);
                    if(normal)mat.EnableKeyword("_NORMALMAP");
                    if(tex.Name=="MetallicSmoothness"){mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",1);}
                }
                if(spec["emission_linear"]!=null){mat.SetColor("_EmissionColor",ColorOf(spec["emission_linear"]));mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;mat.EnableKeyword("_EMISSION");}
                else {mat.SetColor("_EmissionColor",Color.black);mat.DisableKeyword("_EMISSION");}
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
        }
        static Transform Find(Transform root,string name) { return root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name); }
        public static void Mount()
        {
            if(guard==null)throw new Exception("Writer guard required");
            var root=GameObject.Find(RootPath).transform;
            if(root.Find("DetectiveBankersLamp_Visual"))throw new Exception("Already mounted");
            var old=root.GetComponentsInChildren<MeshRenderer>(true); if(old.Length!=41)throw new Exception("Expected 41 old renderers");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"BankersLamp.fbx");
            var visual=new GameObject("DetectiveBankersLamp_Visual");Undo.RegisterCreatedObjectUndo(visual,"Mount detailed banker lamp");visual.transform.SetParent(root,false);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source,root.gameObject.scene);model.transform.SetParent(visual.transform,false);
            // Imported FBX root has X-90 conversion. Cancel it with a wrapper, retaining every imported local pivot.
            visual.transform.localRotation=Quaternion.Inverse(model.transform.localRotation);
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(Out+"Materials/"+m.name+".mat") ?? throw new Exception("Unknown source material "+m.name)).ToArray();
            }
            var filters=model.GetComponentsInChildren<MeshFilter>(true);
            var bounds=model.GetComponentsInChildren<MeshRenderer>(true).Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            if(filters.Length!=127 || Mathf.Abs(bounds.min.y-.815f)>.0002f || Mathf.Abs(bounds.size.y-1.5749123f)>.002f)throw new Exception("Placement validation failed "+bounds);
            var origin=Find(model.transform,"LampLightOrigin");var target=Find(model.transform,"LampLightTarget");
            var lightGo=new GameObject("BankersLamp_LocalSpot");lightGo.transform.SetParent(visual.transform,false);lightGo.transform.position=origin.position;
            lightGo.transform.rotation=Quaternion.LookRotation((target.position-origin.position).normalized,Vector3.forward);
            var light=lightGo.AddComponent<Light>(); light.type=LightType.Spot;light.color=Color.white;light.intensity=.8f;light.range=2.3f;light.spotAngle=80;light.innerSpotAngle=55;light.colorTemperature=3200;light.useColorTemperature=true;light.shadows=LightShadows.None;
            if(Vector3.Dot(light.transform.forward,Vector3.down)<.9999f)throw new Exception("Lamp light not downward");
            foreach(var r in old){Undo.RecordObject(r,"Hide old lamp visual");r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            // Prefab local pose is the documented compensating wrapper, mounted at zero position and unit scale.
            PrefabUtility.SaveAsPrefabAssetAndConnect(visual,PrefabPath,InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            File.WriteAllText(Logs+"mount-result.json",new JObject { ["mesh_count"]=filters.Length,["triangles"]=filters.Sum(x=>x.sharedMesh.triangles.Length/3),["bounds_min"]=JArray.FromObject(new[]{bounds.min.x,bounds.min.y,bounds.min.z}),["bounds_max"]=JArray.FromObject(new[]{bounds.max.x,bounds.max.y,bounds.max.z}),["mount_euler"]=JArray.FromObject(new[]{visual.transform.localEulerAngles.x,visual.transform.localEulerAngles.y,visual.transform.localEulerAngles.z}),["light_position"]=JArray.FromObject(new[]{light.transform.position.x,light.transform.position.y,light.transform.position.z}),["old_renderers_disabled"]=old.Length }.ToString());
        }
        public static void Preservation()
        {
            var diffs=new JArray();int authorized=0;
            foreach(var pair in exact)
            {
                var obj=Resolve(pair.Key); if(!obj){diffs.Add("Missing "+pair.Key);continue;}
                var before=JObject.Parse(pair.Value);var after=JObject.Parse(EditorJsonUtility.ToJson(obj));
                var renderer=obj as MeshRenderer;
                if(renderer && renderer.transform.IsChildOf(GameObject.Find(RootPath).transform))
                {
                    var b=before.SelectToken("MeshRenderer.m_Enabled");var a=after.SelectToken("MeshRenderer.m_Enabled");
                    if(b!=null && a!=null && (bool)b && !(bool)a){a.Replace(b.DeepClone());authorized++;}
                }
                if(!JToken.DeepEquals(before,after))diffs.Add("Serialized "+pair.Key+" "+obj.name);
                var refs=JObject.FromObject(Refs(obj));var expected=JObject.Parse(referenceExact[pair.Key]);
                if(obj==GameObject.Find(RootPath).transform) foreach(var p in refs.Properties().Where(p=>p.Name.StartsWith("m_Children.Array.data[")&&!expected.ContainsKey(p.Name)).ToArray())p.Remove();
                if(!JToken.DeepEquals(expected,refs))diffs.Add("References "+pair.Key+" "+obj.name);
            }
            foreach(var pair in gameObjects) {var obj=Resolve(pair.Key);if(!obj || EditorJsonUtility.ToJson(obj)!=pair.Value)diffs.Add("GameObject "+pair.Key);}
            File.WriteAllText(Logs+"preservation-result.json",new JObject { ["objects_checked"]=gameObjects.Count,["components_checked"]=exact.Count,["authorized_renderer_changes"]=authorized,["differences"]=diffs,["pass"]=diffs.Count==0&&authorized==41 }.ToString());
            if(diffs.Count>0 || authorized!=41)throw new Exception("Preservation failed; inspect report");
        }
    }
}
