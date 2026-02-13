using Microsoft.EntityFrameworkCore;
using RentMangementsystem.Models;

namespace RentMangementsystem.Data
{
    public class RentManagementContext : DbContext
    {
        public RentManagementContext(DbContextOptions<RentManagementContext> options) : base(options)
        {
        }

        public DbSet<Property> Properties { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<RentAgreement> RentAgreements { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ElectricMeterReading> ElectricMeterReadings { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Property)
                .WithMany(p => p.Rooms)
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ElectricMeterReading>()
                .HasOne(emr => emr.Room)
                .WithMany(r => r.ElectricMeterReadings)
                .HasForeignKey(emr => emr.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bill>()
                .HasOne(b => b.RentAgreement)
                .WithMany()
                .HasForeignKey(b => b.RentAgreementId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Tenant)
                .WithMany()
                .HasForeignKey(b => b.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bill>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bills)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Bill>()
                .HasOne(b => b.ElectricMeterReading)
                .WithMany(emr => emr.Bills)
                .HasForeignKey(b => b.ElectricMeterReadingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BillItem>()
                .HasOne(bi => bi.Bill)
                .WithMany(b => b.BillItems)
                .HasForeignKey(bi => bi.BillId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RentAgreement>()
                .HasOne(ra => ra.Property)
                .WithMany(p => p.RentAgreements)
                .HasForeignKey(ra => ra.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RentAgreement>()
                .HasOne(ra => ra.Tenant)
                .WithMany(t => t.RentAgreements)
                .HasForeignKey(ra => ra.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RentAgreement>()
                .HasOne(ra => ra.Room)
                .WithMany(r => r.RentAgreements)
                .HasForeignKey(ra => ra.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.RentAgreement)
                .WithMany(ra => ra.Payments)
                .HasForeignKey(p => p.RentAgreementId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Tenant)
                .WithMany(t => t.Payments)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Bill)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BillId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure decimal precision
            modelBuilder.Entity<Property>()
                .Property(p => p.TotalArea)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Room>()
                .Property(r => r.Area)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Room>()
                .Property(r => r.MonthlyRent)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ElectricMeterReading>()
                .Property(emr => emr.ElectricCharges)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Bill>()
                .Property(b => b.RentAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Bill>()
                .Property(b => b.ElectricAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Bill>()
                .Property(b => b.MiscAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Bill>()
                .Property(b => b.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Bill>()
                .Property(b => b.PaidAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BillItem>()
                .Property(bi => bi.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BillItem>()
                .Property(bi => bi.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RentAgreement>()
                .Property(ra => ra.MonthlyRent)
                .HasPrecision(18, 2);

            modelBuilder.Entity<RentAgreement>()
                .Property(ra => ra.SecurityDeposit)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            // Seed data for your specific property and rooms
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed your main property
            modelBuilder.Entity<Property>().HasData(
                new Property
                {
                    Id = 1,
                    Address = "Your Property Address",
                    PropertyType = "Boarding House",
                    PropertyName = "Your Boarding House Name",
                    TotalRooms = 22,
                    NumberOfFloors = 3,
                    TotalArea = 1000, // Adjust based on your property
                    Description = "22-room boarding house with 3 floors - Ground floor (10 rooms), First floor (6 rooms), Second floor (6 rooms)",
                    HasSharedKitchen = true,
                    HasSharedBathrooms = true,
                    HasWiFi = true,
                    HasLaundryFacility = true,
                    CreatedDate = DateTime.Now.AddDays(-30)
                }
            );

            // Seed system configurations
            modelBuilder.Entity<SystemConfiguration>().HasData(
                new SystemConfiguration
                {
                    Id = 1,
                    ConfigKey = "ElectricUnitCost",
                    ConfigValue = "12.00",
                    Description = "Cost per electric unit in currency",
                    Category = "Electric",
                    LastUpdated = DateTime.Now
                },
                new SystemConfiguration
                {
                    Id = 2,
                    ConfigKey = "PropertyName",
                    ConfigValue = "Your Boarding House",
                    Description = "Name of the property",
                    Category = "General",
                    LastUpdated = DateTime.Now
                },
                new SystemConfiguration
                {
                    Id = 3,
                    ConfigKey = "BillDueDays",
                    ConfigValue = "15",
                    Description = "Number of days for bill due date",
                    Category = "Billing",
                    LastUpdated = DateTime.Now
                }
            );

            // Seed Ground Floor Rooms (10 rooms) - G/1 to G/10
            var rooms = new List<Room>();
            for (int i = 1; i <= 10; i++)
            {
                rooms.Add(new Room
                {
                    Id = i,
                    PropertyId = 1,
                    RoomNumber = $"G/{i}",
                    Floor = "Ground",
                    FloorNumber = 0,
                    Area = 120, // Adjust room size as needed
                    MonthlyRent = 500.00m, // Adjust rent as needed
                    IsAvailable = true,
                    RoomType = "Single",
                    ElectricMeterNumber = $"GM{i:D3}",
                    LastMeterReading = 1000 + i * 50, // Sample initial readings
                    LastReadingDate = DateTime.Now.AddDays(-30),
                    CreatedDate = DateTime.Now.AddDays(-30)
                });
            }

            // Seed First Floor Rooms (6 rooms) - 1/1 to 1/6
            for (int i = 1; i <= 6; i++)
            {
                rooms.Add(new Room
                {
                    Id = 10 + i,
                    PropertyId = 1,
                    RoomNumber = $"1/{i}",
                    Floor = "First",
                    FloorNumber = 1,
                    Area = 130, // Slightly larger rooms on upper floors
                    MonthlyRent = 550.00m,
                    IsAvailable = true,
                    RoomType = "Single",
                    ElectricMeterNumber = $"F1M{i:D3}",
                    LastMeterReading = 1200 + i * 60,
                    LastReadingDate = DateTime.Now.AddDays(-30),
                    CreatedDate = DateTime.Now.AddDays(-30)
                });
            }

            // Seed Second Floor Rooms (6 rooms) - 2/1 to 2/6
            for (int i = 1; i <= 6; i++)
            {
                rooms.Add(new Room
                {
                    Id = 16 + i,
                    PropertyId = 1,
                    RoomNumber = $"2/{i}",
                    Floor = "Second",
                    FloorNumber = 2,
                    Area = 130,
                    MonthlyRent = 550.00m,
                    IsAvailable = true,
                    RoomType = "Single",
                    ElectricMeterNumber = $"F2M{i:D3}",
                    LastMeterReading = 1400 + i * 70,
                    LastReadingDate = DateTime.Now.AddDays(-30),
                    CreatedDate = DateTime.Now.AddDays(-30)
                });
            }

            modelBuilder.Entity<Room>().HasData(rooms);
        }
    }
}