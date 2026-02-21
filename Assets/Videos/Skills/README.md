# Skill Video Previews

One MP4 per skill (3-5 seconds, looping, showing skill in action).

## Required clips

| File                  | Skill        | Category   | Duration |
|-----------------------|--------------|------------|----------|
| freeze.mp4            | Freeze       | Offensive  | 4s       |
| reverse.mp4           | Reverse      | Offensive  | 3s       |
| shrink.mp4            | Shrink       | Offensive  | 4s       |
| obstacle.mp4          | Obstacle     | Offensive  | 3s       |
| shield.mp4            | Shield       | Defensive  | 5s       |
| ghost_skill.mp4       | Ghost        | Defensive  | 3s       |
| deflect.mp4           | Deflect      | Defensive  | 4s       |
| slowmo.mp4            | Slow-Mo      | Defensive  | 4s       |
| magnet.mp4            | Magnet       | Economic   | 5s       |
| double_loot.mp4       | Double Loot  | Economic   | 5s       |
| steal.mp4             | Steal        | Economic   | 3s       |
| vault_key.mp4         | Vault Key    | Economic   | 3s       |

## Placement

At runtime, clips must be in `StreamingAssets/Videos/Skills/` for Android.
During development, place here and update `CardDetailModal.LoadAndPlayVideo()` path.

## Placeholder

Until clips are generated, `CardDetailModal` shows a **gray placeholder box** instead of
the video player. No crashes — graceful fallback.

## Generation plan

- Use pre-rendered Unity screenshots (screen recording via Editor → `ScreenCapture.CaptureScreenshot`)
- Or AI video generation (Runway, Pika) from skill description
- 720×480 px, H.264, 30fps, looping-friendly (cut on same frame start/end)
