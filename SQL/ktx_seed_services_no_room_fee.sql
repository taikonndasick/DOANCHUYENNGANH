/*
    Seed danh mục dịch vụ KTX - KHÔNG bao gồm tiền phòng.
    Tiền phòng được xử lý ở quy trình đăng ký cư trú / hóa đơn nhận phòng.

    Chạy trong đúng database project, ví dụ: QuanLyKTX_Rebuild_DB
*/

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Xóa các dịch vụ tiền phòng nếu trước đó đã lỡ seed vào danh mục dịch vụ.
DELETE FROM dbo.Services
WHERE Name LIKE N'%Tiền phòng%'
   OR Name LIKE N'%Phí phòng%';

-- Helper upsert theo tên dịch vụ.
IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Điện sinh hoạt')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Điện sinh hoạt', N'Đơn vị tính: kWh. Ban quản lý lập vào hóa đơn dịch vụ hằng tháng theo chỉ số sử dụng.', 3500, N'Monthly');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: kWh. Ban quản lý lập vào hóa đơn dịch vụ hằng tháng theo chỉ số sử dụng.', Price = 3500, Type = N'Monthly' WHERE Name = N'Điện sinh hoạt';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Nước sinh hoạt')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Nước sinh hoạt', N'Đơn vị tính: m3. Ban quản lý lập vào hóa đơn dịch vụ hằng tháng theo chỉ số sử dụng.', 12000, N'Monthly');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: m3. Ban quản lý lập vào hóa đơn dịch vụ hằng tháng theo chỉ số sử dụng.', Price = 12000, Type = N'Monthly' WHERE Name = N'Nước sinh hoạt';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Internet / WiFi')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Internet / WiFi', N'Đơn vị tính: sinh viên/tháng. Khoản phí dịch vụ mạng nội trú.', 50000, N'Monthly');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: sinh viên/tháng. Khoản phí dịch vụ mạng nội trú.', Price = 50000, Type = N'Monthly' WHERE Name = N'Internet / WiFi';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Vệ sinh khu vực chung')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Ve sinh khu vực chung', N'Đơn vị tính: sinh viên/tháng. Phục vụ vệ sinh hành lang, khu vực sinh hoạt chung.', 20000, N'Monthly');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: sinh viên/tháng. Phục vụ vệ sinh hành lang, khu vực sinh hoạt chung.', Price = 20000, Type = N'Monthly' WHERE Name = N'Ve sinh khu vực chung';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Bảo trì cơ sở vật chất')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Bảo trì cơ sở vật chất', N'Đơn vị tính: sinh viên/tháng. Hỗ trợ bảo trì thiết bị và hạ tầng dùng chung.', 30000, N'Monthly');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: sinh viên/tháng. Hỗ trợ bảo trì thiết bị và hạ tầng dùng chung.', Price = 30000, Type = N'Monthly' WHERE Name = N'Bảo trì cơ sở vật chất';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Gửi xe máy')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Gửi xe máy', N'Đơn vị tính: xe/tháng. Sinh viên cần đăng ký biển số xe trước hoặc ghi chú biển số khi gửi yêu cầu.', 80000, N'Optional');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: xe/tháng. Sinh viên cần đăng ký biển số xe trước hoặc ghi chú biển số khi gửi yêu cầu.', Price = 80000, Type = N'Optional' WHERE Name = N'Gửi xe máy';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Gửi xe đạp / xe đạp điện')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Gửi xe đạp / xe đạp điện', N'Đơn vị tính: xe/tháng. Dành cho xe đạp, xe đạp điện hoặc phương tiện cá nhân nhỏ.', 30000, N'Optional');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: xe/tháng. Dành cho xe đạp, xe đạp điện hoặc phương tiện cá nhân nhỏ.', Price = 30000, Type = N'Optional' WHERE Name = N'Gửi xe đạp / xe đạp điện';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Cấp lại thẻ KTX')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Cấp lại thẻ KTX', N'Đơn vị tính: lần. Áp dụng khi sinh viên mất hoặc hỏng thẻ nội trú.', 30000, N'Optional');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: lần. Áp dụng khi sinh viên mất hoặc hỏng thẻ nội trú.', Price = 30000, Type = N'Optional' WHERE Name = N'Cấp lại thẻ KTX';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Mượn phòng sinh hoạt chung')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Mượn phòng sinh hoạt chung', N'Đơn vị tính: lượt. Sinh viên gửi yêu cầu kèm thời gian và mục đích sử dụng.', 0, N'Request');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: lượt. Sinh viên gửi yêu cầu kèm thời gian và mục đích sử dụng.', Price = 0, Type = N'Request' WHERE Name = N'Mượn phòng sinh hoạt chung';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Sửa chữa thiết bị phòng ở')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Sửa chữa thiết bị phòng ở', N'Đơn vị tính: yêu cầu. Sinh viên mô tả thiết bị hư hỏng, admin tiếp nhận và xử lý.', 0, N'Request');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: yêu cầu. Sinh viên mô tả thiết bị hư hỏng, admin tiếp nhận và xử lý.', Price = 0, Type = N'Request' WHERE Name = N'Sửa chữa thiết bị phòng ở';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Phun khử khuẩn phòng')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Phun khử khuẩn phòng', N'Đơn vị tính: lượt. Sinh viên gửi yêu cầu khi cần vệ sinh/phun khử khuẩn phòng ở.', 50000, N'Optional');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: lượt. Sinh viên gửi yêu cầu khi cần vệ sinh/phun khử khuẩn phòng ở.', Price = 50000, Type = N'Optional' WHERE Name = N'Phun khử khuẩn phòng';

IF NOT EXISTS (SELECT 1 FROM dbo.Services WHERE Name = N'Giặt sấy tự phục vụ')
    INSERT INTO dbo.Services(Name, Description, Price, Type) VALUES
    (N'Giặt sấy tự phục vụ', N'Đơn vị tính: lượt. Dịch vụ hỗ trợ sinh viên sử dụng máy giặt/sấy khu nội trú.', 15000, N'Optional');
ELSE
    UPDATE dbo.Services SET Description = N'Đơn vị tính: lượt. Dịch vụ hỗ trợ sinh viên sử dụng máy giặt/sấy khu nội trú.', Price = 15000, Type = N'Optional' WHERE Name = N'Giặt sấy tự phục vụ';

COMMIT TRANSACTION;

SELECT Id, Name, Price, Type, Description
FROM dbo.Services
WHERE Name NOT LIKE N'%Tiền phòng%' AND Name NOT LIKE N'%Phí phòng%'
ORDER BY Type, Name;
