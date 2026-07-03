const express = require('express');
const router = express.Router();
const storeController = require('../controllers/storeController');
const { authenticateToken } = require('../middleware/auth');

router.get('/', authenticateToken, storeController.getStore);
router.post('/buy', authenticateToken, storeController.buyItem);
router.post('/luckyspin', authenticateToken, storeController.luckySpin);
router.post('/daily', authenticateToken, storeController.claimDaily);
router.get('/missions', authenticateToken, storeController.getMissions);

module.exports = router;

