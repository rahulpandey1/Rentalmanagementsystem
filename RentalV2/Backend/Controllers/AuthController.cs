using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RentalBackend.Services;

namespace RentalBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly OtpService _otpService;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration config, OtpService otpService, EmailService emailService, ILogger<AuthController> logger)
    {
        _config = config;
        _otpService = otpService;
        _emailService = emailService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] OtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required" });

        var email = request.Email.Trim().ToLowerInvariant();

        // Check if email is in allowed list
        var allowedEmailsSection = _config.GetSection("AllowedEmails");
        var allowedEmails = allowedEmailsSection.Get<string[]>()?.ToList() ?? new List<string>();
        
        // Also support comma-separated string for easier Azure configuration
        var allowedEmailsString = _config["AllowedEmailsString"];
        if (!string.IsNullOrWhiteSpace(allowedEmailsString))
        {
            allowedEmails.AddRange(allowedEmailsString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        if (!allowedEmails.Any(e => e.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Unauthorized login attempt from {Email}", email);
            return StatusCode(403, new { message = "This email is not authorized to access the application." });
        }

        // Generate OTP
        var otp = _otpService.GenerateOtp(email);

        // Send OTP via email
        var sent = await _emailService.SendOtpEmailAsync(email, otp);
        if (!sent)
        {
            _logger.LogError("Failed to send OTP email to {Email}", email);
            return StatusCode(500, new { message = "Failed to send verification code. Please try again." });
        }

        _logger.LogInformation("OTP sent successfully to {Email}", email);
        return Ok(new { message = "Verification code sent to your email." });
    }

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Email and code are required" });

        var email = request.Email.Trim().ToLowerInvariant();

        if (!_otpService.ValidateOtp(email, request.Code))
        {
            return Unauthorized(new { message = "Invalid or expired verification code." });
        }

        // Generate JWT token
        var token = GenerateJwtToken(email);
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        _logger.LogInformation("Login successful for {Email}", email);
        return Ok(new
        {
            token,
            email,
            expiresIn = expiryMinutes * 60, // seconds
            message = "Login successful"
        });
    }

    [AllowAnonymous]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { message = "Not authenticated" });

        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Ok(new { email, authenticated = true });
    }

    private string GenerateJwtToken(string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class OtpRequest
{
    public string Email { get; set; } = "";
}

public class VerifyOtpRequest
{
    public string Email { get; set; } = "";
    public string Code { get; set; } = "";
}
