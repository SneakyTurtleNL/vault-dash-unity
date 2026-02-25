using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ObstacleManager — Spawns, manages, and removes obstacles.
///
/// Features:
///  • Arena-aware spawning — obstacle visuals change per trophy tier
///  • 3 obstacle archetypes: Low / Full / Under (maps to gameplay move: jump / dodge / duck)
///  • Prefab-based system using arena textures (loaded from Resources)
///  • Per-lane spawning (Left / Center / Right)
///  • Perspective scaling via TunnelGenerator
///  • Object pool for performance
///  • Automatic off-screen cleanup
///
/// Arena thresholds (matches RankedProgressionManager.TIERS):
///   Rookie  : 0     – 499
///   Silver  : 500   – 999
///   Gold    : 1000  – 1999
///   Diamond : 2000  – 3499
///   Master  : 3500  – 4499
///   Legend  : 4500+
/// </summary>
public class ObstacleManager : MonoBehaviour
{
    // ─── Arena Obstacle Types ─────────────────────────────────────────────────
    /// <summary>
    /// The three visual/gameplay archetypes for arena-specific obstacles.
    ///   Low   = ground-level; player must jump over
    ///   Full  = full-height wall; player must dodge (change lane)
    ///   Under = overhead obstacle; player must duck / slide under
    /// </summary>
    public enum ArenaObstacleType { Low = 0, Full = 1, Under = 2 }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Spawn Settings")]
    public float spawnInterval = 2.5f;  // seconds between obstacles
    public float minInterval   = 1.0f;
    public float maxInterval   = 3.0f;
    public float spawnZ        = 50f;   // how far ahead obstacles spawn
    public float despawnZ      = -10f;  // Z below which obstacle is removed

    [Header("References")]
    public TunnelGenerator tunnelGen;

    [Header("Lane")]
    public float laneWidth = 2.5f;

    [Header("Arena Override (leave -1 for auto from trophies)")]
    [Tooltip("Force a specific arena index for testing: 0=Rookie,1=Silver,2=Gold,3=Diamond,4=Master,5=Legend. -1 = auto.")]
    public int arenaOverride = -1;

    // ─── Active Obstacles ─────────────────────────────────────────────────────
    private List<GameObject> activeObstacles = new List<GameObject>();

    // ─── Prefab Cache ─────────────────────────────────────────────────────────
    /// Prefab cache[arenaIndex, obstacleTypeIndex] — loaded lazily from Resources
    private readonly GameObject[,] prefabCache = new GameObject[6, 3];
    private readonly bool[,]       cacheLoaded = new bool[6, 3];

    // ─── Arena Names (matches folder + file naming) ───────────────────────────
    private static readonly string[] ArenaNames =
        { "Rookie", "Silver", "Gold", "Diamond", "Master", "Legend" };

    private static readonly string[] TypeSuffixes =
        { "low_obstacle", "full_obstacle", "under_obstacle" };

    // ─── Trophy Thresholds ────────────────────────────────────────────────────
    private static readonly int[] TrophyThresholds = { 0, 500, 1000, 2000, 3500, 4500 };

    // ─── State ────────────────────────────────────────────────────────────────
    private Coroutine spawnRoutine;
    private bool      spawning = false;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        if (tunnelGen == null)
            tunnelGen = FindObjectOfType<TunnelGenerator>();

        // Warm up prefab cache for the current arena to avoid first-frame hitch
        int arena = GetCurrentArenaIndex();
        for (int t = 0; t < 3; t++)
            GetOrLoadPrefab(arena, t);
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    void Update()
    {
        bool shouldSpawn = GameManager.Instance?.CurrentState == GameManager.GameState.Playing;

        if (shouldSpawn && !spawning)
        {
            spawning     = true;
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
        else if (!shouldSpawn && spawning)
        {
            spawning = false;
            if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        }

        RemoveOffscreenObstacles();
    }

    // ─── Spawn Loop ───────────────────────────────────────────────────────────
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (GameManager.Instance?.CurrentState == GameManager.GameState.Playing)
                SpawnObstacle();
        }
    }

    // ─── Spawn ────────────────────────────────────────────────────────────────
    void SpawnObstacle()
    {
        int lane        = Random.Range(0, 3);
        int arenaIndex  = GetCurrentArenaIndex();
        int typeIndex   = Random.Range(0, 3);   // 0=Low, 1=Full, 2=Under

        ArenaObstacleType arenaType = (ArenaObstacleType)typeIndex;
        string arenaName = ArenaNames[arenaIndex];

        GameObject go = BuildObstacleGO(arenaIndex, arenaType, lane);
        activeObstacles.Add(go);

        Debug.Log($"[ObstacleManager] Spawned {arenaName}/{arenaType} in lane {lane}");
    }

    // ─── Build Obstacle GO ────────────────────────────────────────────────────
    /// <summary>
    /// Instantiates the correct arena prefab and configures the Obstacle component.
    /// Falls back to a primitive cube if the prefab is not found.
    /// </summary>
    GameObject BuildObstacleGO(int arenaIndex, ArenaObstacleType arenaType, int lane)
    {
        float x   = (lane - 1) * laneWidth;
        Vector3 pos = new Vector3(x, 0f, spawnZ);

        // ── Load prefab ────────────────────────────────────────────────────────
        int       typeIndex = (int)arenaType;
        GameObject prefab   = GetOrLoadPrefab(arenaIndex, typeIndex);

        GameObject go;
        if (prefab != null)
        {
            go = Instantiate(prefab, pos, Quaternion.identity, transform);
            go.name = $"Obstacle_{ArenaNames[arenaIndex]}_{arenaType}_Lane{lane}";
        }
        else
        {
            // Graceful fallback to primitive cube
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Obstacle_Fallback_{arenaType}_Lane{lane}";
            go.transform.parent   = transform;
            go.transform.position = pos;
            Debug.LogWarning($"[ObstacleManager] Prefab not found for arena={ArenaNames[arenaIndex]}, type={arenaType}. Using fallback.");
        }

        // ── Tag + collider ─────────────────────────────────────────────────────
        go.tag = "Obstacle";
        Collider col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // ── Obstacle script ────────────────────────────────────────────────────
        Obstacle obs = go.GetComponent<Obstacle>();
        if (obs == null) obs = go.AddComponent<Obstacle>();

        // Map ArenaObstacleType → Obstacle.ObstacleType for physics dimensions + behaviour
        obs.type         = MapToObstacleType(arenaType);
        obs.scrollSpeed  = tunnelGen != null ? tunnelGen.scrollSpeed : 5f;
        obs.tunnelGen    = tunnelGen;
        obs.spawnZ       = spawnZ;
        obs.usePerspective = true;

        obs.SetLane(lane, laneWidth);

        // Scale from Obstacle.GetDimensions() (retains gameplay-correct hitbox)
        Vector3 dims = obs.GetDimensions();
        go.transform.localScale = dims;

        pos.y = dims.y * 0.5f;
        go.transform.position = pos;

        return go;
    }

    // ─── Arena Index ──────────────────────────────────────────────────────────
    /// <summary>
    /// Returns the arena index (0–5) based on the player's current trophy count.
    /// Uses arenaOverride if set to a non-negative value (editor testing).
    /// </summary>
    public int GetCurrentArenaIndex()
    {
        if (arenaOverride >= 0)
            return Mathf.Clamp(arenaOverride, 0, 5);

        int trophies = GetPlayerTrophies();
        return GetArenaIndexFromTrophies(trophies);
    }

    /// <summary>Returns 0–5 arena index for a given trophy count.</summary>
    public static int GetArenaIndexFromTrophies(int trophies)
    {
        // Walk thresholds in reverse: highest tier first
        for (int i = TrophyThresholds.Length - 1; i >= 0; i--)
        {
            if (trophies >= TrophyThresholds[i])
                return i;
        }
        return 0; // default Rookie
    }

    int GetPlayerTrophies()
    {
        if (RankedProgressionManager.Instance != null)
            return RankedProgressionManager.Instance.State.trophies;

        // Fallback: read PlayerPrefs directly if manager not available
        return PlayerPrefs.GetInt("VaultDash_Trophies", 0);
    }

    // ─── Prefab Cache ─────────────────────────────────────────────────────────
    /// <summary>Returns cached prefab, loading from Resources on first call.</summary>
    GameObject GetOrLoadPrefab(int arenaIndex, int typeIndex)
    {
        if (cacheLoaded[arenaIndex, typeIndex])
            return prefabCache[arenaIndex, typeIndex];

        string arenaName = ArenaNames[arenaIndex];
        string typeName  = TypeSuffixes[typeIndex];
        string path      = $"Prefabs/Obstacles/{arenaName}/{arenaName.ToLower()}_{typeName}";

        prefabCache[arenaIndex, typeIndex] = Resources.Load<GameObject>(path);
        cacheLoaded[arenaIndex, typeIndex] = true;

        if (prefabCache[arenaIndex, typeIndex] == null)
            Debug.LogWarning($"[ObstacleManager] Resources.Load failed: {path}");

        return prefabCache[arenaIndex, typeIndex];
    }

    // ─── Type Mapping ─────────────────────────────────────────────────────────
    /// <summary>
    /// Maps the 3 visual arena types to the legacy Obstacle.ObstacleType
    /// so that hitbox dimensions and physics stay correct.
    /// </summary>
    static Obstacle.ObstacleType MapToObstacleType(ArenaObstacleType arenaType)
    {
        return arenaType switch
        {
            ArenaObstacleType.Low   => Obstacle.ObstacleType.Spike,   // low ground obstacle → jump
            ArenaObstacleType.Full  => Obstacle.ObstacleType.Wall,    // full wall → dodge lane
            ArenaObstacleType.Under => Obstacle.ObstacleType.Gate,    // overhead → duck/slide
            _                      => Obstacle.ObstacleType.Box,
        };
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────────
    void RemoveOffscreenObstacles()
    {
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject go = activeObstacles[i];
            if (go == null)
            {
                activeObstacles.RemoveAt(i);
                continue;
            }

            if (go.transform.position.z < despawnZ)
            {
                activeObstacles.RemoveAt(i);
                Destroy(go);
            }
        }
    }

    public void ClearAll()
    {
        foreach (var go in activeObstacles)
        {
            if (go != null) Destroy(go);
        }
        activeObstacles.Clear();
    }

    // ─── External Control ─────────────────────────────────────────────────────
    public void SetSpawnInterval(float seconds)
    {
        spawnInterval = Mathf.Clamp(seconds, minInterval, maxInterval);
    }

    // ─── Per-Lane Collision Query ─────────────────────────────────────────────
    /// <summary>Returns true if an obstacle occupies the given lane near the player.</summary>
    public bool IsLaneBlocked(int lane, float playerZ = 0f, float checkRange = 2f)
    {
        foreach (var go in activeObstacles)
        {
            if (go == null) continue;
            Obstacle obs = go.GetComponent<Obstacle>();
            if (obs == null) continue;
            if (obs.lane != lane) continue;
            float dz = go.transform.position.z - playerZ;
            if (dz > 0f && dz < checkRange) return true;
        }
        return false;
    }

    // ─── Debug Info ───────────────────────────────────────────────────────────
    /// <summary>Returns a human-readable summary of the current arena.</summary>
    public string GetArenaDebugInfo()
    {
        int arenaIdx  = GetCurrentArenaIndex();
        int trophies  = GetPlayerTrophies();
        return $"Arena: {ArenaNames[arenaIdx]} (trophies: {trophies})";
    }
}
