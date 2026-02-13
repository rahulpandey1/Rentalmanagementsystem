using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentalBackend.Data;
using RentalBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Context
builder.Services.AddDbContext<RentManagementContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions
            .EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null)));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // No tolerance for expiry
    };
});
builder.Services.AddAuthorization();

// Register services
builder.Services.AddSingleton<OtpService>();
builder.Services.AddHttpClient("EmailClient");
builder.Services.AddScoped<EmailService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Serve Frontend files
var frontendPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Frontend");
if (Directory.Exists(frontendPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            Path.GetFullPath(frontendPath)),
        RequestPath = ""
    });
}
else
{
    app.UseStaticFiles();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Serve index.html for unknown routes (SPA fallback) — but NOT for login.html
// Serve index.html for unknown routes (SPA fallback) — but NOT for login.html
if (Directory.Exists(frontendPath))
{
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            Path.GetFullPath(frontendPath))
    });
}
else
{
    // Production: Serve from wwwroot
    app.MapFallbackToFile("index.html");
}

// Verify database connectivity and ensure schema exists
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<RentManagementContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        if (await context.Database.CanConnectAsync())
        {
            logger.LogInformation("Successfully connected to the Building database.");
            
            // Auto-create tables if they don't exist
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database schema verified/created.");
            
            // Diagnostic: list all tables in the database
            using var cmd = context.Database.GetDbConnection().CreateCommand();
            await context.Database.OpenConnectionAsync();
            cmd.CommandText = "SELECT table_schema, table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name";
            using var reader = await cmd.ExecuteReaderAsync();
            logger.LogInformation("=== Tables in database ===");
            while (await reader.ReadAsync())
            {
                logger.LogInformation("  Table: {Schema}.{Table}", reader.GetString(0), reader.GetString(1));
            }
            logger.LogInformation("=== End tables ===");
            await context.Database.CloseConnectionAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while connecting to the database.");
    }
}

app.Run();
