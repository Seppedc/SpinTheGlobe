using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using DetectiveDesk.Gameplay;

namespace DetectiveDesk.Editor
{
    [InitializeOnLoad]
    public static class GameplayRuntimeValidation
    {
        static GameplayRuntimeValidation() { EditorApplication.playModeStateChanged += State; }
        public static void Arm()
        {
            if(EditorApplication.isPlaying)throw new Exception("Arm only before Play");
            SessionState.SetBool("GameplayValidationArmed",true);
            SessionState.SetBool("GameplayPreviousRunInBackground",Application.runInBackground);
            Application.runInBackground=true; EditorApplication.isPlaying=true;
        }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("GameplayValidationArmed",false))
            {new GameObject("Gameplay validation temporary").AddComponent<GameplayValidationRunner>();}
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("GameplayValidationArmed",false))
            {Application.runInBackground=SessionState.GetBool("GameplayPreviousRunInBackground",false);SessionState.SetBool("GameplayValidationArmed",false);}
        }
    }
    public sealed class GameplayValidationRunner : MonoBehaviour
    {
        readonly List<string> checks=new List<string>();
        GameManager game; GlobeController globe; WallMapManager map; UpgradeConfig asset,config;
        void Check(bool ok,string message) {if(!ok)throw new Exception(message);checks.Add(message);}
        void Level(string field,int value) {var so=new SerializedObject(game);so.FindProperty(field).intValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        void Start() {StartCoroutine(Guard());}
        IEnumerator Guard()
        {
            var test=Run();string failure=null;
            while(true)
            {
                object yielded=null;bool next=false;
                try {next=test.MoveNext();if(next)yielded=test.Current;}catch(Exception e){failure=e.ToString();}
                if(failure!=null||!next)break;
                yield return yielded;
            }
            if(game&&asset)game.config=asset;if(config)Destroy(config);
            File.WriteAllText("ProductionLogs/Gameplay/runtime-validation.json",JsonConvert.SerializeObject(new {status=failure==null?"PASS":"FAIL",checks, failure,physicalMouseEvents="Not simulated; production callbacks, polygon camera rays and UI guards tested",actualPlayMode=true,actualFrames=true,configAssetMutated=false,testMoneyPersisted=false,deterministicCrossings="Production GlobeController.Advance called between actual coroutine frames with automatic Update temporarily disabled"},Formatting.Indented));
            Debug.Log("Gameplay runtime validation "+(failure==null?"PASS":"FAIL")+" checks="+checks.Count+" "+failure);
            EditorApplication.isPlaying=false;
        }
        IEnumerator Run()
        {
            yield return null; yield return null;
            game=UnityEngine.Object.FindFirstObjectByType<GameManager>();globe=game.globe;map=game.map;asset=game.config;
            Check(game&&game.enabled&&game.legacy.Gameplay==game,"Single active manager owns legacy adapter");
            Check(!Physics.GetCurrentIntegrationInfo().isFallback&&Physics.GetCurrentIntegrationInfo().name=="PhysX","Installed PhysX backend active, not fallback");
            Physics.SyncTransforms();
            RaycastHit globeHit;
            Check(Physics.Raycast(new Ray(Camera.main.transform.position,globe.Center-Camera.main.transform.position),out globeHit,100)&&globeHit.collider==globe.dragCollider,"Actual physics camera ray acquires original globe drag collider");
            Check(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<GlobeController>(FindObjectsSortMode.None).Length==1,"No duplicate controllers");
            Check(game.figurines.Length==3&&game.GlobalMultiplier==3,"Three figurines start at SUM multiplier 3");
            Check(game.pinSlots.Length==12&&game.pinSlots.All(s=>s.Occupant)&&game.pinSlots.Select(s=>s.Occupant.SkinIndex).Distinct().Count()==12,"Twelve empty coffer sockets populated with all independent colors");
            foreach(var slot in game.pinSlots)
            {
                var collider=slot.GetComponent<Collider>();RaycastHit hit;
                Check(Physics.Raycast(new Ray(Camera.main.transform.position,collider.bounds.center-Camera.main.transform.position),out hit,100)&&hit.collider==collider,"Original coffer slot physically reachable from camera "+slot.SlotNumber);
            }
            var money=game.Money;var rotation=globe.rotor.localRotation;yield return new WaitForSeconds(.6f);
            Check(game.Money==money&&globe.TotalDegrees==0&&globe.CurrentVelocity==0&&Quaternion.Angle(rotation,globe.rotor.localRotation)<.0001,"At rest: no auto power, hidden rotation or passive cash over actual frames");
            globe.AddSwipe(-1000);Check(globe.CurrentVelocity==0,"Reverse swipe ignored");
            globe.AddSwipe(10000);Check(globe.CurrentVelocity==30&&globe.IsRedline,"Positive swipe caps at 30 deg/s redline");globe.AddSwipe(100);Check(globe.CurrentVelocity==30,"Redline ignores further acceleration");
            var previous=globe.rotor.localRotation;yield return new WaitForSeconds(.12f);
            Check(Mathf.DeltaAngle(previous.eulerAngles.y,globe.rotor.localEulerAngles.y)<0&&globe.TotalDegrees>0,"Actual Update rotates negative Unity Y for west-east");
            var tach=UnityEngine.Object.FindFirstObjectByType<TachometerUI>();Check(tach.needle&&tach.gaugeFace&&tach.speedText&&tach.Ratio>0,"Original relocated gauge and actual RPM label update");
            var mechanism=globe.GetComponent<DetectiveGlobeMechanism>();
            Check(tach.needle==mechanism.rpmNeedle&&tach.needle.name=="RPMGauge_NeedlePivot","Tachometer owns original mechanism pivot reference");
            var visual=tach.needle.Find("GlobeRework_Needle");
            Check(visual&&visual.localPosition==Vector3.zero&&Quaternion.Angle(visual.localRotation,Quaternion.identity)<.001f&&visual.localScale==Vector3.one,"Needle visual module retains baseline identity pose");
            yield return new WaitForEndOfFrame();
            float needleAngle=Mathf.Lerp(135,-135,tach.Ratio);
            Check(Quaternion.Angle(tach.needle.localRotation,Quaternion.Euler(0,0,needleAngle))<.01f,"Original pivot angle matches physical RPM ratio");
            var needleMesh=visual.GetComponentInChildren<MeshFilter>();
            Vector3 physical=(needleMesh.transform.TransformPoint(needleMesh.sharedMesh.bounds.center)-tach.needle.position).normalized;
            Vector3 expectedDirection=tach.needle.parent.TransformDirection(Quaternion.Euler(0,0,needleAngle)*Vector3.up).normalized;
            Check(Vector3.Dot(physical,expectedDirection)>.95f,"Physical needle mesh direction matches original pivot without doubled child angle");
            Check(tach.speedText.canvas.renderMode==RenderMode.WorldSpace&&Vector3.Distance(tach.speedText.transform.position,tach.gaugeFace.bounds.center)<.8f,"Compact actual RPM label beside physical gauge");
            globe.enabled=false;
            config=Instantiate(asset);config.name="Temporary validation configuration";game.config=config;
            game.legacy.money=0;Check(!game.BuyUpgrade(UpgradeKind.Speed)&&game.Money==0,"Unaffordable upgrade rejected without negative cash");
            game.legacy.money=1000000000; money=game.Money;Check(game.BuyUpgrade(UpgradeKind.AutoMotor)&&game.MotorLevel==1&&game.Money==money-100&&globe.AutoFloor==6,"Motor purchase costs 100 and establishes exact 6 deg/s floor");
            foreach(var kind in new[]{UpgradeKind.Speed,UpgradeKind.AutoMotor,UpgradeKind.Durability})
            {
                while(game.Level(kind)<config.maximumUpgradeLevel){int level=game.Level(kind);double cost=config.Cost(kind,level);money=game.Money;Check(game.BuyUpgrade(kind)&&game.Money==money-cost,"Cost and level: "+kind+" "+level);}
                money=game.Money;Check(!game.BuyUpgrade(kind)&&money==game.Money,"Maximum level blocks "+kind);
            }
            for(int i=0;i<3;i++)for(int tier=0;tier<3;tier++){money=game.Money;double cost=config.RestorationCost(tier);Check(game.RestoreFigurine(i)&&game.figurines[i].Tier==tier+1&&game.Money==money-cost,"Restoration cost and tier slot "+i+" tier "+tier);}
            Check(game.GlobalMultiplier==9&&!game.RestoreFigurine(2),"All three fully restored give SUM 9 and cap at tier 3");
            foreach(var f in game.figurines)f.SetTier(0);
            foreach(var f in game.figurines)
            {float surface=f.scatterFrame.TransformPoint(f.scatterCenter).y;Check(f.bustRenderers.All(r=>r.bounds.min.y>=surface-.002f&&r.bounds.min.y<surface+.008f),"Rubble parts rest on plinth "+f.name);}
            Level("speedLevel",0);Level("motorLevel",0);Level("durabilityLevel",0);game.legacy.money=422;globe.EnforceLimits();game.Notify();
            int files=0,cases=0;game.legacy.FilesOpened+=()=>files++;game.legacy.CaseSolved+=()=>cases++;
            game.legacy.OpenFiles();Check(files==1&&game.archive.IsArchiveOpen&&game.WorldInputBlocked,"Files callback opens existing archive and blocks world input");game.archive.SelectEvidence(5);Check(game.archive.EvidenceIndex==5,"Existing archive evidence navigation");game.archive.CloseArchive();
            game.legacy.ToggleSettings();Check(game.WorldInputBlocked,"Settings blocks world input");game.legacy.ToggleSettings();
            game.InspectFigurine(game.figurines[2]);Check(game.legacy.upgradesPanel.activeSelf&&game.WorldInputBlocked,"Third figurine opens existing Upgrades modal");
            Check(game.panel.upgradeButtons.All(b=>b.onClick.GetPersistentEventCount()==1)&&game.panel.figurineButtons.All(b=>b.onClick.GetPersistentEventCount()==1),"Six persistent purchase callbacks bound");
            GameplayIntegration.Capture("ProductionLogs/Gameplay/upgrades-panel.png");game.legacy.ToggleUpgrades();
            money=game.Money;game.legacy.SolveCase();game.legacy.SolveCase();Check(cases==1&&game.Money==money+50&&game.legacy.stampMark.activeSelf&&game.archive.CompletionPlayCount==1,"Existing stamp pays case +50 once and preserves completion event");
            Check(game.legacy.moneyText.text.Contains(game.Money.ToString("N2")),"Existing money HUD reflects case callback");
            foreach(var target in map.targets)
            {CountryData picked;Vector2 coord;Vector3 point=map.GeographicPoint(target.data.latitude,target.data.longitude);Check(map.TryPick(new Ray(Camera.main.transform.position,(point-Camera.main.transform.position).normalized),out picked,out coord)&&picked==target.data,"Camera ray picks actual polygon "+target.data.id);}
            Check(globe.CanBeginDrag(new Ray(Camera.main.transform.position, globe.Center-Camera.main.transform.position)),"Visible globe center acquires drag through original physical collider");
            var brazilPoint=map.GeographicPoint(map.targets.First(t=>t.data.id=="BRA").data.latitude,map.targets.First(t=>t.data.id=="BRA").data.longitude);
            Check(!globe.CanBeginDrag(new Ray(Camera.main.transform.position,brazilPoint-Camera.main.transform.position)),"Visible Brazil map target does not also acquire globe drag through oversized legacy box");
            CountryData ocean;Vector2 oceanPoint;var sea=map.GeographicPoint(-40,-140);Check(!map.TryPick(new Ray(Camera.main.transform.position,sea-Camera.main.transform.position),out ocean,out oceanPoint),"Ocean ray rejected");
            var testTarget=map.targets.First(t=>t.data.id=="EGY").data;var testPoint=map.GeographicPoint(testTarget.latitude,testTarget.longitude);var testRay=new Ray(Camera.main.transform.position,testPoint-Camera.main.transform.position);
            var blocker=new GameObject("Temporary map obstruction validation");blocker.transform.position=testPoint-testRay.direction*.2f;var blockerCollider=blocker.AddComponent<BoxCollider>();blockerCollider.size=Vector3.one*.1f;Physics.SyncTransforms();
            Check(!map.TryPick(testRay,out ocean,out oceanPoint),"Actual collider obstruction blocks map ray");Destroy(blocker);yield return null;
            var first=map.targets.First(t=>t.data.id=="BEL").data;var second=map.targets.First(t=>t.data.id=="EGY").data;
            game.pinSlots[0].Select();Check(map.Launch(first,first.latitude,first.longitude),"Real Belgium pin launched");Check(!map.Launch(first,first.latitude,first.longitude),"Duplicate pending pin blocked");
            game.pinSlots[1].Select();Check(map.Launch(second,second.latitude,second.longitude),"Second country launched with different selected skin");
            yield return new WaitForSeconds(.45f);
            Check(map.ActiveCount==2&&map.PendingCount==0&&map.yarn.line.positionCount==2,"Actual flight coroutines finish and yarn connects two pins");Check(!map.Launch(first,first.latitude,first.longitude),"Duplicate active country blocked");
            var pins=map.ActivePinsSnapshot();Check(pins[0].GetComponent<PinSkin>().SkinIndex==0&&pins[1].GetComponent<PinSkin>().SkinIndex==1&&game.pinSlots.All(s=>s.Occupant),"Two fired colors independent and reusable coffer templates retained");
            foreach(var p in pins)
            {
                Check(p.Marker.transform.parent==globe.rotor&&(p.Marker.transform.localPosition-globe.MarkerPosition(p.Latitude,p.Longitude)).magnitude<.000001f,"Marker lat/lon/radius and rotor parent "+p.Country.id);
                var pin=p.GetComponent<PinSkin>();var rs=pin.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                Check(Vector3.Dot((pin.EnamelHeadRenderer.bounds.center-b.center).normalized,map.mapNormal)>.9f,"Pin head faces outward from board "+p.Country.id);
                var prefabMaterials=map.pinPrefab.GetComponentsInChildren<Renderer>().Where(r=>r!=map.pinPrefab.EnamelHeadRenderer).SelectMany(r=>r.sharedMaterials).ToArray();
                Check(pin.GetComponentsInChildren<Renderer>().Where(r=>r!=pin.EnamelHeadRenderer).SelectMany(r=>r.sharedMaterials).SequenceEqual(prefabMaterials),"Original brass shared materials retained "+p.Country.id);
            }
            game.figurines[0].SetTier(1);game.figurines[1].SetTier(2);game.figurines[2].SetTier(3);
            yield return null;GameplayIntegration.Capture("ProductionLogs/Gameplay/runtime-game.png");
            foreach(var f in game.figurines)f.SetTier(0);
            // Exact crossing assertions use the actual production controller; only automatic frame advance is disabled.
            config.frictionAt60Hz=1;config.baseMaximumSpeed=360;config.autoSpeedPerLevel=360;Level("motorLevel",1);globe.EnforceLimits();
            double expected=0;double phase=globe.TotalDegrees;double end=pins.Max(p=>p.NextPass)+.01;expected=pins.Sum(p=>p.Country.baseValue)*game.GlobalMultiplier+GameplayMath.Crossings(phase,end,globe.FirstRevolutionAngle)*config.baseRevolutionReward*game.GlobalMultiplier;money=game.Money;
            globe.Advance((end-phase)/360);Check(Math.Abs(game.Money-money-expected)<1e-7&&map.ActiveCount==0&&map.DroppedCount==2,"Single-charge countries exact payout once, final detachment and yarn cleanup");yield return null;
            Check(pins.All(p=>!p.Marker)&&map.yarn.line.positionCount==0,"Final drops remove both globe markers and yarn");
            Check(pins.All(p=>p.GetComponent<Rigidbody>()&&!p.GetComponent<Rigidbody>().isKinematic&&p.GetComponent<BoxCollider>()),"Final pins have dynamic rigidbodies and drop colliders");
            float dropHeight=pins[0].transform.position.y;yield return new WaitForSeconds(.3f);var dropBody=pins[0].GetComponent<Rigidbody>();var dropBox=pins[0].GetComponent<BoxCollider>();
            File.WriteAllText("ProductionLogs/Gameplay/drop-diagnostic.json",JsonConvert.SerializeObject(new {height=dropHeight,after=pins[0].transform.position.y,body=EditorJsonUtility.ToJson(dropBody),collider=EditorJsonUtility.ToJson(dropBox),bounds=dropBox.bounds.ToString(),scene=dropBody.gameObject.scene.path,overlaps=Physics.OverlapBox(dropBox.bounds.center,dropBox.bounds.extents).Select(c=>c.name+" "+c.bounds).ToArray()},Formatting.Indented));
            Check(pins[0].transform.position.y<dropHeight,"Detached pin physically falls over actual physics frames (y="+dropHeight+" -> "+pins[0].transform.position.y+", velocity="+dropBody.linearVelocity+", constraints="+dropBody.constraints+", gravity="+Physics.gravity+", mode="+Physics.simulationMode+", sleeping="+dropBody.IsSleeping()+", fixed="+Time.fixedTime+")");
            yield return new WaitForSeconds(1.1f);
            Check(pins[0].transform.position.y>.75f&&pins[0].transform.position.y<1.05f&&Mathf.Abs(dropBody.linearVelocity.y)<.25f,"Dropped pin lands and rests on existing desk collider");
            phase=globe.TotalDegrees;money=game.Money;globe.Advance(.01);Check(game.Money==money,"No early repeat after payout");
            Level("durabilityLevel",2);game.pinSlots[2].Select();Check(map.Launch(first,first.latitude,first.longitude),"Three-charge pin launch");yield return new WaitForSeconds(.45f);
            var durable=map.ActivePins[0];Check(durable.RemainingPasses==3,"Durability captured at shot time");
            for(int pass=0;pass<3;pass++)
            {
                phase=globe.TotalDegrees;end=durable.NextPass+.001;money=game.Money;double basePay=GameplayMath.Crossings(phase,end,globe.FirstRevolutionAngle)*config.baseRevolutionReward*game.GlobalMultiplier;
                globe.Advance((end-phase)/360);Check(Math.Abs(game.Money-money-basePay-first.baseValue*game.GlobalMultiplier)<1e-7&&durable.RemainingPasses==2-pass,"Exact charge decrement and payout pass "+(pass+1));
                if(pass<2){var rest=durable.transform.rotation;yield return null;Check(Quaternion.Angle(rest,durable.transform.rotation)>1&&map.corkDust.particleCount>0,"Intermediate tilt/wobble and world cork dust");phase=globe.TotalDegrees;money=game.Money;globe.Advance(.01);Check(game.Money==money&&durable.RemainingPasses==2-pass,"Next charge waits full 360 degree interval");}
            }
            yield return null;Check(map.ActiveCount==0&&!durable.Marker,"Three-charge final cleanup");
            Check(map.Launch(second,second.latitude,second.longitude),"Skipped-turn test launch");yield return new WaitForSeconds(.45f);var skipped=map.ActivePins[0];phase=globe.TotalDegrees;end=phase+1081;money=game.Money;expected=second.baseValue*3*game.GlobalMultiplier+GameplayMath.Crossings(phase,end,globe.FirstRevolutionAngle)*config.baseRevolutionReward*game.GlobalMultiplier;
            globe.Advance(1081d/360);Check(Math.Abs(game.Money-money-expected)<1e-7&&skipped.RemainingPasses==0&&map.ActiveCount==0,"Skipped three turns neither lose nor duplicate country or base rewards");
            phase=globe.TotalDegrees;money=game.Money;globe.Advance(1);Check(Math.Abs(game.Money-money-config.baseRevolutionReward*game.GlobalMultiplier)<1e-7,"One full origin revolution pays exact base times global");
            game.legacy.settingsPanel.SetActive(false);game.legacy.upgradesPanel.SetActive(false);game.archive.CloseArchive();
        }
    }
}
