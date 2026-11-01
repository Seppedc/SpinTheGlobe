using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;

public static class GlobeIntegration
{
    public const string Dir="Assets/DetectiveDesk/GlobeRework/";
    const string Source="Assets/DetectiveDesk/ArtSource/Gameplay/MechanicalGlobe/";
    public static Transform Root => GameObject.Find("DetectiveOffice/GameplayObjects/MechanicalGlobe").transform;
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent)+"/"+t.name:t.name;
    public static void Snapshot(string file)
    {
        var rows=new JArray();
        foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
        foreach(var c in go.GetComponents<Component>().Where(c=>c))
        { var refs=new JObject(); var so=new SerializedObject(c); var it=so.GetIterator(); while(it.Next(true)) if(it.propertyType==SerializedPropertyType.ObjectReference) refs[it.propertyPath]=it.objectReferenceInstanceIDValue;
          rows.Add(new JObject{{"id",c.GetInstanceID()},{"path",PathOf(go)},{"type",c.GetType().FullName},{"json",EditorJsonUtility.ToJson(c)},{"refs",refs}}); }
        File.WriteAllText(file,rows.ToString());
    }
    public static void Build()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty) throw new Exception("Expected clean Edit Mode scene");
        if(Root.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("GlobeRework_"))) throw new Exception("Globe already integrated. Refresh its existing native mesh/module assets with a reviewed update; do not create duplicate visual children.");
        Snapshot("ProductionLogs/GlobeRework/integration-baseline.json");
        foreach(var d in new[]{"Materials","Textures","NativeMeshes","Prefabs"}) Directory.CreateDirectory(Dir+d);
        var spec=JObject.Parse(File.ReadAllText(Dir+"generation-validation.json"));
        var mats=new Dictionary<string,Material>();
        foreach(var entry in (JObject)spec["materials"])
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=entry.Key};
            foreach(var map in (JObject)entry.Value)
            {
                string src=Source+(string)map.Value, dst=Dir+"Textures/"+System.IO.Path.GetFileName(src);
                if(!File.Exists(src)) throw new Exception("Missing "+src);
                if(!File.Exists(dst)) File.Copy(src,dst);
                AssetDatabase.ImportAsset(dst);
                var ti=(TextureImporter)AssetImporter.GetAtPath(dst); ti.sRGBTexture=map.Key=="BaseColor"; ti.maxTextureSize=2048; ti.textureCompression=TextureImporterCompression.Uncompressed;
                ti.textureType=map.Key=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default; ti.SaveAndReimport();
                var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
                m.SetTexture(map.Key=="BaseColor"?"_BaseMap":map.Key=="Normal"?"_BumpMap":"_MetallicGlossMap",tex);
            }
            m.SetColor("_BaseColor",Color.white); m.SetFloat("_Metallic",1); m.SetFloat("_Smoothness",1); m.SetFloat("_BumpScale",.22f); m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICSPECGLOSSMAP");
            // Keep the reviewed earth readability treatment reproducible in a fresh import.
            if(entry.Key=="DarkShell"||entry.Key=="EngravedLand")
            {
                m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                m.SetFloat("_EnvironmentReflections",0);m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                m.SetFloat("_Smoothness",.6f);m.SetTexture("_EmissionMap",m.GetTexture("_BaseMap"));
                m.SetColor("_EmissionColor",new Color(.06f,.06f,.06f,1));m.EnableKeyword("_EMISSION");
            }
            if(entry.Key=="AmberAccent"||entry.Key=="GaugeFace") {m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",m.GetTexture("_BaseMap"));m.SetColor("_EmissionColor",Color.white*(entry.Key=="GaugeFace"?.055f:.12f));}
            var existing=AssetDatabase.LoadAssetAtPath<Material>(Dir+"Materials/"+entry.Key+".mat");
            if(existing){EditorUtility.CopySerialized(m,existing);UnityEngine.Object.DestroyImmediate(m);m=existing;EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);}else AssetDatabase.CreateAsset(m,Dir+"Materials/"+entry.Key+".mat"); mats.Add(entry.Key,m);
        }
        var importer=(ModelImporter)AssetImporter.GetAtPath(Source+"MechanicalGlobe.fbx");importer.isReadable=true;importer.SaveAndReimport();
        var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var modules=new Dictionary<string,GameObject>();
        var targets=new Dictionary<string,Transform>();
        var mechanism=Root.GetComponent("DetectiveGlobeMechanism");var soMech=new SerializedObject(mechanism);
        targets["Static"]=Root;targets["Sphere"]=Root.Find("RotatingSphere");
        for(int i=0;i<3;i++) {var t=(Transform)soMech.FindProperty("gears").GetArrayElementAtIndex(i).objectReferenceValue;targets[t.name]=t;}
        targets["Needle"]=(Transform)soMech.FindProperty("rpmNeedle").objectReferenceValue;
        int meshCount=0,triangles=0;
        try
        {
            SceneManager.SetActiveScene(scene);
            var imported=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source+"MechanicalGlobe.fbx"),scene);
            imported.transform.rotation=Quaternion.Euler(0,180,0)*imported.transform.rotation;
            var anchors=new JObject();
            foreach(var pair in new Dictionary<string,Vector3>{{"Sphere_RotationPivot",new Vector3(-1.45f,2.15f,-.33f)},{"Pedestal_Assembly",new Vector3(-1.45f,.86f,-.33f)},{"RPMGauge_Assembly",new Vector3(-.49f,1.43f,-.75f)}})
            {var t=imported.GetComponentsInChildren<Transform>().Single(x=>x.name==pair.Key);float err=Vector3.Distance(t.position,pair.Value);anchors[pair.Key]=new JObject{{"error",err},{"position",t.position.ToString("F7")}};if(err>.0001f)throw new Exception("Anchor mismatch "+pair.Key);}
            foreach(var key in targets.Keys) modules[key]=new GameObject("GlobeRework_"+key);
            foreach(var mf in imported.GetComponentsInChildren<MeshFilter>())
            {
                string key="Static"; for(var p=mf.transform;p&&p!=imported.transform;p=p.parent) {if(p.name=="Sphere_RotationPivot")key="Sphere";if(p.name=="RPMGauge_NeedlePivot")key="Needle";foreach(var g in targets.Keys.Where(k=>k.StartsWith("Gear_")))if(p.name==g||p.name==g.Replace("_Pivot","_RearLayer_Pivot"))key=g;}
                var target=targets[key];
                // Source pose is expressed in the established globe parent's coordinates.
                var targetPose=key=="Static"?Matrix4x4.identity:Root.worldToLocalMatrix*target.localToWorldMatrix;
                var matrix=targetPose.inverse*mf.transform.localToWorldMatrix;
                var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=mf.name;
                mesh.vertices=mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
                var normalMatrix=matrix.inverse.transpose;mesh.normals=mesh.normals.Select(n=>normalMatrix.MultiplyVector(n).normalized).ToArray();
                float sign=matrix.determinant<0?-1:1;
                mesh.tangents=mesh.tangents.Select(t=>{var v=matrix.MultiplyVector(new Vector3(t.x,t.y,t.z)).normalized;return new Vector4(v.x,v.y,v.z,t.w*sign);}).ToArray();
                if(sign<0)for(int s=0;s<mesh.subMeshCount;s++){var idx=mesh.GetTriangles(s);for(int i=0;i<idx.Length;i+=3){int q=idx[i];idx[i]=idx[i+1];idx[i+1]=q;}mesh.SetTriangles(idx,s);}
                mesh.RecalculateBounds();if(mesh.vertexCount==0)throw new Exception("Empty mesh");triangles+=mesh.triangles.Length/3;
                AssetDatabase.CreateAsset(mesh,Dir+"NativeMeshes/"+(meshCount++).ToString("D3")+"_"+mf.name+".asset");
                var child=new GameObject(mf.name);child.transform.SetParent(modules[key].transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                child.AddComponent<MeshRenderer>().sharedMaterials=mf.GetComponent<Renderer>().sharedMaterials.Select(m=>mats[m.name]).ToArray();
            }
            foreach(var pair in modules) {if(pair.Value.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>!m||m.shader.name!="Universal Render Pipeline/Lit")))throw new Exception("Invalid material");PrefabUtility.SaveAsPrefabAsset(pair.Value,Dir+"Prefabs/"+pair.Value.name+".prefab");}
            File.WriteAllText(Dir+"import-validation.json",new JObject{{"status","PASS"},{"anchors",anchors},{"mesh_count",meshCount},{"triangles",triangles},{"normal_transform","inverse transpose; determinant checked; tangent handedness retained"},{"material_count",mats.Count},{"additive_scene_validation",true}}.ToString());
        }
        finally {SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(scene,true);}
        if(meshCount!=80||triangles!=272684)throw new Exception("Geometry count mismatch");
        var old=Root.GetComponentsInChildren<Renderer>(true);
        var added=new List<Renderer>();foreach(var pair in targets){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"Prefabs/GlobeRework_"+pair.Key+".prefab"),active);Undo.RegisterCreatedObjectUndo(go,"Globe visual rework");go.transform.SetParent(pair.Value,false);added.AddRange(go.GetComponentsInChildren<Renderer>());}
        foreach(var r in old){Undo.RecordObject(r,"Globe visual rework");r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
        Undo.RecordObject(mechanism,"Globe feedback visuals");var feedback=soMech.FindProperty("feedbackRenderers");feedback.arraySize=added.Count;for(int i=0;i<added.Count;i++)feedback.GetArrayElementAtIndex(i).objectReferenceValue=added[i];soMech.ApplyModifiedProperties();PrefabUtility.RecordPrefabInstancePropertyModifications(mechanism);
        EditorSceneManager.MarkSceneDirty(active);
        Snapshot("ProductionLogs/GlobeRework/integration-after.json");
    }
}
