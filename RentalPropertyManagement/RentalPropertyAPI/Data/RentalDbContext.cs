using Microsoft.EntityFrameworkCore;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.Data
{
    public class RentalDbContext : DbContext
    {
        public RentalDbContext(DbContextOptions<RentalDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ElectricityReading> ElectricityReadings { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<TenantDocument> TenantDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision
            modelBuilder.Entity<Tenant>()
                .Property(t => t.SecurityDeposit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Room>()
                .Property(r => r.MonthlyRent)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ElectricityReading>()
                .Property(e => e.Reading)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ElectricityReading>()
                .Property(e => e.UnitsConsumed)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ElectricityReading>()
                .Property(e => e.BillAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ElectricityReading>()
                .Property(e => e.UnitRate)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MaintenanceRequest>()
                .Property(m => m.EstimatedCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MaintenanceRequest>()
                .Property(m => m.ActualCost)
                .HasPrecision(18, 2);

            // Configure relationships
            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.Room)
                .WithMany(r => r.Tenants)
                .HasForeignKey(t => t.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Tenant)
                .WithMany(t => t.Payments)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ElectricityReading>()
                .HasOne(e => e.Room)
                .WithMany(r => r.ElectricityReadings)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TenantDocument>()
                .HasOne(d => d.Tenant)
                .WithMany(t => t.Documents)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Room)
                .WithMany(r => r.MaintenanceRequests)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Tenant)
                .WithMany(t => t.MaintenanceRequests)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure unique constraints
            modelBuilder.Entity<Room>()
                .HasIndex(r => r.RoomNumber)
                .IsUnique();

            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.Email)
                .IsUnique();

            // Seed initial data for rooms
            var rooms = new List<Room>();
            for (int i = 1; i <= 22; i++)
            {
                rooms.Add(new Room
                {
                    Id = i,
                    RoomNumber = $"R{i:D3}",
                    MonthlyRent = 5000, // Default rent, can be updated
                    Status = RoomStatus.Available,
                    ElectricMeterNumber = $"EM{i:D3}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            modelBuilder.Entity<Room>().HasData(rooms);
        }
    }
}