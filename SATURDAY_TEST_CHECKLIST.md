# Saturday Device Test Checklist — Week 5
Generated: 2026-02-26 | Target device: Android (physical)

---

## How to Use

Mark each item:
- ✅ PASS — Works as expected
- ❌ FAIL — Bug found (note what happened)
- ⏭️ SKIP — Deferred / untestable on device today

---

## 1. Ghost/Bot Match System (GhostMatchSystem.cs)

- [ ] In Editor: set `GhostMatchSystem.ccuThreshold = 999999` (force ghost mode)
- [ ] Start a ranked match → should auto-use ghost opponent (no real player)
- [ ] Top bar shows opponent name + level from replay data
- [ ] Opponent distance/HP animates smoothly during run
- [ ] No "BOT" or "GHOST" indicator visible to player
- [ ] Match ends normally (win/lose) when ghost replay finishes

**Expected**: Seamless ghost match, indistinguishable from real player

---

## 2. P2W Mode Split (GameMode.cs)

- [ ] **Ranked mode**: Start match → revive button does NOT appear on death
- [ ] **Casual mode**: Start match → revive button DOES appear on death
- [ ] **Solo mode**: Revive button available
- [ ] **Ranked leaderboard**: Run with revive in Solo → score NOT submitted to ranked LB
- [ ] `GameMode.Current.IsLeaderboardEligible()` returns false if revive was used

**Expected**: No revive option in Ranked/PvP; revive available in Solo/Casual

---

## 3. Nakama Latency Validation (LatencyValidator.cs)

- [ ] On match start: `LatencyValidator.Instance.ValidateLatency(...)` called
- [ ] Good connection (< 100ms): match proceeds normally
- [ ] High latency simulation (disconnect device from WiFi briefly): ghost fallback triggered
- [ ] Console shows RTT values for each ping attempt
- [ ] `LatencyResult.Status` shown in debug log

**Expected**: <100ms = normal match; >150ms = ghost match substituted

---

## 4. Asset Audit (AssetAudit.cs)

- [ ] Add `AssetAudit` component to test scene, set `RunOnStart = true`
- [ ] All 10 character portraits load (512×512)
- [ ] All 16 UI icons load (256×256)
- [ ] All 5 arena backgrounds load (1024×512)
- [ ] Console shows `ALL PASS ✅` or lists specific failures
- [ ] In Editor: right-click → "Generate Asset Audit Report" → check `ASSET_AUDIT_REPORT.md`
- [ ] **Visual check**: portraits look sharp on device (not pixelated or stretched)
- [ ] **Visual check**: icons consistent style (not mix of flat/3D/painted)

**Expected**: 31/31 assets load; visual consistency confirmed on screen

---

## 5. IAP Security (IAP_SecurityValidator.cs)

- [ ] In Editor: `MockMode = true` → `RunMockTestSuite()` → all 3 tests pass
- [ ] Valid product IDs: gems_100, gems_600, gems_1500, gems_9000
- [ ] Purchase €0.99 pack → receives 100 gems (check PlayerPrefs `VaultDash_Gems`)
- [ ] Purchase €4.99 pack → receives 600 gems
- [ ] **Saturday only** (physical payment): Real IAP → validatePurchase Cloud Function called → gems granted
- [ ] Duplicate receipt test: same token → `AlreadyProcessed` response, no double grant

**Expected**: Valid receipts grant correct gems; invalid/replay receipts rejected

---

## 6. Power-up Balance (PowerUpConfig.cs)

- [ ] **Reverse**: activated → opponent controls inverted for exactly **1.2s** (not 2.0s)
- [ ] **TimeWarp** (was SlowMo): activated → OPPONENT slows down, player unaffected
- [ ] **Steal**: activate while opponent has active power-up → their power-up stolen & given to you
- [ ] **Pulse**: activate → restores exactly **50% HP** (not full revive)
- [ ] **ObstacleSpawn**: activate → 0.5s warning flash appears in opponent's lane, THEN obstacle spawns
- [ ] Power-up names in UI match new names (TimeWarp, not SlowMo)

**Expected**: All 5 balance changes visible and working correctly

---

## 7. Difficulty Curve (DifficultyManager.cs)

- [ ] At 1000 trophies (Gold start): scroll speed ≈ 7.0 u/s
- [ ] At 1500 trophies (Gold mid): scroll speed ≈ 7.4 u/s (smooth ramp)
- [ ] At 1999 trophies (Gold end): scroll speed ≈ 7.8 u/s
- [ ] At 2000 trophies (Diamond start): scroll speed ≈ 7.8 u/s (NO cliff)
- [ ] At 2000-1999 boundary: NO sudden jump in feel
- [ ] `DifficultyManager` context menu → "Print Curve Spot-Check" in console

**Expected**: Smooth ramp across Gold→Diamond; no sudden difficulty spike

---

## 8. Gem Economy (EconomyConfig.cs)

- [ ] Shop screen shows new prices: €0.99, €4.99, €9.99, €49.99
- [ ] Shop shows correct gem amounts: 100, 600, 1500, 9000
- [ ] Battle Pass shows BOTH unlock options: "€4.99" AND "950 gems"
- [ ] Battle Pass premium track: each level completed grants 10 gems
- [ ] 50 levels × 10 = 500 max gems per season (visible in Battle Pass info screen)
- [ ] `EconomyConfig.LogConfig()` in console shows correct values

**Expected**: New pricing visible in shop; battle pass gem unlock option present

---

## 9. Win Streak (WinStreakService.cs)

- [ ] Win 1 match: no badge shown, multiplier = 1.0×
- [ ] Win 2 matches: no badge, multiplier = 1.0×
- [ ] Win 3rd match: "🔥 Streak ×1.5" badge appears in top bar / victory screen
- [ ] Coins after win 3+: exactly 1.5× base amount (e.g. 100 base → 150 earned)
- [ ] Lose a match: streak resets to 0, badge disappears
- [ ] Close app and reopen within 4h: streak preserved
- [ ] Close app, wait 4h simulation (change device clock): streak reset
- [ ] `PlayerPrefs VaultDash_LongestWinStreak` increments correctly

**Expected**: 3+ win streak shows badge and applies 1.5× coin multiplier

---

## 10. Onboarding Bots (OnboardingBotService.cs)

- [ ] **New install** (clear PlayerPrefs): `onboardingMatchesPlayed = 0`
- [ ] Match 1: very easy bot → player loses (bot runs much faster)
- [ ] Match 2: easy bot → player wins (bot runs much slower, crashes into obstacles)
- [ ] Match 3: medium bot → fair match (~60% win rate; play 5 times, win 3-4)
- [ ] After match 3: normal matchmaking triggers (no bot)
- [ ] During onboarding: `IsInOnboarding = true`; after match 3: `false`

**Expected**: Structured difficulty ramp through first 3 matches; then real matchmaking

---

## 11. Clan Chat (ClanChatManager.cs + ClanChatPanel.cs)

- [ ] Open clan screen → chat messages load (or empty state shown)
- [ ] Type message and tap Send → message appears in chat
- [ ] Second device (or emulator): send message → appears on first device in real-time
- [ ] Long-press own message → Delete option → message disappears
- [ ] Long-press other's message → Mute option → member muted for 60min
- [ ] Unread badge shows count on clan tab when panel is closed
- [ ] Open panel → badge count resets to 0

**Expected**: Real-time chat with send/delete/mute; unread badge accurate

---

## 12. Daily Challenges (ChallengeSystem.cs)

- [ ] Open challenges screen → exactly 3 challenges shown
- [ ] Same 3 challenges visible on a second device (same UTC day)
- [ ] Complete "Win 3 matches": progress counter increments per win
- [ ] Complete challenge → green checkmark + "Claim Reward" button
- [ ] Claim reward → XP and coins granted (check console log)
- [ ] Change device date to next UTC 09:00 → new set of 3 challenges appears
- [ ] Firestore: `players/{uid}/dailyChallenges/{YYYY-MM-DD}` document exists with progress

**Expected**: 3 daily challenges, same per day for all players, progress tracked, rewards claimable

---

## Final Build Checks

- [ ] No compiler errors in Unity
- [ ] Android build succeeds (GitHub Actions)
- [ ] APK installs on physical device
- [ ] No crashes in first 5 minutes of play
- [ ] Firebase Analytics events visible in DebugView
- [ ] Performance: stable 60 FPS in gameplay

---

## Known Deferred Items (NOT blocking Saturday)

- IAP real-money purchase test (needs production payment method)
- Visual verification of new asset style consistency
- Clan chat with 5+ concurrent members stress test
- Nakama latency test in real high-latency environment (VPN simulation)
