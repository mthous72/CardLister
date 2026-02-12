# FlipKit Web Application Test Script
# Tests all pages and basic functionality

$baseUrl = "http://localhost:5000"
$results = @()

function Test-Page {
    param(
        [string]$Name,
        [string]$Url,
        [string]$ExpectedContent
    )

    Write-Host "Testing: $Name..." -ForegroundColor Cyan

    try {
        $response = Invoke-WebRequest -Uri "$baseUrl$Url" -UseBasicParsing -ErrorAction Stop
        $statusCode = $response.StatusCode
        $contentMatch = if ($ExpectedContent) { $response.Content -match $ExpectedContent } else { $true }

        if ($statusCode -eq 200 -and $contentMatch) {
            Write-Host "  ✓ PASS - Status: $statusCode" -ForegroundColor Green
            return @{ Name = $Name; Status = "PASS"; Code = $statusCode; Url = $Url }
        } else {
            Write-Host "  ✗ FAIL - Status: $statusCode, Content match: $contentMatch" -ForegroundColor Red
            return @{ Name = $Name; Status = "FAIL"; Code = $statusCode; Url = $Url; Error = "Content mismatch" }
        }
    }
    catch {
        Write-Host "  ✗ FAIL - Error: $($_.Exception.Message)" -ForegroundColor Red
        return @{ Name = $Name; Status = "FAIL"; Code = 0; Url = $Url; Error = $_.Exception.Message }
    }
}

Write-Host "`n=== FlipKit Web Application Test Suite ===" -ForegroundColor Yellow
Write-Host "Base URL: $baseUrl`n" -ForegroundColor Yellow

# Test Home Dashboard
$results += Test-Page -Name "Home Dashboard" -Url "/" -ExpectedContent "FlipKit"

# Test Inventory Pages
$results += Test-Page -Name "Inventory Index" -Url "/Inventory" -ExpectedContent "Inventory"
$results += Test-Page -Name "Inventory Details 404" -Url "/Inventory/Details/1"
$results += Test-Page -Name "Inventory Edit 404" -Url "/Inventory/Edit/1"

# Test Scan Pages
$results += Test-Page -Name "Scan Upload" -Url "/Scan" -ExpectedContent "Scan"

# Test Pricing Pages
$results += Test-Page -Name "Pricing Index" -Url "/Pricing" -ExpectedContent "Pricing"

# Test Export Pages
$results += Test-Page -Name "Export Index" -Url "/Export" -ExpectedContent "Export"

# Test Reports Pages
$results += Test-Page -Name "Reports Dashboard" -Url "/Reports" -ExpectedContent "Reports"
$results += Test-Page -Name "Sales Report" -Url "/Reports/Sales" -ExpectedContent "Sales"
$results += Test-Page -Name "Financial Report" -Url "/Reports/Financial" -ExpectedContent "Financial"

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Yellow
$passed = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$failed = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
$total = $results.Count

Write-Host "Total Tests: $total" -ForegroundColor White
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })

if ($failed -gt 0) {
    Write-Host "`nFailed Tests:" -ForegroundColor Red
    $results | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "  - $($_.Name): $($_.Error)" -ForegroundColor Red
    }
}

Write-Host "`nTest complete!`n" -ForegroundColor Yellow
