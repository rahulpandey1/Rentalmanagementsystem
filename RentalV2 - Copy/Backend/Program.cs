using Microsoft.EntityFrameworkCore;
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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

// Services
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<SeedDataService>();

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
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath),
        RequestPath = ""
    });
}
else 
{
    // Fallback if folder structure is different (e.g. published)
    app.UseStaticFiles();
}

app.UseCors();
app.UseAuthorization();

app.MapControllers();

// Serve index.html for unknown routes (SPA fallback)
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath)
});

// Auto-initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<RentManagementContext>();
        // Check connectivity
        if (await context.Database.CanConnectAsync())
        {
            // Create database schema if it doesn't exist
            await context.Database.EnsureCreatedAsync();

            // First, try to seed from JSON file (authoritative source)
            var seedService = services.GetRequiredService<SeedDataService>();
            var seedPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "seed_data.json");
            await seedService.SeedFromJsonAsync(seedPath);

            // Clean up any incorrectly imported VACANT tenants
            await seedService.CleanupVacantTenantsAsync();
               
            // Sync room availability based on active agreements
            var importService = services.GetRequiredService<ExcelImportService>();
            await importService.SyncRoomAvailabilityAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.Run();
