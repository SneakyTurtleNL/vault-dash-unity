# Spine 2D Character Rigs

## Setup

### 1. Download Spine Unity Runtime
```
https://esotericsoftware.com/spine-unity-download
→ Download spine-unity-4.x-*.unitypackage
→ Import into Unity project
```

### 2. Enable Spine Support
```
Edit → Project Settings → Player → Scripting Define Symbols
Add: SPINE_AVAILABLE
```

### 3. Rig Characters in Spine Editor

Per character workflow:
1. Import character concept art as reference
2. Slice into body parts (head, torso, arms, legs, accessories)
3. Rig skeleton with IK constraints
4. Create animation timelines (see below)
5. Export: JSON + Atlas (.atlas) + Texture

### Required Animations Per Character

| Animation Name | Type | Description |
|---------------|------|-------------|
| `run` | Loop | Running cycle (8-12 frames) |
| `idle` | Loop | Standing idle with breath |
| `jump` | One-shot | Jump arc (launch → peak → fall) |
| `land` | One-shot | Landing impact + recovery |
| `crouch` | Loop | Flattened crouch stance |
| `victory` | One-shot | Character-specific celebration |
| `defeat` | One-shot | Slump / dejected reaction |

### Character-Specific Animation Notes

| Character | Run Style | Jump Style | Victory |
|-----------|-----------|------------|---------|
| Agent Zero | Military stride, arms tight | Sharp tactical leap | Salute |
| Blaze | Aggressive forward lean, fire trails | Explosive burst | Wild dance |
| Knox | Heavy planted steps, powerful push | Ground slam jump | Muscle flex |
| Jade | Fluid snake-like, low center | Silent glide | Elegant spin |
| Phantom | Ethereal float-run | Ghost phase | Vanish/appear |
| Storm | Electric twitch-step | Lightning dash | Electric pulse |
| Rook | Methodical march | Calculated vault | Noble bow |
| Cipher | Mechanical stutter-step | Teleport blink | Hacker gesture |
| Vex | Chaotic bounce | Wild flip | Chaotic celebration |
| Titan | Earth-shaking stomp | Minimal jump | Ground pound flex |

### File Structure
```
Assets/Spine/
├─ agent_zero/
│  ├─ agent_zero.skel    (binary skeleton)
│  ├─ agent_zero.atlas   (texture atlas definition)
│  └─ agent_zero.png     (sprite atlas texture)
├─ blaze/
│  └─ ... (same pattern)
└─ (all 10 characters)
```

### Unity Integration

1. Import .skel/.atlas/.png into Unity
2. Spine will auto-generate SkeletonData asset
3. Create prefab with SkeletonAnimation component
4. Add SpineCharacterController.cs script
5. Assign CharacterSpineProfile from SpineCharacterController.CharacterSpineProfile.AllProfiles[]

### SpineCharacterController Usage

```csharp
// In Player.cs - add these calls:
SpineCharacterController spine = GetComponent<SpineCharacterController>();

// On jump:
spine.TriggerJump();

// On land:
spine.TriggerLand();

// On crouch:
spine.SetCrouching(true);

// On crouch end:
spine.SetCrouching(false);

// Running state:
spine.SetRunning(isMoving);

// Speed-reactive:
spine.SetRunSpeed(character.runSpeed);

// Footstep pitch per character:
FMODAudioManager.Instance.StartFootsteps(profile.footstepPitch);
```

## Fallback

Without SPINE_AVAILABLE defined:
- SpineCharacterController.cs uses fallback Animator
- Standard Unity Animator with same animation state names works
- Assign Animator Controller with states: run, idle, jump, land, crouch, victory, defeat
