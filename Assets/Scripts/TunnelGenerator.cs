using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TunnelGenerator — Procedurally spawns and recycles tunnel segments.
/// Uses object pooling for performance.
/// TODO: Add obstacle spawning, difficulty scaling.
/// </summary>
public class TunnelGenerator : MonoBehaviour
{
    [Header("Tunnel Settings")]
    public GameObject tunnelSegmentPrefab;
    public int poolSize = 10;
    public float segmentLength = 20f;
    public Transform playerTransform;

    private List<GameObject> pool = new List<GameObject>();
    private float spawnZ = 0f;
    private float recycleZ = -segmentLength;

    void Start()
    {
        // Pre-spawn initial segments
        for (int i = 0; i < poolSize; i++)
        {
            SpawnSegment();
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Spawn ahead
        while (spawnZ < playerTransform.position.z + poolSize * segmentLength)
        {
            SpawnSegment();
        }

        // Recycle behind
        RecycleOldSegments();
    }

    void SpawnSegment()
    {
        if (tunnelSegmentPrefab == null) return;

        GameObject seg = Instantiate(tunnelSegmentPrefab, 
            new Vector3(0, 0, spawnZ), Quaternion.identity);
        pool.Add(seg);
        spawnZ += segmentLength;
    }

    void RecycleOldSegments()
    {
        if (playerTransform == null) return;

        foreach (var seg in pool)
        {
            if (seg.transform.position.z < playerTransform.position.z - segmentLength * 2)
            {
                seg.transform.position = new Vector3(0, 0, spawnZ);
                pool.Remove(seg);
                pool.Add(seg);
                spawnZ += segmentLength;
                break;
            }
        }
    }
}
