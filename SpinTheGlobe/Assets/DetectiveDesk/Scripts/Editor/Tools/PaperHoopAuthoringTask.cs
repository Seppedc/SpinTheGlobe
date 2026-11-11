using System;
using System.IO;
using System.Linq;
using DetectiveDesk.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DetectiveDesk.Editor
{
    // Local file-triggered authoring runner used while the Unity connector is unavailable.
    [InitializeOnLoad]
    internal static class PaperHoopAuthoringTask
    {
        const string Request = "Logs/paper-hoop.request";
        static PaperHoopAuthoringTask() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
            string action = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try
            {
                if (action == "author") Author();
                else if (action == "author-whiskey") AuthorWhiskey();
                else if (action == "capture") Capture();
                else if (action == "orient") Orient();
                else if (action == "physics") AuthorPhysics();
                else if (action == "wall-floor") AuthorWallFloorHoop();
                else if (action == "refresh") { AssetDatabase.Refresh(); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); }
                else if (action == "inspect-env")
                {
                    var sb = new System.Text.StringBuilder();
                    var env = GameObject.Find("Environment");
                    if (env)
                    {
                        foreach (Transform child in env.GetComponentsInChildren<Transform>(true))
                        {
                            var r = child.GetComponent<Renderer>();
                            sb.AppendLine($"{child.name} (parent: {child.parent?.name}) -> pos: {child.position}, local: {child.localPosition}, scale: {child.localScale}, bounds: {(r ? r.bounds.ToString() : "none")}");
                        }
                    }
                    var hoop = GameObject.Find("Paper Basketball Hoop");
                    if (hoop)
                    {
                        sb.AppendLine("=== HOOP ===");
                        sb.AppendLine($"Hoop root pos: {hoop.transform.position}, localPos: {hoop.transform.localPosition}, rot: {hoop.transform.eulerAngles}");
                        foreach (Transform child in hoop.GetComponentsInChildren<Transform>(true))
                        {
                            var r = child.GetComponent<Renderer>();
                            var l = child.GetComponent<Light>();
                            sb.AppendLine($"  {child.name} -> pos: {child.position}, rot: {child.eulerAngles}, light: {(l ? l.type + ", intensity: " + l.intensity : "none")}, bounds: {(r ? r.bounds.ToString() : "none")}");
                        }
                    }
                    var cam = Camera.main;
                    if (cam)
                    {
                        sb.AppendLine($"MainCamera pos: {cam.transform.position}, rot: {cam.transform.eulerAngles}, fov: {cam.fieldOfView}");
                    }
                    File.WriteAllText("Logs/inspect-env.txt", sb.ToString());
                }
                else if (action == "tests")
                {
                    var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                    api.RegisterCallbacks(new Results());
                    api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
                }
                File.WriteAllText("Logs/paper-hoop-" + action + ".txt", "OK " + DateTime.UtcNow);
            }
            catch (Exception e) { File.WriteAllText("Logs/paper-hoop-" + action + ".txt", e.ToString()); Debug.LogException(e); }
        }
        sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor t) { }
            public void TestStarted(ITestAdaptor t) { }
            public void TestFinished(ITestResultAdaptor t) { }
            public void RunFinished(ITestResultAdaptor r) { TestRunnerApi.SaveResultToFile(r,"Logs/paper-hoop-editmode.xml"); }
        }
        static Material Material(string name, Color color, float metal, float smooth)
        {
            string path="Assets/DetectiveDesk/Art/Props/PaperHoop/Materials/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path); }
            m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        static void Author()
        {
            var scene=SceneManager.GetActiveScene();
            if (scene.isDirty) throw new InvalidOperationException("Unsaved scene edits preserved; cannot automatically author.");
            var game=Object.FindAnyObjectByType<GameManager>();
            if (!game || !scene.path.EndsWith("DetectiveOffice.unity")) throw new InvalidOperationException("Open DetectiveOffice first.");
            string dir="Assets/DetectiveDesk/Art/Props/PaperHoop/";
            Directory.CreateDirectory(dir+"Materials"); Directory.CreateDirectory(dir+"Prefabs");AssetDatabase.Refresh();
            var wood=Material("Walnut",new Color(.23f,.105f,.04f),0,.35f);
            var brass=Material("Brass",new Color(.55f,.35f,.13f),.8f,.48f);
            var rimMat=Material("RedMetal",new Color(.58f,.12f,.035f),.6f,.4f);
            var cotton=Material("Cotton",new Color(.95f,.93f,.88f),0,.12f);
            var root=GameObject.Find("Paper Basketball Hoop");
            if (!root)
            {
                root=new GameObject("Paper Basketball Hoop");Undo.RegisterCreatedObjectUndo(root,"Add paper hoop");
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(dir+"Models/PaperHoop.fbx"));
                visual.name="Visuals";visual.transform.SetParent(root.transform,false);
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())
                {
                    var mats=renderer.sharedMaterials;
                    for(int i=0;i<mats.Length;i++)
                    {
                        var n=mats[i].name;
                        mats[i]=n.Contains("Walnut")?wood:n.Contains("Cotton")?cotton:n.Contains("RedMetal")?rimMat:brass;
                    }
                    renderer.sharedMaterials=mats;
                }
                var rim=visual.GetComponentsInChildren<Renderer>().First(r=>r.name=="Scoring_Rim");
                var board=visual.GetComponentsInChildren<Renderer>().First(r=>r.name=="Backboard");
                if(rim.bounds.center.z>board.bounds.center.z)visual.transform.localRotation=Quaternion.Euler(0,180,0)*visual.transform.localRotation;
                var target=new GameObject("Hoop scoring center").transform;target.SetParent(root.transform,false);target.position=rim.bounds.center;
                var anchor=new GameObject("Score anchor").transform;anchor.SetParent(root.transform,false);anchor.position=new Vector3(board.bounds.center.x,board.bounds.max.y+.20f,board.bounds.center.z-.06f);
                PrefabUtility.SaveAsPrefabAssetAndConnect(root,dir+"Prefabs/PaperHoop.prefab",InteractionMode.AutomatedAction);
            }
            root.transform.SetParent(game.transform,false);root.transform.position=new Vector3(5.7f,1.05f,2.45f);root.transform.localScale=Vector3.one*1.25f;
            var old=GameObject.Find("Wastepaper Basket");
            if(old)
            {
                var light=old.GetComponentInChildren<Light>();
                if(light){light.name="Hoop warm bounce";light.transform.SetParent(root.transform,true);}
                Undo.DestroyObjectImmediate(old);
            }
            var paper=game.GetComponent<PaperBallThrower>();
            if(!paper)paper=Undo.AddComponent<PaperBallThrower>(game.gameObject);
            paper.basketTarget=root.transform.Find("Hoop scoring center");paper.scoreAnchor=root.transform.Find("Score anchor");
            game.balance.paperBallBasketRadius=.20f;
            ScenePresentationAuthoring.EnsureLayout(game);
            EditorUtility.SetDirty(paper);EditorUtility.SetDirty(game.balance);AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            EconomySnapshotMenu.Export();
            var camera=Camera.main;
            File.WriteAllText("Logs/paper-hoop-placement.txt", "Target="+paper.basketTarget.position+" viewport="+camera.WorldToViewportPoint(paper.basketTarget.position)+" score="+camera.WorldToViewportPoint(paper.scoreAnchor.position));
            Capture();
        }
        static void Orient()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.isDirty) throw new InvalidOperationException("Unsaved edits preserved before orientation fix.");
            var root=GameObject.Find("Paper Basketball Hoop");
            root.transform.position=new Vector3(5.7f,.9f,1.65f);
            root.transform.localScale=Vector3.one;
            var visual=root.transform.Find("Visuals");
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DetectiveDesk/Art/Props/PaperHoop/Models/PaperHoop.fbx");
            visual.localRotation=imported.transform.localRotation;
            var rim=visual.GetComponentsInChildren<Renderer>().First(r=>r.name=="Scoring_Rim");
            var board=visual.GetComponentsInChildren<Renderer>().First(r=>r.name=="Backboard");
            if(rim.bounds.center.z>board.bounds.center.z)visual.localRotation=Quaternion.Euler(0,180,0)*visual.localRotation;
            root.transform.Find("Hoop scoring center").position=rim.bounds.center;
            root.transform.Find("Score anchor").position=new Vector3(board.bounds.center.x,board.bounds.max.y+.65f,board.bounds.center.z-.08f);
            var paper=Object.FindAnyObjectByType<PaperBallThrower>();
            paper.ScoreCanvas.transform.position=paper.scoreAnchor.position;
            PrefabUtility.ApplyPrefabInstance(root,InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene);
            Capture();
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/paper-hoop-placement.txt","Import rotation="+imported.transform.localEulerAngles+" rim="+rim.bounds+" board="+board.bounds);
        }
        static void AuthorPhysics()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.path.EndsWith("DetectiveOffice.unity"))
            {
                EditorSceneManager.OpenScene("Assets/DetectiveDesk/Scenes/DetectiveOffice.unity");
                scene = SceneManager.GetActiveScene();
            }

            // 1. Author PaperBall.prefab
            string ballPrefabPath = "Assets/DetectiveDesk/Art/Props/PaperBall/Prefabs/PaperBall.prefab";
            var ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ballPrefabPath);
            if (ballPrefab)
            {
                using (var edit = new PrefabUtility.EditPrefabContentsScope(ballPrefabPath))
                {
                    var root = edit.prefabContentsRoot;
                    var col = root.GetComponent<SphereCollider>();
                    if (!col) col = root.AddComponent<SphereCollider>();
                    col.center = new Vector3(0, 0.065f, 0);
                    col.radius = 0.065f;

                    var rb = root.GetComponent<Rigidbody>();
                    if (!rb) rb = root.AddComponent<Rigidbody>();
                    rb.mass = 0.05f;
                    rb.linearDamping = 0.25f;
                    rb.angularDamping = 0.5f;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;

                    var instance = root.GetComponent<PaperBallInstance>();
                    if (!instance) instance = root.AddComponent<PaperBallInstance>();
                }
            }

            // 2. Author PaperHoop.prefab
            string hoopPrefabPath = "Assets/DetectiveDesk/Art/Props/PaperHoop/Prefabs/PaperHoop.prefab";
            var hoopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hoopPrefabPath);
            if (hoopPrefab)
            {
                using (var edit = new PrefabUtility.EditPrefabContentsScope(hoopPrefabPath))
                {
                    var root = edit.prefabContentsRoot;
                    var visuals = root.transform.Find("Visuals");
                    if (visuals)
                    {
                        var board = visuals.Find("Backboard");
                        if (board)
                        {
                            var boardCol = board.GetComponent<BoxCollider>();
                            if (!boardCol) boardCol = board.gameObject.AddComponent<BoxCollider>();
                        }

                        var rim = visuals.Find("Scoring_Rim");
                        if (rim)
                        {
                            var rimCol = rim.GetComponent<BoxCollider>();
                            if (!rimCol) rimCol = rim.gameObject.AddComponent<BoxCollider>();
                            rimCol.size = new Vector3(0.55f, 0.04f, 0.55f);
                        }
                    }

                    var target = root.transform.Find("Hoop scoring center");
                    if (target)
                    {
                        var triggerCol = target.GetComponent<BoxCollider>();
                        if (!triggerCol) triggerCol = target.gameObject.AddComponent<BoxCollider>();
                        triggerCol.isTrigger = true;
                        triggerCol.center = Vector3.zero;
                        triggerCol.size = new Vector3(0.36f, 0.08f, 0.36f);

                        var trig = target.GetComponent<PaperHoopTrigger>();
                        if (!trig) trig = target.gameObject.AddComponent<PaperHoopTrigger>();
                    }
                }
            }

            // 3. Author Scene objects
            var game = Object.FindAnyObjectByType<GameManager>();
            if (game)
            {
                var deskCol = GameObject.Find("Desk Physics Surface");
                if (!deskCol)
                {
                    deskCol = new GameObject("Desk Physics Surface");
                    Undo.RegisterCreatedObjectUndo(deskCol, "Add desk physics surface");
                    deskCol.transform.SetParent(game.transform, false);
                }
                deskCol.transform.position = new Vector3(0.35f, 0.76f, -0.55f);
                var col = deskCol.GetComponent<BoxCollider>();
                if (!col) col = deskCol.AddComponent<BoxCollider>();
                col.center = Vector3.zero;
                col.size = new Vector3(8.0f, 0.10f, 3.2f);

                var pileAnchorObj = GameObject.Find("Paper pile anchor");
                if (!pileAnchorObj)
                {
                    pileAnchorObj = new GameObject("Paper pile anchor");
                    Undo.RegisterCreatedObjectUndo(pileAnchorObj, "Add paper pile anchor");
                    pileAnchorObj.transform.SetParent(game.transform, false);
                }
                pileAnchorObj.transform.position = new Vector3(0.45f, 0.82f, -0.85f);

                var boundary = GameObject.Find("Paper pile boundary");
                if (boundary) Undo.DestroyObjectImmediate(boundary);

                boundary = new GameObject("Paper pile boundary");
                Undo.RegisterCreatedObjectUndo(boundary, "Add paper pile boundary");
                boundary.transform.SetParent(game.transform, false);
                boundary.transform.position = new Vector3(0.45f, 0.82f, -0.85f);

                void AddWall(string name, Vector3 pos, Vector3 size)
                {
                    var wall = new GameObject(name);
                    wall.transform.SetParent(boundary.transform, false);
                    wall.transform.localPosition = pos;
                    var wc = wall.AddComponent<BoxCollider>();
                    wc.size = size;
                }
                // Enclose the green desk zone between Globe and Pin Coffer: width 0.80m, depth 0.70m
                AddWall("Wall_N", new Vector3(0, 0.06f, 0.35f), new Vector3(0.85f, 0.12f, 0.04f));
                AddWall("Wall_S", new Vector3(0, 0.06f, -0.35f), new Vector3(0.85f, 0.12f, 0.04f));
                AddWall("Wall_W", new Vector3(-0.42f, 0.06f, 0), new Vector3(0.04f, 0.12f, 0.70f));
                AddWall("Wall_E", new Vector3(0.42f, 0.06f, 0), new Vector3(0.04f, 0.12f, 0.70f));

                var paper = game.GetComponent<PaperBallThrower>();
                if (paper)
                {
                    paper.pileAnchor = pileAnchorObj.transform;
                    paper.paperBallPrefab = AssetDatabase.LoadAssetAtPath<PaperBallInstance>(ballPrefabPath);

                    var hoopRoot = GameObject.Find("Paper Basketball Hoop");
                    if (hoopRoot)
                    {
                        var scoringCenter = hoopRoot.transform.Find("Hoop scoring center");
                        if (scoringCenter)
                        {
                            var trigger = scoringCenter.GetComponent<PaperHoopTrigger>();
                            if (!trigger) trigger = scoringCenter.gameObject.AddComponent<PaperHoopTrigger>();
                            trigger.thrower = paper;
                        }
                    }

                    EditorUtility.SetDirty(paper);
                }

                EditorSceneManager.SaveScene(scene);
            }
        }
        static void AuthorWhiskey()
        {
            var scene = SceneManager.GetActiveScene();
            var game = Object.FindAnyObjectByType<GameManager>();
            if (!game || !scene.path.EndsWith("DetectiveOffice.unity")) throw new InvalidOperationException("Open DetectiveOffice first.");

            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            Transform glassModel = null, bottleModel = null, glassInteraction = null, bottleInteraction = null, glassRim = null, bottleCap = null;
            foreach (var t in all)
            {
                if (t.name == "Generated_WhiskeyGlass") glassModel = t;
                else if (t.name == "Generated_WhiskeyBottle") bottleModel = t;
                else if (t.name == "Whiskey glass") glassInteraction = t;
                else if (t.name == "Whiskey bottle") bottleInteraction = t;
                else if (t.name == "Glass rim") glassRim = t;
                else if (t.name == "Bottle cap") bottleCap = t;
            }

            if (glassModel) glassModel.gameObject.SetActive(true);
            if (bottleModel) bottleModel.gameObject.SetActive(true);
            if (glassInteraction) glassInteraction.gameObject.SetActive(true);
            if (bottleInteraction) bottleInteraction.gameObject.SetActive(true);
            if (glassRim) glassRim.gameObject.SetActive(true);
            if (bottleCap) bottleCap.gameObject.SetActive(true);

            var deskInfo = game.DeskInfo ?? game.GetComponentInChildren<DeskInfoSystem>();
            if (deskInfo)
            {
                var root = deskInfo.CreateLayout(game);
                deskInfo.PreviewLayout();
                EditorUtility.SetDirty(deskInfo);
                EditorUtility.SetDirty(root.gameObject);
                for (int i = 0; i < root.childCount; i++) EditorUtility.SetDirty(root.GetChild(i).gameObject);
            }
            EditorSceneManager.SaveScene(scene);
            Capture();
        }
        static void AuthorWallFloorHoop()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.path.EndsWith("DetectiveOffice.unity"))
            {
                EditorSceneManager.OpenScene("Assets/DetectiveDesk/Scenes/DetectiveOffice.unity");
                scene = SceneManager.GetActiveScene();
            }

            var game = Object.FindAnyObjectByType<GameManager>();
            var env = GameObject.Find("Environment");

            // 1. Remove any light child on the basketball hoop (user explicit: "remove the liight at the basket ball hoop")
            var hoop = GameObject.Find("Paper Basketball Hoop");
            if (hoop)
            {
                foreach (var light in hoop.GetComponentsInChildren<Light>(true))
                {
                    Undo.DestroyObjectImmediate(light.gameObject);
                }
            }

            // 2. Move hoop against the wall and halfway to the atlas
            // Atlas bounds: bottom y = 1.15m, center y = 2.80m, top y = 4.45m.
            // Wall surface (Bay_01_Wall) front face is at z = 2.17m.
            // Hoop root at y = 2.26m places the backboard center at y = 2.80m (exactly halfway to the atlas), rim at y = 2.54m.
            // Hoop brackets and backboard back sit at hoop.z + 0.08m, so hoop.z = 2.09m mounts the backplate flush on the wall face!
            if (hoop)
            {
                hoop.transform.position = new Vector3(5.95f, 2.26f, 2.09f);
                var visual = hoop.transform.Find("Visuals");
                var rim = visual.GetComponentsInChildren<Renderer>().First(r => r.name == "Scoring_Rim");
                var board = visual.GetComponentsInChildren<Renderer>().First(r => r.name == "Backboard");

                var scoringCenter = hoop.transform.Find("Hoop scoring center");
                if (scoringCenter) scoringCenter.position = rim.bounds.center;

                var scoreAnchor = hoop.transform.Find("Score anchor");
                if (scoreAnchor) scoreAnchor.position = new Vector3(board.bounds.center.x, board.bounds.max.y + 0.22f, board.bounds.center.z - 0.08f);

                var paper = Object.FindAnyObjectByType<PaperBallThrower>();
                if (paper)
                {
                    paper.basketTarget = scoringCenter;
                    paper.scoreAnchor = scoreAnchor;
                    if (paper.ScoreCanvas && scoreAnchor)
                    {
                        paper.ScoreCanvas.transform.position = scoreAnchor.position;
                        paper.ScoreCanvas.transform.rotation = scoreAnchor.rotation;
                        EditorUtility.SetDirty(paper.ScoreCanvas);
                    }
                    EditorUtility.SetDirty(paper);
                }

                PrefabUtility.ApplyPrefabInstance(hoop, InteractionMode.AutomatedAction);
            }

            // 3. Room lighting: gentle warm ambient fill for the office room
            var amberFillObj = GameObject.Find("Amber fill");
            if (amberFillObj)
            {
                amberFillObj.transform.position = new Vector3(0.0f, 3.8f, -0.5f);
                var light = amberFillObj.GetComponent<Light>();
                if (light)
                {
                    light.range = 16f;
                    light.intensity = 3.2f;
                    EditorUtility.SetDirty(light);
                }
            }
            var rightFillObj = GameObject.Find("Office right fill");
            if (!rightFillObj)
            {
                rightFillObj = new GameObject("Office right fill");
                rightFillObj.transform.SetParent(env ? env.transform : null, false);
                var light = rightFillObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.95f, 0.82f, 0.68f);
                light.range = 14f;
                light.intensity = 2.8f;
                rightFillObj.transform.position = new Vector3(5.2f, 4.0f, 0.8f);
            }
            else
            {
                rightFillObj.transform.position = new Vector3(5.2f, 4.0f, 0.8f);
                var light = rightFillObj.GetComponent<Light>();
                if (light) { light.range = 14f; light.intensity = 2.8f; light.color = new Color(0.95f, 0.82f, 0.68f); EditorUtility.SetDirty(light); }
            }

            // 4. Extend the wall and floor so camera never looks at black open space
            var wallPlasterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/DetectiveDesk/Art/Environment/BackWall/Materials/BackWall_Warm_Umber_Plaster.mat");
            var floorMahoganyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/DetectiveDesk/Art/Environment/FloorBackground/Materials/Floor_MahoganyBorder.mat");

            if (env)
            {
                // Back wall: enable and expand
                var backWall = env.transform.Find("Back wall");
                if (!backWall)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Back wall";
                    go.transform.SetParent(env.transform, false);
                    backWall = go.transform;
                }
                backWall.localPosition = new Vector3(0f, 5.0f, 2.75f);
                backWall.localScale = new Vector3(18f, 16f, 0.4f);
                var bmr = backWall.GetComponent<MeshRenderer>();
                if (bmr)
                {
                    bmr.enabled = true;
                    if (wallPlasterMat) bmr.sharedMaterial = wallPlasterMat;
                }
                var bcol = backWall.GetComponent<Collider>();
                if (bcol) bcol.enabled = false;

                // Floor: enable and expand
                var floor = env.transform.Find("Floor");
                if (!floor)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Floor";
                    go.transform.SetParent(env.transform, false);
                    floor = go.transform;
                }
                floor.localPosition = new Vector3(0f, -0.16f, 0f);
                floor.localScale = new Vector3(20f, 0.2f, 24f);
                var fmr = floor.GetComponent<MeshRenderer>();
                if (fmr)
                {
                    fmr.enabled = true;
                    if (floorMahoganyMat) fmr.sharedMaterial = floorMahoganyMat;
                }
                var fcol = floor.GetComponent<Collider>();
                if (fcol) fcol.enabled = false;

                // Right wall to enclose right side when camera aims right at the hoop
                var rightWall = env.transform.Find("Right wall");
                if (!rightWall)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Right wall";
                    go.transform.SetParent(env.transform, false);
                    rightWall = go.transform;
                }
                rightWall.localPosition = new Vector3(7.2f, 5.0f, 0f);
                rightWall.localScale = new Vector3(0.4f, 16f, 24f);
                var rmr = rightWall.GetComponent<MeshRenderer>();
                if (rmr)
                {
                    rmr.enabled = true;
                    if (wallPlasterMat) rmr.sharedMaterial = wallPlasterMat;
                }
                var rcol = rightWall.GetComponent<Collider>();
                if (rcol) rcol.enabled = false;

                // Left wall
                var leftWall = env.transform.Find("Left wall");
                if (!leftWall)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Left wall";
                    go.transform.SetParent(env.transform, false);
                    leftWall = go.transform;
                }
                leftWall.localPosition = new Vector3(-7.2f, 5.0f, 0f);
                leftWall.localScale = new Vector3(0.4f, 16f, 24f);
                var lmr = leftWall.GetComponent<MeshRenderer>();
                if (lmr)
                {
                    lmr.enabled = true;
                    if (wallPlasterMat) lmr.sharedMaterial = wallPlasterMat;
                }
                var lcol = leftWall.GetComponent<Collider>();
                if (lcol) lcol.enabled = false;

                // Ceiling
                var ceiling = env.transform.Find("Ceiling");
                if (!ceiling)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Ceiling";
                    go.transform.SetParent(env.transform, false);
                    ceiling = go.transform;
                }
                ceiling.localPosition = new Vector3(0f, 10f, 0f);
                ceiling.localScale = new Vector3(18f, 0.4f, 24f);
                var cmr = ceiling.GetComponent<MeshRenderer>();
                if (cmr)
                {
                    cmr.enabled = true;
                    if (wallPlasterMat) cmr.sharedMaterial = wallPlasterMat;
                }
                var ccol = ceiling.GetComponent<Collider>();
                if (ccol) ccol.enabled = false;
            }

            if (game) ScenePresentationAuthoring.EnsureLayout(game);
            EditorSceneManager.SaveScene(scene);
            Capture();
        }
        static void Capture()
        {
            var c=Camera.main;var rt=new RenderTexture(1920,1080,24);var old=c.targetTexture;var active=RenderTexture.active;
            Vector3 originalPos = c.transform.position;
            Quaternion originalRot = c.transform.rotation;
            try
            {
                c.targetTexture=rt;c.Render();RenderTexture.active=rt;
                var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
                File.WriteAllBytes("Logs/paper-hoop-preview.png",tex.EncodeToPNG());Object.DestroyImmediate(tex);

                c.transform.position = new Vector3(3.90f, 2.65f, -4.80f);
                c.transform.rotation = Quaternion.Euler(8.0f, 16.5f, 0f);
                c.Render();RenderTexture.active=rt;
                var texFront=new Texture2D(1920,1080,TextureFormat.RGB24,false);texFront.ReadPixels(new Rect(0,0,1920,1080),0,0);texFront.Apply();
                File.WriteAllBytes("Logs/paper-hoop-front-preview.png",texFront.EncodeToPNG());Object.DestroyImmediate(texFront);

                c.transform.position = new Vector3(3.90f, 2.65f, -4.80f);
                c.transform.rotation = Quaternion.Euler(28.0f, 20.0f, 0f);
                c.Render();RenderTexture.active=rt;
                var texUp=new Texture2D(1920,1080,TextureFormat.RGB24,false);texUp.ReadPixels(new Rect(0,0,1920,1080),0,0);texUp.Apply();
                File.WriteAllBytes("Logs/paper-hoop-aim-up-preview.png",texUp.EncodeToPNG());Object.DestroyImmediate(texUp);
            }
            finally {c.transform.position=originalPos;c.transform.rotation=originalRot;c.targetTexture=old;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);}
        }
    }
}
