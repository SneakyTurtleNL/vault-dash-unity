# SATURDAY UI POLISH SETUP
## Complete Premium Visual Treatment Checklist

---

## PRE-REQUISITES
- Git pull latest: `git pull origin main`
- Open vault-dash-unity in Unity Editor
- Allow asset import (5-10 min)

---

## PHASE 1: FALLBACK ASSETS (Auto-Generated)
**Duration**: 5 min (automatic)

### How to Generate:
1. In Unity Editor, open `Window → General → Script Execution Order`
2. Find `GenerateFallbackAssets.cs`
3. Place in menu: top-level Tools menu → "Generate Fallback Assets"
4. Alternative: Add to scene as GameObject with Script component, call `GenerateFallbackAssets.GenerateCharacterSprites()` in Awake()

### Output:
- ✅ 10 character sprites (Assets/Resources/Characters/)
- ✅ 16 icon sprites (Assets/Resources/Icons/)
- ✅ 5 arena backgrounds (Assets/Resources/ArenaBackgrounds/)

**Note**: These are fallback quality. Real Scenario.gg assets can be swapped in Week 5.

---

## PHASE 2: BUTTON POLISH
**Duration**: 15 min

### Setup Script: `ButtonPolishEffect.cs`

1. **Main Menu Button** (Play, Shop, Ranked, etc.)
   - Add component: `ButtonPolishEffect`
   - Assign: Button component in Inspector
   - Create glow overlay (child Image with white color, alpha 0.3)
   - Set `Glow Overlay` field in script
   
2. **All Interactive Buttons**
   - Repeat for: CharacterSelectionScreen, ShopScreen, RankedLadderScreen buttons
   - Set these properties in Inspector:
     - Hover Scale: 1.05
     - Press Scale: 0.95
     - Transition Duration: 0.15s
     - Play Click Feedback: TRUE

### Test:
- Hover buttons → should scale up + glow
- Click buttons → bounce feedback + "click" sound
- Disabled buttons → gray out (50% alpha)

---

## PHASE 3: CARD GLOW SYSTEM
**Duration**: 20 min

### Setup Script: `CardGlowRing.cs`

1. **Card Prefab Enhancement**
   - Open CardUI.prefab (create if missing from CardDeckScreen.cs guide)
   - Add child Image: `GlowRing`
     - Size: Same as character portrait
     - Anchors: Stretch to fill
     - Color: White (will change per rarity)
     - Image: White circle sprite (or create white quad)

2. **Attach CardGlowRing Script**
   - Add script to CardUI prefab
   - Drag `GlowRing` Image into `Glow Ring Image` field
   - Glow Ring should pulse automatically

3. **Rarity Colors (Automatic)**
   - Common (0) → Gold glow
   - Rare (1) → Purple glow
   - Epic (2) → Cyan glow
   - Legendary (3) → Red/Orange glow

### Test:
- CardDeckScreen opens
- Each character card has pulsing rarity glow
- Rarity glow brightness matches card type

---

## PHASE 4: PARALLAX BACKGROUNDS
**Duration**: 25 min

### Setup Script: `ParallaxBackgroundController.cs`

1. **Create Background Panel**
   - Add to each Arena scene (or game HUD):
     - Panel: "BackgroundContainer"
     - Size: 1080×1920 (or full screen)
     - Children:
       - Image: `Layer1_Far` (slowest scroll)
       - Image: `Layer2_Mid` (medium scroll)
       - Image: `Layer3_Near` (fastest scroll)

2. **Attach Script**
   - Add `ParallaxBackgroundController` to Panel
   - Assign Layer Images in Inspector:
     - Background Layer 1: Layer1_Far
     - Background Layer 2: Layer2_Mid
     - Background Layer 3: Layer3_Near
   - Set speeds:
     - Layer 1 Speed: 0.3
     - Layer 2 Speed: 0.6
     - Layer 3 Speed: 0.9

3. **Assign Fallback Sprites**
   - Layer1: Load "rookie_bg" from Resources/ArenaBackgrounds
   - Layer2: Load "silver_bg" from Resources/ArenaBackgrounds
   - Layer3: Load "gold_bg" from Resources/ArenaBackgrounds
   - (Later: swap with real Scenario.gg assets)

4. **Test Integration**
   - In GameManager.cs, call:
     ```csharp
     parallaxController.UpdateTravelDistance(playerDistance);
     parallaxController.SetArenaTheme(currentArena);
     ```
   - Backgrounds should scroll at different speeds → depth effect

---

## PHASE 5: UI ANIMATION HELPER
**Duration**: 10 min (integration)

### Script: `UIAnimationHelper.cs`

This is a static utility — no setup needed. Just use in your animation code:

**Example Usage in any UI script:**
```csharp
// Fade in a modal
CanvasGroup modalGroup = modal.GetComponent<CanvasGroup>();
StartCoroutine(UIAnimationHelper.FadeTo(modalGroup, 1f, 0.3f, UIAnimationHelper.EasingType.EaseOutCubic));

// Scale button on click
Button myButton = GetComponent<Button>();
StartCoroutine(UIAnimationHelper.ScaleBounce(myButton.transform, 1.1f, 0.3f));

// Count up score
StartCoroutine(UIAnimationHelper.CountUpText(scoreText, 0, 5000, 1f));
```

### Integration Points:
- CardDeckScreen: Use `CountUpText` for card level upgrades
- VictoryScreen: Use `ScaleBounce` for trophy/gem rewards
- CharacterSelectionScreen: Use `FadeInAndScale` for character reveal

---

## PHASE 6: SFX MANAGER
**Duration**: 10 min

### Setup Script: `SFXManager.cs`

1. **Create Persistent Manager**
   - Hierarchy: Create GameObject "AudioManager"
   - Add `SFXManager` script
   - This auto-creates UI SFX Source + Gameplay SFX Source

2. **Create Audio Clips**
   - Project structure:
     ```
     Assets/Resources/Audio/SFX/
       ├── click.wav
       ├── pop.wav
       ├── upgrade.wav
       ├── victory.wav
       └── defeat.wav
     ```
   - Placeholder: Use FMOD or generate via Scenario.gg (Week 5+)

3. **Wire Button Clicks**
   - In ButtonPolishEffect.cs, uncomment:
     ```csharp
     if (playClickFeedback)
         SFXManager.Instance?.PlayClickSound();
     ```

4. **Wire Game Events**
   - Victory Screen: `SFXManager.Instance.PlayVictorySound()`
   - Defeat Screen: `SFXManager.Instance.PlayDefeatSound()`
   - Card Upgrade: `SFXManager.Instance.PlayUpgradeSound()`

---

## PHASE 7: VAULT ANIMATION INTEGRATION
**Duration**: 15 min

### Script: `VaultOpeningAnimation.cs` (already implemented)

1. **Create Canvas for Vault Panel**
   - Hierarchy: Add Panel to Canvas: "VaultOpeningPanel"
   - Children:
     - Image: `VaultDoor` (vault_body.png)
       - Child: Image: `WheelHandle` (vault_wheel.png) — centered
     - Image: `VaultInterior` (vault_interior.png, behind VaultDoor)
     - Image: `LightBurst` (white circle, alpha 0)
     - RectTransform: `LootContainer` (empty, for coins/gems)
     - TMP_Text: `TapToContinue` (UI text)

2. **Attach VaultOpeningAnimation Script**
   - Add to VaultOpeningPanel
   - Drag all children into Inspector fields:
     - Vault Door: VaultDoor Image
     - Wheel Handle: WheelHandle Image
     - Light Burst: LightBurst Image
     - Loot Container: LootContainer RectTransform
     - Coin Text: TMP_Text (coins earned)
     - Gem Text: TMP_Text (gems earned)
     - Tap Text: TMP_Text ("Tap to continue")

3. **Call from GameOverScreen**
   - When run ends with coins/gems:
   ```csharp
   vaultAnimation.PlayAnimation(coinsEarned: 150, gemsEarned: 5);
   ```

4. **Test**
   - GameOverScreen → vault animates:
     - Bounce in ✓
     - Wheel spins 3× ✓
     - Door swings away ✓
     - Loot floats up ✓
     - Tap to continue appears ✓

---

## PHASE 8: LOOT BURST PARTICLES
**Duration**: 10 min (optional for Saturday)

### Script: `LootBurstEffect.cs`

Advanced particle burst effect for coin/gem pickup animations.

**Setup (if time permits):**
1. Create prefab: LootBurstEffect (with script + particle systems)
2. Attach to LootContainer in VaultOpeningPanel
3. Call `PlayBurst()` when coins appear

**For Saturday MVP**: Can skip this — vault animation works without particles.

---

## QUICK CHECKLIST (Saturday Timeline)

**10:00 - Asset Generation** (5 min)
- [ ] Call GenerateFallbackAssets
- [ ] Verify sprites in project folders

**10:05 - Button Polish** (15 min)
- [ ] Add ButtonPolishEffect to all main buttons
- [ ] Test hover/press/click feedback

**10:20 - Card Glows** (20 min)
- [ ] Enhance CardUI.prefab with glow ring
- [ ] Attach CardGlowRing script
- [ ] Test color per rarity

**10:40 - Parallax Backgrounds** (25 min)
- [ ] Create background panel with 3 layers
- [ ] Attach ParallaxBackgroundController
- [ ] Wire into GameManager

**11:05 - SFX Manager** (10 min)
- [ ] Create AudioManager persistent object
- [ ] Wire button click sounds
- [ ] Test SFX playback

**11:15 - Vault Animation** (15 min)
- [ ] Build VaultOpeningPanel canvas
- [ ] Attach VaultOpeningAnimation script
- [ ] Wire from GameOverScreen

**11:30 - Testing + Polish** (30 min)
- [ ] Run full game flow
- [ ] Verify: characters load, cards glow, buttons polish, vault animates
- [ ] Fix any sprite loading issues
- [ ] Build APK via GitHub Actions

**12:00 - APK Ready for Test**

---

## FALLBACK FOR MISSING SPRITES

If Scenario.gg assets don't arrive:
- CardUI fallback: Solid color square + rarity glow ring (still looks premium)
- Icons fallback: Colored circles with text labels
- Backgrounds fallback: Solid color + shader gradient overlay (already implemented)

**Game is 100% playable with fallbacks** — just cosmetically less polished. Real assets slot in Week 5.

---

## TECHNICAL NOTES

**Shader Requirements:**
- ToonCelShaded.shader ✅ (already implemented for Knox)
- Optional: UIOutlineGlowShader (for glow effects, can use outline component instead)

**Performance:**
- Parallax backgrounds: Minimal cost (3 images, texture offset per frame)
- Glow rings: Minimal cost (alpha pulse, no particle systems)
- Button scales: Minimal cost (single transform animation)
- Audio SFX: Pooled via AudioSource (no heap allocation)

**Tested On:**
- Unity 2022 LTS ✅
- Built-in Render Pipeline ✅
- Mobile Target (Android/iOS) ✅

---

## NEXT STEPS (Week 5)

1. Generate real character sprites via Scenario.gg
2. Replace fallback PNGs with premium assets
3. Add Scenario.gg 3D GLB character import (optional upgrade path)
4. Create real SFX audio clips (or use FMOD)
5. Add season-specific theme overlays

---

**Good luck Saturday! This polish will make the difference. 🎮✨**
