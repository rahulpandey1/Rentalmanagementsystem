namespace RentalPropertyAPI.DTOs
{
    public class DashboardDto
    {
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public decimal TotalRentCollectedThisMonth { get; set; }
        public decimal TotalElectricityCollectedThisMonth { get; set; }
        public decimal PendingPayments { get; set; }
        public int UpcomingMoveOutsCount { get; set; }
        public int PendingMaintenanceRequests { get; set; }
        public List<RecentPaymentDto> RecentPayments { get; set; } = new List<RecentPaymentDto>();
        public List<UpcomingMoveOutDto> UpcomingMoveOuts { get; set; } = new List<UpcomingMoveOutDto>();
    }

    public class RecentPaymentDto
    {
        public string TenantName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentType { get; set; } = string.Empty;
    }

    public class UpcomingMoveOutDto
    {
        public string TenantName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime MoveOutDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class MonthlyReportDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalRentCollected { get; set; }
        public decimal TotalElectricityCollected { get; set; }
        public decimal TotalMaintenanceCost { get; set; }
        public decimal NetIncome { get; set; }
        public int NewTenants { get; set; }
        public int MovedOutTenants { get; set; }
        public List<RoomWiseReportDto> RoomWiseReport { get; set; } = new List<RoomWiseReportDto>();
    }

    public class RoomWiseReportDto
    {
        public string RoomNumber { get; set; } = string.Empty;
        public string? TenantName { get; set; }
        public decimal RentCollected { get; set; }
        public decimal ElectricityCollected { get; set; }
        public decimal MaintenanceCost { get; set; }
        public int DaysOccupied { get; set; }
    }

    public class TenantSettlementDto
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime MoveInDate { get; set; }
        public DateTime MoveOutDate { get; set; }
        public decimal SecurityDeposit { get; set; }
        public decimal TotalRentPaid { get; set; }
        public decimal TotalElectricityPaid { get; set; }
        public decimal TotalMaintenanceCharges { get; set; }
        public decimal OutstandingDues { get; set; }
        public decimal RefundableAmount { get; set; }
        public List<PaymentDto> AllPayments { get; set; } = new List<PaymentDto>();
    }
}