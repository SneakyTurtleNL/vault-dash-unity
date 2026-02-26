/**
 * Vault Dash — Firebase Cloud Functions entry point
 *
 * Functions:
 *   - initSeason / initSeasonCallable  → season bootstrapping
 *   - validatePurchase                 → Google Play IAP validation
 *   - claimSeasonReward                → award gems at season end
 *   - dailyCleanup                     → scheduled nonce + rate-limit cleanup
 *   - updateLeaderboard                → player score submission
 */

const admin     = require('firebase-admin');
const functions = require('firebase-functions'); // eslint-disable-line no-unused-vars

// Initialize Firebase Admin once (all modules share this instance)
admin.initializeApp();

// --- Import modules ---
const initSeason        = require('./initSeason');
const validatePurchase  = require('./validatePurchase');
const claimSeasonReward = require('./claimSeasonReward');
const dailyCleanup      = require('./dailyCleanup');
const updateLeaderboard = require('./updateLeaderboard');

// --- Export all Cloud Functions ---
module.exports = {
  ...initSeason,         // initSeason, initSeasonCallable
  ...validatePurchase,   // validatePurchase
  ...claimSeasonReward,  // claimSeasonReward
  ...dailyCleanup,       // dailyCleanup
  ...updateLeaderboard,  // updateLeaderboard
};
