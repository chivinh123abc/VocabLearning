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
