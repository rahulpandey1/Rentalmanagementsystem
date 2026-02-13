using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentMangementsystem.Data;
using RentMangementsystem.Models;

namespace RentMangementsystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public PaymentsController(RentManagementContext context)
        {
            _context = context;
        }

        // GET: api/Payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            return await _context.Payments
                .Include(p => p.RentAgreement)
                    .ThenInclude(ra => ra.Property)
                .Include(p => p.Tenant)
                .ToListAsync();
        }

        // GET: api/Payments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.RentAgreement)
                    .ThenInclude(ra => ra.Property)
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return payment;
        }

        // GET: api/Payments/tenant/5
        [HttpGet("tenant/{tenantId}")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByTenant(int tenantId)
        {
            return await _context.Payments
                .Where(p => p.TenantId == tenantId)
                .Include(p => p.RentAgreement)
                    .ThenInclude(ra => ra.Property)
                .Include(p => p.Tenant)
                .ToListAsync();
        }

        // GET: api/Payments/overdue
        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetOverduePayments()
        {
            return await _context.Payments
                .Where(p => p.Status == "Pending" && p.DueDate < DateTime.Now)
                .Include(p => p.RentAgreement)
                    .ThenInclude(ra => ra.Property)
                .Include(p => p.Tenant)
                .ToListAsync();
        }

        // POST: api/Payments
        [HttpPost]
        public async Task<ActionResult<Payment>> PostPayment(Payment payment)
        {
            // Validate that rent agreement exists
            var rentAgreement = await _context.RentAgreements.FindAsync(payment.RentAgreementId);
            if (rentAgreement == null)
            {
                return BadRequest("Rent agreement not found");
            }

            // Validate that tenant exists
            var tenant = await _context.Tenants.FindAsync(payment.TenantId);
            if (tenant == null)
            {
                return BadRequest("Tenant not found");
            }

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPayment", new { id = payment.Id }, payment);
        }

        // PUT: api/Payments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPayment(int id, Payment payment)
        {
            if (id != payment.Id)
            {
                return BadRequest();
            }

            _context.Entry(payment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentExists(id))
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

        // PUT: api/Payments/5/markpaid
        [HttpPut("{id}/markpaid")]
        public async Task<IActionResult> MarkPaymentAsPaid(int id, [FromBody] string? transactionReference = null)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            payment.Status = "Paid";
            payment.PaymentDate = DateTime.Now;
            if (!string.IsNullOrEmpty(transactionReference))
            {
                payment.TransactionReference = transactionReference;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Payments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.Id == id);
        }
    }
}