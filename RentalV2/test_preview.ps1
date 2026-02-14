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
    $otp = Read-Host "Enter OTP"
    
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

# 3. Get Preview
$month = (Get-Date).Month
$year = (Get-Date).Year
Write-Host "Getting Preview for ${month}/${year}..." -ForegroundColor Cyan
try {
    $preview = Invoke-RestMethod -Uri "$baseUrl/Bills/preview?month=$month&year=$year" -Method Get -Headers $headers
    # Debug: Get Flats
    Write-Host "Getting Flats..." -ForegroundColor Cyan
    $flats = Invoke-RestMethod -Uri "$baseUrl/Flats" -Method Get -Headers $headers
    Write-Host "Flats Count: $($flats.Count)"
    if ($flats.Count > 0) {
        $flats[0] | ConvertTo-Json -Depth 3
    }

    # Retry Preview with a different period?
    # Try Dec 2024
    Write-Host "Getting Preview for 12/2024..." -ForegroundColor Cyan
    $preview24 = Invoke-RestMethod -Uri "$baseUrl/Bills/preview?month=12&year=2024" -Method Get -Headers $headers
    Write-Host "Count 2024: $($preview24.Count)"
}
catch {
    Write-Error "Debug Failed: $_"
}
