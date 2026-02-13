# Excel Import Test Script for SITA DEVI PANDEY Format
# This script tests the Excel import functionality

param(
    [Parameter(Mandatory=$false)]
    [string]$FilePath = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$CreateSample = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$TestAPI = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "https://localhost:5001"
)

Write-Host "=== Excel Import Test Script ===" -ForegroundColor Green
Write-Host ""

# Function to create sample Excel file
function Create-SampleExcel {
    Write-Host "Creating sample SITA DEVI PANDEY Excel file..." -ForegroundColor Yellow
    
    try {
        # Create Data directory if it doesn't exist
        $dataDir = "Data"
        if (!(Test-Path $dataDir)) {
            New-Item -ItemType Directory -Path $dataDir | Out-Null
            Write-Host "Created Data directory" -ForegroundColor Green
        }
        
        # Sample data structure
        $sampleFilePath = Join-Path $dataDir "Sample_SITA_DEVI_PANDEY.xlsx"
        
        Write-Host "Sample file would be created at: $sampleFilePath" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Sample SITA DEVI PANDEY Excel format should have:" -ForegroundColor Yellow
        Write-Host "Row 1: SITA DEVI PANDEY AND SONS" -ForegroundColor White
        Write-Host "Row 2: 11/1B/1 GANPAT RAI KHEMKA LANE LILUAH HOWRAH(W. B.) 711204" -ForegroundColor White
        Write-Host "Row 3: FOR THE MONTH OF JUL 2025" -ForegroundColor White
        Write-Host "Row 4: Headers - SNO | NAME | ROOM NO | MTLY RENT | ELECTRIC NEW | ELECTRIC PRE | ELECTRIC TOTAL | ELECTRIC COST | MISC RENT | B/F & ADV | TOTAL AMT DUE | AMT PAID | B/F OR ADV | REMARKS" -ForegroundColor White
        Write-Host ""
        Write-Host "Sample data rows:" -ForegroundColor Yellow
        Write-Host "1 | RAHUL SHARMA | G/1 | 600 | 1234 | 1200 | 34 | 408 | 50 | 0 | 1058 | 1058 | 0 | WEF MAY 2025. ADV 5K" -ForegroundColor Gray
        Write-Host "2 | PRIYA SINGH | 1/2 | 650 | 2100 | 2050 | 50 | 600 | 0 | 0 | 1250 | 1250 | 0 | WEF JUN 2025" -ForegroundColor Gray
        Write-Host "3 | VACANT | G/3 | 600 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | NEW" -ForegroundColor Gray
        
    }
    catch {
        Write-Host "Error creating sample: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Function to test import status API
function Test-ImportStatus {
    Write-Host "Testing import status API..." -ForegroundColor Yellow
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/DataImport/status" -Method GET
        
        Write-Host "? Import Status API Response:" -ForegroundColor Green
        Write-Host "  Files Available: $($response.TotalFiles)" -ForegroundColor White
        Write-Host "  Data Folder: $($response.DataFolder)" -ForegroundColor White
        
        if ($response.AvailableFiles) {
            Write-Host "  Available Files:" -ForegroundColor Cyan
            foreach ($file in $response.AvailableFiles) {
                $size = [math]::Round($file.FileSize / 1024, 2)
                Write-Host "    - $($file.FileName) (${size} KB, Modified: $($file.LastModified))" -ForegroundColor Gray
            }
        }
        
        if ($response.RecentUploads) {
            Write-Host "  Recent Uploads:" -ForegroundColor Cyan
            foreach ($upload in $response.RecentUploads) {
                $size = [math]::Round($upload.FileSize / 1024, 2)
                Write-Host "    - $($upload.FileName) (${size} KB)" -ForegroundColor Gray
            }
        }
        
        return $true
    }
    catch {
        Write-Host "? Import Status API Failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to test Excel import
function Test-ExcelImport {
    param([string]$TestFilePath)
    
    Write-Host "Testing Excel import..." -ForegroundColor Yellow
    
    if ($TestFilePath -and (Test-Path $TestFilePath)) {
        Write-Host "Using file: $TestFilePath" -ForegroundColor Cyan
        
        try {
            # Test file upload
            $boundary = [System.Guid]::NewGuid().ToString()
            $fileBytes = [System.IO.File]::ReadAllBytes($TestFilePath)
            $fileName = [System.IO.Path]::GetFileName($TestFilePath)
            
            $body = @"
--$boundary
Content-Disposition: form-data; name="file"; filename="$fileName"
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet

$([System.Text.Encoding]::GetEncoding('iso-8859-1').GetString($fileBytes))
--$boundary--
"@
            
            $headers = @{
                'Content-Type' = "multipart/form-data; boundary=$boundary"
            }
            
            Write-Host "Uploading and importing file..." -ForegroundColor Yellow
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/DataImport/upload" -Method POST -Body $body -Headers $headers
            
            if ($response.Success) {
                Write-Host "? Import Successful!" -ForegroundColor Green
                Write-Host "  Duration: $($response.ImportStats.Duration)" -ForegroundColor White
                Write-Host "  Imported Data:" -ForegroundColor Cyan
                Write-Host "    Tenants: $($response.ImportedData.Tenants)" -ForegroundColor Gray
                Write-Host "    Payments: $($response.ImportedData.Payments)" -ForegroundColor Gray
                Write-Host "    Bills: $($response.ImportedData.Bills)" -ForegroundColor Gray
                Write-Host "    Electric Readings: $($response.ImportedData.ElectricReadings)" -ForegroundColor Gray
                Write-Host "    Total: $($response.ImportedData.Total)" -ForegroundColor Gray
                
                if ($response.ImportStats.BillPeriod) {
                    Write-Host "  Bill Period: $($response.ImportStats.BillPeriod)" -ForegroundColor Cyan
                }
                
                if ($response.Errors -and $response.Errors.Count -gt 0) {
                    Write-Host "  Warnings/Errors:" -ForegroundColor Yellow
                    foreach ($error in $response.Errors) {
                        Write-Host "    - $error" -ForegroundColor Gray
                    }
                }
            } else {
                Write-Host "? Import Failed: $($response.Message)" -ForegroundColor Red
                if ($response.Error) {
                    Write-Host "  Error: $($response.Error)" -ForegroundColor Red
                }
            }
        }
        catch {
            Write-Host "? Import Test Failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        # Test quick import (from Data folder)
        try {
            Write-Host "Testing quick import from Data folder..." -ForegroundColor Yellow
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/DataImport/excel" -Method POST
            
            if ($response.Success) {
                Write-Host "? Quick Import Successful!" -ForegroundColor Green
                Write-Host "  Duration: $($response.ImportStats.Duration)" -ForegroundColor White
                Write-Host "  Imported Data:" -ForegroundColor Cyan
                Write-Host "    Tenants: $($response.ImportedData.Tenants)" -ForegroundColor Gray
                Write-Host "    Payments: $($response.ImportedData.Payments)" -ForegroundColor Gray
                Write-Host "    Bills: $($response.ImportedData.Bills)" -ForegroundColor Gray
                Write-Host "    Electric Readings: $($response.ImportedData.ElectricReadings)" -ForegroundColor Gray
                Write-Host "    Total: $($response.ImportedData.Total)" -ForegroundColor Gray
            } else {
                Write-Host "? Quick Import Failed: $($response.Message)" -ForegroundColor Red
                if ($response.Error) {
                    Write-Host "  Error: $($response.Error)" -ForegroundColor Red
                }
            }
        }
        catch {
            Write-Host "? Quick Import Test Failed: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "  Make sure the application is running and a file exists in the Data folder" -ForegroundColor Yellow
        }
    }
}

# Function to validate Excel file
function Test-FileValidation {
    param([string]$TestFilePath)
    
    if (!$TestFilePath -or !(Test-Path $TestFilePath)) {
        return
    }
    
    Write-Host "Testing file validation..." -ForegroundColor Yellow
    
    try {
        $fileName = [System.IO.Path]::GetFileName($TestFilePath)
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/DataImport/validate/$fileName" -Method GET
        
        if ($response.Success) {
            Write-Host "? File Validation Successful!" -ForegroundColor Green
            Write-Host "  File: $($response.FileName)" -ForegroundColor White
            Write-Host "  Total Worksheets: $($response.TotalWorksheets)" -ForegroundColor White
            Write-Host "  SITA DEVI PANDEY Sheets: $($response.SitaDeviPandeySheets)" -ForegroundColor Cyan
            Write-Host "  Supported Sheets: $($response.SupportedSheets)" -ForegroundColor Cyan
            
            if ($response.Worksheets) {
                Write-Host "  Worksheet Details:" -ForegroundColor Cyan
                foreach ($ws in $response.Worksheets) {
                    $formatType = if ($ws.IsSitaDeviPandeyFormat) { "SITA DEVI PANDEY" } 
                                  elseif ($ws.IsSupported) { "Standard" } 
                                  else { "Unknown" }
                    Write-Host "    - $($ws.Name): $($ws.Rows) rows, $($ws.Columns) cols [$formatType]" -ForegroundColor Gray
                }
            }
        } else {
            Write-Host "? File Validation Failed: $($response.Message)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "? Validation Test Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main execution
try {
    if ($CreateSample) {
        Create-SampleExcel
        return
    }
    
    if ($TestAPI) {
        Write-Host "Testing Excel Import APIs..." -ForegroundColor Cyan
        Write-Host "Base URL: $BaseUrl" -ForegroundColor White
        Write-Host ""
        
        # Test 1: Import Status
        $statusOK = Test-ImportStatus
        Write-Host ""
        
        if ($statusOK) {
            # Test 2: File Validation (if file provided)
            if ($FilePath -and (Test-Path $FilePath)) {
                Test-FileValidation -TestFilePath $FilePath
                Write-Host ""
            }
            
            # Test 3: Excel Import
            Test-ExcelImport -TestFilePath $FilePath
        }
        
        return
    }
    
    # Default behavior - check for files and provide guidance
    Write-Host "Excel Import System Status Check" -ForegroundColor Cyan
    Write-Host ""
    
    # Check if Data directory exists
    if (Test-Path "Data") {
        Write-Host "? Data directory exists" -ForegroundColor Green
        
        $excelFiles = Get-ChildItem -Path "Data" -Filter "*.xlsx" | Select-Object -First 5
        if ($excelFiles) {
            Write-Host "  Excel files found:" -ForegroundColor Cyan
            foreach ($file in $excelFiles) {
                $size = [math]::Round($file.Length / 1024, 2)
                Write-Host "    - $($file.Name) (${size} KB)" -ForegroundColor Gray
            }
        } else {
            Write-Host "  ?  No Excel files found in Data directory" -ForegroundColor Yellow
        }
    } else {
        Write-Host "?  Data directory not found" -ForegroundColor Yellow
        Write-Host "  Creating Data directory..." -ForegroundColor Gray
        New-Item -ItemType Directory -Path "Data" | Out-Null
        Write-Host "  ? Data directory created" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Usage Examples:" -ForegroundColor Cyan
    Write-Host "  Create sample file:  .\Scripts\Test-ExcelImport.ps1 -CreateSample" -ForegroundColor White
    Write-Host "  Test APIs:           .\Scripts\Test-ExcelImport.ps1 -TestAPI" -ForegroundColor White
    Write-Host "  Test specific file:  .\Scripts\Test-ExcelImport.ps1 -TestAPI -FilePath 'Data\MyFile.xlsx'" -ForegroundColor White
    Write-Host ""
    Write-Host "For the import to work:" -ForegroundColor Yellow
    Write-Host "1. Place your Excel file in the Data folder" -ForegroundColor White
    Write-Host "2. Name it 'Payment Chart Draft1.xlsx' or one of the supported names" -ForegroundColor White
    Write-Host "3. Ensure it has password protection with: sanpa@123" -ForegroundColor White
    Write-Host "4. Use the SITA DEVI PANDEY format for best results" -ForegroundColor White
    
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Script Complete ===" -ForegroundColor Green