using Microsoft.EntityFrameworkCore;
using RentMangementsystem.Data;
using RentMangementsystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllersWithViews(); // Add MVC support

// Add Entity Framework with retry policy for Azure SQL
builder.Services.AddDbContext<RentManagementContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        npgsqlOptions => npgsqlOptions
            .EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null)));

// Register ExcelImportService
builder.Services.AddScoped<ExcelImportService>();

// Add API Explorer services for documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles(); // Enable default files (index.html)
app.UseStaticFiles(); // Enable static files

app.UseCors();

app.UseRouting();
app.UseAuthorization();

app.MapControllers(); // Map API controllers

// Fallback to index.html for Angular routing
app.MapFallbackToFile("index.html");

// Apply pending migrations and seed data with improved error handling
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Starting database migration...");
        var context = services.GetRequiredService<RentManagementContext>();
        
        // Test connection first
        logger.LogInformation("Testing database connection...");
        var canConnect = await context.Database.CanConnectAsync();
        
        if (!canConnect)
        {
            logger.LogError("Cannot connect to database. Please check your connection string and Azure SQL firewall settings.");
            throw new Exception("Database connection failed");
        }
        
        logger.LogInformation("Database connection successful. Checking for pending migrations...");
        
        // Check if there are pending migrations
        // var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        
        // if (pendingMigrations.Any())
        // {
        //     logger.LogInformation($"Found {pendingMigrations.Count()} pending migrations. Applying...");
        //     await context.Database.MigrateAsync();
        //     logger.LogInformation("Database migrations applied successfully!");
        // }
        // else
        // {
        //     logger.LogInformation("Database is up to date - no pending migrations.");
        // }
        
        // Verify the database has the expected tables
        var tableExists = await context.Database.ExecuteSqlRawAsync(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Properties'") >= 0;
        
        logger.LogInformation("Database setup completed successfully!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up the database.");
        logger.LogError("Please ensure:");
        logger.LogError("1. The 'Rental' database exists in Azure SQL");
        logger.LogError("2. Your IP address is whitelisted in the Azure SQL firewall");
        logger.LogError("3. The connection string in appsettings.json is correct");
        logger.LogError("4. The user 'rahuladmin' has the necessary permissions");
        
        // Don't stop the application - let it start without database
        logger.LogWarning("Starting application without database connection...");
    }
}

app.Run();
