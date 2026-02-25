using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ShopScreen — In-game store for Vault Dash.
///
/// Tabs:
///   GEMS      — 4 IAP gem packs (consumable)
///   SKINS     — Character cosmetic skins (gems)
///   BATTLE PASS — Premium season pass preview
///
/// IAP flow:
///   ShopScreen → IAPManager.BuyProduct(id) → receipt validation
///   IAPManager.OnGemsGranted event → refresh gem display
///
/// Wire all references in Inspector.
/// </summary>
public class ShopScreen : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Tabs")]
    public Button tabGemsButton;
    public Button tabSkinsButton;
    public Button tabBattlePassButton;
    public GameObject gemsPanel;
    public GameObject skinsPanel;
    public GameObject battlePassPanel;
    public Color tabActiveColor   = new Color(0.9f, 0.75f, 0.1f);
    public Color tabInactiveColor = new Color(0.3f, 0.3f, 0.35f);

    [Header("Currency Display")]
    public TMP_Text gemBalanceText;
    public TMP_Text coinBalanceText;

    [Header("Gem Packs (4 slots — match IAPManager gemPacks order)")]
    public GemPackUI[] gemPackSlots = new GemPackUI[4];

    [Header("Skins Carousel")]
    public Transform    skinCardContainer;      // horizontal scroll content
    public GameObject   skinCardPrefab;
    public ScrollRect   skinScrollRect;

    [Header("Battle Pass")]
    public TMP_Text  battlePassTitleText;
    public TMP_Text  battlePassDescText;
    public TMP_Text  battlePassPriceText;
    public Button    battlePassBuyButton;
    public GameObject battlePassOwnedBadge;

    [Header("Toast Notification")]
    public GameObject toastPanel;
    public TMP_Text   toastText;

    [Header("Back")]
    public Button backButton;

    // ─── Private ──────────────────────────────────────────────────────────────
    private enum Tab { Gems, Skins, BattlePass }
    private Tab _activeTab = Tab.Gems;
    private bool _initialized = false;

    // Gem/coin balance keys
    private const string GEM_KEY  = "VaultDash_Gems";
    private const string COIN_KEY = "VaultDash_Coins";

    // ─── Static Skin Data ─────────────────────────────────────────────────────
    [System.Serializable]
    public struct SkinData
    {
        public string skinId;
        public string displayName;
        public string characterName;
        public int    gemCost;
        public Sprite preview;
    }

    [Header("Skin Definitions")]
    public SkinData[] availableSkins = new SkinData[]
    {
        new SkinData { skinId = "agz_gold",   displayName = "Gold Operative",   characterName = "Agent Zero", gemCost = 200 },
        new SkinData { skinId = "agz_shadow",  displayName = "Shadow Protocol",  characterName = "Agent Zero", gemCost = 350 },
        new SkinData { skinId = "blz_fire",    displayName = "Inferno Blaze",    characterName = "Blaze",      gemCost = 200 },
        new SkinData { skinId = "blz_ice",     displayName = "Cryo Blaze",       characterName = "Blaze",      gemCost = 350 },
        new SkinData { skinId = "knx_mech",    displayName = "Mechknox 3000",    characterName = "Knox",       gemCost = 200 },
        new SkinData { skinId = "jde_neon",    displayName = "Neon Jade",        characterName = "Jade",       gemCost = 200 },
        new SkinData { skinId = "vec_chrome",  displayName = "Chrome Vector",    characterName = "Vector",     gemCost = 250 },
        new SkinData { skinId = "cph_glitch",  displayName = "Glitch Cipher",    characterName = "Cipher",     gemCost = 300 },
        new SkinData { skinId = "nova_void",   displayName = "Void Nova",        characterName = "Nova",       gemCost = 300 },
        new SkinData { skinId = "ryze_arc",    displayName = "Arc Ryze",         characterName = "Ryze",       gemCost = 250 },
    };

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Start()
    {
        if (backButton        != null) backButton.onClick.AddListener(OnBack);
        if (tabGemsButton     != null) tabGemsButton.onClick.AddListener(() => SwitchTab(Tab.Gems));
        if (tabSkinsButton    != null) tabSkinsButton.onClick.AddListener(() => SwitchTab(Tab.Skins));
        if (tabBattlePassButton!= null) tabBattlePassButton.onClick.AddListener(() => SwitchTab(Tab.BattlePass));
        if (battlePassBuyButton!= null) battlePassBuyButton.onClick.AddListener(OnBuyBattlePass);

        // Subscribe IAP events
        IAPManager.OnGemsGranted   += OnGemsGranted;
        IAPManager.OnPurchaseError += OnPurchaseError;
    }

    void OnDestroy()
    {
        IAPManager.OnGemsGranted   -= OnGemsGranted;
        IAPManager.OnPurchaseError -= OnPurchaseError;
    }

    // ─── Activation ───────────────────────────────────────────────────────────
    public void OnActivate()
    {
        RefreshCurrencyDisplay();

        if (!_initialized)
        {
            SetupGemPacks();
            BuildSkinCards();
            SetupBattlePass();
            _initialized = true;
        }

        SwitchTab(_activeTab);
    }

    // ─── Tab Switching ────────────────────────────────────────────────────────
    void SwitchTab(Tab tab)
    {
        _activeTab = tab;

        if (gemsPanel      != null) gemsPanel.SetActive(tab == Tab.Gems);
        if (skinsPanel     != null) skinsPanel.SetActive(tab == Tab.Skins);
        if (battlePassPanel!= null) battlePassPanel.SetActive(tab == Tab.BattlePass);

        // Tint tab buttons
        SetTabTint(tabGemsButton,       tab == Tab.Gems);
        SetTabTint(tabSkinsButton,      tab == Tab.Skins);
        SetTabTint(tabBattlePassButton, tab == Tab.BattlePass);
    }

    void SetTabTint(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? tabActiveColor : tabInactiveColor;
    }

    // ─── Currency ─────────────────────────────────────────────────────────────
    void RefreshCurrencyDisplay()
    {
        int gems  = PlayerPrefs.GetInt(GEM_KEY,  0);
        int coins = PlayerPrefs.GetInt(COIN_KEY, 0);

        // Icons shown via Image components next to these labels (wire in Inspector)
        if (gemBalanceText  != null) gemBalanceText.text  = $"{gems}";
        if (coinBalanceText != null) coinBalanceText.text = $"{coins}";
    }

    // ─── Gem Packs ────────────────────────────────────────────────────────────
    void SetupGemPacks()
    {
        if (IAPManager.Instance == null || gemPackSlots == null) return;

        for (int i = 0; i < gemPackSlots.Length && i < IAPManager.Instance.gemPacks.Length; i++)
        {
            var pack = IAPManager.Instance.gemPacks[i];
            var slot = gemPackSlots[i];
            if (slot == null) continue;

            slot.Setup(
                displayName: pack.displayName,
                gemAmount:   pack.gemAmount,
                priceText:   $"${pack.priceUSD:F2}",
                isBestValue: i == 2, // index 2 = "Best Value" banner
                onBuy: () => OnBuyGemPack(pack.productId)
            );
        }
    }

    void OnBuyGemPack(string productId)
    {
        Debug.Log($"[ShopScreen] Buying gem pack: {productId}");
        IAPManager.Instance?.BuyProduct(productId);
    }

    // ─── Skin Cards ───────────────────────────────────────────────────────────
    void BuildSkinCards()
    {
        if (skinCardContainer == null || skinCardPrefab == null) return;

        foreach (Transform child in skinCardContainer)
            Destroy(child.gameObject);

        foreach (var skin in availableSkins)
        {
            SkinData s = skin; // capture
            GameObject cardGO = Instantiate(skinCardPrefab, skinCardContainer);
            SkinCard card = cardGO.GetComponent<SkinCard>() ?? cardGO.AddComponent<SkinCard>();
            bool owned = IsSkinOwned(s.skinId);
            card.Setup(s, owned, onBuy: () => OnBuySkin(s));
        }
    }

    bool IsSkinOwned(string skinId)
    {
        return PlayerPrefs.GetInt($"Skin_{skinId}", 0) == 1;
    }

    void OnBuySkin(SkinData skin)
    {
        int gems = PlayerPrefs.GetInt(GEM_KEY, 0);
        if (gems < skin.gemCost)
        {
            ShowToast($"Not enough gems! Need {skin.gemCost} gems");
            return;
        }

        // Deduct gems + mark owned
        PlayerPrefs.SetInt(GEM_KEY, gems - skin.gemCost);
        PlayerPrefs.SetInt($"Skin_{skin.skinId}", 1);
        PlayerPrefs.Save();

        RefreshCurrencyDisplay();
        BuildSkinCards(); // rebuild to show owned state
        ShowToast($"{skin.displayName} unlocked!");

        Debug.Log($"[ShopScreen] Skin purchased: {skin.skinId}");
    }

    // ─── Battle Pass ──────────────────────────────────────────────────────────
    void SetupBattlePass()
    {
        bool hasPass = PlayerPrefs.GetInt("BattlePass_Owned", 0) == 1;

        if (battlePassTitleText != null) battlePassTitleText.text = "SEASON 1 BATTLE PASS";
        if (battlePassDescText  != null) battlePassDescText.text  =
            "Unlock 30 tiers of exclusive rewards:\n" +
            "• 5 premium character skins\n" +
            "• 1,000+ gems over the season\n" +
            "• Exclusive Victory animations\n" +
            "• Season title 'Vault Elite'";
        if (battlePassPriceText != null) battlePassPriceText.text = "$9.99";

        if (battlePassOwnedBadge != null) battlePassOwnedBadge.SetActive(hasPass);
        if (battlePassBuyButton  != null) battlePassBuyButton.gameObject.SetActive(!hasPass);
    }

    void OnBuyBattlePass()
    {
        IAPManager.Instance?.BuyProduct("battle_pass_s1");
        Debug.Log("[ShopScreen] Battle pass purchase initiated.");
    }

    // ─── IAP Callbacks ────────────────────────────────────────────────────────
    void OnGemsGranted(string productId, int amount)
    {
        RefreshCurrencyDisplay();
        ShowToast($"+{amount} gems added!");
        Debug.Log($"[ShopScreen] Gems granted: {amount} (from {productId})");
    }

    void OnPurchaseError(string reason)
    {
        ShowToast($"Purchase failed: {reason}");
        Debug.LogWarning($"[ShopScreen] Purchase error: {reason}");
    }

    // ─── Toast ────────────────────────────────────────────────────────────────
    void ShowToast(string msg)
    {
        if (toastPanel == null || toastText == null) return;
        toastText.text = msg;
        StopAllCoroutines();
        StartCoroutine(ToastRoutine());
    }

    IEnumerator ToastRoutine()
    {
        toastPanel.SetActive(true);
        CanvasGroup cg = toastPanel.GetComponent<CanvasGroup>() ?? toastPanel.AddComponent<CanvasGroup>();

        // Fade in
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = elapsed / 0.2f;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(2.5f);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = 1f - (elapsed / 0.4f);
            yield return null;
        }

        toastPanel.SetActive(false);
    }

    // ─── Back ─────────────────────────────────────────────────────────────────
    void OnBack() => UIManager.Instance?.GoBack();
}

// ─── Gem Pack UI Slot ─────────────────────────────────────────────────────────
[System.Serializable]
public class GemPackUI : MonoBehaviour
{
    public TMP_Text    nameText;
    public TMP_Text    gemAmountText;
    public TMP_Text    priceText;
    public Button      buyButton;
    public GameObject  bestValueBanner;

    public void Setup(string displayName, int gemAmount, string priceText, bool isBestValue, System.Action onBuy)
    {
        if (nameText      != null) nameText.text      = displayName;
        // Gem icon shown via Image component next to this label
        if (gemAmountText != null) gemAmountText.text = $"{gemAmount}";
        if (this.priceText!= null) this.priceText.text = priceText;
        if (bestValueBanner != null) bestValueBanner.SetActive(isBestValue);

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy?.Invoke());
        }
    }
}

// ─── Skin Card Component ──────────────────────────────────────────────────────
public class SkinCard : MonoBehaviour
{
    public Image    previewImage;
    public TMP_Text skinNameText;
    public TMP_Text characterNameText;
    public TMP_Text priceText;
    public Button   buyButton;
    public GameObject ownedBadge;

    public void Setup(ShopScreen.SkinData skin, bool owned, System.Action onBuy)
    {
        // Auto-find refs if not wired
        if (previewImage      == null) previewImage      = transform.Find("Preview")?.GetComponent<Image>();
        if (skinNameText      == null) skinNameText      = transform.Find("SkinName")?.GetComponent<TMP_Text>();
        if (characterNameText == null) characterNameText = transform.Find("CharName")?.GetComponent<TMP_Text>();
        if (priceText         == null) priceText         = transform.Find("Price")?.GetComponent<TMP_Text>();
        if (buyButton         == null) buyButton         = GetComponentInChildren<Button>();
        if (ownedBadge        == null) ownedBadge        = transform.Find("OwnedBadge")?.gameObject;

        if (previewImage      != null && skin.preview != null) previewImage.sprite = skin.preview;
        if (skinNameText      != null) skinNameText.text      = skin.displayName;
        if (characterNameText != null) characterNameText.text = skin.characterName;
        // Gem icon shown via Image component; "Owned" badge shown via ownedBadge GO
        if (priceText         != null) priceText.text         = owned ? "Owned" : $"{skin.gemCost} gems";
        if (ownedBadge        != null) ownedBadge.SetActive(owned);

        if (buyButton != null)
        {
            buyButton.interactable = !owned;
            buyButton.onClick.RemoveAllListeners();
            if (!owned) buyButton.onClick.AddListener(() => onBuy?.Invoke());
        }
    }
}
