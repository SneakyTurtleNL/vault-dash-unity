# Firestore Schema — Week 5 Additions

Generated: 2026-02-26
Sprint: Week 5 — Balance, Monetization, Auth & Technical Fixes

---

## Updated / New Collections

### players/{uid}  (existing, extended)

```
players/{uid}/
  trophies              : int
  prestigeLevel         : int
  currentTier           : string
  winStreak             : int         ← NEW (WinStreakService)
  longestWinStreak      : int         ← NEW
  lastWinTimestamp      : timestamp   ← NEW
  onboardingMatchesPlayed : int       ← NEW (OnboardingBotService)
  onboardingCompleted   : bool        ← NEW
```

### players/{uid}/dailyChallenges/{YYYY-MM-DD}  ← NEW (ChallengeSystem)

```
players/{uid}/dailyChallenges/{YYYY-MM-DD}/
  generatedAt : timestamp
  challenges  : array of {
    templateId      : string
    displayText     : string
    targetValue     : int
    currentProgress : int
    completed       : bool
    rewardClaimed   : bool
  }
```

### clans/{clanId}/messages/{messageId}  ← NEW (ClanChatManager)

```
clans/{clanId}/messages/{messageId}/
  authorId   : string
  authorName : string
  text       : string  (max 280 chars)
  timestamp  : timestamp
  deleted    : bool    (soft delete)
```

### clans/{clanId}/mutedMembers/{memberId}  ← NEW

```
clans/{clanId}/mutedMembers/{memberId}/
  mutedUntil : timestamp
  mutedBy    : string  (UID of officer who muted)
```

### ghostReplays/{replayId}  ← NEW (GhostMatchSystem)

```
ghostReplays/{replayId}/
  displayName : string
  level       : int
  difficulty  : string  (easy|medium|hard)
  totalFrames : int
  frameData   : array of {
    frame    : int
    distance : float
    hp       : float
  }
```

---

## Firestore Security Rules (additions)

```javascript
// players/{uid}/dailyChallenges — owner read/write only
match /players/{uid}/dailyChallenges/{dateKey} {
  allow read, write: if request.auth.uid == uid;
}

// clans/{clanId}/messages — members can read; write if not muted
match /clans/{clanId}/messages/{messageId} {
  allow read:  if isClanMember(clanId);
  allow create: if isClanMember(clanId) && !isMuted(clanId, request.auth.uid);
  allow update, delete: if request.auth.uid == resource.data.authorId
                        || isClanOfficer(clanId);
}

// ghostReplays — server read-only (Cloud Functions manage writes)
match /ghostReplays/{replayId} {
  allow read: if request.auth != null;
  allow write: if false;  // managed by Cloud Functions only
}
```

---

## IAP Cloud Function — validatePurchase  (already deployed)

```
Region: europe-west1
Name:   validatePurchase

Request:
  platform      : "android" | "ios"
  productId     : string  (e.g. "gems_100")
  receipt       : string  (raw JSON receipt from Unity IAP)
  purchaseToken : string  (platform token)
  uid           : string  (Firebase UID)

Response (success):
  valid            : true
  gemAmount        : int
  alreadyProcessed : bool

Response (failure):
  valid  : false
  reason : string
```

Fraud checks performed server-side:
1. **Replay attack**: purchaseToken stored in Firestore; duplicate → reject
2. **Receipt verification**: Google Play API / Apple StoreKit validation
3. **Amount mismatch**: server cross-checks productId → gem amount
4. **UID binding**: receipt must be linked to the authenticated user

---

## Product ID Mapping (updated — EconomyConfig.cs)

| Old Product ID | New Product ID | Gems   | Price   |
|----------------|---------------|--------|---------|
| gems_80        | gems_100      | 100    | €0.99   |
| gems_500       | gems_600      | 600    | €4.99   |
| gems_1200      | gems_1500     | 1500   | €9.99   |
| gems_6500      | gems_9000     | 9000   | €49.99  |

> ⚠️ Update IAP Catalog in Unity Editor to use new product IDs.
> Update Google Play Console product definitions to match.
