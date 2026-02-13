using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentMangementsystem.Data;
using RentMangementsystem.Models;

namespace RentMangementsystem.Controllers
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

        // GET: api/Rooms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            return await _context.Rooms
                .Include(r => r.Property)
                .OrderBy(r => r.FloorNumber)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();
        }

        // GET: api/Rooms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetRoom(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Property)
                .Include(r => r.RentAgreements)
                    .ThenInclude(ra => ra.Tenant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            return room;
        }

        // GET: api/Rooms/available
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<Room>>> GetAvailableRooms()
        {
            return await _context.Rooms
                .Where(r => r.IsAvailable)
                .Include(r => r.Property)
                .OrderBy(r => r.FloorNumber)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();
        }

        // GET: api/Rooms/floor/{floorNumber}
        [HttpGet("floor/{floorNumber}")]
        public async Task<ActionResult<IEnumerable<Room>>> GetRoomsByFloor(int floorNumber)
        {
            return await _context.Rooms
                .Where(r => r.FloorNumber == floorNumber)
                .Include(r => r.Property)
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();
        }

        // GET: api/Rooms/property/{propertyId}
        [HttpGet("property/{propertyId}")]
        public async Task<ActionResult<IEnumerable<Room>>> GetRoomsByProperty(int propertyId)
        {
            return await _context.Rooms
                .Where(r => r.PropertyId == propertyId)
                .Include(r => r.Property)
                .OrderBy(r => r.FloorNumber)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();
        }

        // PUT: api/Rooms/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRoom(int id, Room room)
        {
            if (id != room.Id)
            {
                return BadRequest();
            }

            _context.Entry(room).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomExists(id))
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

        // POST: api/Rooms
        [HttpPost]
        public async Task<ActionResult<Room>> PostRoom(Room room)
        {
            // Validate that property exists
            var property = await _context.Properties.FindAsync(room.PropertyId);
            if (property == null)
            {
                return BadRequest("Property not found");
            }

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRoom", new { id = room.Id }, room);
        }

        // PUT: api/Rooms/5/rent
        [HttpPut("{id}/rent")]
        public async Task<IActionResult> UpdateRoomRent(int id, [FromBody] decimal newRent)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            room.MonthlyRent = newRent;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Rooms/5/availability
        [HttpPut("{id}/availability")]
        public async Task<IActionResult> UpdateRoomAvailability(int id, [FromBody] bool isAvailable)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            room.IsAvailable = isAvailable;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Rooms/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}