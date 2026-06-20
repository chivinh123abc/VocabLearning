.PHONY: install seed setup dev start export all

# Cài đặt thư viện node_modules cho backend
install:
	cd server && npm install

# Nạp dữ liệu giả lập từ db.json vào SQL Server
seed:
	cd server && npm run seed

# Từ đồng nghĩa với seed để dễ nhớ
setup: seed

# Chạy server ở chế độ phát triển (Development với auto-reload)
dev:
	cd server && npm run dev

# Chạy server ở chế độ thường
start:
	cd server && npm start

# Xuất dữ liệu hiện tại từ SQL Server ngược lại về db.json
export:
	cd server && npm run db:export

# Tiện ích chạy tất cả từ cài đặt đến chạy thử
all: install seed dev
