# tools/ai/audit/audit-next.ps1 [-Issue n] (#1345)
#
# Starts one SPEC-003 audit: the queue discipline that replaces batching issues or running two audits at
# once. Refuses when an audit is already open, or when a stray `wia-*` worktree exists even though
# current-audit.json is missing (a previous audit that was not closed with audit-done.ps1). With no -Issue,
# takes the first queue entry (artifacts/knowledge/audit-queue.txt, ascending) not already recorded in
# artifacts/knowledge/progress.csv.
#
# Creates .claude/worktrees/wia-<n> on a NEW LOCAL branch audit/<n> based on origin/main (never a detached
# HEAD, and the branch is never pushed: audit-done.ps1 deletes it after collecting), writes
# artifacts/knowledge/current-audit.json, and makes sure a local-model pre-draft
# (artifacts/knowledge/predraft/<n>.md) exists before the archivist stage starts.

param([int]$Issue)

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

if ($Issue) {
    $n = $Issue
}
else {
    $queuePath = Join-Path $knowledgeRoot 'audit-queue.txt'
    if (-not (Test-Path -LiteralPath $queuePath)) { Write-Error "audit-next: no queue file at $queuePath and no -Issue given."; exit 1 }
    $progressPath = Join-Path $knowledgeRoot 'progress.csv'
    $done = if (Test-Path -LiteralPath $progressPath) { @(Get-Content -LiteralPath $progressPath | ForEach-Object { ($_ -split ',')[0] }) } else { @() }
    $queue = @(Get-Content -LiteralPath $queuePath | Where-Object { $_ -match '\S' } | ForEach-Object { $_.Trim() })
    $pending = $queue | Where-Object { $done -notcontains $_ } | Select-Object -First 1
    if (-not $pending) { Write-Error 'audit-next: no pending issue in artifacts/knowledge/audit-queue.txt.'; exit 1 }
    $n = [int]$pending
}

& git -C $mainRoot fetch origin main 2>&1 | Out-Null

$wt = Join-Path $worktreesRoot "wia-$n"
$branch = "audit/$n"
& git -C $mainRoot worktree add -b $branch $wt origin/main 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: failed to create worktree $wt on branch $branch."; exit 1 }

New-Item -ItemType Directory -Force (Get-StagesDir $wt) | Out-Null
New-Item -ItemType Directory -Force $knowledgeRoot | Out-Null

$audit = [ordered]@{
    issue      = $n
    worktree   = $wt
    branch     = $branch
    startedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
}
$audit | ConvertTo-Json | Set-Content -LiteralPath $currentAuditPath -Encoding utf8

$predraftFile = Join-Path $knowledgeRoot "predraft\$n.md"
if (-not (Test-Path -LiteralPath $predraftFile)) {
    & (Join-Path $PSScriptRoot 'qwen-predraft.ps1') -Issue $n
}

$scope = & (Join-Path $PSScriptRoot 'classify-scope.ps1') -Issue $n
"$scope"

$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')
$next = Get-NextStage (Get-StagesDir $wt) $wt $pipeline
if ($null -eq $next) {
    'All stages already complete. Run audit-done.ps1.'
}
elseif ($next.agent -match '^issue-|^audit-|^docs-reviewer$') {
    "Next stage: $($next.stage) (spawn $($next.agent) — issue #$n, worktree $wt, branch $branch)"
}
else {
    "Next stage: $($next.stage) (run $($next.agent))"
}
