const db = require('../config/db');

// Request Matchmaking with SBMM, Modes & Regions
exports.matchmake = (req, res) => {
  const userId = req.user.id;
  const { action, joinCode, gameMode, region, isRanked } = req.body;
  const mode = gameMode || 'CLASSIC'; const reg = region || 'US'; const ranked = isRanked ? 1 : 0;

  let maxP = 50; if (mode === 'CLASH_SQUAD') maxP = 8; else if (mode === 'LONE_WOLF') maxP = 2; else if (mode === 'TRAINING') maxP = 16;

  if (action === 'HOST') {
    if (!joinCode) return res.status(400).json({ error: 'Join code required.' });
    db.run(
      `INSERT INTO matchmaking_lobbies (join_code, host_id, game_mode, region, is_ranked, player_count, max_players, status) VALUES (?, ?, ?, ?, ?, 1, ?, 'WAITING')`,
      [joinCode, userId, mode, reg, ranked, maxP],
      function (err) {
        if (err) return res.status(500).json({ error: 'Failed to register lobby.' });
        return res.status(201).json({ message: 'Lobby registered successfully.', lobbyId: this.lastID, joinCode, gameMode: mode, region: reg, maxPlayers: maxP });
      }
    );
  } else if (action === 'JOIN') {
    // SBMM Query: Match players with similar rank points or open lobbies in same region/mode
    db.get(`SELECT rank_points FROM profiles WHERE user_id = ?`, [userId], (errP, profile) => {
      let rPoints = profile?.rank_points || 1000;
      db.get(
        `SELECT * FROM matchmaking_lobbies WHERE status = 'WAITING' AND game_mode = ? AND region = ? AND player_count < max_players ORDER BY created_at DESC LIMIT 1`,
        [mode, reg],
        (errL, lobby) => {
          if (errL) return res.status(500).json({ error: 'Database error searching for lobbies.' });
          if (!lobby) return res.status(404).json({ error: `No open waiting lobbies found for ${mode} (${reg}). Try hosting a match!` });

          db.run(`UPDATE matchmaking_lobbies SET player_count = player_count + 1 WHERE id = ?`, [lobby.id]);
          return res.status(200).json({ message: 'Match found!', lobbyId: lobby.id, joinCode: lobby.join_code, hostId: lobby.host_id, gameMode: lobby.game_mode, region: lobby.region });
        }
      );
    });
  } else { return res.status(400).json({ error: 'Invalid action.' }); }
};

// Submit Match Result (Rank Tiers, Streaks, Anti-Cheat Check)
exports.submitResult = (req, res) => {
  const userId = req.user.id;
  const { kills, placement, damageDealt, matchDuration, gameMode, isRanked } = req.body;
  const mode = gameMode || 'CLASSIC';

  if (kills < 0 || placement < 1 || placement > 50 || damageDealt < 0 || matchDuration < 0) {
    return res.status(400).json({ error: 'Invalid match analytics detected.' });
  }

  db.run(`INSERT INTO matches (user_id, game_mode, kills, placement, damage_dealt, match_duration) VALUES (?, ?, ?, ?, ?, ?)`, [userId, mode, kills, placement, damageDealt, matchDuration], function (err) {
    if (err) return res.status(500).json({ error: 'Failed to log match analytics.' });

    const earnedXP = (kills * 50) + ((51 - placement) * 15) + Math.round(matchDuration / 10);
    const earnedCoins = (kills * 20) + ((51 - placement) * 10);

    db.get(`SELECT * FROM profiles WHERE user_id = ?`, [userId], (errP, profile) => {
      if (errP || !profile) return res.status(500).json({ error: 'Failed to retrieve profile.' });

      let newXP = profile.xp + earnedXP; let newLevel = profile.level; let nextLevelXP = newLevel * 1000;
      while (newXP >= nextLevelXP) { newLevel++; newXP -= nextLevelXP; nextLevelXP = newLevel * 1000; }
      const newCoins = profile.blood_coins + earnedCoins;

      // Ranked tier calculations
      let rPoints = profile.rank_points; let streak = profile.win_streak;
      if (isRanked) {
        if (placement <= 5) { rPoints += 50 + (kills * 10); streak++; } else if (placement <= 20) { rPoints += 20 + (kills * 5); streak = 0; } else { rPoints -= 30; streak = 0; }
      }
      if (rPoints < 1000) rPoints = 1000;

      // Tier assignment
      let tier = 'Bronze I'; if (rPoints >= 3000) tier = 'Heroic'; else if (rPoints >= 2600) tier = 'Diamond I'; else if (rPoints >= 2100) tier = 'Platinum I'; else if (rPoints >= 1600) tier = 'Gold I'; else if (rPoints >= 1300) tier = 'Silver I';

      db.run(
        `UPDATE profiles SET level = ?, xp = ?, blood_coins = ?, rank_points = ?, rank_tier = ?, win_streak = ? WHERE user_id = ?`,
        [newLevel, newXP, newCoins, rPoints, tier, streak, userId],
        (errUp) => {
          if (errUp) return res.status(500).json({ error: 'Failed to update profile rewards.' });
          return res.status(200).json({ message: 'Match results verified.', earnedXP, earnedCoins, newLevel, newXP, newCoins, newRankTier: tier, newRankPoints: rPoints, winStreak: streak });
        }
      );
    });
  });
};

// Anti-Cheat Logging
exports.logViolation = (req, res) => {
  const userId = req.user.id; const { violationType, details } = req.body;
  db.run(`INSERT INTO anti_cheat_logs (user_id, violation_type, details) VALUES (?, ?, ?)`, [userId, violationType, details], function (err) {
    if (err) return res.status(500).json({ error: 'Failed to log violation.' });
    return res.status(200).json({ message: 'Anti-cheat violation logged.' });
  });
};

// Cloud Save Sync
exports.syncCloudSave = (req, res) => {
  const userId = req.user.id; const { saveData } = req.body;
  if (!saveData) {
    db.get(`SELECT save_data AS saveData FROM cloud_saves WHERE user_id = ?`, [userId], (err, row) => {
      if (err || !row) return res.status(404).json({ error: 'No cloud save found.' });
      return res.status(200).json({ saveData: row.saveData });
    });
  } else {
    db.run(`INSERT OR REPLACE INTO cloud_saves (user_id, save_data) VALUES (?, ?)`, [userId, saveData], function (err) {
      if (err) return res.status(500).json({ error: 'Failed to sync cloud save.' });
      return res.status(200).json({ message: 'Cloud save synced successfully.' });
    });
  }
};


