using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyAPI.Data;
using RentalPropertyAPI.DTOs;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public PaymentsController(RentalDbContext context)
        {
            _context = context;
        }

        // GET: api/payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments(
            int? tenantId = null,
            PaymentType? type = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.Payments
                .Include(p => p.Tenant)
                    .ThenInclude(t => t.Room)
                .AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(p => p.TenantId == tenantId.Value);

            if (type.HasValue)
                query = query.Where(p => p.Type == type.Value);

            if (fromDate.HasValue)
                query = query.Where(p => p.PaymentDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(p => p.PaymentDate <= toDate.Value);

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    TenantId = p.TenantId,
                    TenantName = p.Tenant.FullName,
                    RoomNumber = p.Tenant.Room.RoomNumber,
                    Type = p.Type,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Method = p.Method,
                    TransactionReference = p.TransactionReference,
                    Description = p.Description,
                    BillingPeriodStart = p.BillingPeriodStart,
                    BillingPeriodEnd = p.BillingPeriodEnd,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(payments);
        }

        // GET: api/payments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDto>> GetPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Tenant)
                    .ThenInclude(t => t.Room)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            var paymentDto = new PaymentDto
            {
                Id = payment.Id,
                TenantId = payment.TenantId,
                TenantName = payment.Tenant.FullName,
                RoomNumber = payment.Tenant.Room.RoomNumber,
                Type = payment.Type,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                Method = payment.Method,
                TransactionReference = payment.TransactionReference,
                Description = payment.Description,
                BillingPeriodStart = payment.BillingPeriodStart,
                BillingPeriodEnd = payment.BillingPeriodEnd,
                CreatedAt = payment.CreatedAt
            };

            return Ok(paymentDto);
        }

        // POST: api/payments
        [HttpPost]
        public async Task<ActionResult<PaymentDto>> CreatePayment(CreatePaymentDto createDto)
        {
            // Verify tenant exists
            var tenant = await _context.Tenants
                .Include(t => t.Room)
                .FirstOrDefaultAsync(t => t.Id == createDto.TenantId);

            if (tenant == null)
                return BadRequest("Tenant not found");

            var payment = new Payment
            {
                TenantId = createDto.TenantId,
                Type = createDto.Type,
                Amount = createDto.Amount,
                PaymentDate = createDto.PaymentDate,
                Method = createDto.Method,
                TransactionReference = createDto.TransactionReference,
                Description = createDto.Description,
                BillingPeriodStart = createDto.BillingPeriodStart,
                BillingPeriodEnd = createDto.BillingPeriodEnd,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var paymentDto = new PaymentDto
            {
                Id = payment.Id,
                TenantId = payment.TenantId,
                TenantName = tenant.FullName,
                RoomNumber = tenant.Room.RoomNumber,
                Type = payment.Type,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                Method = payment.Method,
                TransactionReference = payment.TransactionReference,
                Description = payment.Description,
                BillingPeriodStart = payment.BillingPeriodStart,
                BillingPeriodEnd = payment.BillingPeriodEnd,
                CreatedAt = payment.CreatedAt
            };

            return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, paymentDto);
        }

        // DELETE: api/payments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound();

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/payments/summary
        [HttpGet("summary")]
        public async Task<ActionResult<PaymentSummaryDto>> GetPaymentSummary(
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            var startDate = fromDate ?? DateTime.Now.AddMonths(-1);
            var endDate = toDate ?? DateTime.Now;

            var payments = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .ToListAsync();

            var summary = new PaymentSummaryDto
            {
                TotalRentCollected = payments.Where(p => p.Type == PaymentType.Rent).Sum(p => p.Amount),
                TotalElectricityPayments = payments.Where(p => p.Type == PaymentType.Electricity).Sum(p => p.Amount),
                TotalSecurityDeposits = payments.Where(p => p.Type == PaymentType.SecurityDeposit).Sum(p => p.Amount),
                TotalMaintenancePayments = payments.Where(p => p.Type == PaymentType.Maintenance).Sum(p => p.Amount),
                TotalMiscellaneousPayments = payments.Where(p => p.Type == PaymentType.Miscellaneous).Sum(p => p.Amount),
                FromDate = startDate,
                ToDate = endDate
            };

            summary.GrandTotal = summary.TotalRentCollected + summary.TotalElectricityPayments + 
                               summary.TotalSecurityDeposits + summary.TotalMaintenancePayments + 
                               summary.TotalMiscellaneousPayments;

            return Ok(summary);
        }

        // GET: api/payments/tenant/5/summary
        [HttpGet("tenant/{tenantId}/summary")]
        public async Task<ActionResult<object>> GetTenantPaymentSummary(int tenantId)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Room)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
                return NotFound();

            var totalRentPaid = tenant.Payments.Where(p => p.Type == PaymentType.Rent).Sum(p => p.Amount);
            var totalElectricityPaid = tenant.Payments.Where(p => p.Type == PaymentType.Electricity).Sum(p => p.Amount);
            var totalMaintenancePaid = tenant.Payments.Where(p => p.Type == PaymentType.Maintenance).Sum(p => p.Amount);
            var totalSecurityDeposit = tenant.Payments.Where(p => p.Type == PaymentType.SecurityDeposit).Sum(p => p.Amount);
            var totalMiscellaneous = tenant.Payments.Where(p => p.Type == PaymentType.Miscellaneous).Sum(p => p.Amount);

            var lastPayment = tenant.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

            return Ok(new
            {
                TenantName = tenant.FullName,
                RoomNumber = tenant.Room.RoomNumber,
                TotalRentPaid = totalRentPaid,
                TotalElectricityPaid = totalElectricityPaid,
                TotalMaintenancePaid = totalMaintenancePaid,
                TotalSecurityDeposit = totalSecurityDeposit,
                TotalMiscellaneous = totalMiscellaneous,
                GrandTotal = totalRentPaid + totalElectricityPaid + totalMaintenancePaid + totalSecurityDeposit + totalMiscellaneous,
                LastPaymentDate = lastPayment?.PaymentDate,
                LastPaymentAmount = lastPayment?.Amount,
                PaymentCount = tenant.Payments.Count
            });
        }

        // GET: api/payments/monthly-report
        [HttpGet("monthly-report")]
        public async Task<ActionResult<object>> GetMonthlyReport(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var payments = await _context.Payments
                .Include(p => p.Tenant)
                    .ThenInclude(t => t.Room)
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .ToListAsync();

            var groupedByRoom = payments
                .GroupBy(p => new { p.Tenant.RoomId, p.Tenant.Room.RoomNumber, p.Tenant.FullName })
                .Select(g => new
                {
                    RoomNumber = g.Key.RoomNumber,
                    TenantName = g.Key.FullName,
                    RentCollected = g.Where(p => p.Type == PaymentType.Rent).Sum(p => p.Amount),
                    ElectricityCollected = g.Where(p => p.Type == PaymentType.Electricity).Sum(p => p.Amount),
                    MaintenanceCollected = g.Where(p => p.Type == PaymentType.Maintenance).Sum(p => p.Amount),
                    TotalCollected = g.Sum(p => p.Amount)
                })
                .OrderBy(r => r.RoomNumber)
                .ToList();

            var totalRentCollected = payments.Where(p => p.Type == PaymentType.Rent).Sum(p => p.Amount);
            var totalElectricityCollected = payments.Where(p => p.Type == PaymentType.Electricity).Sum(p => p.Amount);
            var totalMaintenanceCollected = payments.Where(p => p.Type == PaymentType.Maintenance).Sum(p => p.Amount);

            return Ok(new
            {
                Month = month,
                Year = year,
                TotalRentCollected = totalRentCollected,
                TotalElectricityCollected = totalElectricityCollected,
                TotalMaintenanceCollected = totalMaintenanceCollected,
                GrandTotal = payments.Sum(p => p.Amount),
                RoomWiseReport = groupedByRoom
            });
        }
    }
}