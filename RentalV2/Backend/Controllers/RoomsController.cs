using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Models;

namespace RentalBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public RoomsController(RentManagementContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetRooms()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Property)
                .Include(r => r.RentAgreements.Where(ra => ra.IsActive))
                    .ThenInclude(ra => ra.Tenant)
                .Include(r => r.ElectricMeterReadings.OrderByDescending(e => e.ReadingDate).Take(1))
                .OrderBy(r => r.FloorNumber).ThenBy(r => r.RoomNumber)
                .ToListAsync();

            return Ok(rooms.Select(r => {
                var activeAgreement = r.RentAgreements?.FirstOrDefault(ra => ra.IsActive);
                var lastReading = r.ElectricMeterReadings?.FirstOrDefault();
                return new {
                    r.Id,
                    r.RoomNumber,
                    r.FloorNumber,
                    r.MonthlyRent,
                    r.IsAvailable,
                    r.ElectricMeterNumber,
                    r.LastMeterReading,
                    r.LastReadingDate,
                    CurrentTenant = activeAgreement?.Tenant != null 
                        ? new { 
                            Id = activeAgreement.Tenant.Id,
                            Name = $"{activeAgreement.Tenant.FirstName} {activeAgreement.Tenant.LastName}",
                            Phone = activeAgreement.Tenant.PhoneNumber,
                            Since = activeAgreement.StartDate,
                            Rent = activeAgreement.MonthlyRent,
                            SecurityDeposit = activeAgreement.SecurityDeposit
                        }
                        : null,
                    RentAgreements = r.RentAgreements
                };
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetRoom(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.RentAgreements)
                    .ThenInclude(ra => ra.Tenant)
                .Include(r => r.ElectricMeterReadings.OrderByDescending(e => e.ReadingDate))
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound();

            var activeAgreement = room.RentAgreements?.FirstOrDefault(ra => ra.IsActive);

            return Ok(new {
                room.Id,
                room.RoomNumber,
                room.FloorNumber,
                room.MonthlyRent,
                room.IsAvailable,
                room.ElectricMeterNumber,
                room.LastMeterReading,
                room.LastReadingDate,
                CurrentTenant = activeAgreement?.Tenant != null
                    ? new {
                        Id = activeAgreement.Tenant.Id,
                        Name = $"{activeAgreement.Tenant.FirstName} {activeAgreement.Tenant.LastName}",
                        Phone = activeAgreement.Tenant.PhoneNumber,
                        Since = activeAgreement.StartDate,
                        Rent = activeAgreement.MonthlyRent,
                        SecurityDeposit = activeAgreement.SecurityDeposit
                    }
                    : null,
                MeterReadingHistory = room.ElectricMeterReadings?.Take(12).Select(e => new {
                    e.Id,
                    e.PreviousReading,
                    e.CurrentReading,
                    UnitsConsumed = e.CurrentReading - e.PreviousReading,
                    e.ElectricCharges,
                    e.ReadingDate,
                    e.IsBilled
                }).ToList(),
                TenantHistory = room.RentAgreements?.OrderByDescending(ra => ra.StartDate).Select(ra => new {
                    TenantName = ra.Tenant != null ? $"{ra.Tenant.FirstName} {ra.Tenant.LastName}" : "Unknown",
                    ra.StartDate,
                    ra.EndDate,
                    ra.IsActive,
                    ra.MonthlyRent,
                    ra.SecurityDeposit
                }).ToList()
            });
        }

        /// <summary>
        /// Assign tenant to room
        /// </summary>
        [HttpPost("{id}/assign-tenant")]
        public async Task<ActionResult> AssignTenant(int id, [FromBody] RoomTenantAssignment request)
        {
            var room = await _context.Rooms
                .Include(r => r.RentAgreements)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound("Room not found");

            var tenant = await _context.Tenants
                .Include(t => t.RentAgreements)
                .FirstOrDefaultAsync(t => t.Id == request.TenantId);

            if (tenant == null) return NotFound("Tenant not found");

            // Deactivate tenant's previous active agreement if any
            var oldAgreement = tenant.RentAgreements.FirstOrDefault(ra => ra.IsActive);
            if (oldAgreement != null)
            {
                oldAgreement.IsActive = false;
                oldAgreement.EndDate = DateTime.UtcNow;
                var oldRoom = await _context.Rooms.FindAsync(oldAgreement.RoomId);
                if (oldRoom != null) oldRoom.IsAvailable = true;
            }

            // Deactivate any current tenant in this room
            var currentAgreement = room.RentAgreements.FirstOrDefault(ra => ra.IsActive);
            if (currentAgreement != null)
            {
                currentAgreement.IsActive = false;
                currentAgreement.EndDate = DateTime.UtcNow;
            }

            var property = await _context.Properties.FirstOrDefaultAsync();

            // Create new agreement
            var agreement = new RentAgreement
            {
                PropertyId = property?.Id ?? 1,
                RoomId = room.Id,
                TenantId = tenant.Id,
                StartDate = request.StartDate ?? DateTime.UtcNow,
                EndDate = request.EndDate ?? DateTime.UtcNow.AddYears(1),
                MonthlyRent = request.MonthlyRent ?? room.MonthlyRent,
                SecurityDeposit = request.SecurityDeposit ?? 0,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.RentAgreements.Add(agreement);

            room.IsAvailable = false;
            await _context.SaveChangesAsync();

            // Record security deposit
            if (request.SecurityDeposit > 0)
            {
                _context.Payments.Add(new Payment
                {
                    TenantId = tenant.Id,
                    RentAgreementId = agreement.Id,
                    Amount = request.SecurityDeposit ?? 0,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = request.PaymentMethod ?? "Cash",
                    PaymentType = "Security Deposit",
                    Status = "Completed",
                    Notes = $"Security deposit for room {room.RoomNumber}",
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { 
                message = $"Tenant {tenant.FirstName} {tenant.LastName} assigned to room {room.RoomNumber}",
                agreement = new { agreement.Id, agreement.MonthlyRent, agreement.StartDate }
            });
        }

        [HttpPost("{id}/vacate")]
        public async Task<ActionResult> VacateRoom(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.RentAgreements)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound("Room not found");

            var activeAgreement = room.RentAgreements.FirstOrDefault(ra => ra.IsActive);
            if (activeAgreement == null) return BadRequest("Room is already vacant/no active agreement");

            // End agreement
            activeAgreement.IsActive = false;
            activeAgreement.EndDate = DateTime.UtcNow;
            
            // Free up room
            room.IsAvailable = true;
            
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Room {room.RoomNumber} is now vacant." });
        }

        [HttpPost("{id}/renew-agreement")]
        public async Task<ActionResult> RenewAgreement(int id, [FromBody] RoomTenantAssignment request)
        {
            var room = await _context.Rooms
                .Include(r => r.RentAgreements)
                .ThenInclude(ra => ra.Tenant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound("Room not found");

            var oldAgreement = room.RentAgreements.FirstOrDefault(ra => ra.IsActive);
            if (oldAgreement == null) return BadRequest("No active agreement to renew");

            var tenant = oldAgreement.Tenant;

            // End old agreement
            oldAgreement.IsActive = false;
            oldAgreement.EndDate = DateTime.UtcNow;

            // Create new agreement (Renew/Hike)
            var newAgreement = new RentAgreement
            {
                PropertyId = oldAgreement.PropertyId,
                RoomId = room.Id,
                TenantId = oldAgreement.TenantId,
                StartDate = request.StartDate ?? DateTime.UtcNow,
                EndDate = request.EndDate ?? DateTime.UtcNow.AddYears(1),
                MonthlyRent = request.MonthlyRent ?? oldAgreement.MonthlyRent, // New Rent!
                SecurityDeposit = oldAgreement.SecurityDeposit, // Carry forward security
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.RentAgreements.Add(newAgreement);
            
            // Room remains occupied (IsAvailable = false)
            room.IsAvailable = false; 
            
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Agreement renewed for {tenant.FirstName} {tenant.LastName}. New Rent: {newAgreement.MonthlyRent}",
                agreementId = newAgreement.Id
            });
        }

        /// <summary>
        /// Update room meter info
        /// </summary>
        [HttpPut("{id}/meter")]
        public async Task<IActionResult> UpdateMeterInfo(int id, [FromBody] MeterUpdateRequest request)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            if (!string.IsNullOrEmpty(request.MeterNumber))
                room.ElectricMeterNumber = request.MeterNumber;
            
            if (request.CurrentReading.HasValue)
            {
                room.LastMeterReading = request.CurrentReading.Value;
                room.LastReadingDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { room.RoomNumber, room.ElectricMeterNumber, room.LastMeterReading });
        }

        /// <summary>
        /// Record meter reading for a room
        /// </summary>
        [HttpPost("{id}/meter-reading")]
        public async Task<ActionResult> RecordMeterReading(int id, [FromBody] MeterReadingRequest request)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            var prevReading = request.PreviousReading ?? room.LastMeterReading ?? 0;
            var currReading = request.CurrentReading;
            var unitsConsumed = currReading - prevReading;

            // Get electric rate
            var rateSetting = await _context.SystemConfigurations.FirstOrDefaultAsync(s => s.ConfigKey == "ElectricRatePerUnit");
            var rate = decimal.Parse(rateSetting?.ConfigValue ?? "8.0");
            var charges = unitsConsumed * rate;

            var reading = new ElectricMeterReading
            {
                RoomId = room.Id,
                PreviousReading = prevReading,
                CurrentReading = currReading,
                ReadingDate = request.ReadingDate ?? DateTime.UtcNow,
                ElectricCharges = charges,
                IsBilled = false,
                Remarks = request.Remarks,
                CreatedDate = DateTime.UtcNow
            };

            _context.ElectricMeterReadings.Add(reading);

            // Update room's last reading
            room.LastMeterReading = currReading;
            room.LastReadingDate = reading.ReadingDate;

            await _context.SaveChangesAsync();

            return Ok(new {
                reading.Id,
                room.RoomNumber,
                prevReading,
                currReading,
                unitsConsumed,
                rate,
                charges
            });
        }

        [HttpPut("{id}/availability")]
        public async Task<IActionResult> UpdateAvailability(int id, [FromBody] bool isAvailable)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.IsAvailable = isAvailable;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/rent")]
        public async Task<IActionResult> UpdateRent(int id, [FromBody] decimal newRent)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.MonthlyRent = newRent;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetRoomSummary()
        {
            var rooms = await _context.Rooms.OrderBy(r => r.FloorNumber).ThenBy(r => r.RoomNumber).ToListAsync();
            return Ok(new { 
                Total = rooms.Count,
                ByFloor = rooms.GroupBy(r => r.FloorNumber).Select(g => new { 
                    Floor = g.Key, 
                    Count = g.Count(), 
                    Rooms = g.Select(r => r.RoomNumber).ToList() 
                }).ToList(),
                AllRoomNumbers = rooms.Select(r => r.RoomNumber).ToList()
            });
        }

        /// <summary>
        /// Get all rooms with meter info for bulk reading entry
        /// </summary>
        [HttpGet("for-billing")]
        public async Task<ActionResult<IEnumerable<object>>> GetRoomsForBilling()
        {
            var rooms = await _context.Rooms
                .Include(r => r.RentAgreements.Where(ra => ra.IsActive))
                    .ThenInclude(ra => ra.Tenant)
                .OrderBy(r => r.FloorNumber).ThenBy(r => r.RoomNumber)
                .ToListAsync();

            return Ok(rooms.Select(r => {
                var activeAgreement = r.RentAgreements?.FirstOrDefault(ra => ra.IsActive);
                return new {
                    r.Id,
                    r.RoomNumber,
                    r.FloorNumber,
                    r.MonthlyRent,
                    r.IsAvailable,
                    r.ElectricMeterNumber,
                    r.LastMeterReading,
                    r.LastReadingDate,
                    IsOccupied = activeAgreement != null,
                    TenantName = activeAgreement?.Tenant != null 
                        ? $"{activeAgreement.Tenant.FirstName} {activeAgreement.Tenant.LastName}" 
                        : null,
                    TenantId = activeAgreement?.TenantId
                };
            }));
        }

        [HttpPost("ensure-all")]
        public async Task<ActionResult<object>> EnsureAllRooms()
        {
            var property = await _context.Properties.FirstOrDefaultAsync();
            if (property == null) return BadRequest("No property found");

            var expectedRooms = new List<(string number, int floor)>();
            for (int i = 1; i <= 10; i++) expectedRooms.Add(($"G/{i}", 0));
            for (int i = 1; i <= 6; i++) expectedRooms.Add(($"1/{i}", 1));
            for (int i = 1; i <= 6; i++) expectedRooms.Add(($"2/{i}", 2));

            int created = 0;
            foreach (var (roomNum, floor) in expectedRooms)
            {
                var exists = await _context.Rooms.AnyAsync(r => r.RoomNumber == roomNum && r.PropertyId == property.Id);
                if (!exists)
                {
                    _context.Rooms.Add(new Room { 
                        RoomNumber = roomNum, 
                        FloorNumber = floor, 
                        PropertyId = property.Id, 
                        IsAvailable = true, 
                        MonthlyRent = 0,
                        ElectricMeterNumber = $"MTR-{roomNum.Replace("/", "")}"
                    });
                    created++;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Created {created} missing rooms", TotalExpected = 22 });
        }
    }

    public class RoomTenantAssignment
    {
        public int TenantId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MonthlyRent { get; set; }
        public decimal? SecurityDeposit { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class MeterUpdateRequest
    {
        public string? MeterNumber { get; set; }
        public int? CurrentReading { get; set; }
    }

    public class MeterReadingRequest
    {
        public int? PreviousReading { get; set; }
        public int CurrentReading { get; set; }
        public DateTime? ReadingDate { get; set; }
        public string? Remarks { get; set; }
    }
}
