# tools/ai/audit/audit-stage.ps1 -Next | -RepairAuthors (#1345, #1374)
#
# -Next prints the next stage due for the open audit: the first stage of tools/ai/audit/pipeline.json whose
# artifact is missing, or present but not yet committed with audit-commit-stage.ps1 (Get-NextStage,
# _audit-lib.ps1). Used by the orchestrator to decide which specialist to spawn next, and by
# .claude/hooks/audit-stage-guard.ps1 to enforce the same order.
#
# -RepairAuthors is the one sanctioned way to repair a corrupted artifacts\knowledge\stages\.authors.json for
# the open audit (#1374): it restores the sidecar from the committed version at the audit worktree's HEAD
# (`git show HEAD:artifacts/knowledge/stages/.authors.json`; '{}' when HEAD carries no such file yet), written
# with the same temp-file-and-move atomic replacement enforce-path-ownership.ps1 uses, then reports which
# stages the working-tree copy recorded that the committed copy does not -- those stages must be re-written by
# their assigned agent through the Write/Edit tool to be recorded again, since this command only ever writes
# content that was already committed and so cannot be used to fabricate an author. Refuses when no audit is
# open, same as -Next.
#
# The working-tree copy is scanned leniently (a regex over `"<stage>": { "agent": "<name>" ...`), not with a
# strict JSON parse: the whole point of this command is to run against a sidecar that may be exactly the
# corrupted, unparseable file #1374 describes (two concatenated JSON objects), so a stage name it can still
# recognise inside that garbage is more useful to report than giving up because the file as a whole is not
# valid JSON.

#
# #2234: several audits may be open at once. -Issue <n> names the audit; it may be omitted when the script runs
# from (or the current directory is) that audit's wia-<n> worktree, or when only one audit is open
# (Resolve-OpenAudit, _audit-lib.ps1). -List prints every open audit with its next stage and the days it has
# been open, and flags STALE any audit open more than 2 days ($script:StaleAuditDays), so none is forgotten
# half-way; audit-next.ps1 refuses to start another audit while one is stale (unless -Force).

param([switch]$Next, [switch]$RepairAuthors, [switch]$List, [int]$Issue)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot

if ($List) {
    try { $all = @(Get-OpenAudits $mainRoot) } catch { Write-Error "audit-stage: $($_.Exception.Message)"; exit 1 }
    if ($all.Count -eq 0) { 'audit-stage: no open audit.'; exit 0 }
    $now = [DateTime]::UtcNow
    foreach ($a in $all) {
        $due = Get-NextStage (Get-StagesDir ([string]$a.worktree)) ([string]$a.worktree) (Get-AuditPipeline $a)
        $mode = if (Test-DeltaAudit $a) { " (delta $($a.set))" } else { '' }
        $age = Get-AuditAgeDays $a $now
        $ageText = if ([double]::IsInfinity($age)) { 'start date unknown' } else { '{0:0.0} days open' -f $age }
        $stale = if ($age -gt $script:StaleAuditDays) { " STALE (open more than $($script:StaleAuditDays) days: finish or close it)" } else { '' }
        "#$($a.issue)$mode wia-$($a.issue): next stage $(if ($null -eq $due) { 'none (run audit-done.ps1 -Issue ' + $a.issue + ')' } else { "$($due.stage) ($($due.agent))" }); $ageText$stale; scope: $(if (@($a.scope).Count) { @($a.scope) -join ', ' } else { '-' })"
    }
    exit 0
}

try { $audit = Resolve-OpenAudit $mainRoot $Issue $PSScriptRoot }
catch { Write-Error "audit-stage: $($_.Exception.Message)"; exit 1 }

if ($RepairAuthors) {
    $wt = [string]$audit.worktree
    $n = [string]$audit.issue
    $authorsPath = Join-Path $wt 'artifacts\knowledge\stages\.authors.json'

    # The working-tree entries before repair (stage name -> agent), so the report below can name the stages
    # the committed copy is about to drop. Scanned leniently rather than with a strict JSON parse: see the
    # header comment above.
    $workingEntries = @{}
    if (Test-Path -LiteralPath $authorsPath) {
        $rawWorking = Get-Content -LiteralPath $authorsPath -Raw
        foreach ($m in [regex]::Matches($rawWorking, '"(?<stage>[^"]+)"\s*:\s*\{\s*"agent"\s*:\s*"(?<agent>[^"]*)"')) {
            $workingEntries[$m.Groups['stage'].Value] = $m.Groups['agent'].Value
        }
    }

    $committedRaw = & git -C $wt show 'HEAD:artifacts/knowledge/stages/.authors.json' 2>$null
    $committedText = if ($LASTEXITCODE -ne 0 -or $null -eq $committedRaw) { '{}' } else { ($committedRaw -join "`n") }
    if ([string]::IsNullOrWhiteSpace($committedText)) { $committedText = '{}' }

    $committedEntries = @{}
    try {
        $parsedCommitted = $committedText | ConvertFrom-Json -AsHashtable
        if ($null -ne $parsedCommitted) { $committedEntries = $parsedCommitted }
    }
    catch { $committedEntries = @{} }

    $dir = Split-Path -Parent $authorsPath
    New-Item -ItemType Directory -Force $dir | Out-Null
    $tempPath = Join-Path $dir ".authors.json.$PID.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        $committedText | Set-Content -LiteralPath $tempPath -Encoding utf8
        [System.IO.File]::Move($tempPath, $authorsPath, $true)
        $tempPath = $null
    }
    finally {
        # A leftover temp file means Move never completed (e.g. the destination was locked): remove it so a
        # failed repair does not leave a stray '.authors.json.<pid>.<guid>.tmp' behind (#1374 review).
        if ($null -ne $tempPath -and (Test-Path -LiteralPath $tempPath)) { Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue }
    }

    "audit-stage: restored artifacts\knowledge\stages\.authors.json for #$n from the committed version at HEAD."
    $dropped = @($workingEntries.Keys | Where-Object { -not $committedEntries.ContainsKey($_) })
    if ($dropped.Count -gt 0) {
        "The committed sidecar has no entry for: $($dropped -join ', '). Their assigned agent must re-write that stage's artifact through the Write/Edit tool to be recorded again."
    }
    else {
        'No stage entries were dropped by the repair.'
    }
    exit 0
}

if (-not $Next) { 'Usage: audit-stage.ps1 -Next | -RepairAuthors [-Issue n] | -List'; exit 0 }

$wt = [string]$audit.worktree
$n = [string]$audit.issue
$stagesDir = Get-StagesDir $wt
$pipeline = Get-AuditPipeline $audit
# Named $dueStage, not $next: the -Next switch parameter above already owns $Next, and PowerShell variable
# names are case-insensitive, so `$next = <object>` would try to convert the result into a SwitchParameter.
$dueStage = Get-NextStage $stagesDir $wt $pipeline

if ($null -eq $dueStage) {
    "All stages complete for #$n. Run audit-done.ps1 -Issue $n."
    exit 0
}
if ($dueStage.agent -eq 'remediation-drafter') {
    # #1572: the remediation stage is a script step, an agent spawn and a script step.
    "Next stage: $($dueStage.stage) for issue #$n, worktree $wt -- three steps:"
    "  1. pwsh -NoProfile -File tools/ai/audit/audit-draft-remediation.ps1 -Prepare -Issue $n"
    "  2. spawn remediation-drafter in the foreground, naming #$n, wia-$n and artifacts/knowledge/remediation/_manifest-$n.json"
    "  3. pwsh -NoProfile -File tools/ai/audit/audit-draft-remediation.ps1 -Finalize -Issue $n (on exit 1, re-spawn remediation-drafter with its output, then run -Finalize again)"
}
elseif ($dueStage.agent -match '^issue-|^audit-|^docs-reviewer$') {
    "Next stage: $($dueStage.stage) (spawn $($dueStage.agent) on issue #$n, worktree $wt)"
}
else {
    "Next stage: $($dueStage.stage) (run $($dueStage.agent) for issue #$n)"
}
