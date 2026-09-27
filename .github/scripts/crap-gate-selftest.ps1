<#
.SYNOPSIS
Self-test for crap-gate.cs (issue #1346, closes #1354).

.DESCRIPTION
Runs crap-gate.cs against the hand-computed fixtures under
.github/scripts/testdata/crap-gate/ and asserts the documented behavior:

  1. `--report` against the violating fixture diff exits 0 and lists the RiskyMethod violation.
  2. `--enforce` against the same diff exits 1 (a non-exempt method exceeds the threshold).
  3. `--enforce` against a diff that only touches the exempt method exits 0 and prints EXEMPT.
  4. `--report` against a diff touching a line inside a nested lambda's own body attributes only
     the lambda, not the enclosing method whose numeric MinLine..MaxLine range spans that line too
     (issue #1506: exact-membership attribution, not a range check).
  5. `--enforce` against a diff touching an unsequenced line (no method's own recorded lines contain
     it) that sits inside both a small nested closure's range AND a large, poorly-covered enclosing
     method's range still reports the enclosing method's violation (issue #1506, PR #1508 review: the
     range-check fallback must attribute to every covering method, not only the innermost one, or a
     real violation in the enclosing method silently escapes).

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

# 4. A diff touching a line inside a nested lambda's own body attributes only the lambda, not the
#    enclosing method whose numeric MinLine..MaxLine range spans the same line (issue #1506).
Write-Host ''
Write-Host '--- Assertion 4: --report on a diff touching a line inside a nested lambda ---'
$result = Invoke-CrapGate -GateArgs @('--cobertura', 'SampleLambda.cobertura.xml', '--diff', 'sample-lambda.diff', '--report')
Write-Host $result.Output
if ($result.ExitCode -ne 0) {
    Write-Host "FAIL: --report should exit 0, got $($result.ExitCode)"
    $failures++
}
elseif ($result.Output -match 'Enclosing\b') {
    Write-Host 'FAIL: the enclosing method must not be attributed a line that belongs only to the nested lambda'
    $failures++
}
elseif ($result.Output -notmatch 'Changed methods analyzed: 1') {
    Write-Host 'FAIL: expected exactly one method (the lambda) to be attributed the changed line'
    $failures++
}
else {
    Write-Host 'PASS: only the lambda is attributed the changed line; the enclosing method is not'
}

# 5. An unsequenced line inside both a small nested closure's range and a large, poorly-covered
#    enclosing method's range must still report the enclosing method's violation under the range
#    fallback (issue #1506, PR #1508 review: no under-reporting).
Write-Host ''
Write-Host '--- Assertion 5: --enforce on a diff touching an unsequenced line shared by a closure and its poorly-covered enclosing method ---'
$result = Invoke-CrapGate -GateArgs @('--cobertura', 'SparseEscape.cobertura.xml', '--diff', 'sparse-escape.diff', '--enforce')
Write-Host $result.Output
if ($result.ExitCode -ne 1) {
    Write-Host "FAIL: --enforce should exit 1 (the Enclosing violation must not escape), got $($result.ExitCode)"
    $failures++
}
elseif ($result.Output -notmatch 'VIOLATION.*Enclosing') {
    Write-Host 'FAIL: --enforce output does not list the Enclosing violation'
    $failures++
}
else {
    Write-Host 'PASS: the enclosing method''s violation is still reported for the shared unsequenced line'
}

Write-Host ''
if ($failures -gt 0) {
    Write-Host "crap-gate self-test FAILED ($failures assertion(s) failed)"
    exit 1
}

Write-Host 'crap-gate self-test PASSED'
exit 0
