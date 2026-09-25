# tools/ai/audit/audit-stage.ps1 -Next (#1345)
#
# Prints the next stage due for the open audit: the first stage of tools/ai/audit/pipeline.json whose
# artifact is missing, or present but not yet committed with audit-commit-stage.ps1 (Get-NextStage,
# _audit-lib.ps1). Used by the orchestrator to decide which specialist to spawn next, and by
# .claude/hooks/audit-stage-guard.ps1 to enforce the same order.

param([switch]$Next)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$audit = Get-CurrentAudit $mainRoot
if ($null -eq $audit) { Write-Error 'audit-stage: no open audit (artifacts/knowledge/current-audit.json not found). Run audit-next.ps1 first.'; exit 1 }

if (-not $Next) { 'Usage: audit-stage.ps1 -Next'; exit 0 }

$wt = [string]$audit.worktree
$n = [string]$audit.issue
$stagesDir = Get-StagesDir $wt
$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')
$next = Get-NextStage $stagesDir $wt $pipeline

if ($null -eq $next) {
    "All stages complete for #$n. Run audit-done.ps1."
    exit 0
}
if ($next.agent -match '^issue-|^audit-|^docs-reviewer$') {
    "Next stage: $($next.stage) (spawn $($next.agent) on issue #$n, worktree $wt)"
}
else {
    "Next stage: $($next.stage) (run $($next.agent) for issue #$n)"
}
