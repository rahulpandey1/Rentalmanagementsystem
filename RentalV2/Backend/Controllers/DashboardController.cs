using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Models;

namespace RentalBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public DashboardController(RentManagementContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetDashboardStats(int? year)
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var yearStart = new DateTime(targetYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var yearEnd = new DateTime(targetYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            var dashboardData = new
            {
                Year = targetYear,
                TotalProperties = await _context.Properties.CountAsync(),
                TotalRooms = await _context.Rooms.CountAsync(),
                AvailableRooms = await _context.Rooms.CountAsync(r => r.IsAvailable),
                OccupiedRooms = await _context.Rooms.CountAsync(r => !r.IsAvailable),
                TotalTenants = await _context.Tenants.CountAsync(),
                ActiveRentAgreements = await _context.RentAgreements.CountAsync(ra => ra.IsActive && ra.StartDate <= yearEnd && (ra.EndDate == null || ra.EndDate >= yearStart)),
                OverduePayments = await _context.Payments.CountAsync(p => p.Status == "Pending" && p.DueDate < DateTime.UtcNow && p.PaymentDate.Year == targetYear),
                TotalRevenue = await _context.Payments
                    .Where(p => (p.Status == "Paid" || p.Status == "Completed") && p.PaymentDate >= yearStart && p.PaymentDate <= yearEnd)
                    .SumAsync(p => p.Amount),
                // Floor-wise occupancy
                GroundFloorOccupied = await _context.Rooms.CountAsync(r => r.FloorNumber == 0 && !r.IsAvailable),
                FirstFloorOccupied = await _context.Rooms.CountAsync(r => r.FloorNumber == 1 && !r.IsAvailable),
                SecondFloorOccupied = await _context.Rooms.CountAsync(r => r.FloorNumber == 2 && !r.IsAvailable),
                GroundFloorTotal = await _context.Rooms.CountAsync(r => r.FloorNumber == 0),
                FirstFloorTotal = await _context.Rooms.CountAsync(r => r.FloorNumber == 1),
                SecondFloorTotal = await _context.Rooms.CountAsync(r => r.FloorNumber == 2),
                // Billing information (filtered by year)
                TotalOutstanding = await _context.Bills
                    .Where(b => b.Status != "Paid" && b.BillDate >= yearStart && b.BillDate <= yearEnd)
                    .SumAsync(b => b.TotalAmount - b.PaidAmount),
                TotalRentDue = await _context.Bills
                    .Where(b => b.Status != "Paid" && b.BillDate >= yearStart && b.BillDate <= yearEnd)
                    .SumAsync(b => b.RentAmount),
                TotalElectricDue = await _context.Bills
                    .Where(b => b.Status != "Paid" && b.BillDate >= yearStart && b.BillDate <= yearEnd)
                    .SumAsync(b => b.ElectricAmount),
                TotalAdvance = await _context.Payments
                    .Where(p => p.PaymentType == "Advance" && p.PaymentDate >= yearStart && p.PaymentDate <= yearEnd)
                    .SumAsync(p => p.Amount),
                OverdueBills = await _context.Bills
                    .CountAsync(b => b.Status != "Paid" && b.DueDate < DateTime.UtcNow && b.BillDate >= yearStart && b.BillDate <= yearEnd)
            };

            return Ok(dashboardData);
        }

        [HttpGet("floor-view")]
        public async Task<ActionResult<object>> GetFloorView()
        {
            var floorData = new
            {
                GroundFloor = await _context.Rooms
                    .Where(r => r.FloorNumber == 0)
                    .Include(r => r.RentAgreements.Where(ra => ra.IsActive))
                        .ThenInclude(ra => ra.Tenant)
                    .OrderBy(r => r.RoomNumber)
                    .ToListAsync(),
                FirstFloor = await _context.Rooms
                    .Where(r => r.FloorNumber == 1)
                    .Include(r => r.RentAgreements.Where(ra => ra.IsActive))
                        .ThenInclude(ra => ra.Tenant)
                    .OrderBy(r => r.RoomNumber)
                    .ToListAsync(),
                SecondFloor = await _context.Rooms
                    .Where(r => r.FloorNumber == 2)
                    .Include(r => r.RentAgreements.Where(ra => ra.IsActive))
                        .ThenInclude(ra => ra.Tenant)
                    .OrderBy(r => r.RoomNumber)
                    .ToListAsync()
            };

            return Ok(floorData);
        }

        [HttpGet("billing-summary")]
        public async Task<ActionResult<object>> GetBillingSummary()
        {
            var summary = new
            {
                TotalOutstanding = await _context.Bills
                    .Where(b => b.Status != "Paid")
                    .SumAsync(b => b.TotalAmount - b.PaidAmount),
                TotalRentDue = await _context.Bills
                    .Where(b => b.Status != "Paid")
                    .SumAsync(b => b.RentAmount),
                TotalElectricDue = await _context.Bills
                    .Where(b => b.Status != "Paid")
                    .SumAsync(b => b.ElectricAmount),
                TotalMiscDue = await _context.Bills
                    .Where(b => b.Status != "Paid")
                    .SumAsync(b => b.MiscAmount),
                TotalAdvance = await _context.Payments
                    .Where(p => p.PaymentType == "Advance")
                    .SumAsync(p => p.Amount),
                OverdueBills = await _context.Bills
                    .CountAsync(b => b.Status != "Paid" && b.DueDate < DateTime.UtcNow),
                RecentBills = await _context.Bills
                    .Include(b => b.Tenant)
                    .Include(b => b.Room)
                    .OrderByDescending(b => b.BillDate)
                    .Take(10)
                    .ToListAsync(),
                OutstandingByTenant = await _context.Bills
                    .Where(b => b.Status != "Paid" && (b.TotalAmount - b.PaidAmount) > 0)
                    .Include(b => b.Tenant)
                    .Include(b => b.Room)
                    .GroupBy(b => new { b.TenantId, FullName = b.Tenant.FirstName + " " + b.Tenant.LastName, b.Room.RoomNumber })
                    .Select(g => new
                    {
                        TenantId = g.Key.TenantId,
                        TenantName = g.Key.FullName,
                        RoomNumber = g.Key.RoomNumber,
                        TotalOutstanding = g.Sum(b => b.TotalAmount - b.PaidAmount),
                        BillCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalOutstanding)
                    .ToListAsync()
            };

            return Ok(summary);
        }
    }
}
