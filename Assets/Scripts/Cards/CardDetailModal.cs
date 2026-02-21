using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// CardDetailModal — Full-screen card detail overlay.
///
/// Displays:
///   • Large character/skill portrait
///   • Rarity glow ring
///   • Name + rarity badge
///   • Level + XP bar (character) / level + duration (skill)
///   • Stats: Speed / Health / Damage (character) | Duration / Power (skill)
///   • Prestige stars (character only)
///   • Video preview (skill only — VideoPlayer component)
///   • Progress bar: X / Y copies to next rarity
///   • [UPGRADE] button → opens UpgradeConfirmModal
///   • [CLOSE] button
///
/// VIDEO PREVIEW
/// ─────────────
/// Uses Unity's VideoPlayer component.
/// Clips loaded from: Assets/Videos/Skills/{skillName}.mp4
/// Placeholder: gray box shown when clip not found.
/// renderMode = RenderTexture → displayed on rawImage.
///
/// USAGE
/// ─────
///   detailModal.ShowCharacter("agent_zero");
///   detailModal.ShowSkill("freeze");
///   detailModal.ShowSkill("freeze", autoUpgrade: true); // directly opens upgrade
/// </summary>
public class CardDetailModal : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Portrait Area")]
    public Image      portraitImage;
    public Image      glowRingImage;
    public TMP_Text   nameLabel;
    public TMP_Text   rarityLabel;

    [Header("Level")]
    public TMP_Text   levelLabel;       // "Level 12 / 20"
    public Slider     levelProgressBar;

    [Header("Stats — Character")]
    public GameObject statsCharacterPanel;
    public TMP_Text   speedLabel;
    public TMP_Text   healthLabel;
    public TMP_Text   damageLabel;

    [Header("Prestige — Character")]
    public GameObject prestigePanel;
    public TMP_Text   prestigeLabel;    // "✦✦✦ Prestige 3"

    [Header("Stats — Skill")]
    public GameObject statsSkillPanel;
    public TMP_Text   durationLabel;
    public TMP_Text   powerLabel;
    public TMP_Text   categoryLabel;    // "Offensive", "Defensive", "Economic"

    [Header("Video Preview — Skill")]
    public GameObject videoPanel;
    public RawImage   videoDisplay;
    public VideoPlayer videoPlayer;
    public RenderTexture videoRenderTexture;
    public GameObject videoPlaceholder; // gray box shown when no clip

    [Header("Progress")]
    public Slider     progressBar;
    public TMP_Text   progressLabel;    // "3 / 10 copies"

    [Header("Upgrade")]
    public Button          upgradeButton;
    public TMP_Text        upgradeButtonLabel; // "Upgrade to Rare — 500 🪙"
    public UpgradeConfirmModal upgradeConfirmModal;

    [Header("Close")]
    public Button     closeButton;
    public GameObject panelRoot;        // root GameObject to show/hide

    // ─── Private ──────────────────────────────────────────────────────────────
    private string _currentCharacterId;
    private string _currentSkillId;
    private bool   _isCharacter;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (closeButton   != null) closeButton.onClick.AddListener(Close);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
        if (panelRoot     != null) panelRoot.SetActive(false);

        // Setup VideoPlayer render target
        if (videoPlayer != null && videoRenderTexture != null)
        {
            videoPlayer.renderMode      = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture   = videoRenderTexture;
            videoPlayer.isLooping       = true;
            if (videoDisplay != null) videoDisplay.texture = videoRenderTexture;
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    public void ShowCharacter(string characterId, bool autoUpgrade = false)
    {
        _currentCharacterId = characterId;
        _currentSkillId     = null;
        _isCharacter        = true;

        if (panelRoot != null) panelRoot.SetActive(true);
        StopVideo();
        PopulateCharacter(characterId);

        if (autoUpgrade) OnUpgradeClicked();
    }

    public void ShowSkill(string skillId, bool autoUpgrade = false)
    {
        _currentSkillId     = skillId;
        _currentCharacterId = null;
        _isCharacter        = false;

        if (panelRoot != null) panelRoot.SetActive(true);
        PopulateSkill(skillId);

        if (autoUpgrade) OnUpgradeClicked();
    }

    public void Close()
    {
        StopVideo();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ─── Populate Character ───────────────────────────────────────────────────
    void PopulateCharacter(string characterId)
    {
        var def = CardDefinitions.FindCharacter(characterId);
        if (!def.HasValue) return;

        CharacterCardData data = GetOrDefaultCharacter(characterId);

        // Portrait + glow
        ApplyPortrait(def.Value.portraitKey);
        if (glowRingImage != null) glowRingImage.color = data.GlowColor();

        // Name + rarity
        if (nameLabel   != null) nameLabel.text   = def.Value.displayName;
        if (rarityLabel != null) rarityLabel.text  = data.RarityName.ToUpper();
        ApplyRarityLabelColor(data.rarity);

        // Level
        if (levelLabel != null) levelLabel.text = $"Level {data.level} / 20";
        if (levelProgressBar != null)
            levelProgressBar.value = Mathf.Clamp01(data.level / 20f);

        // Stats
        if (statsCharacterPanel != null) statsCharacterPanel.SetActive(true);
        if (statsSkillPanel     != null) statsSkillPanel.SetActive(false);
        if (speedLabel  != null) speedLabel.text  = $"Speed  {data.Speed:F1}";
        if (healthLabel != null) healthLabel.text = $"Health {data.Health:F0}";
        if (damageLabel != null) damageLabel.text = $"Damage {data.Damage:F1}";

        // Prestige
        if (prestigePanel != null) prestigePanel.SetActive(data.prestige > 0);
        if (prestigeLabel != null && data.prestige > 0)
            prestigeLabel.text = new string('✦', data.prestige) + $"  Prestige {data.prestige}";

        // Video (none for characters — hide)
        if (videoPanel != null) videoPanel.SetActive(false);

        // Progress
        ApplyProgress(data.copies, data.CopiesNeededForUpgrade(), data.rarity);

        // Upgrade button
        bool canUpgrade = data.CanUpgrade();
        if (upgradeButton != null) upgradeButton.gameObject.SetActive(canUpgrade);
        if (upgradeButtonLabel != null && canUpgrade)
        {
            CardRarity next = (CardRarity)((int)data.rarity + 1);
            upgradeButtonLabel.text = $"Upgrade to {next}  —  {data.UpgradeCostCoins()} 🪙";
        }
    }

    // ─── Populate Skill ───────────────────────────────────────────────────────
    void PopulateSkill(string skillId)
    {
        var def = CardDefinitions.FindSkill(skillId);
        if (!def.HasValue) return;

        SkillCardData data = GetOrDefaultSkill(skillId);
        SkillCategory cat  = CardDefinitions.GetSkillCategory(skillId);
        Color glow         = data.GlowColor(cat);

        // Portrait + glow
        ApplyPortrait(def.Value.portraitKey);
        if (glowRingImage != null) glowRingImage.color = glow;

        // Name + rarity
        if (nameLabel   != null) nameLabel.text   = def.Value.displayName;
        if (rarityLabel != null) rarityLabel.text  = data.RarityName.ToUpper();
        ApplyRarityLabelColor(data.rarity);

        // Level
        if (levelLabel != null) levelLabel.text = $"Level {data.level} / 15";
        if (levelProgressBar != null)
            levelProgressBar.value = Mathf.Clamp01(data.level / 15f);

        // Stats
        if (statsCharacterPanel != null) statsCharacterPanel.SetActive(false);
        if (prestigePanel       != null) prestigePanel.SetActive(false);
        if (statsSkillPanel     != null) statsSkillPanel.SetActive(true);
        if (durationLabel != null) durationLabel.text = $"Duration  {data.Duration:F1}s";
        if (powerLabel    != null) powerLabel.text    = $"Power     {data.Power:F2}×";
        if (categoryLabel != null)
        {
            categoryLabel.text  = cat.ToString();
            categoryLabel.color = CardDefinitions.CategoryColor(cat);
        }

        // Description
        if (nameLabel != null) nameLabel.text = def.Value.displayName;

        // Video
        LoadAndPlayVideo(skillId);

        // Progress
        ApplyProgress(data.copies, data.CopiesNeededForUpgrade(), data.rarity);

        // Upgrade button
        bool canUpgrade = data.CanUpgrade();
        if (upgradeButton != null) upgradeButton.gameObject.SetActive(canUpgrade);
        if (upgradeButtonLabel != null && canUpgrade)
        {
            CardRarity next = (CardRarity)((int)data.rarity + 1);
            upgradeButtonLabel.text = $"Upgrade to {next}  —  {data.UpgradeCostCoins()} 🪙";
        }
    }

    // ─── Video ────────────────────────────────────────────────────────────────
    void LoadAndPlayVideo(string skillId)
    {
        if (videoPanel == null) return;
        videoPanel.SetActive(true);

        if (videoPlayer == null) return;

        // Clip path: Assets/Videos/Skills/{skillId}.mp4
        // At runtime on Android, use StreamingAssets:
        string clipName     = skillId.Replace("_", "");  // "ghost_skill" → "ghostskill"
        string streamingPath = System.IO.Path.Combine(
            Application.streamingAssetsPath, $"Videos/Skills/{skillId}.mp4");

        bool fileExists =
#if UNITY_ANDROID
            // On Android, StreamingAssets are inside the APK (use UnityWebRequest to read)
            // For now: check placeholder
            false;
#else
            System.IO.File.Exists(streamingPath);
#endif

        if (fileExists)
        {
            videoPlayer.url = streamingPath;
            videoPlayer.Play();

            if (videoDisplay     != null) videoDisplay.gameObject.SetActive(true);
            if (videoPlaceholder != null) videoPlaceholder.SetActive(false);
        }
        else
        {
            // Show placeholder gray box
            videoPlayer.Stop();
            if (videoDisplay     != null) videoDisplay.gameObject.SetActive(false);
            if (videoPlaceholder != null) videoPlaceholder.SetActive(true);
        }
    }

    void StopVideo()
    {
        if (videoPlayer != null) videoPlayer.Stop();
    }

    // ─── Upgrade ──────────────────────────────────────────────────────────────
    void OnUpgradeClicked()
    {
        if (upgradeConfirmModal == null)
        {
            // No confirm modal wired: attempt upgrade directly
            AttemptUpgrade();
            return;
        }

        // Show confirm dialog
        if (_isCharacter)
        {
            CharacterCardData data = GetOrDefaultCharacter(_currentCharacterId);
            CardRarity next = (CardRarity)((int)data.rarity + 1);
            upgradeConfirmModal.ShowForCharacter(_currentCharacterId, next, data.UpgradeCostCoins(),
                onConfirm: AttemptUpgrade,
                onCancel:  null);
        }
        else
        {
            SkillCardData data = GetOrDefaultSkill(_currentSkillId);
            CardRarity next = (CardRarity)((int)data.rarity + 1);
            upgradeConfirmModal.ShowForSkill(_currentSkillId, next, data.UpgradeCostCoins(),
                onConfirm: AttemptUpgrade,
                onCancel:  null);
        }
    }

    void AttemptUpgrade()
    {
        if (CardManager.Instance == null) return;
        int coins = CardManager.Instance.GetPlayerCoins();

        if (_isCharacter)
        {
            CardManager.Instance.UpgradeCharacterCard(_currentCharacterId, coins, (success, msg) =>
            {
                Debug.Log($"[CardDetailModal] Char upgrade: {msg}");
                if (success)
                {
                    CardManager.Instance.DeductCoins(GetOrDefaultCharacter(_currentCharacterId).UpgradeCostCoins());
                    PopulateCharacter(_currentCharacterId); // refresh view
                }
            });
        }
        else
        {
            CardManager.Instance.UpgradeSkillCard(_currentSkillId, coins, (success, msg) =>
            {
                Debug.Log($"[CardDetailModal] Skill upgrade: {msg}");
                if (success)
                {
                    CardManager.Instance.DeductCoins(GetOrDefaultSkill(_currentSkillId).UpgradeCostCoins());
                    PopulateSkill(_currentSkillId); // refresh view
                }
            });
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    void ApplyPortrait(string key)
    {
        if (portraitImage == null || string.IsNullOrEmpty(key)) return;
        var sprite = Resources.Load<Sprite>(key);
        if (sprite != null) portraitImage.sprite = sprite;
    }

    void ApplyProgress(int copies, int needed, CardRarity rarity)
    {
        bool isMax = rarity == CardRarity.Legendary;
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(!isMax);
            if (!isMax) progressBar.value = needed <= 0 ? 1f : Mathf.Clamp01((float)copies / needed);
        }
        if (progressLabel != null)
        {
            progressLabel.gameObject.SetActive(!isMax);
            if (!isMax) progressLabel.text = $"{copies} / {needed} copies";
        }
    }

    void ApplyRarityLabelColor(CardRarity rarity)
    {
        if (rarityLabel == null) return;
        switch (rarity)
        {
            case CardRarity.Common:    rarityLabel.color = new Color(0.8f, 0.8f, 0.8f); break;
            case CardRarity.Rare:      rarityLabel.color = new Color(1.0f, 0.84f, 0.0f); break;
            case CardRarity.Epic:      rarityLabel.color = new Color(0.62f, 0.13f, 0.94f); break;
            case CardRarity.Legendary: rarityLabel.color = new Color(0.9f, 0.1f, 0.1f); break;
        }
    }

    CharacterCardData GetOrDefaultCharacter(string id)
    {
        if (CardManager.Instance != null && CardManager.Instance.CharacterCards.TryGetValue(id, out var d))
            return d;
        return new CharacterCardData { characterId = id };
    }

    SkillCardData GetOrDefaultSkill(string id)
    {
        if (CardManager.Instance != null && CardManager.Instance.SkillCards.TryGetValue(id, out var d))
            return d;
        return new SkillCardData { skillId = id };
    }
}
