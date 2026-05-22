const express = require('express');
const router = express.Router();
const gameController = require('../controllers/gameController');

router.get('/globals', gameController.getGlobals);

module.exports = router;
