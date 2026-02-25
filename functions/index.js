const admin = require('firebase-admin');
const functions = require('firebase-functions');

// Initialize Firebase Admin
admin.initializeApp();

// Import functions
const initSeason = require('./initSeason');

// Export all functions
module.exports = {
  ...initSeason
};
