$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5031/api"

function Invoke-Api {
    param($Uri, $Method = "Get", $Body = $null)
    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $headers
        ContentType = "application/json"
    }
    if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 10) }
    try {
        return Invoke-RestMethod @params
    } catch {
        Write-Error "API Call Failed: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            Write-Host "Response Body: $($reader.ReadToEnd())" -ForegroundColor Red
        }
        exit
    }
}

# 1. Login (Dev Token)
Write-Host "1. Logging in..." -ForegroundColor Cyan
$devResponse = Invoke-RestMethod -Uri "$baseUrl/Auth/dev-token" -Method Get
$token = $devResponse.token
$headers = @{ Authorization = "Bearer $token" }
Write-Host "Login Successful." -ForegroundColor Green

# 2. Create Tenants
Write-Host "2. Creating Tenants..." -ForegroundColor Cyan
$tenant1Vals = @{ Name = "User One"; PhoneNumber = "1111111111" }
$tenant2Vals = @{ Name = "User Two"; PhoneNumber = "2222222222" }

# Helper to create tenant unique
function Get-Or-Create-Tenant {
    param($t)
    # Check if exists (simple check by name from list - optimization)
    # properly we just create and ignore error if dup, or add random suffix
    $t.Name = $t.Name + "_" + (Get-Random -Minimum 1000 -Maximum 9999)
    $res = Invoke-Api -Uri "$baseUrl/Tenants" -Method Post -Body $t
    return $res.tenantId
}

$t1Id = Get-Or-Create-Tenant $tenant1Vals
$t2Id = Get-Or-Create-Tenant $tenant2Vals
Write-Host "Created Tenants: $t1Id, $t2Id" -ForegroundColor Green

# 3. Find Available Room
Write-Host "3. Finding Room..." -ForegroundColor Cyan
# Helper: Force Vacate
function Force-Vacate {
    param($flatId)
    $max = 5
    for ($i=0; $i -lt $max; $i++) {
        try {
            Invoke-RestMethod -Uri "$baseUrl/Flats/$flatId/vacate" -Method Post -Headers $headers -ErrorAction Stop
            Write-Host "Vacated once..." -ForegroundColor Yellow
        } catch {
            # If 400, assume it means "Not occupied" or similar error which is good
            Write-Host "Vacate stopped/failed (likely empty now): $($_.Exception.Message)" -ForegroundColor Gray
            break
        }
    }
}

$rooms = Invoke-Api -Uri "$baseUrl/Flats"
# Prefer vacant, else pick first
$targetRoom = $rooms | Where-Object { $_.isAvailable -eq $true } | Select-Object -First 1

if (!$targetRoom) {
    Write-Host "No initially vacant rooms. Picking occupied one..." -ForegroundColor Yellow
    $targetRoom = $rooms | Select-Object -First 1
}

Write-Host "Target Room: $($targetRoom.roomCode) ($($targetRoom.flatId))" -ForegroundColor Green
# FORCE VACATE any existing occupancies (handle multi-occupancy bug)
Force-Vacate $targetRoom.flatId

$roomId = $targetRoom.flatId

# 4. Assign User One
Write-Host "4. Assigning User One..." -ForegroundColor Cyan
$startDate = (Get-Date).ToString("yyyy-MM-dd")
$assignData = @{
    tenantId = $t1Id
    monthlyRent = 10000
    securityDeposit = 5000
    startDate = $startDate
}
Invoke-Api -Uri "$baseUrl/Flats/$roomId/assign-tenant" -Method Post -Body $assignData
Write-Host "Assigned User One." -ForegroundColor Green

# 5. Generate Bill 1
Write-Host "5. Generating Bill 1..." -ForegroundColor Cyan
$month = (Get-Date).Month
$year = (Get-Date).Year
$preview = Invoke-Api -Uri "$baseUrl/Bills/preview?month=$month&year=$year"
$billItem = $preview | Where-Object { $_.flatId -eq $roomId -and $_.tenantId -eq $t1Id }

if (!$billItem) { Write-Error "Bill not found for User One!"; exit }

$billItem.elecNew = $billItem.elecPrev + 10
$batchData = @{
    month = $month
    year = $year
    bills = @($billItem)
}
Invoke-Api -Uri "$baseUrl/Bills/generate-batch" -Method Post -Body $batchData
Write-Host "Bill 1 Generated." -ForegroundColor Green

# 6. Vacate
Write-Host "6. Vacating User One..." -ForegroundColor Cyan
Invoke-Api -Uri "$baseUrl/Flats/$roomId/vacate" -Method Post
Write-Host "Vacated." -ForegroundColor Green

# 7. Assign User Two
Write-Host "7. Assigning User Two..." -ForegroundColor Cyan
$assignData2 = @{
    tenantId = $t2Id
    monthlyRent = 12000
    securityDeposit = 6000
    startDate = $startDate
}
Invoke-Api -Uri "$baseUrl/Flats/$roomId/assign-tenant" -Method Post -Body $assignData2
Write-Host "Assigned User Two." -ForegroundColor Green

# 8. Generate Bill 2 (For User Two)
Write-Host "8. Generating Bill 2..." -ForegroundColor Cyan
$preview2 = Invoke-Api -Uri "$baseUrl/Bills/preview?month=$month&year=$year"
$billItem2 = $preview2 | Where-Object { $_.flatId -eq $roomId -and $_.tenantId -eq $t2Id }

if (!$billItem2) { Write-Error "Bill 2 not found for User Two!"; exit }

$billItem2.elecNew = $billItem2.elecPrev + 15
$batchData2 = @{
    month = $month
    year = $year
    bills = @($billItem2)
}
Invoke-Api -Uri "$baseUrl/Bills/generate-batch" -Method Post -Body $batchData2
Write-Host "Bill 2 Generated." -ForegroundColor Green

# 9. Verify
Write-Host "9. Verifying Ledger..." -ForegroundColor Cyan
$roomDetails = Invoke-Api -Uri "$baseUrl/Flats/$roomId"
$history = $roomDetails.meterReadingHistory

Write-Host "Room Details Headers: $($roomDetails | Get-Member -MemberType NoteProperty | Select -ExpandProperty Name)" -ForegroundColor DarkGray
Write-Host "History Count: $($history.Count)" -ForegroundColor DarkGray
if ($history.Count -gt 0) {
    Write-Host "First History Item: $($history[0] | ConvertTo-Json -Depth 2)" -ForegroundColor DarkGray
}

# Filter by this month
$monthStr = (Get-Date).ToString("yyyy-MM")
$entries = $history | Where-Object { $_.period -eq $monthStr }

Write-Host "Entries matching $monthStr : $($entries.Count)" -ForegroundColor Yellow
$entries | Format-Table

if ($entries.Count -gt 0) {
    Write-Host "Success: Ledger entry exists." -ForegroundColor Green
    
    # Check tenant name if possible? Ledger history DTO might not have tenant name.
    # checking Bills endpoint to see tenant name.
    $bills = Invoke-Api -Uri "$baseUrl/Bills?month=$month&year=$year"
    $targetBill = $bills | Where-Object { $_.roomNumber -eq $targetRoom.roomCode }
    Write-Host "Bill Tenant in System: $($targetBill.tenantName)" -ForegroundColor Cyan
    
} else {
    Write-Error "No ledger entry found for $monthStr !"
}
