using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_FIRESTORE
using Firebase.Firestore;
using Firebase.Extensions;
#endif

/// <summary>
/// ChallengeSystem — Daily randomised challenges, rotating at 09:00 UTC.
///
/// DAILY CHALLENGE RULES:
///   • 3 active challenges per day
///   • Rotate daily at 09:00 UTC
///   • Seeded by date so all players get SAME challenges each day
///   • Types: Win 3 | Deal 1000 Damage | Collect 5 Chests | Win with Character X | Earn 1000 Coins
///   • Rewards: XP 100-300 + Coins 200-500 per challenge
///
/// FIRESTORE SCHEMA:
///   players/{uid}/dailyChallenges/{YYYY-MM-DD}/
///     generatedAt   : timestamp
///     challenges    : array of ChallengeRecord
///
/// LOCAL CACHE:
///   PlayerPrefs key: VaultDash_DailyChallenges_{YYYY-MM-DD}
///
/// USAGE:
///   // Load today's challenges on game start:
///   ChallengeSystem.Instance.LoadTodayChallenges(onLoaded);
///
///   // Report progress events:
///   ChallengeSystem.Instance.ReportEvent(ChallengeEventType.Win, value: 1);
///   ChallengeSystem.Instance.ReportEvent(ChallengeEventType.DamageDealt, value: 250);
/// </summary>
public class ChallengeSystem : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static ChallengeSystem Instance { get; private set; }

    // ─── Constants ────────────────────────────────────────────────────────────
    private const int   CHALLENGES_PER_DAY     = 3;
    private const int   ROTATION_HOUR_UTC      = 9;   // 09:00 UTC

    // ─── Challenge Types ──────────────────────────────────────────────────────
    public enum ChallengeEventType
    {
        Win,
        DamageDealt,
        ChestCollected,
        WinWithCharacter,
        CoinsEarned
    }

    [Serializable]
    public class ChallengeTemplate
    {
        public string            Id;
        public string            DisplayText;    // e.g. "Win 3 matches"
        public ChallengeEventType EventType;
        public int               TargetValue;
        public string            CharacterFilter; // Only for WinWithCharacter (null = any)
        public int               XpReward;
        public int               CoinReward;
    }

    [Serializable]
    public class ChallengeRecord
    {
        public string             TemplateId;
        public string             DisplayText;
        public ChallengeEventType EventType;
        public int                TargetValue;
        public int                CurrentProgress;
        public bool               Completed;
        public bool               RewardClaimed;
        public int                XpReward;
        public int                CoinReward;
        public string             CharacterFilter;

        public float ProgressPercent => TargetValue > 0
            ? Mathf.Clamp01((float)CurrentProgress / TargetValue)
            : 0f;
    }

    // ─── Template Pool ────────────────────────────────────────────────────────
    private static readonly ChallengeTemplate[] TemplatePool = new ChallengeTemplate[]
    {
        new ChallengeTemplate { Id = "win_3",      DisplayText = "Win 3 matches",              EventType = ChallengeEventType.Win,              TargetValue = 3,    XpReward = 200, CoinReward = 300 },
        new ChallengeTemplate { Id = "win_5",      DisplayText = "Win 5 matches",              EventType = ChallengeEventType.Win,              TargetValue = 5,    XpReward = 300, CoinReward = 500 },
        new ChallengeTemplate { Id = "dmg_1000",   DisplayText = "Deal 1000 damage",            EventType = ChallengeEventType.DamageDealt,      TargetValue = 1000, XpReward = 150, CoinReward = 200 },
        new ChallengeTemplate { Id = "dmg_2500",   DisplayText = "Deal 2500 damage",            EventType = ChallengeEventType.DamageDealt,      TargetValue = 2500, XpReward = 250, CoinReward = 400 },
        new ChallengeTemplate { Id = "chest_5",    DisplayText = "Collect 5 chests",            EventType = ChallengeEventType.ChestCollected,   TargetValue = 5,    XpReward = 100, CoinReward = 200 },
        new ChallengeTemplate { Id = "chest_10",   DisplayText = "Collect 10 chests",           EventType = ChallengeEventType.ChestCollected,   TargetValue = 10,   XpReward = 200, CoinReward = 350 },
        new ChallengeTemplate { Id = "win_agz",    DisplayText = "Win with Agent Zero",         EventType = ChallengeEventType.WinWithCharacter, TargetValue = 1,    CharacterFilter = "AgentZero", XpReward = 150, CoinReward = 250 },
        new ChallengeTemplate { Id = "win_blaze",  DisplayText = "Win with Blaze",              EventType = ChallengeEventType.WinWithCharacter, TargetValue = 1,    CharacterFilter = "Blaze",     XpReward = 150, CoinReward = 250 },
        new ChallengeTemplate { Id = "win_knox",   DisplayText = "Win with Knox",               EventType = ChallengeEventType.WinWithCharacter, TargetValue = 1,    CharacterFilter = "Knox",      XpReward = 150, CoinReward = 250 },
        new ChallengeTemplate { Id = "earn_1000",  DisplayText = "Earn 1000 coins in matches",  EventType = ChallengeEventType.CoinsEarned,      TargetValue = 1000, XpReward = 120, CoinReward = 200 },
        new ChallengeTemplate { Id = "earn_3000",  DisplayText = "Earn 3000 coins in matches",  EventType = ChallengeEventType.CoinsEarned,      TargetValue = 3000, XpReward = 280, CoinReward = 450 },
    };

    // ─── Events ───────────────────────────────────────────────────────────────
    public event Action<List<ChallengeRecord>>  OnChallengesLoaded;
    public event Action<ChallengeRecord>        OnChallengeCompleted;
    public event Action<ChallengeRecord, int, int> OnRewardClaimed;  // challenge, xp, coins

    // ─── State ────────────────────────────────────────────────────────────────
    public List<ChallengeRecord> TodayChallenges { get; private set; } = new List<ChallengeRecord>();
    public string                TodayDateKey    => GetTodayDateKey();

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Loads today's challenges from Firestore (or generates them if needed).
    /// </summary>
    public void LoadTodayChallenges(Action<List<ChallengeRecord>> onLoaded = null)
    {
#if FIREBASE_FIRESTORE
        LoadFromFirestore(TodayDateKey, list =>
        {
            TodayChallenges = list;
            onLoaded?.Invoke(list);
            OnChallengesLoaded?.Invoke(list);
        });
#else
        var list = GenerateTodayChallenges();
        TodayChallenges = list;
        onLoaded?.Invoke(list);
        OnChallengesLoaded?.Invoke(list);
#endif
    }

    /// <summary>
    /// Reports a game event that may advance challenge progress.
    /// Call this from MatchManager / GameManager at relevant moments.
    /// </summary>
    /// <param name="eventType">Type of event that occurred.</param>
    /// <param name="value">Event magnitude (damage amount, coin count, etc.)</param>
    /// <param name="characterId">For WinWithCharacter events.</param>
    public void ReportEvent(ChallengeEventType eventType, int value = 1, string characterId = null)
    {
        bool anyUpdated = false;

        foreach (var ch in TodayChallenges)
        {
            if (ch.Completed) continue;
            if (ch.EventType != eventType) continue;

            // Character filter check
            if (eventType == ChallengeEventType.WinWithCharacter &&
                !string.IsNullOrEmpty(ch.CharacterFilter) &&
                !string.Equals(ch.CharacterFilter, characterId, StringComparison.OrdinalIgnoreCase))
                continue;

            ch.CurrentProgress = Mathf.Min(ch.CurrentProgress + value, ch.TargetValue);
            anyUpdated = true;

            if (ch.CurrentProgress >= ch.TargetValue && !ch.Completed)
            {
                ch.Completed = true;
                Debug.Log($"[Challenges] ✅ Completed: {ch.DisplayText}");
                OnChallengeCompleted?.Invoke(ch);
            }
        }

        if (anyUpdated) SaveProgressLocally();
    }

    /// <summary>Claims the reward for a completed challenge.</summary>
    public bool ClaimReward(string templateId)
    {
        var ch = TodayChallenges.Find(c => c.TemplateId == templateId);
        if (ch == null || !ch.Completed || ch.RewardClaimed) return false;

        ch.RewardClaimed = true;
        SaveProgressLocally();
        SyncProgressToFirestore();

        // Grant rewards
        FirebaseManager.Instance?.GrantXP(ch.XpReward, $"challenge_{templateId}");
        FirebaseManager.Instance?.GrantCoins(ch.CoinReward, $"challenge_{templateId}");

        Debug.Log($"[Challenges] 🎁 Reward claimed: {ch.XpReward} XP + {ch.CoinReward} coins for '{ch.DisplayText}'");
        OnRewardClaimed?.Invoke(ch, ch.XpReward, ch.CoinReward);
        return true;
    }

    // ─── Challenge Generation ─────────────────────────────────────────────────

    /// <summary>
    /// Generates today's 3 challenges using a deterministic seed from the date.
    /// All players see the same challenges on the same day.
    /// </summary>
    private List<ChallengeRecord> GenerateTodayChallenges()
    {
        string dateKey = TodayDateKey;
        int    seed    = dateKey.GetHashCode();   // deterministic per day

        var rng       = new System.Random(seed);
        var pool      = new List<ChallengeTemplate>(TemplatePool);
        var chosen    = new List<ChallengeRecord>();

        // Try to pick 3 distinct types
        var usedTypes = new HashSet<ChallengeEventType>();

        while (chosen.Count < CHALLENGES_PER_DAY && pool.Count > 0)
        {
            int idx = rng.Next(pool.Count);
            var tmpl = pool[idx];
            pool.RemoveAt(idx);

            // Prefer diverse types; allow repeats only if pool runs dry
            if (usedTypes.Contains(tmpl.EventType) && chosen.Count < pool.Count)
                continue;

            usedTypes.Add(tmpl.EventType);
            chosen.Add(new ChallengeRecord
            {
                TemplateId      = tmpl.Id,
                DisplayText     = tmpl.DisplayText,
                EventType       = tmpl.EventType,
                TargetValue     = tmpl.TargetValue,
                CurrentProgress = 0,
                Completed       = false,
                RewardClaimed   = false,
                XpReward        = tmpl.XpReward,
                CoinReward      = tmpl.CoinReward,
                CharacterFilter = tmpl.CharacterFilter
            });
        }

        Debug.Log($"[Challenges] Generated {chosen.Count} challenges for {dateKey}:");
        foreach (var c in chosen)
            Debug.Log($"  • {c.DisplayText} (+{c.XpReward}xp, +{c.CoinReward}coins)");

        return chosen;
    }

    // ─── Date Key ─────────────────────────────────────────────────────────────

    private string GetTodayDateKey()
    {
        // Use UTC time; challenges rotate at 09:00 UTC
        var now = DateTime.UtcNow;
        var rotationTime = new DateTime(now.Year, now.Month, now.Day, ROTATION_HOUR_UTC, 0, 0, DateTimeKind.Utc);

        // Before 09:00 UTC → still "yesterday's" challenges
        var effectiveDate = now < rotationTime ? now.AddDays(-1) : now;
        return effectiveDate.ToString("yyyy-MM-dd");
    }

    // ─── Persistence ──────────────────────────────────────────────────────────

    private void SaveProgressLocally()
    {
        // Serialise progress as JSON to PlayerPrefs
        var wrapper = new ProgressWrapper { challenges = TodayChallenges };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString($"VaultDash_DailyChallenges_{TodayDateKey}", json);
        PlayerPrefs.Save();
    }

    private bool TryLoadProgressLocally(out List<ChallengeRecord> challenges)
    {
        string key  = $"VaultDash_DailyChallenges_{TodayDateKey}";
        string json = PlayerPrefs.GetString(key, null);

        if (!string.IsNullOrEmpty(json))
        {
            var wrapper = JsonUtility.FromJson<ProgressWrapper>(json);
            challenges = wrapper.challenges ?? new List<ChallengeRecord>();
            return true;
        }

        challenges = null;
        return false;
    }

    [Serializable]
    private class ProgressWrapper
    {
        public List<ChallengeRecord> challenges;
    }

    // ─── Firestore ────────────────────────────────────────────────────────────

#if FIREBASE_FIRESTORE
    private void LoadFromFirestore(string dateKey, Action<List<ChallengeRecord>> onDone)
    {
        string uid = FirebaseManager.Instance?.UserId;
        if (string.IsNullOrEmpty(uid)) { onDone?.Invoke(GenerateTodayChallenges()); return; }

        var db  = FirebaseFirestore.DefaultInstance;
        var doc = db.Collection("players").Document(uid)
                    .Collection("dailyChallenges").Document(dateKey);

        doc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                // Generate fresh challenges and save them
                var fresh = GenerateTodayChallenges();
                SaveProgressLocally();
                SyncProgressToFirestore();
                onDone?.Invoke(fresh);
                return;
            }

            // Deserialise
            if (TryLoadProgressLocally(out var cached))
            {
                onDone?.Invoke(cached);
            }
            else
            {
                onDone?.Invoke(GenerateTodayChallenges());
            }
        });
    }

    private void SyncProgressToFirestore()
    {
        string uid = FirebaseManager.Instance?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        // Convert to plain objects for Firestore
        var challengeData = new List<object>();
        foreach (var ch in TodayChallenges)
        {
            challengeData.Add(new Dictionary<string, object>
            {
                { "templateId",      ch.TemplateId },
                { "displayText",     ch.DisplayText },
                { "targetValue",     ch.TargetValue },
                { "currentProgress", ch.CurrentProgress },
                { "completed",       ch.Completed },
                { "rewardClaimed",   ch.RewardClaimed }
            });
        }

        var db  = FirebaseFirestore.DefaultInstance;
        var doc = db.Collection("players").Document(uid)
                    .Collection("dailyChallenges").Document(TodayDateKey);

        doc.SetAsync(new Dictionary<string, object>
        {
            { "generatedAt", Timestamp.GetCurrentTimestamp() },
            { "challenges",  challengeData }
        }, SetOptions.MergeAll);
    }
#endif
}
