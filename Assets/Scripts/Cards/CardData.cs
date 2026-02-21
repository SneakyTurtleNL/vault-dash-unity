using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CardData — All shared data types for the Vault Dash Card System.
///
/// Hierarchy:
///   CardRarity        → Common / Rare / Epic / Legendary
///   SkillCategory     → Offensive / Defensive / Economic
///   CharacterCardData → live runtime state for one character card
///   SkillCardData     → live runtime state for one skill card
///   CardDefinition    → static catalogue (name, description, artwork key)
///   CardDefinitions   → static registry with all 10 chars + 12 skills
///
/// Upgrade thresholds (copies needed for each rarity step):
///   Common → Rare  :  5 copies
///   Rare   → Epic  : 10 copies
///   Epic   → Legend: 20 copies
///   (Legend is max rarity — 50 spare copies shown but unused)
///
/// Stat scaling per rarity (+10 % per tier above Common):
///   Common   → base
///   Rare     → base × 1.10
///   Epic     → base × 1.20
///   Legendary→ base × 1.30
/// </summary>

// ─── Enums ────────────────────────────────────────────────────────────────────

public enum CardRarity
{
    Common    = 0,
    Rare      = 1,
    Epic      = 2,
    Legendary = 3
}

public enum SkillCategory
{
    Offensive  = 0,   // red
    Defensive  = 1,   // blue
    Economic   = 2,   // yellow
}

// ─── Runtime State ────────────────────────────────────────────────────────────

[Serializable]
public class CharacterCardData
{
    public string      characterId;          // "agent_zero", "blaze", …
    public CardRarity  rarity    = CardRarity.Common;
    public int         level     = 1;        // 1–20
    public int         copies    = 0;        // spare copies collected
    public int         prestige  = 0;        // prestige star count (reset from Legend)
    public long        lastUpgradeTimestamp; // Unix seconds (for Firestore)

    // Stats (base values multiplied by RarityMultiplier at runtime)
    public float baseSpeed  = 1.0f;
    public float baseHealth = 100f;
    public float baseDamage = 10f;

    public float RarityMultiplier => 1f + (int)rarity * 0.10f;
    public float Speed  => baseSpeed  * RarityMultiplier;
    public float Health => baseHealth * RarityMultiplier;
    public float Damage => baseDamage * RarityMultiplier;

    /// <summary>Copies required to advance to next rarity.</summary>
    public int CopiesNeededForUpgrade()
    {
        switch (rarity)
        {
            case CardRarity.Common:    return 5;
            case CardRarity.Rare:      return 10;
            case CardRarity.Epic:      return 20;
            case CardRarity.Legendary: return int.MaxValue; // already max
            default:                   return int.MaxValue;
        }
    }

    public bool CanUpgrade() => rarity < CardRarity.Legendary && copies >= CopiesNeededForUpgrade();

    public int UpgradeCostCoins()
    {
        switch (rarity)
        {
            case CardRarity.Common: return 200;
            case CardRarity.Rare:   return 500;
            case CardRarity.Epic:   return 1500;
            default:                return 0;
        }
    }

    /// <summary>Display string for rarity.</summary>
    public string RarityName => rarity.ToString();

    /// <summary>
    /// Glow color per rarity:
    ///   Common    = silver/white
    ///   Rare      = gold
    ///   Epic      = purple
    ///   Legendary = red/diamond
    /// </summary>
    public Color GlowColor()
    {
        switch (rarity)
        {
            case CardRarity.Common:    return new Color(0.80f, 0.80f, 0.80f); // silver
            case CardRarity.Rare:      return new Color(1.00f, 0.84f, 0.00f); // gold
            case CardRarity.Epic:      return new Color(0.62f, 0.13f, 0.94f); // purple
            case CardRarity.Legendary: return new Color(0.90f, 0.10f, 0.10f); // red
            default:                   return Color.white;
        }
    }
}

[Serializable]
public class SkillCardData
{
    public string      skillId;             // "freeze", "reverse", …
    public CardRarity  rarity    = CardRarity.Common;
    public int         level     = 1;       // 1–15
    public int         copies    = 0;
    public bool        isInActiveDeck = false;
    public long        lastUpgradeTimestamp;

    // Skill stats
    public float baseDuration = 3.0f;  // seconds the skill lasts
    public float basePower    = 1.0f;  // generic power multiplier

    public float RarityMultiplier => 1f + (int)rarity * 0.10f;
    public float Duration => baseDuration * RarityMultiplier;
    public float Power    => basePower    * RarityMultiplier;

    public int CopiesNeededForUpgrade()
    {
        switch (rarity)
        {
            case CardRarity.Common:    return 5;
            case CardRarity.Rare:      return 10;
            case CardRarity.Epic:      return 20;
            case CardRarity.Legendary: return int.MaxValue;
            default:                   return int.MaxValue;
        }
    }

    public bool CanUpgrade() => rarity < CardRarity.Legendary && copies >= CopiesNeededForUpgrade();

    public int UpgradeCostCoins()
    {
        switch (rarity)
        {
            case CardRarity.Common: return 150;
            case CardRarity.Rare:   return 400;
            case CardRarity.Epic:   return 1200;
            default:                return 0;
        }
    }

    public string RarityName => rarity.ToString();

    /// <summary>
    /// Glow color is category-driven at Common, then rarity-driven at Rare+.
    /// (Common skill shows category color; higher rarities show rarity ring.)
    /// </summary>
    public Color GlowColor(SkillCategory cat)
    {
        if (rarity == CardRarity.Common)
        {
            switch (cat)
            {
                case SkillCategory.Offensive: return new Color(1f, 0.2f, 0.2f); // red
                case SkillCategory.Defensive: return new Color(0.2f, 0.5f, 1f); // blue
                case SkillCategory.Economic:  return new Color(1f, 0.85f, 0.0f); // yellow
            }
        }
        // Rarity color ring overrides category for Rare+
        switch (rarity)
        {
            case CardRarity.Rare:      return new Color(1.00f, 0.84f, 0.00f);
            case CardRarity.Epic:      return new Color(0.62f, 0.13f, 0.94f);
            case CardRarity.Legendary: return new Color(0.90f, 0.10f, 0.10f);
            default:                   return Color.white;
        }
    }
}

// ─── Static Definitions ───────────────────────────────────────────────────────

[Serializable]
public struct CardDefinition
{
    public string id;
    public string displayName;
    public string description;
    public string portraitKey;       // used to load sprite from Resources
    public SkillCategory category;   // only relevant for skills
}

public static class CardDefinitions
{
    // ── Characters (10) ───────────────────────────────────────────────────────
    public static readonly CardDefinition[] Characters = new CardDefinition[]
    {
        new CardDefinition { id = "agent_zero", displayName = "Agent Zero",  portraitKey = "Characters/agent_zero",  description = "Elite tactical operative." },
        new CardDefinition { id = "blaze",      displayName = "Blaze",       portraitKey = "Characters/blaze",       description = "Fastest runner in the vault." },
        new CardDefinition { id = "knox",       displayName = "Knox",        portraitKey = "Characters/knox",        description = "Tank. Takes hits, keeps running." },
        new CardDefinition { id = "jade",       displayName = "Jade",        portraitKey = "Characters/jade",        description = "Agile serpent, razor reflexes." },
        new CardDefinition { id = "cipher",     displayName = "Cipher",      portraitKey = "Characters/cipher",      description = "Hacker. Bends reality." },
        new CardDefinition { id = "ghost",      displayName = "Ghost",       portraitKey = "Characters/ghost",       description = "Ethereal. Phasing through obstacles." },
        new CardDefinition { id = "nova",       displayName = "Nova",        portraitKey = "Characters/nova",        description = "Energy burst specialist." },
        new CardDefinition { id = "pulse",      displayName = "Pulse",       portraitKey = "Characters/pulse",       description = "Tech runner. Built-in sensors." },
        new CardDefinition { id = "eclipse",    displayName = "Eclipse",     portraitKey = "Characters/eclipse",     description = "Dark force. Shadow step mastery." },
        new CardDefinition { id = "phoenix",    displayName = "Phoenix",     portraitKey = "Characters/phoenix",     description = "Rises from failure. Comeback king." },
    };

    // ── Skills (12) ───────────────────────────────────────────────────────────
    public static readonly CardDefinition[] Skills = new CardDefinition[]
    {
        // Offensive (red)
        new CardDefinition { id = "freeze",        displayName = "Freeze",       category = SkillCategory.Offensive, portraitKey = "Skills/freeze",       description = "Enemy slows for 3s." },
        new CardDefinition { id = "reverse",       displayName = "Reverse",      category = SkillCategory.Offensive, portraitKey = "Skills/reverse",      description = "Enemy controls invert 2s." },
        new CardDefinition { id = "shrink",        displayName = "Shrink",       category = SkillCategory.Offensive, portraitKey = "Skills/shrink",       description = "Enemy shrinks 4s." },
        new CardDefinition { id = "obstacle",      displayName = "Obstacle",     category = SkillCategory.Offensive, portraitKey = "Skills/obstacle",     description = "Spawn obstacle for enemy." },
        // Defensive (blue)
        new CardDefinition { id = "shield",        displayName = "Shield",       category = SkillCategory.Defensive, portraitKey = "Skills/shield",       description = "One free hit 5s." },
        new CardDefinition { id = "ghost_skill",   displayName = "Ghost",        category = SkillCategory.Defensive, portraitKey = "Skills/ghost_skill",  description = "Invulnerable 3s." },
        new CardDefinition { id = "deflect",       displayName = "Deflect",      category = SkillCategory.Defensive, portraitKey = "Skills/deflect",      description = "Bounce obstacles 4s." },
        new CardDefinition { id = "slowmo",        displayName = "Slow-Mo",      category = SkillCategory.Defensive, portraitKey = "Skills/slowmo",       description = "Time slows to 0.5× for 4s." },
        // Economic (yellow)
        new CardDefinition { id = "magnet",        displayName = "Magnet",       category = SkillCategory.Economic,  portraitKey = "Skills/magnet",       description = "Auto-loot 5s." },
        new CardDefinition { id = "double_loot",   displayName = "Double Loot",  category = SkillCategory.Economic,  portraitKey = "Skills/double_loot",  description = "2× coins 5s." },
        new CardDefinition { id = "steal",         displayName = "Steal",        category = SkillCategory.Economic,  portraitKey = "Skills/steal",        description = "Take 100 coins from enemy." },
        new CardDefinition { id = "vault_key",     displayName = "Vault Key",    category = SkillCategory.Economic,  portraitKey = "Skills/vault_key",    description = "Open chest immediately." },
    };

    // ── Category color (static) ───────────────────────────────────────────────
    public static Color CategoryColor(SkillCategory cat)
    {
        switch (cat)
        {
            case SkillCategory.Offensive: return new Color(1f, 0.22f, 0.22f);
            case SkillCategory.Defensive: return new Color(0.22f, 0.55f, 1f);
            case SkillCategory.Economic:  return new Color(1f,  0.87f, 0.0f);
            default: return Color.white;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    public static CardDefinition? FindCharacter(string id)
    {
        foreach (var def in Characters)
            if (def.id == id) return def;
        return null;
    }

    public static CardDefinition? FindSkill(string id)
    {
        foreach (var def in Skills)
            if (def.id == id) return def;
        return null;
    }

    public static SkillCategory GetSkillCategory(string skillId)
    {
        foreach (var def in Skills)
            if (def.id == skillId) return def.category;
        return SkillCategory.Offensive;
    }
}
