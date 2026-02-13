# Azure SQL Firewall Rule Creator
# This script adds your current IP to the Azure SQL firewall

Write-Host "=== Adding IP to Azure SQL Firewall ===" -ForegroundColor Green
Write-Host ""

$currentIP = "199.64.7.210"
$serverName = "rahulpandey"
$resourceGroupName = "YourResourceGroupName"  # You'll need to update this

Write-Host "Current IP Address: $currentIP" -ForegroundColor Yellow
Write-Host "Server Name: $serverName" -ForegroundColor Yellow
Write-Host ""

# Check if Azure CLI is installed
try {
    $azVersion = az --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Azure CLI found" -ForegroundColor Green
        
        Write-Host "Adding firewall rule..." -ForegroundColor Yellow
        
        # Add firewall rule
        az sql server firewall-rule create `
            --resource-group $resourceGroupName `
            --server $serverName `
            --name "MyCurrentIP" `
            --start-ip-address $currentIP `
            --end-ip-address $currentIP
            
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Firewall rule added successfully!" -ForegroundColor Green
            Write-Host "You can now run the migration." -ForegroundColor Green
        } else {
            Write-Host "? Failed to add firewall rule via CLI" -ForegroundColor Red
            Write-Host "Please add the rule manually in Azure Portal" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Azure CLI not found." -ForegroundColor Yellow
        Write-Host "Please add the firewall rule manually in Azure Portal:" -ForegroundColor Yellow
        Write-Host "1. Go to https://portal.azure.com" -ForegroundColor White
        Write-Host "2. Find your SQL Server: $serverName" -ForegroundColor White
        Write-Host "3. Go to Networking/Firewall" -ForegroundColor White
        Write-Host "4. Add IP: $currentIP" -ForegroundColor White
    }
} catch {
    Write-Host "Please add the firewall rule manually in Azure Portal:" -ForegroundColor Yellow
    Write-Host "1. Go to https://portal.azure.com" -ForegroundColor White
    Write-Host "2. Find your SQL Server: $serverName" -ForegroundColor White
    Write-Host "3. Go to Networking/Firewall" -ForegroundColor White
    Write-Host "4. Add IP: $currentIP" -ForegroundColor White
}

Write-Host ""
Write-Host "=== Firewall Setup Complete ===" -ForegroundColor Green