# tools/ai/audit/audit-next.ps1 [-Issue n] (#1345)
#
# Starts one SPEC-003 audit: the queue discipline that replaces batching issues or running two audits at
# once. Refuses when an audit is already open, or when a stray `wia-*` worktree exists even though
# current-audit.json is missing (a previous audit that was not closed with audit-done.ps1). With no -Issue,
# takes the first queue entry (artifacts/knowledge/audit-queue.txt, ascending) not already recorded in
# artifacts/knowledge/progress.csv. -Issue n is only accepted when n equals that same next pending entry AND
# gh reports it CLOSED; there is no override to jump the queue.
#
# Creates .claude/worktrees/wia-<n> on a NEW LOCAL branch audit/<n> based on origin/main (never a detached
# HEAD, and the branch is never pushed: audit-done.ps1 deletes it after collecting), writes
# artifacts/knowledge/current-audit.json, and makes sure a local-model pre-draft
# (artifacts/knowledge/predraft/<n>.md) exists before the archivist stage starts.

# -Delta <set> (#1763): starts a DELTA audit instead, a re-check of an audit already published, for the rules
# decided after it ran only (set rules-2026-10: docs rule (a), tests rule (b)). It takes the next of the
# audited issues (the distinct issues of progress.csv, in order) that has no delta in the set's progress file
# (artifacts/knowledge/delta-progress-<set>.csv) -- or -Issue n, any issue of that list not yet done --, reuses
# the scope the original audit recorded (the knowledge record and the published archivist/code stage files in
# docs/knowledge, written to artifacts/knowledge/delta-scope.md of the new worktree; no pre-draft, no
# classify-scope), creates the audit worktree as today, records `mode: delta` and `set` in current-audit.json
# and prints the first stage of tools/ai/audit/pipeline-delta.json.
#   pwsh -NoProfile -File tools/ai/audit/audit-next.ps1 -Delta rules-2026-10 [-Issue n]

param([int]$Issue, [string]$Delta = '')

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$knowledgeRoot = Get-KnowledgeRoot $mainRoot
$currentAuditPath = Get-CurrentAuditPath $mainRoot
$worktreesRoot = Join-Path $mainRoot '.claude\worktrees'

if (Test-Path -LiteralPath $currentAuditPath) {
    $current = Get-Content -LiteralPath $currentAuditPath -Raw | ConvertFrom-Json
    Write-Error "audit-next: an audit is already open (issue #$($current.issue), worktree $($current.worktree)). Run audit-done.ps1 to close it first."
    exit 1
}
$existingWia = @(Get-ChildItem -LiteralPath $worktreesRoot -Directory -Filter 'wia-*' -ErrorAction SilentlyContinue)
if ($existingWia.Count -gt 0) {
    $names = ($existingWia | ForEach-Object { $_.Name }) -join ', '
    Write-Error "audit-next: no current-audit.json, but a wia-* worktree already exists ($names). Remove it (git worktree remove) or restore artifacts/knowledge/current-audit.json before starting a new audit."
    exit 1
}

if ($Delta) {
    $deltaPipelineSource = Get-Pipeline $PSScriptRoot 'pipeline-delta.json'
    if ([string]$deltaPipelineSource.delta.set -ne $Delta) {
        Write-Error "audit-next: unknown delta set '$Delta' (tools/ai/audit/pipeline-delta.json defines '$($deltaPipelineSource.delta.set)')."
        exit 1
    }
    $candidates = @(Get-DeltaCandidates $knowledgeRoot)
    if ($candidates.Count -eq 0) { Write-Error "audit-next: no audited issue in artifacts/knowledge/progress.csv; nothing to re-check."; exit 1 }
    $deltaDone = @(Get-DeltaDone $knowledgeRoot $Delta)
    $pendingDelta = @($candidates | Where-Object { $deltaDone -notcontains $_ })
    if ($pendingDelta.Count -eq 0) { Write-Error "audit-next: every audited issue already has a '$Delta' delta."; exit 1 }
    if ($Issue) {
        if ($pendingDelta -notcontains [string]$Issue) {
            Write-Error "audit-next: -Issue $Issue is not an audited issue without a '$Delta' delta (pending: $($pendingDelta -join ', '))."
            exit 1
        }
        $n = $Issue
    }
    else { $n = [int]$pendingDelta[0] }

    $fetchOutput = & git -C $mainRoot fetch origin main 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: git fetch origin main failed: $fetchOutput"; exit 1 }
    $wt = Join-Path $worktreesRoot "wia-$n"
    $branch = "audit/$n"
    $addOutput = & git -C $mainRoot worktree add -b $branch $wt origin/main 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: failed to create worktree $wt on branch $branch`: $addOutput"; exit 1 }

    $scopeText = New-DeltaScopeText $n $wt $Delta
    if ($null -eq $scopeText) {
        $rmOut = & git -C $mainRoot worktree remove $wt --force 2>&1
        $brOut = & git -C $mainRoot branch -D $branch 2>&1
        Write-Error "audit-next: docs/knowledge/issues/$n.md does not exist on origin/main, so the scope the original audit recorded cannot be reused; worktree and branch removed, no audit left open. Publish that audit's record first (audit-done.ps1)."
        exit 1
    }
    New-Item -ItemType Directory -Force (Get-StagesDir $wt) | Out-Null
    Set-Content -LiteralPath (Join-Path $wt 'artifacts\knowledge\delta-scope.md') -Value $scopeText -Encoding utf8

    $oldDrafts = @(Get-ChildItem -LiteralPath (Join-Path $knowledgeRoot 'remediation') -Filter "$n-*.md" -File -ErrorAction SilentlyContinue)
    if ($oldDrafts.Count -gt 0) {
        Write-Warning "audit-next: $($oldDrafts.Count) draft(s) of the original audit of #$n are in artifacts/knowledge/remediation; a full audit-draft-remediation.ps1 -Prepare removes this issue's drafts and open-remediation.ps1 skips a name listed in opened.csv. Move them away before the remediation stage of the delta."
    }

    $audit = [ordered]@{
        issue      = $n
        worktree   = $wt
        branch     = $branch
        startedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        mode       = 'delta'
        set        = $Delta
    }
    New-Item -ItemType Directory -Force $knowledgeRoot | Out-Null
    $audit | ConvertTo-Json | Set-Content -LiteralPath $currentAuditPath -Encoding utf8

    "Delta audit $Delta of #$n (pending deltas: $($pendingDelta.Count)); scope reused from the original audit: $wt\artifacts\knowledge\delta-scope.md"
    $deltaPipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit') 'pipeline-delta.json'
    $marker = [string]$deltaPipeline.delta.promptMarker
    $nextDelta = Get-NextStage (Get-StagesDir $wt) $wt $deltaPipeline
    if ($null -eq $nextDelta) { 'All stages already complete. Run audit-done.ps1.' }
    else {
        $ruleHint = if ($nextDelta.PSObject.Properties['rule']) { "check only rule ($($nextDelta.rule))" } else { 'delta stage' }
        "Next stage: $($nextDelta.stage) (spawn $($nextDelta.agent) -- issue #$n, worktree $wt, branch $branch; the prompt says `"$marker, $ruleHint`")"
    }
    exit 0
}

# The pending queue entry, computed the same way whether -Issue is given or not: the first
# audit-queue.txt entry (ascending) not already recorded in progress.csv. -Issue is only ever accepted
# when it equals this value AND gh reports the issue CLOSED (review thread T7); there is no override.
$queuePath = Join-Path $knowledgeRoot 'audit-queue.txt'
if (-not (Test-Path -LiteralPath $queuePath)) { Write-Error "audit-next: no queue file at $queuePath."; exit 1 }
$progressPath = Join-Path $knowledgeRoot 'progress.csv'
$done = if (Test-Path -LiteralPath $progressPath) { @(Get-Content -LiteralPath $progressPath | ForEach-Object { ($_ -split ',')[0] }) } else { @() }
$queue = @(Get-Content -LiteralPath $queuePath | Where-Object { $_ -match '\S' } | ForEach-Object { $_.Trim() })
$pending = $queue | Where-Object { $done -notcontains $_ } | Select-Object -First 1
if (-not $pending) { Write-Error 'audit-next: no pending issue in artifacts/knowledge/audit-queue.txt.'; exit 1 }

if ($Issue) {
    if ([string]$Issue -ne [string]$pending) {
        Write-Error "audit-next: -Issue $Issue is not the next pending issue in artifacts/knowledge/audit-queue.txt (next pending: #$pending)."
        exit 1
    }
    $stateOutput = & gh issue view $Issue --repo dlrivada/Encina --json state --jq '.state' 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: gh issue view #$Issue failed: $stateOutput"; exit 1 }
    if (([string]$stateOutput).Trim() -ne 'CLOSED') {
        Write-Error "audit-next: issue #$Issue is not CLOSED (gh reports '$stateOutput')."
        exit 1
    }
    $n = $Issue
}
else {
    $n = [int]$pending
}

$fetchOutput = & git -C $mainRoot fetch origin main 2>&1
if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: git fetch origin main failed: $fetchOutput"; exit 1 }

$wt = Join-Path $worktreesRoot "wia-$n"
$branch = "audit/$n"
$addOutput = & git -C $mainRoot worktree add -b $branch $wt origin/main 2>&1
if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: failed to create worktree $wt on branch $branch`: $addOutput"; exit 1 }

New-Item -ItemType Directory -Force (Get-StagesDir $wt) | Out-Null
New-Item -ItemType Directory -Force $knowledgeRoot | Out-Null

# The pre-draft must exist BEFORE current-audit.json is written (review thread T9): a generator that fails
# or exits without writing predraft/<n>.md must never leave an audit "open" that the next audit-next.ps1
# call refuses to touch. On failure, undo the worktree and branch just created (checking their exit codes
# too) and leave no current-audit.json behind.
$predraftFile = Join-Path $knowledgeRoot "predraft\$n.md"
if (-not (Test-Path -LiteralPath $predraftFile)) {
    & (Join-Path $PSScriptRoot 'qwen-predraft.ps1') -Issue $n
    $predraftExit = $LASTEXITCODE
    if ($predraftExit -ne 0 -or -not (Test-Path -LiteralPath $predraftFile)) {
        $rmOut = & git -C $mainRoot worktree remove $wt --force 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: pre-draft generation failed for #$n (exit $predraftExit) AND cleanup 'git worktree remove $wt' also failed: $rmOut"; exit 1 }
        $brOut = & git -C $mainRoot branch -D $branch 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: pre-draft generation failed for #$n (exit $predraftExit) AND cleanup 'git branch -D $branch' also failed: $brOut"; exit 1 }
        Write-Error "audit-next: pre-draft generation failed for #$n (exit $predraftExit, predraft\$n.md present: $(Test-Path -LiteralPath $predraftFile)); worktree and branch removed, no audit left open."
        exit 1
    }
}

$audit = [ordered]@{
    issue      = $n
    worktree   = $wt
    branch     = $branch
    startedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
}
$audit | ConvertTo-Json | Set-Content -LiteralPath $currentAuditPath -Encoding utf8

$scope = & (Join-Path $PSScriptRoot 'classify-scope.ps1') -Issue $n
"$scope"

$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')
$next = Get-NextStage (Get-StagesDir $wt) $wt $pipeline
if ($null -eq $next) {
    'All stages already complete. Run audit-done.ps1.'
}
elseif ($next.agent -match '^issue-|^audit-|^docs-reviewer$') {
    "Next stage: $($next.stage) (spawn $($next.agent) -- issue #$n, worktree $wt, branch $branch)"
}
else {
    "Next stage: $($next.stage) (run $($next.agent))"
}
