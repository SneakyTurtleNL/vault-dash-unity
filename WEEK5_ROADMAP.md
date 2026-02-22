# WEEK 5-12 ROADMAP
## Vault Dash Post-MVP Development

---

## 🎯 STRATEGIC VISION

**Saturday MVP**: Fully playable game with premium polish (ready for alpha testing)
**Week 5+**: Professional AAA-grade features (retention, monetization, community)

---

## WEEK 5: ADVANCED VFX & POLISH

### Priority 1: Particle Effects (2 days)
**Status**: Framework ready (ParticleExplosionEffect.cs)

**Tasks**:
- [ ] Create particle prefabs:
  - [ ] Coin burst (10-15 coins, radial spray)
  - [ ] Gem sparkle (small sparks, upward float)
  - [ ] Power-up activation (colored sphere expand + ring)
  - [ ] Critical hit cross (rotating X with screen shake)
- [ ] Wire to gameplay events:
  - [ ] Coin pickup → burst
  - [ ] Power-up use → activation effect
  - [ ] Critical hit → cross + shake
  - [ ] Game over → confetti rain
- [ ] Performance optimization:
  - [ ] Object pooling for particles
  - [ ] Disable on low-end devices

**Tools**: Built-in Particle System
**Effort**: 1-2 days
**Impact**: High (game feel, visual feedback)

---

### Priority 2: Screen Shake & Camera (1 day)
**Status**: ScreenShakeController.cs ready

**Tasks**:
- [ ] Integrate ScreenShakeController into GameManager
- [ ] Trigger on events:
  - [ ] Player collision (0.1s, 0.1 intensity)
  - [ ] Power-up use (0.15s, 0.15 intensity)
  - [ ] Defeat (0.5s, 0.3 intensity)
- [ ] Test intensity/duration balance

**Effort**: <1 day
**Impact**: High (immersion)

---

### Priority 3: UI Screen Transitions (1 day)
**Status**: UITransitionController.cs ready

**Tasks**:
- [ ] Register all screens with transition controller
- [ ] Implement transitions:
  - [ ] Main Menu ↔ Game (fade + slide)
  - [ ] Game ↔ Results (fade)
  - [ ] Menus (slide left/right)
- [ ] Test transition timing (0.3-0.5s optimal)

**Effort**: <1 day
**Impact**: Medium (UX polish)

---

## WEEK 6: AUDIO & MUSIC

### Priority 1: FMOD Integration (3 days)
**Status**: FMODMusicManager.cs framework ready

**Tasks**:
- [ ] Import FMOD Studio Integration package
- [ ] Create FMOD Studio project with events:
  - [ ] Menu music (60s loop)
  - [ ] Gameplay music (with Intensity parameter, 0-10)
  - [ ] Victory fanfare (5s climax)
  - [ ] Defeat theme (3s descending)
- [ ] Wire intensity ramp:
  - [ ] Start: 0 (calm)
  - [ ] Mid-game: 5 (normal pace)
  - [ ] Chase mode: 10 (adrenaline)
  - [ ] Victory/Defeat: special track
- [ ] Test on device (latency, performance)

**Tools**: FMOD Studio ($)
**Effort**: 2-3 days
**Impact**: Very High (immersion, retention)

---

### Priority 2: SFX Expansion (2 days)
**Status**: SFXManager.cs ready (basic framework)

**Tasks**:
- [ ] Create/source 20+ SFX clips:
  - UI: click, pop, upgrade, notification (4×)
  - Gameplay: jump, land, power-up use, collision, victory, defeat (6×)
  - Powerups: freeze, shield, magnet, slow-mo sounds (4×)
  - Environmental: chest open, coin pickup, gem shine (3×)
- [ ] Categorize by priority:
  - Must-have: UI clicks, victory/defeat, power-ups
  - Nice-to-have: landing, environment
- [ ] Wire to events

**Effort**: 2 days
**Impact**: High (game feel)

---

## WEEK 7: COSMETICS & MONETIZATION

### Priority 1: Prestige Cosmetics System (3 days)
**Status**: PrestigeCosmetics.cs ready

**Tasks**:
- [ ] Create prestige skin unlock tiers:
  - [ ] P1: First prestige (bronze glow)
  - [ ] P5: Badge variant (silver glow)
  - [ ] P10: Unique skin per character (gold glow)
  - [ ] P20: Exclusive cosmetics (legendary glow)
  - [ ] P50: Ultra-rare cosmetics (rainbow glow)
- [ ] Design 5 prestige skins minimum
- [ ] Generate via Scenario.gg:
  - [ ] Input prompt: "Character name in prestige skin at P{level}"
  - [ ] Output: 512×512 PNG
- [ ] Wire unlock logic:
  - [ ] Player prestige increases → check unlocked cosmetics
  - [ ] UI shows available skins → apply on-click
  - [ ] Persist selection to Firestore

**Effort**: 3 days
**Impact**: Very High (retention, cosmetic revenue)

---

### Priority 2: Trail & Effect Cosmetics (2 days)
**Status**: Framework ready

**Tasks**:
- [ ] Create 3 trail particle effects:
  - [ ] Fire trail (red/orange particles)
  - [ ] Ice trail (blue/white particles)
  - [ ] Electric trail (yellow/cyan particles)
- [ ] Create 3 character effects:
  - [ ] Aura (glowing outline)
  - [ ] Shadow (dark silhouette)
  - [ ] Glow (bright halo)
- [ ] Apply at prestige unlock
- [ ] Test performance (particle count)

**Effort**: 2 days
**Impact**: High (polish, cosmetic revenue)

---

## WEEK 8: 3D CHARACTER UPGRADE (OPTIONAL)

### Priority 1: GLB 3D Import Pipeline (2 days)
**Status**: GLBCharacterImporter.cs ready

**Tasks**:
- [ ] Test GLB export from Scenario.gg (Knox 1024×1024)
- [ ] Import into Unity using UnityGLTF or GLTFUtility
- [ ] Apply ToonCelShaded shader to remove PBR glossiness
- [ ] Setup animator (if skeleton exists)
- [ ] Test 360° rotation in-game
- [ ] Performance benchmark (mobile frame rate impact)

**Effort**: 1-2 days
**Impact**: High (visual quality, AAA feel)

---

### Priority 2: Spine 2D Animation (Optional, +2 days)
**Status**: SpineCharacterController.cs framework exists

**Tasks**:
- [ ] Rig Knox GLB with Spine skeleton (external tool)
- [ ] Export Spine animation files
- [ ] Import into Unity via Spine Runtime
- [ ] Test animations: idle, run, jump, celebrate
- [ ] Performance test (skeletal animation cost)

**Effort**: 2+ days (external rigging)
**Impact**: Medium (visual polish)

---

## WEEK 9-10: MONETIZATION & RETENTION

### Priority 1: Season 1 Features (4 days)
**Status**: SeasonManager.cs + Cloud Functions ready

**Tasks**:
- [ ] Season 1 cosmetics (theme-specific skins):
  - [ ] "Vault Protector" skin per character (5×)
  - [ ] Generate via Scenario.gg
  - [ ] Unlock via season battle pass
- [ ] Season rewards (tier ladder):
  - [ ] 30 levels
  - [ ] Gems + coins + cosmetics per tier
  - [ ] Final reward: Exclusive cosmetic
- [ ] Season leaderboard
  - [ ] Top 100 players visible
  - [ ] End-of-season crown cosmetic
  - [ ] Season archive (historical leaderboards)

**Effort**: 3-4 days
**Impact**: Very High (engagement, monetization)

---

### Priority 2: Battle Pass Enhancement (3 days)
**Status**: ShopScreen ready

**Tasks**:
- [ ] Premium tier (950 gems/month):
  - [ ] 800 gems refund (value-add)
  - [ ] 10 bonus levels (cosmetic unlocks)
  - [ ] Exclusive rewards per tier
- [ ] Track progress visually (level/XP bar)
- [ ] End-of-season countdown timer
- [ ] Auto-refund premium gems on level-up

**Effort**: 2-3 days
**Impact**: High (revenue, retention)

---

## WEEK 11-12: LIVE OPS & ALPHA PREP

### Priority 1: Event System (3 days)
**Status**: ClanEventManager.cs ready

**Tasks**:
- [ ] Weekly challenges (5× unique weekly tasks):
  - [ ] "Destroy 100 obstacles"
  - [ ] "Win 5 matches"
  - [ ] "Reach prestige"
  - [ ] "Collect 1000 gems"
  - [ ] "Play 10 games"
- [ ] Daily bonuses (login streak rewards)
- [ ] Seasonal themes (cosmetics, quests per season)
- [ ] Firestore event scheduling

**Effort**: 2-3 days
**Impact**: High (engagement loops)

---

### Priority 2: Analytics & Monitoring (2 days)
**Status**: Firebase Analytics ready

**Tasks**:
- [ ] Setup Firebase Crashlytics on device
- [ ] Log key events:
  - [ ] Player journey (first_run, level_complete, prestige)
  - [ ] Monetization (purchase, gem_spent, cosmetic_unlock)
  - [ ] Engagement (daily_active, session_length, retention_d1)
- [ ] Create Firebase dashboard
- [ ] Monitor D1 retention (>25% target)
- [ ] Track ARPPU, session length

**Effort**: 1-2 days
**Impact**: High (data-driven decisions)

---

### Priority 3: Play Console Submission Prep (2 days)
**Status**: Metadata + assets ready

**Tasks**:
- [ ] Final app review:
  - [ ] Crash testing on 5+ devices
  - [ ] Network error handling
  - [ ] Offline mode validation
  - [ ] Permissions review (no unnecessary perms)
- [ ] Store listing:
  - [ ] Polish screenshots (5×)
  - [ ] Video trailer (15s gameplay)
  - [ ] Final description + keywords
- [ ] Submit to Play Console Alpha track

**Effort**: 1-2 days
**Impact**: Critical (beta testing)

---

## 🎯 TIMELINE SUMMARY

| Week | Focus | Status | ETA |
|------|-------|--------|-----|
| 4 (Done) | MVP Core | ✅ Complete | 2026-02-22 |
| 5 | VFX + Polish | 📅 Scheduled | 2026-03-01 |
| 6 | Audio + Music | 📅 Scheduled | 2026-03-08 |
| 7 | Cosmetics | 📅 Scheduled | 2026-03-15 |
| 8 | 3D Characters (Opt) | 📅 Optional | 2026-03-22 |
| 9-10 | Monetization | 📅 Scheduled | 2026-04-05 |
| 11-12 | Live Ops + Alpha | 📅 Scheduled | 2026-04-19 |

---

## 💡 RESOURCE REQUIREMENTS

| Resource | Week | Cost | Notes |
|----------|------|------|-------|
| Scenario.gg credits | 7, 8 | ~$50 | Cosmetics + 3D test |
| FMOD Studio | 6 | $0-400 | Indie license free/cheap |
| SSL certificate | Alpha | $0-100 | For multiplayer backend |
| VPS upgrade | Alpha | $10-20/mo | Handle load testing |

---

## 🚀 SUCCESS METRICS (Post-MVP)

| Metric | Week 4 Target | Week 12 Target |
|--------|---------------|----------------|
| D1 Retention | 25%+ | 35%+ |
| D7 Retention | 10%+ | 15%+ |
| ARPPU | $1-2 | $3-5 |
| Session Length | 30-60s | 2-5m (retention hook) |
| Crash Rate | <0.1% | <0.05% |

---

## 📝 NOTES

- **Prestige cosmetics drive retention** — unlock at key milestones
- **FMOD music intensity ramp** — dynamic audio matches gameplay pace
- **GLB 3D is optional** — beautiful but non-critical (2D sprites sufficient)
- **Alpha testing critical** — catch bugs pre-launch
- **Community feedback loop** — iterate fast based on tester input

---

**Good luck with Week 5+! 🚀 You've got a solid MVP foundation.**
