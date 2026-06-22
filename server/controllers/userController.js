const { getPool, sql } = require('../config/db');
const authController = require('./authController');

// Đồng bộ trạng thái chơi (Save Game)
exports.syncUserData = async (req, res) => {
  const user = req.body;

  if (!user || !user.id) {
    return res.status(400).json({ success: false, message: 'Dữ liệu đồng bộ không hợp lệ!' });
  }

  // Bảo mật: Kiểm tra ID từ Token có trùng khớp với ID dữ liệu đồng bộ không
  if (req.user.id !== user.id) {
    console.warn(`🚨 [Bảo mật] Người chơi '${req.user.id}' đang cố gắng ghi đè dữ liệu của người chơi '${user.id}'!`);
    return res.status(403).json({ success: false, message: 'Từ chối truy cập! Bạn không có quyền chỉnh sửa dữ liệu của tài khoản khác.' });
  }

  try {
    const pool = await getPool();
    const transaction = new sql.Transaction(pool);
    await transaction.begin();

    try {
      // 1. Cập nhật thông số cơ bản của User & Weekly Login trực tiếp
      const updateUReq = new sql.Request(transaction);
      updateUReq.input('id', sql.VarChar, user.id);
      updateUReq.input('exp', sql.Int, user.exp);
      updateUReq.input('coins', sql.Int, user.coins);
      updateUReq.input('rankPoints', sql.Int, user.rankPoints || 0);
      updateUReq.input('wins', sql.Int, user.wins || 0);
      updateUReq.input('totalGames', sql.Int, user.totalGames || 0);

      // Cột của Weekly Login
      const weekStartDate = user.weeklyLogin?.weekStartDate || '';
      const isRewardClaimed = user.weeklyLogin?.isRewardClaimed ? 1 : 0;

      updateUReq.input('weekStart', sql.VarChar, weekStartDate);
      updateUReq.input('claimed', sql.Bit, isRewardClaimed);

      await updateUReq.query(`
        UPDATE [users]
        SET [exp] = @exp,
            [coins] = @coins,
            [rankPoints] = @rankPoints,
            [wins] = @wins,
            [totalGames] = @totalGames,
            [weekStartDate] = @weekStart,
            [isRewardClaimed] = @claimed
        WHERE [id] = @id
      `);

      // 1a. Đồng bộ loginDates vào bảng user_login_dates (chuẩn hóa 1NF)
      const delLogin = new sql.Request(transaction);
      delLogin.input('userId', sql.VarChar, user.id);
      await delLogin.query('DELETE FROM [user_login_dates] WHERE [userId] = @userId');
      const loginDates = user.weeklyLogin?.loginDates || [];
      for (const dateStr of loginDates) {
        const addLogin = new sql.Request(transaction);
        addLogin.input('userId', sql.VarChar, user.id);
        addLogin.input('loginDate', sql.VarChar, dateStr);
        await addLogin.query('INSERT INTO [user_login_dates] ([userId], [loginDate]) VALUES (@userId, @loginDate)');
      }

      // 1b. Cập nhật điểm kỷ lục vào bảng user_solo_records [NEW] (Sử dụng UPSERT)
      const updateSoloReq = new sql.Request(transaction);
      updateSoloReq.input('userId', sql.VarChar, user.id);
      updateSoloReq.input('bestSurvivor', sql.Int, user.bestSurvivor || 0);
      updateSoloReq.input('bestQuick10', sql.Int, user.bestQuick10 || 0);
      updateSoloReq.input('bestTimeRush', sql.Int, user.bestTimeRush || 0);

      await updateSoloReq.query(`
        IF EXISTS (SELECT 1 FROM [user_solo_records] WHERE [userId] = @userId)
        BEGIN
          UPDATE [user_solo_records]
          SET [bestSurvivor] = @bestSurvivor,
              [bestQuick10] = @bestQuick10,
              [bestTimeRush] = @bestTimeRush
          WHERE [userId] = @userId
        END
        ELSE
        BEGIN
          INSERT INTO [user_solo_records] ([userId], [bestSurvivor], [bestQuick10], [bestTimeRush])
          VALUES (@userId, @bestSurvivor, @bestQuick10, @bestTimeRush)
        END
      `);

      // Xóa tiến độ học và cấp độ hoàn thành cũ trước khi đồng bộ
      const delCl = new sql.Request(transaction);
      delCl.input('userId', sql.VarChar, user.id);
      await delCl.query('DELETE FROM [user_set_completed_levels] WHERE [userId] = @userId');
      await delCl.query('DELETE FROM [user_set_progress] WHERE [userId] = @userId');

      const processedSetIds = new Set();

      // 3. Đồng bộ Learned Sets
      if (user.learnedSets) {
        for (const setId of user.learnedSets) {
          if (processedSetIds.has(setId)) continue;
          processedSetIds.add(setId);

          const addLs = new sql.Request(transaction);
          addLs.input('userId', sql.VarChar, user.id);
          addLs.input('setId', sql.VarChar, setId);
          await addLs.query('INSERT INTO [user_set_progress] ([userId], [setId], [status]) VALUES (@userId, @setId, \'completed\')');
        }
      }

      // 4. Đồng bộ Saved Set Levels (gộp vào user_set_progress.currentLevel)
      if (user.savedSetLevels) {
        for (const s of user.savedSetLevels) {
          const addSsl = new sql.Request(transaction);
          addSsl.input('userId', sql.VarChar, user.id);
          addSsl.input('setId', sql.VarChar, s.setId);
          addSsl.input('lvl', sql.NVarChar, s.level);
          await addSsl.query(`
            IF EXISTS (SELECT 1 FROM [user_set_progress] WHERE [userId] = @userId AND [setId] = @setId)
              UPDATE [user_set_progress] SET [currentLevel] = @lvl WHERE [userId] = @userId AND [setId] = @setId
            ELSE
              INSERT INTO [user_set_progress] ([userId], [setId], [status], [currentLevel]) VALUES (@userId, @setId, 'learning', @lvl)
          `);
        }
      }

      // 5. Đồng bộ Set Progress & Completed Levels
      if (user.setProgress) {
        for (const p of user.setProgress) {
          if (processedSetIds.has(p.setId)) continue;
          processedSetIds.add(p.setId);

          const addSp = new sql.Request(transaction);
          addSp.input('userId', sql.VarChar, user.id);
          addSp.input('setId', sql.VarChar, p.setId);
          await addSp.query('INSERT INTO [user_set_progress] ([userId], [setId], [status]) VALUES (@userId, @setId, \'learning\')');

          if (p.completedLevels && p.completedLevels.length > 0) {
            for (const lvl of p.completedLevels) {
              const addCl = new sql.Request(transaction);
              addCl.input('userId', sql.VarChar, user.id);
              addCl.input('setId', sql.VarChar, p.setId);
              addCl.input('lvl', sql.NVarChar, lvl);
              await addCl.query('INSERT INTO [user_set_completed_levels] ([userId], [setId], [level]) VALUES (@userId, @setId, @lvl)');
            }
          }
        }
      }

      // 6. Đồng bộ Word Progress
      if (user.wordProgress) {
        const delWp = new sql.Request(transaction);
        delWp.input('userId', sql.VarChar, user.id);
        await delWp.query('DELETE FROM [user_word_progress] WHERE [userId] = @userId');

        for (const wp of user.wordProgress) {
          const addWp = new sql.Request(transaction);
          addWp.input('userId', sql.VarChar, user.id);
          addWp.input('wordId', sql.VarChar, wp.wordId);
          addWp.input('status', sql.Int, wp.status);
          await addWp.query('INSERT INTO [user_word_progress] ([userId], [wordId], [status]) VALUES (@userId, @wordId, @status)');
        }
      }

      // 7. Đồng bộ Shop History
      if (user.shopHistory) {
        const delSh = new sql.Request(transaction);
        delSh.input('userId', sql.VarChar, user.id);
        await delSh.query('DELETE FROM [user_shop_history] WHERE [userId] = @userId');

        for (const sh of user.shopHistory) {
          const addSh = new sql.Request(transaction);
          addSh.input('userId', sql.VarChar, user.id);
          addSh.input('itemName', sql.NVarChar, sh.itemName);
          addSh.input('price', sql.Int, sh.price);
          addSh.input('date', sql.VarChar, sh.date);
          await addSh.query('INSERT INTO [user_shop_history] ([userId], [itemName], [price], [date]) VALUES (@userId, @itemName, @price, @date)');
        }
      }

      // 8. Đồng bộ Battle History & Rounds
      if (user.battleHistory) {
        // Cascade delete tự động xóa rounds khi trận đấu bị xóa
        const delBh = new sql.Request(transaction);
        delBh.input('userId', sql.VarChar, user.id);
        await delBh.query('DELETE FROM [user_battle_history] WHERE [userId] = @userId');

        for (const bh of user.battleHistory) {
          const addBh = new sql.Request(transaction);
          addBh.input('matchId', sql.VarChar, bh.matchId);
          addBh.input('userId', sql.VarChar, user.id);
          addBh.input('date', sql.VarChar, bh.date);
          addBh.input('isRanked', sql.Bit, bh.isRanked ? 1 : 0);
          addBh.input('isWin', sql.Bit, bh.isWin ? 1 : 0);
          addBh.input('opp', sql.NVarChar, bh.opponentName);
          addBh.input('pHp', sql.Int, bh.playerFinalHP);
          addBh.input('eHp', sql.Int, bh.enemyFinalHP);
          addBh.input('correct', sql.Int, bh.correctCount);
          addBh.input('total', sql.Int, bh.totalRounds);

          await addBh.query(`
            INSERT INTO [user_battle_history] (
              [matchId], [userId], [date], [isRanked], [isWin], [opponentName], 
              [playerFinalHP], [enemyFinalHP], [correctCount], [totalRounds]
            ) VALUES (
              @matchId, @userId, @date, @isRanked, @isWin, @opp, @pHp, @eHp, @correct, @total
            )
          `);

          if (bh.rounds && bh.rounds.length > 0) {
            for (const rnd of bh.rounds) {
              const addRnd = new sql.Request(transaction);
              addRnd.input('matchId', sql.VarChar, bh.matchId);
              addRnd.input('q', sql.NVarChar, rnd.question);
              addRnd.input('cAns', sql.NVarChar, rnd.correctAnswer);
              addRnd.input('pAns', sql.NVarChar, rnd.playerAnswer);
              addRnd.input('img', sql.NVarChar, rnd.imageUrl || '');
              addRnd.input('isC', sql.Bit, rnd.isCorrect ? 1 : 0);
              addRnd.input('isT', sql.Bit, rnd.isTimeout ? 1 : 0);

              await addRnd.query(`
                INSERT INTO [battle_rounds] (
                  [matchId], [question], [correctAnswer], [playerAnswer], [imageUrl], [isCorrect], [isTimeout]
                ) VALUES (
                  @matchId, @q, @cAns, @pAns, @img, @isC, @isT
                )
              `);
            }
          }
        }
      }

      // 9. Đồng bộ Inventory (chuẩn hóa 2NF: chỉ lưu userId, itemId + trạng thái)
      if (user.inventory) {
        const delInv = new sql.Request(transaction);
        delInv.input('userId', sql.VarChar, user.id);
        await delInv.query('DELETE FROM [user_inventory] WHERE [userId] = @userId');

        for (const item of user.inventory) {
          const addInv = new sql.Request(transaction);
          addInv.input('userId', sql.VarChar, user.id);
          addInv.input('itemId', sql.VarChar, item.id);
          addInv.input('qty', sql.Int, item.quantity || 1);
          addInv.input('isEq', sql.Bit, item.isEquipped ? 1 : 0);
          addInv.input('isCbt', sql.Bit, item.isCombatItem ? 1 : 0);

          await addInv.query(`
            INSERT INTO [user_inventory] (
              [userId], [itemId], [quantity], [isEquipped], [isCombatItem]
            ) VALUES (
              @userId, @itemId, @qty, @isEq, @isCbt
            )
          `);
        }
      }

      // 10. Đồng bộ Quests
      if (user.quests) {
        const delQ = new sql.Request(transaction);
        delQ.input('userId', sql.VarChar, user.id);
        await delQ.query('DELETE FROM [user_quests] WHERE [userId] = @userId');

        for (const q of user.quests) {
          const addQ = new sql.Request(transaction);
          addQ.input('userId', sql.VarChar, user.id);
          addQ.input('questId', sql.VarChar, q.id);
          addQ.input('prog', sql.Int, q.currentProgress);
          addQ.input('claimed', sql.Bit, q.isClaimed ? 1 : 0);

          await addQ.query(`
            INSERT INTO [user_quests] ([userId], [questId], [currentProgress], [isClaimed])
            VALUES (@userId, @questId, @prog, @claimed)
          `);
        }
      }

      // 11. Đồng bộ Achievements
      if (user.achievements) {
        const delAch = new sql.Request(transaction);
        delAch.input('userId', sql.VarChar, user.id);
        await delAch.query('DELETE FROM [user_achievements] WHERE [userId] = @userId');

        for (const a of user.achievements) {
          const addAch = new sql.Request(transaction);
          addAch.input('userId', sql.VarChar, user.id);
          addAch.input('achId', sql.VarChar, a.id);
          addAch.input('prog', sql.Int, a.currentProgress);
          addAch.input('isU', sql.Bit, a.isUnlocked ? 1 : 0);
          addAch.input('uDate', sql.NVarChar, a.unlockDate || '');

          await addAch.query(`
            INSERT INTO [user_achievements] ([userId], [achievementId], [currentProgress], [isUnlocked], [unlockDate])
            VALUES (@userId, @achId, @prog, @isU, @uDate)
          `);
        }
      }

      await transaction.commit();
      console.log(`💾 Đồng bộ và lưu dữ liệu tiến trình thành công cho User: ${user.username}`);
      return res.json({ success: true, message: 'Đồng bộ tiến trình game thành công!' });

    } catch (err) {
      await transaction.rollback();
      throw err;
    }
  } catch (err) {
    console.error('Lỗi khi đồng bộ tiến trình: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi đồng bộ dữ liệu!' });
  }
};

// Lấy thông tin chi tiết đầy đủ của người chơi từ DB
exports.getUserProfile = async (req, res) => {
  try {
    const pool = await getPool();
    const fullProfile = await authController.getUserFullProfile(pool, req.user.id);
    if (!fullProfile) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy thông tin tài khoản!' });
    }
    return res.json({
      success: true,
      user: fullProfile
    });
  } catch (err) {
    console.error('Lỗi khi lấy thông tin người chơi: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi lấy thông tin!' });
  }
};

// Cho phép người dùng tự đổi tên hiển thị (username)
exports.changeUsername = async (req, res) => {
  const { newUsername } = req.body; // newUsername in payload holds the requested display name
  const userId = req.user.id;

  if (!newUsername || !newUsername.trim()) {
    return res.status(400).json({ success: false, message: 'Tên mới không được để trống!' });
  }

  const cleanDisplayName = newUsername.trim();

  // Validate length/format basic
  if (cleanDisplayName.length < 3 || cleanDisplayName.length > 20) {
    return res.status(400).json({ success: false, message: 'Tên phải dài từ 3 đến 20 ký tự!' });
  }

  try {
    const pool = await getPool();

    // Cập nhật tên hiển thị mới và lấy username
    const updateReq = pool.request();
    updateReq.input('newDisplayName', sql.NVarChar, cleanDisplayName);
    updateReq.input('userId', sql.VarChar, userId);
    const result = await updateReq.query(`
      UPDATE [users] SET [displayName] = @newDisplayName WHERE [id] = @userId;
      SELECT [username] FROM [users] WHERE [id] = @userId;
    `);

    const username = result.recordset[0]?.username || '';

    console.log(`👤 Người dùng ID ${userId} đã đổi tên hiển thị thành '${cleanDisplayName}'`);
    return res.json({ success: true, message: 'Đổi tên hiển thị thành công!', username: username, displayName: cleanDisplayName });

  } catch (err) {
    console.error('Lỗi khi đổi tên hiển thị: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi đổi tên hiển thị!' });
  }
};

