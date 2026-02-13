# Quick Excel Import Test
# This script tests the Excel import service directly

Write-Host "=== Quick Excel Import Test ===" -ForegroundColor Green
Write-Host ""

# Check for Excel files
$dataDir = "Data"
if (!(Test-Path $dataDir)) {
    Write-Host "? Data directory not found" -ForegroundColor Red
    exit 1
}

$excelFiles = Get-ChildItem -Path $dataDir -Filter "*.xlsx"
if ($excelFiles.Count -eq 0) {
    Write-Host "? No Excel files found" -ForegroundColor Red
    exit 1
}

# Show available files
Write-Host "?? Available Excel files:" -ForegroundColor Cyan
for ($i = 0; $i -lt $excelFiles.Count; $i++) {
    $file = $excelFiles[$i]
    $size = [math]::Round($file.Length / 1024, 2)
    Write-Host "  [$i] $($file.Name) (${size} KB)" -ForegroundColor White
}

# Select file (prefer NoPassword version)
$targetFile = $excelFiles | Where-Object { $_.Name -match "NoPassword" } | Select-Object -First 1
if (!$targetFile) {
    $targetFile = $excelFiles[0]
}

Write-Host ""
Write-Host "?? Selected: $($targetFile.Name)" -ForegroundColor Yellow
Write-Host ""

# Test file access
try {
    Write-Host "Testing file access..." -ForegroundColor Yellow
    $fileInfo = Get-Item $targetFile.FullName
    Write-Host "? File accessible: $($fileInfo.FullName)" -ForegroundColor Green
    Write-Host "   Size: $([math]::Round($fileInfo.Length / 1024, 2)) KB" -ForegroundColor Gray
    Write-Host "   Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
}
catch {
    Write-Host "? Cannot access file: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test Excel file structure using EPPlus
Write-Host ""
Write-Host "?? Analyzing Excel file structure..." -ForegroundColor Yellow

try {
    # Load the EPPlus assembly (assuming it's available after build)
    Add-Type -Path "bin\Release\net8.0\EPPlus.dll" -ErrorAction SilentlyContinue
    
    # Try to open the Excel file
    $excelPackage = $null
    try {
        # Try without password first
        $excelPackage = New-Object OfficeOpenXml.ExcelPackage($fileInfo)
        Write-Host "? Opened Excel file without password" -ForegroundColor Green
    }
    catch {
        Write-Host "??  File might be password protected or corrupted" -ForegroundColor Yellow
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
    
    if ($excelPackage) {
        $workbook = $excelPackage.Workbook
        Write-Host "?? Worksheets found: $($workbook.Worksheets.Count)" -ForegroundColor Cyan
        
        foreach ($worksheet in $workbook.Worksheets) {
            $rows = if ($worksheet.Dimension) { $worksheet.Dimension.Rows } else { 0 }
            $cols = if ($worksheet.Dimension) { $worksheet.Dimension.Columns } else { 0 }
            Write-Host "   - $($worksheet.Name): $rows rows, $cols columns" -ForegroundColor White
            
            # Check for SITA DEVI PANDEY format
            if ($rows -gt 0) {
                $firstCell = $worksheet.Cells[1, 1].Value
                $secondCell = $worksheet.Cells[2, 1].Value
                $thirdCell = $worksheet.Cells[3, 1].Value
                $fourthCell = $worksheet.Cells[4, 1].Value
                
                if ($firstCell -like "*SITA DEVI PANDEY*" -or 
                    $secondCell -like "*GANPAT RAI KHEMKA*" -or 
                    $thirdCell -like "*FOR THE MONTH*" -or 
                    $fourthCell -eq "SNO") {
                    Write-Host "     ?? SITA DEVI PANDEY format detected!" -ForegroundColor Green
                }
            }
        }
        
        $excelPackage.Dispose()
    }
}
catch {
    Write-Host "??  Could not analyze Excel structure: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "? File analysis complete" -ForegroundColor Green
Write-Host ""
Write-Host "To run the actual import:" -ForegroundColor Cyan
Write-Host "1. Start the application: dotnet run" -ForegroundColor White
Write-Host "2. Navigate to: https://localhost:5001/Dashboard/DataImport" -ForegroundColor White
Write-Host "3. Click 'Import Now' or use: .\Scripts\Run-Import.ps1" -ForegroundColor White

Write-Host ""
Write-Host "=== Analysis Complete ===" -ForegroundColor Green