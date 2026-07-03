const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const db = require('../config/db');
const { JWT_SECRET } = require('../middleware/auth');

// Register real user
exports.register = async (req, res) => {
  const { username, password } = req.body;
  if (!username || !password) return res.status(400).json({ error: 'Username and password are required.' });

  try {
    const salt = await bcrypt.genSalt(10); const passwordHash = await bcrypt.hash(password, salt);
    db.run(`INSERT INTO users (username, password_hash, is_guest) VALUES (?, ?, 0)`, [username, passwordHash], function (err) {
      if (err) return res.status(400).json({ error: 'Username already exists.' });
      const userId = this.lastID;
      db.run(`INSERT INTO profiles (user_id, display_name) VALUES (?, ?)`, [userId, username], (errP) => {
        const token = jwt.sign({ id: userId, username }, JWT_SECRET, { expiresIn: '7d' });
        return res.status(201).json({ message: 'User registered successfully.', token, userId, username });
      });
    });
  } catch (error) { return res.status(500).json({ error: 'Server error during registration.' }); }
};

// Login user
exports.login = async (req, res) => {
  const { username, password } = req.body;
  if (!username || !password) return res.status(400).json({ error: 'Username and password are required.' });

  db.get(`SELECT * FROM users WHERE username = ?`, [username], async (err, user) => {
    if (err || !user) return res.status(401).json({ error: 'Invalid username or password.' });
    if (user.is_banned) return res.status(403).json({ error: 'Account banned. Reason: ' + user.ban_reason });

    const isMatch = await bcrypt.compare(password, user.password_hash);
    if (!isMatch) return res.status(401).json({ error: 'Invalid username or password.' });

    const token = jwt.sign({ id: user.id, username: user.username }, JWT_SECRET, { expiresIn: '7d' });
    return res.status(200).json({ message: 'Login successful.', token, userId: user.id, username: user.username });
  });
};

// Guest login with recovery
exports.guest = async (req, res) => {
  const { recoveryCode } = req.body;
  if (recoveryCode) {
    db.get(`SELECT * FROM users WHERE username = ?`, [recoveryCode], (err, user) => {
      if (err || !user) return res.status(404).json({ error: 'Guest recovery code not found.' });
      if (user.is_banned) return res.status(403).json({ error: 'Account banned.' });
      const token = jwt.sign({ id: user.id, username: user.username }, JWT_SECRET, { expiresIn: '7d' });
      return res.status(200).json({ message: 'Guest account recovered.', token, userId: user.id, username: user.username });
    });
    return;
  }

  const guestUsername = 'Guest_' + Math.floor(100000 + Math.random() * 900000);
  const guestPassword = 'guest_secret_password_2026';
  try {
    const salt = await bcrypt.genSalt(10); const passwordHash = await bcrypt.hash(guestPassword, salt);
    db.run(`INSERT INTO users (username, password_hash, is_guest) VALUES (?, ?, 1)`, [guestUsername, passwordHash], function (err) {
      if (err) return res.status(500).json({ error: 'Failed to create guest user.' });
      const userId = this.lastID;
      db.run(`INSERT INTO profiles (user_id, display_name) VALUES (?, ?)`, [userId, guestUsername], (errP) => {
        const token = jwt.sign({ id: userId, username: guestUsername }, JWT_SECRET, { expiresIn: '7d' });
        return res.status(201).json({ message: 'Guest account created.', token, userId, username: guestUsername, recoveryCode: guestUsername });
      });
    });
  } catch (error) { return res.status(500).json({ error: 'Server error.' }); }
};

// OAuth login simulation (Google, Facebook, VK, Apple ID)
exports.oauth = async (req, res) => {
  const { provider, providerId, email, displayName } = req.body;
  if (!provider || !providerId) return res.status(400).json({ error: 'OAuth provider and ID are required.' });

  const colName = provider.toLowerCase() + '_id';
  db.get(`SELECT * FROM users WHERE ${colName} = ?`, [providerId], async (err, user) => {
    if (user) {
      if (user.is_banned) return res.status(403).json({ error: 'Account banned. Reason: ' + user.ban_reason });
      const token = jwt.sign({ id: user.id, username: user.username }, JWT_SECRET, { expiresIn: '7d' });
      return res.status(200).json({ message: `${provider} login successful.`, token, userId: user.id, username: user.username });
    } else {
      const username = displayName || (provider + '_' + Math.floor(1000 + Math.random() * 9000));
      const salt = await bcrypt.genSalt(10); const passwordHash = await bcrypt.hash(providerId, salt);
      db.run(`INSERT INTO users (username, password_hash, ${colName}, is_guest) VALUES (?, ?, ?, 0)`, [username, passwordHash, providerId], function (errIns) {
        if (errIns) return res.status(500).json({ error: 'Failed to create OAuth account.' });
        const userId = this.lastID;
        db.run(`INSERT INTO profiles (user_id, display_name) VALUES (?, ?)`, [userId, username], (errP) => {
          const token = jwt.sign({ id: userId, username }, JWT_SECRET, { expiresIn: '7d' });
          return res.status(201).json({ message: `${provider} account created.`, token, userId, username });
        });
      });
    }
  });
};

// Account Linking (Merge guest to real/OAuth account)
exports.linkAccount = (req, res) => {
  const userId = req.user.id;
  const { provider, providerId } = req.body;
  if (!provider || !providerId) return res.status(400).json({ error: 'Provider details required.' });

  const colName = provider.toLowerCase() + '_id';
  db.run(`UPDATE users SET ${colName} = ?, is_guest = 0 WHERE id = ?`, [providerId, userId], function (err) {
    if (err) return res.status(500).json({ error: 'Failed to link account.' });
    return res.status(200).json({ message: `Successfully linked ${provider} to account.` });
  });
};


