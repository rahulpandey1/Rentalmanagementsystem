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
    [AuditLog("Payment", "Bill")]
    public class BillsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public BillsController(RentManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Generate next sequential invoice number (INV-000001, INV-000002, ...)
        /// </summary>
        private async Task<string> GenerateNextInvoiceNumber()
        {
            var maxInvoice = await _context.MonthlyLedgers
                .Where(l => l.InvoiceNumber != null)
                .OrderByDescending(l => l.InvoiceNumber)
                .Select(l => l.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (maxInvoice != null && maxInvoice.StartsWith("INV-") && 
                int.TryParse(maxInvoice.Substring(4), out int currentNum))
            {
                nextNum = currentNum + 1;
            }

            return $"INV-{nextNum:D6}";
        }

        /// <summary>
        /// Get ledger entries for a specific month/year period
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetBills(int? month, int? year)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var period = new DateOnly(targetYear, targetMonth, 1);

            var ledgers = await _context.MonthlyLedgers
                .Include(l => l.Flat)
                .Include(l => l.Tenant)
                .Where(l => l.Period == period)
                .OrderBy(l => l.SerialNumber)
                .ThenBy(l => l.Flat!.RoomCode)
                .ToListAsync();

            // If no data for selected period, show all flats as vacant
            if (!ledgers.Any())
            {
                var flats = await _context.Flats.OrderBy(f => f.RoomCode).ToListAsync();
                return Ok(flats.Select(f => new
                {
                    Id = f.FlatId,
                    TenantName = "VACANT",
                    RoomNumber = f.RoomCode,
                    BillPeriod = period.ToString("MMMM yyyy"),
                    TotalAmount = 0m,
                    PaidAmount = 0m,
                    ClosingBalance = 0m,
                    Status = "No Data"
                }));
            }

            var result = ledgers.Select(l => new
            {
                Id = l.MonthlyLedgerId,
                l.InvoiceNumber,
                TenantName = l.Tenant?.Name ?? "VACANT",
                RoomNumber = l.Flat?.RoomCode,
                BillPeriod = l.Period.ToString("MMMM yyyy"),
                TotalAmount = l.TotalDue,
                PaidAmount = l.AmountPaid,
                ClosingBalance = l.ClosingBalance,
                Status = l.ClosingBalance <= 0 ? "Paid" : "Pending",
                PaymentDate = l.PaymentDate?.ToString("dd-MMM-yyyy"),
                TenantId = l.TenantId,
                SecurityDeposit = l.Tenant != null ? l.Tenant.SecurityDeposit : 0,
                // Details for Invoice
                MonthlyRent = l.MonthlyRent,
                ElectricAmount = l.ElecCost, 
                MiscAmount = l.MiscRent,
                Carryover = l.Carryover,
                Remarks = l.Remarks,
                
                // Meter Readings
                ElecPrev = l.ElecPrev,
                ElecNew = l.ElecNew,
                ElecUnits = l.ElecUnits,
                ElecRate = l.ElecRate,
                MiscChargeName = l.MiscChargeName
            });

            return Ok(result);
        }

        /// <summary>
        /// Get outstanding ledger entries for a specific month/year
        /// </summary>
        [HttpGet("outstanding")]
        public async Task<ActionResult<IEnumerable<object>>> GetOutstanding(int? month, int? year)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var period = new DateOnly(targetYear, targetMonth, 1);

            var ledgers = await _context.MonthlyLedgers
                .Include(l => l.Flat)
                .Include(l => l.Tenant)
                .Where(l => l.Period == period && l.ClosingBalance > 0)
                .OrderByDescending(l => l.ClosingBalance)
                .ToListAsync();

            var result = ledgers.Select(l => new
            {
                Id = l.MonthlyLedgerId,
                l.InvoiceNumber,
                TenantName = l.Tenant?.Name ?? "VACANT",
                RoomNumber = l.Flat?.RoomCode,
                BillPeriod = l.Period.ToString("MMMM yyyy"),
                TotalAmount = l.TotalDue,
                PaidAmount = l.AmountPaid,
                ClosingBalance = l.ClosingBalance,
                Status = "Pending",

                // Details for Invoice
                MonthlyRent = l.MonthlyRent,
                ElectricAmount = l.ElecCost,
                MiscAmount = l.MiscRent,
                Carryover = l.Carryover,
                Remarks = l.Remarks
            });

            return Ok(result);
        }

        /// <summary>
        /// Generate monthly bills for all active tenants
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateBills([FromQuery] int? month, [FromQuery] int? year)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var period = new DateOnly(targetYear, targetMonth, 1);
            var prevPeriod = period.AddMonths(-1);

            // 1. Get all active occupancies for this period
            // Active means: StartDate <= Period AND (EndDate is NULL OR EndDate >= Period)
            var activeOccupancies = await _context.Occupancies
                .Include(o => o.Flat)
                .Include(o => o.Tenant)
                .Where(o => o.StartDate < period.AddMonths(1) && (o.EndDate == null || o.EndDate >= period))
                .ToListAsync();

            int generatedCount = 0;

            foreach (var occupancy in activeOccupancies)
            {
                // 2. Check if ledger already handles this period
                var existing = await _context.MonthlyLedgers
                    .FirstOrDefaultAsync(l => l.Period == period && l.FlatId == occupancy.FlatId);

                if (existing != null) continue; // Skip if already exists

                // 3. Get previous month's ledger to copy data
                var prevLedger = await _context.MonthlyLedgers
                    .FirstOrDefaultAsync(l => l.Period == prevPeriod && l.FlatId == occupancy.FlatId);

                // 4. Create new ledger
                var newLedger = new MonthlyLedger
                {
                    Period = period,
                    FlatId = occupancy.FlatId,
                    TenantId = occupancy.TenantId,
                    DateOfAllotment = occupancy.StartDate,
                    
                    // Copy from previous or defaults
                    MonthlyRent = prevLedger?.MonthlyRent ?? 0, 
                    ElectricSecurity = prevLedger?.ElectricSecurity ?? 0,
                    ElecPrev = prevLedger?.ElecNew ?? 0,
                    ElecNew = prevLedger?.ElecNew ?? 0, 
                    ElecRate = prevLedger?.ElecRate ?? 12.0m, 
                    MiscRent = prevLedger?.MiscRent ?? 0,
                    MiscChargeName = prevLedger?.MiscChargeName,
                    
                    // Carryover is previous closing balance
                    Carryover = prevLedger?.ClosingBalance ?? 0,
                };

                // Calculate totals
                // Total Due = Arrears + Rent + Misc + (Elec - paid separately usually, but here part of bill?)
                // Assuming ElecCost is calculated when readings are entered. For now just Rent + Carryover
                newLedger.TotalDue = newLedger.Carryover + newLedger.MonthlyRent + newLedger.MiscRent;
                newLedger.ClosingBalance = newLedger.TotalDue; // Assumes 0 paid initially
                newLedger.InvoiceNumber = await GenerateNextInvoiceNumber();

                _context.MonthlyLedgers.Add(newLedger);
                generatedCount++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Generated {generatedCount} bills for {period:MMMM yyyy}", count = generatedCount });
        }

        [HttpGet("preview")]
        public async Task<ActionResult<IEnumerable<object>>> GetBillPreview(int? month, int? year)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var period = new DateOnly(targetYear, targetMonth, 1);
            var prevPeriod = period.AddMonths(-1);

            var activeOccupancies = await _context.Occupancies
                .Include(o => o.Flat)
                .Include(o => o.Tenant)
                .Where(o => o.StartDate < period.AddMonths(1) && (o.EndDate == null || o.EndDate >= period))
                .OrderBy(o => o.Flat!.RoomCode)
                .ToListAsync();

            var previews = new List<object>();

            foreach (var occupancy in activeOccupancies)
            {
                var existing = await _context.MonthlyLedgers
                    .FirstOrDefaultAsync(l => l.Period == period && l.FlatId == occupancy.FlatId);

                var prevLedger = await _context.MonthlyLedgers
                    .FirstOrDefaultAsync(l => l.Period == prevPeriod && l.FlatId == occupancy.FlatId);

                previews.Add(new
                {
                    FlatId = occupancy.FlatId,
                    TenantId = occupancy.TenantId,
                    RoomNumber = occupancy.Flat?.RoomCode,
                    TenantName = occupancy.Tenant?.Name,
                    MonthlyRent = existing?.MonthlyRent ?? prevLedger?.MonthlyRent ?? 0,
                    ElecPrev = existing?.ElecPrev ?? prevLedger?.ElecNew ?? 0,
                    ElecNew = existing?.ElecNew ?? prevLedger?.ElecNew ?? 0, // Default to prev reading if new
                    ElecRate = existing?.ElecRate ?? prevLedger?.ElecRate ?? 12.0m,
                    MiscRent = existing?.MiscRent ?? prevLedger?.MiscRent ?? 0,
                    MiscChargeName = existing?.MiscChargeName ?? prevLedger?.MiscChargeName,
                    Carryover = existing?.Carryover ?? prevLedger?.ClosingBalance ?? 0,
                    IsGenerated = existing != null
                });
            }

            return Ok(previews);
        }

        [HttpPost("generate-batch")]
        public async Task<IActionResult> GenerateBatchBills([FromBody] BatchGenerateRequest request)
        {
            var period = new DateOnly(request.Year, request.Month, 1);
            int count = 0;

            foreach (var item in request.Bills)
            {
                var ledger = await _context.MonthlyLedgers
                    .FirstOrDefaultAsync(l => l.Period == period && l.FlatId == item.FlatId);

                if (ledger == null)
                {
                    ledger = new MonthlyLedger
                    {
                        Period = period,
                        FlatId = item.FlatId,
                        TenantId = item.TenantId,
                        MonthlyLedgerId = Guid.NewGuid(),
                        DateOfAllotment = period, // approximation
                        InvoiceNumber = await GenerateNextInvoiceNumber()
                    };
                    _context.MonthlyLedgers.Add(ledger);
                }
                else if (string.IsNullOrEmpty(ledger.InvoiceNumber))
                {
                    ledger.InvoiceNumber = await GenerateNextInvoiceNumber();
                }

                // Update fields from preview
                ledger.MonthlyRent = item.MonthlyRent;
                ledger.ElecPrev = item.ElecPrev;
                ledger.ElecNew = item.ElecNew;
                ledger.ElecRate = item.ElecRate;
                ledger.ElecUnits = (ledger.ElecNew - ledger.ElecPrev);
                if (ledger.ElecUnits < 0) ledger.ElecUnits = 0;
                
                ledger.ElecCost = ledger.ElecUnits * ledger.ElecRate;
                
                ledger.MiscRent = item.MiscRent;
                ledger.MiscChargeName = item.MiscChargeName;
                ledger.Carryover = item.Carryover; 
                ledger.Remarks = string.Empty; // TODO: Add remarks to batch?

                ledger.TotalDue = ledger.Carryover + ledger.MonthlyRent + ledger.ElecCost + ledger.MiscRent;
                ledger.ClosingBalance = ledger.TotalDue - ledger.AmountPaid;
                ledger.UpdatedUtc = DateTime.UtcNow;

                count++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Processed {count} bills", count });
        }

        public class BatchGenerateRequest
        {
            public int Month { get; set; }
            public int Year { get; set; }
            public List<BatchBillItem> Bills { get; set; } = new();
        }

        public class BatchBillItem
        {
            public Guid FlatId { get; set; }
            public Guid? TenantId { get; set; }
            public decimal MonthlyRent { get; set; }
            public decimal ElecPrev { get; set; }
            public decimal ElecNew { get; set; }
            public decimal ElecRate { get; set; }
            public decimal MiscRent { get; set; }
            public string? MiscChargeName { get; set; }
            public decimal Carryover { get; set; }
        }

        /// <summary>
        /// Update meter reading and recalculate bill
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBill([FromRoute] Guid id, [FromBody] BillUpdateModel model)
        {
            var ledger = await _context.MonthlyLedgers.FindAsync(id);
            if (ledger == null) return NotFound();

            // Update fields
            if (model.CurrentReading.HasValue)
            {
                ledger.ElecNew = model.CurrentReading.Value;
                ledger.ElecUnits = ledger.ElecNew - ledger.ElecPrev;
                if (ledger.ElecUnits < 0) ledger.ElecUnits = 0; 
                
                ledger.ElecCost = ledger.ElecUnits * ledger.ElecRate;
            }

            if (model.MonthlyRent.HasValue)
            {
                ledger.MonthlyRent = model.MonthlyRent.Value;
            }

            if (model.MiscAmount.HasValue)
            {
                ledger.MiscRent = model.MiscAmount.Value;
            }

            if (model.MiscChargeName != null)
            {
                ledger.MiscChargeName = model.MiscChargeName;
            }

            if (model.Remarks != null)
            {
                ledger.Remarks = model.Remarks;
            }

            // Recalculate Totals
            // Total = Arrears + Rent + Electric + Misc
            ledger.TotalDue = ledger.Carryover + ledger.MonthlyRent + ledger.ElecCost + ledger.MiscRent;
            
            // Recalculate Closing Balance (Total - Paid)
            ledger.ClosingBalance = ledger.TotalDue - ledger.AmountPaid;

            ledger.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Bill updated successfully", ledger });
        }

        /// <summary>
        /// Record a payment against a bill (supports partial, exact, and overpayment)
        /// </summary>
        [HttpPut("{id}/record-payment")]
        public async Task<IActionResult> RecordPayment([FromRoute] Guid id, [FromBody] RecordPaymentModel model)
        {
            var ledger = await _context.MonthlyLedgers
                .Include(l => l.Flat)
                .Include(l => l.Tenant)
                .FirstOrDefaultAsync(l => l.MonthlyLedgerId == id);
            if (ledger == null) return NotFound();

            // Split payment: rentAmount goes to bill, depositAmount goes to security deposit
            var depositAmount = model.SecurityDepositAmount;
            var rentAmount = model.AmountPaid;

            // Apply rent payment
            ledger.AmountPaid = rentAmount;
            ledger.PaymentDate = model.PaymentDate.HasValue 
                ? DateOnly.FromDateTime(model.PaymentDate.Value) 
                : DateOnly.FromDateTime(DateTime.UtcNow);
            
            // ClosingBalance = TotalDue - AmountPaid
            ledger.ClosingBalance = ledger.TotalDue - ledger.AmountPaid;
            ledger.UpdatedUtc = DateTime.UtcNow;

            // Handle security deposit portion
            if (depositAmount > 0 && ledger.TenantId.HasValue)
            {
                var tenant = await _context.Tenants.FindAsync(ledger.TenantId.Value);
                if (tenant != null)
                {
                    tenant.SecurityDeposit += depositAmount;
                    tenant.UpdatedUtc = DateTime.UtcNow;

                    _context.SecurityDepositTransactions.Add(new SecurityDepositTransaction
                    {
                        TenantId = tenant.TenantId,
                        Amount = depositAmount,
                        Type = "TopUp",
                        Description = $"Payment split: \u20b9{depositAmount} to deposit (bill {ledger.InvoiceNumber ?? ledger.Period.ToString("MMM yyyy")})"
                    });
                }
            }

            // Payment record for audit trail
            var paymentRecord = await _context.Payments
                .FirstOrDefaultAsync(p => p.Period == ledger.Period && p.FlatId == ledger.FlatId);
            if (paymentRecord == null)
            {
                paymentRecord = new Payment
                {
                    Period = ledger.Period,
                    FlatId = ledger.FlatId,
                    TenantId = ledger.TenantId,
                    Amount = rentAmount + depositAmount,
                    PaymentDate = ledger.PaymentDate,
                    Source = "Manual"
                };
                _context.Payments.Add(paymentRecord);
            }
            else
            {
                paymentRecord.Amount = rentAmount + depositAmount;
                paymentRecord.PaymentDate = ledger.PaymentDate;
            }

            await _context.SaveChangesAsync();

            var status = ledger.ClosingBalance <= 0 ? "Paid" : "Pending";
            var depositNote = depositAmount > 0 ? $" + \u20b9{depositAmount} to deposit" : "";

            return Ok(new { 
                message = $"Payment of \u20b9{rentAmount} recorded for {ledger.Flat?.RoomCode}. Status: {status}{depositNote}",
                invoiceNumber = ledger.InvoiceNumber,
                totalDue = ledger.TotalDue,
                amountPaid = ledger.AmountPaid,
                closingBalance = ledger.ClosingBalance,
                status
            });
        }

        /// <summary>
        /// Adjust bill from tenant's security deposit
        /// </summary>
        [HttpPost("{id}/adjust-from-deposit")]
        public async Task<IActionResult> AdjustFromDeposit([FromRoute] Guid id, [FromBody] AdjustDepositModel model)
        {
            var ledger = await _context.MonthlyLedgers
                .Include(l => l.Flat)
                .Include(l => l.Tenant)
                .FirstOrDefaultAsync(l => l.MonthlyLedgerId == id);
            if (ledger == null) return NotFound();
            if (ledger.TenantId == null) return BadRequest("No tenant associated with this bill.");

            var tenant = await _context.Tenants.FindAsync(ledger.TenantId.Value);
            if (tenant == null) return NotFound("Tenant not found.");

            var adjustAmount = model.Amount > 0 ? model.Amount : ledger.ClosingBalance;
            if (adjustAmount <= 0) return BadRequest("Nothing to adjust.");
            if (adjustAmount > tenant.SecurityDeposit)
                return BadRequest($"Insufficient deposit balance. Available: \u20b9{tenant.SecurityDeposit}");

            // Deduct from deposit
            tenant.SecurityDeposit -= adjustAmount;
            tenant.UpdatedUtc = DateTime.UtcNow;

            // Credit to bill
            ledger.AmountPaid += adjustAmount;
            ledger.ClosingBalance = ledger.TotalDue - ledger.AmountPaid;
            ledger.UpdatedUtc = DateTime.UtcNow;

            // Record transaction
            _context.SecurityDepositTransactions.Add(new SecurityDepositTransaction
            {
                TenantId = tenant.TenantId,
                Amount = -adjustAmount,
                Type = "Adjustment",
                Description = $"Adjusted \u20b9{adjustAmount} against {ledger.Flat?.RoomCode} {ledger.Period:MMM yyyy}"
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"\u20b9{adjustAmount} adjusted from deposit. Deposit balance: \u20b9{tenant.SecurityDeposit}",
                adjustedAmount = adjustAmount,
                securityDeposit = tenant.SecurityDeposit,
                billBalance = ledger.ClosingBalance
            });
        }

        public class RecordPaymentModel
        {
            public decimal AmountPaid { get; set; }
            public decimal SecurityDepositAmount { get; set; } // Optional: portion going to deposit
            public DateTime? PaymentDate { get; set; }
        }

        public class AdjustDepositModel
        {
            public decimal Amount { get; set; } // 0 = use full closing balance
        }

        public class BillUpdateModel
        {
            public decimal? CurrentReading { get; set; }
            public decimal? MonthlyRent { get; set; }
            public decimal? MiscAmount { get; set; }
            public string? MiscChargeName { get; set; }
            public string? Remarks { get; set; }
        }
    }
}
