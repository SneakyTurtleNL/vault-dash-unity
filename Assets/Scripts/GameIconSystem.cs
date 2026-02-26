using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameIconSystem — Singleton for loading and caching custom Scenario.gg icons.
///
/// All icons live in Assets/Resources/Icons/{category}/{name}.png
/// Load via: GameIconSystem.GetIcon("tier_diamond")
///           GameIconSystem.GetSprite("tier_diamond")
///           GameIconSystem.ApplyIcon(image, "tier_diamond")
///
/// Icon key → file path mapping:
///   Tier icons     → Icons/Tiers/tier_{rookie|silver|gold|diamond|master|legend}
///   Currency       → Icons/Currency/{gem|coin|trophy}
///   Power-ups      → Icons/PowerUps/power_{electric|freeze|shrink|reverse|obstacle}
///   Actions        → Icons/Actions/{star|shield|sword}
///   UI flat        → Icons/UI/{gem_flat|coin_flat|trophy_flat|crown_flat|star_flat|lightning_flat}
///
/// USAGE:
///   // Display tier icon on an Image component:
///   GameIconSystem.ApplyIcon(tierImage, "tier_diamond");
///
///   // Get Sprite for manual assignment:
///   var sprite = GameIconSystem.GetSprite("trophy");
///
///   // Get tier icon key for a given Tier enum:
///   string key = GameIconSystem.TierIconKey(RankedProgressionManager.Tier.Diamond);
/// </summary>
public class GameIconSystem : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static GameIconSystem Instance { get; private set; }

    // ─── Icon Registry ────────────────────────────────────────────────────────
    /// <summary>Maps short key → Resources path (without "Icons/" prefix).</summary>
    private static readonly Dictionary<string, string> IconPaths = new Dictionary<string, string>
    {
        // ── Tier Icons (Scenario.gg Puffy Icons 3.0) ─────────────────────────
        { "tier_rookie",          "Icons/Tiers/tier_rookie"   },   // bronze medal
        { "tier_silver",          "Icons/Tiers/tier_silver"   },   // silver medal
        { "tier_gold",            "Icons/Tiers/tier_gold"     },   // gold medal
        { "tier_diamond",         "Icons/Tiers/tier_diamond"  },   // crystal gem
        { "tier_master",          "Icons/Tiers/tier_master"   },   // mystical card
        { "tier_legend",          "Icons/Tiers/tier_legend"   },   // golden crown

        // ── Currency Icons ────────────────────────────────────────────────────
        { "gem",                  "Icons/Currency/gem"        },   // premium currency
        { "coin",                 "Icons/Currency/coin"       },   // soft currency
        { "trophy",               "Icons/Currency/trophy"     },   // trophies/rank

        // ── Action Icons ──────────────────────────────────────────────────────
        { "star",                 "Icons/Actions/star"        },   // achievement/prestige
        { "shield",               "Icons/Actions/shield"      },   // defense/block
        { "sword",                "Icons/Actions/sword"       },   // attack/rematch

        // ── Power-Up Icons ────────────────────────────────────────────────────
        { "power_electric",       "Icons/PowerUps/power_electric"  },  // electric
        { "power_freeze",         "Icons/PowerUps/power_freeze"    },  // freeze power-up HUD
        { "power_shrink",         "Icons/PowerUps/power_shrink"    },  // shrink power-up HUD
        { "power_reverse",        "Icons/PowerUps/power_reverse"   },  // reverse power-up HUD
        { "power_obstacle",       "Icons/PowerUps/power_obstacle"  },  // obstacle power-up HUD

        // ── UI Flat Variants (smaller/HUD use) ────────────────────────────────
        { "gem_flat",             "Icons/UI/gem_flat"         },
        { "coin_flat",            "Icons/UI/coin_flat"        },
        { "trophy_flat",          "Icons/UI/trophy_flat"      },
        { "crown_flat",           "Icons/UI/crown_flat"       },
        { "star_flat",            "Icons/UI/star_flat"        },
        { "lightning_flat",       "Icons/UI/lightning_flat"   },

        // ── Prestige Badges ───────────────────────────────────────────────────
        // Placeholder PNGs (128x128, purple tones) — swap with Scenario.gg art post-launch.
        // Keys: "prestige_1", "prestige_5", "prestige_10", "prestige_20"
        { "prestige_1",           "Icons/Prestige/prestige_1"           },
        { "prestige_5",           "Icons/Prestige/prestige_5"           },
        { "prestige_10",          "Icons/Prestige/prestige_10"          },
        { "prestige_20",          "Icons/Prestige/prestige_20"          },
        // TODO post-launch: generate prestige_2, prestige_3, prestige_4, prestige_6..20 ranges
        // from Scenario.gg. Use prestige_1 as fallback for levels 2-4, prestige_5 for 6-9, etc.

        // ── Battle Pass Icons ─────────────────────────────────────────────────
        // Placeholder PNGs (128x128, gold tones) — swap with Scenario.gg art post-launch.
        { "battle_pass_tier_1",   "Icons/BattlePass/battle_pass_tier_1"  },
        { "battle_pass_tier_30",  "Icons/BattlePass/battle_pass_tier_30" },
        { "battle_pass_premium",  "Icons/BattlePass/battle_pass_premium" },
        // TODO post-launch: generate full battle-pass icon set (tiers 1-50) via Scenario.gg.
        // Add keys: "bp_tier_{n}" for each tier milestone.

        // ── Seasonal Icons ────────────────────────────────────────────────────
        // Placeholder PNGs (128x128, theme colors) — swap with Scenario.gg art post-launch.
        { "season_rookie",        "Icons/Seasonal/season_rookie"        },
        { "season_silver",        "Icons/Seasonal/season_silver"        },
        { "season_legend",        "Icons/Seasonal/season_legend"        },
        // TODO post-launch: generate per-season icons via Scenario.gg (season_neon, season_frost, etc.)

        // ── Card Rarity Backgrounds ───────────────────────────────────────────
        // Placeholder PNGs (128x128, rarity color palette) — swap with art post-launch.
        { "card_common_bg",       "Icons/CardBg/card_common_bg"         },
        { "card_rare_bg",         "Icons/CardBg/card_rare_bg"           },
        { "card_epic_bg",         "Icons/CardBg/card_epic_bg"           },
        { "card_legendary_bg",    "Icons/CardBg/card_legendary_bg"      },

        // ── Splash / Loading ──────────────────────────────────────────────────
        // Placeholder PNGs (256x256, dark navy) — swap with final art post-launch.
        { "splash_art_main",      "Splash/splash_art_main"              },
        { "loading_screen_bg",    "Splash/loading_screen_bg"            },

        // ── Loot Burst Texture Variants ───────────────────────────────────────
        // Placeholder PNGs — used as particle textures for loot burst effects.
        { "coin_burst_large",     "Particles/LootBurst/coin_burst_large" },
        { "gem_burst_large",      "Particles/LootBurst/gem_burst_large"  },
        { "chest_burst",          "Particles/LootBurst/chest_burst"      },
    };

    // ─── Cache ────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Preload all tier icons at startup (fast, ~6 small PNGs)
        PreloadTierIcons();
    }

    void PreloadTierIcons()
    {
        var tierKeys = new[] { "tier_rookie", "tier_silver", "tier_gold", "tier_diamond", "tier_master", "tier_legend" };
        foreach (var key in tierKeys)
            GetSprite(key); // warms cache
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Get a Sprite for an icon key. Returns null if icon not found.
    /// Cached after first load.
    /// </summary>
    public static Sprite GetSprite(string key)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"[GameIconSystem] Not initialized. Key: {key}");
            return null;
        }
        return Instance.GetSpriteInternal(key);
    }

    /// <summary>
    /// Apply icon to a Unity UI Image. Safe — does nothing if image is null or icon missing.
    /// </summary>
    public static void ApplyIcon(Image image, string key)
    {
        if (image == null) return;
        var sprite = GetSprite(key);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.enabled = true;
        }
    }

    /// <summary>
    /// Get the icon key for a RankedProgressionManager tier.
    /// </summary>
    public static string TierIconKey(RankedProgressionManager.Tier tier)
    {
        return tier switch
        {
            RankedProgressionManager.Tier.Rookie  => "tier_rookie",
            RankedProgressionManager.Tier.Silver  => "tier_silver",
            RankedProgressionManager.Tier.Gold    => "tier_gold",
            RankedProgressionManager.Tier.Diamond => "tier_diamond",
            RankedProgressionManager.Tier.Master  => "tier_master",
            RankedProgressionManager.Tier.Legend  => "tier_legend",
            _                                      => "tier_rookie",
        };
    }

    /// <summary>
    /// Get tier icon key for a TierInfo struct (convenience overload).
    /// </summary>
    public static string TierIconKey(RankedProgressionManager.TierInfo tierInfo)
    {
        return TierIconKey(tierInfo.tier);
    }

    /// <summary>
    /// Get the icon key for a prestige badge level.
    /// Maps any prestige level to the nearest available badge art.
    /// Placeholder badges: 1, 5, 10, 20. Higher levels use prestige_20.
    /// </summary>
    public static string PrestigeBadgeKey(int prestigeLevel)
    {
        if (prestigeLevel <= 0)  return "";       // no badge at prestige 0
        if (prestigeLevel < 5)   return "prestige_1";
        if (prestigeLevel < 10)  return "prestige_5";
        if (prestigeLevel < 20)  return "prestige_10";
        return "prestige_20";
        // TODO post-launch: extend thresholds when per-level badges are generated.
    }

    /// <summary>
    /// Apply prestige badge icon to an Image component.
    /// Hides the image if no prestige (level 0).
    /// </summary>
    public static void ApplyPrestigeBadge(Image image, int prestigeLevel)
    {
        if (image == null) return;
        string key = PrestigeBadgeKey(prestigeLevel);
        if (string.IsNullOrEmpty(key))
        {
            image.enabled = false;
            return;
        }
        ApplyIcon(image, key);
    }

    /// <summary>
    /// Get the seasonal icon key for a season tier string.
    /// "Rookie" → season_rookie, "Silver" → season_silver, others → season_legend.
    /// </summary>
    public static string SeasonIconKey(string seasonTier)
    {
        if (string.IsNullOrEmpty(seasonTier)) return "season_rookie";
        return seasonTier.ToLower() switch
        {
            "rookie" => "season_rookie",
            "silver" => "season_silver",
            "gold"   => "season_silver",   // gold shares silver placeholder for now
            "diamond"=> "season_legend",
            "master" => "season_legend",
            "legend" => "season_legend",
            _ => "season_rookie",
        };
        // TODO post-launch: create per-season themed icons (season_neon, etc.)
    }

    /// <summary>
    /// Get the battle pass icon key for a tier number.
    /// </summary>
    public static string BattlePassTierKey(int tier)
    {
        if (tier >= 30) return "battle_pass_tier_30";
        return "battle_pass_tier_1";
        // TODO post-launch: add battle_pass_tier_n for each tier milestone
    }

    /// <summary>
    /// Get the card rarity background icon key.
    /// </summary>
    public static string CardRarityBgKey(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common    => "card_common_bg",
            CardRarity.Rare      => "card_rare_bg",
            CardRarity.Epic      => "card_epic_bg",
            CardRarity.Legendary => "card_legendary_bg",
            _ => "card_common_bg",
        };
    }

    /// <summary>
    /// Check whether an icon key exists in the registry.
    /// </summary>
    public static bool HasIcon(string key) => IconPaths.ContainsKey(key);

    /// <summary>
    /// Preload a specific icon into cache.
    /// </summary>
    public static void Preload(string key) => GetSprite(key);

    // ─── Internal ─────────────────────────────────────────────────────────────

    Sprite GetSpriteInternal(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (_spriteCache.TryGetValue(key, out var cached)) return cached;

        if (!IconPaths.TryGetValue(key, out string path))
        {
            Debug.LogWarning($"[GameIconSystem] Unknown icon key: '{key}'");
            return null;
        }

        var tex = Resources.Load<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogWarning($"[GameIconSystem] Texture not found at Resources/{path}");
            return null;
        }

        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect
        );

        sprite.name = key;
        _spriteCache[key] = sprite;
        _textureCache[key] = tex;

        return sprite;
    }

    // ─── Editor Helpers ───────────────────────────────────────────────────────

    [ContextMenu("Debug: Log All Icons")]
    void DbgLogAllIcons()
    {
        Debug.Log($"[GameIconSystem] Registry ({IconPaths.Count} icons):");
        foreach (var kv in IconPaths)
            Debug.Log($"  '{kv.Key}' → Resources/{kv.Value}");
    }

    [ContextMenu("Debug: Preload All Icons")]
    void DbgPreloadAll()
    {
        foreach (var key in IconPaths.Keys)
        {
            var sprite = GetSpriteInternal(key);
            Debug.Log($"  {key}: {(sprite != null ? "OK" : "MISSING")}");
        }
    }
}
