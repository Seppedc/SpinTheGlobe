using DetectiveDesk.Economy;

namespace DetectiveDesk.Core
{
    // Game-wide event payloads published through EventBus.
    // Keep them small, immutable and free of UnityEngine.Object references where possible.

    public readonly struct CurrencyChanged
    {
        public readonly Currency Currency;
        public readonly double Balance;
        public readonly double Delta;
        public CurrencyChanged(Currency currency, double balance, double delta) { Currency = currency; Balance = balance; Delta = delta; }
    }

    public readonly struct SkillPurchased
    {
        public readonly string NodeId;
        public readonly int NewRank;
        public SkillPurchased(string nodeId, int newRank) { NodeId = nodeId; NewRank = newRank; }
    }

    public readonly struct StatsChanged { }

    public readonly struct GameLoaded { }

    public readonly struct PinPaidOut
    {
        public readonly int PinIndex;
        public readonly double Amount;
        public readonly int RemainingStrength;
        public PinPaidOut(int pinIndex, double amount, int remainingStrength) { PinIndex = pinIndex; Amount = amount; RemainingStrength = remainingStrength; }
    }

    public readonly struct PinFellOff
    {
        public readonly int PinIndex;
        public readonly int DurabilityLevel;
        public PinFellOff(int pinIndex, int durabilityLevel) { PinIndex = pinIndex; DurabilityLevel = durabilityLevel; }
    }
}
