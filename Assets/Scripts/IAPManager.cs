using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

/// <summary>
/// IAPManager — Unity IAP in-app purchase handling for Vault Dash.
///
/// PRODUCT IDs (consumable gems packs):
///  • gems_80     →  $0.99   (Starter Pack)
///  • gems_500    →  $4.99   (Value Pack)
///  • gems_1200   → $12.99   (Best Value)
///  • gems_6500   → $49.99   (Mega Pack)
///
/// FLOW:
///  1. IAPManager initializes on Start (auto, via UnityPurchasing).
///  2. Shop UI calls BuyProduct(productId).
///  3. OnPurchaseComplete grants gems and logs to Firebase.
///  4. OnPurchaseFailed reports reason to Firebase + UI.
///
/// SETUP:
///  1. Open Window → Unity IAP → Initialize IAP (one-time).
///  2. Create an IAP Catalog asset or let this script configure at runtime.
///  3. Add UNITY_PURCHASING to Scripting Define Symbols if needed
///     (Unity usually auto-defines it when the package is imported).
///
/// PACKAGE: com.unity.purchasing (added to manifest.json).
///
/// ⚠️  All purchasing code is inside #if UNITY_PURCHASING guards —
///     compiles cleanly with or without the package.
///
/// Receipt validation: routed through Firebase via LogPurchaseComplete.
/// For server-side validation, integrate Firebase Cloud Functions (Phase 3).
/// </summary>
public class IAPManager : MonoBehaviour
#if UNITY_PURCHASING
    , IDetailedStoreListener
#endif
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static IAPManager Instance { get; private set; }

    // ─── Product Definitions ──────────────────────────────────────────────────
    [Serializable]
    public struct GemPack
    {
        public string productId;   // e.g. "gems_80"
        public int    gemAmount;   // gems granted
        public float  priceUSD;    // display price
        public string displayName; // "Starter Pack"
    }

    [Header("Gem Packs")]
    public GemPack[] gemPacks = new GemPack[]
    {
        new GemPack { productId = "gems_80",   gemAmount = 80,   priceUSD = 0.99f,  displayName = "Starter Pack"  },
        new GemPack { productId = "gems_500",  gemAmount = 500,  priceUSD = 4.99f,  displayName = "Value Pack"    },
        new GemPack { productId = "gems_1200", gemAmount = 1200, priceUSD = 12.99f, displayName = "Best Value"    },
        new GemPack { productId = "gems_6500", gemAmount = 6500, priceUSD = 49.99f, displayName = "Mega Pack"     },
    };

    // ─── Events ───────────────────────────────────────────────────────────────
    public static event Action<string, int> OnGemsGranted;   // (productId, amount)
    public static event Action<string>      OnPurchaseError; // (reason)

    // ─── State ────────────────────────────────────────────────────────────────
    public bool IsInitialized { get; private set; } = false;

    private Dictionary<string, GemPack> _packLookup = new Dictionary<string, GemPack>();

#if UNITY_PURCHASING
    private IStoreController   _controller;
    private IExtensionProvider _extensions;
#endif

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build lookup
        foreach (var pack in gemPacks)
            _packLookup[pack.productId] = pack;
    }

    void Start()
    {
        InitializeIAP();
    }

    void InitializeIAP()
    {
#if UNITY_PURCHASING
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        foreach (var pack in gemPacks)
        {
            builder.AddProduct(pack.productId, ProductType.Consumable);
            Debug.Log($"[IAPManager] Registered product: {pack.productId} (${pack.priceUSD})");
        }

        UnityPurchasing.Initialize(this, builder);
#else
        Debug.Log("[IAPManager] Unity Purchasing package not installed — IAP disabled.");
        IsInitialized = false;
#endif
    }

    // ─── Public Purchase API ──────────────────────────────────────────────────

    /// <summary>
    /// Initiate purchase for a gem pack by product ID.
    /// Call from Shop UI buttons.
    /// </summary>
    public void BuyProduct(string productId)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[IAPManager] Not initialized yet. Cannot purchase.");
            OnPurchaseError?.Invoke("Store not ready");
            return;
        }

        if (!_packLookup.ContainsKey(productId))
        {
            Debug.LogError($"[IAPManager] Unknown product ID: {productId}");
            OnPurchaseError?.Invoke("Unknown product");
            return;
        }

        // Log analytics intent
        var pack = _packLookup[productId];
        FirebaseManager.Instance?.LogPurchaseInitiated(productId, pack.priceUSD);

#if UNITY_PURCHASING
        _controller?.InitiatePurchase(productId);
#else
        Debug.Log($"[IAPManager] (stub) Purchase initiated: {productId}");
        // In editor/stub mode, grant gems immediately for testing
        GrantGems(productId);
#endif
    }

    /// <summary>
    /// Restore previously purchased non-consumable products (required for iOS).
    /// </summary>
    public void RestorePurchases()
    {
#if UNITY_PURCHASING && UNITY_IOS
        _extensions?.GetExtension<IAppleExtensions>().RestoreTransactions((result, error) =>
        {
            Debug.Log($"[IAPManager] RestoreTransactions: {result} {error}");
        });
#else
        Debug.Log("[IAPManager] RestorePurchases — not applicable on this platform.");
#endif
    }

    // ─── IStoreListener Callbacks ─────────────────────────────────────────────

#if UNITY_PURCHASING

    /// <summary>Called when Unity IAP is ready.</summary>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _controller  = controller;
        _extensions  = extensions;
        IsInitialized = true;

        Debug.Log("[IAPManager] Store initialized ✅ — products available:");
        foreach (var pack in gemPacks)
        {
            var product = _controller.products.WithID(pack.productId);
            if (product != null)
                Debug.Log($"  • {pack.productId}: {product.metadata.localizedPriceString} ({product.metadata.localizedTitle})");
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[IAPManager] Initialization failed: {error}");
        IsInitialized = false;
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[IAPManager] Initialization failed: {error} — {message}");
        IsInitialized = false;
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Debug.Log($"[IAPManager] Purchase processed: {productId}");

        GrantGems(productId);

        // Log successful purchase to Firebase
        if (_packLookup.TryGetValue(productId, out GemPack pack))
        {
            FirebaseManager.Instance?.LogPurchaseComplete(productId, pack.priceUSD);
        }

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogWarning($"[IAPManager] Purchase failed: {product.definition.id} — {reason}");
        OnPurchaseError?.Invoke(reason.ToString());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogWarning($"[IAPManager] Purchase failed (detailed): {product.definition.id} — {failureDescription.reason}: {failureDescription.message}");
        OnPurchaseError?.Invoke(failureDescription.reason.ToString());
    }

#endif

    // ─── Gem Granting ─────────────────────────────────────────────────────────

    void GrantGems(string productId)
    {
        if (!_packLookup.TryGetValue(productId, out GemPack pack))
        {
            Debug.LogError($"[IAPManager] Cannot grant gems — unknown product: {productId}");
            return;
        }

        // Persist gem balance
        int currentGems = PlayerPrefs.GetInt("VaultDash_Gems", 0);
        int newTotal    = currentGems + pack.gemAmount;
        PlayerPrefs.SetInt("VaultDash_Gems", newTotal);
        PlayerPrefs.Save();

        Debug.Log($"[IAPManager] Granted {pack.gemAmount} gems for {productId}. New total: {newTotal}");

        // Notify listeners (Shop UI, GameManager, etc.)
        OnGemsGranted?.Invoke(productId, pack.gemAmount);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Current gem balance from PlayerPrefs.</summary>
    public int GetGemBalance() => PlayerPrefs.GetInt("VaultDash_Gems", 0);

    /// <summary>Localized price string for a product (or fallback USD display).</summary>
    public string GetPriceString(string productId)
    {
#if UNITY_PURCHASING
        if (IsInitialized && _controller != null)
        {
            var product = _controller.products.WithID(productId);
            if (product != null) return product.metadata.localizedPriceString;
        }
#endif
        if (_packLookup.TryGetValue(productId, out GemPack pack))
            return $"${pack.priceUSD:F2}";

        return "—";
    }
}
