# Firestore Card Schema — Vault Dash

Updated: 2026-02-21

---

## Collection Structure

```
players/
  {uid}/
    cards/
      characters/
        data/
          agent_zero/   ← DocumentSnapshot per character
            rarity:         int       0=Common 1=Rare 2=Epic 3=Legendary
            level:          int       1–20
            copies:         int       spare copies collected
            prestige:       int       0 = no prestige, 1+ = prestige rank
            lastUpgradeAt:  Timestamp Firebase server timestamp

          blaze/        ← same structure
          knox/
          jade/
          cipher/
          ghost/
          nova/
          pulse/
          eclipse/
          phoenix/

      skills/
        data/
          freeze/       ← DocumentSnapshot per skill
            rarity:         int       0–3
            level:          int       1–15
            copies:         int
            isInActiveDeck: bool
            lastUpgradeAt:  Timestamp

          reverse/
          shrink/
          obstacle/
          shield/
          ghost_skill/
          deflect/
          slowmo/
          magnet/
          double_loot/
          steal/
          vault_key/

    deck/
      active/
        slots: string[]   ordered list of 4 skillIds (active deck)
```

---

## Rarity Upgrade Thresholds

| From     | To       | Copies Needed | Coin Cost |
|----------|----------|---------------|-----------|
| Common   | Rare     | 5             | 200 (char) / 150 (skill) |
| Rare     | Epic     | 10            | 500 (char) / 400 (skill) |
| Epic     | Legendary| 20            | 1500 (char) / 1200 (skill) |
| Legendary| —        | MAX           | — |

---

## Stat Scaling per Rarity

```
Multiplier = 1.0 + (rarity_int × 0.10)

Common    → ×1.00  (base)
Rare      → ×1.10  (+10%)
Epic      → ×1.20  (+20%)
Legendary → ×1.30  (+30%)
```

**Characters** scale: Speed, Health, Damage
**Skills** scale: Duration, Power

---

## Backward Compatibility

Existing players with NO card documents get default state on first read:
- All cards start at `Common` rarity, `level 1`, `copies 0`
- `SeedDefaults()` in `CardManager.cs` fills in any missing entries

---

## Security Rules (Firestore)

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Users can only read/write their own card data
    match /players/{userId}/cards/{rest=**} {
      allow read, write: if request.auth.uid == userId;
    }
    match /players/{userId}/deck/{rest=**} {
      allow read, write: if request.auth.uid == userId;
    }
  }
}
```

---

## CardManager.cs Implementation Notes

- Upgrade is done **locally first** then written to Firestore (optimistic update)
- `SetAsync(data, SetOptions.MergeAll)` — preserves fields not in the write
- Firestore is optional: falls back to `PlayerPrefs` when SDK not installed
  (`usePlayerPrefsFallback = true`)
- `FIREBASE_FIRESTORE` scripting define enables all Firestore code paths
