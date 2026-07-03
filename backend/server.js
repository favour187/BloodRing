const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const path = require('path');
const { apiLimiter } = require('./middleware/rateLimiter');

const app = express();
const PORT = process.env.PORT || 5000;

app.use(helmet()); app.use(cors()); app.use(express.json()); app.use(express.urlencoded({ extended: true }));
app.use('/api', apiLimiter);

const authRoutes = require('./routes/authRoutes');
const profileRoutes = require('./routes/profileRoutes');
const matchRoutes = require('./routes/matchRoutes');
const leaderboardRoutes = require('./routes/leaderboardRoutes');
const socialRoutes = require('./routes/socialRoutes');
const storeRoutes = require('./routes/storeRoutes');

app.use('/api/auth', authRoutes);
app.use('/api/profile', profileRoutes);
app.use('/api/match', matchRoutes);
app.use('/api/leaderboard', leaderboardRoutes);
app.use('/api/social', socialRoutes);
app.use('/api/store', storeRoutes);

app.get('/', (req, res) => { res.json({ message: 'BloodRing Apex Enterprise Backend Service Active (v2.0.0)' }); });
app.use((err, req, res, next) => { console.error(err.stack); res.status(500).json({ error: 'Internal server error.' }); });

app.listen(PORT, () => { console.log(`BloodRing Apex Enterprise Backend running on port ${PORT}`); });


