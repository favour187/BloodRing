const sqlite3 = require('sqlite3').verbose();
const path = require('path');

const dbPath = path.resolve(__dirname, '../bloodring.db');
const db = new sqlite3.Database(dbPath, (err) => {
  if (err) {
    console.error('Error connecting to SQLite database:', err.message);
  } else {
    console.log('Connected to SQLite database at:', dbPath);
    initDb();
  }
});

function initDb() {
  db.serialize(() => {
    db.run(`CREATE TABLE IF NOT EXISTS users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      username TEXT UNIQUE NOT NULL,
      password_hash TEXT NOT NULL,
      google_id TEXT,
      facebook_id TEXT,
      vk_id TEXT,
      apple_id TEXT,
      is_guest INTEGER DEFAULT 1,
      is_banned INTEGER DEFAULT 0,
      ban_reason TEXT,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS profiles (
      user_id INTEGER PRIMARY KEY,
      display_name TEXT NOT NULL,
      level INTEGER DEFAULT 1,
      xp INTEGER DEFAULT 0,
      blood_coins INTEGER DEFAULT 1000,
      diamonds INTEGER DEFAULT 100,
      selected_character TEXT DEFAULT 'DJNeon',
      rank_tier TEXT DEFAULT 'Bronze I',
      rank_points INTEGER DEFAULT 1000,
      win_streak INTEGER DEFAULT 0,
      kill_milestone INTEGER DEFAULT 0,
      battle_pass_level INTEGER DEFAULT 1,
      last_daily_claim DATETIME,
      FOREIGN KEY (user_id) REFERENCES users (id)
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS inventory (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER NOT NULL,
      item_type TEXT NOT NULL,
      item_id TEXT NOT NULL,
      level INTEGER DEFAULT 1,
      fragments INTEGER DEFAULT 0,
      FOREIGN KEY (user_id) REFERENCES users (id)
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS friends (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER NOT NULL,
      friend_id INTEGER NOT NULL,
      status TEXT DEFAULT 'ACCEPTED',
      FOREIGN KEY (user_id) REFERENCES users (id),
      FOREIGN KEY (friend_id) REFERENCES users (id)
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS guilds (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      name TEXT UNIQUE NOT NULL,
      leader_id INTEGER NOT NULL,
      level INTEGER DEFAULT 1,
      members_count INTEGER DEFAULT 1,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS guild_members (
      guild_id INTEGER NOT NULL,
      user_id INTEGER NOT NULL,
      role TEXT DEFAULT 'MEMBER',
      PRIMARY KEY (guild_id, user_id)
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS messages (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER NOT NULL,
      sender_name TEXT NOT NULL,
      region TEXT DEFAULT 'GLOBAL',
      message TEXT NOT NULL,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS missions (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER NOT NULL,
      mission_id TEXT NOT NULL,
      mission_type TEXT DEFAULT 'DAILY',
      progress INTEGER DEFAULT 0,
      target INTEGER DEFAULT 1,
      is_claimed INTEGER DEFAULT 0,
      FOREIGN KEY (user_id) REFERENCES users (id)
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS matches (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER NOT NULL,
      game_mode TEXT DEFAULT 'CLASSIC',
      map_name TEXT DEFAULT 'IslaVerde',
      kills INTEGER DEFAULT 0,
      placement INTEGER DEFAULT 50,
      damage_dealt REAL DEFAULT 0.0,
      match_duration REAL DEFAULT 0.0,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (user_id) REFERENCES users (id)
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS matchmaking_lobbies (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      join_code TEXT NOT NULL,
      host_id INTEGER NOT NULL,
      game_mode TEXT DEFAULT 'CLASSIC',
      region TEXT DEFAULT 'US',
      is_ranked INTEGER DEFAULT 0,
      player_count INTEGER DEFAULT 1,
      max_players INTEGER DEFAULT 50,
      status TEXT DEFAULT 'WAITING',
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS anti_cheat_logs (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER NOT NULL,
      violation_type TEXT NOT NULL,
      details TEXT,
      created_at DATETIME DEFAULT CURRENT_TIMESTAMP
    )`);

    db.run(`CREATE TABLE IF NOT EXISTS cloud_saves (
      user_id INTEGER PRIMARY KEY,
      save_data TEXT NOT NULL,
      updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
      FOREIGN KEY (user_id) REFERENCES users (id)
    )`);

    console.log('BloodRing Enterprise Database tables initialized successfully.');
  });
}

module.exports = db;


