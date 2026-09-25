# tools/ai/audit/audit-commit-stage.ps1 -Stage <name> | -Lessons (#1345)
#
# Commits one stage's artifact (and anything else the stage wrote under artifacts\knowledge\, which is
# gitignored, hence `git add -f`) on the open audit's branch (audit/<n>, created detached-free by
# audit-next.ps1). The commit message is "audit #<n>: <stage> stage" with the trailer "Stage: <stage>" -- a
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
#
# -Lessons commits stages\lessons.md instead of a pipeline.json stage: it is not one of pipeline.json's
# stages (the orchestrator hand-edits it to resolve each 'Applied: TODO'), so the -Stage path above does not
# apply to it, and enforce-path-ownership.ps1 blocks a bare `git commit` for every caller inside an open
# audit's worktree (review thread T2): this is the one authorized way to commit it. It validates the same
# lessons-resolved check audit-done.ps1 performs (Test-LessonsResolved, shared in _audit-lib.ps1) before
# committing "audit #<n>: lessons" with the trailer "Stage: lessons", and refuses anything else.
#
# Every git call captures its output into a variable and reads $LASTEXITCODE on the very next line, never
# piping straight into a cmdlet (`| Out-Null`): PowerShell updates $LASTEXITCODE only once the native process
# has exited, and a downstream cmdlet can race that, so a piped check can read a stale exit code (the same
# gotcha require-specialists.ps1 documents for its own git calls). Test-Hooks.ps1's audit-commit-stage.ps1
# cases caught this intermittently, since the old `2>&1 | Out-Null; if ($LASTEXITCODE -ne 0)` form sometimes
# reported "nothing to commit" for a commit that actually raced past the check.

param(
    [Parameter(Mandatory, ParameterSetName = 'Stage')][string]$Stage,
    [Parameter(Mandatory, ParameterSetName = 'Lessons')][switch]$Lessons
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$audit = Get-CurrentAudit $mainRoot
if ($null -eq $audit) { Write-Error 'audit-commit-stage: no open audit (artifacts/knowledge/current-audit.json not found). Run audit-next.ps1 first.'; exit 1 }

$wt = [string]$audit.worktree
$n = [string]$audit.issue

if ($Lessons) {
    $lessonsFile = Join-Path (Get-StagesDir $wt) 'lessons.md'
    $reason = Test-LessonsResolved $lessonsFile
    if ($reason) { Write-Error "audit-commit-stage: $reason"; exit 1 }

    $addOutput = & git -C $wt add -f 'artifacts/knowledge/stages/lessons.md' 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "audit-commit-stage: 'git add -f artifacts/knowledge/stages/lessons.md' failed in $wt`: $addOutput"; exit 1 }

    $commitOutput = & git -C $wt commit -q -m "audit #$n`: lessons" -m 'Stage: lessons' 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "audit-commit-stage: nothing to commit for lessons in #$n (already committed, or lessons.md matches what was last committed); git said: $commitOutput"
        exit 1
    }

    "audit-commit-stage: committed lessons for #$n"
    exit 0
}

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
    # #1374: an unreadable sidecar (corrupted by a lost race between the two concurrent hook instances, before
    # the mutex fix) is a DIFFERENT failure than "no recorded author" -- the artifact may well have been
    # written by the right agent, but the sidecar cannot prove it. Reporting the parse error, never masking it
    # behind "no recorded author", is what let #1374 be diagnosed instead of silently blocking the audit.
    $unreadableReason = $null
    if (Test-Path -LiteralPath $authorsPath) {
        try { $authors = Get-Content -LiteralPath $authorsPath -Raw | ConvertFrom-Json -AsHashtable; $authorship = $authors[$Stage] }
        catch { $unreadableReason = $_.Exception.Message }
    }
    if ($unreadableReason) {
        Write-Error "audit-commit-stage: refusing to commit '$Stage' for #${n}: artifacts\knowledge\stages\.authors.json is unreadable: $unreadableReason; run 'pwsh -NoProfile -File tools/ai/audit/audit-stage.ps1 -RepairAuthors' from the main checkout, then have $expectedAgent re-write the '$Stage' stage artifact so it is recorded again (#1374)."
        exit 1
    }
    if ($null -eq $authorship -or [string]$authorship.agent -ne $expectedAgent) {
        $found = if ($null -eq $authorship) { 'no recorded author' } else { "recorded author '$($authorship.agent)'" }
        Write-Error "audit-commit-stage: refusing to commit '$Stage' for #${n}: pipeline.json assigns it to $expectedAgent, but artifacts\knowledge\stages\.authors.json has $found. The artifact must be written by $expectedAgent through the Write/Edit tool (enforce-path-ownership.ps1 records authorship there); a hand-edited or fabricated artifact is not accepted (#1345)."
        exit 1
    }
}

$addOutput = & git -C $wt add -f 'artifacts/knowledge' 2>&1
if ($LASTEXITCODE -ne 0) { Write-Error "audit-commit-stage: 'git add -f artifacts/knowledge' failed in $wt.: $addOutput"; exit 1 }

$commitOutput = & git -C $wt commit -q -m "audit #$n`: $Stage stage" -m "Stage: $Stage" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "audit-commit-stage: nothing to commit for stage '$Stage' in #$n (already committed, or the artifact matches what was last committed); git said: $commitOutput"
    exit 1
}

"audit-commit-stage: committed stage '$Stage' for #$n ($artifactRelative)"
