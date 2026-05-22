# 🎮 VocabLearning - Hướng dẫn chạy dự án

## 💻 Cách khởi chạy Server Backend (Node.js & SQL Server)

Đảm bảo bạn đã cài đặt **Node.js** và máy tính đang chạy dịch vụ **Microsoft SQL Server**.

### Bước 1: Di chuyển vào thư mục server
```bash
cd server
```

### Bước 2: Cài đặt các thư viện phụ thuộc
```bash
npm install
```

### Bước 3: Cấu hình tệp tin môi trường `.env`
Sao chép tệp `.env.example` thành `.env` và điền thông tin đăng nhập SQL Server của bạn:
```env
PORT=5000
DB_USER=sa
DB_PASSWORD=mat_khau_sql_server_cua_ban
DB_SERVER=localhost
DB_NAME=GAMEHOCTUVUNG
DB_PORT=1433
DB_TRUST_SERVER_CERTIFICATE=true
```

### Bước 4: Khởi tạo cơ sở dữ liệu và nạp dữ liệu mẫu
Lệnh này sẽ tự động kết nối vào SQL Server để tạo database `GAMEHOCTUVUNG`, thiết lập toàn bộ cấu trúc bảng và import dữ liệu từ vựng mẫu từ Unity:
```bash
npm run db:setup
```

### Bước 5: Chạy Server
* **Chế độ phát triển (Tự động tải lại khi sửa code):**
  ```bash
  npm run dev
  ```
* **Chế độ chạy thường:**
  ```bash
  npm run start
  ```

---

## 🎮 Cách chạy Client (Unity)

1. Mở thư mục gốc của dự án bằng **Unity Editor**.
2. Đảm bảo cấu hình API Endpoint của game trong Unity trỏ về server của bạn (mặc định: `http://localhost:5000/api`).
3. Bấm **Play** để trải nghiệm game!
