const db = require('../config/db');

exports.getProfile = (req, res) => {
  const userId = req.user.id;
  db.get(`SELECT * FROM profiles WHERE user_id = ?`, [userId], (err, profile) => {
    if (err)      return res.status(500).json({ error: 'Database error fetching profile.' });
    if (!profile) return res.status(404).json({ error: 'Profile not found.' });
    return res.status(200).json({
      userId:            profile.user_id,
      displayName:       profile.display_name,
      level:             profile.level,
      xp:                profile.xp,
      bloodCoins:        profile.blood_coins,
      diamonds:          profile.diamonds,
      selectedCharacter: profile.selected_character,
      rank_tier:         profile.rank_tier,
      rank_points:       profile.rank_points,
      win_streak:        profile.win_streak,
      kill_milestone:    profile.kill_milestone,
      battle_pass_level: profile.battle_pass_level
    });
  });
};

exports.updateCharacter = (req, res) => {
  const userId = req.user.id;
  const { characterName } = req.body;

  const validCharacters = [
    'DJNeon','Pulse','Bolt','Ronin','Mirage','Sonic','Zero','Cypher','Viper','Axiom',
    'Echo','Lynx','Shadow','Specter','Ghost','Titan','Blaze','Revan','Helix','Mako',
    'Stryker','Nexus','Nova','Atlas','Cipher','Talon','Phantom','Apex','Havoc','Jager',
    'Riven','Vex','Krypton'
  ];

  if (!validCharacters.includes(characterName))
    return res.status(400).json({ error: 'Invalid character choice.' });

  db.run(`UPDATE profiles SET selected_character = ? WHERE user_id = ?`,
    [characterName, userId], function (err) {
      if (err) return res.status(500).json({ error: 'Database error updating character.' });
      return res.status(200).json({ message: 'Character updated successfully.', selectedCharacter: characterName });
    });
};


