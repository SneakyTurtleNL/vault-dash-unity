using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_FIRESTORE
using Firebase.Firestore;
using Firebase.Extensions;
#endif

/// <summary>
/// RankedProgressionManager — Central authority for trophy tier + prestige system.
///
/// TIER THRESHOLDS (trophies):
///   Rookie   0    – 499
///   Silver   500  – 999
///   Gold     1000 – 1999
///   Diamond  2000 – 3499
///   Master   3500 – 4499
///   Legend   4500+
///
/// PRESTIGE FLOW:
///   When player hits Legend AND has >= 4500 trophies + completes the prestige
///   confirmation, trophies reset to 0 (Rookie) but prestigeLevel increments.
///   Each prestige level grants a badge star: Prestige 1 = ⭐, Prestige 2 = ⭐⭐, etc.
///
/// FIRESTORE SCHEMA (guarded by FIREBASE_FIRESTORE define):
///   players/{uid}/
///     trophies        : int
///     prestigeLevel   : int
///     currentTier     : string
///     prestige/{prestigeLevel}/
///       achievedAt    : timestamp
///       totalMatches  : int
///       totalWins     : int
///       peakTrophies  : int
///
/// LOCAL FALLBACK (always):
///   PlayerPrefs keys: VaultDash_Trophies, VaultDash_PrestigeLevel
///
/// SETUP:
///   1. Import Firebase Firestore Unity SDK
///   2. Add FIREBASE_FIRESTORE to Player → Scripting Define Symbols
///   3. Place this MonoBehaviour in your persistent scene (alongside FirebaseManager)
///
/// Usage:
///   RankedProgressionManager.Instance.AddTrophies(15);
///   RankedProgressionManager.Instance.OnPrestigeAvailable += ShowPrestigePrompt;
/// </summary>
public class RankedProgressionManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static RankedProgressionManager Instance { get; private set; }

    // ─── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired whenever trophies or prestige changes.</summary>
    public event Action<ProgressionState> OnProgressionChanged;

    /// <summary>Fired when player is eligible to prestige (reached Legend and was notified).</summary>
    public event Action<int> OnPrestigeAvailable;  // param = current prestigeLevel

    /// <summary>Fired immediately after a prestige reset completes.</summary>
    public event Action<int> OnPrestigeCompleted;  // param = new prestigeLevel

    /// <summary>Fired when player crosses a tier boundary (up or down).</summary>
    public event Action<TierInfo, TierInfo> OnTierChanged;  // (oldTier, newTier)

    // ─── Tier Definitions ─────────────────────────────────────────────────────
    public enum Tier { Rookie, Silver, Gold, Diamond, Master, Legend }

    [Serializable]
    public struct TierInfo
    {
        public Tier   tier;
        public string name;
        public int    minTrophies;
        public int    maxTrophies;   // -1 = no cap (Legend)
        public Color  color;
        public string emoji;

        public bool IsLegend => tier == Tier.Legend;

        public int ProgressionCap => maxTrophies < 0 ? 99999 : maxTrophies;

        public float NormalizedProgress(int trophies)
        {
            if (maxTrophies < 0) return 1f;
            int range = maxTrophies - minTrophies;
            if (range <= 0) return 0f;
            return Mathf.Clamp01((float)(trophies - minTrophies) / range);
        }

        public int TrophiesToNext(int trophies)
        {
            if (maxTrophies < 0) return 0;
            return Mathf.Max(0, maxTrophies - trophies + 1);
        }
    }

    public static readonly TierInfo[] TIERS = new TierInfo[]
    {
        new TierInfo { tier = Tier.Rookie,  name = "Rookie",  minTrophies = 0,    maxTrophies = 499,  emoji = "🥉", color = new Color(0.68f, 0.68f, 0.68f) },
        new TierInfo { tier = Tier.Silver,  name = "Silver",  minTrophies = 500,  maxTrophies = 999,  emoji = "🥈", color = new Color(0.80f, 0.85f, 0.90f) },
        new TierInfo { tier = Tier.Gold,    name = "Gold",    minTrophies = 1000, maxTrophies = 1999, emoji = "🥇", color = new Color(0.90f, 0.75f, 0.10f) },
        new TierInfo { tier = Tier.Diamond, name = "Diamond", minTrophies = 2000, maxTrophies = 3499, emoji = "💎", color = new Color(0.40f, 0.70f, 1.00f) },
        new TierInfo { tier = Tier.Master,  name = "Master",  minTrophies = 3500, maxTrophies = 4499, emoji = "🔮", color = new Color(0.80f, 0.40f, 1.00f) },
        new TierInfo { tier = Tier.Legend,  name = "Legend",  minTrophies = 4500, maxTrophies = -1,   emoji = "👑", color = new Color(1.00f, 0.84f, 0.00f) },
    };

    // Prestige threshold — must be in Legend tier with this many trophies to trigger
    public const int PRESTIGE_THRESHOLD = 4500;

    // ─── State ────────────────────────────────────────────────────────────────
    [Serializable]
    public class ProgressionState
    {
        public int    trophies;
        public int    prestigeLevel;   // 0 = no prestige, 1+ = prestige badges
        public TierInfo currentTier;
        public bool   prestigeAvailable;
    }

    public ProgressionState State { get; private set; } = new ProgressionState();

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Debug")]
    [Tooltip("Set trophies directly in editor for testing")]
    [SerializeField] private int _debugSetTrophies = -1;
    [SerializeField] private bool _applyDebugOnStart = false;

    // ─── PlayerPrefs Keys ─────────────────────────────────────────────────────
    private const string PP_TROPHIES  = "VaultDash_Trophies";
    private const string PP_PRESTIGE  = "VaultDash_PrestigeLevel";
    private const string PP_NOTIFIED  = "VaultDash_PrestigeNotified";

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadFromLocal();

        if (_applyDebugOnStart && _debugSetTrophies >= 0)
        {
            State.trophies = _debugSetTrophies;
            SaveToLocal();
        }

        // Attempt to load from Firestore (async — local is already available)
        StartCoroutine(LoadFromFirestore());

        CheckPrestigeAvailability();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Add or remove trophies. Handles tier change events + prestige unlock.</summary>
    public void AddTrophies(int delta)
    {
        var oldTier = State.currentTier;
        State.trophies = Mathf.Max(0, State.trophies + delta);
        State.currentTier = GetTierForTrophies(State.trophies);

        SaveToLocal();
        StartCoroutine(PushToFirestore());

        // Tier changed?
        if (oldTier.tier != State.currentTier.tier)
        {
            Debug.Log($"[Prestige] Tier change: {oldTier.name} → {State.currentTier.name}");
            OnTierChanged?.Invoke(oldTier, State.currentTier);
        }

        // Update season peak trophies (SeasonManager tracks the personal best)
        SeasonManager.Instance?.UpdatePeakTrophies(State.trophies);

        CheckPrestigeAvailability();
        OnProgressionChanged?.Invoke(State);
    }

    /// <summary>Set trophies directly (for data migration / reset). Fires events.</summary>
    public void SetTrophies(int value)
    {
        var oldTier = State.currentTier;
        State.trophies = Mathf.Max(0, value);
        State.currentTier = GetTierForTrophies(State.trophies);
        SaveToLocal();
        StartCoroutine(PushToFirestore());

        if (oldTier.tier != State.currentTier.tier)
            OnTierChanged?.Invoke(oldTier, State.currentTier);

        CheckPrestigeAvailability();
        OnProgressionChanged?.Invoke(State);
    }

    /// <summary>
    /// Execute prestige reset. Only call after player confirmed.
    /// Saves prestige record to Firestore, resets trophies to 0.
    /// </summary>
    public void ExecutePrestige()
    {
        if (!State.prestigeAvailable)
        {
            Debug.LogWarning("[Prestige] ExecutePrestige called but not available");
            return;
        }

        int oldPrestige = State.prestigeLevel;
        int peakTrophies = State.trophies;
        int totalMatches = PlayerPrefs.GetInt("VaultDash_TotalMatches", 0);
        int totalWins    = PlayerPrefs.GetInt("VaultDash_TotalWins", 0);

        // Increment prestige
        State.prestigeLevel++;

        // Reset trophies to Rookie
        State.trophies = 0;
        State.currentTier = GetTierForTrophies(0);
        State.prestigeAvailable = false;

        // Clear "already notified" so we can notify again after next Legend climb
        PlayerPrefs.SetInt(PP_NOTIFIED, 0);

        SaveToLocal();
        StartCoroutine(SavePrestigeRecord(State.prestigeLevel, peakTrophies, totalMatches, totalWins));

        Debug.Log($"[Prestige] ✨ Prestige {State.prestigeLevel} achieved! Trophies reset to 0.");
        OnPrestigeCompleted?.Invoke(State.prestigeLevel);
        OnProgressionChanged?.Invoke(State);
    }

    // ─── Tier Helpers ─────────────────────────────────────────────────────────

    public static TierInfo GetTierForTrophies(int trophies)
    {
        for (int i = TIERS.Length - 1; i >= 0; i--)
            if (trophies >= TIERS[i].minTrophies) return TIERS[i];
        return TIERS[0];
    }

    public static TierInfo GetTierInfo(Tier tier)
    {
        foreach (var t in TIERS)
            if (t.tier == tier) return t;
        return TIERS[0];
    }

    public static string GetPrestigeStars(int prestigeLevel)
    {
        if (prestigeLevel <= 0) return "";
        // Group into sets of 5 stars for readability
        int full  = prestigeLevel / 5;
        int rem   = prestigeLevel % 5;
        string s  = "";
        for (int i = 0; i < full; i++) s += "⭐⭐⭐⭐⭐ ";
        for (int i = 0; i < rem;  i++) s += "⭐";
        return s.Trim();
    }

    public static string GetPrestigeLabel(int prestigeLevel)
    {
        if (prestigeLevel <= 0) return "";
        return $"Prestige {prestigeLevel}";
    }

    public static Color GetPrestigeGlowColor(int prestigeLevel)
    {
        if (prestigeLevel <= 0) return Color.clear;
        // Purple glow, intensity scales with prestige
        float intensity = Mathf.Min(1f, 0.6f + prestigeLevel * 0.04f);
        return new Color(0.65f, 0.1f, 1.0f, intensity); // deep purple
    }

    // ─── Prestige Availability ────────────────────────────────────────────────

    void CheckPrestigeAvailability()
    {
        bool eligible = State.currentTier.tier == Tier.Legend
                        && State.trophies >= PRESTIGE_THRESHOLD;
        State.prestigeAvailable = eligible;

        if (eligible)
        {
            int alreadyNotified = PlayerPrefs.GetInt(PP_NOTIFIED, 0);
            if (alreadyNotified == 0)
            {
                PlayerPrefs.SetInt(PP_NOTIFIED, 1);
                PlayerPrefs.Save();
                Debug.Log($"[Prestige] 🌟 Prestige available! Current prestige: {State.prestigeLevel}");
                OnPrestigeAvailable?.Invoke(State.prestigeLevel);
            }
        }
    }

    // ─── Local Persistence ────────────────────────────────────────────────────

    void LoadFromLocal()
    {
        State.trophies      = PlayerPrefs.GetInt(PP_TROPHIES, 0);
        State.prestigeLevel = PlayerPrefs.GetInt(PP_PRESTIGE, 0);
        State.currentTier   = GetTierForTrophies(State.trophies);
        Debug.Log($"[Prestige] Loaded local: trophies={State.trophies} prestige={State.prestigeLevel} tier={State.currentTier.name}");
    }

    void SaveToLocal()
    {
        PlayerPrefs.SetInt(PP_TROPHIES,  State.trophies);
        PlayerPrefs.SetInt(PP_PRESTIGE,  State.prestigeLevel);
        PlayerPrefs.SetString("VaultDash_Rank", State.currentTier.name);
        PlayerPrefs.Save();
    }

    // ─── Firestore Read ───────────────────────────────────────────────────────

    IEnumerator LoadFromFirestore()
    {
#if FIREBASE_FIRESTORE
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
        {
            yield return new WaitUntil(() =>
                FirebaseManager.Instance != null && FirebaseManager.Instance.IsInitialized);
        }

        string uid = GetCurrentUID();
        if (string.IsNullOrEmpty(uid)) { yield break; }

        var db   = FirebaseFirestore.DefaultInstance;
        var docRef = db.Collection("players").Document(uid);
        var task = docRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogWarning($"[Prestige] Firestore load error: {task.Exception.Message}");
            yield break;
        }

        var snap = task.Result;
        if (snap.Exists)
        {
            int fseTrophies  = snap.ContainsField("trophies")      ? snap.GetValue<int>("trophies")      : State.trophies;
            int fsePrestige  = snap.ContainsField("prestigeLevel")  ? snap.GetValue<int>("prestigeLevel") : State.prestigeLevel;

            // Use Firestore as source of truth only if >= local (prevents rollback)
            if (fseTrophies >= State.trophies || fsePrestige > State.prestigeLevel)
            {
                State.trophies      = fseTrophies;
                State.prestigeLevel = fsePrestige;
                State.currentTier   = GetTierForTrophies(State.trophies);
                SaveToLocal();
                Debug.Log($"[Prestige] Firestore synced: trophies={State.trophies} prestige={State.prestigeLevel}");
                CheckPrestigeAvailability();
                OnProgressionChanged?.Invoke(State);
            }
        }
        else
        {
            // New player — push initial record
            yield return PushToFirestore();
        }
#else
        Debug.Log("[Prestige] Firestore not enabled — using local PlayerPrefs only.");
        yield return null;
#endif
    }

    // ─── Firestore Write ──────────────────────────────────────────────────────

    IEnumerator PushToFirestore()
    {
#if FIREBASE_FIRESTORE
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
            yield break;

        string uid = GetCurrentUID();
        if (string.IsNullOrEmpty(uid)) yield break;

        var db     = FirebaseFirestore.DefaultInstance;
        var docRef = db.Collection("players").Document(uid);

        var data = new Dictionary<string, object>
        {
            { "trophies",      State.trophies      },
            { "prestigeLevel", State.prestigeLevel  },
            { "currentTier",   State.currentTier.name },
            { "lastUpdated",   FieldValue.ServerTimestamp },
        };

        var task = docRef.SetAsync(data, SetOptions.MergeAll);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
            Debug.LogWarning($"[Prestige] Firestore write error: {task.Exception.Message}");
        else
            Debug.Log($"[Prestige] Firestore pushed: trophies={State.trophies} prestige={State.prestigeLevel}");
#else
        yield return null;
#endif
    }

    IEnumerator SavePrestigeRecord(int prestigeLevel, int peakTrophies, int totalMatches, int totalWins)
    {
#if FIREBASE_FIRESTORE
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
            yield break;

        string uid = GetCurrentUID();
        if (string.IsNullOrEmpty(uid)) yield break;

        var db     = FirebaseFirestore.DefaultInstance;
        // players/{uid}/prestige/{prestigeLevel}
        var recordRef = db.Collection("players").Document(uid)
                          .Collection("prestige").Document(prestigeLevel.ToString());

        var record = new Dictionary<string, object>
        {
            { "achievedAt",    FieldValue.ServerTimestamp },
            { "totalMatches",  totalMatches               },
            { "totalWins",     totalWins                  },
            { "peakTrophies",  peakTrophies               },
        };

        var task = recordRef.SetAsync(record);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
            Debug.LogWarning($"[Prestige] Firestore prestige record error: {task.Exception.Message}");
        else
            Debug.Log($"[Prestige] ✅ Prestige {prestigeLevel} record saved to Firestore.");

        // Also update the parent player doc with latest trophies (reset to 0)
        yield return PushToFirestore();
#else
        Debug.Log($"[Prestige] (stub) Prestige {prestigeLevel} record: peakTrophies={peakTrophies}");
        yield return PushToFirestore();
#endif
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static string GetCurrentUID()
    {
#if FIREBASE_AUTH
        return Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "";
#else
        // Fallback: device-unique ID (not as secure but avoids null)
        return SystemInfo.deviceUniqueIdentifier;
#endif
    }

    // ─── Debug Gizmos ─────────────────────────────────────────────────────────

    [ContextMenu("Debug: Add 50 Trophies")]
    void DbgAdd50() => AddTrophies(50);

    [ContextMenu("Debug: Add 500 Trophies")]
    void DbgAdd500() => AddTrophies(500);

    [ContextMenu("Debug: Set Trophies to 4500 (Legend)")]
    void DbgSetLegend() => SetTrophies(4500);

    [ContextMenu("Debug: Execute Prestige")]
    void DbgPrestige()
    {
        State.prestigeAvailable = true;
        ExecutePrestige();
    }

    [ContextMenu("Debug: Reset Everything")]
    void DbgReset()
    {
        State.trophies      = 0;
        State.prestigeLevel = 0;
        State.currentTier   = GetTierForTrophies(0);
        State.prestigeAvailable = false;
        PlayerPrefs.SetInt(PP_NOTIFIED, 0);
        SaveToLocal();
        OnProgressionChanged?.Invoke(State);
    }
}
