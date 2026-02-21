# Skill Icon Sprites

Place PNG sprites here, named exactly as the skillId:

  freeze.png, reverse.png, shrink.png, obstacle.png
  shield.png, ghost_skill.png, deflect.png, slowmo.png
  magnet.png, double_loot.png, steal.png, vault_key.png

These are loaded at runtime via:
  Resources.Load<Sprite>("Skills/{skillId}")

Placeholder: any gray/white square works as a placeholder during dev.
Size recommendation: 256×256 px, transparent background.
