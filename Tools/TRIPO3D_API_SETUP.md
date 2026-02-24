# Tripo3D API Setup — Bulk Character Generation

## Overview

Automation script voor image-to-3D conversion via Tripo3D API. Perfect voor bulk character generation (alle 10 characters + skins).

## API Details

- **Base URL**: `https://api.tripo3d.ai/v2/openapi`
- **Auth**: Bearer token (API key)
- **Key**: `tsk_potWLVr_XhKHDY-vsAZ-Z-hi-NbCBSOaZlgmPgWa1tw`

## Usage

```bash
python3 tripo3d_generator.py <image_path> [output_dir]
```

**Example:**
```bash
python3 tripo3d_generator.py agent_zero.jpg ./output
# → Uploads image
# → Submits image_to_model task
# → Polls for completion (5-10 min typically)
# → Downloads GLB + GLTF to ./output/
```

## Workflow

1. **Upload image** → get `image_key`
2. **Submit task** with `image_key` → get `task_id`
3. **Poll task** until `status: success`
4. **Download models** (GLB, GLTF)

## For Bulk Generation

Once validated, can loop over character list:

```bash
for char in agent_zero blaze tank ghost viper nova pulse eclipse phoenix cipher; do
    python3 tripo3d_generator.py $char.jpg ./characters/$char
done
```

## Dependencies

```bash
pip install requests
```

## Notes

- Free tier: 25 credits/day
- Professional: unlimited
- Each image_to_model ≈ 5 credits
- Processing: 5-15 minutes per model

## Troubleshooting

- **1004 error**: Check parameter format
- **Timeout**: Model complex, wait longer
- **No download**: Check task `model` object in response

---

Generated: 2026-02-24
Status: Ready for bulk character generation phase
