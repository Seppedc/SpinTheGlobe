using DetectiveDesk.Gameplay;
using DetectiveDesk.Progression;
using DetectiveDesk.Save;
using NUnit.Framework;
using UnityEngine;

namespace DetectiveDesk.Tests
{
    public sealed class FirstUseGuidanceTests
    {
        [Test]
        public void FreshOrUntouchedSave_StillNeedsGlobe()
        {
            var state = new PlayerGuidanceState();
            Assert.IsTrue(state.NeedsGlobe);
            state.Load(new SaveData { completedGuidance = null, playTimeSeconds = 10 });
            Assert.IsTrue(state.NeedsGlobe, "quitting before interacting must not lose the prompt");
        }

        [Test]
        public void CompletedPrompt_IsSavedOnce_AndSurvivesRetirement()
        {
            var state = new PlayerGuidanceState();
            Assert.IsTrue(state.Complete(PlayerGuidanceState.GlobeSpin));
            Assert.IsFalse(state.Complete(PlayerGuidanceState.GlobeSpin));
            var data = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(new SaveData
            {
                completedGuidance = state.ToSave(), prestiges = 1,
            }));
            var restored = new PlayerGuidanceState();
            restored.Load(data);
            Assert.IsFalse(restored.NeedsGlobe);
            CollectionAssert.Contains(restored.ToSave(), PlayerGuidanceState.GlobeSpin);
            Assert.IsFalse(restored.NeedsPinBox, "retired legacy players should not replay the pin introduction");
            Assert.IsFalse(restored.NeedsPinShooter);
        }

        [Test]
        public void ShooterPrompt_WaitsForUnlockAndVisibleBoxOpening()
        {
            var state = new PlayerGuidanceState();
            Assert.IsFalse(state.ShowPinShooter(false));
            Assert.IsFalse(state.ShowPinShooter(true));
            state.Complete(PlayerGuidanceState.PinBoxOpened);
            Assert.IsFalse(state.ShowPinShooter(false));
            Assert.IsTrue(state.ShowPinShooter(true));
            state.Complete(PlayerGuidanceState.PinFire);
            Assert.IsFalse(state.ShowPinShooter(true));
        }

        [Test]
        public void SaveBetweenFirstPinPurchaseAndOpening_KeepsTheIntroductionPending()
        {
            var state = new PlayerGuidanceState();
            state.Complete(PlayerGuidanceState.PinIntroStarted);
            var data = new SaveData { lifetimeMoney = 100, completedGuidance = state.ToSave() };
            data.skills.Add(new SkillRank { id = "pin_01", rank = 1 });
            var restored = new PlayerGuidanceState();
            restored.Load(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data)));
            Assert.IsTrue(restored.NeedsPinBox);
            Assert.IsTrue(restored.NeedsPinShooter);
            Assert.IsFalse(restored.ShowPinShooter(true));
        }

        [Test]
        public void OldPinOwnersSkipPrompts_ButPlayersWithoutPinsStillGetThem()
        {
            var state = new PlayerGuidanceState();
            var data = new SaveData { completedGuidance = null };
            data.skills.Add(new SkillRank { id = "pin_01", rank = 1 });
            state.Load(data);
            Assert.IsFalse(state.NeedsPinBox);
            Assert.IsFalse(state.NeedsPinShooter);
            data.skills.Clear();
            data.skills.Add(new SkillRank { id = "root", rank = 1 });
            state.Load(data);
            Assert.IsTrue(state.NeedsPinBox);
            Assert.IsTrue(state.NeedsPinShooter);
        }

        [Test]
        public void CompletedPinIntroduction_SurvivesSaveAndRetirement()
        {
            var state = new PlayerGuidanceState();
            state.Complete(PlayerGuidanceState.PinIntroStarted);
            state.Complete(PlayerGuidanceState.PinBoxOpened);
            state.Complete(PlayerGuidanceState.PinFire);
            var restored = new PlayerGuidanceState();
            restored.Load(new SaveData { completedGuidance = state.ToSave(), prestiges = 1 });
            Assert.IsFalse(restored.NeedsPinBox);
            Assert.IsFalse(restored.ShowPinShooter(true));
        }

        [Test]
        public void FailedShot_DoesNotDismissShooterPrompt()
        {
            WithGlobe((game, globe) =>
            {
                game.Wallet.Add(DetectiveDesk.Economy.Currency.Money, 100);
                game.PurchaseSkill("root");
                game.PurchaseSkill("pin_01");
                game.Guidance.Complete(PlayerGuidanceState.PinBoxOpened);
                game.map = game.gameObject.AddComponent<WallMapManager>();
                game.map.game = game;
                game.FirePin(); // No prefab/meridian is configured, so the launch cannot happen.
                Assert.IsTrue(game.Guidance.ShowPinShooter(true));
            });
        }

        [Test]
        public void ShooterOverlay_TargetsTheWholeLauncherModel()
        {
            WithGlobe((game, globe) =>
            {
                var model = new GameObject("PinLauncher").transform;
                model.SetParent(game.transform, false);
                var pivot = new GameObject("Recoil_Slide_Pivot").transform;
                pivot.SetParent(model, false);
                game.map = game.gameObject.AddComponent<WallMapManager>();
                game.map.launcherVisual = pivot;
                var guidance = game.gameObject.AddComponent<FirstUseGuidance>();
                guidance.Initialize(game);
                Assert.AreSame(model, guidance.shooterTarget);
            });
        }

        [Test]
        public void LegacyProgress_DoesNotRestartTheIntroduction()
        {
            var state = new PlayerGuidanceState();
            state.Load(JsonUtility.FromJson<SaveData>("{\"lifetimeMoney\":100}"));
            Assert.IsFalse(state.NeedsGlobe);
            state.Load(new SaveData { prestiges = 1, completedGuidance = null });
            Assert.IsFalse(state.NeedsGlobe);
            var data = new SaveData();
            data.skills.Add(new SkillRank { id = "root", rank = 1 });
            state.Load(data);
            Assert.IsFalse(state.NeedsGlobe);
        }

        [TestCase(0, 0)]
        [TestCase(.25, .375)]
        [TestCase(.5, .75)]
        [TestCase(.75, .375)]
        [TestCase(1, 0)]
        public void Pulse_FadesSmoothlyOverAFullCycle(double cycle, double expected)
        {
            Assert.AreEqual(expected, PlayerGuidanceState.PulseOpacity(cycle * 2.8f, 2.8f, .75f), 1e-6);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FirstValidManualInput_CompletesGlobePrompt(bool drag)
        {
            WithGlobe((game, globe) =>
            {
                if (drag)
                {
                    globe.BeginDrag(Vector2.zero, 0);
                    globe.ReleaseDrag(new Vector2(200, 0), .2);
                }
                else globe.AddClick();
                Assert.Greater(globe.CurrentVelocity, 0);
                Assert.IsFalse(game.Guidance.NeedsGlobe);
            });
        }

        [Test]
        public void MotorAndIneffectiveOrCancelledGestures_DoNotCompletePrompt()
        {
            WithGlobe((game, globe) =>
            {
                game.Stats.SetBase(StatId.GlobeAutoSpeed, 30);
                globe.Advance(1);
                Assert.IsTrue(game.Guidance.NeedsGlobe, "motor rotation is not player interaction");
                globe.BeginDrag(Vector2.zero, 0);
                globe.ReleaseDrag(new Vector2(20, 0), .2);
                Assert.IsTrue(game.Guidance.NeedsGlobe, "a drag below minimum distance adds no spin");
                globe.BeginDrag(Vector2.zero, 1);
                globe.TrackDrag(new Vector2(200, 0));
                globe.CancelDrag();
                globe.ReleaseDrag(new Vector2(200, 0), 1.2);
                Assert.IsTrue(game.Guidance.NeedsGlobe);
            });
        }

        static void WithGlobe(System.Action<GameManager, GlobeController> action)
        {
            var root = new GameObject("Guidance input test");
            root.SetActive(false);
            var balance = ScriptableObject.CreateInstance<GameBalance>();
            var tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            try
            {
                tree.SetNodes(DefaultSkillTreeFactory.Create());
                var game = root.AddComponent<GameManager>();
                game.balance = balance;
                game.skillTreeAsset = tree;
                game.useSaveFile = false;
                game.PrepareScenePresentation();
                var globe = root.AddComponent<GlobeController>();
                globe.game = game;
                globe.rotor = root.transform;
                action(game, globe);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(tree);
                Object.DestroyImmediate(balance);
            }
        }
    }
}
