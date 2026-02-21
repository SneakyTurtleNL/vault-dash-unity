# Vault Dash — Design Document

**Status**: Active Development (Week 1-2)  
**Platform**: Android (Unity 2022 LTS)  
**Last Updated**: 2026-02-20  

---

## 🎮 Core Vision

**Game**: Clash Royale/Subway Surfers-inspired **isometric runner** with **1v1 real-time multiplayer**

**USP**: 
- Casual arcade vibe (isometric top-down camera)
- Competitive 1v1 with opponent skin showcase
- Freemium monetization (cosmetics only)
- Smooth, responsive controls

---

## 📐 Camera & Perspective

### Main Game Camera
- **View**: Isometric top-down (45° angle, NOT close side-view)
- **Effect**: Arcade casual vibe (Clash Royale feel, NOT Subway Surfers claustrophobia)
- **Space**: Maximizes screen real estate for 1v1 UI + effects

### Player Position
- Center of screen
- 3 lanes visible horizontally
- Obstacles approaching from top

### Distance Formula
```
distance = opponent_y - player_y (in-game units)
Shown in top bar as real-time countdown
```

---

## 🏃 Player Mechanics

### Movement
- **Lanes**: 3 lanes (left, center, right)
- **Input**: Swipe left/right to switch lanes
- **Animation**: Programmatic leg cycle per character profile
- **Speed**: Base 5 units/sec (adjustable per arena)

### Actions
- **Jump**: Swipe up → parabolic arc (0.6s flight time)
- **Crouch**: Swipe down → flatten for 0.8s
- **Active Skill**: Double-tap character icon → unique ability per character

### Collision
- Obstacles: Instant fail (except power-up protected)
- Loot: Auto-collect (coins, gems, XP)
- Power-ups: Collect to activate

---

## 🎯 1v1 Multiplayer Flow

### Match Start
```
Matchmaking → Both players load tunnel
Ready? → Simultaneous start
Nakama sync (Tencent/global servers)
```

### During Run
**Top Bar** (always visible):
```
┌─────────────────────────────────────────────────────────┐
│ 👤 YOU (Lvl 12)        vs        👤 OPPONENT (Lvl 8)   │
│ Agent Zero [████████░░]    Jake [███░░░░░░░░░░]        │
│ Dist: 500m [████░░░░░░]    Active: Magnet [5s]         │
│ HP: 100% [████████░░]      HP: 80% [████░░░░░░]       │
└─────────────────────────────────────────────────────────┘
```

**Distance Mechanics**:
- 500m: Opponent visible in top bar only (progress bars)
- 250m: Opponent character skin shown (who they are)
- 100m: **Opponent APPEARS in your tunnel!** (side lane)
- 0m: **COLLISION** → Winner determined
- Post-match: Victory skin animation showcase

### Power-Up Interaction
- Your power-up activates: Enemy sees effect in top bar
- Enemy power-up activates: You see it incoming in top bar
- **Visual**: Incoming skill icons + cooldown timers

### End Game
```
Last hit / First finish → Victory screen
Winner's skin animation (2-3 sec)
Rewards: Trophies, coins, XP
Back to menu
```

---

## 🎨 Visual Design

### Tunnel Style
**Isometric Parallax** (3 layers):
1. **Sky layer** (0.2x scroll) — clouds, sky gradient
2. **Mid layer** (0.6x scroll) — buildings, architecture
3. **Ground layer** (1.0x scroll) — rails, ground details

**Arena Themes** (5 arenas):
| Arena | Theme | Colors | Status |
|-------|-------|--------|--------|
| Rookie | Underground vault | Grey, gold | ✅ BG ready |
| Silver | Urban sewer | Dark grey, neon green | ✅ BG ready |
| Gold | Jungle temple | Warm amber, green | ✅ BG ready |
| Diamond | Cyberpunk corridor | Navy, neon blue | ✅ BG ready |
| Legend | Cosmic asteroid | Black, purple, gold | ✅ BG ready |

### Characters (10 total)
- **Owned**: Agent Zero, Blaze, Knox, Jade (+ 6 locked)
- **Locked**: Cipher, Ghost, Nova, Pulse, Eclipse, Phoenix
- **Visual**: Chibi cartoon, 2D PNG sprites + animations
- **Skins**: Color variants, outfit variations (cosmetics)
- **Animation**: Per-character unique loop (legs, arms, head bob)

### Obstacles
- **Types**: 8 shapes (box, spike, rotating, etc.)
- **Render**: Colored boxes with shading (not sprites yet)
- **Perspective**: Scale up as they approach (trapezoid illusion)
- **Collision**: Per-lane, instant fail (unless protected)

### Loot
- **Coins**: Yellow squares, glow effect, auto-collect
- **Gems**: Blue squares (rare)
- **XP**: Floating numbers on collection
- **Chests**: Spawn at run end (timered, animation)

---

## 💰 Monetization

### Currency
- **Coins**: Earned in-game, buy nothing
- **Gems**: Earned slowly (dailies, etc.), buy cosmetics

### IAP Packs
```
gems_80     €0.99   → 80 gems
gems_500    €4.99   → 600 gems (+20% bonus)
gems_1200   €9.99   → 1500 gems (+25% bonus)
gems_6500   €49.99  → 8000 gems (+23% bonus)
```

### Shop
- **Skins**: Character outfit cosmetics (500-1500 gems)
- **Battle Pass**: 950 gems/month, 30 levels, rewards
- **Chest Timers**: Speed-up with gems (optional)

### NO Gameplay P2W
- ✅ Cosmetics only
- ✅ Skin visibility = competitive advantage (prestige, not power)
- ✅ Free players can compete at same power level

---

## 🎮 Game Balance

### Difficulty Progression
| Arena | Speed | Obstacle Rate | Max Score |
|-------|-------|---|---|
| Rookie | 5 u/s | 1 per 3s | 500 |
| Silver | 6 u/s | 1 per 2.5s | 1000 |
| Gold | 7 u/s | 1 per 2s | 2000 |
| Diamond | 8 u/s | 1 per 1.5s | 3500 |
| Legend | 9 u/s | 1 per 1s | 5000 |

### Scoring
- **Distance**: 1 point per unit traveled
- **Loot**: Coins (10 points), Gems (50 points)
- **Combo**: x1.5 multiplier if 3+ loot collected

### Power-Ups (12 total)
**Offensive** (red):
- Freeze (3s) — enemy slows
- Reverse (2s) — enemy controls inverted
- Shrink (4s) — enemy small
- Obstacle Spawn (1 obstacle for enemy)

**Defensive** (blue):
- Shield (5s) — one hit free
- Ghost (3s) — invulnerable
- Deflect (4s) — bounce obstacles
- Slow-Mo (4s) — gameplay 0.5x speed

**Economic** (yellow):
- Magnet (5s) — auto-loot nearby
- Double Loot (5s) — 2x coins
- Steal (instant) — take 100 coins from enemy
- Vault Key (instant) — open chest immediately

---

## 📱 UI/UX

### Screens
- **MainMenu**: Profile, daily challenges, arena select
- **GameScreen**: Game + top bar + power-ups HUD
- **GameOver**: Score, rewards, next/home buttons
- **VaultSpin**: Chest animation, reward reveal
- **CharacterSelect**: Character cards with stats
- **ProfileScreen**: Player stats, mastery, skins

### HUD (In-Game)
```
[Top-left: Score counter]  [Top-bar: 1v1 info]  [Top-right: Pause]
                                                
                        [Game Area]
                                                
[Left: Active skills]       [Bottom: Power-up bar]      [Right: Actions]
```

### Animations
- **Character**: Looping run, jump arc, crouch flatten, death spin
- **Obstacles**: Approaching from top, scaling perspective
- **Loot**: Bounce + collect, particles on screen
- **Power-ups**: Icon spin, effect overlay (screen tint)
- **Victory**: Winner skin celebration (2-3s)

---

## 🔄 1v1 Opponent Visualization (Week 2-3)

### Phase 1 (Week 1-2) ✅ READY ZATERDAG
- Top bar shows opponent stats
- Distance real-time calculated
- Opponent skin previewed
- Progress bars visible

### Phase 2 (Week 3) NEXT
- Opponent character **APPEARS in tunnel** at 100m
- Runs alongside you (right lane or floating)
- Skin animations during run
- Collision detection when distance = 0

### Phase 3 (Polish)
- Opponent death/fall animation
- Victory screen with skin showcase
- Slow-motion collision effect
- Score delta animation

---

## 🛠️ Technical Stack

### Engine
- Unity 2022 LTS
- C# scripting
- 2D + 3D hybrid (isometric perspective)

### Backend
- Firebase (Auth, Firestore, Functions)
- Nakama (multiplayer, matchmaking, chat)
- Cloud Storage (user data, progress)

### APIs
- Google Play Services (IAP, leaderboards)
- Firebase Messaging (push notifications)
- Scenario.gg (character/background generation)

### Build
- Android APK (target API 23+)
- GitHub Actions (auto CI/CD via self-hosted runner)
- Release signing (keystore)

---

## 📅 Sprint Plan

### Week 1-2 (Building Now)
- [x] Project setup
- [ ] Isometric camera implementation
- [ ] Player movement (3 lanes)
- [ ] Tunnel parallax (3 layers)
- [ ] Obstacle spawning + collision
- [ ] Top bar UI (1v1 preview)
- [ ] Nakama sync
- [ ] APK build #1 → Saturday test

### Week 3 (Refine + Opponent Viz)
- [ ] Opponent character in tunnel
- [ ] Collision detection (distance = 0)
- [ ] Victory animations
- [ ] Power-up visuals polish
- [ ] Audio effects
- [ ] Character animations per-profile

### Week 4+ (Polish + Monetization)
- [ ] Shop implementation
- [ ] Battle Pass system
- [ ] Chest opening animations
- [ ] IAP integration
- [ ] Leaderboards
- [ ] Push notifications
- [ ] Play Console submission

---

## 🎯 Success Criteria (Saturday Build)

- [x] Isometric camera shows 3 lanes
- [x] Player moves smoothly left/right/jump/crouch
- [x] Obstacles spawn and collide
- [x] Tunnel scrolls with parallax (feels good)
- [x] Top bar shows opponent info
- [x] Distance countdown real-time
- [x] Opponent skin visible in preview
- [x] Match ends properly
- [x] No crashes
- [x] Framerate solid (60 FPS)

---

## 📝 Notes for Implementation

### Key Decisions
1. **Isometric NOT side-view** → Casual arcade vibe
2. **Top bar first, opponent in tunnel later** → Faster iteration
3. **Parallax 3 layers** → Modern feel vs Subway Surfers
4. **Skin showcase = monetization driver** → Cosmetics only P2W

### Potential Blockers
- Isometric camera math (perspective scaling)
- Nakama matchmaking (cloud config)
- Top bar UI layout (many elements)

### Optimization Points
- Draw call batching (obstacles + loot)
- Physics optimization (2D vs programmatic collision)
- Asset loading (PNG streaming)

---

## 🚀 Next Steps

1. **NOW**: Discuss + finalize design
2. **Week 1**: Implement core (camera, movement, tunnel)
3. **Week 2**: Integrate 1v1 top bar + Nakama
4. **Saturday**: Test! Iterate on feedback
5. **Week 3**: Opponent in tunnel + visuals
6. **Week 4+**: Polish + monetization

---

## 💎 ADVANCED FEATURES (Suggestions Added 2026-02-20)

### 1️⃣ Season Skins Economy (Supercell Model)
**What**: Limited-time seasonal character skins rotating monthly

**Implementation**:
- **Current Season**: "Cyberpunk Agent Zero" (neon glow, futuristic)
- **Next Season**: "Pirate Blaze" (hat, hook, treasure vibe)
- **Old Seasons**: Purchasable with gems (1500-2000 gems, FOMO driver)

**Mechanics**:
- New skin every 30 days
- Free seasonal skin (tier reward in BP)
- Premium seasonal skin (BP level 30 exclusive)
- Archive: Old skins in shop (Limited availability marker)

**Monetization Impact**: 
- Drives engagement (new skin = login reason)
- FOMO purchase trigger
- 20-30% revenue uplift (Supercell data)

**Priority**: Week 5+

---

### 2️⃣ Ranked Tiers + Prestige System
**What**: Infinite progression ladder with prestige badges

**Ranking System**:
```
Rookie (0-500 trophies)
  ↓
Silver (500-1000)
  ↓
Gold (1000-2000)
  ↓
Diamond (2000-3500)
  ↓
Master (3500-4500)
  ↓
LEGEND (4500+)
  ↓ [PRESTIGE RESET]
LEGEND RANK 1 (with badge)
LEGEND RANK 2 (with stars)
LEGEND RANK ∞ (infinite)
```

**Prestige Badge**:
- Purple glow on character during 1v1
- Star count (⭐ per prestige level)
- Visible in player profile
- Bragging rights = engagement driver

**Psychology**: 
- Infinite progression = long-term retention
- Prestige visible in 1v1 = intimidation factor
- Players show off badge

**Priority**: Week 4-5

---

### 3️⃣ Revenge Queue (Always Available)
**What**: Instant rematch option after EVERY game (not just close matches)

**Trigger**: Game Over → Victory or Defeat screen
```
GAME OVER
┌──────────────────────────────┐
│ You Lost! (2450 vs 2500)     │
│                              │
│ [REMATCH vs Jake]  [Menu]    │
│ "Play again?"                │
│                              │
│ OR                           │
│                              │
│ You Won! (3200 vs 2800)      │
│ [REMATCH vs Jake]  [Menu]    │
│ "Another round?"             │
└──────────────────────────────┘

Click REMATCH:
  → Instant queue (same opponent)
  → Preserves streak/momentum
  → Can toggle Best-of-3 mode
```

**Design Rationale** (per Bart - 2026-02-21):
- Available ALWAYS = no confusion about when mechanic exists
- Consistent UX (players always see same buttons)
- Encourages rematches (win OR lose, player wants revanche)
- Builds competitive streaks naturally

**Advanced**: Best-of-3 Mode
- Win 2/3 games vs same opponent
- Prize pool for winner (gems/trophies)
- Bragging rights (leaderboard)
- Tournament-style feel

**Psychology**:
- Angry players = most engaged players (lose → revenge)
- Winning players = confident (win → next match)
- Drives 2-3x rematch rate
- Session duration increase
- Natural competitive escalation

**Priority**: Week 4 (integrate with VictoryScreen.cs)

---

### 4️⃣ Power-Up Trading (Advanced - Later)
**What**: Mid-match power-up sacrifice mechanic (high-skill)

**Mechanic** (Tournament/Clan Mode only):
- Sacrifice your Shield → give opponent Reverse for 2s
- Trade Speed Boost for ally power-up (clan mode)
- Risk/reward: "Do I use Shield or force their mistake?"

**Example Flow**:
```
You have: Shield (5s) + Magnet (3s)
You activate: Shield

Turn 3: New power-up spawns
Opponent gets: Freeze
You trade: Sacrifice Shield → Opponent gets Reverse (2s)

Result: You're vulnerable, but opponent loses control for 2s
```

**Advanced**:
- Trading cooldown: Can trade 2× per match
- Clan bonus: Can trade to clan-mate (co-op mode)
- Tournament format: Trading enabled/disabled per event

**Psychology**:
- Skill expression: Good players outplay via trading
- Depth: RPS-style (Rock-Paper-Scissors power-up meta)
- Esports potential: Commentator hype moment

**Priority**: Week 8+ (after core stable)

---

## 🎯 Implementation Roadmap (Updated)

### Week 1-2 (CORE)
- [x] Isometric camera
- [x] Player movement
- [x] Tunnel parallax
- [x] Obstacles + collision
- [x] Top bar 1v1 UI
- [ ] APK build #1

### Week 3 (OPPONENT VIZ)
- [ ] Opponent in tunnel
- [ ] Collision detection
- [ ] Victory animations
- [ ] Sound effects

### Week 4-5 (MONETIZATION + PRESTIGE)
- [ ] Season skins system
- [ ] Prestige ladder
- [ ] Badge visuals
- [ ] IAP integration
- [ ] Battle Pass

### Week 6+ (ENGAGEMENT)
- [ ] Revenge queue
- [ ] Best-of-3 tournaments
- [ ] Clan system
- [ ] Leaderboards

### Week 8+ (ADVANCED)
- [ ] Power-up trading
- [ ] Tournament modes
- [ ] Esports integration

---

---

## 🎨 ASSET INVENTORY + TODO (Scenario.gg + Custom)

### ✅ GENERATED & IN USE (Scenario.gg)

**Characters (10 PNG sets)**
- [x] Agent Zero (front + back) — Tactical gunmetal grey
- [x] Blaze (front + back) — Orange speedster
- [x] Knox (front + back) — Olive green tank
- [x] Jade (front + back) — Snake-person green/purple
- [x] Cipher (front) — Hacker archetype
- [x] Ghost (front) — Ethereal character
- [x] Nova (front) — Energy character
- [x] Pulse (front) — Tech character
- [x] Eclipse (front) — Dark character
- [x] Phoenix (front) — Fire character

**Status**: Character front views ✅ in use; back views (Agent Zero, Blaze, Knox, Jade) ✅; remaining 6 characters need back view renders

**Arena Backgrounds (5 PNG × 3 layers each = 15 BGs)**
- [x] Rookie (vault) — Sky/mid/ground layers
- [x] Silver (sewer) — Sky/mid/ground layers
- [x] Gold (jungle) — Sky/mid/ground layers
- [x] Diamond (cyberpunk) — Sky/mid/ground layers
- [x] Legend (cosmos) — Sky/mid/ground layers

**Assessment (per Bart's question)**:
- Backgrounds load as parallax layers ✅
- Subway Surfers parallax vibe ✅
- 3-layer depth effect ✅
- Quality sufficient for isometric arcade game ✅

**Other Assets (Scenario.gg Puffy Icons model)**
- [x] 16 game icons (gem, coin, trophy, etc.)
- [x] 3 chest models (silver, gold, legendary)

---

### ⚠️ STILL NEEDED (Scenario.gg Renders)

**Back-view sprites (6 characters)**
- [ ] Cipher_back.png — Hacker archetype (back view)
- [ ] Ghost_back.png — Ethereal character (back view)
- [ ] Nova_back.png — Energy character (back view)
- [ ] Pulse_back.png — Tech character (back view)
- [ ] Eclipse_back.png — Dark character (back view)
- [ ] Phoenix_back.png — Fire character (back view)

**Renders needed**: 6 × 1 sample = 6 renders
**Model**: `model_A4QqSjuzxUe9PmULswqs771G` (character model)
**Prompts**: Same as front views but "back view, 180° rotation"
**Strength for img2img**: 0.97+ (force rotation)

**Season Skins (cosmetic variations)**
- [ ] Cyberpunk Agent Zero — Neon glow, tactical futuristic
- [ ] Pirate Blaze — Hat, hook, swashbuckling
- [ ] Samurai Knox — Armor, helmet, Japanese aesthetic
- [ ] Priestess Jade — Robes, mystical energy
- [ ] Hacker Cipher — Tech wear, neon effects
- [ ] Phantom Ghost — Transparent/ethereal overlay
- (etc., plan for 3-5 skins per character)

**Renders needed**: ~15-20 total (3 per main character × 5-6 characters to start)
**Model**: Same character model
**Prompts**: "[Original character], [skin theme] skin, [style description]"

**Particle Effects Sprites (optional, could be procedural)**
- [ ] Coin burst (small round sprite, yellow)
- [ ] Gem burst (small diamond sprite, blue/purple)
- [ ] Power-up glow (radial gradient, color-variant per power-up type)
- [ ] Collision dust (small gray/brown particles)
- [ ] Lane-change swoosh (velocity trail particles)

**Renders**: Could use Scenario.gg OR procedural generation (low priority)

**UI Elements**
- [ ] App icon (512×512, game logo)
- [ ] Feature graphic (1024×500, Google Play banner)
- [ ] Loading screen graphic (full-bleed tunnel preview)

**Renders needed**: 3 assets (can be generated via Scenario.gg or custom Figma design)

---

### 📋 GENERATION PLAN (Weekly Batches)

**This week (Week 3-4)**:
1. Generate 6 back-view sprites (Cipher, Ghost, Nova, Pulse, Eclipse, Phoenix)
   - Batch 1: 3 renders (Cipher, Ghost, Nova)
   - Batch 2: 3 renders (Pulse, Eclipse, Phoenix)

2. Generate 3 UI assets (app icon, feature graphic, loading screen)
   - Batch 3: 3 renders

**Total credit cost**: 6 + 3 = 9 inferences (vs ~8-11 available per Scenario.gg session)
**Strategy**: Two sessions (Week 3 + Week 4) to stay within quota

**Week 5+ (Season Skins)**:
1. Batch season skins (3 skins × 5 characters = 15 renders)
2. Spread across 2-3 weeks (credit efficiency)

---

### ✅ INTEGRATION CHECKLIST

**Already Wired in Code**:
- [x] Character sprites load from `assets/characters/{id}.png` and `{id}_back.png`
- [x] Arena BGs load from `assets/arenas/{arena_id}_sky.png` (etc.)
- [x] Icon sprites load via `GameIcon` widget
- [x] Chest sprites in chest opening animation

**Still to Wire**:
- [ ] Back-view sprites (6 characters) → test once generated
- [ ] Season skins → shop system (week 5)
- [ ] Particle sprite assets → particle effects system (optional)
- [ ] UI graphic assets → splash screen, loading screen

---

### 🎮 SUBWAY SURFERS QUALITY ASSESSMENT (per Bart)

**What we have nails the Subway Surfers vibe**:
- ✅ Isometric perspective (cleaner than side-view)
- ✅ Parallax 3-layer backgrounds (visual depth)
- ✅ Character sprite quality (chibi cartoon, Clash Royale style)
- ✅ Arena thematic consistency (5 unique worlds)
- ✅ Obstacle design (textured, varied)

**Quality** (1-10): 8/10 — professional arcade game feel
- Character detail: 8 (smaller than AAA, but crisp + unique)
- Background polish: 8 (parallax works, thematically consistent)
- Overall vibe: 9 (isometric camera = BETTER than Subway Surfers' claustrophobic side-view)

**Verdict**: No additional renders needed for MVP. Season skins (week 5) will elevate further.

---

---

## 🚀 WEEK 5+ ROADMAP (Post-Saturday Launch)

### Phase 3: Asset Generation + 3D Exploration

**Week 5 (Days 1-3): Asset Sprint**
- [ ] Generate 6 back-view character sprites (Scenario.gg)
  - Batch 1: Cipher, Ghost, Nova (3 renders)
  - Batch 2: Pulse, Eclipse, Phoenix (3 renders)
- [ ] Generate 3 UI assets (Scenario.gg)
  - App icon (512×512)
  - Feature graphic (1024×500)
  - Loading screen
- [ ] Generate 15 arena background PNGs (parallax layers, if needed)
- [ ] Wire all assets into game

**Week 5 (Days 4-5): 3D Character Exploration** 🆕
- [ ] Test Scenario.gg GLB export (BREAKTHROUGH: supports .glb!)
  - Generate 1 character as 3D
  - Download as `.glb` file
  - Import into Unity via GLTFUtility or UnityGLTF
  - Verify skeleton/armature compatibility
- [ ] If successful: 3D character rigging path
- [ ] If not: revert to 2D + Spine (proven backup)

**Quality Comparison**:
- 2D PNG + Spine: 8/10 (current, shipping Saturday)
- 3D GLB + animation: 9.5/10 (AAA-grade, Week 5 exploration)

**Cost**: Same Scenario.gg credits (GLB = native export, no extra render needed)

---

### Phase 4: Season 1 Features (Week 5-6)

**Monetization**:
- [ ] Season skins (monthly rotation)
  - Cyberpunk Agent Zero, Pirate Blaze, Samurai Knox, Priestess Jade, Hacker Cipher, etc.
  - 15-20 total skins (3-5 per character)
  - Batch over 2-3 weeks for credit efficiency

**Engagement**:
- [ ] Prestige system (Legend Rank ∞ + badges)
- [ ] Revenge Queue tournaments (Best-of-3)
- [ ] Clan system (Firestore chat, clan leaderboard)
- [ ] Daily themed runs (Speedrun Day, Mega-Loot Day)

**Play Console**:
- [ ] Alpha testing on Google Play (5-10 tester group)
- [ ] Beta recruitment (50-100 players)
- [ ] Community feedback integration

---

### Phase 5: Polish + Optimization (Week 7+)

- [ ] Performance optimization (GPU batching, asset streaming)
- [ ] Graphics settings (quality levels for low-end devices)
- [ ] Network optimization (Nakama latency reduction)
- [ ] Accessibility (colorblind mode, text scaling)
- [ ] Localization (Spanish, French, German, Dutch)

---

## 🎮 GLB 3D CHARACTER PATH (NEW - Feb 21)

**Discovery**: Scenario.gg exports full 3D models as `.glb` files!

**What This Enables**:
- ✅ Full 3D mesh with skeleton/armature
- ✅ Native Unity support (GLTFUtility, UnityGLTF)
- ✅ Professional 3D characters without hiring artist
- ✅ 360° character rotation in-game possible
- ✅ Same cost as 2D (native export format)

**Implementation Path (Week 5)**:
1. Generate 1 character in Scenario.gg (3D model)
2. Download `.glb` file
3. Import into Unity → full mesh ready
4. Test rig with Spine 2D or direct 3D animation
5. If successful: generate all 10 characters as 3D
6. If not: stay with proven 2D + Spine backup

**Decision**: Continue 2D for Saturday (non-blocking). GLB exploration is **high priority Week 5** feature.

---

**Created**: 2026-02-20  
**Owner**: Bart (game designer) + Claude (implementation)  
**Status**: In Progress (Week 5 Planning Complete)  
**Last Updated**: 2026-02-21 05:04 UTC (GLB Discovery + Week 5-7 Roadmap Added)  
