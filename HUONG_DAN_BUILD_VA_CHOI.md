# Hướng dẫn Build và Chơi Đấu LAN PvP - Game VocabLearning

Tài liệu này hướng dẫn chi tiết cách cấu hình, khởi chạy hệ thống Backend, cách Build game ra file `.exe` chạy trên Windows và cách chơi đối kháng 2 người qua mạng LAN.

---

## 🛠️ Yêu cầu Hệ thống
1. **Client**: Unity Editor phiên bản 6000.4.0f1 trở lên (chạy trên Windows).
2. **Backend**: Node.js đã được cài đặt trên máy làm máy chủ (Host).
3. **Database**: Microsoft SQL Server (đã khởi tạo và cấu hình DB kết nối thành công).

---

## 🔌 Phần 1: Khởi chạy Backend Server (Máy chủ)

Để hệ thống lưu trữ điểm Rank, tài khoản người dùng và đồng bộ phòng đấu LAN hoạt động, **bắt buộc** phải có máy chủ Backend chạy song song.

### Cách 1: Chạy nhanh bằng file Batch (Khuyên dùng)
Ngay tại thư mục gốc của dự án, click đúp chuột vào file:
* **[Start_Server.bat](file:///d:/Chivinh/2026%20Du%20an%20mon%20hoc/Vocab/VocabLearning/Start_Server.bat)**
Một cửa sổ Command Prompt sẽ tự động mở ra, nạp cơ sở dữ liệu và khởi chạy Server Express trên cổng `5000`.

### Cách 2: Chạy thủ công bằng dòng lệnh
Mở Terminal/Command Prompt tại thư mục `server` và chạy các lệnh:
1. **Cài đặt thư viện (nếu là lần đầu)**:
   ```bash
   npm install
   ```
2. **Khởi tạo dữ liệu mẫu vào SQL Server**:
   ```bash
   npm run seed
   ```
3. **Chạy server ở chế độ phát triển (Development)**:
   ```bash
   npm run dev
   ```

---

## 📦 Phần 2: Hướng dẫn Build Game ra file `.exe` (Windows)

Để chơi đối kháng giữa 2 người, bạn cần build game ra định dạng `.exe` để chạy trên máy tính thứ hai hoặc chạy song song với Unity Editor trên cùng 1 máy.

### Các bước Build trong Unity Editor:
1. Mở dự án trong **Unity Editor**.
2. Trong cửa sổ **Project**, đúp chuột vào file cảnh chính:
   * **[Assets/Scenes/MainMenuScene.unity](file:///d:/Chivinh/2026%20Du%20an%20mon%20hoc/Vocab/VocabLearning/Assets/Scenes/MainMenuScene.unity)** để nạp game.
3. Chọn menu **File** -> **Build Settings...** (hoặc nhấn `Ctrl + Shift + B`).
4. Tại danh sách **Scenes In Build**:
   * Nếu ô trống, nhấn nút **Add Open Scenes** để thêm cảnh `MainMenuScene` vào.
5. Tại danh sách **Platform**:
   * Chọn mục tiêu là **Windows** (hoặc **Standalone** -> Target: Windows).
   * Đảm bảo **Architecture** chọn **Intel 64-bit** (x86_64).
6. Nhấn nút **Build** ở góc dưới bên phải.
7. Tạo một thư mục mới (ví dụ đặt tên là `Builds`) trong thư mục dự án của bạn và chọn nó.
8. Đặt tên cho file thực thi (ví dụ: `VocabGame`) và nhấn **Save**.
9. Đợi 1 - 2 phút để quá trình build hoàn tất. Thư mục chứa file `VocabGame.exe` sẽ tự động mở ra.

---

## 🎮 Phần 3: Hướng dẫn kết nối và Đấu LAN PvP (2 Người)

Hệ thống Đấu Hạng (Ranked Mode) hoạt động dựa trên cơ chế ghép trận thời gian thực qua mạng cục bộ (LAN).

### Bước 1: Cấu hình địa chỉ IP máy chủ (Host) qua file `.env`

Game hỗ trợ tự động tải địa chỉ IP của máy chủ từ file cấu hình bên ngoài có tên là **`.env`** nằm cùng thư mục với file `.exe` chạy game (hoặc nằm ở thư mục gốc của dự án nếu chạy trong Unity Editor). Bạn **không cần** sửa mã nguồn hay build lại game khi đổi máy chủ!

> [!NOTE]
> File `.env` chứa thông tin cấu hình IP cá nhân của từng người dùng nên đã được thêm vào `.gitignore` để **không bị đẩy lên Git**, tránh xung đột IP giữa các thành viên.
> Dự án cung cấp sẵn một file **`.env.example`** làm mẫu cấu hình.

#### 📍 Vị trí của các file cấu hình phải nằm ở đâu?
* **Khi chạy trong Unity Editor (để test)**:
  1. Sao chép file `.env.example` nằm ở thư mục gốc của dự án.
  2. Dán và đổi tên thành `.env` (cũng đặt tại thư mục gốc):
     `d:\Chivinh\2026 Du an mon hoc\Vocab\VocabLearning\.env`
* **Khi chạy bản Build game (`.exe`)**: File `.env` bắt buộc phải nằm ở **cùng thư mục, ngay bên cạnh** file chạy game `.exe` của bạn:
  ```text
  📁 Builds/ (Thư mục chứa game đã build)
   ├── 📄 VocabGame.exe (File chạy game chính)
   ├── 📁 VocabGame_Data/ (Thư mục dữ liệu game)
   ├── ...
   └── 📄 .env (👈 FILE .env PHẢI NẰM TẠI ĐÂY)
  ```

#### 🛠️ Các bước cấu hình chi tiết:
1. **Tìm địa chỉ IP của máy chủ (Máy chạy Backend Server)**:
   * Trên máy tính đang chạy Backend Server (đã chạy file `Start_Server.bat`), nhấn nút **Start**, gõ `cmd` rồi nhấn Enter để mở **Command Prompt**.
   * Gõ lệnh `ipconfig` rồi nhấn Enter.
   * Tìm dòng `IPv4 Address` dưới mục kết nối mạng của bạn (ví dụ: `192.168.1.15`). Đây là IP máy chủ của bạn.
2. **Khởi tạo và cấu hình file `.env`**:
   * **Trong Unity Editor**:
     1. Tìm file `.env.example` ở thư mục gốc dự án.
     2. Copy và đổi tên thành `.env`.
     3. Mở file `.env` bằng Notepad và sửa dòng `ServerUrl=http://192.168.1.15:5000/api` (thay IP cho đúng).
   * **Trong bản Build game (.exe)**:
     1. Mở thư mục chứa game đã build (ví dụ thư mục `Builds/`).
     2. Nếu chưa thấy file `.env`, bạn hãy copy file `.env` hoặc `.env.example` từ thư mục gốc của dự án vào đây và đổi tên thành `.env`. (Hoặc chỉ cần khởi động game `VocabGame.exe` lên 1 lần, game cũng sẽ tự động tạo ra file `.env` mặc định ngay bên cạnh nó).
     3. Mở file `.env` bằng Notepad.
     4. Chỉnh sửa địa chỉ IP tại dòng `ServerUrl` khớp với IP máy chủ bạn đã tìm ở Bước 1. Ví dụ:
        ```text
        ServerUrl=http://192.168.1.15:5000/api
        ```
     5. Lưu file cấu hình lại (`Ctrl + S`).
3. **Gửi game sang máy khác (Client)**:
   * Khi muốn gửi game sang máy tính thứ hai (máy khách), bạn hãy copy/nén **toàn bộ thư mục chứa bản build** (bao gồm file `VocabGame.exe`, thư mục `VocabGame_Data` và file `.env` đi kèm).
   * Trên máy tính thứ hai đó, mở file `.env` lên và kiểm tra xem dòng `ServerUrl` đã đúng với IP của máy chủ chưa. Nếu chưa đúng, chỉ cần sửa lại cho đúng rồi lưu lại. Bấm chạy game `VocabGame.exe` là hai máy có thể kết nối chung vào Server và chơi game cùng nhau ngay lập tức!

### Bước 2: Chuẩn bị 2 tài khoản chơi game
1. Cả hai máy tính (hoặc 2 cửa sổ game trên 1 máy) phải kết nối chung vào **cùng một mạng LAN/Wifi**.
2. Khởi động game.
3. Đăng nhập vào 2 tài khoản khác nhau trên 2 Client (Ví dụ: Máy A đăng nhập tài khoản `user1`, Máy B đăng nhập tài khoản `user2`).

### Bước 3: Ghép trận và Bắt đầu chơi
1. Cả hai người chơi cùng nhấn nút **Đấu Hạng (Ranked Mode)** trên màn hình chính.
2. Giao diện tìm trận tối giản với Spinner xoay vòng sẽ xuất hiện.
3. Ngay khi Server tìm thấy nhau (trong khoảng dưới 0.1 giây), cả hai màn hình sẽ hiển thị thông báo **"ĐÃ TÌM THẤY ĐỐI THỦ! ⚔️ VS ⚔️"** trong vòng **2 giây** trước khi tự động nạp vào trận đấu.

### Bước 4: Logic Đấu PvP trong trận
* **Cơ chế tốc độ (Speed Contest)**: Mỗi câu hỏi có 15 giây đếm ngược.
  * Nếu một bên chọn đáp án **ĐÚNG** trước, lượt đấu đó sẽ **dừng lại ngay lập tức**. Nút bấm của bên chậm hơn sẽ bị khóa lại (làm mờ 50%) và không được chọn nữa. Bên chậm hơn (hoặc trả lời sai) sẽ bị trừ 10 HP.
  * Nếu cả hai cùng trả lời sai, cả hai đều bị trừ 10 HP.
* **Xử lý thoát trận (Flee)**:
  * Nếu một người chơi nhấn nút bỏ chạy (**Flee/Thoát**), game sẽ xử thua lập tức cho người đó (-15 Rank).
  * Đối thủ còn lại sẽ ngay lập tức được thông báo chiến thắng (**VICTORY!** và +25 Rank) thông qua cơ chế tự động đồng bộ thời gian thực.
* **Bảng tổng kết (Summary)**: Khi trận đấu kết thúc (máu của 1 trong 2 người về 0), màn hình tổng kết sẽ hiển thị chính xác tỉ lệ câu trả lời đúng của bạn (ví dụ: `2 / 3` đúng), tô màu xanh lá cho câu đúng, màu đỏ cho câu sai và màu xám cho câu bị bỏ qua/hết giờ.
