# Vault Dash — Season System Firestore Schema

> Version: 2026-02-21
> Season 1 starts: 2026-02-21  |  Duration: 30 days (configurable)
> Hard reset UTC time: 00:00 UTC on season end date

---

## config/currentSeason  (single document)

| Field         | Type      | Description                            |
|---------------|-----------|----------------------------------------|
| seasonId      | string    | e.g. `"season_1"`                      |
| seasonNumber  | int       | 1, 2, 3 …                              |
| name          | string    | "Neon Vault"                           |
| endDate       | Timestamp | UTC end of this season                 |
| durationDays  | int       | Configurable (default 30)              |

---

## config/seasons/{seasonId}  (one doc per season)

| Field                  | Type      | Description                                    |
|------------------------|-----------|------------------------------------------------|
| seasonId               | string    | "season_1"                                     |
| seasonNumber           | int       | 1                                              |
| name                   | string    | "Neon Vault"                                   |
| theme                  | string    | "neon" / "pirate" / "winter" etc.              |
| startDate              | Timestamp |                                                |
| endDate                | Timestamp |                                                |
| resetTimeUtc           | string    | "00:00" (HH:mm, configurable)                  |
| durationDays           | int       | Default 30                                     |
| rewardsDistributed     | bool      | Set true after Cloud Function runs             |
| hardResetDone          | bool      | Set true after trophy hard reset               |
| **cosmetic**           | map       |                                                |
| cosmetic.skinId        | string    | "neon_vault_skin"                              |
| cosmetic.skinName      | string    | "Neon Vault Operative"                         |
| cosmetic.description   | string    | "Exclusive Season 1 cosmetic"                  |
| cosmetic.gemCost       | int       | Gem purchase price (e.g. 500)                  |
| cosmetic.prestigeFree  | int       | Free at this prestige level (default 5)        |
| cosmetic.themeColor    | string    | Hex color "#4444FF"                            |
| cosmetic.iconUrl       | string    | Path/URL to skin preview sprite                |
| **arenaOverlay**       | map       |                                                |
| arenaOverlay.primary   | string    | "#4444FF"                                      |
| arenaOverlay.glow      | string    | "#6666FF"                                      |
| arenaOverlay.fog       | string    | "#000033"                                      |
| **powerupTheme**       | map       | Seasonal power-up name overrides               |
| powerupTheme.FREEZE    | string    | "Winter Freeze" (winter season etc.)           |

---

## players/{uid}  (existing doc — new fields added)

| Field                    | Type | Description                            |
|--------------------------|------|----------------------------------------|
| trophies                 | int  | Current trophies (HARD RESET → 0)      |
| peakTrophiesThisSeason   | int  | Tracked live; reset each season        |
| currentSeasonId          | string | Which season this peak belongs to   |

### players/{uid}/seasons/{seasonId}  (subcollection)

| Field               | Type      | Description                          |
|---------------------|-----------|--------------------------------------|
| peakTrophies        | int       | Season personal best                 |
| finalTier           | string    | Tier at season end ("Legend" etc.)   |
| finalPrestige       | int       | Prestige level at season end         |
| claimedSeasonReward | bool      | Firestore transaction guard          |
| gemReward           | int       | Gems earned (peakTrophies / 100, ≤500)|
| claimedAt           | Timestamp | When reward was claimed              |

---

## leaderboards/season/{seasonId}/top100/{uid}  (archived top 100)

| Field        | Type      | Description                   |
|--------------|-----------|-------------------------------|
| rank         | int       | Final rank in this season     |
| username     | string    |                               |
| trophies     | int       | Final trophy count            |
| tier         | string    | "Legend" etc.                 |
| prestigeLevel| int       |                               |
| archivedAt   | Timestamp | When archived                 |

---

## Season Reset Rules

1. **Trophy reset is HARD**: ALL players → 0 trophies at exact `resetTimeUtc` on `endDate`.
2. **Prestige NEVER resets** — it is permanent progression.
3. **Cards / cosmetics / gems NEVER reset**.
4. **Leaderboard archived** before reset (past seasons readable, can't climb them).
5. **Reward claimed once per player per season** — Firestore transaction in Cloud Function.
6. **Gem reward formula**: `floor(peakTrophies / 100)`, capped at **500 gems**.

---

## Season Reward Tiers (gem bonus on top of gem formula)

| Peak Trophies | Tier    | Gem Bonus     |
|---------------|---------|---------------|
| 4500+         | Legend  | +50 bonus gems|
| 3500–4499     | Master  | +25 bonus gems|
| 2000–3499     | Diamond | +10 bonus gems|
| < 2000        | —       | No bonus       |

---

## Season Cosmetic Rules

- Each season has **1 exclusive skin**.
- Available **free** to players who reach **Prestige 5** during that season.
- Available to purchase for **gems** any time during the season.
- After season ends: skin moves to **Season Archive** shop (purchasable with gems, higher cost).
- Archive cost: original gem cost × 1.5 (rounded up).
