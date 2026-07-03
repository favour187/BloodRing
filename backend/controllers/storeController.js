const db = require('../config/db');

exports.getStore = (req, res) => {
  const userId = req.user.id;
  const storeItems = [
    { id: 'char_djneon', name: 'DJ Neon Bundle', type: 'CHARACTER', priceDiamonds: 599, priceCoins: 15000 },
    { id: 'char_pulse', name: 'Pulse Shield Suit', type: 'CHARACTER', priceDiamonds: 499, priceCoins: 12000 },
    { id: 'skin_awm_flame', name: 'AWM Dragon Flame', type: 'WEAPON_SKIN', priceDiamonds: 299, priceCoins: 8000 },
    { id: 'skin_mp40_cobra', name: 'MP40 Predatory Cobra', type: 'WEAPON_SKIN', priceDiamonds: 399, priceCoins: 10000 },
    { id: 'skin_m1887_rapper', name: 'M1887 Rapper Underworld', type: 'WEAPON_SKIN', priceDiamonds: 349, priceCoins: 9000 },
    { id: 'bundle_heroic', name: 'Heroic Warrior Bundle', type: 'BUNDLE', priceDiamonds: 899, priceCoins: 25000 }
  ];

  db.all(`SELECT item_type AS itemType, item_id AS itemId, level, fragments FROM inventory WHERE user_id = ?`, [userId], (err, inventory) => {
    if (err) return res.status(500).json({ error: 'Database error fetching store.' });
    return res.status(200).json({ storeItems, inventory });
  });
};

exports.buyItem = (req, res) => {
  const userId = req.user.id; const { itemId, currency } = req.body;
  const storeItems = [
    { id: 'char_djneon', name: 'DJ Neon Bundle', type: 'CHARACTER', priceDiamonds: 599, priceCoins: 15000 },
    { id: 'char_pulse', name: 'Pulse Shield Suit', type: 'CHARACTER', priceDiamonds: 499, priceCoins: 12000 },
    { id: 'skin_awm_flame', name: 'AWM Dragon Flame', type: 'WEAPON_SKIN', priceDiamonds: 299, priceCoins: 8000 },
    { id: 'skin_mp40_cobra', name: 'MP40 Predatory Cobra', type: 'WEAPON_SKIN', priceDiamonds: 399, priceCoins: 10000 },
    { id: 'skin_m1887_rapper', name: 'M1887 Rapper Underworld', type: 'WEAPON_SKIN', priceDiamonds: 349, priceCoins: 9000 },
    { id: 'bundle_heroic', name: 'Heroic Warrior Bundle', type: 'BUNDLE', priceDiamonds: 899, priceCoins: 25000 }
  ];
  const item = storeItems.find(i => i.id === itemId);
  if (!item) return res.status(404).json({ error: 'Item not found in store.' });

  db.get(`SELECT * FROM profiles WHERE user_id = ?`, [userId], (err, profile) => {
    if (err || !profile) return res.status(500).json({ error: 'Failed to retrieve profile.' });
    let cost = currency === 'GEMS' || currency === 'DIAMONDS' ? item.priceDiamonds : item.priceCoins;
    let currentBal = currency === 'GEMS' || currency === 'DIAMONDS' ? profile.diamonds : profile.blood_coins;

    if (currentBal < cost) return res.status(400).json({ error: `Not enough ${currency.toLowerCase()} to buy this item.` });
    let sqlUpdate = currency === 'GEMS' || currency === 'DIAMONDS' ? `UPDATE profiles SET diamonds = diamonds - ? WHERE user_id = ?` : `UPDATE profiles SET blood_coins = blood_coins - ? WHERE user_id = ?`;

    db.run(sqlUpdate, [cost, userId], (errUp) => {
      if (errUp) return res.status(500).json({ error: 'Transaction failed.' });
      db.run(`INSERT INTO inventory (user_id, item_type, item_id) VALUES (?, ?, ?)`, [userId, item.type, item.id], (errIns) => {
        return res.status(200).json({ message: `Successfully purchased ${item.name}!`, item });
      });
    });
  });
};

exports.luckySpin = (req, res) => {
  const userId = req.user.id;
  db.get(`SELECT diamonds FROM profiles WHERE user_id = ?`, [userId], (err, profile) => {
    if (err || !profile || profile.diamonds < 10) return res.status(400).json({ error: 'Not enough Gems for Lucky Spin (10 Gems required).' });
    db.run(`UPDATE profiles SET diamonds = diamonds - 10 WHERE user_id = ?`, [userId], (errU) => {
      const rewards = [
        { name: '500 BloodCoins', coins: 500, diamonds: 0 },
        { name: '50 Gems', coins: 0, diamonds: 50 },
        { name: 'AWM Dragon Flame Skin', skin: 'skin_awm_flame' },
        { name: 'DJ Neon Character Fragments x20', fragments: 20 }
      ];
      const win = rewards[Math.floor(Math.random() * rewards.length)];
      if (win.coins > 0) db.run(`UPDATE profiles SET blood_coins = blood_coins + ? WHERE user_id = ?`, [win.coins, userId]);
      if (win.diamonds > 0) db.run(`UPDATE profiles SET diamonds = diamonds + ? WHERE user_id = ?`, [win.diamonds, userId]);
      if (win.skin) db.run(`INSERT INTO inventory (user_id, item_type, item_id) VALUES (?, 'WEAPON_SKIN', ?)`, [userId, win.skin]);
      return res.status(200).json({ message: `Lucky Spin landed on: ${win.name}!`, reward: win.name });
    });
  });
};

exports.claimDaily = (req, res) => {
  const userId = req.user.id;
  db.run(`UPDATE profiles SET blood_coins = blood_coins + 200, diamonds = diamonds + 5, last_daily_claim = CURRENT_TIMESTAMP WHERE user_id = ?`, [userId], (err) => {
    if (err) return res.status(500).json({ error: 'Failed to claim daily reward.' });
    return res.status(200).json({ message: 'Daily Reward Claimed: +200 BloodCoins, +5 Gems!' });
  });
};

exports.getMissions = (req, res) => {
  const userId = req.user.id;
  const initialMissions = [
    { id: 'm_kill_5', desc: 'Eliminate 5 enemies in Classic mode', target: 5, rewardCoins: 300, rewardXP: 250 },
    { id: 'm_win_1', desc: 'Get 1 Apex Victory in any mode', target: 1, rewardCoins: 500, rewardXP: 500 },
    { id: 'm_play_3', desc: 'Play 3 matches in Squad Clash', target: 3, rewardCoins: 200, rewardXP: 150 }
  ];
  db.all(`SELECT * FROM missions WHERE user_id = ?`, [userId], (err, missions) => {
    if (err) return res.status(500).json({ error: 'Database error.' });
    db.get(`SELECT battle_pass_level AS bpLevel FROM profiles WHERE user_id = ?`, [userId], (errP, profile) => {
      return res.status(200).json({ bpLevel: profile?.bpLevel || 1, missions: missions.length > 0 ? missions : initialMissions });
    });
  });
};


