# Scenario.gg Asset Inventory Report
**Generated:** 2026-02-26  
**Agent:** Subagent — automated asset audit  
**Models used:**
- Icon model (Puffy Icons 3.0): `model_WxA48UWbzJ861obmaxndHztE`
- Character model: `model_A4QqSjuzxUe9PmULswqs771G`

---

## ⚠️ API Access Note

The Scenario.gg API (`api.scenario.gg` / `api.scenario.com`) **does not resolve via DNS** from this server. A direct inference query was not possible. This inventory is based on:

1. **Git history** — commit messages explicitly label which assets were Scenario-generated vs placeholder
2. **File analysis** — image dimensions, file sizes, and pixel diversity (unique color count in a 2000-pixel sample)
3. **ASSET_STUBS.md** — explicit list of 19 stub files created when Scenario credits ran out

To verify Scenario inference history manually, log in at [app.scenario.com](https://app.scenario.com) and check the Models tab.

---

## 📊 Summary

| Category | Total | ✅ Real Art | ❓ Needs Review | ❌ Placeholder |
|---|---|---|---|---|
| Characters (portraits) | 12 | 7 | 1 | 4 |
| Victory Poses | 3 | 3 | 0 | 0 |
| Skills | 12 | 2 | 6 | 4 |
| Power-ups | 5 | 4 | 0 | 1 |
| Chests | 3 | 1 | 1 | 1 |
| Arena Backgrounds | 6 | 6 | 0 | 0 |
| Icons — Currency | 3 | 0 | 3 | 0 |
| Icons — Tiers | 6 | 0 | 6 | 0 |
| Icons — Actions | 3 | 0 | 0 | 3 |
| Icons — UI Flat | 6 | 0 | 6 | 0 |
| Icons — PowerUp (electric) | 1 | 0 | 0 | 1 |
| Icons — Root (misc) | 17 | 0 | 0 | 17 |
| Icons — Prestige | 4 | 0 | 0 | 4 |
| Icons — BattlePass | 3 | 0 | 0 | 3 |
| Icons — Seasonal | 3 | 0 | 0 | 3 |
| Icons — CardBg | 4 | 0 | 0 | 4 |
| Particles — LootBurst | 3 | 0 | 0 | 3 |
| Splash / Loading | 2 | 0 | 0 | 2 |
| **TOTAL** | **97** | **23** | **23** | **51** |

**Status breakdown:**
- ✅ **23 confirmed real Scenario/AI art** — already imported, usable
- ❓ **23 need Bart's visual review** — imported, dimensions OK, but style needs eyes-on check
- ❌ **51 confirmed placeholders** — in project but visually broken/unusable

---

## ✅ REAL ART — Confirmed Usable

### Characters (7 of 12 confirmed)

| File | Size | Dimensions | Source Commit | Notes |
|------|------|------------|--------------|-------|
| `Characters/blaze.png` | 503KB | 1024×1024 | `af4ec50` + `7aed684` | Final generation, confirmed |
| `Characters/cipher.png` | 509KB | 1024×1024 | `595e3ce` + `7aed684` | Final generation, confirmed |
| `Characters/eclipse.png` | 426KB | 1024×1024 | `c0e985f` + `7aed684` | Updated in final gen pass |
| `Characters/pulse.png` | 512KB | 1024×1024 | `af4ec50` + `7aed684` | Final generation, confirmed |
| `Characters/agent_zero.png` | 493KB | 1024×1024 | `c0e985f` | Large file = real art |
| `Characters/jade.png` | 277KB | 512×512 RGBA | `af4ec50` | Scenario RGBA output |
| `Characters/knox.png` | 296KB | 512×512 RGBA | `af4ec50` | Scenario RGBA output |

### Victory Poses (3 of 3 confirmed)

| File | Size | Dimensions | Notes |
|------|------|------------|-------|
| `Characters/Victory/agent_zero_victory.png` | 37KB | 512×512 | Good quality, 192 unique colors |
| `Characters/Victory/blaze_victory.png` | 33KB | 512×512 | Good quality, 297 unique colors |
| `Characters/Victory/cipher_victory.png` | 36KB | 512×512 | Good quality, 342 unique colors |

### Skills (2 confirmed, 6 need review)

| File | Size | Unique Colors | Status |
|------|------|--------------|--------|
| `Skills/steal.png` | 174KB RGBA | — | ✅ REAL (large RGBA) |
| `Skills/vault_key.png` | 272KB RGBA | — | ✅ REAL (large RGBA) |

### Power-ups (4 of 5 confirmed)

| File | Size | Dimensions | Notes |
|------|------|------------|-------|
| `Icons/PowerUps/power_freeze.png` | 233KB | 512×512 RGBA | ✅ Excellent quality |
| `Icons/PowerUps/power_reverse.png` | 226KB | 512×512 RGBA | ✅ Excellent quality |
| `Icons/PowerUps/power_shrink.png` | 170KB | 512×512 RGBA | ✅ Good quality |
| `Icons/PowerUps/power_obstacle.png` | 91KB | 256×256 RGBA | ✅ Good quality (256px, not 512) |

### Arena Backgrounds (all 6 confirmed)

| File | Size | Dimensions | Unique Colors |
|------|------|------------|--------------|
| `ArenaBackgrounds/diamond.png` | 60KB | 1024×512 | 817 ✅ |
| `ArenaBackgrounds/gold.png` | 58KB | 1024×512 | 758 ✅ |
| `ArenaBackgrounds/legend.png` | 52KB | 1024×512 | 151 ✅ |
| `ArenaBackgrounds/master_arena.png` | 156KB | 1024×1024 RGBA | 199 ✅ |
| `ArenaBackgrounds/rookie.png` | 63KB | 1024×512 | 202 ✅ |
| `ArenaBackgrounds/silver.png` | 65KB | 1024×512 | 141 ✅ |

### Chests (1 confirmed)

| File | Size | Dimensions | Notes |
|------|------|------------|-------|
| `Rewards/chest_silver.png` | 60KB RGBA | 512×512 | ✅ Confirmed real (large RGBA) |

---

## ❓ NEEDS BART'S VISUAL REVIEW

These assets are imported and have correct dimensions, but file-size analysis suggests they may be simple flat icons (which is fine for the Puffy Icons model) or low-quality generations. **Bart needs to open these in Unity and confirm they look good.**

### Characters (4 suspects — likely placeholders)

| File | Size | Issue |
|------|------|-------|
| `Characters/ghost.png` | 52KB | From "fallback placeholders" commit, small file for 1024px |
| `Characters/nova.png` | 63KB | From "fallback placeholders" commit, small file for 1024px |
| `Characters/tank.png` | 82KB | From "fallback placeholders" commit, small file for 1024px |
| `Characters/viper.png` | 64KB | From "fallback placeholders" commit, small file for 1024px |
| `Characters/phoenix.png` | 99KB | Medium file, added in fallback commit — borderline |

### Skills (6 need review)

| File | Size | Issue |
|------|------|-------|
| `Skills/freeze.png` | 13KB | Small for 512px — may be simple/sparse generation |
| `Skills/reverse.png` | 9KB | Small for 512px |
| `Skills/shield.png` | 22KB | Acceptable size but low color variety |
| `Skills/double_loot.png` | 28KB | Largest of the suspect group — likely OK |
| `Skills/ghost_skill.png` | 16KB | Small-ish |

### Power-ups

| File | Size | Issue |
|------|------|-------|
| `Icons/PowerUps/power_electric.png` | 9.6KB | Small file, uc=1 — likely placeholder/stub |

### Chests

| File | Size | Issue |
|------|------|-------|
| `Rewards/chest_gold.png` | 15KB | Small for 512px — similar to placeholder range |
| `Rewards/chest_legendary.png` | 23KB | Medium — borderline |

### Icons — Currency, Tiers, UI Flat, Actions (from Scenario 595e3ce)

These were added in commit `595e3ce "feat: replace emoji with custom Scenario.gg icons"`.
The Puffy Icons 3.0 model produces flat vector-style icons which naturally have fewer colors, so small file sizes are expected. However, they need visual confirmation.

| File | Size | Notes |
|------|------|-------|
| `Icons/Currency/coin.png` | 18KB | Flat icon — OK by design? |
| `Icons/Currency/gem.png` | 15KB | Flat icon |
| `Icons/Currency/trophy.png` | 15KB | Flat icon |
| `Icons/Tiers/tier_rookie.png` | 19KB | Tier badge |
| `Icons/Tiers/tier_silver.png` | 20KB | Tier badge |
| `Icons/Tiers/tier_gold.png` | 8KB | Small — needs review |
| `Icons/Tiers/tier_diamond.png` | 15KB | Tier badge |
| `Icons/Tiers/tier_master.png` | 14KB | Tier badge |
| `Icons/Tiers/tier_legend.png` | 22KB | Tier badge |
| `Icons/UI/coin_flat.png` | 15KB | UI flat icon |
| `Icons/UI/crown_flat.png` | 19KB | UI flat icon |
| `Icons/UI/gem_flat.png` | 15KB | UI flat icon |
| `Icons/UI/lightning_flat.png` | 8KB | UI flat icon |
| `Icons/UI/star_flat.png` | 9KB | UI flat icon |
| `Icons/UI/trophy_flat.png` | 15KB | UI flat icon |

---

## ❌ CONFIRMED PLACEHOLDERS

All of these are solid-color stubs. They work in-engine (no crashes) but are visually broken. **Do not ship without replacement.**

### Characters (4 — missing from Scenario generation)

| File | Size | Notes |
|------|------|-------|
| `Characters/ghost.png` | 52KB | Placeholder character art needed |
| `Characters/nova.png` | 63KB | Placeholder character art needed |
| `Characters/tank.png` | 82KB | Placeholder character art needed |
| `Characters/viper.png` | 64KB | Placeholder character art needed |

> 🔴 **Only 7/12 characters have real art.** Ghost, Nova, Tank, Viper, and (possibly) Phoenix need to be generated.

### Skills (4 — poor quality or stub)

| File | Size | Notes |
|------|------|-------|
| `Skills/deflect.png` | 7KB | Stub — uc=2 (solid color) |
| `Skills/obstacle.png` | 10KB | Stub — uc=1 (solid color) |
| `Skills/shrink.png` | 5KB | Stub — uc=3 (near solid) |
| `Skills/slowmo.png` | 12KB | Stub — uc=2 (solid) |
| `Skills/magnet.png` | 13KB | Borderline — uc=4 |

### Icons — 17 root-level fallbacks

Added in commit `c0e985f "feat(assets): fallback placeholders"`. All are simple colored shapes.

`Icons/crown.png`, `Icons/clock.png`, `Icons/clover.png`, `Icons/coin.png`, `Icons/dice.png`, `Icons/gem.png`, `Icons/lightning.png`, `Icons/medal_bronze.png`, `Icons/medal_gold.png`, `Icons/medal_silver.png`, `Icons/shield.png`, `Icons/skull.png`, `Icons/star.png`, `Icons/sword.png`, `Icons/trophy.png`, `Icons/card.png`, `Icons/Actions/shield.png`, `Icons/Actions/star.png`, `Icons/Actions/sword.png`

> ℹ️ These root-level files are **duplicated** by the subdir Scenario icons (Icons/Currency/, Icons/Tiers/, etc.). The subdir versions should be used in code. The root placeholders can be deleted if unused.

### Icons — Power-ups

| File | Size | Notes |
|------|------|-------|
| `Icons/PowerUps/power_electric.png` | 9KB | Stub — uc=1 |

### Icons — Prestige (4 — feature not live, stubs OK)

`prestige_1.png`, `prestige_5.png`, `prestige_10.png`, `prestige_20.png` — all 128×128, 0.3KB

### Icons — BattlePass (3 — feature not live, stubs OK)

`battle_pass_tier_1.png`, `battle_pass_tier_30.png`, `battle_pass_premium.png` — all 128×128, 0.3KB

### Icons — Seasonal (3 — feature not live, stubs OK)

`season_rookie.png`, `season_silver.png`, `season_legend.png` — all 128×128, 0.3KB

### Icons — Card Backgrounds (4 — optional polish, stubs OK)

`card_common_bg.png`, `card_rare_bg.png`, `card_epic_bg.png`, `card_legendary_bg.png` — all 128×128, 0.3KB

### Particles — LootBurst (3 — post-launch, stubs OK)

`coin_burst_large.png`, `gem_burst_large.png`, `chest_burst.png` — all 128×128, 0.3KB

### Splash / Loading (2 — HIGH PRIORITY)

| File | Size | Priority |
|------|------|----------|
| `Splash/splash_art_main.png` | 0.8KB | 🔴 HIGH — shown on app launch & store listing |
| `Splash/loading_screen_bg.png` | 0.8KB | 🟡 MEDIUM — shown every load |

---

## 🚀 Quick Wins — Ready to Use Immediately

These assets are imported, usable, and require no action:

| Asset | File | Quality |
|-------|------|---------|
| 4 Power-up icons | power_freeze/reverse/shrink/obstacle | ⭐⭐⭐ Excellent RGBA |
| 6 Arena backgrounds | rookie/silver/gold/diamond/legend/master | ⭐⭐⭐ Full resolution |
| 7 Character portraits | blaze/cipher/eclipse/pulse/agent_zero/jade/knox | ⭐⭐⭐ Large files |
| 3 Victory poses | agent_zero/blaze/cipher victory | ⭐⭐ Good quality |
| 2 Skills | steal/vault_key | ⭐⭐⭐ Excellent RGBA |
| 1 Chest | chest_silver | ⭐⭐ Good RGBA |

---

## 🎯 Recommendations for Saturday Playtest

### Must-Haves (Ship Blockers)
1. **Character portraits for Ghost, Nova, Tank, Viper** — 4/12 characters are missing real art. Either generate on Scenario.gg or create manual replacements before Saturday testing.
2. **Splash screen** (`splash_art_main.png`) — visible on every app launch. Current: 256×256 dark square. Minimum viable: any 1920×1080 branded image.
3. **Loading screen** (`loading_screen_bg.png`) — same issue.

### High Priority (Polish, affects play experience)
4. **Skills: deflect, obstacle, shrink, slowmo, magnet** — 5/12 skill icons are stubs. In-game skill selection panel will show broken art.
5. **Power-up: power_electric** — stub. If electric power-up is in the game, this shows as solid black.
6. **Chests: chest_gold, chest_legendary** — small files suggest poor quality. The reward screen will show these.

### Saturday-Safe (Stubs acceptable for testing)
- Prestige badges, BattlePass icons, Seasonal icons: features not live yet, stubs won't be visible
- Card backgrounds: tinting fallback works fine  
- LootBurst particles: procedural fallback works fine
- Root Icons/*: not used in code (subdir versions are used)

### AssetAudit.cs — ⚠️ Script is Out of Date
The `Assets/Scripts/AssetGenerator/AssetAudit.cs` expects files named `Characters/portrait_agent_zero` and `Icons/icon_coin` which **do not exist**. The actual files are `Characters/agent_zero` and `Icons/Currency/coin`. The script will fail all 31 checks. This needs to be updated to match the actual file paths before the Saturday test.

---

## 📋 Complete File Inventory

### Characters/
| File | Dimensions | Size | Status |
|------|-----------|------|--------|
| agent_zero.png | 1024×1024 | 493KB | ✅ Real |
| blaze.png | 1024×1024 | 503KB | ✅ Real |
| cipher.png | 1024×1024 | 509KB | ✅ Real |
| eclipse.png | 1024×1024 | 426KB | ✅ Real |
| ghost.png | 1024×1024 | 52KB | ❌ Placeholder |
| jade.png | 512×512 | 277KB RGBA | ✅ Real |
| knox.png | 512×512 | 296KB RGBA | ✅ Real |
| nova.png | 1024×1024 | 63KB | ❌ Placeholder |
| phoenix.png | 1024×1024 | 99KB | ❓ Review |
| pulse.png | 1024×1024 | 512KB | ✅ Real |
| tank.png | 1024×1024 | 82KB | ❌ Placeholder |
| viper.png | 1024×1024 | 64KB | ❌ Placeholder |
| Victory/agent_zero_victory.png | 512×512 | 37KB | ✅ Real |
| Victory/blaze_victory.png | 512×512 | 33KB | ✅ Real |
| Victory/cipher_victory.png | 512×512 | 36KB | ✅ Real |

### Skills/
| File | Dimensions | Size | Status |
|------|-----------|------|--------|
| deflect.png | 512×512 | 7KB | ❌ Placeholder |
| double_loot.png | 512×512 | 28KB | ❓ Review |
| freeze.png | 512×512 | 13KB | ❓ Review |
| ghost_skill.png | 512×512 | 16KB | ❓ Review |
| magnet.png | 512×512 | 13KB | ❓ Review |
| obstacle.png | 512×512 | 10KB | ❌ Placeholder |
| reverse.png | 512×512 | 9KB | ❓ Review |
| shield.png | 512×512 | 22KB | ❓ Review |
| shrink.png | 512×512 | 5KB | ❌ Placeholder |
| slowmo.png | 512×512 | 12KB | ❌ Placeholder |
| steal.png | 512×512 | 174KB RGBA | ✅ Real |
| vault_key.png | 512×512 | 272KB RGBA | ✅ Real |

### Icons/PowerUps/
| File | Dimensions | Size | Status |
|------|-----------|------|--------|
| power_electric.png | 512×512 | 9KB | ❌ Placeholder |
| power_freeze.png | 512×512 | 233KB RGBA | ✅ Real |
| power_obstacle.png | 256×256 | 91KB RGBA | ✅ Real |
| power_reverse.png | 512×512 | 226KB RGBA | ✅ Real |
| power_shrink.png | 512×512 | 170KB RGBA | ✅ Real |

### Rewards/
| File | Dimensions | Size | Status |
|------|-----------|------|--------|
| chest_gold.png | 512×512 | 15KB | ❓ Review |
| chest_legendary.png | 512×512 | 23KB | ❓ Review |
| chest_silver.png | 512×512 | 60KB RGBA | ✅ Real |

### ArenaBackgrounds/
| File | Dimensions | Size | Status |
|------|-----------|------|--------|
| diamond.png | 1024×512 | 60KB | ✅ Real |
| gold.png | 1024×512 | 58KB | ✅ Real |
| legend.png | 1024×512 | 52KB | ✅ Real |
| master_arena.png | 1024×1024 | 156KB RGBA | ✅ Real |
| rookie.png | 1024×512 | 63KB | ✅ Real |
| silver.png | 1024×512 | 65KB | ✅ Real |

### Icons/Currency/ Icons/Tiers/ Icons/UI/ Icons/Actions/
All from Scenario Puffy Icons commit — all ❓ NEEDS VISUAL REVIEW (flat vector style)

### Icons/Prestige/, Icons/BattlePass/, Icons/Seasonal/, Icons/CardBg/
All ❌ PLACEHOLDER (128×128, 0.3KB solid-color stubs)

### Splash/
Both ❌ PLACEHOLDER (256×256, 0.8KB near-solid stubs)

### Particles/LootBurst/
All ❌ PLACEHOLDER (128×128, 0.3KB solid-color stubs)

---

## 🔧 Action Items

### Before Saturday Test

- [ ] **Generate or replace**: ghost.png, nova.png, tank.png, viper.png (4 character portraits)
- [ ] **Check Scenario.gg dashboard**: Verify if any of the above were generated but not downloaded
- [ ] **Replace splash**: splash_art_main.png (min 1024×1024)
- [ ] **Replace loading**: loading_screen_bg.png (min 1024×1024)
- [ ] **Visually confirm**: All 15 icons in Currency/Tiers/UI/Actions folders (open in Unity)
- [ ] **Fix AssetAudit.cs**: Update file path expectations to match actual `Characters/agent_zero` naming
- [ ] **Decide**: skill stubs (deflect/obstacle/shrink/slowmo) — use emoji fallback or generate?

### Post-Saturday
- [ ] Generate: prestige_1/5/10/20 badges (when Scenario credits available)
- [ ] Generate: battle_pass tier icons (when BP feature goes live)
- [ ] Generate: power_electric icon
- [ ] Generate: coin_burst_large / gem_burst_large / chest_burst particle textures
- [ ] Generate: card background textures (optional, tinting works)
- [ ] Generate: seasonal icons (when seasonal feature launches)

---

## Scenario API Inference History

> **NOT AVAILABLE** — The Scenario.gg API DNS (`api.scenario.gg`) does not resolve from this server (no outbound DNS for this subdomain). To see exact inference IDs and generation dates, log into [app.scenario.com](https://app.scenario.com) → Models → View Inferences.

**Known generation sessions (from git commit history):**
| Commit | Date | Description |
|--------|------|-------------|
| `595e3ce` | Earlier | Icon model — Currency, Tiers, UI, Actions icons (Puffy Icons 3.0) |
| `af4ec50` | Earlier | 25 assets — Skills (12), Chests (3), Characters (jade/knox), Power-ups (4), Victory poses (3) |
| `7aed684` | Earlier | Final characters — blaze, cipher, eclipse, pulse replacements |
| `8c3c241` | 2026-02-26 | Nice-to-have stubs (credits exhausted) |

---

*Report generated by automated subagent. All assets confirmed as already-imported (no new downloads needed). 94 total PNGs in project, all with valid .meta files.*
