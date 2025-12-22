#!/usr/bin/env pwsh
# Script to query SonarCloud issues for the Encina project
# Usage: ./scripts/sonar-issues.ps1 [rule]
# Examples:
#   ./scripts/sonar-issues.ps1              # All open issues
#   ./scripts/sonar-issues.ps1 S2699        # Only S2699 (missing assertions)
#   ./scripts/sonar-issues.ps1 IDE0052      # Only IDE0052 (unused members)

param(
    [string]$Rule = "",
    [int]$PageSize = 100
)

$baseUrl = "https://sonarcloud.io/api/issues/search"
$projectKey = "dlrivada_Encina"

$params = @{
    componentKeys = $projectKey
    statuses = "OPEN"
    ps = $PageSize
}

if ($Rule) {
    # Handle both short (S2699) and full (csharpsquid:S2699) format
    if ($Rule -notmatch ":") {
        if ($Rule -match "^S\d+$") {
            $Rule = "csharpsquid:$Rule"
        } elseif ($Rule -match "^IDE\d+$" -or $Rule -match "^RS\d+$" -or $Rule -match "^SYSLIB\d+$") {
            $Rule = "external_roslyn:$Rule"
        }
    }
    $params.rules = $Rule
}

$queryString = ($params.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join "&"
$url = "$baseUrl`?$queryString"

try {
    $response = Invoke-RestMethod -Uri $url -Method Get

    Write-Host "`nSonarCloud Issues Summary" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    Write-Host "Total issues: $($response.total)" -ForegroundColor Yellow

    if ($response.total -gt 0) {
        Write-Host "`nIssues by file:" -ForegroundColor Green

        $grouped = $response.issues | Group-Object { $_.component -replace "dlrivada_Encina:", "" }

        foreach ($group in $grouped | Sort-Object Name) {
            Write-Host "`n  $($group.Name):" -ForegroundColor White
            foreach ($issue in $group.Group | Sort-Object line) {
                $severity = switch ($issue.severity) {
                    "BLOCKER" { "BLOCKER" }
                    "CRITICAL" { "CRITICAL" }
                    "MAJOR" { "MAJOR" }
                    "MINOR" { "MINOR" }
                    "INFO" { "INFO" }
                    default { $issue.severity }
                }
                $color = switch ($issue.severity) {
                    "BLOCKER" { "Red" }
                    "CRITICAL" { "Red" }
                    "MAJOR" { "Yellow" }
                    "MINOR" { "Cyan" }
                    default { "Gray" }
                }
                Write-Host "    Line $($issue.line): [$severity] $($issue.message)" -ForegroundColor $color
            }
        }

        Write-Host "`n`nIssues by rule:" -ForegroundColor Green
        $byRule = $response.issues | Group-Object rule
        foreach ($ruleGroup in $byRule | Sort-Object Count -Descending) {
            Write-Host "  $($ruleGroup.Name): $($ruleGroup.Count) issues" -ForegroundColor White
        }
    }
}
catch {
    Write-Host "Error querying SonarCloud API: $_" -ForegroundColor Red
    exit 1
}
