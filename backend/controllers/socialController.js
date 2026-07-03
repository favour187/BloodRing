const db = require('../config/db');

// Get friends list with online status simulation
exports.getFriends = (req, res) => {
  const userId = req.user.id;
  const query = `
    SELECT u.id AS friendId, p.display_name AS displayName, p.rank_tier AS rankTier, f.status
    FROM friends f
    JOIN users u ON f.friend_id = u.id
    JOIN profiles p ON u.id = p.user_id
    WHERE f.user_id = ?
  `;
  db.all(query, [userId], (err, rows) => {
    if (err) return res.status(500).json({ error: 'Database error fetching friends.' });
    // Simulate online status (randomly true/false)
    const friends = rows.map(r => ({ ...r, isOnline: Math.random() > 0.3 }));
    return res.status(200).json({ friends });
  });
};

// Add friend
exports.addFriend = (req, res) => {
  const userId = req.user.id; const { friendUsername } = req.body;
  db.get(`SELECT id FROM users WHERE username = ?`, [friendUsername], (err, friend) => {
    if (err || !friend) return res.status(404).json({ error: 'User not found.' });
    if (friend.id === userId) return res.status(400).json({ error: 'Cannot add yourself.' });
    db.run(`INSERT INTO friends (user_id, friend_id, status) VALUES (?, ?, 'ACCEPTED')`, [userId, friend.id], function (errF) {
      if (errF) return res.status(500).json({ error: 'Already friends or database error.' });
      return res.status(200).json({ message: `Successfully added ${friendUsername} as friend.` });
    });
  });
};

// Block friend / user
exports.blockFriend = (req, res) => {
  const userId = req.user.id; const { friendId } = req.body;
  db.run(`UPDATE friends SET status = 'BLOCKED' WHERE user_id = ? AND friend_id = ?`, [userId, friendId], function (err) {
    if (err) return res.status(500).json({ error: 'Failed to block user.' });
    return res.status(200).json({ message: 'User blocked.' });
  });
};

// Create Guild
exports.createGuild = (req, res) => {
  const userId = req.user.id; const { guildName } = req.body;
  if (!guildName) return res.status(400).json({ error: 'Guild name required.' });
  db.run(`INSERT INTO guilds (name, leader_id, level, members_count) VALUES (?, ?, 1, 1)`, [guildName, userId], function (err) {
    if (err) return res.status(400).json({ error: 'Guild name already exists.' });
    const guildId = this.lastID;
    db.run(`INSERT INTO guild_members (guild_id, user_id, role) VALUES (?, ?, 'LEADER')`, [guildId, userId], (errM) => {
      return res.status(201).json({ message: `Guild ${guildName} created successfully!`, guildId, guildName });
    });
  });
};

// Get Guild Details
exports.getGuild = (req, res) => {
  const userId = req.user.id;
  db.get(`SELECT g.* FROM guild_members gm JOIN guilds g ON gm.guild_id = g.id WHERE gm.user_id = ?`, [userId], (err, guild) => {
    if (err || !guild) return res.status(404).json({ error: 'Not in a guild.' });
    db.all(`SELECT p.display_name AS displayName, p.rank_tier AS rankTier, gm.role FROM guild_members gm JOIN profiles p ON gm.user_id = p.user_id WHERE gm.guild_id = ?`, [guild.id], (errM, members) => {
      return res.status(200).json({ guild, members });
    });
  });
};

// Send Global / Regional Chat Message
exports.sendMessage = (req, res) => {
  const userId = req.user.id; const senderName = req.user.username; const { message, region } = req.body;
  if (!message) return res.status(400).json({ error: 'Message cannot be empty.' });
  db.run(`INSERT INTO messages (user_id, sender_name, region, message) VALUES (?, ?, ?, ?)`, [userId, senderName, region || 'GLOBAL', message], function (err) {
    if (err) return res.status(500).json({ error: 'Failed to send message.' });
    return res.status(200).json({ message: 'Message sent.' });
  });
};

// Get Chat Messages
exports.getMessages = (req, res) => {
  const region = req.query.region || 'GLOBAL';
  db.all(`SELECT sender_name AS senderName, message, created_at AS createdAt FROM messages WHERE region = ? ORDER BY created_at DESC LIMIT 50`, [region], (err, messages) => {
    if (err) return res.status(500).json({ error: 'Failed to fetch messages.' });
    return res.status(200).json({ messages });
  });
};

// Share win/kill simulation
exports.shareSocial = (req, res) => {
  const { platform, shareType } = req.body;
  return res.status(200).json({ message: `Successfully shared ${shareType} to ${platform}! Rewards: +50 BloodCoins.` });
};


