# Post-Processing Profiles — Vault Dash

## Setup (do once in Unity Editor)

### 1. Create Profile
Right-click in this folder → Create → Post-processing → Post-processing Profile  
Name it `VaultDash_Default.asset`

### 2. Add Overrides to the Profile
Click the profile asset → Add effect:
- **Bloom** — Threshold: 0.9, Intensity: 1.0, Diffusion: 7
- **Vignette** — Intensity: 0.2, Smoothness: 0.5, Rounded: off  
- **Color Grading** — Mode: Low Definition Range, Temperature: 0, Saturation: 10

### 3. Assign to PostProcessingManager
- Select the Main Camera in the scene
- Add component: **Post-process Volume** (set as Global)
- Assign `VaultDash_Default.asset` to the Profile field
- Drag the Camera (or the Volume GameObject) to `PostProcessingManager.volume` field

### 4. PostProcessingManager Script
The script (`Assets/Scripts/PostProcessingManager.cs`) will:
- `BloomPulse(4f)` → called when gem or power-up collected (from Player.cs)
- `VignetteFlash()` → called on obstacle collision (from Player.cs)
- `SetVictoryColors()` / `SetDefeatColors()` → called from GameManager.OnGameOver()

### Package
`com.unity.postprocessing 3.3.0` — added to `Packages/manifest.json`

Compile guard: `#if UNITY_POST_PROCESSING_STACK_V2`  
This define is automatically set by the Post-Processing package on import. ✅
