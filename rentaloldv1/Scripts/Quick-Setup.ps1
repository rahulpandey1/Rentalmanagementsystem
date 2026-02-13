# Quick Database Setup Script
# Run this after updating your password in appsettings.json

Write-Host "=== Quick Database Setup - Boarding House Management ===" -ForegroundColor Green
Write-Host ""

# Apply the migration that's already been created
Write-Host "Applying database migration to Azure SQL..." -ForegroundColor Yellow
dotnet ef database update

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "? SUCCESS! Database setup completed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your system now includes:" -ForegroundColor Cyan
    Write-Host "• 22 rooms: G/1-G/10 (ground), 1/1-1/6 (first), 2/1-2/6 (second)" -ForegroundColor White
    Write-Host "• Electric meters for each room" -ForegroundColor White
    Write-Host "• Billing system (?12 per unit)" -ForegroundColor White
    Write-Host "• System configurations" -ForegroundColor White
    Write-Host ""
    Write-Host "?? Ready to start the application:" -ForegroundColor Yellow
    Write-Host "   dotnet run" -ForegroundColor White
    Write-Host ""
    Write-Host "?? Then open: https://localhost:5001" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "? Migration failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure you have:" -ForegroundColor Yellow
    Write-Host "1. Updated the password in appsettings.json" -ForegroundColor White
    Write-Host "2. Your IP is whitelisted in Azure SQL firewall" -ForegroundColor White
    Write-Host "3. The 'Rental' database exists in Azure SQL" -ForegroundColor White
}

Write-Host ""
pause