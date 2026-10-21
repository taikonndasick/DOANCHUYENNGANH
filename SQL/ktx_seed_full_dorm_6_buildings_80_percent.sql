/*
    Seed dữ liệu demo KTX HUTECH
    - 6 tòa: A1, A2, B1, B2, C1, C2
    - Mỗi tòa 12 tầng, mỗi tầng 16 phòng
    - Tên phòng: A1.01.02 = Tòa A1, tầng 01, phòng 02
    - Lấp đầy khoảng 80% tổng sức chứa: 4608 / 5760 sinh viên
    - Tỉ lệ sinh viên đang ở: 45% nam, 55% nữ
    - Tạo thẻ KTX và biển số xe mẫu cho từng sinh viên seed
    - Tạo bảng FormTemplates và dữ liệu mẫu cho mục Quản lý biểu mẫu

    Cách dùng:
    1. Mở SSMS hoặc SQL Server Object Explorer.
    2. Chọn đúng database project KTX.
    3. Chạy toàn bộ file này.
*/

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'Bắt đầu seed dữ liệu KTX...';

    IF OBJECT_ID(N'dbo.FormTemplates', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.FormTemplates
        (
            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FormTemplates PRIMARY KEY,
            Name NVARCHAR(200) NOT NULL,
            Type NVARCHAR(100) NOT NULL,
            Description NVARCHAR(MAX) NULL,
            Icon NVARCHAR(100) NOT NULL CONSTRAINT DF_FormTemplates_Icon DEFAULT N'bi-journal-text',
            TemplateFilePath NVARCHAR(500) NULL,
            OriginalFileName NVARCHAR(255) NULL,
            IsActive BIT NOT NULL CONSTRAINT DF_FormTemplates_IsActive DEFAULT 1,
            IsTemporary BIT NOT NULL CONSTRAINT DF_FormTemplates_IsTemporary DEFAULT 0,
            CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_FormTemplates_CreatedAt DEFAULT SYSDATETIME()
        );
    END;

    -- Xóa dữ liệu seed cũ nếu đã chạy script trước đó
    DECLARE @SeedResidentIds TABLE (Id INT PRIMARY KEY);
    INSERT INTO @SeedResidentIds(Id)
    SELECT Id FROM dbo.ResidentProfiles WHERE StudentCode LIKE N'KTX26%';

    DELETE FROM dbo.Vehicles WHERE ResidentProfileId IN (SELECT Id FROM @SeedResidentIds);
    DELETE FROM dbo.ResidentCards WHERE ResidentProfileId IN (SELECT Id FROM @SeedResidentIds);

    IF OBJECT_ID(N'dbo.StudentRequestForms', N'U') IS NOT NULL
        DELETE FROM dbo.StudentRequestForms WHERE ResidentProfileId IN (SELECT Id FROM @SeedResidentIds);

    IF OBJECT_ID(N'dbo.HealthInsuranceDeclarations', N'U') IS NOT NULL
        DELETE FROM dbo.HealthInsuranceDeclarations WHERE ResidentProfileId IN (SELECT Id FROM @SeedResidentIds);

    IF OBJECT_ID(N'dbo.MedicalRegistrations', N'U') IS NOT NULL
        DELETE FROM dbo.MedicalRegistrations WHERE ResidentProfileId IN (SELECT Id FROM @SeedResidentIds);

    IF OBJECT_ID(N'dbo.SurveyResponses', N'U') IS NOT NULL
        DELETE FROM dbo.SurveyResponses WHERE ResidentProfileId IN (SELECT Id FROM @SeedResidentIds);

    DELETE FROM dbo.Invoices WHERE ResidentProfileId IN (SELECT Id FROM @SeedResidentIds);
    DELETE FROM dbo.ResidentProfiles WHERE Id IN (SELECT Id FROM @SeedResidentIds);

    -- Upsert tòa nhà
    DECLARE @Buildings TABLE
    (
        Code NVARCHAR(20),
        Name NVARCHAR(100),
        GenderGroup NVARCHAR(10),
        Description NVARCHAR(500)
    );

    INSERT INTO @Buildings(Code, Name, GenderGroup, Description)
    VALUES
    (N'A1', N'Tòa A1 - Khu nam', N'Nam', N'Tòa nam 12 tầng, tầng 1-6 phòng 8 người, tầng 7-12 phòng 6 người.'),
    (N'A2', N'Tòa A2 - Khu nam', N'Nam', N'Tòa nam 12 tầng, tầng 1-6 phòng 4 người, tầng 7-12 phòng 2 người.'),
    (N'B1', N'Tòa B1 - Khu nữ', N'Nữ', N'Tòa nữ 12 tầng, tầng 1-6 phòng 8 người, tầng 7-12 phòng 6 người.'),
    (N'B2', N'Tòa B2 - Khu nữ', N'Nữ', N'Tòa nữ 12 tầng, tầng 1-6 phòng 4 người, tầng 7-12 phòng 2 người.'),
    (N'C1', N'Tòa C1 - Khu nam', N'Nam', N'Tòa nam 12 tầng, tầng 1-6 phòng 6 người, tầng 7-12 phòng 4 người.'),
    (N'C2', N'Tòa C2 - Khu nữ', N'Nữ', N'Tòa nữ 12 tầng, tầng 1-6 phòng 6 người, tầng 7-12 phòng 4 người.');

    MERGE dbo.Buildings AS target
    USING @Buildings AS source
    ON target.Code = source.Code
    WHEN MATCHED THEN
        UPDATE SET
            Name = source.Name,
            Address = N'Khu ký túc xá HUTECH',
            TotalFloors = 12,
            Description = source.Description,
            Status = N'Đang hoạt động'
    WHEN NOT MATCHED THEN
        INSERT (Code, Name, Address, TotalFloors, Description, Status)
        VALUES (source.Code, source.Name, N'Khu ký túc xá HUTECH', 12, source.Description, N'Đang hoạt động');

    -- Upsert phòng
    DECLARE @BuildingCode NVARCHAR(20), @BuildingId INT, @Floor INT, @RoomNo INT;
    DECLARE @MaxOcc INT, @RoomType NVARCHAR(100), @RoomPrice DECIMAL(18,2), @RoomCode NVARCHAR(50), @Note NVARCHAR(300);

    DECLARE building_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT Code FROM @Buildings ORDER BY Code;

    OPEN building_cursor;
    FETCH NEXT FROM building_cursor INTO @BuildingCode;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @BuildingId = Id FROM dbo.Buildings WHERE Code = @BuildingCode;
        SET @Floor = 1;

        WHILE @Floor <= 12
        BEGIN
            SET @RoomNo = 1;
            WHILE @RoomNo <= 16
            BEGIN
                IF @BuildingCode IN (N'A1', N'B1')
                BEGIN
                    IF @Floor BETWEEN 1 AND 6
                    BEGIN
                        SET @MaxOcc = 8; SET @RoomType = N'Phòng 8 người'; SET @RoomPrice = 800000;
                    END
                    ELSE
                    BEGIN
                        SET @MaxOcc = 6; SET @RoomType = N'Phòng 6 người'; SET @RoomPrice = 1000000;
                    END
                END
                ELSE IF @BuildingCode IN (N'A2', N'B2')
                BEGIN
                    IF @Floor BETWEEN 1 AND 6
                    BEGIN
                        SET @MaxOcc = 4; SET @RoomType = N'Phòng 4 người'; SET @RoomPrice = 1200000;
                    END
                    ELSE
                    BEGIN
                        SET @MaxOcc = 2; SET @RoomType = N'Phòng 2 người'; SET @RoomPrice = 1900000;
                    END
                END
                ELSE
                BEGIN
                    IF @Floor BETWEEN 1 AND 6
                    BEGIN
                        SET @MaxOcc = 6; SET @RoomType = N'Phòng 6 người'; SET @RoomPrice = 1000000;
                    END
                    ELSE
                    BEGIN
                        SET @MaxOcc = 4; SET @RoomType = N'Phòng 4 người'; SET @RoomPrice = 1200000;
                    END
                END

                SET @RoomCode = CONCAT(@BuildingCode, N'.', RIGHT(N'00' + CAST(@Floor AS NVARCHAR(2)), 2), N'.', RIGHT(N'00' + CAST(@RoomNo AS NVARCHAR(2)), 2));
                SET @Note = CONCAT(N'Dữ liệu seed: ', @RoomType, N' - ', @MaxOcc, N' sinh viên tối đa.');

                IF EXISTS (SELECT 1 FROM dbo.Apartments WHERE ApartmentCode = @RoomCode)
                BEGIN
                    UPDATE dbo.Apartments
                    SET BuildingId = @BuildingId,
                        ApartmentType = @RoomType,
                        Floor = @Floor,
                        MaxOccupancy = @MaxOcc,
                        RoomPrice = @RoomPrice,
                        Note = @Note,
                        Status = CASE WHEN Status IN (N'Bảo trì', N'Ngưng sử dụng') THEN Status ELSE N'Còn chỗ' END
                    WHERE ApartmentCode = @RoomCode;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.Apartments(BuildingId, ApartmentCode, ApartmentType, Floor, CurrentOccupancy, MaxOccupancy, RoomPrice, Status, Note)
                    VALUES(@BuildingId, @RoomCode, @RoomType, @Floor, 0, @MaxOcc, @RoomPrice, N'Còn chỗ', @Note);
                END

                SET @RoomNo += 1;
            END
            SET @Floor += 1;
        END

        FETCH NEXT FROM building_cursor INTO @BuildingCode;
    END

    CLOSE building_cursor;
    DEALLOCATE building_cursor;

    -- Chuẩn bị danh sách tên/khoa
    DECLARE @LastNames TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(30));
    INSERT INTO @LastNames(Name) VALUES (N'Nguyễn'),(N'Trần'),(N'Lê'),(N'Phạm'),(N'Hoàng'),(N'Võ'),(N'Đặng'),(N'Bùi'),(N'Đỗ'),(N'Huỳnh'),(N'Phan'),(N'Vũ');

    DECLARE @MaleFirst TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(30));
    INSERT INTO @MaleFirst(Name) VALUES (N'An'),(N'Bảo'),(N'Cường'),(N'Duy'),(N'Đạt'),(N'Hải'),(N'Khang'),(N'Lâm'),(N'Minh'),(N'Nam'),(N'Phong'),(N'Quân'),(N'Sơn'),(N'Tài'),(N'Thành'),(N'Việt');

    DECLARE @FemaleFirst TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(30));
    INSERT INTO @FemaleFirst(Name) VALUES (N'An'),(N'Anh'),(N'Chi'),(N'Dung'),(N'Giang'),(N'Hà'),(N'Hạnh'),(N'Hoa'),(N'Linh'),(N'Mai'),(N'Ngọc'),(N'Nhi'),(N'Phương'),(N'Thảo'),(N'Trang'),(N'Vy');

    DECLARE @Faculties TABLE (Id INT IDENTITY(1,1), Name NVARCHAR(100));
    INSERT INTO @Faculties(Name) VALUES
    (N'Công nghệ thông tin'),(N'Quản trị kinh doanh'),(N'Kế toán - Tài chính'),(N'Ngôn ngữ Anh'),(N'Công nghệ kỹ thuật ô tô'),
    (N'Thiết kế đồ họa'),(N'Du lịch - Nhà hàng - Khách sạn'),(N'Luật'),(N'Marketing'),(N'Điện - Điện tử');

    -- Tính mục tiêu 80% và 45/55
    DECLARE @TotalCapacity INT = (SELECT SUM(MaxOccupancy) FROM dbo.Apartments WHERE ApartmentCode LIKE N'[ABC][12].___.__' ESCAPE '\');
    -- Pattern trên một số collation có thể không như mong muốn, dùng theo tòa cho chắc
    SELECT @TotalCapacity = SUM(a.MaxOccupancy)
    FROM dbo.Apartments a
    JOIN dbo.Buildings b ON b.Id = a.BuildingId
    WHERE b.Code IN (N'A1',N'A2',N'B1',N'B2',N'C1',N'C2');

    DECLARE @TargetTotal INT = CAST(ROUND(@TotalCapacity * 0.80, 0) AS INT);
    DECLARE @TargetMale INT = CAST(ROUND(@TargetTotal * 0.45, 0) AS INT);
    DECLARE @TargetFemale INT = @TargetTotal - @TargetMale;

    DECLARE @MaleCapacity INT, @FemaleCapacity INT;
    SELECT @MaleCapacity = SUM(a.MaxOccupancy)
    FROM dbo.Apartments a JOIN dbo.Buildings b ON b.Id = a.BuildingId
    WHERE b.Code IN (N'A1',N'A2',N'C1');

    SELECT @FemaleCapacity = SUM(a.MaxOccupancy)
    FROM dbo.Apartments a JOIN dbo.Buildings b ON b.Id = a.BuildingId
    WHERE b.Code IN (N'B1',N'B2',N'C2');

    -- Bảng phòng và số sinh viên cần seed mỗi phòng
    IF OBJECT_ID('tempdb..#RoomPlan') IS NOT NULL DROP TABLE #RoomPlan;
    CREATE TABLE #RoomPlan
    (
        RowId INT IDENTITY(1,1) PRIMARY KEY,
        ApartmentId INT,
        ApartmentCode NVARCHAR(50),
        BuildingCode NVARCHAR(20),
        Gender NVARCHAR(10),
        MaxOccupancy INT,
        PlannedOccupancy INT
    );

    INSERT INTO #RoomPlan(ApartmentId, ApartmentCode, BuildingCode, Gender, MaxOccupancy, PlannedOccupancy)
    SELECT a.Id, a.ApartmentCode, b.Code,
           CASE WHEN b.Code IN (N'A1',N'A2',N'C1') THEN N'Nam' ELSE N'Nữ' END,
           a.MaxOccupancy,
           0
    FROM dbo.Apartments a
    JOIN dbo.Buildings b ON b.Id = a.BuildingId
    WHERE b.Code IN (N'A1',N'A2',N'B1',N'B2',N'C1',N'C2')
    ORDER BY b.Code, a.Floor, a.ApartmentCode;

    UPDATE #RoomPlan
    SET PlannedOccupancy = FLOOR(MaxOccupancy * (CAST(@TargetMale AS FLOAT) / @MaleCapacity))
    WHERE Gender = N'Nam';

    UPDATE #RoomPlan
    SET PlannedOccupancy = FLOOR(MaxOccupancy * (CAST(@TargetFemale AS FLOAT) / @FemaleCapacity))
    WHERE Gender = N'Nữ';

    DECLARE @MaleExtra INT = @TargetMale - (SELECT SUM(PlannedOccupancy) FROM #RoomPlan WHERE Gender = N'Nam');
    DECLARE @FemaleExtra INT = @TargetFemale - (SELECT SUM(PlannedOccupancy) FROM #RoomPlan WHERE Gender = N'Nữ');

    UPDATE rp
    SET PlannedOccupancy = PlannedOccupancy + 1
    FROM #RoomPlan rp
    WHERE Gender = N'Nam'
      AND PlannedOccupancy < MaxOccupancy
      AND RowId IN (SELECT TOP (@MaleExtra) RowId FROM #RoomPlan WHERE Gender = N'Nam' AND PlannedOccupancy < MaxOccupancy ORDER BY RowId);

    UPDATE rp
    SET PlannedOccupancy = PlannedOccupancy + 1
    FROM #RoomPlan rp
    WHERE Gender = N'Nữ'
      AND PlannedOccupancy < MaxOccupancy
      AND RowId IN (SELECT TOP (@FemaleExtra) RowId FROM #RoomPlan WHERE Gender = N'Nữ' AND PlannedOccupancy < MaxOccupancy ORDER BY RowId);

    -- Seed sinh viên, thẻ KTX, biển số
    DECLARE @RowId INT = 1, @MaxRowId INT = (SELECT MAX(RowId) FROM #RoomPlan);
    DECLARE @ApartmentId INT, @PlanCount INT, @Gender NVARCHAR(10), @i INT;
    DECLARE @StudentNo INT = 0, @ResidentId INT;
    DECLARE @FullName NVARCHAR(100), @StudentCode NVARCHAR(20), @Phone NVARCHAR(20), @Email NVARCHAR(120), @Faculty NVARCHAR(100), @ClassName NVARCHAR(50), @NationalId NVARCHAR(20), @Plate NVARCHAR(20);
    DECLARE @LastName NVARCHAR(30), @FirstName NVARCHAR(30), @MiddleName NVARCHAR(30);

    WHILE @RowId <= @MaxRowId
    BEGIN
        SELECT @ApartmentId = ApartmentId, @PlanCount = PlannedOccupancy, @Gender = Gender
        FROM #RoomPlan WHERE RowId = @RowId;

        SET @i = 1;
        WHILE @i <= @PlanCount
        BEGIN
            SET @StudentNo += 1;
            SET @StudentCode = CONCAT(N'KTX26', RIGHT(N'00000' + CAST(@StudentNo AS NVARCHAR(10)), 5));

            SELECT @LastName = Name FROM @LastNames WHERE Id = ((@StudentNo - 1) % (SELECT COUNT(*) FROM @LastNames)) + 1;
            IF @Gender = N'Nam'
            BEGIN
                SET @MiddleName = CASE WHEN @StudentNo % 3 = 0 THEN N'Minh' WHEN @StudentNo % 3 = 1 THEN N'Văn' ELSE N'Hoàng' END;
                SELECT @FirstName = Name FROM @MaleFirst WHERE Id = ((@StudentNo - 1) % (SELECT COUNT(*) FROM @MaleFirst)) + 1;
            END
            ELSE
            BEGIN
                SET @MiddleName = CASE WHEN @StudentNo % 3 = 0 THEN N'Minh' WHEN @StudentNo % 3 = 1 THEN N'Thị' ELSE N'Ngọc' END;
                SELECT @FirstName = Name FROM @FemaleFirst WHERE Id = ((@StudentNo - 1) % (SELECT COUNT(*) FROM @FemaleFirst)) + 1;
            END

            SET @FullName = CONCAT(@LastName, N' ', @MiddleName, N' ', @FirstName);
            SELECT @Faculty = Name FROM @Faculties WHERE Id = ((@StudentNo - 1) % (SELECT COUNT(*) FROM @Faculties)) + 1;
            SET @ClassName = CONCAT(N'22D', RIGHT(N'00' + CAST(((@StudentNo - 1) % 12) + 1 AS NVARCHAR(2)), 2), N'KTX');
            SET @Phone = CONCAT(N'09', RIGHT(N'00000000' + CAST(10000000 + @StudentNo AS NVARCHAR(20)), 8));
            SET @Email = CONCAT(LOWER(@StudentCode), N'@hutech.edu.vn');
            SET @NationalId = CONCAT(N'079', RIGHT(N'000000000' + CAST(260000000 + @StudentNo AS NVARCHAR(20)), 9));

            INSERT INTO dbo.ResidentProfiles
            (
                UserId, FullName, StudentCode, DateOfBirth, Gender, NationalId, PhoneNumber, Email, Address,
                ClassName, Faculty, ApartmentId, Status, CreatedAt
            )
            VALUES
            (
                NULL, @FullName, @StudentCode,
                DATEADD(DAY, -((@StudentNo * 17) % 1500), DATEFROMPARTS(2006, 9, 1)),
                @Gender, @NationalId, @Phone, @Email, N'TP. Hồ Chí Minh',
                @ClassName, @Faculty, @ApartmentId, N'Đang ở', DATEADD(DAY, -(@StudentNo % 120), SYSDATETIME())
            );

            SET @ResidentId = SCOPE_IDENTITY();

            INSERT INTO dbo.ResidentCards(ResidentProfileId, CardCode, IssueDate, ExpiryDate, Status)
            VALUES(@ResidentId, CONCAT(N'HUTECH', @StudentCode), DATEFROMPARTS(2026, 6, 1), DATEFROMPARTS(2027, 6, 1), N'Active');

            SET @Plate = CONCAT(RIGHT(N'00' + CAST(11 + (@StudentNo % 89) AS NVARCHAR(2)), 2), N'B1-', RIGHT(N'00000' + CAST((@StudentNo * 73) % 100000 AS NVARCHAR(5)), 5));
            INSERT INTO dbo.Vehicles(ResidentProfileId, LicensePlate, VehicleType, Status)
            VALUES(@ResidentId, @Plate, N'Xe máy', N'Active');

            SET @i += 1;
        END

        SET @RowId += 1;
    END

    -- Cập nhật lại số lượng hiện tại của phòng
    UPDATE a
    SET CurrentOccupancy = ISNULL(x.TotalResident, 0),
        Status = CASE
            WHEN a.Status IN (N'Bảo trì', N'Ngưng sử dụng') THEN a.Status
            WHEN ISNULL(x.TotalResident, 0) >= a.MaxOccupancy THEN N'Đã đầy'
            ELSE N'Còn chỗ'
        END
    FROM dbo.Apartments a
    OUTER APPLY
    (
        SELECT COUNT(*) AS TotalResident
        FROM dbo.ResidentProfiles r
        WHERE r.ApartmentId = a.Id AND r.Status = N'Đang ở'
    ) x;

    -- Seed danh mục biểu mẫu
    IF NOT EXISTS (SELECT 1 FROM dbo.FormTemplates WHERE Type = N'Miễn giảm phí')
    BEGIN
        INSERT INTO dbo.FormTemplates(Name, Type, Description, Icon, IsActive, IsTemporary, CreatedAt)
        VALUES
        (N'Đơn xin miễn giảm phí', N'Miễn giảm phí', N'Dành cho sinh viên thuộc diện chính sách, hộ nghèo/cận nghèo hoặc trường hợp đặc biệt cần gửi minh chứng.', N'bi-cash-coin', 1, 0, SYSDATETIME()),
        (N'Phiếu phản ánh sửa chữa', N'Sửa chữa', N'Gửi phản ánh khi phòng ở, thiết bị hoặc khu vực sinh hoạt có hư hỏng.', N'bi-tools', 1, 0, SYSDATETIME()),
        (N'Đơn đề nghị xác nhận lưu trú', N'Xác nhận lưu trú', N'Dùng khi sinh viên cần giấy xác nhận đang lưu trú tại ký túc xá.', N'bi-file-earmark-check', 1, 0, SYSDATETIME()),
        (N'Đơn kiến nghị / phản ánh', N'Kiến nghị', N'Gửi góp ý, phản ánh hoặc đề xuất đến ban quản lý ký túc xá.', N'bi-chat-left-text', 1, 0, SYSDATETIME()),
        (N'Đơn xin trả phòng', N'Trả phòng', N'Sinh viên gửi yêu cầu trả phòng, ghi rõ ngày trả dự kiến và tình trạng phòng.', N'bi-box-arrow-right', 1, 0, SYSDATETIME()),
        (N'Khai báo bảo hiểm y tế', N'BHYT', N'Sinh viên cập nhật thông tin thẻ bảo hiểm y tế để phục vụ quản lý hồ sơ nội trú.', N'bi-shield-check', 1, 0, SYSDATETIME()),
        (N'Biểu mẫu tạm thời', N'Khác', N'Mẫu tạm dùng cho các yêu cầu phát sinh ngoài danh mục chính.', N'bi-journal-plus', 1, 1, SYSDATETIME());
    END

    DECLARE @TotalRooms INT = (SELECT COUNT(*) FROM dbo.Apartments a JOIN dbo.Buildings b ON b.Id = a.BuildingId WHERE b.Code IN (N'A1',N'A2',N'B1',N'B2',N'C1',N'C2'));
    DECLARE @SeedStudents INT = (SELECT COUNT(*) FROM dbo.ResidentProfiles WHERE StudentCode LIKE N'KTX26%');
    PRINT CONCAT(N'Hoàn tất seed. Số phòng KTX: ', @TotalRooms, N'. Số sinh viên seed: ', @SeedStudents, N'.');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(MAX) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
