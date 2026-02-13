# Test Database Connection Script
# This script tests the Entity Framework connection to Azure SQL

Write-Host "=== Testing Azure SQL Database Connection ===" -ForegroundColor Green
Write-Host ""

try {
    Write-Host "Testing Entity Framework connection..." -ForegroundColor Yellow
    
    # Test the database context info
    dotnet ef dbcontext info --verbose
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Database connection successful!" -ForegroundColor Green
        Write-Host ""
        
        # Check if migrations exist
        Write-Host "Checking migration status..." -ForegroundColor Yellow
        dotnet ef migrations list
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Migrations found!" -ForegroundColor Green
            Write-Host ""
            
            # Check if database is up to date
            Write-Host "Checking if database needs updates..." -ForegroundColor Yellow
            $migrationScript = dotnet ef migrations script --idempotent
            
            if ($migrationScript -match "No migrations") {
                Write-Host "? Database is up to date!" -ForegroundColor Green
            } else {
                Write-Host "??  Database needs migration update" -ForegroundColor Yellow
                Write-Host ""
                $apply = Read-Host "Would you like to apply pending migrations? (y/N)"
                if ($apply -eq "y" -or $apply -eq "Y") {
                    Write-Host "Applying migrations..." -ForegroundColor Yellow
                    dotnet ef database update --verbose
                    
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "? Migrations applied successfully!" -ForegroundColor Green
                    } else {
                        Write-Host "? Migration failed!" -ForegroundColor Red
                    }
                }
            }
        }
    } else {
        Write-Host "? Database connection failed!" -ForegroundColor Red
    }
} catch {
    Write-Host "? Error testing connection: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Connection Test Complete ===" -ForegroundColor Green