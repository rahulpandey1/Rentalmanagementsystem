using OfficeOpenXml;
using RentalBackend.Data;
using RentalBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace RentalBackend.Services
{
    public class ExcelImportService
    {
        private readonly RentManagementContext _context;
        private readonly ILogger<ExcelImportService> _logger;

        public ExcelImportService(RentManagementContext context, ILogger<ExcelImportService> logger)
        {
            _context = context;
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public class ImportResult
        {
            public bool Success { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public Dictionary<string, int> ImportedData { get; set; } = new Dictionary<string, int>();
            public dynamic ImportStats { get; set; }
        }

        public async Task<ImportResult> ImportPaymentDataFromExcel(string filePath)
        {
            var result = new ImportResult { Success = true };
            var startTime = DateTime.UtcNow;

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    _logger.LogInformation("Processing Excel file: {FilePath}", filePath);
                    
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        result.Success = false;
                        result.Errors.Add("No worksheets found in the Excel file.");
                        return result;
                    }

                    var property = await _context.Properties.FirstOrDefaultAsync() 
                                   ?? new Property { PropertyName = "Default Property", TotalFloors = 3, TotalRooms = 22 };
                    if (property.Id == 0) _context.Properties.Add(property);
                    await _context.SaveChangesAsync();

                    int importedTenants = 0;
                    int importedRooms = 0;

                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        var sheetName = worksheet.Name;
                        if (sheetName.ToLower().Contains("start") || sheetName.ToLower().Contains("instruction")) continue; // Skip non-data sheets if any

                        int rowCount = worksheet.Dimension?.Rows ?? 0;
                        int colCount = worksheet.Dimension?.Columns ?? 0;

                        if (rowCount == 0) continue;

                        // Find Headers
                        int nameCol = -1;
                        int roomCol = -1;
                        int rentCol = -1;
                        int headerRow = -1;

                        // Scanning first 5 rows for headers
                        for (int r = 1; r <= 5; r++)
                        {
                            for (int c = 1; c <= colCount; c++)
                            {
                                var val = worksheet.Cells[r, c].Text.Trim().ToUpper();
                                if (val == "NAME") nameCol = c;
                                else if (val.Contains("ROOM") && val.Contains("NO")) roomCol = c; // Matches "ROOM NO"
                                else if (val == "CURRENT RENT" || val == "RENT") rentCol = c;
                            }
                            if (nameCol != -1 && roomCol != -1)
                            {
                                headerRow = r;
                                break;
                            }
                        }

                        if (headerRow == -1)
                        {
                            _logger.LogWarning($"Could not find headers in sheet {sheetName}");
                            continue;
                        }

                        // Load all existing tenants into memory for faster matching
                        var allTenants = await _context.Tenants.ToListAsync();

                        for (int row = headerRow + 1; row <= rowCount; row++)
                        {
                            var roomNumber = worksheet.Cells[row, roomCol].Text.Trim();
                            var tenantName = worksheet.Cells[row, nameCol].Text.Trim();

                            if (string.IsNullOrEmpty(roomNumber)) continue;

                            // Parse Room Number Logic (G/1, 1/1, 2/1)
                            int floorNumber = ParseFloor(roomNumber);
                            
                            // Create/Update Room
                            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber && r.PropertyId == property.Id);
                            if (room == null)
                            {
                                room = new Room
                                {
                                    RoomNumber = roomNumber,
                                    PropertyId = property.Id,
                                    FloorNumber = floorNumber,
                                    MonthlyRent = 0,
                                    IsAvailable = true
                                };
                                _context.Rooms.Add(room);
                                await _context.SaveChangesAsync();
                                importedRooms++;
                            }

                            // Update Rent if available
                            if (rentCol != -1)
                            {
                                var rentText = worksheet.Cells[row, rentCol].Text;
                                var rentVal = CleanDecimal(rentText);
                                if (rentVal > 0)
                                {
                                    room.MonthlyRent = rentVal;
                                }
                            }

                            // Process Tenant
                            bool isVacant = string.IsNullOrEmpty(tenantName) || 
                                          tenantName.Equals("VACANT", StringComparison.OrdinalIgnoreCase) || 
                                          tenantName.Equals("EMPTY", StringComparison.OrdinalIgnoreCase);

                            if (!isVacant)
                            {
                                // Create/Get Tenant - Use in-memory search
                                var tenant = allTenants.FirstOrDefault(t => 
                                    (t.FirstName + " " + t.LastName).Equals(tenantName, StringComparison.OrdinalIgnoreCase) ||
                                    t.FirstName.Equals(tenantName, StringComparison.OrdinalIgnoreCase));
                                    
                                if (tenant == null)
                                {
                                    var names = tenantName.Split(' ');
                                    tenant = new Tenant
                                    {
                                        FirstName = names[0],
                                        LastName = names.Length > 1 ? string.Join(" ", names.Skip(1)) : "",
                                        CreatedDate = DateTime.UtcNow
                                    };
                                    _context.Tenants.Add(tenant);
                                    await _context.SaveChangesAsync();
                                    allTenants.Add(tenant); // Add to in-memory list
                                    importedTenants++;
                                }

                                // Create Active Rent Agreement
                                var agreement = await _context.RentAgreements.FirstOrDefaultAsync(ra => ra.RoomId == room.Id && ra.IsActive);
                                if (agreement == null)
                                {
                                    agreement = new RentAgreement
                                    {
                                        PropertyId = property.Id,
                                        RoomId = room.Id,
                                        TenantId = tenant.Id,
                                        StartDate = DateTime.UtcNow,
                                        EndDate = DateTime.UtcNow.AddYears(1),
                                        MonthlyRent = room.MonthlyRent,
                                        IsActive = true
                                    };
                                    _context.RentAgreements.Add(agreement);
                                }
                                else if (agreement.TenantId != tenant.Id)
                                {
                                    // Room occupied by different tenant in DB? Update to new tenant
                                    agreement.TenantId = tenant.Id;
                                }
                                
                                room.IsAvailable = false;
                                _logger.LogInformation($"Marking Room {room.RoomNumber} as Occupied by {tenant.FirstName}");
                            }
                            else 
                            {
                                 _logger.LogInformation($"Room {roomNumber} is VACANT ({tenantName})");
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    result.ImportedData.Add("tenants", importedTenants);
                    result.ImportedData.Add("rooms", importedRooms);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Excel file");
                result.Success = false;
                result.Errors.Add($"Import failed: {ex.Message}");
            }

            result.ImportStats = new { Duration = (DateTime.UtcNow - startTime).ToString() };
            return result;
        }

        private int ParseFloor(string roomNumber)
        {
            if (string.IsNullOrEmpty(roomNumber)) return 0;
            var parts = roomNumber.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var prefix = parts[0].Trim();

            if (prefix.Equals("G", StringComparison.OrdinalIgnoreCase) || 
                prefix.Equals("Ground", StringComparison.OrdinalIgnoreCase)) return 0;
            
            if (int.TryParse(prefix, out int floor)) return floor;

            // Fallback for direct numbers if any (though user says G/1, 1/1)
            if (roomNumber.StartsWith("G")) return 0;
            if (roomNumber.StartsWith("1")) return 1;
            if (roomNumber.StartsWith("2")) return 2;
            
            return 0; // Default Ground
        }

        private decimal CleanDecimal(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0;
            var clean = System.Text.RegularExpressions.Regex.Replace(input, @"[^0-9.]", "");
            if (decimal.TryParse(clean, out decimal val)) return val;
            return 0;
        }

        /// <summary>
        /// Synchronize room availability based on active RentAgreements.
        /// Call this after import to ensure data consistency.
        /// </summary>
        public async Task SyncRoomAvailabilityAsync()
        {
            var rooms = await _context.Rooms.ToListAsync();
            var activeAgreementRoomIds = await _context.RentAgreements
                .Where(ra => ra.IsActive)
                .Select(ra => ra.RoomId)
                .ToListAsync();

            foreach (var room in rooms)
            {
                bool shouldBeOccupied = activeAgreementRoomIds.Contains(room.Id);
                if (room.IsAvailable == shouldBeOccupied) // Mismatch!
                {
                    room.IsAvailable = !shouldBeOccupied;
                    _logger.LogInformation($"SyncRoomAvailability: Room {room.RoomNumber} set to {(room.IsAvailable ? "Available" : "Occupied")}");
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
