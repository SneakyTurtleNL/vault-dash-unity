# Sunday Automation Scripts — Complete Guide

_Three one-click scripts to automate the Sunday device test workflow._

---

## Overview

**What**: Three automation scripts to handle APK building, Firestore setup, and asset organization.

**When to use**: Sunday morning, after characters are generated and committed.

**Result**: APK on Desktop, Firestore ready, assets organized — ready for device test.

---

## Scripts

### 1. APKBuildAutomation.cs

**Location**: `Assets/Editor/APKBuildAutomation.cs`

**Purpose**: One-click build + GitHub Actions polling + APK download

**Workflow**:
```
1. Stage & commit all changes
2. Push to GitHub main
3. GitHub Actions auto-builds (5-10 min)
4. Poll GitHub Actions every 30 sec
5. When build succeeds → download APK
6. Save to Desktop: vault-dash-release.apk
```

**Usage**:
```
Unity Editor → Tools → APK Build & Download
```

**What it does**:
- Commits current state with timestamp
- Pushes to origin/main
- Polls GitHub API for latest workflow run status
- Times out after 15 minutes
- Downloads APK artifact from GitHub Releases
- Moves to Desktop for adb flashing

**Requirements**:
- Git installed + configured
- GitHub token in code (already set: ghp_jz...)
- Network connection

**Output**:
```
~/Desktop/vault-dash-release.apk
```

---

### 2. FirestoreSeasonInitializer.cs

**Location**: `Assets/Editor/FirestoreSeasonInitializer.cs`

**Purpose**: Initialize Firestore config/seasons/season_1 for device testing

**Creates**:
```
config/seasons/season_1 document with:
├── loadingScreenTheme (character, eventText, colors, background)
├── rewards (gem tiers: 50/100/200/500/1000)
├── active: true
├── startDate: now
└── endDate: now + 30 days
```

**Usage**:
```
Unity Editor → Tools → Initialize Firestore Season
```

**What it does**:
- Connects to Firebase (must be logged in)
- Creates season_1 config document
- Sets LoadingScreenTheme (Agent Zero, blue/gold colors)
- Sets reward tiers
- Marks season as active

**Requirements**:
- Firebase initialized + user logged in
- Network connection
- Firestore database set up

**Output**:
```
Firestore: config/seasons/season_1 (created)
```

---

### 3. AssetOrganizer.cs

**Location**: `Assets/Editor/AssetOrganizer.cs`

**Purpose**: Organize raw generated assets into proper folder structure

**Creates**:
```
Assets/Resources/
├── Characters/
│   ├── agent_zero.png
│   ├── cipher.png
│   ├── blaze.png
│   ├── tank.png
│   ├── ghost.png
│   ├── viper.png
│   ├── nova.png
│   ├── pulse.png
│   ├── eclipse.png
│   └── phoenix.png (10 total)
├── Icons/
│   ├── trophy.png
│   ├── gem.png
│   ├── coin.png
│   ... (16 total)
└── Backgrounds/
    ├── rookie_bg.png
    ├── silver_bg.png
    ├── gold_bg.png
    ├── diamond_bg.png
    └── legend_bg.png (5 total)
```

**Usage**:
```
Unity Editor → Tools → Organize Assets
```

**What it does**:
- Creates Character/Icons/Background folders
- Scans for PNG files by name
- Moves assets to correct folders
- Removes duplicates
- Refreshes asset database

**Requirements**:
- Assets in `Assets/Resources/` root
- PNGs named correctly (agent_zero.png, etc.)

**Output**:
```
Assets/Resources/Characters/ (10 files)
Assets/Resources/Icons/ (16 files)
Assets/Resources/Backgrounds/ (5 files)
```

---

## Sunday Workflow

### Step 1: Prepare Assets (Manual)
```
1. Verify all 10 characters look good (no chibi, adult proportions)
2. Commit characters + particle system code
3. Push to GitHub (initial commit)
```

### Step 2: Organize Assets (Automated)
```
Unity → Tools → Organize Assets
Confirms organization is complete
```

### Step 3: Build APK (Automated)
```
Unity → Tools → APK Build & Download
⏳ Waits for GitHub Actions (5-10 min)
📥 Downloads APK to Desktop
```

### Step 4: Initialize Firestore (Automated)
```
Unity → Tools → Initialize Firestore Season
Firestore season_1 created with defaults
```

### Step 5: Device Test (Manual)
```
adb install -r ~/Desktop/vault-dash-release.apk
Follow ONBOARDING_TEST_GUIDE.md (6 test cases)
Collect feedback
```

---

## Error Handling

### APKBuildAutomation

**Error**: "Build timed out after 15 minutes"
- GitHub Actions may be slow
- Check: github.com/SneakyTurtleNL/vault-dash-unity/actions
- Manually download from Releases if timeout occurs

**Error**: "Command failed: git push"
- Merge conflicts likely
- Resolve manually: `git fetch origin && git rebase origin/main`
- Then retry Tools → APK Build & Download

**Error**: "Could not find APK in latest release"
- Build might have failed
- Check GitHub Actions logs
- Ensure GitHub token is valid

### FirestoreSeasonInitializer

**Error**: "Failed to initialize season: Not authenticated"
- Firebase not initialized
- Ensure `FirebaseApp.CheckAndFixDependencies()` passed
- Try again after reload

**Error**: "Failed to initialize season: Permission denied"
- Firestore rules may be blocking writes
- Check Firebase Console → Firestore Rules
- Ensure current user has write access to `config/`

### AssetOrganizer

**Error**: "File not found: agent_zero.png"
- Assets not generated or named incorrectly
- Check `Assets/Resources/` for files
- Ensure names match exactly (lowercase)

**Error**: "Directory creation failed"
- Permissions issue
- Ensure you have write access to Assets/

---

## Testing Checklist

After running scripts:

- [ ] Assets organized in Resources/Characters/Icons/Backgrounds/
- [ ] APK on Desktop (vault-dash-release.apk)
- [ ] Firestore season_1 document created (Firebase Console)
- [ ] APK installs on device without errors
- [ ] LoadingScreen shows Agent Zero character
- [ ] Character Selection shows all 10 portraits
- [ ] Game starts and plays (30 sec tunnel)
- [ ] Results screen shows score

---

## Code Locations

| Script | Purpose | Location |
|--------|---------|----------|
| APKBuildAutomation | Build + download | Assets/Editor/ |
| FirestoreSeasonInitializer | Season init | Assets/Editor/ |
| AssetOrganizer | Asset organization | Assets/Editor/ |

---

## Future Enhancements

1. **APK Installation**: Auto-run `adb install` after download
2. **Device Logging**: Auto-capture logcat during test
3. **Screenshot Validation**: Verify LoadingScreen + GameScreen via screenshots
4. **Test Report**: Auto-generate HTML report of test results
5. **Rollback**: Script to revert to previous APK if needed

---

## Troubleshooting

**General Issues**:
- Ensure git config is correct: `git config user.name` / `git config user.email`
- Check GitHub token expiry: https://github.com/settings/tokens
- Verify Firestore database exists: Firebase Console
- Check network connection (all scripts require internet)

**After Script Runs**:
- If no output: Check Editor console (Window → General → Console)
- If progress bar stuck: Check system Task Manager for hung Unity processes
- If partial completion: Rerun script — should pick up where it left off

---

**Status**: ✅ Ready to use. All three scripts integrated into Sunday workflow.

**Next**: Just run the three Tools menus in order on Sunday morning! 🚀
