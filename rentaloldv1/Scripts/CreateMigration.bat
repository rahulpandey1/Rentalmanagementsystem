@echo off
echo === Boarding House Management System - Database Migration Script ===
echo.

REM Check if dotnet ef tools are installed
echo Checking if Entity Framework tools are installed...
dotnet ef --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Entity Framework tools not found. Installing...
    dotnet tool install --global dotnet-ef
    if %errorlevel% neq 0 (
        echo Failed to install EF tools. Please install manually: dotnet tool install --global dotnet-ef
        pause
        exit /b 1
    )
) else (
    echo Entity Framework tools found.
)

echo.

REM Remove existing migrations if any
echo Cleaning up existing migrations...
if exist "Migrations" (
    rmdir /s /q "Migrations"
    echo Existing migrations removed.
)

echo.

REM Create initial migration
echo Creating initial migration for Boarding House Management System...
dotnet ef migrations add InitialCreate --verbose

if %errorlevel% equ 0 (
    echo Initial migration created successfully!
) else (
    echo Failed to create migration. Please check for errors above.
    pause
    exit /b 1
)

echo.
echo Migration Details:
echo - Database: Azure SQL Database (rahulpandey.database.windows.net)
echo - Migration Name: InitialCreate
echo - Tables to be created:
echo   * Properties (Main property information)
echo   * Rooms (22 rooms: G/1-G/10, 1/1-1/6, 2/1-2/6)
echo   * Tenants (Tenant information)
echo   * RentAgreements (Room rental contracts)
echo   * Payments (Payment records)
echo   * Bills (Comprehensive billing)
echo   * BillItems (Bill line items)
echo   * ElectricMeterReadings (Electric consumption)
echo   * SystemConfigurations (System settings)

echo.
echo IMPORTANT: Before applying the migration, make sure to:
echo 1. Replace {your_password} in appsettings.json with your actual Azure SQL password
echo 2. Ensure your IP address is whitelisted in Azure SQL firewall
echo 3. Verify the Azure SQL server is accessible

echo.
set /p apply="Do you want to apply the migration to Azure SQL Database now? (y/N): "

if /i "%apply%"=="y" (
    echo.
    echo Applying migration to Azure SQL Database...
    
    dotnet ef database update --verbose
    
    if %errorlevel% equ 0 (
        echo.
        echo === MIGRATION COMPLETED SUCCESSFULLY! ===
        echo.
        echo Your Boarding House Management System database is now ready!
        echo.
        echo Next steps:
        echo 1. Run the application: dotnet run
        echo 2. Navigate to: https://localhost:5001
        echo 3. Check API documentation: https://localhost:5001/swagger
        echo.
        echo The system includes:
        echo - 22 pre-configured rooms with electric meters
        echo - System configurations (Electric unit cost: Rs.12)
        echo - Sample property data
    ) else (
        echo.
        echo Migration failed. Please check the error messages above.
        echo.
        echo Common issues:
        echo 1. Password not updated in appsettings.json
        echo 2. IP address not whitelisted in Azure SQL firewall
        echo 3. Azure SQL server not accessible
        echo 4. Database 'Rental' may not exist (create it first)
    )
) else (
    echo.
    echo Migration not applied. To apply later, run:
    echo dotnet ef database update
    echo.
    echo Remember to update your password in appsettings.json first!
)

echo.
echo Migration script completed.
pause