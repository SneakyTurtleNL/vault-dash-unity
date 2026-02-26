/**
 * Cloud Function: dailyCleanup
 *
 * Scheduled daily at 03:00 UTC.
 * Removes:
 *   - Expired IAP nonces (iap_nonces where expiresAt < now)
 *   - Old rate-limit entries (rate_limits where resetAt < 24h ago)
 *   - Stale leaderboard entries for deleted users (optional, soft skip)
 *
 * Firestore delete batches are capped at 500 ops per batch.
 */

const functions = require('firebase-functions');
const admin     = require('firebase-admin');

async function deleteBatch(db, query) {
  const snap = await query.get();
  if (snap.empty) return 0;

  let deleted = 0;
  const batch  = db.batch();
  snap.docs.forEach((doc) => {
    batch.delete(doc.ref);
    deleted++;
  });
  await batch.commit();
  return deleted;
}

const dailyCleanup = functions.pubsub
  .schedule('0 3 * * *')       // every day at 03:00 UTC
  .timeZone('UTC')
  .onRun(async (_context) => {
    const db  = admin.firestore();
    const now = admin.firestore.Timestamp.now();

    console.log('🧹 dailyCleanup started at', new Date().toISOString());
    let totalDeleted = 0;

    // 1. Expired IAP nonces
    try {
      const expiredNonces = db.collection('iap_nonces')
        .where('expiresAt', '<', now)
        .limit(500);
      const n = await deleteBatch(db, expiredNonces);
      console.log(`  Deleted ${n} expired IAP nonces`);
      totalDeleted += n;
    } catch (e) {
      console.error('  ❌ Failed to clean iap_nonces:', e.message);
    }

    // 2. Old rate-limit entries (older than 24 h)
    try {
      const cutoff = admin.firestore.Timestamp.fromMillis(Date.now() - 24 * 60 * 60 * 1000);
      const oldRateLimits = db.collection('rate_limits')
        .where('resetAt', '<', cutoff)
        .limit(500);
      const n = await deleteBatch(db, oldRateLimits);
      console.log(`  Deleted ${n} old rate-limit entries`);
      totalDeleted += n;
    } catch (e) {
      console.error('  ❌ Failed to clean rate_limits:', e.message);
    }

    // 3. Write cleanup log
    try {
      await db.collection('system_logs').add({
        event:        'dailyCleanup',
        totalDeleted,
        runAt:        now,
      });
    } catch (e) {
      console.warn('  ⚠️  Could not write cleanup log:', e.message);
    }

    console.log(`✅ dailyCleanup complete. Total deleted: ${totalDeleted}`);
    return null;
  });

module.exports = { dailyCleanup };
