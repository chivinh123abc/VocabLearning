const express = require('express');
const router = express.Router();
const userController = require('../controllers/userController');
const { verifyToken } = require('../middleware/authMiddleware');

router.post('/sync', verifyToken, userController.syncUserData);
router.get('/profile', verifyToken, userController.getUserProfile);
router.post('/change-username', verifyToken, userController.changeUsername);

module.exports = router;
