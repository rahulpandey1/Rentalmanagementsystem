using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyAPI.Data;
using RentalPropertyAPI.DTOs;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public MaintenanceController(RentalDbContext context)
        {
            _context = context;
        }

        // GET: api/maintenance
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceRequestDto>>> GetMaintenanceRequests(
            MaintenanceStatus? status = null,
            MaintenanceType? type = null,
            int? roomId = null,
            int? tenantId = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.Tenant)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(m => m.Status == status.Value);

            if (type.HasValue)
                query = query.Where(m => m.Type == type.Value);

            if (roomId.HasValue)
                query = query.Where(m => m.RoomId == roomId.Value);

            if (tenantId.HasValue)
                query = query.Where(m => m.TenantId == tenantId.Value);

            var requests = await query
                .OrderByDescending(m => m.RequestDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MaintenanceRequestDto
                {
                    Id = m.Id,
                    RoomId = m.RoomId,
                    RoomNumber = m.Room != null ? m.Room.RoomNumber : null,
                    TenantId = m.TenantId,
                    TenantName = m.Tenant != null ? m.Tenant.FullName : null,
                    Title = m.Title,
                    Description = m.Description,
                    Type = m.Type,
                    Status = m.Status,
                    Priority = m.Priority,
                    EstimatedCost = m.EstimatedCost,
                    ActualCost = m.ActualCost,
                    RequestDate = m.RequestDate,
                    CompletedDate = m.CompletedDate,
                    Notes = m.Notes,
                    ChargeToTenant = m.ChargeToTenant,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        // GET: api/maintenance/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceRequestDto>> GetMaintenanceRequest(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.Tenant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null)
                return NotFound();

            var requestDto = new MaintenanceRequestDto
            {
                Id = request.Id,
                RoomId = request.RoomId,
                RoomNumber = request.Room?.RoomNumber,
                TenantId = request.TenantId,
                TenantName = request.Tenant?.FullName,
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                Status = request.Status,
                Priority = request.Priority,
                EstimatedCost = request.EstimatedCost,
                ActualCost = request.ActualCost,
                RequestDate = request.RequestDate,
                CompletedDate = request.CompletedDate,
                Notes = request.Notes,
                ChargeToTenant = request.ChargeToTenant,
                CreatedAt = request.CreatedAt
            };

            return Ok(requestDto);
        }

        // POST: api/maintenance
        [HttpPost]
        public async Task<ActionResult<MaintenanceRequestDto>> CreateMaintenanceRequest(CreateMaintenanceRequestDto createDto)
        {
            // Validate room exists if provided
            if (createDto.RoomId.HasValue)
            {
                var room = await _context.Rooms.FindAsync(createDto.RoomId.Value);
                if (room == null)
                    return BadRequest("Room not found");
            }

            // Validate tenant exists if provided
            if (createDto.TenantId.HasValue)
            {
                var tenant = await _context.Tenants.FindAsync(createDto.TenantId.Value);
                if (tenant == null)
                    return BadRequest("Tenant not found");
            }

            var request = new MaintenanceRequest
            {
                RoomId = createDto.RoomId,
                TenantId = createDto.TenantId,
                Title = createDto.Title,
                Description = createDto.Description,
                Type = createDto.Type,
                Priority = createDto.Priority,
                EstimatedCost = createDto.EstimatedCost,
                Notes = createDto.Notes,
                ChargeToTenant = createDto.ChargeToTenant,
                Status = MaintenanceStatus.Pending,
                RequestDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            await _context.Entry(request)
                .Reference(m => m.Room)
                .LoadAsync();
            await _context.Entry(request)
                .Reference(m => m.Tenant)
                .LoadAsync();

            var requestDto = new MaintenanceRequestDto
            {
                Id = request.Id,
                RoomId = request.RoomId,
                RoomNumber = request.Room?.RoomNumber,
                TenantId = request.TenantId,
                TenantName = request.Tenant?.FullName,
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                Status = request.Status,
                Priority = request.Priority,
                EstimatedCost = request.EstimatedCost,
                ActualCost = request.ActualCost,
                RequestDate = request.RequestDate,
                CompletedDate = request.CompletedDate,
                Notes = request.Notes,
                ChargeToTenant = request.ChargeToTenant,
                CreatedAt = request.CreatedAt
            };

            return CreatedAtAction(nameof(GetMaintenanceRequest), new { id = request.Id }, requestDto);
        }

        // PUT: api/maintenance/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaintenanceRequest(int id, UpdateMaintenanceRequestDto updateDto)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            request.Title = updateDto.Title;
            request.Description = updateDto.Description;
            request.Type = updateDto.Type;
            request.Status = updateDto.Status;
            request.Priority = updateDto.Priority;
            request.EstimatedCost = updateDto.EstimatedCost;
            request.ActualCost = updateDto.ActualCost;
            request.CompletedDate = updateDto.CompletedDate;
            request.Notes = updateDto.Notes;
            request.ChargeToTenant = updateDto.ChargeToTenant;
            request.UpdatedAt = DateTime.UtcNow;

            // If status is being set to completed and no completion date is provided, set it to now
            if (updateDto.Status == MaintenanceStatus.Completed && !updateDto.CompletedDate.HasValue)
            {
                request.CompletedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/maintenance/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaintenanceRequest(int id)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            _context.MaintenanceRequests.Remove(request);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/maintenance/pending
        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<MaintenanceRequestDto>>> GetPendingRequests()
        {
            var requests = await _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.Tenant)
                .Where(m => m.Status == MaintenanceStatus.Pending || m.Status == MaintenanceStatus.InProgress)
                .OrderBy(m => m.Priority)
                .ThenBy(m => m.RequestDate)
                .Select(m => new MaintenanceRequestDto
                {
                    Id = m.Id,
                    RoomId = m.RoomId,
                    RoomNumber = m.Room != null ? m.Room.RoomNumber : null,
                    TenantId = m.TenantId,
                    TenantName = m.Tenant != null ? m.Tenant.FullName : null,
                    Title = m.Title,
                    Description = m.Description,
                    Type = m.Type,
                    Status = m.Status,
                    Priority = m.Priority,
                    EstimatedCost = m.EstimatedCost,
                    ActualCost = m.ActualCost,
                    RequestDate = m.RequestDate,
                    CompletedDate = m.CompletedDate,
                    Notes = m.Notes,
                    ChargeToTenant = m.ChargeToTenant,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        // GET: api/maintenance/summary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetMaintenanceSummary(
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var startDate = fromDate ?? DateTime.Now.AddMonths(-1);
            var endDate = toDate ?? DateTime.Now;

            var requests = await _context.MaintenanceRequests
                .Where(m => m.RequestDate >= startDate && m.RequestDate <= endDate)
                .ToListAsync();

            var totalRequests = requests.Count;
            var pendingRequests = requests.Count(m => m.Status == MaintenanceStatus.Pending);
            var inProgressRequests = requests.Count(m => m.Status == MaintenanceStatus.InProgress);
            var completedRequests = requests.Count(m => m.Status == MaintenanceStatus.Completed);
            var cancelledRequests = requests.Count(m => m.Status == MaintenanceStatus.Cancelled);

            var totalEstimatedCost = requests.Where(m => m.EstimatedCost.HasValue).Sum(m => m.EstimatedCost.Value);
            var totalActualCost = requests.Where(m => m.ActualCost.HasValue).Sum(m => m.ActualCost.Value);

            var averageCompletionTime = requests
                .Where(m => m.Status == MaintenanceStatus.Completed && m.CompletedDate.HasValue)
                .Select(m => (m.CompletedDate.Value - m.RequestDate).TotalDays)
                .DefaultIfEmpty(0)
                .Average();

            return Ok(new
            {
                TotalRequests = totalRequests,
                PendingRequests = pendingRequests,
                InProgressRequests = inProgressRequests,
                CompletedRequests = completedRequests,
                CancelledRequests = cancelledRequests,
                TotalEstimatedCost = totalEstimatedCost,
                TotalActualCost = totalActualCost,
                AverageCompletionDays = Math.Round(averageCompletionTime, 1),
                FromDate = startDate,
                ToDate = endDate
            });
        }

        // POST: api/maintenance/5/complete
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteMaintenanceRequest(int id, [FromBody] decimal? actualCost = null)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            if (request.Status == MaintenanceStatus.Completed)
                return BadRequest("Maintenance request is already completed");

            request.Status = MaintenanceStatus.Completed;
            request.CompletedDate = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            if (actualCost.HasValue)
                request.ActualCost = actualCost.Value;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}