const express = require('express');
const cors = require('cors');
require('dotenv').config();

const { initDatabase } = require('./config/db');

const authRoutes = require('./routes/authRoutes');
const gameRoutes = require('./routes/gameRoutes');
const userRoutes = require('./routes/userRoutes');
const adminRoutes = require('./routes/adminRoutes');

const app = express();
const PORT = process.env.PORT || 5000;

app.use(cors());
app.use(express.json());

// Logger middleware
app.use((req, res, next) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.url}`);
  next();
});

// Định tuyến API (Mount Routers)
app.use('/api/auth', authRoutes);
app.use('/api', gameRoutes); // Mounts /api/globals
app.use('/api/user', userRoutes); // Mounts /api/user/sync
app.use('/api/admin', adminRoutes); // Mounts /api/admin/words

// Khởi chạy server
app.listen(PORT, async () => {
  console.log(`🚀 Node.js Express server đang chạy trên cổng ${PORT}`);
  try {
    // Tự động kiểm tra database và bảng khi khởi động
    await initDatabase();
  } catch (err) {
    console.error('🚨 Khởi tạo Database thất bại lúc khởi động: ', err.message);
  }
});
