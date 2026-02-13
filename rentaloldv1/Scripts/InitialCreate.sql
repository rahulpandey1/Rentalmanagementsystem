IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Properties] (
    [Id] int NOT NULL IDENTITY,
    [Address] nvarchar(200) NOT NULL,
    [PropertyType] nvarchar(100) NOT NULL,
    [PropertyName] nvarchar(100) NOT NULL,
    [TotalRooms] int NOT NULL,
    [NumberOfFloors] int NOT NULL,
    [TotalArea] decimal(18,2) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [Description] nvarchar(1000) NULL,
    [HasSharedKitchen] bit NOT NULL,
    [HasSharedBathrooms] bit NOT NULL,
    [HasParking] bit NOT NULL,
    [HasLaundryFacility] bit NOT NULL,
    [HasWiFi] bit NOT NULL,
    CONSTRAINT [PK_Properties] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SystemConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [ConfigKey] nvarchar(100) NOT NULL,
    [ConfigValue] nvarchar(500) NOT NULL,
    [Description] nvarchar(200) NULL,
    [Category] nvarchar(50) NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_SystemConfigurations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Tenants] (
    [Id] int NOT NULL IDENTITY,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [Address] nvarchar(300) NULL,
    [DateOfBirth] datetime2 NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Rooms] (
    [Id] int NOT NULL IDENTITY,
    [PropertyId] int NOT NULL,
    [RoomNumber] nvarchar(50) NOT NULL,
    [Floor] nvarchar(50) NOT NULL,
    [FloorNumber] int NOT NULL,
    [Area] decimal(18,2) NOT NULL,
    [MonthlyRent] decimal(18,2) NOT NULL,
    [IsAvailable] bit NOT NULL,
    [Description] nvarchar(500) NULL,
    [RoomType] nvarchar(100) NULL,
    [HasPrivateBathroom] bit NOT NULL,
    [HasAC] bit NOT NULL,
    [IsFurnished] bit NOT NULL,
    [ElectricMeterNumber] nvarchar(100) NULL,
    [LastMeterReading] int NULL,
    [LastReadingDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Rooms_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ElectricMeterReadings] (
    [Id] int NOT NULL IDENTITY,
    [RoomId] int NOT NULL,
    [PreviousReading] int NOT NULL,
    [CurrentReading] int NOT NULL,
    [ReadingDate] datetime2 NOT NULL,
    [PreviousReadingDate] datetime2 NOT NULL,
    [ElectricCharges] decimal(18,2) NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [IsBilled] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_ElectricMeterReadings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ElectricMeterReadings_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [RentAgreements] (
    [Id] int NOT NULL IDENTITY,
    [PropertyId] int NOT NULL,
    [TenantId] int NOT NULL,
    [RoomId] int NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [MonthlyRent] decimal(18,2) NOT NULL,
    [SecurityDeposit] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [Terms] nvarchar(1000) NULL,
    [AgreementType] nvarchar(100) NULL,
    CONSTRAINT [PK_RentAgreements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RentAgreements_Properties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [Properties] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RentAgreements_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RentAgreements_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Bills] (
    [Id] int NOT NULL IDENTITY,
    [RentAgreementId] int NOT NULL,
    [TenantId] int NOT NULL,
    [RoomId] int NULL,
    [ElectricMeterReadingId] int NULL,
    [BillNumber] nvarchar(50) NOT NULL,
    [BillDate] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [BillPeriod] nvarchar(50) NOT NULL,
    [RentAmount] decimal(18,2) NOT NULL,
    [ElectricAmount] decimal(18,2) NOT NULL,
    [MiscAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [Remarks] nvarchar(1000) NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Bills] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bills_ElectricMeterReadings_ElectricMeterReadingId] FOREIGN KEY ([ElectricMeterReadingId]) REFERENCES [ElectricMeterReadings] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Bills_RentAgreements_RentAgreementId] FOREIGN KEY ([RentAgreementId]) REFERENCES [RentAgreements] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bills_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Bills_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BillItems] (
    [Id] int NOT NULL IDENTITY,
    [BillId] int NOT NULL,
    [ItemType] nvarchar(100) NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Quantity] int NULL,
    [UnitPrice] decimal(18,2) NULL,
    [Remarks] nvarchar(500) NULL,
    CONSTRAINT [PK_BillItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BillItems_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [RentAgreementId] int NOT NULL,
    [TenantId] int NOT NULL,
    [BillId] int NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [PaymentMethod] nvarchar(50) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [TransactionReference] nvarchar(100) NULL,
    [Notes] nvarchar(500) NULL,
    [PaymentType] nvarchar(100) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payments_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Payments_RentAgreements_RentAgreementId] FOREIGN KEY ([RentAgreementId]) REFERENCES [RentAgreements] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Payments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'CreatedDate', N'Description', N'HasLaundryFacility', N'HasParking', N'HasSharedBathrooms', N'HasSharedKitchen', N'HasWiFi', N'NumberOfFloors', N'PropertyName', N'PropertyType', N'TotalArea', N'TotalRooms') AND [object_id] = OBJECT_ID(N'[Properties]'))
    SET IDENTITY_INSERT [Properties] ON;
INSERT INTO [Properties] ([Id], [Address], [CreatedDate], [Description], [HasLaundryFacility], [HasParking], [HasSharedBathrooms], [HasSharedKitchen], [HasWiFi], [NumberOfFloors], [PropertyName], [PropertyType], [TotalArea], [TotalRooms])
VALUES (1, N'Your Property Address', '2025-07-12T12:26:06.3864387+05:30', N'22-room boarding house with 3 floors - Ground floor (10 rooms), First floor (6 rooms), Second floor (6 rooms)', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), 3, N'Your Boarding House Name', N'Boarding House', 1000.0, 22);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'CreatedDate', N'Description', N'HasLaundryFacility', N'HasParking', N'HasSharedBathrooms', N'HasSharedKitchen', N'HasWiFi', N'NumberOfFloors', N'PropertyName', N'PropertyType', N'TotalArea', N'TotalRooms') AND [object_id] = OBJECT_ID(N'[Properties]'))
    SET IDENTITY_INSERT [Properties] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'ConfigKey', N'ConfigValue', N'Description', N'LastUpdated') AND [object_id] = OBJECT_ID(N'[SystemConfigurations]'))
    SET IDENTITY_INSERT [SystemConfigurations] ON;
INSERT INTO [SystemConfigurations] ([Id], [Category], [ConfigKey], [ConfigValue], [Description], [LastUpdated])
VALUES (1, N'Electric', N'ElectricUnitCost', N'12.00', N'Cost per electric unit in currency', '2025-08-11T12:26:06.3864578+05:30'),
(2, N'General', N'PropertyName', N'Your Boarding House', N'Name of the property', '2025-08-11T12:26:06.3864581+05:30'),
(3, N'Billing', N'BillDueDays', N'15', N'Number of days for bill due date', '2025-08-11T12:26:06.3864585+05:30');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'ConfigKey', N'ConfigValue', N'Description', N'LastUpdated') AND [object_id] = OBJECT_ID(N'[SystemConfigurations]'))
    SET IDENTITY_INSERT [SystemConfigurations] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Area', N'CreatedDate', N'Description', N'ElectricMeterNumber', N'Floor', N'FloorNumber', N'HasAC', N'HasPrivateBathroom', N'IsAvailable', N'IsFurnished', N'LastMeterReading', N'LastReadingDate', N'MonthlyRent', N'PropertyId', N'RoomNumber', N'RoomType') AND [object_id] = OBJECT_ID(N'[Rooms]'))
    SET IDENTITY_INSERT [Rooms] ON;
INSERT INTO [Rooms] ([Id], [Area], [CreatedDate], [Description], [ElectricMeterNumber], [Floor], [FloorNumber], [HasAC], [HasPrivateBathroom], [IsAvailable], [IsFurnished], [LastMeterReading], [LastReadingDate], [MonthlyRent], [PropertyId], [RoomNumber], [RoomType])
VALUES (1, 120.0, '2025-07-12T12:26:06.3864656+05:30', NULL, N'GM001', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1050, '2025-07-12T12:26:06.3864653+05:30', 500.0, 1, N'G/1', N'Single'),
(2, 120.0, '2025-07-12T12:26:06.3864661+05:30', NULL, N'GM002', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1100, '2025-07-12T12:26:06.3864661+05:30', 500.0, 1, N'G/2', N'Single'),
(3, 120.0, '2025-07-12T12:26:06.3864665+05:30', NULL, N'GM003', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1150, '2025-07-12T12:26:06.3864665+05:30', 500.0, 1, N'G/3', N'Single'),
(4, 120.0, '2025-07-12T12:26:06.3864670+05:30', NULL, N'GM004', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1200, '2025-07-12T12:26:06.3864669+05:30', 500.0, 1, N'G/4', N'Single'),
(5, 120.0, '2025-07-12T12:26:06.3864674+05:30', NULL, N'GM005', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1250, '2025-07-12T12:26:06.3864673+05:30', 500.0, 1, N'G/5', N'Single'),
(6, 120.0, '2025-07-12T12:26:06.3864679+05:30', NULL, N'GM006', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1300, '2025-07-12T12:26:06.3864678+05:30', 500.0, 1, N'G/6', N'Single'),
(7, 120.0, '2025-07-12T12:26:06.3864682+05:30', NULL, N'GM007', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1350, '2025-07-12T12:26:06.3864682+05:30', 500.0, 1, N'G/7', N'Single'),
(8, 120.0, '2025-07-12T12:26:06.3864719+05:30', NULL, N'GM008', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1400, '2025-07-12T12:26:06.3864718+05:30', 500.0, 1, N'G/8', N'Single'),
(9, 120.0, '2025-07-12T12:26:06.3864722+05:30', NULL, N'GM009', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1450, '2025-07-12T12:26:06.3864722+05:30', 500.0, 1, N'G/9', N'Single'),
(10, 120.0, '2025-07-12T12:26:06.3864729+05:30', NULL, N'GM010', N'Ground', 0, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1500, '2025-07-12T12:26:06.3864729+05:30', 500.0, 1, N'G/10', N'Single'),
(11, 130.0, '2025-07-12T12:26:06.3864738+05:30', NULL, N'F1M001', N'First', 1, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1260, '2025-07-12T12:26:06.3864737+05:30', 550.0, 1, N'1/1', N'Single'),
(12, 130.0, '2025-07-12T12:26:06.3864742+05:30', NULL, N'F1M002', N'First', 1, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1320, '2025-07-12T12:26:06.3864742+05:30', 550.0, 1, N'1/2', N'Single'),
(13, 130.0, '2025-07-12T12:26:06.3864745+05:30', NULL, N'F1M003', N'First', 1, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1380, '2025-07-12T12:26:06.3864745+05:30', 550.0, 1, N'1/3', N'Single'),
(14, 130.0, '2025-07-12T12:26:06.3864749+05:30', NULL, N'F1M004', N'First', 1, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1440, '2025-07-12T12:26:06.3864748+05:30', 550.0, 1, N'1/4', N'Single'),
(15, 130.0, '2025-07-12T12:26:06.3864752+05:30', NULL, N'F1M005', N'First', 1, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1500, '2025-07-12T12:26:06.3864751+05:30', 550.0, 1, N'1/5', N'Single'),
(16, 130.0, '2025-07-12T12:26:06.3864755+05:30', NULL, N'F1M006', N'First', 1, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1560, '2025-07-12T12:26:06.3864754+05:30', 550.0, 1, N'1/6', N'Single'),
(17, 130.0, '2025-07-12T12:26:06.3864761+05:30', NULL, N'F2M001', N'Second', 2, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1470, '2025-07-12T12:26:06.3864760+05:30', 550.0, 1, N'2/1', N'Single'),
(18, 130.0, '2025-07-12T12:26:06.3864765+05:30', NULL, N'F2M002', N'Second', 2, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1540, '2025-07-12T12:26:06.3864765+05:30', 550.0, 1, N'2/2', N'Single'),
(19, 130.0, '2025-07-12T12:26:06.3864769+05:30', NULL, N'F2M003', N'Second', 2, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1610, '2025-07-12T12:26:06.3864768+05:30', 550.0, 1, N'2/3', N'Single'),
(20, 130.0, '2025-07-12T12:26:06.3864771+05:30', NULL, N'F2M004', N'Second', 2, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1680, '2025-07-12T12:26:06.3864771+05:30', 550.0, 1, N'2/4', N'Single'),
(21, 130.0, '2025-07-12T12:26:06.3864774+05:30', NULL, N'F2M005', N'Second', 2, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1750, '2025-07-12T12:26:06.3864774+05:30', 550.0, 1, N'2/5', N'Single'),
(22, 130.0, '2025-07-12T12:26:06.3864777+05:30', NULL, N'F2M006', N'Second', 2, CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 1820, '2025-07-12T12:26:06.3864777+05:30', 550.0, 1, N'2/6', N'Single');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Area', N'CreatedDate', N'Description', N'ElectricMeterNumber', N'Floor', N'FloorNumber', N'HasAC', N'HasPrivateBathroom', N'IsAvailable', N'IsFurnished', N'LastMeterReading', N'LastReadingDate', N'MonthlyRent', N'PropertyId', N'RoomNumber', N'RoomType') AND [object_id] = OBJECT_ID(N'[Rooms]'))
    SET IDENTITY_INSERT [Rooms] OFF;
GO

CREATE INDEX [IX_BillItems_BillId] ON [BillItems] ([BillId]);
GO

CREATE INDEX [IX_Bills_ElectricMeterReadingId] ON [Bills] ([ElectricMeterReadingId]);
GO

CREATE INDEX [IX_Bills_RentAgreementId] ON [Bills] ([RentAgreementId]);
GO

CREATE INDEX [IX_Bills_RoomId] ON [Bills] ([RoomId]);
GO

CREATE INDEX [IX_Bills_TenantId] ON [Bills] ([TenantId]);
GO

CREATE INDEX [IX_ElectricMeterReadings_RoomId] ON [ElectricMeterReadings] ([RoomId]);
GO

CREATE INDEX [IX_Payments_BillId] ON [Payments] ([BillId]);
GO

CREATE INDEX [IX_Payments_RentAgreementId] ON [Payments] ([RentAgreementId]);
GO

CREATE INDEX [IX_Payments_TenantId] ON [Payments] ([TenantId]);
GO

CREATE INDEX [IX_RentAgreements_PropertyId] ON [RentAgreements] ([PropertyId]);
GO

CREATE INDEX [IX_RentAgreements_RoomId] ON [RentAgreements] ([RoomId]);
GO

CREATE INDEX [IX_RentAgreements_TenantId] ON [RentAgreements] ([TenantId]);
GO

CREATE INDEX [IX_Rooms_PropertyId] ON [Rooms] ([PropertyId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250811065606_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

