using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Season data models for Vault Dash Season System.
///
/// SeasonInfo       — Loaded from config/currentSeason and config/seasons/{id}
/// SeasonCosmetic   — Skin/cosmetic attached to each season
/// ArenaOverlay     — Seasonal arena theming (colors, glow)
/// PlayerSeasonRecord — Stored at players/{uid}/seasons/{seasonId}
/// SeasonLeaderboardEntry — Archived top-100 entry
///
/// Firestore schema: see Assets/Firebase/SEASON_SCHEMA.md
/// </summary>

// ─── Season Info ──────────────────────────────────────────────────────────────

[Serializable]
public class SeasonInfo
{
    public string    seasonId;        // "season_1"
    public int       seasonNumber;    // 1, 2, 3 …
    public string    name;            // "Neon Vault"
    public string    theme;           // "neon" | "pirate" | "winter" etc.
    public DateTime  startDate;
    public DateTime  endDate;
    public string    resetTimeUtc;    // "00:00"
    public int       durationDays;    // default 30
    public bool      rewardsDistributed;
    public bool      hardResetDone;

    public SeasonCosmetic   cosmetic;
    public ArenaOverlay     arenaOverlay;
    public PowerupTheme     powerupTheme;

    // ── Computed ───────────────────────────────────────────────────────────

    public bool IsActive => DateTime.UtcNow >= startDate && DateTime.UtcNow < endDate;

    public TimeSpan TimeRemaining
    {
        get
        {
            var remaining = endDate - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public string TimeRemainingFormatted
    {
        get
        {
            var t = TimeRemaining;
            if (t.TotalSeconds <= 0) return "Season ended";
            if (t.TotalDays >= 1)
                return $"{(int)t.TotalDays}d {t.Hours}h";
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours}h {t.Minutes}m";
            return $"{t.Minutes}m";
        }
    }

    /// <summary>Days until next season (assumes seasons are consecutive).</summary>
    public int DaysUntilEnd => Mathf.Max(0, (int)(endDate - DateTime.UtcNow).TotalDays);

    public override string ToString() =>
        $"Season {seasonNumber} '{name}' [{startDate:yyyy-MM-dd} → {endDate:yyyy-MM-dd}]";
}

// ─── Season Cosmetic ──────────────────────────────────────────────────────────

[Serializable]
public class SeasonCosmetic
{
    public string skinId;          // "neon_vault_skin"
    public string skinName;        // "Neon Vault Operative"
    public string description;
    public int    gemCost;         // purchase during season (default 500)
    public int    archiveGemCost;  // purchase after season ends (1.5x, set by Cloud Function)
    public int    prestigeFree;    // free at this prestige level (default 5)
    public string themeColor;      // hex "#4444FF"
    public string iconPath;        // Resources path or addressable key

    // ── Derived ───────────────────────────────────────────────────────────

    public Color ThemeColorUnity
    {
        get
        {
            if (ColorUtility.TryParseHtmlString(themeColor, out Color c)) return c;
            return Color.cyan;
        }
    }

    public int ArchiveCostDerived =>
        archiveGemCost > 0 ? archiveGemCost : Mathf.CeilToInt(gemCost * 1.5f);
}

// ─── Arena Overlay ────────────────────────────────────────────────────────────

[Serializable]
public class ArenaOverlay
{
    public string primary;   // "#4444FF"
    public string glow;      // "#6666FF"
    public string fog;       // "#000033"

    public Color PrimaryColor => ParseHex(primary, new Color(0.27f, 0.27f, 1f));
    public Color GlowColor    => ParseHex(glow,    new Color(0.4f, 0.6f, 1f));
    public Color FogColor     => ParseHex(fog,     new Color(0f, 0f, 0.2f));

    static Color ParseHex(string hex, Color fallback)
    {
        if (string.IsNullOrEmpty(hex)) return fallback;
        return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : fallback;
    }
}

// ─── Powerup Theme ────────────────────────────────────────────────────────────

[Serializable]
public class PowerupTheme
{
    /// <summary>
    /// Maps canonical power-up id → seasonal display name.
    /// e.g. { "FREEZE": "Winter Freeze" } for winter season.
    /// </summary>
    public Dictionary<string, string> overrides = new Dictionary<string, string>();

    public string GetName(string powerUpId)
    {
        if (overrides != null && overrides.TryGetValue(powerUpId, out string themed))
            return themed;
        return powerUpId; // fallback = original id
    }
}

// ─── Player Season Record ─────────────────────────────────────────────────────

[Serializable]
public class PlayerSeasonRecord
{
    public string   seasonId;
    public int      peakTrophies;
    public string   finalTier;
    public int      finalPrestige;
    public bool     claimedSeasonReward;
    public int      gemReward;            // gems awarded

    // ── Reward Calculation ────────────────────────────────────────────────

    /// <summary>
    /// Season reward formula: floor(peakTrophies / 100), capped at 500.
    /// Plus tier bonus: Legend +50, Master +25, Diamond +10.
    /// </summary>
    public static int CalculateGemReward(int peakTrophies)
    {
        int base_gems = Mathf.Min(peakTrophies / 100, 500);

        // Tier bonus
        if (peakTrophies >= 4500) base_gems += 50;      // Legend
        else if (peakTrophies >= 3500) base_gems += 25; // Master
        else if (peakTrophies >= 2000) base_gems += 10; // Diamond

        return Mathf.Min(base_gems, 500); // absolute cap
    }

    public string TierName
    {
        get
        {
            if (finalTier != null) return finalTier;
            return RankedProgressionManager.GetTierForTrophies(peakTrophies).name;
        }
    }
}

// ─── Season Leaderboard Entry (archived) ─────────────────────────────────────

[Serializable]
public class SeasonLeaderboardEntry
{
    public string   uid;
    public int      rank;
    public string   username;
    public int      trophies;
    public string   tier;
    public int      prestigeLevel;
    public bool     isLocalPlayer;

    public string DisplayRank =>
        prestigeLevel > 0
            ? $"{tier} {RankedProgressionManager.GetPrestigeStars(prestigeLevel)}"
            : tier;
}
