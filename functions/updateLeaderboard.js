/**
 * Cloud Function: updateLeaderboard
 *
 * Called by the game client to submit / update a player's score.
 * - Upserts the leaderboard document for the authenticated user
 * - Updates global rank in a lightweight denormalized fashion
 *   (full ranking happens server-side via sorting; clients read ordered queries)
 * - Rate-limited: max 1 update per 60 s per user
 *
 * Input:  { trophies: number, displayName?: string, avatarId?: string }
 * Output: { success: bool, rank: number, message: string }
 */

const functions = require('firebase-functions');
const admin     = require('firebase-admin');

const RATE_LIMIT_SECONDS = 60; // minimum gap between score submissions

const updateLeaderboard = functions.https.onCall(async (data, context) => {
  // 1. Auth guard
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be signed in.');
  }

  const uid         = context.auth.uid;
  const trophies    = data && typeof data.trophies === 'number' ? data.trophies : null;
  const displayName = (data && typeof data.displayName === 'string') ? data.displayName.slice(0, 32) : null;
  const avatarId    = (data && typeof data.avatarId   === 'string') ? data.avatarId   : null;

  if (trophies === null || trophies < 0 || trophies > 1_000_000) {
    throw new functions.https.HttpsError('invalid-argument', 'trophies must be a non-negative number.');
  }

  const db  = admin.firestore();
  const now = Date.now();

  // 2. Rate-limit check
  const rlRef = db.collection('rate_limits').doc(`lb_${uid}`);
  const rlDoc = await rlRef.get();
  if (rlDoc.exists) {
    const lastUpdate = rlDoc.data().lastUpdate || 0;
    const elapsed    = (now - lastUpdate) / 1000;
    if (elapsed < RATE_LIMIT_SECONDS) {
      throw new functions.https.HttpsError(
        'resource-exhausted',
        `Please wait ${Math.ceil(RATE_LIMIT_SECONDS - elapsed)}s before updating again.`
      );
    }
  }

  // 3. Upsert leaderboard entry
  const lbRef = db.collection('leaderboard').doc(uid);

  const updatePayload = {
    uid,
    trophies,
    updatedAt: admin.firestore.Timestamp.now(),
  };
  if (displayName) updatePayload.displayName = displayName;
  if (avatarId)    updatePayload.avatarId    = avatarId;

  // Atomic batch: update leaderboard + refresh rate-limit
  const batch = db.batch();
  batch.set(lbRef, updatePayload, { merge: true });
  batch.set(rlRef, { lastUpdate: now, resetAt: admin.firestore.Timestamp.fromMillis(now + RATE_LIMIT_SECONDS * 1000) }, { merge: true });
  await batch.commit();

  // 4. Compute approximate rank (count of players with MORE trophies + 1)
  //    Using a count aggregation query (Firestore count() — available in firebase-admin v11+)
  let rank = 1;
  try {
    const countSnap = await db.collection('leaderboard')
      .where('trophies', '>', trophies)
      .count()
      .get();
    rank = (countSnap.data().count || 0) + 1;

    // Update player's stored rank
    await lbRef.update({ rank });

    // Update meta total
    const metaRef = db.doc('leaderboard/meta');
    await metaRef.set({ totalPlayers: admin.firestore.FieldValue.increment(0), updatedAt: admin.firestore.Timestamp.now() }, { merge: true });
    // Ensure totalPlayers is tracked (increment 0 if doc already exists, otherwise set)
    const metaSnap = await metaRef.get();
    if (!metaSnap.exists || !metaSnap.data().totalPlayers) {
      const totalSnap = await db.collection('leaderboard').where('uid', '!=', 'meta').count().get();
      await metaRef.set({ totalPlayers: totalSnap.data().count }, { merge: true });
    }
  } catch (e) {
    // count() might not be available in all emulator setups — degrade gracefully
    console.warn('⚠️  Rank calculation skipped:', e.message);
  }

  console.log(`✅ updateLeaderboard: uid=${uid} trophies=${trophies} rank=${rank}`);
  return { success: true, rank, message: `Leaderboard updated! You are rank #${rank}.` };
});

module.exports = { updateLeaderboard };
