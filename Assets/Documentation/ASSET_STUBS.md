# Asset Stubs — Post-Launch Replacement Guide

**Generated:** 2026-02-26  
**Status:** Placeholder PNGs in place, code wired and ready.

---

## What Was Done

All NICE-TO-HAVE assets from Plan C are now stubbed in code and on disk.
No new Scenario.gg generation was used (credits exhausted).

### Placeholder Assets Created

19 placeholder PNGs (valid RGB PNG files, solid color with crosshatch border):

| File | Size | Color | Category |
|------|------|-------|----------|
| `Icons/Prestige/prestige_1.png` | 128×128 | Purple (90, 20, 160) | Prestige |
| `Icons/Prestige/prestige_5.png` | 128×128 | Purple (120, 30, 200) | Prestige |
| `Icons/Prestige/prestige_10.png` | 128×128 | Purple (150, 50, 220) | Prestige |
| `Icons/Prestige/prestige_20.png` | 128×128 | Purple (180, 80, 240) | Prestige |
| `Icons/BattlePass/battle_pass_tier_1.png` | 128×128 | Gold (200, 160, 30) | Battle Pass |
| `Icons/BattlePass/battle_pass_tier_30.png` | 128×128 | Gold (220, 140, 20) | Battle Pass |
| `Icons/BattlePass/battle_pass_premium.png` | 128×128 | Gold (230, 180, 10) | Battle Pass |
| `Icons/Seasonal/season_rookie.png` | 128×128 | Green (100, 180, 100) | Seasonal |
| `Icons/Seasonal/season_silver.png` | 128×128 | Silver-blue (160, 170, 185) | Seasonal |
| `Icons/Seasonal/season_legend.png` | 128×128 | Gold (200, 140, 20) | Seasonal |
| `Icons/CardBg/card_common_bg.png` | 128×128 | Dark grey (55, 55, 55) | Card BG |
| `Icons/CardBg/card_rare_bg.png` | 128×128 | Blue (30, 60, 130) | Card BG |
| `Icons/CardBg/card_epic_bg.png` | 128×128 | Purple (80, 20, 130) | Card BG |
| `Icons/CardBg/card_legendary_bg.png` | 128×128 | Dark orange (140, 40, 10) | Card BG |
| `Splash/splash_art_main.png` | 256×256 | Dark navy (20, 20, 40) | Splash |
| `Splash/loading_screen_bg.png` | 256×256 | Darker navy (10, 10, 30) | Splash |
| `Particles/LootBurst/coin_burst_large.png` | 128×128 | Gold (220, 185, 30) | Loot Burst |
| `Particles/LootBurst/gem_burst_large.png` | 128×128 | Purple (80, 40, 200) | Loot Burst |
| `Particles/LootBurst/chest_burst.png` | 128×128 | Brown (140, 90, 40) | Loot Burst |

All have matching `.meta` files with deterministic GUIDs.

---

## Code Wiring Done

### `GameIconSystem.cs`
- Added 19 icon keys to `IconPaths` dictionary.
- Added helper methods:
  - `PrestigeBadgeKey(int prestigeLevel)` — maps prestige level → nearest badge art
  - `ApplyPrestigeBadge(Image, int)` — applies badge icon to UI Image
  - `SeasonIconKey(string tierName)` — maps tier name → seasonal icon
  - `BattlePassTierKey(int tier)` — maps tier number → battle pass icon key
  - `CardRarityBgKey(CardRarity)` — maps rarity → card background icon key

### `PrestigeBadge.cs`
- Added `prestigeBadgeIcon` Image field (Inspector, optional).
- `Refresh()` now calls `GameIconSystem.ApplyPrestigeBadge()` automatically.

### `CardUI.cs`
- Added `rarityBackgroundImage` Image field (Inspector, optional).
- `ApplyRarityFrame()` now loads and applies `card_{rarity}_bg` sprite.
- Falls back to color tinting if `rarityBackgroundImage` not assigned.

### `SeasonManager.cs`
- Added `GetCurrentSeasonIconKey()` — returns icon key based on player's current tier.
- Added `ApplyCurrentSeasonIcon(Image)` — one-call helper for UI components.
- Added `GetSeasonIconKeyForTier(string)` — static helper for archive screens.

### `ParticleEffects.cs`
- Added `coinBurstTexture`, `gemBurstTexture`, `chestBurstTexture` Texture2D fields.
- `Awake()` auto-loads from `Particles/LootBurst/` if not assigned in Inspector.
- Logged warnings if textures missing.

### New Files Created
- `Scripts/UI/BattlePassScreen.cs` — full stub UI controller with icon wiring.
  Uses `battle_pass_tier_1`, `battle_pass_tier_30`, `battle_pass_premium`.
- `Scripts/UI/LoadingScreen.cs` — splash art + async scene loading controller.
  Uses `splash_art_main`, `loading_screen_bg`.

---

## Post-Launch: How to Swap Real Art

When Scenario.gg credits are available:

1. **Generate** new assets with the same filenames.
2. **Copy** generated PNGs into the same `Assets/Resources/{path}` directories.
3. **Unity** will hot-swap textures on next build (GUIDs are deterministic — no reassignment needed in Inspector).
4. For splash/loading (full-screen), increase source image size to 1920×1080 or 2560×1440.

### Prestige Badges: Extend Range
Currently only 4 badges (1, 5, 10, 20). Add more:
```csharp
// In GameIconSystem.IconPaths:
{ "prestige_2",  "Icons/Prestige/prestige_2"  },
{ "prestige_3",  "Icons/Prestige/prestige_3"  },
// etc.

// Update PrestigeBadgeKey() thresholds accordingly.
```

### Battle Pass: Full 50-Tier Set
Generate `battle_pass_tier_{1..50}.png` and add to `IconPaths`:
```csharp
// Dynamic approach — add to GameIconSystem.Start():
for (int i = 1; i <= 50; i++)
    IconPaths[$"battle_pass_tier_{i}"] = $"Icons/BattlePass/battle_pass_tier_{i}";
```

### Seasonal: Per-Season Icons
Generate `season_neon.png`, `season_frost.png`, etc. Wire via:
```csharp
// In SeasonManager.GetCurrentSeasonIconKey():
return GameIconSystem.SeasonIconKey(CurrentSeason.theme);
// Requires extending GameIconSystem.SeasonIconKey() with season theme mapping.
```

---

## What Still Needs Real Art (Priority)

| Asset | Blocker | Notes |
|-------|---------|-------|
| `splash_art_main` | 🔴 Post-launch | Full game splash. High priority for store listing. |
| `loading_screen_bg` | 🟡 Post-launch | Loading screen. Can reuse splash art. |
| `prestige_{1-20}` | 🟡 Nice-to-have | Prestige system launches with colored placeholders. |
| `battle_pass_*` | 🟢 Pre-feature | Battle Pass feature not live. Stubs sufficient. |
| `season_*` | 🟢 Pre-feature | Seasonal rewards show tier icons for now. |
| `card_*_bg` | 🟡 Polish | Card tinting works. Textures are visual upgrade. |
| `coin/gem/chest_burst_large` | 🟢 Post-launch | Loot burst uses procedural particles as fallback. |
