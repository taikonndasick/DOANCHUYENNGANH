/*
    Seed / update bộ biểu mẫu cốt lõi cho đồ án KTX.
    Chạy trên đúng database của project, ví dụ: QuanLyKTX_Rebuild_DB.
    Script không xóa biểu mẫu cũ; chỉ thêm mới nếu chưa có và cập nhật mô tả/icon nếu đã tồn tại.
*/

IF OBJECT_ID(N'dbo.FormTemplates', N'U') IS NULL
BEGIN
    PRINT N'Chưa có bảng FormTemplates. Hãy chạy migration tạo FormTemplate trước.';
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();

DECLARE @Templates TABLE
(
    [Name] NVARCHAR(250),
    [Type] NVARCHAR(100),
    [Description] NVARCHAR(MAX),
    [Icon] NVARCHAR(100),
    [IsActive] BIT,
    [IsTemporary] BIT
);

INSERT INTO @Templates([Name], [Type], [Description], [Icon], [IsActive], [IsTemporary])
VALUES
(N'Đơn đăng ký ở ký túc xá', N'Đăng ký ở', N'Mẫu tham khảo cho sinh viên đăng ký lưu trú ký túc xá. Luồng chính đã được hệ thống xử lý bằng form đăng ký online.', N'bi-house-add', 1, 0),
(N'Đơn xin chuyển phòng', N'Chuyển phòng', N'Dành cho sinh viên đang lưu trú có nhu cầu chuyển sang phòng khác. Admin kiểm tra phòng hiện tại, phòng mong muốn và tình trạng thanh toán trước khi duyệt.', N'bi-arrow-left-right', 1, 0),
(N'Đơn xin trả phòng / chuyển ra trước hạn', N'Trả phòng', N'Dùng khi sinh viên muốn kết thúc lưu trú, bàn giao phòng và hoàn tất nghĩa vụ tài chính trước khi rời ký túc xá.', N'bi-box-arrow-right', 1, 0),
(N'Đơn đề nghị miễn, giảm lệ phí phòng ở', N'Miễn giảm phí', N'Dành cho sinh viên thuộc diện chính sách, hộ nghèo/cận nghèo, mồ côi, khuyết tật hoặc có hoàn cảnh khó khăn kèm giấy tờ minh chứng.', N'bi-cash-coin', 1, 0),
(N'Phiếu phản ánh sửa chữa', N'Sửa chữa', N'Gửi phản ánh khi phòng ở, thiết bị, điện, nước, cửa, giường, bàn ghế hoặc khu vực sinh hoạt chung có hư hỏng.', N'bi-tools', 1, 0),
(N'Tờ khai thay đổi thông tin cư trú', N'Thay đổi cư trú', N'Dùng khi sinh viên cần cập nhật thông tin cư trú, địa chỉ, số điện thoại, email, thông tin người thân hoặc nội dung liên hệ.', N'bi-person-lines-fill', 1, 0),
(N'Khai báo bảo hiểm y tế', N'BHYT', N'Sinh viên cập nhật thông tin thẻ bảo hiểm y tế, nơi đăng ký khám chữa bệnh ban đầu và thời hạn thẻ để phục vụ quản lý, hỗ trợ y tế nội trú.', N'bi-shield-plus', 1, 0),
(N'Đơn đăng ký cấp lại thẻ KTX', N'Cấp lại thẻ', N'Dùng khi sinh viên bị mất thẻ, hỏng thẻ hoặc cần cấp lại thẻ nội trú do thay đổi thông tin.', N'bi-credit-card-2-front', 1, 0),
(N'Đơn xin về trễ', N'Về trễ', N'Dành cho sinh viên cần xin phép về sau giờ quy định của ký túc xá và cam kết chịu trách nhiệm về việc đi lại.', N'bi-clock-history', 1, 0),
(N'Bản cam kết thực hiện nội quy phòng ở', N'Cam kết', N'Mẫu cam kết giữ gìn vệ sinh, bảo quản tài sản, chấp hành giờ giấc, an ninh, phòng cháy chữa cháy và nội quy ký túc xá.', N'bi-clipboard-check', 1, 0),
(N'Đơn đề nghị xác nhận lưu trú', N'Xác nhận lưu trú', N'Dùng khi sinh viên cần giấy xác nhận đang lưu trú tại ký túc xá để nộp cho khoa, trường hoặc đơn vị liên quan.', N'bi-file-earmark-check', 1, 0),
(N'Đơn kiến nghị / phản ánh', N'Kiến nghị', N'Dùng để gửi góp ý, phản ánh vấn đề sinh hoạt, vệ sinh, an ninh, dịch vụ hoặc đề xuất đến ban quản lý ký túc xá.', N'bi-chat-left-text', 1, 0),
(N'Biểu mẫu khác', N'Khác', N'Mẫu tạm dùng cho các yêu cầu phát sinh chưa thuộc nhóm biểu mẫu chính.', N'bi-journal-plus', 1, 1);

MERGE dbo.FormTemplates AS Target
USING @Templates AS Source
    ON Target.[Name] = Source.[Name]
WHEN MATCHED THEN
    UPDATE SET
        Target.[Type] = Source.[Type],
        Target.[Description] = Source.[Description],
        Target.[Icon] = Source.[Icon],
        Target.[IsActive] = Source.[IsActive],
        Target.[IsTemporary] = Source.[IsTemporary]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Name], [Type], [Description], [Icon], [TemplateFilePath], [OriginalFileName], [IsActive], [IsTemporary], [CreatedAt])
    VALUES (Source.[Name], Source.[Type], Source.[Description], Source.[Icon], NULL, NULL, Source.[IsActive], Source.[IsTemporary], @Now);

PRINT N'Đã cập nhật bộ biểu mẫu cốt lõi cho KTX, bao gồm Khai báo BHYT.';
SELECT [Name], [Type], [IsActive], [IsTemporary]
FROM dbo.FormTemplates
ORDER BY [Type], [Name];
