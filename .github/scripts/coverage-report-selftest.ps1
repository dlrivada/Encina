<#
.SYNOPSIS
Fixture-run self-test for coverage-report.cs (issue #1668, PR1).

.DESCRIPTION
Runs coverage-report.cs against the hand-computed fixtures under
.github/scripts/testdata/coverage-report/ and asserts the exit codes of its manifest check modes:

  1. --check-justifications exits 0 on manifest-ok (every per-file target has a justification).
  2. --check-justifications exits 1 on manifest-unjustified (a target without a justification).
  3. --check-stale-manifest exits 1 on manifest-stale (a key names a file that does not exist) and
     exits 0 on manifest-ok.
  4. --check-missing-manifest exits 1 when a src file has no manifest entry (an extra source file is
     added to the staged tree) and exits 0 on manifest-ok.
  5. An empty --manifest directory fails every check mode with exit 1 ("no coverage manifest was
     loaded"); a gate must never pass vacuously.
  6. A nonexistent --manifest directory fails with exit 1 and does not fall back to an ancestor search.

The check modes resolve src/ against the repo root derived from the manifest directory (its parent's
parent), so each scenario is staged in a temporary tree: <tmp>/.github/coverage-manifest/Pkg.json
plus <tmp>/src copied from the fixtures. Nothing reads the network, the clock or randomness.

The per-flag target check (PR2 of #1668) adds its own assertions when the check exists.

Exit code: 0 if every assertion passes, 1 otherwise.
#>

$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$reportScript = Join-Path $scriptRoot 'coverage-report.cs'
$fixtureDir = Join-Path $scriptRoot 'testdata/coverage-report'

if (-not (Test-Path $reportScript)) {
    Write-Error "coverage-report.cs not found at $reportScript"
    exit 1
}
if (-not (Test-Path $fixtureDir)) {
    Write-Error "test fixtures not found at $fixtureDir"
    exit 1
}

$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("coverage-report-selftest-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stagingRoot | Out-Null

# Stages <staging>/<Name>/.github/coverage-manifest/Pkg.json (from the named fixture manifest) and
# <staging>/<Name>/src (copied from the fixture sources). Returns the manifest directory.
function New-Scenario {
    param([string]$Name, [string]$ManifestFixture, [string[]]$ExtraSourceFiles = @())

    $root = Join-Path $stagingRoot $Name
    $manifestDir = Join-Path $root '.github/coverage-manifest'
    New-Item -ItemType Directory -Path $manifestDir -Force | Out-Null
    Copy-Item (Join-Path $fixtureDir "$ManifestFixture/Pkg.json") $manifestDir
    Copy-Item (Join-Path $fixtureDir 'src') (Join-Path $root 'src') -Recurse
    foreach ($extra in $ExtraSourceFiles) {
        Set-Content -Path (Join-Path $root "src/Pkg/$extra") -Value 'namespace Pkg; public static class Extra { }'
    }
    return $manifestDir
}

function Invoke-CoverageReport {
    param([string[]]$ReportArgs)

    $output = & dotnet run --file $reportScript -- @ReportArgs 2>&1 | Out-String
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}

$script:failures = 0

function Assert-Run {
    param([string]$Title, [string[]]$ReportArgs, [int]$ExpectedExit, [string]$ExpectedOutput = '')

    Write-Host ''
    Write-Host "--- $Title ---"
    $result = Invoke-CoverageReport -ReportArgs $ReportArgs
    Write-Host $result.Output
    if ($result.ExitCode -ne $ExpectedExit) {
        Write-Host "FAIL: expected exit $ExpectedExit, got $($result.ExitCode)"
        $script:failures++
    }
    elseif ($ExpectedOutput -and $result.Output -notmatch $ExpectedOutput) {
        Write-Host "FAIL: output does not match '$ExpectedOutput'"
        $script:failures++
    }
    else {
        Write-Host "PASS: exit $ExpectedExit"
    }
}

try {
    Write-Host '=== coverage-report self-test ==='

    $ok = New-Scenario -Name 'ok' -ManifestFixture 'manifest-ok'
    $unjustified = New-Scenario -Name 'unjustified' -ManifestFixture 'manifest-unjustified'
    $stale = New-Scenario -Name 'stale' -ManifestFixture 'manifest-stale'
    $missing = New-Scenario -Name 'missing' -ManifestFixture 'manifest-ok' -ExtraSourceFiles @('Extra.cs')
    $empty = Join-Path $fixtureDir 'manifest-empty'
    $nonexistent = Join-Path $stagingRoot 'does-not-exist'

    Assert-Run '1. --check-justifications on manifest-ok' @('--check-justifications', '--manifest', $ok) 0 'per-file targets are justified'
    Assert-Run '2. --check-justifications on manifest-unjustified' @('--check-justifications', '--manifest', $unjustified) 1 "target 'unit' has no justification"
    Assert-Run '3a. --check-stale-manifest on manifest-ok' @('--check-stale-manifest', '--manifest', $ok) 0 'No stale manifest keys'
    Assert-Run '3b. --check-stale-manifest on manifest-stale' @('--check-stale-manifest', '--manifest', $stale) 1 'Pkg: Gone\.cs'
    Assert-Run '4a. --check-missing-manifest on manifest-ok' @('--check-missing-manifest', '--manifest', $ok) 0 'No missing manifest entries'
    Assert-Run '4b. --check-missing-manifest with an unlisted src file' @('--check-missing-manifest', '--manifest', $missing) 1 'Pkg: Extra\.cs'
    Assert-Run '5. empty --manifest directory (all check modes)' @('--check-justifications', '--check-stale-manifest', '--check-missing-manifest', '--manifest', $empty) 1 'no coverage manifest was loaded'
    Assert-Run '6. nonexistent --manifest directory' @('--check-stale-manifest', '--manifest', $nonexistent) 1 "--manifest directory '.*' does not exist"
}
finally {
    Remove-Item -Recurse -Force $stagingRoot -ErrorAction SilentlyContinue
}

Write-Host ''
if ($script:failures -gt 0) {
    Write-Host "coverage-report self-test FAILED ($($script:failures) assertion(s) failed)"
    exit 1
}

Write-Host 'coverage-report self-test PASSED'
exit 0
