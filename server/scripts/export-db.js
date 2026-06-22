const fs = require('fs');
const path = require('path');
const { getPool } = require('../config/db');

async function exportDb() {
  try {
    const pool = await getPool();
    console.log('🔌 Đang kết nối và trích xuất dữ liệu từ SQL Server...');

    // 1. Lấy Words
    const wordsRes = await pool.request().query('SELECT * FROM [words]');
    const words = wordsRes.recordset.map(w => ({
      id: w.id,
      word: w.word,
      meaning: w.meaning,
      rankRequired: w.rankRequired,
      imageUrl: w.imageUrl || '',
      imageSub: w.imageSub || ''
    }));
    console.log(`🔤 Trích xuất ${words.length} từ vựng.`);

    // 2. Lấy Vocab Sets & Links
    const setsRes = await pool.request().query('SELECT * FROM [vocab_sets]');
    const setWordsRes = await pool.request().query('SELECT * FROM [vocab_set_words]');
    const setLvlWordsRes = await pool.request().query('SELECT * FROM [vocab_set_level_words]');

    // Nhóm wordIds theo setId
    const setWordsMap = {};
    setWordsRes.recordset.forEach(row => {
      if (!setWordsMap[row.setId]) setWordsMap[row.setId] = [];
      setWordsMap[row.setId].push(row.wordId);
    });

    // Nhóm level wordIds theo setId và difficulty
    const setLvlWordsMap = {}; // { setId: { difficulty: [wordIds] } }
    setLvlWordsRes.recordset.forEach(row => {
      if (!setLvlWordsMap[row.setId]) setLvlWordsMap[row.setId] = {};
      if (!setLvlWordsMap[row.setId][row.difficulty]) setLvlWordsMap[row.setId][row.difficulty] = [];
      setLvlWordsMap[row.setId][row.difficulty].push(row.wordId);
    });

    const vocabSets = setsRes.recordset.map(s => {
      const wordIds = setWordsMap[s.id] || [];
      const levelsMap = setLvlWordsMap[s.id] || {};
      const levels = Object.keys(levelsMap).map(diff => ({
        difficulty: diff,
        wordIds: levelsMap[diff]
      }));

      return {
        id: s.id,
        title: s.title,
        description: s.description || '',
        wordCount: wordIds.length,
        category: s.category || '',
        difficulty: s.difficulty || '',
        rankRequired: s.rankRequired || 'Dong',
        wordIds,
        levels
      };
    });
    console.log(`📚 Trích xuất ${vocabSets.length} bộ từ vựng.`);

    // 3. Lấy Achievements
    const achRes = await pool.request().query('SELECT * FROM [achievements]');
    const achievements = achRes.recordset.map(a => ({
      id: a.id,
      icon: a.icon || '',
      title: a.title,
      description: a.description || '',
      maxProgress: a.maxProgress
    }));
    console.log(`🏆 Trích xuất ${achievements.length} thành tựu.`);

    // 4. Lấy Quests (questPool)
    const questsRes = await pool.request().query('SELECT * FROM [quests]');
    const questPool = questsRes.recordset.map(q => ({
      id: q.id,
      title: q.title,
      description: q.description || '',
      maxProgress: q.maxProgress,
      rewardCoins: q.rewardCoins,
      rewardExp: q.rewardExp,
      questType: q.questType
    }));
    console.log(`⚔️ Trích xuất ${questPool.length} nhiệm vụ tĩnh.`);

    // 5. Lấy Shop Items
    const shopRes = await pool.request().query('SELECT * FROM [shop_items]');
    const shopItems = shopRes.recordset.map(item => ({
      id: item.id,
      name: item.name,
      description: item.description || '',
      icon: item.icon || '',
      price: item.price,
      rarity: item.rarity || 'Common',
      category: item.category || 'Cosmetic',
      equipType: item.equipType || ''
    }));
    console.log(`🛒 Trích xuất ${shopItems.length} vật phẩm shop.`);

    // 6. Lấy thông tin Users
    const usersRes = await pool.request().query('SELECT * FROM [users]');
    const soloRes = await pool.request().query('SELECT * FROM [user_solo_records]');
    const savedLvlRes = await pool.request().query('SELECT * FROM [user_saved_set_levels]');
    const setProgRes = await pool.request().query('SELECT * FROM [user_set_progress]');
    const setCompLvlRes = await pool.request().query('SELECT * FROM [user_set_completed_levels]');
    const wordProgRes = await pool.request().query('SELECT * FROM [user_word_progress]');
    const shopHistRes = await pool.request().query('SELECT * FROM [user_shop_history]');
    const battleHistRes = await pool.request().query('SELECT * FROM [user_battle_history]');
    const roundsRes = await pool.request().query('SELECT * FROM [battle_rounds]');
    const inventoryRes = await pool.request().query('SELECT * FROM [user_inventory]');
    const userQuestsRes = await pool.request().query('SELECT * FROM [user_quests]');
    const userAchRes = await pool.request().query('SELECT * FROM [user_achievements]');

    // Ánh xạ dữ liệu phụ trợ
    const soloMap = {};
    soloRes.recordset.forEach(r => { soloMap[r.userId] = r; });

    const learnedSetsMap = {};
    setProgRes.recordset.forEach(r => {
      if (r.status === 'completed') {
        if (!learnedSetsMap[r.userId]) learnedSetsMap[r.userId] = [];
        learnedSetsMap[r.userId].push(r.setId);
      }
    });

    const savedLvlMap = {};
    savedLvlRes.recordset.forEach(r => {
      if (!savedLvlMap[r.userId]) savedLvlMap[r.userId] = [];
      savedLvlMap[r.userId].push({ setId: r.setId, level: r.level });
    });

    const completedLvlsMap = {}; // { userId: { setId: [levels] } }
    setCompLvlRes.recordset.forEach(r => {
      if (!completedLvlsMap[r.userId]) completedLvlsMap[r.userId] = {};
      if (!completedLvlsMap[r.userId][r.setId]) completedLvlsMap[r.userId][r.setId] = [];
      completedLvlsMap[r.userId][r.setId].push(r.level);
    });

    const setProgMap = {};
    setProgRes.recordset.forEach(r => {
      if (r.status === 'learning') {
        if (!setProgMap[r.userId]) setProgMap[r.userId] = [];
        const completedLevels = (completedLvlsMap[r.userId] && completedLvlsMap[r.userId][r.setId]) || [];
        setProgMap[r.userId].push({
          setId: r.setId,
          completedLevels
        });
      }
    });

    const wordProgMap = {};
    wordProgRes.recordset.forEach(r => {
      if (!wordProgMap[r.userId]) wordProgMap[r.userId] = [];
      wordProgMap[r.userId].push({ wordId: r.wordId, status: r.status });
    });

    const shopHistMap = {};
    shopHistRes.recordset.forEach(r => {
      if (!shopHistMap[r.userId]) shopHistMap[r.userId] = [];
      shopHistMap[r.userId].push({
        itemName: r.itemName,
        price: r.price,
        date: r.date
      });
    });

    const roundsMap = {};
    roundsRes.recordset.forEach(r => {
      if (!roundsMap[r.matchId]) roundsMap[r.matchId] = [];
      roundsMap[r.matchId].push({
        question: r.question,
        correctAnswer: r.correctAnswer,
        playerAnswer: r.playerAnswer,
        imageUrl: r.imageUrl || '',
        isCorrect: r.isCorrect,
        isTimeout: r.isTimeout
      });
    });

    const battleHistMap = {};
    battleHistRes.recordset.forEach(r => {
      if (!battleHistMap[r.userId]) battleHistMap[r.userId] = [];
      const rounds = roundsMap[r.matchId] || [];
      battleHistMap[r.userId].push({
        matchId: r.matchId,
        date: r.date,
        isRanked: r.isRanked,
        isWin: r.isWin,
        opponentName: r.opponentName,
        playerFinalHP: r.playerFinalHP,
        enemyFinalHP: r.enemyFinalHP,
        correctCount: r.correctCount,
        totalRounds: r.totalRounds,
        rounds
      });
    });

    const inventoryMap = {};
    inventoryRes.recordset.forEach(r => {
      if (!inventoryMap[r.userId]) inventoryMap[r.userId] = [];
      inventoryMap[r.userId].push({
        id: r.itemId,
        icon: r.icon || '',
        name: r.name,
        description: r.description || '',
        quantity: r.quantity,
        rarity: r.rarity || 'Common',
        category: r.category || 'Cosmetic',
        equipType: r.equipType || '',
        isEquipped: r.isEquipped,
        isCombatItem: r.isCombatItem
      });
    });

    const userQuestsMap = {};
    const staticQuestsMap = {};
    questPool.forEach(q => { staticQuestsMap[q.id] = q; });

    userQuestsRes.recordset.forEach(r => {
      if (!userQuestsMap[r.userId]) userQuestsMap[r.userId] = [];
      const staticQ = staticQuestsMap[r.questId] || {};
      userQuestsMap[r.userId].push({
        id: r.questId,
        title: staticQ.title || '',
        description: staticQ.description || '',
        currentProgress: r.currentProgress,
        maxProgress: staticQ.maxProgress || 1,
        rewardCoins: staticQ.rewardCoins || 0,
        rewardExp: staticQ.rewardExp || 0,
        isClaimed: r.isClaimed,
        questType: staticQ.questType || ''
      });
    });

    const userAchMap = {};
    userAchRes.recordset.forEach(r => {
      if (!userAchMap[r.userId]) userAchMap[r.userId] = [];
      userAchMap[r.userId].push({
        achievementId: r.achievementId,
        currentProgress: r.currentProgress,
        isUnlocked: r.isUnlocked,
        unlockDate: r.unlockDate || ''
      });
    });

    const mapUserJson = (u) => {
      const solo = soloMap[u.id] || {};
      const loginDates = u.loginDates ? u.loginDates.split(',') : [];

      return {
        id: u.id,
        token: '',
        username: u.username,
        displayName: u.displayName || u.username,
        email: u.email || '',
        password: '',
        role: u.role,
        status: u.status,
        level: Math.floor((u.exp || 0) / 1000) + 1,
        exp: u.exp || 0,
        expNeeded: 1000,
        coins: u.coins || 0,
        rank: u.rankPoints >= 24000 ? 'SieuCap' : (u.rankPoints >= 18000 ? 'BachKim' : (u.rankPoints >= 12000 ? 'Vang' : (u.rankPoints >= 6000 ? 'Bac' : 'Dong'))),
        rankPoints: u.rankPoints || 0,
        wins: u.wins || 0,
        totalGames: u.totalGames || 0,
        weeklyLogin: {
          weekStartDate: u.weekStartDate || '',
          loginDates,
          isRewardClaimed: u.isRewardClaimed || false
        },
        learnedSets: learnedSetsMap[u.id] || [],
        savedSetLevels: savedLvlMap[u.id] || [],
        setProgress: setProgMap[u.id] || [],
        wordProgress: wordProgMap[u.id] || [],
        battleHistory: battleHistMap[u.id] || [],
        bestSurvivor: solo.bestSurvivor || 0,
        bestQuick10: solo.bestQuick10 || 0,
        bestTimeRush: solo.bestTimeRush || 0,
        shopHistory: shopHistMap[u.id] || [],
        inventory: inventoryMap[u.id] || [],
        quests: userQuestsMap[u.id] || [],
        achievements: userAchMap[u.id] || []
      };
    };

    const registeredUsers = [];
    let currentUser = null;

    usersRes.recordset.forEach(u => {
      const userJson = mapUserJson(u);
      if (u.role === 'admin' || u.username === 'admin') {
        currentUser = userJson;
      } else {
        registeredUsers.push(userJson);
      }
    });

    if (!currentUser && usersRes.recordset.length > 0) {
      currentUser = mapUserJson(usersRes.recordset[0]);
    }

    const dbJsonPath = path.join(__dirname, '../../Assets/Resources/Mockdata/db.json');
    let dbData = {};
    if (fs.existsSync(dbJsonPath)) {
      dbData = JSON.parse(fs.readFileSync(dbJsonPath, 'utf8'));
    }

    dbData.words = words;
    dbData.vocabSets = vocabSets;
    dbData.achievements = achievements;
    dbData.questPool = questPool;
    dbData.shopItems = shopItems;
    dbData.registeredUsers = registeredUsers;

    if (currentUser) {
      dbData.currentUser = currentUser;
      dbData.inventory = currentUser.inventory || [];
      dbData.quests = currentUser.quests || [];
    }

    fs.writeFileSync(dbJsonPath, JSON.stringify(dbData, null, 4), 'utf8');
    console.log('🎉 XUẤT Snapshot CSDL THÀNH CÔNG! Đã đồng bộ tất cả bảng từ SQL Server vào db.json.');
    process.exit(0);
  } catch (err) {
    console.error('❌ Lỗi khi trích xuất dữ liệu: ', err.message);
    process.exit(1);
  }
}

exportDb();
