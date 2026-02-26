using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BattlePassScreen — UI controller for the Battle Pass screen.
///
/// STUB: Battle Pass feature not yet live. This script wires up the battle pass
/// icon assets from GameIconSystem and provides a ready-to-implement foundation.
///
/// Asset references:
///   Icons/BattlePass/battle_pass_tier_1     — tier 1 icon (placeholder)
///   Icons/BattlePass/battle_pass_tier_30    — tier 30 icon (placeholder)
///   Icons/BattlePass/battle_pass_premium    — premium pass icon (placeholder)
///
/// TODO post-launch:
///   1. Generate full 50-tier icon set via Scenario.gg.
///   2. Implement actual battle pass reward data (ScriptableObject or Firestore).
///   3. Wire premium unlock via IAPManager.
///   4. Build out tier progression bar (see TierProgressionBar in PrestigeBadge.cs).
///
/// SETUP:
///   1. Add BattlePassScreen to a canvas panel (set inactive by default).
///   2. Wire Inspector references below.
///   3. Call Show() from MainMenuScreen / ShopScreen / nav button.
/// </summary>
public class BattlePassScreen : MonoBehaviour
{
    // ─── Inspector References ─────────────────────────────────────────────────
    [Header("Header")]
    [Tooltip("Title label — 'BATTLE PASS' or current season name")]
    public TMP_Text titleLabel;

    [Tooltip("Current player tier / pass level text")]
    public TMP_Text playerTierLabel;

    [Header("Battle Pass Icons")]
    [Tooltip("Icon for the free track tier 1 reward")]
    public Image tierStartIcon;

    [Tooltip("Icon for the free track milestone (tier 30 / final)")]
    public Image tierEndIcon;

    [Tooltip("Icon for the premium battle pass badge")]
    public Image premiumPassIcon;

    [Header("Premium Pass")]
    [Tooltip("Button to purchase premium battle pass")]
    public Button   purchasePremiumButton;
    public TMP_Text premiumCostLabel;

    [Tooltip("Root to show when player already owns premium")]
    public GameObject premiumOwnedBadge;

    [Header("Close")]
    public Button closeButton;

    // ─── State ────────────────────────────────────────────────────────────────
    private bool _isPremiumOwner = false;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (purchasePremiumButton != null)
            purchasePremiumButton.onClick.AddListener(OnPurchasePremium);
    }

    void OnEnable()
    {
        RefreshIcons();
        RefreshPremiumState();
    }

    // ─── Icon Wiring ──────────────────────────────────────────────────────────

    void RefreshIcons()
    {
        // Apply battle pass icons from GameIconSystem.
        // Placeholder PNGs: 128x128 gold-toned solid colors.
        // Swap with Scenario.gg art post-launch (same resource path).
        GameIconSystem.ApplyIcon(tierStartIcon, "battle_pass_tier_1");
        GameIconSystem.ApplyIcon(tierEndIcon,   "battle_pass_tier_30");
        GameIconSystem.ApplyIcon(premiumPassIcon, "battle_pass_premium");

        // Header
        if (titleLabel != null)
            titleLabel.text = "BATTLE PASS";

        // Player tier from ranked progression
        if (playerTierLabel != null && RankedProgressionManager.Instance != null)
        {
            var tier = RankedProgressionManager.Instance.State.currentTier;
            playerTierLabel.text = tier.name.ToUpper();
            playerTierLabel.color = tier.color;
        }
    }

    // ─── Premium State ────────────────────────────────────────────────────────

    void RefreshPremiumState()
    {
        // TODO: Check actual premium ownership (IAPManager / Firestore).
        // For now: default not owned.
        _isPremiumOwner = PlayerPrefs.GetInt("VaultDash_BattlePassPremium", 0) == 1;

        if (premiumOwnedBadge != null)
            premiumOwnedBadge.SetActive(_isPremiumOwner);

        if (purchasePremiumButton != null)
            purchasePremiumButton.gameObject.SetActive(!_isPremiumOwner);

        if (premiumCostLabel != null)
            premiumCostLabel.text = "500 💎";  // placeholder price
    }

    // ─── Actions ──────────────────────────────────────────────────────────────

    void OnPurchasePremium()
    {
        // TODO: Wire to IAPManager.Purchase("battle_pass_premium")
        // For now: stub that marks it as owned (for testing only).
        Debug.Log("[BattlePassScreen] Premium purchase stub triggered.");

#if UNITY_EDITOR
        PlayerPrefs.SetInt("VaultDash_BattlePassPremium", 1);
        PlayerPrefs.Save();
        RefreshPremiumState();
#endif
    }

    // ─── Show / Hide ──────────────────────────────────────────────────────────

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshIcons();
        RefreshPremiumState();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ─── Static Helpers (for use by other UI) ────────────────────────────────

    /// <summary>
    /// Apply a battle pass tier icon to any Image component.
    /// Tier 1-29 → tier_1 placeholder; 30+ → tier_30 placeholder.
    /// </summary>
    public static void ApplyTierIcon(Image image, int tier)
    {
        if (image == null) return;
        string key = GameIconSystem.BattlePassTierKey(tier);
        GameIconSystem.ApplyIcon(image, key);
    }

    /// <summary>
    /// Apply premium badge icon to any Image component.
    /// </summary>
    public static void ApplyPremiumIcon(Image image)
        => GameIconSystem.ApplyIcon(image, "battle_pass_premium");
}
