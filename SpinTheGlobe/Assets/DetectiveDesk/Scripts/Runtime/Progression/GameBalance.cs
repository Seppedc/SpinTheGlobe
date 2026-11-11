using System;
using System.Collections.Generic;
using DetectiveDesk.Economy;
using UnityEngine;

namespace DetectiveDesk.Progression
{
    /// <summary>
    /// Every tuning number of the game: base stat values (before skill-tree upgrades) plus the
    /// rules of each system. All balancing happens in this asset and in the SkillTreeAsset, never in code.
    /// Create the asset via Tools ▸ Spin The Globe ▸ Create Default Data; without it the defaults below are used.
    /// </summary>
    [CreateAssetMenu(menuName = "Spin The Globe/Game Balance", fileName = "GameBalance")]
    public sealed class GameBalance : ScriptableObject
    {
        [Serializable]
        public struct StatBase
        {
            public StatId stat;
            public double value;
            public StatBase(StatId stat, double value) { this.stat = stat; this.value = value; }
        }

        [Serializable]
        public struct LootEntry
        {
            public LootKind kind;
            [Min(0)] public float weight;
            public LootEntry(LootKind kind, float weight) { this.kind = kind; this.weight = weight; }
        }

        /// <summary>A detective rank, earned by retiring the agency. Add more in the inspector; they are sorted by retirements.</summary>
        [Serializable]
        public struct DetectiveRank
        {
            public string title;
            [Tooltip("Retirements needed to reach this rank.")]
            [Min(0)] public int retirements;
            [Tooltip("Permanent bonus on every source of income while at this rank (0.25 = +25%). Not cumulative: the rank's own value.")]
            [HideInInspector] public float incomeBonus; // Legacy serialized field; prestige count now determines income.
            public DetectiveRank(string title, int retirements, float incomeBonus) { this.title = title; this.retirements = retirements; this.incomeBonus = incomeBonus; }
        }

        [Header("Start")]
        [Min(0)] public double startingMoney = 0;
        [Min(0)] public double startingGold = 0;
        [Tooltip("One-time permanent Gold purchase for the glass, bottle and drink skill branch.")]
        [Min(1)] public double whiskeyUnlockGoldCost = 5;
        [Tooltip("Seconds between automatic saves.")]
        [Min(5)] public float autosaveInterval = 30;

        [Header("Prestige (Retire the agency)")]
        [Tooltip("Money that must be earned in a run before retiring is possible.")]
        [Min(1)] public double prestigeThreshold = 15_000;
        [Tooltip("Gold for retiring = floor(scale × (earned this run ÷ threshold)^exponent).")]
        [Min(0)] public double prestigeGoldScale = 25;
        [Range(0.1f, 1f)] public double prestigeExponent = 0.5;

        [Tooltip("Permanent additive money-income bonus for each completed retirement.")]
        [Min(0)] public double prestigeIncomeBonusPerRetirement = 0.10;

        [Min(0)] public double prestigeRequirementIncrease = 5_000;
        [Min(0)] public int maxPrestiges = 5;
        [Min(1)] public double finalRunGoal = 50_000;

        [Header("Detective ranks (earned by retiring; add more any time)")]
        public List<DetectiveRank> ranks = new List<DetectiveRank>
        {
            new DetectiveRank("Rookie", 0, 0f),
            new DetectiveRank("Detective", 1, 0f),
            new DetectiveRank("Inspector", 2, 0f),
            new DetectiveRank("Chief Inspector", 3, 0f),
            new DetectiveRank("Commissioner", 4, 0f),
            new DetectiveRank("Legend of the Bureau", 5, 0f),
        };

        [Header("Base stats (before upgrades)")]
        public List<StatBase> baseStats = new List<StatBase>(Defaults());

        [Header("Pin Strength appearance (level 0 through 20)")]
        public Color[] pinStrengthColors = DefaultPinStrengthColors();
        [Range(0, 1)] public float pinEnamelMetallic = .15f;
        [Range(0, 1)] public float pinMaxStrengthMetallic = .85f;

        public static Color[] DefaultPinStrengthColors() => new[]
        {
            new Color(.48f, .53f, .58f), // 0: slate
            new Color(.35f, .49f, .69f),
            new Color(.24f, .43f, .82f),
            new Color(.16f, .34f, .92f),
            new Color(.10f, .51f, .87f),
            new Color(.08f, .66f, .78f),
            new Color(.08f, .76f, .65f),
            new Color(.10f, .68f, .46f),
            new Color(.18f, .59f, .29f),
            new Color(.40f, .69f, .23f),
            new Color(.62f, .76f, .24f),
            new Color(.70f, .49f, .82f),
            new Color(.58f, .31f, .83f),
            new Color(.76f, .24f, .68f),
            new Color(.87f, .23f, .46f),
            new Color(.85f, .16f, .22f),
            new Color(.92f, .30f, .16f),
            new Color(.91f, .43f, .20f),
            new Color(.76f, .38f, .23f),
            new Color(.87f, .58f, .38f),
            new Color(1f, .76f, .24f), // 20: gold
        };

        [Header("Player guidance")]
        [Tooltip("Material for first-use white surface prompts.")]
        public Material guidanceOverlayMaterial;
        [Min(1)] public float guidancePulseSeconds = 2.8f;
        [Range(0, 1)] public float guidanceOverlayOpacity = .75f;
        [Header("Globe")]
        [Range(0.5f, 1)] public float frictionAt60Hz = 0.9875f;
        [Tooltip("°/s gained per pixel dragged (× Drag Power).")]
        [Min(0)] public float dragAcceleration = 0.25f;
        [Tooltip("Pointer speed in pixels/second for a full-strength drag. Slower drags give a smaller push.")]
        [Min(1)] public float dragReferencePixelsPerSecond = 900f;
        [Tooltip("Maximum multiplier from a very fast flick.")]
        [Min(1)] public float dragMaxSpeedFactor = 1.5f;
        [Tooltip("Width used to normalize drag distance across window sizes. Distances below use these reference pixels.")]
        [Min(1)] public float dragReferenceScreenWidth = 1920f;
        [Tooltip("Minimum net forward distance for a drag boost. Smaller movements give no drag boost.")]
        [Min(0)] public float dragMinPixels = 32f;
        [Tooltip("Net forward distance that uses the full distance contribution. Longer drags cannot add more distance power.")]
        [Min(1)] public float dragFullPixels = 200f;
        [Tooltip("Maximum speed added by one released swipe, as a share of the current speed limit.")]
        [Range(0, 1)] public float dragMaxBoostFraction = 0.5f;
        [Tooltip("Drag and click impulses scale with max speed ÷ this, so spinning by hand stays useful as the globe gets faster.")]
        [Min(1)] public float impulseReferenceSpeed = 135f;
        [Tooltip("Payouts per revolution. 1 = only when the globe crosses the red payout line (the rule). Higher values pay in steps between crossings.")]
        [Min(1)] public int globeTicksPerRevolution = 1;
        [Tooltip("°/s gained per click on the globe (× Click Power). Smaller than a good drag.")]
        [Min(0)] public float clickImpulse = 3f;
        [Tooltip("Money for each full revolution past the start line (× Globe Multiplier).")]
        [Min(0)] public double revolutionReward = 1;

        [Header("Pins")]
        [Tooltip("Money each time a pin passes the payout line, before landing bonuses and upgrades.")]
        [Min(0)] public double pinBasePayout = 0.5;
        [Min(0.5f)] public float cityRadiusDegrees = 4f;
        [Min(0.05f)] public float pinFlightSeconds = 0.32f;

        [Header("Case file")]
        [Min(1)] public int caseBaseEvidence = 3;
        [Tooltip("Consecutive cases sharing the same evidence count.")]
        [Min(1)] public int caseRepeatCount = 5;
        [Tooltip("Extra evidence after each repeated group. There is no evidence-count cap.")]
        [Min(0)] public int caseEvidencePerGroup = 1;
        [Tooltip("Cases in the first Strength tier. Later tiers last 2x, 3x, 4x... this many cases.")]
        [Min(1)] public int caseBaseTierLength = 15;
        [Min(0)] public int caseMaxDurabilityRequirement = 5;
        [Tooltip("First case's minimum reward, before income and reward upgrades.")]
        [Min(0)] public double caseBaseReward = 5;
        [Tooltip("Small linear increase per solved case, as a fraction of the first case's base reward.")]
        [Min(0)] public double caseRewardGrowthPerLevel = 0.02;
        [Tooltip("A case pays at least this many seconds of your recent income (globe + pins). Keeps cases relevant all game without exploding.")]
        [Min(0)] public float caseIncomeSeconds = 1.5f;
        [Tooltip("Gold per approved case is CaseGold × (1 + case number / this).")]
        [Min(1)] public float caseGoldLevelDivisor = 10;
        [Tooltip("Highest chance an approved case drops gold (Press Coverage and its upgrades never go above this).")]
        [Range(0, 1)] public float caseGoldChanceCap = 0.10f;

        [Header("Whiskey glass")]
        [Range(0.01f, 1)] public float whiskeyFillPerClick = 0.1f;
        [Tooltip("Fill speed multiplier while an ice cube is in the glass.")]
        [Min(1)] public float iceCubeFillMultiplier = 2f;

        [Header("Gold shop")]
        public List<GoldShopItem> goldShopItems = GoldShopCatalog.CreateDefaults();

        [Header("Statues")]
        [Min(1)] public int statueBasePieces = 7;
        [Tooltip("Extra pieces needed for each further pedestal.")]
        [Min(0)] public int statuePiecesPerIndex = 0;
        [Tooltip("Gold fill (0..1) added per globe revolution (× Gold Fill Rate).")]
        [Range(0.001f, 1)] public float statueGoldFillPerRevolution = 0.04f;
        [Min(0)] public double statueBaseGold = 1;

        [Header("Lootbox")]
        [Min(0.1f)] public float lootboxOpeningSeconds = 4.5f;
        public CostCurve lootboxCost = new CostCurve(50, 1, 5);
        public List<LootEntry> lootTable = new List<LootEntry>
        {
            new LootEntry(LootKind.PaperBall, 20),
            new LootEntry(LootKind.IceCube, 20),
            new LootEntry(LootKind.StatuePiece, 4),
            new LootEntry(LootKind.StatuePiece2, 4),
            new LootEntry(LootKind.StatuePiece3, 4),
            new LootEntry(LootKind.Gold, 1),
            new LootEntry(LootKind.Money, 47),
        };
        [Min(0)] public double lootGoldMin = 1;
        [Min(0)] public double lootGoldMax = 1;
        [Tooltip("Chance a money drop is a jackpot (2–4× the price) instead of a partial refund (30–80%).")]
        [Range(0, 1)] public float moneyJackpotChance = 0.25f;

        [Header("Paper ball (placeholder reward)")]
        [Min(.1f)] public float paperBallFlightSeconds = 1.1f;
        [Min(0)] public float paperBallArcHeight = 1.3f;
        [Min(0)] public float paperBallBasketRadius = .20f;
        [Min(1)] public float paperBallMaxAimDistance = 15;
        [Min(.1f)] public float paperBallDropSeconds = .25f;
        [Min(.1f)] public float paperBallResultSeconds = 1.5f;

        /// <summary>Code defaults first, then the asset's list on top, so stats added later never start at 0.</summary>
        public void ApplyTo(StatSheet sheet)
        {
            foreach (var s in Defaults()) sheet.SetBase(s.stat, s.value);
            foreach (var s in baseStats) if (s.stat != StatId.None) sheet.SetBase(s.stat, s.value);
        }

        /// <summary>Provisional starting values; tune in the asset.</summary>
        public static StatBase[] Defaults() => new[]
        {
            new StatBase(StatId.GlobeMaxSpeed, 90),        // °/s (15 RPM); 101 × +7.5 RPM reaches 772.5 RPM
            new StatBase(StatId.GlobeAutoSpeed, 0),
            new StatBase(StatId.GlobeAutoShare, 0),
            new StatBase(StatId.GlobeDragPower, 1),
            new StatBase(StatId.GlobeSpeedDecay, 1),
            new StatBase(StatId.GlobeClickPower, 1),
            new StatBase(StatId.GlobeMultiplier, 1),
            new StatBase(StatId.IncomeMultiplier, 1),

            new StatBase(StatId.PinSlots, 0),              // start with no pins
            new StatBase(StatId.ShooterCooldown, 4.5),     // seconds; 40 reload ranks reach ~0.027 s
            new StatBase(StatId.PinLandChance, 0.3),       // ≈ Earth's land fraction
            new StatBase(StatId.PinCityChance, 0.05),
            new StatBase(StatId.PinDurability, 1),         // passes before falling off
            new StatBase(StatId.PinPayout, 1),
            new StatBase(StatId.PinAutoFire, 0),
            new StatBase(StatId.PinDoubleShotChance, 0),

            new StatBase(StatId.CasePayout, 1),
            new StatBase(StatId.CaseUnlocked, 0),
            new StatBase(StatId.CaseCooldown, 30),
            new StatBase(StatId.CaseAutoStampChance, 0),
            new StatBase(StatId.CaseEvidenceReduction, 0),
            new StatBase(StatId.CaseGold, 0),
            new StatBase(StatId.CaseGoldChance, 0),

            new StatBase(StatId.WhiskeyBoostDuration, 10),
            new StatBase(StatId.WhiskeyCooldown, 60),
            new StatBase(StatId.WhiskeyBoostStrength, 1.5),
            new StatBase(StatId.WhiskeyAutoRefill, 0),

            new StatBase(StatId.StatueGoldFillRate, 1),
            new StatBase(StatId.StatuePayout, 1),
            new StatBase(StatId.StatuePieceReduction, 0),
            new StatBase(StatId.StatueBonusGoldChance, 0),

            new StatBase(StatId.LootboxPrice, 1),
            new StatBase(StatId.LootLuck, 0),
            new StatBase(StatId.LootDoubleChance, 0),
        };
    }
}
