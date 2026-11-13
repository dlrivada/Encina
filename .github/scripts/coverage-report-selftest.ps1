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

  7-12. --check-targets (#1651) on the manifest-targets and cobertura fixtures: exit 0 on the good fixture
     and exit 1 under --enforce for a pair below its target, a required flag with no data (only with
     --require-all-flags, or when the flag ran), an unparsable Cobertura file and a touched src file with no
     per-file target; report-only exits 0 everywhere and records the findings in check-targets.json; the
     summary JSON keys that docs/coverage/app.js reads stay equal to expected-latest-shape.json.

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

# Stages a --check-targets scenario under <staging>/<Name>: the manifest-targets manifest (optionally with
# a changed unit target for A.cs), the fixture sources and one Cobertura directory per requested flag
# (UnitTests, GuardTests; 'broken' is staged as the GuardTests report). Returns the paths.
function New-TargetsScenario {
    param(
        [string]$Name,
        [string[]]$Flags = @('UnitTests', 'GuardTests'),
        [string]$BrokenFlag = '',
        [string[]]$ExtraSourceFiles = @(),
        [string]$UnitTargetA = '80')

    $root = Join-Path $stagingRoot $Name
    $manifestDir = Join-Path $root '.github/coverage-manifest'
    New-Item -ItemType Directory -Path $manifestDir -Force | Out-Null
    $manifestText = (Get-Content (Join-Path $fixtureDir 'manifest-targets/Pkg.json') -Raw).Replace('"unit": 80', "`"unit`": $UnitTargetA")
    Set-Content -Path (Join-Path $manifestDir 'Pkg.json') -Value $manifestText
    Copy-Item (Join-Path $fixtureDir 'src') (Join-Path $root 'src') -Recurse
    foreach ($extra in $ExtraSourceFiles) {
        Set-Content -Path (Join-Path $root "src/Pkg/$extra") -Value 'namespace Pkg; public static class Extra { }'
    }
    $inputDir = Join-Path $root 'in'
    foreach ($flag in $Flags) {
        $target = Join-Path $inputDir $flag
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        $source = if ($flag -eq $BrokenFlag) { 'cobertura/broken' } else { "cobertura/$flag" }
        Copy-Item (Join-Path $fixtureDir "$source/coverage.cobertura.xml") $target
    }
    return [pscustomobject]@{
        Manifest = $manifestDir
        Input    = $inputDir
        Output   = Join-Path $root 'out'
        Touched  = Join-Path $fixtureDir 'touched.txt'
    }
}

# The argument list of a --check-targets run on a staged scenario.
function Get-TargetsArgs {
    param($Scenario, [switch]$Enforce, [switch]$RequireAll, [switch]$WithTouched)

    $list = @('--check-targets', '--manifest', $Scenario.Manifest, '--input', $Scenario.Input, '--output', $Scenario.Output)
    if ($Enforce) { $list += '--enforce' }
    if ($RequireAll) { $list += '--require-all-flags' }
    if ($WithTouched) { $list += @('--files-from', $Scenario.Touched) }
    return $list
}

$script:failures = 0

# Asserts the finding kinds written to check-targets.json by the last run of a scenario ('' means none).
function Assert-Findings {
    param([string]$Title, $Scenario, [string]$ExpectedKinds)

    $json = Get-Content (Join-Path $Scenario.Output 'check-targets.json') -Raw | ConvertFrom-Json
    $kinds = (@($json.findings | ForEach-Object { $_.kind }) | Sort-Object -Unique) -join ','
    if ($kinds -ne $ExpectedKinds) {
        Write-Host "FAIL: $Title - finding kinds '$kinds', expected '$ExpectedKinds'"
        $script:failures++
    }
    else {
        Write-Host "PASS: $Title - finding kinds '$kinds'"
    }
}

# The key sets of the summary JSON that docs/coverage/app.js reads must not change (#1651).
function Assert-SummaryShape {
    param($Scenario)

    $expected = Get-Content (Join-Path $fixtureDir 'expected-latest-shape.json') -Raw | ConvertFrom-Json
    $actual = Get-Content (Join-Path $Scenario.Output 'encina-coverage-summary.json') -Raw | ConvertFrom-Json
    $sections = @(
        @{ Name = 'top level'; Actual = $actual; Expected = $expected.topLevel; Optional = $expected.topLevelOptional },
        @{ Name = 'overall'; Actual = $actual.overall; Expected = $expected.overall; Optional = @() },
        @{ Name = 'fileTargets row'; Actual = $actual.fileTargets[0]; Expected = $expected.fileTargetRow; Optional = @() },
        @{ Name = 'package'; Actual = $actual.packages[0]; Expected = $expected.package; Optional = @() })
    foreach ($section in $sections) {
        $names = @($section.Actual.PSObject.Properties.Name)
        $missing = @($section.Expected | Where-Object { $names -notcontains $_ })
        $extra = @($names | Where-Object { ($section.Expected -notcontains $_) -and ($section.Optional -notcontains $_) })
        if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
            Write-Host "FAIL: summary JSON $($section.Name) keys changed (missing: $($missing -join ',') extra: $($extra -join ','))"
            $script:failures++
        }
        else {
            Write-Host "PASS: summary JSON $($section.Name) keys unchanged"
        }
    }
}

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

    # --check-targets (#1651): every scenario is run report-only and with --enforce.
    $good = New-TargetsScenario -Name 'targets-good'
    Assert-Run '7a. --check-targets --enforce on the good fixture (both flags, touched list)' (Get-TargetsArgs $good -Enforce -WithTouched) 0 'No findings'
    Assert-Findings '7a. good fixture' $good ''
    Assert-SummaryShape $good

    $below = New-TargetsScenario -Name 'targets-below' -UnitTargetA '95'
    Assert-Run '8a. --check-targets --enforce, A.cs unit below its target' (Get-TargetsArgs $below -Enforce) 1 'BELOW-TARGET: Pkg/A\.cs unit'
    Assert-Findings '8a. below target' $below 'below-target'
    Assert-Run '8b. --check-targets report-only, A.cs unit below its target' (Get-TargetsArgs $below) 0 'report-only'

    $unitOnly = New-TargetsScenario -Name 'targets-unit-only' -Flags @('UnitTests')
    Assert-Run '9a. --check-targets --enforce with only the unit flag in the run (guard did not run: not required)' (Get-TargetsArgs $unitOnly -Enforce) 0 'No findings'
    Assert-Run '9b. --check-targets --enforce --require-all-flags with guard missing' (Get-TargetsArgs $unitOnly -Enforce -RequireAll) 1 'NO-DATA: Pkg/A\.cs guard'
    Assert-Findings '9b. missing required flag' $unitOnly 'no-data'
    Assert-Run '9c. --check-targets --require-all-flags report-only with guard missing' (Get-TargetsArgs $unitOnly -RequireAll) 0 'report-only'

    $broken = New-TargetsScenario -Name 'targets-broken' -BrokenFlag 'GuardTests'
    Assert-Run '10a. --check-targets --enforce with an unparsable Cobertura file' (Get-TargetsArgs $broken -Enforce) 1 'PARSE-ERROR: GuardTests/coverage\.cobertura\.xml'
    Assert-Findings '10a. parse error' $broken 'no-data,parse-error'
    Assert-Run '10b. --check-targets report-only with an unparsable Cobertura file (recorded, exit 0)' (Get-TargetsArgs $broken) 0 'PARSE-ERROR'

    $untargeted = New-TargetsScenario -Name 'targets-untargeted' -ExtraSourceFiles @('Extra.cs')
    Assert-Run '11a. --check-targets --enforce, a touched src file has no per-file target' (Get-TargetsArgs $untargeted -Enforce -WithTouched) 1 'NO-TARGETS: src/Pkg/Extra\.cs'
    Assert-Findings '11a. touched file without targets' $untargeted 'no-targets'
    Assert-Run '11b. --check-targets --enforce without --files-from ignores the touched list' (Get-TargetsArgs $untargeted -Enforce) 0 'No findings'
    Assert-Run '11c. --check-targets report-only, a touched src file has no per-file target' (Get-TargetsArgs $untargeted -WithTouched) 0 'report-only'

    Assert-Run '12a. --enforce without --check-targets is rejected' @('--enforce', '--manifest', $good.Manifest, '--input', $good.Input, '--output', $good.Output) 1 'only apply to --check-targets'
    Assert-Run '12b. --check-targets cannot be combined with --check-justifications' @('--check-targets', '--check-justifications', '--manifest', $good.Manifest) 1 'cannot be combined'
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
