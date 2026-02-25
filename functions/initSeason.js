/**
 * Cloud Function: initSeason
 * 
 * Initializes Firestore Season 1 for device testing.
 * Can be called via HTTP or CLI.
 * 
 * Usage:
 *   firebase functions:shell
 *   > initSeason()
 */

const functions = require('firebase-functions');
const admin = require('firebase-admin');

const initSeason = functions.https.onRequest(async (req, res) => {
  // Security: Only allow from internal IP or with auth token
  const allowedIps = ['127.0.0.1', '::1', '46.225.122.119'];
  const clientIp = req.ip;
  
  if (!allowedIps.includes(clientIp)) {
    console.warn(`Unauthorized initSeason call from ${clientIp}`);
    return res.status(403).json({ error: 'Forbidden' });
  }

  try {
    const db = admin.firestore();

    const seasonData = {
      // LoadingScreenTheme
      loadingScreenTheme: {
        character: 'agent_zero',
        eventText: 'Season 1: Rise of the Vault',
        primaryColor: '#2E7FD9',      // Blue
        accentColor: '#FFD700',       // Gold
        backgroundName: 'rookie_bg'
      },

      // Reward tiers (level → gems)
      rewards: {
        tier1: 50,
        tier2: 100,
        tier3: 200,
        tier4: 500,
        tier5: 1000
      },

      // Season metadata
      active: true,
      startDate: admin.firestore.Timestamp.now(),
      endDate: admin.firestore.Timestamp.fromDate(
        new Date(Date.now() + 30 * 24 * 60 * 60 * 1000)
      ),
      prestigeResets: false,
      trophyResetEnabled: true,
      createdAt: admin.firestore.Timestamp.now(),
      environment: 'production'
    };

    // Write to Firestore
    await db.doc('config/seasons/season_1').set(seasonData, { merge: true });

    console.log('✅ Season 1 initialized successfully');
    res.json({
      success: true,
      message: 'Season 1 initialized',
      data: seasonData
    });
  } catch (error) {
    console.error('❌ Error initializing season:', error);
    res.status(500).json({
      success: false,
      error: error.message
    });
  }
});

// Callable version (for Dart/Flutter)
const initSeasonCallable = functions.https.onCall(async (data, context) => {
  // Check authentication (optional: require admin claim)
  // if (!context.auth) throw new functions.https.HttpsError('unauthenticated', 'User must be authenticated');

  try {
    const db = admin.firestore();

    const seasonData = {
      loadingScreenTheme: {
        character: 'agent_zero',
        eventText: 'Season 1: Rise of the Vault',
        primaryColor: '#2E7FD9',
        accentColor: '#FFD700',
        backgroundName: 'rookie_bg'
      },
      rewards: {
        tier1: 50,
        tier2: 100,
        tier3: 200,
        tier4: 500,
        tier5: 1000
      },
      active: true,
      startDate: admin.firestore.Timestamp.now(),
      endDate: admin.firestore.Timestamp.fromDate(
        new Date(Date.now() + 30 * 24 * 60 * 60 * 1000)
      ),
      prestigeResets: false,
      trophyResetEnabled: true,
      createdAt: admin.firestore.Timestamp.now(),
      environment: 'production'
    };

    await db.doc('config/seasons/season_1').set(seasonData, { merge: true });

    return {
      success: true,
      message: 'Season 1 initialized via callable function'
    };
  } catch (error) {
    throw new functions.https.HttpsError('internal', error.message);
  }
});

module.exports = {
  initSeason,
  initSeasonCallable
};
