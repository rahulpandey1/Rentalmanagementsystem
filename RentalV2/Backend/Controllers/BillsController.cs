using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Models;

namespace RentalBackend.Controllers
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bill>>> GetBills()
        {
            return await _context.Bills
                .Include(b => b.Tenant)
                .Include(b => b.Room)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
        }

        [HttpGet("outstanding")]
        public async Task<ActionResult<IEnumerable<Bill>>> GetOutstandingBills()
        {
            return await _context.Bills
                .Where(b => b.Status != "Paid" && b.TotalAmount > b.PaidAmount)
                .Include(b => b.Tenant)
                .Include(b => b.Room)
                .OrderByDescending(b => b.DueDate)
                .ToListAsync();
        }

        [HttpPost("generate")]
        public async Task<ActionResult<Bill>> GenerateBill([FromBody] BillGenerateRequest request)
        {
            var room = await _context.Rooms
                .Include(r => r.RentAgreements.Where(ra => ra.IsActive))
                .ThenInclude(ra => ra.Tenant)
                .FirstOrDefaultAsync(r => r.Id == request.RoomId);

            if (room == null)
                return BadRequest("Room not found");

            var activeAgreement = room.RentAgreements.FirstOrDefault(ra => ra.IsActive);
            if (activeAgreement == null)
                return BadRequest("No active tenant in this room");

            // Get any pending amount from previous bills
            var pendingAmount = await _context.Bills
                .Where(b => b.RoomId == room.Id && b.TenantId == activeAgreement.TenantId && b.Status != "Paid")
                .SumAsync(b => b.TotalAmount - b.PaidAmount);

            var electricAmount = request.ElectricAmount ?? 0;

            // If meter readings provided, calculate electric charges
            if (request.CurrentReading.HasValue && request.CurrentReading > 0)
            {
                var prevReading = request.PreviousReading ?? room.LastMeterReading ?? 0;
                var unitsConsumed = request.CurrentReading.Value - prevReading;

                var rateSetting = await _context.SystemConfigurations.FirstOrDefaultAsync(s => s.ConfigKey == "ElectricRatePerUnit");
                var rate = decimal.Parse(rateSetting?.ConfigValue ?? "8.0");
                electricAmount = unitsConsumed * rate;

                // Record meter reading
                var meterReading = new ElectricMeterReading
                {
                    RoomId = room.Id,
                    PreviousReading = prevReading,
                    CurrentReading = request.CurrentReading.Value,
                    ReadingDate = DateTime.UtcNow,
                    ElectricCharges = electricAmount,
                    IsBilled = true,
                    Remarks = $"Bill period: {request.BillPeriod}",
                    CreatedDate = DateTime.UtcNow
                };
                _context.ElectricMeterReadings.Add(meterReading);

                // Update room's last reading
                room.LastMeterReading = request.CurrentReading.Value;
                room.LastReadingDate = DateTime.UtcNow;
            }

            var rentAmount = request.RentAmount ?? room.MonthlyRent;
            var miscAmount = request.MiscAmount ?? 0;
            var totalAmount = rentAmount + electricAmount + miscAmount;

            // Add pending amount to remarks if any
            var remarks = request.Remarks ?? "";
            if (pendingAmount > 0)
            {
                remarks = $"Previous pending: ₹{pendingAmount:N0}. " + remarks;
            }

            var bill = new Bill
            {
                BillNumber = "BILL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                RoomId = room.Id,
                TenantId = activeAgreement.TenantId,
                BillDate = DateTime.UtcNow,
                BillPeriod = request.BillPeriod ?? DateTime.UtcNow.ToString("MMMM yyyy"),
                DueDate = request.DueDate ?? DateTime.UtcNow.AddDays(7),
                RentAmount = rentAmount,
                ElectricAmount = electricAmount,
                MiscAmount = miscAmount,
                TotalAmount = totalAmount,
                PaidAmount = 0,
                Status = "Pending",
                Remarks = remarks,
                CreatedDate = DateTime.UtcNow
            };
            
            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBills", new { id = bill.Id }, bill);
        }

        /// <summary>
        /// Generate bills for all occupied rooms with meter readings
        /// </summary>
        [HttpPost("generate-bulk-with-readings")]
        public async Task<ActionResult<object>> GenerateBulkBillsWithReadings([FromBody] BulkBillWithReadingsRequest request)
        {
            var billPeriod = request.BillPeriod ?? DateTime.UtcNow.ToString("MMMM yyyy");
            var dueDate = request.DueDate ?? DateTime.UtcNow.AddDays(7);

            var rateSetting = await _context.SystemConfigurations.FirstOrDefaultAsync(s => s.ConfigKey == "ElectricRatePerUnit");
            var rate = decimal.Parse(rateSetting?.ConfigValue ?? "8.0");

            var generatedBills = new List<object>();
            var skipped = 0;
            var errors = new List<string>();

            foreach (var roomReading in request.RoomReadings)
            {
                var room = await _context.Rooms
                    .Include(r => r.RentAgreements.Where(ra => ra.IsActive))
                    .ThenInclude(ra => ra.Tenant)
                    .FirstOrDefaultAsync(r => r.Id == roomReading.RoomId);

                if (room == null)
                {
                    errors.Add($"Room ID {roomReading.RoomId} not found");
                    continue;
                }

                var activeAgreement = room.RentAgreements.FirstOrDefault(ra => ra.IsActive);
                if (activeAgreement == null)
                {
                    skipped++;
                    continue;
                }

                // Check if bill already exists
                var existingBill = await _context.Bills
                    .AnyAsync(b => b.RoomId == room.Id && b.BillPeriod == billPeriod);

                if (existingBill)
                {
                    skipped++;
                    continue;
                }

                // Calculate electric charges
                var prevReading = roomReading.PreviousReading ?? room.LastMeterReading ?? 0;
                var currReading = roomReading.CurrentReading;
                var unitsConsumed = currReading - prevReading;
                var electricCharges = unitsConsumed * rate;

                // Record meter reading
                var meterReading = new ElectricMeterReading
                {
                    RoomId = room.Id,
                    PreviousReading = prevReading,
                    CurrentReading = currReading,
                    ReadingDate = DateTime.UtcNow,
                    ElectricCharges = electricCharges,
                    IsBilled = true,
                    Remarks = $"Bulk bill: {billPeriod}",
                    CreatedDate = DateTime.UtcNow
                };
                _context.ElectricMeterReadings.Add(meterReading);

                // Update room's last reading
                room.LastMeterReading = currReading;
                room.LastReadingDate = DateTime.UtcNow;

                var totalAmount = room.MonthlyRent + electricCharges;

                var bill = new Bill
                {
                    BillNumber = "BILL-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + room.RoomNumber.Replace("/", ""),
                    RoomId = room.Id,
                    TenantId = activeAgreement.TenantId,
                    BillDate = DateTime.UtcNow,
                    BillPeriod = billPeriod,
                    DueDate = dueDate,
                    RentAmount = room.MonthlyRent,
                    ElectricAmount = electricCharges,
                    MiscAmount = 0,
                    TotalAmount = totalAmount,
                    PaidAmount = 0,
                    Status = "Pending",
                    Remarks = $"Units: {unitsConsumed} @ ₹{rate}/unit",
                    CreatedDate = DateTime.UtcNow
                };

                _context.Bills.Add(bill);
                generatedBills.Add(new {
                    room.RoomNumber,
                    TenantName = $"{activeAgreement.Tenant?.FirstName} {activeAgreement.Tenant?.LastName}",
                    Rent = room.MonthlyRent,
                    UnitsConsumed = unitsConsumed,
                    ElectricCharges = electricCharges,
                    TotalAmount = totalAmount
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                generated = generatedBills.Count, 
                skipped = skipped,
                errors = errors,
                bills = generatedBills,
                message = $"Generated {generatedBills.Count} bills for {billPeriod}. Skipped {skipped}."
            });
        }

        /// <summary>
        /// Simple bulk generate (rent only, no meter readings)
        /// </summary>
        [HttpPost("generate-bulk")]
        public async Task<ActionResult<object>> GenerateBulkBills([FromBody] BulkBillRequest request)
        {
            var billPeriod = request.BillPeriod ?? DateTime.UtcNow.ToString("MMMM yyyy");
            var dueDate = request.DueDate ?? DateTime.UtcNow.AddDays(7);

            var roomsWithTenants = await _context.Rooms
                .Include(r => r.RentAgreements.Where(ra => ra.IsActive))
                .ThenInclude(ra => ra.Tenant)
                .Where(r => r.RentAgreements.Any(ra => ra.IsActive))
                .ToListAsync();

            var generatedBills = new List<Bill>();
            var skipped = 0;

            foreach (var room in roomsWithTenants)
            {
                var activeAgreement = room.RentAgreements.FirstOrDefault(ra => ra.IsActive);
                if (activeAgreement == null) continue;

                var existingBill = await _context.Bills
                    .AnyAsync(b => b.RoomId == room.Id && b.BillPeriod == billPeriod);

                if (existingBill)
                {
                    skipped++;
                    continue;
                }

                var bill = new Bill
                {
                    BillNumber = "BILL-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + room.RoomNumber.Replace("/", ""),
                    RoomId = room.Id,
                    TenantId = activeAgreement.TenantId,
                    BillDate = DateTime.UtcNow,
                    BillPeriod = billPeriod,
                    DueDate = dueDate,
                    RentAmount = room.MonthlyRent,
                    ElectricAmount = 0,
                    MiscAmount = 0,
                    TotalAmount = room.MonthlyRent,
                    PaidAmount = 0,
                    Status = "Pending",
                    Remarks = "Auto-generated bulk bill",
                    CreatedDate = DateTime.UtcNow
                };

                _context.Bills.Add(bill);
                generatedBills.Add(bill);
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                generated = generatedBills.Count, 
                skipped = skipped,
                message = $"Generated {generatedBills.Count} bills for {billPeriod}. Skipped {skipped} (already exist)."
            });
        }

        /// <summary>
        /// Record payment against a bill
        /// </summary>
        [HttpPost("{id}/payment")]
        public async Task<ActionResult> RecordPayment(int id, [FromBody] PaymentRequest request)
        {
            var bill = await _context.Bills.FindAsync(id);
            if (bill == null) return NotFound();

            bill.PaidAmount += request.Amount;
            if (bill.PaidAmount >= bill.TotalAmount)
            {
                bill.Status = "Paid";
            }
            else
            {
                bill.Status = "Partial";
            }

            var payment = new Payment
            {
                TenantId = bill.TenantId,
                BillId = bill.Id,
                Amount = request.Amount,
                PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
                PaymentMethod = request.PaymentMethod ?? "Cash",
                PaymentType = "Bill Payment",
                TransactionReference = request.TransactionReference,
                Status = "Completed",
                Notes = $"Payment for bill {bill.BillNumber}",
                CreatedDate = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new {
                bill.Id,
                bill.BillNumber,
                bill.TotalAmount,
                bill.PaidAmount,
                Remaining = bill.TotalAmount - bill.PaidAmount,
                bill.Status
            });
        }
    }

    public class BillGenerateRequest
    {
        public int RoomId { get; set; }
        public string? BillPeriod { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? RentAmount { get; set; }
        public decimal? ElectricAmount { get; set; }
        public decimal? MiscAmount { get; set; }
        public string? Remarks { get; set; }
        public int? PreviousReading { get; set; }
        public int? CurrentReading { get; set; }
    }

    public class BulkBillRequest
    {
        public string? BillPeriod { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class BulkBillWithReadingsRequest
    {
        public string? BillPeriod { get; set; }
        public DateTime? DueDate { get; set; }
        public List<RoomMeterReading> RoomReadings { get; set; } = new();
    }

    public class RoomMeterReading
    {
        public int RoomId { get; set; }
        public int? PreviousReading { get; set; }
        public int CurrentReading { get; set; }
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionReference { get; set; }
    }
}
