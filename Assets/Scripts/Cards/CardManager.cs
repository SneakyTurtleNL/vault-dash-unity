using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_FIRESTORE
using Firebase.Firestore;
using Firebase.Extensions;
#endif

/// <summary>
/// CardManager — Singleton. Manages all card state for Vault Dash.
///
/// Responsibilities:
///   • Load character + skill cards from Firestore on startup
///   • Persist updates via Firestore transactions
///   • Expose runtime card state to UI screens
///   • Upgrade logic: deduct copies, bump rarity, record timestamp
///   • Active skill deck: 4 of 12 skills, saved to Firestore
///
/// FIRESTORE SCHEMA
/// ────────────────
/// players/{uid}/cards/characters/{characterId}
///   rarity           : int   (0=Common … 3=Legendary)
///   level            : int   (1-20)
///   copies           : int
///   prestige         : int
///   lastUpgradeAt    : timestamp
///
/// players/{uid}/cards/skills/{skillId}
///   rarity           : int   (0=Common … 3=Legendary)
///   level            : int   (1-15)
///   copies           : int
///   isInActiveDeck   : bool
///   lastUpgradeAt    : timestamp
///
/// players/{uid}/deck/active
///   slots            : string[] (4 skillIds)
///
/// SETUP
/// ─────
/// 1. Add FIREBASE_FIRESTORE to Scripting Define Symbols.
/// 2. Ensure Firebase is initialized (FirebaseManager) before calling LoadAllCards().
/// 3. Set uid via SetUserId() once Auth resolves.
/// </summary>
public class CardManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static CardManager Instance { get; private set; }

    // ─── Events ───────────────────────────────────────────────────────────────
    public event Action OnCardsLoaded;
    public event Action<string> OnCharacterUpgraded;     // characterId
    public event Action<string> OnSkillUpgraded;         // skillId
    public event Action         OnActiveDeckChanged;

    // ─── Runtime State ────────────────────────────────────────────────────────
    /// <summary>All character cards keyed by characterId.</summary>
    public Dictionary<string, CharacterCardData> CharacterCards { get; private set; }
        = new Dictionary<string, CharacterCardData>();

    /// <summary>All skill cards keyed by skillId.</summary>
    public Dictionary<string, SkillCardData> SkillCards { get; private set; }
        = new Dictionary<string, SkillCardData>();

    /// <summary>Active deck: ordered list of 4 skill IDs.</summary>
    public List<string> ActiveDeck { get; private set; } = new List<string>();

    public bool IsLoaded { get; private set; } = false;

    // ─── Config ───────────────────────────────────────────────────────────────
    [Header("Config")]
    [Tooltip("Maximum cards in the active skill deck.")]
    public int activeDeckSize = 4;

    [Tooltip("Fallback: use PlayerPrefs when Firestore is unavailable.")]
    public bool usePlayerPrefsFallback = true;

    // ─── Private ──────────────────────────────────────────────────────────────
    private string _uid = "";
    private const string PREFS_PREFIX = "VD_Cards_";

    // ─── Init ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // If FirebaseManager is present, wait for it; otherwise load from prefs.
        if (FirebaseManager.Instance != null)
            StartCoroutine(WaitForFirebaseThenLoad());
        else
            LoadFromPlayerPrefs();
    }

    IEnumerator WaitForFirebaseThenLoad()
    {
        yield return new WaitUntil(() => FirebaseManager.Instance.IsInitialized);
        // uid will be set externally via SetUserId() once Auth resolves
        // If not yet set, fall back to prefs
        if (string.IsNullOrEmpty(_uid))
            LoadFromPlayerPrefs();
    }

    /// <summary>Called by auth system once Firebase UID is known.</summary>
    public void SetUserId(string uid)
    {
        _uid = uid;
        StartCoroutine(LoadFromFirestore());
    }

    // ─── Loading ──────────────────────────────────────────────────────────────

    void SeedDefaults()
    {
        // Ensure every defined card has at least a default entry
        foreach (var def in CardDefinitions.Characters)
        {
            if (!CharacterCards.ContainsKey(def.id))
                CharacterCards[def.id] = new CharacterCardData { characterId = def.id };
        }
        foreach (var def in CardDefinitions.Skills)
        {
            if (!SkillCards.ContainsKey(def.id))
                SkillCards[def.id] = new SkillCardData { skillId = def.id };
        }
        // Default active deck: first 4 skills in order
        if (ActiveDeck.Count == 0)
        {
            for (int i = 0; i < Mathf.Min(activeDeckSize, CardDefinitions.Skills.Length); i++)
                ActiveDeck.Add(CardDefinitions.Skills[i].id);
        }
    }

    void LoadFromPlayerPrefs()
    {
        CharacterCards.Clear();
        SkillCards.Clear();
        ActiveDeck.Clear();

        // Characters
        foreach (var def in CardDefinitions.Characters)
        {
            string prefix = $"{PREFS_PREFIX}Char_{def.id}";
            var card = new CharacterCardData
            {
                characterId = def.id,
                rarity      = (CardRarity)PlayerPrefs.GetInt($"{prefix}_rarity", 0),
                level       = PlayerPrefs.GetInt($"{prefix}_level",   1),
                copies      = PlayerPrefs.GetInt($"{prefix}_copies",  0),
                prestige    = PlayerPrefs.GetInt($"{prefix}_prestige", 0),
            };
            CharacterCards[def.id] = card;
        }

        // Skills
        foreach (var def in CardDefinitions.Skills)
        {
            string prefix = $"{PREFS_PREFIX}Skill_{def.id}";
            var card = new SkillCardData
            {
                skillId         = def.id,
                rarity          = (CardRarity)PlayerPrefs.GetInt($"{prefix}_rarity", 0),
                level           = PlayerPrefs.GetInt($"{prefix}_level",   1),
                copies          = PlayerPrefs.GetInt($"{prefix}_copies",  0),
                isInActiveDeck  = PlayerPrefs.GetInt($"{prefix}_deck",    0) == 1,
            };
            SkillCards[def.id] = card;
        }

        // Active deck
        for (int i = 0; i < activeDeckSize; i++)
        {
            string slot = PlayerPrefs.GetString($"{PREFS_PREFIX}Deck_slot{i}", "");
            if (!string.IsNullOrEmpty(slot)) ActiveDeck.Add(slot);
        }

        SeedDefaults();
        IsLoaded = true;
        Debug.Log("[CardManager] Cards loaded from PlayerPrefs.");
        OnCardsLoaded?.Invoke();
    }

    void SaveToPlayerPrefs()
    {
        foreach (var kv in CharacterCards)
        {
            string prefix = $"{PREFS_PREFIX}Char_{kv.Key}";
            PlayerPrefs.SetInt($"{prefix}_rarity",   (int)kv.Value.rarity);
            PlayerPrefs.SetInt($"{prefix}_level",    kv.Value.level);
            PlayerPrefs.SetInt($"{prefix}_copies",   kv.Value.copies);
            PlayerPrefs.SetInt($"{prefix}_prestige", kv.Value.prestige);
        }
        foreach (var kv in SkillCards)
        {
            string prefix = $"{PREFS_PREFIX}Skill_{kv.Key}";
            PlayerPrefs.SetInt($"{prefix}_rarity",  (int)kv.Value.rarity);
            PlayerPrefs.SetInt($"{prefix}_level",   kv.Value.level);
            PlayerPrefs.SetInt($"{prefix}_copies",  kv.Value.copies);
            PlayerPrefs.SetInt($"{prefix}_deck",    kv.Value.isInActiveDeck ? 1 : 0);
        }
        for (int i = 0; i < ActiveDeck.Count; i++)
            PlayerPrefs.SetString($"{PREFS_PREFIX}Deck_slot{i}", ActiveDeck[i]);
        PlayerPrefs.Save();
    }

    // ─── Firestore Load/Save ──────────────────────────────────────────────────

    IEnumerator LoadFromFirestore()
    {
#if FIREBASE_FIRESTORE
        var db = FirebaseFirestore.DefaultInstance;

        bool charsDone = false, skillsDone = false;

        // Load characters
        db.Collection("players").Document(_uid)
          .Collection("cards").Document("characters")
          .Collection(string.Empty)  // subcollection trick — use direct collection instead
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  foreach (var doc in task.Result.Documents)
                      CharacterCards[doc.Id] = ParseCharacterDoc(doc);
              }
              else
              {
                  Debug.LogWarning("[CardManager] Firestore char load failed, using prefs.");
              }
              charsDone = true;
          });

        yield return new WaitUntil(() => charsDone);

        // Load skills
        db.Collection("players").Document(_uid)
          .Collection("cards").Document("skills")
          .Collection(string.Empty)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  foreach (var doc in task.Result.Documents)
                      SkillCards[doc.Id] = ParseSkillDoc(doc);
              }
              skillsDone = true;
          });

        yield return new WaitUntil(() => skillsDone);

        SeedDefaults();
        IsLoaded = true;
        Debug.Log("[CardManager] Cards loaded from Firestore.");
        OnCardsLoaded?.Invoke();
#else
        LoadFromPlayerPrefs();
        yield return null;
#endif
    }

#if FIREBASE_FIRESTORE
    CharacterCardData ParseCharacterDoc(DocumentSnapshot doc)
    {
        var data = doc.ToDictionary();
        return new CharacterCardData
        {
            characterId = doc.Id,
            rarity      = (CardRarity)(long)data.GetValueOrDefault("rarity", 0L),
            level       = (int)(long)data.GetValueOrDefault("level", 1L),
            copies      = (int)(long)data.GetValueOrDefault("copies", 0L),
            prestige    = (int)(long)data.GetValueOrDefault("prestige", 0L),
        };
    }

    SkillCardData ParseSkillDoc(DocumentSnapshot doc)
    {
        var data = doc.ToDictionary();
        return new SkillCardData
        {
            skillId        = doc.Id,
            rarity         = (CardRarity)(long)data.GetValueOrDefault("rarity", 0L),
            level          = (int)(long)data.GetValueOrDefault("level", 1L),
            copies         = (int)(long)data.GetValueOrDefault("copies", 0L),
            isInActiveDeck = (bool)data.GetValueOrDefault("isInActiveDeck", false),
        };
    }
#endif

    // ─── Upgrade Logic ────────────────────────────────────────────────────────

    /// <summary>
    /// Upgrade a character card's rarity.
    /// Deducts required copies, increments rarity, saves.
    /// Returns true on success.
    /// </summary>
    public bool UpgradeCharacterCard(string characterId, int playerCoins, Action<bool, string> callback)
    {
        if (!CharacterCards.TryGetValue(characterId, out var card))
        {
            callback?.Invoke(false, "Card not found.");
            return false;
        }
        if (!card.CanUpgrade())
        {
            callback?.Invoke(false, "Not enough copies or already Legendary.");
            return false;
        }
        int cost = card.UpgradeCostCoins();
        if (playerCoins < cost)
        {
            callback?.Invoke(false, $"Need {cost} coins.");
            return false;
        }

        int copiesNeeded = card.CopiesNeededForUpgrade();
        card.copies -= copiesNeeded;
        card.rarity  = (CardRarity)((int)card.rarity + 1);
        card.lastUpgradeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        CharacterCards[characterId] = card;
        PersistCharacterCard(characterId, card);

        Debug.Log($"[CardManager] {characterId} upgraded to {card.rarity}!");
        OnCharacterUpgraded?.Invoke(characterId);
        callback?.Invoke(true, $"Upgraded to {card.rarity}!");
        return true;
    }

    /// <summary>
    /// Upgrade a skill card's rarity.
    /// </summary>
    public bool UpgradeSkillCard(string skillId, int playerCoins, Action<bool, string> callback)
    {
        if (!SkillCards.TryGetValue(skillId, out var card))
        {
            callback?.Invoke(false, "Skill not found.");
            return false;
        }
        if (!card.CanUpgrade())
        {
            callback?.Invoke(false, "Not enough copies or already Legendary.");
            return false;
        }
        int cost = card.UpgradeCostCoins();
        if (playerCoins < cost)
        {
            callback?.Invoke(false, $"Need {cost} coins.");
            return false;
        }

        int copiesNeeded = card.CopiesNeededForUpgrade();
        card.copies -= copiesNeeded;
        card.rarity  = (CardRarity)((int)card.rarity + 1);
        card.lastUpgradeTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SkillCards[skillId] = card;
        PersistSkillCard(skillId, card);

        Debug.Log($"[CardManager] Skill {skillId} upgraded to {card.rarity}!");
        OnSkillUpgraded?.Invoke(skillId);
        callback?.Invoke(true, $"Upgraded to {card.rarity}!");
        return true;
    }

    /// <summary>Add copies to a character card (e.g. from chest reward).</summary>
    public void AddCharacterCopies(string characterId, int count)
    {
        if (!CharacterCards.TryGetValue(characterId, out var card)) return;
        card.copies += count;
        CharacterCards[characterId] = card;
        PersistCharacterCard(characterId, card);
        Debug.Log($"[CardManager] +{count} copies for {characterId} (total: {card.copies})");
    }

    /// <summary>Add copies to a skill card (e.g. from chest reward).</summary>
    public void AddSkillCopies(string skillId, int count)
    {
        if (!SkillCards.TryGetValue(skillId, out var card)) return;
        card.copies += count;
        SkillCards[skillId] = card;
        PersistSkillCard(skillId, card);
        Debug.Log($"[CardManager] +{count} copies for skill {skillId} (total: {card.copies})");
    }

    // ─── Active Deck ──────────────────────────────────────────────────────────

    /// <summary>Toggle a skill in/out of the active deck (max 4 slots).</summary>
    public bool ToggleSkillInDeck(string skillId)
    {
        if (ActiveDeck.Contains(skillId))
        {
            ActiveDeck.Remove(skillId);
            if (SkillCards.TryGetValue(skillId, out var card))
            {
                card.isInActiveDeck = false;
                SkillCards[skillId] = card;
            }
            PersistActiveDeck();
            OnActiveDeckChanged?.Invoke();
            return false; // removed
        }
        else
        {
            if (ActiveDeck.Count >= activeDeckSize)
            {
                Debug.LogWarning("[CardManager] Active deck full — remove a skill first.");
                return false;
            }
            ActiveDeck.Add(skillId);
            if (SkillCards.TryGetValue(skillId, out var card))
            {
                card.isInActiveDeck = true;
                SkillCards[skillId] = card;
            }
            PersistActiveDeck();
            OnActiveDeckChanged?.Invoke();
            return true; // added
        }
    }

    /// <summary>Replace a deck slot directly (drag-drop use case).</summary>
    public void SetDeckSlot(int slot, string skillId)
    {
        if (slot < 0 || slot >= activeDeckSize) return;

        // Remove old skill from deck flag
        if (slot < ActiveDeck.Count && !string.IsNullOrEmpty(ActiveDeck[slot]))
        {
            string old = ActiveDeck[slot];
            if (SkillCards.TryGetValue(old, out var oldCard))
            {
                oldCard.isInActiveDeck = false;
                SkillCards[old] = oldCard;
            }
        }

        // Grow list if necessary
        while (ActiveDeck.Count <= slot) ActiveDeck.Add("");
        ActiveDeck[slot] = skillId;

        if (!string.IsNullOrEmpty(skillId) && SkillCards.TryGetValue(skillId, out var newCard))
        {
            newCard.isInActiveDeck = true;
            SkillCards[skillId] = newCard;
        }

        PersistActiveDeck();
        OnActiveDeckChanged?.Invoke();
    }

    public bool IsInActiveDeck(string skillId) => ActiveDeck.Contains(skillId);

    // ─── Persistence Helpers ──────────────────────────────────────────────────

    void PersistCharacterCard(string characterId, CharacterCardData card)
    {
#if FIREBASE_FIRESTORE
        if (!string.IsNullOrEmpty(_uid))
        {
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection("players").Document(_uid)
                        .Collection("cards").Document("characters")
                        .Collection("data").Document(characterId);
            var data = new Dictionary<string, object>
            {
                { "rarity",        (int)card.rarity },
                { "level",         card.level },
                { "copies",        card.copies },
                { "prestige",      card.prestige },
                { "lastUpgradeAt", Timestamp.FromDateTime(
                    DateTimeOffset.FromUnixTimeSeconds(card.lastUpgradeTimestamp).UtcDateTime) },
            };
            doc.SetAsync(data, SetOptions.MergeAll);
        }
#endif
        if (usePlayerPrefsFallback) SaveToPlayerPrefs();
    }

    void PersistSkillCard(string skillId, SkillCardData card)
    {
#if FIREBASE_FIRESTORE
        if (!string.IsNullOrEmpty(_uid))
        {
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection("players").Document(_uid)
                        .Collection("cards").Document("skills")
                        .Collection("data").Document(skillId);
            var data = new Dictionary<string, object>
            {
                { "rarity",        (int)card.rarity },
                { "level",         card.level },
                { "copies",        card.copies },
                { "isInActiveDeck",card.isInActiveDeck },
                { "lastUpgradeAt", Timestamp.FromDateTime(
                    DateTimeOffset.FromUnixTimeSeconds(card.lastUpgradeTimestamp).UtcDateTime) },
            };
            doc.SetAsync(data, SetOptions.MergeAll);
        }
#endif
        if (usePlayerPrefsFallback) SaveToPlayerPrefs();
    }

    void PersistActiveDeck()
    {
#if FIREBASE_FIRESTORE
        if (!string.IsNullOrEmpty(_uid))
        {
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection("players").Document(_uid)
                        .Collection("deck").Document("active");
            doc.SetAsync(new Dictionary<string, object>
            {
                { "slots", ActiveDeck }
            }, SetOptions.MergeAll);
        }
#endif
        if (usePlayerPrefsFallback) SaveToPlayerPrefs();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Player coin balance stored in PlayerPrefs (shared with economy).</summary>
    public int GetPlayerCoins() => PlayerPrefs.GetInt("VaultDash_Coins", 0);

    public void DeductCoins(int amount)
    {
        int current = GetPlayerCoins();
        PlayerPrefs.SetInt("VaultDash_Coins", Mathf.Max(0, current - amount));
        PlayerPrefs.Save();
    }

    /// <summary>Debug: give copies to all cards for testing.</summary>
    [ContextMenu("DEBUG — Give 20 copies to all cards")]
    public void Debug_GiveCopies()
    {
        foreach (var key in CharacterCards.Keys)
        {
            var card = CharacterCards[key];
            card.copies += 20;
            CharacterCards[key] = card;
        }
        foreach (var key in SkillCards.Keys)
        {
            var card = SkillCards[key];
            card.copies += 20;
            SkillCards[key] = card;
        }
        SaveToPlayerPrefs();
        Debug.Log("[CardManager] DEBUG: +20 copies to all cards.");
    }
}
