# Arena Background Sprites

## File Naming Convention
```
{arena}_{layer}.png

Examples:
  rookie_sky.png     — Underground vault sky layer
  rookie_mid.png     — Underground vault mid layer  
  rookie_ground.png  — Underground vault ground layer
  silver_sky.png     — Urban sewer sky
  ...
```

## Required Files (15 total — 5 arenas × 3 layers)

| Arena | Layer | File | Theme |
|-------|-------|------|-------|
| Rookie | sky | `rookie_sky.png` | Dark grey ceiling with gold trim |
| Rookie | mid | `rookie_mid.png` | Vault walls with gold lockers |
| Rookie | ground | `rookie_ground.png` | Concrete floor with gold lines |
| Silver | sky | `silver_sky.png` | Dark sewer ceiling |
| Silver | mid | `silver_mid.png` | Pipe-lined walls, neon green drips |
| Silver | ground | `silver_ground.png` | Wet concrete, neon reflections |
| Gold | sky | `gold_sky.png` | Jungle canopy, amber filtered light |
| Gold | mid | `gold_mid.png` | Temple ruins, moss, amber stone |
| Gold | ground | `gold_ground.png` | Ancient tiles, jungle roots |
| Diamond | sky | `diamond_sky.png` | Cyberpunk city skyline, navy |
| Diamond | mid | `diamond_mid.png` | Neon corridor panels, holograms |
| Diamond | ground | `diamond_ground.png` | Reflective metallic floor |
| Legend | sky | `legend_sky.png` | Deep space, stars, nebula |
| Legend | mid | `legend_mid.png` | Asteroid corridor, purple crystal |
| Legend | ground | `legend_ground.png` | Space rock floor, gold dust |

## Sprite Dimensions
- Recommended: 2048×512 px (wide seamless tile for parallax scroll)
- Format: PNG, RGBA (alpha channel for transparency effects)
- Import Settings: Sprite Mode = Single, Pixels Per Unit = 100

## Parallax Scroll Speeds
- Sky layer: 0.2× scroll speed
- Mid layer: 0.6× scroll speed
- Ground layer: 1.0× scroll speed (matches tunnel tile speed)

## Asset Generation Options
1. **Scenario.gg** — AI render each layer with prompt
2. **Midjourney** — `isometric game background, [theme], pixel art style --ar 4:1`
3. **Custom art** — Illustrator/Photoshop

## Temporary Fallback
TunnelGenerator.cs will use solid-color backgrounds if sprites are null.
Each arena has a unique color theme coded in TunnelGenerator.arenaBackgrounds[].
