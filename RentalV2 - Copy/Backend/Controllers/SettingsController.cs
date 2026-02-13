using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalBackend.Data;
using RentalBackend.Models;

namespace RentalBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly RentManagementContext _context;

        public SettingsController(RentManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all settings
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<Dictionary<string, string>>> GetSettings()
        {
            var settings = await _context.SystemConfigurations.ToListAsync();
            var result = settings.ToDictionary(s => s.ConfigKey, s => s.ConfigValue ?? "");
            
            // Ensure default values exist
            if (!result.ContainsKey("ElectricRatePerUnit"))
            {
                result["ElectricRatePerUnit"] = "8.0";
                await EnsureSetting("ElectricRatePerUnit", "8.0", "Electricity rate per unit (kWh)");
            }
            if (!result.ContainsKey("LateFeePercentage"))
            {
                result["LateFeePercentage"] = "5";
                await EnsureSetting("LateFeePercentage", "5", "Late fee percentage after due date");
            }
            if (!result.ContainsKey("BillDueDays"))
            {
                result["BillDueDays"] = "7";
                await EnsureSetting("BillDueDays", "7", "Number of days after bill generation for due date");
            }

            return Ok(result);
        }

        /// <summary>
        /// Get specific setting
        /// </summary>
        [HttpGet("{key}")]
        public async Task<ActionResult<string>> GetSetting(string key)
        {
            var setting = await _context.SystemConfigurations.FirstOrDefaultAsync(s => s.ConfigKey == key);
            if (setting == null)
            {
                return NotFound();
            }
            return Ok(setting.ConfigValue);
        }

        /// <summary>
        /// Update a setting
        /// </summary>
        [HttpPut("{key}")]
        public async Task<ActionResult> UpdateSetting(string key, [FromBody] SettingUpdateRequest request)
        {
            var setting = await _context.SystemConfigurations.FirstOrDefaultAsync(s => s.ConfigKey == key);
            if (setting == null)
            {
                setting = new SystemConfiguration
                {
                    ConfigKey = key,
                    ConfigValue = request.Value,
                    Description = request.Description,
                    LastUpdated = DateTime.UtcNow
                };
                _context.SystemConfigurations.Add(setting);
            }
            else
            {
                setting.ConfigValue = request.Value;
                setting.LastUpdated = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(request.Description))
                    setting.Description = request.Description;
            }

            await _context.SaveChangesAsync();
            return Ok(new { key = key, value = request.Value });
        }

        /// <summary>
        /// Calculate electric charges based on readings
        /// </summary>
        [HttpPost("calculate-electric")]
        public async Task<ActionResult> CalculateElectricCharges([FromBody] ElectricCalculateRequest request)
        {
            var rateSetting = await _context.SystemConfigurations.FirstOrDefaultAsync(s => s.ConfigKey == "ElectricRatePerUnit");
            var rate = decimal.Parse(rateSetting?.ConfigValue ?? "8.0");

            var unitsConsumed = request.CurrentReading - request.PreviousReading;
            var charges = unitsConsumed * rate;

            return Ok(new
            {
                previousReading = request.PreviousReading,
                currentReading = request.CurrentReading,
                unitsConsumed = unitsConsumed,
                ratePerUnit = rate,
                totalCharges = charges
            });
        }

        private async Task EnsureSetting(string key, string value, string description)
        {
            var exists = await _context.SystemConfigurations.AnyAsync(s => s.ConfigKey == key);
            if (!exists)
            {
                _context.SystemConfigurations.Add(new SystemConfiguration
                {
                    ConfigKey = key,
                    ConfigValue = value,
                    Description = description,
                    LastUpdated = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }
    }

    public class SettingUpdateRequest
    {
        public string Value { get; set; } = "";
        public string? Description { get; set; }
    }

    public class ElectricCalculateRequest
    {
        public int PreviousReading { get; set; }
        public int CurrentReading { get; set; }
    }
}
