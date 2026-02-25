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
///   Power-ups      → Icons/PowerUps/power_{electric}
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
        { "power_electric",       "Icons/PowerUps/power_electric" },  // electric

        // ── UI Flat Variants (smaller/HUD use) ────────────────────────────────
        { "gem_flat",             "Icons/UI/gem_flat"         },
        { "coin_flat",            "Icons/UI/coin_flat"        },
        { "trophy_flat",          "Icons/UI/trophy_flat"      },
        { "crown_flat",           "Icons/UI/crown_flat"       },
        { "star_flat",            "Icons/UI/star_flat"        },
        { "lightning_flat",       "Icons/UI/lightning_flat"   },
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
