using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyAPI.Data;
using RentalPropertyAPI.DTOs;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public RoomsController(RentalDbContext context)
        {
            _context = context;
        }

        // GET: api/rooms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetRooms(RoomStatus? status = null)
        {
            var query = _context.Rooms
                .Include(r => r.Tenants.Where(t => t.IsActive))
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            var rooms = await query
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    MonthlyRent = r.MonthlyRent,
                    Status = r.Status,
                    ElectricMeterNumber = r.ElectricMeterNumber,
                    TenantCount = r.Tenants.Count(t => t.IsActive),
                    CurrentTenantName = r.Tenants.Where(t => t.IsActive).Select(t => t.FullName).FirstOrDefault(),
                    CreatedAt = r.CreatedAt
                })
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            return Ok(rooms);
        }

        // GET: api/rooms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDto>> GetRoom(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Tenants.Where(t => t.IsActive))
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound();

            var roomDto = new RoomDto
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                MonthlyRent = room.MonthlyRent,
                Status = room.Status,
                ElectricMeterNumber = room.ElectricMeterNumber,
                TenantCount = room.Tenants.Count(t => t.IsActive),
                CurrentTenantName = room.Tenants.Where(t => t.IsActive).Select(t => t.FullName).FirstOrDefault(),
                CreatedAt = room.CreatedAt
            };

            return Ok(roomDto);
        }

        // POST: api/rooms
        [HttpPost]
        public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomDto createDto)
        {
            // Check if room number already exists
            if (await _context.Rooms.AnyAsync(r => r.RoomNumber == createDto.RoomNumber))
                return BadRequest("Room number already exists");

            var room = new Room
            {
                RoomNumber = createDto.RoomNumber,
                MonthlyRent = createDto.MonthlyRent,
                ElectricMeterNumber = createDto.ElectricMeterNumber,
                Status = RoomStatus.Available,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var roomDto = new RoomDto
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                MonthlyRent = room.MonthlyRent,
                Status = room.Status,
                ElectricMeterNumber = room.ElectricMeterNumber,
                TenantCount = 0,
                CurrentTenantName = null,
                CreatedAt = room.CreatedAt
            };

            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, roomDto);
        }

        // PUT: api/rooms/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, UpdateRoomDto updateDto)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return NotFound();

            // Check if room number is being changed and if it conflicts with existing rooms
            if (updateDto.RoomNumber != room.RoomNumber && 
                await _context.Rooms.AnyAsync(r => r.RoomNumber == updateDto.RoomNumber && r.Id != id))
                return BadRequest("Room number already exists");

            room.RoomNumber = updateDto.RoomNumber;
            room.MonthlyRent = updateDto.MonthlyRent;
            room.Status = updateDto.Status;
            room.ElectricMeterNumber = updateDto.ElectricMeterNumber;
            room.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/rooms/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Tenants)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound();

            // Check if room has active tenants
            if (room.Tenants.Any(t => t.IsActive))
                return BadRequest("Cannot delete room with active tenants");

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/rooms/available
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetAvailableRooms()
        {
            var rooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.Available)
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    MonthlyRent = r.MonthlyRent,
                    Status = r.Status,
                    ElectricMeterNumber = r.ElectricMeterNumber,
                    TenantCount = 0,
                    CurrentTenantName = null,
                    CreatedAt = r.CreatedAt
                })
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            return Ok(rooms);
        }

        // GET: api/rooms/occupancy-summary
        [HttpGet("occupancy-summary")]
        public async Task<ActionResult<object>> GetOccupancySummary()
        {
            var totalRooms = await _context.Rooms.CountAsync();
            var occupiedRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
            var availableRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Available);
            var maintenanceRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Maintenance);

            var occupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;

            return Ok(new
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                AvailableRooms = availableRooms,
                MaintenanceRooms = maintenanceRooms,
                OccupancyRate = Math.Round(occupancyRate, 2)
            });
        }
    }
}