const express = require('express');
const router = express.Router();
const matchController = require('../controllers/matchController');
const { authenticateToken } = require('../middleware/auth');

router.post('/matchmake', authenticateToken, matchController.matchmake);
router.post('/result', authenticateToken, matchController.submitResult);
router.post('/anticheat', authenticateToken, matchController.logViolation);
router.post('/cloudsave', authenticateToken, matchController.syncCloudSave);

module.exports = router;


