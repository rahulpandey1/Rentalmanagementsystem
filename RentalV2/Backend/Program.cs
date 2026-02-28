using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentalBackend.Data;
using RentalBackend.Filters;
using RentalBackend.Middleware;
using RentalBackend.Services;
using Serilog;

// Configure Serilog early for startup logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Rental Management Application");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog from configuration
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "RentalManagement")
        .WriteTo.Console());

    // Add services to the container.
    builder.Services.AddControllers(options =>
        {
            // Register the global audit action filter
            options.Filters.Add<AuditActionFilter>();
        })
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

    // Admin authorization policy (email-based)
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy =>
        {
            policy.RequireAssertion(context =>
            {
                var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(email)) return false;

                var config = context.Resource is HttpContext httpContext
                    ? httpContext.RequestServices.GetRequiredService<IConfiguration>()
                    : null;
                var adminEmails = config?.GetSection("AdminEmails").Get<string[]>() ?? Array.Empty<string>();
                return adminEmails.Any(a => a.Equals(email, StringComparison.OrdinalIgnoreCase));
            });
        });
    });

    // Register services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<AuditService>();
    builder.Services.AddScoped<AuditActionFilter>();
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

    // Correlation ID middleware — must be early in the pipeline
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"]?.ToString() ?? "N/A");
            diagnosticContext.Set("UserId", httpContext.User?.FindFirst(ClaimTypes.Email)?.Value ?? "Anonymous");
        };
    });

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

                // Ensure AuditLogs table exists (EnsureCreated won't add new tables to existing DB)
                try
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS public.""AuditLogs"" (
                            ""Id"" uuid NOT NULL PRIMARY KEY,
                            ""UserId"" varchar(255) NOT NULL,
                            ""UserName"" varchar(255) NOT NULL,
                            ""Role"" varchar(50) NOT NULL,
                            ""Action"" varchar(100) NOT NULL,
                            ""ModuleName"" varchar(100) NOT NULL,
                            ""EntityName"" varchar(100) NOT NULL,
                            ""EntityId"" varchar(255),
                            ""OldValues"" text,
                            ""NewValues"" text,
                            ""IpAddress"" varchar(50),
                            ""BrowserInfo"" varchar(500),
                            ""RequestUrl"" varchar(500),
                            ""CorrelationId"" varchar(100),
                            ""CreatedDateTime"" timestamp with time zone NOT NULL DEFAULT NOW()
                        );
                        CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_CreatedDateTime"" ON public.""AuditLogs"" (""CreatedDateTime"");
                        CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_UserId"" ON public.""AuditLogs"" (""UserId"");
                        CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_Action"" ON public.""AuditLogs"" (""Action"");
                        CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_ModuleName"" ON public.""AuditLogs"" (""ModuleName"");
                    ");
                    logger.LogInformation("AuditLogs table verified/created.");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "AuditLogs table creation SQL completed with warning (table may already exist).");
                }

                // Add new columns for meter & billing enhancements
                try
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$ BEGIN
                            ALTER TABLE public.""Flats"" ADD COLUMN IF NOT EXISTS ""MeterId"" varchar(100);
                            ALTER TABLE public.""Flats"" ADD COLUMN IF NOT EXISTS ""BaseRent"" numeric(12,2) NOT NULL DEFAULT 0;
                            ALTER TABLE public.""MonthlyLedgers"" ADD COLUMN IF NOT EXISTS ""MiscChargeName"" varchar(255);
                            ALTER TABLE public.""MonthlyLedgers"" ADD COLUMN IF NOT EXISTS ""InvoiceNumber"" varchar(20);
                        END $$;
                        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MonthlyLedgers_InvoiceNumber"" 
                            ON public.""MonthlyLedgers"" (""InvoiceNumber"") WHERE ""InvoiceNumber"" IS NOT NULL;
                    ");
                    logger.LogInformation("Meter & billing columns verified/added.");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Meter & billing columns migration completed with warning.");
                }

                // Tenant profile & documents migration
                try
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$ BEGIN
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""FatherName"" varchar(255);
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""Phone"" varchar(20);
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""Email"" varchar(255);
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""AadhaarNumber"" varchar(20);
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""PanNumber"" varchar(20);
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""PermanentAddress"" varchar(1000);
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""EmergencyContact"" varchar(255);
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""EmergencyPhone"" varchar(20);
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""TentativeRoomCode"" varchar(50);
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""TentativeRent"" numeric(12,2) NOT NULL DEFAULT 0;
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""Notes"" varchar(2000);
                        END $$;

                        CREATE TABLE IF NOT EXISTS public.""TenantDocuments"" (
                            ""DocumentId"" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                            ""TenantId"" uuid NOT NULL REFERENCES public.""Tenants""(""TenantId"") ON DELETE CASCADE,
                            ""DocumentType"" varchar(50) NOT NULL DEFAULT 'Other',
                            ""FileName"" varchar(255) NOT NULL DEFAULT '',
                            ""FilePath"" varchar(500) NOT NULL DEFAULT '',
                            ""UploadedUtc"" timestamp NOT NULL DEFAULT now()
                        );
                    ");
                    logger.LogInformation("Tenant profile columns & documents table verified/added.");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Tenant profile migration completed with warning.");
                }

                // Security deposit migration
                try
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$ BEGIN
                            ALTER TABLE public.""Tenants"" ADD COLUMN IF NOT EXISTS ""SecurityDeposit"" numeric(12,2) NOT NULL DEFAULT 0;
                        END $$;

                        CREATE TABLE IF NOT EXISTS public.""SecurityDepositTransactions"" (
                            ""TransactionId"" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                            ""TenantId"" uuid NOT NULL REFERENCES public.""Tenants""(""TenantId"") ON DELETE CASCADE,
                            ""Amount"" numeric(12,2) NOT NULL DEFAULT 0,
                            ""Type"" varchar(50) NOT NULL DEFAULT 'Collection',
                            ""Description"" varchar(500),
                            ""CreatedUtc"" timestamp NOT NULL DEFAULT now()
                        );
                    ");
                    logger.LogInformation("Security deposit columns & transactions table verified/added.");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Security deposit migration completed with warning.");
                }

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

    // Schedule audit log cleanup (runs every 24 hours)
    _ = Task.Run(async () =>
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(24));
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<RentManagementContext>();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var retentionDays = config.GetValue<int>("AuditRetentionDays", 90);
                var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

                var deleted = await context.Database.ExecuteSqlRawAsync(
                    @"DELETE FROM public.""AuditLogs"" WHERE ""CreatedDateTime"" < {0}", cutoff);
                Log.Information("Audit cleanup: deleted {Count} records older than {Days} days", deleted, retentionDays);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Audit cleanup task failed");
            }
        }
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
