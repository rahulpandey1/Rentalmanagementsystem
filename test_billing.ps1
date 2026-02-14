$baseUrl = "http://localhost:5031/api"
$email = "rahulpandey9911@gmail.com"

# 1. Request OTP
Write-Host "Requesting OTP..." -ForegroundColor Cyan
Invoke-RestMethod -Uri "$baseUrl/Auth/request-otp" -Method Post -Body (@{ email = $email } | ConvertTo-Json) -ContentType "application/json"

# 2. Wait for user to check logs
Write-Host "Check the terminal logs for the OTP and enter it below:" -ForegroundColor Yellow
$otp = Read-Host "OTP"

# 3. Verify OTP
Write-Host "Verifying OTP..." -ForegroundColor Cyan
try {
    $tokenResponse = Invoke-RestMethod -Uri "$baseUrl/Auth/verify-otp" -Method Post -Body (@{ email = $email; code = $otp } | ConvertTo-Json) -ContentType "application/json"
    $token = $tokenResponse.token
    Write-Host "Login Successful! Token received." -ForegroundColor Green
}
catch {
    Write-Error "Login Failed: $_"
    exit
}

$headers = @{ Authorization = "Bearer $token" }

# 4. Generate Bills
$month = (Get-Date).Month
$year = (Get-Date).Year
Write-Host "Generating Bills for ${month}/${year}..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/Bills/generate?month=$month&year=$year" -Method Post -Headers $headers
    Write-Host "Bill Generation Result: $($response | ConvertTo-Json)" -ForegroundColor Green
}
catch {
    Write-Error "Generate Bills Failed: $_"
}

# 5. List Bills
Write-Host "Listing Bills..." -ForegroundColor Cyan
try {
    $bills = Invoke-RestMethod -Uri "$baseUrl/Bills?month=$month&year=$year" -Method Get -Headers $headers
    $bills | Select-Object RoomNumber, TenantName, MonthlyRent, ElectricAmount, TotalAmount | Format-Table -AutoSize
}
catch {
    Write-Error "List Bills Failed: $_"
}
