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
            });

            // Tenant entity configuration
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.TenantId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.HasIndex(e => e.Name).IsUnique();
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
        }
    }
}
