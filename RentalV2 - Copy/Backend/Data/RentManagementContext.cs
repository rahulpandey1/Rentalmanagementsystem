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

        public DbSet<Property> Properties { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<RentAgreement> RentAgreements { get; set; }
        public DbSet<ElectricMeterReading> ElectricMeterReadings { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Property>().HasData(
                new Property { Id = 1, PropertyName = "Sita Devi Pandey and Sons", Address = "11/1B/1 GANPAT RAI KHEMKA LANE LILUAH HOWRAH(W. B.) 711204", TotalFloors = 3, TotalRooms = 22 }
            );
            
            modelBuilder.Entity<SystemConfiguration>().HasData(
                new SystemConfiguration { Id = 1, ConfigKey = "ElectricUnitCost", ConfigValue = "12.00", Description = "Cost per unit of electricity", LastUpdated = DateTime.UtcNow },
                new SystemConfiguration { Id = 2, ConfigKey = "BillDueDays", ConfigValue = "15", Description = "Number of days to pay bill before overdue", LastUpdated = DateTime.UtcNow }
            );

            // Configure relationships if needed, e.g.
            modelBuilder.Entity<BillItem>()
                .Property(b => b.Amount)
                .HasColumnType("decimal(18,2)");
                
            base.OnModelCreating(modelBuilder);
        }
    }
}
