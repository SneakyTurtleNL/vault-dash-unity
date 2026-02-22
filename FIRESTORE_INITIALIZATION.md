# FIRESTORE INITIALIZATION GUIDE
## Automated Setup for Saturday Testing

---

## 🎯 CRITICAL DOCUMENTS FOR SATURDAY

**Before running app:**
1. Initialize `config/seasons/season_1` document
2. Optional: Seed test player data

---

## 📋 MANUAL SETUP (Firebase Console)

### Option A: Create season_1 Document

1. Go to **Firebase Console** → `vault-dash` project → **Firestore Database**
2. Create collection (if not exists): `config`
3. Create document: `seasons` → `season_1`

**Document Content:**
```json
{
  "id": "season_1",
  "name": "Season 1: The Vault",
  "startDate": 1740163200000,
  "endDate": 1745347200000,
  "active": true,
  "currentSeasonId": "season_1",
  "loadingScreenTheme": {
    "seasonId": "season_1",
    "seasonLabel": "Season 1 · The Vault",
    "backgroundColor": "#0A0A1A",
    "accentColor": "#2979FF",
    "characterSkinId": "knox",
    "eventText": "Bereik de kluis voordat anderen het doen!",
    "backgroundImageUrl": ""
  },
  "rewards": {
    "levelRewards": [
      {"level": 5, "gems": 10, "coins": 500},
      {"level": 10, "gems": 25, "coins": 1000},
      {"level": 15, "gems": 50, "coins": 2000},
      {"level": 20, "gems": 100, "coins": 5000}
    ]
  },
  "trophyResets": {
    "lastReset": 1740163200000,
    "nextReset": 1745347200000
  }
}
```

---

## ⚙️ CLOUD FUNCTION SETUP (Alternative)

**If you have Cloud Functions deployed:**

```bash
# Call initSeason Cloud Function
curl -X POST https://region-project-cloudfunctions.net/initSeason \
  -H "Authorization: Bearer $ID_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "seasonId": "season_1",
    "name": "Season 1: The Vault",
    "durationDays": 30
  }'
```

---

## ✅ VERIFICATION

After creating `config/seasons/season_1`:

1. Go to **Firestore** → `config` collection
2. Should see `currentSeason` document with `currentSeasonId: "season_1"`
3. Should see `seasons` subcollection with `season_1` document
4. **Status**: ✅ Ready for app test

---

## 🎮 WHAT HAPPENS ON APP STARTUP

1. App boots → Firebase init
2. LoadingScreenManager checks `config/seasons/active`
3. Retrieves `season_1` loadingScreenTheme
4. Displays themed loading screen (Knox character, blue accent)
5. Game starts with Season 1 active

---

## 🧪 TEST SEQUENCE

**After Firestore init:**

1. Build APK: `git push origin main` → GitHub Actions builds (5-10 min)
2. Download APK from GitHub Releases
3. `adb install -r vault-dash-release.apk`
4. Launch app
5. Should see:
   - SplashScreen (2 sec)
   - LoadingScreen with Season 1 theme
   - Main menu with all features
   - Play → Game starts with easyMode active
   - Game over → Vault animation
   - Results screen

---

## ⚠️ TROUBLESHOOTING

**LoadingScreen hangs (>5 sec):**
- **Cause**: `config/seasons/season_1` not found
- **Fix**: Create manually (Firebase Console) or run initSeason function

**LoadingScreen crashes:**
- Check logcat: `adb logcat | grep vault-dash`
- Likely: JSON parsing error in loadingScreenTheme
- Verify: All fields match LoadingScreenTheme.dart model

**Game won't start after loading:**
- Firestore Security Rules may be blocking access
- Check: `players/{uid}/` document auto-created on first run
- Or: `FirebaseAuth.instance.currentUser` returns null

---

## 📊 SCHEMA REFERENCE

### config/seasons/season_1
```
{
  id: string                    // "season_1"
  name: string                  // "Season 1: The Vault"
  active: boolean               // true
  startDate: timestamp          // Firebase timestamp
  endDate: timestamp            // Firebase timestamp
  loadingScreenTheme: {
    seasonId: string
    seasonLabel: string
    backgroundColor: string     // hex color
    accentColor: string         // hex color
    characterSkinId: string     // "knox"
    eventText: string
    backgroundImageUrl: string  // URL or empty
  },
  rewards: {
    levelRewards: [{
      level: number
      gems: number
      coins: number
    }]
  },
  trophyResets: {
    lastReset: timestamp
    nextReset: timestamp
  }
}
```

---

**Ready to test?** Firestore initialized → Build APK → Download → Test 🚀
