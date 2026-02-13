using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentMangementsystem.Data;
using RentMangementsystem.Models;

namespace RentMangementsystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public PropertiesController(RentManagementContext context)
        {
            _context = context;
        }

        // GET: api/Properties
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Property>>> GetProperties()
        {
            return await _context.Properties
                .Include(p => p.Rooms)
                .ToListAsync();
        }

        // GET: api/Properties/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Property>> GetProperty(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Rooms)
                .Include(p => p.RentAgreements)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            return property;
        }

        // GET: api/Properties/5/rooms
        [HttpGet("{id}/rooms")]
        public async Task<ActionResult<IEnumerable<Room>>> GetPropertyRooms(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            return await _context.Rooms
                .Where(r => r.PropertyId == id)
                .OrderBy(r => r.FloorNumber)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();
        }

        // GET: api/Properties/5/rooms/available
        [HttpGet("{id}/rooms/available")]
        public async Task<ActionResult<IEnumerable<Room>>> GetPropertyAvailableRooms(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }

            return await _context.Rooms
                .Where(r => r.PropertyId == id && r.IsAvailable)
                .OrderBy(r => r.FloorNumber)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();
        }

        // PUT: api/Properties/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProperty(int id, Property property)
        {
            if (id != property.Id)
            {
                return BadRequest();
            }

            _context.Entry(property).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropertyExists(id))
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

        // POST: api/Properties
        [HttpPost]
        public async Task<ActionResult<Property>> PostProperty(Property property)
        {
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProperty", new { id = property.Id }, property);
        }

        // GET: api/Properties/5/occupancy
        [HttpGet("{id}/occupancy")]
        public async Task<ActionResult<object>> GetPropertyOccupancy(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Rooms)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var totalRooms = property.Rooms.Count;
            var availableRooms = property.Rooms.Count(r => r.IsAvailable);
            var occupiedRooms = totalRooms - availableRooms;
            var occupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;

            return new
            {
                PropertyId = id,
                PropertyName = property.PropertyName,
                TotalRooms = totalRooms,
                AvailableRooms = availableRooms,
                OccupiedRooms = occupiedRooms,
                OccupancyRate = Math.Round(occupancyRate, 2)
            };
        }

        // DELETE: api/Properties/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Rooms)
                .Include(p => p.RentAgreements)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (property == null)
            {
                return NotFound();
            }

            // Check if property has active rent agreements
            if (property.RentAgreements.Any(ra => ra.IsActive))
            {
                return BadRequest("Cannot delete property with active rent agreements");
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.Id == id);
        }
    }
}