using UnityEngine;

/// <summary>
/// SpineCharacterController — Skeletal animation controller for Vault Dash characters.
///
/// Architecture:
///   • When SPINE_AVAILABLE is defined: uses Spine.Unity SkeletonAnimation
///   • Fallback: Unity legacy Animator with same animation name triggers
///
/// Spine Setup (Week 3):
///   1. Download Spine Unity Runtime from esotericsoftware.com
///   2. Import spine-unity-*.unitypackage
///   3. Add SPINE_AVAILABLE to Scripting Define Symbols
///   4. Export each character from Spine editor → .skel + .atlas → Assets/Spine/
///   5. Assign SkeletonAnimation component on player prefab
///
/// Animation Names (must match Spine timeline names):
///   "run"      — looping run cycle
///   "idle"     — looping idle stance
///   "jump"     — one-shot jump arc
///   "land"     — one-shot landing snap
///   "crouch"   — looping crouch
///   "victory"  — one-shot celebration
///   "defeat"   — one-shot slump
///
/// Per-Character Tuning (CharacterSpineProfile):
///   Agent Zero: tactical run, rigid posture, sharp limb snaps
///   Blaze:      fast aggressive lean, dynamic jump
///   Knox:       heavy planted steps, power crouch
///   Jade:       fluid snake-like motion, low centre of gravity
///   Vector:     acrobatic bouncy stride
///   Cipher:     mechanical stuttered motion
///   Nova:       floaty ethereal animation, slight glow trail
///   Ryze:       glitchy teleport-step run
///   Titan:      slow but impactful, ground shake cues
///   Echo:       mirrored twin-echo animation
/// </summary>
public class SpineCharacterController : MonoBehaviour
{
    // ─── Animation Names ──────────────────────────────────────────────────────
    public const string ANIM_RUN     = "run";
    public const string ANIM_IDLE    = "idle";
    public const string ANIM_JUMP    = "jump";
    public const string ANIM_LAND    = "land";
    public const string ANIM_CROUCH  = "crouch";
    public const string ANIM_VICTORY = "victory";
    public const string ANIM_DEFEAT  = "defeat";

#if SPINE_AVAILABLE
    // ─── Spine References ─────────────────────────────────────────────────────
    [Header("Spine")]
    public Spine.Unity.SkeletonAnimation skeletonAnimation;

    [Header("Spine Track Config")]
    [Tooltip("Track 0 = base body motion (run/jump/crouch)")]
    public int baseTrack = 0;
    [Tooltip("Track 1 = additive overlays (victory pulse etc.)")]
    public int overlayTrack = 1;

    private Spine.AnimationState _spineState;
    private Spine.Skeleton       _skeleton;
#endif

    // ─── Fallback Animator ────────────────────────────────────────────────────
    [Header("Fallback")]
    public Animator fallbackAnimator;

    // ─── Per-Character Tuning ─────────────────────────────────────────────────
    [Header("Character Profile")]
    public CharacterSpineProfile profile;

    // ─── State ────────────────────────────────────────────────────────────────
    private string _currentAnim = "";
    private bool   _isGrounded  = true;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
#if SPINE_AVAILABLE
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<Spine.Unity.SkeletonAnimation>();

        if (skeletonAnimation != null)
        {
            _spineState = skeletonAnimation.AnimationState;
            _skeleton   = skeletonAnimation.Skeleton;

            // Apply character-specific skin (if profile sets one)
            if (profile != null && !string.IsNullOrEmpty(profile.spineSkin))
            {
                var skin = _skeleton.Data.FindSkin(profile.spineSkin);
                if (skin != null) _skeleton.SetSkin(skin);
            }

            // Set initial run speed scale
            if (profile != null)
                skeletonAnimation.timeScale = profile.runAnimSpeed;

            Debug.Log($"[SpineController] Spine active for {(profile != null ? profile.characterName : "Unknown")}");
        }
        else
        {
            Debug.LogWarning("[SpineController] No SkeletonAnimation found — using fallback Animator.");
        }
#endif

        if (fallbackAnimator == null)
            fallbackAnimator = GetComponent<Animator>();
    }

    // ─── Public Animation API ─────────────────────────────────────────────────

    /// <summary>Start looping run animation.</summary>
    public void SetRunning(bool running)
    {
        string anim = running ? ANIM_RUN : ANIM_IDLE;
        if (_currentAnim == anim) return;
        _currentAnim = anim;
        PlayLoop(anim);
    }

    /// <summary>Trigger jump arc (one-shot → auto return to run).</summary>
    public void TriggerJump()
    {
        _isGrounded = false;
        PlayOnce(ANIM_JUMP, onComplete: () =>
        {
            // After jump peaks, keep playing until grounded
        });

        if (profile != null && profile.jumpScaleBoost > 0f)
        {
            // Quick squash-stretch punch
            StartCoroutine(SquashStretch(profile.jumpScaleBoost));
        }
    }

    /// <summary>Call when player lands after jump.</summary>
    public void TriggerLand()
    {
        _isGrounded = true;
        PlayOnce(ANIM_LAND, onComplete: () => PlayLoop(ANIM_RUN));
    }

    /// <summary>Start/stop crouch animation.</summary>
    public void SetCrouching(bool crouching)
    {
        if (crouching)
            PlayLoop(ANIM_CROUCH);
        else
            PlayLoop(ANIM_RUN);
    }

    /// <summary>Victory celebration one-shot.</summary>
    public void TriggerVictory()
    {
        PlayOnce(ANIM_VICTORY, loop: false, onComplete: () => PlayLoop(ANIM_IDLE));
    }

    /// <summary>Defeat slump one-shot.</summary>
    public void TriggerDefeat()
    {
        PlayOnce(ANIM_DEFEAT, loop: false, onComplete: null);
    }

    // ─── Playback Internals ───────────────────────────────────────────────────
    void PlayLoop(string animName)
    {
#if SPINE_AVAILABLE
        if (_spineState != null)
        {
            var entry = _spineState.SetAnimation(baseTrack, animName, true);
            if (profile != null && animName == ANIM_RUN)
                entry.TimeScale = profile.runAnimSpeed;
            else
                entry.TimeScale = 1f;
            return;
        }
#endif
        // Fallback: Unity Animator
        if (fallbackAnimator != null)
            fallbackAnimator.Play(animName, 0);
    }

    void PlayOnce(string animName, bool loop = false, System.Action onComplete = null)
    {
#if SPINE_AVAILABLE
        if (_spineState != null)
        {
            var entry = _spineState.SetAnimation(baseTrack, animName, loop);
            entry.TimeScale = 1f;
            if (onComplete != null)
                entry.Complete += (e) => onComplete?.Invoke();
            return;
        }
#endif
        // Fallback
        if (fallbackAnimator != null)
        {
            fallbackAnimator.Play(animName, 0);
            if (onComplete != null)
                StartCoroutine(WaitForAnimComplete(animName, onComplete));
        }
    }

    System.Collections.IEnumerator WaitForAnimComplete(string animName, System.Action onComplete)
    {
        // Wait until animator exits this state
        yield return null;
        while (fallbackAnimator != null &&
               fallbackAnimator.GetCurrentAnimatorStateInfo(0).IsName(animName) &&
               fallbackAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }
        onComplete?.Invoke();
    }

    System.Collections.IEnumerator SquashStretch(float boost)
    {
        Vector3 original = transform.localScale;

        // Squash on jump
        float elapsed = 0f;
        float dur = 0.1f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            transform.localScale = Vector3.Lerp(original, new Vector3(original.x * (1f + boost), original.y * (1f - boost * 0.5f), original.z), t);
            yield return null;
        }

        // Stretch at apex
        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.25f;
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(original.x * (1f - boost * 0.3f), original.y * (1f + boost), original.z), t);
            yield return null;
        }

        // Restore
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.15f;
            transform.localScale = Vector3.Lerp(transform.localScale, original, t);
            yield return null;
        }
        transform.localScale = original;
    }

    // ─── Speed-Reactive Animation ─────────────────────────────────────────────
    /// <summary>
    /// Call from Player.cs every frame to drive run cycle speed.
    /// speedMultiplier: 1.0 = base speed, 1.5 = blaze-fast
    /// </summary>
    public void SetRunSpeed(float speedMultiplier)
    {
#if SPINE_AVAILABLE
        if (_spineState != null && _currentAnim == ANIM_RUN)
        {
            var entry = _spineState.GetCurrent(baseTrack);
            if (entry != null && entry.Animation.Name == ANIM_RUN)
                entry.TimeScale = (profile != null ? profile.runAnimSpeed : 1f) * speedMultiplier;
        }
#endif
        if (fallbackAnimator != null)
            fallbackAnimator.speed = speedMultiplier;
    }
}

// ─── Per-Character Spine Profile ──────────────────────────────────────────────
[System.Serializable]
public class CharacterSpineProfile
{
    [Header("Identity")]
    public string characterName = "Agent Zero";
    public string spineSkin     = "default";       // Spine skin name

    [Header("Run Cycle")]
    [Tooltip("TimeScale multiplier for run animation (Blaze=1.4, Knox=0.8)")]
    public float runAnimSpeed   = 1.0f;

    [Header("Jump")]
    [Tooltip("Squash-stretch boost factor on jump (0=off, 0.3=punchy)")]
    public float jumpScaleBoost = 0.2f;

    [Header("Step")]
    [Tooltip("Foot impact sound pitch (0.8=deep Knox, 1.2=light Jade)")]
    public float footstepPitch  = 1.0f;

    // ── Preset Profiles ──────────────────────────────────────────────────────
    public static CharacterSpineProfile[] AllProfiles = new CharacterSpineProfile[]
    {
        new CharacterSpineProfile { characterName = "Agent Zero", spineSkin = "agent_zero", runAnimSpeed = 1.0f, jumpScaleBoost = 0.20f, footstepPitch = 1.00f },
        new CharacterSpineProfile { characterName = "Blaze",      spineSkin = "blaze",      runAnimSpeed = 1.4f, jumpScaleBoost = 0.35f, footstepPitch = 1.15f },
        new CharacterSpineProfile { characterName = "Knox",       spineSkin = "knox",       runAnimSpeed = 0.8f, jumpScaleBoost = 0.15f, footstepPitch = 0.80f },
        new CharacterSpineProfile { characterName = "Jade",       spineSkin = "jade",       runAnimSpeed = 1.1f, jumpScaleBoost = 0.10f, footstepPitch = 1.10f },
        new CharacterSpineProfile { characterName = "Vector",     spineSkin = "vector",     runAnimSpeed = 1.2f, jumpScaleBoost = 0.40f, footstepPitch = 1.05f },
        new CharacterSpineProfile { characterName = "Cipher",     spineSkin = "cipher",     runAnimSpeed = 0.9f, jumpScaleBoost = 0.25f, footstepPitch = 0.90f },
        new CharacterSpineProfile { characterName = "Nova",       spineSkin = "nova",       runAnimSpeed = 1.05f,jumpScaleBoost = 0.30f, footstepPitch = 1.20f },
        new CharacterSpineProfile { characterName = "Ryze",       spineSkin = "ryze",       runAnimSpeed = 1.3f, jumpScaleBoost = 0.45f, footstepPitch = 1.00f },
        new CharacterSpineProfile { characterName = "Titan",      spineSkin = "titan",      runAnimSpeed = 0.7f, jumpScaleBoost = 0.10f, footstepPitch = 0.70f },
        new CharacterSpineProfile { characterName = "Echo",       spineSkin = "echo",       runAnimSpeed = 1.0f, jumpScaleBoost = 0.20f, footstepPitch = 1.00f },
    };
}
