using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Models;

namespace RentalBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public TenantsController(RentManagementContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTenants(int? year, bool? includeUnassigned)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var yearStart = new DateTime(targetYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var yearEnd = new DateTime(targetYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            var today = DateTime.UtcNow;

            var tenants = await _context.Tenants
                .Include(t => t.RentAgreements)
                    .ThenInclude(ra => ra.Room)
                .Include(t => t.Payments)
                .ToListAsync();

            // Build result for all tenants
            var result = tenants
                .Where(t => 
                    // Include unassigned tenants if requested
                    (includeUnassigned == true && !t.RentAgreements.Any()) ||
                    // Or tenants who had agreements active in the selected year
                    t.RentAgreements.Any(ra => 
                        ra.StartDate <= yearEnd && 
                        (ra.EndDate == null || ra.EndDate >= yearStart || ra.IsActive)))
                .Select(t => {
                    var activeAgreement = t.RentAgreements.FirstOrDefault(ra => ra.IsActive);
                    var earliestAgreement = t.RentAgreements.OrderBy(ra => ra.StartDate).FirstOrDefault();
                    var tenureStartDate = earliestAgreement?.StartDate;
                    var tenureYears = tenureStartDate.HasValue 
                        ? (int)((today - tenureStartDate.Value).TotalDays / 365) 
                        : 0;
                    var tenureMonths = tenureStartDate.HasValue 
                        ? (int)(((today - tenureStartDate.Value).TotalDays % 365) / 30) 
                        : 0;

                    // Check if rent increase is due (has been living for 1+ years)
                    var needsRentIncrease = tenureYears >= 1 && activeAgreement != null;

                    // Calculate total security deposit and payments
                    var totalSecurityDeposit = t.RentAgreements.Sum(ra => ra.SecurityDeposit);
                    var totalPayments = t.Payments?.Sum(p => p.Amount) ?? 0;

                    return new {
                        t.Id,
                        t.FirstName,
                        t.LastName,
                        t.PhoneNumber,
                        t.Email,
                        t.Address,
                        t.IdProofType,
                        t.IdProofNumber,
                        t.IsActive,
                        t.CreatedDate,
                        CurrentRoom = activeAgreement?.Room?.RoomNumber,
                        CurrentRoomId = activeAgreement?.RoomId,
                        CurrentRent = activeAgreement?.MonthlyRent ?? 0,
                        SecurityDeposit = totalSecurityDeposit,
                        TotalPayments = totalPayments,
                        IsAssigned = activeAgreement != null,
                        Since = tenureStartDate,
                        TenureYears = tenureYears,
                        TenureMonths = tenureMonths,
                        TenureDisplay = tenureYears > 0 
                            ? $"{tenureYears} year(s) {tenureMonths} month(s)" 
                            : tenureMonths > 0 
                                ? $"{tenureMonths} month(s)" 
                                : "New",
                        NeedsRentIncrease = needsRentIncrease,
                        RentIncreaseMessage = needsRentIncrease 
                            ? $"Tenant living since {tenureStartDate:MMM yyyy}. Consider rent increase." 
                            : null,
                        PaymentHistory = t.Payments?.OrderByDescending(p => p.PaymentDate)
                            .Take(5)
                            .Select(p => new {
                                p.Id,
                                p.Amount,
                                p.PaymentDate,
                                p.PaymentMethod,
                                p.Notes
                            }).ToList(),
                        AllRooms = t.RentAgreements
                            .OrderByDescending(ra => ra.StartDate)
                            .Select(ra => new { 
                                ra.Room?.RoomNumber, 
                                ra.StartDate, 
                                ra.EndDate, 
                                ra.IsActive,
                                ra.MonthlyRent,
                                ra.SecurityDeposit
                            }).ToList()
                    };
                })
                .OrderByDescending(t => t.NeedsRentIncrease)
                .ThenByDescending(t => !t.IsAssigned) // Unassigned first if requested
                .ThenBy(t => t.FirstName)
                .ToList();

            return Ok(result);
        }

        /// <summary>
        /// Get unassigned tenants (waiting queue)
        /// </summary>
        [HttpGet("unassigned")]
        public async Task<ActionResult<IEnumerable<object>>> GetUnassignedTenants()
        {
            var tenants = await _context.Tenants
                .Include(t => t.RentAgreements)
                .Where(t => !t.RentAgreements.Any(ra => ra.IsActive))
                .Select(t => new {
                    t.Id,
                    t.FirstName,
                    t.LastName,
                    t.PhoneNumber,
                    t.Email,
                    t.CreatedDate
                })
                .ToListAsync();

            return Ok(tenants);
        }

        /// <summary>
        /// Get single tenant with full details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTenant(int id)
        {
            var tenant = await _context.Tenants
                .Include(t => t.RentAgreements)
                    .ThenInclude(ra => ra.Room)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return NotFound();

            var activeAgreement = tenant.RentAgreements.FirstOrDefault(ra => ra.IsActive);
            var totalSecurityDeposit = tenant.RentAgreements.Sum(ra => ra.SecurityDeposit);

            // Get pending bills
            var pendingBills = await _context.Bills
                .Where(b => b.TenantId == id && b.Status != "Paid" && b.TotalAmount > b.PaidAmount)
                .Select(b => new {
                    b.Id,
                    b.BillNumber,
                    b.BillPeriod,
                    b.TotalAmount,
                    b.PaidAmount,
                    Outstanding = b.TotalAmount - b.PaidAmount,
                    b.DueDate
                })
                .ToListAsync();

            return Ok(new {
                tenant.Id,
                tenant.FirstName,
                tenant.LastName,
                tenant.PhoneNumber,
                tenant.Email,
                tenant.Address,
                tenant.IdProofType,
                tenant.IdProofNumber,
                tenant.DateOfBirth,
                tenant.EmergencyContactName,
                tenant.EmergencyContactPhone,
                tenant.IsActive,
                tenant.CreatedDate,
                CurrentRoom = activeAgreement?.Room?.RoomNumber,
                CurrentRoomId = activeAgreement?.RoomId,
                CurrentRent = activeAgreement?.MonthlyRent ?? 0,
                SecurityDeposit = totalSecurityDeposit,
                PendingBills = pendingBills,
                TotalOutstanding = pendingBills.Sum(b => b.Outstanding),
                PaymentHistory = tenant.Payments?.OrderByDescending(p => p.PaymentDate)
                    .Select(p => new {
                        p.Id,
                        p.Amount,
                        p.PaymentDate,
                        p.PaymentMethod,
                        p.PaymentType,
                        p.Notes
                    }).ToList(),
                RoomHistory = tenant.RentAgreements
                    .OrderByDescending(ra => ra.StartDate)
                    .Select(ra => new {
                        ra.Room?.RoomNumber,
                        ra.StartDate,
                        ra.EndDate,
                        ra.IsActive,
                        ra.MonthlyRent,
                        ra.SecurityDeposit
                    }).ToList()
            });
        }

        /// <summary>
        /// Add tenant with optional room assignment
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> PostTenant([FromBody] TenantCreateRequest request)
        {
            var tenant = new Tenant
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Address = request.Address,
                IdProofType = request.IdProofType,
                IdProofNumber = request.IdProofNumber,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // If room is specified, create rent agreement
            if (request.RoomId.HasValue && request.RoomId > 0)
            {
                var room = await _context.Rooms.FindAsync(request.RoomId.Value);
                if (room != null)
                {
                    var property = await _context.Properties.FirstOrDefaultAsync();
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

                    // Mark room as occupied
                    room.IsAvailable = false;
                    await _context.SaveChangesAsync();

                    // Record security deposit payment if provided
                    if (request.SecurityDeposit.HasValue && request.SecurityDeposit > 0)
                    {
                        var payment = new Payment
                        {
                            TenantId = tenant.Id,
                            RentAgreementId = agreement.Id,
                            Amount = request.SecurityDeposit.Value,
                            PaymentDate = DateTime.UtcNow,
                            PaymentMethod = request.PaymentMethod ?? "Cash",
                            PaymentType = "Security Deposit",
                            Status = "Completed",
                            Notes = "Initial security deposit",
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.Payments.Add(payment);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return CreatedAtAction("GetTenant", new { id = tenant.Id }, new {
                tenant.Id,
                tenant.FirstName,
                tenant.LastName,
                Message = request.RoomId.HasValue ? "Tenant added and assigned to room" : "Tenant added to queue"
            });
        }

        /// <summary>
        /// Assign tenant to a room
        /// </summary>
        [HttpPost("{id}/assign")]
        public async Task<ActionResult> AssignTenantToRoom(int id, [FromBody] RoomAssignmentRequest request)
        {
            var tenant = await _context.Tenants
                .Include(t => t.RentAgreements)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return NotFound("Tenant not found");

            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null) return NotFound("Room not found");

            // Deactivate any existing active agreement
            var existingAgreement = tenant.RentAgreements.FirstOrDefault(ra => ra.IsActive);
            if (existingAgreement != null)
            {
                existingAgreement.IsActive = false;
                existingAgreement.EndDate = DateTime.UtcNow;
                
                // Mark old room as available
                var oldRoom = await _context.Rooms.FindAsync(existingAgreement.RoomId);
                if (oldRoom != null) oldRoom.IsAvailable = true;
            }

            var property = await _context.Properties.FirstOrDefaultAsync();

            // Create new rent agreement
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

            // Mark room as occupied
            room.IsAvailable = false;
            await _context.SaveChangesAsync();

            // Record security deposit payment if provided
            if (request.SecurityDeposit.HasValue && request.SecurityDeposit > 0)
            {
                var payment = new Payment
                {
                    TenantId = tenant.Id,
                    RentAgreementId = agreement.Id,
                    Amount = request.SecurityDeposit.Value,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = request.PaymentMethod ?? "Cash",
                    PaymentType = "Security Deposit",
                    Status = "Completed",
                    Notes = "Security deposit for room " + room.RoomNumber,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
            }

            return Ok(new { 
                message = $"Tenant assigned to room {room.RoomNumber}",
                roomNumber = room.RoomNumber,
                monthlyRent = agreement.MonthlyRent
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTenant(int id, Tenant tenant)
        {
            if (id != tenant.Id)
            {
                return BadRequest();
            }

            _context.Entry(tenant).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TenantExists(id))
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

        private bool TenantExists(int id)
        {
            return _context.Tenants.Any(e => e.Id == id);
        }
    }

    public class TenantCreateRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? IdProofType { get; set; }
        public string? IdProofNumber { get; set; }
        // Optional room assignment
        public int? RoomId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MonthlyRent { get; set; }
        public decimal? SecurityDeposit { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class RoomAssignmentRequest
    {
        public int RoomId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MonthlyRent { get; set; }
        public decimal? SecurityDeposit { get; set; }
        public string? PaymentMethod { get; set; }
    }
}
