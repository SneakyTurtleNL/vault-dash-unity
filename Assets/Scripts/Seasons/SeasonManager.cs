using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_FIRESTORE
using Firebase.Firestore;
using Firebase.Extensions;
#endif

/// <summary>
/// SeasonManager — Singleton: tracks the active season, listens for season changes,
/// orchestrates season end (trophy reset → reward calculation → archival).
///
/// SEASON CYCLE:
///   Season 1 starts 2026-02-21.  Duration = 30 days (configurable via Firestore).
///   Hard reset: ALL players' trophies → 0 at endDate 00:00 UTC.
///   Prestige / cards / gems / cosmetics are NEVER reset.
///
/// FIRESTORE PATHS (see SEASON_SCHEMA.md):
///   config/currentSeason          — live pointer to active season
///   config/seasons/{seasonId}     — full season config
///   players/{uid}/seasons/{seasonId} — per-player season record
///   leaderboards/season/{seasonId}/top100 — archived top-100
///
/// EVENTS (subscribe from UI code):
///   OnSeasonChanged(SeasonInfo newSeason)          — new season went live
///   OnSeasonRewardCalculated(int gems, SeasonInfo) — player reward ready to claim
///   OnSeasonEndingSoon(SeasonInfo, TimeSpan)        — < 24h warning
///
/// SEASON REWARD FORMULA:
///   gems = floor(peakTrophies / 100), capped at 500.
///   Plus tier bonus: Legend +50, Master +25, Diamond +10.
///   Reward claimed once per season via Firestore transaction.
///
/// SETUP:
///   1. Add FIREBASE_FIRESTORE to Scripting Define Symbols.
///   2. Place SeasonManager on a persistent GameObject (DontDestroyOnLoad).
///   3. Subscribe to events from MainMenuScreen, ProfileScreen etc.
/// </summary>
public class SeasonManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static SeasonManager Instance { get; private set; }

    // ─── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired when a new season becomes active (app start or live switch).</summary>
    public event Action<SeasonInfo> OnSeasonChanged;

    /// <summary>Fired when the season reward has been calculated and is ready to claim.</summary>
    public event Action<int, SeasonInfo> OnSeasonRewardCalculated;  // (gems, oldSeason)

    /// <summary>Fired once when < 24h remain in the current season.</summary>
    public event Action<SeasonInfo, TimeSpan> OnSeasonEndingSoon;

    // ─── State ────────────────────────────────────────────────────────────────
    public SeasonInfo   CurrentSeason    { get; private set; }
    public bool         IsInitialized    { get; private set; }

    /// <summary>Player's peak trophies this season (local cache from Firestore).</summary>
    public int PeakTrophiesThisSeason { get; private set; }

    // ─── PlayerPrefs Keys ─────────────────────────────────────────────────────
    private const string PP_SEASON_ID          = "VaultDash_CurrentSeasonId";
    private const string PP_PEAK_TROPHIES      = "VaultDash_PeakTrophiesThisSeason";
    private const string PP_ENDING_SOON_FIRED  = "VaultDash_SeasonEndingSoonFired";

    // ─── Firestore config paths ────────────────────────────────────────────────
    private const string PATH_CURRENT_SEASON = "config/currentSeason";
    private const string COL_SEASONS         = "config/seasons";     // {seasonId}
    private const string COL_PLAYERS         = "players";
    private const string COL_LEADERBOARD_ROOT= "leaderboards/season"; // /{seasonId}/top100/{uid}

#if FIREBASE_FIRESTORE
    private ListenerRegistration _seasonListener;
#endif

    // ─── Countdown coroutine ──────────────────────────────────────────────────
    private Coroutine _countdownCoroutine;

    // ─── Init ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadLocalCache();
        StartCoroutine(InitializeFromFirestore());
    }

    void OnDestroy()
    {
#if FIREBASE_FIRESTORE
        _seasonListener?.Stop();
#endif
        if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
    }

    // ─── Local cache ──────────────────────────────────────────────────────────

    void LoadLocalCache()
    {
        PeakTrophiesThisSeason = PlayerPrefs.GetInt(PP_PEAK_TROPHIES, 0);
    }

    void SaveLocalCache()
    {
        if (CurrentSeason != null)
            PlayerPrefs.SetString(PP_SEASON_ID, CurrentSeason.seasonId);
        PlayerPrefs.SetInt(PP_PEAK_TROPHIES, PeakTrophiesThisSeason);
        PlayerPrefs.Save();
    }

    // ─── Firestore Initialization ─────────────────────────────────────────────

    IEnumerator InitializeFromFirestore()
    {
        // Wait for FirebaseManager if present
        if (FirebaseManager.Instance != null)
        {
            yield return new WaitUntil(() => FirebaseManager.Instance.IsInitialized);
        }

#if FIREBASE_FIRESTORE
        yield return StartCoroutine(LoadCurrentSeasonFromFirestore());
        AttachSeasonListener();
#else
        Debug.Log("[SeasonManager] Firestore not enabled — using stub season data.");
        CurrentSeason = CreateStubSeason();
        IsInitialized = true;
        OnSeasonChanged?.Invoke(CurrentSeason);
#endif

        _countdownCoroutine = StartCoroutine(CountdownLoop());
    }

#if FIREBASE_FIRESTORE
    IEnumerator LoadCurrentSeasonFromFirestore()
    {
        var db     = FirebaseFirestore.DefaultInstance;
        var parts  = PATH_CURRENT_SEASON.Split('/');
        var docRef = db.Collection(parts[0]).Document(parts[1]);
        var task   = docRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogWarning($"[SeasonManager] Could not load currentSeason: {task.Exception.Message}");
            CurrentSeason = CreateStubSeason();
            IsInitialized = true;
            yield break;
        }

        var snap = task.Result;
        if (!snap.Exists)
        {
            Debug.Log("[SeasonManager] config/currentSeason not found — using stub.");
            CurrentSeason = CreateStubSeason();
            IsInitialized = true;
            yield break;
        }

        string seasonId = snap.ContainsField("seasonId") ? snap.GetValue<string>("seasonId") : null;
        if (string.IsNullOrEmpty(seasonId))
        {
            CurrentSeason = CreateStubSeason();
            IsInitialized = true;
            yield break;
        }

        yield return StartCoroutine(LoadSeasonDoc(seasonId, season =>
        {
            CurrentSeason = season ?? CreateStubSeason();
            IsInitialized = true;
            OnSeasonChanged?.Invoke(CurrentSeason);
            Debug.Log($"[SeasonManager] ✅ Active season: {CurrentSeason}");
        }));

        // Also load player's peak trophies for this season
        yield return StartCoroutine(LoadPlayerSeasonPeak(seasonId));
    }

    IEnumerator LoadSeasonDoc(string seasonId, Action<SeasonInfo> callback)
    {
        var db      = FirebaseFirestore.DefaultInstance;
        var docRef  = db.Collection("config").Document("seasons")
                        .Collection(seasonId).Document(seasonId);
        // Alternative path: config/seasons/{seasonId}
        var altRef  = db.Collection("config").Document("seasons");
        // Use actual path: config > seasons > {seasonId} (nested collection pattern)
        // Firestore: config/seasons is a document that has subcollection — 
        // or config/seasons/{seasonId} is top-level document.
        // Per schema: `config/seasons/{seasonId}` means collection "config", doc "seasons" is wrong.
        // Correct: collection "seasons" under "config" parent → use /{seasonId} doc in subcollection.
        // Re-interpret: collection path = "config/seasons" → not valid.
        // Best: top-level collection "seasons" (simpler), id = seasonId.
        var seasonRef = db.Collection("seasons").Document(seasonId);
        var task      = seasonRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogWarning($"[SeasonManager] LoadSeasonDoc error: {task.Exception.Message}");
            callback(null);
            yield break;
        }

        var snap = task.Result;
        if (!snap.Exists)
        {
            Debug.Log($"[SeasonManager] Season doc '{seasonId}' not found.");
            callback(null);
            yield break;
        }

        callback(ParseSeasonDoc(snap));
    }

    // ─── Real-time Listener ───────────────────────────────────────────────────

    void AttachSeasonListener()
    {
        var db    = FirebaseFirestore.DefaultInstance;
        var parts = PATH_CURRENT_SEASON.Split('/');
        var ref_  = db.Collection(parts[0]).Document(parts[1]);

        _seasonListener = ref_.Listen(snap =>
        {
            if (!snap.Exists) return;

            string newSeasonId = snap.ContainsField("seasonId")
                ? snap.GetValue<string>("seasonId") : null;

            if (string.IsNullOrEmpty(newSeasonId)) return;

            // Season changed?
            string prevId = CurrentSeason?.seasonId;
            if (newSeasonId != prevId)
            {
                Debug.Log($"[SeasonManager] 🔄 Season change detected: {prevId} → {newSeasonId}");
                StartCoroutine(HandleSeasonChange(prevId, newSeasonId));
            }
        });

        Debug.Log("[SeasonManager] Firestore listener attached to config/currentSeason.");
    }

    IEnumerator HandleSeasonChange(string oldSeasonId, string newSeasonId)
    {
        // If there was a previous season, fire end logic
        if (!string.IsNullOrEmpty(oldSeasonId) && CurrentSeason != null)
        {
            Debug.Log($"[SeasonManager] 🏁 Season ended: {oldSeasonId}");
            yield return StartCoroutine(OnSeasonEnded(CurrentSeason));
        }

        // Load new season
        yield return StartCoroutine(LoadSeasonDoc(newSeasonId, newSeason =>
        {
            CurrentSeason = newSeason ?? CreateStubSeason();
            PeakTrophiesThisSeason = 0;        // reset for new season
            PlayerPrefs.SetInt(PP_ENDING_SOON_FIRED, 0);
            SaveLocalCache();
            OnSeasonChanged?.Invoke(CurrentSeason);
            Debug.Log($"[SeasonManager] ✅ New season live: {CurrentSeason}");
        }));

        yield return StartCoroutine(LoadPlayerSeasonPeak(newSeasonId));
    }

    // ─── Season Ended Logic ───────────────────────────────────────────────────

    /// <summary>
    /// Called when Firestore listener detects a season change.
    /// Records peak trophies, resets trophies to 0, calculates gem reward.
    /// The actual Firestore write is done via Cloud Function (checkSeasonEnd);
    /// this client-side method handles the local state + reward notification.
    /// </summary>
    IEnumerator OnSeasonEnded(SeasonInfo endedSeason)
    {
        Debug.Log($"[SeasonManager] Processing season end for: {endedSeason.seasonId}");

        string uid = GetCurrentUID();
        if (string.IsNullOrEmpty(uid)) yield break;

        int peakTrophies  = PeakTrophiesThisSeason;
        int currentPresti = RankedProgressionManager.Instance?.State.prestigeLevel ?? 0;
        string finalTier  = RankedProgressionManager.Instance?.State.currentTier.name ?? "Rookie";

        // Calculate gem reward
        int gemsEarned = PlayerSeasonRecord.CalculateGemReward(peakTrophies);
        Debug.Log($"[SeasonManager] 💎 Season reward: {gemsEarned} gems (peak: {peakTrophies} trophies)");

        // Write season record to Firestore (players/{uid}/seasons/{seasonId})
        yield return StartCoroutine(WritePlayerSeasonRecord(uid, endedSeason.seasonId,
            peakTrophies, finalTier, currentPresti, gemsEarned));

        // Notify UI
        OnSeasonRewardCalculated?.Invoke(gemsEarned, endedSeason);

        // Reset local peak trophies
        PeakTrophiesThisSeason = 0;
        SaveLocalCache();

        // Note: Trophy hard reset to 0 is done SERVER-SIDE by Cloud Function (hardSeasonReset).
        // RankedProgressionManager will sync from Firestore on next load.
    }

    IEnumerator WritePlayerSeasonRecord(
        string uid, string seasonId,
        int peakTrophies, string finalTier, int finalPrestige, int gemsEarned)
    {
#if FIREBASE_FIRESTORE
        var db        = FirebaseFirestore.DefaultInstance;
        var recordRef = db.Collection("players").Document(uid)
                          .Collection("seasons").Document(seasonId);

        // Transaction to prevent double-claim
        var transaction = db.RunTransactionAsync(tx =>
        {
            return tx.GetSnapshotAsync(recordRef).ContinueWith(t =>
            {
                var snap = t.Result;
                // Only write if not already recorded
                if (!snap.Exists || !snap.ContainsField("peakTrophies"))
                {
                    var data = new Dictionary<string, object>
                    {
                        { "seasonId",             seasonId      },
                        { "peakTrophies",         peakTrophies  },
                        { "finalTier",            finalTier     },
                        { "finalPrestige",        finalPrestige },
                        { "claimedSeasonReward",  false         },
                        { "gemReward",            gemsEarned    },
                    };
                    tx.Set(recordRef, data);
                }
            });
        });

        yield return new WaitUntil(() => transaction.IsCompleted);

        if (transaction.Exception != null)
            Debug.LogWarning($"[SeasonManager] WritePlayerSeasonRecord error: {transaction.Exception.Message}");
        else
            Debug.Log($"[SeasonManager] ✅ Player season record saved: {seasonId}");
#else
        yield return null;
#endif
    }

    // ─── Claim Season Reward ──────────────────────────────────────────────────

    /// <summary>
    /// Claim gems for a completed season. Firestore transaction prevents double-claim.
    /// Returns actual gems awarded (0 if already claimed or error).
    /// </summary>
    public IEnumerator ClaimSeasonReward(string seasonId, Action<int> onComplete)
    {
        string uid = GetCurrentUID();
        if (string.IsNullOrEmpty(uid)) { onComplete?.Invoke(0); yield break; }

        int awarded = 0;

#if FIREBASE_FIRESTORE
        var db        = FirebaseFirestore.DefaultInstance;
        var recordRef = db.Collection("players").Document(uid)
                          .Collection("seasons").Document(seasonId);
        var playerRef = db.Collection("players").Document(uid);

        var transaction = db.RunTransactionAsync(tx =>
        {
            return tx.GetSnapshotAsync(recordRef).ContinueWith(snapTask =>
            {
                var snap = snapTask.Result;
                if (!snap.Exists)
                    return System.Threading.Tasks.Task.CompletedTask;

                bool claimed = snap.ContainsField("claimedSeasonReward")
                               && snap.GetValue<bool>("claimedSeasonReward");
                if (claimed)
                    return System.Threading.Tasks.Task.CompletedTask;

                int gems = snap.ContainsField("gemReward") ? snap.GetValue<int>("gemReward") : 0;
                awarded = gems;

                // Mark claimed
                tx.Update(recordRef, new Dictionary<string, object>
                {
                    { "claimedSeasonReward", true },
                    { "claimedAt", FieldValue.ServerTimestamp },
                });

                // Award gems to player
                if (gems > 0)
                {
                    tx.Update(playerRef, new Dictionary<string, object>
                    {
                        { "gems", FieldValue.Increment(gems) },
                    });
                }

                return System.Threading.Tasks.Task.CompletedTask;
            }).Unwrap();
        });

        yield return new WaitUntil(() => transaction.IsCompleted);

        if (transaction.Exception != null)
        {
            Debug.LogWarning($"[SeasonManager] ClaimSeasonReward error: {transaction.Exception.Message}");
            awarded = 0;
        }
        else
        {
            Debug.Log($"[SeasonManager] ✅ Claimed {awarded} gems for season {seasonId}");
        }
#else
        Debug.Log($"[SeasonManager] (stub) ClaimSeasonReward: {seasonId}");
        yield return null;
#endif

        onComplete?.Invoke(awarded);
    }

    // ─── Peak Trophy Tracking ─────────────────────────────────────────────────

    /// <summary>
    /// Called by RankedProgressionManager after every match to keep peak updated.
    /// Updates both local cache and Firestore atomically.
    /// </summary>
    public void UpdatePeakTrophies(int currentTrophies)
    {
        if (CurrentSeason == null || !CurrentSeason.IsActive) return;
        if (currentTrophies <= PeakTrophiesThisSeason) return;

        PeakTrophiesThisSeason = currentTrophies;
        SaveLocalCache();
        StartCoroutine(PushPeakTrophiesToFirestore(currentTrophies));
    }

    IEnumerator PushPeakTrophiesToFirestore(int peakTrophies)
    {
#if FIREBASE_FIRESTORE
        string uid = GetCurrentUID();
        if (string.IsNullOrEmpty(uid) || CurrentSeason == null) yield break;

        var db     = FirebaseFirestore.DefaultInstance;
        var ref_   = db.Collection("players").Document(uid);
        var task   = ref_.UpdateAsync(new Dictionary<string, object>
        {
            { "peakTrophiesThisSeason", peakTrophies         },
            { "currentSeasonId",        CurrentSeason.seasonId },
        });
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
            Debug.LogWarning($"[SeasonManager] PushPeakTrophies error: {task.Exception.Message}");
#else
        yield return null;
#endif
    }

    IEnumerator LoadPlayerSeasonPeak(string seasonId)
    {
#if FIREBASE_FIRESTORE
        string uid = GetCurrentUID();
        if (string.IsNullOrEmpty(uid)) yield break;

        var db      = FirebaseFirestore.DefaultInstance;
        var docRef  = db.Collection("players").Document(uid);
        var task    = docRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null || !task.Result.Exists) yield break;

        var snap = task.Result;
        if (snap.ContainsField("currentSeasonId") &&
            snap.GetValue<string>("currentSeasonId") == seasonId &&
            snap.ContainsField("peakTrophiesThisSeason"))
        {
            int peak = snap.GetValue<int>("peakTrophiesThisSeason");
            if (peak > PeakTrophiesThisSeason)
            {
                PeakTrophiesThisSeason = peak;
                PlayerPrefs.SetInt(PP_PEAK_TROPHIES, peak);
                PlayerPrefs.Save();
            }
        }
#else
        yield return null;
#endif
    }

    // ─── Season Archive Leaderboard ───────────────────────────────────────────

    /// <summary>
    /// Loads archived top-100 for a specific past season.
    /// Path: leaderboards/season/{seasonId}/top100/{uid}
    /// </summary>
    public IEnumerator LoadSeasonLeaderboard(
        string seasonId, int limit, Action<List<SeasonLeaderboardEntry>> callback)
    {
        var result = new List<SeasonLeaderboardEntry>();

#if FIREBASE_FIRESTORE
        string uid = GetCurrentUID();
        var db     = FirebaseFirestore.DefaultInstance;
        var colRef = db.Collection("leaderboards")
                       .Document("season")
                       .Collection(seasonId)
                       .Document("top100")
                       .Collection("entries");

        // Alternatively: leaderboards/season/{seasonId}/top100 as a collection
        // Use: collection("leaderboards").document("season").collection(seasonId)
        var altColRef = db.Collection("leaderboards")
                          .Document("season")
                          .Collection(seasonId);

        var task = altColRef
            .OrderBy("rank")
            .Limit(limit)
            .GetSnapshotAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogWarning($"[SeasonManager] LoadSeasonLeaderboard error: {task.Exception.Message}");
            callback(result);
            yield break;
        }

        foreach (var doc in task.Result.Documents)
        {
            var data  = doc.ToDictionary();
            var entry = new SeasonLeaderboardEntry
            {
                uid           = doc.Id,
                rank          = data.ContainsKey("rank")          ? Convert.ToInt32(data["rank"])          : 0,
                username      = data.ContainsKey("username")      ? data["username"].ToString()            : "Unknown",
                trophies      = data.ContainsKey("trophies")      ? Convert.ToInt32(data["trophies"])      : 0,
                tier          = data.ContainsKey("tier")          ? data["tier"].ToString()                : "Rookie",
                prestigeLevel = data.ContainsKey("prestigeLevel") ? Convert.ToInt32(data["prestigeLevel"]) : 0,
                isLocalPlayer = doc.Id == uid,
            };
            result.Add(entry);
        }

        Debug.Log($"[SeasonManager] Loaded {result.Count} entries for season {seasonId}");
#else
        // Stub data for testing
        yield return new WaitForSecondsRealtime(0.3f);
        result = GenerateStubLeaderboard(seasonId);
#endif

        callback(result);
    }

    // ─── Past Season Record for Player ────────────────────────────────────────

    /// <summary>Returns player's record for a past (or current) season from Firestore.</summary>
    public IEnumerator GetPlayerSeasonRecord(
        string seasonId, Action<PlayerSeasonRecord> callback)
    {
#if FIREBASE_FIRESTORE
        string uid = GetCurrentUID();
        if (string.IsNullOrEmpty(uid)) { callback(null); yield break; }

        var db      = FirebaseFirestore.DefaultInstance;
        var docRef  = db.Collection("players").Document(uid)
                        .Collection("seasons").Document(seasonId);
        var task    = docRef.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null || !task.Result.Exists)
        {
            callback(null);
            yield break;
        }

        var data = task.Result.ToDictionary();
        callback(new PlayerSeasonRecord
        {
            seasonId             = seasonId,
            peakTrophies         = data.ContainsKey("peakTrophies")        ? Convert.ToInt32(data["peakTrophies"])        : 0,
            finalTier            = data.ContainsKey("finalTier")           ? data["finalTier"].ToString()                 : "Rookie",
            finalPrestige        = data.ContainsKey("finalPrestige")       ? Convert.ToInt32(data["finalPrestige"])       : 0,
            claimedSeasonReward  = data.ContainsKey("claimedSeasonReward") ? (bool)data["claimedSeasonReward"]            : false,
            gemReward            = data.ContainsKey("gemReward")           ? Convert.ToInt32(data["gemReward"])           : 0,
        });
#else
        yield return null;
        callback(new PlayerSeasonRecord
        {
            seasonId            = seasonId,
            peakTrophies        = PeakTrophiesThisSeason,
            finalTier           = "Gold",
            claimedSeasonReward = false,
            gemReward           = PlayerSeasonRecord.CalculateGemReward(PeakTrophiesThisSeason),
        });
#endif
    }

    // ─── Countdown Loop ───────────────────────────────────────────────────────

    /// <summary>Checks every 5 min for season end / ending-soon events.</summary>
    IEnumerator CountdownLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(300f);  // every 5 minutes

            if (CurrentSeason == null || !IsInitialized) continue;

            // Ending soon?  (< 24h)
            var remaining = CurrentSeason.TimeRemaining;
            if (remaining.TotalHours < 24 && remaining.TotalSeconds > 0)
            {
                int alreadyFired = PlayerPrefs.GetInt(PP_ENDING_SOON_FIRED, 0);
                if (alreadyFired == 0)
                {
                    PlayerPrefs.SetInt(PP_ENDING_SOON_FIRED, 1);
                    PlayerPrefs.Save();
                    Debug.Log($"[SeasonManager] ⏰ Season ending soon: {remaining.Hours}h {remaining.Minutes}m");
                    OnSeasonEndingSoon?.Invoke(CurrentSeason, remaining);
                }
            }

            // Season actually ended but listener hasn't fired yet?
            if (DateTime.UtcNow >= CurrentSeason.endDate && CurrentSeason.IsActive == false)
            {
                Debug.Log("[SeasonManager] ⚡ Season expired — refreshing from Firestore.");
#if FIREBASE_FIRESTORE
                yield return StartCoroutine(LoadCurrentSeasonFromFirestore());
#endif
            }
        }
    }

    // ─── Season Cosmetic Helpers ──────────────────────────────────────────────

    /// <summary>
    /// Check whether the local player has unlocked the current season's exclusive skin.
    /// Conditions: prestige >= cosmetic.prestigeFree during this season.
    /// </summary>
    public bool PlayerEarnedSeasonSkin()
    {
        if (CurrentSeason?.cosmetic == null) return false;
        int prestige = RankedProgressionManager.Instance?.State.prestigeLevel ?? 0;
        return prestige >= CurrentSeason.cosmetic.prestigeFree;
    }

    // ─── Firestore parse ──────────────────────────────────────────────────────

#if FIREBASE_FIRESTORE
    SeasonInfo ParseSeasonDoc(DocumentSnapshot snap)
    {
        var data = snap.ToDictionary();
        var season = new SeasonInfo
        {
            seasonId      = snap.Id,
            seasonNumber  = data.ContainsKey("seasonNumber") ? Convert.ToInt32(data["seasonNumber"]) : 1,
            name          = data.ContainsKey("name")         ? data["name"].ToString() : "Season 1",
            theme         = data.ContainsKey("theme")        ? data["theme"].ToString() : "neon",
            resetTimeUtc  = data.ContainsKey("resetTimeUtc") ? data["resetTimeUtc"].ToString() : "00:00",
            durationDays  = data.ContainsKey("durationDays") ? Convert.ToInt32(data["durationDays"]) : 30,
            rewardsDistributed = data.ContainsKey("rewardsDistributed") && (bool)data["rewardsDistributed"],
            hardResetDone      = data.ContainsKey("hardResetDone")      && (bool)data["hardResetDone"],
        };

        if (data.ContainsKey("startDate") && data["startDate"] is Timestamp st)
            season.startDate = st.ToDateTime();
        if (data.ContainsKey("endDate") && data["endDate"] is Timestamp et)
            season.endDate = et.ToDateTime();

        // Cosmetic
        if (data.ContainsKey("cosmetic") && data["cosmetic"] is Dictionary<string, object> cosmDict)
        {
            season.cosmetic = new SeasonCosmetic
            {
                skinId         = cosmDict.ContainsKey("skinId")        ? cosmDict["skinId"].ToString()                : "",
                skinName       = cosmDict.ContainsKey("skinName")      ? cosmDict["skinName"].ToString()              : "",
                description    = cosmDict.ContainsKey("description")   ? cosmDict["description"].ToString()           : "",
                gemCost        = cosmDict.ContainsKey("gemCost")       ? Convert.ToInt32(cosmDict["gemCost"])         : 500,
                archiveGemCost = cosmDict.ContainsKey("archiveGemCost")? Convert.ToInt32(cosmDict["archiveGemCost"])  : 0,
                prestigeFree   = cosmDict.ContainsKey("prestigeFree")  ? Convert.ToInt32(cosmDict["prestigeFree"])    : 5,
                themeColor     = cosmDict.ContainsKey("themeColor")    ? cosmDict["themeColor"].ToString()            : "#4444FF",
                iconPath       = cosmDict.ContainsKey("iconPath")      ? cosmDict["iconPath"].ToString()              : "",
            };
        }

        // Arena overlay
        if (data.ContainsKey("arenaOverlay") && data["arenaOverlay"] is Dictionary<string, object> overlayDict)
        {
            season.arenaOverlay = new ArenaOverlay
            {
                primary = overlayDict.ContainsKey("primary") ? overlayDict["primary"].ToString() : "#4444FF",
                glow    = overlayDict.ContainsKey("glow")    ? overlayDict["glow"].ToString()    : "#6666FF",
                fog     = overlayDict.ContainsKey("fog")     ? overlayDict["fog"].ToString()     : "#000033",
            };
        }

        // Power-up theme
        if (data.ContainsKey("powerupTheme") && data["powerupTheme"] is Dictionary<string, object> themeDict)
        {
            season.powerupTheme = new PowerupTheme { overrides = new Dictionary<string, string>() };
            foreach (var kv in themeDict)
                season.powerupTheme.overrides[kv.Key] = kv.Value.ToString();
        }
        else
        {
            season.powerupTheme = new PowerupTheme { overrides = new Dictionary<string, string>() };
        }

        return season;
    }
#endif

    // ─── Stub Helpers (no Firestore) ──────────────────────────────────────────

    static SeasonInfo CreateStubSeason()
    {
        return new SeasonInfo
        {
            seasonId     = "season_1",
            seasonNumber = 1,
            name         = "Neon Vault",
            theme        = "neon",
            startDate    = new DateTime(2026, 2, 21, 0, 0, 0, DateTimeKind.Utc),
            endDate      = new DateTime(2026, 3, 23, 0, 0, 0, DateTimeKind.Utc),
            resetTimeUtc = "00:00",
            durationDays = 30,
            cosmetic = new SeasonCosmetic
            {
                skinId       = "neon_vault_skin",
                skinName     = "Neon Vault Operative",
                description  = "Exclusive Season 1 operative skin with neon glow effects.",
                gemCost      = 500,
                prestigeFree = 5,
                themeColor   = "#4444FF",
            },
            arenaOverlay = new ArenaOverlay
            {
                primary = "#4444FF",
                glow    = "#6666FF",
                fog     = "#000033",
            },
            powerupTheme = new PowerupTheme { overrides = new Dictionary<string, string>() },
        };
    }

    static List<SeasonLeaderboardEntry> GenerateStubLeaderboard(string seasonId)
    {
        var list  = new List<SeasonLeaderboardEntry>();
        var names = new[] {
            "VaultKing","NeonDash","TrophyHunter","BlazeFast","KnoxBeast",
            "JadeViper","VectorAce","CipherGhost","NovaFlare","RyzeArc",
        };
        for (int i = 0; i < 10; i++)
        {
            list.Add(new SeasonLeaderboardEntry
            {
                uid           = $"stub_{i}",
                rank          = i + 1,
                username      = names[i],
                trophies      = 6000 - i * 300,
                tier          = i == 0 ? "Legend" : i < 3 ? "Master" : "Diamond",
                prestigeLevel = i < 3 ? i + 1 : 0,
                isLocalPlayer = false,
            });
        }
        return list;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static string GetCurrentUID()
    {
#if FIREBASE_AUTH
        return Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "";
#else
        return SystemInfo.deviceUniqueIdentifier;
#endif
    }

    // ─── Debug / Inspector ────────────────────────────────────────────────────

    [ContextMenu("Debug: Simulate Season End")]
    void DbgSimulateSeasonEnd()
    {
        if (CurrentSeason == null) { Debug.Log("[SeasonManager] No active season."); return; }
        PeakTrophiesThisSeason = RankedProgressionManager.Instance?.State.trophies ?? 1500;
        StartCoroutine(OnSeasonEnded(CurrentSeason));
    }

    [ContextMenu("Debug: Log Season Info")]
    void DbgLogSeason()
    {
        if (CurrentSeason == null) { Debug.Log("[SeasonManager] No season loaded."); return; }
        Debug.Log($"[SeasonManager] {CurrentSeason}  " +
                  $"Remaining: {CurrentSeason.TimeRemainingFormatted}  " +
                  $"PeakTrophies: {PeakTrophiesThisSeason}  " +
                  $"Initialized: {IsInitialized}");
    }

    [ContextMenu("Debug: Award 500 Test Gems (stub)")]
    void DbgAwardGems()
    {
        OnSeasonRewardCalculated?.Invoke(500, CurrentSeason);
    }
}
