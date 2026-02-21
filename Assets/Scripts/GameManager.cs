using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager — Core game-flow controller for Vault Dash.
/// Manages state machine (Menu → Playing → GameOver), scoring, arenas, and player spawn.
/// Singleton: persists across scenes.
///
/// Week 2 additions:
///  • AudioManager: start/stop music on state change
///  • OpponentVisualizer: reset on new match
///  • VictoryScreen: route GameOver through VictoryScreen when in a match
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─── State ────────────────────────────────────────────────────────────────
    public enum GameState { Menu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; }

    // ─── Scoring ──────────────────────────────────────────────────────────────
    public int   Score         { get; private set; }
    public int   HighScore     { get; private set; }
    public float Distance      { get; private set; }   // meters traveled this run
    public int   LootScore     { get; private set; }   // coins + gems collected
    public float ComboMultiplier { get; private set; } = 1f;
    private int  consecutiveLoot = 0;

    private const string HIGH_SCORE_KEY = "VaultDash_HighScore";

    // ─── Arena ────────────────────────────────────────────────────────────────
    public enum Arena { Rookie, Silver, Gold, Diamond, Legend }

    [System.Serializable]
    public struct ArenaSettings
    {
        public Arena arena;
        public float scrollSpeed;
        public float obstacleInterval;  // seconds between obstacles
        public int   maxScore;
    }

    [Header("Arenas")]
    public ArenaSettings[] arenas = new ArenaSettings[]
    {
        new ArenaSettings { arena = Arena.Rookie,  scrollSpeed = 5f, obstacleInterval = 3.0f, maxScore = 500  },
        new ArenaSettings { arena = Arena.Silver,  scrollSpeed = 6f, obstacleInterval = 2.5f, maxScore = 1000 },
        new ArenaSettings { arena = Arena.Gold,    scrollSpeed = 7f, obstacleInterval = 2.0f, maxScore = 2000 },
        new ArenaSettings { arena = Arena.Diamond, scrollSpeed = 8f, obstacleInterval = 1.5f, maxScore = 3500 },
        new ArenaSettings { arena = Arena.Legend,  scrollSpeed = 9f, obstacleInterval = 1.0f, maxScore = 5000 },
    };

    [Header("Arena Selection")]
    public Arena selectedArena = Arena.Rookie;

    // ─── References ───────────────────────────────────────────────────────────
    [Header("References")]
    public GameObject      playerPrefab;
    public Transform       playerSpawnPoint;
    public TunnelGenerator tunnelGenerator;
    public ObstacleManager obstacleManager;

    [Header("UI")]
    public GameObject startScreen;
    public GameObject gameOverScreen;
    public GameObject pauseScreen;
    public TMPro.TextMeshProUGUI gameOverScoreText;
    public TMPro.TextMeshProUGUI gameOverHighScoreText;

    // ─── Runtime ──────────────────────────────────────────────────────────────
    private GameObject  playerInstance;
    private float       runTimer;
    private Coroutine   distanceRoutine;

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

        HighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    void Start()
    {
        Debug.Log("[GameManager] Vault Dash — Ready!");
        SetState(GameState.Menu);
    }

    // ─── State Machine ────────────────────────────────────────────────────────
    public void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameManager] → {newState}");

        switch (newState)
        {
            case GameState.Menu:
                Time.timeScale = 1f;
                ShowScreen(startScreen, true);
                ShowScreen(gameOverScreen, false);
                ShowScreen(pauseScreen, false);
                // ── Week 2 ──
                AudioManager.Instance?.StopFootsteps();
                AudioManager.Instance?.PlayMenuMusic();
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                ShowScreen(startScreen, false);
                ShowScreen(gameOverScreen, false);
                ShowScreen(pauseScreen, false);
                // ── Week 2 ──
                AudioManager.Instance?.PlayMatchMusic();
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                ShowScreen(pauseScreen, true);
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                OnGameOver();
                break;
        }
    }

    // ─── Game Flow ────────────────────────────────────────────────────────────
    public void StartGame()
    {
        // Reset tracking
        Score            = 0;
        Distance         = 0f;
        LootScore        = 0;
        ComboMultiplier  = 1f;
        consecutiveLoot  = 0;
        runTimer         = 0f;

        // ── Week 2: reset opponent visualizer ──
        OpponentVisualizer.Instance?.ResetVisualizer();

        // ── Phase 2: reset FPS stats + post-processing ──
        FrameRateDisplay.Instance?.ResetStats();
        PostProcessingManager.Instance?.ResetEffects();

        // Apply arena settings
        ArenaSettings settings = GetArenaSettings(selectedArena);
        if (tunnelGenerator != null)
        {
            tunnelGenerator.SetScrollSpeed(settings.scrollSpeed);
            // ── Week 3-4: Load arena background ──
            tunnelGenerator.ApplyArena(selectedArena);
        }
        if (obstacleManager != null)
            obstacleManager.SetSpawnInterval(settings.obstacleInterval);

        // Spawn player
        SpawnPlayer();

        // ── Phase 2: Rebind Cinemachine virtual camera to new player instance ──
        CinemachineSetup cinemachineSetup = FindObjectOfType<CinemachineSetup>();
        if (cinemachineSetup != null && playerInstance != null)
            cinemachineSetup.BindToPlayer(playerInstance.transform);

        // Start distance counter
        if (distanceRoutine != null) StopCoroutine(distanceRoutine);
        distanceRoutine = StartCoroutine(TrackDistance(settings.scrollSpeed));

        SetState(GameState.Playing);

        // ── Phase 2: Firebase game_start event ──
        string arenaName = selectedArena.ToString();
        string charName  = "Agent Zero";  // TODO: pull from CharacterDatabase.GetProfile(selectedChar).displayName
        int    charIdx   = PlayerPrefs.GetInt("SelectedCharacter", 0);
        if (CharacterDatabase.Instance != null)
        {
            var prof = CharacterDatabase.Instance.GetProfile(charIdx);
            if (prof != null) charName = prof.displayName;
        }
        FirebaseManager.Instance?.LogGameStart(arenaName, charName);
        FirebaseManager.Instance?.SetPlayerLevel(PlayerPrefs.GetInt("VaultDash_PlayerLevel", 1));
        FirebaseManager.Instance?.SetSelectedCharacter(charName);
    }

    void SpawnPlayer()
    {
        if (playerInstance != null) Destroy(playerInstance);

        Vector3  spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (playerPrefab != null)
        {
            playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
        }
        else
        {
            // Fallback: create a simple cube player
            playerInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerInstance.transform.position   = spawnPos;
            playerInstance.transform.localScale  = new Vector3(1f, 1.5f, 0.5f);
            playerInstance.name = "Player_Fallback";

            // Add Player script
            Player p = playerInstance.AddComponent<Player>();
            playerInstance.tag = "Player";

            // Add BoxCollider (already has one from Primitive, make it trigger)
            BoxCollider bc = playerInstance.GetComponent<BoxCollider>();
            if (bc != null) bc.isTrigger = true;
        }

        Debug.Log("[GameManager] Player spawned.");
    }

    IEnumerator TrackDistance(float speed)
    {
        while (CurrentState == GameState.Playing)
        {
            Distance += speed * Time.deltaTime;
            // Score: 1 point per unit traveled
            Score = Mathf.RoundToInt(Distance) + LootScore;
            yield return null;
        }
    }

    void OnGameOver()
    {
        if (distanceRoutine != null)
        {
            StopCoroutine(distanceRoutine);
            distanceRoutine = null;
        }

        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, HighScore);
            PlayerPrefs.Save();
        }

        ShowScreen(gameOverScreen, true);

        if (gameOverScoreText != null)
            gameOverScoreText.text = $"Score: {Score}";
        if (gameOverHighScoreText != null)
            gameOverHighScoreText.text = $"Best: {HighScore}";

        Debug.Log($"[GameManager] Game Over — Score: {Score}, Best: {HighScore}");

        // ── Phase 2: Firebase game_over event (winner determined by MatchManager) ──
        bool isWinner = MatchManager.Instance != null && MatchManager.Instance.Status == MatchManager.MatchStatus.Finished
            ? GameManager.Instance?.Distance >= MatchManager.Instance.OpponentDistance
            : false;
        FirebaseManager.Instance?.LogGameOver(Score, Distance, isWinner);

        // ── Phase 2: Color grading based on result ──
        if (isWinner) PostProcessingManager.Instance?.SetVictoryColors();
        else          PostProcessingManager.Instance?.SetDefeatColors();
    }

    public void PauseGame()  => SetState(GameState.Paused);
    public void ResumeGame() => SetState(GameState.Playing);
    public void GameOver()   => SetState(GameState.GameOver);

    public void RestartGame()
    {
        // Clean up old player
        if (playerInstance != null) Destroy(playerInstance);
        // Clear obstacles
        if (obstacleManager != null) obstacleManager.ClearAll();
        StartGame();
    }

    public void ReturnToMenu()
    {
        if (playerInstance != null) Destroy(playerInstance);
        if (obstacleManager != null) obstacleManager.ClearAll();
        SetState(GameState.Menu);
    }

    // ─── Scoring ──────────────────────────────────────────────────────────────
    public void AddScore(int points)
    {
        if (CurrentState != GameState.Playing) return;
        LootScore += points;

        // Combo multiplier: 3+ loot → x1.5
        consecutiveLoot++;
        ComboMultiplier = consecutiveLoot >= 3 ? 1.5f : 1f;

        // Apply multiplier
        int boosted = Mathf.RoundToInt(points * ComboMultiplier);
        Score       = Mathf.RoundToInt(Distance) + LootScore;

        Debug.Log($"[GameManager] +{boosted} pts (combo x{ComboMultiplier})  Total: {Score}");
    }

    public void ResetCombo() => consecutiveLoot = 0;

    // ─── Arena ────────────────────────────────────────────────────────────────
    public void SelectArena(Arena arena)
    {
        selectedArena = arena;
        Debug.Log($"[GameManager] Arena selected: {arena}");
    }

    ArenaSettings GetArenaSettings(Arena arena)
    {
        foreach (var a in arenas)
            if (a.arena == arena) return a;
        return arenas[0];
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    static void ShowScreen(GameObject screen, bool visible)
    {
        if (screen != null) screen.SetActive(visible);
    }
}
