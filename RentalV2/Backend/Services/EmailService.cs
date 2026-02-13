using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RentalBackend.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("EmailClient");
    }

    public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
    {
        try
        {
            // 1. Get OAuth2 token
            var token = await FetchTokenAsync();
            if (token == null)
            {
                _logger.LogError("Failed to acquire OAuth2 token for email sending");
                return false;
            }

            // 2. Build email content
            var htmlBody = BuildOtpHtmlBody(otpCode);
            var textBody = $"Your Rental Management login code is: {otpCode}\n\nThis code expires in 5 minutes.\n\nIf you didn't request this, ignore this email.";

            var payload = new Dictionary<string, object?>
            {
                ["to"] = new[] { toEmail },
                ["subject"] = $"Rental App - Login Code: {otpCode}",
                ["htmlBody"] = htmlBody,
                ["textBody"] = textBody
            };

            var baseUrl = _config["EmailApi:BaseUrl"]?.TrimEnd('/') ?? "";
            var sendPath = _config["EmailApi:SendPath"] ?? "/api/v1/userManagement/emails/send";
            var url = baseUrl + sendPath;

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation("Sending OTP email to {Email} via {Url}", toEmail, url);

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Email API response: {Status} - {Body}", (int)response.StatusCode, body);

            // Check for success - API returns "true" in body on success
            var bodyValue = body.Trim().Trim('"');
            var bodyIsTrue = string.Equals(bodyValue, "true", StringComparison.OrdinalIgnoreCase);

            if (response.IsSuccessStatusCode || bodyIsTrue)
            {
                _logger.LogInformation("OTP email sent successfully to {Email}", toEmail);
                return true;
            }

            _logger.LogError("Email API failed: {Status} {Body}", (int)response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            return false;
        }
    }

    private async Task<string?> FetchTokenAsync()
    {
        try
        {
            var tokenUrl = _config["Auth:TokenUrl"] ?? "";
            var clientId = _config["Auth:ClientId"] ?? "";
            var clientSecret = _config["Auth:ClientSecret"] ?? "";
            var scope = _config["Auth:Scope"] ?? "bulkuserscope";

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = scope
            };

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };

            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            var response = await _httpClient.SendAsync(request);
            _logger.LogInformation("Token request status: {Status}", (int)response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token error: {Error}", err);
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("access_token", out var at))
                return at.GetString();

            _logger.LogError("access_token not found in token response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token request failed");
            return null;
        }
    }

    private static string BuildOtpHtmlBody(string otpCode)
    {
        return $@"
<div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 12px 12px 0 0; text-align: center;'>
        <h1 style='margin: 0; font-size: 24px;'>🏠 Rental Management</h1>
        <p style='margin: 10px 0 0; opacity: 0.9;'>Login Verification</p>
    </div>
    <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 12px 12px;'>
        <p style='color: #333; font-size: 16px;'>Your one-time login code is:</p>
        <div style='background: #f5f5f5; border: 2px dashed #667eea; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;'>
            <span style='font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #333;'>{otpCode}</span>
        </div>
        <p style='color: #666; font-size: 14px;'>This code expires in <strong>5 minutes</strong>.</p>
        <p style='color: #999; font-size: 12px; margin-top: 20px;'>If you didn't request this code, you can safely ignore this email.</p>
    </div>
</div>";
    }
}
