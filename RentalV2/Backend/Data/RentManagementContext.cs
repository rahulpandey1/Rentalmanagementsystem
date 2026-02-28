using Microsoft.EntityFrameworkCore;
using RentalBackend.Models;

namespace RentalBackend.Data
{
    public class RentManagementContext : DbContext
    {
        public RentManagementContext(DbContextOptions<RentManagementContext> options)
            : base(options)
        {
        }

        public DbSet<Flat> Flats { get; set; } = null!;
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<Occupancy> Occupancies { get; set; } = null!;
        public DbSet<MonthlyLedger> MonthlyLedgers { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<TenantDocument> TenantDocuments { get; set; } = null!;
        public DbSet<SecurityDepositTransaction> SecurityDepositTransactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("public");

            // Flat entity configuration
            modelBuilder.Entity<Flat>(entity =>
            {
                entity.HasKey(e => e.FlatId);
                entity.Property(e => e.RoomCode).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.RoomCode).IsUnique();
                entity.Property(e => e.Floor).IsRequired(false);
                entity.Property(e => e.MeterId).HasMaxLength(100);
                entity.Property(e => e.BaseRent).HasPrecision(12, 2);
            });

            // Tenant entity configuration
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.TenantId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.FatherName).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.AadhaarNumber).HasMaxLength(20);
                entity.Property(e => e.PanNumber).HasMaxLength(20);
                entity.Property(e => e.PermanentAddress).HasMaxLength(1000);
                entity.Property(e => e.EmergencyContact).HasMaxLength(255);
                entity.Property(e => e.EmergencyPhone).HasMaxLength(20);
                entity.Property(e => e.TentativeRoomCode).HasMaxLength(50);
                entity.Property(e => e.TentativeRent).HasPrecision(12, 2);
                entity.Property(e => e.SecurityDeposit).HasPrecision(12, 2);
                entity.Property(e => e.Notes).HasMaxLength(2000);
            });

            // TenantDocument entity configuration
            modelBuilder.Entity<TenantDocument>(entity =>
            {
                entity.HasKey(e => e.DocumentId);
                entity.Property(e => e.DocumentType).HasMaxLength(50);
                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.FilePath).HasMaxLength(500);
                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.Documents)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // SecurityDepositTransaction entity configuration
            modelBuilder.Entity<SecurityDepositTransaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId);
                entity.Property(e => e.Amount).HasPrecision(12, 2);
                entity.Property(e => e.Type).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.DepositTransactions)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Occupancy entity configuration
            modelBuilder.Entity<Occupancy>(entity =>
            {
                entity.HasKey(e => e.OccupancyId);
                entity.HasOne(e => e.Flat)
                    .WithMany(f => f.Occupancies)
                    .HasForeignKey(e => e.FlatId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.Occupancies)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.FlatId, e.TenantId, e.StartDate })
                    .IsUnique()
                    .HasFilter("\"EndDate\" IS NULL");
            });

            // MonthlyLedger entity configuration
            modelBuilder.Entity<MonthlyLedger>(entity =>
            {
                entity.HasKey(e => e.MonthlyLedgerId);
                entity.Property(e => e.Period).HasColumnType("date");
                entity.Property(e => e.DateOfAllotment).HasColumnType("date");
                entity.Property(e => e.PaymentDate).HasColumnType("date");
                entity.Property(e => e.ElectricSecurity).HasPrecision(12, 2);
                entity.Property(e => e.MonthlyRent).HasPrecision(12, 2);
                entity.Property(e => e.ElecNew).HasPrecision(12, 3);
                entity.Property(e => e.ElecPrev).HasPrecision(12, 3);
                entity.Property(e => e.ElecRate).HasPrecision(12, 3);
                entity.Property(e => e.ElecUnits).HasPrecision(12, 3);
                entity.Property(e => e.ElecCost).HasPrecision(12, 2);
                entity.Property(e => e.MiscRent).HasPrecision(12, 2);
                entity.Property(e => e.Carryover).HasPrecision(12, 2);
                entity.Property(e => e.TotalDue).HasPrecision(12, 2);
                entity.Property(e => e.AmountPaid).HasPrecision(12, 2);
                entity.Property(e => e.ClosingBalance).HasPrecision(12, 2);
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.MiscChargeName).HasMaxLength(255);
                entity.Property(e => e.InvoiceNumber).HasMaxLength(20);
                entity.HasIndex(e => e.InvoiceNumber).IsUnique().HasFilter("\"InvoiceNumber\" IS NOT NULL");

                entity.HasOne(e => e.Flat)
                    .WithMany(f => f.MonthlyLedgers)
                    .HasForeignKey(e => e.FlatId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.MonthlyLedgers)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.Period, e.FlatId }).IsUnique();
            });

            // Payment entity configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.Period).HasColumnType("date");
                entity.Property(e => e.PaymentDate).HasColumnType("date");
                entity.Property(e => e.Amount).HasPrecision(12, 2);
                entity.Property(e => e.Source).IsRequired().HasMaxLength(50).HasDefaultValue("ExcelImport");

                entity.HasOne(e => e.Flat)
                    .WithMany(f => f.Payments)
                    .HasForeignKey(e => e.FlatId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Tenant)
                    .WithMany(t => t.Payments)
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.Period, e.FlatId });
                entity.HasIndex(e => e.TenantId);
            });

            // AuditLog entity configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ModuleName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityId).HasMaxLength(255);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.BrowserInfo).HasMaxLength(500);
                entity.Property(e => e.RequestUrl).HasMaxLength(500);
                entity.Property(e => e.CorrelationId).HasMaxLength(100);

                entity.HasIndex(e => e.CreatedDateTime);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.ModuleName);
            });
        }

    }
}
