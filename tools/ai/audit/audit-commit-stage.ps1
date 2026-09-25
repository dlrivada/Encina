# tools/ai/audit/audit-commit-stage.ps1 -Stage <name> (#1345)
#
# Commits one stage's artifact (and anything else the stage wrote under artifacts\knowledge\, which is
# gitignored, hence `git add -f`) on the open audit's branch (audit/<n>, created detached-free by
# audit-next.ps1). The commit message is "audit #<n>: <stage> stage" with the trailer "Stage: <stage>" — a
# plain provenance marker for `git log --grep`, not AI attribution.
#
# A stage counts as "done" (audit-stage.ps1 -Next, audit-stage-guard.ps1, audit-done.ps1) only once this
# script has run for it: the artifact file alone is not enough, so a stage agent that writes its file but
# never commits cannot silently let the next stage start early.
#
# Refuses (#1345, the fabrication gap) unless artifacts\knowledge\stages\.authors.json records the stage's
# LAST write as coming from the agent pipeline.json assigns to it: enforce-path-ownership.ps1 is the only
# thing that updates that sidecar, on every Write/Edit tool call it allows, so a missing or mismatched entry
# means the artifact was never actually written by its assigned agent through that tool (or was hand-edited
# by something the sidecar never saw). The 'remediation' stage is exempt: its "agent" is the local-model
# script (audit-draft-remediation.ps1), which writes with Set-Content, never through the Write/Edit tool, so
# the sidecar never gets an entry for it.

param([Parameter(Mandatory)][string]$Stage)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$audit = Get-CurrentAudit $mainRoot
if ($null -eq $audit) { Write-Error 'audit-commit-stage: no open audit (artifacts/knowledge/current-audit.json not found). Run audit-next.ps1 first.'; exit 1 }

$wt = [string]$audit.worktree
$n = [string]$audit.issue
$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')
$stageDef = $pipeline.stages | Where-Object { $_.stage -eq $Stage }
if ($null -eq $stageDef) {
    $names = ($pipeline.stages | ForEach-Object { $_.stage }) -join ', '
    Write-Error "audit-commit-stage: unknown stage '$Stage'. Known stages: $names."
    exit 1
}

$artifactRelative = "artifacts/knowledge/stages/$($stageDef.artifact)"
$artifactFull = Join-Path $wt ($artifactRelative -replace '/', '\')
if (-not (Test-Path -LiteralPath $artifactFull)) {
    Write-Error "audit-commit-stage: stage artifact '$artifactRelative' does not exist in $wt. Write it before committing the stage."
    exit 1
}

$expectedAgent = [string]$stageDef.agent
$knownStageAgents = 'issue-archivist', 'issue-auditor', 'test-auditor', 'audit-verifier', 'docs-reviewer'
if ($expectedAgent -in $knownStageAgents) {
    $authorsPath = Join-Path $wt 'artifacts\knowledge\stages\.authors.json'
    $authorship = $null
    if (Test-Path -LiteralPath $authorsPath) {
        try { $authors = Get-Content -LiteralPath $authorsPath -Raw | ConvertFrom-Json -AsHashtable; $authorship = $authors[$Stage] } catch { $authorship = $null }
    }
    if ($null -eq $authorship -or [string]$authorship.agent -ne $expectedAgent) {
        $found = if ($null -eq $authorship) { 'no recorded author' } else { "recorded author '$($authorship.agent)'" }
        Write-Error "audit-commit-stage: refusing to commit '$Stage' for #${n}: pipeline.json assigns it to $expectedAgent, but artifacts\knowledge\stages\.authors.json has $found. The artifact must be written by $expectedAgent through the Write/Edit tool (enforce-path-ownership.ps1 records authorship there); a hand-edited or fabricated artifact is not accepted (#1345)."
        exit 1
    }
}

& git -C $wt add -f 'artifacts/knowledge' 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Error "audit-commit-stage: 'git add -f artifacts/knowledge' failed in $wt."; exit 1 }

& git -C $wt commit -q -m "audit #$n`: $Stage stage" -m "Stage: $Stage" 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "audit-commit-stage: nothing to commit for stage '$Stage' in #$n (already committed, or the artifact matches what was last committed)."
    exit 1
}

"audit-commit-stage: committed stage '$Stage' for #$n ($artifactRelative)"
