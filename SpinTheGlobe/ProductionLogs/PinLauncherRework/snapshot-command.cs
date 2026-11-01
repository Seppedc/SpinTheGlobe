using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
internal class CommandScript : IRunCommand {
 [Serializable] public class Row { public string id,path,type,json; public string[] refs; }
 [Serializable] public class Data { public List<Row> rows=new List<Row>(); }
 string PathOf(Transform t) { return t.parent==null?t.name:PathOf(t.parent)+"/"+t.name; }
 public void Execute(ExecutionResult result) {
 var d=new Data();
 for(int i=0;i<SceneManager.sceneCount;i++) foreach(var root in SceneManager.GetSceneAt(i).GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true)) {
 d.rows.Add(new Row{id=GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString(),path=PathOf(t),type="GameObject",json=EditorJsonUtility.ToJson(t.gameObject),refs=new string[0]});
 foreach(var c in t.GetComponents<Component>()) { if(c==null) throw new Exception("Missing script "+PathOf(t)); var refs=new List<string>(); var so=new SerializedObject(c); var p=so.GetIterator(); while(p.Next(true)) if(p.propertyType==SerializedPropertyType.ObjectReference) refs.Add(p.propertyPath+"="+(p.objectReferenceValue==null?"null":GlobalObjectId.GetGlobalObjectIdSlow(p.objectReferenceValue).ToString()));
 d.rows.Add(new Row{id=GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),path=PathOf(t),type=c.GetType().FullName,json=EditorJsonUtility.ToJson(c),refs=refs.ToArray()}); }
 }
 File.WriteAllText("ProductionLogs/PinLauncherRework/SNAPSHOT_NAME.json",JsonUtility.ToJson(d,true)); result.Log("Snapshot rows="+d.rows.Count);
 }
}
