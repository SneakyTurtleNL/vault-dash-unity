/**
 * Cloud Function: claimSeasonReward
 *
 * Awards gems to a player at season end based on their final rank.
 * Idempotent — a player can only claim each season once.
 *
 * Input:  { seasonId: string }  (uid comes from context.auth)
 * Output: { success: bool, gems: number, tier: string, message: string }
 */

const functions = require('firebase-functions');
const admin = require('firebase-admin');

// Rank thresholds → reward tiers
// Firestore: config/seasons/<seasonId>.rewards.tier1..tier5
const DEFAULT_REWARDS = {
  tier1: 50,
  tier2: 100,
  tier3: 200,
  tier4: 500,
  tier5: 1000,
};

/**
 * Determine tier based on player trophies and global leaderboard position.
 * Top 1%  → tier5 | Top 5% → tier4 | Top 20% → tier3 | Top 50% → tier2 | rest → tier1
 */
function getTier(rank, totalPlayers) {
  const pct = rank / Math.max(totalPlayers, 1);
  if (pct <= 0.01) return 'tier5';
  if (pct <= 0.05) return 'tier4';
  if (pct <= 0.20) return 'tier3';
  if (pct <= 0.50) return 'tier2';
  return 'tier1';
}

const claimSeasonReward = functions.https.onCall(async (data, context) => {
  // 1. Auth guard
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be signed in.');
  }

  const uid      = context.auth.uid;
  const seasonId = data && data.seasonId;

  if (!seasonId || typeof seasonId !== 'string') {
    throw new functions.https.HttpsError('invalid-argument', 'seasonId is required.');
  }

  const db = admin.firestore();

  // 2. Idempotency guard
  const claimRef = db.collection('season_claims').doc(`${seasonId}_${uid}`);
  const claimDoc = await claimRef.get();
  if (claimDoc.exists) {
    const d = claimDoc.data();
    return { success: true, alreadyClaimed: true, gems: d.gems, tier: d.tier, message: 'Reward already claimed.' };
  }

  // 3. Load season config
  const seasonDoc = await db.doc(`config/seasons/${seasonId}`).get();
  if (!seasonDoc.exists) {
    throw new functions.https.HttpsError('not-found', `Season ${seasonId} not found.`);
  }
  const seasonData = seasonDoc.data();
  const rewards    = seasonData.rewards || DEFAULT_REWARDS;

  // 4. Look up player's leaderboard rank
  const lbRef      = db.collection('leaderboard').doc(uid);
  const lbDoc      = await lbRef.get();
  const rank        = lbDoc.exists ? (lbDoc.data().rank || 9999) : 9999;

  // Get total players from leaderboard meta doc
  const metaDoc    = await db.doc('leaderboard/meta').get();
  const totalPlayers = metaDoc.exists ? (metaDoc.data().totalPlayers || 1) : 1;

  const tier = getTier(rank, totalPlayers);
  const gems = rewards[tier] || DEFAULT_REWARDS[tier] || 50;
  const now  = admin.firestore.Timestamp.now();

  // 5. Atomic: mark claim + award gems
  await db.runTransaction(async (tx) => {
    // Re-check idempotency inside transaction
    const claimSnap = await tx.get(claimRef);
    if (claimSnap.exists) return; // already claimed (race condition)

    tx.set(claimRef, { uid, seasonId, tier, gems, claimedAt: now, rank });

    const walletRef = db.collection('wallets').doc(uid);
    tx.set(walletRef, { gems: admin.firestore.FieldValue.increment(gems), updatedAt: now }, { merge: true });
  });

  console.log(`✅ claimSeasonReward: uid=${uid} season=${seasonId} tier=${tier} gems=+${gems} rank=${rank}/${totalPlayers}`);
  return {
    success: true,
    gems,
    tier,
    rank,
    message: `You placed in ${tier}! +${gems} gems rewarded.`,
  };
});

module.exports = { claimSeasonReward };
