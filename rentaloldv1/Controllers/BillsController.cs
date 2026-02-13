using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentMangementsystem.Data;
using RentMangementsystem.Models;

namespace RentMangementsystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public BillsController(RentManagementContext context)
        {
            _context = context;
        }

        // GET: api/Bills
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bill>>> GetBills()
        {
            return await _context.Bills
                .Include(b => b.RentAgreement)
                .Include(b => b.Tenant)
                .Include(b => b.Room)
                .Include(b => b.BillItems)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        // GET: api/Bills/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Bill>> GetBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.RentAgreement)
                .Include(b => b.Tenant)
                .Include(b => b.Room)
                .Include(b => b.ElectricMeterReading)
                .Include(b => b.BillItems)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            return bill;
        }

        // GET: api/Bills/tenant/{tenantId}
        [HttpGet("tenant/{tenantId}")]
        public async Task<ActionResult<IEnumerable<Bill>>> GetBillsByTenant(int tenantId)
        {
            return await _context.Bills
                .Where(b => b.TenantId == tenantId)
                .Include(b => b.RentAgreement)
                .Include(b => b.Room)
                .Include(b => b.BillItems)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        // GET: api/Bills/outstanding
        [HttpGet("outstanding")]
        public async Task<ActionResult<IEnumerable<Bill>>> GetOutstandingBills()
        {
            return await _context.Bills
                .Where(b => b.Status != "Paid" && b.OutstandingAmount > 0)
                .Include(b => b.RentAgreement)
                .Include(b => b.Tenant)
                .Include(b => b.Room)
                .OrderBy(b => b.DueDate)
                .ToListAsync();
        }

        // GET: api/Bills/summary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetBillsSummary()
        {
            var summary = new
            {
                TotalOutstanding = await _context.Bills
                    .Where(b => b.Status != "Paid")
                    .SumAsync(b => b.OutstandingAmount),
                TotalRentDue = await _context.Bills
                    .Where(b => b.Status != "Paid")
                    .SumAsync(b => b.RentAmount - b.Payments.Where(p => p.PaymentType == "Rent").Sum(p => p.Amount)),
                TotalElectricDue = await _context.Bills
                    .Where(b => b.Status != "Paid")
                    .SumAsync(b => b.ElectricAmount - b.Payments.Where(p => p.PaymentType == "Electric").Sum(p => p.Amount)),
                TotalMiscDue = await _context.Bills
                    .Where(b => b.Status != "Paid")
                    .SumAsync(b => b.MiscAmount - b.Payments.Where(p => p.PaymentType == "Miscellaneous").Sum(p => p.Amount)),
                OverdueBills = await _context.Bills
                    .CountAsync(b => b.Status != "Paid" && b.DueDate < DateTime.Now),
                TotalAdvancePayments = await _context.Payments
                    .Where(p => p.PaymentType == "Advance")
                    .SumAsync(p => p.Amount)
            };

            return summary;
        }

        // POST: api/Bills
        [HttpPost]
        public async Task<ActionResult<Bill>> PostBill(Bill bill)
        {
            // Validate rent agreement exists
            var rentAgreement = await _context.RentAgreements
                .Include(ra => ra.Room)
                .Include(ra => ra.Tenant)
                .FirstOrDefaultAsync(ra => ra.Id == bill.RentAgreementId);

            if (rentAgreement == null)
            {
                return BadRequest("Rent agreement not found");
            }

            // Auto-populate fields
            bill.TenantId = rentAgreement.TenantId;
            bill.RoomId = rentAgreement.RoomId;
            
            // Generate bill number if not provided
            if (string.IsNullOrEmpty(bill.BillNumber))
            {
                bill.BillNumber = await GenerateBillNumber();
            }

            // Set due date if not provided
            if (bill.DueDate == default)
            {
                var dueDays = await GetBillDueDays();
                bill.DueDate = bill.BillDate.AddDays(dueDays);
            }

            // Calculate total amount
            bill.TotalAmount = bill.RentAmount + bill.ElectricAmount + bill.MiscAmount;

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBill", new { id = bill.Id }, bill);
        }

        // POST: api/Bills/generate/{rentAgreementId}
        [HttpPost("generate/{rentAgreementId}")]
        public async Task<ActionResult<Bill>> GenerateBill(int rentAgreementId, [FromBody] GenerateBillRequest request)
        {
            var rentAgreement = await _context.RentAgreements
                .Include(ra => ra.Room)
                .Include(ra => ra.Tenant)
                .FirstOrDefaultAsync(ra => ra.Id == rentAgreementId);

            if (rentAgreement == null)
            {
                return BadRequest("Rent agreement not found");
            }

            var bill = new Bill
            {
                RentAgreementId = rentAgreementId,
                TenantId = rentAgreement.TenantId,
                RoomId = rentAgreement.RoomId,
                BillNumber = await GenerateBillNumber(),
                BillDate = DateTime.Now,
                BillPeriod = request.BillPeriod ?? DateTime.Now.ToString("MMM yyyy"),
                RentAmount = rentAgreement.MonthlyRent,
                Remarks = request.Remarks
            };

            // Add electric charges if meter reading is provided
            if (request.ElectricMeterReadingId.HasValue)
            {
                var meterReading = await _context.ElectricMeterReadings
                    .FindAsync(request.ElectricMeterReadingId.Value);
                
                if (meterReading != null)
                {
                    bill.ElectricMeterReadingId = meterReading.Id;
                    bill.ElectricAmount = meterReading.ElectricCharges;
                    meterReading.IsBilled = true;
                }
            }

            // Add miscellaneous items
            bill.MiscAmount = request.MiscellaneousItems?.Sum(item => item.Amount) ?? 0;

            // Calculate total
            bill.TotalAmount = bill.RentAmount + bill.ElectricAmount + bill.MiscAmount;

            // Set due date
            var dueDays = await GetBillDueDays();
            bill.DueDate = bill.BillDate.AddDays(dueDays);

            _context.Bills.Add(bill);

            // Add bill items for miscellaneous charges
            if (request.MiscellaneousItems != null)
            {
                foreach (var item in request.MiscellaneousItems)
                {
                    var billItem = new BillItem
                    {
                        Bill = bill,
                        ItemType = "Miscellaneous",
                        Description = item.Description,
                        Amount = item.Amount,
                        Remarks = item.Remarks
                    };
                    _context.BillItems.Add(billItem);
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBill", new { id = bill.Id }, bill);
        }

        // PUT: api/Bills/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBill(int id, Bill bill)
        {
            if (id != bill.Id)
            {
                return BadRequest();
            }

            // Recalculate total amount
            bill.TotalAmount = bill.RentAmount + bill.ElectricAmount + bill.MiscAmount;

            _context.Entry(bill).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BillExists(id))
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

        // DELETE: api/Bills/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
            {
                return NotFound();
            }

            if (bill.Payments.Any())
            {
                return BadRequest("Cannot delete bill with associated payments");
            }

            _context.Bills.Remove(bill);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<string> GenerateBillNumber()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var prefix = $"BILL{year:D4}{month:D2}";
            
            var lastBill = await _context.Bills
                .Where(b => b.BillNumber.StartsWith(prefix))
                .OrderByDescending(b => b.BillNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastBill != null)
            {
                var lastSequence = lastBill.BillNumber.Substring(prefix.Length);
                if (int.TryParse(lastSequence, out int parsed))
                {
                    sequence = parsed + 1;
                }
            }

            return $"{prefix}{sequence:D4}";
        }

        private async Task<int> GetBillDueDays()
        {
            var config = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == "BillDueDays");
            
            if (config != null && int.TryParse(config.ConfigValue, out int days))
            {
                return days;
            }
            
            return 15; // Default value
        }

        private bool BillExists(int id)
        {
            return _context.Bills.Any(e => e.Id == id);
        }
    }

    public class GenerateBillRequest
    {
        public string? BillPeriod { get; set; }
        public int? ElectricMeterReadingId { get; set; }
        public List<MiscItem>? MiscellaneousItems { get; set; }
        public string? Remarks { get; set; }
    }

    public class MiscItem
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Remarks { get; set; }
    }
}