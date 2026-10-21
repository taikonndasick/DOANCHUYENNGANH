# ĐỒ ÁN CHUYÊN NGÀNH: XÂY DỰNG WEBSITE QUẢN LÝ CƯ TRÚ SINH VIÊN KÝ TÚC XÁ

Hệ thống quản lý cư trú sinh viên ký túc xá được xây dựng trên nền tảng **ASP.NET Core 8.0 MVC**, **Entity Framework Core 8**, **SQL Server** và **SignalR**.

---

## 🚀 CÔNG NGHỆ SỬ DỤNG
- **Backend**: C# .NET 8.0, ASP.NET Core MVC, Entity Framework Core 8.0, ASP.NET Core Identity.
- **Realtime**: SignalR Hub (`/notificationHub`) đẩy thông báo tức thì.
- **Frontend**: Razor Views (`.cshtml`), Bootstrap 5, AdminLTE, SweetAlert2, jQuery.
- **Database**: Microsoft SQL Server (`QuanLyKTX_Rebuild_DB`).

---

## 📂 CẤU TRÚC DỰ ÁN
```
├── Areas/
│   ├── Admin/         # Phân hệ quản trị (Quản lý phòng, sinh viên, hóa đơn, biểu mẫu...)
│   ├── User/          # Phân hệ sinh viên (Đăng ký phòng, xem hóa đơn, nộp đơn từ...)
│   └── Reception/     # Phân hệ lễ tân / bảo vệ
├── Controllers/       # Các controller chính (Auth, Home, Account...)
├── Data/              # ApplicationDbContext và SeedData khởi tạo tài khoản
├── Hubs/              # SignalR NotificationHub
├── Models/            # Entity models (Apartment, ResidentProfile, StudentRegistration...)
├── Services/          # Business logic services
├── SQL/               # Script tạo CSDL và seed dữ liệu mẫu KTX
├── docs/              # Tài liệu phân tích, hợp đồng API và file Báo cáo Word
│   ├── agent-protocol.md      # Quy ước phối hợp Codex (BE) & Antigravity (FE)
│   ├── business-rules.md      # Đặc tả quy tắc nghiệp vụ 7 module mở rộng
│   ├── decisions.md           # Nhật ký quyết định kiến trúc (ADR)
│   ├── integration-status.md  # Bảng theo dõi tiến độ tích hợp
│   ├── api-questions.md       # Kênh trao đổi câu hỏi API giữa các Agent
│   ├── openapi.yaml           # Hợp đồng API OpenAPI 3.0
│   └── BaoCao_QuanLyKTX.docx  # Báo cáo đồ án Word chi tiết
└── QuanLyKTX.csproj   # File cấu hình project chính
```

---

## 🛠 HƯỚNG DẪN CÀI ĐẶT & CHẠY DỰ ÁN

### 1. Cấu hình Cơ sở dữ liệu:
Trong file `appsettings.json`, chỉnh sửa chuỗi kết nối phù hợp với máy của bạn:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=QuanLyKTX_Rebuild_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 2. Khởi tạo dữ liệu (nếu cài mới):
Mở file `SQL/ktx_seed_full_dorm_6_buildings_80_percent.sql` trong SQL Server Management Studio (SSMS) và thực thi trên database `QuanLyKTX_Rebuild_DB`.

### 3. Chạy ứng dụng:
```bash
dotnet restore
dotnet build
dotnet run
```
Truy cập: `https://localhost:7001` (hoặc cổng HTTP hiển thị trên console).

---

## 👥 TÀI KHOẢN MẶC ĐỊNH
- **Admin**: `admin` / Mật khẩu: `123456`
- **Lễ tân**: `reception` / Mật khẩu: `123456`
- **Sinh viên**: `user` / Mật khẩu: `123456`
