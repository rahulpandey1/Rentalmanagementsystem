# Entity Framework Migration Script for Boarding House Management System
# This script creates the initial database migration for Azure SQL Database

Write-Host "=== Boarding House Management System - Database Migration Script ===" -ForegroundColor Green
Write-Host ""

# Check if dotnet ef tools are installed
Write-Host "Checking if Entity Framework tools are installed..." -ForegroundColor Yellow
$efToolsCheck = dotnet ef --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Entity Framework tools not found. Installing..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install EF tools. Please install manually: dotnet tool install --global dotnet-ef" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Entity Framework tools found: $efToolsCheck" -ForegroundColor Green
}

Write-Host ""

# Remove existing migrations if any
Write-Host "Cleaning up existing migrations..." -ForegroundColor Yellow
if (Test-Path "Migrations") {
    Remove-Item -Recurse -Force "Migrations"
    Write-Host "Existing migrations removed." -ForegroundColor Green
}

Write-Host ""

# Create initial migration
Write-Host "Creating initial migration for Boarding House Management System..." -ForegroundColor Yellow
dotnet ef migrations add InitialCreate --verbose

if ($LASTEXITCODE -eq 0) {
    Write-Host "Initial migration created successfully!" -ForegroundColor Green
} else {
    Write-Host "Failed to create migration. Please check for errors above." -ForegroundColor Red
    exit 1
}

Write-Host ""

# Display migration information
Write-Host "Migration Details:" -ForegroundColor Cyan
Write-Host "- Database: Azure SQL Database (rahulpandey.database.windows.net)" -ForegroundColor White
Write-Host "- Migration Name: InitialCreate" -ForegroundColor White
Write-Host "- Tables to be created:" -ForegroundColor White
Write-Host "  * Properties (Main property information)" -ForegroundColor Gray
Write-Host "  * Rooms (22 rooms: G/1-G/10, 1/1-1/6, 2/1-2/6)" -ForegroundColor Gray
Write-Host "  * Tenants (Tenant information)" -ForegroundColor Gray
Write-Host "  * RentAgreements (Room rental contracts)" -ForegroundColor Gray
Write-Host "  * Payments (Payment records)" -ForegroundColor Gray
Write-Host "  * Bills (Comprehensive billing)" -ForegroundColor Gray
Write-Host "  * BillItems (Bill line items)" -ForegroundColor Gray
Write-Host "  * ElectricMeterReadings (Electric consumption)" -ForegroundColor Gray
Write-Host "  * SystemConfigurations (System settings)" -ForegroundColor Gray

Write-Host ""

# Prompt for password before applying migration
Write-Host "IMPORTANT: Before applying the migration, make sure to:" -ForegroundColor Yellow
Write-Host "1. Replace {your_password} in appsettings.json with your actual Azure SQL password" -ForegroundColor Yellow
Write-Host "2. Ensure your IP address is whitelisted in Azure SQL firewall" -ForegroundColor Yellow
Write-Host "3. Verify the Azure SQL server is accessible" -ForegroundColor Yellow

Write-Host ""
$apply = Read-Host "Do you want to apply the migration to Azure SQL Database now? (y/N)"

if ($apply -eq "y" -or $apply -eq "Y") {
    Write-Host ""
    Write-Host "Applying migration to Azure SQL Database..." -ForegroundColor Yellow
    
    # Apply migration
    dotnet ef database update --verbose
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "=== MIGRATION COMPLETED SUCCESSFULLY! ===" -ForegroundColor Green
        Write-Host ""
        Write-Host "Your Boarding House Management System database is now ready!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "1. Run the application: dotnet run" -ForegroundColor White
        Write-Host "2. Navigate to: https://localhost:5001" -ForegroundColor White
        Write-Host "3. Check API documentation: https://localhost:5001/swagger" -ForegroundColor White
        Write-Host ""
        Write-Host "The system includes:" -ForegroundColor White
        Write-Host "- 22 pre-configured rooms with electric meters" -ForegroundColor Gray
        Write-Host "- System configurations (Electric unit cost: ?12)" -ForegroundColor Gray
        Write-Host "- Sample property data" -ForegroundColor Gray
    } else {
        Write-Host ""
        Write-Host "Migration failed. Please check the error messages above." -ForegroundColor Red
        Write-Host ""
        Write-Host "Common issues:" -ForegroundColor Yellow
        Write-Host "1. Password not updated in appsettings.json" -ForegroundColor Gray
        Write-Host "2. IP address not whitelisted in Azure SQL firewall" -ForegroundColor Gray
        Write-Host "3. Azure SQL server not accessible" -ForegroundColor Gray
        Write-Host "4. Database 'Rental' may not exist (create it first)" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "Migration not applied. To apply later, run:" -ForegroundColor Yellow
    Write-Host "dotnet ef database update" -ForegroundColor White
    Write-Host ""
    Write-Host "Remember to update your password in appsettings.json first!" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Migration script completed." -ForegroundColor Green