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

param([switch]$Next, [switch]$RepairAuthors)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$audit = Get-CurrentAudit $mainRoot
if ($null -eq $audit) { Write-Error 'audit-stage: no open audit (artifacts/knowledge/current-audit.json not found). Run audit-next.ps1 first.'; exit 1 }

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
    $committedText | Set-Content -LiteralPath $tempPath -Encoding utf8
    [System.IO.File]::Move($tempPath, $authorsPath, $true)

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

if (-not $Next) { 'Usage: audit-stage.ps1 -Next | -RepairAuthors'; exit 0 }

$wt = [string]$audit.worktree
$n = [string]$audit.issue
$stagesDir = Get-StagesDir $wt
$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')
# Named $dueStage, not $next: the -Next switch parameter above already owns $Next, and PowerShell variable
# names are case-insensitive, so `$next = <object>` would try to convert the result into a SwitchParameter.
$dueStage = Get-NextStage $stagesDir $wt $pipeline

if ($null -eq $dueStage) {
    "All stages complete for #$n. Run audit-done.ps1."
    exit 0
}
if ($dueStage.agent -match '^issue-|^audit-|^docs-reviewer$') {
    "Next stage: $($dueStage.stage) (spawn $($dueStage.agent) on issue #$n, worktree $wt)"
}
else {
    "Next stage: $($dueStage.stage) (run $($dueStage.agent) for issue #$n)"
}
