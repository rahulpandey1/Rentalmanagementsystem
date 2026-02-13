using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RentalBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        // Default settings (no SystemConfiguration table in the new DB)
        private static readonly Dictionary<string, string> _defaults = new()
        {
            { "ElectricRatePerUnit", "8.0" },
            { "BillDueDays", "7" },
            { "LateFeePercentage", "5" }
        };

        [HttpGet]
        public ActionResult<Dictionary<string, string>> GetSettings()
        {
            return Ok(_defaults);
        }

        [HttpGet("{key}")]
        public ActionResult<string> GetSetting(string key)
        {
            if (_defaults.TryGetValue(key, out var value))
                return Ok(value);
            return NotFound();
        }

        [HttpPut("{key}")]
        public ActionResult UpdateSetting(string key, [FromBody] SettingUpdateRequest request)
        {
            // In-memory only for now since we don't have a settings table
            if (_defaults.ContainsKey(key))
                _defaults[key] = request.Value;
            else
                _defaults[key] = request.Value;

            return Ok();
        }
    }

    public class SettingUpdateRequest
    {
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
