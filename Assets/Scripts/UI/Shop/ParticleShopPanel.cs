using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// ParticleShopPanel: UI for browsing and purchasing particle cosmetics.
/// 
/// Features:
/// - Browse all available particles by type (aura, weapon_trail, footstep, etc.)
/// - Show price and unlock requirements (prestige tier, etc.)
/// - Purchase with gems (if not already owned)
/// - Preview effect before buying
/// - Show "OWNED" / "LOCKED" badges
/// </summary>

public class ParticleShopPanel : MonoBehaviour
{
    [SerializeField] private Transform scrollContent;           // Parent for particle items
    [SerializeField] private GameObject particleItemPrefab;     // Prefab for shop item UI
    [SerializeField] private Dropdown filterDropdown;           // Filter by type: All / Aura / Trail / etc.
    [SerializeField] private Text selectedParticleNameText;     // Display selected particle name
    [SerializeField] private Text selectedParticleDescText;     // Display selected particle description
    [SerializeField] private Text selectedParticlePriceText;    // Display selected particle price
    [SerializeField] private Button purchaseButton;             // "Buy" button
    [SerializeField] private Button previewButton;              // "Preview" button
    [SerializeField] private Image selectedParticleRarityImage; // Color-coded rarity indicator

    private CharacterParticleCosmetics _particleSystem;
    private string _selectedParticleId;
    private List<ParticleCosmetic> _filteredCatalog = new List<ParticleCosmetic>();

    private void OnEnable()
    {
        _particleSystem = CharacterParticleCosmetics.Instance;
        RefreshShop();
        
        if (filterDropdown != null)
            filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        
        if (previewButton != null)
            previewButton.onClick.AddListener(OnPreviewClicked);
    }

    private void OnDisable()
    {
        if (filterDropdown != null)
            filterDropdown.onValueChanged.RemoveListener(OnFilterChanged);
        
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
        
        if (previewButton != null)
            previewButton.onClick.RemoveListener(OnPreviewClicked);
    }

    /// <summary>
    /// Refresh the shop display based on current filter.
    /// </summary>
    public void RefreshShop()
    {
        // Clear existing items
        foreach (Transform child in scrollContent)
            Destroy(child.gameObject);

        // Get filtered catalog
        _filteredCatalog = GetFilteredCatalog();

        // Create shop items
        foreach (var cosmetic in _filteredCatalog)
        {
            var item = Instantiate(particleItemPrefab, scrollContent);
            var itemComponent = item.GetComponent<ParticleShopItem>();
            
            if (itemComponent != null)
            {
                itemComponent.SetupUI(cosmetic, _particleSystem.IsUnlocked(cosmetic.id), 
                    () => SelectParticle(cosmetic.id));
            }
        }
    }

    /// <summary>
    /// Get filtered catalog based on dropdown selection.
    /// </summary>
    private List<ParticleCosmetic> GetFilteredCatalog()
    {
        if (filterDropdown == null || filterDropdown.value == 0)
            return _particleSystem.GetCatalog(); // All

        var selectedFilter = filterDropdown.options[filterDropdown.value].text;
        var effectType = selectedFilter.ToLower().Replace(" ", "_");
        
        return _particleSystem.GetCatalogByType(effectType);
    }

    /// <summary>
    /// Filter dropdown changed — refresh shop.
    /// </summary>
    private void OnFilterChanged(int newIndex)
    {
        RefreshShop();
    }

    /// <summary>
    /// Player selected a particle from the shop.
    /// </summary>
    private void SelectParticle(string particleId)
    {
        _selectedParticleId = particleId;
        var cosmetic = _particleSystem.GetParticleCosmeticById(particleId);

        if (cosmetic == null)
            return;

        // Update UI
        if (selectedParticleNameText != null)
            selectedParticleNameText.text = cosmetic.name;

        if (selectedParticleDescText != null)
            selectedParticleDescText.text = cosmetic.description;

        if (selectedParticleRarityImage != null)
            selectedParticleRarityImage.color = cosmetic.rarity;

        var isUnlocked = _particleSystem.IsUnlocked(particleId);

        if (selectedParticlePriceText != null)
        {
            if (isUnlocked)
                selectedParticlePriceText.text = "OWNED";
            else if (cosmetic.gemsPrice == 0)
                selectedParticlePriceText.text = "LOCKED"; // Earned only
            else
                selectedParticlePriceText.text = $"{cosmetic.gemsPrice} GEMS";
        }

        // Update button states
        if (purchaseButton != null)
        {
            purchaseButton.interactable = !isUnlocked && cosmetic.gemsPrice > 0;
            purchaseButton.GetComponentInChildren<Text>().text = isUnlocked ? "OWNED" : "BUY";
        }
    }

    /// <summary>
    /// Player clicked "Buy" button.
    /// </summary>
    private void OnPurchaseClicked()
    {
        var cosmetic = _particleSystem.GetParticleCosmeticById(_selectedParticleId);
        
        if (cosmetic == null || cosmetic.gemsPrice == 0)
            return;

        // TODO: Call shop system to deduct gems and unlock particle
        // For now, just show message
        Debug.Log($"Purchase clicked for {cosmetic.name} ({cosmetic.gemsPrice} gems)");

        // Example: ShopSystem.Instance.PurchaseParticle(uid, particleId);
        // After purchase:
        // - Deduct gems from player account
        // - Unlock particle via CharacterParticleCosmetics.UnlockParticle()
        // - Refresh shop
    }

    /// <summary>
    /// Player clicked "Preview" button — play effect at center of screen.
    /// </summary>
    private void OnPreviewClicked()
    {
        var cosmetic = _particleSystem.GetParticleCosmeticById(_selectedParticleId);
        
        if (cosmetic == null)
            return;

        // Play effect at screen center
        var screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 10f);
        var worldPos = Camera.main.ScreenToWorldPoint(screenCenter);
        
        _particleSystem.PlayParticleEffect(_selectedParticleId, worldPos);

        Debug.Log($"Previewing particle: {cosmetic.name}");
    }
}

/// <summary>
/// Individual particle shop item UI component.
/// </summary>
public class ParticleShopItem : MonoBehaviour
{
    [SerializeField] private Image particleIconImage;
    [SerializeField] private Text particleNameText;
    [SerializeField] private Text particlePriceText;
    [SerializeField] private Image lockedOverlay;              // Gray overlay if locked
    [SerializeField] private Text ownedBadgeText;             // "OWNED" badge
    [SerializeField] private Button selectButton;

    public void SetupUI(ParticleCosmetic cosmetic, bool isUnlocked, System.Action onSelected)
    {
        if (particleNameText != null)
            particleNameText.text = cosmetic.name;

        if (particlePriceText != null)
        {
            if (isUnlocked)
                particlePriceText.text = "✓ OWNED";
            else if (cosmetic.gemsPrice == 0)
                particlePriceText.text = "LOCKED";
            else
                particlePriceText.text = $"{cosmetic.gemsPrice}💎";
        }

        if (lockedOverlay != null)
            lockedOverlay.gameObject.SetActive(!isUnlocked && cosmetic.gemsPrice > 0);

        if (ownedBadgeText != null)
            ownedBadgeText.gameObject.SetActive(isUnlocked);

        if (selectButton != null)
            selectButton.onClick.AddListener(() => onSelected?.Invoke());
    }
}
