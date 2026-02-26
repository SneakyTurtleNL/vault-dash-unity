using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_FIRESTORE
using Firebase.Firestore;
using Firebase.Extensions;
#endif

/// <summary>
/// GhostMatchSystem — Replay-based bot fallback for low CCU.
///
/// BEHAVIOUR:
///   When CCU < 500 (or matchmaking times out after 10s), this system
///   loads a recorded match replay from Firestore (or a local JSON fallback)
///   and plays it back as a "ghost" opponent.  The player experience is
///   seamless — no indicator, names are randomised from the replay roster.
///
/// FIRESTORE SCHEMA:
///   ghostReplays/{replayId}/
///     displayName   : string   (opponent name to show)
///     level         : int
///     frameData     : array of {frame:int, distance:float, hp:float}
///     totalFrames   : int
///     difficulty    : string   (easy / medium / hard)
///
/// INTEGRATION:
///   1. Call GhostMatchSystem.Instance.ShouldUseBotMatch(ccuCount, onDecision).
///   2. If true, call StartGhostMatch(onFrameUpdate) before starting the run.
///   3. Each frame, query GhostOpponentDistance / GhostOpponentHP for top-bar UI.
///   4. Call EndGhostMatch() when the run finishes.
///
/// LOCAL FALLBACK (no Firestore):
///   Reads Assets/Resources/GhostReplays/*.json  (bundled at build time).
/// </summary>
public class GhostMatchSystem : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static GhostMatchSystem Instance { get; private set; }

    // ─── Config ───────────────────────────────────────────────────────────────
    [Header("CCU Threshold")]
    [Tooltip("Below this CCU count, ghost match is used instead of real matchmaking.")]
    public int ccuThreshold = 500;

    [Tooltip("Matchmaking timeout (seconds) before ghost fallback kicks in.")]
    public float matchmakingTimeoutSec = 10f;

    [Header("Playback")]
    [Tooltip("Target frames-per-second for replay playback interpolation.")]
    public int replayFPS = 30;

    // ─── Events ───────────────────────────────────────────────────────────────
    /// Fired every frame with (ghostDistance, ghostHP).
    public event Action<float, float> OnGhostFrame;
    /// Fired when the ghost finishes its replay (ghost 'died').
    public event Action OnGhostFinished;

    // ─── State ────────────────────────────────────────────────────────────────
    public bool   IsGhostMatch        { get; private set; } = false;
    public float  GhostOpponentDistance { get; private set; } = 0f;
    public float  GhostOpponentHP      { get; private set; } = 100f;
    public string GhostOpponentName    { get; private set; } = "???";
    public int    GhostOpponentLevel   { get; private set; } = 1;

    // ─── Replay Data ──────────────────────────────────────────────────────────
    [Serializable]
    private class ReplayFrame
    {
        public int   frame;
        public float distance;
        public float hp;
    }

    [Serializable]
    private class ReplayData
    {
        public string        displayName;
        public int           level;
        public string        difficulty;
        public int           totalFrames;
        public List<ReplayFrame> frameData;
    }

    private ReplayData   _activeReplay;
    private Coroutine    _playbackCoroutine;
    private int          _currentFrame = 0;

    // ─── Built-in fallback replays ────────────────────────────────────────────
    private static readonly string[] FallbackNames =
        { "Vault_Runner42", "CryptoKid", "ThievingFox", "ShadowAgent", "NightOwl99" };

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Decides whether to use a ghost match.
    /// Calls back immediately (if CCU is known) or after timeout.
    /// </summary>
    public void ShouldUseBotMatch(int currentCCU, Action<bool> onDecision)
    {
        if (currentCCU < ccuThreshold)
        {
            Debug.Log($"[GhostMatch] CCU={currentCCU} < {ccuThreshold} → ghost match");
            onDecision?.Invoke(true);
        }
        else
        {
            onDecision?.Invoke(false);
        }
    }

    /// <summary>
    /// Starts a ghost match.  Loads replay from Firestore (or local fallback).
    /// difficulty: "easy" | "medium" | "hard"
    /// </summary>
    public void StartGhostMatch(string difficulty = "medium")
    {
        IsGhostMatch = true;
        _currentFrame = 0;
        LoadReplay(difficulty);
    }

    /// <summary>Stops ghost playback (call at end of match).</summary>
    public void EndGhostMatch()
    {
        IsGhostMatch = false;
        if (_playbackCoroutine != null)
        {
            StopCoroutine(_playbackCoroutine);
            _playbackCoroutine = null;
        }
    }

    // ─── Replay Loading ───────────────────────────────────────────────────────

    private void LoadReplay(string difficulty)
    {
#if FIREBASE_FIRESTORE
        LoadReplayFromFirestore(difficulty);
#else
        LoadReplayFromResources(difficulty);
#endif
    }

#if FIREBASE_FIRESTORE
    private void LoadReplayFromFirestore(string difficulty)
    {
        var db = FirebaseFirestore.DefaultInstance;
        db.Collection("ghostReplays")
          .WhereEqualTo("difficulty", difficulty)
          .Limit(10)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted || task.Result.Count == 0)
              {
                  Debug.LogWarning("[GhostMatch] Firestore load failed, using local replay.");
                  LoadReplayFromResources(difficulty);
                  return;
              }

              // Pick a random replay from the results
              var snapshots = new List<DocumentSnapshot>(task.Result.Documents);
              var snap = snapshots[UnityEngine.Random.Range(0, snapshots.Count)];

              var replay = new ReplayData
              {
                  displayName  = snap.ContainsField("displayName") ? snap.GetValue<string>("displayName") : RandomName(),
                  level        = snap.ContainsField("level")       ? snap.GetValue<int>("level")           : UnityEngine.Random.Range(5, 30),
                  difficulty   = difficulty,
                  totalFrames  = snap.ContainsField("totalFrames") ? snap.GetValue<int>("totalFrames")     : 1800,
                  frameData    = new List<ReplayFrame>()
              };

              // Deserialise frame array
              if (snap.ContainsField("frameData"))
              {
                  var raw = snap.GetValue<List<object>>("frameData");
                  foreach (Dictionary<string, object> f in raw)
                  {
                      replay.frameData.Add(new ReplayFrame
                      {
                          frame    = Convert.ToInt32(f["frame"]),
                          distance = Convert.ToSingle(f["distance"]),
                          hp       = Convert.ToSingle(f["hp"])
                      });
                  }
              }
              else
              {
                  replay.frameData = GenerateSyntheticFrames(difficulty, replay.totalFrames);
              }

              StartPlayback(replay);
          });
    }
#endif

    private void LoadReplayFromResources(string difficulty)
    {
        var asset = Resources.Load<TextAsset>($"GhostReplays/{difficulty}_replay");
        if (asset != null)
        {
            var replay = JsonUtility.FromJson<ReplayData>(asset.text);
            StartPlayback(replay);
        }
        else
        {
            // Generate synthetic replay on the fly
            var replay = new ReplayData
            {
                displayName = RandomName(),
                level       = UnityEngine.Random.Range(5, 30),
                difficulty  = difficulty,
                totalFrames = 1800,
                frameData   = GenerateSyntheticFrames(difficulty, 1800)
            };
            StartPlayback(replay);
        }
    }

    private void StartPlayback(ReplayData replay)
    {
        _activeReplay = replay;
        GhostOpponentName  = replay.displayName;
        GhostOpponentLevel = replay.level;
        GhostOpponentHP    = 100f;
        GhostOpponentDistance = 0f;

        Debug.Log($"[GhostMatch] Starting playback as '{replay.displayName}' (diff={replay.difficulty}, frames={replay.totalFrames})");

        if (_playbackCoroutine != null) StopCoroutine(_playbackCoroutine);
        _playbackCoroutine = StartCoroutine(PlaybackCoroutine());
    }

    // ─── Playback Coroutine ───────────────────────────────────────────────────

    private IEnumerator PlaybackCoroutine()
    {
        float interval = 1f / replayFPS;
        _currentFrame = 0;

        while (_currentFrame < _activeReplay.totalFrames)
        {
            yield return new WaitForSeconds(interval);

            // Binary-search for the frame entry (or interpolate)
            var fd = FindFrame(_currentFrame);
            GhostOpponentDistance = fd.distance;
            GhostOpponentHP       = fd.hp;

            OnGhostFrame?.Invoke(GhostOpponentDistance, GhostOpponentHP);

            _currentFrame++;
        }

        GhostOpponentHP = 0f;
        OnGhostFinished?.Invoke();
        IsGhostMatch = false;
        Debug.Log("[GhostMatch] Ghost replay finished.");
    }

    // ─── Frame Lookup ─────────────────────────────────────────────────────────

    private ReplayFrame FindFrame(int frame)
    {
        var frames = _activeReplay.frameData;
        if (frames == null || frames.Count == 0)
            return new ReplayFrame { frame = frame, distance = frame * 0.3f, hp = 100f };

        // Find closest frame ≤ requested
        int lo = 0, hi = frames.Count - 1, idx = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (frames[mid].frame <= frame) { idx = mid; lo = mid + 1; }
            else                             hi = mid - 1;
        }
        return frames[idx];
    }

    // ─── Synthetic Frame Generation ───────────────────────────────────────────

    /// <summary>
    /// Generates plausible-looking frame data when no recorded replay is available.
    /// Difficulty controls average speed and obstacle encounter rate.
    /// </summary>
    private List<ReplayFrame> GenerateSyntheticFrames(string difficulty, int totalFrames)
    {
        float speedBase = difficulty switch
        {
            "easy"   => 0.20f,
            "medium" => 0.30f,
            "hard"   => 0.42f,
            _        => 0.30f
        };

        float hpDecayRate = difficulty switch
        {
            "easy"   => 0.015f,
            "medium" => 0.030f,
            "hard"   => 0.050f,
            _        => 0.030f
        };

        var frames = new List<ReplayFrame>(totalFrames / 5 + 1);
        float dist = 0f;
        float hp   = 100f;

        for (int i = 0; i < totalFrames; i += 5)  // sample every 5 frames to keep list small
        {
            // Add slight random jitter to speed
            float speed = speedBase + UnityEngine.Random.Range(-0.05f, 0.05f);
            dist += speed * 5f;

            // Occasional HP hits
            if (UnityEngine.Random.value < hpDecayRate)
                hp = Mathf.Max(0, hp - UnityEngine.Random.Range(5f, 20f));

            frames.Add(new ReplayFrame { frame = i, distance = dist, hp = hp });

            if (hp <= 0f) break;
        }

        return frames;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private string RandomName() =>
        FallbackNames[UnityEngine.Random.Range(0, FallbackNames.Length)];
}
