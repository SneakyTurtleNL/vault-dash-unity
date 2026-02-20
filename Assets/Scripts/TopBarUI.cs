using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TopBarUI — 1v1 opponent preview bar.
///
/// Layout (always visible during game):
/// ┌──────────────────────────────────────────────────────────────────┐
/// │ [SkinIcon] YOU (Lvl 12)   ◄══ 500m ══►   OPPONENT (Lvl 8) [Icon]│
/// │ [HP bar]  ████████░░       Dist ████░░    ████░░░░░░  [HP bar]  │
/// │ [Skill]                                              [Skill]     │
/// └──────────────────────────────────────────────────────────────────┘
///
/// Wire up all fields in the Inspector or via MatchManager at runtime.
/// </summary>
public class TopBarUI : MonoBehaviour
{
    // ─── Player Side ──────────────────────────────────────────────────────────
    [Header("Your Side")]
    public TMP_Text    yourNameText;
    public TMP_Text    yourLevelText;
    public Image       yourSkinPreview;       // small character icon
    public Slider      yourHPBar;
    public TMP_Text    yourHPText;
    public Image       yourSkillIcon;         // active skill icon
    public TMP_Text    yourSkillCooldown;     // "3s" countdown

    // ─── Opponent Side ────────────────────────────────────────────────────────
    [Header("Opponent Side")]
    public TMP_Text    opponentNameText;
    public TMP_Text    opponentLevelText;
    public Image       opponentSkinPreview;
    public Slider      opponentHPBar;
    public TMP_Text    opponentHPText;
    public Image       opponentSkillIcon;
    public TMP_Text    opponentSkillCooldown;

    // ─── Distance ─────────────────────────────────────────────────────────────
    [Header("Distance")]
    public TMP_Text    distanceText;     // "342m"
    public Slider      distanceBar;      // full = 500m, empty = 0m
    public float       matchDistance     = 500f;  // total match distance

    // ─── Runtime Data ─────────────────────────────────────────────────────────
    // Set these from MatchManager when match starts
    [HideInInspector] public string yourName      = "YOU";
    [HideInInspector] public int    yourLevel      = 1;
    [HideInInspector] public string opponentName  = "OPPONENT";
    [HideInInspector] public int    opponentLevel  = 1;

    // HP values (0–100)
    [HideInInspector] public float  yourHP         = 100f;
    [HideInInspector] public float  opponentHP     = 100f;

    // Opponent data synced from Nakama
    [HideInInspector] public float  opponentDistance = 0f;

    // Active skill data
    [HideInInspector] public Sprite yourSkillSprite;
    [HideInInspector] public float  yourSkillTimer  = 0f;
    [HideInInspector] public Sprite opponentSkillSprite;
    [HideInInspector] public float  opponentSkillTimer = 0f;

    // ─── Private ──────────────────────────────────────────────────────────────
    private float currentDistance = 500f;  // starts at max, counts down

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        RefreshNameLabels();
        SetYourHP(100f);
        SetOpponentHP(100f);
        SetDistance(matchDistance);
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    void Update()
    {
        if (GameManager.Instance?.CurrentState != GameManager.GameState.Playing) return;

        // Distance: derived from GameManager (your run) vs opponent's synced position
        float yourDist     = GameManager.Instance.Distance;
        currentDistance    = Mathf.Max(0f, matchDistance - yourDist);

        UpdateDistanceDisplay(currentDistance);
        UpdateSkillCooldowns();

        // HP simulation: HP decreases if hits taken (driven externally via MatchManager)
        // For now just keep whatever was set
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    public void RefreshNameLabels()
    {
        SafeSetText(yourNameText,     yourName);
        SafeSetText(yourLevelText,    $"Lvl {yourLevel}");
        SafeSetText(opponentNameText, opponentName);
        SafeSetText(opponentLevelText,$"Lvl {opponentLevel}");
    }

    public void SetYourHP(float hp)
    {
        yourHP = Mathf.Clamp(hp, 0f, 100f);
        if (yourHPBar  != null) yourHPBar.value  = yourHP / 100f;
        SafeSetText(yourHPText, $"{Mathf.RoundToInt(yourHP)}%");
    }

    public void SetOpponentHP(float hp)
    {
        opponentHP = Mathf.Clamp(hp, 0f, 100f);
        if (opponentHPBar != null) opponentHPBar.value = opponentHP / 100f;
        SafeSetText(opponentHPText, $"{Mathf.RoundToInt(opponentHP)}%");
    }

    public void SetDistance(float distance)
    {
        currentDistance = Mathf.Max(0f, distance);
        UpdateDistanceDisplay(currentDistance);
    }

    void UpdateDistanceDisplay(float dist)
    {
        SafeSetText(distanceText, $"{Mathf.RoundToInt(dist)}m");
        if (distanceBar != null)
            distanceBar.value = dist / matchDistance;
    }

    public void SetYourSkin(Sprite sprite)
    {
        if (yourSkinPreview != null) yourSkinPreview.sprite = sprite;
    }

    public void SetOpponentSkin(Sprite sprite)
    {
        if (opponentSkinPreview != null) opponentSkinPreview.sprite = sprite;
    }

    public void ActivateYourSkill(Sprite icon, float durationSeconds)
    {
        yourSkillSprite = icon;
        yourSkillTimer  = durationSeconds;
        if (yourSkillIcon != null)
        {
            yourSkillIcon.sprite  = icon;
            yourSkillIcon.enabled = icon != null;
        }
    }

    public void ActivateOpponentSkill(Sprite icon, float durationSeconds)
    {
        opponentSkillSprite = icon;
        opponentSkillTimer  = durationSeconds;
        if (opponentSkillIcon != null)
        {
            opponentSkillIcon.sprite  = icon;
            opponentSkillIcon.enabled = icon != null;
        }
    }

    void UpdateSkillCooldowns()
    {
        // Tick timers
        if (yourSkillTimer > 0f)
        {
            yourSkillTimer -= Time.deltaTime;
            SafeSetText(yourSkillCooldown, $"{Mathf.CeilToInt(yourSkillTimer)}s");
            if (yourSkillTimer <= 0f)
            {
                yourSkillTimer = 0f;
                if (yourSkillIcon != null) yourSkillIcon.enabled = false;
                SafeSetText(yourSkillCooldown, "");
            }
        }

        if (opponentSkillTimer > 0f)
        {
            opponentSkillTimer -= Time.deltaTime;
            SafeSetText(opponentSkillCooldown, $"{Mathf.CeilToInt(opponentSkillTimer)}s");
            if (opponentSkillTimer <= 0f)
            {
                opponentSkillTimer = 0f;
                if (opponentSkillIcon != null) opponentSkillIcon.enabled = false;
                SafeSetText(opponentSkillCooldown, "");
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    static void SafeSetText(TMP_Text label, string text)
    {
        if (label != null) label.text = text;
    }
}
