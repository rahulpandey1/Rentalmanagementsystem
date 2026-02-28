using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Filters;
using RentalBackend.Models;

namespace RentalBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [AuditLog("Tenant", "Tenant")]
    public class TenantsController : ControllerBase
    {
        private readonly RentManagementContext _context;
        private readonly IWebHostEnvironment _env;

        public TenantsController(RentManagementContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTenants(int? year, int? month)
        {
            // If period specified, show tenants based on ledger data for that month
            if (month != null && year != null)
            {
                var period = new DateOnly(year.Value, month.Value, 1);
                var ledgers = await _context.MonthlyLedgers
                    .Include(l => l.Tenant)
                    .Include(l => l.Flat)
                    .Where(l => l.Period == period && l.TenantId != null)
                    .OrderBy(l => l.Tenant!.Name)
                    .ToListAsync();

                var result = ledgers.Select(l => new
                {
                    Id = l.TenantId,
                    TenantId = l.TenantId,
                    Name = l.Tenant?.Name ?? "VACANT",
                    FirstName = l.Tenant?.Name ?? "VACANT",
                    LastName = "",
                    PhoneNumber = l.Tenant?.Phone,
                    Phone = l.Tenant?.Phone,
                    Email = l.Tenant?.Email,
                    FatherName = l.Tenant?.FatherName,
                    Address = l.Tenant?.PermanentAddress,
                    IsActive = true,
                    IsAssigned = true,
                    RoomNumber = l.Flat?.RoomCode,
                    FlatId = l.FlatId,
                    MonthlyRent = l.MonthlyRent,
                    SecurityDeposit = l.ElectricSecurity,
                    StartDate = l.DateOfAllotment?.ToDateTime(TimeOnly.MinValue),
                    NeedsRentIncrease = false,
                    ClosingBalance = l.ClosingBalance
                });

                return Ok(result);
            }

            // Default: show all tenants with current occupancy
            var tenants = await _context.Tenants
                .Include(t => t.Occupancies)
                    .ThenInclude(o => o.Flat)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var defaultResult = tenants.Select(t =>
            {
                var activeOcc = t.Occupancies
                    .FirstOrDefault(o => o.EndDate == null);

                var latestLedger = _context.MonthlyLedgers
                    .Where(l => l.TenantId == t.TenantId)
                    .OrderByDescending(l => l.Period)
                    .FirstOrDefault();

                return new
                {
                    Id = t.TenantId,
                    t.TenantId,
                    t.Name,
                    FirstName = t.Name,
                    LastName = "",
                    PhoneNumber = t.Phone,
                    t.Phone,
                    t.Email,
                    t.FatherName,
                    Address = t.PermanentAddress,
                    IsActive = true,
                    IsAssigned = activeOcc != null,
                    RoomNumber = activeOcc?.Flat?.RoomCode,
                    FlatId = activeOcc?.FlatId,
                    MonthlyRent = latestLedger?.MonthlyRent ?? 0,
                    SecurityDeposit = latestLedger?.ElectricSecurity ?? 0,
                    StartDate = activeOcc?.StartDate?.ToDateTime(TimeOnly.MinValue),
                    NeedsRentIncrease = false,
                    ClosingBalance = latestLedger?.ClosingBalance ?? 0
                };
            });

            return Ok(defaultResult);
        }

        [HttpGet("unassigned")]
        public async Task<ActionResult<IEnumerable<object>>> GetUnassignedTenants()
        {
            var allTenantIds = await _context.Tenants.Select(t => t.TenantId).ToListAsync();
            var assignedTenantIds = await _context.Occupancies
                .Where(o => o.EndDate == null && o.TenantId != null)
                .Select(o => o.TenantId!.Value)
                .Distinct()
                .ToListAsync();

            var unassignedIds = allTenantIds.Except(assignedTenantIds).ToList();

            var unassigned = await _context.Tenants
                .Where(t => unassignedIds.Contains(t.TenantId))
                .Select(t => new
                {
                    Id = t.TenantId,
                    t.TenantId,
                    t.Name,
                    FirstName = t.Name,
                    LastName = "",
                    PhoneNumber = t.Phone,
                    t.Phone,
                    t.TentativeRoomCode,
                    t.TentativeRent
                })
                .ToListAsync();

            return Ok(unassigned);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTenant(Guid id)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Occupancies)
                    .ThenInclude(o => o.Flat)
                .Include(t => t.Documents)
                .FirstOrDefaultAsync(t => t.TenantId == id);

            if (tenant == null) return NotFound();

            var activeOcc = tenant.Occupancies.FirstOrDefault(o => o.EndDate == null);

            // Get ledger history
            var ledgerHistory = await _context.MonthlyLedgers
                .Where(l => l.TenantId == id)
                .Include(l => l.Flat)
                .OrderByDescending(l => l.Period)
                .Take(24)
                .Select(l => new
                {
                    Period = l.Period.ToString("yyyy-MM"),
                    RoomCode = l.Flat!.RoomCode,
                    l.MonthlyRent,
                    l.ElecCost,
                    l.MiscRent,
                    l.TotalDue,
                    l.AmountPaid,
                    l.ClosingBalance
                })
                .ToListAsync();

            var latestLedger = ledgerHistory.FirstOrDefault();

            var result = new
            {
                Id = tenant.TenantId,
                tenant.TenantId,
                tenant.Name,
                FirstName = tenant.Name,
                LastName = "",
                tenant.FatherName,
                Phone = tenant.Phone,
                PhoneNumber = tenant.Phone,
                tenant.Email,
                tenant.AadhaarNumber,
                tenant.PanNumber,
                tenant.PermanentAddress,
                tenant.EmergencyContact,
                tenant.EmergencyPhone,
                tenant.TentativeRoomCode,
                tenant.TentativeRent,
                tenant.SecurityDeposit,
                tenant.Notes,
                IsActive = true,
                IsAssigned = activeOcc != null,
                RoomNumber = activeOcc?.Flat?.RoomCode,
                FlatId = activeOcc?.FlatId,
                StartDate = activeOcc?.StartDate?.ToDateTime(TimeOnly.MinValue),
                MonthlyRent = latestLedger?.MonthlyRent ?? 0,
                ClosingBalance = latestLedger?.ClosingBalance ?? 0,
                LedgerHistory = ledgerHistory,
                OccupancyHistory = tenant.Occupancies.OrderByDescending(o => o.StartDate).Select(o => new
                {
                    RoomCode = o.Flat?.RoomCode,
                    o.StartDate,
                    o.EndDate,
                    IsActive = o.EndDate == null
                }),
                Documents = tenant.Documents.OrderByDescending(d => d.UploadedUtc).Select(d => new
                {
                    d.DocumentId,
                    d.DocumentType,
                    d.FileName,
                    d.UploadedUtc
                }),
                DepositHistory = _context.SecurityDepositTransactions
                    .Where(dt => dt.TenantId == id)
                    .OrderByDescending(dt => dt.CreatedUtc)
                    .Select(dt => new
                    {
                        dt.TransactionId,
                        dt.Amount,
                        dt.Type,
                        dt.Description,
                        dt.CreatedUtc
                    }).ToList()
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> PostTenant([FromBody] TenantCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest("Name is required.");

            var name = !string.IsNullOrWhiteSpace(request.Name)
                ? request.Name
                : $"{request.FirstName} {request.LastName}".Trim();

            // Check for duplicate
            var exists = await _context.Tenants.AnyAsync(t => t.Name == name);
            if (exists)
                return BadRequest($"Tenant '{name}' already exists.");

            var tenant = new Tenant
            {
                Name = name,
                FatherName = request.FatherName,
                Phone = request.PhoneNumber ?? request.Phone,
                Email = request.Email,
                AadhaarNumber = request.AadhaarNumber,
                PanNumber = request.PanNumber,
                PermanentAddress = request.Address ?? request.PermanentAddress,
                EmergencyContact = request.EmergencyContact,
                EmergencyPhone = request.EmergencyPhone,
                TentativeRoomCode = request.TentativeRoomCode,
                TentativeRent = request.TentativeRent,
                Notes = request.Notes
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // If a flat was specified, create occupancy
            if (request.FlatId != null || request.RoomId != null)
            {
                Flat? flat = null;
                if (request.FlatId != null)
                    flat = await _context.Flats.FindAsync(request.FlatId);
                else if (request.RoomId != null)
                {
                    // RoomId might be sent as string GUID from frontend
                    if (Guid.TryParse(request.RoomId, out var roomGuid))
                        flat = await _context.Flats.FindAsync(roomGuid);
                }

                if (flat != null)
                {
                    var occupancy = new Occupancy
                    {
                        FlatId = flat.FlatId,
                        TenantId = tenant.TenantId,
                        StartDate = request.StartDate != null
                            ? DateOnly.FromDateTime(request.StartDate.Value)
                            : DateOnly.FromDateTime(DateTime.UtcNow)
                    };
                    _context.Occupancies.Add(occupancy);
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = $"Tenant '{name}' added.", tenantId = tenant.TenantId });
        }

        [HttpPost("{id}/assign")]
        public async Task<ActionResult> AssignTenantToRoom(Guid id, [FromBody] RoomAssignmentRequest request)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound("Tenant not found.");

            Flat? flat = null;
            if (request.FlatId != null)
                flat = await _context.Flats.FindAsync(request.FlatId);
            else if (request.RoomId != null)
            {
                if (Guid.TryParse(request.RoomId, out var roomGuid))
                    flat = await _context.Flats.FindAsync(roomGuid);
            }

            if (flat == null) return NotFound("Flat not found.");

            // Check if flat already has active occupancy
            var existingOcc = await _context.Occupancies
                .FirstOrDefaultAsync(o => o.FlatId == flat.FlatId && o.EndDate == null && o.TenantId != null);
            if (existingOcc != null)
                return BadRequest("This flat is already occupied.");

            var occupancy = new Occupancy
            {
                FlatId = flat.FlatId,
                TenantId = id,
                StartDate = request.StartDate != null
                    ? DateOnly.FromDateTime(request.StartDate.Value)
                    : DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _context.Occupancies.Add(occupancy);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Tenant '{tenant.Name}' assigned to {flat.RoomCode}." });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> PutTenant(Guid id, [FromBody] TenantUpdateRequest request)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name))
                tenant.Name = request.Name;
            else if (!string.IsNullOrWhiteSpace(request.FirstName))
                tenant.Name = $"{request.FirstName} {request.LastName}".Trim();

            if (request.FatherName != null) tenant.FatherName = request.FatherName;
            if (request.Phone != null) tenant.Phone = request.Phone;
            else if (request.PhoneNumber != null) tenant.Phone = request.PhoneNumber;
            if (request.Email != null) tenant.Email = request.Email;
            if (request.AadhaarNumber != null) tenant.AadhaarNumber = request.AadhaarNumber;
            if (request.PanNumber != null) tenant.PanNumber = request.PanNumber;
            if (request.PermanentAddress != null) tenant.PermanentAddress = request.PermanentAddress;
            else if (request.Address != null) tenant.PermanentAddress = request.Address;
            if (request.EmergencyContact != null) tenant.EmergencyContact = request.EmergencyContact;
            if (request.EmergencyPhone != null) tenant.EmergencyPhone = request.EmergencyPhone;
            if (request.TentativeRoomCode != null) tenant.TentativeRoomCode = request.TentativeRoomCode;
            if (request.TentativeRent.HasValue) tenant.TentativeRent = request.TentativeRent.Value;
            if (request.Notes != null) tenant.Notes = request.Notes;

            tenant.UpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tenant updated." });
        }

        // === Document management ===

        [HttpGet("{id}/documents")]
        public async Task<ActionResult> GetDocuments(Guid id)
        {
            var docs = await _context.TenantDocuments
                .Where(d => d.TenantId == id)
                .OrderByDescending(d => d.UploadedUtc)
                .Select(d => new
                {
                    d.DocumentId,
                    d.DocumentType,
                    d.FileName,
                    d.UploadedUtc
                })
                .ToListAsync();

            return Ok(docs);
        }

        [HttpPost("{id}/documents")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> UploadDocument(Guid id, [FromForm] IFormFile file, [FromForm] string? documentType)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound("Tenant not found.");

            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            // Max 10MB
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("File size exceeds 10MB limit.");

            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
                "uploads", "tenants", id.ToString());
            Directory.CreateDirectory(uploadsDir);

            // Generate unique filename
            var ext = Path.GetExtension(file.FileName);
            var storedName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, storedName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var doc = new TenantDocument
            {
                TenantId = id,
                DocumentType = documentType ?? "Other",
                FileName = file.FileName,
                FilePath = filePath
            };
            _context.TenantDocuments.Add(doc);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Document '{file.FileName}' uploaded.",
                documentId = doc.DocumentId,
                doc.DocumentType,
                doc.FileName
            });
        }

        [HttpGet("{id}/documents/{docId}/download")]
        public async Task<ActionResult> DownloadDocument(Guid id, Guid docId)
        {
            var doc = await _context.TenantDocuments
                .FirstOrDefaultAsync(d => d.DocumentId == docId && d.TenantId == id);

            if (doc == null) return NotFound();

            if (!System.IO.File.Exists(doc.FilePath))
                return NotFound("File not found on disk.");

            var bytes = await System.IO.File.ReadAllBytesAsync(doc.FilePath);
            var contentType = "application/octet-stream";
            return File(bytes, contentType, doc.FileName);
        }

        [HttpDelete("{id}/documents/{docId}")]
        public async Task<ActionResult> DeleteDocument(Guid id, Guid docId)
        {
            var doc = await _context.TenantDocuments
                .FirstOrDefaultAsync(d => d.DocumentId == docId && d.TenantId == id);

            if (doc == null) return NotFound();

            // Delete file from disk
            if (System.IO.File.Exists(doc.FilePath))
                System.IO.File.Delete(doc.FilePath);

            _context.TenantDocuments.Remove(doc);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Document '{doc.FileName}' deleted." });
        }

        // === Security Deposit Management ===

        [HttpPost("{id}/deposit")]
        public async Task<ActionResult> AddDeposit(Guid id, [FromBody] DepositRequest request)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null) return NotFound("Tenant not found.");
            if (request.Amount <= 0) return BadRequest("Amount must be positive.");

            var type = string.IsNullOrEmpty(request.Type) ? "TopUp" : request.Type;

            tenant.SecurityDeposit += request.Amount;
            tenant.UpdatedUtc = DateTime.UtcNow;

            _context.SecurityDepositTransactions.Add(new SecurityDepositTransaction
            {
                TenantId = id,
                Amount = request.Amount,
                Type = type,
                Description = request.Description ?? $"{type}: ₹{request.Amount}"
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"₹{request.Amount} added to security deposit. New balance: ₹{tenant.SecurityDeposit}",
                securityDeposit = tenant.SecurityDeposit
            });
        }

        [HttpGet("{id}/deposit-history")]
        public async Task<ActionResult> GetDepositHistory(Guid id)
        {
            var history = await _context.SecurityDepositTransactions
                .Where(dt => dt.TenantId == id)
                .OrderByDescending(dt => dt.CreatedUtc)
                .Select(dt => new
                {
                    dt.TransactionId,
                    dt.Amount,
                    dt.Type,
                    dt.Description,
                    dt.CreatedUtc
                })
                .ToListAsync();

            var tenant = await _context.Tenants.FindAsync(id);
            return Ok(new
            {
                currentBalance = tenant?.SecurityDeposit ?? 0,
                transactions = history
            });
        }
    }

    public class DepositRequest
    {
        public decimal Amount { get; set; }
        public string? Type { get; set; } // Collection, TopUp
        public string? Description { get; set; }
    }

    public class TenantCreateRequest
    {
        public string? Name { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FatherName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? PermanentAddress { get; set; }
        public string? AadhaarNumber { get; set; }
        public string? PanNumber { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? TentativeRoomCode { get; set; }
        public decimal TentativeRent { get; set; }
        public string? Notes { get; set; }
        public string? IdProofType { get; set; }
        public string? IdProofNumber { get; set; }
        public Guid? FlatId { get; set; }
        public string? RoomId { get; set; }
        public DateTime? StartDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal SecurityDeposit { get; set; }
    }

    public class RoomAssignmentRequest
    {
        public Guid? FlatId { get; set; }
        public string? RoomId { get; set; }
        public DateTime? StartDate { get; set; }
        public decimal MonthlyRent { get; set; }
        public decimal SecurityDeposit { get; set; }
    }

    public class TenantUpdateRequest
    {
        public string? Name { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FatherName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? PermanentAddress { get; set; }
        public string? AadhaarNumber { get; set; }
        public string? PanNumber { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? TentativeRoomCode { get; set; }
        public decimal? TentativeRent { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
