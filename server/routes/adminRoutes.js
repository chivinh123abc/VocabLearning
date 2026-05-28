const express = require('express');
const router = express.Router();
const adminController = require('../controllers/adminController');

router.post('/words', adminController.addWord);
router.put('/words/:id', adminController.updateWord);
router.delete('/words/:id', adminController.deleteWord);

router.post('/vocabsets', adminController.addVocabSet);
router.put('/vocabsets/:id', adminController.updateVocabSet);
router.delete('/vocabsets/:id', adminController.deleteVocabSet);

module.exports = router;
