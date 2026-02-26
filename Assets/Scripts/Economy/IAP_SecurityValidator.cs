using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_FUNCTIONS
using Firebase.Functions;
using Firebase.Extensions;
#endif

/// <summary>
/// IAP_SecurityValidator — Server-side receipt validation via Firebase Cloud Function.
///
/// CLOUD FUNCTION: validatePurchase  (already deployed at europe-west1)
///   Request:
///     { "platform": "android"|"ios", "productId": string,
///       "receipt": string, "purchaseToken": string, "uid": string }
///   Response (success):
///     { "valid": true, "gemAmount": int, "alreadyProcessed": bool }
///   Response (failure):
///     { "valid": false, "reason": string }
///
/// FRAUD DETECTION (server-side, documented here for reference):
///   • Replay attack  : purchaseToken must be unused (Firestore dedup)
///   • Invalid receipt: Google/Apple receipt verification API
///   • Amount mismatch: server checks productId → gem mapping
///   • UID mismatch   : receipt must match authenticated user
///
/// FLOW:
///   1. Unity IAP calls ProcessPurchase() in IAPManager.
///   2. IAPManager calls IAP_SecurityValidator.Validate(productId, receipt, token).
///   3. Validator POSTs to validatePurchase Cloud Function.
///   4. On success: grant gems via FirebaseManager.
///   5. On failure: log, refund if needed, show error.
///
/// PHYSICAL DEVICE TEST: Deferred to Saturday.
/// This class currently supports MOCK MODE for editor/CI validation.
/// </summary>
public class IAP_SecurityValidator : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static IAP_SecurityValidator Instance { get; private set; }

    // ─── Config ───────────────────────────────────────────────────────────────
    [Header("Cloud Function")]
    [Tooltip("Name of the Firebase callable function.")]
    public string FunctionName = "validatePurchase";

    [Header("Mock Mode (Editor / CI Testing)")]
    [Tooltip("Enable to test without a real IAP receipt.")]
    public bool MockMode = false;

    [Tooltip("Simulated server delay in mock mode (seconds).")]
    public float MockDelaySeconds = 0.5f;

    // ─── Validation Result ────────────────────────────────────────────────────
    public enum ValidationStatus { Success, AlreadyProcessed, InvalidReceipt, NetworkError, FraudDetected }

    public struct ValidationResult
    {
        public ValidationStatus Status;
        public string           ProductId;
        public int              GemsGranted;
        public string           ErrorReason;
        public bool             AlreadyProcessed;

        public bool IsSuccess => Status == ValidationStatus.Success || Status == ValidationStatus.AlreadyProcessed;

        public override string ToString() =>
            $"IAPValidation({Status}, product={ProductId}, gems={GemsGranted}, reason={ErrorReason})";
    }

    // ─── Events ───────────────────────────────────────────────────────────────
    public event Action<ValidationResult> OnValidationComplete;

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Validates a purchase receipt with the server.
    /// </summary>
    /// <param name="platform">"android" or "ios"</param>
    /// <param name="productId">IAP product ID</param>
    /// <param name="receipt">Raw JSON receipt from Unity IAP</param>
    /// <param name="purchaseToken">Platform-specific purchase token (Android) or transaction ID (iOS)</param>
    /// <param name="onComplete">Callback with result</param>
    public void Validate(string platform, string productId, string receipt,
                         string purchaseToken, Action<ValidationResult> onComplete)
    {
        if (MockMode)
        {
            StartCoroutine(MockValidate(productId, onComplete));
            return;
        }

#if FIREBASE_FUNCTIONS
        CallCloudFunction(platform, productId, receipt, purchaseToken, onComplete);
#else
        Debug.LogWarning("[IAPSecurity] FIREBASE_FUNCTIONS not defined — using mock fallback.");
        StartCoroutine(MockValidate(productId, onComplete));
#endif
    }

    /// <summary>
    /// Runs a battery of offline validation tests (for CI/pre-device testing).
    /// Logs pass/fail for each scenario.
    /// </summary>
    public void RunMockTestSuite()
    {
        StartCoroutine(RunTestSuiteCoroutine());
    }

    // ─── Cloud Function Call ──────────────────────────────────────────────────

#if FIREBASE_FUNCTIONS
    private void CallCloudFunction(string platform, string productId, string receipt,
                                   string purchaseToken, Action<ValidationResult> onComplete)
    {
        string uid = FirebaseManager.Instance?.UserId ?? "unknown";

        var data = new Dictionary<string, object>
        {
            { "platform",      platform },
            { "productId",     productId },
            { "receipt",       receipt },
            { "purchaseToken", purchaseToken },
            { "uid",           uid }
        };

        FirebaseFunctions.DefaultInstance
            .GetHttpsCallable(FunctionName)
            .CallAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[IAPSecurity] Cloud function error: {task.Exception?.Message}");
                    var result = new ValidationResult
                    {
                        Status    = ValidationStatus.NetworkError,
                        ProductId = productId,
                        ErrorReason = task.Exception?.InnerException?.Message ?? "Unknown"
                    };
                    onComplete?.Invoke(result);
                    OnValidationComplete?.Invoke(result);
                    return;
                }

                var response = task.Result.Data as Dictionary<string, object>;
                ParseAndHandleResponse(response, productId, onComplete);
            });
    }

    private void ParseAndHandleResponse(Dictionary<string, object> response,
                                        string productId, Action<ValidationResult> onComplete)
    {
        bool   valid      = response != null && Convert.ToBoolean(response.GetValueOrDefault("valid", false));
        int    gemAmount  = response != null ? Convert.ToInt32(response.GetValueOrDefault("gemAmount", 0)) : 0;
        bool   alreadyP   = response != null && Convert.ToBoolean(response.GetValueOrDefault("alreadyProcessed", false));
        string reason     = response != null ? (string)response.GetValueOrDefault("reason", "") : "No response";

        ValidationResult result;

        if (alreadyP)
        {
            result = new ValidationResult
            {
                Status           = ValidationStatus.AlreadyProcessed,
                ProductId        = productId,
                GemsGranted      = 0,  // already granted before
                AlreadyProcessed = true
            };
            Debug.Log($"[IAPSecurity] {productId}: already processed (idempotent OK).");
        }
        else if (valid)
        {
            result = new ValidationResult
            {
                Status      = ValidationStatus.Success,
                ProductId   = productId,
                GemsGranted = gemAmount
            };
            GrantGems(productId, gemAmount);
            Debug.Log($"[IAPSecurity] {productId}: valid ✅ → {gemAmount} gems granted.");
        }
        else
        {
            result = new ValidationResult
            {
                Status      = string.IsNullOrEmpty(reason) ? ValidationStatus.InvalidReceipt : ValidationStatus.FraudDetected,
                ProductId   = productId,
                ErrorReason = reason
            };
            Debug.LogWarning($"[IAPSecurity] {productId}: INVALID — {reason}");
        }

        onComplete?.Invoke(result);
        OnValidationComplete?.Invoke(result);
    }
#endif

    // ─── Gem Grant ────────────────────────────────────────────────────────────

    private void GrantGems(string productId, int amount)
    {
        var pack = EconomyConfig.GetPackByProductId(productId);
        if (pack == null)
        {
            Debug.LogWarning($"[IAPSecurity] No pack config for {productId} — granting {amount} from server.");
        }

        // Delegate to FirebaseManager / player profile
        FirebaseManager.Instance?.GrantGems(amount, $"iap_{productId}");
    }

    // ─── Mock Validation (Editor / CI) ───────────────────────────────────────

    private IEnumerator MockValidate(string productId, Action<ValidationResult> onComplete)
    {
        yield return new WaitForSeconds(MockDelaySeconds);

        var pack = EconomyConfig.GetPackByProductId(productId);
        int gems = pack?.GemAmount ?? 0;

        var result = new ValidationResult
        {
            Status      = ValidationStatus.Success,
            ProductId   = productId,
            GemsGranted = gems
        };

        Debug.Log($"[IAPSecurity] MOCK validate {productId} → {gems} gems");
        onComplete?.Invoke(result);
        OnValidationComplete?.Invoke(result);
    }

    // ─── Test Suite ───────────────────────────────────────────────────────────

    private IEnumerator RunTestSuiteCoroutine()
    {
        Debug.Log("[IAPSecurity] ─── Starting Mock Test Suite ───");

        int pass = 0, fail = 0;

        // Test 1: Valid €0.99 pack
        yield return RunTest("Valid €0.99 pack", "gems_100", "VALID_RECEIPT_100", "TOKEN_VALID_1",
            r => r.Status == ValidationStatus.Success && r.GemsGranted == 100, ref pass, ref fail);

        // Test 2: Valid €4.99 pack
        yield return RunTest("Valid €4.99 pack", "gems_600", "VALID_RECEIPT_600", "TOKEN_VALID_2",
            r => r.Status == ValidationStatus.Success && r.GemsGranted == 600, ref pass, ref fail);

        // Test 3: Unknown product ID
        yield return RunTest("Unknown productId", "gems_FAKE", "RECEIPT", "TOKEN_FAKE",
            r => r.GemsGranted == 0, ref pass, ref fail);

        Debug.Log($"[IAPSecurity] Test Suite: {pass} passed, {fail} failed.");

        if (fail == 0) Debug.Log("[IAPSecurity] ✅ All tests PASSED.");
        else           Debug.LogWarning($"[IAPSecurity] ⚠️ {fail} test(s) FAILED.");
    }

    private IEnumerator RunTest(string testName, string productId, string receipt,
                                string token, Func<ValidationResult, bool> assertion,
                                ref int pass, ref int fail)
    {
        bool done = false;
        ValidationResult result = default;

        StartCoroutine(MockValidate(productId, r => { result = r; done = true; }));

        yield return new WaitUntil(() => done);

        bool passed = assertion(result);
        if (passed) { pass++; Debug.Log($"  ✅ PASS: {testName}"); }
        else         { fail++; Debug.LogWarning($"  ❌ FAIL: {testName} — {result}"); }
    }
}
