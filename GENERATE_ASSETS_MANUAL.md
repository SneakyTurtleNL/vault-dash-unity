# GENERATE ASSETS MANUALLY (Web UI - Fastest for Saturday)

## Quick Setup (15-20 minutes)

### Option A: Web UI Batch Generation (RECOMMENDED)

1. **Go to** https://app.scenario.gg
2. **Sign in** with your account
3. **Create 31 generations** using templates below
4. **Download all PNGs**
5. **Drag into Unity** → Assets auto-import

---

## GENERATION TEMPLATES (Copy-Paste Prompts)

### PHASE 1: CHARACTERS (10 assets)

Model: **Vault Dash Characters** (or equivalent chibi model)
Size: **512×512 per image**
Samples: **1** (cost-efficient)

**Prompts (copy-paste):**

```
1. agent_zero
"blue tactical soldier character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"

2. cipher
"purple hacker character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"

3. blaze
"red fiery warrior character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"

4. tank
"green armored soldier character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"

5. ghost
"white ghost specter character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"

6. viper
"yellow snake-themed character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"

7. nova
"cyan space explorer character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"

8. pulse
"pink energy character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"

9. eclipse
"dark moon character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"

10. phoenix
"orange firebird character, chibi style, front-facing, cel-shaded, flat colors, Clash Royale aesthetic, clean white background, 512x512"
```

---

### PHASE 2: ICONS (16 assets)

Model: **Puffy Icons 3.0** (model_WxA48UWbzJ861obmaxndHztE)
Size: **256×256 per icon**
Samples: **1**

**Prompts (copy-paste):**

```
1. trophy
"gold trophy icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

2. gem
"purple gemstone icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

3. coin
"gold coin icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

4. sword
"red sword icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

5. shield
"blue shield icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

6. lightning
"yellow lightning bolt icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

7. skull
"gray skull icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

8. dice
"white dice icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

9. clover
"green lucky clover icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

10. card
"playing card icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

11. star
"yellow star icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

12. medal_gold
"gold medal icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

13. medal_silver
"silver medal icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

14. medal_bronze
"bronze medal icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

15. clock
"antique clock icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"

16. crown
"royal crown icon, glossy 3D rounded, Puffy Icons style, professional, bold colors, clean white background, 256x256"
```

---

### PHASE 3: ARENA BACKGROUNDS (5 assets)

Model: **Cartoon Backgrounds 2.0** (model_hHuMquQ1QvEGHS1w7tGuYXud)
Size: **1024×512 per background**
Samples: **1**

**Prompts (copy-paste):**

```
1. rookie
"underground bank vault scene, gray concrete walls, steel safe boxes, golden details, industrial beton, Cartoon Backgrounds style, vibrant colors, Supercell aesthetic, isometric perspective, game environment, 1024x512"

2. silver
"urban sewer scene, graffiti walls, glowing green sludge, neon reflections, glowing fungi, Cartoon Backgrounds style, vibrant colors, Supercell aesthetic, isometric perspective, game environment, 1024x512"

3. gold
"ancient jungle temple scene, warm amber lighting, moss-covered vines, golden torches, stone statues, Cartoon Backgrounds style, vibrant colors, Supercell aesthetic, isometric perspective, game environment, 1024x512"

4. diamond
"cyberpunk data corridor scene, neon blue grid, dark navy walls, laser obstacles, digital screens, Cartoon Backgrounds style, vibrant colors, Supercell aesthetic, isometric perspective, game environment, 1024x512"

5. legend
"outer space asteroid scene, cosmic purple and gold nebula, crystal formations, starfield, alien structures, Cartoon Backgrounds style, vibrant colors, Supercell aesthetic, isometric perspective, game environment, 1024x512"
```

---

## DOWNLOAD & ORGANIZE

**After generation completes:**

1. Download all 31 PNGs from Scenario.gg
2. Organize into folders:
   ```
   vault-dash-unity/
   ├── Assets/Resources/Characters/
   │   ├── agent_zero.png
   │   ├── cipher.png
   │   ├── ... (10 total)
   ├── Assets/Resources/Icons/
   │   ├── trophy.png
   │   ├── gem.png
   │   ├── ... (16 total)
   └── Assets/Resources/ArenaBackgrounds/
       ├── rookie.png
       ├── silver.png
       ├── ... (5 total)
   ```

3. **Open vault-dash-unity in Unity**
4. Assets auto-import from Resources folders
5. Ready for Saturday APK build!

---

## FALLBACK: Already Have Them!

⚠️ If Scenario.gg is slow, **don't worry**:

- GenerateFallbackAssets.cs already created programmatic fallbacks
- Game is **100% playable** with fallback colors
- Real premium assets slot in post-generation
- Saturday test proceeds with or without real sprites

---

## TIMELINE

- **Now**: Start web UI batch generation (parallel to other work)
- **+15 min**: All 31 assets queued
- **+2-5 min**: Scenario.gg processes (async in background)
- **+5 min**: Download all PNGs
- **+2 min**: Drag into Unity
- **+5 min**: Build APK
- **Saturday**: Ship with premium assets ✨

---

## TOTAL TIME: ~30 minutes (mostly async waiting)

You're free to work on other features while generations process!
