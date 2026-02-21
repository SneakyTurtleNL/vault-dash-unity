# Character Portrait Sprites

Place PNG sprites here, named exactly as the characterId:

  agent_zero.png, blaze.png, knox.png, jade.png, cipher.png
  ghost.png, nova.png, pulse.png, eclipse.png, phoenix.png

These are loaded at runtime via:
  Resources.Load<Sprite>("Characters/{characterId}")

Existing Scenario.gg sprites: copy from Assets/Sprites/ to here, renaming to match IDs.
Size: original Scenario.gg output (512× or larger), transparent background.
