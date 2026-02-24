# Sidekick Modular Characters — Color System Reference

## Overview

Synty Sidekick uses a **Material Override System** for colors:
- Each body part (head, torso, arms, legs) = separate material slot
- Change material color = instant skin variation
- No mesh re-modeling needed

## Color Workflow

### 1. Understanding Material Slots

In Sidekick Creator, each character part has slots:
- **Head Material** → skin tone, hair color
- **Torso Material** → chest armor, primary color
- **Shoulders Material** → shoulder armor, secondary color
- **Arms Material** → sleeve color
- **Legs Material** → pants/boots color
- **Accent Material** → glowing details, trim

### 2. Setting Colors

**In Sidekick Creator:**
```
1. Select character in editor
2. Find "Material Settings" panel
3. For each part, adjust color swatch
4. Live preview updates
5. Save as "Preset" when happy
```

**Via Inspector (if not using Creator):**
```
1. Select character prefab
2. Find SkinnedMeshRenderer components
3. Each renderer = material slot
4. Edit material color directly
5. Prefab → Overrides → Apply
```

## Color Palette — Vault Dash Characters

### Agent Zero

| Skin | Torso | Shoulders | Accents | Notes |
|------|-------|-----------|---------|-------|
| Blue (Base) | #0066FF | #003366 | #00FFFF | Tactical operative primary |
| Red | #FF3333 | #660000 | #FF6600 | Battle Pass Tier 10 |
| Gold | #FFD700 | #8B4513 | #FF8C00 | Premium/Prestige unlock |
| Neon Purple | #9933FF | #3D0099 | #FF00FF | Limited edition |

### Blaze

| Skin | Torso | Shoulders | Accents | Notes |
|------|-------|-----------|---------|-------|
| Orange (Base) | #FF6600 | #CC3300 | #FFCC00 | Fire warrior primary |
| Black/Lava | #1A1A1A | #660000 | #FF3333 | Dark theme |
| Gold/Flame | #FFD700 | #FF6600 | #FF0000 | Prestige unlock |

### Ghost

| Skin | Torso | Shoulders | Accents | Notes |
|------|-------|-----------|---------|-------|
| White (Base) | #FFFFFF | #E6E6E6 | #4D9FFF | Stealth operative |
| Silver | #C0C0C0 | #808080 | #0099FF | Battle Pass variant |
| Neon Blue | #0099FF | #0066FF | #00FFFF | Cyber theme |

### Cipher

| Skin | Torso | Shoulders | Accents | Notes |
|------|-------|-----------|---------|-------|
| Dark Green (Base) | #1A4D2E | #0D2818 | #00FF66 | Hacker operative |
| Matrix Green | #00FF00 | #00CC00 | #00FF00 | Classic hacker |
| Cyberpunk Pink | #FF00FF | #CC00CC | #FFFF00 | Retro-futuristic |

### Tank

| Skin | Torso | Shoulders | Accents | Notes |
|------|-------|-----------|---------|-------|
| Olive (Base) | #6B8E23 | #556B2F | #8B7355 | Military operative |
| Bronze | #8B4513 | #654321 | #DAA520 | Premium variant |
| Dark Gray | #333333 | #1A1A1A | #666666 | Stealth variant |

---

## How to Create Skins (Step-by-Step)

### Method 1: Via Sidekick Character Creator Tool

```
1. Open Sidekick Creator (Assets → Tools → Sidekick Creator)
2. Build base character (select head, torso, legs)
3. In "Material Settings" panel, adjust colors
4. Click "Save as Preset" → Name: "Agent_Zero_Blue"
5. Repeat for each color
6. Click "Bake" → Save as prefab
```

### Method 2: Via Material Inspector (Direct)

```
1. In Assets, locate character prefab
2. Double-click → opens prefab editor
3. Select the character instance
4. In Inspector, find "Skinned Mesh Renderer"
5. Expand "Materials" section
6. Click each material slot → adjust color in picker
7. Save and close
```

### Method 3: Via Script (Runtime)

If you want players to customize:
```csharp
SkinnedMeshRenderer renderer = character.GetComponent<SkinnedMeshRenderer>();
renderer.material.color = new Color(1f, 0f, 0f, 1f);  // Red
```

---

## Exporting & Using Skins

### Baking Process

Sidekick has a "Bake" feature:
- Converts modular parts → single combined mesh
- Optimizes for game performance
- Combines all materials into one

**Steps:**
```
1. In Sidekick Creator, right-click character
2. Click "Bake" or "Export"
3. Choose: "Combine all meshes" = YES
4. Output location: Assets/Characters/Skins/
5. Name: Agent_Zero_Blue.prefab
6. Click Save
```

**Result:** Single prefab with all parts combined, ready for instantiation.

---

## Performance Tips

### Do's ✅
- Use single material per character (combine before baking)
- Pre-bake all skins (don't modify at runtime)
- Use simple color changes (no complex textures)
- Share material atlases between characters

### Don'ts ❌
- Don't keep modular parts separate in-game (slow)
- Don't create new materials at runtime (allocates memory)
- Don't add too many unique skins (bloat)

---

## Material Color Naming Convention

For consistency, name saved color presets:
```
[CharacterID]_[ColorName]

Examples:
- agent_zero_blue
- agent_zero_red
- agent_zero_gold
- blaze_orange
- blaze_black
- ghost_white
- cipher_green
- tank_olive
```

---

## Testing Colors

**Before baking, test:**
1. Load character in scene
2. Adjust material colors in Inspector
3. Preview with ToonCelShaded shader
4. Confirm color reads well against tunnel background
5. Then bake

**Color contrast check:**
- Bright colors: #FF0000 (bright red)
- Dark colors: #003300 (dark green)
- Midtones: #666666 (gray)

---

## Troubleshooting

### Colors look washed out in game
- **Cause:** Lighting too bright or shader not applied
- **Fix:** Check ToonCelShaded shader + lighting intensity

### Material doesn't change
- **Cause:** Material is shared instance (changes all characters)
- **Fix:** Make sure each skin prefab has its own material instance

### Baked prefab looks different
- **Cause:** Combined materials lost original colors
- **Fix:** Re-bake with "Preserve material colors" option enabled

---

## Integration with CharacterManager_Sidekick.cs

Once skins are baked as prefabs, reference them in Inspector:

```
CharacterManager_Sidekick component:
  Available Skins (5):
    [0] characterId: "agent_zero" | skinId: "blue" | prefab: Agent_Zero_Blue.prefab
    [1] characterId: "agent_zero" | skinId: "red" | prefab: Agent_Zero_Red.prefab
    [2] characterId: "agent_zero" | skinId: "gold" | prefab: Agent_Zero_Gold.prefab
    [3] characterId: "blaze" | skinId: "orange" | prefab: Blaze_Orange.prefab
    [4] characterId: "blaze" | skinId: "black" | prefab: Blaze_Black.prefab
    ... etc
```

Then call:
```csharp
characterManager.LoadCharacter("agent_zero", "blue");
```

---

**Last Updated:** 2026-02-24
**Status:** Ready for Sunday setup
