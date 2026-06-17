const { getPool, sql } = require('../config/db');
const { getRankName, getExpNeeded } = require('./authController');

// Lấy toàn bộ dữ liệu tĩnh toàn cục (words, sets, achievements, quests, shop, leaderboard)
exports.getGlobals = async (req, res) => {
  try {
    const pool = await getPool();
    
    // 1. Lấy toàn bộ Words
    const wordsResult = await pool.request().query('SELECT * FROM [words]');
    const words = wordsResult.recordset;

    // 2. Lấy toàn bộ Vocab Sets kèm theo link words và cấp độ
    const setsResult = await pool.request().query('SELECT * FROM [vocab_sets]');
    const vocabSets = [];

    for (const set of setsResult.recordset) {
      const linkReq = pool.request();
      linkReq.input('setId', sql.VarChar, set.id);
      
      // Lấy danh sách wordIds
      const wordsLinkResult = await linkReq.query('SELECT [wordId] FROM [vocab_set_words] WHERE [setId] = @setId');
      const wordIds = wordsLinkResult.recordset.map(r => r.wordId);

      // Lấy thông tin Levels
      const levelsResult = await linkReq.query('SELECT [difficulty] FROM [vocab_set_levels] WHERE [setId] = @setId');
      const levels = [];
      for (const lvl of levelsResult.recordset) {
        const lvlReq = pool.request();
        lvlReq.input('setId', sql.VarChar, set.id);
        lvlReq.input('diff', sql.NVarChar, lvl.difficulty);
        const lvlWordsResult = await lvlReq.query('SELECT [wordId] FROM [vocab_set_level_words] WHERE [setId] = @setId AND [difficulty] = @diff');
        levels.push({
          difficulty: lvl.difficulty,
          wordIds: lvlWordsResult.recordset.map(r => r.wordId)
        });
      }

      vocabSets.push({
        id: set.id,
        title: set.title,
        description: set.description,
        wordCount: wordIds.length,
        category: set.category,
        difficulty: set.difficulty,
        rankRequired: set.rankRequired,
        wordIds,
        levels
      });
    }

    // 3. Lấy toàn bộ Achievements tĩnh
    const achResult = await pool.request().query('SELECT * FROM [achievements]');
    const achievements = achResult.recordset;

    // 4. Lấy bể Nhiệm vụ (questPool)
    const questResult = await pool.request().query('SELECT * FROM [quests]');
    const questPool = questResult.recordset;

    // 5. Lấy danh sách vật phẩm trong shop
    const shopResult = await pool.request().query('SELECT * FROM [shop_items]');
    const shopItems = shopResult.recordset;

    // 6. Lấy danh sách Bảng xếp hạng người chơi thực (sắp xếp theo Rank Points và EXP) kèm Avatar đang trang bị
    const lbResult = await pool.request().query(`
      SELECT TOP 50 
        u.[id], u.[username], u.[displayName], u.[exp], u.[coins], u.[rankPoints], u.[wins], u.[totalGames],
        ISNULL(sr.[bestSurvivor], 0) AS [bestSurvivor],
        ISNULL(sr.[bestQuick10], 0) AS [bestQuick10],
        ISNULL(sr.[bestTimeRush], 0) AS [bestTimeRush],
        (SELECT TOP 1 ui.[icon] 
         FROM [user_inventory] ui 
         WHERE ui.[userId] = u.[id] AND ui.[equipType] = 'Avatar' AND ui.[isEquipped] = 1) AS [equippedAvatarIcon]
      FROM [users] u
      LEFT JOIN [user_solo_records] sr ON u.[id] = sr.[userId]
      ORDER BY u.[rankPoints] DESC, u.[exp] DESC
    `);
    const leaderboardUsers = lbResult.recordset.map(u => {
      const calculatedLevel = Math.floor(u.exp / 1000) + 1;
      const userObj = {
        ...u,
        displayName: u.displayName || u.username,
        level: calculatedLevel,
        expNeeded: 1000,
        rank: getRankName(u.rankPoints),
        inventory: []
      };

      if (u.equippedAvatarIcon) {
        userObj.inventory.push({
          id: 'avatar_equipped',
          name: 'Equipped Avatar',
          icon: u.equippedAvatarIcon,
          equipType: 'Avatar',
          isEquipped: true,
          category: 'Cosmetic'
        });
      }

      delete userObj.equippedAvatarIcon;
      return userObj;
    });

    return res.json({
      success: true,
      words,
      vocabSets,
      achievements,
      questPool,
      shopItems,
      leaderboardUsers
    });

  } catch (err) {
    console.error('Lỗi khi lấy dữ liệu toàn cục: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi tải dữ liệu game!' });
  }
};

// Quản lý matchmaking và phòng đấu LAN
let matchmakingQueue = []; // [{ userId, username, rankPoints, avatar, matchedRoomId, polledAt }]
let activeRooms = {}; // { roomId: { roomId, players: [{ userId, username, rankPoints, hp, answered, isCorrect, answerTime, answerText, avatar }], wordPool, currentRoundIndex, status, winnerId } }

// Dọn dẹp hàng chờ hết hạn (quá 5 giây không poll)
const cleanMatchmakingQueue = () => {
  const now = Date.now();
  matchmakingQueue = matchmakingQueue.filter(p => (now - p.polledAt < 5000) || p.matchedRoomId);
};

// Đăng ký ghép trận
exports.matchmake = async (req, res) => {
  try {
    const { userId, username, rankPoints, avatar } = req.body;
    if (!userId) {
      return res.status(400).json({ success: false, message: 'Thiếu userId!' });
    }

    cleanMatchmakingQueue();

    // 1. Kiểm tra xem người chơi đã có phòng đang đấu hay chưa
    const existingRoomId = Object.keys(activeRooms).find(id => 
      activeRooms[id].players.some(p => p.userId === userId) && activeRooms[id].status === 'playing'
    );
    if (existingRoomId) {
      return res.json({ success: true, status: 'matched', roomId: existingRoomId });
    }

    // 2. Dọn dẹp hàng chờ nếu có yêu cầu trùng lặp của userId này
    matchmakingQueue = matchmakingQueue.filter(p => p.userId !== userId);

    // 3. Tìm đối thủ phù hợp trong hàng chờ (chưa bị ghép cặp và khác userId)
    const candidates = matchmakingQueue.filter(p => p.userId !== userId && !p.matchedRoomId);
    
    if (candidates.length > 0) {
      // Sắp xếp đối thủ theo hiệu điểm Rank nhỏ nhất
      candidates.sort((a, b) => Math.abs(a.rankPoints - rankPoints) - Math.abs(b.rankPoints - rankPoints));
      const opponent = candidates[0];

      // Ghép cặp thành công! Tạo roomId mới
      const roomId = 'room_' + Date.now() + '_' + Math.random().toString(36).substring(2, 7);

      // Lấy 20 từ vựng ngẫu nhiên từ cơ sở dữ liệu làm bộ câu hỏi chung
      const pool = await getPool();
      const wordsResult = await pool.request().query('SELECT TOP 20 * FROM [words] ORDER BY NEWID()');
      const wordPool = wordsResult.recordset;

      // Thiết lập phòng đấu mới
      activeRooms[roomId] = {
        roomId,
        players: [
          { userId: opponent.userId, username: opponent.username, rankPoints: opponent.rankPoints, hp: 100, answered: false, isCorrect: false, answerTime: 0, answerText: "", avatar: opponent.avatar || "👤" },
          { userId, username, rankPoints, hp: 100, answered: false, isCorrect: false, answerTime: 0, answerText: "", avatar: avatar || "👤" }
        ],
        wordPool: wordPool.length > 0 ? wordPool : [],
        currentRoundIndex: 0,
        status: 'playing',
        winnerId: null
      };

      // Gán roomId cho đối thủ trong hàng chờ
      opponent.matchedRoomId = roomId;

      console.log(`[LAN Battle] Ghép trận thành công: ${username} VS ${opponent.username} -> Room: ${roomId}`);
      return res.json({ success: true, status: 'matched', roomId });
    } else {
      // Không tìm thấy đối thủ, đưa người chơi vào hàng chờ
      matchmakingQueue.push({
        userId,
        username,
        rankPoints: rankPoints || 0,
        avatar: avatar || "👤",
        matchedRoomId: null,
        polledAt: Date.now()
      });
      console.log(`[LAN Battle] ${username} đang xếp hàng chờ ghép trận...`);
      return res.json({ success: true, status: 'searching' });
    }
  } catch (err) {
    console.error('Lỗi khi ghép trận: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi ghép trận!' });
  }
};

// Kiểm tra trạng thái hàng chờ ghép trận (Polling)
exports.getMatchmakeStatus = async (req, res) => {
  try {
    const { userId } = req.params;
    if (!userId) {
      return res.status(400).json({ success: false, message: 'Thiếu userId!' });
    }

    cleanMatchmakingQueue();

    // 1. Kiểm tra xem đã có phòng đấu đang chơi hay chưa
    const existingRoomId = Object.keys(activeRooms).find(id => 
      activeRooms[id].players.some(p => p.userId === userId) && activeRooms[id].status === 'playing'
    );
    if (existingRoomId) {
      return res.json({ success: true, status: 'matched', roomId: existingRoomId });
    }

    // 2. Tìm trong queue
    const queueEntryIndex = matchmakingQueue.findIndex(p => p.userId === userId);
    if (queueEntryIndex === -1) {
      return res.json({ success: true, status: 'idle' });
    }

    const entry = matchmakingQueue[queueEntryIndex];
    if (entry.matchedRoomId) {
      const roomId = entry.matchedRoomId;
      // Ghép trận thành công, xóa khỏi queue
      matchmakingQueue.splice(queueEntryIndex, 1);
      return res.json({ success: true, status: 'matched', roomId });
    }

    // Cập nhật thời gian poll để không bị dọn dẹp
    entry.polledAt = Date.now();
    return res.json({ success: true, status: 'searching' });
  } catch (err) {
    console.error('Lỗi khi lấy trạng thái ghép trận: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi lấy trạng thái ghép trận!' });
  }
};

// Hủy hàng chờ ghép trận
exports.cancelMatchmake = async (req, res) => {
  try {
    const { userId } = req.body;
    matchmakingQueue = matchmakingQueue.filter(p => p.userId !== userId);
    console.log(`[LAN Battle] User ${userId} đã hủy tìm trận.`);
    return res.json({ success: true, message: 'Hủy tìm trận thành công!' });
  } catch (err) {
    console.error('Lỗi khi hủy ghép trận: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi hủy ghép trận!' });
  }
};

// Lấy thông tin phòng đấu
exports.getRoomState = async (req, res) => {
  try {
    const { roomId } = req.params;
    const room = activeRooms[roomId];
    if (!room) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy phòng đấu!' });
    }
    return res.json({ success: true, room });
  } catch (err) {
    console.error('Lỗi khi lấy thông tin phòng: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi lấy thông tin phòng!' });
  }
};

// Gửi câu trả lời
exports.submitAnswer = async (req, res) => {
  try {
    const { roomId } = req.params;
    const { userId, roundIndex, answerText, answerTime } = req.body;

    const room = activeRooms[roomId];
    if (!room) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy phòng đấu!' });
    }

    if (room.status === 'finished') {
      return res.json({ success: true, message: 'Trận đấu đã kết thúc!', room });
    }

    // Nếu client gửi đáp án cho một lượt cũ đã được giải quyết xong (ví dụ đối thủ trả lời đúng trước)
    if (roundIndex < room.currentRoundIndex) {
      return res.json({ success: true, message: 'Lượt đấu đã kết thúc trước đó!', room });
    }

    if (roundIndex > room.currentRoundIndex) {
      return res.status(400).json({ success: false, message: 'Lượt đấu không trùng khớp!' });
    }

    const player = room.players.find(p => p.userId === userId);
    if (!player) {
      return res.status(404).json({ success: false, message: 'Người chơi không có trong phòng!' });
    }

    // Ghi nhận đáp án
    player.answered = true;
    player.answerText = answerText;
    player.answerTime = answerTime || 0;

    // Kiểm tra tính đúng đắn của đáp án
    const currentWord = room.wordPool[room.currentRoundIndex];
    if (currentWord) {
      // Đáp án có thể khớp với tiếng Anh (word) hoặc tiếng Việt (meaning)
      const correctAns = currentWord.word;
      const correctMean = currentWord.meaning;
      player.isCorrect = (answerText === correctAns || answerText === correctMean);
    } else {
      player.isCorrect = false;
    }

    // Nếu có ít nhất 1 người trả lời ĐÚNG, hoặc cả hai đã trả lời xong
    const hasCorrectAnswer = room.players.some(p => p.answered && p.isCorrect);
    const allAnswered = room.players.every(p => p.answered);

    if (hasCorrectAnswer || allAnswered) {
      const p1 = room.players[0];
      const p2 = room.players[1];

      // Giải quyết lượt đấu (Trừ HP)
      if (p1.isCorrect && p2.isCorrect) {
        // Cả hai cùng đúng: ai nhanh hơn (answerTime nhỏ hơn) thì an toàn, người chậm hơn mất 10 HP. Nếu bằng nhau thì không ai mất HP.
        if (p1.answerTime < p2.answerTime) {
          p2.hp -= 10;
        } else if (p2.answerTime < p1.answerTime) {
          p1.hp -= 10;
        }
      } else if (p1.isCorrect && !p2.isCorrect) {
        // p1 đúng, p2 sai hoặc chưa trả lời
        p2.hp -= 10;
      } else if (!p1.isCorrect && p2.isCorrect) {
        // p1 sai hoặc chưa trả lời, p2 đúng
        p1.hp -= 10;
      } else {
        // Cả hai cùng sai
        p1.hp -= 10;
        p2.hp -= 10;
      }

      // Giới hạn HP không âm
      if (p1.hp < 0) p1.hp = 0;
      if (p2.hp < 0) p2.hp = 0;

      // Reset trạng thái trả lời cho vòng sau
      p1.answered = false;
      p2.answered = false;
      p1.isCorrect = false;
      p2.isCorrect = false;

      // Kiểm tra kết thúc trận đấu (Kết thúc khi có ít nhất 1 người chơi HP <= 0, hoặc hết sạch bộ câu hỏi dự phòng)
      if (p1.hp <= 0 || p2.hp <= 0 || room.currentRoundIndex >= room.wordPool.length - 1) {
        room.status = 'finished';
        if (p1.hp > p2.hp) {
          room.winnerId = p1.userId;
        } else if (p2.hp > p1.hp) {
          room.winnerId = p2.userId;
        } else {
          room.winnerId = null; // Hòa
        }

        // Cập nhật kết quả vào SQL Server DB
        const pool = await getPool();
        const winnerId = room.winnerId;
        const loserId = room.players.find(p => p.userId !== winnerId)?.userId;

        if (winnerId) {
          // Người thắng
          await pool.request()
            .input('winId', sql.VarChar, winnerId)
            .query('UPDATE [users] SET [rankPoints] = [rankPoints] + 25, [wins] = [wins] + 1, [totalGames] = [totalGames] + 1, [exp] = [exp] + 50, [coins] = [coins] + 50 WHERE [id] = @winId');
        }
        if (loserId && winnerId) {
          // Người thua
          await pool.request()
            .input('loseId', sql.VarChar, loserId)
            .query('UPDATE [users] SET [rankPoints] = CASE WHEN [rankPoints] >= 15 THEN [rankPoints] - 15 ELSE 0 END, [totalGames] = [totalGames] + 1, [exp] = [exp] + 15 WHERE [id] = @loseId');
        }
        if (!winnerId) {
          // Hòa: cả hai nhận phần thưởng khuyến khích
          for (const p of room.players) {
            await pool.request()
              .input('pId', sql.VarChar, p.userId)
              .query('UPDATE [users] SET [totalGames] = [totalGames] + 1, [exp] = [exp] + 15 WHERE [id] = @pId');
          }
        }
      } else {
        // Chưa kết thúc, chuyển sang lượt tiếp theo
        room.currentRoundIndex++;
      }
    }

    return res.json({ success: true, room });
  } catch (err) {
    console.error('Lỗi khi gửi đáp án: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi gửi đáp án!' });
  }
};

// Đầu hàng hoặc thoát trận đấu giữa chừng
exports.leaveRoom = async (req, res) => {
  try {
    const { roomId } = req.params;
    const { userId } = req.body;

    const room = activeRooms[roomId];
    if (!room) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy phòng đấu!' });
    }

    if (room.status !== 'finished') {
      room.status = 'finished';
      const fleeingPlayer = room.players.find(p => p.userId === userId);
      const otherPlayer = room.players.find(p => p.userId !== userId);

      if (fleeingPlayer) fleeingPlayer.hp = 0;
      if (otherPlayer) {
        room.winnerId = otherPlayer.userId;

        // Cập nhật SQL Server
        const pool = await getPool();
        await pool.request()
          .input('winId', sql.VarChar, otherPlayer.userId)
          .query('UPDATE [users] SET [rankPoints] = [rankPoints] + 25, [wins] = [wins] + 1, [totalGames] = [totalGames] + 1, [exp] = [exp] + 50, [coins] = [coins] + 50 WHERE [id] = @winId');
        
        await pool.request()
          .input('loseId', sql.VarChar, userId)
          .query('UPDATE [users] SET [rankPoints] = CASE WHEN [rankPoints] >= 15 THEN [rankPoints] - 15 ELSE 0 END, [totalGames] = [totalGames] + 1, [exp] = [exp] + 15 WHERE [id] = @loseId');
      }
    }

    return res.json({ success: true, message: 'Bạn đã rời phòng đấu!', room });
  } catch (err) {
    console.error('Lỗi khi rời phòng: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi rời phòng!' });
  }
};
