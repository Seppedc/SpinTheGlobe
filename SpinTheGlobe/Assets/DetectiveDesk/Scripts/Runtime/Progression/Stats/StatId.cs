namespace DetectiveDesk.Progression
{
    /// <summary>
    /// Every tunable gameplay number that upgrades can modify.
    /// Explicit values are serialized into assets and saves: append only, never renumber.
    /// Per-pin stats (PinDurability, PinPayout) are addressed with a pin index; -1 means "all pins".
    /// </summary>
    public enum StatId
    {
        None = 0,

        // Globe
        GlobeMaxSpeed = 100,        // degrees / second cap for drag and auto-rotate
        GlobeAutoSpeed = 101,       // degrees / second auto-rotate floor (never above GlobeMaxSpeed)
        GlobeDragPower = 102,       // multiplier on drag acceleration
        GlobeClickPower = 103,      // multiplier on click impulse
        GlobeMultiplier = 104,      // base multiplier on all globe earnings
        IncomeMultiplier = 105,     // multiplier on every income source (golden nodes)

        // Pins & shooter
        PinSlots = 200,             // number of pins unlocked (0..12)
        ShooterCooldown = 201,      // seconds between shots
        PinLandChance = 202,        // probability a shot lands on land
        PinCityChance = 203,        // probability a shot lands near a big city
        PinDurability = 210,        // passes a pin survives (per pin)
        PinPayout = 211,            // payout multiplier (per pin)
        PinAutoFire = 220,          // ≥1: the shooter fires by itself when ready
        PinDoubleShotChance = 221,  // chance a shot fires a second pin

        // Case file
        CasePayout = 300,           // multiplier on case rewards
        CaseCooldown = 301,         // seconds before the next case opens
        CaseAutoStampChance = 302,  // probability a full case stamps itself
        CaseEvidenceReduction = 303,// fewer evidence needed per case
        CaseGold = 304,             // gold paid per approved case

        // Whiskey glass
        WhiskeyBoostDuration = 400, // seconds
        WhiskeyCooldown = 401,      // seconds
        WhiskeyBoostStrength = 402, // payout & spin multiplier while active
        WhiskeyAutoRefill = 403,    // ≥1: the glass refills itself after the cooldown

        // Statues
        StatueGoldFillRate = 500,   // multiplier on gold fill per globe revolution
        StatuePayout = 501,         // multiplier on gold from smashing
        StatuePieceReduction = 502, // fewer pieces needed per statue

        // Lootbox
        LootboxPrice = 600,         // multiplier on the lootbox price
        LootLuck = 601,             // extra weight for statue pieces and gold
        LootDoubleChance = 602,     // chance a lootbox gives two rewards
    }
}
