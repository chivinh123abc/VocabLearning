const bcrypt = require('bcryptjs');
const nodemailer = require('nodemailer');
const { getPool, sql } = require('../config/db');

// ===== OTP STORE (In-Memory) =====
// Map<email, { otp, expiresAt }> - tự xóa sau 5 phút
const otpStore = new Map();

// Cấu hình Nodemailer Gmail SMTP
const transporter = nodemailer.createTransport({
  service: 'gmail',
  auth: {
    user: process.env.EMAIL_USER,
    pass: process.env.EMAIL_PASS
  }
});


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
  return level * 100;
};

// Export để sử dụng ở các Controller khác
exports.getRankName = getRankName;
exports.getExpNeeded = getExpNeeded;

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

  // 2. Tách chuỗi loginDates từ DB thành mảng JSON
  const loginDatesStr = user.loginDates || '';
  const loginDates = loginDatesStr ? loginDatesStr.split(',') : [];
  const weeklyLogin = {
    weekStartDate: user.weekStartDate || "",
    isRewardClaimed: !!user.isRewardClaimed,
    loginDates: loginDates
  };

  // 3. Learned Sets
  const learnedResult = await req.query('SELECT [setId] FROM [user_learned_sets] WHERE [userId] = @userId');
  const learnedSets = learnedResult.recordset.map(r => r.setId);

  // 4. Saved Set Levels
  const savedLvlResult = await req.query('SELECT [setId], [level] FROM [user_saved_set_levels] WHERE [userId] = @userId');
  const savedSetLevels = savedLvlResult.recordset.map(r => ({ setId: r.setId, level: r.level }));

  // 5. Set Progress & Completed Levels
  const progressResult = await req.query('SELECT [setId] FROM [user_set_progress] WHERE [userId] = @userId');
  const setProgress = [];
  for (const p of progressResult.recordset) {
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

  // 9. Inventory
  const inventoryResult = await req.query('SELECT [itemId] as id, [icon], [name], [description], [quantity], [rarity], [category], [equipType], [isEquipped], [isCombatItem] FROM [user_inventory] WHERE [userId] = @userId');
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

  // Trả về cấu trúc JSON tương thích Unity Client
  return {
    id: user.id,
    username: user.username,
    email: user.email,
    role: user.role,
    status: user.status,
    level: user.level,
    exp: user.exp,
    expNeeded: getExpNeeded(user.level),
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

    // Cho phép đăng nhập bằng cả username lẫn email
    const userResult = await request.query('SELECT * FROM [users] WHERE [username] = @username OR [email] = @username');

    if (userResult.recordset.length === 0) {
      return res.status(401).json({ success: false, message: 'Tên đăng nhập hoặc mật khẩu không chính xác!' });
    }

    const user = userResult.recordset[0];

    // Kiểm tra trạng thái tài khoản trước khi đăng nhập (0: Inactive, 1: Active, 2: Banned)
    if (user.status === 2) {
      return res.status(403).json({ success: false, message: 'Tài khoản của bạn đã bị khóa (banned)!' });
    }
    if (user.status === 0) {
      return res.status(403).json({ success: false, message: 'Tài khoản của bạn chưa được kích hoạt (inactive)!' });
    }

    const isPasswordValid = bcrypt.compareSync(password, user.password);

    if (!isPasswordValid) {
      return res.status(401).json({ success: false, message: 'Tên đăng nhập hoặc mật khẩu không chính xác!' });
    }

    // Đăng nhập thành công -> Lấy toàn bộ profile của user
    const fullProfile = await getUserFullProfile(pool, user.id);
    return res.json(fullProfile);

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

  // Kiểm tra độ dài mật khẩu tối thiểu 8 ký tự (theo ND_QD 1)
  if (password.length < 8) {
    return res.status(400).json({ success: false, message: 'Mật khẩu phải có ít nhất 8 ký tự!' });
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

    const userId = 'user_' + Date.now() + Math.random().toString(36).substr(2, 5);
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
          [id], [username], [email], [password], [role], [status], [level], [exp], [coins], 
          [rankPoints], [wins], [totalGames],
          [weekStartDate], [isRewardClaimed], [loginDates]
        ) VALUES (
          @id, @username, @email, @password, 'user', 1, 1, 0, 500, 
          0, 0, 0,
          @today, 0, ''
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

      // Đăng ký thành công -> Lấy thông tin đầy đủ để tự động đăng nhập luôn
      const fullProfile = await getUserFullProfile(pool, userId);
      return res.status(201).json(fullProfile);

    } catch (err) {
      await transaction.rollback();
      throw err;
    }

  } catch (err) {
    console.error('Lỗi khi đăng ký: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi đăng ký tài khoản!' });
  }
};

// ===== QUÊN MẬT KHẨU: Gửi OTP về email =====
exports.forgotPassword = async (req, res) => {
  const { email } = req.body;

  if (!email) {
    return res.status(400).json({ success: false, message: 'Vui lòng nhập địa chỉ email!' });
  }

  try {
    const pool = await getPool();
    const request = pool.request();
    request.input('email', sql.NVarChar, email);
    const result = await request.query('SELECT [id], [username] FROM [users] WHERE [email] = @email');

    if (result.recordset.length === 0) {
      // Trả lỗi nhưng không tiết lộ email có tồn tại không (bảo mật)
      return res.status(404).json({ success: false, message: 'Không tìm thấy tài khoản với email này!' });
    }

    const user = result.recordset[0];

    // Tạo OTP 6 chữ số ngẫu nhiên
    const otp = Math.floor(100000 + Math.random() * 900000).toString();
    const expiresAt = Date.now() + 5 * 60 * 1000; // Hết hạn sau 5 phút

    // Lưu OTP vào bộ nhớ
    otpStore.set(email, { otp, expiresAt });

    // Tự xóa OTP sau 5 phút
    setTimeout(() => otpStore.delete(email), 5 * 60 * 1000);

    // Gửi email qua Gmail SMTP
    await transporter.sendMail({
      from: `"VocabLearning 📚" <${process.env.EMAIL_USER}>`,
      to: email,
      subject: 'Mã OTP đặt lại mật khẩu - VocabLearning',
      html: `
        <div style="font-family: Arial, sans-serif; max-width: 480px; margin: auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px;">
          <h2 style="color: #3b82f6;">🔑 Đặt lại mật khẩu</h2>
          <p>Xin chào <strong>${user.username}</strong>,</p>
          <p>Mã OTP của bạn là:</p>
          <div style="font-size: 36px; font-weight: bold; letter-spacing: 8px; text-align: center; padding: 16px; background: #f1f5f9; border-radius: 8px; margin: 16px 0;">
            ${otp}
          </div>
          <p style="color: #64748b;">Mã có hiệu lực trong <strong>5 phút</strong>. Không chia sẻ mã này cho bất kỳ ai.</p>
          <p style="color: #94a3b8; font-size: 12px;">Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
        </div>
      `
    });

    console.log(`📧 Đã gửi OTP đến ${email} cho tài khoản: ${user.username}`);
    return res.json({ success: true, message: 'Mã OTP đã được gửi đến email của bạn!' });

  } catch (err) {
    console.error('Lỗi khi gửi OTP: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi gửi mã OTP!' });
  }
};

// ===== ĐẶT LẠI MẬT KHẨU: Xác thực OTP + cập nhật mật khẩu mới =====
exports.resetPassword = async (req, res) => {
  const { email, otp, newPassword } = req.body;

  if (!email || !otp || !newPassword) {
    return res.status(400).json({ success: false, message: 'Vui lòng nhập đầy đủ thông tin!' });
  }

  // Kiểm tra độ dài mật khẩu mới
  if (newPassword.length < 8) {
    return res.status(400).json({ success: false, message: 'Mật khẩu mới phải có ít nhất 8 ký tự!' });
  }

  // Kiểm tra OTP tồn tại
  const stored = otpStore.get(email);
  if (!stored) {
    return res.status(400).json({ success: false, message: 'Mã OTP không hợp lệ hoặc đã hết hạn!' });
  }

  // Kiểm tra OTP hết hạn
  if (Date.now() > stored.expiresAt) {
    otpStore.delete(email);
    return res.status(400).json({ success: false, message: 'Mã OTP đã hết hạn! Vui lòng yêu cầu mã mới.' });
  }

  // Kiểm tra OTP khớp
  if (stored.otp !== otp) {
    return res.status(400).json({ success: false, message: 'Mã OTP không chính xác!' });
  }

  try {
    const pool = await getPool();
    const hashedPassword = bcrypt.hashSync(newPassword, 10);

    const request = pool.request();
    request.input('email', sql.NVarChar, email);
    request.input('password', sql.VarChar, hashedPassword);
    const result = await request.query('UPDATE [users] SET [password] = @password WHERE [email] = @email');

    if (result.rowsAffected[0] === 0) {
      return res.status(404).json({ success: false, message: 'Không tìm thấy tài khoản với email này!' });
    }

    // Xóa OTP đã dùng
    otpStore.delete(email);

    console.log(`🔒 Đặt lại mật khẩu thành công cho: ${email}`);
    return res.json({ success: true, message: 'Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.' });

  } catch (err) {
    console.error('Lỗi khi đặt lại mật khẩu: ', err);
    return res.status(500).json({ success: false, message: 'Lỗi máy chủ khi đặt lại mật khẩu!' });
  }
};
