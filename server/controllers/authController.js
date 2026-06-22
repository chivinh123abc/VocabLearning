const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const nodemailer = require('nodemailer');
const { getPool, sql } = require('../config/db');

// Hàm chuyển đổi điểm rank thành chuỗi rank tương ứng (Đồng bộ với Unity)
const getRankName = (rankPoints) => {
  if (rankPoints >= 20000) return 'SieuCap';
  if (rankPoints >= 10000) return 'KimCuong';
  if (rankPoints >= 5000) return 'BachKim';
  if (rankPoints >= 2500) return 'Vang';
  if (rankPoints >= 1000) return 'Bac';
  return 'Dong';
};

const getExpNeeded = (level) => {
  return 1000;
};


// Export để sử dụng ở các Controller khác
exports.getRankName = getRankName;
exports.getExpNeeded = getExpNeeded;
exports.getUserFullProfile = getUserFullProfile;

// Hàm hỗ trợ lấy toàn bộ thông tin tiến độ của một User
async function getUserFullProfile(pool, userId) {
  const req = pool.request();
  req.input('userId', sql.VarChar, userId);

  // 1. Thông tin User cơ bản & Weekly Login gộp trực tiếp
  const userResult = await req.query('SELECT * FROM [users] WHERE [id] = @userId');
  if (userResult.recordset.length === 0) return null;
  const user = userResult.recordset[0];

  // 1b. Thông tin điểm kỷ lục Solo [NEW]
  const soloResult = await req.query('SELECT * FROM [user_solo_records] WHERE [userId] = @userId');
  const solo = soloResult.recordset[0] || { bestSurvivor: 0, bestQuick10: 0, bestTimeRush: 0 };

  // 2. Lấy loginDates từ bảng user_login_dates (chuẩn hóa 1NF)
  const loginDatesResult = await req.query('SELECT [loginDate] FROM [user_login_dates] WHERE [userId] = @userId');
  const loginDates = loginDatesResult.recordset.map(r => r.loginDate);
  const weeklyLogin = {
    weekStartDate: user.weekStartDate || "",
    isRewardClaimed: !!user.isRewardClaimed,
    loginDates: loginDates
  };

  // 3 & 5. Set Progress & Learned Sets (Lấy từ bảng user_set_progress gộp chung)
  const progressResult = await req.query('SELECT [setId], [status], [currentLevel] FROM [user_set_progress] WHERE [userId] = @userId');
  
  // Tách thành learnedSets (những bộ đã học xong)
  const learnedSets = progressResult.recordset
    .filter(r => r.status === 'completed')
    .map(r => r.setId);

  // 4. Saved Set Levels (giờ lấy từ cột currentLevel trong user_set_progress)
  const savedSetLevels = progressResult.recordset
    .filter(r => r.currentLevel)
    .map(r => ({ setId: r.setId, level: r.currentLevel }));

  // Tách thành setProgress (những bộ đang học dở)
  const setProgress = [];
  const learningSets = progressResult.recordset.filter(r => r.status === 'learning');
  for (const p of learningSets) {
    const compReq = pool.request();
    compReq.input('userId', sql.VarChar, userId);
    compReq.input('setId', sql.VarChar, p.setId);
    const compResult = await compReq.query('SELECT [level] FROM [user_set_completed_levels] WHERE [userId] = @userId AND [setId] = @setId');
    setProgress.push({
      setId: p.setId,
      completedLevels: compResult.recordset.map(r => r.level)
    });
  }

  // 6. Word Progress
  const wordProgResult = await req.query('SELECT [wordId], [status] FROM [user_word_progress] WHERE [userId] = @userId');
  const wordProgress = wordProgResult.recordset.map(r => ({ wordId: r.wordId, status: r.status }));

  // 7. Shop History
  const shopHistoryResult = await req.query('SELECT [itemName], [price], [date] FROM [user_shop_history] WHERE [userId] = @userId');
  const shopHistory = shopHistoryResult.recordset.map(r => ({ itemName: r.itemName, price: r.price, date: r.date }));

  // 8. Battle History & Rounds
  const battleHistoryResult = await req.query('SELECT * FROM [user_battle_history] WHERE [userId] = @userId ORDER BY [date] DESC');
  const battleHistory = [];
  for (const bh of battleHistoryResult.recordset) {
    const roundReq = pool.request();
    roundReq.input('matchId', sql.VarChar, bh.matchId);
    const roundResult = await roundReq.query('SELECT * FROM [battle_rounds] WHERE [matchId] = @matchId');
    battleHistory.push({
      matchId: bh.matchId,
      date: bh.date,
      isRanked: bh.isRanked,
      isWin: bh.isWin,
      opponentName: bh.opponentName,
      playerFinalHP: bh.playerFinalHP,
      enemyFinalHP: bh.enemyFinalHP,
      correctCount: bh.correctCount,
      totalRounds: bh.totalRounds,
      rounds: roundResult.recordset.map(r => ({
        question: r.question,
        correctAnswer: r.correctAnswer,
        playerAnswer: r.playerAnswer,
        imageUrl: r.imageUrl,
        isCorrect: r.isCorrect,
        isTimeout: r.isTimeout
      }))
    });
  }

  // 9. Inventory (JOIN với shop_items để lấy metadata — chuẩn hóa 2NF)
  const inventoryResult = await req.query(`
    SELECT ui.[itemId] as id, si.[icon], si.[name], si.[description], ui.[quantity], 
           si.[rarity], si.[category], si.[equipType], ui.[isEquipped], ui.[isCombatItem]
    FROM [user_inventory] ui
    INNER JOIN [shop_items] si ON ui.[itemId] = si.[id]
    WHERE ui.[userId] = @userId
  `);
  const inventory = inventoryResult.recordset;

  // 10. Quests
  const questResult = await req.query(`
    SELECT q.[id], q.[title], q.[description], q.[maxProgress], q.[rewardCoins], q.[rewardExp], q.[questType], uq.[currentProgress], uq.[isClaimed]
    FROM [user_quests] uq
    INNER JOIN [quests] q ON uq.[questId] = q.[id]
    WHERE uq.[userId] = @userId
  `);
  const quests = questResult.recordset;

  // 11. Achievements
  const achResult = await req.query(`
    SELECT a.[id], a.[icon], a.[title], a.[description], a.[maxProgress], ua.[currentProgress], ua.[isUnlocked], ua.[unlockDate]
    FROM [user_achievements] ua
    INNER JOIN [achievements] a ON ua.[achievementId] = a.[id]
    WHERE ua.[userId] = @userId
  `);
  const achievements = achResult.recordset.map(r => ({
    id: r.id,
    icon: r.icon,
    title: r.title,
    description: r.description,
    maxProgress: r.maxProgress,
    currentProgress: r.currentProgress,
    isUnlocked: r.isUnlocked,
    unlockDate: r.unlockDate
  }));

  return {
    id: user.id,
    username: user.username,
    displayName: user.displayName || user.username,
    email: user.email,
    role: user.role,
    status: user.status || 'active',
    level: Math.floor(user.exp / 1000) + 1,
    exp: user.exp,
    expNeeded: 1000,
    coins: user.coins,
    rank: getRankName(user.rankPoints),
    rankPoints: user.rankPoints,
    wins: user.wins,
    totalGames: user.totalGames,
    lastQuestRefreshDate: "",
    bestSurvivor: solo.bestSurvivor,
    bestQuick10: solo.bestQuick10,
    bestTimeRush: solo.bestTimeRush,
    weeklyLogin,
    learnedSets,
    savedSetLevels,
    setProgress,
    wordProgress,
    shopHistory,
    battleHistory,
    inventory,
    quests,
    achievements
  };
}

// Logic Đăng nhập (Login)
exports.login = async (req, res) => {
  const { username, password } = req.body;

  if (!username || !password) {
    return res.status(400).json({ success: false, message: 'Vui lòng cung cấp username và password!' });
  }

  try {
    const pool = await getPool();
    const request = pool.request();
    request.input('username', sql.NVarChar, username);

    const userResult = await request.query('SELECT * FROM [users] WHERE [username] = @username');

    if (userResult.recordset.length === 0) {
      return res.status(401).json({ success: false, message: 'Tên đăng nhập hoặc mật khẩu không chính xác!' });
    }

    const user = userResult.recordset[0];
    const isPasswordValid = bcrypt.compareSync(password, user.password);

    if (!isPasswordValid) {
      return res.status(401).json({ success: false, message: 'Tên đăng nhập hoặc mật khẩu không chính xác!' });
    }

    // Kiểm tra trạng thái tài khoản (Chặn tài khoản bị Banned)
    if (user.status === 'banned') {
      console.warn(`🚨 [Xác thực] Đăng nhập bị chặn: Tài khoản '${user.username}' đã bị khóa bởi Ban Quản Trị.`);
      return res.status(403).json({ success: false, message: 'Tài khoản của bạn đã bị khóa bởi Ban Quản Trị vì vi phạm điều khoản!' });
    }

    // Đăng nhập thành công -> Tạo mã JWT Token & Lấy toàn bộ profile của user
    const token = jwt.sign(
      { id: user.id, role: user.role },
      process.env.JWT_SECRET || 'nckh_vocab_learning_secret_key_2026',
      { expiresIn: '30d' }
    );

    const fullProfile = await getUserFullProfile(pool, user.id);
    return res.json({
      ...fullProfile,
      token: token
    });

  } catch (err) {
    console.error('Lỗi khi đăng nhập: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ nội bộ!' });
  }
};

// Logic Đăng ký (Register)
exports.register = async (req, res) => {
  const { username, email, password } = req.body;

  if (!username || !password) {
    return res.status(400).json({ success: false, message: 'Tên đăng nhập và mật khẩu là bắt buộc!' });
  }

  try {
    const pool = await getPool();
    
    // Kiểm tra trùng username
    const checkReq = pool.request();
    checkReq.input('username', sql.NVarChar, username);
    const checkResult = await checkReq.query('SELECT [id] FROM [users] WHERE [username] = @username');

    if (checkResult.recordset.length > 0) {
      return res.status(400).json({ success: false, message: 'Tên đăng nhập đã tồn tại!' });
    }

    const userId = Date.now().toString() + Math.random().toString(36).substr(2, 5);
    const hashedPassword = bcrypt.hashSync(password, 10);

    const transaction = new sql.Transaction(pool);
    await transaction.begin();

    try {
      // 1. Tạo user mới kèm Weekly Login mặc định
      const today = new Date().toISOString().split('T')[0];
      const insertUserReq = new sql.Request(transaction);
      insertUserReq.input('id', sql.VarChar, userId);
      insertUserReq.input('username', sql.NVarChar, username);
      insertUserReq.input('email', sql.NVarChar, email || `${username}@vocab.com`);
      insertUserReq.input('password', sql.VarChar, hashedPassword);
      insertUserReq.input('today', sql.VarChar, today);
      
      await insertUserReq.query(`
        INSERT INTO [users] (
          [id], [username], [displayName], [email], [password], [role], [exp], [coins], 
          [rankPoints], [wins], [totalGames],
          [weekStartDate], [isRewardClaimed]
        ) VALUES (
          @id, @username, @username, @email, @password, 'user', 0, 500, 
          0, 0, 0,
          @today, 0
        )
      `);

      // 1b. Khởi tạo điểm kỷ lục solo trống [NEW]
      const insertSoloReq = new sql.Request(transaction);
      insertSoloReq.input('userId', sql.VarChar, userId);
      await insertSoloReq.query(`
        INSERT INTO [user_solo_records] ([userId], [bestSurvivor], [bestQuick10], [bestTimeRush])
        VALUES (@userId, 0, 0, 0)
      `);

      // 2. Khởi tạo nhiệm vụ mặc định cho User mới từ questPool (bảng quests)
      const questsResult = await transaction.request().query('SELECT [id] FROM [quests]');
      for (const q of questsResult.recordset) {
        const uqReq = new sql.Request(transaction);
        uqReq.input('userId', sql.VarChar, userId);
        uqReq.input('questId', sql.VarChar, q.id);
        await uqReq.query(`
          INSERT INTO [user_quests] ([userId], [questId], [currentProgress], [isClaimed])
          VALUES (@userId, @questId, 0, 0)
        `);
      }

      // 3. Khởi tạo tiến trình thành tựu mặc định (bảng achievements)
      const achsResult = await transaction.request().query('SELECT [id] FROM [achievements]');
      for (const a of achsResult.recordset) {
        const uaReq = new sql.Request(transaction);
        uaReq.input('userId', sql.VarChar, userId);
        uaReq.input('achId', sql.VarChar, a.id);
        await uaReq.query(`
          INSERT INTO [user_achievements] ([userId], [achievementId], [currentProgress], [isUnlocked], [unlockDate])
          VALUES (@userId, @achId, 0, 0, '')
        `);
      }

      await transaction.commit();

      // Đăng ký thành công -> Tạo mã JWT Token & Lấy thông tin đầy đủ để tự động đăng nhập luôn
      const token = jwt.sign(
        { id: userId, role: 'user' },
        process.env.JWT_SECRET || 'nckh_vocab_learning_secret_key_2026',
        { expiresIn: '30d' }
      );

      const fullProfile = await getUserFullProfile(pool, userId);
      return res.status(201).json({
        ...fullProfile,
        token: token
      });

    } catch (err) {
      await transaction.rollback();
      throw err;
    }

  } catch (err) {
    console.error('Lỗi khi đăng ký: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi đăng ký tài khoản!' });
  }
};

// Bộ nhớ đệm lưu mã OTP khôi phục mật khẩu
const otpStore = new Map();

// Gửi mã OTP khôi phục mật khẩu qua email
exports.forgotPassword = async (req, res) => {
  const { email } = req.body;

  if (!email || !email.trim()) {
    return res.status(400).json({ success: false, message: 'Vui lòng cung cấp email của bạn!' });
  }

  try {
    const pool = await getPool();
    const checkReq = pool.request();
    checkReq.input('email', sql.NVarChar, email.trim());
    const checkRes = await checkReq.query('SELECT [id] FROM [users] WHERE [email] = @email');

    if (checkRes.recordset.length === 0) {
      return res.status(400).json({ success: false, message: 'Địa chỉ email này không tồn tại trên hệ thống!' });
    }

    // Tạo mã OTP gồm 6 chữ số ngẫu nhiên
    const otp = Math.floor(100000 + Math.random() * 900000).toString();
    const expires = Date.now() + 5 * 60 * 1000; // Có hiệu lực trong 5 phút

    const emailKey = email.trim().toLowerCase();
    otpStore.set(emailKey, { otp, expires });

    // Tạo kênh vận chuyển email thông qua SMTP của Gmail
    const transporter = nodemailer.createTransport({
      service: 'gmail',
      auth: {
        user: process.env.EMAIL_USER,
        pass: process.env.EMAIL_PASS
      }
    });

    const mailOptions = {
      from: `"VocabLearning Support" <${process.env.EMAIL_USER}>`,
      to: email.trim(),
      subject: '[VocabLearning] Mã OTP Khôi Phục Mật Khẩu',
      text: `Xin chào,\n\nBạn đã gửi yêu cầu đặt lại mật khẩu cho tài khoản VocabLearning.\nMã OTP xác nhận của bạn là: ${otp}\n\nMã OTP này có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.\n\nTrân trọng,\nĐội ngũ VocabLearning.`
    };

    await transporter.sendMail(mailOptions);
    console.log(`✉️ Đã gửi OTP (${otp}) khôi phục mật khẩu tới: ${email}`);

    return res.json({ success: true, message: 'Gửi mã OTP khôi phục mật khẩu thành công!' });

  } catch (err) {
    console.error('Lỗi khi gửi email OTP: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi gửi mã khôi phục mật khẩu!' });
  }
};

// Xác thực OTP và Đặt mật khẩu mới
exports.resetPassword = async (req, res) => {
  const { email, otp, newPassword } = req.body;

  if (!email || !otp || !newPassword) {
    return res.status(400).json({ success: false, message: 'Vui lòng điền đầy đủ các thông tin yêu cầu!' });
  }

  const emailKey = email.trim().toLowerCase();
  const record = otpStore.get(emailKey);

  if (!record) {
    return res.status(400).json({ success: false, message: 'Chưa yêu cầu mã OTP cho email này!' });
  }

  if (record.otp !== otp.trim()) {
    return res.status(400).json({ success: false, message: 'Mã OTP không chính xác!' });
  }

  if (Date.now() > record.expires) {
    otpStore.delete(emailKey);
    return res.status(400).json({ success: false, message: 'Mã OTP đã hết hạn sử dụng! Vui lòng yêu cầu mã mới.' });
  }

  try {
    const hashedPassword = bcrypt.hashSync(newPassword.trim(), 10);
    const pool = await getPool();

    const updateReq = pool.request();
    updateReq.input('email', sql.NVarChar, email.trim());
    updateReq.input('password', sql.VarChar, hashedPassword);
    const result = await updateReq.query('UPDATE [users] SET [password] = @password WHERE [email] = @email');

    if (result.rowsAffected[0] === 0) {
      return res.status(400).json({ success: false, message: 'Email không khớp với người dùng nào trong hệ thống.' });
    }

    // Xóa mã OTP sau khi sử dụng thành công
    otpStore.delete(emailKey);
    console.log(`🔑 Đã đổi mật khẩu khôi phục thành công cho hòm thư: ${email}`);

    return res.json({ success: true, message: 'Đặt lại mật khẩu thành công! Bạn có thể sử dụng mật khẩu mới để đăng nhập.' });

  } catch (err) {
    console.error('Lỗi khi khôi phục mật khẩu: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi khôi phục mật khẩu!' });
  }
};
