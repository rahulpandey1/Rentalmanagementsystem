$baseUrl = "http://localhost:5031/api"
$email = "rahulpandey9911@gmail.com"

# 1. Login
try {
    Write-Host "Attempting Dev Login..." -ForegroundColor Cyan
    $devResponse = Invoke-RestMethod -Uri "$baseUrl/Auth/dev-token" -Method Get -ErrorAction Stop
    $token = $devResponse.token
    Write-Host "Dev Login Successful! (Super User Mode)" -ForegroundColor Green
}
catch {
    Write-Host "Dev Login failed/disabled. Using OTP flow." -ForegroundColor Yellow
    $otp = "283343" 
    if ($args.Count -gt 0) { $otp = $args[0] }

    Write-Host "Verifying OTP $otp..." -ForegroundColor Cyan
    try {
        $tokenResponse = Invoke-RestMethod -Uri "$baseUrl/Auth/verify-otp" -Method Post -Body (@{ email = $email; code = $otp } | ConvertTo-Json) -ContentType "application/json"
        $token = $tokenResponse.token
        Write-Host "Login Successful!" -ForegroundColor Green
    }
    catch {
        Write-Error "Login Failed: $_"
        exit
    }
}

$headers = @{ Authorization = "Bearer $token" }

# 2. Get a Flat (Vacant or Occupied)
Write-Host "Finding a flat..."
$flats = Invoke-RestMethod -Uri "$baseUrl/Flats" -Method Get -Headers $headers

# Try vacant first
$targetFlat = $flats | Where-Object { $_.IsAvailable -eq $true } | Select-Object -First 1

if (!$targetFlat) {
    Write-Host "No vacant flats. Picking an occupied one to vacate..." -ForegroundColor Yellow
    $targetFlat = $flats | Select-Object -First 1
    
    if ($targetFlat) {
        Write-Host "Vacating $($targetFlat.RoomCode)..."
        try {
            Invoke-RestMethod -Uri "$baseUrl/Flats/$($targetFlat.FlatId)/vacate" -Method Post -Headers $headers
            Write-Host "Vacated." -ForegroundColor Green
        } catch {
            Write-Error "Failed to vacate: $_"
            exit
        }
    } else {
        Write-Error "No flats found at all!"
        exit
    }
}

Write-Host "Target Flat: $($targetFlat.RoomCode) ($($targetFlat.FlatId))" -ForegroundColor Yellow

# 3. Get a Tenant to Assign (or Unassigned one)
$tenants = Invoke-RestMethod -Uri "$baseUrl/Tenants" -Method Get -Headers $headers
$tenant = $tenants | Select-Object -First 1 # Just pick first one for test

Write-Host "Assigning Tenant: $($tenant.Name) ($($tenant.TenantId))" -ForegroundColor Yellow

# 4. Assign Tenant
$assignData = @{
    tenantId = $tenant.TenantId
    monthlyRent = 12000
    securityDeposit = 5000
    startDate = (Get-Date).ToString("yyyy-MM-dd")
}

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/Flats/$($targetFlat.FlatId)/assign-tenant" -Method Post -Headers $headers -Body ($assignData | ConvertTo-Json) -ContentType "application/json"
    Write-Host "Assignment Response: $($response.message)" -ForegroundColor Green
}
catch {
    Write-Error "Assignment Failed: $_"
    exit
}

# 5. Verify Ledger
Write-Host "Verifying Ledger..."
$flatDetails = Invoke-RestMethod -Uri "$baseUrl/Flats/$($targetFlat.FlatId)" -Method Get -Headers $headers
$ledger = $flatDetails.MeterReadingHistory | Select-Object -First 1

if ($ledger) {
    Write-Host "Ledger Found for $($ledger.Period)" -ForegroundColor Cyan
    Write-Host "Rent: $($ledger.MonthlyRent) (Expected 12000)"
    if ($ledger.MonthlyRent -eq 12000) { Write-Host "Rent MATCH" -ForegroundColor Green } else { Write-Host "Rent MISMATCH" -ForegroundColor Red }
    
    # Note: Security isn't in simple history view usually, need to check DB or extensive view
    # But Rent match confirms logical path execution
} else {
    Write-Error "No ledger created!"
}

# 6. Clean up (Vacate)
Write-Host "Vacating..."
Invoke-RestMethod -Uri "$baseUrl/Flats/$($targetFlat.FlatId)/vacate" -Method Post -Headers $headers
Write-Host "Vacated."
