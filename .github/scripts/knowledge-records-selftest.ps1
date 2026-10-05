<#
.SYNOPSIS
Self-test for knowledge-records.cs (#1735): schema 1 and schema 2 acceptance, unknown schema rejection,
the audit.record existence rule and the audits-folder rule.

.DESCRIPTION
Builds small fixture trees under the temp folder (records in <root>/docs/knowledge/issues, results in
<root>/docs/knowledge/audits), runs `knowledge-records.cs --check --repo-root <root>` against each and asserts the
exit code and, for failures, the error text. The last case checks the real docs/knowledge tree of this checkout.

Exit code: 0 if every case passes, 1 otherwise.
#>

$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'knowledge-records.cs'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$base = Join-Path ([IO.Path]::GetTempPath()) ("knowledge-selftest-" + [guid]::NewGuid().ToString('N').Substring(0, 8))

$failures = [System.Collections.Generic.List[string]]::new()

function New-Fixture([string]$Name) {
    $root = Join-Path $base $Name
    New-Item -ItemType Directory -Force (Join-Path $root 'docs\knowledge\issues') | Out-Null
    return $root
}

function Add-File([string]$Root, [string]$Relative, [string]$Content) {
    $p = Join-Path $Root $Relative
    New-Item -ItemType Directory -Force (Split-Path -Parent $p) | Out-Null
    [IO.File]::WriteAllText($p, $Content, [Text.UTF8Encoding]::new($false))
}

# A record with the given schema. $AuditBlock is the whole 'audit:' block text ('' for none).
function Get-Record([int]$Schema, [int]$Issue, [string]$AuditBlock, [switch]$Flow) {
    $lists = if ($Flow) { "packages: [Encina, Encina.Messaging]`nprs: []`nlinked_prs: []`nremediation: []`n" } else { "packages:`nprs:`nlinked_prs:`nremediation:`n" }
    $knowledge = if ($Schema -eq 2) {
        "knowledge:`n  - kind: decision`n    statement: >-`n      A folded statement`n      over two lines.`n    current: `"yes`"`n    sources:`n      - plain source text without a marker`n    destinations:`n      - kind: adr`n        status: present`n        target: `"prose target that is not a path`"`n"
    }
    else {
        "knowledge:`n  - kind: decision`n    statement: `"A statement.`"`n    current: yes`n    sources:`n      - `"quote: \`"x\`" (issue #1, 2025-12-22)`"`n    destinations:`n      - kind: backlog`n        status: planned`n        target: `"#1735`"`n"
    }
    return "---`nschema: $Schema`nnav_exclude: true`nissue: $Issue`ntitle: `"[DEBT] Fixture`"`nclosed: 2025-12-22`nstate_reason: completed`noutcome: delivered`ntype: debt`narea: core`nreview: verified`n$lists$knowledge$AuditBlock---`n`n## Asked`n`nFixture.`n"
}

function Get-AuditBlock([string]$Verdict, [string]$Record) {
    return "audit:`n  checklist: 1`n  date: 2026-10-05`n  verdict: $Verdict`n  record: `"$Record`"`n"
}

function Invoke-Check([string]$Root, [string[]]$Extra = @()) {
    $out = & dotnet run --file $script -- --check --dir (Join-Path $Root 'docs\knowledge\issues') --repo-root $Root @Extra 2>&1 | ForEach-Object { "$_" }
    return @{ Exit = $LASTEXITCODE; Text = ($out -join "`n") }
}

function Assert-Case([string]$Name, $Result, [bool]$ExpectOk, [string]$ExpectText = '') {
    $ok = if ($ExpectOk) { $Result.Exit -eq 0 } else { $Result.Exit -ne 0 -and ($ExpectText -eq '' -or $Result.Text -like "*$ExpectText*") }
    if ($ok) { Write-Host "PASS  $Name" }
    else {
        Write-Host "FAIL  $Name (exit $($Result.Exit); expected $(if ($ExpectOk) { 'success' } else { "failure containing '$ExpectText'" }))`n$($Result.Text)"
        $failures.Add($Name)
    }
}

try {
    $r = New-Fixture 'v2-ok'
    Add-File $r 'docs\knowledge\issues\1.md' (Get-Record 2 1 '' -Flow)
    Assert-Case 'schema 2 accepted (flow lists, block scalar, free-text destination status, no audit block)' (Invoke-Check $r) $true

    $r = New-Fixture 'v1-ok'
    Add-File $r 'docs\knowledge\issues\2.md' (Get-Record 1 2 (Get-AuditBlock 'not-audited' 'not written yet'))
    Assert-Case 'schema 1 accepted' (Invoke-Check $r) $true

    $r = New-Fixture 'v3-unknown'
    Add-File $r 'docs\knowledge\issues\3.md' (Get-Record 3 3 '')
    Assert-Case 'unknown schema 3 rejected' (Invoke-Check $r) $false "'schema' must be one of 1, 2 (found '3')"

    $r = New-Fixture 'v1-flow'
    Add-File $r 'docs\knowledge\issues\4.md' (Get-Record 1 4 (Get-AuditBlock 'not-audited' 'not written yet') -Flow)
    Assert-Case 'schema 1 rejects flow lists' (Invoke-Check $r) $false 'only accepted by schema 2'

    $r = New-Fixture 'dangling'
    Add-File $r 'docs\knowledge\issues\5.md' (Get-Record 1 5 (Get-AuditBlock 'conforms' 'docs/knowledge/audits/issue-5.md'))
    Assert-Case 'dangling audit.record fails' (Invoke-Check $r) $false 'which does not exist'

    $r = New-Fixture 'dangling-v2'
    Add-File $r 'docs\knowledge\issues\6.md' (Get-Record 2 6 (Get-AuditBlock 'findings-tracked' 'docs/knowledge/audits/issue-6.md'))
    Assert-Case 'dangling audit.record fails for schema 2' (Invoke-Check $r) $false 'which does not exist'

    $r = New-Fixture 'not-audited-path'
    Add-File $r 'docs\knowledge\issues\7.md' (Get-Record 1 7 (Get-AuditBlock 'not-audited' 'docs/knowledge/audits/issue-7.md'))
    Assert-Case 'not-audited naming a missing result file fails' (Invoke-Check $r) $false 'which does not exist'

    $r = New-Fixture 'audited-no-file'
    Add-File $r 'docs\knowledge\issues\8.md' (Get-Record 1 8 (Get-AuditBlock 'conforms' 'not written yet'))
    Assert-Case 'an audited verdict needs a result file' (Invoke-Check $r) $false 'must name the audit result file'

    $r = New-Fixture 'linked-ok'
    Add-File $r 'docs\knowledge\issues\9.md' (Get-Record 1 9 (Get-AuditBlock 'conforms' 'docs/knowledge/audits/issue-9.md'))
    Add-File $r 'docs\knowledge\audits\issue-9.md' "# Audit of issue #9`n"
    Add-File $r 'docs\knowledge\audits\9\stages\verification.md' "Verdict: PASS`n"
    Assert-Case 'existing audit.record, result and stage folder accepted' (Invoke-Check $r) $true

    $r = New-Fixture 'orphan-result'
    Add-File $r 'docs\knowledge\issues\10.md' (Get-Record 1 10 (Get-AuditBlock 'not-audited' 'not written yet'))
    Add-File $r 'docs\knowledge\audits\issue-11.md' "# Audit of issue #11`n"
    Assert-Case 'audit result without a record fails' (Invoke-Check $r) $false 'no matching record issues/11.md'

    $r = New-Fixture 'orphan-stages'
    Add-File $r 'docs\knowledge\issues\12.md' (Get-Record 1 12 (Get-AuditBlock 'not-audited' 'not written yet'))
    Add-File $r 'docs\knowledge\audits\13\stages\code.md' "x`n"
    Assert-Case 'stage folder without a record fails' (Invoke-Check $r) $false 'no matching record issues/13.md'

    $r = New-Fixture 'delta-only'
    Add-File $r 'docs\knowledge\issues\17.md' (Get-Record 1 17 (Get-AuditBlock 'not-audited' 'not written yet'))
    Add-File $r 'docs\knowledge\audits\17\delta-2026-10\docs.md' "x`n"
    Assert-Case 'an audit folder holding only a delta folder is valid (#1763)' (Invoke-Check $r) $true ''

    $r = New-Fixture 'no-stages-no-delta'
    Add-File $r 'docs\knowledge\issues\18.md' (Get-Record 1 18 (Get-AuditBlock 'not-audited' 'not written yet'))
    Add-File $r 'docs\knowledge\audits\18\notes.md' "x`n"
    Assert-Case 'an audit folder with neither stages nor a delta folder fails (#1763)' (Invoke-Check $r) $false 'missing stages folder'

    $r = New-Fixture 'other-issue-target'
    Add-File $r 'docs\knowledge\issues\15.md' (Get-Record 1 15 (Get-AuditBlock 'conforms' 'docs/knowledge/audits/issue-16.md'))
    Add-File $r 'docs\knowledge\issues\16.md' (Get-Record 1 16 (Get-AuditBlock 'conforms' 'docs/knowledge/audits/issue-16.md'))
    Add-File $r 'docs\knowledge\audits\issue-16.md' "# Audit of issue #16`n"
    Assert-Case "audit.record naming another issue's result fails" (Invoke-Check $r) $false "must be exactly 'docs/knowledge/audits/issue-15.md'"

    $r = New-Fixture 'rooted-target'
    Add-File $r 'docs\knowledge\issues\17.md' (Get-Record 1 17 (Get-AuditBlock 'conforms' 'C:/Users/someone/audits/issue-17.md'))
    Assert-Case 'a rooted audit.record fails' (Invoke-Check $r) $false 'must be exactly'

    $r = New-Fixture 'artifacts-target'
    Add-File $r 'docs\knowledge\issues\18.md' (Get-Record 1 18 (Get-AuditBlock 'findings-tracked' 'artifacts/knowledge/audits/issue-18.md'))
    Add-File $r 'artifacts\knowledge\audits\issue-18.md' "# local only`n"
    Assert-Case 'an artifacts/ audit.record fails even when the ignored file exists locally' (Invoke-Check $r) $false 'must be exactly'

    $r = New-Fixture 'v1-nested-flow'
    Add-File $r 'docs\knowledge\issues\19.md' ((Get-Record 1 19 (Get-AuditBlock 'not-audited' 'not written yet')) -replace 'destinations:\n      - kind: backlog\n        status: planned\n        target: "#1735"', 'destinations: []')
    Assert-Case 'schema 1 rejects a nested flow list' (Invoke-Check $r) $false 'flow list'

    $r = New-Fixture 'stage-link'
    Add-File $r 'docs\knowledge\issues\20.md' (Get-Record 1 20 (Get-AuditBlock 'conforms' 'docs/knowledge/audits/issue-20.md'))
    Add-File $r 'docs\knowledge\audits\issue-20.md' "# Audit of issue #20`n- code: [20/stages/code.md](20/stages/code.md)`n"
    Add-File $r 'docs\knowledge\audits\20\stages\archivist.md' "x`n"
    Assert-Case 'a dangling stage link in an audit result fails' (Invoke-Check $r) $false "stage link '20/stages/code.md' does not exist"

    $r = New-Fixture 'skip-links'
    Add-File $r 'docs\knowledge\issues\14.md' (Get-Record 1 14 (Get-AuditBlock 'conforms' 'docs/knowledge/audits/issue-14.md'))
    Assert-Case '--skip-audit-links skips the existence check' (Invoke-Check $r @('--skip-audit-links')) $true

    $real = & dotnet run --file $script -- --check --dir (Join-Path $repo 'docs\knowledge\issues') 2>&1 | ForEach-Object { "$_" }
    Assert-Case 'the real docs/knowledge tree validates' @{ Exit = $LASTEXITCODE; Text = ($real -join "`n") } $true
}
finally {
    if (Test-Path -LiteralPath $base) { Remove-Item -LiteralPath $base -Recurse -Force }
}

if ($failures.Count -gt 0) {
    Write-Host "knowledge-records-selftest: $($failures.Count) case(s) failed."
    exit 1
}
Write-Host 'knowledge-records-selftest: all cases passed.'
exit 0
