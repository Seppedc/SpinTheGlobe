using System;
using DetectiveDesk.Core;
using UnityEngine;

namespace DetectiveDesk.Economy
{
    /// <summary>
    /// Single source of truth for all currencies. Plain C# so it can be unit tested and saved.
    /// Every change is broadcast as <see cref="CurrencyChanged"/> on the EventBus.
    /// </summary>
    [Serializable]
    public sealed class Wallet
    {
        [SerializeField] double money;
        [SerializeField] double gold;
        [SerializeField] double lifetimeMoney;
        [SerializeField] double lifetimeGold;

        /// <summary>currency, new balance, delta</summary>
        public event Action<Currency, double, double> Changed;

        public double Money => money;
        public double Gold => gold;
        public double LifetimeMoney => lifetimeMoney;
        public double LifetimeGold => lifetimeGold;

        public double Get(Currency currency) => currency == Currency.Gold ? gold : money;

        public bool CanAfford(Currency currency, double amount) => IsValidAmount(amount) && Get(currency) + 1e-9 >= amount;

        public void Add(Currency currency, double amount)
        {
            if (!IsValidAmount(amount) || amount == 0) return;
            if (currency == Currency.Gold) lifetimeGold = Clamp(lifetimeGold + amount);
            else lifetimeMoney = Clamp(lifetimeMoney + amount);
            Set(currency, Clamp(Get(currency) + amount), amount);
        }

        public bool TrySpend(Currency currency, double amount)
        {
            if (!CanAfford(currency, amount)) return false;
            if (amount == 0) return true;
            Set(currency, Math.Max(0, Get(currency) - amount), -amount);
            return true;
        }

        /// <summary>Restore state from a save without counting it as earnings.</summary>
        public void Load(double moneyBalance, double goldBalance, double lifetimeMoneyTotal, double lifetimeGoldTotal)
        {
            money = Sanitize(moneyBalance);
            gold = Sanitize(goldBalance);
            lifetimeMoney = Math.Max(money, Sanitize(lifetimeMoneyTotal));
            lifetimeGold = Math.Max(gold, Sanitize(lifetimeGoldTotal));
            Notify(Currency.Money, 0);
            Notify(Currency.Gold, 0);
        }

        void Set(Currency currency, double value, double delta)
        {
            if (currency == Currency.Gold) gold = value; else money = value;
            Notify(currency, delta);
        }

        void Notify(Currency currency, double delta)
        {
            double balance = Get(currency);
            Changed?.Invoke(currency, balance, delta);
            EventBus.Publish(new CurrencyChanged(currency, balance, delta));
        }

        static bool IsValidAmount(double v) => !double.IsNaN(v) && !double.IsInfinity(v) && v >= 0;
        static double Sanitize(double v) => double.IsNaN(v) || v < 0 ? 0 : Clamp(v);
        static double Clamp(double v) => double.IsInfinity(v) || v > double.MaxValue ? double.MaxValue : v;
    }
}
