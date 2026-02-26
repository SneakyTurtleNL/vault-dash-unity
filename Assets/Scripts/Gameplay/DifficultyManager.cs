using UnityEngine;

/// <summary>
/// DifficultyManager — Smooth difficulty curve across trophy tiers.
///
/// FIX: Eliminates the abrupt 25% speed jump between Gold→Diamond.
///
/// BEFORE (broken):
///   Gold (1000-1999)    : 7.0 u/s flat
///   Diamond (2000-3499) : 9.0 u/s flat  ← 28.5% cliff!
///
/// AFTER (smooth):
///   Gold range    : linearly ramps 7.0 → 7.8  u/s  (trophies 1000-1999)
///   Diamond range : linearly ramps 7.8 → 8.5  u/s  (trophies 2000-3499)
///   Master range  : linearly ramps 8.5 → 9.2  u/s  (trophies 3500-4499)
///   Legend range  : linearly ramps 9.2 → 10.0 u/s  (trophies 4500+)
///   No tier ever jumps more than ~8% in a single step.
///
/// USAGE:
///   float speed = DifficultyManager.Instance.GetScrollSpeed(trophies);
///   float interval = DifficultyManager.Instance.GetObstacleInterval(trophies);
///
/// INTEGRATION:
///   - GameManager.cs    : pass trophy count, set arena scroll speed
///   - ObstacleManager.cs: use GetObstacleInterval() for spawn timer
///   - TunnelGenerator.cs: use GetScrollSpeed() for scroll delta
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static DifficultyManager Instance { get; private set; }

    // ─── Tier Breakpoints ─────────────────────────────────────────────────────
    [System.Serializable]
    public struct TierCurve
    {
        public string TierName;
        public int    TrophyMin;
        public int    TrophyMax;     // -1 = no cap
        public float  SpeedMin;      // u/s at bottom of tier
        public float  SpeedMax;      // u/s at top of tier
        public float  IntervalMin;   // obstacle interval (seconds) at top of tier
        public float  IntervalMax;   // obstacle interval (seconds) at bottom of tier
    }

    [Header("Difficulty Curve (edit in Inspector or via this default)")]
    public TierCurve[] Tiers;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (Tiers == null || Tiers.Length == 0) Tiers = DefaultTiers();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Returns the scroll speed (u/s) for the given trophy count.</summary>
    public float GetScrollSpeed(int trophies)
    {
        var (curve, t) = FindCurve(trophies);
        return Mathf.Lerp(curve.SpeedMin, curve.SpeedMax, t);
    }

    /// <summary>Returns obstacle spawn interval (seconds) for the given trophy count.</summary>
    public float GetObstacleInterval(int trophies)
    {
        var (curve, t) = FindCurve(trophies);
        // Interval shrinks as difficulty goes up (lerp from Max→Min as t increases)
        return Mathf.Lerp(curve.IntervalMax, curve.IntervalMin, t);
    }

    /// <summary>Returns the tier name for the given trophy count.</summary>
    public string GetTierName(int trophies)
    {
        var (curve, _) = FindCurve(trophies);
        return curve.TierName;
    }

    // ─── Curve Lookup ─────────────────────────────────────────────────────────

    private (TierCurve curve, float t) FindCurve(int trophies)
    {
        foreach (var tier in Tiers)
        {
            int hi = tier.TrophyMax < 0 ? int.MaxValue : tier.TrophyMax;
            if (trophies >= tier.TrophyMin && trophies <= hi)
            {
                float range = hi == int.MaxValue
                    ? 1000f
                    : (tier.TrophyMax - tier.TrophyMin);

                float t = range > 0
                    ? Mathf.Clamp01((trophies - tier.TrophyMin) / range)
                    : 0f;

                return (tier, t);
            }
        }

        // Fallback: last tier at max
        return (Tiers[Tiers.Length - 1], 1f);
    }

    // ─── Default Curves ───────────────────────────────────────────────────────

    public static TierCurve[] DefaultTiers() => new TierCurve[]
    {
        // Rookie:  0 – 499    (5.0 → 6.0 u/s)  gentle intro
        new TierCurve { TierName = "Rookie",  TrophyMin = 0,    TrophyMax = 499,  SpeedMin = 5.0f, SpeedMax = 6.0f, IntervalMin = 2.2f, IntervalMax = 3.0f },

        // Silver:  500 – 999  (6.0 → 7.0 u/s)  comfortable ramp
        new TierCurve { TierName = "Silver",  TrophyMin = 500,  TrophyMax = 999,  SpeedMin = 6.0f, SpeedMax = 7.0f, IntervalMin = 1.8f, IntervalMax = 2.2f },

        // Gold:    1000-1999  (7.0 → 7.8 u/s)  ✅ FIX: smooth ramp (was flat 7.0)
        new TierCurve { TierName = "Gold",    TrophyMin = 1000, TrophyMax = 1999, SpeedMin = 7.0f, SpeedMax = 7.8f, IntervalMin = 1.5f, IntervalMax = 1.8f },

        // Diamond: 2000-3499  (7.8 → 8.5 u/s)  ✅ FIX: starts at 7.8 (was cliff to 9.0)
        new TierCurve { TierName = "Diamond", TrophyMin = 2000, TrophyMax = 3499, SpeedMin = 7.8f, SpeedMax = 8.5f, IntervalMin = 1.2f, IntervalMax = 1.5f },

        // Master:  3500-4499  (8.5 → 9.2 u/s)
        new TierCurve { TierName = "Master",  TrophyMin = 3500, TrophyMax = 4499, SpeedMin = 8.5f, SpeedMax = 9.2f, IntervalMin = 1.0f, IntervalMax = 1.2f },

        // Legend:  4500+      (9.2 → 10.0 u/s) no cap
        new TierCurve { TierName = "Legend",  TrophyMin = 4500, TrophyMax = -1,   SpeedMin = 9.2f, SpeedMax = 10.0f, IntervalMin = 0.8f, IntervalMax = 1.0f },
    };

    // ─── Debug ────────────────────────────────────────────────────────────────

    [ContextMenu("Print Curve Spot-Check")]
    private void DebugCurve()
    {
        int[] samples = { 0, 250, 500, 750, 1000, 1499, 1999, 2000, 2500, 3499, 3500, 4000, 4500, 6000 };
        foreach (int t in samples)
            Debug.Log($"[DifficultyManager] trophies={t:4} → speed={GetScrollSpeed(t):F2} u/s, interval={GetObstacleInterval(t):F2}s [{GetTierName(t)}]");
    }
}
