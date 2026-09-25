# Shared by the tools/ai/audit/ scripts and by .claude/hooks/audit-stage-guard.ps1 (#1345): the SPEC-003
# audit pipeline definition and the paths of the audit's persistent state. The pipeline (stage order, agent,
# minimum model and artifact name) is never hard-coded here or in any caller — it is read from
# tools/ai/audit/pipeline.json, so a reorder or a renamed artifact is a one-file edit.
#
# Audit data is unversioned, in the MAIN checkout (the repository root, resolved as the parent of git's
# common directory — never the current working directory, which may be inside a `wia-<n>` worktree):
#   artifacts/knowledge/current-audit.json   the open audit: { issue, worktree, startedUtc }
#   artifacts/knowledge/audit-queue.txt      closed issue numbers, ascending, one per line
#   artifacts/knowledge/progress.csv         history of finished audits
#   artifacts/knowledge/issues|audits|remediation|predraft|stages/<n>/   collected outputs
#
# Stage artifacts live in the audit worktree, on a local branch `audit/<n>` (never pushed), under
# artifacts/knowledge/stages/<file>. A stage counts as done only when its artifact file exists AND a commit
# on that branch carries the trailer `Stage: <name>` (audit-commit-stage.ps1 makes that commit) — an
# artifact written but not committed is not yet "done" for ordering purposes (the maintainer's two
# additions to #1345 phase A).

# The main checkout: the parent of git's common directory (`git rev-parse --git-common-dir`), which for a
# worktree resolves to the MAIN repository's .git folder regardless of $From's own location or the current
# working directory (#1345 decision: audit data is anchored to the checkout, not to cwd).
function Get-MainRoot([string]$From) {
    if ([string]::IsNullOrWhiteSpace($From)) { $From = (Get-Location).Path }
    $common = & git -C $From rev-parse --git-common-dir 2>$null | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($common)) { throw "Get-MainRoot: '$From' is not inside a git repository." }
    $full = [IO.Path]::GetFullPath((Join-Path $From $common))
    return (Split-Path -Parent $full)
}

function Get-KnowledgeRoot([string]$MainRoot) { Join-Path $MainRoot 'artifacts\knowledge' }

function Get-CurrentAuditPath([string]$MainRoot) { Join-Path (Get-KnowledgeRoot $MainRoot) 'current-audit.json' }

function Get-CurrentAudit([string]$MainRoot) {
    $p = Get-CurrentAuditPath $MainRoot
    if (-not (Test-Path -LiteralPath $p)) { return $null }
    return Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
}

function Get-StagesDir([string]$Worktree) { Join-Path $Worktree 'artifacts\knowledge\stages' }

# tools/ai/audit/pipeline.json read from $ToolsAuditDir (normally the audit worktree's own copy, so an audit
# keeps the pipeline version it started with even if main later reorders it; callers pass the worktree's
# tools\ai\audit, or their own $PSScriptRoot when no worktree is known yet, e.g. audit-next.ps1 before the
# worktree exists).
function Get-Pipeline([string]$ToolsAuditDir) {
    $p = Join-Path $ToolsAuditDir 'pipeline.json'
    if (-not (Test-Path -LiteralPath $p)) { throw "Get-Pipeline: pipeline.json not found at '$p'." }
    return Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
}

# True when a commit on the branch checked out at $Worktree carries the trailer 'Stage: <StageName>'
# (audit-commit-stage.ps1's own commit message format).
function Test-StageCommitted([string]$Worktree, [string]$StageName) {
    $out = & git -C $Worktree log -1 --grep "Stage: $StageName" --pretty=format:%H 2>$null
    return -not [string]::IsNullOrWhiteSpace(($out | Select-Object -First 1))
}

# The first stage (in pipeline order) that is not yet done: its artifact is missing under $StagesDir, or it
# is present but not yet committed on the audit branch at $Worktree. $null when every stage is done.
function Get-NextStage([string]$StagesDir, [string]$Worktree, $Pipeline) {
    foreach ($stage in $Pipeline.stages) {
        $file = Join-Path $StagesDir $stage.artifact
        $done = (Test-Path -LiteralPath $file) -and (Test-StageCommitted $Worktree $stage.stage)
        if (-not $done) { return $stage }
    }
    return $null
}

# True when stages/verification.md exists and its first line is exactly 'Verdict: FAIL' — the one condition
# that allows re-running an earlier stage out of the normal fixed order (audit-stage-guard.ps1).
function Test-LastVerdictFail([string]$StagesDir) {
    $file = Join-Path $StagesDir 'verification.md'
    if (-not (Test-Path -LiteralPath $file)) { return $false }
    return (Get-Content -LiteralPath $file -TotalCount 1) -eq 'Verdict: FAIL'
}

# The '## Findings' or '## Lessons for the pipeline' section of a stage artifact, as raw text; '' when the
# file or the section does not exist. $Heading excludes the leading '##'.
function Get-StageSection([string]$Path, [string]$Heading) {
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $text = Get-Content -LiteralPath $Path -Raw
    $pattern = "(?ms)^##\s*$([regex]::Escape($Heading))\s*$(?<body>.*?)(?=^##\s|\z)"
    $m = [regex]::Match($text, $pattern)
    if (-not $m.Success) { return '' }
    return $m.Groups['body'].Value.Trim()
}
