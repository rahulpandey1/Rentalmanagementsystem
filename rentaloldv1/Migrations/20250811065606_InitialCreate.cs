using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RentMangementsystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PropertyType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalRooms = table.Column<int>(type: "int", nullable: false),
                    NumberOfFloors = table.Column<int>(type: "int", nullable: false),
                    TotalArea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HasSharedKitchen = table.Column<bool>(type: "bit", nullable: false),
                    HasSharedBathrooms = table.Column<bool>(type: "bit", nullable: false),
                    HasParking = table.Column<bool>(type: "bit", nullable: false),
                    HasLaundryFacility = table.Column<bool>(type: "bit", nullable: false),
                    HasWiFi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Floor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FloorNumber = table.Column<int>(type: "int", nullable: false),
                    Area = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RoomType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasPrivateBathroom = table.Column<bool>(type: "bit", nullable: false),
                    HasAC = table.Column<bool>(type: "bit", nullable: false),
                    IsFurnished = table.Column<bool>(type: "bit", nullable: false),
                    ElectricMeterNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastMeterReading = table.Column<int>(type: "int", nullable: true),
                    LastReadingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ElectricMeterReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    PreviousReading = table.Column<int>(type: "int", nullable: false),
                    CurrentReading = table.Column<int>(type: "int", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousReadingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ElectricCharges = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsBilled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricMeterReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectricMeterReadings_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RentAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Terms = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AgreementType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentAgreements_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentAgreements_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RentAgreements_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RentAgreementId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: true),
                    ElectricMeterReadingId = table.Column<int>(type: "int", nullable: true),
                    BillNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillPeriod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ElectricAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MiscAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bills_ElectricMeterReadings_ElectricMeterReadingId",
                        column: x => x.ElectricMeterReadingId,
                        principalTable: "ElectricMeterReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Bills_RentAgreements_RentAgreementId",
                        column: x => x.RentAgreementId,
                        principalTable: "RentAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bills_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Bills_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillItems_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RentAgreementId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    BillId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Payments_RentAgreements_RentAgreementId",
                        column: x => x.RentAgreementId,
                        principalTable: "RentAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Properties",
                columns: new[] { "Id", "Address", "CreatedDate", "Description", "HasLaundryFacility", "HasParking", "HasSharedBathrooms", "HasSharedKitchen", "HasWiFi", "NumberOfFloors", "PropertyName", "PropertyType", "TotalArea", "TotalRooms" },
                values: new object[] { 1, "Your Property Address", new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4387), "22-room boarding house with 3 floors - Ground floor (10 rooms), First floor (6 rooms), Second floor (6 rooms)", true, false, true, true, true, 3, "Your Boarding House Name", "Boarding House", 1000m, 22 });

            migrationBuilder.InsertData(
                table: "SystemConfigurations",
                columns: new[] { "Id", "Category", "ConfigKey", "ConfigValue", "Description", "LastUpdated" },
                values: new object[,]
                {
                    { 1, "Electric", "ElectricUnitCost", "12.00", "Cost per electric unit in currency", new DateTime(2025, 8, 11, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4578) },
                    { 2, "General", "PropertyName", "Your Boarding House", "Name of the property", new DateTime(2025, 8, 11, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4581) },
                    { 3, "Billing", "BillDueDays", "15", "Number of days for bill due date", new DateTime(2025, 8, 11, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4585) }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "Area", "CreatedDate", "Description", "ElectricMeterNumber", "Floor", "FloorNumber", "HasAC", "HasPrivateBathroom", "IsAvailable", "IsFurnished", "LastMeterReading", "LastReadingDate", "MonthlyRent", "PropertyId", "RoomNumber", "RoomType" },
                values: new object[,]
                {
                    { 1, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4656), null, "GM001", "Ground", 0, false, false, true, false, 1050, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4653), 500.00m, 1, "G/1", "Single" },
                    { 2, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4661), null, "GM002", "Ground", 0, false, false, true, false, 1100, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4661), 500.00m, 1, "G/2", "Single" },
                    { 3, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4665), null, "GM003", "Ground", 0, false, false, true, false, 1150, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4665), 500.00m, 1, "G/3", "Single" },
                    { 4, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4670), null, "GM004", "Ground", 0, false, false, true, false, 1200, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4669), 500.00m, 1, "G/4", "Single" },
                    { 5, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4674), null, "GM005", "Ground", 0, false, false, true, false, 1250, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4673), 500.00m, 1, "G/5", "Single" },
                    { 6, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4679), null, "GM006", "Ground", 0, false, false, true, false, 1300, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4678), 500.00m, 1, "G/6", "Single" },
                    { 7, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4682), null, "GM007", "Ground", 0, false, false, true, false, 1350, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4682), 500.00m, 1, "G/7", "Single" },
                    { 8, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4719), null, "GM008", "Ground", 0, false, false, true, false, 1400, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4718), 500.00m, 1, "G/8", "Single" },
                    { 9, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4722), null, "GM009", "Ground", 0, false, false, true, false, 1450, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4722), 500.00m, 1, "G/9", "Single" },
                    { 10, 120m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4729), null, "GM010", "Ground", 0, false, false, true, false, 1500, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4729), 500.00m, 1, "G/10", "Single" },
                    { 11, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4738), null, "F1M001", "First", 1, false, false, true, false, 1260, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4737), 550.00m, 1, "1/1", "Single" },
                    { 12, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4742), null, "F1M002", "First", 1, false, false, true, false, 1320, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4742), 550.00m, 1, "1/2", "Single" },
                    { 13, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4745), null, "F1M003", "First", 1, false, false, true, false, 1380, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4745), 550.00m, 1, "1/3", "Single" },
                    { 14, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4749), null, "F1M004", "First", 1, false, false, true, false, 1440, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4748), 550.00m, 1, "1/4", "Single" },
                    { 15, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4752), null, "F1M005", "First", 1, false, false, true, false, 1500, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4751), 550.00m, 1, "1/5", "Single" },
                    { 16, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4755), null, "F1M006", "First", 1, false, false, true, false, 1560, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4754), 550.00m, 1, "1/6", "Single" },
                    { 17, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4761), null, "F2M001", "Second", 2, false, false, true, false, 1470, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4760), 550.00m, 1, "2/1", "Single" },
                    { 18, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4765), null, "F2M002", "Second", 2, false, false, true, false, 1540, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4765), 550.00m, 1, "2/2", "Single" },
                    { 19, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4769), null, "F2M003", "Second", 2, false, false, true, false, 1610, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4768), 550.00m, 1, "2/3", "Single" },
                    { 20, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4771), null, "F2M004", "Second", 2, false, false, true, false, 1680, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4771), 550.00m, 1, "2/4", "Single" },
                    { 21, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4774), null, "F2M005", "Second", 2, false, false, true, false, 1750, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4774), 550.00m, 1, "2/5", "Single" },
                    { 22, 130m, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4777), null, "F2M006", "Second", 2, false, false, true, false, 1820, new DateTime(2025, 7, 12, 12, 26, 6, 386, DateTimeKind.Local).AddTicks(4777), 550.00m, 1, "2/6", "Single" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillItems_BillId",
                table: "BillItems",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_ElectricMeterReadingId",
                table: "Bills",
                column: "ElectricMeterReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_RentAgreementId",
                table: "Bills",
                column: "RentAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_RoomId",
                table: "Bills",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_TenantId",
                table: "Bills",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricMeterReadings_RoomId",
                table: "ElectricMeterReadings",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BillId",
                table: "Payments",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RentAgreementId",
                table: "Payments",
                column: "RentAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RentAgreements_PropertyId",
                table: "RentAgreements",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_RentAgreements_RoomId",
                table: "RentAgreements",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RentAgreements_TenantId",
                table: "RentAgreements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_PropertyId",
                table: "Rooms",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillItems");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");

            migrationBuilder.DropTable(
                name: "Bills");

            migrationBuilder.DropTable(
                name: "ElectricMeterReadings");

            migrationBuilder.DropTable(
                name: "RentAgreements");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Properties");
        }
    }
}
