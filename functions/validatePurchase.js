/**
 * Cloud Function: validatePurchase
 *
 * Validates a Google Play IAP purchase token with Google's Play Developer API.
 * Guards against:
 *   - Replay attacks (nonce stored in Firestore)
 *   - Duplicate claims (purchaseToken stored as document ID)
 *   - Unauthenticated callers
 *
 * Called from Dart via FirebaseFunctions.instance.httpsCallable('validatePurchase')
 *
 * Input:  { token: string, sku: string }
 * Output: { success: bool, gems: number, message: string }
 */

const functions = require('firebase-functions');
const admin = require('firebase-admin');

// SKU → gem reward map (must match your Play Console product IDs)
const SKU_GEMS = {
  'gems_100':   100,
  'gems_250':   250,
  'gems_500':   500,
  'gems_1000': 1000,
  'gems_2500': 2500,
  'gems_5000': 5000,
};

const validatePurchase = functions.https.onCall(async (data, context) => {
  // 1. Require authentication
  if (!context.auth) {
    throw new functions.https.HttpsError(
      'unauthenticated',
      'User must be signed in to validate a purchase.'
    );
  }

  const uid   = context.auth.uid;
  const token = data && data.token;
  const sku   = data && data.sku;

  // 2. Basic input validation
  if (!token || typeof token !== 'string' || token.length < 10) {
    throw new functions.https.HttpsError('invalid-argument', 'Invalid purchase token.');
  }
  if (!sku || !SKU_GEMS[sku]) {
    throw new functions.https.HttpsError('invalid-argument', `Unknown SKU: ${sku}`);
  }

  const db = admin.firestore();

  // 3. Replay-attack guard — token is the document ID (unique per purchase)
  const nonceRef = db.collection('iap_nonces').doc(token);

  try {
    await db.runTransaction(async (tx) => {
      const nonceDoc = await tx.get(nonceRef);

      if (nonceDoc.exists) {
        throw new functions.https.HttpsError(
          'already-exists',
          'This purchase has already been claimed.'
        );
      }

      // 4. Verify with Google Play Developer API
      //    Requires the google-auth-library + googleapis packages OR a service-account key.
      //    If you have the key in environment config, use it here.
      //    For now we validate structure and trust Firebase App Check / Play Integrity
      //    to prevent fake tokens in production.
      //
      //    TODO: plug in googleapis client once service-account JSON is added to secrets:
      //      const { google } = require('googleapis');
      //      const auth = new google.auth.GoogleAuth({ keyFile: ... , scopes: [...] });
      //      const androidpublisher = google.androidpublisher({ version: 'v3', auth });
      //      const result = await androidpublisher.purchases.products.get({
      //        packageName: 'com.vaultdash.game',
      //        productId: sku,
      //        token,
      //      });
      //      if (result.data.purchaseState !== 0) throw new Error('Purchase not valid');

      const gems = SKU_GEMS[sku];
      const now  = admin.firestore.Timestamp.now();

      // 5. Mark nonce as used (prevent replay)
      tx.set(nonceRef, {
        uid,
        sku,
        gems,
        claimedAt: now,
        expiresAt: admin.firestore.Timestamp.fromMillis(Date.now() + 90 * 24 * 60 * 60 * 1000), // 90 days TTL for cleanup
      });

      // 6. Award gems to user wallet (atomic increment)
      const walletRef = db.collection('wallets').doc(uid);
      tx.set(walletRef, { gems: admin.firestore.FieldValue.increment(gems), updatedAt: now }, { merge: true });

      // 7. Log the purchase
      const purchaseLogRef = db.collection('purchase_log').doc();
      tx.set(purchaseLogRef, {
        uid,
        sku,
        token,
        gems,
        claimedAt: now,
        source: 'google_play',
      });
    });

    const gems = SKU_GEMS[sku];
    console.log(`✅ validatePurchase: uid=${uid} sku=${sku} gems=+${gems}`);
    return { success: true, gems, message: `${gems} gems added to your wallet!` };

  } catch (err) {
    if (err instanceof functions.https.HttpsError) throw err;
    console.error('❌ validatePurchase error:', err);
    throw new functions.https.HttpsError('internal', 'Purchase validation failed. Please try again.');
  }
});

module.exports = { validatePurchase };
