using DetectiveDesk.Gameplay;
using DetectiveDesk.Save;
using DetectiveDesk.Progression;
using DetectiveDesk.Economy;
using NUnit.Framework;
using UnityEngine;

namespace DetectiveDesk.Tests
{
    public sealed class PaperBallThrowerTests
    {
        GameObject root;
        GameManager game;
        GameBalance balance;
        SkillTreeAsset tree;
        PaperBallThrower thrower;
        [SetUp] public void SetUp()
        {
            root = new GameObject("Paper test"); root.SetActive(false);
            game = root.AddComponent<GameManager>();
            balance = ScriptableObject.CreateInstance<GameBalance>();
            tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            tree.SetNodes(DefaultSkillTreeFactory.Create());
            game.balance = balance; game.skillTreeAsset = tree; game.useSaveFile = false;
            game.PrepareScenePresentation();
            thrower = root.AddComponent<PaperBallThrower>();
            Transform Make(string name, Vector3 position) { var t = new GameObject(name).transform; t.SetParent(root.transform); t.position = position; return t; }
            thrower.throwOrigin = Make("Origin", Vector3.zero);
            thrower.basketTarget = Make("Target", Vector3.right * 5);
            thrower.paperVisual = Make("Paper", Vector3.zero);
            thrower.Initialize(game);
            thrower.CreateLayout(game).transform.SetParent(root.transform, false);
            typeof(GameManager).GetProperty("PaperBalls").SetValue(game, thrower);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(root); Object.DestroyImmediate(balance); Object.DestroyImmediate(tree); }
        [Test] public void SuccessfulThrowConsumesOnceAndScoresWithoutCurrency()
        {
            game.Inventory.AddPaperBall(2);
            Assert.IsTrue(thrower.BeginAim());
            Assert.IsTrue(thrower.TryThrowAt(thrower.basketTarget.position));
            Assert.IsFalse(thrower.BeginAim());
            Assert.AreEqual(1, game.Inventory.PaperBalls);
            Assert.AreEqual(0, game.Wallet.Get(Currency.Money));
            thrower.AdvanceFlight(balance.paperBallFlightSeconds / 2);
            Assert.Greater(thrower.paperVisual.position.y, 0);
            thrower.AdvanceFlight(balance.paperBallFlightSeconds);
            Assert.IsFalse(thrower.IsThrowing);
            Assert.IsFalse(thrower.paperVisual.gameObject.activeSelf);
            Assert.Less(thrower.paperVisual.position.y, thrower.basketTarget.position.y);
            Assert.AreEqual(0, game.Wallet.Get(Currency.Money));
            Assert.AreEqual(1, thrower.ScoredShots);
            Assert.IsTrue(thrower.BeginAim());
            Assert.IsTrue(thrower.TryThrowAt(thrower.basketTarget.position));
            Assert.AreEqual(0, game.Inventory.PaperBalls);
        }
        [Test] public void EmptyInventoryOrMissingBasketCannotConsumeOrPay()
        {
            Assert.IsFalse(thrower.BeginAim());
            game.Inventory.AddPaperBall();
            thrower.basketTarget = null;
            Assert.IsFalse(thrower.BeginAim());
            Assert.AreEqual(1, game.Inventory.PaperBalls);
            Assert.AreEqual(0, game.Wallet.Get(Currency.Money));
        }
        [Test] public void CancelPreservesBallAndMissConsumesWithoutReward()
        {
            game.Inventory.AddPaperBall();
            Assert.IsTrue(thrower.BeginAim());
            Assert.IsTrue(game.WorldInputBlocked);
            Assert.AreEqual(1, game.Inventory.PaperBalls);
            thrower.CancelAim();
            Assert.IsFalse(game.WorldInputBlocked);
            Assert.AreEqual(1, game.Inventory.PaperBalls);
            Assert.IsTrue(thrower.BeginAim());
            Assert.IsTrue(thrower.TryThrowAt(thrower.basketTarget.position + Vector3.right));
            Assert.AreEqual(0, game.Inventory.PaperBalls);
            thrower.AdvanceFlight(10);
            Assert.IsFalse(thrower.IsThrowing);
            Assert.AreEqual(0, thrower.ScoredShots);
            Assert.AreEqual(0, game.Wallet.Get(Currency.Money));
        }
        [Test] public void InvalidAimAndRepeatedFlightUpdatesCannotConsumeOrPayAgain()
        {
            game.Inventory.AddPaperBall();
            Assert.IsTrue(thrower.BeginAim());
            Assert.IsFalse(thrower.TryThrowAt(Vector3.right * 1000));
            Assert.IsFalse(thrower.TryThrowAt(new Vector3(float.NaN, 0, 0)));
            Assert.AreEqual(1, game.Inventory.PaperBalls);
            Assert.IsTrue(thrower.TryThrowAt(thrower.basketTarget.position));
            Assert.IsFalse(thrower.TryThrowAt(thrower.basketTarget.position));
            thrower.AdvanceFlight(10);
            thrower.AdvanceFlight(10);
            Assert.AreEqual(0, game.Wallet.Get(Currency.Money));
            Assert.AreEqual(0, game.Wallet.Get(Currency.Gold));
            Assert.AreEqual(1, thrower.ScoredShots);
        }
        [Test] public void LifetimeScoreSurvivesSaveAndInitialization()
        {
            thrower.LoadState(new SaveData { paperShotsScored = 42 });
            thrower.Initialize(game);
            Assert.AreEqual(42, thrower.ScoredShots);
            game.Inventory.AddPaperBall();
            Assert.IsTrue(thrower.BeginAim());
            Assert.IsTrue(thrower.TryThrowAt(thrower.basketTarget.position));
            thrower.AdvanceFlight(10);
            var data = new SaveData();
            thrower.SaveState(data);
            var loaded = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            thrower.LoadState(loaded);
            Assert.AreEqual(43, thrower.ScoredShots);
            thrower.LoadState(new SaveData { paperShotsScored = -1 });
            Assert.AreEqual(0, thrower.ScoredShots);
        }

        [Test] public void OutsideRimDoesNotScoreAndLegacySaveStartsAtZero()
        {
            thrower.LoadState(JsonUtility.FromJson<SaveData>("{\"paperBalls\":5}"));
            Assert.AreEqual(0, thrower.ScoredShots);
            game.Inventory.AddPaperBall();
            Assert.IsTrue(thrower.BeginAim());
            Assert.IsTrue(thrower.TryThrowAt(thrower.basketTarget.position + Vector3.right * (balance.paperBallBasketRadius + .01f)));
            thrower.AdvanceFlight(10);
            Assert.AreEqual(0, thrower.ScoredShots);
        }

        [Test] public void SavedHoopCameraAimScoresWithoutCurrency()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/DetectiveDesk/Scenes/DetectiveOffice.unity");
            try
            {
                GameManager owner = null;
                Camera camera = null;
                foreach (var sceneRoot in scene.GetRootGameObjects())
                {
                    if (!owner) owner = sceneRoot.GetComponentInChildren<GameManager>(true);
                    if (!camera) camera = sceneRoot.GetComponentInChildren<Camera>(true);
                }
                Assert.IsNotNull(owner);
                Assert.IsNotNull(camera);
                owner.useSaveFile = false;
                owner.PrepareScenePresentation();
                var paper = owner.GetComponent<PaperBallThrower>();
                typeof(GameManager).GetProperty("PaperBalls").SetValue(owner, paper);
                Assert.IsNotNull(paper.basketTarget);
                Assert.IsNotNull(paper.scoreAnchor);
                paper.Initialize(owner);
                paper.LoadState(new SaveData { paperShotsScored = 5 });
                owner.Inventory.AddPaperBall();
                var point = camera.WorldToScreenPoint(paper.basketTarget.position);
                var ray = camera.ScreenPointToRay(point);
                Assert.IsTrue(new Plane(Vector3.up, paper.basketTarget.position).Raycast(ray, out float distance));
                Assert.IsTrue(paper.BeginAim());
                Assert.IsTrue(paper.TryThrowAt(ray.GetPoint(distance)));
                Assert.IsTrue(owner.WorldInputBlocked, "A throw must not click objects behind the aim point.");
                paper.AdvanceFlight(10);
                Assert.AreEqual(6, paper.ScoredShots);
                Assert.AreEqual(0, owner.Wallet.Money);
                Assert.AreEqual(0, owner.Wallet.Gold);
                StringAssert.Contains("6", paper.ScoreCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>().text);
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void PhysicsPileAndFlickThrowLaunchConsumesInventory()
        {
            var ballObj = new GameObject("TestBall");
            var rb = ballObj.AddComponent<Rigidbody>();
            var col = ballObj.AddComponent<SphereCollider>();
            var instance = ballObj.AddComponent<PaperBallInstance>();
            thrower.paperBallPrefab = instance;

            game.Inventory.AddPaperBall(3);
            thrower.SyncPileWithInventory();
            Assert.AreEqual(3, thrower.ActivePile.Count);

            var grabbedBall = thrower.ActivePile[0];
            grabbedBall.BeginGrab(Camera.main);
            Assert.IsTrue(grabbedBall.IsGrabbed);

            // Flick upward
            grabbedBall.Throw(new Vector2(200f, 800f));
            Assert.IsTrue(grabbedBall.IsThrown);
            Assert.Greater(grabbedBall.Velocity.magnitude, 2.0f);
            Assert.AreEqual(2, game.Inventory.PaperBalls);

            // Test scoring trigger
            var hoopObj = new GameObject("HoopTrigger");
            var triggerCol = hoopObj.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            var trigger = hoopObj.AddComponent<PaperHoopTrigger>();
            trigger.thrower = thrower;

            long prevScore = thrower.ScoredShots;
            thrower.OnHoopScored(grabbedBall);
            Assert.AreEqual(prevScore + 1, thrower.ScoredShots);

            Object.DestroyImmediate(ballObj);
            Object.DestroyImmediate(hoopObj);
        }

        [Test]
        public void HoopShootingModeTransitionsCameraAndReturnsToDesk()
        {
            var camObj = new GameObject("TestCam");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 2.38f, -5.8f);
            thrower.ActiveCamera = cam;

            var ballObj = new GameObject("TestBallPrefab");
            ballObj.AddComponent<Rigidbody>();
            ballObj.AddComponent<SphereCollider>();
            var ballInstance = ballObj.AddComponent<PaperBallInstance>();
            thrower.paperBallPrefab = ballInstance;

            game.Inventory.AddPaperBall(2);
            thrower.EnterHoopShootingMode();

            Assert.AreEqual(PaperBallThrower.HoopMode.TransitioningToHoop, thrower.CurrentMode);
            Assert.IsTrue(thrower.IsAiming);
            Assert.IsTrue(game.WorldInputBlocked);

            // Advance through transition
            thrower.AdvanceCameraTransition(0.5f);

            Assert.AreEqual(PaperBallThrower.HoopMode.InHoopMode, thrower.CurrentMode);
            Assert.AreEqual(3.90f, cam.transform.position.x, 0.05f);

            // Return to desk
            thrower.ReturnToDesk();
            Assert.AreEqual(PaperBallThrower.HoopMode.TransitioningToDesk, thrower.CurrentMode);

            thrower.AdvanceCameraTransition(0.5f);

            Assert.AreEqual(PaperBallThrower.HoopMode.Desk, thrower.CurrentMode);
            Assert.AreEqual(0f, cam.transform.position.x, 0.05f);
            Assert.IsFalse(thrower.IsAiming);

            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void SoftRimAssistNudgesCloseShotTowardCenter()
        {
            var hoopObj = new GameObject("HoopTrigger");
            hoopObj.transform.position = new Vector3(5.70f, 1.28f, 1.27f);
            var triggerCol = hoopObj.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(0.5f, 0.2f, 0.5f);
            var trigger = hoopObj.AddComponent<PaperHoopTrigger>();
            trigger.thrower = thrower;

            var ballObj = new GameObject("NearMissBall");
            ballObj.transform.position = new Vector3(5.85f, 1.35f, 1.27f); // 0.15m to the right
            var rb = ballObj.AddComponent<Rigidbody>();
            var ballCol = ballObj.AddComponent<SphereCollider>();
            var ball = ballObj.AddComponent<PaperBallInstance>();
            ball.Launch(new Vector3(0f, -0.5f, 0f), Vector3.zero);

            // Apply soft rim assist directly
            bool assisted = trigger.ApplySoftRimAssist(ballCol);
            Assert.IsTrue(assisted);
            Assert.IsTrue(ball.IsThrown);

            Object.DestroyImmediate(hoopObj);
            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void DraggingDownDecreasesArcHeightAndDraggingUpIncreasesIt()
        {
            var camObj = new GameObject("TestCam");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.transform.position = new Vector3(3.90f, 2.65f, -4.80f);
            thrower.ActiveCamera = cam;

            var ballObj = new GameObject("TestBallPrefab");
            ballObj.AddComponent<Rigidbody>();
            ballObj.AddComponent<SphereCollider>();
            var ballInstance = ballObj.AddComponent<PaperBallInstance>();
            thrower.paperBallPrefab = ballInstance;

            game.Inventory.AddPaperBall(2);
            thrower.EnterHoopShootingMode();
            thrower.AdvanceCameraTransition(0.5f);

            var startMouseField = typeof(PaperBallThrower).GetField("dragStartMouse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isDraggingField = typeof(PaperBallThrower).GetField("isDragging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var velField = typeof(PaperBallThrower).GetField("lastComputedVelocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            isDraggingField.SetValue(thrower, true);
            startMouseField.SetValue(thrower, (Vector2)Input.mousePosition);

            // Neutral drag
            thrower.UpdateDragAim();
            Vector3 neutralVel = (Vector3)velField.GetValue(thrower);

            // Drag down: startMouse is higher than current mouse
            startMouseField.SetValue(thrower, (Vector2)Input.mousePosition + new Vector2(0f, 200f));
            thrower.UpdateDragAim();
            Vector3 downVel = (Vector3)velField.GetValue(thrower);

            // Drag up: startMouse is lower than current mouse
            startMouseField.SetValue(thrower, (Vector2)Input.mousePosition - new Vector2(0f, 200f));
            thrower.UpdateDragAim();
            Vector3 upVel = (Vector3)velField.GetValue(thrower);

            Assert.Less(downVel.y, neutralVel.y, "Dragging down must lower the vertical launch velocity (smaller arc).");
            Assert.Less(downVel.z, neutralVel.z, "Dragging down must shorten the forward launch velocity.");
            Assert.Greater(upVel.y, neutralVel.y, "Dragging up must heighten the vertical launch velocity (taller arc).");
            Assert.Greater(upVel.z, neutralVel.z, "Dragging up must lengthen the forward launch velocity.");

            Object.DestroyImmediate(camObj);
            Object.DestroyImmediate(ballObj);
        }
    }
}
