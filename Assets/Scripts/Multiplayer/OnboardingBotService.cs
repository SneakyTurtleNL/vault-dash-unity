using System;
using UnityEngine;

/// <summary>
/// OnboardingBotService — Structured bot progression for new players.
///
/// SEQUENCE:
///   Match 1: Very Easy bot  — guaranteed player LOSS (intro to game feel)
///   Match 2: Easy bot       — guaranteed player WIN  (first win hook)
///   Match 3: Medium bot     — ~60% chance player wins (challenge introduction)
///   Match 4+: Normal matchmaking (real players or adaptive bots via GhostMatchSystem)
///
/// PHILOSOPHY:
///   Match 1: "Wow this game is hard!" → engagement
///   Match 2: "I can beat this!"       → confidence + retention
///   Match 3: "Getting better!"        → mastery loop begins
///
/// FIRESTORE:
///   players/{uid}/onboardingMatchesPlayed : int  (0–3, after 3 = graduated)
///
/// LOCAL FALLBACK:
///   PlayerPrefs key: VaultDash_OnboardingMatchesPlayed
///
/// USAGE:
///   if (OnboardingBotService.Instance.IsInOnboarding)
///       var config = OnboardingBotService.Instance.GetCurrentBotConfig();
/// </summary>
public class OnboardingBotService : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static OnboardingBotService Instance { get; private set; }

    // ─── Constants ────────────────────────────────────────────────────────────
    private const int  ONBOARDING_MATCH_COUNT = 3;
    private const string PREF_MATCHES = "VaultDash_OnboardingMatchesPlayed";

    // ─── Bot Config ───────────────────────────────────────────────────────────
    public enum BotDifficulty { VeryEasy, Easy, Medium, Hard, Adaptive }

    [Serializable]
    public struct OnboardingBotConfig
    {
        public int           MatchNumber;          // 1-indexed
        public BotDifficulty Difficulty;
        public float         WinProbability;       // 0 = bot always wins, 1 = player always wins
        public float         BotSpeedMultiplier;   // relative to player speed
        public int           BotObstacleHitRate;   // hits per 100 obstacles (0=perfect, 100=crash every obstacle)
        public string        DisplayName;          // shown in matchmaking UI
        public int           DisplayLevel;         // fake level shown to player
    }

    public static readonly OnboardingBotConfig[] BotConfigs = new OnboardingBotConfig[]
    {
        new OnboardingBotConfig
        {
            MatchNumber         = 1,
            Difficulty          = BotDifficulty.VeryEasy,
            WinProbability      = 0.0f,   // bot ALWAYS wins (player guaranteed loss)
            BotSpeedMultiplier  = 1.4f,   // bot runs noticeably faster
            BotObstacleHitRate  = 0,      // bot never hits obstacles
            DisplayName         = "VaultBot_Alpha",
            DisplayLevel        = 5
        },
        new OnboardingBotConfig
        {
            MatchNumber         = 2,
            Difficulty          = BotDifficulty.Easy,
            WinProbability      = 1.0f,   // player ALWAYS wins
            BotSpeedMultiplier  = 0.7f,   // bot runs slower
            BotObstacleHitRate  = 60,     // bot hits 60% of obstacles
            DisplayName         = "NewPlayer_42",
            DisplayLevel        = 2
        },
        new OnboardingBotConfig
        {
            MatchNumber         = 3,
            Difficulty          = BotDifficulty.Medium,
            WinProbability      = 0.6f,   // player wins ~60% of the time
            BotSpeedMultiplier  = 0.95f,  // almost equal speed
            BotObstacleHitRate  = 25,     // bot hits 25% of obstacles
            DisplayName         = "VaultRunner_77",
            DisplayLevel        = 8
        }
    };

    // ─── State ────────────────────────────────────────────────────────────────
    public int  MatchesPlayed    { get; private set; } = 0;
    public bool IsInOnboarding   => MatchesPlayed < ONBOARDING_MATCH_COUNT;
    public int  CurrentMatchNum  => MatchesPlayed + 1;   // 1-indexed

    // ─── Events ───────────────────────────────────────────────────────────────
    public event Action OnOnboardingCompleted;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromPrefs();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the bot config for the current onboarding match.
    /// Returns null if the player has graduated from onboarding.
    /// </summary>
    public OnboardingBotConfig? GetCurrentBotConfig()
    {
        if (!IsInOnboarding) return null;

        int idx = MatchesPlayed;  // 0-based
        if (idx < BotConfigs.Length) return BotConfigs[idx];
        return null;
    }

    /// <summary>
    /// Call after each onboarding match ends (win or loss).
    /// Increments match count and syncs to storage.
    /// </summary>
    public void RecordMatchCompleted()
    {
        if (!IsInOnboarding) return;

        MatchesPlayed++;
        SaveToPrefs();

        Debug.Log($"[Onboarding] Match {MatchesPlayed}/{ONBOARDING_MATCH_COUNT} completed. " +
                  $"InOnboarding={IsInOnboarding}");

        if (!IsInOnboarding)
        {
            Debug.Log("[Onboarding] 🎓 Onboarding complete — switching to normal matchmaking.");
            OnOnboardingCompleted?.Invoke();
            SyncToFirestore();
        }
    }

    /// <summary>
    /// Determines if the player should win this match based on WinProbability.
    /// Call once at match start to pre-determine outcome (prevents mid-match flip).
    /// </summary>
    public bool ShouldPlayerWinThisMatch()
    {
        var config = GetCurrentBotConfig();
        if (config == null) return false;   // graduated, use real matchmaking

        float roll = UnityEngine.Random.value;
        bool  win  = roll <= config.Value.WinProbability;

        Debug.Log($"[Onboarding] Match {CurrentMatchNum}: WinProb={config.Value.WinProbability}, " +
                  $"Roll={roll:F2} → PlayerWins={win}");
        return win;
    }

    // ─── Ghost/Bot Bridge ────────────────────────────────────────────────────

    /// <summary>
    /// Configures the GhostMatchSystem for the current onboarding match.
    /// Call this instead of GhostMatchSystem.StartGhostMatch() during onboarding.
    /// </summary>
    public void StartOnboardingMatch()
    {
        var config = GetCurrentBotConfig();
        if (config == null || GhostMatchSystem.Instance == null) return;

        string difficulty = config.Value.Difficulty switch
        {
            BotDifficulty.VeryEasy => "easy",
            BotDifficulty.Easy     => "easy",
            BotDifficulty.Medium   => "medium",
            _                      => "medium"
        };

        GhostMatchSystem.Instance.StartGhostMatch(difficulty);

        Debug.Log($"[Onboarding] Started match {CurrentMatchNum} — " +
                  $"bot='{config.Value.DisplayName}' (Lv{config.Value.DisplayLevel}), " +
                  $"difficulty={difficulty}");
    }

    // ─── Persistence ──────────────────────────────────────────────────────────

    private void SaveToPrefs()
    {
        PlayerPrefs.SetInt(PREF_MATCHES, MatchesPlayed);
        PlayerPrefs.Save();
    }

    private void LoadFromPrefs()
    {
        MatchesPlayed = PlayerPrefs.GetInt(PREF_MATCHES, 0);
    }

    private void SyncToFirestore()
    {
#if FIREBASE_FIRESTORE
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized) return;
        string uid = FirebaseManager.Instance.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        var db  = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        var doc = db.Collection("players").Document(uid);

        doc.SetAsync(
            new System.Collections.Generic.Dictionary<string, object>
            {
                { "onboardingMatchesPlayed", MatchesPlayed },
                { "onboardingCompleted",     true }
            },
            Firebase.Firestore.SetOptions.MergeAll
        );
#endif
    }
}
