const db = require('../config/db');

// Get Top 10 Global Leaderboard (By Total Kills & Player Level)
exports.getLeaderboard = (req, res) => {
  const query = `
    SELECT p.display_name AS displayName, p.level, p.blood_coins AS bloodCoins, COALESCE(SUM(m.kills), 0) AS totalKills
    FROM profiles p
    LEFT JOIN matches m ON p.user_id = m.user_id
    GROUP BY p.user_id, p.display_name, p.level, p.blood_coins
    ORDER BY totalKills DESC, p.level DESC
    LIMIT 10
  `;

  db.all(query, [], (err, rows) => {
    if (err) {
      return res.status(500).json({ error: 'Database error fetching leaderboard.' });
    }

    return res.status(200).json({ leaderboard: rows });
  });
};


