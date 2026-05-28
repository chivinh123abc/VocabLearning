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

// Lấy danh sách tất cả người dùng (Admin)
exports.getAllUsers = async (req, res) => {
  try {
    const pool = await getPool();
    const result = await pool.request().query(`
      SELECT [id], [username], [email], [role], [status], [level], [exp], [coins], [rankPoints], [wins], [totalGames]
      FROM [users]
      ORDER BY [username] ASC
    `);
    
    return res.json({ success: true, users: result.recordset });
  } catch (err) {
    console.error('Lỗi khi Admin lấy danh sách user: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi lấy danh sách user!' });
  }
};

// Cập nhật thông số & trạng thái người dùng (Admin)
exports.updateUser = async (req, res) => {
  const userId = req.params.id;
  const { role, status, level, exp, coins, rankPoints } = req.body;

  try {
    const pool = await getPool();
    const request = pool.request();
    request.input('id', sql.VarChar, userId);
    request.input('role', sql.VarChar, role || 'user');
    request.input('status', sql.VarChar, status || 'active');
    request.input('level', sql.Int, level !== undefined ? level : 1);
    request.input('exp', sql.Int, exp !== undefined ? exp : 0);
    request.input('coins', sql.Int, coins !== undefined ? coins : 0);
    request.input('rankPoints', sql.Int, rankPoints !== undefined ? rankPoints : 0);

    const result = await request.query(`
      UPDATE [users]
      SET [role] = @role,
          [status] = @status,
          [level] = @level,
          [exp] = @exp,
          [coins] = @coins,
          [rankPoints] = @rankPoints
      WHERE [id] = @id
    `);

    if (result.rowsAffected[0] === 0) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy người dùng để sửa!' });
    }

    console.log(`👤 Admin đã cập nhật người dùng: ${userId} (Role: ${role}, Status: ${status})`);
    return res.json({ success: true, message: 'Cập nhật người dùng thành công!' });

  } catch (err) {
    console.error('Lỗi khi Admin cập nhật user: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi cập nhật người dùng!' });
  }
};

// Xóa người dùng (Admin) - Cascade xóa tự động ở SQL Server
exports.deleteUser = async (req, res) => {
  const userId = req.params.id;

  try {
    const pool = await getPool();
    const request = pool.request();
    request.input('id', sql.VarChar, userId);

    const result = await request.query('DELETE FROM [users] WHERE [id] = @id');

    if (result.rowsAffected[0] === 0) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy người dùng để xóa!' });
    }

    console.log(`👤 Admin đã xóa người dùng: ${userId}`);
    return res.json({ success: true, message: 'Xóa người dùng thành công!' });

  } catch (err) {
    console.error('Lỗi khi Admin xóa user: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi xóa người dùng!' });
  }
};

