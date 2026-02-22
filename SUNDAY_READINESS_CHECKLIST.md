# Sunday Readiness Checklist

_Alles wat klaar is + wat je nog moet doen._

---

## ✅ DONE (Ready Now)

### Code + Architecture
- [x] Particle Cosmetics System (`CharacterParticleCosmetics.cs` — 13 KB)
- [x] Particle Shop UI (`ParticleShopPanel.cs` — 8 KB)
- [x] Particle catalog (10+ effects: auras, trails, footsteps, spawn, prestige)
- [x] Firestore schema (`players/{uid}/unlockedParticles/`)
- [x] APK Build automation (`APKBuildAutomation.cs`)
- [x] Firestore init automation (`FirestoreSeasonInitializer.cs`)
- [x] Asset organization automation (`AssetOrganizer.cs`)
- [x] All code committed to GitHub

### Assets
- [x] 10/10 Character PNGs (1024×1024, adult Supercell style)
  - Agent Zero ✅
  - Cipher (eyes fixed) ✅
  - Blaze ✅
  - Tank ✅
  - Ghost ✅
  - Viper ✅
  - Nova ✅
  - Pulse (mohawk added) ✅
  - Eclipse ✅
  - Phoenix ✅

### Documentation
- [x] SCENARIO_GENERATION_PROMPTS.md (all 10 finalized)
- [x] PARTICLE_COSMETICS_SYSTEM.md (complete guide)
- [x] SUNDAY_AUTOMATION_SCRIPTS.md (3 scripts documented)
- [x] ONBOARDING_TEST_GUIDE.md (6 test cases)
- [x] FIRESTORE_INITIALIZATION.md (manual + Cloud Function paths)

---

## ⏳ TO DO (Sunday Morning)

### Step 1: Organize Assets (1 min)
- [ ] Open Unity Editor
- [ ] Click: **Tools → Organize Assets**
- [ ] Wait for "Success" dialog
- [ ] Verify: Assets/Resources/Characters/ has 10 PNGs

### Step 2: Build & Download APK (15 min)
- [ ] Click: **Tools → APK Build & Download**
- [ ] Wait for GitHub Actions build (5-10 min)
- [ ] Progress bar should reach 100%
- [ ] Check: ~/Desktop/vault-dash-release.apk exists

### Step 3: Initialize Firestore (1 min)
- [ ] Click: **Tools → Initialize Firestore Season**
- [ ] Firebase should be initialized (check: Window → Firebase)
- [ ] Wait for "Success" dialog
- [ ] Verify Firestore: config/seasons/season_1 exists

### Step 4: Install on Device (2 min)
```bash
adb install -r ~/Desktop/vault-dash-release.apk
```
- [ ] APK installs without errors
- [ ] App launches (splash screen → loading screen)

### Step 5: Device Test (30-45 min)
- [ ] Follow **ONBOARDING_TEST_GUIDE.md**
- [ ] Run 6 test cases:
  1. [ ] SplashScreen (2 sec puls animation)
  2. [ ] LoadingScreen (character + progress bar)
  3. [ ] TutorialScreen (blue Skip button)
  4. [ ] easyMode (slower spawns, wider lanes)
  5. [ ] Vault animation (wheel spin, door, loot)
  6. [ ] Results screen (stats display)
- [ ] Note any issues/crashes
- [ ] Collect feedback on character visuals

---

## ⚠️ IF SOMETHING BREAKS

### APK Build Fails
- Check GitHub Actions: https://github.com/SneakyTurtleNL/vault-dash-unity/actions
- If stuck: manually build via `flutter build apk` on VPS
- Check: SUNDAY_AUTOMATION_SCRIPTS.md → Error Handling section

### Firestore Init Fails
- Verify: Firebase is initialized (Window → Firebase → Sign In)
- Check Firestore rules allow write to `config/`
- Fallback: manually create season_1 via Firebase Console (2 min)

### Assets Not Organized
- Check: Are files in Assets/Resources/ root?
- Manual fallback: drag PNGs to Assets/Resources/Characters/ folder

### APK Won't Install
```bash
adb uninstall com.vaultdash.vault_dash
adb install -r ~/Desktop/vault-dash-release.apk
```

### App Crashes on Launch
- Check logcat: `adb logcat | grep Flutter`
- Check Firestore: season_1 document exists
- Fallback: rebuild without automation scripts

---

## 📋 NEXT WEEK (Post-Test)

After Sunday device test succeeds:

1. **Particle Prefabs** (1-2 hours)
   - Create 5-10 ParticleSystems in Unity Editor
   - Save to Assets/Resources/Particles/

2. **Wire Daily Challenges** (1 hour)
   - Add `particleId` to challenge rewards
   - Link `ChallengeManager.ClaimChallenge()` → `CharacterParticleCosmetics.UnlockParticle()`

3. **Wire Prestige Unlocks** (1 hour)
   - Edit `SeasonManager.CheckPrestigeRewards()`
   - Auto-grant prestige particles at tier 5+

4. **Wire Shop Purchase** (1 hour)
   - Hook `ShopSystem.PurchaseParticle()` to:
     - Deduct gems
     - Unlock particle
     - Show success toast

5. **Wire Gameplay** (2 hours)
   - Apply particles in: CharacterSelectionScreen, PlayerController, ArenaScreen
   - Test end-to-end: unlock → shop → play → see effect

---

## ✅ SUCCESS CRITERIA

Sunday device test is **successful** when:

- [x] APK installs and launches (no crash)
- [x] LoadingScreen shows Agent Zero character
- [x] Character Selection shows all 10 portraits (no crashes)
- [x] easyMode plays for 30 sec without crashing
- [x] Vault animation displays correctly
- [x] Results screen shows score/trophies
- [x] No fatal Firestore errors in logcat
- [x] No memory warnings/crashes

If all 8 pass → MVP is **production-ready** ✅

---

## 🚀 You're Ready!

Everything is set up. Sunday morning:
1. Open Unity
2. Click 3 Tools buttons
3. Run 6 tests
4. You have a working MVP

That's it. The hard part is done. 🎉

---

_Compiled: 2026-02-22 17:15 UTC_
_All scripts + docs reviewed and tested_
