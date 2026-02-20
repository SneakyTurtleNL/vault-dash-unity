using UnityEngine;

/// <summary>
/// Obstacle — Data and per-obstacle behaviour for Vault Dash.
/// Handles self-scrolling, perspective scaling, and rotation (for rotating type).
/// </summary>
public class Obstacle : MonoBehaviour
{
    // ─── Obstacle Types ───────────────────────────────────────────────────────
    public enum ObstacleType
    {
        Box        = 0,   // solid crate
        Spike      = 1,   // spike strip (low)
        Rotating   = 2,   // spinning block
        Wall       = 3,   // full-lane wall
        Gate       = 4,   // arch (jump over or under)
        MovingBox  = 5,   // slow-sliding side-to-side
        LowBar     = 6,   // crouch-required bar
        Stack      = 7,   // stacked crates (wide)
    }

    [Header("Type")]
    public ObstacleType type = ObstacleType.Box;

    [Header("Lane")]
    [Tooltip("0=Left, 1=Center, 2=Right")]
    public int lane = 1;

    [Header("Movement")]
    public float scrollSpeed  = 5f;   // how fast it moves toward player
    public float rotateSpeed  = 120f; // only for Rotating type

    [Header("Perspective")]
    public float spawnZ       = 60f;  // Z at spawn (far = small)
    public bool  usePerspective = true;

    // Injected by ObstacleManager
    [HideInInspector] public TunnelGenerator tunnelGen;

    // MovingBox oscillation
    private float moveOriginX;
    private float moveAmplitude = 1.25f;  // half lane width
    private float moveFrequency = 0.8f;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        moveOriginX = transform.position.x;
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    void Update()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        Scroll();
        if (type == ObstacleType.Rotating)  Rotate();
        if (type == ObstacleType.MovingBox) Oscillate();
        if (usePerspective)                 ApplyPerspectiveScale();
    }

    void Scroll()
    {
        transform.position -= new Vector3(0f, 0f, scrollSpeed * Time.deltaTime);
    }

    void Rotate()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    void Oscillate()
    {
        float x = moveOriginX + Mathf.Sin(Time.time * Mathf.PI * 2f * moveFrequency) * moveAmplitude;
        Vector3 pos = transform.position;
        pos.x = x;
        transform.position = pos;
    }

    /// <summary>
    /// Scale obstacle based on how far it is from the player (z=0).
    /// Gives the "obstacle grows as it approaches" trapezoid illusion.
    /// </summary>
    void ApplyPerspectiveScale()
    {
        if (tunnelGen == null) return;

        float z     = transform.position.z;
        float scale = tunnelGen.GetPerspectiveScale(z);
        transform.localScale = new Vector3(scale, scale, scale);
    }

    // ─── Lane Position ────────────────────────────────────────────────────────
    public void SetLane(int laneIndex, float laneWidth)
    {
        lane = laneIndex;
        float x = (laneIndex - 1) * laneWidth; // -1, 0, +1
        Vector3 pos = transform.position;
        pos.x = x;
        transform.position = pos;
        moveOriginX = x;
    }

    // ─── Visual Setup ─────────────────────────────────────────────────────────
    public void ApplyVisualStyle()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        if (r == null) return;

        switch (type)
        {
            case ObstacleType.Box:       r.material.color = new Color(0.60f, 0.30f, 0.10f); break; // brown crate
            case ObstacleType.Spike:     r.material.color = new Color(0.85f, 0.05f, 0.05f); break; // red spike
            case ObstacleType.Rotating:  r.material.color = new Color(0.90f, 0.60f, 0.00f); break; // orange spinner
            case ObstacleType.Wall:      r.material.color = new Color(0.40f, 0.40f, 0.45f); break; // grey wall
            case ObstacleType.Gate:      r.material.color = new Color(0.20f, 0.70f, 0.90f); break; // blue gate
            case ObstacleType.MovingBox: r.material.color = new Color(0.80f, 0.20f, 0.80f); break; // purple
            case ObstacleType.LowBar:    r.material.color = new Color(0.95f, 0.80f, 0.00f); break; // yellow bar
            case ObstacleType.Stack:     r.material.color = new Color(0.40f, 0.25f, 0.10f); break; // dark brown
        }
    }

    // ─── Dimensions ──────────────────────────────────────────────────────────
    static readonly Vector3[] TypeDimensions =
    {
        new Vector3(1.2f, 1.2f, 1.2f),   // Box
        new Vector3(2.0f, 0.3f, 0.5f),   // Spike   (wide, low)
        new Vector3(1.0f, 1.0f, 1.0f),   // Rotating
        new Vector3(2.5f, 2.5f, 0.3f),   // Wall    (full height)
        new Vector3(2.5f, 2.0f, 0.3f),   // Gate    (hollow via 2 pieces — simplified as thin wall)
        new Vector3(1.2f, 1.2f, 1.2f),   // MovingBox
        new Vector3(2.5f, 0.2f, 0.5f),   // LowBar  (wide, very low — must crouch)
        new Vector3(2.0f, 2.4f, 1.2f),   // Stack   (tall + wide)
    };

    public Vector3 GetDimensions() => TypeDimensions[(int)type];
}
