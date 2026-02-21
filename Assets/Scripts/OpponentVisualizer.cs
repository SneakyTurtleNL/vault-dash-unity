using System.Collections;
using UnityEngine;

/// <summary>
/// OpponentVisualizer — Renders the opponent character inside the tunnel.
///
/// Distance-based appearance rules:
///  500m+  → Opponent is only visible in the TopBar (handled by TopBarUI)
///   100m  → Opponent CHARACTER appears at far end of tunnel (small, in right lane)
///    50m  → Opponent starts animating toward player (scale up, Z approaches)
///    20m  → Close-up: screen starts subtly shaking
///     0m  → COLLISION! Slow-mo, winner flash, fade to VictoryScreen
///
/// The opponent is represented by a coloured capsule by default.
/// Swap out the opponentCharacterPrefab for a real sprite rig when art is ready.
///
/// Wire up:
///   • MatchManager → calls SetDistance(float) each frame
///   • GameManager  → calls TriggerCollision() when distance = 0
/// </summary>
public class OpponentVisualizer : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static OpponentVisualizer Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Opponent Prefab / Fallback")]
    [Tooltip("Optional custom prefab; if null a procedural capsule is used")]
    public GameObject opponentCharacterPrefab;

    [Header("Lane Settings")]
    public float laneWidth    = 2.5f;
    [Tooltip("Which lane the opponent runs in (0=left, 1=center, 2=right)")]
    public int   opponentLane = 2;    // right lane

    [Header("Tunnel Position")]
    [Tooltip("Z position where opponent first appears (far end of tunnel)")]
    public float spawnZ       = 40f;
    [Tooltip("Z position at collision (player is at z=0)")]
    public float collisionZ   = 1.5f;

    [Header("Scale")]
    [Tooltip("Scale at spawn (far away — looks tiny)")]
    public float farScale     = 0.3f;
    [Tooltip("Scale at collision point")]
    public float nearScale    = 1.2f;

    [Header("Thresholds (meters)")]
    public float appearDistance    = 100f;
    public float approachDistance  = 50f;
    public float shakeDistance     = 20f;
    public float collisionDistance = 0f;

    [Header("Screen Shake")]
    public float shakeMagnitude    = 0.05f;
    public float shakeFrequency    = 8f;

    [Header("Slow-Mo Collision")]
    public float slowMoDuration    = 0.35f;
    public float slowMoTimeScale   = 0.25f;

    [Header("References")]
    public Camera mainCamera;
    public VictoryScreen victoryScreen;

    // ─── Runtime State ────────────────────────────────────────────────────────
    private GameObject  _opponentGO;
    private Renderer    _opponentRenderer;
    private bool        _opponentVisible    = false;
    private bool        _collisionTriggered = false;
    private bool        _nearSoundPlayed    = false;

    // Approach animation
    private Coroutine   _approachRoutine;

    // Original camera position for shake
    private Vector3     _camOrigin;
    private bool        _shaking = false;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) _camOrigin = mainCamera.transform.localPosition;
        if (victoryScreen == null) victoryScreen = FindObjectOfType<VictoryScreen>();
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    /// <summary>
    /// Called every frame by MatchManager with the remaining match distance (meters).
    /// </summary>
    public void SetDistance(float distanceRemaining)
    {
        if (_collisionTriggered) return;

        // At 100m: make opponent appear
        if (distanceRemaining <= appearDistance && !_opponentVisible)
            ShowOpponent();

        // At 50m: start approach animation
        if (distanceRemaining <= approachDistance && _opponentVisible && _approachRoutine == null)
            _approachRoutine = StartCoroutine(ApproachRoutine());

        // At 20m: screen shake
        if (distanceRemaining <= shakeDistance && !_shaking && _opponentVisible)
            StartScreenShake();

        // Audio tension
        AudioManager.Instance?.UpdateTension(distanceRemaining);

        // At ~100m: play "opponent near" audio cue once
        if (distanceRemaining <= appearDistance + 5f && !_nearSoundPlayed)
        {
            _nearSoundPlayed = true;
            AudioManager.Instance?.PlayOpponentNear();
        }

        // Update opponent position based on distance
        UpdateOpponentPosition(distanceRemaining);
    }

    /// <summary>
    /// Call when distance reaches 0 (winner already determined in MatchManager).
    /// </summary>
    public void TriggerCollision(bool playerWon)
    {
        if (_collisionTriggered) return;
        _collisionTriggered = true;

        StopAllCoroutines();
        StartCoroutine(CollisionSequence(playerWon));
    }

    /// <summary>
    /// Set opponent character profile (color, name etc) from MatchManager.
    /// </summary>
    public void SetOpponentProfile(CharacterAnimationProfile profile)
    {
        if (_opponentRenderer == null || profile == null) return;

        // Tint the primitive to the character's primary color
        if (_opponentRenderer.material != null)
            _opponentRenderer.material.color = profile.primaryColor;
    }

    // ─── Spawn ────────────────────────────────────────────────────────────────
    void ShowOpponent()
    {
        if (_opponentGO != null) return;

        float xPos = (opponentLane - 1) * laneWidth;

        if (opponentCharacterPrefab != null)
        {
            _opponentGO = Instantiate(opponentCharacterPrefab,
                new Vector3(xPos, 0f, spawnZ),
                Quaternion.identity);
        }
        else
        {
            // Fallback: capsule
            _opponentGO            = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _opponentGO.name       = "Opponent_Visual";
            _opponentGO.transform.position = new Vector3(xPos, 1f, spawnZ);

            // Remove collider — visual only
            Collider col = _opponentGO.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Assign a distinctive color
            _opponentRenderer = _opponentGO.GetComponent<Renderer>();
            if (_opponentRenderer != null)
                _opponentRenderer.material.color = new Color(1f, 0.3f, 0.1f); // red-orange
        }

        _opponentGO.transform.localScale = Vector3.one * farScale;
        _opponentVisible = true;

        Debug.Log("[OpponentVisualizer] Opponent appeared in tunnel at 100m!");
    }

    // ─── Position Update ──────────────────────────────────────────────────────
    void UpdateOpponentPosition(float distanceRemaining)
    {
        if (_opponentGO == null) return;

        // Map distance (appearDistance → 0) to Z position (spawnZ → collisionZ)
        float t  = Mathf.Clamp01(1f - (distanceRemaining / appearDistance));
        float z  = Mathf.Lerp(spawnZ, collisionZ, t);
        float sc = Mathf.Lerp(farScale, nearScale, t);

        Vector3 pos = _opponentGO.transform.position;
        pos.z = z;
        _opponentGO.transform.position   = pos;
        _opponentGO.transform.localScale = Vector3.one * sc;
    }

    // ─── Approach Animation (50m–0m) ──────────────────────────────────────────
    IEnumerator ApproachRoutine()
    {
        // Add a subtle bob to the opponent to give life
        float elapsed = 0f;
        while (_opponentGO != null && !_collisionTriggered)
        {
            elapsed += Time.deltaTime;

            // Slight vertical bob
            if (_opponentGO != null)
            {
                Vector3 pos = _opponentGO.transform.position;
                float yBase = (_opponentGO.transform.localScale.y * 0.5f);
                pos.y = yBase + Mathf.Abs(Mathf.Sin(elapsed * 6f)) * 0.15f;
                _opponentGO.transform.position = pos;
            }

            yield return null;
        }
    }

    // ─── Screen Shake ─────────────────────────────────────────────────────────
    void StartScreenShake()
    {
        if (_shaking) return;
        _shaking = true;
        StartCoroutine(ScreenShakeRoutine());
    }

    IEnumerator ScreenShakeRoutine()
    {
        while (!_collisionTriggered && mainCamera != null)
        {
            // Oscillating shake that gets stronger as opponent gets closer
            float x = Mathf.Sin(Time.time * shakeFrequency * 2f) * shakeMagnitude;
            float y = Mathf.Cos(Time.time * shakeFrequency * 1.5f) * shakeMagnitude * 0.5f;
            mainCamera.transform.localPosition = _camOrigin + new Vector3(x, y, 0f);
            yield return null;
        }

        // Reset
        if (mainCamera != null)
            mainCamera.transform.localPosition = _camOrigin;
    }

    // ─── Collision Sequence ───────────────────────────────────────────────────
    IEnumerator CollisionSequence(bool playerWon)
    {
        Debug.Log($"[OpponentVisualizer] COLLISION! Player won: {playerWon}");

        // 1. Slow-motion flash
        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Flash opponent (red if lose, gold if win)
        if (_opponentRenderer != null)
        {
            Color flashColor = playerWon ? Color.yellow : Color.red;
            StartCoroutine(FlashColor(_opponentRenderer, flashColor, 0.3f));
        }

        yield return new WaitForSecondsRealtime(slowMoDuration);

        // 2. Restore time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // 3. Stop shaking
        _shaking = false;

        // 4. Audio
        if (playerWon)
            AudioManager.Instance?.PlayVictory();
        else
            AudioManager.Instance?.PlayDefeat();

        AudioManager.Instance?.StopFootsteps();
        AudioManager.Instance?.ResetTension();

        // 5. Brief pause before screen transition
        yield return new WaitForSecondsRealtime(0.15f);

        // 6. Hide opponent GO
        if (_opponentGO != null)
        {
            Destroy(_opponentGO);
            _opponentGO = null;
        }

        // 7. Trigger victory screen
        if (victoryScreen != null)
            victoryScreen.Show(playerWon);
        else
            GameManager.Instance?.SetState(GameManager.GameState.GameOver);
    }

    IEnumerator FlashColor(Renderer r, Color flashColor, float duration)
    {
        Color original = r.material.color;
        float elapsed  = 0f;
        int   flashes  = 4;

        for (int i = 0; i < flashes * 2; i++)
        {
            r.material.color = (i % 2 == 0) ? flashColor : original;
            yield return new WaitForSecondsRealtime(duration / (flashes * 2f));
        }
        r.material.color = original;
    }

    // ─── Reset (call when starting a new match) ───────────────────────────────
    public void ResetVisualizer()
    {
        _collisionTriggered = false;
        _opponentVisible    = false;
        _nearSoundPlayed    = false;
        _shaking            = false;

        if (_approachRoutine != null) StopCoroutine(_approachRoutine);
        _approachRoutine = null;

        if (_opponentGO != null)
        {
            Destroy(_opponentGO);
            _opponentGO = null;
        }

        if (mainCamera != null)
            mainCamera.transform.localPosition = _camOrigin;

        AudioManager.Instance?.ResetTension();
    }

    // ─── Gizmos (Editor Debug) ────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        float xPos = (opponentLane - 1) * laneWidth;

        // Appearance point (100m threshold = spawnZ)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(xPos, 1f, spawnZ), 0.5f);
        UnityEditor.Handles.Label(new Vector3(xPos + 1f, 1f, spawnZ), "Appears (100m)");

        // Collision point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(xPos, 1f, collisionZ), 0.5f);
        UnityEditor.Handles.Label(new Vector3(xPos + 1f, 1f, collisionZ), "Collision (0m)");
    }
#endif
}
