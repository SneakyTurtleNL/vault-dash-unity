using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TunnelGenerator — Isometric camera, 3-layer parallax, tunnel tile recycling, lane grid.
///
/// Camera:  45° isometric (euler 45,0,0), orthographic size 6, pos (0,8,-8).
/// Parallax: Sky 0.2x | Mid 0.6x | Ground 1.0x
/// Tunnel: Quads tiles recycled ahead of player.
/// </summary>
public class TunnelGenerator : MonoBehaviour
{
    // ─── Camera ───────────────────────────────────────────────────────────────
    [Header("Camera")]
    public Camera mainCamera;
    public Vector3 cameraPosition    = new Vector3(0f, 8f, -8f);
    public Vector3 cameraEuler       = new Vector3(45f, 0f, 0f);
    public float   orthoSize         = 6f;

    // ─── Tunnel Tiles ─────────────────────────────────────────────────────────
    [Header("Tunnel Tiles")]
    public GameObject groundTilePrefab;
    public int   visibleTiles   = 12;
    public float tileLength     = 8f;   // Z depth per tile

    // ─── Parallax Layers ──────────────────────────────────────────────────────
    [Header("Parallax")]
    public Transform skyLayer;           // parent with sky sprite(s)
    public Transform midLayer;           // parent with building sprites
    public Transform groundLayer;        // parent with ground rail sprites

    private const float SKY_SPEED  = 0.2f;
    private const float MID_SPEED  = 0.6f;
    private const float GND_SPEED  = 1.0f;

    [Header("Arena / Speed")]
    public float scrollSpeed = 5f;       // units per second (overridden by GameManager arena)

    // ─── Lane Grid ────────────────────────────────────────────────────────────
    [Header("Lane Grid")]
    public bool  showLaneGrid   = true;
    public float laneWidth      = 2.5f;
    public Material gridMaterial;        // assign a simple semi-transparent material

    // ─── Perspective Scaling ──────────────────────────────────────────────────
    [Header("Perspective Scale")]
    [Tooltip("Scale multiplier for objects near spawn (far away)")]
    public float farScale  = 0.3f;
    [Tooltip("Scale multiplier for objects at player position (close)")]
    public float nearScale = 1.0f;
    public float perspectiveDepth = 60f; // total Z range for scaling

    // ─── Private ──────────────────────────────────────────────────────────────
    private List<GameObject> tilePool    = new List<GameObject>();
    private float            spawnZ      = 0f;
    private float            totalScroll = 0f;  // accumulated scroll for parallax UV

    // Lane grid lines (LineRenderer objects)
    private List<LineRenderer> laneLines = new List<LineRenderer>();

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        SetupCamera();
        PrewarmTiles();
        if (showLaneGrid) BuildLaneGrid();
    }

    // ─── Camera Setup ─────────────────────────────────────────────────────────
    void SetupCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("[TunnelGenerator] No camera found.");
            return;
        }

        mainCamera.transform.position    = cameraPosition;
        mainCamera.transform.eulerAngles = cameraEuler;
        mainCamera.orthographic          = true;
        mainCamera.orthographicSize      = orthoSize;

        Debug.Log("[TunnelGenerator] Isometric camera configured: 45° angle, ortho size " + orthoSize);
    }

    // ─── Tile Pool ────────────────────────────────────────────────────────────
    void PrewarmTiles()
    {
        spawnZ = 0f;

        for (int i = 0; i < visibleTiles; i++)
        {
            SpawnTile();
        }
    }

    void SpawnTile()
    {
        GameObject tile;

        if (groundTilePrefab != null)
        {
            tile = Instantiate(groundTilePrefab, new Vector3(0f, 0f, spawnZ), Quaternion.identity, transform);
        }
        else
        {
            // Fallback: create a flat quad procedurally
            tile              = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name         = "GroundTile";
            tile.transform.parent        = transform;
            tile.transform.position      = new Vector3(0f, -0.01f, spawnZ);
            tile.transform.eulerAngles   = new Vector3(90f, 0f, 0f);
            tile.transform.localScale    = new Vector3(laneWidth * 3f, tileLength, 1f);

            // Checkerboard-ish colours for lanes
            Renderer r = tile.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.15f, 0.15f, 0.18f);
            }
        }

        tilePool.Add(tile);
        spawnZ += tileLength;
    }

    // ─── Lane Grid ────────────────────────────────────────────────────────────
    void BuildLaneGrid()
    {
        // Draw two vertical lines separating left|center|right lanes
        float[] xPositions = { -laneWidth, laneWidth };  // left-center border, center-right border

        foreach (float x in xPositions)
        {
            GameObject go = new GameObject("LaneLine_" + x);
            go.transform.parent = transform;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace   = true;
            lr.positionCount   = 2;
            lr.startWidth      = 0.05f;
            lr.endWidth        = 0.05f;
            lr.startColor      = new Color(1f, 1f, 1f, 0.15f);
            lr.endColor        = new Color(1f, 1f, 1f, 0.02f);

            if (gridMaterial != null) lr.material = gridMaterial;

            lr.SetPosition(0, new Vector3(x, 0.01f, -20f));
            lr.SetPosition(1, new Vector3(x, 0.01f,  80f));

            laneLines.Add(lr);
        }
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    void Update()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        float delta = scrollSpeed * Time.deltaTime;
        totalScroll += delta;

        ScrollTiles(delta);
        ScrollParallax(delta);
        RecycleOffscreenTiles();
    }

    // ─── Tile Scrolling ───────────────────────────────────────────────────────
    void ScrollTiles(float delta)
    {
        foreach (var tile in tilePool)
        {
            if (tile == null) continue;
            tile.transform.position -= new Vector3(0f, 0f, delta);
        }
    }

    void RecycleOffscreenTiles()
    {
        // Any tile that passes behind the camera (z < recycleThreshold) → move to front
        float recycleZ = cameraPosition.z - tileLength * 2f;

        foreach (var tile in tilePool)
        {
            if (tile == null) continue;
            if (tile.transform.position.z < recycleZ)
            {
                tile.transform.position = new Vector3(0f, tile.transform.position.y, spawnZ);
                spawnZ += tileLength;
            }
        }
    }

    // ─── Parallax ─────────────────────────────────────────────────────────────
    void ScrollParallax(float delta)
    {
        // Move each layer by its scroll factor
        MoveLayer(skyLayer,    delta * SKY_SPEED);
        MoveLayer(midLayer,    delta * MID_SPEED);
        MoveLayer(groundLayer, delta * GND_SPEED);
    }

    void MoveLayer(Transform layer, float amount)
    {
        if (layer == null) return;

        Vector3 pos  = layer.position;
        pos.z       -= amount;

        // Wrap: if layer drifts too far back, teleport forward by its wrap distance
        float wrapAt = -50f;
        float wrapTo =  50f;
        if (pos.z < wrapAt) pos.z += (wrapTo - wrapAt);

        layer.position = pos;
    }

    // ─── Perspective Scaling Helper ───────────────────────────────────────────
    /// <summary>
    /// Compute a perspective scale for an object at worldZ relative to player (z=0).
    /// Objects at perspectiveDepth away → farScale; at z=0 → nearScale.
    /// </summary>
    public float GetPerspectiveScale(float worldZ)
    {
        float t = Mathf.Clamp01(worldZ / perspectiveDepth);
        return Mathf.Lerp(nearScale, farScale, t);
    }

    // ─── Arena Speed ──────────────────────────────────────────────────────────
    public void SetScrollSpeed(float speed) => scrollSpeed = speed;
}
