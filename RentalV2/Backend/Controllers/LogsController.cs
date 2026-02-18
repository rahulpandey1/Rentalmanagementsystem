using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalBackend.Services;

namespace RentalBackend.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogger<LogsController> _logger;
    private readonly AuditService _auditService;

    public LogsController(ILogger<LogsController> logger, AuditService auditService)
    {
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Download all log files as a ZIP archive
    /// </summary>
    [HttpGet("download")]
    public async Task<IActionResult> DownloadLogs()
    {
        // Audit the download action
        await _auditService.LogAsync("Download", "FileLogs", "LogFiles");

        var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

        if (!Directory.Exists(logsPath))
        {
            return NotFound(new { message = "No log files found." });
        }

        var logFiles = Directory.GetFiles(logsPath, "*.txt");

        if (logFiles.Length == 0)
        {
            return NotFound(new { message = "No log files found." });
        }

        var zipFileName = $"RentalLogs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
        var tempZipPath = Path.Combine(Path.GetTempPath(), zipFileName);

        try
        {
            // Create ZIP file
            using (var zipArchive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                foreach (var logFile in logFiles)
                {
                    // Copy file to a temp location to avoid file lock issues
                    var tempCopy = Path.GetTempFileName();
                    System.IO.File.Copy(logFile, tempCopy, true);
                    zipArchive.CreateEntryFromFile(tempCopy, Path.GetFileName(logFile));
                    System.IO.File.Delete(tempCopy);
                }
            }

            // Read the ZIP file to memory and delete the temp file
            var zipBytes = await System.IO.File.ReadAllBytesAsync(tempZipPath);

            _logger.LogInformation("Log files downloaded: {Count} files, {Size} bytes", logFiles.Length, zipBytes.Length);

            return File(zipBytes, "application/zip", zipFileName);
        }
        finally
        {
            // Clean up temp zip file
            if (System.IO.File.Exists(tempZipPath))
            {
                try { System.IO.File.Delete(tempZipPath); }
                catch { /* best effort cleanup */ }
            }
        }
    }
}
