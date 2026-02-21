using System.Collections;
using UnityEngine;

/// <summary>
/// MatchManager — Nakama real-time multiplayer integration.
///
/// Handles:
///  • Match creation + join
///  • Real-time score sync (every 30 frames)
///  • Distance sync (real-time)
///  • Match-end detection (distance ≤ 0)
///  • Matchmaking queue
///
/// Nakama SDK: add com.heroiclabs.nakama-dotnet to Packages/manifest.json.
/// Toggle NAKAMA_AVAILABLE define when SDK is present.
/// Without SDK, this class runs in OFFLINE_MODE (solo practice).
///
/// Week 2 additions:
///  • OpponentVisualizer: SetDistance() every frame + TriggerCollision() at 0m
///  • AudioManager: UpdateTension() as distance shrinks
///  • Winner determination: who finished first?
/// </summary>
public class MatchManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static MatchManager Instance { get; private set; }

    // ─── Nakama Config ────────────────────────────────────────────────────────
    [Header("Nakama Server")]
    public string nakamaHost     = "127.0.0.1";
    public int    nakamaPort     = 7350;
    public string nakamaKey      = "defaultkey";
    public bool   useSsl         = false;

    // ─── Match State ──────────────────────────────────────────────────────────
    [Header("Match")]
    public float matchDistance   = 500f;  // 500m total run

    public enum MatchStatus { Idle, Connecting, Matchmaking, InMatch, Finished }
    public MatchStatus Status { get; private set; } = MatchStatus.Idle;

    public string MatchId        { get; private set; }
    public string OpponentId     { get; private set; }
    public string OpponentName   { get; private set; } = "???";
    public int    OpponentLevel  { get; private set; } = 1;
    public float  OpponentDistance { get; private set; }
    public float  OpponentHP     { get; private set; } = 100f;

    // ─── References ───────────────────────────────────────────────────────────
    [Header("References")]
    public TopBarUI topBar;

    // ─── Sync Config ──────────────────────────────────────────────────────────
    [Header("Sync")]
    public int syncEveryFrames = 30;  // score sync every 30 frames

    // ─── Private ──────────────────────────────────────────────────────────────
    private int    frameCounter;
    private bool   offlineMode = false;
    private float  simulatedOpponentProgress = 0f;

    // Op-codes for match data messages
    private const int OP_SCORE    = 1;
    private const int OP_DISTANCE = 2;
    private const int OP_END      = 3;

#if NAKAMA_AVAILABLE
    private Nakama.IClient    _client;
    private Nakama.ISession   _session;
    private Nakama.ISocket    _socket;
    private Nakama.IMatch     _match;
#endif

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (topBar == null) topBar = FindObjectOfType<TopBarUI>();
        InitNakama();
    }

    void InitNakama()
    {
#if NAKAMA_AVAILABLE
        _client = new Nakama.Client("http", nakamaHost, nakamaPort, nakamaKey);
        Debug.Log($"[MatchManager] Nakama client created → {nakamaHost}:{nakamaPort}");
#else
        offlineMode = true;
        Debug.Log("[MatchManager] OFFLINE MODE — Nakama SDK not present. Add heroiclabs/nakama-unity to Packages.");
#endif
    }

    // ─── Authentication ───────────────────────────────────────────────────────
    public void AuthenticateAndConnect()
    {
#if NAKAMA_AVAILABLE
        StartCoroutine(AuthRoutine());
#else
        Debug.Log("[MatchManager] Offline: skipping auth.");
        Status = MatchStatus.Idle;
#endif
    }

#if NAKAMA_AVAILABLE
    IEnumerator AuthRoutine()
    {
        Status = MatchStatus.Connecting;

        var task = _client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("[MatchManager] Auth failed: " + task.Exception?.Message);
            Status = MatchStatus.Idle;
            yield break;
        }

        _session = task.Result;
        Debug.Log($"[MatchManager] Authenticated as {_session.UserId}");

        // Open socket
        _socket = _client.NewSocket();
        var connectTask = _socket.ConnectAsync(_session);
        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (connectTask.IsFaulted)
        {
            Debug.LogError("[MatchManager] Socket connect failed: " + connectTask.Exception?.Message);
            Status = MatchStatus.Idle;
            yield break;
        }

        // Subscribe to match data
        _socket.ReceivedMatchState += OnMatchStateReceived;
        _socket.ReceivedMatchPresence += OnPresenceEvent;

        Debug.Log("[MatchManager] Socket connected.");
        Status = MatchStatus.Idle;
    }
#endif

    // ─── Matchmaking ──────────────────────────────────────────────────────────
    public void StartMatchmaking()
    {
#if NAKAMA_AVAILABLE
        StartCoroutine(MatchmakingRoutine());
#else
        // Offline: simulate finding an opponent
        Debug.Log("[MatchManager] Offline matchmaking — simulated opponent.");
        StartOfflineMatch();
#endif
    }

#if NAKAMA_AVAILABLE
    IEnumerator MatchmakingRoutine()
    {
        Status = MatchStatus.Matchmaking;
        Debug.Log("[MatchManager] Searching for opponent...");

        var ticketTask = _socket.AddMatchmakerAsync("+properties.mode:vault_dash", 2, 2);
        yield return new WaitUntil(() => ticketTask.IsCompleted);

        if (ticketTask.IsFaulted)
        {
            Debug.LogError("[MatchManager] Matchmaking failed: " + ticketTask.Exception?.Message);
            Status = MatchStatus.Idle;
            yield break;
        }

        // Wait for match to be found via event — handled in OnMatchmakerMatched
        _socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
        Debug.Log("[MatchManager] Ticket created — waiting for match...");
    }

    void OnMatchmakerMatched(Nakama.IMatchmakerMatched matched)
    {
        StartCoroutine(JoinMatchRoutine(matched));
    }

    IEnumerator JoinMatchRoutine(Nakama.IMatchmakerMatched matched)
    {
        var joinTask = _socket.JoinMatchedAsync(matched);
        yield return new WaitUntil(() => joinTask.IsCompleted);

        if (joinTask.IsFaulted)
        {
            Debug.LogError("[MatchManager] Join failed: " + joinTask.Exception?.Message);
            yield break;
        }

        _match    = joinTask.Result;
        MatchId   = _match.Id;
        Status    = MatchStatus.InMatch;

        // Extract opponent info from presences
        foreach (var presence in _match.Presences)
        {
            if (presence.UserId != _session.UserId)
            {
                OpponentId   = presence.UserId;
                OpponentName = presence.Username;
            }
        }

        Debug.Log($"[MatchManager] Match joined: {MatchId}, vs {OpponentName}");
        OnMatchStarted();
    }

    void OnMatchStateReceived(Nakama.IMatchState state)
    {
        if (state.UserPresence.UserId == _session?.UserId) return; // ignore self

        string json = System.Text.Encoding.UTF8.GetString(state.State);

        switch (state.OpCode)
        {
            case OP_SCORE:
                // {"score": 1234}
                break;

            case OP_DISTANCE:
                // {"distance": 342.5}
                if (float.TryParse(ExtractJsonFloat(json, "distance"), out float d))
                    OpponentDistance = d;
                break;

            case OP_END:
                Debug.Log("[MatchManager] Opponent finished match.");
                EndMatch(false);
                break;
        }
    }

    void OnPresenceEvent(Nakama.IMatchPresenceEvent evt)
    {
        foreach (var left in evt.Leaves)
        {
            if (left.UserId == OpponentId)
            {
                Debug.Log("[MatchManager] Opponent disconnected.");
                EndMatch(true); // you win on disconnect
            }
        }
    }
#endif

    // ─── Offline / Solo Mode ──────────────────────────────────────────────────
    void StartOfflineMatch()
    {
        MatchId      = "offline_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        OpponentName = "Bot_" + Random.Range(100, 999);
        OpponentLevel = Random.Range(1, 15);
        Status       = MatchStatus.InMatch;
        simulatedOpponentProgress = 0f;
        OnMatchStarted();
    }

    void SimulateOpponent()
    {
        // Simulate opponent running at a slightly different speed
        if (Status != MatchStatus.InMatch) return;

        float opponentSpeed = 4.5f + Random.Range(-0.5f, 0.5f);
        simulatedOpponentProgress += opponentSpeed * Time.deltaTime;
        OpponentDistance = Mathf.Max(0f, matchDistance - simulatedOpponentProgress);

        // Simulate HP taking occasional damage
        if (Random.value < 0.001f)
            OpponentHP = Mathf.Max(0f, OpponentHP - 10f);

        // ── Week 2: Did the opponent finish first? ──
        if (simulatedOpponentProgress >= matchDistance)
            EndMatch(false);  // player loses
    }

    // ─── Match Lifecycle ──────────────────────────────────────────────────────
    void OnMatchStarted()
    {
        Debug.Log($"[MatchManager] Match started: {MatchId} vs {OpponentName} (Lvl {OpponentLevel})");

        if (topBar != null)
        {
            topBar.yourName      = "YOU";
            topBar.yourLevel     = 1;  // TODO: load from player profile
            topBar.opponentName  = OpponentName;
            topBar.opponentLevel = OpponentLevel;
            topBar.matchDistance = matchDistance;
            topBar.RefreshNameLabels();
        }

        GameManager.Instance?.StartGame();
    }

    // ─── Per-Frame Sync ───────────────────────────────────────────────────────
    void Update()
    {
        if (Status != MatchStatus.InMatch) return;
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        // Offline: simulate opponent
        if (offlineMode) SimulateOpponent();

        // Sync every N frames
        frameCounter++;
        if (frameCounter >= syncEveryFrames)
        {
            frameCounter = 0;
            SyncScore();
        }

        // Distance sync every frame
        SyncDistance();

        // Update top bar with opponent data
        UpdateTopBar();

        // Check win/lose condition
        CheckMatchEnd();
    }

    void SyncScore()
    {
        int score = GameManager.Instance?.Score ?? 0;

#if NAKAMA_AVAILABLE
        if (_socket != null && _match != null)
        {
            string json = $"{{\"score\":{score}}}";
            var bytes   = System.Text.Encoding.UTF8.GetBytes(json);
            _socket.SendMatchStateAsync(_match.Id, OP_SCORE, bytes);
        }
#endif

        Debug.Log($"[MatchManager] Score sync: {score}");
    }

    void SyncDistance()
    {
        float dist = GameManager.Instance?.Distance ?? 0f;

#if NAKAMA_AVAILABLE
        if (_socket != null && _match != null)
        {
            string json = $"{{\"distance\":{dist:F1}}}";
            var bytes   = System.Text.Encoding.UTF8.GetBytes(json);
            _socket.SendMatchStateAsync(_match.Id, OP_DISTANCE, bytes);
        }
#endif
    }

    void UpdateTopBar()
    {
        if (topBar == null) return;

        float remaining = matchDistance - (GameManager.Instance?.Distance ?? 0f);
        topBar.SetDistance(Mathf.Max(0f, remaining));
        topBar.SetOpponentHP(OpponentHP);
    }

    void CheckMatchEnd()
    {
        float yourDist       = GameManager.Instance?.Distance ?? 0f;
        float remaining      = matchDistance - yourDist;
        float clampedRemaining = Mathf.Max(0f, remaining);

        // ── Week 2: Drive OpponentVisualizer with current distance ──
        OpponentVisualizer.Instance?.SetDistance(clampedRemaining);

        if (yourDist >= matchDistance)
        {
            // You finished first!
#if NAKAMA_AVAILABLE
            if (_socket != null && _match != null)
            {
                string json = "{\"finished\":true}";
                var bytes   = System.Text.Encoding.UTF8.GetBytes(json);
                _socket.SendMatchStateAsync(_match.Id, OP_END, bytes);
            }
#endif
            EndMatch(true);
        }
    }

    void EndMatch(bool youWin)
    {
        if (Status == MatchStatus.Finished) return; // guard double-call
        Status = MatchStatus.Finished;

        Debug.Log($"[MatchManager] Match ended. Result: {(youWin ? "WIN" : "LOSE")}");

        // ── Week 2: Trigger visual collision + victory screen ──
        if (OpponentVisualizer.Instance != null)
            OpponentVisualizer.Instance.TriggerCollision(youWin);
        else
            GameManager.Instance?.GameOver(); // fallback
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    /// <summary>Call from menu button.</summary>
    public void FindMatch()
    {
#if NAKAMA_AVAILABLE
        if (Status == MatchStatus.Idle) StartMatchmaking();
#else
        StartOfflineMatch();
#endif
    }

    // ─── Revenge Queue / Rematch ──────────────────────────────────────────────
    /// <summary>
    /// Request a rematch against the same opponent (Bart's Revenge Queue design).
    /// • If opponent is available → start direct match with them
    /// • If opponent unavailable → fall back to random matchmaking
    /// • Best-of-3 mode: winner needs 2 wins, prize pool increased
    /// </summary>
    public void RequestRematch(string opponentId, bool bestOf3 = false)
    {
        if (Status == MatchStatus.InMatch || Status == MatchStatus.Matchmaking)
        {
            Debug.LogWarning("[MatchManager] RequestRematch called while already in a match/queue.");
            return;
        }

        // Reset match state
        Status = MatchStatus.Idle;
        simulatedOpponentProgress = 0f;
        OpponentHP = 100f;

        bool opponentAvailable = MatchmakingService.IsPlayerAvailable(opponentId);

        Debug.Log($"[MatchManager] Rematch requested vs {opponentId} — Available: {opponentAvailable}, BO3: {bestOf3}");

        if (bestOf3)
        {
            // Track best-of-3 state
            int myBO3Wins  = PlayerPrefs.GetInt("BO3_Win_Me",  0);
            int oppBO3Wins = PlayerPrefs.GetInt("BO3_Win_Opp", 0);
            Debug.Log($"[MatchManager] BO3 score: Me {myBO3Wins} — Opp {oppBO3Wins}");
        }

        if (opponentAvailable && !string.IsNullOrEmpty(opponentId) && opponentId != "offline")
        {
#if NAKAMA_AVAILABLE
            StartDirectMatch(opponentId);
#else
            // In offline mode, keep the same opponent name for continuity
            ReplayOfflineMatch();
#endif
        }
        else
        {
            Debug.Log("[MatchManager] Opponent unavailable — falling back to random matchmaking.");
            FindMatch();
        }
    }

    /// <summary>Replay offline match against same bot (keeps opponent name).</summary>
    void ReplayOfflineMatch()
    {
        MatchId   = "offline_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        // Keep OpponentName + OpponentLevel from previous match for continuity
        Status    = MatchStatus.InMatch;
        simulatedOpponentProgress = 0f;
        OpponentHP = 100f;

        Debug.Log($"[MatchManager] Rematch started (offline) vs {OpponentName}");
        OnMatchStarted();
    }

#if NAKAMA_AVAILABLE
    /// <summary>Direct challenge another player by ID (no lobby — instant match).</summary>
    void StartDirectMatch(string targetId)
    {
        // TODO: implement Nakama private match invitation
        // var match = await _socket.CreateMatchAsync();
        // Send match ID to target via notification
        Debug.Log($"[MatchManager] Direct match challenge sent to {targetId}.");
        StartMatchmaking(); // fallback to queue until direct challenge is implemented
    }
#endif

    public void LeaveMatch()
    {
#if NAKAMA_AVAILABLE
        if (_socket != null && _match != null)
            _socket.LeaveMatchAsync(_match.Id);
#endif
        Status = MatchStatus.Idle;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    static string ExtractJsonFloat(string json, string key)
    {
        string search = $"\"{key}\":";
        int idx = json.IndexOf(search);
        if (idx < 0) return "0";
        int start = idx + search.Length;
        int end   = json.IndexOfAny(new[] { ',', '}', ' ' }, start);
        if (end < 0) end = json.Length;
        return json.Substring(start, end - start).Trim();
    }
}
