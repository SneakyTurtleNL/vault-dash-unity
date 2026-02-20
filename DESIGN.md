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

**Created**: 2026-02-20  
**Owner**: Bart (game designer) + Claude (implementation)  
**Status**: In Progress  
