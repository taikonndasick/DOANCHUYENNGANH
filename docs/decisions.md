# NHẬT KÝ QUYẾT ĐỊNH THIẾT KẾ (ARCHITECTURE DECISION LOG - ADR)

| Mã | Quyết định | Nội dung chi tiết | Lý do | Người đề xuất |
| :--- | :--- | :--- | :--- | :--- |
| **ADR-001** | Kỳ hạn gia hạn | Gia hạn theo học kỳ cố định (HK1, HK2, Hè) do BQL mở đợt. | Tránh gia hạn lắt léo từng tháng, khớp với lịch đào tạo đại học. | User & Antigravity |
| **ADR-002** | Ràng buộc chuyển phòng | Chỉ cho phép chuyển giữa các phòng CÙNG loại và CÙNG mức giá. | Tránh phát sinh bài toán tính tiền thừa/thiếu giữa kỳ phức tạp. | User & Antigravity |
| **ADR-003** | Thanh toán điện nước | Sinh 1 hóa đơn chung cho phòng, gán Trưởng phòng đại diện thanh toán. | Đơn giản hóa đối soát kế toán KTX; các thành viên khác vẫn quét QR trả hộ được. | User & Antigravity |
| **ADR-004** | Điểm kỷ luật & Chế tài | Xếp loại rèn luyện KTX; điểm Kém (<50) gửi thông báo cảnh cáo về trường, không cấm gia hạn phòng. | Giữ tính nhân văn giáo dục, không đẩy sinh viên ra đường. | User & Antigravity |
| **ADR-005** | Hàng chờ (Waiting List) | FIFO danh sách chờ; Admin chủ động bấm duyệt khi có phòng trống. | Đảm bảo tính kiểm soát thực tế của BQL KTX trước khi cấp phòng. | User & Antigravity |
| **ADR-006** | Kiến trúc Chatbot AI | Dạng Hybrid: Nút bấm câu hỏi nhanh + Gemini API trả lời tự do theo ngữ cảnh sinh viên. | Tối ưu trải nghiệm: vừa nhanh gọn cho câu hỏi phổ biến, vừa thông minh cho câu hỏi cá nhân hóa. | User & Antigravity |
| **ADR-007** | Mô hình cộng tác 2 Agent | Phân công Codex (Backend) & Antigravity (Frontend) giao tiếp qua thư mục `docs/`. | Đảm bảo tính độc lập, tránh xung đột file và có tài liệu đồng bộ chuẩn. | Codex & User |
