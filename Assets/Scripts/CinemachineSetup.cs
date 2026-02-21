using UnityEngine;
using Cinemachine;

/// <summary>
/// CinemachineSetup — Manages the Cinemachine Virtual Camera for Vault Dash.
///
/// Cinemachine package: com.unity.cinemachine 2.9.7 (in manifest.json ✅)
///
/// FEATURES:
///  • Isometric 45° follow camera (replaces raw Camera.main positioning)
///  • Smooth follow via CinemachineTransposer body component
///  • Dynamic look-ahead when opponent is within 100 m
///  • Integrates with TunnelGenerator and GameManager state
///
/// SETUP IN UNITY EDITOR:
///  1. Cinemachine → Create Virtual Camera (from the Cinemachine menu).
///  2. Attach this script to that Virtual Camera GameObject.
///  3. The script auto-configures the VCam (Body, Lens, rotation).
///  4. Optionally assign playerTransform + opponentTransform in Inspector.
///     If left null, the script auto-finds tagged "Player" at runtime.
///
/// The script disables TunnelGenerator's manual camera placement when active.
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachineSetup : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Targets")]
    [Tooltip("Player transform — auto-found via 'Player' tag if not assigned")]
    public Transform playerTransform;

    [Tooltip("Opponent visualizer transform for look-ahead direction")]
    public Transform opponentTransform;

    [Header("Camera Settings")]
    [Tooltip("World-space offset from the follow target (matches TunnelGenerator defaults)")]
    public Vector3 cameraOffset = new Vector3(0f, 8f, -8f);

    [Tooltip("Orthographic size of the virtual camera lens")]
    public float orthoSize = 6f;

    [Tooltip("XY damping — higher = smoother / more lag")]
    [Range(0f, 3f)]
    public float bodyDampingXY = 0.5f;

    [Tooltip("Z damping")]
    [Range(0f, 3f)]
    public float bodyDampingZ = 0.8f;

    [Header("Look-Ahead")]
    [Tooltip("Distance (m) within which opponent presence triggers forward look-ahead")]
    public float lookAheadTriggerDistance = 100f;

    [Tooltip("Max Z offset added to the camera when opponent is very close")]
    public float maxLookAheadOffset = 2f;

    [Tooltip("Lerp speed for look-ahead transition")]
    [Range(0.5f, 10f)]
    public float lookAheadLerpSpeed = 3f;

    // ─── Private ──────────────────────────────────────────────────────────────
    private CinemachineVirtualCamera _vCam;
    private CinemachineTransposer    _transposer;
    private float                    _currentLookAheadZ = 0f;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        _vCam = GetComponent<CinemachineVirtualCamera>();
    }

    void Start()
    {
        AutoFindPlayer();
        ConfigureVirtualCamera();
    }

    void AutoFindPlayer()
    {
        if (playerTransform != null) return;

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerTransform = playerGO.transform;
            Debug.Log("[CinemachineSetup] Auto-found Player: " + playerGO.name);
        }
    }

    void ConfigureVirtualCamera()
    {
        if (_vCam == null) return;

        // ── Follow & LookAt ──
        if (playerTransform != null)
        {
            _vCam.Follow = playerTransform;
            _vCam.LookAt = playerTransform;
        }

        // ── Lens: orthographic, isometric size ──
        _vCam.m_Lens.Orthographic     = true;
        _vCam.m_Lens.OrthographicSize = orthoSize;

        // ── Body: Transposer with world-space offset ──
        _transposer = _vCam.GetCinemachineComponent<CinemachineTransposer>();
        if (_transposer == null)
            _transposer = _vCam.AddCinemachineComponent<CinemachineTransposer>();

        _transposer.m_FollowOffset = cameraOffset;
        _transposer.m_XDamping     = bodyDampingXY;
        _transposer.m_YDamping     = bodyDampingXY;
        _transposer.m_ZDamping     = bodyDampingZ;
        _transposer.m_BindingMode  = CinemachineTransposer.BindingMode.WorldSpace;

        // ── Aim: inherit rotation from virtual camera's transform ──
        // Keep camera at 45° isometric angle — use SameAsFollowTarget for zero aim processing.
        var aim = _vCam.GetCinemachineComponent<CinemachineSameAsFollowTarget>();
        if (aim == null)
            aim = _vCam.AddCinemachineComponent<CinemachineSameAsFollowTarget>();

        // Set isometric rotation (45° tilt, no Y/Z roll — matches TunnelGenerator)
        _vCam.transform.eulerAngles = new Vector3(45f, 0f, 0f);

        Debug.Log($"[CinemachineSetup] VCam configured — ortho {orthoSize}, offset {cameraOffset}, damping XY:{bodyDampingXY} Z:{bodyDampingZ}");
    }

    // ─── LateUpdate ───────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;
        ApplyLookAhead();
    }

    void ApplyLookAhead()
    {
        float opponentDist   = GetOpponentDistance();
        float targetZ        = 0f;

        if (opponentDist > 0f && opponentDist <= lookAheadTriggerDistance)
        {
            // Stronger look-ahead as opponent closes in
            float t    = 1f - (opponentDist / lookAheadTriggerDistance);
            targetZ    = Mathf.Lerp(0f, maxLookAheadOffset, t);
        }

        _currentLookAheadZ = Mathf.Lerp(_currentLookAheadZ, targetZ, lookAheadLerpSpeed * Time.deltaTime);

        // Nudge transposer Z offset
        if (_transposer != null)
        {
            Vector3 offset = _transposer.m_FollowOffset;
            offset.z       = cameraOffset.z + _currentLookAheadZ;
            _transposer.m_FollowOffset = offset;
        }
    }

    float GetOpponentDistance()
    {
        if (MatchManager.Instance != null)
            return MatchManager.Instance.OpponentDistance;

        if (playerTransform != null && opponentTransform != null)
            return Vector3.Distance(playerTransform.position, opponentTransform.position);

        return 0f;
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Rebind the virtual camera to a newly-spawned player.
    /// Called by GameManager.StartGame() after each SpawnPlayer().
    /// </summary>
    public void BindToPlayer(Transform player)
    {
        playerTransform = player;

        if (_vCam != null)
        {
            _vCam.Follow = player;
            _vCam.LookAt = player;
        }

        Debug.Log("[CinemachineSetup] Camera rebound to: " + (player != null ? player.name : "null"));
    }

    /// <summary>Override ortho size at runtime (e.g., zoom out for power-up).</summary>
    public void SetOrthoSize(float size)
    {
        orthoSize = size;
        if (_vCam != null)
            _vCam.m_Lens.OrthographicSize = size;
    }
}
