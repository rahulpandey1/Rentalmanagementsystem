# ====================================================================
# Boarding House Management System - Complete Database Setup Script
# ====================================================================
# This script handles the complete setup of Azure SQL Database for the
# 22-room boarding house management system with billing capabilities
# ====================================================================

param(
    [Parameter(Mandatory=$false)]
    [string]$Password = $null,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipPasswordPrompt = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$ApplyMigration = $false
)

# Script configuration
$ErrorActionPreference = "Stop"
$ServerName = "rahulpandey.database.windows.net"
$DatabaseName = "Rental"
$Username = "rahuladmin"

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "    Boarding House Management System - Database Setup" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target Database Configuration:" -ForegroundColor Yellow
Write-Host "  Server: $ServerName" -ForegroundColor White
Write-Host "  Database: $DatabaseName" -ForegroundColor White
Write-Host "  Username: $Username" -ForegroundColor White
Write-Host ""

# Function to check if EF tools are installed
function Test-EFTools {
    try {
        $version = dotnet ef --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Entity Framework tools found: $version" -ForegroundColor Green
            return $true
        }
    }
    catch {
        return $false
    }
    return $false
}

# Function to install EF tools
function Install-EFTools {
    Write-Host "Installing Entity Framework tools..." -ForegroundColor Yellow
    try {
        dotnet tool install --global dotnet-ef
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Entity Framework tools installed successfully" -ForegroundColor Green
            return $true
        } else {
            Write-Host "? Failed to install Entity Framework tools" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "? Error installing Entity Framework tools: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to update connection string
function Update-ConnectionString {
    param([string]$Password)
    
    $connectionString = "Server=tcp:$ServerName,1433;Initial Catalog=$DatabaseName;Persist Security Info=False;User ID=$Username;Password=$Password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    
    try {
        # Update appsettings.json
        $appSettingsPath = "appsettings.json"
        if (Test-Path $appSettingsPath) {
            $json = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
            $json.ConnectionStrings.DefaultConnection = $connectionString
            $json | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
            Write-Host "? Updated $appSettingsPath" -ForegroundColor Green
        }
        
        # Update appsettings.Development.json
        $devSettingsPath = "appsettings.Development.json"
        if (Test-Path $devSettingsPath) {
            $devJson = Get-Content $devSettingsPath -Raw | ConvertFrom-Json
            $devJson.ConnectionStrings.DefaultConnection = $connectionString
            $devJson | ConvertTo-Json -Depth 10 | Set-Content $devSettingsPath
            Write-Host "? Updated $devSettingsPath" -ForegroundColor Green
        }
        
        return $true
    }
    catch {
        Write-Host "? Error updating connection strings: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to test database connection
function Test-DatabaseConnection {
    Write-Host "Testing database connection..." -ForegroundColor Yellow
    try {
        dotnet ef dbcontext info --no-build | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Database connection successful" -ForegroundColor Green
            return $true
        } else {
            Write-Host "? Database connection failed" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "? Database connection test failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to create migration
function New-Migration {
    Write-Host "Creating database migration..." -ForegroundColor Yellow
    
    # Clean existing migrations if any
    if (Test-Path "Migrations") {
        Write-Host "Removing existing migrations..." -ForegroundColor Yellow
        Remove-Item -Recurse -Force "Migrations"
    }
    
    try {
        dotnet ef migrations add InitialCreate
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Migration created successfully" -ForegroundColor Green
            
            # Generate SQL script
            Write-Host "Generating SQL script..." -ForegroundColor Yellow
            dotnet ef migrations script --output "Scripts/InitialCreate.sql"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "? SQL script generated: Scripts/InitialCreate.sql" -ForegroundColor Green
            }
            
            return $true
        } else {
            Write-Host "? Failed to create migration" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "? Migration creation failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to apply migration
function Invoke-Migration {
    Write-Host "Applying migration to Azure SQL Database..." -ForegroundColor Yellow
    try {
        dotnet ef database update --verbose
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Migration applied successfully!" -ForegroundColor Green
            return $true
        } else {
            Write-Host "? Migration failed" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "? Migration application failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Main execution
try {
    # Step 1: Check and install EF tools
    Write-Host "Step 1: Checking Entity Framework tools..." -ForegroundColor Cyan
    if (-not (Test-EFTools)) {
        if (-not (Install-EFTools)) {
            throw "Failed to install Entity Framework tools"
        }
    }
    
    # Step 2: Get password if not provided
    if (-not $Password -and -not $SkipPasswordPrompt) {
        Write-Host ""
        Write-Host "Step 2: Database Authentication" -ForegroundColor Cyan
        $SecurePassword = Read-Host "Enter Azure SQL password for user '$Username'" -AsSecureString
        $Password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword))
    }
    
    # Step 3: Update connection strings
    if ($Password) {
        Write-Host ""
        Write-Host "Step 3: Updating connection strings..." -ForegroundColor Cyan
        if (-not (Update-ConnectionString -Password $Password)) {
            throw "Failed to update connection strings"
        }
    } else {
        Write-Host ""
        Write-Host "Step 3: Skipping connection string update (no password provided)" -ForegroundColor Yellow
        Write-Host "Please manually update the password in appsettings.json before applying migration" -ForegroundColor Yellow
    }
    
    # Step 4: Build project
    Write-Host ""
    Write-Host "Step 4: Building project..." -ForegroundColor Cyan
    dotnet build --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Project build failed"
    }
    Write-Host "? Project built successfully" -ForegroundColor Green
    
    # Step 5: Create migration
    Write-Host ""
    Write-Host "Step 5: Creating database migration..." -ForegroundColor Cyan
    if (-not (New-Migration)) {
        throw "Failed to create migration"
    }
    
    # Step 6: Test connection and apply migration
    if ($Password -and ($ApplyMigration -or -not $SkipPasswordPrompt)) {
        Write-Host ""
        Write-Host "Step 6: Database deployment..." -ForegroundColor Cyan
        
        if (Test-DatabaseConnection) {
            if ($ApplyMigration) {
                $applyNow = "y"
            } else {
                $applyNow = Read-Host "Apply migration to Azure SQL Database now? (y/N)"
            }
            
            if ($applyNow -eq "y" -or $applyNow -eq "Y") {
                if (Invoke-Migration) {
                    Write-Host ""
                    Write-Host "=====================================================================" -ForegroundColor Green
                    Write-Host "               ?? DATABASE SETUP COMPLETED SUCCESSFULLY! ??" -ForegroundColor Green
                    Write-Host "=====================================================================" -ForegroundColor Green
                    Write-Host ""
                    Write-Host "Your Boarding House Management System is ready!" -ForegroundColor Green
                    Write-Host ""
                    Write-Host "Database includes:" -ForegroundColor Cyan
                    Write-Host "  ? 22 pre-configured rooms (G/1-G/10, 1/1-1/6, 2/1-2/6)" -ForegroundColor White
                    Write-Host "  ? Electric meters for each room" -ForegroundColor White
                    Write-Host "  ? Billing system with ?12 per unit electric cost" -ForegroundColor White
                    Write-Host "  ? Complete property and system configuration" -ForegroundColor White
                    Write-Host ""
                    Write-Host "Next steps:" -ForegroundColor Cyan
                    Write-Host "  1. Run the application: dotnet run" -ForegroundColor Yellow
                    Write-Host "  2. Open browser: https://localhost:5001" -ForegroundColor Yellow
                    Write-Host "  3. API documentation: https://localhost:5001/swagger" -ForegroundColor Yellow
                } else {
                    throw "Migration application failed"
                }
            } else {
                Write-Host ""
                Write-Host "Migration not applied. To apply later:" -ForegroundColor Yellow
                Write-Host "  dotnet ef database update" -ForegroundColor White
            }
        } else {
            Write-Host ""
            Write-Host "??  Connection test failed. Common issues:" -ForegroundColor Yellow
            Write-Host "  • Verify the password is correct" -ForegroundColor White
            Write-Host "  • Check if your IP is whitelisted in Azure SQL firewall" -ForegroundColor White
            Write-Host "  • Ensure the database '$DatabaseName' exists" -ForegroundColor White
            Write-Host "  • Verify Azure SQL server is accessible" -ForegroundColor White
        }
    } else {
        Write-Host ""
        Write-Host "Skipping database deployment. Migration files created successfully." -ForegroundColor Yellow
        Write-Host "To apply migration later, update password in appsettings.json and run:" -ForegroundColor Yellow
        Write-Host "  dotnet ef database update" -ForegroundColor White
    }
    
}
catch {
    Write-Host ""
    Write-Host "=====================================================================" -ForegroundColor Red
    Write-Host "                           ? SETUP FAILED" -ForegroundColor Red
    Write-Host "=====================================================================" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please check the error above and try again." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "                       Setup script completed" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan