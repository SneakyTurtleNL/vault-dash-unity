# Firebase Analytics — Vault Dash

## Project
- **Firebase Project ID**: vault-dash-unity
- **Platform**: Android (primary), iOS (secondary)

## Installation Steps

### 1. Download Firebase SDK
```
https://firebase.google.com/download/unity
```
Download: `firebase_unity_sdk_x.y.z.zip`

### 2. Import Packages
In Unity: Assets → Import Package → Custom Package

Import these (in order):
1. `FirebaseAnalytics.unitypackage`  ← required
2. `FirebaseDatabase.unitypackage`   ← optional (real-time leaderboards)

### 3. Add google-services.json
- Go to Firebase Console → Project Settings → Android
- Download `google-services.json`  
- Place at: `Assets/google-services.json`

### 4. Enable Define Symbol
Project Settings → Player → Android → Scripting Define Symbols:
```
FIREBASE_ANALYTICS
```
This activates all Firebase code in `FirebaseManager.cs`.

## Tracked Events (from FirebaseManager.cs)

| Event | Parameters | Trigger |
|-------|-----------|---------|
| `game_start` | arena, character | GameManager.StartGame() |
| `game_over` | score, distance, winner | GameManager.OnGameOver() |
| `opponent_collision` | distance_at_impact | Player.OnTriggerEnter(Obstacle) |
| `loot_collected` | loot_type, count | Player.OnTriggerEnter(Coin/Gem) |
| `power_up_used` | power_up_type | (hook in power-up activation code) |
| `purchase_initiated` | item_id, price | IAPManager.BuyProduct() |
| `purchase` (standard) | item_id, value, currency | IAPManager.ProcessPurchase() |

## User Properties

| Property | Source |
|----------|--------|
| `player_level` | PlayerPrefs["VaultDash_PlayerLevel"] |
| `selected_character` | CharacterDatabase profile |
| `total_matches` | Nakama match history |

## Without SDK (stub mode)
FirebaseManager compiles and runs cleanly without the SDK.  
All events are logged to `Debug.Log` only. No crashes. ✅
