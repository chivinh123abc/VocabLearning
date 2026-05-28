const jwt = require('jsonwebtoken');
require('dotenv').config();

const JWT_SECRET = process.env.JWT_SECRET || 'nckh_vocab_learning_secret_key_2026';

// Middleware xác minh tính hợp lệ của JWT Token
const verifyToken = (req, res, next) => {
  const authHeader = req.headers['authorization'];
  
  // Hỗ trợ cả trường hợp gửi Header dạng "Bearer <token>"
  const token = authHeader && authHeader.startsWith('Bearer ') ? authHeader.split(' ')[1] : authHeader;

  if (!token) {
    console.warn(`[AuthMiddleware] Khách gửi yêu cầu thiếu Token bảo mật tại URL: ${req.originalUrl}`);
    return res.status(401).json({ success: false, message: 'Không tìm thấy Token bảo mật! Vui lòng đăng nhập lại.' });
  }

  jwt.verify(token, JWT_SECRET, (err, decoded) => {
    if (err) {
      console.warn(`[AuthMiddleware] Token không hợp lệ hoặc đã hết hạn. Chi tiết lỗi: ${err.message}`);
      return res.status(403).json({ success: false, message: 'Token không hợp lệ hoặc đã hết hạn!' });
    }
    
    // Gán thông tin đã giải mã vào req.user (chứa id và role)
    req.user = decoded;
    next();
  });
};

// Middleware kiểm tra quyền Administrator
const isAdmin = (req, res, next) => {
  if (req.user && req.user.role === 'admin') {
    next();
  } else {
    console.warn(`[AuthMiddleware] Tài khoản '${req.user ? req.user.id : 'Không rõ'}' cố gắng truy cập quyền Admin nhưng bị từ chối!`);
    return res.status(403).json({ success: false, message: 'Từ chối truy cập! Quyền hạn này chỉ dành cho Admin.' });
  }
};

module.exports = {
  verifyToken,
  isAdmin
};
