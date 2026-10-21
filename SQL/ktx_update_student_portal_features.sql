/*
Chạy script này trong SQL Server nếu chưa muốn dùng Migration.
Mục tiêu:
1) Fix lỗi Invalid column name BankAccountNumber / BankName ở StudentRegistrations.
2) Tạo bảng cho Biểu mẫu, Khai báo BHYT, Đăng ký khám chữa bệnh, Khảo sát.
*/

IF OBJECT_ID(N'[dbo].[StudentRegistrations]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.StudentRegistrations', 'BankAccountNumber') IS NULL
        ALTER TABLE [dbo].[StudentRegistrations] ADD [BankAccountNumber] NVARCHAR(100) NOT NULL CONSTRAINT DF_StudentRegistrations_BankAccountNumber DEFAULT N'';

    IF COL_LENGTH('dbo.StudentRegistrations', 'BankName') IS NULL
        ALTER TABLE [dbo].[StudentRegistrations] ADD [BankName] NVARCHAR(200) NOT NULL CONSTRAINT DF_StudentRegistrations_BankName DEFAULT N'';
END
GO

IF OBJECT_ID(N'[dbo].[StudentRequestForms]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StudentRequestForms](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ResidentProfileId] INT NOT NULL,
        [FormType] NVARCHAR(100) NOT NULL,
        [Title] NVARCHAR(250) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [AttachmentPath] NVARCHAR(500) NULL,
        [OriginalFileName] NVARCHAR(255) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ReviewedAt] DATETIME2 NULL,
        [AdminNote] NVARCHAR(MAX) NULL,
        [RejectReason] NVARCHAR(MAX) NULL,
        CONSTRAINT [FK_StudentRequestForms_ResidentProfiles_ResidentProfileId]
            FOREIGN KEY([ResidentProfileId]) REFERENCES [dbo].[ResidentProfiles]([Id]) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'[dbo].[HealthInsuranceDeclarations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[HealthInsuranceDeclarations](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ResidentProfileId] INT NOT NULL,
        [InsuranceCode] NVARCHAR(100) NOT NULL,
        [HospitalName] NVARCHAR(250) NULL,
        [ValidFrom] DATETIME2 NULL,
        [ValidTo] DATETIME2 NULL,
        [AttachmentPath] NVARCHAR(500) NULL,
        [OriginalFileName] NVARCHAR(255) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_HealthInsuranceDeclarations_ResidentProfiles_ResidentProfileId]
            FOREIGN KEY([ResidentProfileId]) REFERENCES [dbo].[ResidentProfiles]([Id]) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'[dbo].[MedicalRegistrations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MedicalRegistrations](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ResidentProfileId] INT NOT NULL,
        [RegistrationType] NVARCHAR(150) NOT NULL,
        [PreferredDate] DATETIME2 NULL,
        [Note] NVARCHAR(MAX) NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT N'Chờ duyệt',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_MedicalRegistrations_ResidentProfiles_ResidentProfileId]
            FOREIGN KEY([ResidentProfileId]) REFERENCES [dbo].[ResidentProfiles]([Id]) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'[dbo].[SurveyResponses]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SurveyResponses](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ResidentProfileId] INT NOT NULL,
        [SurveyTitle] NVARCHAR(250) NOT NULL,
        [SatisfactionLevel] INT NOT NULL,
        [Feedback] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [FK_SurveyResponses_ResidentProfiles_ResidentProfileId]
            FOREIGN KEY([ResidentProfileId]) REFERENCES [dbo].[ResidentProfiles]([Id]) ON DELETE CASCADE
    );
END
GO
