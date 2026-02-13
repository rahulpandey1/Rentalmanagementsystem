using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentMangementsystem.Data;
using RentMangementsystem.Models;

namespace RentMangementsystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RentAgreementsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public RentAgreementsController(RentManagementContext context)
        {
            _context = context;
        }

        // GET: api/RentAgreements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RentAgreement>>> GetRentAgreements()
        {
            return await _context.RentAgreements
                .Include(ra => ra.Property)
                .Include(ra => ra.Tenant)
                .Include(ra => ra.Room)
                .ToListAsync();
        }

        // GET: api/RentAgreements/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RentAgreement>> GetRentAgreement(int id)
        {
            var rentAgreement = await _context.RentAgreements
                .Include(ra => ra.Property)
                .Include(ra => ra.Tenant)
                .Include(ra => ra.Room)
                .Include(ra => ra.Payments)
                .FirstOrDefaultAsync(ra => ra.Id == id);

            if (rentAgreement == null)
            {
                return NotFound();
            }

            return rentAgreement;
        }

        // GET: api/RentAgreements/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<RentAgreement>>> GetActiveRentAgreements()
        {
            return await _context.RentAgreements
                .Where(ra => ra.IsActive)
                .Include(ra => ra.Property)
                .Include(ra => ra.Tenant)
                .Include(ra => ra.Room)
                .ToListAsync();
        }

        // GET: api/RentAgreements/room/{roomId}
        [HttpGet("room/{roomId}")]
        public async Task<ActionResult<RentAgreement?>> GetActiveRentAgreementByRoom(int roomId)
        {
            return await _context.RentAgreements
                .Where(ra => ra.RoomId == roomId && ra.IsActive)
                .Include(ra => ra.Property)
                .Include(ra => ra.Tenant)
                .Include(ra => ra.Room)
                .FirstOrDefaultAsync();
        }

        // POST: api/RentAgreements
        [HttpPost]
        public async Task<ActionResult<RentAgreement>> PostRentAgreement(RentAgreement rentAgreement)
        {
            // Validate that property exists
            var property = await _context.Properties.FindAsync(rentAgreement.PropertyId);
            if (property == null)
            {
                return BadRequest("Property not found");
            }

            // Validate that tenant exists
            var tenant = await _context.Tenants.FindAsync(rentAgreement.TenantId);
            if (tenant == null)
            {
                return BadRequest("Tenant not found");
            }

            // If room is specified, validate room and availability
            if (rentAgreement.RoomId.HasValue)
            {
                var room = await _context.Rooms.FindAsync(rentAgreement.RoomId.Value);
                if (room == null)
                {
                    return BadRequest("Room not found");
                }

                if (!room.IsAvailable)
                {
                    return BadRequest("Room is not available");
                }

                // Check if room is already rented (has active agreement)
                var existingAgreement = await _context.RentAgreements
                    .AnyAsync(ra => ra.RoomId == rentAgreement.RoomId && ra.IsActive);
                
                if (existingAgreement)
                {
                    return BadRequest("Room is already rented");
                }

                // Set rent from room if not specified
                if (rentAgreement.MonthlyRent == 0)
                {
                    rentAgreement.MonthlyRent = room.MonthlyRent;
                }

                // Mark room as unavailable
                room.IsAvailable = false;
            }

            _context.RentAgreements.Add(rentAgreement);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRentAgreement", new { id = rentAgreement.Id }, rentAgreement);
        }

        // PUT: api/RentAgreements/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRentAgreement(int id, RentAgreement rentAgreement)
        {
            if (id != rentAgreement.Id)
            {
                return BadRequest();
            }

            _context.Entry(rentAgreement).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RentAgreementExists(id))
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

        // PUT: api/RentAgreements/5/terminate
        [HttpPut("{id}/terminate")]
        public async Task<IActionResult> TerminateRentAgreement(int id)
        {
            var rentAgreement = await _context.RentAgreements
                .Include(ra => ra.Room)
                .FirstOrDefaultAsync(ra => ra.Id == id);

            if (rentAgreement == null)
            {
                return NotFound();
            }

            rentAgreement.IsActive = false;
            rentAgreement.EndDate = DateTime.Now;
            
            // Make room available again if it was assigned
            if (rentAgreement.Room != null)
            {
                rentAgreement.Room.IsAvailable = true;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/RentAgreements/5/room/{roomId}
        [HttpPut("{id}/room/{roomId}")]
        public async Task<IActionResult> AssignRoomToAgreement(int id, int roomId)
        {
            var rentAgreement = await _context.RentAgreements
                .Include(ra => ra.Room)
                .FirstOrDefaultAsync(ra => ra.Id == id);

            if (rentAgreement == null)
            {
                return NotFound("Rent agreement not found");
            }

            var newRoom = await _context.Rooms.FindAsync(roomId);
            if (newRoom == null)
            {
                return BadRequest("Room not found");
            }

            if (!newRoom.IsAvailable)
            {
                return BadRequest("Room is not available");
            }

            // Free up the old room if it exists
            if (rentAgreement.Room != null)
            {
                rentAgreement.Room.IsAvailable = true;
            }

            // Assign new room
            rentAgreement.RoomId = roomId;
            rentAgreement.MonthlyRent = newRoom.MonthlyRent;
            newRoom.IsAvailable = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/RentAgreements/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRentAgreement(int id)
        {
            var rentAgreement = await _context.RentAgreements
                .Include(ra => ra.Room)
                .FirstOrDefaultAsync(ra => ra.Id == id);
            
            if (rentAgreement == null)
            {
                return NotFound();
            }

            // Make room available again if it was assigned
            if (rentAgreement.Room != null)
            {
                rentAgreement.Room.IsAvailable = true;
            }
            
            _context.RentAgreements.Remove(rentAgreement);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RentAgreementExists(int id)
        {
            return _context.RentAgreements.Any(e => e.Id == id);
        }
    }
}