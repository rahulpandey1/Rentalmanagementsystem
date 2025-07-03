using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyAPI.Data;
using RentalPropertyAPI.DTOs;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElectricityController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public ElectricityController(RentalDbContext context)
        {
            _context = context;
        }

        // GET: api/electricity/readings
        [HttpGet("readings")]
        public async Task<ActionResult<IEnumerable<ElectricityReadingDto>>> GetReadings(
            int? roomId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.ElectricityReadings
                .Include(e => e.Room)
                .AsQueryable();

            if (roomId.HasValue)
                query = query.Where(e => e.RoomId == roomId.Value);

            if (fromDate.HasValue)
                query = query.Where(e => e.ReadingDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(e => e.ReadingDate <= toDate.Value);

            var readings = await query
                .OrderByDescending(e => e.ReadingDate)
                .Select(e => new ElectricityReadingDto
                {
                    Id = e.Id,
                    RoomId = e.RoomId,
                    RoomNumber = e.Room.RoomNumber,
                    Reading = e.Reading,
                    ReadingDate = e.ReadingDate,
                    UnitsConsumed = e.UnitsConsumed,
                    BillAmount = e.BillAmount,
                    UnitRate = e.UnitRate,
                    Notes = e.Notes,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return Ok(readings);
        }

        // GET: api/electricity/readings/5
        [HttpGet("readings/{id}")]
        public async Task<ActionResult<ElectricityReadingDto>> GetReading(int id)
        {
            var reading = await _context.ElectricityReadings
                .Include(e => e.Room)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (reading == null)
                return NotFound();

            var readingDto = new ElectricityReadingDto
            {
                Id = reading.Id,
                RoomId = reading.RoomId,
                RoomNumber = reading.Room.RoomNumber,
                Reading = reading.Reading,
                ReadingDate = reading.ReadingDate,
                UnitsConsumed = reading.UnitsConsumed,
                BillAmount = reading.BillAmount,
                UnitRate = reading.UnitRate,
                Notes = reading.Notes,
                CreatedAt = reading.CreatedAt
            };

            return Ok(readingDto);
        }

        // POST: api/electricity/readings
        [HttpPost("readings")]
        public async Task<ActionResult<ElectricityReadingDto>> CreateReading(CreateElectricityReadingDto createDto)
        {
            // Verify room exists
            var room = await _context.Rooms.FindAsync(createDto.RoomId);
            if (room == null)
                return BadRequest("Room not found");

            // Get the latest reading for this room to calculate units consumed
            var latestReading = await _context.ElectricityReadings
                .Where(e => e.RoomId == createDto.RoomId)
                .OrderByDescending(e => e.ReadingDate)
                .FirstOrDefaultAsync();

            decimal? unitsConsumed = null;
            decimal? billAmount = null;

            if (latestReading != null && createDto.Reading >= latestReading.Reading)
            {
                unitsConsumed = createDto.Reading - latestReading.Reading;
                billAmount = unitsConsumed * createDto.UnitRate;
            }

            var reading = new ElectricityReading
            {
                RoomId = createDto.RoomId,
                Reading = createDto.Reading,
                ReadingDate = createDto.ReadingDate,
                UnitRate = createDto.UnitRate,
                UnitsConsumed = unitsConsumed,
                BillAmount = billAmount,
                Notes = createDto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.ElectricityReadings.Add(reading);
            await _context.SaveChangesAsync();

            var readingDto = new ElectricityReadingDto
            {
                Id = reading.Id,
                RoomId = reading.RoomId,
                RoomNumber = room.RoomNumber,
                Reading = reading.Reading,
                ReadingDate = reading.ReadingDate,
                UnitsConsumed = reading.UnitsConsumed,
                BillAmount = reading.BillAmount,
                UnitRate = reading.UnitRate,
                Notes = reading.Notes,
                CreatedAt = reading.CreatedAt
            };

            return CreatedAtAction(nameof(GetReading), new { id = reading.Id }, readingDto);
        }

        // DELETE: api/electricity/readings/5
        [HttpDelete("readings/{id}")]
        public async Task<IActionResult> DeleteReading(int id)
        {
            var reading = await _context.ElectricityReadings.FindAsync(id);
            if (reading == null)
                return NotFound();

            _context.ElectricityReadings.Remove(reading);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/electricity/bills
        [HttpGet("bills")]
        public async Task<ActionResult<IEnumerable<ElectricityBillDto>>> GetBills(
            int? roomId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.ElectricityReadings
                .Include(e => e.Room)
                    .ThenInclude(r => r.Tenants.Where(t => t.IsActive))
                .Where(e => e.UnitsConsumed.HasValue && e.BillAmount.HasValue)
                .AsQueryable();

            if (roomId.HasValue)
                query = query.Where(e => e.RoomId == roomId.Value);

            if (fromDate.HasValue)
                query = query.Where(e => e.ReadingDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(e => e.ReadingDate <= toDate.Value);

            var bills = await query
                .OrderByDescending(e => e.ReadingDate)
                .Select(e => new ElectricityBillDto
                {
                    RoomId = e.RoomId,
                    RoomNumber = e.Room.RoomNumber,
                    TenantName = e.Room.Tenants.Where(t => t.IsActive).Select(t => t.FullName).FirstOrDefault(),
                    CurrentReading = e.Reading,
                    UnitsConsumed = e.UnitsConsumed.Value,
                    UnitRate = e.UnitRate,
                    BillAmount = e.BillAmount.Value,
                    BillingPeriodStart = e.ReadingDate.AddMonths(-1),
                    BillingPeriodEnd = e.ReadingDate
                })
                .ToListAsync();

            // Get previous readings for each bill
            foreach (var bill in bills)
            {
                var previousReading = await _context.ElectricityReadings
                    .Where(e => e.RoomId == bill.RoomId && e.ReadingDate < bill.BillingPeriodEnd)
                    .OrderByDescending(e => e.ReadingDate)
                    .Select(e => e.Reading)
                    .FirstOrDefaultAsync();

                bill.PreviousReading = previousReading;
            }

            return Ok(bills);
        }

        // GET: api/electricity/bills/room/5
        [HttpGet("bills/room/{roomId}")]
        public async Task<ActionResult<IEnumerable<ElectricityBillDto>>> GetRoomBills(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Tenants.Where(t => t.IsActive))
                .Include(r => r.ElectricityReadings)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null)
                return NotFound("Room not found");

            var readings = room.ElectricityReadings
                .Where(e => e.UnitsConsumed.HasValue && e.BillAmount.HasValue)
                .OrderByDescending(e => e.ReadingDate)
                .ToList();

            var bills = new List<ElectricityBillDto>();

            for (int i = 0; i < readings.Count; i++)
            {
                var currentReading = readings[i];
                var previousReading = i < readings.Count - 1 ? readings[i + 1] : null;

                bills.Add(new ElectricityBillDto
                {
                    RoomId = room.Id,
                    RoomNumber = room.RoomNumber,
                    TenantName = room.Tenants.Where(t => t.IsActive).Select(t => t.FullName).FirstOrDefault(),
                    PreviousReading = previousReading?.Reading ?? 0,
                    CurrentReading = currentReading.Reading,
                    UnitsConsumed = currentReading.UnitsConsumed.Value,
                    UnitRate = currentReading.UnitRate,
                    BillAmount = currentReading.BillAmount.Value,
                    BillingPeriodStart = previousReading?.ReadingDate ?? currentReading.ReadingDate.AddMonths(-1),
                    BillingPeriodEnd = currentReading.ReadingDate
                });
            }

            return Ok(bills);
        }

        // GET: api/electricity/pending-readings
        [HttpGet("pending-readings")]
        public async Task<ActionResult<IEnumerable<object>>> GetPendingReadings()
        {
            var currentDate = DateTime.Now;
            var lastMonthStart = new DateTime(currentDate.Year, currentDate.Month, 1).AddMonths(-1);

            var roomsWithoutCurrentMonthReading = await _context.Rooms
                .Include(r => r.Tenants.Where(t => t.IsActive))
                .Include(r => r.ElectricityReadings)
                .Where(r => r.Status == RoomStatus.Occupied)
                .Where(r => !r.ElectricityReadings.Any(e => 
                    e.ReadingDate.Year == currentDate.Year && 
                    e.ReadingDate.Month == currentDate.Month))
                .Select(r => new
                {
                    RoomId = r.Id,
                    RoomNumber = r.RoomNumber,
                    TenantName = r.Tenants.Where(t => t.IsActive).Select(t => t.FullName).FirstOrDefault(),
                    ElectricMeterNumber = r.ElectricMeterNumber,
                    LastReading = r.ElectricityReadings
                        .OrderByDescending(e => e.ReadingDate)
                        .Select(e => new { e.Reading, e.ReadingDate })
                        .FirstOrDefault(),
                    DaysSinceLastReading = r.ElectricityReadings.Any() 
                        ? (currentDate - r.ElectricityReadings.Max(e => e.ReadingDate)).Days 
                        : (int?)null
                })
                .ToListAsync();

            return Ok(roomsWithoutCurrentMonthReading);
        }
    }
}