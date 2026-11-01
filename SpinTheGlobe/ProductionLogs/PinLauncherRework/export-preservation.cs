using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
internal class CommandScript : IRunCommand {
 string Q(string s){return "\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n").Replace("\t","\\t")+"\"";}
 string P(Transform t){return t.parent==null?t.name:P(t.parent)+"/"+t.name;}
 public void Execute(ExecutionResult result){
 var rows=new List<string>();var objs=new List<string>();
 for(int i=0;i<SceneManager.sceneCount;i++) foreach(var root in SceneManager.GetSceneAt(i).GetRootGameObjects()) foreach(var t in root.GetComponentsInChildren<Transform>(true)){
 objs.Add("{\"id\":"+Q(GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString())+",\"path\":"+Q(P(t))+",\"activeSelf\":"+(t.gameObject.activeSelf?"true":"false")+",\"layer\":"+t.gameObject.layer+",\"tag\":"+Q(t.tag)+",\"siblingIndex\":"+t.GetSiblingIndex()+",\"prefab\":"+Q(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject))+"}");
 foreach(var c in t.GetComponents<Component>()){if(c==null)throw new Exception("Missing script");var refs=new List<string>();var so=new SerializedObject(c);var prop=so.GetIterator();while(prop.Next(true))if(prop.propertyType==SerializedPropertyType.ObjectReference)refs.Add(Q(prop.propertyPath+ "="+(prop.objectReferenceValue==null?"null":GlobalObjectId.GetGlobalObjectIdSlow(prop.objectReferenceValue).ToString())));
 rows.Add("{\"id\":"+Q(GlobalObjectId.GetGlobalObjectIdSlow(c).ToString())+",\"path\":"+Q(P(t))+",\"type\":"+Q(c.GetType().FullName)+",\"json\":"+Q(EditorJsonUtility.ToJson(c))+",\"refs\":["+string.Join(",",refs)+"]}");}
 }
 File.WriteAllText("ProductionLogs/PinLauncherRework/preservation-after.json","{\"objects\":["+string.Join(",",objs)+"],\"rows\":["+string.Join(",",rows)+"]}");result.Log("Exported "+rows.Count+" components, "+objs.Count+" objects");
 }
}
