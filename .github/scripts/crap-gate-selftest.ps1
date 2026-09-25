<#
.SYNOPSIS
Self-test for crap-gate.cs (issue #1346, closes #1354).

.DESCRIPTION
Runs crap-gate.cs against the hand-computed fixtures under
.github/scripts/testdata/crap-gate/ and asserts the documented behavior:

  1. `--report` against the violating fixture diff exits 0 and lists the RiskyMethod violation.
  2. `--enforce` against the same diff exits 1 (a non-exempt method exceeds the threshold).
  3. `--enforce` against a diff that only touches the exempt method exits 0 and prints EXEMPT.

Run this as the first step of the ci.yml `crap-gate` job, before the gate is trusted to block a
real PR: if crap-gate.cs regresses (diff parsing, the exemption comment, the exit codes), this
script fails loudly instead of the gate silently passing every PR.

Exit code: 0 if every assertion passes, 1 otherwise.
#>

$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$gateScript = Join-Path $scriptRoot 'crap-gate.cs'
$testDataDir = Join-Path $scriptRoot 'testdata/crap-gate'

if (-not (Test-Path $gateScript)) {
    Write-Error "crap-gate.cs not found at $gateScript"
    exit 1
}
if (-not (Test-Path $testDataDir)) {
    Write-Error "test fixtures not found at $testDataDir"
    exit 1
}

function Invoke-CrapGate {
    param([string[]]$GateArgs)

    Push-Location $testDataDir
    try {
        $output = & dotnet run --file $gateScript -- @GateArgs 2>&1 | Out-String
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
    }
    finally {
        Pop-Location
    }
}

$failures = 0

Write-Host '=== crap-gate self-test ==='

# 1. --report against the violating diff always exits 0 and lists the RiskyMethod violation.
Write-Host ''
Write-Host '--- Assertion 1: --report on the violating diff ---'
$result = Invoke-CrapGate -GateArgs @('--cobertura', 'SampleComplex.cobertura.xml', '--diff', 'sample.diff', '--report')
Write-Host $result.Output
if ($result.ExitCode -ne 0) {
    Write-Host "FAIL: --report should exit 0, got $($result.ExitCode)"
    $failures++
}
elseif ($result.Output -notmatch 'VIOLATION.*RiskyMethod') {
    Write-Host 'FAIL: --report output does not list the RiskyMethod violation'
    $failures++
}
else {
    Write-Host 'PASS: --report exits 0 and lists the RiskyMethod violation'
}

# 2. --enforce against the same diff exits 1 (RiskyMethod is a non-exempt violation).
Write-Host ''
Write-Host '--- Assertion 2: --enforce on the violating diff ---'
$result = Invoke-CrapGate -GateArgs @('--cobertura', 'SampleComplex.cobertura.xml', '--diff', 'sample.diff', '--enforce', '--threshold', '10')
Write-Host $result.Output
if ($result.ExitCode -ne 1) {
    Write-Host "FAIL: --enforce should exit 1 on the violating diff, got $($result.ExitCode)"
    $failures++
}
else {
    Write-Host 'PASS: --enforce exits 1 on the violating diff'
}

# 3. The exempt-only diff exits 0 even under --enforce, and prints EXEMPT.
Write-Host ''
Write-Host '--- Assertion 3: --enforce on the exempt-only diff ---'
$result = Invoke-CrapGate -GateArgs @('--cobertura', 'SampleComplex.cobertura.xml', '--diff', 'sample-exempt-only.diff', '--enforce', '--threshold', '10')
Write-Host $result.Output
if ($result.ExitCode -ne 0) {
    Write-Host "FAIL: the exempt-only diff should exit 0, got $($result.ExitCode)"
    $failures++
}
elseif ($result.Output -notmatch 'EXEMPT') {
    Write-Host 'FAIL: the exempt-only diff output does not print EXEMPT'
    $failures++
}
else {
    Write-Host 'PASS: the exempt-only diff exits 0 and prints EXEMPT'
}

Write-Host ''
if ($failures -gt 0) {
    Write-Host "crap-gate self-test FAILED ($failures assertion(s) failed)"
    exit 1
}

Write-Host 'crap-gate self-test PASSED'
exit 0
