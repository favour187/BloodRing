const express = require('express');
const router = express.Router();
const authController = require('../controllers/authController');
const { authLimiter } = require('../middleware/rateLimiter');
const { authenticateToken } = require('../middleware/auth');

router.post('/register', authLimiter, authController.register);
router.post('/login', authLimiter, authController.login);
router.post('/guest', authLimiter, authController.guest);
router.post('/oauth', authLimiter, authController.oauth);
router.post('/link', authenticateToken, authController.linkAccount);

module.exports = router;


