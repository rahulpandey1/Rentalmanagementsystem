using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using RentMangementsystem.Data;
using RentMangementsystem.Models;
using System.Globalization;

namespace RentMangementsystem.Services
{
    public class ExcelImportService
    {
        private readonly RentManagementContext _context;
        private readonly ILogger<ExcelImportService> _logger;
        private const string ExcelPassword = "sanpa@123";

        public ExcelImportService(RentManagementContext context, ILogger<ExcelImportService> logger)
        {
            _context = context;
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<ImportResult> ImportPaymentDataFromExcel(string filePath)
        {
            var result = new ImportResult
            {
                ImportStartTime = DateTime.Now
            };
            
            try
            {
                _logger.LogInformation("Starting Excel import from: {FilePath}", filePath);
                
                ExcelPackage? package = null;
                
                // Try to open the Excel file - first without password, then with password
                try
                {
                    // Try without password first
                    _logger.LogInformation("Attempting to open Excel file without password...");
                    package = new ExcelPackage(new FileInfo(filePath));
                    _logger.LogInformation("Successfully opened Excel file without password");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("File appears to be password protected, trying with password...");
                    try
                    {
                        // Try with the known password
                        package?.Dispose(); // Dispose previous attempt if any
                        package = new ExcelPackage(new FileInfo(filePath), ExcelPassword);
                        _logger.LogInformation("Successfully opened password-protected Excel file");
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError("Failed to open Excel file with or without password. Error: {Error}", ex2.Message);
                        throw new Exception($"Cannot open Excel file. Tried both with and without password. Error: {ex2.Message}");
                    }
                }
                
                using (package)
                {
                    var workbook = package.Workbook;
                    
                    // Track what we're importing
                    result.SheetsProcessed = workbook.Worksheets.Count;
                    _logger.LogInformation("Found {WorksheetCount} worksheets in the Excel file", workbook.Worksheets.Count);
                    
                    // Process each worksheet
                    foreach (var worksheet in workbook.Worksheets)
                    {
                        _logger.LogInformation("Processing worksheet: {WorksheetName}", worksheet.Name);
                        
                        // Check if this looks like the SITA DEVI PANDEY format
                        if (await IsSitaDeviPandeyFormat(worksheet))
                        {
                            await ProcessSitaDeviPandeySheet(worksheet, result);
                            result.SitaDeviPandeySheets++;
                        }
                        else
                        {
                            // Process standard format worksheets
                            switch (worksheet.Name.ToLower())
                            {
                                case "tenants":
                                case "tenant":
                                    await ProcessTenantsSheet(worksheet, result);
                                    break;
                                case "payments":
                                case "payment":
                                    await ProcessPaymentsSheet(worksheet, result);
                                    break;
                                case "rooms":
                                case "room":
                                    await ProcessRoomsSheet(worksheet, result);
                                    break;
                                case "agreements":
                                case "rent agreements":
                                    await ProcessRentAgreementsSheet(worksheet, result);
                                    break;
                                case "bills":
                                case "billing":
                                    await ProcessBillsSheet(worksheet, result);
                                    break;
                                case "electric":
                                case "meter readings":
                                    await ProcessElectricReadingsSheet(worksheet, result);
                                    break;
                                default:
                                    _logger.LogWarning("Unknown worksheet: {WorksheetName}", worksheet.Name);
                                    result.UnknownSheets++;
                                    break;
                            }
                        }
                    }
                }
                
                // Save all changes in a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    result.Success = true;
                    result.ImportEndTime = DateTime.Now;
                    
                    // Log detailed import summary
                    _logger.LogInformation("Excel import completed successfully in {Duration}ms. Summary: {Summary}", 
                        result.ImportDurationMs, 
                        $"Tenants: {result.TenantsImported}, Payments: {result.PaymentsImported}, Bills: {result.BillsImported}, Electric: {result.ElectricReadingsImported}");
                        
                    // Create import log entry
                    await CreateImportLogEntry(result, filePath);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Excel data");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ImportEndTime = DateTime.Now;
            }
            
            return result;
        }

        private async Task CreateImportLogEntry(ImportResult result, string filePath)
        {
            try
            {
                // You can add this to track import history
                var logEntry = new
                {
                    FilePath = Path.GetFileName(filePath),
                    ImportTime = result.ImportStartTime,
                    Duration = result.ImportDurationMs,
                    TenantsImported = result.TenantsImported,
                    PaymentsImported = result.PaymentsImported,
                    BillsImported = result.BillsImported,
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage
                };
                
                _logger.LogInformation("Import log: {@ImportLog}", logEntry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create import log entry");
            }
        }

        private async Task<bool> IsSitaDeviPandeyFormat(ExcelWorksheet worksheet)
        {
            try
            {
                // Check for SITA DEVI PANDEY format by looking for specific headers or content
                var firstRow = GetCellValue(worksheet, 1, 1)?.ToUpper();
                var secondRow = GetCellValue(worksheet, 2, 1)?.ToUpper();
                var thirdRow = GetCellValue(worksheet, 3, 1)?.ToUpper();
                
                return firstRow?.Contains("SITA DEVI PANDEY") == true ||
                       secondRow?.Contains("GANPAT RAI KHEMKA") == true ||
                       thirdRow?.Contains("FOR THE MONTH") == true ||
                       GetCellValue(worksheet, 4, 1)?.ToUpper() == "SNO" ||
                       GetCellValue(worksheet, 4, 2)?.ToUpper() == "NAME";
            }
            catch
            {
                return false;
            }
        }

        private async Task ProcessSitaDeviPandeySheet(ExcelWorksheet worksheet, ImportResult result)
        {
            _logger.LogInformation("Processing SITA DEVI PANDEY format sheet");
            
            // Extract month/year from the sheet if possible
            var billPeriod = ExtractBillPeriodFromSheet(worksheet);
            result.BillPeriod = billPeriod;
            
            // Find the header row (should contain SNO, NAME, ROOM NO, etc.)
            int headerRow = 4; // Usually row 4 based on your format
            for (int row = 1; row <= 10; row++)
            {
                var cellValue = GetCellValue(worksheet, row, 1)?.ToUpper();
                if (cellValue == "SNO")
                {
                    headerRow = row;
                    break;
                }
            }
            
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= headerRow) return;
            
            // Expected columns based on your format:
            // 1=SNO, 2=NAME, 3=ROOM NO, 4=MTLY RENT, 5=ELECTRIC NEW, 6=ELECTRIC PRE, 
            // 7=ELECTRIC TOTAL, 8=ELECTRIC COST, 9=MISC RENT, 10=B/F & ADV, 
            // 11=TOTAL AMT DUE, 12=AMT PAID, 13=B/F OR ADV, 14=REMARKS
            
            for (int row = headerRow + 1; row <= rowCount; row++)
            {
                try
                {
                    var sno = GetCellValue(worksheet, row, 1);
                    var tenantName = GetCellValue(worksheet, row, 2);
                    var roomNumber = GetCellValue(worksheet, row, 3);
                    var monthlyRentText = GetCellValue(worksheet, row, 4);
                    var electricNew = GetCellValue(worksheet, row, 5);
                    var electricPre = GetCellValue(worksheet, row, 6);
                    var electricTotal = GetCellValue(worksheet, row, 7);
                    var electricCost = GetCellValue(worksheet, row, 8);
                    var miscRent = GetCellValue(worksheet, row, 9);
                    var bfAdv = GetCellValue(worksheet, row, 10);
                    var totalAmtDue = GetCellValue(worksheet, row, 11);
                    var amtPaid = GetCellValue(worksheet, row, 12);
                    var bfOrAdv = GetCellValue(worksheet, row, 13);
                    var remarks = GetCellValue(worksheet, row, 14);
                    
                    // Skip empty rows or total rows
                    if (string.IsNullOrEmpty(tenantName) || 
                        tenantName.ToUpper().Contains("TOTAL") ||
                        tenantName.ToUpper().Contains("VACANT") ||
                        tenantName.ToUpper().Contains("NEW"))
                    {
                        if (tenantName?.ToUpper().Contains("VACANT") == true)
                        {
                            result.VacantRoomsFound++;
                            // Still process vacant rooms to update room status
                            await ProcessSitaDeviRoom(roomNumber, monthlyRentText, tenantName);
                        }
                        continue;
                    }
                    
                    _logger.LogInformation("Processing row {Row}: {TenantName} in room {RoomNumber}", row, tenantName, roomNumber);
                    
                    // Process tenant
                    var tenant = await ProcessSitaDeviTenant(tenantName, remarks);
                    if (tenant == null) 
                    {
                        result.SkippedRows++;
                        continue;
                    }
                    
                    // Process room and update rent
                    await ProcessSitaDeviRoom(roomNumber, monthlyRentText, tenantName);
                    
                    // Process electric meter reading
                    if (!string.IsNullOrEmpty(electricNew) && !string.IsNullOrEmpty(electricPre))
                    {
                        await ProcessSitaDeviElectricReading(roomNumber, electricNew, electricPre, electricCost);
                        result.ElectricReadingsImported++;
                    }
                    
                    // Process rent agreement
                    await ProcessSitaDeviRentAgreement(tenant.Id, roomNumber, monthlyRentText, remarks);
                    
                    // Process bill/payment
                    await ProcessSitaDeviBill(tenant.Id, roomNumber, monthlyRentText, electricCost, miscRent, totalAmtDue, amtPaid, remarks, billPeriod);
                    
                    result.TenantsImported++;
                    result.BillsImported++;
                    
                    if (TryParseDecimal(amtPaid) > 0)
                    {
                        result.PaymentsImported++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error processing SITA DEVI PANDEY row {Row}: {Error}", row, ex.Message);
                    result.ErrorRows++;
                    result.Errors.Add($"Row {row}: {ex.Message}");
                }
            }
        }

        private string ExtractBillPeriodFromSheet(ExcelWorksheet worksheet)
        {
            try
            {
                // Look for "FOR THE MONTH OF" pattern in first few rows
                for (int row = 1; row <= 5; row++)
                {
                    for (int col = 1; col <= 10; col++)
                    {
                        var cellValue = GetCellValue(worksheet, row, col)?.ToUpper();
                        if (cellValue?.Contains("FOR THE MONTH") == true)
                        {
                            // Extract month and year
                            var parts = cellValue.Split(" ");
                            for (int i = 0; i < parts.Length - 1; i++)
                            {
                                if (parts[i] == "MONTH" && parts[i + 1] == "OF" && i + 2 < parts.Length)
                                {
                                    var monthYear = string.Join(" ", parts.Skip(i + 2));
                                    return monthYear.Trim();
                                }
                            }
                        }
                    }
                }
                
                // Default to current month if not found
                return DateTime.Now.ToString("MMM yyyy").ToUpper();
            }
            catch
            {
                return DateTime.Now.ToString("MMM yyyy").ToUpper();
            }
        }

        private async Task<Tenant?> ProcessSitaDeviTenant(string tenantName, string? remarks)
        {
            if (string.IsNullOrEmpty(tenantName)) return null;
            
            // Split name into first and last name
            var nameParts = tenantName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 1) return null;
            
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "N/A";
            
            // Check if tenant already exists (case-insensitive)
            var existingTenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.FirstName.ToLower() == firstName.ToLower() && 
                                        t.LastName.ToLower() == lastName.ToLower());
            
            if (existingTenant == null)
            {
                var tenant = new Tenant
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"{firstName.ToLower()}.{lastName.ToLower()}@sitadevipandey.com".Replace(" ", ""),
                    PhoneNumber = ExtractPhoneFromRemarks(remarks),
                    Address = "11/1B/1 GANPAT RAI KHEMKA LANE LILUAH HOWRAH(W. B.) 711204",
                    DateOfBirth = DateTime.Now.AddYears(-30), // Default age
                    CreatedDate = DateTime.Now
                };
                
                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync(); // Save to get ID
                return tenant;
            }
            
            return existingTenant;
        }

        private async Task ProcessSitaDeviRoom(string? roomNumber, string? monthlyRentText, string tenantName)
        {
            if (string.IsNullOrEmpty(roomNumber)) return;
            
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
            if (room != null)
            {
                // Update rent if provided
                var rent = TryParseDecimal(monthlyRentText);
                if (rent.HasValue && rent.Value > 0)
                {
                    room.MonthlyRent = rent.Value;
                }
                
                // Update availability based on whether tenant is vacant/new
                room.IsAvailable = tenantName.ToUpper().Contains("VACANT") || tenantName.ToUpper().Contains("NEW");
            }
            else
            {
                _logger.LogWarning("Room {RoomNumber} not found in database", roomNumber);
            }
        }

        private async Task ProcessSitaDeviElectricReading(string? roomNumber, string electricNew, string electricPre, string? electricCost)
        {
            if (string.IsNullOrEmpty(roomNumber)) return;
            
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
            if (room == null) return;
            
            var currentReading = (int)(TryParseDecimal(electricNew) ?? 0);
            var previousReading = (int)(TryParseDecimal(electricPre) ?? 0);
            var charges = TryParseDecimal(electricCost) ?? 0;
            
            if (currentReading > 0 && previousReading >= 0 && currentReading >= previousReading)
            {
                // Check if this reading already exists
                var existingReading = await _context.ElectricMeterReadings
                    .FirstOrDefaultAsync(emr => emr.RoomId == room.Id && 
                                               emr.CurrentReading == currentReading &&
                                               emr.ReadingDate.Date == DateTime.Now.Date);
                
                if (existingReading == null)
                {
                    var reading = new ElectricMeterReading
                    {
                        RoomId = room.Id,
                        CurrentReading = currentReading,
                        PreviousReading = previousReading,
                        ReadingDate = DateTime.Now,
                        PreviousReadingDate = DateTime.Now.AddMonths(-1),
                        ElectricCharges = charges,
                        Remarks = "Imported from SITA DEVI PANDEY Excel",
                        CreatedDate = DateTime.Now
                    };
                    
                    _context.ElectricMeterReadings.Add(reading);
                    
                    // Update room's last reading
                    room.LastMeterReading = currentReading;
                    room.LastReadingDate = DateTime.Now;
                }
            }
        }

        private async Task ProcessSitaDeviRentAgreement(int tenantId, string? roomNumber, string? monthlyRentText, string? remarks)
        {
            if (string.IsNullOrEmpty(roomNumber)) return;
            
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
            var rent = TryParseDecimal(monthlyRentText) ?? 500m;
            
            // Check if agreement already exists for this tenant and room
            var existingAgreement = await _context.RentAgreements
                .FirstOrDefaultAsync(ra => ra.TenantId == tenantId && ra.RoomId == room.Id && ra.IsActive);
            
            if (existingAgreement == null)
            {
                var startDate = ExtractStartDateFromRemarks(remarks) ?? DateTime.Now.AddMonths(-1);
                var advance = ExtractAdvanceFromRemarks(remarks) ?? 5000m;
                
                var agreement = new RentAgreement
                {
                    TenantId = tenantId,
                    PropertyId = 1,
                    RoomId = room?.Id,
                    StartDate = startDate,
                    EndDate = startDate.AddYears(1),
                    MonthlyRent = rent,
                    SecurityDeposit = advance,
                    IsActive = true,
                    AgreementType = "Monthly",
                    Terms = remarks ?? "Standard boarding house agreement",
                    CreatedDate = DateTime.Now
                };
                
                _context.RentAgreements.Add(agreement);
            }
            else
            {
                // Update existing agreement if needed
                existingAgreement.MonthlyRent = rent;
            }
        }

        private async Task ProcessSitaDeviBill(int tenantId, string? roomNumber, string? monthlyRentText, 
            string? electricCost, string? miscRent, string? totalAmtDue, string? amtPaid, string? remarks, string billPeriod)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
            var agreement = await _context.RentAgreements
                .FirstOrDefaultAsync(ra => ra.TenantId == tenantId && ra.IsActive);
            
            if (agreement == null) return;
            
            var rentAmount = TryParseDecimal(monthlyRentText) ?? 0;
            var electricAmount = TryParseDecimal(electricCost) ?? 0;
            var miscAmount = TryParseDecimal(miscRent) ?? 0;
            var totalDue = TryParseDecimal(totalAmtDue) ?? 0;
            var paidAmount = TryParseDecimal(amtPaid) ?? 0;
            
            // Check if bill already exists for this period
            var existingBill = await _context.Bills
                .FirstOrDefaultAsync(b => b.TenantId == tenantId && 
                                        b.BillPeriod == billPeriod &&
                                        b.RoomId == room.Id);
            
            if (existingBill == null)
            {
                // Generate bill for the extracted period
                var billNumber = await GenerateBillNumber();
                
                var bill = new Bill
                {
                    RentAgreementId = agreement.Id,
                    TenantId = tenantId,
                    RoomId = room?.Id,
                    BillNumber = billNumber,
                    BillDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(15),
                    BillPeriod = billPeriod,
                    RentAmount = rentAmount,
                    ElectricAmount = electricAmount,
                    MiscAmount = miscAmount,
                    TotalAmount = totalDue > 0 ? totalDue : (rentAmount + electricAmount + miscAmount),
                    PaidAmount = paidAmount,
                    Status = paidAmount >= (totalDue > 0 ? totalDue : (rentAmount + electricAmount + miscAmount)) ? "Paid" : "Pending",
                    Remarks = remarks,
                    CreatedDate = DateTime.Now
                };
                
                _context.Bills.Add(bill);
                
                // If there's a payment, create payment record
                if (paidAmount > 0)
                {
                    var payment = new Payment
                    {
                        RentAgreementId = agreement.Id,
                        TenantId = tenantId,
                        BillId = null, // Will be set after bill is saved
                        Amount = paidAmount,
                        PaymentDate = DateTime.Now,
                        DueDate = DateTime.Now,
                        PaymentMethod = "Cash",
                        Status = "Paid",
                        PaymentType = "Monthly Rent",
                        Notes = $"Imported from SITA DEVI PANDEY Excel - {billPeriod}",
                        CreatedDate = DateTime.Now
                    };
                    
                    _context.Payments.Add(payment);
                }
            }
        }

        // Helper methods for extracting information from remarks
        private string? ExtractPhoneFromRemarks(string? remarks)
        {
            if (string.IsNullOrEmpty(remarks)) return null;
            
            // Look for phone number patterns in remarks
            var phonePattern = @"\d{10}";
            var match = System.Text.RegularExpressions.Regex.Match(remarks, phonePattern);
            return match.Success ? match.Value : null;
        }

        private DateTime? ExtractStartDateFromRemarks(string? remarks)
        {
            if (string.IsNullOrEmpty(remarks)) return null;
            
            try
            {
                if (remarks.Contains("WEF"))
                {
                    var parts = remarks.Split("WEF", StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        var datePart = parts[1].Split('.')[0].Trim();
                        
                        // Handle different date formats like "MAY 2025", "JUL 21", "01 DEC 2022"
                        if (DateTime.TryParse(datePart, out DateTime date))
                        {
                            return date;
                        }
                        
                        // Handle "MAY 2025" format
                        if (datePart.Contains(" 20"))
                        {
                            var monthYear = datePart.Split(' ');
                            if (monthYear.Length == 2 && 
                                DateTime.TryParseExact($"01 {datePart}", "dd MMM yyyy", null, DateTimeStyles.None, out date))
                            {
                                return date;
                            }
                        }
                        
                        // Handle "JUL 21" format (assume 20xx)
                        if (datePart.Length <= 6 && datePart.Contains(" "))
                        {
                            var parts2 = datePart.Split(' ');
                            if (parts2.Length == 2 && int.TryParse(parts2[1], out int year))
                            {
                                if (year < 100) year += 2000; // Convert 21 to 2021
                                if (DateTime.TryParseExact($"01 {parts2[0]} {year}", "dd MMM yyyy", null, DateTimeStyles.None, out date))
                                {
                                    return date;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
            
            return null;
        }

        private decimal? ExtractAdvanceFromRemarks(string? remarks)
        {
            if (string.IsNullOrEmpty(remarks)) return null;
            
            try
            {
                if (remarks.Contains("ADV"))
                {
                    var advPattern = @"ADV\s*(\d+)K?";
                    var match = System.Text.RegularExpressions.Regex.Match(remarks, advPattern);
                    if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal advance))
                    {
                        return remarks.Contains("K") ? advance * 1000 : advance;
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
            
            return null;
        }

        // Keep all existing methods for standard format processing...
        private async Task ProcessTenantsSheet(ExcelWorksheet worksheet, ImportResult result)
        {
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1) return; // No data rows
            
            for (int row = 2; row <= rowCount; row++) // Skip header row
            {
                try
                {
                    var firstName = GetCellValue(worksheet, row, 1);
                    var lastName = GetCellValue(worksheet, row, 2);
                    var email = GetCellValue(worksheet, row, 3);
                    var phone = GetCellValue(worksheet, row, 4);
                    var address = GetCellValue(worksheet, row, 5);
                    var dobText = GetCellValue(worksheet, row, 6);
                    
                    if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                        continue;
                    
                    // Check if tenant already exists
                    var existingTenant = await _context.Tenants
                        .FirstOrDefaultAsync(t => t.FirstName == firstName && t.LastName == lastName);
                    
                    if (existingTenant == null)
                    {
                        var tenant = new Tenant
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            Email = email ?? $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
                            PhoneNumber = phone,
                            Address = address,
                            DateOfBirth = TryParseDate(dobText) ?? DateTime.Now.AddYears(-25),
                            CreatedDate = DateTime.Now
                        };
                        
                        _context.Tenants.Add(tenant);
                        result.TenantsImported++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error processing tenant row {Row}: {Error}", row, ex.Message);
                    result.ErrorRows++;
                }
            }
        }

        private async Task ProcessPaymentsSheet(ExcelWorksheet worksheet, ImportResult result)
        {
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1) return;
            
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var tenantName = GetCellValue(worksheet, row, 1);
                    var roomNumber = GetCellValue(worksheet, row, 2);
                    var amountText = GetCellValue(worksheet, row, 3);
                    var dateText = GetCellValue(worksheet, row, 4);
                    var paymentType = GetCellValue(worksheet, row, 5) ?? "Rent";
                    var status = GetCellValue(worksheet, row, 6) ?? "Paid";
                    var method = GetCellValue(worksheet, row, 7) ?? "Cash";
                    
                    if (string.IsNullOrEmpty(tenantName) || string.IsNullOrEmpty(amountText))
                        continue;
                    
                    // Find tenant
                    var tenant = await FindTenantByName(tenantName);
                    if (tenant == null) continue;
                    
                    // Find room and rent agreement
                    var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
                    var rentAgreement = await _context.RentAgreements
                        .FirstOrDefaultAsync(ra => ra.TenantId == tenant.Id && 
                                                  (ra.RoomId == room.Id || ra.RoomId == null));
                    
                    if (rentAgreement == null)
                    {
                        // Create a default rent agreement
                        rentAgreement = new RentAgreement
                        {
                            TenantId = tenant.Id,
                            PropertyId = 1,
                            RoomId = room?.Id,
                            StartDate = DateTime.Now.AddMonths(-1),
                            EndDate = DateTime.Now.AddMonths(11),
                            MonthlyRent = room?.MonthlyRent ?? 500m,
                            SecurityDeposit = 1000m,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };
                        _context.RentAgreements.Add(rentAgreement);
                        await _context.SaveChangesAsync(); // Save to get ID
                    }
                    
                    var payment = new Payment
                    {
                        RentAgreementId = rentAgreement.Id,
                        TenantId = tenant.Id,
                        Amount = TryParseDecimal(amountText) ?? 0,
                        PaymentDate = TryParseDate(dateText) ?? DateTime.Now,
                        DueDate = TryParseDate(dateText) ?? DateTime.Now,
                        PaymentMethod = method,
                        Status = status,
                        PaymentType = paymentType,
                        CreatedDate = DateTime.Now
                    };
                    
                    _context.Payments.Add(payment);
                    result.PaymentsImported++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error processing payment row {Row}: {Error}", row, ex.Message);
                    result.ErrorRows++;
                }
            }
        }

        private async Task ProcessRoomsSheet(ExcelWorksheet worksheet, ImportResult result)
        {
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1) return;
            
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var roomNumber = GetCellValue(worksheet, row, 1);
                    var floor = GetCellValue(worksheet, row, 2);
                    var rentText = GetCellValue(worksheet, row, 3);
                    var areaText = GetCellValue(worksheet, row, 4);
                    var meterNumber = GetCellValue(worksheet, row, 5);
                    var isAvailableText = GetCellValue(worksheet, row, 6);
                    
                    if (string.IsNullOrEmpty(roomNumber)) continue;
                    
                    var existingRoom = await _context.Rooms
                        .FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
                    
                    if (existingRoom != null)
                    {
                        // Update existing room
                        existingRoom.MonthlyRent = TryParseDecimal(rentText) ?? existingRoom.MonthlyRent;
                        existingRoom.Area = TryParseDecimal(areaText) ?? existingRoom.Area;
                        existingRoom.ElectricMeterNumber = meterNumber ?? existingRoom.ElectricMeterNumber;
                        existingRoom.IsAvailable = TryParseBool(isAvailableText) ?? existingRoom.IsAvailable;
                        result.RoomsImported++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error processing room row {Row}: {Error}", row, ex.Message);
                    result.ErrorRows++;
                }
            }
        }

        private async Task ProcessRentAgreementsSheet(ExcelWorksheet worksheet, ImportResult result)
        {
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1) return;
            
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var tenantName = GetCellValue(worksheet, row, 1);
                    var roomNumber = GetCellValue(worksheet, row, 2);
                    var startDateText = GetCellValue(worksheet, row, 3);
                    var endDateText = GetCellValue(worksheet, row, 4);
                    var rentText = GetCellValue(worksheet, row, 5);
                    var depositText = GetCellValue(worksheet, row, 6);
                    
                    if (string.IsNullOrEmpty(tenantName)) continue;
                    
                    var tenant = await FindTenantByName(tenantName);
                    if (tenant == null) continue;
                    
                    var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
                    
                    var agreement = new RentAgreement
                    {
                        TenantId = tenant.Id,
                        PropertyId = 1,
                        RoomId = room?.Id,
                        StartDate = TryParseDate(startDateText) ?? DateTime.Now,
                        EndDate = TryParseDate(endDateText) ?? DateTime.Now.AddYears(1),
                        MonthlyRent = TryParseDecimal(rentText) ?? (room?.MonthlyRent ?? 500m),
                        SecurityDeposit = TryParseDecimal(depositText) ?? 1000m,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };
                    
                    _context.RentAgreements.Add(agreement);
                    result.AgreementsImported++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error processing agreement row {Row}: {Error}", row, ex.Message);
                    result.ErrorRows++;
                }
            }
        }

        private async Task ProcessBillsSheet(ExcelWorksheet worksheet, ImportResult result)
        {
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1) return;
            
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var tenantName = GetCellValue(worksheet, row, 1);
                    var roomNumber = GetCellValue(worksheet, row, 2);
                    var billPeriod = GetCellValue(worksheet, row, 3);
                    var rentAmountText = GetCellValue(worksheet, row, 4);
                    var electricAmountText = GetCellValue(worksheet, row, 5);
                    var miscAmountText = GetCellValue(worksheet, row, 6);
                    var dueDateText = GetCellValue(worksheet, row, 7);
                    
                    if (string.IsNullOrEmpty(tenantName)) continue;
                    
                    var tenant = await FindTenantByName(tenantName);
                    if (tenant == null) continue;
                    
                    var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
                    var agreement = await _context.RentAgreements
                        .FirstOrDefaultAsync(ra => ra.TenantId == tenant.Id);
                    
                    if (agreement == null) continue;
                    
                    var bill = new Bill
                    {
                        RentAgreementId = agreement.Id,
                        TenantId = tenant.Id,
                        RoomId = room?.Id,
                        BillNumber = await GenerateBillNumber(),
                        BillDate = DateTime.Now,
                        DueDate = TryParseDate(dueDateText) ?? DateTime.Now.AddDays(15),
                        BillPeriod = billPeriod ?? DateTime.Now.ToString("MMM yyyy"),
                        RentAmount = TryParseDecimal(rentAmountText) ?? 0,
                        ElectricAmount = TryParseDecimal(electricAmountText) ?? 0,
                        MiscAmount = TryParseDecimal(miscAmountText) ?? 0,
                        Status = "Pending",
                        CreatedDate = DateTime.Now
                    };
                    
                    bill.TotalAmount = bill.RentAmount + bill.ElectricAmount + bill.MiscAmount;
                    
                    _context.Bills.Add(bill);
                    result.BillsImported++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error processing bill row {Row}: {Error}", row, ex.Message);
                    result.ErrorRows++;
                }
            }
        }

        private async Task ProcessElectricReadingsSheet(ExcelWorksheet worksheet, ImportResult result)
        {
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1) return;
            
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var roomNumber = GetCellValue(worksheet, row, 1);
                    var currentReadingText = GetCellValue(worksheet, row, 2);
                    var previousReadingText = GetCellValue(worksheet, row, 3);
                    var dateText = GetCellValue(worksheet, row, 4);
                    
                    if (string.IsNullOrEmpty(roomNumber)) continue;
                    
                    var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == roomNumber);
                    if (room == null) continue;
                    
                    var reading = new ElectricMeterReading
                    {
                        RoomId = room.Id,
                        CurrentReading = (int)(TryParseDecimal(currentReadingText) ?? 0),
                        PreviousReading = (int)(TryParseDecimal(previousReadingText) ?? 0),
                        ReadingDate = TryParseDate(dateText) ?? DateTime.Now,
                        PreviousReadingDate = TryParseDate(dateText)?.AddMonths(-1) ?? DateTime.Now.AddMonths(-1),
                        CreatedDate = DateTime.Now
                    };
                    
                    var unitsConsumed = reading.CurrentReading - reading.PreviousReading;
                    reading.ElectricCharges = unitsConsumed * 12m; // ?12 per unit
                    
                    _context.ElectricMeterReadings.Add(reading);
                    result.ElectricReadingsImported++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error processing electric reading row {Row}: {Error}", row, ex.Message);
                    result.ErrorRows++;
                }
            }
        }

        private async Task<Tenant?> FindTenantByName(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var firstName = parts[0];
                var lastName = string.Join(" ", parts.Skip(1));
                return await _context.Tenants
                    .FirstOrDefaultAsync(t => t.FirstName == firstName && t.LastName == lastName);
            }
            return await _context.Tenants
                .FirstOrDefaultAsync(t => (t.FirstName + " " + t.LastName) == fullName);
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

        private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            return worksheet.Cells[row, col]?.Value?.ToString()?.Trim() ?? string.Empty;
        }

        private decimal? TryParseDecimal(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.Replace(",", "").Replace("?", "").Replace("$", "").Replace("Rs.", "").Trim();
            return decimal.TryParse(value, out decimal result) ? result : null;
        }

        private DateTime? TryParseDate(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return DateTime.TryParse(value, out DateTime result) ? result : null;
        }

        private bool? TryParseBool(string? value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            value = value.ToLower();
            if (value == "yes" || value == "true" || value == "1" || value == "available")
                return true;
            if (value == "no" || value == "false" || value == "0" || value == "occupied")
                return false;
            return null;
        }
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ImportStartTime { get; set; }
        public DateTime ImportEndTime { get; set; }
        public long ImportDurationMs => (long)(ImportEndTime - ImportStartTime).TotalMilliseconds;
        public string? BillPeriod { get; set; }
        
        public int TenantsImported { get; set; }
        public int PaymentsImported { get; set; }
        public int RoomsImported { get; set; }
        public int AgreementsImported { get; set; }
        public int BillsImported { get; set; }
        public int ElectricReadingsImported { get; set; }
        
        public int SheetsProcessed { get; set; }
        public int SitaDeviPandeySheets { get; set; }
        public int UnknownSheets { get; set; }
        public int VacantRoomsFound { get; set; }
        public int SkippedRows { get; set; }
        public int ErrorRows { get; set; }
        
        public List<string> Errors { get; set; } = new List<string>();
        
        public int TotalImported => TenantsImported + PaymentsImported + RoomsImported + 
                                   AgreementsImported + BillsImported + ElectricReadingsImported;
    }
}