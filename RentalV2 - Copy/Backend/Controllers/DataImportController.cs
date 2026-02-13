using Microsoft.AspNetCore.Mvc;
using RentalBackend.Services;

namespace RentalBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataImportController : ControllerBase
    {
        private readonly ExcelImportService _importService;
        private readonly ILogger<DataImportController> _logger;

        public DataImportController(ExcelImportService importService, ILogger<DataImportController> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var tempPath = Path.GetTempFileName();
            
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var result = await _importService.ImportPaymentDataFromExcel(tempPath);
                
                if (result.Success)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                    System.IO.File.Delete(tempPath);
            }
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncRoomAvailability()
        {
            await _importService.SyncRoomAvailabilityAsync();
            return Ok(new { message = "Room availability synchronized successfully." });
        }
    }
}
