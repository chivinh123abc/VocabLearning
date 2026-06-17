const express = require('express');
const router = express.Router();
const gameController = require('../controllers/gameController');

router.get('/globals', gameController.getGlobals);

// LAN PvP Matchmaking & Battle Room routes
router.post('/battle/matchmake', gameController.matchmake);
router.get('/battle/matchmake/status/:userId', gameController.getMatchmakeStatus);
router.post('/battle/matchmake/cancel', gameController.cancelMatchmake);
router.get('/battle/room/:roomId', gameController.getRoomState);
router.post('/battle/room/:roomId/answer', gameController.submitAnswer);
router.post('/battle/room/:roomId/leave', gameController.leaveRoom);

module.exports = router;
