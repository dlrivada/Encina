<#
.SYNOPSIS
    Turns a lychee JSON report of the scheduled full external link scan into one tracking issue (#1494).

.DESCRIPTION
    Parses the lychee JSON output, classifies every failure as broken (HTTP 404 or 410) or transient
    (everything else: 5xx, timeouts, connection failures, 403 from bot blocking), writes a Markdown body
    that follows .github/ISSUE_TEMPLATE/infrastructure.md, then:
      - any failure: creates or updates ONE tracking issue found by the label link-health (reopening it
        when closed);
      - clean scan: comments "Clean scan <run url>" and closes the tracking issue when it is open.
    The exit code is 1 only when at least one broken (404/410) link exists, otherwise 0.
    Failures are read from lychee 0.24.2's error_map and timeout_map (timeouts are transient).
    Fail-closed rules (the script throws, the job fails, the tracking issue is left alone):
      - the report has no error_map;
      - the errors or timeouts count is positive but its map has no entry;
      - -LycheeExitCode is a non-zero number but the report shows no failure.

.PARAMETER ReportPath
    Path of the lychee JSON report (lychee --format json --output <file>).

.PARAMETER RunUrl
    URL of the workflow run, linked from the issue body and the closing comment.

.PARAMETER DryRun
    Prints the gh commands that would run instead of running them.

.PARAMETER SimulatedIssue
    Only with -DryRun: the state of the tracking issue to pretend exists (none, open, closed).

.PARAMETER LycheeExitCode
    Exit code of the lychee step (steps.<id>.outputs.exit_code); empty skips the cross-check.

.PARAMETER SelfTest
    Runs the script logic against .github/scripts/fixtures/link-health/*.json in dry-run mode and asserts
    the classification, the body headers, the intended gh commands and the exit code. Needs no gh or network.
#>
[CmdletBinding()]
param(
    [string] $ReportPath,
    [string] $RunUrl = '(local run)',
    [string] $Repo = '',
    [switch] $DryRun,
    [ValidateSet('none', 'open', 'closed')]
    [string] $SimulatedIssue = 'none',
    [switch] $SelfTest,
    [string] $LycheeExitCode = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:IssueTitle = '[INFRA] External link health: failures found by the scheduled link scan'
$script:IssueLabel = 'link-health'
$script:BodyHeaders = @(
    '## Category', '## Description', '## Component Affected', '## Current Behavior',
    '## Expected Behavior', '## Impact', '## Proposed Solution', '## Related Issues'
)

function Get-StatusCode {
    param($Status)
    if ($null -eq $Status) { return $null }
    # The code is preferred. The text is a fallback only when it STARTS with a three-digit code
    # ("404 Not Found"): a network-error text that merely contains a URL ending in /404 stays transient.
    if ($Status -is [string]) {
        if ($Status -match '^\s*(\d{3})\b') { return [int] $Matches[1] }
        return $null
    }
    if ($Status.PSObject.Properties['code'] -and $null -ne $Status.code) { return [int] $Status.code }
    if ($Status.PSObject.Properties['text'] -and $Status.text -match '^\s*(\d{3})\b') { return [int] $Matches[1] }
    return $null
}

function Get-StatusText {
    param($Status)
    if ($null -eq $Status) { return 'unknown' }
    if ($Status -is [string]) { return $Status }
    if ($Status.PSObject.Properties['text'] -and $Status.text) { return [string] $Status.text }
    $code = Get-StatusCode $Status
    if ($null -ne $code) { return [string] $code }
    return 'unknown'
}

function Read-LinkFailures {
    param([string] $Path)
    $json = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    # lychee 0.24.2 (formatters/stats/response.rs, json.rs): a timeout goes to timeout_map, every
    # other failure to error_map; both are keyed by source file with { url, status, span } entries.
    $failures = [System.Collections.Generic.List[object]]::new()
    # Fail closed on schema drift: a report without the failure map must never read as a clean scan.
    if (-not $json.PSObject.Properties['error_map']) { throw "Unrecognized lychee report: no error_map in $Path" }
    foreach ($pair in @(@('error_map', 'errors'), @('timeout_map', 'timeouts'))) {
        $mapName = $pair[0]; $countName = $pair[1]
        $entries = 0
        if ($json.PSObject.Properties[$mapName]) {
            foreach ($source in $json.$mapName.PSObject.Properties) {
                foreach ($entry in @($source.Value)) {
                    $entries++
                    $code = Get-StatusCode $entry.status
                    $line = if ($entry.PSObject.Properties['span'] -and $entry.span -and $entry.span.PSObject.Properties['line']) { [string] $entry.span.line } else { '?' }
                    $class = if ($mapName -eq 'error_map' -and ($code -eq 404 -or $code -eq 410)) { 'broken' } else { 'transient' }
                    $failures.Add([pscustomobject]@{
                            File   = [string] $source.Name
                            Line   = $line
                            Url    = [string] $entry.url
                            Status = Get-StatusText $entry.status
                            Class  = $class
                        })
                }
            }
        }
        # A positive count with no entry in its map is schema drift, never a clean scan.
        if ($json.PSObject.Properties[$countName] -and [int] $json.$countName -gt 0 -and $entries -eq 0) {
            throw "Unrecognized lychee report: $countName is $($json.$countName) but $mapName has no entries in $Path"
        }
    }
    return , $failures
}

function ConvertTo-TableCell {
    param([string] $Text)
    return ($Text -replace '\|', '%7C' -replace '[\r\n]+', ' ').Trim()
}

function New-IssueBody {
    param($Failures, [string] $RunUrl)
    $broken = @($Failures | Where-Object Class -eq 'broken').Count
    $transient = @($Failures | Where-Object Class -eq 'transient').Count
    # GitHub rejects issue bodies over 65,536 characters (a network outage makes every link transient):
    # broken links sort first, the Status cell is truncated and rows are added until the accumulated
    # table reaches the budget, with a note for the rest. The fixed text around the table is < 3,000.
    $tableBudget = 56000
    $sorted = @($Failures | Sort-Object @{Expression = { $_.Class }; Descending = $false }, File, @{Expression = { [int]($_.Line -replace '\D', '0') } })
    $rows = [System.Collections.Generic.List[string]]::new()
    $used = 0
    foreach ($f in $sorted) {
        $status = ConvertTo-TableCell $f.Status
        if ($status.Length -gt 120) { $status = $status.Substring(0, 117) + '...' }
        $url = ConvertTo-TableCell $f.Url
        if ($url.Length -gt 300) { $url = $url.Substring(0, 297) + '...' }
        $row = '| `{0}:{1}` | {2} | {3} | {4} |' -f (ConvertTo-TableCell $f.File), $f.Line, $url, $status, $f.Class
        if ($used + $row.Length + 1 -gt $tableBudget) { break }
        $rows.Add($row)
        $used += $row.Length + 1
    }
    $omitted = $sorted.Count - $rows.Count
    if ($omitted -gt 0) {
        $rows.Add('')
        $rows.Add("Table truncated: $omitted more failure(s) are in the run log of $RunUrl.")
    }
    $lines = @(
        '## Category', '',
        '- [x] CI/CD Workflow (GitHub Actions)',
        '- [ ] Build System (MSBuild, .csproj, .slnx)',
        '- [ ] Docker / Docker Compose',
        '- [ ] Developer Tooling (CLI, scripts, dev containers)',
        '- [ ] Test Infrastructure (fixtures, runners, parallelization)',
        '- [ ] Package Publishing (NuGet)',
        '- [ ] Repository Configuration (branch rules, templates)',
        '- [ ] Other: ___', '',
        '## Description', '',
        'The scheduled full external link scan (`.github/workflows/link-check.yml`) found failing links. This issue is created and updated by `.github/scripts/link-health-report.ps1`; it is closed automatically by the next clean scan.', '',
        "Last scan: $RunUrl", '',
        "Broken (HTTP 404 or 410): **$broken**. Transient (5xx, timeouts, connection failures, 403): **$transient**.", '',
        '## Component Affected', '',
        '- **File(s)**: the Markdown files listed in the table below, `.github/workflows/link-check.yml`, `.github/lychee.toml`',
        '- **Environment**: GitHub Actions (scheduled and manual runs)', '',
        '## Current Behavior', '',
        '| File:line | URL | Status | Class |',
        '| --- | --- | --- | --- |'
    ) + @($rows) + @(
        '',
        '## Expected Behavior', '',
        'Broken links (404/410) are fixed or removed. Transient failures are re-checked by the next scan; a host that fails repeatedly is a candidate for `.github/lychee.toml` `exclude` or `.lycheeignore` with a justification.', '',
        '## Impact', '',
        '- **Developer Experience**: pull requests and pushes check only the Markdown files they change, so these failures never block them.',
        '- **CI/CD Time**: not applicable (reported out of band).',
        '- **Reliability**: a broken link fails the scheduled scan; transient failures only appear here.', '',
        '## Proposed Solution', '',
        'Fix or replace each broken link; confirm transient ones by opening the URL. The issue closes when a scan finds no failures.', '',
        '## Related Issues', '',
        '- #1494 - Check only changed files'' links on pull requests; move the full external scan to a schedule'
    )
    return ($lines -join "`n") + "`n"
}

function Invoke-Gh {
    param([string[]] $Arguments, [switch] $Mutating)
    $display = 'gh ' + (($Arguments | ForEach-Object { if ($_ -match '\s') { "'$_'" } else { $_ } }) -join ' ')
    if ($script:DryRunMode) {
        $script:Commands.Add($display)
        Write-Host "DRY-RUN $display"
        return $null
    }
    $out = & gh @Arguments
    if ($LASTEXITCODE -ne 0) { throw "gh failed ($LASTEXITCODE): $display" }
    return $out
}

function Invoke-LinkHealth {
    param([string] $Path, [string] $RunUrl, [string] $Repo, [bool] $Dry, [string] $Simulated, [string] $LycheeExit = '')
    $script:DryRunMode = $Dry
    $script:Commands = [System.Collections.Generic.List[string]]::new()
    $repoArgs = if ($Repo) { @('--repo', $Repo) } else { @() }

    $failures = Read-LinkFailures -Path $Path
    # Fail closed: lychee exited non-zero (link failures exit 2; a crash, bad input or bad config exits
    # otherwise) yet the report shows no failure, so the report cannot be trusted as a clean scan.
    if ($LycheeExit -match '^\d+$' -and [int] $LycheeExit -ne 0 -and $failures.Count -eq 0) {
        throw "lychee exited with code $LycheeExit but the report lists no failure; not treating it as a clean scan."
    }
    $broken = @($failures | Where-Object Class -eq 'broken').Count
    $transient = @($failures | Where-Object Class -eq 'transient').Count
    Write-Host "Link health: $broken broken (404/410), $transient transient."
    foreach ($f in $failures) { Write-Host ("  [{0}] {1}:{2} {3} -> {4}" -f $f.Class, $f.File, $f.Line, $f.Url, $f.Status) }

    # Existing tracking issue (open or closed), found by label.
    $existing = $null
    if ($Dry) {
        if ($Simulated -ne 'none') { $existing = [pscustomobject]@{ number = 1; state = $Simulated.ToUpperInvariant() } }
    }
    else {
        $found = Invoke-Gh -Arguments (@('issue', 'list') + $repoArgs + @('--label', $script:IssueLabel, '--state', 'all', '--limit', '1', '--json', 'number,state'))
        $parsed = @($found | ConvertFrom-Json)
        if ($parsed.Count -gt 0) { $existing = $parsed[0] }
    }

    $body = $null
    if ($failures.Count -gt 0) {
        $body = New-IssueBody -Failures $failures -RunUrl $RunUrl
        $bodyFile = Join-Path ([IO.Path]::GetTempPath()) "link-health-body-$([guid]::NewGuid().ToString('N')).md"
        [IO.File]::WriteAllText($bodyFile, $body)
        try {
            if ($null -eq $existing) {
                Invoke-Gh -Arguments (@('label', 'create', $script:IssueLabel) + $repoArgs + @('--description', 'External link scan findings', '--color', 'FBCA04', '--force')) | Out-Null
                Invoke-Gh -Arguments (@('issue', 'create') + $repoArgs + @('--title', $script:IssueTitle, '--label', "$($script:IssueLabel),area-ci-cd", '--body-file', $bodyFile)) | Out-Null
            }
            else {
                Invoke-Gh -Arguments (@('issue', 'edit', [string] $existing.number) + $repoArgs + @('--title', $script:IssueTitle, '--body-file', $bodyFile)) | Out-Null
                if ($existing.state -eq 'CLOSED') {
                    Invoke-Gh -Arguments (@('issue', 'reopen', [string] $existing.number) + $repoArgs) | Out-Null
                }
            }
        }
        finally { Remove-Item -LiteralPath $bodyFile -Force -ErrorAction SilentlyContinue }
    }
    elseif ($null -ne $existing -and $existing.state -eq 'OPEN') {
        Invoke-Gh -Arguments (@('issue', 'comment', [string] $existing.number) + $repoArgs + @('--body', "Clean scan $RunUrl")) | Out-Null
        Invoke-Gh -Arguments (@('issue', 'close', [string] $existing.number) + $repoArgs + @('--reason', 'completed')) | Out-Null
    }

    return [pscustomobject]@{
        Broken    = $broken
        Transient = $transient
        Body      = $body
        Commands  = @($script:Commands)
        ExitCode  = $(if ($broken -gt 0) { 1 } else { 0 })
    }
}

function Invoke-SelfTest {
    $fixtures = Join-Path $PSScriptRoot 'fixtures/link-health'
    $check = {
        param([bool] $Condition, [string] $Message)
        if ($Condition) { Write-Host "  PASS $Message" } else { Write-Host "  FAIL $Message"; $script:selfTestFailed++ }
    }
    $script:selfTestFailed = 0

    $cases = @(
        @{ Name = 'clean'; Broken = 0; Transient = 0; Exit = 0; Simulated = 'open'; Expect = @('gh issue comment 1', 'Clean scan https://example.test/run/1', 'gh issue close 1'); Absent = @('gh issue create') },
        @{ Name = 'clean'; Broken = 0; Transient = 0; Exit = 0; Simulated = 'none'; Expect = @(); Absent = @('gh issue') },
        @{ Name = 'transient-only'; Broken = 0; Transient = 3; Exit = 0; Simulated = 'none'; Expect = @('gh label create link-health', 'gh issue create'); Absent = @('gh issue close') },
        @{ Name = 'timeout-only'; Broken = 0; Transient = 1; Exit = 0; Simulated = 'open'; Expect = @('gh issue edit 1'); Absent = @('gh issue create', 'gh issue close') },
        @{ Name = 'network-error-404-url'; Broken = 0; Transient = 1; Exit = 0; Simulated = 'none'; Expect = @('gh issue create'); Absent = @('gh issue close') },
        @{ Name = 'broken-and-transient'; Broken = 2; Transient = 2; Exit = 1; Simulated = 'closed'; Expect = @('gh issue edit 1', 'gh issue reopen 1'); Absent = @('gh issue create') }
    )
    foreach ($case in $cases) {
        Write-Host "Fixture $($case.Name) (issue: $($case.Simulated))"
        $r = Invoke-LinkHealth -Path (Join-Path $fixtures "$($case.Name).json") -RunUrl 'https://example.test/run/1' -Repo '' -Dry $true -Simulated $case.Simulated
        & $check ($r.Broken -eq $case.Broken) "broken count is $($case.Broken) (got $($r.Broken))"
        & $check ($r.Transient -eq $case.Transient) "transient count is $($case.Transient) (got $($r.Transient))"
        & $check ($r.ExitCode -eq $case.Exit) "exit code is $($case.Exit) (got $($r.ExitCode))"
        $joined = $r.Commands -join "`n"
        foreach ($e in $case.Expect) { & $check ($joined.Contains($e)) "commands contain '$e'" }
        foreach ($a in $case.Absent) { & $check (-not $joined.Contains($a)) "commands do not contain '$a'" }
        if ($case.Broken + $case.Transient -gt 0) {
            $bodyLines = $r.Body -split "`n"
            $positions = @(foreach ($h in $script:BodyHeaders) { [array]::IndexOf($bodyLines, $h) })
            & $check (@($positions | Where-Object { $_ -lt 0 }).Count -eq 0) 'body has every template header'
            $sorted = @($positions | Sort-Object)
            & $check (($positions -join ',') -eq ($sorted -join ',')) 'body headers are in template order'
            & $check ($r.Body.Contains('- [x] CI/CD Workflow (GitHub Actions)')) 'Category CI/CD is ticked'
            & $check ($r.Body.Contains('| File:line | URL | Status | Class |')) 'body has the report table'
        }
    }

    # Classification detail on the mixed fixture.
    $mixed = Read-LinkFailures -Path (Join-Path $fixtures 'broken-and-transient.json')
    $byUrl = @{}
    foreach ($f in $mixed) { $byUrl[$f.Url] = $f }
    & $check ($byUrl['https://example.org/gone-for-good'].Class -eq 'broken') '410 is broken'
    & $check ($byUrl['https://example.org/moved-away|pipe'].Class -eq 'broken') '404 is broken'
    & $check ($byUrl['https://example.org/slow'].Class -eq 'transient') 'a timeout_map entry is read and is transient'
    & $check ($byUrl['https://example.org/overloaded'].Class -eq 'transient') '504 is transient'
    $t = Read-LinkFailures -Path (Join-Path $fixtures 'transient-only.json')
    & $check (@($t | Where-Object { $_.Status -like '403*' -and $_.Class -eq 'transient' }).Count -eq 1) '403 is transient'
    $threw = $false
    try { Read-LinkFailures -Path (Join-Path $fixtures 'schema-drift.json') | Out-Null } catch { $threw = $true }
    & $check $threw 'a report without error_map fails closed instead of reading as clean'
    $threw = $false
    try { Read-LinkFailures -Path (Join-Path $fixtures 'count-drift.json') | Out-Null } catch { $threw = $true }
    & $check $threw 'a positive errors/timeouts count with an empty map fails closed'
    $threw = $false
    try { Invoke-LinkHealth -Path (Join-Path $fixtures 'clean.json') -RunUrl 'u' -Repo '' -Dry $true -Simulated 'open' -LycheeExit '1' | Out-Null } catch { $threw = $true }
    & $check $threw 'lychee exit code 1 with a clean report fails closed'
    $threw = $false
    try { Invoke-LinkHealth -Path (Join-Path $fixtures 'clean.json') -RunUrl 'u' -Repo '' -Dry $true -Simulated 'open' -LycheeExit '0' | Out-Null } catch { $threw = $true }
    & $check (-not $threw) 'lychee exit code 0 with a clean report is clean'
    $net = Read-LinkFailures -Path (Join-Path $fixtures 'network-error-404-url.json')
    & $check ($net[0].Class -eq 'transient') 'a network error whose text contains /404 stays transient'
    $many = 1..300 | ForEach-Object {
        [pscustomobject]@{
            File   = "docs/some/deeply/nested/folder/structure/page-number-$_.md"
            Line   = "$_"
            Url    = "https://www.example-host-$_.org/a/rather/long/path/segment/for/realism/page-$_"
            Status = "Network error: error sending request for url (https://www.example-host-$_.org/a/rather/long/path/segment/for/realism/page-$_): client error (Connect): dns error: failed to lookup address information"
            Class  = 'transient'
        }
    }
    $big = New-IssueBody -Failures $many -RunUrl 'https://example.test/run/1'
    & $check ($big.Length -lt 60000 -and $big.Contains('Table truncated:')) "a body with 300 realistic long failures stays under 60,000 characters (got $($big.Length))"
    $body = New-IssueBody -Failures $mixed -RunUrl 'https://example.test/run/1'
    & $check ($body.Contains('moved-away%7Cpipe')) 'pipe in a URL is escaped in the table'

    if ($script:selfTestFailed -gt 0) {
        Write-Host "SELF-TEST FAILED: $($script:selfTestFailed) assertion(s)"
        return 1
    }
    Write-Host 'SELF-TEST PASSED'
    return 0
}

if ($SelfTest) {
    exit (Invoke-SelfTest)
}

if (-not $ReportPath) { throw 'ReportPath is required (or use -SelfTest).' }
if (-not (Test-Path -LiteralPath $ReportPath)) {
    # lychee writes no report when it fails before scanning; do not report a clean scan in that case.
    throw "Report not found: $ReportPath"
}
$result = Invoke-LinkHealth -Path $ReportPath -RunUrl $RunUrl -Repo $Repo -Dry $DryRun.IsPresent -Simulated $SimulatedIssue -LycheeExit $LycheeExitCode
exit $result.ExitCode
