const express = require('express');
const router = express.Router();
const adminController = require('../controllers/adminController');
const { verifyToken, isAdmin } = require('../middleware/authMiddleware');

router.post('/words', verifyToken, isAdmin, adminController.addWord);
router.put('/words/:id', verifyToken, isAdmin, adminController.updateWord);
router.delete('/words/:id', verifyToken, isAdmin, adminController.deleteWord);

// Quản lý User
router.get('/users', verifyToken, isAdmin, adminController.getAllUsers);
router.put('/users/:id', verifyToken, isAdmin, adminController.updateUser);
router.delete('/users/:id', verifyToken, isAdmin, adminController.deleteUser);

module.exports = router;

