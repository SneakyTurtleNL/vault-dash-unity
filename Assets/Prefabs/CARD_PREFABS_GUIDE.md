# Card System Prefab Guide

## What to Build in Unity Editor

### 1. CardUI Prefab (`Assets/Prefabs/Cards/CardUI.prefab`)

```
CardUI (Button, CardUI.cs)
├── Frame (Image)                  ← frameImage — background rect, rarity-tinted
├── GlowRing (Image)               ← glowRingImage — soft circle behind portrait
├── Portrait (Image)               ← portraitImage — character/skill art
├── CategoryBadge (Image)          ← categoryBadgeImage — colored strip, skill only
├── NameLabel (TMP_Text)           ← nameLabel
├── LevelBadge (TMP_Text)          ← levelLabel — top-right corner "Lv 12/20"
├── PrestigeRow (GameObject)
│   ├── Star1 (Image)              ← prestigeStars[0]
│   ├── Star2 (Image)              ← prestigeStars[1]
│   └── Star3 (Image)              ← prestigeStars[2]  (up to 5)
├── ProgressBar (Slider)           ← progressBar
├── ProgressLabel (TMP_Text)       ← progressLabel "3 / 5 copies"
├── UpgradeButton (Button)
│   └── UpgradeCostLabel (TMP_Text)← upgradeCostLabel "500 🪙 → Rare"
└── SelectedBorder (Image)         ← selectedBorder (border ring, set enabled = false by default)
```

**Recommended size**: 160×220 px (character card) or 130×180 px (skill library card)

---

### 2. CardDeckScreen (`Assets/Scenes/MainMenu.unity` — new Canvas panel)

```
CardDeckScreen (CanvasGroup, CardDeckScreen.cs)
├── Header
│   ├── HeaderLabel (TMP_Text) "Characters"
│   └── CoinBalanceText (TMP_Text) "🪙 1,240"
├── ScrollView
│   └── Viewport/Content (GridLayoutGroup — 4 cols, cell 160×220)
│       └── [CardUI × 10 spawned at runtime]
├── GoToSkillsButton (Button) "Skills →"
└── BackButton (Button) "←"
```

Wire `cardContainer` → the `Content` transform.
Wire `cardPrefab` → `CardUI.prefab`.

---

### 3. SkillDeckScreen

```
SkillDeckScreen (CanvasGroup, SkillDeckScreen.cs)
├── DeckPanel
│   ├── DeckTitleLabel (TMP_Text) "Active Deck (pick 4)"
│   └── DeckSlots (HorizontalLayoutGroup)
│       ├── Slot1 (CardUI) ← deckSlots[0]
│       ├── Slot2 (CardUI) ← deckSlots[1]
│       ├── Slot3 (CardUI) ← deckSlots[2]
│       └── Slot4 (CardUI) ← deckSlots[3]
├── DividerLine
├── LibraryPanel
│   ├── DeckFullLabel (TMP_Text) "Deck full! Remove a skill first."
│   └── ScrollView/Viewport/Content (GridLayoutGroup — 4 cols, cell 130×180)
│       └── [CardUI × 12 spawned at runtime]
├── CoinBalanceText (TMP_Text)
├── GoToCharactersButton (Button)
└── BackButton (Button)
```

---

### 4. CardDetailModal (overlay on top of all screens)

```
CardDetailModal (CanvasGroup → starts hidden)
└── PanelRoot (Image — dark overlay + centered card)
    ├── PortraitArea
    │   ├── GlowRing (Image)        ← glowRingImage
    │   └── Portrait (Image)        ← portraitImage (large, ~300px)
    ├── InfoArea
    │   ├── NameLabel (TMP_Text)
    │   ├── RarityLabel (TMP_Text) "EPIC"
    │   ├── LevelLabel (TMP_Text)  "Level 8 / 20"
    │   └── LevelProgressBar (Slider)
    ├── StatsCharacterPanel
    │   ├── SpeedLabel  (TMP_Text)
    │   ├── HealthLabel (TMP_Text)
    │   └── DamageLabel (TMP_Text)
    ├── PrestigePanel
    │   └── PrestigeLabel (TMP_Text) "✦✦ Prestige 2"
    ├── StatsSkillPanel
    │   ├── DurationLabel (TMP_Text)
    │   ├── PowerLabel    (TMP_Text)
    │   └── CategoryLabel (TMP_Text) [colored]
    ├── VideoPanel
    │   ├── VideoDisplay (RawImage) ← videoDisplay — linked to RenderTexture
    │   └── VideoPlaceholder (Image) ← gray box shown when no clip
    ├── ProgressBar (Slider)
    ├── ProgressLabel (TMP_Text) "3 / 10 copies"
    ├── UpgradeButton (Button)
    │   └── UpgradeButtonLabel (TMP_Text) "Upgrade to Epic — 500 🪙"
    └── CloseButton (Button) "✕"
```

**VideoPlayer** component on CardDetailModal root:
- renderMode = RenderTexture
- targetTexture = create a RenderTexture (720×480) in Assets/
- isLooping = true

---

### 5. UpgradeConfirmModal (overlay, above CardDetailModal)

```
UpgradeConfirmModal (CanvasGroup → starts hidden)
└── PanelRoot (dark backdrop)
    ├── TitleLabel (TMP_Text)       "Upgrade Agent Zero"
    ├── BodyLabel (TMP_Text)        "Upgrade to Epic?\nCost: 500 🪙"
    ├── CoinBalanceLabel (TMP_Text) "Your balance: 1,240 🪙"
    ├── CardPreviewImage (Image)    small portrait
    ├── RarityGlowImage (Image)     ring showing target rarity color
    ├── ConfirmButton (Button)
    │   └── ConfirmButtonLabel "UPGRADE  500 🪙"
    └── CancelButton (Button) "Cancel"
```

---

## UIManager Wiring

In the UIManager Inspector, wire:
- `cardDeckPanel`  → CardDeckScreen CanvasGroup
- `skillDeckPanel` → SkillDeckScreen CanvasGroup

In MainMenuScreen, add buttons:
- `[MY CARDS]`  → `UIManager.Instance.ShowCardDeckScreen()`
- `[MY SKILLS]` → `UIManager.Instance.ShowSkillDeckScreen()`

---

## CardManager Setup (Scene)

Add `CardManager` component to an empty GameObject in `MainMenu.unity`.
- `usePlayerPrefsFallback = true` (always, until full Firebase SDK is in)
- `activeDeckSize = 4`
- `SetUserId(uid)` called from your auth flow once uid is known

---

## Notes

- All card data stored first in `PlayerPrefs` (immediate), then synced to Firestore
- No special shaders needed — all glow effects are color-tinted `Image` components
- `Resources.Load<Sprite>(key)` returns null gracefully if asset missing (grey placeholder)
- Videos show placeholder gray box until `.mp4` clips added to `StreamingAssets/Videos/Skills/`
