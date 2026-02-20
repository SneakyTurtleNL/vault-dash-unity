using System.Collections;
using UnityEngine;

/// <summary>
/// Player — Core movement system for Vault Dash.
/// Handles 3-lane switching, jump, crouch, swipe input, animation profiles, and collision.
/// </summary>
public class Player : MonoBehaviour
{
    // ─── Enums ────────────────────────────────────────────────────────────────
    public enum Lane { Left = 0, Center = 1, Right = 2 }

    public enum CharacterProfile { AgentZero = 0, Blaze = 1, Knox = 2, Jade = 3 }

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

    void Start()
    {
        currentLane = Lane.Center;
        targetX     = GetLaneX(currentLane);
        ApplyAnimationProfile();
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

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t      = elapsed / jumpDuration;
            float height = jumpHeight * Mathf.Sin(t * Mathf.PI); // parabolic arc

            Vector3 pos = transform.position;
            pos.y       = height;
            transform.position = pos;

            yield return null;
        }

        // Land
        Vector3 land = transform.position;
        land.y       = 0f;
        transform.position = land;

        if (animator != null) animator.SetBool("IsJumping", false);
        isJumping = false;
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

        // Lower collider
        if (playerCollider != null) playerCollider.size = crouchColliderSize;

        // Flatten visually
        Vector3 scale = transform.localScale;
        scale.y = crouchScaleY;
        transform.localScale = scale;

        if (animator != null) animator.SetBool("IsCrouching", true);

        yield return new WaitForSeconds(crouchDuration);

        // Restore
        scale.y = 1f;
        transform.localScale = scale;
        if (playerCollider != null) playerCollider.size = defaultColliderSize;
        if (animator != null) animator.SetBool("IsCrouching", false);

        isCrouching = false;
    }

    // ─── Animation Profiles ───────────────────────────────────────────────────
    void ApplyAnimationProfile()
    {
        if (animator == null) return;

        // Each character has a unique run speed + ID for the animator
        switch (characterProfile)
        {
            case CharacterProfile.AgentZero: animator.SetFloat("RunSpeed", 1.0f); animator.SetInteger("CharacterID", 0); break;
            case CharacterProfile.Blaze:     animator.SetFloat("RunSpeed", 1.2f); animator.SetInteger("CharacterID", 1); break;
            case CharacterProfile.Knox:      animator.SetFloat("RunSpeed", 0.9f); animator.SetInteger("CharacterID", 2); break;
            case CharacterProfile.Jade:      animator.SetFloat("RunSpeed", 1.1f); animator.SetInteger("CharacterID", 3); break;
        }
    }

    // ─── Collision ────────────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            if (animator != null) animator.SetTrigger("Die");
            GameManager.Instance?.GameOver();
        }
        else if (other.CompareTag("Coin"))
        {
            GameManager.Instance?.AddScore(10);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Gem"))
        {
            GameManager.Instance?.AddScore(50);
            Destroy(other.gameObject);
        }
    }

    // ─── Public Accessors ─────────────────────────────────────────────────────
    public Lane   CurrentLane  => currentLane;
    public bool   IsJumping    => isJumping;
    public bool   IsCrouching  => isCrouching;
    public int    LaneIndex    => (int)currentLane;
}
