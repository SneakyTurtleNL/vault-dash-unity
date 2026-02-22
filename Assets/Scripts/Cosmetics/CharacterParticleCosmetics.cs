using UnityEngine;
using System.Collections.Generic;
using Firebase.Firestore;

/// <summary>
/// CharacterParticleCosmetics: Manage optional particle effects for characters.
/// 
/// Players can unlock/purchase particle effects (auras, weapon trails, footsteps, etc.)
/// that play alongside their character in gameplay and UI.
/// 
/// System is modular: each effect is a ParticleCosmetic prefab that can be:
/// - Earned from daily challenges / weekly missions
/// - Purchased with gems
/// - Unlocked via prestige tiers
/// 
/// Effects are applied at runtime via ParticleSystem instances.
/// </summary>

[System.Serializable]
public class ParticleCosmetic
{
    public string id;                           // "aura_blue", "trail_fire", etc.
    public string name;                         // Display name
    public string characterId;                  // Which character: "agent_zero", "blaze", etc. NULL = universal
    public string effectType;                   // "aura", "weapon_trail", "footstep", "spawn_burst", "levelup_burst"
    public string prefabPath;                   // Resources path: "Particles/Auras/aura_blue"
    public int gemsPrice;                       // Cost (0 = not for sale, earned only)
    public int unlockPrestigeTier;              // Prestige level requirement (0 = no requirement)
    public string description;                  // "Blue energy aura" — shown in shop
    public Color rarity;                        // UI color: common(gray), rare(blue), epic(purple), legendary(gold)
}

[System.Serializable]
public class UnlockedParticleCosmetic
{
    public string particleId;
    public bool isPurchased;                    // true = bought, false = earned
    public System.DateTime unlockedDate;
}

public class CharacterParticleCosmetics : MonoBehaviour
{
    // Singleton
    private static CharacterParticleCosmetics _instance;
    public static CharacterParticleCosmetics Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<CharacterParticleCosmetics>();
            if (_instance == null)
            {
                var go = new GameObject("CharacterParticleCosmetics");
                _instance = go.AddComponent<CharacterParticleCosmetics>();
            }
            return _instance;
        }
    }

    // Built-in catalog of available particle effects
    private List<ParticleCosmetic> _catalog = new List<ParticleCosmetic>();
    
    // Player's unlocked particles (from Firestore: players/{uid}/unlockedParticles/{particleId})
    private Dictionary<string, UnlockedParticleCosmetic> _unlockedParticles = new Dictionary<string, UnlockedParticleCosmetic>();
    
    // Active particles per character (in gameplay)
    private Dictionary<string, string> _activeParticlePerCharacter = new Dictionary<string, string>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeCatalog();
    }

    /// <summary>
    /// Initialize the catalog of available particle cosmetics.
    /// This is the "store" of all possible effects players can unlock.
    /// </summary>
    private void InitializeCatalog()
    {
        _catalog.Clear();

        // AURAS (universal for all characters)
        AddParticleCosmetic(new ParticleCosmetic
        {
            id = "aura_blue_energy",
            name = "Blue Energy Aura",
            characterId = null, // universal
            effectType = "aura",
            prefabPath = "Particles/Auras/aura_blue_energy",
            gemsPrice = 150,
            unlockPrestigeTier = 0,
            description = "Calm blue energy field around the character",
            rarity = new Color(0.3f, 0.6f, 1f) // blue
        });

        AddParticleCosmetic(new ParticleCosmetic
        {
            id = "aura_golden_light",
            name = "Golden Light Aura",
            characterId = null,
            effectType = "aura",
            prefabPath = "Particles/Auras/aura_golden_light",
            gemsPrice = 200,
            unlockPrestigeTier = 5,
            description = "Prestigious golden light — prestige tier 5 unlock",
            rarity = new Color(1f, 0.84f, 0f) // gold
        });

        AddParticleCosmetic(new ParticleCosmetic
        {
            id = "aura_purple_mystical",
            name = "Purple Mystical Aura",
            characterId = null,
            effectType = "aura",
            prefabPath = "Particles/Auras/aura_purple_mystical",
            gemsPrice = 200,
            unlockPrestigeTier = 0,
            description = "Mystical purple energy with mystique",
            rarity = new Color(0.8f, 0.3f, 1f) // purple
        });

        // WEAPON TRAILS (character-specific or universal)
        AddParticleCosmetic(new ParticleCosmetic
        {
            id = "trail_fire_blaze",
            name = "Fire Trail Effect",
            characterId = null, // universal
            effectType = "weapon_trail",
            prefabPath = "Particles/Trails/trail_fire",
            gemsPrice = 100,
            unlockPrestigeTier = 0,
            description = "Flaming trail behind the character during movement",
            rarity = new Color(1f, 0.5f, 0f) // orange
        });

        AddParticleCosmetic(new ParticleCosmetic
        {
            id = "trail_ice_cyan",
            name = "Ice Trail Effect",
            characterId = null,
            effectType = "weapon_trail",
            prefabPath = "Particles/Trails/trail_ice",
            gemsPrice = 100,
            unlockPrestigeTier = 0,
            description = "Icy cyan trail with frost particles",
            rarity = new Color(0.3f, 0.8f, 1f) // cyan
        });

        // FOOTSTEPS (optional sfx + visual)
        AddParticleCosmetic(new ParticleCosmetic
        {
            id = "footstep_stars",
            name = "Star Footsteps",
            characterId = null,
            effectType = "footstep",
            prefabPath = "Particles/Footsteps/footstep_stars",
            gemsPrice = 80,
            unlockPrestigeTier = 0,
            description = "Leave sparkly stars as you run",
            rarity = new Color(1f, 0.84f, 0f) // gold
        });

        // SPAWN BURST (character appear effect)
        AddParticleCosmetic(new ParticleCosmetic
        {
            id = "spawn_burst_cosmic",
            name = "Cosmic Spawn Burst",
            characterId = null,
            effectType = "spawn_burst",
            prefabPath = "Particles/Spawn/spawn_burst_cosmic",
            gemsPrice = 120,
            unlockPrestigeTier = 0,
            description = "Cosmic energy burst when entering the arena",
            rarity = new Color(0.4f, 0.1f, 0.8f) // cosmic purple
        });

        // LEVEL-UP BURST (prestige unlock celebration)
        AddParticleCosmetic(new ParticleCosmetic
        {
            id = "levelup_burst_legendary",
            name = "Legendary Level-Up Burst",
            characterId = null,
            effectType = "levelup_burst",
            prefabPath = "Particles/LevelUp/levelup_burst_legendary",
            gemsPrice = 0, // Earned only
            unlockPrestigeTier = 10,
            description = "Legendary particles when reaching prestige tier 10 — earned, not purchased",
            rarity = new Color(1f, 0.84f, 0f) // gold
        });
    }

    /// <summary>
    /// Add a particle cosmetic to the catalog.
    /// </summary>
    private void AddParticleCosmetic(ParticleCosmetic cosmetic)
    {
        _catalog.Add(cosmetic);
    }

    /// <summary>
    /// Load player's unlocked particles from Firestore.
    /// </summary>
    public async System.Threading.Tasks.Task LoadUnlockedParticles(string uid)
    {
        try
        {
            var db = FirebaseFirestore.DefaultInstance;
            var docRef = db.Collection("players").Document(uid).Collection("unlockedParticles");
            var query = await docRef.GetSnapshotAsync();

            _unlockedParticles.Clear();
            foreach (var doc in query.Documents)
            {
                var data = doc.ConvertTo<UnlockedParticleCosmetic>();
                _unlockedParticles[doc.Id] = data;
            }

            Debug.Log($"Loaded {_unlockedParticles.Count} unlocked particle effects for {uid}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load unlocked particles: {e.Message}");
        }
    }

    /// <summary>
    /// Get all available particle cosmetics from the catalog.
    /// </summary>
    public List<ParticleCosmetic> GetCatalog() => _catalog;

    /// <summary>
    /// Get catalog items that match a filter.
    /// </summary>
    public List<ParticleCosmetic> GetCatalogByType(string effectType)
    {
        return _catalog.FindAll(p => p.effectType == effectType);
    }

    /// <summary>
    /// Check if a particle is unlocked by the player.
    /// </summary>
    public bool IsUnlocked(string particleId)
    {
        return _unlockedParticles.ContainsKey(particleId);
    }

    /// <summary>
    /// Unlock a particle (e.g., from daily challenge, prestige unlock).
    /// Saves to Firestore: players/{uid}/unlockedParticles/{particleId}
    /// </summary>
    public async System.Threading.Tasks.Task UnlockParticle(string uid, string particleId, bool isPurchased = false)
    {
        try
        {
            var unlocked = new UnlockedParticleCosmetic
            {
                particleId = particleId,
                isPurchased = isPurchased,
                unlockedDate = System.DateTime.UtcNow
            };

            var db = FirebaseFirestore.DefaultInstance;
            var docRef = db.Collection("players").Document(uid).Collection("unlockedParticles").Document(particleId);
            await docRef.SetAsync(unlocked);

            _unlockedParticles[particleId] = unlocked;
            Debug.Log($"Unlocked particle: {particleId} (purchased: {isPurchased})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to unlock particle: {e.Message}");
        }
    }

    /// <summary>
    /// Activate a particle effect on a character in gameplay.
    /// The effect stays active until explicitly deactivated or character changes.
    /// </summary>
    public void SetActiveParticle(string characterId, string particleId)
    {
        if (!IsUnlocked(particleId))
        {
            Debug.LogWarning($"Particle {particleId} is not unlocked");
            return;
        }

        _activeParticlePerCharacter[characterId] = particleId;
        Debug.Log($"Activated particle {particleId} for character {characterId}");
    }

    /// <summary>
    /// Get the currently active particle for a character.
    /// </summary>
    public string GetActiveParticle(string characterId)
    {
        return _activeParticlePerCharacter.ContainsKey(characterId)
            ? _activeParticlePerCharacter[characterId]
            : null;
    }

    /// <summary>
    /// Deactivate particle effect for a character.
    /// </summary>
    public void DeactivateParticle(string characterId)
    {
        if (_activeParticlePerCharacter.ContainsKey(characterId))
            _activeParticlePerCharacter.Remove(characterId);
    }

    /// <summary>
    /// Instantiate and play a particle effect at a position.
    /// Used during gameplay to show effects.
    /// </summary>
    public ParticleSystem PlayParticleEffect(string particleId, Vector3 position)
    {
        var cosmetic = _catalog.Find(p => p.id == particleId);
        if (cosmetic == null)
        {
            Debug.LogWarning($"Particle cosmetic not found: {particleId}");
            return null;
        }

        try
        {
            var prefab = Resources.Load<ParticleSystem>(cosmetic.prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found at: {cosmetic.prefabPath}");
                return null;
            }

            var instance = Instantiate(prefab, position, Quaternion.identity);
            instance.Play();
            
            // Auto-destroy after effect ends
            Destroy(instance.gameObject, instance.main.duration);

            return instance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to play particle effect {particleId}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get a particle cosmetic by ID from the catalog.
    /// </summary>
    public ParticleCosmetic GetParticleCosmeticById(string particleId)
    {
        return _catalog.Find(p => p.id == particleId);
    }

    /// <summary>
    /// Get all unlocked particles (for profile/shop display).
    /// </summary>
    public List<UnlockedParticleCosmetic> GetUnlockedParticles()
    {
        return new List<UnlockedParticleCosmetic>(_unlockedParticles.Values);
    }
}
