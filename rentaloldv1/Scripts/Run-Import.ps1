# Direct Excel Import Script
# This script directly calls the import service to process the Excel file

Write-Host "=== Direct Excel Import ===" -ForegroundColor Green
Write-Host ""

# Check if Data directory exists
if (!(Test-Path "Data")) {
    Write-Host "? Data directory not found" -ForegroundColor Red
    exit 1
}

# Look for Excel files
$excelFiles = Get-ChildItem -Path "Data" -Filter "*.xlsx"

if ($excelFiles.Count -eq 0) {
    Write-Host "? No Excel files found in Data directory" -ForegroundColor Red
    exit 1
}

Write-Host "?? Found Excel files:" -ForegroundColor Cyan
foreach ($file in $excelFiles) {
    $size = [math]::Round($file.Length / 1024, 2)
    Write-Host "  - $($file.Name) (${size} KB)" -ForegroundColor Gray
}

# Select the first Excel file or the non-password file
$targetFile = $excelFiles | Where-Object { $_.Name -match "NoPassword" } | Select-Object -First 1
if (!$targetFile) {
    $targetFile = $excelFiles[0]
}

Write-Host ""
Write-Host "?? Using file: $($targetFile.Name)" -ForegroundColor Yellow
Write-Host ""

# Start the import process by running the application and calling the API
Write-Host "Starting import process..." -ForegroundColor Green

try {
    # First, build the project
    Write-Host "Building project..." -ForegroundColor Yellow
    $buildResult = dotnet build --configuration Release --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "? Build successful" -ForegroundColor Green

    # Start the application in background
    Write-Host "Starting application..." -ForegroundColor Yellow
    $job = Start-Job -ScriptBlock {
        Set-Location $using:PWD
        dotnet run --configuration Release --no-build --no-restore
    }

    # Wait for application to start
    Write-Host "Waiting for application to start..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10

    # Test if application is running
    $maxRetries = 30
    $retryCount = 0
    $appStarted = $false

    while ($retryCount -lt $maxRetries -and !$appStarted) {
        try {
            $response = Invoke-WebRequest -Uri "https://localhost:5001/api/DataImport/status" -Method GET -SkipCertificateCheck -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                $appStarted = $true
                Write-Host "? Application started successfully" -ForegroundColor Green
            }
        }
        catch {
            $retryCount++
            Write-Host "." -NoNewline -ForegroundColor Yellow
            Start-Sleep -Seconds 2
        }
    }

    if (!$appStarted) {
        Write-Host ""
        Write-Host "? Application failed to start within timeout" -ForegroundColor Red
        Stop-Job -Job $job
        Remove-Job -Job $job
        exit 1
    }

    Write-Host ""
    Write-Host "?? Running Excel import..." -ForegroundColor Green

    # Call the import API
    try {
        $importResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/DataImport/excel" -Method POST -SkipCertificateCheck

        if ($importResponse.Success) {
            Write-Host ""
            Write-Host "?? Import Successful!" -ForegroundColor Green
            Write-Host "Duration: $($importResponse.ImportStats.Duration)" -ForegroundColor White
            Write-Host ""
            Write-Host "?? Import Results:" -ForegroundColor Cyan
            Write-Host "  Tenants: $($importResponse.ImportedData.Tenants)" -ForegroundColor White
            Write-Host "  Payments: $($importResponse.ImportedData.Payments)" -ForegroundColor White
            Write-Host "  Bills: $($importResponse.ImportedData.Bills)" -ForegroundColor White
            Write-Host "  Electric Readings: $($importResponse.ImportedData.ElectricReadings)" -ForegroundColor White
            Write-Host "  Agreements: $($importResponse.ImportedData.Agreements)" -ForegroundColor White
            Write-Host "  Total Records: $($importResponse.ImportedData.Total)" -ForegroundColor Yellow

            if ($importResponse.ImportStats.BillPeriod) {
                Write-Host ""
                Write-Host "?? Bill Period: $($importResponse.ImportStats.BillPeriod)" -ForegroundColor Cyan
            }

            if ($importResponse.ImportStats.VacantRoomsFound -gt 0) {
                Write-Host "?? Vacant Rooms Found: $($importResponse.ImportStats.VacantRoomsFound)" -ForegroundColor Cyan
            }

            if ($importResponse.Errors -and $importResponse.Errors.Count -gt 0) {
                Write-Host ""
                Write-Host "??  Warnings:" -ForegroundColor Yellow
                foreach ($error in $importResponse.Errors) {
                    Write-Host "  - $error" -ForegroundColor Gray
                }
            }

            Write-Host ""
            Write-Host "?? You can now view the imported data at:" -ForegroundColor Green
            Write-Host "  Dashboard: https://localhost:5001/Dashboard" -ForegroundColor White
            Write-Host "  Data Import: https://localhost:5001/Dashboard/DataImport" -ForegroundColor White
            Write-Host ""
            Write-Host "Press any key to open the dashboard..."
            $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
            Start-Process "https://localhost:5001/Dashboard"

        } else {
            Write-Host ""
            Write-Host "? Import Failed!" -ForegroundColor Red
            Write-Host "Message: $($importResponse.Message)" -ForegroundColor Red
            if ($importResponse.Error) {
                Write-Host "Error: $($importResponse.Error)" -ForegroundColor Red
            }
        }
    }
    catch {
        Write-Host ""
        Write-Host "? API call failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Stop the application
    Write-Host ""
    Write-Host "Stopping application..." -ForegroundColor Yellow
    Stop-Job -Job $job
    Remove-Job -Job $job
    Write-Host "? Application stopped" -ForegroundColor Green

}
catch {
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Import Complete ===" -ForegroundColor Green