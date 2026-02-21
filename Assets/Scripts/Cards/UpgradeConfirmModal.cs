using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UpgradeConfirmModal — "Are you sure?" dialog before spending coins.
///
/// Shows:
///   "Upgrade to Epic? Cost: 500 🪙"
///   [CONFIRM]  [CANCEL]
///
/// Usage (from CardDetailModal):
///   upgradeConfirmModal.ShowForCharacter("agent_zero", CardRarity.Rare, 200,
///       onConfirm: () => DoUpgrade(), onCancel: null);
///
///   upgradeConfirmModal.ShowForSkill("freeze", CardRarity.Epic, 400,
///       onConfirm: () => DoUpgrade(), onCancel: null);
/// </summary>
public class UpgradeConfirmModal : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Content")]
    public TMP_Text titleLabel;          // "Upgrade Agent Zero"
    public TMP_Text bodyLabel;           // "Upgrade to Epic? Cost: 500 🪙"
    public TMP_Text coinBalanceLabel;    // "Your balance: 1,240 🪙"
    public Image    cardPreviewImage;    // small card portrait
    public Image    rarityGlowImage;     // shows target rarity color

    [Header("Buttons")]
    public Button   confirmButton;
    public TMP_Text confirmButtonLabel;  // "UPGRADE  500 🪙"
    public Button   cancelButton;

    [Header("Root")]
    public GameObject panelRoot;

    // ─── Private ──────────────────────────────────────────────────────────────
    private Action _onConfirm;
    private Action _onCancel;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    public void ShowForCharacter(string characterId, CardRarity targetRarity, int cost,
                                 Action onConfirm, Action onCancel)
    {
        var def = CardDefinitions.FindCharacter(characterId);
        string displayName = def.HasValue ? def.Value.displayName : characterId;
        string portraitKey = def.HasValue ? def.Value.portraitKey : "";

        Show(displayName, "character", targetRarity, cost, portraitKey, onConfirm, onCancel);
    }

    public void ShowForSkill(string skillId, CardRarity targetRarity, int cost,
                             Action onConfirm, Action onCancel)
    {
        var def = CardDefinitions.FindSkill(skillId);
        string displayName = def.HasValue ? def.Value.displayName : skillId;
        string portraitKey = def.HasValue ? def.Value.portraitKey : "";

        Show(displayName, "skill", targetRarity, cost, portraitKey, onConfirm, onCancel);
    }

    // ─── Internal Show ────────────────────────────────────────────────────────
    void Show(string displayName, string cardType, CardRarity targetRarity, int cost,
              string portraitKey, Action onConfirm, Action onCancel)
    {
        _onConfirm = onConfirm;
        _onCancel  = onCancel;

        // Title
        if (titleLabel != null)
            titleLabel.text = $"Upgrade {displayName}";

        // Body
        if (bodyLabel != null)
            bodyLabel.text = $"Upgrade to <b>{targetRarity}</b>?\nCost: <b>{cost:N0} 🪙</b>";

        // Balance
        int coins = CardManager.Instance?.GetPlayerCoins()
                    ?? PlayerPrefs.GetInt("VaultDash_Coins", 0);
        if (coinBalanceLabel != null)
            coinBalanceLabel.text = $"Your balance: {coins:N0} 🪙";

        // Confirm button
        if (confirmButtonLabel != null)
            confirmButtonLabel.text = $"UPGRADE  {cost:N0} 🪙";
        if (confirmButton != null)
            confirmButton.interactable = coins >= cost;

        // Portrait
        if (cardPreviewImage != null && !string.IsNullOrEmpty(portraitKey))
        {
            var sprite = Resources.Load<Sprite>(portraitKey);
            if (sprite != null) cardPreviewImage.sprite = sprite;
        }

        // Rarity glow on preview
        if (rarityGlowImage != null)
        {
            rarityGlowImage.color = RarityGlowColor(targetRarity);
        }

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    // ─── Buttons ──────────────────────────────────────────────────────────────
    void OnConfirm()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        _onConfirm?.Invoke();
        _onConfirm = null;
        _onCancel  = null;
    }

    void OnCancel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        _onCancel?.Invoke();
        _onConfirm = null;
        _onCancel  = null;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    static Color RarityGlowColor(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Common:    return new Color(0.8f, 0.8f, 0.8f);
            case CardRarity.Rare:      return new Color(1.0f, 0.84f, 0.0f);
            case CardRarity.Epic:      return new Color(0.62f, 0.13f, 0.94f);
            case CardRarity.Legendary: return new Color(0.9f, 0.1f, 0.1f);
            default:                   return Color.white;
        }
    }
}
