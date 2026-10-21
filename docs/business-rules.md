# QUY TẮC NGHIỆP VỤ HỆ THỐNG (BUSINESS RULES SPECIFICATION)

Tài liệu này là căn cứ chính thức để Backend (Codex) triển khai Data Models, Migrations, Validation logic và Frontend (Antigravity) triển khai Form UI & Client Validation.

---

## 1. GIA HẠN HỢP ĐỒNG CƯ TRÚ (Contract Renewal)
- **BR-GH01 (Cấu hình đợt gia hạn)**:
  - Admin tạo đợt gia hạn theo học kỳ cố định (Học kỳ 1, Học kỳ 2, Học kỳ hè).
  - Mỗi đợt thiết lập: Thời gian mở nhận đơn -> Thời gian đóng, Thời hạn hợp đồng mới (`StartDate`, `EndDate`), và Mức phí phòng cho kỳ mới.
- **BR-GH02 (Điều kiện sinh viên nộp đơn)**:
  - Sinh viên phải có hợp đồng đang còn hiệu lực (`Status = "Active"`).
  - Không còn hóa đơn quá hạn chưa thanh toán (`Unpaid` vượt quá `DueDate`).
  - Nộp đơn trong thời gian đợt gia hạn đang mở. Mỗi sinh viên chỉ gửi tối đa 1 đơn gia hạn trong cùng một đợt.
- **BR-GH03 (Xét duyệt của Quản trị viên)**:
  - Admin xem danh sách đơn gia hạn ở trạng thái `Pending` -> Chọn **Phê duyệt (Approved)** hoặc **Từ chối (Rejected)** (bắt buộc nhập lý do nếu từ chối).
- **BR-GH04 (Tự động hóa sau khi duyệt)**:
  - Cập nhật thời hạn hợp đồng: Kéo dài ngày kết thúc (`EndDate`) theo kỳ mới.
  - Tự động sinh 1 Hóa đơn tiền phòng kỳ mới (`Invoice`) cho sinh viên với hạn nộp quy định (mặc định 15 ngày).
  - Gửi thông báo hệ thống (Notification) xác nhận gia hạn thành công đến tài khoản sinh viên.

---

## 2. ĐĂNG KÝ CHUYỂN PHÒNG (Room Transfer)
- **BR-CP01 (Ràng buộc tương thích loại phòng - Strict Match)**:
  - Sinh viên chỉ được phép chuyển sang phòng CÙNG loại và CÙNG mức giá với phòng hiện tại (VD: Phòng 4 người chỉ được chuyển sang phòng 4 người khác còn chỗ trống; không được chuyển sang phòng khác mức giá để tránh chênh lệch tiền).
- **BR-CP02 (Điều kiện gửi đơn)**:
  - Hợp đồng hiện tại còn hạn tối thiểu 30 ngày.
  - Đã thanh toán đầy đủ các hóa đơn của phòng hiện tại.
  - Phòng đích phải còn chỗ trống thực tế: $\text{Số sinh viên đang ở} < \text{Sức chứa tối đa}$.
  - Bắt buộc nhập lý do chuyển phòng (tối thiểu 10 ký tự).
- **BR-CP03 (Xét duyệt & Cập nhật dữ liệu)**:
  - Đơn chuyển phòng gửi lên Admin ở trạng thái `Pending`.
  - Khi Admin bấm **Phê duyệt**:
    - Tự động đổi `ApartmentId` của sinh viên trong hồ sơ sang phòng mới.
    - Giảm sĩ số phòng cũ đi 1 và tăng sĩ số phòng mới lên 1.
    - Cập nhật thông tin phòng mới vào Hợp đồng hiện hành.

---

## 3. CƠ SỞ VẬT CHẤT & BÀN GIAO TÀI SẢN (Asset Handover)
- **BR-CSVC01 (Định mức tài sản phòng)**:
  - Mỗi phòng có danh mục tài sản quy chuẩn theo loại phòng (Giường, bàn học, ghế, quạt trần, đèn tuýp, bình nóng lạnh, khóa cửa...).
- **BR-CSVC02 (Biên bản bàn giao nhận phòng - Check-in)**:
  - Khi sinh viên mới được xếp vào phòng, hệ thống tự động sinh Biên bản bàn giao ở trạng thái `Chờ xác nhận`.
  - Sinh viên kiểm tra thực tế trong vòng 48 giờ và tick xác nhận tình trạng từng món: `Tốt` hoặc `Báo hỏng/Cần sửa`.
  - Hết 48 giờ nếu sinh viên không phản hồi, hệ thống tự động khóa biên bản ở trạng thái `Đã xác nhận` (mặc định ban đầu đều tốt).
- **BR-CSVC03 (Nghiệm thu khi trả phòng/chuyển đi - Check-out)**:
  - Khi sinh viên trả phòng hoặc chuyển phòng, Admin kiểm tra lại tài sản.
  - Nếu phát hiện mất mát hoặc hư hỏng do lỗi người dùng: Admin ghi nhận loại tài sản, số lượng hỏng và mức bồi thường theo bảng giá quy định.
  - Hệ thống tự động cộng khoản bồi thường vào **Hóa đơn thanh lý trả phòng**.

---

## 4. HÓA ĐƠN ĐIỆN - NƯỚC - RÁC THEO PHÒNG (Room Utility Billing)
- **BR-HD01 (Nhập chỉ số định kỳ theo phòng)**:
  - Cuối mỗi tháng, Admin chọn Tòa nhà -> Phòng -> Nhập:
    - Chỉ số điện cũ & mới ($\text{Điện mới} \ge \text{Điện cũ}$).
    - Chỉ số nước cũ & mới ($\text{Nước mới} \ge \text{Nước cũ}$).
    - Phí rác/vệ sinh cố định của phòng.
- **BR-HD02 (Công thức tính tổng tiền phòng)**:
  $$\text{Tổng tiền phòng} = [(\text{Điện mới} - \text{Điện cũ}) \times \text{Đơn giá điện}] + [(\text{Nước mới} - \text{Nước cũ}) \times \text{Đơn giá nước}] + \text{Phí rác}$$
- **BR-HD03 (Chỉ định đại diện thanh toán - Trưởng phòng)**:
  - Mỗi phòng có 1 sinh viên được gán vai trò `Trưởng phòng (Room Leader)`.
  - Hóa đơn điện nước tháng được phát hành chung cho cả phòng, người đứng tên đại diện thanh toán chính là Trưởng phòng.
- **BR-HD04 (Minh bạch thông tin & Thanh toán VietQR)**:
  - Mọi sinh viên trong phòng đều xem được chi tiết hóa đơn (chỉ số cũ/mới, sản lượng tiêu thụ, tổng tiền).
  - Hóa đơn tích hợp mã VietQR động (Napas 247) có sẵn số tiền và cú pháp `KTX DIENNUOC [MaPhong] [Thang]`.
  - Bất kỳ thành viên nào trong phòng cũng có thể quét QR trả thay cho phòng; khi thanh toán thành công, hóa đơn đổi sang `Paid` và tất cả thành viên đều thấy cập nhật ngay lập tức.

---

## 5. VI PHẠM & ĐIỂM RÈN LUYỆN KTX (Discipline & Merits)
- **BR-KL01 (Khởi tạo điểm rèn luyện)**: Mỗi sinh viên khi bắt đầu một kỳ lưu trú được cấp mặc định 100 điểm.
- **BR-KL02 (Ghi nhận vi phạm & Điểm trừ)**:
  - Ban quản lý lập biên bản vi phạm: Chọn sinh viên, chọn lỗi vi phạm (Về quá giờ: -5đ; Nấu ăn sai quy định: -10đ; Gây ồn ào: -5đ; Xả rác: -5đ; Sử dụng chất cấm: -30đ), ngày vi phạm, ghi chú/ảnh minh chứng.
  - Hệ thống tự động trừ điểm rèn luyện của sinh viên trong kỳ đó.
  - Nếu lỗi có mức phạt tiền hành chính, hệ thống tự động sinh thêm khoản tiền phạt kèm theo.
- **BR-KL03 (Điểm cộng / Khen thưởng)**:
  - Tham gia dọn vệ sinh KTX: +5đ; Ban tự quản KTX: +10đ; Tham gia hiến máu/tình nguyện: +10đ.
- **BR-KL04 (Thang xếp loại & Chế tài)**:
  - Thang xếp loại: $\ge 90$: Xuất sắc | $80 - 89$: Tốt | $65 - 79$: Khá | $50 - 64$: Trung bình | $< 50$: Kém.
  - Chế tài: Khi xếp loại **Kém** (dưới 50 điểm), hệ thống tự động gửi **Thông báo cảnh cáo kỷ luật** tới sinh viên và xuất danh sách báo cáo về Khoa/Nhà trường (không cấm gia hạn phòng nhưng lưu hồ sơ kỷ luật).

---

## 6. ĐỢT ĐĂNG KÝ & DANH SÁCH CHỜ (Waiting List)
- **BR-DK01 (Quản lý đợt đăng ký)**:
  - Admin tạo đợt đăng ký: Tên đợt (Đợt chính thức, Đợt phụ/bổ sung), Ngày bắt đầu, Ngày kết thúc.
  - Cổng đăng ký chỉ mở khi thời gian hiện tại nằm trong khoảng [Ngày bắt đầu, Ngày kết thúc].
- **BR-DK02 (Cơ chế vào Danh sách chờ - Waiting List)**:
  - Khi sinh viên nộp đơn đăng ký nhưng tất cả các phòng phù hợp đều đã hết chỗ, hoặc đăng ký vào đợt phụ ngoài đợt:
  - Hệ thống tự động đưa hồ sơ vào `WaitingList` với số thứ tự hàng đợi (Queue Position: 1, 2, 3...) theo nguyên tắc ai nộp trước xếp trước (FIFO).
  - Sinh viên có thể xem trực tiếp vị trí hàng đợi của mình trên trang cá nhân (*"Vị trí hàng chờ: #3"*).
- **BR-DK03 (Duyệt cấp phòng từ Danh sách chờ)**:
  - Khi có phòng trống (do sinh viên cũ trả phòng hoặc chuyển đi):
  - Admin mở màn hình Danh sách chờ -> Hệ thống gợi ý sinh viên đứng đầu danh sách chờ phù hợp với loại phòng/giới tính của phòng vừa trống.
  - Admin bấm **"Duyệt cấp phòng"** -> Chuyển thành cư dân chính thức, tạo Hợp đồng, cập nhật số lượng phòng và gửi thông báo mời nhận phòng tới sinh viên.

---

## 7. TRỢ LÝ ẢO AI (AI Dorm Assistant)
- **BR-AI01 (Phân loại câu hỏi & Trả lời)**:
  - Hỏi đáp chung (FAQ): Giải đáp chính xác nội quy giờ giấc, các khoản chi phí, quy định cấm, hướng dẫn chuyển phòng, hướng dẫn thanh toán.
  - Hỏi đáp dữ liệu cá nhân (Context-Aware):
    - Nếu chưa đăng nhập: Yêu cầu đăng nhập để tra cứu.
    - Nếu đã đăng nhập: Tự động truyền ngữ cảnh của sinh viên (Họ tên, Mã SV, Phòng đang ở, Hạn hợp đồng, Hóa đơn điện nước tháng này) để AI trả lời đúng 100% số liệu thực tế.
- **BR-AI02 (Nút bấm câu hỏi nhanh - Quick FAQ Buttons)**:
  - Tích hợp các nút bấm một chạm ở widget chat (Giờ giới nghiêm, Cách xin chuyển phòng, Quét QR điện nước, Kiểm tra hóa đơn).
