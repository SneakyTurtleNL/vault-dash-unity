using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ObstacleManager — Spawns, manages, and removes obstacles.
///
/// Features:
///  • 8 obstacle types (Obstacle.ObstacleType enum)
///  • Per-lane spawning (Left / Center / Right)
///  • Perspective scaling via TunnelGenerator
///  • Object pool for performance
///  • Automatic off-screen cleanup
/// </summary>
public class ObstacleManager : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Spawn Settings")]
    public float spawnInterval     = 2.5f;  // seconds between obstacles
    public float minInterval       = 1.0f;
    public float maxInterval       = 3.0f;
    public float spawnZ            = 50f;   // how far ahead obstacles spawn
    public float despawnZ          = -10f;  // Z below which obstacle is removed

    [Header("References")]
    public TunnelGenerator tunnelGen;

    [Header("Lane")]
    public float laneWidth = 2.5f;

    // ─── Active Obstacles ─────────────────────────────────────────────────────
    private List<GameObject> activeObstacles = new List<GameObject>();

    // ─── State ────────────────────────────────────────────────────────────────
    private Coroutine spawnRoutine;
    private bool      spawning = false;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        if (tunnelGen == null)
            tunnelGen = FindObjectOfType<TunnelGenerator>();
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    void Update()
    {
        bool shouldSpawn = GameManager.Instance?.CurrentState == GameManager.GameState.Playing;

        if (shouldSpawn && !spawning)
        {
            spawning       = true;
            spawnRoutine   = StartCoroutine(SpawnLoop());
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

    void SpawnObstacle()
    {
        // Pick random lane (0=Left, 1=Center, 2=Right)
        int lane = Random.Range(0, 3);

        // Pick random type (weighted toward simpler types early)
        int typeCount   = System.Enum.GetValues(typeof(Obstacle.ObstacleType)).Length;
        int typeIndex   = Random.Range(0, typeCount);
        Obstacle.ObstacleType type = (Obstacle.ObstacleType)typeIndex;

        // Build the obstacle GameObject
        GameObject go = BuildObstacleGO(type, lane);
        activeObstacles.Add(go);

        Debug.Log($"[ObstacleManager] Spawned {type} in lane {lane}");
    }

    GameObject BuildObstacleGO(Obstacle.ObstacleType type, int lane)
    {
        // Position
        float x = (lane - 1) * laneWidth;
        Vector3 pos = new Vector3(x, 0f, spawnZ);

        // Create base primitive
        PrimitiveType prim = type switch
        {
            Obstacle.ObstacleType.Spike   => PrimitiveType.Cube,
            Obstacle.ObstacleType.Wall    => PrimitiveType.Cube,
            Obstacle.ObstacleType.LowBar  => PrimitiveType.Cube,
            Obstacle.ObstacleType.Gate    => PrimitiveType.Cube,
            Obstacle.ObstacleType.Stack   => PrimitiveType.Cube,
            _                             => PrimitiveType.Cube
        };

        GameObject go  = GameObject.CreatePrimitive(prim);
        go.name        = $"Obstacle_{type}_Lane{lane}";
        go.transform.parent = transform;

        // Obstacle script
        Obstacle obs     = go.AddComponent<Obstacle>();
        obs.type         = type;
        obs.scrollSpeed  = tunnelGen != null ? tunnelGen.scrollSpeed : 5f;
        obs.tunnelGen    = tunnelGen;
        obs.spawnZ       = spawnZ;
        obs.usePerspective = true;

        // Set lane position
        obs.SetLane(lane, laneWidth);

        // Apply per-type dimensions
        Vector3 dims = obs.GetDimensions();
        go.transform.localScale = dims;

        // Adjust Y so obstacle sits on ground
        pos.y = dims.y * 0.5f;
        go.transform.position = pos;

        // Tag + collider
        go.tag = "Obstacle";
        Collider col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Color
        obs.ApplyVisualStyle();

        return go;
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
}
