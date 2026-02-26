using System;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_FIRESTORE
using Firebase.Firestore;
using Firebase.Extensions;
#endif

/// <summary>
/// WinStreakService — Tracks consecutive wins and applies the 1.5× coin bonus.
///
/// RULES:
///   • 3+ consecutive wins → coins earned × 1.5
///   • Streak resets on: loss / quit / 4-hour inactivity gap
///   • "longestWinStreak" stat tracked on player profile
///   • UI badge: "🔥 Streak ×1.5" shown in top bar / victory screen
///
/// FIRESTORE SCHEMA:
///   players/{uid}/
///     winStreak         : int    (current streak)
///     longestWinStreak  : int
///     lastWinTimestamp  : timestamp
///
/// LOCAL FALLBACK:
///   PlayerPrefs keys: VaultDash_WinStreak, VaultDash_LongestWinStreak, VaultDash_LastWinTs
///
/// USAGE:
///   // After a match ends:
///   WinStreakService.Instance.RecordMatchResult(won: true);
///   float multiplier = WinStreakService.Instance.CoinMultiplier;
///
///   // In VictoryScreen / CoinReward:
///   int coins = baseCoins * (int)WinStreakService.Instance.CoinMultiplier;
/// </summary>
public class WinStreakService : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static WinStreakService Instance { get; private set; }

    // ─── Constants ────────────────────────────────────────────────────────────
    private const int   STREAK_THRESHOLD       = 3;        // wins needed to activate bonus
    private const float STREAK_MULTIPLIER      = 1.5f;
    private const float INACTIVITY_RESET_HOURS = 4f;

    private const string PREF_STREAK     = "VaultDash_WinStreak";
    private const string PREF_LONGEST    = "VaultDash_LongestWinStreak";
    private const string PREF_LAST_WIN   = "VaultDash_LastWinTs";    // Unix seconds

    // ─── Events ───────────────────────────────────────────────────────────────
    /// Fired when streak changes (newStreak value).
    public event Action<int>   OnStreakChanged;
    /// Fired when streak resets.
    public event Action        OnStreakReset;
    /// Fired when bonus activates (streak hits ≥ 3).
    public event Action        OnStreakBonusActivated;

    // ─── State ────────────────────────────────────────────────────────────────
    public int   CurrentStreak    { get; private set; } = 0;
    public int   LongestStreak    { get; private set; } = 0;
    public float CoinMultiplier   => CurrentStreak >= STREAK_THRESHOLD ? STREAK_MULTIPLIER : 1f;
    public bool  IsStreakActive   => CurrentStreak >= STREAK_THRESHOLD;

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
    /// Call at the end of every match.
    /// <param name="won">True if player won; false if they lost or quit.</param>
    /// </summary>
    public void RecordMatchResult(bool won)
    {
        // Check inactivity gap first
        if (won && IsInactivityExpired())
        {
            Debug.Log("[WinStreak] 4h gap detected — streak reset before recording win.");
            ResetStreak();
        }

        if (won)
        {
            CurrentStreak++;
            if (CurrentStreak > LongestStreak) LongestStreak = CurrentStreak;
            SaveLastWinTimestamp();

            Debug.Log($"[WinStreak] Win! Streak={CurrentStreak}, Multiplier={CoinMultiplier}×");
            OnStreakChanged?.Invoke(CurrentStreak);

            if (CurrentStreak == STREAK_THRESHOLD)
                OnStreakBonusActivated?.Invoke();
        }
        else
        {
            if (CurrentStreak > 0)
            {
                Debug.Log($"[WinStreak] Loss/quit — streak {CurrentStreak} reset.");
                ResetStreak();
            }
        }

        SaveToPrefs();
        SyncToFirestore();
    }

    /// <summary>
    /// Applies the streak multiplier to a base coin amount.
    /// Returns base coins if streak is below threshold.
    /// </summary>
    public int ApplyMultiplier(int baseCoins)
    {
        if (!IsStreakActive) return baseCoins;
        return Mathf.RoundToInt(baseCoins * STREAK_MULTIPLIER);
    }

    /// <summary>
    /// Returns a localised badge string for the HUD.
    /// Returns empty string if no streak bonus.
    /// </summary>
    public string GetBadgeText()
    {
        if (!IsStreakActive) return string.Empty;
        return $"🔥 Streak ×{STREAK_MULTIPLIER}";
    }

    // ─── Internal ─────────────────────────────────────────────────────────────

    private void ResetStreak()
    {
        CurrentStreak = 0;
        OnStreakChanged?.Invoke(0);
        OnStreakReset?.Invoke();
    }

    private bool IsInactivityExpired()
    {
        long lastTs = (long)PlayerPrefs.GetFloat(PREF_LAST_WIN, 0f);
        if (lastTs == 0) return false;   // no prior win

        long nowTs    = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        float gapHours = (nowTs - lastTs) / 3600f;
        return gapHours >= INACTIVITY_RESET_HOURS;
    }

    private void SaveLastWinTimestamp()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetFloat(PREF_LAST_WIN, now);
    }

    // ─── Persistence ──────────────────────────────────────────────────────────

    private void SaveToPrefs()
    {
        PlayerPrefs.SetInt(PREF_STREAK,  CurrentStreak);
        PlayerPrefs.SetInt(PREF_LONGEST, LongestStreak);
        PlayerPrefs.Save();
    }

    private void LoadFromPrefs()
    {
        CurrentStreak = PlayerPrefs.GetInt(PREF_STREAK,  0);
        LongestStreak = PlayerPrefs.GetInt(PREF_LONGEST, 0);

        // Check whether the loaded streak should be reset due to inactivity
        if (CurrentStreak > 0 && IsInactivityExpired())
        {
            Debug.Log("[WinStreak] Loaded streak expired due to inactivity — resetting.");
            ResetStreak();
            SaveToPrefs();
        }
    }

    private void SyncToFirestore()
    {
#if FIREBASE_FIRESTORE
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized) return;

        string uid = FirebaseManager.Instance.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        var db  = FirebaseFirestore.DefaultInstance;
        var doc = db.Collection("players").Document(uid);

        var data = new Dictionary<string, object>
        {
            { "winStreak",        CurrentStreak },
            { "longestWinStreak", LongestStreak },
            { "lastWinTimestamp", Timestamp.GetCurrentTimestamp() }
        };

        doc.SetAsync(data, SetOptions.MergeAll)
           .ContinueWithOnMainThread(t =>
           {
               if (t.IsFaulted)
                   Debug.LogWarning($"[WinStreak] Firestore sync failed: {t.Exception?.Message}");
           });
#endif
    }
}
