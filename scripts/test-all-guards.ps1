# Test all database provider guard tests
$providers = @(
    "Dapper.SqlServer",
    "Dapper.PostgreSQL",
    "Dapper.MySQL",
    "Dapper.Sqlite",
    "Dapper.Oracle",
    "ADO.SqlServer",
    "ADO.PostgreSQL",
    "ADO.MySQL",
    "ADO.Sqlite",
    "ADO.Oracle"
)

Write-Host "=== Summary of all 10 Database Provider Guard Tests ===`n" -ForegroundColor Cyan

$totalPassed = 0
$totalFailed = 0
$totalTests = 0

foreach ($provider in $providers) {
    Write-Host "Testing Encina.$provider..." -NoNewline
    $testPath = "tests/Encina.$provider.GuardTests"
    $result = dotnet test $testPath --verbosity quiet --nologo 2>&1 | Select-String -Pattern "(Correctas|Con error)" | Select-Object -Last 1

    if ($result -match "Superado:\s+(\d+)") {
        $passed = [int]$matches[1]
        $totalPassed += $passed
    }

    if ($result -match "Con error:\s+(\d+)") {
        $failed = [int]$matches[1]
        $totalFailed += $failed
    }

    if ($result -match "Total:\s+(\d+)") {
        $total = [int]$matches[1]
        $totalTests += $total
    }

    Write-Host " $result" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })
}

Write-Host "`n=== TOTAL SUMMARY ===" -ForegroundColor Cyan
Write-Host "Total Passed: $totalPassed" -ForegroundColor Green
Write-Host "Total Failed: $totalFailed" -ForegroundColor $(if ($totalFailed -eq 0) { "Green" } else { "Red" })
Write-Host "Total Tests: $totalTests" -ForegroundColor Cyan
Write-Host "Pass Rate: $([math]::Round(($totalPassed / $totalTests) * 100, 2))%" -ForegroundColor Cyan
