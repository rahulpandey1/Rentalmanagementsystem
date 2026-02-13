using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RentalBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomMonthlyRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropertyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TotalFloors = table.Column<int>(type: "integer", nullable: false),
                    TotalRooms = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConfigValue = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IdProofType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IdProofNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropertyId = table.Column<int>(type: "integer", nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FloorNumber = table.Column<int>(type: "integer", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    Area = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ElectricMeterNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastMeterReading = table.Column<int>(type: "integer", nullable: true),
                    LastReadingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RentAgreementId = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    BillId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransactionReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ElectricMeterReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    PreviousReading = table.Column<int>(type: "integer", nullable: false),
                    CurrentReading = table.Column<int>(type: "integer", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousReadingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ElectricCharges = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsBilled = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricMeterReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectricMeterReadings_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RentAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PropertyId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AgreementType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Terms = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentAgreements_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RentAgreements_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomMonthlyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    TenantName = table.Column<string>(type: "text", nullable: true),
                    IsVacant = table.Column<bool>(type: "boolean", nullable: false),
                    InitialAllotmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InitialRent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentAllotmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentRent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ElectricSecurity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentAdvance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PreviousReading = table.Column<int>(type: "integer", nullable: false),
                    CurrentReading = table.Column<int>(type: "integer", nullable: false),
                    UnitsConsumed = table.Column<int>(type: "integer", nullable: false),
                    RatePerUnit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ElectricBillAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MiscCharges = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BalanceBroughtForward = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmountDue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BalanceCarriedForward = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomMonthlyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomMonthlyRecords_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RentAgreementId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    ElectricMeterReadingId = table.Column<int>(type: "integer", nullable: true),
                    BillNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BillDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillPeriod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ElectricAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MiscAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bills_ElectricMeterReadings_ElectricMeterReadingId",
                        column: x => x.ElectricMeterReadingId,
                        principalTable: "ElectricMeterReadings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bills_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Bills_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillId = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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

            migrationBuilder.InsertData(
                table: "Properties",
                columns: new[] { "Id", "Address", "PropertyName", "TotalFloors", "TotalRooms" },
                values: new object[] { 1, "11/1B/1 GANPAT RAI KHEMKA LANE LILUAH HOWRAH(W. B.) 711204", "Sita Devi Pandey and Sons", 3, 22 });

            migrationBuilder.InsertData(
                table: "SystemConfigurations",
                columns: new[] { "Id", "ConfigKey", "ConfigValue", "Description", "LastUpdated" },
                values: new object[,]
                {
                    { 1, "ElectricUnitCost", "12.00", "Cost per unit of electricity", new DateTime(2026, 2, 8, 2, 16, 48, 869, DateTimeKind.Utc).AddTicks(8633) },
                    { 2, "BillDueDays", "15", "Number of days to pay bill before overdue", new DateTime(2026, 2, 8, 2, 16, 48, 869, DateTimeKind.Utc).AddTicks(8634) }
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
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RentAgreements_RoomId",
                table: "RentAgreements",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RentAgreements_TenantId",
                table: "RentAgreements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomMonthlyRecords_RoomId",
                table: "RoomMonthlyRecords",
                column: "RoomId");

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
                name: "RentAgreements");

            migrationBuilder.DropTable(
                name: "RoomMonthlyRecords");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");

            migrationBuilder.DropTable(
                name: "Bills");

            migrationBuilder.DropTable(
                name: "ElectricMeterReadings");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Properties");
        }
    }
}
