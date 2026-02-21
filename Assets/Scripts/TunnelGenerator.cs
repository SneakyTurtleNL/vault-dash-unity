using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TunnelGenerator — Isometric camera, 3-layer parallax, tunnel tile recycling, lane grid.
///
/// Camera:  45° isometric (euler 45,0,0), orthographic size 6, pos (0,8,-8).
/// Parallax: Sky 0.2x | Mid 0.6x | Ground 1.0x
/// Tunnel: Quads tiles recycled ahead of player.
///
/// Week 3-4 additions:
///   • Arena background loading (5 themed parallax sets)
///   • Smooth cross-fade when switching arena backgrounds
///   • Per-arena sprite assignments (Assets/Sprites/Arenas/)
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

        // ── Phase 2: If CinemachineSetup is present, let it handle the camera ──
        // TunnelGenerator still sets orthographic mode + size as fallback baseline;
        // Cinemachine's lens override will take priority if a Virtual Camera is active.
        CinemachineSetup cinemachineSetup = FindObjectOfType<CinemachineSetup>();
        if (cinemachineSetup != null)
        {
            // Let Cinemachine own position + rotation; only set ortho mode here.
            mainCamera.orthographic     = true;
            mainCamera.orthographicSize = orthoSize;
            Debug.Log("[TunnelGenerator] CinemachineSetup detected — camera position deferred to Cinemachine.");
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

    // ─── Arena Backgrounds (Week 3-4) ─────────────────────────────────────────
    /// <summary>
    /// Arena parallax background definitions.
    /// Each arena has 3 sprite layers: sky, mid, ground.
    /// Sprites must be placed in Assets/Sprites/Arenas/.
    /// </summary>
    [System.Serializable]
    public struct ArenaBackground
    {
        public GameManager.Arena arena;
        [Tooltip("Sky layer sprite (scrolls at 0.2x)")]
        public Sprite skySprite;
        [Tooltip("Mid layer sprite (scrolls at 0.6x)")]
        public Sprite midSprite;
        [Tooltip("Ground layer sprite (scrolls at 1.0x)")]
        public Sprite groundSprite;
        [Tooltip("Background tint color")]
        public Color  ambientColor;
        [Tooltip("Fog/atmosphere color")]
        public Color  fogColor;
    }

    [Header("Arena Backgrounds")]
    [Tooltip("Assign 5 arena backgrounds — one per rank tier")]
    public ArenaBackground[] arenaBackgrounds = new ArenaBackground[]
    {
        // Defaults — override in Inspector with actual sprites
        new ArenaBackground { arena = GameManager.Arena.Rookie,  ambientColor = new Color(0.15f,0.15f,0.18f), fogColor = new Color(0.1f,0.1f,0.12f) },
        new ArenaBackground { arena = GameManager.Arena.Silver,  ambientColor = new Color(0.08f,0.12f,0.10f), fogColor = new Color(0.05f,0.08f,0.06f) },
        new ArenaBackground { arena = GameManager.Arena.Gold,    ambientColor = new Color(0.20f,0.15f,0.05f), fogColor = new Color(0.15f,0.10f,0.03f) },
        new ArenaBackground { arena = GameManager.Arena.Diamond, ambientColor = new Color(0.05f,0.10f,0.20f), fogColor = new Color(0.03f,0.06f,0.15f) },
        new ArenaBackground { arena = GameManager.Arena.Legend,  ambientColor = new Color(0.08f,0.03f,0.15f), fogColor = new Color(0.05f,0.02f,0.10f) },
    };

    [Header("Background Transition")]
    [Tooltip("Duration to cross-fade between arena backgrounds")]
    public float backgroundFadeDuration = 1.0f;

    // ─── Internal background renderers ───────────────────────────────────────
    private SpriteRenderer _skyRenderer;
    private SpriteRenderer _midRenderer;
    private SpriteRenderer _groundRenderer;
    private Coroutine      _bgFadeRoutine;
    private GameManager.Arena _currentArena = GameManager.Arena.Rookie;

    /// <summary>
    /// Load and apply arena background sprites + ambient color.
    /// Called from GameManager.StartGame() when arena is selected.
    /// </summary>
    public void LoadArenaBackground(GameManager.Arena arena)
    {
        if (_currentArena == arena && _bgFadeRoutine != null) return;

        _currentArena = arena;
        ArenaBackground bg = GetArenaBackground(arena);

        Debug.Log($"[TunnelGenerator] Loading arena background: {arena}");

        // Ensure sprite renderer components exist on layer objects
        EnsureSpriteRenderer(ref _skyRenderer,    skyLayer,    "SkyBG");
        EnsureSpriteRenderer(ref _midRenderer,    midLayer,    "MidBG");
        EnsureSpriteRenderer(ref _groundRenderer, groundLayer, "GroundBG");

        if (_bgFadeRoutine != null) StopCoroutine(_bgFadeRoutine);
        _bgFadeRoutine = StartCoroutine(CrossFadeBackground(bg));

        // Set RenderSettings fog color to match arena
        RenderSettings.fogColor = bg.fogColor;
        RenderSettings.ambientLight = bg.ambientColor;
    }

    void EnsureSpriteRenderer(ref SpriteRenderer sr, Transform layer, string goName)
    {
        if (layer == null) return;
        if (sr != null && sr.transform.IsChildOf(layer)) return;

        // Check if one already exists
        sr = layer.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) return;

        // Create one
        GameObject go = new GameObject(goName);
        go.transform.SetParent(layer, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = new Vector3(30f, 18f, 1f);
        sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -10;
    }

    IEnumerator CrossFadeBackground(ArenaBackground bg)
    {
        // Fade out current
        float elapsed = 0f;
        float halfDur = backgroundFadeDuration * 0.5f;

        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / halfDur);
            SetLayerAlpha(_skyRenderer,    alpha);
            SetLayerAlpha(_midRenderer,    alpha);
            SetLayerAlpha(_groundRenderer, alpha);
            yield return null;
        }

        // Swap sprites
        if (_skyRenderer    != null) { _skyRenderer.sprite    = bg.skySprite;    ApplyArenaFallbackColor(_skyRenderer,    bg.ambientColor, 0.4f); }
        if (_midRenderer    != null) { _midRenderer.sprite    = bg.midSprite;    ApplyArenaFallbackColor(_midRenderer,    bg.ambientColor, 0.6f); }
        if (_groundRenderer != null) { _groundRenderer.sprite = bg.groundSprite; ApplyArenaFallbackColor(_groundRenderer, bg.ambientColor, 0.8f); }

        // Fade in new
        elapsed = 0f;
        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float alpha = elapsed / halfDur;
            SetLayerAlpha(_skyRenderer,    alpha);
            SetLayerAlpha(_midRenderer,    alpha);
            SetLayerAlpha(_groundRenderer, alpha);
            yield return null;
        }

        SetLayerAlpha(_skyRenderer,    1f);
        SetLayerAlpha(_midRenderer,    1f);
        SetLayerAlpha(_groundRenderer, 1f);

        _bgFadeRoutine = null;
        Debug.Log($"[TunnelGenerator] Arena background loaded: {bg.arena}");
    }

    void SetLayerAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    /// <summary>
    /// When no sprite is assigned, paint a solid color quad so the arena still looks distinct.
    /// </summary>
    void ApplyArenaFallbackColor(SpriteRenderer sr, Color baseColor, float brightness)
    {
        if (sr == null) return;
        if (sr.sprite != null) return; // sprite assigned — use it

        sr.color = new Color(
            baseColor.r * brightness,
            baseColor.g * brightness,
            baseColor.b * brightness,
            1f);

        // Create a white 1x1 pixel sprite as fallback solid color quad
        if (sr.sprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        }
    }

    ArenaBackground GetArenaBackground(GameManager.Arena arena)
    {
        foreach (var bg in arenaBackgrounds)
            if (bg.arena == arena) return bg;
        return arenaBackgrounds.Length > 0 ? arenaBackgrounds[0] : default;
    }

    /// <summary>
    /// Convenience: called from GameManager when arena is selected.
    /// </summary>
    public void ApplyArena(GameManager.Arena arena)
    {
        LoadArenaBackground(arena);
    }
}
