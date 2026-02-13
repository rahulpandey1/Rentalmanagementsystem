using System.Collections.Concurrent;

namespace RentalBackend.Services;

public class OtpService
{
    private readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _otpStore = new();
    private readonly ILogger<OtpService> _logger;

    public OtpService(ILogger<OtpService> logger)
    {
        _logger = logger;
    }

    public string GenerateOtp(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var code = Random.Shared.Next(100000, 999999).ToString();
        var expiry = DateTime.UtcNow.AddMinutes(5);

        _otpStore[normalizedEmail] = (code, expiry);
        _logger.LogInformation("OTP generated: {Code} for {Email}, expires at {Expiry}", code, normalizedEmail, expiry);

        return code;
    }

    public bool ValidateOtp(string email, string code)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!_otpStore.TryRemove(normalizedEmail, out var stored))
        {
            _logger.LogWarning("No OTP found for {Email}", normalizedEmail);
            return false;
        }

        if (DateTime.UtcNow > stored.Expiry)
        {
            _logger.LogWarning("OTP expired for {Email}", normalizedEmail);
            return false;
        }

        if (stored.Code != code.Trim())
        {
            _logger.LogWarning("Invalid OTP for {Email}", normalizedEmail);
            // Put it back so they can retry
            _otpStore[normalizedEmail] = stored;
            return false;
        }

        _logger.LogInformation("OTP validated for {Email}", normalizedEmail);
        return true;
    }
}
