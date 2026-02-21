# Audio Assets — Vault Dash

## Status
Procedural placeholders are auto-generated at runtime by `ProceduralAudio.cs` + `AudioManagerBootstrap.cs`.

Real WAV/OGG files dropped into this folder and assigned in the `AudioManager` Inspector fields
will automatically override the procedural versions.

## Required Files

| Filename           | Description                        | Suggested Source         |
|--------------------|------------------------------------|--------------------------|
| `footstep.wav`     | Short foot strike, 60–80ms         | Freesound.org            |
| `jump.wav`         | Rising whoosh, 150–200ms           | Freesound.org            |
| `crouch.wav`       | Soft thud/slide, 100ms             | Freesound.org            |
| `lane_change.wav`  | Quick swish, 100–130ms             | Freesound.org            |
| `coin_collect.wav` | Bright ding, 200ms (e.g. 880Hz)    | Freesound.org            |
| `gem_collect.wav`  | Sparkle chord, 300ms               | Freesound.org            |
| `power_up.wav`     | Rising sweep, 400ms                | Freesound.org            |
| `obstacle_hit.wav` | Impact crunch, 250ms               | Freesound.org            |
| `opponent_near.wav`| Low rumble/alarm, 300ms            | Freesound.org            |
| `victory.wav`      | Uplifting fanfare, 1–2s            | Freesound.org / Composer |
| `defeat.wav`       | Sad sting, 800ms                   | Freesound.org            |
| `menu_bgm.ogg`     | Menu background loop               | Composer                 |
| `match_bgm.ogg`    | In-game driving loop               | Composer                 |

## Notes
- Footstep interval: 320ms default (adjusted per character run speed)
- Match BGM pitch increases 1.0→1.4x as distance closes from 100m→0m (tension effect)
- Victory fanfare triggers on `OpponentVisualizer.TriggerCollision(true)`

## Import Settings (Unity)
- SFX: `Load Type: Decompress On Load`, `Compression: Vorbis`, `Quality: 70`
- BGM: `Load Type: Streaming`, `Compression: Vorbis`, `Quality: 50`
- Footstep: `Load Type: Decompress On Load`, very short so keep in memory
