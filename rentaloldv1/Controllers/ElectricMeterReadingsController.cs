using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentMangementsystem.Data;
using RentMangementsystem.Models;

namespace RentMangementsystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElectricMeterReadingsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public ElectricMeterReadingsController(RentManagementContext context)
        {
            _context = context;
        }

        // GET: api/ElectricMeterReadings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ElectricMeterReading>>> GetElectricMeterReadings()
        {
            return await _context.ElectricMeterReadings
                .Include(emr => emr.Room)
                .OrderByDescending(emr => emr.ReadingDate)
                .ToListAsync();
        }

        // GET: api/ElectricMeterReadings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ElectricMeterReading>> GetElectricMeterReading(int id)
        {
            var reading = await _context.ElectricMeterReadings
                .Include(emr => emr.Room)
                .FirstOrDefaultAsync(emr => emr.Id == id);

            if (reading == null)
            {
                return NotFound();
            }

            return reading;
        }

        // GET: api/ElectricMeterReadings/room/{roomId}
        [HttpGet("room/{roomId}")]
        public async Task<ActionResult<IEnumerable<ElectricMeterReading>>> GetReadingsByRoom(int roomId)
        {
            return await _context.ElectricMeterReadings
                .Where(emr => emr.RoomId == roomId)
                .Include(emr => emr.Room)
                .OrderByDescending(emr => emr.ReadingDate)
                .ToListAsync();
        }

        // GET: api/ElectricMeterReadings/room/{roomId}/latest
        [HttpGet("room/{roomId}/latest")]
        public async Task<ActionResult<ElectricMeterReading?>> GetLatestReadingByRoom(int roomId)
        {
            return await _context.ElectricMeterReadings
                .Where(emr => emr.RoomId == roomId)
                .Include(emr => emr.Room)
                .OrderByDescending(emr => emr.ReadingDate)
                .FirstOrDefaultAsync();
        }

        // POST: api/ElectricMeterReadings
        [HttpPost]
        public async Task<ActionResult<ElectricMeterReading>> PostElectricMeterReading(ElectricMeterReading reading)
        {
            // Get the room to validate and get the last reading
            var room = await _context.Rooms.FindAsync(reading.RoomId);
            if (room == null)
            {
                return BadRequest("Room not found");
            }

            // Get the latest reading for this room
            var latestReading = await _context.ElectricMeterReadings
                .Where(emr => emr.RoomId == reading.RoomId)
                .OrderByDescending(emr => emr.ReadingDate)
                .FirstOrDefaultAsync();

            if (latestReading != null)
            {
                reading.PreviousReading = latestReading.CurrentReading;
                reading.PreviousReadingDate = latestReading.ReadingDate;
            }
            else
            {
                // First reading for this room
                reading.PreviousReading = room.LastMeterReading ?? 0;
                reading.PreviousReadingDate = room.LastReadingDate ?? DateTime.Now.AddMonths(-1);
            }

            // Validate that current reading is not less than previous
            if (reading.CurrentReading < reading.PreviousReading)
            {
                return BadRequest("Current reading cannot be less than previous reading");
            }

            // Calculate electric charges
            var electricUnitCost = await GetElectricUnitCost();
            var unitsConsumed = reading.CurrentReading - reading.PreviousReading;
            reading.ElectricCharges = unitsConsumed * electricUnitCost;

            // Update room's last reading
            room.LastMeterReading = reading.CurrentReading;
            room.LastReadingDate = reading.ReadingDate;

            _context.ElectricMeterReadings.Add(reading);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetElectricMeterReading", new { id = reading.Id }, reading);
        }

        // PUT: api/ElectricMeterReadings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutElectricMeterReading(int id, ElectricMeterReading reading)
        {
            if (id != reading.Id)
            {
                return BadRequest();
            }

            // Recalculate electric charges
            var electricUnitCost = await GetElectricUnitCost();
            var unitsConsumed = reading.CurrentReading - reading.PreviousReading;
            reading.ElectricCharges = unitsConsumed * electricUnitCost;

            _context.Entry(reading).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ElectricMeterReadingExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/ElectricMeterReadings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteElectricMeterReading(int id)
        {
            var reading = await _context.ElectricMeterReadings.FindAsync(id);
            if (reading == null)
            {
                return NotFound();
            }

            if (reading.IsBilled)
            {
                return BadRequest("Cannot delete reading that has been billed");
            }

            _context.ElectricMeterReadings.Remove(reading);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<decimal> GetElectricUnitCost()
        {
            var config = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == "ElectricUnitCost");
            
            if (config != null && decimal.TryParse(config.ConfigValue, out decimal cost))
            {
                return cost;
            }
            
            return 12.00m; // Default value
        }

        private bool ElectricMeterReadingExists(int id)
        {
            return _context.ElectricMeterReadings.Any(e => e.Id == id);
        }
    }
}