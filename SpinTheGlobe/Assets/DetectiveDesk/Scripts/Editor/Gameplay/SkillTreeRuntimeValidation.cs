using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using DetectiveDesk.Gameplay;
namespace DetectiveDesk.Editor
{
    [InitializeOnLoad] public static class SkillTreeRuntimeValidation
    {
        static SkillTreeRuntimeValidation(){EditorApplication.playModeStateChanged+=State;}
        public static void Arm(){if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");SessionState.SetBool("SkillTreeTest",true);SessionState.SetBool("SkillTreeBackground",Application.runInBackground);Application.runInBackground=true;EditorApplication.isPlaying=true;}
        static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("SkillTreeTest",false))new GameObject("Temporary skill tree validation").AddComponent<SkillTreeValidationRunner>();if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("SkillTreeTest",false)){Application.runInBackground=SessionState.GetBool("SkillTreeBackground",false);SessionState.SetBool("SkillTreeTest",false);}}
    }
    public sealed class SkillTreeValidationRunner:MonoBehaviour
    {
        readonly List<string> checks=new List<string>();GameManager g;SkillTreePanel p;
        void Check(bool ok,string s){if(!ok)throw new Exception(s);checks.Add(s);}
        void Start(){StartCoroutine(Guard());}
        IEnumerator Guard(){var run=Run();string failure=null;while(true){object value=null;bool next=false;try{next=run.MoveNext();if(next)value=run.Current;}catch(Exception e){failure=e.ToString();}if(failure!=null||!next)break;yield return value;}File.WriteAllText("ProductionLogs/SkillTree/runtime-validation.json",JsonConvert.SerializeObject(new{status=failure==null?"PASS":"FAIL",checks,failure,actualPlayMode=true,actualFrames=true,assetValuesMutated=false,physicalMouseAndEscapeSynthesized=false},Formatting.Indented));Debug.Log("Skill tree runtime "+(failure==null?"PASS":"FAIL")+" "+checks.Count+" "+failure);EditorApplication.isPlaying=false;}
        SkillTreeNodeView View(string id)=>p.nodeViews.Single(v=>v.nodeId==id);
        void Select(string id){View(id).button.onClick.Invoke();Check(p.SelectedId==id,"Node selection callback "+id);}
        void ResetLevels(){var so=new SerializedObject(g);foreach(var field in new[]{"speedLevel","motorLevel","durabilityLevel"})so.FindProperty(field).intValue=0;so.ApplyModifiedPropertiesWithoutUndo();foreach(var f in g.figurines)f.SetTier(0);g.Notify();}
        void Buy(string id){Select(id);double before=g.Money;double cost=g.skillTree.Cost(g,g.skillTree.Find(id));int rank=g.skillTree.CurrentRank(g,g.skillTree.Find(id));p.buyButton.onClick.Invoke();Check(g.Money==before-cost&&g.skillTree.CurrentRank(g,g.skillTree.Find(id))==rank+1,"Actual purchase callback fee and single effect "+id);before=g.Money;p.buyButton.onClick.Invoke();Check(g.Money==before,"Owned callback never double charges "+id);}
        IEnumerator Run()
        {
            yield return null;yield return null;g=FindFirstObjectByType<GameManager>();p=g.skillTreePanel;
            Check(g.Money==422&&g.SpeedLevel==0&&g.MotorLevel==0&&g.DurabilityLevel==0&&g.figurines.All(f=>f.Tier==0),"Original cash and progress at Play start");
            Check(p&&p.game==g&&p.definition==g.skillTree&&p.nodeViews.Length==39&&p.nodeViews.Select(v=>v.nodeId).Distinct().Count()==39,"39 unique views share actual graph and manager");
            Check(p.nodeViews.All(v=>v.owner==p&&v.button&&v.title&&v.state&&v.cost&&v.button.onClick.GetPersistentEventCount()==1&&v.button.onClick.GetPersistentTarget(0)==v&&v.button.onClick.GetPersistentMethodName(0)=="Select"),"All node callbacks and labels correctly bound");
            Check(p.buyButton.onClick.GetPersistentEventCount()==1&&p.buyButton.onClick.GetPersistentTarget(0)==p&&p.buyButton.onClick.GetPersistentMethodName(0)=="PurchaseSelected","Exactly one production purchase callback");
            Check(FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length==1&&FindObjectsByType<GlobeController>(FindObjectsSortMode.None).Length==1&&FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Single manager globe and EventSystem");
            g.legacy.ToggleUpgrades();yield return null;p.ShowEngineering();
            Check(g.WorldInputBlocked&&!g.panel.gameObject.activeSelf&&p.engineering.activeSelf&&!p.restorations.activeSelf,"Engineering modal blocks world and retains inactive legacy list");
            Select("motor_03");double cash=g.Money;Check(!p.buyButton.interactable&&p.detailRequirements.text.Contains("NEEDED")&&p.detailRequirements.text.Contains("Momentum 1"),"Locked selected node displays all requirements and disabled buy");p.buyButton.onClick.Invoke();Check(g.Money==cash&&!p.TryPurchaseSelected(),"Locked actual callback cannot spend");
            Select("speed_01");Check(g.Money==cash,"Selection spends no currency");g.legacy.money=0;yield return null;
            Check(View("speed_01").CurrentState==SkillNodeState.Unaffordable&&!p.buyButton.interactable&&View("motor_03").CurrentState==SkillNodeState.Locked,"Cash delta over real frame distinguishes unaffordable and prerequisite lock");
            g.Credit(1000000);Check(p.buyButton.interactable,"Income event refreshes purchase immediately");
            p.engineeringScroll.verticalNormalizedPosition=1;GameplayIntegration.Capture("ProductionLogs/SkillTree/engineering-tree.png");
            Buy("motor_01");Buy("motor_02");Select("motor_03");cash=g.Money;g.panel.BuyMotor();g.legacy.UpgradeIncome();Check(g.MotorLevel==2&&g.Money==cash&&!p.buyButton.interactable,"Hidden legacy list and DetectiveGame.UpgradeIncome cannot bypass motor3 prerequisite");
            Buy("speed_01");Check(View("motor_03").CurrentState==SkillNodeState.Available,"Speed1 unlocks motor3");Buy("motor_03");
            Select("speed_01");GameplayIntegration.Capture("ProductionLogs/SkillTree/purchased-tree.png");
            p.engineeringScroll.StopMovement();Canvas.ForceUpdateCanvases();p.engineeringScroll.verticalNormalizedPosition=0;yield return null;yield return null;Canvas.ForceUpdateCanvases();
            GameplayIntegration.Capture("ProductionLogs/SkillTree/engineering-bottom.png");
            var bottom=(RectTransform)View("speed_10").transform;Vector3[] corners=new Vector3[4];bottom.GetWorldCorners(corners);Check(corners.All(c=>p.engineeringScroll.viewport.rect.Contains(p.engineeringScroll.viewport.InverseTransformPoint(c))),"Rank10 completely reachable within bounded viewport");Select("speed_10");Check(!p.buyButton.interactable,"Bottom rank10 selectable while locked");
            p.ShowRestorations();Select("figure_0_1");cash=g.Money;Check(!p.buyButton.interactable&&!g.RestoreFigurine(0)&&g.Money==cash,"Restoration1 blocked before Pincraft1");
            Buy("durability_01");g.InspectFigurine(g.figurines[2]);Check(p.CurrentTab==1&&p.SelectedId=="figure_2_1"&&p.restorations.activeSelf,"Figurine opens restoration tab and focuses its next tier");
            GameplayIntegration.Capture("ProductionLogs/SkillTree/restoration-tree.png");
            Buy("figure_0_1");Buy("figure_0_2");Select("figure_0_3");cash=g.Money;Check(!g.RestoreFigurine(0)&&g.Money==cash&&!p.buyButton.interactable,"Restoration3 blocked until Speed3");Buy("speed_02");Buy("speed_03");Buy("figure_0_3");Check(g.figurines[1].Tier==0&&g.figurines[2].Tier==0&&g.GlobalMultiplier==5,"Independent figurines and existing multiplier sum");
            ResetLevels();g.legacy.money=1000000;Buy("durability_01");Buy("figure_1_1");Select("figure_1_2");cash=g.Money;Check(!g.RestoreFigurine(1)&&g.Money==cash&&!p.buyButton.interactable,"Restoration2 blocked until Motor1");Buy("motor_01");Buy("figure_1_2");
            var so=new SerializedObject(g);so.FindProperty("motorLevel").intValue=7;so.ApplyModifiedPropertiesWithoutUndo();g.Notify();Check(g.skillTree.State(g,g.skillTree.Find("motor_07"),out var reason)==SkillNodeState.Owned&&g.SpeedLevel==0,"Legacy progress grandfathered without new prerequisites");
            ResetLevels();g.legacy.money=double.NaN;Check(!g.PurchaseSkill("speed_01")&&!g.TrySpend(0),"Nonfinite balance fails closed");g.legacy.money=422;
            p.ShowEngineering();p.engineeringScroll.verticalNormalizedPosition=1;g.Notify();
            var close=g.legacy.upgradesPanel.transform.Find("Button_CLOSE").GetComponent<Button>();Check(close.onClick.GetPersistentMethodName(0)=="ToggleUpgrades","Original close binding retained");close.onClick.Invoke();Check(!g.legacy.upgradesPanel.activeSelf&&!g.WorldInputBlocked,"Original close callback clears world input guard");
            g.legacy.ToggleSettings();Check(g.WorldInputBlocked,"Settings remains connected");g.legacy.ToggleSettings();g.legacy.OpenFiles();Check(g.archive.IsArchiveOpen&&g.WorldInputBlocked,"Archive remains connected");g.archive.CloseArchive();
            Check(g.Money==422&&g.SpeedLevel==0&&g.MotorLevel==0&&g.DurabilityLevel==0&&g.figurines.All(f=>f.Tier==0),"Runtime test state reset before exit");
        }
    }
}
