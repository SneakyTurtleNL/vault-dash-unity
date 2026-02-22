# Particle Cosmetics System — Complete Guide

_Particles are earned, not inherent. Players unlock optional visual effects for their characters through gameplay and purchases._

---

## Overview

**What**: Optional particle effects (auras, weapon trails, footsteps, spawn bursts) that players can unlock and apply to their characters.

**Why**: 
- Rewards for daily/weekly challenges
- Prestige tier unlocks
- Gem shop monetization
- Cosmetic progression (separate from gameplay)

**When**: Particles activate in:
- Character selection screen (preview)
- Gameplay (aura, trails, footsteps)
- Results screen (prestige burst)

**No P2W**: Particles are PURELY COSMETIC. No stat bonuses, no gameplay advantage.

---

## System Architecture

### Core Components

| Component | Purpose | Location |
|-----------|---------|----------|
| **CharacterParticleCosmetics.cs** | Manager: catalog, unlocks, active effects | `Assets/Scripts/Cosmetics/` |
| **ParticleShopPanel.cs** | UI: browse, filter, purchase particles | `Assets/Scripts/UI/Shop/` |
| **ParticleCosmetic (class)** | Data model: id, name, type, price, etc. | Serializable in CharacterParticleCosmetics.cs |
| **UnlockedParticleCosmetic (class)** | Player unlock data: when, how (earned vs purchased) | Firestore schema |

### Runtime Flow

```
Player logs in
  ↓
CharacterParticleCosmetics.Instance initialized
  ↓
LoadUnlockedParticles(uid) → fetches from Firestore
  ↓
Player enters Character Select
  ↓
Can preview unlocked particles OR purchase new ones
  ↓
SetActiveParticle(characterId, particleId)
  ↓
Player enters Gameplay
  ↓
PlayParticleEffect(particleId, position) at key moments
  ↓
Results: show prestige-tier unlock burst
```

---

## Firestore Schema

### Collection Structure

```
players/{uid}/unlockedParticles/{particleId}
├── particleId: string       (e.g., "aura_blue_energy")
├── isPurchased: bool        (true = bought, false = earned)
└── unlockedDate: timestamp
```

### Example Firestore Document

```json
{
  "players": {
    "user123": {
      "unlockedParticles": {
        "aura_blue_energy": {
          "particleId": "aura_blue_energy",
          "isPurchased": false,
          "unlockedDate": "2026-02-22T10:30:00Z"
        },
        "trail_fire": {
          "particleId": "trail_fire",
          "isPurchased": true,
          "unlockedDate": "2026-02-22T11:15:00Z"
        }
      }
    }
  }
}
```

---

## Catalog: Built-In Particles

### Auras (Universal)

| ID | Name | Price | Prestige Req | Description |
|:---|:-----|------:|:----------:|:------------|
| `aura_blue_energy` | Blue Energy Aura | 150💎 | — | Calm blue field |
| `aura_golden_light` | Golden Light Aura | 200💎 | **5** | Prestigious golden light — **PRESTIGE UNLOCK** |
| `aura_purple_mystical` | Purple Mystical Aura | 200💎 | — | Mystical purple energy |

### Weapon Trails

| ID | Name | Price | Prestige Req | Description |
|:---|:-----|------:|:----------:|:------------|
| `trail_fire_blaze` | Fire Trail | 100💎 | — | Flaming trail during movement |
| `trail_ice_cyan` | Ice Trail | 100💎 | — | Icy cyan trail with frost |

### Footsteps

| ID | Name | Price | Prestige Req | Description |
|:---|:-----|------:|:----------:|:------------|
| `footstep_stars` | Star Footsteps | 80💎 | — | Sparkly stars while running |

### Spawn Effects

| ID | Name | Price | Prestige Req | Description |
|:---|:-----|------:|:----------:|:------------|
| `spawn_burst_cosmic` | Cosmic Spawn Burst | 120💎 | — | Cosmic energy on arena entry |

### Prestige Unlocks (Earned, Not Purchased)

| ID | Name | Price | Prestige Req | Description |
|:---|:-----|------:|:----------:|:------------|
| `levelup_burst_legendary` | Legendary Level-Up Burst | — | **10** | Auto-earned at prestige tier 10 |

---

## How Players Earn Particles

### Method 1: Daily Challenges
```
Daily Challenge Reward: "Footstep Stars"
→ Completes daily → Claims 80 gems OR particle
→ CharacterParticleCosmetics.UnlockParticle(uid, "footstep_stars", isPurchased: false)
→ Firestore updated
```

### Method 2: Weekly Missions
```
Weekly Mission: "Win 5 ranked matches"
→ Completes → Rewards: 200 gems OR "Fire Trail" particle
→ UnlockParticle(uid, "trail_fire", isPurchased: false)
```

### Method 3: Prestige Tier Unlocks
```
Player reaches prestige tier 5
→ SeasonManager checks UnlockPrestigeTier requirement
→ Auto-unlock: "Golden Light Aura"
→ UnlockParticle(uid, "aura_golden_light", isPurchased: false, autoUnlocked: true)
→ Show notification: "New cosmetic unlocked!"
```

### Method 4: Gem Shop Purchase
```
Player browses ParticleShopPanel
→ Selects "Blue Energy Aura" (150 gems)
→ Clicks "Buy"
→ ShopSystem.PurchaseParticle(uid, "aura_blue_energy", 150)
  - Deduct 150 gems
  - UnlockParticle(uid, "aura_blue_energy", isPurchased: true)
→ "Purchase successful!" toast
→ Particle now in player's collection
```

---

## Integration: Where to Apply Particles

### 1. Character Selection Screen

**File**: `CharacterSelectionScreen.cs` (or similar)

```csharp
// When player previews a character
CharacterModel character = GetSelectedCharacter();
string activeParticle = CharacterParticleCosmetics.Instance.GetActiveParticle(character.id);

if (!string.IsNullOrEmpty(activeParticle))
{
    // Show particle effect around character preview
    PlayParticleEffect(activeParticle, characterPreviewPosition);
}
```

### 2. Gameplay (In-Game Character)

**File**: `PlayerController.cs` or `TunnelGameManager.cs`

```csharp
// At game start
PlayerController player = GetPlayer();
string selectedCharacterId = player.ActiveCharacterId;
string particleId = CharacterParticleCosmetics.Instance.GetActiveParticle(selectedCharacterId);

if (!string.IsNullOrEmpty(particleId))
{
    // Create persistent particle effect on player
    ParticleSystem ps = CharacterParticleCosmetics.Instance.PlayParticleEffect(
        particleId, 
        player.transform.position
    );
    ps.transform.SetParent(player.transform);
    // Particle now follows player throughout game
}
```

### 3. Footsteps (Per-Frame Position)

**File**: `PlayerController.cs`

```csharp
private float _nextFootstepTime = 0f;
private float _footstepInterval = 0.3f; // Every 0.3 sec while moving

void Update()
{
    if (!IsMoving) return;
    
    if (Time.time >= _nextFootstepTime)
    {
        string particleId = CharacterParticleCosmetics.Instance.GetActiveParticle(ActiveCharacterId);
        if (!string.IsNullOrEmpty(particleId) && particleId.Contains("footstep"))
        {
            CharacterParticleCosmetics.Instance.PlayParticleEffect(particleId, transform.position);
        }
        
        _nextFootstepTime = Time.time + _footstepInterval;
    }
}
```

### 4. Spawn/Spawn Burst

**File**: `TunnelGameManager.cs` or `ArenaScreen.cs`

```csharp
// When player enters arena
void OnArenaStarted()
{
    string selectedCharacter = GetSelectedCharacter();
    string particleId = CharacterParticleCosmetics.Instance.GetActiveParticle(selectedCharacter);
    
    if (particleId?.Contains("spawn") == true)
    {
        CharacterParticleCosmetics.Instance.PlayParticleEffect(particleId, playerSpawnPosition);
    }
}
```

### 5. Prestige Tier Unlock Celebration

**File**: `SeasonManager.cs` (Season reset logic)

```csharp
// When player reaches new prestige tier
void CheckPrestigeRewards(int prestigeTier)
{
    // Auto-unlock particles for this tier
    var catalog = CharacterParticleCosmetics.Instance.GetCatalog();
    var tierRewards = catalog.FindAll(p => p.unlockPrestigeTier == prestigeTier);
    
    foreach (var reward in tierRewards)
    {
        CharacterParticleCosmetics.Instance.UnlockParticle(uid, reward.id, isPurchased: false);
        
        // Show burst effect
        CharacterParticleCosmetics.Instance.PlayParticleEffect(reward.id, screenCenter);
        
        // Announce: "Prestige tier 5 reached! Golden Aura unlocked!"
    }
}
```

---

## Creating New Particles (Prefab-Based)

### Step 1: Design in Unity Editor

1. Create new empty GameObject: `Aura_CustomBlue`
2. Add `ParticleSystem` component
3. Configure:
   - **Emission**: 50 particles/sec
   - **Lifetime**: 2 seconds
   - **Speed**: varies (-0.5 to 0.5 m/s for aura)
   - **Color**: blue gradient
   - **Size**: 0.5–1.0 units
4. Save as prefab: `Assets/Resources/Particles/Auras/aura_custom_blue.prefab`

### Step 2: Register in CharacterParticleCosmetics.cs

```csharp
// In InitializeCatalog()
AddParticleCosmetic(new ParticleCosmetic
{
    id = "aura_custom_blue",
    name = "Custom Blue Aura",
    characterId = null, // universal
    effectType = "aura",
    prefabPath = "Particles/Auras/aura_custom_blue",
    gemsPrice = 150,
    unlockPrestigeTier = 0,
    description = "Sleek custom blue aura",
    rarity = new Color(0.3f, 0.6f, 1f) // blue
});
```

### Step 3: Test

1. Load player unlocks: `CharacterParticleCosmetics.Instance.LoadUnlockedParticles(uid)`
2. Unlock particle: `CharacterParticleCosmetics.Instance.UnlockParticle(uid, "aura_custom_blue", false)`
3. Play: `CharacterParticleCosmetics.Instance.PlayParticleEffect("aura_custom_blue", Vector3.zero)`
4. Verify in gameplay or character select

---

## Daily Challenge Integration Example

**File**: `ChallengeManager.cs`

```csharp
public class ChallengeReward
{
    public int coins;
    public int gems;
    public string particleId; // NEW: optional particle reward
}

void ClaimChallenge(Challenge challenge)
{
    var reward = challenge.reward;
    
    // Award coins/gems
    AddCoins(reward.coins);
    AddGems(reward.gems);
    
    // Award particle (if applicable)
    if (!string.IsNullOrEmpty(reward.particleId))
    {
        CharacterParticleCosmetics.Instance.UnlockParticle(
            currentUID, 
            reward.particleId, 
            isPurchased: false
        );
        
        ShowToast($"Unlocked: {reward.particleId}!");
    }
}
```

---

## Shop Integration Example

**File**: `ShopSystem.cs`

```csharp
public class ShopSystem : MonoBehaviour
{
    public async void PurchaseParticle(string uid, string particleId, int gemsPrice)
    {
        // Verify player has enough gems
        if (GetPlayerGems() < gemsPrice)
        {
            ShowError("Not enough gems!");
            return;
        }
        
        // Deduct gems
        DeductGems(gemsPrice);
        
        // Unlock particle
        await CharacterParticleCosmetics.Instance.UnlockParticle(uid, particleId, isPurchased: true);
        
        // Show success
        ShowToast($"Purchased {particleId}!");
        ParticleShopPanel.RefreshShop();
    }
}
```

---

## Testing Checklist

- [ ] Initialize CharacterParticleCosmetics.Instance on game start
- [ ] LoadUnlockedParticles() fetches from Firestore correctly
- [ ] Daily challenge claims unlock particles
- [ ] Prestige tier 5+ auto-unlocks prestige particles
- [ ] Shop shows correct prices and "OWNED" badges
- [ ] Purchase deducts gems and unlocks particle
- [ ] SetActiveParticle() works and persists
- [ ] PlayParticleEffect() instantiates and destroys correctly
- [ ] Aura shows on character in selection screen
- [ ] Particle follows player in gameplay
- [ ] Footsteps trigger at correct intervals
- [ ] Spawn burst plays when entering arena
- [ ] Prestige tier unlock celebration works

---

## Future Enhancements

1. **Character-Specific Particles**: e.g., "Blaze Fire Aura" only for Blaze
2. **Particle Layering**: Stack multiple effects (aura + trail + footstep)
3. **Custom Colors**: Let players tint auras (requires shader parameter)
4. **Animations**: Particles that pulse/breathe with character rhythm
5. **Combo Effects**: Special particles for prestige milestone (tier 50, 100, etc.)
6. **Battle Pass Rewards**: Seasonal exclusive particles
7. **Tournament Rewards**: Unique particles for tournament winners

---

## Code Locations

| File | Purpose |
|------|---------|
| `CharacterParticleCosmetics.cs` | Core manager + catalog |
| `ParticleShopPanel.cs` | Shop UI + purchase flow |
| `ChallengeManager.cs` | Daily challenge rewards |
| `SeasonManager.cs` | Prestige tier unlocks |
| `ShopSystem.cs` | Gem purchase processing |
| `PlayerController.cs` | Apply active particle in gameplay |
| `CharacterSelectionScreen.cs` | Preview particles |

---

**Status**: ✅ System ready to integrate. Particles are optional add-on; core game works without them.

**Next Steps**:
1. Create 5-10 particle prefabs (auras, trails, footsteps)
2. Wire into daily challenges → reward pool
3. Wire prestige tier unlocks → auto-grant at tier 5/10
4. Test end-to-end: unlock → purchase → play → see effect
