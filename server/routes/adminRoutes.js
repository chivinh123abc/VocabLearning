const express = require('express');
const router = express.Router();
const multer = require('multer');
const adminController = require('../controllers/adminController');
const { verifyToken, isAdmin } = require('../middleware/authMiddleware');

const upload = multer({ storage: multer.memoryStorage() });

router.post('/words', verifyToken, isAdmin, adminController.addWord);
router.post('/words/upload-image', verifyToken, isAdmin, upload.single('image'), adminController.uploadImage);
router.put('/words/:id', verifyToken, isAdmin, adminController.updateWord);
router.delete('/words/:id', verifyToken, isAdmin, adminController.deleteWord);

// Quản lý User
router.get('/users', verifyToken, isAdmin, adminController.getAllUsers);
router.put('/users/:id', verifyToken, isAdmin, adminController.updateUser);
router.delete('/users/:id', verifyToken, isAdmin, adminController.deleteUser);

module.exports = router;

