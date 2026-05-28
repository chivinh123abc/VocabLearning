const sql = require('mssql');
require('dotenv').config();

const dbConfig = {
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  server: process.env.DB_SERVER,
  port: parseInt(process.env.DB_PORT || '1433'),
  options: {
    encrypt: true, // SQL Server requires encryption on Azure/Cloud, recommended for local too
    trustServerCertificate: process.env.DB_TRUST_SERVER_CERTIFICATE === 'true', // self-signed cert on localhost
    enableArithAbort: true
  }
};

// Cấu hình kết nối tới database cụ thể
const appDbConfig = {
  ...dbConfig,
  database: process.env.DB_NAME
};

let pool = null;

// Hàm kết nối chính
async function getPool() {
  if (pool) return pool;
  try {
    pool = await new sql.ConnectionPool(appDbConfig).connect();
    console.log(`🔌 Kết nối thành công tới SQL Server Database: ${process.env.DB_NAME}`);
    return pool;
  } catch (err) {
    console.error('❌ Lỗi kết nối SQL Server: ', err.message);
    pool = null;
    throw err;
  }
}

// Khởi tạo Database và các Bảng nếu chưa tồn tại
async function initDatabase(forceRecreate = false) {
  console.log('⏳ Đang kiểm tra và khởi tạo Cơ sở dữ liệu...');

  // Bước 1: Kết nối tới database "master" để đảm bảo database đích tồn tại
  const masterConfig = { ...dbConfig, database: 'master' };
  let masterPool;
  try {
    masterPool = await new sql.ConnectionPool(masterConfig).connect();
    const dbName = process.env.DB_NAME;

    // Kiểm tra database đích
    const checkDbQuery = `SELECT database_id FROM sys.databases WHERE name = @dbName`;
    const request = masterPool.request();
    request.input('dbName', sql.NVarChar, dbName);
    const result = await request.query(checkDbQuery);
    const dbExists = result.recordset.length > 0;

    if (dbExists && forceRecreate) {
      console.log(`⚠️ Phát hiện Database "${dbName}" đã tồn tại và yêu cầu thiết lập lại (forceRecreate). Đang tiến hành xóa để làm sạch...`);
      // Đóng các kết nối đang hoạt động và xóa database
      await masterPool.request().query(`
        ALTER DATABASE [${dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [${dbName}];
      `);
      console.log(`🧹 Đã xóa Database cũ: ${dbName}`);
    }

    if (!dbExists || forceRecreate) {
      console.log(`Database "${dbName}" chưa tồn tại hoặc vừa được xóa. Đang tiến hành tạo mới...`);
      // Không được sử dụng Parameterized Query cho tên database trong câu lệnh CREATE DATABASE
      await masterPool.request().query(`CREATE DATABASE [${dbName}]`);
      console.log(`✅ Đã tạo thành công Database: ${dbName}`);
    } else {
      console.log(`Database "${dbName}" đã tồn tại.`);
    }
  } catch (err) {
    console.error('❌ Lỗi kiểm tra/tạo Database trên master: ', err.message);
    throw err;
  } finally {
    if (masterPool) {
      await masterPool.close();
    }
  }

  // Bước 2: Kết nối trực tiếp vào database đích và tạo các Bảng
  const dbPool = await getPool();
  
  // Mock transaction object to execute queries on dbPool directly and catch specific errors
  const transaction = {
    begin: async () => {},
    commit: async () => {},
    rollback: async () => {},
    request: () => {
      return {
        query: async (queryText) => {
          try {
            return await dbPool.request().query(queryText);
          } catch (err) {
            console.error('\n❌ Lỗi chi tiết khi chạy truy vấn SQL:');
            console.error('--- TRUY VẤN LỖI ---');
            console.error(queryText.trim());
            console.error('--------------------');
            console.error('Mã lỗi/Thông điệp:', err.message);
            throw err;
          }
        }
      };
    }
  };

  try {
    await transaction.begin();
    console.log('🏗️ Đang tạo cấu trúc bảng (nếu chưa có)...');

    // 1. Bảng users
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
      BEGIN
        CREATE TABLE [users] (
          [id] VARCHAR(50) PRIMARY KEY,
          [username] NVARCHAR(50) NOT NULL UNIQUE,
          [email] NVARCHAR(100) NULL UNIQUE,
          [password] VARCHAR(255) NOT NULL,
          [role] VARCHAR(20) NOT NULL DEFAULT 'user',
          [status] VARCHAR(20) NOT NULL DEFAULT 'active',
          [level] INT NOT NULL DEFAULT 1,
          [exp] INT NOT NULL DEFAULT 0,
          [coins] INT NOT NULL DEFAULT 0,
          [rankPoints] INT NOT NULL DEFAULT 0,
          [wins] INT NOT NULL DEFAULT 0,
          [totalGames] INT NOT NULL DEFAULT 0,
          [weekStartDate] VARCHAR(20) NULL,
          [isRewardClaimed] BIT NOT NULL DEFAULT 0,
          [loginDates] NVARCHAR(MAX) NOT NULL DEFAULT ''
        );
        CREATE INDEX idx_users_username ON [users]([username]);
      END
      ELSE
      BEGIN
        -- Tự động nâng cấp thêm cột [status] nếu bảng đã tồn tại từ trước (tránh lỗi xung đột CSDL cũ)
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('users') AND name = 'status')
        BEGIN
          ALTER TABLE [users] ADD [status] VARCHAR(20) NOT NULL DEFAULT 'active';
        END
      END
    `);

    // 2. Bảng user_solo_records [NEW]
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_solo_records')
      BEGIN
        CREATE TABLE [user_solo_records] (
          [userId] VARCHAR(50) PRIMARY KEY FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [bestSurvivor] INT NOT NULL DEFAULT 0,
          [bestQuick10] INT NOT NULL DEFAULT 0,
          [bestTimeRush] INT NOT NULL DEFAULT 0
        );
      END
    `);

    // Tự động dọn dẹp bảng user_weekly_login và user_login_dates cũ nếu còn tồn tại
    await transaction.request().query(`
      IF EXISTS (SELECT * FROM sys.tables WHERE name = 'user_weekly_login')
      BEGIN
        DROP TABLE [user_weekly_login];
      END
      IF EXISTS (SELECT * FROM sys.tables WHERE name = 'user_login_dates')
      BEGIN
        DROP TABLE [user_login_dates];
      END
    `);

    // 4. Bảng words (Từ vựng trung tâm)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'words')
      BEGIN
        CREATE TABLE [words] (
          [id] VARCHAR(20) PRIMARY KEY,
          [word] NVARCHAR(100) NOT NULL,
          [meaning] NVARCHAR(255) NOT NULL,
          [rankRequired] VARCHAR(20) NOT NULL DEFAULT 'Dong',
          [imageUrl] NVARCHAR(512) NULL,
          [imageSub] NVARCHAR(512) NULL
        );
      END
    `);

    // 5. Bảng vocab_sets (Bộ từ vựng)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'vocab_sets')
      BEGIN
        CREATE TABLE [vocab_sets] (
          [id] VARCHAR(20) PRIMARY KEY,
          [title] NVARCHAR(100) NOT NULL,
          [description] NVARCHAR(255) NULL,
          [category] NVARCHAR(50) NULL,
          [difficulty] NVARCHAR(20) NULL,
          [rankRequired] VARCHAR(20) NULL
        );
      END
    `);

    // 6. Bảng vocab_set_words (N-N giữa vocab_sets và words)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'vocab_set_words')
      BEGIN
        CREATE TABLE [vocab_set_words] (
          [setId] VARCHAR(20) FOREIGN KEY REFERENCES [vocab_sets]([id]) ON DELETE CASCADE,
          [wordId] VARCHAR(20) FOREIGN KEY REFERENCES [words]([id]) ON DELETE CASCADE,
          PRIMARY KEY ([setId], [wordId])
        );
      END
    `);

    // 7. Bảng vocab_set_levels (Lưu trữ thông tin chia level của Set)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'vocab_set_levels')
      BEGIN
        CREATE TABLE [vocab_set_levels] (
          [setId] VARCHAR(20) FOREIGN KEY REFERENCES [vocab_sets]([id]) ON DELETE CASCADE,
          [difficulty] NVARCHAR(20) NOT NULL,
          PRIMARY KEY ([setId], [difficulty])
        );
      END
    `);

    // 8. Bảng vocab_set_level_words (Quan hệ N-N liên kết level của set với words)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'vocab_set_level_words')
      BEGIN
        CREATE TABLE [vocab_set_level_words] (
          [setId] VARCHAR(20) FOREIGN KEY REFERENCES [vocab_sets]([id]) ON DELETE CASCADE,
          [difficulty] NVARCHAR(20) NOT NULL,
          [wordId] VARCHAR(20) FOREIGN KEY REFERENCES [words]([id]) ON DELETE CASCADE,
          PRIMARY KEY ([setId], [difficulty], [wordId])
        );
      END
    `);

    // 9. Bảng user_learned_sets
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_learned_sets')
      BEGIN
        CREATE TABLE [user_learned_sets] (
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [setId] VARCHAR(20) FOREIGN KEY REFERENCES [vocab_sets]([id]) ON DELETE CASCADE,
          PRIMARY KEY ([userId], [setId])
        );
      END
    `);

    // 10. Bảng user_saved_set_levels
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_saved_set_levels')
      BEGIN
        CREATE TABLE [user_saved_set_levels] (
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [setId] VARCHAR(20) FOREIGN KEY REFERENCES [vocab_sets]([id]) ON DELETE CASCADE,
          [level] NVARCHAR(20) NOT NULL,
          PRIMARY KEY ([userId], [setId])
        );
      END
    `);

    // 11. Bảng user_set_progress
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_set_progress')
      BEGIN
        CREATE TABLE [user_set_progress] (
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [setId] VARCHAR(20) FOREIGN KEY REFERENCES [vocab_sets]([id]) ON DELETE CASCADE,
          PRIMARY KEY ([userId], [setId])
        );
      END
    `);

    // 12. Bảng user_set_completed_levels (Level đã hoàn thành thuộc bộ từ vựng của User)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_set_completed_levels')
      BEGIN
        CREATE TABLE [user_set_completed_levels] (
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [setId] VARCHAR(20) FOREIGN KEY REFERENCES [vocab_sets]([id]) ON DELETE CASCADE,
          [level] NVARCHAR(20) NOT NULL,
          PRIMARY KEY ([userId], [setId], [level])
        );
      END
    `);

    // 13. Bảng user_word_progress (Trạng thái học từng từ của User)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_word_progress')
      BEGIN
        CREATE TABLE [user_word_progress] (
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [wordId] VARCHAR(20) FOREIGN KEY REFERENCES [words]([id]) ON DELETE CASCADE,
          [status] INT NOT NULL DEFAULT 0,
          PRIMARY KEY ([userId], [wordId])
        );
        CREATE INDEX idx_user_word_progress_user ON [user_word_progress]([userId]);
      END
    `);

    // 14. Bảng achievements (Danh sách thành tựu tĩnh)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'achievements')
      BEGIN
        CREATE TABLE [achievements] (
          [id] VARCHAR(50) PRIMARY KEY,
          [icon] NVARCHAR(20) NULL,
          [title] NVARCHAR(100) NOT NULL,
          [description] NVARCHAR(255) NULL,
          [maxProgress] INT NOT NULL
        );
      END
    `);

    // 15. Bảng user_achievements
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_achievements')
      BEGIN
        CREATE TABLE [user_achievements] (
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [achievementId] VARCHAR(50) FOREIGN KEY REFERENCES [achievements]([id]) ON DELETE CASCADE,
          [currentProgress] INT NOT NULL DEFAULT 0,
          [isUnlocked] BIT NOT NULL DEFAULT 0,
          [unlockDate] NVARCHAR(50) NULL,
          PRIMARY KEY ([userId], [achievementId])
        );
      END
    `);

    // 16. Bảng shop_items (Danh sách vật phẩm tĩnh trong shop)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'shop_items')
      BEGIN
        CREATE TABLE [shop_items] (
          [id] VARCHAR(50) PRIMARY KEY,
          [name] NVARCHAR(100) NOT NULL,
          [description] NVARCHAR(255) NULL,
          [icon] NVARCHAR(20) NULL,
          [price] INT NOT NULL,
          [rarity] NVARCHAR(20) NULL,
          [category] NVARCHAR(20) NULL,
          [equipType] NVARCHAR(20) NULL
        );
      END
    `);

    // 17. Bảng user_inventory (Kho đồ của User)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_inventory')
      BEGIN
        CREATE TABLE [user_inventory] (
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [itemId] VARCHAR(50) NOT NULL,
          [icon] NVARCHAR(20) NULL,
          [name] NVARCHAR(100) NOT NULL,
          [description] NVARCHAR(255) NULL,
          [quantity] INT NOT NULL DEFAULT 1,
          [rarity] NVARCHAR(20) NULL,
          [category] NVARCHAR(20) NULL,
          [equipType] NVARCHAR(20) NULL,
          [isEquipped] BIT NOT NULL DEFAULT 0,
          [isCombatItem] BIT NOT NULL DEFAULT 0,
          PRIMARY KEY ([userId], [itemId])
        );
        CREATE INDEX idx_user_inventory_user ON [user_inventory]([userId]);
      END
    `);

    // 18. Bảng quests (Bể nhiệm vụ tĩnh)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'quests')
      BEGIN
        CREATE TABLE [quests] (
          [id] VARCHAR(50) PRIMARY KEY,
          [title] NVARCHAR(100) NOT NULL,
          [description] NVARCHAR(255) NULL,
          [maxProgress] INT NOT NULL,
          [rewardCoins] INT NOT NULL,
          [rewardExp] INT NOT NULL,
          [questType] VARCHAR(50) NOT NULL
        );
      END
    `);

    // 19. Bảng user_quests (Tiến trình nhiệm vụ hôm nay của User)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_quests')
      BEGIN
        CREATE TABLE [user_quests] (
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [questId] VARCHAR(50) FOREIGN KEY REFERENCES [quests]([id]) ON DELETE CASCADE,
          [currentProgress] INT NOT NULL DEFAULT 0,
          [isClaimed] BIT NOT NULL DEFAULT 0,
          PRIMARY KEY ([userId], [questId])
        );
        CREATE INDEX idx_user_quests_user ON [user_quests]([userId]);
      END
    `);

    // 20. Bảng user_shop_history (Lịch sử giao dịch shop)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_shop_history')
      BEGIN
        CREATE TABLE [user_shop_history] (
          [id] INT IDENTITY(1,1) PRIMARY KEY,
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [itemName] NVARCHAR(100) NOT NULL,
          [price] INT NOT NULL,
          [date] VARCHAR(30) NOT NULL
        );
      END
    `);

    // 21. Bảng user_battle_history
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_battle_history')
      BEGIN
        CREATE TABLE [user_battle_history] (
          [matchId] VARCHAR(50) PRIMARY KEY,
          [userId] VARCHAR(50) FOREIGN KEY REFERENCES [users]([id]) ON DELETE CASCADE,
          [date] VARCHAR(30) NOT NULL,
          [isRanked] BIT NOT NULL DEFAULT 0,
          [isWin] BIT NOT NULL DEFAULT 0,
          [opponentName] NVARCHAR(50) NOT NULL,
          [playerFinalHP] INT NOT NULL,
          [enemyFinalHP] INT NOT NULL,
          [correctCount] INT NOT NULL,
          [totalRounds] INT NOT NULL
        );
        CREATE INDEX idx_user_battle_history_user ON [user_battle_history]([userId]);
      END
    `);

    // 22. Bảng battle_rounds (Vòng đấu của trận đấu)
    await transaction.request().query(`
      IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'battle_rounds')
      BEGIN
        CREATE TABLE [battle_rounds] (
          [id] INT IDENTITY(1,1) PRIMARY KEY,
          [matchId] VARCHAR(50) FOREIGN KEY REFERENCES [user_battle_history]([matchId]) ON DELETE CASCADE,
          [question] NVARCHAR(255) NOT NULL,
          [correctAnswer] NVARCHAR(255) NOT NULL,
          [playerAnswer] NVARCHAR(255) NOT NULL,
          [imageUrl] NVARCHAR(512) NULL,
          [isCorrect] BIT NOT NULL DEFAULT 0,
          [isTimeout] BIT NOT NULL DEFAULT 0
        );
        CREATE INDEX idx_battle_rounds_match ON [battle_rounds]([matchId]);
      END
    `);

    await transaction.commit();
    console.log('✅ Thiết kế database quan hệ đã được tạo lập thành công trên MS SQL Server!');
  } catch (err) {
    if (transaction) {
      await transaction.rollback();
    }
    console.error('❌ Thất bại khi chạy Migration Database: ', err.message);
    throw err;
  }
}

module.exports = {
  sql,
  getPool,
  initDatabase
};
