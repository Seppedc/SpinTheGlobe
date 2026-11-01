using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DetectiveDesk.Gameplay;
namespace DetectiveDesk.Editor
{
    public static class SkillTreePreservationValidation
    {
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
            var baseline=JObject.Parse(File.ReadAllText("ProductionLogs/SkillTree/scene-before.json"));var differences=new List<object>();int checkedFields=0,checkedObjects=0,checkedComponents=0;
            foreach(var entry in baseline["scenes"].SelectMany(s=>s["objects"]))
            {
                string hierarchy=(string)entry["ref"]["hierarchy"];bool allowed=hierarchy=="DetectiveOffice/GameUI/Upgrades panel"||hierarchy.StartsWith("DetectiveOffice/GameUI/Upgrades panel/");
                GlobalObjectId gid;GlobalObjectId.TryParse((string)entry["ref"]["globalId"],out gid);var obj=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as GameObject;
                if(!obj){differences.Add(new{hierarchy,issue="Original object missing"});continue;}checkedObjects++;
                if(!allowed&&((bool)entry["activeSelf"]!=obj.activeSelf||(int)entry["layer"]!=obj.layer||(string)entry["tag"]!=obj.tag))differences.Add(new{hierarchy,issue="Original flags changed"});
                foreach(var component in entry["components"].Where(c=>c.Type!=JTokenType.Null))
                {
                    GlobalObjectId.TryParse((string)component["ref"]["globalId"],out gid);var target=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                    if(!target){differences.Add(new{hierarchy,issue="Original component missing",type=(string)component["ref"]["type"]});continue;}checkedComponents++;if(allowed)continue;
                    var serialized=new SerializedObject(target);var props=component["properties"].ToArray();var collapsed=props.Where(v=>(string)v["type"]=="String"||(string)v["type"]=="ObjectReference").Select(v=>(string)v["path"]).ToArray();
                    foreach(var property in props)
                    {
                        string path=(string)property["path"],kind=(string)property["type"];
                        if(collapsed.Any(c=>path.StartsWith(c+".")))continue;
                        var actual=serialized.FindProperty(path);if(actual==null){differences.Add(new{hierarchy,path,issue="Missing original field"});continue;}
                        bool equal=true;var expected=property["value"];string value="";
                        switch(kind)
                        {
                            case "Integer":equal=actual.longValue==(long)expected;break;
                            case "Boolean":equal=actual.boolValue==(bool)expected;break;
                            case "String":equal=actual.stringValue==(string)expected;break;
                            case "Enum":equal=actual.intValue==(int)expected;break;
                            case "Float":double number;equal=double.TryParse((string)expected,NumberStyles.Float,CultureInfo.InvariantCulture,out number)&&Math.Abs(actual.doubleValue-number)<=1e-9*Math.Max(1,Math.Abs(number));break;
                            case "ObjectReference":equal=expected.Type==JTokenType.Null?!actual.objectReferenceValue:actual.objectReferenceValue&&GlobalObjectId.GetGlobalObjectIdSlow(actual.objectReferenceValue).ToString()==(string)expected["globalId"];break;
                            case "Vector2":value=actual.vector2Value.ToString("G9");break;
                            case "Vector3":value=actual.vector3Value.ToString("G9");break;
                            case "Vector4":value=actual.vector4Value.ToString("G9");break;
                            case "Quaternion":value=actual.quaternionValue.ToString("G9");break;
                            case "Color":value=actual.colorValue.ToString();break;
                            case "Rect":value=actual.rectValue.ToString();break;
                        }
                        if(value!="")
                        {
                            if(kind.StartsWith("Vector")||kind=="Quaternion")
                            {
                                var a=Regex.Matches(value,@"[-+]?\d*\.?\d+(?:[Ee][-+]?\d+)?").Cast<Match>().Select(m=>float.Parse(m.Value,CultureInfo.InvariantCulture)).ToArray();
                                var b=Regex.Matches((string)expected,@"[-+]?\d*\.?\d+(?:[Ee][-+]?\d+)?").Cast<Match>().Select(m=>float.Parse(m.Value,CultureInfo.InvariantCulture)).ToArray();
                                equal=a.SequenceEqual(b);
                            }
                            else equal=value==(string)expected;
                        }
                        checkedFields++;if(!equal)differences.Add(new{hierarchy,type=target.GetType().FullName,path,expected,actual=value!=""?value:actual.propertyType.ToString()});
                    }
                }
            }
            var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();string error;var scene=SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            int missingScripts=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));int missingRefs=0;
            foreach(var c in all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c))
            {var so=new SerializedObject(c);var it=so.GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&!it.objectReferenceValue&&it.objectReferenceInstanceIDValue!=0)missingRefs++;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DetectiveDesk/Gameplay/SkillTree/SkillTreePanel.prefab");var pp=prefab.GetComponent<SkillTreePanel>();bool prefabValid=pp&&!pp.game&&pp.definition==game.skillTree&&pp.nodeViews.Length==39&&pp.nodeViews.All(v=>v&&v.owner==pp&&v.button&&v.button.onClick.GetPersistentTarget(0)==v)&&pp.buyButton.onClick.GetPersistentTarget(0)==pp;
            bool progress=game.Money==422&&game.SpeedLevel==0&&game.MotorLevel==0&&game.DurabilityLevel==0&&game.figurines.All(f=>f.Tier==0);
            var report=new{status=differences.Count==0&&missingScripts==0&&missingRefs==0&&prefabValid&&progress?"PASS":"FAIL",checkedObjects,checkedComponents,checkedFields,differences,missingScripts,missingRefs,prefabInternalReferencesValid=prefabValid,progressRestored=progress,objectCount=all.Length,componentCount=all.Sum(t=>t.GetComponents<Component>().Length),sceneCount=SceneManager.sceneCount,dirty=scene.isDirty,editMode=!EditorApplication.isPlaying,physics=Physics.GetCurrentIntegrationInfo().name,defaultGraphValid=game.skillTree.Validate(out error),nodes=game.skillTree.nodes.Length,limitations="Baseline stores curve/gradient/mask values as type labels; scalar children compared where serialized. Authorized Upgrades subtree excluded from field comparison; all original identities still required."};
            File.WriteAllText("ProductionLogs/SkillTree/preservation-validation.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        }
    }
}
