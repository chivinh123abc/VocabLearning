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

    // 6. Lấy danh sách Bảng xếp hạng người chơi thực (sắp xếp theo Rank Points và EXP)
    const lbResult = await pool.request().query(`
      SELECT TOP 50 
        u.[id], u.[username], u.[level], u.[exp], u.[coins], u.[rankPoints], u.[wins], u.[totalGames],
        ISNULL(sr.[bestSurvivor], 0) AS [bestSurvivor],
        ISNULL(sr.[bestQuick10], 0) AS [bestQuick10],
        ISNULL(sr.[bestTimeRush], 0) AS [bestTimeRush]
      FROM [users] u
      LEFT JOIN [user_solo_records] sr ON u.[id] = sr.[userId]
      ORDER BY u.[rankPoints] DESC, u.[level] DESC
    `);
    const leaderboardUsers = lbResult.recordset.map(u => ({
      ...u,
      expNeeded: getExpNeeded(u.level),
      rank: getRankName(u.rankPoints)
    }));

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
