using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// CardUI — Shared visual component for one card (character or skill).
///
/// Attach this to a Card prefab that has the following children (all optional,
/// gracefully skipped if null):
///
///   Frame          Image  — background; tinted by rarity
///   GlowRing       Image  — ring around portrait; color = rarity glow
///   Portrait       Image  — character / skill artwork
///   CategoryBadge  Image  — skill category color bar (skill only)
///   NameLabel      TMP    — card name
///   LevelBadge     TMP    — "Lv 12" top-right
///   PrestigeRow    GO     — prestige stars parent (char only)
///   PrestigeStar[] Image  — up to 5 prestige star images
///   ProgressBar    Slider — X / Y copies to upgrade
///   ProgressLabel  TMP    — "3 / 5 copies"
///   UpgradeButton  Button — visible only when upgrade is available
///   UpgradeCost    TMP    — "500  coins" on upgrade button
///   SelectedBorder Image  — highlighted when card is in active deck
///
/// Usage:
///   card.SetupCharacter(cardData, definition);
///   card.SetupSkill(cardData, definition);
///   card.SetSelected(true);
///   card.OnClick = () => OpenDetail(card);
/// </summary>
[RequireComponent(typeof(Button))]
public class CardUI : MonoBehaviour
{
    // ─── Inspector references ─────────────────────────────────────────────────
    [Header("Frame")]
    public Image frameImage;
    public Image glowRingImage;

    [Header("Content")]
    public Image      portraitImage;
    public Image      categoryBadgeImage; // skill only
    public TMP_Text   nameLabel;

    [Header("Badges")]
    public TMP_Text   levelLabel;          // "Lv 12"
    public GameObject prestigeRow;         // character only
    public Image[]    prestigeStars;       // up to 5 stars

    [Header("Progress")]
    public Slider     progressBar;
    public TMP_Text   progressLabel;       // "3 / 5"

    [Header("Upgrade")]
    public Button     upgradeButton;
    public TMP_Text   upgradeCostLabel;

    [Header("Selection")]
    public Image      selectedBorder;

    // ─── Rarity frame tints ───────────────────────────────────────────────────
    [Header("Rarity Frame Colors")]
    public Color commonFrameColor    = new Color(0.22f, 0.22f, 0.22f);
    public Color rareFrameColor      = new Color(0.16f, 0.14f, 0.05f);
    public Color epicFrameColor      = new Color(0.14f, 0.05f, 0.20f);
    public Color legendaryFrameColor = new Color(0.25f, 0.04f, 0.04f);

    // ─── Public state ─────────────────────────────────────────────────────────
    public Action OnClick;
    public Action OnUpgradeClick;

    // ─── Private ──────────────────────────────────────────────────────────────
    private CharacterCardData _charData;
    private SkillCardData     _skillData;
    private bool              _isCharacter;
    private Button            _button;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() => OnClick?.Invoke());
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(() => OnUpgradeClick?.Invoke());
    }

    // ─── Setup (Character) ────────────────────────────────────────────────────
    public void SetupCharacter(CharacterCardData data, CardDefinition def)
    {
        _charData    = data;
        _isCharacter = true;

        ApplyRarityFrame(data.rarity, data.GlowColor());
        ApplyPortrait(def.portraitKey);
        ApplyName(def.displayName);
        ApplyLevel(data.level, maxLevel: 20);
        ApplyPrestige(data.prestige);
        ApplyProgress(data.copies, data.CopiesNeededForUpgrade(), data.rarity);
        ApplyUpgradeButton(data);

        // Hide category badge (chars don't have one)
        if (categoryBadgeImage != null) categoryBadgeImage.gameObject.SetActive(false);
    }

    // ─── Setup (Skill) ────────────────────────────────────────────────────────
    public void SetupSkill(SkillCardData data, CardDefinition def)
    {
        _skillData   = data;
        _isCharacter = false;

        SkillCategory cat = CardDefinitions.GetSkillCategory(data.skillId);
        Color glow = data.GlowColor(cat);

        ApplyRarityFrame(data.rarity, glow);
        ApplyPortrait(def.portraitKey);
        ApplyName(def.displayName);
        ApplyLevel(data.level, maxLevel: 15);
        ApplyProgress(data.copies, data.CopiesNeededForUpgrade(), data.rarity);
        ApplyUpgradeButton(data);

        // Category badge (colored strip at bottom of card)
        if (categoryBadgeImage != null)
        {
            categoryBadgeImage.gameObject.SetActive(true);
            categoryBadgeImage.color = CardDefinitions.CategoryColor(cat);
        }

        // No prestige for skills
        if (prestigeRow != null) prestigeRow.SetActive(false);
    }

    // ─── Visuals ──────────────────────────────────────────────────────────────
    void ApplyRarityFrame(CardRarity rarity, Color glowColor)
    {
        if (frameImage != null)
        {
            switch (rarity)
            {
                case CardRarity.Common:    frameImage.color = commonFrameColor;    break;
                case CardRarity.Rare:      frameImage.color = rareFrameColor;      break;
                case CardRarity.Epic:      frameImage.color = epicFrameColor;      break;
                case CardRarity.Legendary: frameImage.color = legendaryFrameColor; break;
            }
        }
        if (glowRingImage != null) glowRingImage.color = glowColor;
    }

    void ApplyPortrait(string portraitKey)
    {
        if (portraitImage == null || string.IsNullOrEmpty(portraitKey)) return;
        var sprite = Resources.Load<Sprite>(portraitKey);
        if (sprite != null)
            portraitImage.sprite = sprite;
        // If not found: keep default placeholder (grey box)
    }

    void ApplyName(string displayName)
    {
        if (nameLabel != null) nameLabel.text = displayName;
    }

    void ApplyLevel(int level, int maxLevel)
    {
        if (levelLabel != null) levelLabel.text = $"Lv {level}/{maxLevel}";
    }

    void ApplyPrestige(int prestigeCount)
    {
        if (prestigeRow != null) prestigeRow.SetActive(prestigeCount > 0);
        if (prestigeStars == null) return;
        for (int i = 0; i < prestigeStars.Length; i++)
        {
            if (prestigeStars[i] != null)
                prestigeStars[i].gameObject.SetActive(i < prestigeCount);
        }
    }

    void ApplyProgress(int copies, int needed, CardRarity rarity)
    {
        bool isMax = rarity == CardRarity.Legendary;

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(!isMax);
            if (!isMax)
            {
                float fill = needed <= 0 ? 1f : Mathf.Clamp01((float)copies / needed);
                progressBar.value = fill;
            }
        }

        if (progressLabel != null)
        {
            progressLabel.gameObject.SetActive(!isMax);
            if (!isMax)
                progressLabel.text = $"{copies} / {needed}";
        }
    }

    void ApplyUpgradeButton(CharacterCardData data)
    {
        bool canUpgrade = data.CanUpgrade();
        if (upgradeButton != null) upgradeButton.gameObject.SetActive(canUpgrade);
        if (upgradeCostLabel != null && canUpgrade)
        {
            CardRarity next = (CardRarity)((int)data.rarity + 1);
            upgradeCostLabel.text = $"{data.UpgradeCostCoins()}  coins → {next}";
        }
    }

    void ApplyUpgradeButton(SkillCardData data)
    {
        bool canUpgrade = data.CanUpgrade();
        if (upgradeButton != null) upgradeButton.gameObject.SetActive(canUpgrade);
        if (upgradeCostLabel != null && canUpgrade)
        {
            CardRarity next = (CardRarity)((int)data.rarity + 1);
            upgradeCostLabel.text = $"{data.UpgradeCostCoins()}  coins → {next}";
        }
    }

    // ─── Selection ────────────────────────────────────────────────────────────
    public void SetSelected(bool selected)
    {
        if (selectedBorder != null) selectedBorder.enabled = selected;
        transform.localScale = selected ? Vector3.one * 1.06f : Vector3.one;
    }

    // ─── Refresh ──────────────────────────────────────────────────────────────
    /// <summary>Call after card data changes (e.g. after upgrade) to re-render.</summary>
    public void Refresh()
    {
        if (_isCharacter && _charData != null)
        {
            var def = CardDefinitions.FindCharacter(_charData.characterId);
            if (def.HasValue) SetupCharacter(_charData, def.Value);
        }
        else if (!_isCharacter && _skillData != null)
        {
            var def = CardDefinitions.FindSkill(_skillData.skillId);
            if (def.HasValue) SetupSkill(_skillData, def.Value);
        }
    }
}
