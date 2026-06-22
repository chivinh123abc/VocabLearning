const fs = require('fs');
const path = require('path');
const bcrypt = require('bcryptjs');
const { getPool, initDatabase, sql } = require('../config/db');

async function seed() {
  try {
    // Khởi tạo Database và Bảng trước (forceRecreate = true để xóa database cũ bị trùng lặp)
    await initDatabase(true);

    const dbPool = await getPool();
    console.log('📖 Đang đọc dữ liệu giả lập từ db.json...');

    // Đường dẫn tới db.json của Unity
    const dbJsonPath = path.join(__dirname, '../../Assets/Resources/Mockdata/db.json');
    if (!fs.existsSync(dbJsonPath)) {
      throw new Error(`🚨 Lỗi: Không tìm thấy file db.json tại đường dẫn: ${dbJsonPath}`);
    }

    const dbData = JSON.parse(fs.readFileSync(dbJsonPath, 'utf8'));

    // Tạo các Set chứa ID hợp lệ để validate dữ liệu động từ db.json
    const validWordIds = new Set((dbData.words || []).map(w => w.id));
    const validSetIds = new Set((dbData.vocabSets || []).map(s => s.id));
    const validQuestIds = new Set((dbData.questPool || []).map(q => q.id));
    const validAchievementIds = new Set((dbData.achievements || []).map(a => a.id));

    // Bắt đầu một Transaction để đảm bảo toàn vẹn dữ liệu
    const transaction = new sql.Transaction(dbPool);
    await transaction.begin();

    try {
      console.log('🧹 Đang làm sạch cơ sở dữ liệu cũ...');
      await transaction.request().query(`
        DELETE FROM [battle_rounds];
        DELETE FROM [user_battle_history];
        DELETE FROM [user_shop_history];
        DELETE FROM [user_quests];
        DELETE FROM [quests];
        DELETE FROM [user_inventory];
        DELETE FROM [shop_items];
        DELETE FROM [user_achievements];
        DELETE FROM [achievements];
        DELETE FROM [user_word_progress];
        DELETE FROM [user_set_completed_levels];
        DELETE FROM [user_set_progress];
        DELETE FROM [user_saved_set_levels];
        DELETE FROM [vocab_set_level_words];
        DELETE FROM [vocab_set_levels];
        DELETE FROM [vocab_set_words];
        DELETE FROM [vocab_sets];
        DELETE FROM [words];
        DELETE FROM [user_solo_records];
        DELETE FROM [users];
      `);
      console.log('✨ Đã dọn dẹp xong cơ sở dữ liệu.');

      // 1. Nhập Words
      console.log(`🔤 Đang nạp ${dbData.words.length} từ vựng...`);
      for (const w of dbData.words) {
        const req = new sql.Request(transaction);
        req.input('id', sql.VarChar, w.id);
        req.input('word', sql.NVarChar, w.word);
        req.input('meaning', sql.NVarChar, w.meaning);
        req.input('rankRequired', sql.VarChar, w.rankRequired || 'Dong');
        req.input('imageUrl', sql.NVarChar, w.imageUrl || '');
        req.input('imageSub', sql.NVarChar, w.imageSub || '');
        await req.query(`
          INSERT INTO [words] ([id], [word], [meaning], [rankRequired], [imageUrl], [imageSub])
          VALUES (@id, @word, @meaning, @rankRequired, @imageUrl, @imageSub)
        `);
      }

      // 2. Nhập Vocab Sets & Vocab Set Words & Levels
      console.log(`📚 Đang nạp ${dbData.vocabSets.length} Bộ từ vựng và phân chia Cấp độ...`);
      for (const set of dbData.vocabSets) {
        const reqSet = new sql.Request(transaction);
        reqSet.input('id', sql.VarChar, set.id);
        reqSet.input('title', sql.NVarChar, set.title);
        reqSet.input('description', sql.NVarChar, set.description || '');
        reqSet.input('category', sql.NVarChar, set.category || '');
        reqSet.input('difficulty', sql.NVarChar, set.difficulty || '');
        reqSet.input('rankRequired', sql.VarChar, set.rankRequired || 'Dong');
        await reqSet.query(`
          INSERT INTO [vocab_sets] ([id], [title], [description], [category], [difficulty], [rankRequired])
          VALUES (@id, @title, @description, @category, @difficulty, @rankRequired)
        `);

        // N-N: vocab_set_words
        if (set.wordIds && set.wordIds.length > 0) {
          for (const wordId of set.wordIds) {
            const reqLink = new sql.Request(transaction);
            reqLink.input('setId', sql.VarChar, set.id);
            reqLink.input('wordId', sql.VarChar, wordId);
            await reqLink.query(`
              INSERT INTO [vocab_set_words] ([setId], [wordId])
              VALUES (@setId, @wordId)
            `);
          }
        }

        // Levels & Level Words
        if (set.levels && set.levels.length > 0) {
          for (const lvl of set.levels) {
            const reqLvl = new sql.Request(transaction);
            reqLvl.input('setId', sql.VarChar, set.id);
            reqLvl.input('diff', sql.NVarChar, lvl.difficulty);
            await reqLvl.query(`
              INSERT INTO [vocab_set_levels] ([setId], [difficulty])
              VALUES (@setId, @diff)
            `);

            if (lvl.wordIds && lvl.wordIds.length > 0) {
              for (const wordId of lvl.wordIds) {
                const reqLvlWord = new sql.Request(transaction);
                reqLvlWord.input('setId', sql.VarChar, set.id);
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
      }

      // 3. Nhập Achievements (Bể thành tựu tĩnh)
      console.log(`🏆 Đang nạp ${dbData.achievements.length} Thành tựu tĩnh...`);
      for (const ach of dbData.achievements) {
        const reqAch = new sql.Request(transaction);
        reqAch.input('id', sql.VarChar, ach.id);
        reqAch.input('icon', sql.NVarChar, ach.icon || '');
        reqAch.input('title', sql.NVarChar, ach.title);
        reqAch.input('description', sql.NVarChar, ach.description || '');
        reqAch.input('maxProgress', sql.Int, ach.maxProgress);
        await reqAch.query(`
          INSERT INTO [achievements] ([id], [icon], [title], [description], [maxProgress])
          VALUES (@id, @icon, @title, @description, @maxProgress)
        `);
      }

      // 4. Nhập Quests (Bể nhiệm vụ tĩnh từ questPool)
      console.log(`⚔️ Đang nạp ${dbData.questPool.length} Nhiệm vụ tĩnh từ Pool...`);
      for (const q of dbData.questPool) {
        const reqQ = new sql.Request(transaction);
        reqQ.input('id', sql.VarChar, q.id);
        reqQ.input('title', sql.NVarChar, q.title);
        reqQ.input('description', sql.NVarChar, q.description || '');
        reqQ.input('maxProgress', sql.Int, q.maxProgress);
        reqQ.input('rewardCoins', sql.Int, q.rewardCoins);
        reqQ.input('rewardExp', sql.Int, q.rewardExp);
        reqQ.input('questType', sql.VarChar, q.questType);
        await reqQ.query(`
          INSERT INTO [quests] ([id], [title], [description], [maxProgress], [rewardCoins], [rewardExp], [questType])
          VALUES (@id, @title, @description, @maxProgress, @rewardCoins, @rewardExp, @questType)
        `);
      }

      // 5. Nhập Shop Items
      console.log(`🛒 Đang nạp ${dbData.shopItems.length} Vật phẩm trong Shop...`);
      for (const item of dbData.shopItems) {
        const reqItem = new sql.Request(transaction);
        reqItem.input('id', sql.VarChar, item.id);
        reqItem.input('name', sql.NVarChar, item.name);
        reqItem.input('description', sql.NVarChar, item.description || '');
        reqItem.input('icon', sql.NVarChar, item.icon || '');
        reqItem.input('price', sql.Int, item.price);
        reqItem.input('rarity', sql.NVarChar, item.rarity || 'Common');
        reqItem.input('category', sql.NVarChar, item.category || 'Cosmetic');
        reqItem.input('equipType', sql.NVarChar, item.equipType || '');
        await reqItem.query(`
          INSERT INTO [shop_items] ([id], [name], [description], [icon], [price], [rarity], [category], [equipType])
          VALUES (@id, @name, @description, @icon, @price, @rarity, @category, @equipType)
        `);
      }

      // 6. Nhập Người dùng (Registered Users + Leaderboard Users + Current User)
      const allUsersMap = new Map();

      // Gom tất cả users từ registeredUsers, leaderboardUsers và currentUser
      if (dbData.registeredUsers) {
        for (const u of dbData.registeredUsers) {
          allUsersMap.set(u.username, u);
        }
      }
      if (dbData.leaderboardUsers) {
        for (const u of dbData.leaderboardUsers) {
          if (!allUsersMap.has(u.username)) {
            // Leaderboard bots không có mật khẩu -> đặt mặc định là rỗng hoặc ngẫu nhiên
            u.password = u.password || '';
            u.role = u.role || 'user';
            allUsersMap.set(u.username, u);
          }
        }
      }
      if (dbData.currentUser && !allUsersMap.has(dbData.currentUser.username)) {
        allUsersMap.set(dbData.currentUser.username, dbData.currentUser);
      }

      console.log(`👤 Đang chuẩn hóa và mã hóa mật khẩu cho ${allUsersMap.size} Tài khoản người chơi...`);

      const insertedEmails = new Set();

      for (const [username, u] of allUsersMap.entries()) {
        // Để reset hoàn toàn tiến độ học tập trên DB khi chạy db:setup:
        u.learnedSets = [];
        u.savedSetLevels = [];
        u.setProgress = [];
        u.wordProgress = [];
        u.shopHistory = [];
        u.battleHistory = [];

        const reqU = new sql.Request(transaction);

        // Băm mật khẩu (Bcrypt) nếu có mật khẩu thô
        let hashedPassword = '';
        if (u.password) {
          hashedPassword = bcrypt.hashSync(u.password, 10);
        } else {
          hashedPassword = bcrypt.hashSync(Math.random().toString(36), 10); // bot ngẫu nhiên
        }

        // Đảm bảo tính duy nhất của email trong DB
        let userEmail = u.email;
        if (!userEmail || insertedEmails.has(userEmail)) {
          userEmail = `${u.username}@vocab.com`;
        }
        if (insertedEmails.has(userEmail)) {
          userEmail = `${u.username}_${Math.floor(Math.random() * 1000)}@vocab.com`;
        }
        insertedEmails.add(userEmail);

        reqU.input('id', sql.VarChar, u.id);
        reqU.input('username', sql.NVarChar, u.username);
        reqU.input('displayName', sql.NVarChar, u.displayName || u.username);
        reqU.input('email', sql.NVarChar, userEmail);
        reqU.input('password', sql.VarChar, hashedPassword);
        reqU.input('role', sql.VarChar, u.role || 'user');
        reqU.input('exp', sql.Int, u.exp || 0);
        reqU.input('coins', sql.Int, u.coins || 0);
        reqU.input('rankPoints', sql.Int, u.rankPoints || 0);
        reqU.input('wins', sql.Int, u.wins || 0);
        reqU.input('totalGames', sql.Int, u.totalGames || 0);

        // Nạp trực tiếp Weekly Login
        const weekStartDate = u.weeklyLogin?.weekStartDate || '';
        const isRewardClaimed = u.weeklyLogin?.isRewardClaimed ? 1 : 0;
        const loginDatesStr = (u.weeklyLogin?.loginDates || []).join(',');

        reqU.input('weekStart', sql.VarChar, weekStartDate);
        reqU.input('claimed', sql.Bit, isRewardClaimed);
        reqU.input('loginDates', sql.NVarChar, loginDatesStr);

        await reqU.query(`
          INSERT INTO [users] (
            [id], [username], [displayName], [email], [password], [role], [exp], [coins], 
            [rankPoints], [wins], [totalGames], 
            [weekStartDate], [isRewardClaimed], [loginDates]
          ) VALUES (
            @id, @username, @displayName, @email, @password, @role, @exp, @coins, 
            @rankPoints, @wins, @totalGames, 
            @weekStart, @claimed, @loginDates
          )
        `);

        // Nạp điểm kỷ lục vào bảng user_solo_records [NEW]
        const reqUsrRecord = new sql.Request(transaction);
        reqUsrRecord.input('userId', sql.VarChar, u.id);
        reqUsrRecord.input('bestSurvivor', sql.Int, u.bestSurvivor || 0);
        reqUsrRecord.input('bestQuick10', sql.Int, u.bestQuick10 || 0);
        reqUsrRecord.input('bestTimeRush', sql.Int, u.bestTimeRush || 0);
        await reqUsrRecord.query(`
          INSERT INTO [user_solo_records] ([userId], [bestSurvivor], [bestQuick10], [bestTimeRush])
          VALUES (@userId, @bestSurvivor, @bestQuick10, @bestTimeRush)
        `);

        // Learned Sets (Được chèn vào user_set_progress với status = 'completed')
        if (u.learnedSets && u.learnedSets.length > 0) {
          for (const setId of u.learnedSets) {
            if (!validSetIds.has(setId)) {
              console.warn(`⚠️ Bỏ qua learnedSet '${setId}' của user '${u.username}' do Set không tồn tại.`);
              continue;
            }
            const reqLs = new sql.Request(transaction);
            reqLs.input('userId', sql.VarChar, u.id);
            reqLs.input('setId', sql.VarChar, setId);
            await reqLs.query(`
              INSERT INTO [user_set_progress] ([userId], [setId], [status])
              VALUES (@userId, @setId, 'completed')
            `);
          }
        }

        // Saved Set Levels
        if (u.savedSetLevels && u.savedSetLevels.length > 0) {
          for (const s of u.savedSetLevels) {
            if (!validSetIds.has(s.setId)) {
              console.warn(`⚠️ Bỏ qua savedSetLevel cho Set '${s.setId}' của user '${u.username}' do Set không tồn tại.`);
              continue;
            }
            const reqSsl = new sql.Request(transaction);
            reqSsl.input('userId', sql.VarChar, u.id);
            reqSsl.input('setId', sql.VarChar, s.setId);
            reqSsl.input('level', sql.NVarChar, s.level);
            await reqSsl.query(`
              INSERT INTO [user_saved_set_levels] ([userId], [setId], [level])
              VALUES (@userId, @setId, @level)
            `);
          }
        }

        // Set Progress & Completed Levels (Được chèn vào user_set_progress với status = 'learning')
        if (u.setProgress && u.setProgress.length > 0) {
          for (const p of u.setProgress) {
            if (!validSetIds.has(p.setId)) {
              console.warn(`⚠️ Bỏ qua setProgress cho Set '${p.setId}' của user '${u.username}' do Set không tồn tại.`);
              continue;
            }
            const reqSp = new sql.Request(transaction);
            reqSp.input('userId', sql.VarChar, u.id);
            reqSp.input('setId', sql.VarChar, p.setId);
            await reqSp.query(`
              INSERT INTO [user_set_progress] ([userId], [setId], [status])
              VALUES (@userId, @setId, 'learning')
            `);

            if (p.completedLevels && p.completedLevels.length > 0) {
              for (const lvl of p.completedLevels) {
                const reqCl = new sql.Request(transaction);
                reqCl.input('userId', sql.VarChar, u.id);
                reqCl.input('setId', sql.VarChar, p.setId);
                reqCl.input('lvl', sql.NVarChar, lvl);
                await reqCl.query(`
                  INSERT INTO [user_set_completed_levels] ([userId], [setId], [level])
                  VALUES (@userId, @setId, @lvl)
                `);
              }
            }
          }
        }

        // Word Progress
        if (u.wordProgress && u.wordProgress.length > 0) {
          for (const wp of u.wordProgress) {
            if (!validWordIds.has(wp.wordId)) {
              console.warn(`⚠️ Bỏ qua wordProgress cho Từ '${wp.wordId}' của user '${u.username}' do Từ không tồn tại.`);
              continue;
            }
            const reqWp = new sql.Request(transaction);
            reqWp.input('userId', sql.VarChar, u.id);
            reqWp.input('wordId', sql.VarChar, wp.wordId);
            reqWp.input('status', sql.Int, wp.status);
            await reqWp.query(`
              INSERT INTO [user_word_progress] ([userId], [wordId], [status])
              VALUES (@userId, @wordId, @status)
            `);
          }
        }

        // Shop History
        if (u.shopHistory && u.shopHistory.length > 0) {
          for (const sh of u.shopHistory) {
            const reqSh = new sql.Request(transaction);
            reqSh.input('userId', sql.VarChar, u.id);
            reqSh.input('itemName', sql.NVarChar, sh.itemName);
            reqSh.input('price', sql.Int, sh.price);
            reqSh.input('date', sql.VarChar, sh.date);
            await reqSh.query(`
              INSERT INTO [user_shop_history] ([userId], [itemName], [price], [date])
              VALUES (@userId, @itemName, @price, @date)
            `);
          }
        }

        // Battle History & Battle Rounds
        if (u.battleHistory && u.battleHistory.length > 0) {
          for (const bh of u.battleHistory) {
            const reqBh = new sql.Request(transaction);
            reqBh.input('matchId', sql.VarChar, bh.matchId);
            reqBh.input('userId', sql.VarChar, u.id);
            reqBh.input('date', sql.VarChar, bh.date);
            reqBh.input('isRanked', sql.Bit, bh.isRanked ? 1 : 0);
            reqBh.input('isWin', sql.Bit, bh.isWin ? 1 : 0);
            reqBh.input('opp', sql.NVarChar, bh.opponentName);
            reqBh.input('pHp', sql.Int, bh.playerFinalHP);
            reqBh.input('eHp', sql.Int, bh.enemyFinalHP);
            reqBh.input('correct', sql.Int, bh.correctCount);
            reqBh.input('total', sql.Int, bh.totalRounds);
            await reqBh.query(`
              INSERT INTO [user_battle_history] (
                [matchId], [userId], [date], [isRanked], [isWin], [opponentName], 
                [playerFinalHP], [enemyFinalHP], [correctCount], [totalRounds]
              ) VALUES (
                @matchId, @userId, @date, @isRanked, @isWin, @opp, @pHp, @eHp, @correct, @total
              )
            `);

            if (bh.rounds && bh.rounds.length > 0) {
              for (const rnd of bh.rounds) {
                const reqRnd = new sql.Request(transaction);
                reqRnd.input('matchId', sql.VarChar, bh.matchId);
                reqRnd.input('q', sql.NVarChar, rnd.question);
                reqRnd.input('cAns', sql.NVarChar, rnd.correctAnswer);
                reqRnd.input('pAns', sql.NVarChar, rnd.playerAnswer);
                reqRnd.input('img', sql.NVarChar, rnd.imageUrl || '');
                reqRnd.input('isC', sql.Bit, rnd.isCorrect ? 1 : 0);
                reqRnd.input('isT', sql.Bit, rnd.isTimeout ? 1 : 0);
                await reqRnd.query(`
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
      }

      // 7. Nhập Inventory, Quests và Achievements của Current User (test_user_1)
      const currentUserId = dbData.currentUser.id; // "test_user_1"
      console.log(`🎒 Nạp Kho đồ, Nhiệm vụ và Tiến trình thành tựu của Tài khoản mẫu (${currentUserId})...`);

      // Inventory
      if (dbData.inventory && dbData.inventory.length > 0) {
        for (const item of dbData.inventory) {
          const reqInv = new sql.Request(transaction);
          reqInv.input('userId', sql.VarChar, currentUserId);
          reqInv.input('itemId', sql.VarChar, item.id);
          reqInv.input('icon', sql.NVarChar, item.icon || '');
          reqInv.input('name', sql.NVarChar, item.name);
          reqInv.input('desc', sql.NVarChar, item.description || '');
          reqInv.input('qty', sql.Int, item.quantity || 1);
          reqInv.input('rarity', sql.NVarChar, item.rarity || 'Common');
          reqInv.input('cat', sql.NVarChar, item.category || 'Cosmetic');
          reqInv.input('eq', sql.NVarChar, item.equipType || '');
          reqInv.input('isEq', sql.Bit, item.isEquipped ? 1 : 0);
          reqInv.input('isCbt', sql.Bit, item.isCombatItem ? 1 : 0);

          await reqInv.query(`
            INSERT INTO [user_inventory] (
              [userId], [itemId], [icon], [name], [description], [quantity], [rarity], [category], [equipType], [isEquipped], [isCombatItem]
            ) VALUES (
              @userId, @itemId, @icon, @name, @desc, @qty, @rarity, @cat, @eq, @isEq, @isCbt
            )
          `);
        }
      }

      // Quests (user_quests)
      if (dbData.quests && dbData.quests.length > 0) {
        for (const q of dbData.quests) {
          if (!validQuestIds.has(q.id)) {
            console.warn(`⚠️ Bỏ qua userQuest '${q.id}' của user mẫu do Quest không tồn tại trong questPool.`);
            continue;
          }
          const reqQ = new sql.Request(transaction);
          reqQ.input('userId', sql.VarChar, currentUserId);
          reqQ.input('questId', sql.VarChar, q.id);
          reqQ.input('prog', sql.Int, 0); // Reset progress về 0 khi khởi tạo DB
          reqQ.input('claimed', sql.Bit, 0); // Reset claimed về false khi khởi tạo DB

          await reqQ.query(`
            INSERT INTO [user_quests] ([userId], [questId], [currentProgress], [isClaimed])
            VALUES (@userId, @questId, @prog, @claimed)
          `);

        }
      }

      // Achievements (user_achievements)
      if (dbData.achievements && dbData.achievements.length > 0) {
        for (const ach of dbData.achievements) {
          if (!validAchievementIds.has(ach.id)) {
            console.warn(`⚠️ Bỏ qua userAchievement '${ach.id}' của user mẫu do Achievement không tồn tại.`);
            continue;
          }
          const reqUa = new sql.Request(transaction);
          reqUa.input('userId', sql.VarChar, currentUserId);
          reqUa.input('achId', sql.VarChar, ach.id);
          reqUa.input('prog', sql.Int, 0); // Reset progress về 0 khi khởi tạo DB
          reqUa.input('isU', sql.Bit, 0); // Reset trạng thái mở khóa về false khi khởi tạo DB
          reqUa.input('uDate', sql.NVarChar, ''); // Reset ngày mở khóa khi khởi tạo DB

          await reqUa.query(`
            INSERT INTO [user_achievements] ([userId], [achievementId], [currentProgress], [isUnlocked], [unlockDate])
            VALUES (@userId, @achId, @prog, @isU, @uDate)
          `);

        }
      }

      await transaction.commit();
      console.log('🎉 DI CƯ VÀ SEED DỮ LIỆU THÀNH CÔNG! Dữ liệu cũ đã được nạp hoàn chỉnh vào SQL Server quan hệ.');
      process.exit(0);
    } catch (err) {
      await transaction.rollback();
      console.error('❌ Lỗi trong quá trình Transaction Seeding: ', err.message);
      process.exit(1);
    }
  } catch (err) {
    console.error('❌ Thất bại trong quá trình kết nối/seeding: ', err.message);
    process.exit(1);
  }
}

seed();
