# Unity IAP Setup — Vault Dash

## Package
`com.unity.purchasing 4.9.3` — added to `Packages/manifest.json` ✅

## Product IDs (Consumable)

| Product ID    | Gems  | Price USD | Display Name  |
|---------------|-------|-----------|---------------|
| `gems_80`     | 80    | $0.99     | Starter Pack  |
| `gems_500`    | 500   | $4.99     | Value Pack    |
| `gems_1200`   | 1200  | $12.99    | Best Value    |
| `gems_6500`   | 6500  | $49.99    | Mega Pack     |

## Google Play Setup
1. Open Google Play Console → Your App → Monetize → Products → In-app products
2. Create product for each ID above (match exact IDs)
3. Set prices (can localise per region)
4. Publish products (must be in at least "alpha" track)

## Apple App Store Setup  
1. App Store Connect → My Apps → Your App → In-App Purchases
2. Create Consumable for each product ID
3. Submit for review alongside a new build

## Script: IAPManager.cs
- **Auto-initializes** on Start via `UnityPurchasing.Initialize()`
- **Call** `IAPManager.Instance.BuyProduct("gems_80")` from Shop UI
- **Listen** to `IAPManager.OnGemsGranted` for purchase success
- **Listen** to `IAPManager.OnPurchaseError` for failures

## Gem Balance
Stored in `PlayerPrefs["VaultDash_Gems"]`.  
TODO Phase 3: sync to Nakama server wallet for anti-cheat.

## Receipt Validation
Currently: client-side only (Unity IAP built-in).  
Phase 3: Server-side validation via Firebase Cloud Functions.

## Without Package
`#if UNITY_PURCHASING` guards ensure clean compilation.  
In editor/stub mode, `BuyProduct()` immediately grants gems (for testing). ✅
