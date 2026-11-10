using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using NUnit.Framework;

namespace DetectiveDesk.Tests
{
    public sealed class EconomyTests
    {
        [SetUp] public void SetUp() => EventBus.Clear();

        [Test]
        public void Wallet_AddAndSpend_TracksBalanceAndLifetime()
        {
            var w = new Wallet();
            w.Add(Currency.Money, 100);
            Assert.IsTrue(w.TrySpend(Currency.Money, 40));
            Assert.AreEqual(60, w.Money, 1e-9);
            Assert.AreEqual(100, w.LifetimeMoney, 1e-9);
            Assert.AreEqual(0, w.Gold, 1e-9);
        }

        [Test]
        public void Wallet_RejectsOverspendAndInvalidAmounts()
        {
            var w = new Wallet();
            w.Add(Currency.Gold, 5);
            Assert.IsFalse(w.TrySpend(Currency.Gold, 6));
            Assert.IsFalse(w.TrySpend(Currency.Gold, double.NaN));
            w.Add(Currency.Gold, -10);
            w.Add(Currency.Gold, double.PositiveInfinity);
            Assert.AreEqual(5, w.Gold, 1e-9);
        }

        [Test]
        public void Wallet_PublishesCurrencyChanged()
        {
            var w = new Wallet();
            double seen = -1;
            System.Action<CurrencyChanged> h = e => { if (e.Currency == Currency.Money) seen = e.Balance; };
            EventBus.Subscribe(h);
            w.Add(Currency.Money, 12.5);
            EventBus.Unsubscribe(h);
            Assert.AreEqual(12.5, seen, 1e-9);
            Assert.AreEqual(0, EventBus.ListenerCount<CurrencyChanged>());
        }

        [TestCase(0, "0")]
        [TestCase(12.5, "12.5")]
        [TestCase(999, "999")]
        [TestCase(1000, "1.00K")]
        [TestCase(1234567, "1.23M")]
        [TestCase(999999, "999K")]
        [TestCase(4.56e10, "45.6B")]
        [TestCase(-2500, "-2.50K")]
        public void NumberFormatter_Formats(double value, string expected) =>
            Assert.AreEqual(expected, NumberFormatter.Format(value));

        [Test]
        public void NumberFormatter_UsesLetterSuffixesAfterNamedOnes()
        {
            Assert.AreEqual("aa", NumberFormatter.Suffix(12));
            Assert.AreEqual("ab", NumberFormatter.Suffix(13));
            StringAssert.EndsWith("aa", NumberFormatter.Format(1e36));
        }

        [Test]
        public void CostCurve_GrowsExponentiallyAndHonoursExplicitCosts()
        {
            var c = new CostCurve(10, 2);
            Assert.AreEqual(10, c.Cost(0));
            Assert.AreEqual(40, c.Cost(2));
            c.explicitCosts = new double[] { 1, 3 };
            Assert.AreEqual(1, c.Cost(0));
            Assert.AreEqual(3, c.Cost(1));
            Assert.AreEqual(40, c.Cost(2));
        }

        [TestCase(11, 10)]
        [TestCase(12, 10)]
        [TestCase(13, 15)]
        [TestCase(14, 15)]
        [TestCase(26, 25)]
        [TestCase(27, 25)]
        [TestCase(28, 30)]
        [TestCase(89, 90)]
        [TestCase(1, 5)]
        [TestCase(0, 0)]
        public void CostCurve_RoundsFinalPriceToNearestFive(double raw, double expected)
        {
            var curve = new CostCurve(raw, 1) { roundingStep = 5 };
            Assert.AreEqual(expected, curve.Cost(0));
            curve.explicitCosts = new[] { raw };
            Assert.AreEqual(expected, curve.Cost(0), "hand-tuned prices follow the same rule");
        }
    }
}
