using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Models;

namespace RentalBackend.Services
{
    public class SeedDataService
    {
        private readonly RentManagementContext _context;
        private readonly ILogger<SeedDataService> _logger;

        public SeedDataService(RentManagementContext context, ILogger<SeedDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedFromJsonAsync(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("Seed data file not found: {Path}", jsonPath);
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var seedData = JsonSerializer.Deserialize<SeedDataRoot>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (seedData == null) return;

            // Create or get Property
            var property = await _context.Properties.FirstOrDefaultAsync();
            if (property == null)
            {
                property = new Property
                {
                    PropertyName = seedData.Property.Name,
                    Address = seedData.Property.Address,
                    TotalFloors = seedData.Property.TotalFloors,
                    TotalRooms = seedData.Rooms.Count
                };
                _context.Properties.Add(property);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created property: {Name}", property.PropertyName);
            }

            int roomsCreated = 0, tenantsCreated = 0, agreementsCreated = 0;

            foreach (var roomData in seedData.Rooms)
            {
                // Create or update Room
                var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomData.Number && r.PropertyId == property.Id);
                if (room == null)
                {
                    room = new Room
                    {
                        RoomNumber = roomData.Number,
                        FloorNumber = roomData.Floor,
                        PropertyId = property.Id,
                        MonthlyRent = roomData.Rent,
                        IsAvailable = !roomData.Occupied
                    };
                    _context.Rooms.Add(room);
                    await _context.SaveChangesAsync();
                    roomsCreated++;
                }
                else
                {
                    // Update existing room
                    room.MonthlyRent = roomData.Rent;
                    room.IsAvailable = !roomData.Occupied;
                }

                // Process tenant if occupied
                if (roomData.Occupied && !string.IsNullOrEmpty(roomData.CurrentTenant))
                {
                    var names = roomData.CurrentTenant.Split(' ', 2);
                    var firstName = names[0];
                    var lastName = names.Length > 1 ? names[1] : "";

                    // Find or create tenant
                    var tenant = await _context.Tenants.FirstOrDefaultAsync(t =>
                        t.FirstName.ToLower() == firstName.ToLower() &&
                        (string.IsNullOrEmpty(lastName) || t.LastName.ToLower() == lastName.ToLower()));

                    if (tenant == null)
                    {
                        tenant = new Tenant
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.Tenants.Add(tenant);
                        await _context.SaveChangesAsync();
                        tenantsCreated++;
                    }

                    // Create active agreement if not exists
                    var existingAgreement = await _context.RentAgreements
                        .FirstOrDefaultAsync(ra => ra.RoomId == room.Id && ra.IsActive);

                    if (existingAgreement == null)
                    {
                        var startDate = DateTime.TryParse(roomData.AllotDate, out var parsed) ? parsed : DateTime.UtcNow;
                        var agreement = new RentAgreement
                        {
                            PropertyId = property.Id,
                            RoomId = room.Id,
                            TenantId = tenant.Id,
                            StartDate = startDate,
                            EndDate = startDate.AddYears(3),
                            MonthlyRent = roomData.Rent,
                            SecurityDeposit = roomData.Advance,
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.RentAgreements.Add(agreement);
                        agreementsCreated++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seed complete: {Rooms} rooms, {Tenants} tenants, {Agreements} agreements created",
                roomsCreated, tenantsCreated, agreementsCreated);
        }

        /// <summary>
        /// Clean up tenants that were incorrectly imported with "VACANT" in their name
        /// </summary>
        public async Task CleanupVacantTenantsAsync()
        {
            // Find all tenants with VACANT in their name (case insensitive)
            var vacantTenants = await _context.Tenants
                .Where(t => t.FirstName.ToUpper().Contains("VACANT") || 
                           t.LastName.ToUpper().Contains("VACANT") ||
                           t.FirstName.ToUpper().Contains("VACAMT") ||
                           t.FirstName.ToUpper().Contains("BLANK"))
                .ToListAsync();

            if (vacantTenants.Count == 0)
            {
                _logger.LogInformation("No VACANT tenants to clean up");
                return;
            }

            _logger.LogInformation("Found {Count} VACANT tenants to clean up", vacantTenants.Count);

            foreach (var tenant in vacantTenants)
            {
                // Get their rent agreements
                var agreements = await _context.RentAgreements
                    .Include(ra => ra.Room)
                    .Where(ra => ra.TenantId == tenant.Id)
                    .ToListAsync();

                foreach (var agreement in agreements)
                {
                    // Mark room as available
                    if (agreement.Room != null)
                    {
                        agreement.Room.IsAvailable = true;
                        _logger.LogInformation("Marking room {Room} as available (was assigned to VACANT tenant)", agreement.Room.RoomNumber);
                    }
                    // Deactivate the agreement
                    agreement.IsActive = false;
                }

                // Remove the fake tenant
                _context.Tenants.Remove(tenant);
                _logger.LogInformation("Removed VACANT tenant: {Name}", $"{tenant.FirstName} {tenant.LastName}");
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleanup complete: removed {Count} VACANT tenants", vacantTenants.Count);
        }
    }

    public class SeedDataRoot
    {
        public PropertyData Property { get; set; } = new();
        public List<RoomData> Rooms { get; set; } = new();
    }

    public class PropertyData
    {
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public int TotalFloors { get; set; }
    }

    public class RoomData
    {
        public string Number { get; set; } = "";
        public int Floor { get; set; }
        public string? CurrentTenant { get; set; }
        public decimal Rent { get; set; }
        public decimal Advance { get; set; }
        public string? AllotDate { get; set; }
        public bool Occupied { get; set; }
        public string? VacantFrom { get; set; }
    }
}
