using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CharacterAnimationProfile — Per-character animation + movement data.
///
/// Each of the 10 playable characters has unique:
///  • Run speed / stride style (animator float)
///  • Jump height + duration
///  • Sprite frames (run, jump, crouch, idle, victory)
///  • Victory emote type
///
/// Attach a CharacterDatabase ScriptableObject or populate via the
/// CharacterDatabase singleton at runtime.
/// </summary>

// ─── Enums ────────────────────────────────────────────────────────────────────
public enum VaultCharacter
{
    AgentZero = 0,
    Blaze     = 1,
    Knox      = 2,
    Jade      = 3,
    Phantom   = 4,
    Storm     = 5,
    Rook      = 6,
    Cipher    = 7,
    Vex       = 8,
    Titan     = 9,
}

public enum VictoryEmote
{
    Spin      = 0,
    Jump      = 1,
    ThumpsUp  = 2,
    Dance     = 3,
    Flex      = 4,
    Salute    = 5,
}

// ─── Profile Data ─────────────────────────────────────────────────────────────
[System.Serializable]
public class CharacterAnimationProfile
{
    [Header("Identity")]
    public VaultCharacter characterId;
    public string         displayName;
    public Color          primaryColor   = Color.white;
    public Color          accentColor    = Color.cyan;

    [Header("Run Style")]
    [Tooltip("Animator RunSpeed float — higher = faster stride")]
    public float runSpeed    = 1.0f;
    [Tooltip("Animator CharacterID int — selects animator sub-state")]
    public int   animatorId  = 0;
    [Tooltip("Lateral lane-switch speed multiplier")]
    public float laneSwitch  = 1.0f;

    [Header("Jump")]
    public float jumpHeight   = 3f;
    public float jumpDuration = 0.6f;

    [Header("Crouch")]
    public float crouchDuration = 0.8f;

    [Header("Victory")]
    public VictoryEmote victoryEmote = VictoryEmote.ThumpsUp;
    [Tooltip("Duration of the victory animation showcase (seconds)")]
    public float victoryDuration = 2.5f;

    [Header("Sprites (assign in Inspector)")]
    public Sprite   idleSprite;
    public Sprite[] runFrames;      // looped for sprite-based animation
    public Sprite   jumpSprite;
    public Sprite   crouchSprite;
    public Sprite   victorySprite;
    public Sprite   portraitSprite; // used in TopBar + VictoryScreen
}

// ─── Database ─────────────────────────────────────────────────────────────────
/// <summary>
/// Holds all 10 character profiles.  Add as a component to a persistent
/// GameObject (GameManager or a dedicated CharacterDatabase GO).
/// </summary>
public class CharacterDatabase : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static CharacterDatabase Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Character Profiles (10 total)")]
    public List<CharacterAnimationProfile> profiles = new List<CharacterAnimationProfile>();

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (profiles.Count == 0)
            BuildDefaultProfiles();
    }

    // ─── Default Profile Factory ──────────────────────────────────────────────
    /// <summary>
    /// Creates hardcoded profiles for all 10 characters so the game works
    /// without any Inspector wiring.  Sprites remain null until assigned.
    /// </summary>
    void BuildDefaultProfiles()
    {
        profiles = new List<CharacterAnimationProfile>
        {
            // 0 – Agent Zero: tactical, sharp, military
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.AgentZero,
                displayName    = "Agent Zero",
                primaryColor   = new Color(0.10f, 0.10f, 0.12f),
                accentColor    = new Color(0.00f, 0.80f, 1.00f),
                runSpeed       = 1.0f,
                animatorId     = 0,
                laneSwitch     = 1.0f,
                jumpHeight     = 3.0f,
                jumpDuration   = 0.60f,
                crouchDuration = 0.80f,
                victoryEmote   = VictoryEmote.Salute,
                victoryDuration= 2.5f,
            },
            // 1 – Blaze: fast, aggressive, fire motif
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.Blaze,
                displayName    = "Blaze",
                primaryColor   = new Color(0.90f, 0.30f, 0.00f),
                accentColor    = new Color(1.00f, 0.80f, 0.00f),
                runSpeed       = 1.25f,
                animatorId     = 1,
                laneSwitch     = 1.15f,
                jumpHeight     = 3.2f,
                jumpDuration   = 0.55f,
                crouchDuration = 0.70f,
                victoryEmote   = VictoryEmote.Dance,
                victoryDuration= 2.0f,
            },
            // 2 – Knox: heavy, slow, tank
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.Knox,
                displayName    = "Knox",
                primaryColor   = new Color(0.40f, 0.35f, 0.30f),
                accentColor    = new Color(0.80f, 0.60f, 0.20f),
                runSpeed       = 0.85f,
                animatorId     = 2,
                laneSwitch     = 0.85f,
                jumpHeight     = 2.5f,
                jumpDuration   = 0.70f,
                crouchDuration = 0.90f,
                victoryEmote   = VictoryEmote.Flex,
                victoryDuration= 3.0f,
            },
            // 3 – Jade: fluid, snake-like, stealth
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.Jade,
                displayName    = "Jade",
                primaryColor   = new Color(0.10f, 0.50f, 0.30f),
                accentColor    = new Color(0.40f, 1.00f, 0.50f),
                runSpeed       = 1.10f,
                animatorId     = 3,
                laneSwitch     = 1.20f,
                jumpHeight     = 3.5f,
                jumpDuration   = 0.65f,
                crouchDuration = 0.75f,
                victoryEmote   = VictoryEmote.Spin,
                victoryDuration= 2.5f,
            },
            // 4 – Phantom: ghost, ethereal, semi-transparent
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.Phantom,
                displayName    = "Phantom",
                primaryColor   = new Color(0.70f, 0.70f, 0.90f),
                accentColor    = new Color(0.50f, 0.00f, 1.00f),
                runSpeed       = 1.05f,
                animatorId     = 4,
                laneSwitch     = 1.10f,
                jumpHeight     = 3.8f,
                jumpDuration   = 0.70f,
                crouchDuration = 0.80f,
                victoryEmote   = VictoryEmote.ThumpsUp,
                victoryDuration= 2.0f,
            },
            // 5 – Storm: electric, lightning
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.Storm,
                displayName    = "Storm",
                primaryColor   = new Color(0.15f, 0.15f, 0.50f),
                accentColor    = new Color(0.60f, 0.80f, 1.00f),
                runSpeed       = 1.20f,
                animatorId     = 5,
                laneSwitch     = 1.25f,
                jumpHeight     = 3.3f,
                jumpDuration   = 0.55f,
                crouchDuration = 0.70f,
                victoryEmote   = VictoryEmote.Dance,
                victoryDuration= 2.0f,
            },
            // 6 – Rook: chess piece, noble, methodical
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.Rook,
                displayName    = "Rook",
                primaryColor   = new Color(0.85f, 0.85f, 0.80f),
                accentColor    = new Color(1.00f, 0.85f, 0.30f),
                runSpeed       = 0.95f,
                animatorId     = 6,
                laneSwitch     = 0.95f,
                jumpHeight     = 2.8f,
                jumpDuration   = 0.65f,
                crouchDuration = 0.85f,
                victoryEmote   = VictoryEmote.Salute,
                victoryDuration= 2.5f,
            },
            // 7 – Cipher: hacker, digital, neon
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.Cipher,
                displayName    = "Cipher",
                primaryColor   = new Color(0.05f, 0.05f, 0.10f),
                accentColor    = new Color(0.00f, 1.00f, 0.50f),
                runSpeed       = 1.15f,
                animatorId     = 7,
                laneSwitch     = 1.15f,
                jumpHeight     = 3.1f,
                jumpDuration   = 0.60f,
                crouchDuration = 0.75f,
                victoryEmote   = VictoryEmote.ThumpsUp,
                victoryDuration= 2.0f,
            },
            // 8 – Vex: rebel, punk, chaos
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.Vex,
                displayName    = "Vex",
                primaryColor   = new Color(0.60f, 0.00f, 0.40f),
                accentColor    = new Color(1.00f, 0.00f, 0.80f),
                runSpeed       = 1.30f,
                animatorId     = 8,
                laneSwitch     = 1.30f,
                jumpHeight     = 3.6f,
                jumpDuration   = 0.50f,
                crouchDuration = 0.65f,
                victoryEmote   = VictoryEmote.Dance,
                victoryDuration= 2.0f,
            },
            // 9 – Titan: colossal, slow, unstoppable
            new CharacterAnimationProfile
            {
                characterId    = VaultCharacter.Titan,
                displayName    = "Titan",
                primaryColor   = new Color(0.50f, 0.20f, 0.10f),
                accentColor    = new Color(1.00f, 0.50f, 0.10f),
                runSpeed       = 0.75f,
                animatorId     = 9,
                laneSwitch     = 0.80f,
                jumpHeight     = 2.2f,
                jumpDuration   = 0.80f,
                crouchDuration = 1.00f,
                victoryEmote   = VictoryEmote.Flex,
                victoryDuration= 3.0f,
            },
        };

        Debug.Log($"[CharacterDatabase] Built {profiles.Count} default profiles.");
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    public CharacterAnimationProfile GetProfile(VaultCharacter id)
    {
        foreach (var p in profiles)
            if (p.characterId == id) return p;

        Debug.LogWarning($"[CharacterDatabase] Profile not found for {id}, using default.");
        return profiles.Count > 0 ? profiles[0] : null;
    }

    public CharacterAnimationProfile GetProfile(int index)
    {
        if (index >= 0 && index < profiles.Count) return profiles[index];
        return profiles.Count > 0 ? profiles[0] : null;
    }

    public int ProfileCount => profiles.Count;
}
