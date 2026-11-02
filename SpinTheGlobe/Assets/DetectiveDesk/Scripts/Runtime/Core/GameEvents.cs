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

    /// <summary>The globe completed one or more full revolutions past the start line.</summary>
    public readonly struct GlobeRevolutions
    {
        public readonly long Count;
        public GlobeRevolutions(long count) { Count = count; }
    }

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
        /// <summary>Owned rank of this pin's Durability node (0 = never upgraded).</summary>
        public readonly int DurabilityLevel;
        public PinFellOff(int pinIndex, int durabilityLevel) { PinIndex = pinIndex; DurabilityLevel = durabilityLevel; }
    }

    public readonly struct CaseStamped
    {
        public readonly int Level;
        public readonly double Reward;
        public readonly bool Automatic;
        public CaseStamped(int level, double reward, bool automatic) { Level = level; Reward = reward; Automatic = automatic; }
    }

    public readonly struct WhiskeyBoostChanged
    {
        public readonly bool Active;
        public WhiskeyBoostChanged(bool active) { Active = active; }
    }

    public readonly struct StatueSmashed
    {
        public readonly int Index;
        public readonly double Gold;
        public StatueSmashed(int index, double gold) { Index = index; Gold = gold; }
    }

    public readonly struct LootOpened
    {
        public readonly LootKind Kind;
        public readonly double Amount;
        public readonly string Description;
        public LootOpened(LootKind kind, double amount, string description) { Kind = kind; Amount = amount; Description = description; }
    }
}
