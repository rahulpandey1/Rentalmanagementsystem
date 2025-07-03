using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyAPI.Data;
using RentalPropertyAPI.DTOs;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public TenantsController(RentalDbContext context)
        {
            _context = context;
        }

        // GET: api/tenants
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TenantDto>>> GetTenants(
            bool? isActive = null,
            int? roomId = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Tenants
                .Include(t => t.Room)
                .AsQueryable();

            if (isActive.HasValue)
                query = query.Where(t => t.IsActive == isActive.Value);

            if (roomId.HasValue)
                query = query.Where(t => t.RoomId == roomId.Value);

            var tenants = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TenantDto
                {
                    Id = t.Id,
                    FullName = t.FullName,
                    PhoneNumber = t.PhoneNumber,
                    Email = t.Email,
                    PermanentAddress = t.PermanentAddress,
                    MoveInDate = t.MoveInDate,
                    MoveOutDate = t.MoveOutDate,
                    SecurityDeposit = t.SecurityDeposit,
                    IsActive = t.IsActive,
                    RoomId = t.RoomId,
                    RoomNumber = t.Room.RoomNumber,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(tenants);
        }

        // GET: api/tenants/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TenantDto>> GetTenant(int id)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Room)
                .Include(t => t.Payments)
                .Include(t => t.Documents)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null)
                return NotFound();

            var tenantDto = new TenantDto
            {
                Id = tenant.Id,
                FullName = tenant.FullName,
                PhoneNumber = tenant.PhoneNumber,
                Email = tenant.Email,
                PermanentAddress = tenant.PermanentAddress,
                MoveInDate = tenant.MoveInDate,
                MoveOutDate = tenant.MoveOutDate,
                SecurityDeposit = tenant.SecurityDeposit,
                IsActive = tenant.IsActive,
                RoomId = tenant.RoomId,
                RoomNumber = tenant.Room.RoomNumber,
                CreatedAt = tenant.CreatedAt
            };

            return Ok(tenantDto);
        }

        // POST: api/tenants
        [HttpPost]
        public async Task<ActionResult<TenantDto>> CreateTenant(CreateTenantDto createDto)
        {
            // Check if room exists and is available
            var room = await _context.Rooms.FindAsync(createDto.RoomId);
            if (room == null)
                return BadRequest("Room not found");

            if (room.Status != RoomStatus.Available)
                return BadRequest("Room is not available");

            // Check if email is already taken
            if (await _context.Tenants.AnyAsync(t => t.Email == createDto.Email))
                return BadRequest("Email is already in use");

            var tenant = new Tenant
            {
                FullName = createDto.FullName,
                PhoneNumber = createDto.PhoneNumber,
                Email = createDto.Email,
                PermanentAddress = createDto.PermanentAddress,
                MoveInDate = createDto.MoveInDate,
                SecurityDeposit = createDto.SecurityDeposit,
                RoomId = createDto.RoomId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);

            // Update room status to occupied
            room.Status = RoomStatus.Occupied;
            room.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload tenant with room data
            await _context.Entry(tenant).Reference(t => t.Room).LoadAsync();

            var tenantDto = new TenantDto
            {
                Id = tenant.Id,
                FullName = tenant.FullName,
                PhoneNumber = tenant.PhoneNumber,
                Email = tenant.Email,
                PermanentAddress = tenant.PermanentAddress,
                MoveInDate = tenant.MoveInDate,
                MoveOutDate = tenant.MoveOutDate,
                SecurityDeposit = tenant.SecurityDeposit,
                IsActive = tenant.IsActive,
                RoomId = tenant.RoomId,
                RoomNumber = tenant.Room.RoomNumber,
                CreatedAt = tenant.CreatedAt
            };

            return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenantDto);
        }

        // PUT: api/tenants/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenant(int id, UpdateTenantDto updateDto)
        {
            var tenant = await _context.Tenants.Include(t => t.Room).FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null)
                return NotFound();

            // If room is being changed, check availability
            if (updateDto.RoomId != tenant.RoomId)
            {
                var newRoom = await _context.Rooms.FindAsync(updateDto.RoomId);
                if (newRoom == null)
                    return BadRequest("New room not found");

                if (newRoom.Status != RoomStatus.Available)
                    return BadRequest("New room is not available");

                // Mark old room as available
                tenant.Room.Status = RoomStatus.Available;
                tenant.Room.UpdatedAt = DateTime.UtcNow;

                // Mark new room as occupied
                newRoom.Status = RoomStatus.Occupied;
                newRoom.UpdatedAt = DateTime.UtcNow;

                tenant.RoomId = updateDto.RoomId;
            }

            // Check if email is already taken by another tenant
            if (updateDto.Email != tenant.Email && 
                await _context.Tenants.AnyAsync(t => t.Email == updateDto.Email && t.Id != id))
                return BadRequest("Email is already in use");

            tenant.FullName = updateDto.FullName;
            tenant.PhoneNumber = updateDto.PhoneNumber;
            tenant.Email = updateDto.Email;
            tenant.PermanentAddress = updateDto.PermanentAddress;
            tenant.MoveOutDate = updateDto.MoveOutDate;
            tenant.SecurityDeposit = updateDto.SecurityDeposit;
            tenant.IsActive = updateDto.IsActive;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/tenants/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTenant(int id)
        {
            var tenant = await _context.Tenants.Include(t => t.Room).FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null)
                return NotFound();

            // Mark room as available
            tenant.Room.Status = RoomStatus.Available;
            tenant.Room.UpdatedAt = DateTime.UtcNow;

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/tenants/5/moveout
        [HttpPost("{id}/moveout")]
        public async Task<ActionResult<TenantSettlementDto>> ProcessMoveOut(int id, DateTime moveOutDate)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Room)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null)
                return NotFound();

            if (!tenant.IsActive)
                return BadRequest("Tenant is already inactive");

            // Calculate settlement
            var totalRentPaid = tenant.Payments
                .Where(p => p.Type == PaymentType.Rent)
                .Sum(p => p.Amount);

            var totalElectricityPaid = tenant.Payments
                .Where(p => p.Type == PaymentType.Electricity)
                .Sum(p => p.Amount);

            var totalMaintenanceCharges = tenant.Payments
                .Where(p => p.Type == PaymentType.Maintenance)
                .Sum(p => p.Amount);

            // For this example, outstanding dues calculation is simplified
            // In a real system, you'd calculate based on unpaid bills
            var outstandingDues = 0m;

            var refundableAmount = tenant.SecurityDeposit - outstandingDues;

            // Update tenant status
            tenant.MoveOutDate = moveOutDate;
            tenant.IsActive = false;
            tenant.UpdatedAt = DateTime.UtcNow;

            // Mark room as available
            tenant.Room.Status = RoomStatus.Available;
            tenant.Room.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var settlement = new TenantSettlementDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.FullName,
                RoomNumber = tenant.Room.RoomNumber,
                MoveInDate = tenant.MoveInDate,
                MoveOutDate = moveOutDate,
                SecurityDeposit = tenant.SecurityDeposit,
                TotalRentPaid = totalRentPaid,
                TotalElectricityPaid = totalElectricityPaid,
                TotalMaintenanceCharges = totalMaintenanceCharges,
                OutstandingDues = outstandingDues,
                RefundableAmount = refundableAmount
            };

            return Ok(settlement);
        }
    }
}