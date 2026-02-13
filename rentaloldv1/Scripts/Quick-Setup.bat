@echo off
echo === Quick Database Setup - Boarding House Management ===
echo.

echo Applying database migration to Azure SQL...
dotnet ef database update

if %errorlevel% equ 0 (
    echo.
    echo Success! Database setup completed!
    echo.
    echo Your system now includes:
    echo • 22 rooms: G/1-G/10 (ground), 1/1-1/6 (first), 2/1-2/6 (second)
    echo • Electric meters for each room
    echo • Billing system (Rs.12 per unit)
    echo • System configurations
    echo.
    echo Ready to start the application:
    echo    dotnet run
    echo.
    echo Then open: https://localhost:5001
) else (
    echo.
    echo Migration failed!
    echo.
    echo Make sure you have:
    echo 1. Updated the password in appsettings.json
    echo 2. Your IP is whitelisted in Azure SQL firewall
    echo 3. The 'Rental' database exists in Azure SQL
)

echo.
pause