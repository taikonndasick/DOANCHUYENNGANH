PATCH: Seed dữ liệu KTX + Quản lý biểu mẫu

1) Paste code:
- Models/FormTemplate.cs
- Data/ApplicationDbContext.cs
- Areas/Admin/Controllers/FormTemplateController.cs
- Areas/Admin/Views/FormTemplate/*
- Areas/User/Controllers/FormController.cs
- Areas/User/Views/Form/Templates.cshtml
- Views/Shared/_AdminLayout.cshtml

2) Build project.

3) Chạy SQL:
- Mở SQL/ktx_seed_full_dorm_6_buildings_80_percent.sql trong SSMS hoặc SQL Server Object Explorer.
- Chọn đúng database đang dùng trong appsettings.json.
- Execute toàn bộ file.

4) Test:
/Admin/FormTemplate
/User/Form/Templates
/Admin/Building
/Admin/Room
/Admin/Resident
/Admin/ResidentCard
/Admin/Vehicle

Lưu ý:
- Script chỉ xóa dữ liệu seed có StudentCode LIKE 'KTX26%'.
- Dữ liệu thật hiện có của bạn không bị xóa.
- Nếu chạy nhiều lần, dữ liệu seed cũ sẽ được tạo lại để tránh trùng.
