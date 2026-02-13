using OfficeOpenXml;
using RentalBackend.Data;
using RentalBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
            public dynamic? ImportStats { get; set; }
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
                                   ?? new Property { PropertyName = "SITA DEVI PANDEY HOUSING PROJECT", TotalFloors = 3, TotalRooms = 22 };
                    if (property.Id == 0) _context.Properties.Add(property);
                    await _context.SaveChangesAsync();

                    int totalRecords = 0;

                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        var sheetName = worksheet.Name.Trim();
                        // Parse Month/Year from Sheet Name (e.g., "JUL 25", "JAN 2025")
                        if (!TryParseMonthYear(sheetName, out int month, out int year))
                        {
                            _logger.LogWarning($"Skipping sheet '{sheetName}': Could not parse month/year.");
                            continue;
                        }

                        _logger.LogInformation($"Processing Sheet: {sheetName} -> {month}/{year}");

                        int rowCount = worksheet.Dimension?.Rows ?? 0;
                        int colCount = worksheet.Dimension?.Columns ?? 0;

                        if (rowCount == 0) continue;

                        // Find Column Indices
                        var colMap = MapColumns(worksheet, colCount);
                        if (!colMap.ContainsKey("NAME") || !colMap.ContainsKey("ROOM NO"))
                        {
                             _logger.LogWarning($"Skipping sheet '{sheetName}': Missing NAME or ROOM NO columns.");
                             continue;
                        }

                        for (int row = colMap["HEADER_ROW"] + 1; row <= rowCount; row++)
                        {
                            var roomNumber = GetText(worksheet, row, colMap, "ROOM NO");
                            if (string.IsNullOrWhiteSpace(roomNumber) || roomNumber.ToLower() == "total") continue;

                            // 1. Ensure Room Exists
                            var floorNumber = ParseFloor(roomNumber);
                            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
                            if (room == null)
                            {
                                room = new Room { RoomNumber = roomNumber, PropertyId = property.Id, FloorNumber = floorNumber, MonthlyRent = 0 };
                                _context.Rooms.Add(room);
                                await _context.SaveChangesAsync();
                            }

                            // 2. Parse Tenant Details
                            var tenantName = GetText(worksheet, row, colMap, "NAME");
                            bool isVacant = string.IsNullOrWhiteSpace(tenantName) || 
                                          tenantName.Contains("VACANT", StringComparison.OrdinalIgnoreCase) ||
                                          tenantName.Contains("EMPTY", StringComparison.OrdinalIgnoreCase);

                            // 3. Create RoomMonthlyRecord
                            var record = await _context.RoomMonthlyRecords
                                .FirstOrDefaultAsync(r => r.RoomId == room.Id && r.Month == month && r.Year == year);

                            if (record == null)
                            {
                                record = new RoomMonthlyRecord { RoomId = room.Id, Month = month, Year = year };
                                _context.RoomMonthlyRecords.Add(record);
                            }

                            record.TenantName = isVacant ? "VACANT" : tenantName;
                            record.IsVacant = isVacant;
                            
                            // Allotment & Rent
                            record.InitialAllotmentDate = ParseDate(GetText(worksheet, row, colMap, "DATE OF ALLOTMENT")); // "INITIAL ALLOTMENT"
                            record.InitialRent = ParseDecimal(GetText(worksheet, row, colMap, "MTLY RENT")); // "MTLY RENT" or "INITIAL RENT"
                            
                            // Current values (mapped from same column if header is ambiguous, or specific ones if available)
                            // User mentioned "CURRENT ALLOTMENT" and "CURRENT RENT" columns might exist
                            record.CurrentAllotmentDate = ParseDate(GetText(worksheet, row, colMap, "CURRENT ALLOTMENT")) ?? record.InitialAllotmentDate;
                            record.CurrentRent = ParseDecimal(GetText(worksheet, row, colMap, "CURRENT RENT"));
                            if (record.CurrentRent == 0) record.CurrentRent = record.InitialRent; // Fallback

                            // Security
                            record.ElectricSecurity = ParseDecimal(GetText(worksheet, row, colMap, "ELECTRIC SECURITY"));
                            record.CurrentAdvance = ParseDecimal(GetText(worksheet, row, colMap, "CURRENT ADVANCE"));

                            // Electric
                            record.CurrentReading = ParseInt(GetText(worksheet, row, colMap, "NEW"));
                            record.PreviousReading = ParseInt(GetText(worksheet, row, colMap, "PRE")); // "PRE" or "OLD"
                            record.UnitsConsumed = ParseInt(GetText(worksheet, row, colMap, "TOTAL")); // "TOTAL" units
                            record.ElectricBillAmount = ParseDecimal(GetText(worksheet, row, colMap, "COST"));

                            // Financials
                            record.MiscCharges = ParseDecimal(GetText(worksheet, row, colMap, "MISC RENT"));
                            record.BalanceBroughtForward = ParseDecimal(GetText(worksheet, row, colMap, "B/F & ADV"));
                            record.TotalAmountDue = ParseDecimal(GetText(worksheet, row, colMap, "TOTAL AMT DUE"));
                            record.AmountPaid = ParseDecimal(GetText(worksheet, row, colMap, "AMT PAID"));
                            record.BalanceCarriedForward = ParseDecimal(GetText(worksheet, row, colMap, "B/F OR ADV"));
                            
                            record.PaymentDate = ParseDate(GetText(worksheet, row, colMap, "PAYMENT DATE"));
                            record.Remarks = GetText(worksheet, row, colMap, "REMARKS");

                            // Also update Room current state if this is the latest month/year
                            if (IsLatestMonth(month, year))
                            {
                                room.IsAvailable = isVacant;
                                room.MonthlyRent = record.CurrentRent > 0 ? record.CurrentRent : room.MonthlyRent;
                                if (record.CurrentReading > 0)
                                {
                                    room.LastMeterReading = record.CurrentReading;
                                    room.LastReadingDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddDays(-1);
                                }
                            }

                            totalRecords++;
                        }
                        await _context.SaveChangesAsync();
                    }

                    result.ImportedData.Add("records", totalRecords);
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

        private bool TryParseMonthYear(string sheetName, out int month, out int year)
        {
            month = 0; year = 0;
            try 
            {
                // Formats: "JUL 25", "JULY 2025", "JUL-25"
                var parts = sheetName.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return false;

                if (DateTime.TryParseExact(parts[0], "MMM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime mDate) ||
                    DateTime.TryParseExact(parts[0], "MMMM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out mDate))
                {
                    month = mDate.Month;
                }
                else return false;

                if (int.TryParse(parts[1], out int y))
                {
                    year = y < 100 ? 2000 + y : y;
                    return true;
                }
            }
            catch {}
            return false;
        }

        private Dictionary<string, int> MapColumns(ExcelWorksheet sheet, int colCount)
        {
            var map = new Dictionary<string, int>();
            // Scan first 5 rows for header keywords
            for (int r = 1; r <= 5; r++)
            {
                for (int c = 1; c <= colCount; c++)
                {
                    var txt = sheet.Cells[r, c].Text.Trim().ToUpper();
                    if (string.IsNullOrEmpty(txt)) continue;
                    
                    if (txt == "NAME") map["NAME"] = c;
                    else if (txt.Contains("ROOM") && txt.Contains("NO")) map["ROOM NO"] = c;
                    else if (txt.Contains("INITIAL ALLOTMENT") || txt.Contains("DATE OF ALLOTMENT")) map["DATE OF ALLOTMENT"] = c;
                    else if (txt.Contains("ELECTRIC") && txt.Contains("SECURITY")) map["ELECTRIC SECURITY"] = c;
                    else if (txt.Contains("MTLY") && txt.Contains("RENT")) map["MTLY RENT"] = c; // "MTLY RENT"
                    else if (txt == "NEW") map["NEW"] = c;
                    else if (txt == "PRE" || txt == "OLD") map["PRE"] = c;
                    else if (txt == "TOTAL") map["TOTAL"] = c;
                    else if (txt == "COST") map["COST"] = c;
                    else if (txt.Contains("MISC") && txt.Contains("RENT")) map["MISC RENT"] = c;
                    else if (txt.Contains("B/F") && txt.Contains("&") && txt.Contains("ADV")) map["B/F & ADV"] = c;
                    else if (txt.Contains("TOTAL") && txt.Contains("AMT") && txt.Contains("DUE")) map["TOTAL AMT DUE"] = c;
                    else if (txt.Contains("AMT") && txt.Contains("PAID")) map["AMT PAID"] = c;
                    else if (txt.Contains("B/F") && txt.Contains("OR") && txt.Contains("ADV")) map["B/F OR ADV"] = c;
                    else if (txt.Contains("PAYMENT") && txt.Contains("DATE")) map["PAYMENT DATE"] = c;
                    else if (txt.Contains("REMARKS")) map["REMARKS"] = c;
                    else if (txt.Contains("CURRENT") && txt.Contains("RENT")) map["CURRENT RENT"] = c;
                    else if (txt.Contains("CURRENT") && txt.Contains("ALLOTMENT")) map["CURRENT ALLOTMENT"] = c;
                    else if (txt.Contains("CURRENT") && txt.Contains("ADVANCE")) map["CURRENT ADVANCE"] = c;
                }
                if (map.ContainsKey("NAME") && map.ContainsKey("ROOM NO"))
                {
                    map["HEADER_ROW"] = r;
                    break;
                }
            }
            return map;
        }

        private string GetText(ExcelWorksheet sheet, int row, Dictionary<string, int> map, string key)
        {
            return map.ContainsKey(key) ? sheet.Cells[row, map[key]].Text.Trim() : "";
        }

        private decimal ParseDecimal(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return 0;
            val = System.Text.RegularExpressions.Regex.Replace(val, @"[^0-9.-]", "");
            return decimal.TryParse(val, out decimal d) ? d : 0;
        }

        private int ParseInt(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return 0;
            val = System.Text.RegularExpressions.Regex.Replace(val, @"[^0-9-]", "");
            return int.TryParse(val, out int i) ? i : 0;
        }

        private DateTime? ParseDate(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            if (DateTime.TryParse(val, out DateTime d)) return d.ToUniversalTime();
            // Try OA Date (Excel serial date)
            if (double.TryParse(val, out double oa)) return DateTime.FromOADate(oa).ToUniversalTime();
            return null;
        }
        
        private int ParseFloor(string roomNumber)
        {
            if (string.IsNullOrEmpty(roomNumber)) return 0;
            var parts = roomNumber.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var prefix = parts[0].Trim();
            if (prefix.Equals("G", StringComparison.OrdinalIgnoreCase)) return 0;
            if (int.TryParse(prefix, out int floor)) return floor;
            return 0;
        }
        
        private bool IsLatestMonth(int month, int year)
        {
            var now = DateTime.UtcNow;
            return year == now.Year && month == now.Month; 
        }

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
