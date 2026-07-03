const rateLimit = require('express-rate-limit');

const apiLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  limit: 100, // Limit each IP to 100 requests per windowMs
  standardHeaders: 'draft-7',
  legacyHeaders: false,
  message: { error: 'Too many requests from this IP, please try again after 15 minutes.' }
});

const authLimiter = rateLimit({
  windowMs: 60 * 60 * 1000, // 1 hour
  limit: 20, // Limit each IP to 20 login/register requests per hour
  standardHeaders: 'draft-7',
  legacyHeaders: false,
  message: { error: 'Too many login attempts from this IP, please try again after an hour.' }
});

module.exports = { apiLimiter, authLimiter };


