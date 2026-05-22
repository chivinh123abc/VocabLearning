const express = require('express');
const router = express.Router();
const adminController = require('../controllers/adminController');

router.post('/words', adminController.addWord);
router.put('/words/:id', adminController.updateWord);
router.delete('/words/:id', adminController.deleteWord);

module.exports = router;
