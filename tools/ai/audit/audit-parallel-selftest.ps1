# tools/ai/audit/audit-parallel-selftest.ps1 (#2234)
#
# Self-test for running several SPEC-003 audits in parallel, each one individually. Builds a throwaway "main"
# repository with a local bare "origin", four packages under src/ and a queue of five closed issues whose
# pre-drafts name their packages (10: Encina.A, 11: Encina.A, 12: Encina.B, 13: Encina.C, 14: Encina.D), and runs
# the REAL scripts with git and gh replaced by PowerShell functions (git forwards to the real git except `push`;
# gh records and answers with fixed data). Asserts:
#   1. audit-next opens #10, then skips #11 (its scope overlaps #10 on Encina.A) and opens #12; each audit has its
#      own state file with its scope and the concurrent audits; the limit (pipeline.json maxParallelAudits, 2 by
#      default) refuses a third audit;
#   2. maxParallelAudits 3 opens a third audit (#13, #11 still skipped); a value outside 2-5 is refused; a stray
#      wia-* worktree with no state file is refused; -Issue that is not the next non-overlapping issue is refused;
#   3. the resolver: with three audits open a script needs -Issue, -Issue names one audit, a worktree context
#      names it too, and -Issue contradicting the worktree context is refused; audit-stage -List shows all three;
#   4. the cross-audit duplicate check of audit-draft-remediation.ps1 -Prepare: a finding of #13 that matches an
#      unopened draft of the concurrent audit #12 stops -Prepare with nothing written; once that draft is opened
#      the finding is recorded as a duplicate of its issue; the manifest names the concurrent audits; in the other
#      direction #12 does not stop on an unopened draft of the higher-numbered #13 (the lower number goes first);
#   5. the cross-audit duplicate check of open-remediation.ps1: a draft of #12 that matches an opened draft of #10
#      opens nothing; against an unopened one it only warns and opens; 5b. #10 and #12 opening matching drafts at
#      the same time (two processes) create exactly one issue, the other run refuses;
#   6. closing out of order: #12 (opened second) closes first; #10 and #13 stay open with their state, progress.csv
#      records #12, and the next audit-next skips the overlapping #11 again and opens #14; once #10 closes, #11 is
#      opened;
#   7. no audit forgotten half-way: an audit is recorded as open with its start date the moment audit-next starts
#      it and is never handed to a second slot; audit-stage -List shows every open audit's stage and days open and
#      flags one open more than 2 days STALE; audit-next refuses to start another audit while one is stale, naming
#      it, unless -Force;
#   8. concurrent registrations (Register-OpenAudit, the open-audits lock): six processes registering at once, the
#      same issue twice among them, leave exactly one registration per issue and mutually consistent `concurrent`
#      lists. This case fails when the lock is removed (verified when it was written, #2234 PR review).
# Nothing here can push or open anything.
#
# Exit code: 0 if every assertion passes, 1 otherwise.

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$realGit = (Get-Command git -CommandType Application | Select-Object -First 1).Source
$base = Join-Path ([IO.Path]::GetTempPath()) ("audit-parallel-selftest-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
$failures = [System.Collections.Generic.List[string]]::new()

function Assert-That([string]$Name, [bool]$Condition, [string]$Detail = '') {
    if ($Condition) { Write-Host "PASS  $Name" }
    else { Write-Host "FAIL  $Name $Detail"; $failures.Add($Name) }
}

# Fixture git: any failure stops the self-test (a fixture that did not build must never read as a pass or a
# confusing assertion failure). GitMayFail is for the calls whose failure the test expects or tolerates.
function Git {
    $raw = & $realGit @args 2>&1
    $code = $LASTEXITCODE
    $out = @($raw | ForEach-Object { "$_" })
    if ($code -ne 0) { throw "fixture command 'git $($args -join ' ')' failed (exit $code): $($out -join ' ')" }
    return $out
}
function GitMayFail { & $realGit @args 2>&1 | ForEach-Object { "$_" } }

function Write-Text([string]$Path, [string]$Text) {
    New-Item -ItemType Directory -Force (Split-Path -Parent $Path) | Out-Null
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}

try {
    $main = Join-Path $base 'main'
    $origin = Join-Path $base 'origin.git'
    $stubs = Join-Path $base 'stubs'
    $log = Join-Path $base 'commands.log'
    New-Item -ItemType Directory -Force $main, $stubs | Out-Null

    Write-Text (Join-Path $stubs 'git-stub.ps1') @'
Add-Content -LiteralPath $env:AUDIT_STUB_LOG -Value ('git ' + ($args -join ' '))
if ($args -contains 'push') { exit 0 }
& $env:AUDIT_STUB_REAL_GIT @args
exit $LASTEXITCODE
'@
    Write-Text (Join-Path $stubs 'gh-stub.ps1') @'
Add-Content -LiteralPath $env:AUDIT_STUB_LOG -Value ('gh ' + ($args -join ' '))
if ($args[0] -eq 'issue' -and $args[1] -eq 'view') { 'CLOSED'; exit 0 }
if ($args[0] -eq 'label') { 'technical-debt'; 'area-testing'; 'bug'; 'p0-mandatory'; 'p1-recommended'; 'p2-post-1.0'; exit 0 }
if ($args[0] -eq 'api') { "v0.14.0 $([char]0x2014) Hardening"; "v0.19.0 $([char]0x2014) Providers & Testing"; "v0.21.0 $([char]0x2014) Documentation"; exit 0 }
if ($args[0] -eq 'issue' -and $args[1] -eq 'create') {
    # The concurrency case (section 5b) widens the create and counts every create in its own file.
    if ($env:AUDIT_STUB_CREATE_DELAY_MS) { Start-Sleep -Milliseconds ([int]$env:AUDIT_STUB_CREATE_DELAY_MS) }
    if ($env:AUDIT_STUB_CREATES) { New-Item -ItemType File -Path (Join-Path $env:AUDIT_STUB_CREATES ([guid]::NewGuid().ToString('N'))) | Out-Null }
    'https://github.com/dlrivada/Encina/issues/7001'; exit 0
}
if ($args -contains 'list') { exit 0 }
if ($args[0] -eq 'project') { exit 0 }
'https://github.com/dlrivada/Encina/pull/9999'
exit 0
'@

    # --- fixture repository ---------------------------------------------------------------------------------
    Git init -q -b main $main | Out-Null
    Git -C $main config user.name 'Selftest' | Out-Null
    Git -C $main config user.email 'selftest@example.invalid' | Out-Null
    Git init -q --bare -b main $origin | Out-Null
    Copy-Item -Recurse (Join-Path $repo 'tools\ai\audit') (Join-Path $main 'tools\ai\audit') -Force
    New-Item -ItemType Directory -Force (Join-Path $main '.github\scripts') | Out-Null
    Copy-Item (Join-Path $repo '.github\scripts\knowledge-records.cs') (Join-Path $main '.github\scripts\knowledge-records.cs') -Force
    Copy-Item -Recurse (Join-Path $repo '.github\ISSUE_TEMPLATE') (Join-Path $main '.github\ISSUE_TEMPLATE') -Force
    Write-Text (Join-Path $main '.gitignore') "artifacts/`n"
    foreach ($p in 'A', 'B', 'C', 'D') { Write-Text (Join-Path $main "src\Encina.$p\Widget.cs") "namespace Encina.$p;`n" }
    Git -C $main add -A | Out-Null
    Git -C $main commit -q -m 'fixture main' | Out-Null
    Git -C $main remote add origin $origin | Out-Null
    Git -C $main push -q origin main | Out-Null

    $knowledge = Join-Path $main 'artifacts\knowledge'
    $openDir = Join-Path $knowledge 'open-audits'
    $remDir = Join-Path $knowledge 'remediation'
    Write-Text (Join-Path $knowledge 'audit-queue.txt') "10`n11`n12`n13`n14`n15`n"
    Write-Text (Join-Path $knowledge 'progress.csv') "issue,status,findings_blocker,findings_major,findings_minor,remediation_opened,notes`n"
    $packages = @{ 10 = 'Encina.A'; 11 = 'Encina.A'; 12 = 'Encina.B'; 13 = 'Encina.C'; 14 = 'Encina.D'; 15 = 'Encina.B' }
    foreach ($n in $packages.Keys) {
        Write-Text (Join-Path $knowledge "predraft\$n.md") "---`nissue: $n`ntitle: `"[DEBT] Fixture $n`"`npackages: [$($packages[$n]), Encina.NotAPackage]`n---`n`n## Decisions`n- none`n"
    }
    $pipelinePath = Join-Path $main 'tools\ai\audit\pipeline.json'
    $pipelineText = Get-Content $pipelinePath -Raw
    function Set-MaxParallel([string]$Value) { Write-Text $pipelinePath ($pipelineText -replace '"maxParallelAudits":\s*\d+', "`"maxParallelAudits`": $Value") }
    function Get-State([int]$N) { $p = Join-Path $openDir "$N.json"; if (Test-Path $p) { Get-Content $p -Raw | ConvertFrom-Json } else { $null } }
    function Get-Wt([int]$N) { Join-Path $main ".claude\worktrees\wia-$N" }

    $env:AUDIT_STUB_LOG = $log
    $env:AUDIT_STUB_REAL_GIT = $realGit
    # Runs one of the fixture's own scripts in a child pwsh whose git and gh are the stubs; -From sets the child's
    # current directory (a worktree context for the resolver).
    function Invoke-Script([string]$Script, [string[]]$ScriptArgs, [string]$From = $main) {
        if (Test-Path $log) { Remove-Item $log -Force }
        $command = "Set-Location -LiteralPath '$From'; function git { & '$stubs\git-stub.ps1' @args }; function gh { & '$stubs\gh-stub.ps1' @args }; & '$main\tools\ai\audit\$Script' $($ScriptArgs -join ' '); exit `$LASTEXITCODE"
        $out = & pwsh -NoProfile -Command $command 2>&1 | ForEach-Object { "$_" }
        return @{ Exit = $LASTEXITCODE; Text = ($out -join "`n"); Log = $(if (Test-Path $log) { @(Get-Content $log) } else { @() }) }
    }

    # --- 1. two audits, the overlap rule and the default limit -----------------------------------------------
    $r = Invoke-Script 'audit-next.ps1' @()
    Assert-That 'audit-next opens #10, the first queue entry' ($r.Exit -eq 0 -and $r.Text -like '*Audit #10 opened*' -and (Test-Path (Get-Wt 10))) $r.Text
    $s10 = Get-State 10
    Assert-That '#10 has its own state file with its scope (only folders under src/ count) and no concurrent audit' ($s10 -and (@($s10.scope) -join ',') -eq 'Encina.A' -and @($s10.concurrent).Count -eq 0 -and $s10.branch -eq 'audit/10') ($s10 | ConvertTo-Json -Compress)
    $r = Invoke-Script 'audit-next.ps1' @()
    Assert-That 'the second audit-next skips #11 (overlaps #10 on Encina.A) and opens #12' ($r.Exit -eq 0 -and $r.Text -like '*Audit #12 opened*' -and $r.Text -like '*#11 overlaps #10 (Encina.A)*' -and -not (Test-Path (Get-Wt 11))) $r.Text
    $s10 = Get-State 10; $s12 = Get-State 12
    Assert-That 'each audit lists the other as concurrent' ((@($s12.concurrent) -join ',') -eq '10' -and (@($s10.concurrent) -join ',') -eq '12') "$($s10 | ConvertTo-Json -Compress) $($s12 | ConvertTo-Json -Compress)"
    $r = Invoke-Script 'audit-next.ps1' @()
    Assert-That 'the default limit (2) refuses a third audit' ($r.Exit -ne 0 -and $r.Text -like '*2 audits are already open*maxParallelAudits is 2*' -and -not (Test-Path (Get-Wt 13))) $r.Text

    # --- 2. three audits, invalid settings, stray worktree, -Issue ---------------------------------------------
    foreach ($bad in '1', '6', '"three"') {
        Set-MaxParallel $bad
        $r = Invoke-Script 'audit-next.ps1' @()
        Assert-That "maxParallelAudits $bad is refused" ($r.Exit -ne 0 -and $r.Text -like '*maxParallelAudits must be an integer from 2 to 5*') $r.Text
    }
    Set-MaxParallel '3'
    New-Item -ItemType Directory -Force (Join-Path $main '.claude\worktrees\wia-77') | Out-Null
    $r = Invoke-Script 'audit-next.ps1' @()
    Assert-That 'a stray wia-77 with no state file is refused' ($r.Exit -ne 0 -and $r.Text -like '*not an open audit (wia-77*') $r.Text
    Remove-Item -Recurse -Force (Join-Path $main '.claude\worktrees\wia-77')
    $r = Invoke-Script 'audit-next.ps1' @('-Issue', '11')
    Assert-That '-Issue 11 is refused: it overlaps #10, the next non-overlapping issue is #13' ($r.Exit -ne 0 -and $r.Text -like '*-Issue 11 is not the next pending issue*next: #13*') $r.Text
    $r = Invoke-Script 'audit-next.ps1' @('-Issue', '13')
    Assert-That 'maxParallelAudits 3: -Issue 13 opens a third audit' ($r.Exit -eq 0 -and $r.Text -like '*Audit #13 opened*open audits now: 3 of 3*') $r.Text
    $s13 = Get-State 13
    Assert-That '#13 lists #10 and #12 as concurrent, and both list #13' ((@($s13.concurrent) -join ',') -eq '10,12' -and (@((Get-State 10).concurrent) -join ',') -eq '12,13' -and (@((Get-State 12).concurrent) -join ',') -eq '10,13') ($s13 | ConvertTo-Json -Compress)

    # --- 3. the resolver -----------------------------------------------------------------------------------------
    $r = Invoke-Script 'audit-stage.ps1' @('-Next')
    Assert-That 'with three audits open, audit-stage -Next without -Issue is refused' ($r.Exit -ne 0 -and $r.Text -like '*3 audits are open (#10, #12, #13); pass -Issue*') $r.Text
    $r = Invoke-Script 'audit-stage.ps1' @('-Next', '-Issue', '12')
    Assert-That '-Issue 12 names #12' ($r.Exit -eq 0 -and $r.Text -like '*Next stage: archivist (spawn issue-archivist on issue #12*wia-12*') $r.Text
    $r = Invoke-Script 'audit-stage.ps1' @('-Next') (Get-Wt 13)
    Assert-That 'run from inside wia-13, the worktree names the audit' ($r.Exit -eq 0 -and $r.Text -like '*issue #13*') $r.Text
    $r = Invoke-Script 'audit-stage.ps1' @('-Next', '-Issue', '12') (Get-Wt 13)
    Assert-That '-Issue 12 from inside wia-13 is refused (contradiction)' ($r.Exit -ne 0 -and $r.Text -like '*contradicts the worktree wia-13*') $r.Text
    $r = Invoke-Script 'audit-stage.ps1' @('-Next', '-Issue', '11')
    Assert-That '-Issue of an issue that is not open is refused' ($r.Exit -ne 0 -and $r.Text -like '*#11 is not an open audit*') $r.Text
    $r = Invoke-Script 'audit-stage.ps1' @('-List')
    Assert-That 'audit-stage -List shows the three open audits with their next stage' ($r.Exit -eq 0 -and $r.Text -like '*#10 wia-10: next stage archivist*' -and $r.Text -like '*#12 wia-12*' -and $r.Text -like '*#13 wia-13*') $r.Text

    # --- 4. cross-audit duplicate check of -Prepare --------------------------------------------------------------
    $wt13 = Get-Wt 13
    $finding = '1. **Major** - `src/Encina.C/Widget.cs:12`: `WidgetStore.SaveAsync` swallows the store error and reports success.'
    Write-Text (Join-Path $wt13 'artifacts\knowledge\stages\code.md') "## Findings`n$finding`n## Lessons for the pipeline`n- none`n"
    Write-Text (Join-Path $wt13 'artifacts\knowledge\stages\tests.md') "## Findings`n- none`n"
    Write-Text (Join-Path $wt13 'artifacts\knowledge\stages\docs.md') "## Findings`n- none`n"
    $otherDraft = Join-Path $remDir '12-code-1-widgetstore-swallows.md'
    Write-Text $otherDraft "<!-- issue`ntitle: [BUG] WidgetStore.SaveAsync swallows the store error`nlabels: bug`nmilestone:`nkind: bug`n-->`n`n## Description`n`nThe store error is lost.`n`n## Steps to Reproduce`n`n1. Call ``WidgetStore.SaveAsync`` in ``src/Encina.C/Widget.cs:12`` with a failing store.`n"
    $r = Invoke-Script 'audit-draft-remediation.ps1' @('-Prepare', '-NoGh', '-Issue', '13')
    Assert-That '-Prepare of #13 stops on a match with the unopened draft of the concurrent audit #12' ($r.Exit -ne 0 -and $r.Text -like '*cross-audit duplicate check*12-code-1-widgetstore-swallows.md*concurrent audit #12*not opened*') $r.Text
    Assert-That 'the stopped -Prepare wrote nothing' (-not (Test-Path (Join-Path $remDir '_manifest-13.json')) -and -not (Test-Path (Join-Path $remDir '_input-13-code-1.md')))
    # The other direction (CodeRabbit on PR #2243): the lower-numbered audit goes first. #12's -Prepare does not stop
    # on the matching unopened draft of the higher-numbered #13, so the two can never block each other.
    $wt12 = Get-Wt 12
    $finding12 = '1. **Major** - `src/Encina.C/Widget.cs:12`: `WidgetStore.SaveAsync` swallows the store error, seen from #12.'
    Write-Text (Join-Path $wt12 'artifacts\knowledge\stages\code.md') "## Findings`n$finding12`n## Lessons for the pipeline`n- none`n"
    Write-Text (Join-Path $wt12 'artifacts\knowledge\stages\tests.md') "## Findings`n- none`n"
    Write-Text (Join-Path $wt12 'artifacts\knowledge\stages\docs.md') "## Findings`n- none`n"
    $draft13 = Join-Path $remDir '13-code-1-widgetstore-swallows.md'
    Write-Text $draft13 (Get-Content $otherDraft -Raw)
    Move-Item -LiteralPath $otherDraft -Destination (Join-Path $base 'draft12.saved')
    $r = Invoke-Script 'audit-draft-remediation.ps1' @('-Prepare', '-NoGh', '-Issue', '12')
    $m12 = if (Test-Path (Join-Path $remDir '_manifest-12.json')) { Get-Content (Join-Path $remDir '_manifest-12.json') -Raw | ConvertFrom-Json } else { $null }
    $f12 = if ($m12) { @($m12.findings | Where-Object { $_.key -eq 'code 1' })[0] } else { $null }
    Assert-That '-Prepare of #12 does not stop on the matching unopened draft of the higher-numbered #13 (lower number goes first)' ($r.Exit -eq 0 -and $f12 -and $f12.draftFile -and -not $f12.duplicateOf) "$($r.Text) $($f12 | ConvertTo-Json -Compress)"
    Remove-Item -Force $draft13
    Get-ChildItem $remDir -File | Where-Object { $_.Name -like '_input-12-*' -or $_.Name -eq '_manifest-12.json' } | Remove-Item -Force
    Move-Item -LiteralPath (Join-Path $base 'draft12.saved') -Destination $otherDraft
    Write-Text (Join-Path $remDir 'opened.csv') "12-code-1-widgetstore-swallows.md,https://github.com/dlrivada/Encina/issues/5551`n"
    $r = Invoke-Script 'audit-draft-remediation.ps1' @('-Prepare', '-NoGh', '-Issue', '13')
    $manifest = if (Test-Path (Join-Path $remDir '_manifest-13.json')) { Get-Content (Join-Path $remDir '_manifest-13.json') -Raw | ConvertFrom-Json } else { $null }
    $f1 = if ($manifest) { @($manifest.findings | Where-Object { $_.key -eq 'code 1' })[0] } else { $null }
    Assert-That 'once that draft is an issue, the finding is its duplicate (as a sequential run would record it)' ($r.Exit -eq 0 -and $f1 -and $f1.duplicateOf -eq '5551' -and $f1.duplicateSource -eq 'cross-audit' -and -not $f1.draftFile -and $f1.remediationLine -like '*duplicate of #5551 (draft 12-code-1-widgetstore-swallows.md of the concurrent audit #12)*') "$($r.Text) $($f1 | ConvertTo-Json -Compress)"
    Assert-That 'the manifest names the concurrent audits the check compared' ((@($manifest.concurrentAudits) -join ',') -eq '10,12') ($manifest.concurrentAudits -join ',')
    $r = Invoke-Script 'audit-draft-remediation.ps1' @('-Prepare', '-NoGh', '-Issue', '13', '-NotDuplicate', '"code 1"')
    $f1 = @((Get-Content (Join-Path $remDir '_manifest-13.json') -Raw | ConvertFrom-Json).findings | Where-Object { $_.key -eq 'code 1' })[0]
    Assert-That '-NotDuplicate (the verifier''s ruling) skips the cross-audit check too' ($r.Exit -eq 0 -and -not $f1.duplicateOf -and $f1.draftFile) ($f1 | ConvertTo-Json -Compress)
    Remove-Item -Force (Join-Path $remDir 'opened.csv'), $otherDraft
    Get-ChildItem $remDir -Filter '*13*' -File | Remove-Item -Force

    # --- 5. cross-audit duplicate check of open-remediation.ps1 ---------------------------------------------------
    $draft12 = Join-Path $remDir '12-code-2-widget-retry.md'
    $input12 = Join-Path $remDir '_input-12-code-2.md'
    Write-Text $input12 '`src/Encina.B/Widget.cs:40`: `WidgetRetry.Run` retries forever.'
    Write-Text (Join-Path $remDir '_manifest-12.json') (@{ issue = 12; findings = @(@{ key = 'code 2'; draftFile = $draft12; inputFile = $input12 }) } | ConvertTo-Json -Depth 5)
    $draftText = "<!-- issue`ntitle: [DEBT] WidgetRetry.Run retries forever`nlabels: technical-debt`nmilestone:`nkind: debt`n-->`n`n## Type`n`n- [x] Incorrect implementation`n`n## Description`n`nRetries never stop.`n`n## Location`n`n- **File(s)**: ``src/Encina.B/Widget.cs:40`` (``WidgetRetry.Run``)`n`n## Priority`n`n- [ ] **High** - x`n- [x] **Medium** - y`n- [ ] **Low** - z`n"
    Write-Text $draft12 $draftText
    $draft10 = Join-Path $remDir '10-code-1-widget-retry.md'
    Write-Text $draft10 ($draftText.Replace('Retries never stop.', 'Seen from audit #10.'))
    Write-Text (Join-Path $remDir 'opened.csv') "10-code-1-widget-retry.md,https://github.com/dlrivada/Encina/issues/6001`n"
    $r = Invoke-Script 'open-remediation.ps1' @('-Issue', '12')
    Assert-That 'open-remediation of #12 refuses a draft that duplicates an opened draft of the concurrent audit #10, and opens nothing' ($r.Exit -ne 0 -and ($r.Text -replace '\s+\|\s+', ' ' -replace '\s+', ' ') -like '*12-code-2-widget-retry.md duplicates 10-code-1-widget-retry.md of the concurrent audit #10*issues/6001*' -and -not ($r.Log | Where-Object { $_ -like 'gh issue create*' })) $r.Text
    Remove-Item -Force (Join-Path $remDir 'opened.csv')
    $r = Invoke-Script 'open-remediation.ps1' @('-Issue', '12')
    Assert-That 'against an unopened draft it only warns, and #12 opens first' ($r.Exit -eq 0 -and $r.Text -like '*matches 10-code-1-widget-retry.md of the concurrent audit #10, not opened yet*' -and @($r.Log | Where-Object { $_ -like 'gh issue create*' }).Count -eq 1) $r.Text
    Remove-Item -Force $draft10

    # --- 5b. two audits open matching drafts at the same time (CodeRabbit on PR #2243) ---------------------------------
    # #10 and #12 each hold an unopened draft of the same defect and run open-remediation.ps1 at once, as separate
    # processes; the stubbed `gh issue create` takes 6 s. The re-check, the create and the opened.csv row happen
    # under the open-audits lock, so exactly one issue is created and the other run refuses. Without the lock both
    # pass the up-front check and both create.
    $pairText = "<!-- issue`ntitle: [DEBT] CacheStore.Get ignores the expiry`nlabels: technical-debt`nmilestone:`nkind: debt`n-->`n`n## Type`n`n- [x] Incorrect implementation`n`n## Description`n`nExpired entries are served.`n`n## Location`n`n- **File(s)**: ``src/Encina.B/Cache.cs:7`` (``CacheStore.Get``)`n`n## Priority`n`n- [ ] **High** - x`n- [x] **Medium** - y`n- [ ] **Low** - z`n"
    $pairFinding = '`src/Encina.B/Cache.cs:7`: `CacheStore.Get` ignores the expiry.'
    $pairDrafts = @{}
    foreach ($n in 10, 12) {
        $pairDraft = Join-Path $remDir "$n-code-3-cache-expiry.md"
        $pairInput = Join-Path $remDir "_input-$n-code-3.md"
        Write-Text $pairDraft $pairText
        Write-Text $pairInput $pairFinding
        $findings = @(@{ key = 'code 3'; draftFile = $pairDraft; inputFile = $pairInput })
        if ($n -eq 12) { $findings += @{ key = 'code 2'; draftFile = $draft12; inputFile = $input12 } }
        Write-Text (Join-Path $remDir "_manifest-$n.json") (@{ issue = $n; findings = $findings } | ConvertTo-Json -Depth 5)
        $pairDrafts[$n] = $pairDraft
    }
    $creates = Join-Path $base 'creates'
    New-Item -ItemType Directory -Force $creates | Out-Null
    $openJobs = foreach ($n in 10, 12) {
        $jobLog = Join-Path $base "open-$n.log"
        $command = "Set-Location -LiteralPath '$main'; function gh { & '$stubs\gh-stub.ps1' @args }; & '$main\tools\ai\audit\open-remediation.ps1' -Issue $n; exit `$LASTEXITCODE"
        Start-Job -ArgumentList $command, $jobLog, $creates, $n -ScriptBlock {
            param($Command, $JobLog, $Creates, $N)
            $env:AUDIT_STUB_LOG = $JobLog
            $env:AUDIT_STUB_CREATES = $Creates
            $env:AUDIT_STUB_CREATE_DELAY_MS = '6000'
            $raw = & pwsh -NoProfile -Command $Command 2>&1
            $code = $LASTEXITCODE
            [pscustomobject]@{ Issue = $N; Exit = $code; Text = (@($raw | ForEach-Object { "$_" }) -join ' ') }
        }
    }
    $openResults = @($openJobs | Wait-Job -Timeout 240 | Receive-Job)
    $openJobs | Remove-Job -Force
    $createCount = @(Get-ChildItem $creates -File).Count
    $winners = @($openResults | Where-Object { $_.Exit -eq 0 })
    $losers = @($openResults | Where-Object { $_.Exit -ne 0 -and ($_.Text -replace '\s+\|\s+', ' ' -replace '\s+', ' ') -like '*already opened as*' })
    Assert-That 'two audits opening matching drafts at once: exactly one gh issue create, one run succeeds and the other refuses' ($createCount -eq 1 -and $winners.Count -eq 1 -and $losers.Count -eq 1) ("creates: $createCount; " + (($openResults | ForEach-Object { "#$($_.Issue) exit $($_.Exit): $($_.Text)" }) -join ' || '))
    # Leave no unopened draft behind for the closing steps below.
    $openedNames = @(Get-Content (Join-Path $remDir 'opened.csv') | ForEach-Object { ($_ -split ',')[0].Trim() })
    foreach ($pairDraft in $pairDrafts.Values) { if ($openedNames -notcontains (Split-Path -Leaf $pairDraft)) { Remove-Item -Force $pairDraft } }

    # --- 6. closing out of order -----------------------------------------------------------------------------------
    $pipeline = Get-Content $pipelinePath -Raw | ConvertFrom-Json
    function Complete-Audit([int]$N) {
        $wt = Get-Wt $N
        $stagesDir = Join-Path $wt 'artifacts\knowledge\stages'
        foreach ($stage in $pipeline.stages) {
            $text = if ($stage.stage -eq 'verification') { "Verdict: PASS`n## Verified claims`nFixture.`n" } else { "## Findings`n- none`n" }
            Write-Text (Join-Path $stagesDir $stage.artifact) $text
            Git -C $wt add -f "artifacts/knowledge/stages/$($stage.artifact)" | Out-Null
            Git -C $wt -c user.name=Selftest -c user.email=selftest@example.invalid commit -q -m "audit #${N}: $($stage.stage) stage" -m "Stage: $($stage.stage)" | Out-Null
        }
        Write-Text (Join-Path $stagesDir 'lessons.md') "No lessons recorded across the pipeline stages for #$N.`n"
        Write-Text (Join-Path $wt "artifacts\knowledge\issues\$N.md") "---`nschema: 1`nnav_exclude: true`nissue: $N`ntitle: `"[DEBT] Fixture $N`"`nclosed: 2025-12-22`nstate_reason: completed`noutcome: delivered`ntype: debt`narea: core`nreview: verified`npackages:`n  - Encina`nprs:`nlinked_prs:`nremediation:`nknowledge:`n  - kind: decision`n    statement: `"A statement.`"`n    current: yes`n    sources:`n      - `"paraphrase: fixture (issue #1, 2025-12-22)`"`n    destinations:`n      - kind: backlog`n        status: planned`n        target: `"#2234`"`naudit:`n  checklist: 1`n  date: 2026-10-10`n  verdict: not-audited`n  record: `"not written yet`"`n---`n`nRECORD $N`n"
    }
    Complete-Audit 12
    $r = Invoke-Script 'audit-done.ps1' @('-Issue', '12')
    Assert-That '#12, opened second, closes first' ($r.Exit -eq 0 -and -not (Test-Path (Get-Wt 12)) -and $null -eq (Get-State 12)) $r.Text
    Assert-That 'the other audits stay open with their state and worktrees' ((Get-State 10) -and (Get-State 13) -and (Test-Path (Get-Wt 10)) -and (Test-Path (Get-Wt 13)))
    Assert-That 'progress.csv records #12 only' ((@(Get-Content (Join-Path $knowledge 'progress.csv') | Where-Object { $_ -match '^\d+,done' } | ForEach-Object { ($_ -split ',')[0] }) -join ',') -eq '12')
    $r = Invoke-Script 'audit-next.ps1' @()
    Assert-That 'the freed slot goes to #14: #11 still overlaps #10, #12 is done' ($r.Exit -eq 0 -and $r.Text -like '*Audit #14 opened*' -and $r.Text -like '*#11 overlaps #10*') $r.Text
    Assert-That '#14 lists the audits open with it (#10, #13) as concurrent' ((@((Get-State 14).concurrent) -join ',') -eq '10,13') ((Get-State 14) | ConvertTo-Json -Compress)
    Complete-Audit 10
    $r = Invoke-Script 'audit-done.ps1' @('-Issue', '10')
    Assert-That '#10 closes' ($r.Exit -eq 0 -and $null -eq (Get-State 10)) $r.Text
    $r = Invoke-Script 'audit-next.ps1' @()
    Assert-That 'with #10 closed, the skipped #11 is opened (queue order kept)' ($r.Exit -eq 0 -and $r.Text -like '*Audit #11 opened*') $r.Text
    Assert-That 'progress.csv records the audits in the order they closed (#12, #10)' ((@(Get-Content (Join-Path $knowledge 'progress.csv') | Where-Object { $_ -match '^\d+,done' } | ForEach-Object { ($_ -split ',')[0] }) -join ',') -eq '12,10')

    # --- 7. no audit forgotten half-way ---------------------------------------------------------------------------
    $s11 = Get-State 11
    $age11 = ([DateTime]::UtcNow - ([DateTime]$s11.startedUtc).ToUniversalTime()).TotalMinutes
    Assert-That 'an audit is recorded as open with its start date the moment audit-next starts it' ($s11 -and $age11 -ge 0 -and $age11 -lt 30) ($s11 | ConvertTo-Json -Compress)
    $r = Invoke-Script 'audit-next.ps1' @('-Issue', '11')
    Assert-That 'audit-next never hands an open audit to a second slot' ($r.Exit -ne 0 -and @(Get-ChildItem $openDir -Filter '11.json').Count -eq 1) $r.Text
    $s13 = Get-Content (Join-Path $openDir '13.json') -Raw | ConvertFrom-Json -AsHashtable
    $s13.startedUtc = [DateTime]::UtcNow.AddDays(-5).ToString('yyyy-MM-ddTHH:mm:ssZ')
    Write-Text (Join-Path $openDir '13.json') ($s13 | ConvertTo-Json)
    $r = Invoke-Script 'audit-stage.ps1' @('-List')
    Assert-That '-List shows every open audit with its stage and days open, and flags #13 (5 days) STALE' ($r.Exit -eq 0 -and $r.Text -match '#13 wia-13: next stage \S+.*5\.0 days open STALE' -and $r.Text -match '#11 wia-11: next stage archivist \(issue-archivist\); 0\.0 days open; scope' -and $r.Text -notmatch '#11[^\n]*STALE' -and $r.Text -notmatch '#14[^\n]*STALE') $r.Text
    Set-MaxParallel '4'
    $r = Invoke-Script 'audit-next.ps1' @()
    Assert-That 'audit-next refuses to start another audit while #13 is stale, naming it' ($r.Exit -ne 0 -and $r.Text -like '*stale audit(s) open for more than 2 days: #13 (wia-13*-Force*' -and $null -eq (Get-State 15)) $r.Text
    $r = Invoke-Script 'audit-next.ps1' @('-Force')
    Assert-That '-Force starts the next audit anyway, with a warning naming the stale one' ($r.Exit -eq 0 -and $r.Text -like '*-Force*#13*' -and $r.Text -like '*Audit #15 opened*') $r.Text

    # --- 8. concurrent registrations (the open-audits lock) ---------------------------------------------------------
    # Six registrations start at once as separate processes against the same state folder, with the window between
    # reading the open audits and writing the state files widened to 3 s (the test seam of Register-OpenAudit): four
    # different issues and the same issue twice. With the lock they run one after another: the duplicate is refused
    # and every pair of open audits lists each other as concurrent. Without it every run reads the same snapshot,
    # so updates are lost and the duplicate is written twice.
    $regRoot = Join-Path $base 'register'
    $regOpen = Join-Path $regRoot 'artifacts\knowledge\open-audits'
    New-Item -ItemType Directory -Force $regOpen | Out-Null
    Write-Text (Join-Path $regOpen '50.json') (@{ issue = 50; worktree = 'wia-50'; branch = 'audit/50'; startedUtc = '2026-10-10T00:00:00Z'; scope = @(); concurrent = @() } | ConvertTo-Json)
    $lib = Join-Path $main 'tools\ai\audit\_audit-lib.ps1'
    $jobs = foreach ($n in 61, 62, 63, 64, 70, 70) {
        Start-Job -ArgumentList $lib, $regRoot, $n -ScriptBlock {
            param($Lib, $Root, $N)
            $ErrorActionPreference = 'Stop'
            $env:ENCINA_AUDIT_REGISTER_TEST_DELAY_MS = '3000'
            . $Lib
            try { $null = Register-OpenAudit $Root ([ordered]@{ issue = $N; worktree = "wia-$N"; branch = "audit/$N"; startedUtc = '2026-10-10T00:00:00Z' }); "ok $N" }
            catch { "refused ${N}: $($_.Exception.Message)" }
        }
    }
    $results = @($jobs | Wait-Job -Timeout 180 | Receive-Job)
    $jobs | Remove-Job -Force
    Assert-That 'concurrent registrations: the same issue is registered once and refused once' (@($results | Where-Object { $_ -eq 'ok 70' }).Count -eq 1 -and @($results | Where-Object { $_ -like 'refused 70: *already an open audit*' }).Count -eq 1) ($results -join ' | ')
    Assert-That 'concurrent registrations: the four different issues are all registered' (@($results | Where-Object { $_ -match '^ok 6[1-4]$' }).Count -eq 4) ($results -join ' | ')
    $regStates = @{}
    foreach ($f in Get-ChildItem $regOpen -Filter '*.json') { $regStates[[int]$f.BaseName] = Get-Content $f.FullName -Raw | ConvertFrom-Json }
    $allOpen = @($regStates.Keys | Sort-Object)
    $inconsistent = @(foreach ($k in $allOpen) {
            $expected = (@($allOpen | Where-Object { $_ -ne $k }) -join ',')
            $actual = (@($regStates[$k].concurrent | ForEach-Object { [int]$_ } | Sort-Object) -join ',')
            if ($expected -ne $actual) { "#$k concurrent [$actual], expected [$expected]" }
        })
    Assert-That 'concurrent registrations: six open audits, and each lists every other one as concurrent (no lost update)' (($allOpen -join ',') -eq '50,61,62,63,64,70' -and $inconsistent.Count -eq 0) (($allOpen -join ',') + ' ' + ($inconsistent -join '; '))
}
finally {
    Set-Location $PSScriptRoot
    if (Test-Path -LiteralPath $base) {
        Get-ChildItem -LiteralPath $base -Recurse -Force -File -ErrorAction SilentlyContinue | ForEach-Object { $_.IsReadOnly = $false }
        Remove-Item -LiteralPath $base -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($failures.Count -gt 0) {
    Write-Host "audit-parallel-selftest: $($failures.Count) assertion(s) failed."
    exit 1
}
Write-Host 'audit-parallel-selftest: all assertions passed.'
exit 0
