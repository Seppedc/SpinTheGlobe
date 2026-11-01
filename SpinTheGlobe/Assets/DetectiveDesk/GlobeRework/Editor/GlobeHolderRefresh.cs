using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;

// Revision-only refresh. Never calls the initial GlobeIntegration.Build authoring path.
public static class GlobeHolderRefresh
{
    public const string Log="ProductionLogs/GlobeHolderRevision/";
    public const string Evidence="Assets/DetectiveDesk/GlobeHolderRevision/";
    const string Source="Assets/DetectiveDesk/ArtSource/Gameplay/MechanicalGlobe/";
    const string Modules="Assets/DetectiveDesk/GlobeRework/Prefabs/";
    static Transform Root=>GlobeIntegration.Root;
    static Transform Needle=>(Transform)new SerializedObject(Root.GetComponent("DetectiveGlobeMechanism")).FindProperty("rpmNeedle").objectReferenceValue;
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    static string Identity(UnityEngine.Object o)=>o?GlobalObjectId.GetGlobalObjectIdSlow(o).ToString():"null";
    static string StableJson(UnityEngine.Object o)=>Regex.Replace(EditorJsonUtility.ToJson(o),"\\{\\s*\"instanceID\"\\s*:\\s*(-?[0-9]+)\\s*\\}",m=>"{\"stableReference\":\""+Identity(EditorUtility.InstanceIDToObject(int.Parse(m.Groups[1].Value)))+"\"}");
    static JObject Snapshot()
    {
        var rows=new JObject();
        foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
        foreach(var c in t.GetComponents<Component>().Where(c=>c))
        {
            var refs=new JObject();var it=new SerializedObject(c).GetIterator();
            while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference)refs[it.propertyPath]=Identity(it.objectReferenceValue);
            rows[Identity(c)]=new JObject{{"path",PathOf(t)},{"type",c.GetType().FullName},{"json",JObject.Parse(StableJson(c))},{"references",refs}};
        }
        return rows;
    }
    static void Ready(bool clean)
    {
        if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Expected EditMode without compilation");
        var s=SceneManager.GetActiveScene();if(s.path!="Assets/DetectiveDesk/Generated/DetectiveOffice.unity"||(clean&&s.isDirty))throw new Exception("Unexpected scene or unsaved edits");
    }
    public static void Baseline()
    {
        Ready(true);Directory.CreateDirectory(Log);Directory.CreateDirectory(Evidence);
        if(File.Exists(Log+"scene-baseline.json"))throw new Exception("Baseline already exists; do not replace it");
        var snapshot=Snapshot();File.WriteAllText(Log+"scene-baseline.json",snapshot.ToString());
        var state=new JObject{{"scene",SceneManager.GetActiveScene().path},{"components",snapshot.Count},{"needle_identity",Identity(Needle)},{"needle_parent",PathOf(Needle.parent)},{"needle_position",JToken.FromObject(new[]{Needle.localPosition.x,Needle.localPosition.y,Needle.localPosition.z})},{"root_prefab",PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Root.gameObject)}};
        File.WriteAllText(Log+"scene-baseline-summary.json",state.ToString());
        if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Log+"FreshCheckpoint/DetectiveOffice.unity",true))throw new Exception("Scene copy failed");
        Debug.Log("Globe holder baseline captured: "+snapshot.Count+" existing components; scene copy only");
    }
    static bool Revised(string n)=>n=="Meridian_BroadGraduatedBrassBand"||n.StartsWith("Meridian_PolishedEdge_")||n=="Meridian_EngravedGraduations"||n.StartsWith("Pedestal_Gauge")||n.StartsWith("RPMGauge_");
    public static void Apply()
    {
        Ready(true);
        if(!File.Exists(Log+"scene-baseline.json")||!File.Exists(Log+"promotion-validation.json"))throw new Exception("Missing backup/promotion proof");
        var before=JObject.Parse(File.ReadAllText(Log+"scene-baseline.json"));if(!JToken.DeepEquals(before,Snapshot()))throw new Exception("Scene changed since baseline");
        var scene=SceneManager.GetActiveScene();Scene review=default(Scene);var meshes=new Dictionary<string,UnityEngine.Mesh>();var names=new HashSet<string>();var anchors=new JObject();
        var localDelta=Needle.parent.InverseTransformVector(Root.TransformVector(new Vector3(-.74f,-.25f,-.05f)));
        var futurePose=Root.worldToLocalMatrix*Matrix4x4.TRS(Needle.position+Root.TransformVector(new Vector3(-.74f,-.25f,-.05f)),Needle.rotation,Needle.lossyScale);
        try
        {
            review=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(review);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source+"MechanicalGlobe.fbx"),review);
            model.transform.rotation=Quaternion.Euler(0,180,0)*model.transform.rotation;
            foreach(var p in new Dictionary<string,Vector3>{{"Sphere_RotationPivot",new Vector3(-1.45f,2.15f,-.33f)},{"Pedestal_Assembly",new Vector3(-1.45f,.86f,-.33f)},{"RPMGauge_Assembly",new Vector3(-1.23f,1.18f,-.80f)}})
            {var t=model.GetComponentsInChildren<Transform>().Single(x=>x.name==p.Key);float e=Vector3.Distance(t.position,p.Value);anchors[p.Key]=new JObject{{"position",t.position.ToString("F7")},{"error",e}};if(e>.0001f)throw new Exception("Invalid anchor "+p.Key);}
            var filters=model.GetComponentsInChildren<MeshFilter>();if(filters.Length!=51)throw new Exception("Unexpected imported mesh count");
            foreach(var mf in filters)
            {
                if(!names.Add(mf.name))throw new Exception("Duplicate source object name "+mf.name);
                if(mf.name.StartsWith("Sphere_")||!Revised(mf.name))continue;
                bool needle=false;for(var p=mf.transform;p;p=p.parent)if(p.name=="RPMGauge_NeedlePivot")needle=true;
                var matrix=(needle?futurePose.inverse:Matrix4x4.identity)*mf.transform.localToWorldMatrix;
                var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
                var normal=matrix.inverse.transpose;mesh.normals=mesh.normals.Select(n=>normal.MultiplyVector(n).normalized).ToArray();float sign=matrix.determinant<0?-1:1;
                mesh.tangents=mesh.tangents.Select(t=>{var v=matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w*sign);}).ToArray();
                if(sign<0)for(int s=0;s<mesh.subMeshCount;s++){var idx=mesh.GetTriangles(s);for(int i=0;i<idx.Length;i+=3){int q=idx[i];idx[i]=idx[i+1];idx[i+1]=q;}mesh.SetTriangles(idx,s);}
                mesh.RecalculateBounds();if(mesh.vertexCount==0)throw new Exception("Empty mesh");meshes.Add(mf.name,mesh);
            }
        }
        finally{SceneManager.SetActiveScene(scene);if(review.IsValid())EditorSceneManager.CloseScene(review,true);}
        var changes=new JArray();var disabled=new HashSet<string>();
        try
        {
            // Resolve using source object names on the existing prefab filters, never shifted indices.
            foreach(var key in new[]{"Static","Needle"})
            {
                string path=Modules+"GlobeRework_"+key+".prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach(var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if(meshes.TryGetValue(mf.name,out var mesh))
                        {
                            var asset=mf.sharedMesh;string assetPath=AssetDatabase.GetAssetPath(asset);if(!assetPath.StartsWith("Assets/DetectiveDesk/GlobeRework/NativeMeshes/")||mf.name.StartsWith("Sphere_"))throw new Exception("Protected mesh binding");
                            mesh.name=asset.name;EditorUtility.CopySerialized(mesh,asset);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);changes.Add(new JObject{{"object",mf.name},{"asset",assetPath},{"guid",AssetDatabase.AssetPathToGUID(assetPath)}});
                        }
                        else if(!names.Contains(mf.name))
                        {
                            if(!mf.name.StartsWith("Meridian_DegreeLabel_"))throw new Exception("Unexpected missing source "+mf.name);
                            mf.GetComponent<Renderer>().enabled=false;disabled.Add(mf.name);
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
            foreach(var key in new[]{"Gear_12T_Pivot","Gear_14T_Pivot","Gear_16T_Pivot"})
            {
                string path=Modules+"GlobeRework_"+key+".prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
                try{foreach(var r in prefab.GetComponentsInChildren<Renderer>(true)){r.enabled=false;disabled.Add(r.name);}PrefabUtility.SaveAsPrefabAsset(prefab,path);}
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
            Undo.RecordObject(Needle,"Globe holder gauge position");Needle.localPosition+=localDelta;PrefabUtility.RecordPrefabInstancePropertyModifications(Needle);
            foreach(var r in Root.GetComponentsInChildren<Renderer>(true).Where(r=>disabled.Contains(r.name)&&r.transform.GetComponentsInParent<Transform>(true).Any(t=>t.name.StartsWith("GlobeRework_"))))
            {Undo.RecordObject(r,"Hide obsolete globe visual");r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            var mechanism=Root.GetComponent("DetectiveGlobeMechanism");var so=new SerializedObject(mechanism);var feedback=so.FindProperty("feedbackRenderers");var kept=Enumerable.Range(0,feedback.arraySize).Select(i=>(Renderer)feedback.GetArrayElementAtIndex(i).objectReferenceValue).Where(r=>r&&r.enabled).ToArray();
            Undo.RecordObject(mechanism,"Globe hover active renderers");feedback.arraySize=kept.Length;for(int i=0;i<kept.Length;i++)feedback.GetArrayElementAtIndex(i).objectReferenceValue=kept[i];so.ApplyModifiedProperties();PrefabUtility.RecordPrefabInstancePropertyModifications(mechanism);
            EditorSceneManager.MarkSceneDirty(scene);
            File.WriteAllText(Log+"refresh-changes.json",new JObject{{"native_mesh_changes",changes},{"disabled_names",new JArray(disabled)},{"anchors",anchors},{"source_mesh_count",names.Count},{"needle_parent_delta",JToken.FromObject(new[]{localDelta.x,localDelta.y,localDelta.z})},{"feedback_count",kept.Length}}.ToString());
            Preservation();
        }
        finally{foreach(var mesh in meshes.Values)UnityEngine.Object.DestroyImmediate(mesh);}
    }
    static void Differences(JToken a,JToken b,string path,List<string> changes)
    {
        if(JToken.DeepEquals(a,b))return;
        if(a is JObject ao&&b is JObject bo){foreach(var key in ao.Properties().Select(p=>p.Name).Union(bo.Properties().Select(p=>p.Name)))Differences(ao[key],bo[key],path.Length==0?key:path+"."+key,changes);return;}
        changes.Add(path);
    }
    public static void Preservation()
    {
        Ready(false);var baseline=JObject.Parse(File.ReadAllText(Log+"scene-baseline.json"));var now=Snapshot();var changes=JObject.Parse(File.ReadAllText(Log+"refresh-changes.json"));var disabled=new HashSet<string>(((JArray)changes["disabled_names"]).Values<string>());
        var diffs=new JArray();var failures=new JArray();if(baseline.Count!=now.Count)failures.Add("Component count changed");
        foreach(var pair in baseline)
        {
            var next=now[pair.Key];if(next==null){failures.Add("Missing component "+pair.Key);continue;}
            var paths=new List<string>();Differences(pair.Value,next,"",paths);if(paths.Count==0)continue;
            string p=(string)pair.Value["path"],type=(string)pair.Value["type"];bool allowed=false;
            if(pair.Key==Identity(Needle))
            {allowed=paths.All(x=>x.StartsWith("json.Transform.m_LocalPosition."));var old=pair.Value["json"]["Transform"]["m_LocalPosition"];var expected=new Vector3((float)old["x"],(float)old["y"],(float)old["z"])+Needle.parent.InverseTransformVector(Root.TransformVector(new Vector3(-.74f,-.25f,-.05f)));allowed&=Vector3.Distance(Needle.localPosition,expected)<.00001f;}
            else if(type=="UnityEngine.MeshRenderer"&&p.Contains("/GlobeRework_")&&disabled.Contains(p.Split('/').Last()))allowed=paths.All(x=>x=="json.MeshRenderer.m_Enabled")&&(bool)next["json"]["MeshRenderer"]["m_Enabled"]==false;
            else if(type=="DetectiveDesk.DetectiveGlobeMechanism"&&p==PathOf(Root))
            {
                allowed=paths.All(x=>x=="json.MonoBehaviour.feedbackRenderers"||x.StartsWith("references.feedbackRenderers"));
                var refs=(JObject)pair.Value["references"];
                var oldList=refs.Properties().Where(x=>x.Name.StartsWith("feedbackRenderers.Array.data[")).Select(x=>(string)x.Value).ToArray();
                var expected=oldList.Where(id=>{var r=baseline[id];return r!=null&&!disabled.Contains(((string)r["path"]).Split('/').Last());}).ToArray();
                var newList=((JObject)next["references"]).Properties().Where(x=>x.Name.StartsWith("feedbackRenderers.Array.data[")).Select(x=>(string)x.Value).ToArray();
                allowed&=expected.SequenceEqual(newList)&&newList.Length==(int)changes["feedback_count"];
            }
            var row=new JObject{{"id",pair.Key},{"path",p},{"type",type},{"fields",new JArray(paths)},{"authorized",allowed}};diffs.Add(row);if(!allowed)failures.Add(row.DeepClone());
        }
        File.WriteAllText(Log+"scene-after.json",now.ToString());File.WriteAllText(Log+"preservation-validation.json",new JObject{{"status",failures.Count==0?"PASS":"FAIL"},{"baseline_components",baseline.Count},{"current_components",now.Count},{"changed_components",diffs},{"failures",failures}}.ToString());
        if(failures.Count>0)throw new Exception("Preservation failed: "+failures.ToString());
    }
    public static void Save()
    {Preservation();if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new Exception("Scene save failed");}
}
