# GIAO THỨC PHỐI HỢP AGENT (AGENT PROTOCOL)
**Dự án**: Website Quản lý Cư trú Ký túc xá Sinh viên (ASP.NET Core 8 MVC)
**Phân công trách nhiệm**:
- **Codex (Backend Agent)**: Thiết kế CSDL (Entity Framework Models, Migrations), Services, Repositories, Controllers / Web APIs, xác thực & phân quyền, Swagger / OpenAPI specs.
- **Antigravity (Frontend Agent)**: Thiết kế Razor Views (`.cshtml`), UI/UX Bootstrap, CSS/JS, Client-side Validation, SignalR client, tích hợp gọi API / ViewModel, AI Chatbot widget UI.

---

## NGUYÊN TẮC LÀM VIỆC (WORK RULES)

1. **Nguồn sự thật duy nhất (Single Source of Truth)**:
   - Nghiệp vụ: Tuân thủ nghiêm ngặt `docs/business-rules.md`.
   - API / Dữ liệu: Tuân thủ `docs/openapi.yaml`.
2. **Quy trình trao đổi câu hỏi & giải đáp**:
   - Khi có bất kỳ thắc mắc kỹ thuật nào, agent tạo một mục mới trong `docs/api-questions.md` với mã định danh (VD: `FE-001`, `BE-001`) và trạng thái `OPEN`.
   - Agent đối ứng đọc và trả lời, đổi trạng thái sang `RESOLVED`.
   - Quyết định kiến trúc quan trọng được tóm tắt lại vào `docs/decisions.md`.
3. **Cập nhật tiến độ**:
   - Cả hai agent chủ động cập nhật trạng thái các task trong `docs/integration-status.md` (TODO -> IN_PROGRESS -> REVIEW -> DONE).
4. **Không sửa đè file (File Ownership Boundary)**:
   - Codex chịu trách nhiệm chính: `Models/`, `Data/`, `Migrations/`, `Services/`, `Controllers/Api/` (hoặc Business Controller logic).
   - Antigravity chịu trách nhiệm chính: `Views/`, `wwwroot/` (css, js), `ViewModels/` (hoặc phối hợp), Chatbot UI.
