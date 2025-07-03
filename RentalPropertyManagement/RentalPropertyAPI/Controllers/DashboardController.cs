using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalPropertyAPI.Data;
using RentalPropertyAPI.DTOs;
using RentalPropertyAPI.Models;

namespace RentalPropertyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public DashboardController(RentalDbContext context)
        {
            _context = context;
        }

        // GET: api/dashboard
        [HttpGet]
        public async Task<ActionResult<DashboardDto>> GetDashboard()
        {
            var currentDate = DateTime.Now;
            var currentMonthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
            var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);

            // Room statistics
            var totalRooms = await _context.Rooms.CountAsync();
            var occupiedRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
            var availableRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Available);
            var maintenanceRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Maintenance);

            // Payment statistics for current month
            var currentMonthPayments = await _context.Payments
                .Where(p => p.PaymentDate >= currentMonthStart && p.PaymentDate <= currentMonthEnd)
                .ToListAsync();

            var totalRentCollectedThisMonth = currentMonthPayments
                .Where(p => p.Type == PaymentType.Rent)
                .Sum(p => p.Amount);

            var totalElectricityCollectedThisMonth = currentMonthPayments
                .Where(p => p.Type == PaymentType.Electricity)
                .Sum(p => p.Amount);

            // Pending maintenance requests
            var pendingMaintenanceRequests = await _context.MaintenanceRequests
                .CountAsync(m => m.Status == MaintenanceStatus.Pending || m.Status == MaintenanceStatus.InProgress);

            // Recent payments (last 10)
            var recentPayments = await _context.Payments
                .Include(p => p.Tenant)
                    .ThenInclude(t => t.Room)
                .OrderByDescending(p => p.PaymentDate)
                .Take(10)
                .Select(p => new RecentPaymentDto
                {
                    TenantName = p.Tenant.FullName,
                    RoomNumber = p.Tenant.Room.RoomNumber,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentType = p.Type.ToString()
                })
                .ToListAsync();

            // Upcoming move-outs (next 30 days)
            var upcomingMoveOuts = await _context.Tenants
                .Include(t => t.Room)
                .Where(t => t.IsActive && t.MoveOutDate.HasValue && 
                           t.MoveOutDate.Value >= currentDate && 
                           t.MoveOutDate.Value <= currentDate.AddDays(30))
                .Select(t => new UpcomingMoveOutDto
                {
                    TenantName = t.FullName,
                    RoomNumber = t.Room.RoomNumber,
                    MoveOutDate = t.MoveOutDate.Value,
                    DaysRemaining = (t.MoveOutDate.Value - currentDate).Days
                })
                .OrderBy(u => u.MoveOutDate)
                .ToListAsync();

            // Calculate pending payments (simplified - in real system, you'd have more complex logic)
            var activeTenants = await _context.Tenants
                .Include(t => t.Room)
                .Where(t => t.IsActive)
                .CountAsync();

            var expectedRentThisMonth = activeTenants * 5000m; // Assuming average rent of 5000
            var pendingPayments = Math.Max(0, expectedRentThisMonth - totalRentCollectedThisMonth);

            var dashboard = new DashboardDto
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                AvailableRooms = availableRooms,
                MaintenanceRooms = maintenanceRooms,
                TotalRentCollectedThisMonth = totalRentCollectedThisMonth,
                TotalElectricityCollectedThisMonth = totalElectricityCollectedThisMonth,
                PendingPayments = pendingPayments,
                UpcomingMoveOutsCount = upcomingMoveOuts.Count,
                PendingMaintenanceRequests = pendingMaintenanceRequests,
                RecentPayments = recentPayments,
                UpcomingMoveOuts = upcomingMoveOuts
            };

            return Ok(dashboard);
        }

        // GET: api/dashboard/occupancy-trends
        [HttpGet("occupancy-trends")]
        public async Task<ActionResult<object>> GetOccupancyTrends(int months = 12)
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-months);

            var trends = new List<object>();

            for (int i = 0; i < months; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // For simplicity, using current occupancy. In a real system, you'd track historical data
                var occupiedRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
                var totalRooms = await _context.Rooms.CountAsync();

                trends.Add(new
                {
                    Month = monthStart.ToString("yyyy-MM"),
                    MonthName = monthStart.ToString("MMM yyyy"),
                    OccupiedRooms = occupiedRooms,
                    TotalRooms = totalRooms,
                    OccupancyRate = totalRooms > 0 ? Math.Round((double)occupiedRooms / totalRooms * 100, 2) : 0
                });
            }

            return Ok(trends);
        }

        // GET: api/dashboard/revenue-trends
        [HttpGet("revenue-trends")]
        public async Task<ActionResult<object>> GetRevenueTrends(int months = 12)
        {
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-months);

            var trends = new List<object>();

            for (int i = 0; i < months; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthlyPayments = await _context.Payments
                    .Where(p => p.PaymentDate >= monthStart && p.PaymentDate <= monthEnd)
                    .ToListAsync();

                var rentRevenue = monthlyPayments.Where(p => p.Type == PaymentType.Rent).Sum(p => p.Amount);
                var electricityRevenue = monthlyPayments.Where(p => p.Type == PaymentType.Electricity).Sum(p => p.Amount);
                var otherRevenue = monthlyPayments.Where(p => p.Type != PaymentType.Rent && p.Type != PaymentType.Electricity).Sum(p => p.Amount);

                trends.Add(new
                {
                    Month = monthStart.ToString("yyyy-MM"),
                    MonthName = monthStart.ToString("MMM yyyy"),
                    RentRevenue = rentRevenue,
                    ElectricityRevenue = electricityRevenue,
                    OtherRevenue = otherRevenue,
                    TotalRevenue = rentRevenue + electricityRevenue + otherRevenue
                });
            }

            return Ok(trends);
        }

        // GET: api/dashboard/room-wise-summary
        [HttpGet("room-wise-summary")]
        public async Task<ActionResult<IEnumerable<object>>> GetRoomWiseSummary()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Tenants.Where(t => t.IsActive))
                .Include(r => r.ElectricityReadings)
                .Include(r => r.MaintenanceRequests.Where(m => m.Status != MaintenanceStatus.Completed))
                .Select(r => new
                {
                    RoomId = r.Id,
                    RoomNumber = r.RoomNumber,
                    Status = r.Status.ToString(),
                    MonthlyRent = r.MonthlyRent,
                    CurrentTenant = r.Tenants.Where(t => t.IsActive).Select(t => new
                    {
                        t.Id,
                        t.FullName,
                        t.MoveInDate,
                        DaysOccupied = (DateTime.Now - t.MoveInDate).Days
                    }).FirstOrDefault(),
                    LastElectricityReading = r.ElectricityReadings
                        .OrderByDescending(e => e.ReadingDate)
                        .Select(e => new { e.Reading, e.ReadingDate })
                        .FirstOrDefault(),
                    PendingMaintenanceRequests = r.MaintenanceRequests
                        .Where(m => m.Status != MaintenanceStatus.Completed)
                        .Count(),
                    TotalMaintenanceCost = r.MaintenanceRequests
                        .Where(m => m.ActualCost.HasValue)
                        .Sum(m => m.ActualCost.Value)
                })
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            return Ok(rooms);
        }

        // GET: api/dashboard/alerts
        [HttpGet("alerts")]
        public async Task<ActionResult<object>> GetAlerts()
        {
            var currentDate = DateTime.Now;
            var alerts = new List<object>();

            // Overdue rent payments (simplified logic)
            var tenantsWithoutRecentPayments = await _context.Tenants
                .Include(t => t.Room)
                .Include(t => t.Payments)
                .Where(t => t.IsActive)
                .Where(t => !t.Payments.Any(p => p.Type == PaymentType.Rent && 
                                                p.PaymentDate >= currentDate.AddDays(-35)))
                .Select(t => new
                {
                    Type = "Overdue Rent",
                    Message = $"{t.FullName} (Room {t.Room.RoomNumber}) - No rent payment in last 35 days",
                    Priority = "High",
                    TenantId = t.Id,
                    RoomId = t.RoomId
                })
                .ToListAsync();

            alerts.AddRange(tenantsWithoutRecentPayments);

            // Pending electricity readings
            var roomsNeedingElectricityReading = await _context.Rooms
                .Include(r => r.Tenants.Where(t => t.IsActive))
                .Include(r => r.ElectricityReadings)
                .Where(r => r.Status == RoomStatus.Occupied)
                .Where(r => !r.ElectricityReadings.Any(e => 
                    e.ReadingDate.Year == currentDate.Year && 
                    e.ReadingDate.Month == currentDate.Month))
                .Select(r => new
                {
                    Type = "Pending Electricity Reading",
                    Message = $"Room {r.RoomNumber} - Monthly electricity reading pending",
                    Priority = "Medium",
                    TenantId = r.Tenants.Where(t => t.IsActive).Select(t => t.Id).FirstOrDefault(),
                    RoomId = r.Id
                })
                .ToListAsync();

            alerts.AddRange(roomsNeedingElectricityReading);

            // High priority maintenance requests
            var urgentMaintenance = await _context.MaintenanceRequests
                .Include(m => m.Room)
                .Include(m => m.Tenant)
                .Where(m => m.Priority == MaintenancePriority.High || m.Priority == MaintenancePriority.Urgent)
                .Where(m => m.Status == MaintenanceStatus.Pending || m.Status == MaintenanceStatus.InProgress)
                .Select(m => new
                {
                    Type = "Urgent Maintenance",
                    Message = $"{m.Title} - Room {m.Room.RoomNumber} ({m.Priority})",
                    Priority = m.Priority == MaintenancePriority.Urgent ? "Critical" : "High",
                    TenantId = m.TenantId,
                    RoomId = m.RoomId,
                    MaintenanceId = m.Id
                })
                .ToListAsync();

            alerts.AddRange(urgentMaintenance);

            // Upcoming move-outs
            var upcomingMoveOuts = await _context.Tenants
                .Include(t => t.Room)
                .Where(t => t.IsActive && t.MoveOutDate.HasValue && 
                           t.MoveOutDate.Value >= currentDate && 
                           t.MoveOutDate.Value <= currentDate.AddDays(7))
                .Select(t => new
                {
                    Type = "Upcoming Move-out",
                    Message = $"{t.FullName} (Room {t.Room.RoomNumber}) - Moving out on {t.MoveOutDate.Value:yyyy-MM-dd}",
                    Priority = "Medium",
                    TenantId = t.Id,
                    RoomId = t.RoomId
                })
                .ToListAsync();

            alerts.AddRange(upcomingMoveOuts);

            return Ok(new
            {
                TotalAlerts = alerts.Count,
                CriticalAlerts = alerts.Count(a => a.GetType().GetProperty("Priority")?.GetValue(a)?.ToString() == "Critical"),
                HighPriorityAlerts = alerts.Count(a => a.GetType().GetProperty("Priority")?.GetValue(a)?.ToString() == "High"),
                Alerts = alerts.Take(20) // Limit to 20 most recent alerts
            });
        }
    }
}