-- Chạy trong đúng database project nếu trước đó đã lỡ tạo hóa đơn nhận phòng sai logic.
-- Mục tiêu:
-- 1) Tiền phòng nhận phòng = đơn giá tháng x 12 tháng.
-- 2) Hóa đơn nhận phòng sau khi admin duyệt phải ở trạng thái Unpaid/Chờ thanh toán.
-- 3) Xóa bản ghi thanh toán mô phỏng đã tạo sai cho hóa đơn nhận phòng.

BEGIN TRANSACTION;

DELETE p
FROM dbo.Payments p
JOIN dbo.Invoices i ON i.Id = p.InvoiceId
WHERE i.Description LIKE N'Phí đăng ký nhận phòng%';

UPDATE i
SET
    i.TotalAmount = a.RoomPrice * 12,
    i.Status = 'Unpaid',
    i.DueDate = ISNULL(i.DueDate, DATEADD(DAY, 5, GETDATE()))
FROM dbo.Invoices i
JOIN dbo.ResidentProfiles r ON r.Id = i.ResidentProfileId
JOIN dbo.Apartments a ON a.Id = r.ApartmentId
WHERE i.Description LIKE N'Phí đăng ký nhận phòng%';

UPDATE r
SET r.Status = N'Chờ thanh toán'
FROM dbo.ResidentProfiles r
JOIN dbo.Invoices i ON i.ResidentProfileId = r.Id
WHERE i.Description LIKE N'Phí đăng ký nhận phòng%'
  AND i.Status = 'Unpaid';

COMMIT TRANSACTION;

SELECT i.Id, i.InvoiceCode, r.FullName, a.ApartmentCode, i.TotalAmount, i.Status, i.Description
FROM dbo.Invoices i
JOIN dbo.ResidentProfiles r ON r.Id = i.ResidentProfileId
LEFT JOIN dbo.Apartments a ON a.Id = r.ApartmentId
WHERE i.Description LIKE N'Phí đăng ký nhận phòng%'
ORDER BY i.CreatedAt DESC;
