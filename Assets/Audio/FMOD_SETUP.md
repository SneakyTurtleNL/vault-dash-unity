# FMOD Studio Setup Guide — Vault Dash

## Step 1: Download FMOD Studio

1. Go to https://fmod.com/download
2. Create free account (required even for free tier)
3. Download: **FMOD Studio** (DAW) + **FMOD Engine** (Unity package)
4. Free for indie devs earning < $200k USD/year

## Step 2: Install Unity Integration

```
Window → Package Manager → Add package from disk
→ Select: fmod-unity-*-installer.pkg.zip/FMOD Unity.unitypackage
```

Or via manifest.json:
```json
{
  "dependencies": {
    "com.fmod.fmodstudio": "file:../../FMOD/fmod-unity-2.02.xx.zip"
  }
}
```

## Step 3: Add Scripting Define Symbol

```
Edit → Project Settings → Player → Scripting Define Symbols
Add: FMOD_AVAILABLE
```

## Step 4: Create FMOD Studio Project

1. Open FMOD Studio app
2. Create new project: `Assets/Audio/VaultDash.fspro`
3. Create Banks:
   - Master Bank (auto-created)
   - SFX Bank
   - Music Bank

## Step 5: Create Events

Create these events in FMOD Studio:

### SFX Bank
| Event Path | Description | Loop |
|-----------|-------------|------|
| `event:/SFX/Footstep` | Running footstep | Yes (triggered by code) |
| `event:/SFX/Jump` | Jump launch | No |
| `event:/SFX/Crouch` | Crouch flatten | No |
| `event:/SFX/LaneChange` | Lateral swipe | No |
| `event:/SFX/ObstacleHit` | Crash/collision | No |
| `event:/SFX/CoinCollect` | Coin pickup | No |
| `event:/SFX/GemCollect` | Gem pickup (higher pitch) | No |
| `event:/SFX/PowerUp` | Power-up activation | No |
| `event:/SFX/OpponentNear` | Danger sting | No |
| `event:/SFX/Countdown` | 3-2-1 countdown tick | No |

### Music Bank
| Event Path | Description | Notes |
|-----------|-------------|-------|
| `event:/Music/MenuBGM` | Main menu loop | Loop point set |
| `event:/Music/MatchBGM` | In-game music | Add Tension parameter |
| `event:/Music/VictoryFanfare` | Win sting | One-shot |
| `event:/Music/DefeatSting` | Lose sting | One-shot |

## Step 6: Tension Parameter (MatchBGM)

In `event:/Music/MatchBGM`:
1. Add Game Parameter: `Tension` (range 0.0 → 1.0)
2. Connect to Pitch: +0 semitones at 0.0 → +4 semitones at 1.0
3. Connect to Tempo: 120 BPM at 0.0 → 150 BPM at 1.0
4. Connect to LP Filter: open at 0.0 → brighter at 1.0

## Step 7: Footstep Parameter

In `event:/SFX/Footstep`:
1. Add Game Parameter: `CharacterPitch` (range 0.8 → 1.2)
2. Connect to Pitch: maps pitch shift per character type
   - Knox: 0.8 (deep)
   - Agent Zero: 1.0 (neutral)
   - Blaze: 1.15 (fast/light)
   - Nova: 1.2 (ethereal)

## Step 8: Bus Structure

```
Master Bus
├─ bus:/SFX    (volume controlled from settings)
│  ├─ Footsteps
│  ├─ Actions
│  └─ Events
└─ bus:/Music  (volume controlled from settings)
   ├─ BGM
   └─ Stings
```

## Step 9: Build Banks

```
File → Build → Build All
Output: Assets/Audio/FMOD/
```

Unity auto-loads `.bank` files at runtime.

## Step 10: Unity Event Browser

After building:
1. FMOD Studio → Show in Browser
2. Drag events to `FMODAudioManager` Inspector fields
3. Or type event paths directly in string fields

## Audio Asset Sources

### Free sound libraries
- **Freesound.org** — community sounds (CC license)
- **Pixabay.com/sound** — royalty free
- **ZapSplat.com** — game SFX packs
- **Epidemic Sound** — music stems (subscription)

### Recommended search terms
- Footsteps: "concrete footstep loop" 
- Jump: "whoosh jump spring"
- Coin: "coin pickup chime"
- Gem: "gem sparkle collect"
- Music: "electronic tension loop" / "cyberpunk ambient"

### AI generation
- **ElevenLabs Music** — generate custom BGM
- **Suno.ai** — full music tracks
- **Soundraw.io** — customizable game music

---

*FMODAudioManager.cs is ready to use immediately.*  
*Without FMOD_AVAILABLE defined, it falls back to AudioManager.cs.*
