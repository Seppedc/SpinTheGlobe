using System;
using DetectiveDesk.Gameplay;
using DetectiveDesk.Progression;
using NUnit.Framework;
using UnityEngine;

namespace DetectiveDesk.Tests
{
    public sealed class GlobeInputTests
    {
        [TestCase(0)]
        [TestCase(5)]
        [TestCase(10)]
        public void LessFriction_RetainsManualSpeed_AndStaysIndependentOfFrameRate(int level)
        {
            var balance = ScriptableObject.CreateInstance<GameBalance>();
            try
            {
                double friction = GameplayMath.SpinFriction(balance.frictionAt60Hz, Math.Pow(.85, level));
                double expected = 90;
                double angle = GameplayMath.Integrate(ref expected, 2, friction, 30, 90);
                double baseline = 90;
                GameplayMath.Integrate(ref baseline, 2, balance.frictionAt60Hz, 30, 90);
                if (level > 0) Assert.Greater(expected, baseline, "less speed is lost above the motor floor");
                Assert.Greater(expected, 30);
                Assert.Less(expected, 90, "friction still exists at maximum level");
                foreach (int fps in new[] { 30, 60, 144 })
                {
                    double velocity = 90, travelled = 0;
                    for (int frame = 0; frame < 2 * fps; frame++)
                        travelled += GameplayMath.Integrate(ref velocity, 1.0 / fps, friction, 30, 90);
                    Assert.AreEqual(expected, velocity, 1e-8);
                    Assert.AreEqual(angle, travelled, 1e-8);
                }
                double stopped = 0;
                Assert.AreEqual(0, GameplayMath.Integrate(ref stopped, 1, friction, 0, 90), "no hidden auto-rotation");
            }
            finally { UnityEngine.Object.DestroyImmediate(balance); }
        }

        [TestCase(15, 0)]
        [TestCase(15, 5)]
        [TestCase(772.5, 0)]
        [TestCase(772.5, 5)]
        public void QuickSwipes_BeatRapidClicks_AtBothEndsOfProgression(double rpm, int level)
        {
            var b = ScriptableObject.CreateInstance<GameBalance>();
            var tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            try
            {
                tree.SetNodes(DefaultSkillTreeFactory.Create());
                var stats = new StatSheet(); b.ApplyTo(stats);
                double drag = stats.Preview(StatId.GlobeDragPower, tree.Find("globe_drag").modifiers, level);
                double click = stats.Preview(StatId.GlobeClickPower, tree.Find("globe_click").modifiers, level);
                double cap = rpm * 6;
                double dragging = Travel(b, cap, drag, true);
                double clicking = Travel(b, cap, click, false);
                Assert.Greater(dragging, clicking * 1.25, "one quick 200px flick/s beats six clicks/s");
                Assert.Less(clicking / 20, cap * .55, "clicks leave headroom for dragging");
                Assert.Greater(clicking, 0, "clicking remains available");
            }
            finally { UnityEngine.Object.DestroyImmediate(tree); UnityEngine.Object.DestroyImmediate(b); }
        }

        [Test]
        public void EqualDistance_SlowDragStaysGentle_AndFastFlickDoesNotInstantlyMax()
        {
            var b = ScriptableObject.CreateInstance<GameBalance>();
            try
            {
                double slow = Gesture(b, 200, 2, 60, out double slowPeak);
                double fast = Gesture(b, 200, .2, 60, out double fastPeak);
                Assert.Greater(fast, slow * 5, "speed matters even when distance is identical");
                Assert.Less(slowPeak, 90 * .2, "a slow drag stays gentle");
                Assert.Less(fastPeak, 90 * .75, "one ordinary quick swipe cannot hit the opening limit");
                Gesture(b, 1000, 5, 60, out double longSlowPeak);
                Assert.Less(longSlowPeak, 90 * .25, "a long slow drag must not wind straight to max");
            }
            finally { UnityEngine.Object.DestroyImmediate(b); }
        }

        [Test]
        public void DragResponse_IsConsistentAcrossFrameRates()
        {
            var b = ScriptableObject.CreateInstance<GameBalance>();
            try
            {
                double baseline = Gesture(b, 200, .2, 144, out _);
                foreach (int fps in new[] { 30, 60 })
                    Assert.AreEqual(baseline, Gesture(b, 200, .2, fps, out _), baseline * .02, "FPS " + fps);
            }
            finally { UnityEngine.Object.DestroyImmediate(b); }
        }

        [Test]
        public void WrongDirection_AndInvalidInput_GiveNoImpulse()
        {
            var b = ScriptableObject.CreateInstance<GameBalance>();
            try
            {
                Assert.AreEqual(0, GameplayMath.DragImpulse(-10, .1, b, 1, 90));
                Assert.AreEqual(0, GameplayMath.DragImpulse(10, 0, b, 1, 90));
                Assert.AreEqual(0, GameplayMath.DragImpulse(double.NaN, .1, b, 1, 90));
                Assert.AreEqual(0, GameplayMath.DragImpulse(10, double.PositiveInfinity, b, 1, 90));
            }
            finally { UnityEngine.Object.DestroyImmediate(b); }
        }

        [TestCase(0)]
        [TestCase(30)]
        [TestCase(600)]
        [TestCase(3000)]
        public void MotorBaseline_DoesNotConsumeTheManualBoost(double floor)
        {
            var b = ScriptableObject.CreateInstance<GameBalance>();
            try
            {
                double boost = b.clickImpulse;
                double reference = boost;
                double referenceAngle = GameplayMath.Integrate(ref reference, .25, b.frictionAt60Hz, 0, 10000);
                double velocity = floor + boost;
                double angle = GameplayMath.Integrate(ref velocity, .25, b.frictionAt60Hz, floor, 10000);
                Assert.AreEqual(reference, velocity - floor, 1e-9, "motor speed must not erase click momentum");
                Assert.Greater(velocity - floor, boost * .8, "the click should still be noticeable after a quarter second");
                Assert.AreEqual(referenceAngle, angle - floor * .25, 1e-9, "payout travel must include baseline plus boost");
            }
            finally { UnityEngine.Object.DestroyImmediate(b); }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void MotorAndBoostTravel_AreIndependentOfFrameRate(int fps)
        {
            var b = ScriptableObject.CreateInstance<GameBalance>();
            try
            {
                double wholeVelocity = 690;
                double wholeAngle = GameplayMath.Integrate(ref wholeVelocity, 2, b.frictionAt60Hz, 600, 1000);
                double frameVelocity = 690, frameAngle = 0;
                for (int frame = 0; frame < fps * 2; frame++)
                    frameAngle += GameplayMath.Integrate(ref frameVelocity, 1.0 / fps, b.frictionAt60Hz, 600, 1000);
                Assert.AreEqual(wholeVelocity, frameVelocity, 1e-8);
                Assert.AreEqual(wholeAngle, frameAngle, 1e-8, "frame skips must not change earned revolutions");
            }
            finally { UnityEngine.Object.DestroyImmediate(b); }
        }

        [Test]
        public void MotorFloor_StaysSteady_AndRespectsTheSpeedCap()
        {
            double velocity = 0;
            double angle = GameplayMath.Integrate(ref velocity, 2, .9875, 30, 90);
            Assert.AreEqual(30, velocity);
            Assert.AreEqual(60, angle);
            velocity = 1000;
            GameplayMath.Integrate(ref velocity, .1, .9875, 30, 90);
            Assert.That(velocity, Is.InRange(30, 90));
            velocity = 0;
            angle = GameplayMath.Integrate(ref velocity, 1, .9875, 120, 90);
            Assert.AreEqual(90, velocity, "a motor above the cap is clamped to the cap");
            Assert.AreEqual(90, angle);
        }

        [Test]
        public void UnpoweredGlobe_StillSlowsToRest()
        {
            double velocity = 60;
            double angle = GameplayMath.Integrate(ref velocity, 100, .9875, 0, 90);
            Assert.AreEqual(0, velocity);
            Assert.Greater(angle, 0);
            Assert.Less(angle, 100, "no hidden auto-rotation when the motor is off");
        }

        static double Gesture(GameBalance b, double pixels, double seconds, int fps, out double peak)
        {
            var gesture = new GlobeDragGesture();
            gesture.Begin(Vector2.zero, 0);
            double elapsed = 0;
            while (elapsed < seconds - 1e-9)
            {
                double dt = Math.Min(1.0 / fps, seconds - elapsed);
                elapsed += dt;
                gesture.Move(new Vector2((float)(pixels * elapsed / seconds), 0));
            }
            Assert.IsTrue(gesture.TryRelease(new Vector2((float)pixels, 0), seconds, 8, out var release));
            peak = GameplayMath.DragImpulse(release.HorizontalPixels, release.Seconds, b, 1, 90);
            return peak;
        }

        static double Travel(GameBalance b, double cap, double power, bool drag)
        {
            double velocity = 0, degrees = 0;
            for (int frame = 0; frame < 20 * 60; frame++)
            {
                double impulse = drag
                    ? (frame % 60 == 12 ? GameplayMath.DragImpulse(200, .2, b, power, cap) : 0)
                    : (frame % 10 == 0 ? GameplayMath.SpinImpulse(b.clickImpulse, power, cap, b.impulseReferenceSpeed) : 0);
                velocity = Math.Min(cap, velocity + impulse);
                degrees += GameplayMath.Integrate(ref velocity, 1.0 / 60, b.frictionAt60Hz, 0, cap);
            }
            return degrees;
        }
    }
}
