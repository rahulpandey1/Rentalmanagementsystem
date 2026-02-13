using Microsoft.AspNetCore.Mvc;
using RentMangementsystem.Services;

namespace RentMangementsystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataImportController : ControllerBase
    {
        private readonly ExcelImportService _importService;
        private readonly ILogger<DataImportController> _logger;
        private readonly IWebHostEnvironment _environment;

        public DataImportController(ExcelImportService importService, ILogger<DataImportController> logger, IWebHostEnvironment environment)
        {
            _importService = importService;
            _logger = logger;
            _environment = environment;
        }

        // POST: api/DataImport/excel
        [HttpPost("excel")]
        public async Task<ActionResult<ImportResult>> ImportFromExcel()
        {
            try
            {
                var dataFolder = Path.Combine(_environment.ContentRootPath, "Data");
                var excelFilePath = Path.Combine(dataFolder, "Payment Chart Draft1.xlsx");
                
                if (!System.IO.File.Exists(excelFilePath))
                {
                    // Try alternative file names
                    var alternativeFiles = new[]
                    {
                        "SITA DEVI PANDEY.xlsx",
                        "Payment Chart.xlsx",
                        "Monthly Bill.xlsx",
                        "Rent Sheet.xlsx"
                    };
                    
                    foreach (var altFile in alternativeFiles)
                    {
                        var altPath = Path.Combine(dataFolder, altFile);
                        if (System.IO.File.Exists(altPath))
                        {
                            excelFilePath = altPath;
                            break;
                        }
                    }
                    
                    if (!System.IO.File.Exists(excelFilePath))
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = "Excel file not found",
                            Error = $"Please place your Excel file in the Data folder. Looking for: {Path.GetFileName(excelFilePath)}",
                            ExpectedLocation = dataFolder,
                            SupportedFiles = alternativeFiles
                        });
                    }
                }

                _logger.LogInformation("Starting Excel import from: {FilePath}", excelFilePath);
                
                var result = await _importService.ImportPaymentDataFromExcel(excelFilePath);
                
                if (result.Success)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = $"Data imported successfully in {result.ImportDurationMs}ms",
                        ImportedData = new
                        {
                            Tenants = result.TenantsImported,
                            Payments = result.PaymentsImported,
                            Rooms = result.RoomsImported,
                            Agreements = result.AgreementsImported,
                            Bills = result.BillsImported,
                            ElectricReadings = result.ElectricReadingsImported,
                            Total = result.TotalImported
                        },
                        ImportStats = new
                        {
                            Duration = $"{result.ImportDurationMs}ms",
                            SheetsProcessed = result.SheetsProcessed,
                            SitaDeviPandeySheets = result.SitaDeviPandeySheets,
                            BillPeriod = result.BillPeriod,
                            VacantRoomsFound = result.VacantRoomsFound,
                            SkippedRows = result.SkippedRows,
                            ErrorRows = result.ErrorRows
                        },
                        Errors = result.Errors
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Import failed",
                        Error = result.ErrorMessage,
                        ImportStats = new
                        {
                            Duration = $"{result.ImportDurationMs}ms",
                            ErrorRows = result.ErrorRows,
                            SkippedRows = result.SkippedRows
                        },
                        Errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Excel import");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error during import",
                    Error = ex.Message,
                    Details = "Check the logs for more information"
                });
            }
        }

        // POST: api/DataImport/upload
        [HttpPost("upload")]
        public async Task<ActionResult<ImportResult>> UploadAndImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "No file uploaded",
                    Error = "Please select a file to upload"
                });
            }

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid file type",
                    Error = "Only .xlsx files are supported",
                    ProvidedFileType = Path.GetExtension(file.FileName)
                });
            }

            // Check file size (limit to 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "File too large",
                    Error = "File size must be less than 10MB",
                    ProvidedSize = $"{file.Length / 1024 / 1024}MB"
                });
            }

            try
            {
                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileNameWithoutExtension(file.FileName)}.xlsx";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("File uploaded: {FilePath} ({FileSize} bytes)", filePath, file.Length);

                var result = await _importService.ImportPaymentDataFromExcel(filePath);

                // Keep uploaded file for reference if import was successful
                if (!result.Success)
                {
                    // Delete file if import failed
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }

                if (result.Success)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = $"Data imported successfully from uploaded file in {result.ImportDurationMs}ms",
                        FileName = file.FileName,
                        ImportedData = new
                        {
                            Tenants = result.TenantsImported,
                            Payments = result.PaymentsImported,
                            Rooms = result.RoomsImported,
                            Agreements = result.AgreementsImported,
                            Bills = result.BillsImported,
                            ElectricReadings = result.ElectricReadingsImported,
                            Total = result.TotalImported
                        },
                        ImportStats = new
                        {
                            Duration = $"{result.ImportDurationMs}ms",
                            SheetsProcessed = result.SheetsProcessed,
                            SitaDeviPandeySheets = result.SitaDeviPandeySheets,
                            BillPeriod = result.BillPeriod,
                            VacantRoomsFound = result.VacantRoomsFound,
                            SkippedRows = result.SkippedRows,
                            ErrorRows = result.ErrorRows
                        },
                        Errors = result.Errors
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Import failed",
                        FileName = file.FileName,
                        Error = result.ErrorMessage,
                        ImportStats = new
                        {
                            Duration = $"{result.ImportDurationMs}ms",
                            ErrorRows = result.ErrorRows,
                            SkippedRows = result.SkippedRows
                        },
                        Errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file upload and import");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error during import",
                    Error = ex.Message,
                    Details = "Check server logs for detailed information"
                });
            }
        }

        // GET: api/DataImport/status
        [HttpGet("status")]
        public async Task<ActionResult> GetImportStatus()
        {
            try
            {
                var dataFolder = Path.Combine(_environment.ContentRootPath, "Data");
                var primaryFile = Path.Combine(dataFolder, "Payment Chart Draft1.xlsx");
                
                var availableFiles = new List<object>();
                
                // Check for multiple Excel files
                var excelFiles = new[]
                {
                    "Payment Chart Draft1.xlsx",
                    "SITA DEVI PANDEY.xlsx",
                    "Payment Chart.xlsx",
                    "Monthly Bill.xlsx",
                    "Rent Sheet.xlsx"
                };

                foreach (var fileName in excelFiles)
                {
                    var fullPath = Path.Combine(dataFolder, fileName);
                    if (System.IO.File.Exists(fullPath))
                    {
                        var fileInfo = new FileInfo(fullPath);
                        availableFiles.Add(new
                        {
                            FileName = fileName,
                            FilePath = fullPath,
                            FileSize = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime,
                            IsMain = fileName == "Payment Chart Draft1.xlsx"
                        });
                    }
                }

                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
                var recentUploads = new List<object>();
                
                if (Directory.Exists(uploadsFolder))
                {
                    var uploadedFiles = Directory.GetFiles(uploadsFolder, "*.xlsx")
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.LastWriteTime)
                        .Take(5); // Last 5 uploads

                    foreach (var fileInfo in uploadedFiles)
                    {
                        recentUploads.Add(new
                        {
                            FileName = fileInfo.Name,
                            FileSize = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime
                        });
                    }
                }
                
                return Ok(new
                {
                    ExcelFileExists = availableFiles.Any(),
                    DataFolder = dataFolder,
                    AvailableFiles = availableFiles,
                    RecentUploads = recentUploads,
                    TotalFiles = availableFiles.Count,
                    SystemInfo = new
                    {
                        SupportedFormats = new[] { ".xlsx" },
                        MaxFileSize = "10MB",
                        RequiredPassword = "sanpa@123",
                        SupportedSheets = new[]
                        {
                            "SITA DEVI PANDEY format (auto-detected)",
                            "Standard sheets: tenants, payments, rooms, agreements, bills, electric"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking import status");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error checking status",
                    Error = ex.Message
                });
            }
        }

        // GET: api/DataImport/validate/{fileName}
        [HttpGet("validate/{fileName}")]
        public async Task<ActionResult> ValidateExcelFile(string fileName)
        {
            try
            {
                var dataFolder = Path.Combine(_environment.ContentRootPath, "Data");
                var filePath = Path.Combine(dataFolder, fileName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "File not found",
                        RequestedFile = fileName
                    });
                }

                // Validate file without importing
                var validation = await ValidateExcelStructure(filePath);
                
                return Ok(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Excel file: {FileName}", fileName);
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error validating file",
                    Error = ex.Message
                });
            }
        }

        private async Task<object> ValidateExcelStructure(string filePath)
        {
            try
            {
                using var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath), "sanpa@123");
                var workbook = package.Workbook;
                
                var worksheetInfo = new List<object>();
                
                foreach (var worksheet in workbook.Worksheets)
                {
                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    var colCount = worksheet.Dimension?.Columns ?? 0;
                    
                    // Check if it's SITA DEVI PANDEY format
                    var isSitaDeviFormat = false;
                    try
                    {
                        var firstRow = worksheet.Cells[1, 1]?.Value?.ToString()?.ToUpper();
                        var secondRow = worksheet.Cells[2, 1]?.Value?.ToString()?.ToUpper();
                        var thirdRow = worksheet.Cells[3, 1]?.Value?.ToString()?.ToUpper();
                        
                        isSitaDeviFormat = firstRow?.Contains("SITA DEVI PANDEY") == true ||
                                          secondRow?.Contains("GANPAT RAI KHEMKA") == true ||
                                          thirdRow?.Contains("FOR THE MONTH") == true ||
                                          worksheet.Cells[4, 1]?.Value?.ToString()?.ToUpper() == "SNO";
                    }
                    catch { }
                    
                    worksheetInfo.Add(new
                    {
                        Name = worksheet.Name,
                        Rows = rowCount,
                        Columns = colCount,
                        IsSitaDeviPandeyFormat = isSitaDeviFormat,
                        IsSupported = isSitaDeviFormat || IsSupportedStandardSheet(worksheet.Name),
                        PreviewData = GetWorksheetPreview(worksheet, 5)
                    });
                }
                
                return new
                {
                    Success = true,
                    FileName = Path.GetFileName(filePath),
                    FileSize = new FileInfo(filePath).Length,
                    TotalWorksheets = workbook.Worksheets.Count,
                    SitaDeviPandeySheets = worksheetInfo.Count(w => ((dynamic)w).IsSitaDeviPandeyFormat),
                    SupportedSheets = worksheetInfo.Count(w => ((dynamic)w).IsSupported),
                    Worksheets = worksheetInfo,
                    ValidationStatus = "Valid"
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Success = false,
                    Message = "File validation failed",
                    Error = ex.Message,
                    ValidationStatus = "Invalid"
                };
            }
        }

        private bool IsSupportedStandardSheet(string sheetName)
        {
            var supportedNames = new[] { "tenants", "tenant", "payments", "payment", "rooms", "room", 
                                       "agreements", "rent agreements", "bills", "billing", "electric", "meter readings" };
            return supportedNames.Contains(sheetName.ToLower());
        }

        private List<List<string>> GetWorksheetPreview(OfficeOpenXml.ExcelWorksheet worksheet, int maxRows)
        {
            var preview = new List<List<string>>();
            var rowCount = Math.Min(worksheet.Dimension?.Rows ?? 0, maxRows);
            var colCount = Math.Min(worksheet.Dimension?.Columns ?? 0, 10); // Max 10 columns for preview
            
            for (int row = 1; row <= rowCount; row++)
            {
                var rowData = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = worksheet.Cells[row, col]?.Value?.ToString()?.Trim() ?? "";
                    rowData.Add(cellValue.Length > 50 ? cellValue.Substring(0, 50) + "..." : cellValue);
                }
                preview.Add(rowData);
            }
            
            return preview;
        }

        // DELETE: api/DataImport/cleanup
        [HttpDelete("cleanup")]
        public async Task<ActionResult> CleanupUploads()
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = "No uploads folder found",
                        FilesDeleted = 0
                    });
                }

                var files = Directory.GetFiles(uploadsFolder, "*.xlsx");
                var deletedCount = 0;
                var errors = new List<string>();

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        // Delete files older than 7 days
                        if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-7))
                        {
                            System.IO.File.Delete(file);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to delete {Path.GetFileName(file)}: {ex.Message}");
                    }
                }

                return Ok(new
                {
                    Success = true,
                    Message = $"Cleanup completed. Deleted {deletedCount} old files.",
                    FilesDeleted = deletedCount,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during uploads cleanup");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error during cleanup",
                    Error = ex.Message
                });
            }
        }
    }
}