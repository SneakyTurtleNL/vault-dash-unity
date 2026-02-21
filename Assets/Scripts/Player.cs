using System.Collections;
using UnityEngine;

/// <summary>
/// Player — Core movement system for Vault Dash.
/// Handles 3-lane switching, jump, crouch, swipe input, animation profiles, and collision.
///
/// Week 2 additions:
///  • AudioManager integration (footsteps, jump, crouch, lane-change, collect SFX)
///  • CharacterAnimationProfile integration (per-character jump height, speed etc.)
///  • ParticleEffects integration (collect burst, jump trail)
///  • Score popup on loot collection
/// </summary>
public class Player : MonoBehaviour
{
    // ─── Enums ────────────────────────────────────────────────────────────────
    public enum Lane { Left = 0, Center = 1, Right = 2 }

    public enum CharacterProfile { AgentZero = 0, Blaze = 1, Knox = 2, Jade = 3 }

    // ─── Week 2: Character Selection ──────────────────────────────────────────
    [Header("Week 2 — Character Profile")]
    [Tooltip("VaultCharacter enum index — set from lobby/character selection")]
    public int selectedCharacterIndex = 0;

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Lane Movement")]
    public float laneWidth        = 2.5f;
    public float laneSwitchSpeed  = 14f;

    [Header("Jump")]
    public float jumpHeight       = 3f;
    public float jumpDuration     = 0.6f;   // spec: 0.6 s flight

    [Header("Crouch")]
    public float crouchDuration   = 0.8f;   // spec: 0.8 s
    public float crouchScaleY     = 0.5f;

    [Header("Character")]
    public CharacterProfile characterProfile = CharacterProfile.AgentZero;
    public Animator animator;

    [Header("Collider")]
    public BoxCollider playerCollider;

    // ─── Private State ────────────────────────────────────────────────────────
    private Lane    currentLane   = Lane.Center;
    private float   targetX;
    private bool    isJumping;
    private bool    isCrouching;

    // Swipe tracking
    private Vector2 swipeTouchStart;
    private bool    swipeBegan;
    private const float MIN_SWIPE_PX = 40f;

    // Collider sizes
    private Vector3 defaultColliderSize;
    private Vector3 crouchColliderSize;

    // Coroutine handles
    private Coroutine jumpRoutine;
    private Coroutine crouchRoutine;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (playerCollider == null)
            playerCollider = GetComponent<BoxCollider>();

        if (playerCollider != null)
        {
            defaultColliderSize = playerCollider.size;
            crouchColliderSize  = new Vector3(
                defaultColliderSize.x,
                defaultColliderSize.y * crouchScaleY,
                defaultColliderSize.z);
        }
    }

    // Week 2 runtime profile (loaded from CharacterDatabase)
    private CharacterAnimationProfile _activeProfile;

    void Start()
    {
        currentLane = Lane.Center;
        targetX     = GetLaneX(currentLane);

        // ── Week 2: Load character profile ──
        LoadCharacterProfile();
        ApplyAnimationProfile();

        // ── Week 2: Start footstep loop ──
        AudioManager.Instance?.StartFootsteps();
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        HandleKeyboardInput();
        HandleTouchInput();
        UpdateLateralMovement();
    }

    // ─── Input ────────────────────────────────────────────────────────────────
    void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) SwitchLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SwitchLane(+1);
        if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) TryJump();
        if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S)) TryCrouch();
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                swipeTouchStart = touch.position;
                swipeBegan      = true;
                break;

            case TouchPhase.Ended when swipeBegan:
                swipeBegan = false;
                Vector2 delta = touch.position - swipeTouchStart;

                if (delta.magnitude < MIN_SWIPE_PX) break;  // too short

                if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                    SwitchLane(delta.x > 0 ? +1 : -1);     // left/right swipe
                else if (delta.y > 0)
                    TryJump();                               // swipe up
                else
                    TryCrouch();                             // swipe down
                break;
        }
    }

    // ─── Lane Movement ────────────────────────────────────────────────────────
    void SwitchLane(int dir)
    {
        int newIdx = Mathf.Clamp((int)currentLane + dir, 0, 2);
        if (newIdx == (int)currentLane) return;

        currentLane = (Lane)newIdx;
        targetX     = GetLaneX(currentLane);

        if (animator != null)
            animator.SetTrigger(dir > 0 ? "SlideRight" : "SlideLeft");

        // ── Week 2 ──
        AudioManager.Instance?.PlayLaneChange();
        ParticleEffects.Instance?.LaneSwoosh(transform.position, dir);
    }

    float GetLaneX(Lane lane) => ((int)lane - 1) * laneWidth;  // -1, 0, +1

    void UpdateLateralMovement()
    {
        Vector3 pos = transform.position;
        pos.x       = Mathf.MoveTowards(pos.x, targetX, laneSwitchSpeed * Time.deltaTime);
        transform.position = pos;
    }

    // ─── Jump ─────────────────────────────────────────────────────────────────
    void TryJump()
    {
        if (isJumping || isCrouching) return;
        if (jumpRoutine != null) StopCoroutine(jumpRoutine);
        jumpRoutine = StartCoroutine(JumpRoutine());
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;
        if (animator != null) animator.SetBool("IsJumping", true);

        // ── Week 2: jump SFX + pause footsteps ──
        AudioManager.Instance?.PlayJump();
        AudioManager.Instance?.StopFootsteps();

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t      = elapsed / jumpDuration;
            float height = jumpHeight * Mathf.Sin(t * Mathf.PI); // parabolic arc

            Vector3 pos = transform.position;
            pos.y       = height;
            transform.position = pos;

            // ── Week 2: jump trail particles ──
            ParticleEffects.Instance?.JumpTrail(transform.position, t);

            yield return null;
        }

        // Land
        Vector3 land = transform.position;
        land.y       = 0f;
        transform.position = land;

        if (animator != null) animator.SetBool("IsJumping", false);
        isJumping = false;

        // ── Week 2: resume footsteps ──
        AudioManager.Instance?.StartFootsteps();
    }

    // ─── Crouch ───────────────────────────────────────────────────────────────
    void TryCrouch()
    {
        if (isCrouching || isJumping) return;
        if (crouchRoutine != null) StopCoroutine(crouchRoutine);
        crouchRoutine = StartCoroutine(CrouchRoutine());
    }

    IEnumerator CrouchRoutine()
    {
        isCrouching = true;

        // ── Week 2: crouch SFX ──
        AudioManager.Instance?.PlayCrouch();

        // Lower collider
        if (playerCollider != null) playerCollider.size = crouchColliderSize;

        // Flatten visually
        Vector3 scale = transform.localScale;
        scale.y = crouchScaleY;
        transform.localScale = scale;

        if (animator != null) animator.SetBool("IsCrouching", true);

        // Use profile's crouchDuration if available
        float duration = _activeProfile != null ? _activeProfile.crouchDuration : crouchDuration;
        yield return new WaitForSeconds(duration);

        // Restore
        scale.y = 1f;
        transform.localScale = scale;
        if (playerCollider != null) playerCollider.size = defaultColliderSize;
        if (animator != null) animator.SetBool("IsCrouching", false);

        isCrouching = false;
    }

    // ─── Week 2: Load Character Profile ───────────────────────────────────────
    void LoadCharacterProfile()
    {
        if (CharacterDatabase.Instance == null) return;

        int idx = PlayerPrefs.GetInt("SelectedCharacter", selectedCharacterIndex);
        _activeProfile = CharacterDatabase.Instance.GetProfile(idx);

        if (_activeProfile == null) return;

        // Override jump stats from profile
        jumpHeight   = _activeProfile.jumpHeight;
        jumpDuration = _activeProfile.jumpDuration;
        laneSwitchSpeed *= _activeProfile.laneSwitch;

        // Update footstep cadence
        AudioManager.Instance?.SetFootstepSpeed(_activeProfile.runSpeed);

        Debug.Log($"[Player] Loaded character profile: {_activeProfile.displayName}");
    }

    // ─── Animation Profiles ───────────────────────────────────────────────────
    void ApplyAnimationProfile()
    {
        if (animator == null) return;

        if (_activeProfile != null)
        {
            // Use data from CharacterDatabase (Week 2 path)
            animator.SetFloat("RunSpeed",    _activeProfile.runSpeed);
            animator.SetInteger("CharacterID", _activeProfile.animatorId);
        }
        else
        {
            // Legacy fallback (Week 1 enum)
            switch (characterProfile)
            {
                case CharacterProfile.AgentZero: animator.SetFloat("RunSpeed", 1.0f); animator.SetInteger("CharacterID", 0); break;
                case CharacterProfile.Blaze:     animator.SetFloat("RunSpeed", 1.2f); animator.SetInteger("CharacterID", 1); break;
                case CharacterProfile.Knox:      animator.SetFloat("RunSpeed", 0.9f); animator.SetInteger("CharacterID", 2); break;
                case CharacterProfile.Jade:      animator.SetFloat("RunSpeed", 1.1f); animator.SetInteger("CharacterID", 3); break;
            }
        }
    }

    // ─── Collision ────────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            if (animator != null) animator.SetTrigger("Die");

            // ── Week 2 ──
            AudioManager.Instance?.StopFootsteps();
            AudioManager.Instance?.PlayObstacleHit();
            ParticleEffects.Instance?.ObstacleBurst(transform.position);

            // ── Phase 2: Vignette flash + Firebase event ──
            PostProcessingManager.Instance?.VignetteFlash();
            FirebaseManager.Instance?.LogOpponentCollision(GameManager.Instance?.Distance ?? 0f);

            GameManager.Instance?.GameOver();
        }
        else if (other.CompareTag("Coin"))
        {
            int pts = 10;
            bool combo = false;

            GameManager.Instance?.AddScore(pts);

            // Check combo
            if (GameManager.Instance != null && GameManager.Instance.ComboMultiplier > 1f)
            {
                combo = true;
                pts = Mathf.RoundToInt(pts * GameManager.Instance.ComboMultiplier);
            }

            // ── Week 2 ──
            AudioManager.Instance?.PlayCoinCollect();
            ParticleEffects.Instance?.CoinBurst(other.transform.position);
            ParticleEffects.Instance?.ScorePopup(other.transform.position, pts, combo);

            // ── Phase 2: Bloom pulse + Firebase ──
            PostProcessingManager.Instance?.BloomPulse(2.5f);
            FirebaseManager.Instance?.LogLootCollected("coin");

            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Gem"))
        {
            int pts = 50;
            GameManager.Instance?.AddScore(pts);

            // ── Week 2 ──
            AudioManager.Instance?.PlayGemCollect();
            ParticleEffects.Instance?.GemBurst(other.transform.position);
            ParticleEffects.Instance?.ScorePopup(other.transform.position, pts, false);

            // ── Phase 2: Big bloom pulse + Firebase ──
            PostProcessingManager.Instance?.BloomPulse(5f);
            FirebaseManager.Instance?.LogLootCollected("gem");

            Destroy(other.gameObject);
        }
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────────
    void OnDestroy()
    {
        AudioManager.Instance?.StopFootsteps();
    }

    // ─── Public Accessors ─────────────────────────────────────────────────────
    public Lane   CurrentLane  => currentLane;
    public bool   IsJumping    => isJumping;
    public bool   IsCrouching  => isCrouching;
    public int    LaneIndex    => (int)currentLane;
}
