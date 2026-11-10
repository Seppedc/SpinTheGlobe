using DetectiveDesk.Gameplay;
using DetectiveDesk.Progression;
using NUnit.Framework;
using UnityEngine;

namespace DetectiveDesk.Tests
{
    public sealed class GlobeDragGestureTests
    {
        GameBalance balance;

        [SetUp]
        public void SetUp() => balance = ScriptableObject.CreateInstance<GameBalance>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(balance);

        [Test]
        public void ReturningToStart_GivesNeitherDragNorClick()
        {
            var gesture = new GlobeDragGesture();
            gesture.Begin(Vector2.zero, 0);
            for (int i = 0; i < 20; i++) gesture.Move(new Vector2(i % 2 == 0 ? 300 : -300, 0));
            Assert.IsTrue(gesture.TryRelease(Vector2.zero, 2, 8, out var release));
            Assert.IsFalse(release.IsClick);
            Assert.AreEqual(0, GameplayMath.DragImpulse(release.HorizontalPixels, release.Seconds, balance, 1, 90));
        }

        [Test]
        public void Backtracking_DoesNotIncreasePowerAboveSameNetDistance()
        {
            var gesture = new GlobeDragGesture();
            gesture.Begin(Vector2.zero, 0);
            for (int i = 0; i < 20; i++) gesture.Move(new Vector2(i % 2 == 0 ? 400 : -400, 0));
            Assert.IsTrue(gesture.TryRelease(new Vector2(100, 0), 1, 8, out var release));
            Assert.AreEqual(100, release.HorizontalPixels);
            Assert.AreEqual(GameplayMath.DragImpulse(100, 1, balance, 1, 90),
                GameplayMath.DragImpulse(release.HorizontalPixels, release.Seconds, balance, 1, 90));
        }

        [Test]
        public void Gesture_CanBeReleasedOnlyOnce_AndCancelledGesturesGiveNothing()
        {
            var gesture = new GlobeDragGesture();
            gesture.Begin(Vector2.zero, 0);
            Assert.IsTrue(gesture.TryRelease(new Vector2(200, 0), .2, 8, out _));
            Assert.IsFalse(gesture.TryRelease(new Vector2(400, 0), .3, 8, out _));
            gesture.Begin(Vector2.zero, 1);
            gesture.Move(new Vector2(200, 0));
            gesture.Cancel();
            Assert.IsFalse(gesture.TryRelease(new Vector2(200, 0), 1.2, 8, out _));
        }

        [Test]
        public void TinyClickJitter_RemainsAClick_ButVerticalDragsDoNotBecomeClicks()
        {
            var gesture = new GlobeDragGesture();
            gesture.Begin(Vector2.zero, 0);
            Assert.IsTrue(gesture.TryRelease(new Vector2(3, 2), .1, 8, out var click));
            Assert.IsTrue(click.IsClick);
            gesture.Begin(Vector2.zero, 1);
            gesture.Move(new Vector2(0, 200));
            Assert.IsTrue(gesture.TryRelease(Vector2.zero, 1.2, 8, out var vertical));
            Assert.IsFalse(vertical.IsClick);
            Assert.AreEqual(0, GameplayMath.DragImpulse(vertical.HorizontalPixels, vertical.Seconds, balance, 1, 90));
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(31)]
        public void SubThresholdFastDrags_GiveNoDragBoost(double pixels)
        {
            Assert.AreEqual(0, GameplayMath.DragImpulse(pixels, .001, balance, 1, 90));
        }

        [Test]
        public void LargerSwipe_GivesMorePower_UntilItsDistanceContributionIsFull()
        {
            double shortSwipe = GameplayMath.DragImpulse(40, .04, balance, 1, 90);
            double fullSwipe = GameplayMath.DragImpulse(200, .2, balance, 1, 90);
            Assert.Greater(fullSwipe, shortSwipe * 2);
            Assert.AreEqual(fullSwipe, GameplayMath.DragImpulse(1000, 1, balance, 1, 90), 1e-9,
                "equally fast overlong gestures cannot add extra distance power");
        }

        [TestCase(90)]
        [TestCase(4635)]
        public void FastOrUpgradedSwipe_CannotExceedTheSingleGestureCap(double maxSpeed)
        {
            double impulse = GameplayMath.DragImpulse(10000, .001, balance, 100, maxSpeed);
            Assert.AreEqual(maxSpeed * balance.dragMaxBoostFraction, impulse, 1e-8);
            Assert.Less(impulse, maxSpeed, "a single swipe from rest cannot instantly hit the speed limit");
        }

        [Test]
        public void HoldingLonger_DoesNotChargeExtraPower()
        {
            Assert.Less(GameplayMath.DragImpulse(200, 10, balance, 1, 90),
                GameplayMath.DragImpulse(200, .2, balance, 1, 90));
        }

        [Test]
        public void InvalidSamplesOrTime_CancelTheGesture()
        {
            var gesture = new GlobeDragGesture();
            gesture.Begin(Vector2.zero, 0);
            gesture.Move(new Vector2(float.NaN, 0));
            Assert.IsFalse(gesture.TryRelease(new Vector2(200, 0), .2, 8, out _));
            gesture.Begin(Vector2.zero, 1);
            Assert.IsFalse(gesture.TryRelease(new Vector2(200, 0), .5, 8, out _));
        }

        [Test]
        public void Globe_AddsNoDragVelocityWhileHeld_AndConsumesReleaseOnce()
        {
            var root = new GameObject("Inactive drag test");
            root.SetActive(false);
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
                game.globe = globe;
                globe.BeginDrag(Vector2.zero, 0);
                globe.TrackDrag(new Vector2(200, 0));
                globe.TrackDrag(new Vector2(-200, 0));
                Assert.AreEqual(0, globe.CurrentVelocity, "holding and moving never adds speed");
                globe.ReleaseDrag(Vector2.zero, 1);
                Assert.AreEqual(0, globe.CurrentVelocity, "back to the start gives no drag or click");
                globe.BeginDrag(Vector2.zero, 2);
                globe.TrackDrag(new Vector2(200, 0));
                Assert.AreEqual(0, globe.CurrentVelocity);
                globe.ReleaseDrag(new Vector2(200, 0), 2.2);
                double releasedVelocity = globe.CurrentVelocity;
                Assert.That(releasedVelocity, Is.GreaterThan(0).And.LessThan(globe.MaximumVelocity));
                globe.ReleaseDrag(new Vector2(400, 0), 2.3);
                Assert.AreEqual(releasedVelocity, globe.CurrentVelocity, "the release cannot be replayed");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(tree);
            }
        }

        [TestCase(960)]
        [TestCase(1920)]
        [TestCase(3840)]
        public void EqualRelativeSwipes_AreConsistentAcrossViewportWidths(int width)
        {
            var root = new GameObject("Inactive scaled swipe test");
            root.SetActive(false);
            var tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            var target = new RenderTexture(width, 1080, 0);
            try
            {
                tree.SetNodes(DefaultSkillTreeFactory.Create());
                var game = root.AddComponent<GameManager>();
                game.balance = balance;
                game.skillTreeAsset = tree;
                game.useSaveFile = false;
                game.PrepareScenePresentation();
                var cameraObject = new GameObject("Swipe camera");
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>();
                camera.targetTexture = target;
                camera.rect = new Rect(0, 0, 1, 1);
                Assert.AreEqual(width, camera.pixelWidth, "the test camera must use the requested viewport width");
                var globe = root.AddComponent<GlobeController>();
                globe.game = game;
                globe.gameCamera = camera;
                game.globe = globe;
                globe.BeginDrag(Vector2.zero, 0);
                globe.ReleaseDrag(new Vector2(100f * width / balance.dragReferenceScreenWidth, 0), .2);
                Assert.AreEqual(GameplayMath.DragImpulse(100, .2, balance, 1, 90), globe.CurrentVelocity, 1e-4);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(tree);
                Object.DestroyImmediate(target);
            }
        }
    }
}
