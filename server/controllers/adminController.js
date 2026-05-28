const { getPool, sql } = require('../config/db');

// Thêm từ vựng mới (Admin)
exports.addWord = async (req, res) => {
  const { id, word, meaning, rankRequired, imageUrl, imageSub } = req.body;

  if (!id || !word || !meaning) {
    return res.status(400).json({ success: false, message: 'ID, Từ vựng và Ý nghĩa là bắt buộc!' });
  }

  try {
    const pool = await getPool();
    const request = pool.request();
    request.input('id', sql.VarChar, id);
    request.input('word', sql.NVarChar, word);
    request.input('meaning', sql.NVarChar, meaning);
    request.input('rankRequired', sql.VarChar, rankRequired || 'Dong');
    request.input('imageUrl', sql.NVarChar, imageUrl || '');
    request.input('imageSub', sql.NVarChar, imageSub || '');

    await request.query(`
      INSERT INTO [words] ([id], [word], [meaning], [rankRequired], [imageUrl], [imageSub])
      VALUES (@id, @word, @meaning, @rankRequired, @imageUrl, @imageSub)
    `);

    console.log(`📝 Admin đã thêm từ vựng mới: ${word} (${id})`);
    return res.status(201).json({ success: true, message: 'Đã thêm từ vựng thành công!' });

  } catch (err) {
    console.error('Lỗi khi Admin thêm từ: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi thêm từ vựng!' });
  }
};

// Sửa từ vựng (Admin)
exports.updateWord = async (req, res) => {
  const wordId = req.params.id;
  const { word, meaning, rankRequired, imageUrl, imageSub } = req.body;

  if (!word || !meaning) {
    return res.status(400).json({ success: false, message: 'Từ vựng và Ý nghĩa là bắt buộc!' });
  }

  try {
    const pool = await getPool();
    const request = pool.request();
    request.input('id', sql.VarChar, wordId);
    request.input('word', sql.NVarChar, word);
    request.input('meaning', sql.NVarChar, meaning);
    request.input('rankRequired', sql.VarChar, rankRequired || 'Dong');
    request.input('imageUrl', sql.NVarChar, imageUrl || '');
    request.input('imageSub', sql.NVarChar, imageSub || '');

    const result = await request.query(`
      UPDATE [words]
      SET [word] = @word,
          [meaning] = @meaning,
          [rankRequired] = @rankRequired,
          [imageUrl] = @imageUrl,
          [imageSub] = @imageSub
      WHERE [id] = @id
    `);

    if (result.rowsAffected[0] === 0) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy từ vựng để sửa!' });
    }

    console.log(`📝 Admin đã cập nhật từ vựng: ${word} (${wordId})`);
    return res.json({ success: true, message: 'Cập nhật từ vựng thành công!' });

  } catch (err) {
    console.error('Lỗi khi Admin sửa từ: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi sửa từ vựng!' });
  }
};

// Xóa từ vựng (Admin)
exports.deleteWord = async (req, res) => {
  const wordId = req.params.id;

  try {
    const pool = await getPool();
    const request = pool.request();
    request.input('id', sql.VarChar, wordId);

    const result = await request.query('DELETE FROM [words] WHERE [id] = @id');

    if (result.rowsAffected[0] === 0) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy từ vựng để xóa!' });
    }

    console.log(`📝 Admin đã xóa từ vựng: ${wordId}`);
    return res.json({ success: true, message: 'Xóa từ vựng thành công!' });

  } catch (err) {
    console.error('Lỗi khi Admin xóa từ: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi xóa từ vựng!' });
  }
};

// Thêm bộ từ vựng mới (Admin)
exports.addVocabSet = async (req, res) => {
  const { id, title, description, category, difficulty, wordIds, levels } = req.body;

  if (!id || !title) {
    return res.status(400).json({ success: false, message: 'ID và Tên bộ từ vựng là bắt buộc!' });
  }

  try {
    const pool = await getPool();
    const transaction = new sql.Transaction(pool);
    await transaction.begin();

    try {
      // 1. Thêm vào bảng vocab_sets
      const reqSet = new sql.Request(transaction);
      reqSet.input('id', sql.VarChar, id);
      reqSet.input('title', sql.NVarChar, title);
      reqSet.input('description', sql.NVarChar, description || '');
      reqSet.input('category', sql.NVarChar, category || '');
      reqSet.input('difficulty', sql.NVarChar, difficulty || '');
      reqSet.input('rankRequired', sql.VarChar, 'Dong');

      await reqSet.query(`
        INSERT INTO [vocab_sets] ([id], [title], [description], [category], [difficulty], [rankRequired])
        VALUES (@id, @title, @description, @category, @difficulty, @rankRequired)
      `);

      // 2. Thêm liên kết từ vựng vocab_set_words
      if (wordIds && wordIds.length > 0) {
        for (const wordId of wordIds) {
          const reqLink = new sql.Request(transaction);
          reqLink.input('setId', sql.VarChar, id);
          reqLink.input('wordId', sql.VarChar, wordId);
          await reqLink.query(`
            INSERT INTO [vocab_set_words] ([setId], [wordId])
            VALUES (@setId, @wordId)
          `);
        }
      }

      // 3. Thêm các level và level words
      if (levels && levels.length > 0) {
        for (const lvl of levels) {
          const reqLvl = new sql.Request(transaction);
          reqLvl.input('setId', sql.VarChar, id);
          reqLvl.input('diff', sql.NVarChar, lvl.difficulty);
          await reqLvl.query(`
            INSERT INTO [vocab_set_levels] ([setId], [difficulty])
            VALUES (@setId, @diff)
          `);

          if (lvl.wordIds && lvl.wordIds.length > 0) {
            for (const wordId of lvl.wordIds) {
              const reqLvlWord = new sql.Request(transaction);
              reqLvlWord.input('setId', sql.VarChar, id);
              reqLvlWord.input('diff', sql.NVarChar, lvl.difficulty);
              reqLvlWord.input('wordId', sql.VarChar, wordId);
              await reqLvlWord.query(`
                INSERT INTO [vocab_set_level_words] ([setId], [difficulty], [wordId])
                VALUES (@setId, @diff, @wordId)
              `);
            }
          }
        }
      }

      await transaction.commit();
      console.log(`📝 Admin đã thêm bộ từ vựng mới: ${title} (${id})`);
      return res.status(201).json({ success: true, message: 'Đã thêm bộ từ vựng thành công!' });

    } catch (err) {
      await transaction.rollback();
      throw err;
    }

  } catch (err) {
    console.error('Lỗi khi Admin thêm bộ từ vựng: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi thêm bộ từ vựng!' });
  }
};

// Sửa bộ từ vựng (Admin)
exports.updateVocabSet = async (req, res) => {
  const setId = req.params.id;
  const { title, description, category, difficulty, wordIds, levels } = req.body;

  if (!title) {
    return res.status(400).json({ success: false, message: 'Tên bộ từ vựng là bắt buộc!' });
  }

  try {
    const pool = await getPool();
    const transaction = new sql.Transaction(pool);
    await transaction.begin();

    try {
      // 1. Cập nhật bảng vocab_sets
      const reqSet = new sql.Request(transaction);
      reqSet.input('id', sql.VarChar, setId);
      reqSet.input('title', sql.NVarChar, title);
      reqSet.input('description', sql.NVarChar, description || '');
      reqSet.input('category', sql.NVarChar, category || '');
      reqSet.input('difficulty', sql.NVarChar, difficulty || '');

      const result = await reqSet.query(`
        UPDATE [vocab_sets]
        SET [title] = @title,
            [description] = @description,
            [category] = @category,
            [difficulty] = @difficulty
        WHERE [id] = @id
      `);

      if (result.rowsAffected[0] === 0) {
        await transaction.rollback();
        return res.status(404).json({ success: false, message: 'Không tìm thấy bộ từ vựng để sửa!' });
      }

      // 2. Xóa các liên kết cũ
      const reqDel = new sql.Request(transaction);
      reqDel.input('setId', sql.VarChar, setId);
      await reqDel.query(`
        DELETE FROM [vocab_set_level_words] WHERE [setId] = @setId;
        DELETE FROM [vocab_set_levels] WHERE [setId] = @setId;
        DELETE FROM [vocab_set_words] WHERE [setId] = @setId;
      `);

      // 3. Thêm liên kết từ vựng vocab_set_words mới
      if (wordIds && wordIds.length > 0) {
        for (const wordId of wordIds) {
          const reqLink = new sql.Request(transaction);
          reqLink.input('setId', sql.VarChar, setId);
          reqLink.input('wordId', sql.VarChar, wordId);
          await reqLink.query(`
            INSERT INTO [vocab_set_words] ([setId], [wordId])
            VALUES (@setId, @wordId)
          `);
        }
      }

      // 4. Thêm các level và level words mới
      if (levels && levels.length > 0) {
        for (const lvl of levels) {
          const reqLvl = new sql.Request(transaction);
          reqLvl.input('setId', sql.VarChar, setId);
          reqLvl.input('diff', sql.NVarChar, lvl.difficulty);
          await reqLvl.query(`
            INSERT INTO [vocab_set_levels] ([setId], [difficulty])
            VALUES (@setId, @diff)
          `);

          if (lvl.wordIds && lvl.wordIds.length > 0) {
            for (const wordId of lvl.wordIds) {
              const reqLvlWord = new sql.Request(transaction);
              reqLvlWord.input('setId', sql.VarChar, setId);
              reqLvlWord.input('diff', sql.NVarChar, lvl.difficulty);
              reqLvlWord.input('wordId', sql.VarChar, wordId);
              await reqLvlWord.query(`
                INSERT INTO [vocab_set_level_words] ([setId], [difficulty], [wordId])
                VALUES (@setId, @diff, @wordId)
              `);
            }
          }
        }
      }

      await transaction.commit();
      console.log(`📝 Admin đã cập nhật bộ từ vựng: ${title} (${setId})`);
      return res.json({ success: true, message: 'Cập nhật bộ từ vựng thành công!' });

    } catch (err) {
      await transaction.rollback();
      throw err;
    }

  } catch (err) {
    console.error('Lỗi khi Admin sửa bộ từ vựng: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi sửa bộ từ vựng!' });
  }
};

// Xóa bộ từ vựng (Admin)
exports.deleteVocabSet = async (req, res) => {
  const setId = req.params.id;

  try {
    const pool = await getPool();
    const transaction = new sql.Transaction(pool);
    await transaction.begin();

    try {
      // 1. Xóa các liên kết liên quan trước
      const reqDel = new sql.Request(transaction);
      reqDel.input('setId', sql.VarChar, setId);
      await reqDel.query(`
        DELETE FROM [vocab_set_level_words] WHERE [setId] = @setId;
        DELETE FROM [vocab_set_levels] WHERE [setId] = @setId;
        DELETE FROM [vocab_set_words] WHERE [setId] = @setId;
        DELETE FROM [user_learned_sets] WHERE [setId] = @setId;
        DELETE FROM [user_saved_set_levels] WHERE [setId] = @setId;
        DELETE FROM [user_set_progress] WHERE [setId] = @setId;
      `);

      // 2. Xóa bộ từ vựng khỏi vocab_sets
      const reqSet = new sql.Request(transaction);
      reqSet.input('id', sql.VarChar, setId);
      const result = await reqSet.query('DELETE FROM [vocab_sets] WHERE [id] = @id');

      if (result.rowsAffected[0] === 0) {
        await transaction.rollback();
        return res.status(404).json({ success: false, message: 'Không tìm thấy bộ từ vựng để xóa!' });
      }

      await transaction.commit();
      console.log(`📝 Admin đã xóa bộ từ vựng: ${setId}`);
      return res.json({ success: true, message: 'Xóa bộ từ vựng thành công!' });

    } catch (err) {
      await transaction.rollback();
      throw err;
    }

  } catch (err) {
    console.error('Lỗi khi Admin xóa bộ từ vựng: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi xóa bộ từ vựng!' });
  }
};
