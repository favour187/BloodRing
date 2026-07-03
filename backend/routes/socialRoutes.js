const express = require('express');
const router = express.Router();
const socialController = require('../controllers/socialController');
const { authenticateToken } = require('../middleware/auth');

router.get('/friends', authenticateToken, socialController.getFriends);
router.post('/friends/add', authenticateToken, socialController.addFriend);
router.post('/friends/block', authenticateToken, socialController.blockFriend);

router.post('/guilds/create', authenticateToken, socialController.createGuild);
router.get('/guilds', authenticateToken, socialController.getGuild);

router.post('/chat/send', authenticateToken, socialController.sendMessage);
router.get('/chat', authenticateToken, socialController.getMessages);

router.post('/share', authenticateToken, socialController.shareSocial);

module.exports = router;


