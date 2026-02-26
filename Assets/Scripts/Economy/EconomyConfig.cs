using System;
using UnityEngine;

/// <summary>
/// EconomyConfig — Centralised gem economy settings.
///
/// REBALANCING (post-alpha):
///   FREE GEM INCOME  : reduced from 40-60 → 15-20 per month
///   GEM PACKS        : repriced to match EU market (€ pricing)
///   BATTLE PASS      : dual unlock — €4.99 cash OR 950 gems
///   BATTLEPASS EARN  : 50 levels × 10 gems/level = max 500 gems/season
///
/// GEM PACK PRICES (new):
///   €0.99  → 100 gems   (Starter)
///   €4.99  → 600 gems   (Value — was 500)
///   €9.99  → 1500 gems  (Popular)
///   €49.99 → 9000 gems  (Mega)
///
/// FREE GEM SOURCES (new caps):
///   Daily login bonus      : 1 gem/day  → ~30/month
///   Battle Pass season earn: max 500/season
///   Achievement rewards    : ~10-20/month
///   Weekly chest           : 0-5 gems
///   Target monthly free    : 15-20 gems  (login NOT included in target pool;
///                             login gems are a retention mechanic, counted separately)
///
/// USAGE:
///   EconomyConfig.GemPacks[0].GemAmount
///   EconomyConfig.BattlePass.GemPrice
///   EconomyConfig.FreeMonthlyGemTarget
/// </summary>
public static class EconomyConfig
{
    // ─── Gem Packs ────────────────────────────────────────────────────────────
    [Serializable]
    public struct GemPackDefinition
    {
        public string ProductId;      // matches IAP product ID
        public int    GemAmount;
        public float  PriceEUR;
        public string DisplayName;
        public string Tag;            // "starter" | "value" | "popular" | "mega"
        public bool   IsBestValue;
    }

    public static readonly GemPackDefinition[] GemPacks = new GemPackDefinition[]
    {
        new GemPackDefinition { ProductId = "gems_100",  GemAmount = 100,  PriceEUR = 0.99f,  DisplayName = "Starter Pack",  Tag = "starter",  IsBestValue = false },
        new GemPackDefinition { ProductId = "gems_600",  GemAmount = 600,  PriceEUR = 4.99f,  DisplayName = "Value Pack",    Tag = "value",    IsBestValue = false },
        new GemPackDefinition { ProductId = "gems_1500", GemAmount = 1500, PriceEUR = 9.99f,  DisplayName = "Popular Pack",  Tag = "popular",  IsBestValue = true  },
        new GemPackDefinition { ProductId = "gems_9000", GemAmount = 9000, PriceEUR = 49.99f, DisplayName = "Mega Pack",     Tag = "mega",     IsBestValue = false },
    };

    // ─── Battle Pass ──────────────────────────────────────────────────────────
    public struct BattlePassDefinition
    {
        public string ProductId;            // IAP product ID for cash purchase
        public float  PriceEUR;             // cash price
        public int    GemPrice;             // gem alternative price
        public int    TotalLevels;          // total season levels
        public int    GemsPerLevel;         // gems earned per level (premium track)
        public int    MaxGemsEarnable;      // TotalLevels × GemsPerLevel
    }

    public static readonly BattlePassDefinition BattlePass = new BattlePassDefinition
    {
        ProductId        = "battle_pass_season",
        PriceEUR         = 4.99f,
        GemPrice         = 950,    // ✅ NEW: can also buy with gems
        TotalLevels      = 50,     // 50-level track
        GemsPerLevel     = 10,     // 10 gems per premium level completed
        MaxGemsEarnable  = 500     // 50 × 10 = 500 gems per season
    };

    // ─── Free Gem Income ──────────────────────────────────────────────────────

    /// <summary>
    /// Target free gem income (excl. daily login).
    /// Used for balance testing / documentation purposes.
    /// </summary>
    public const int FreeMonthlyGemTarget = 18;  // midpoint of 15-20 range

    /// <summary>Achievement pool per month (typical, not guaranteed).</summary>
    public const int AchievementGemsPerMonth = 15;

    /// <summary>Weekly chest gem range.</summary>
    public const int WeeklyChestGemMin = 0;
    public const int WeeklyChestGemMax = 5;

    /// <summary>Daily login bonus (separate from monthly target; retention mechanic).</summary>
    public const int DailyLoginGems = 1;  // 1 gem/day = ~30/month

    // ─── Gem Costs (reference table) ─────────────────────────────────────────

    /// <summary>Cost in gems for ad-free chest skip.</summary>
    public const int ChestSkipCost = 10;

    /// <summary>Cost in gems for casual revive (Solo/Casual modes only).</summary>
    public const int ReviveGemCost = 30;

    /// <summary>Minimum gems required to unlock a season cosmetic.</summary>
    public const int CosmeticMinCost = 200;

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the gem pack definition matching the given IAP product ID.
    /// Returns null if not found.
    /// </summary>
    public static GemPackDefinition? GetPackByProductId(string productId)
    {
        foreach (var pack in GemPacks)
            if (pack.ProductId.Equals(productId, StringComparison.OrdinalIgnoreCase))
                return pack;
        return null;
    }

    /// <summary>
    /// Converts a gem count to a rough EUR value using the best-value pack rate.
    /// Useful for fraud detection comparison.
    /// </summary>
    public static float GemsToEUR(int gems)
    {
        // Best rate: 9000 gems / €49.99 = 180 gems/€
        const float gemsPerEuro = 180f;
        return gems / gemsPerEuro;
    }

    // ─── Debug ────────────────────────────────────────────────────────────────

    public static void LogConfig()
    {
        Debug.Log("[EconomyConfig] ─── Gem Packs ───");
        foreach (var p in GemPacks)
            Debug.Log($"  {p.ProductId}: {p.GemAmount} gems @ €{p.PriceEUR} [{p.Tag}]");

        Debug.Log($"[EconomyConfig] Battle Pass: €{BattlePass.PriceEUR} OR {BattlePass.GemPrice} gems | " +
                  $"Max earn: {BattlePass.MaxGemsEarnable} gems/season");

        Debug.Log($"[EconomyConfig] Free gem target: {FreeMonthlyGemTarget}/month");
    }
}
