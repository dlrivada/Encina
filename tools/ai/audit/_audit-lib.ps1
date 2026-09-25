# Shared by the tools/ai/audit/ scripts and by .claude/hooks/audit-stage-guard.ps1 (#1345): the SPEC-003
# audit pipeline definition and the paths of the audit's persistent state. The pipeline (stage order, agent,
# minimum model and artifact name) is never hard-coded here or in any caller -- it is read from
# tools/ai/audit/pipeline.json, so a reorder or a renamed artifact is a one-file edit.
#
# Audit data is unversioned, in the MAIN checkout (the repository root, resolved as the parent of git's
# common directory -- never the current working directory, which may be inside a `wia-<n>` worktree):
#   artifacts/knowledge/current-audit.json   the open audit: { issue, worktree, startedUtc }
#   artifacts/knowledge/audit-queue.txt      closed issue numbers, ascending, one per line
#   artifacts/knowledge/progress.csv         history of finished audits
#   artifacts/knowledge/issues|audits|remediation|predraft|stages/<n>/   collected outputs
#
# Stage artifacts live in the audit worktree, on a local branch `audit/<n>` (never pushed), under
# artifacts/knowledge/stages/<file>. A stage counts as done only when its artifact file exists AND a commit
# on that branch carries the trailer `Stage: <name>` (audit-commit-stage.ps1 makes that commit) -- an
# artifact written but not committed is not yet "done" for ordering purposes (the maintainer's two
# additions to #1345 phase A).

# The main checkout: the parent of git's common directory (`git rev-parse --git-common-dir`), which for a
# worktree resolves to the MAIN repository's .git folder regardless of $From's own location or the current
# working directory (#1345 decision: audit data is anchored to the checkout, not to cwd).
function Get-MainRoot([string]$From) {
    if ([string]::IsNullOrWhiteSpace($From)) { $From = (Get-Location).Path }
    $common = & git -C $From rev-parse --git-common-dir 2>$null | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($common)) { throw "Get-MainRoot: '$From' is not inside a git repository." }
    # git prints an ABSOLUTE path here for a worktree, relative (usually '.git') for the main checkout.
    # PowerShell's Join-Path (unlike [IO.Path]::Combine) does not special-case a rooted second argument, so
    # it must be handled explicitly or a worktree's common-dir corrupts into '$From\D:\...\.git'.
    $full = if ([IO.Path]::IsPathRooted($common)) { [IO.Path]::GetFullPath($common) } else { [IO.Path]::GetFullPath((Join-Path $From $common)) }
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

# True when a commit on the branch checked out at $Worktree carries the trailer 'Stage: <StageName>' AND the
# working-tree copy of $ArtifactRelativePath (forward slashes, relative to $Worktree) has no uncommitted
# changes (`git status --porcelain` for that path is empty). The second half matters: --grep alone only
# proves SOME commit once carried that trailer, not that the file on disk right now is what was committed --
# an artifact edited again after audit-commit-stage.ps1 ran, and never re-committed, must NOT read as done.
function Test-StageCommitted([string]$Worktree, [string]$StageName, [string]$ArtifactRelativePath) {
    $commit = & git -C $Worktree log -1 --grep "Stage: $StageName" --fixed-strings --pretty=format:%H 2>$null
    if ([string]::IsNullOrWhiteSpace(($commit | Select-Object -First 1))) { return $false }
    $dirty = & git -C $Worktree status --porcelain -- $ArtifactRelativePath 2>$null
    return [string]::IsNullOrWhiteSpace(($dirty | Select-Object -First 1))
}

# The first stage (in pipeline order) that is not yet done: its artifact is missing under $StagesDir, or it
# is present but not committed (clean, with a 'Stage: <name>' commit) on the audit branch at $Worktree.
# $null when every stage is done.
function Get-NextStage([string]$StagesDir, [string]$Worktree, $Pipeline) {
    foreach ($stage in $Pipeline.stages) {
        $file = Join-Path $StagesDir $stage.artifact
        $relative = "artifacts/knowledge/stages/$($stage.artifact)"
        $done = (Test-Path -LiteralPath $file) -and (Test-StageCommitted $Worktree $stage.stage $relative)
        if (-not $done) { return $stage }
    }
    return $null
}

# True when the verifier stage's artifact (the pipeline.json stage whose agent is 'audit-verifier', never a
# hard-coded 'verification.md' -- a renamed artifact must not silently stop this check from finding it) exists
# and its first line is exactly 'Verdict: FAIL' -- the one condition that allows re-running an earlier stage
# out of the normal fixed order (audit-stage-guard.ps1). $Pipeline is the object Get-Pipeline returns, passed
# in the same style as Get-NextStage above.
function Test-LastVerdictFail([string]$StagesDir, $Pipeline) {
    $verifierStage = $Pipeline.stages | Where-Object { $_.agent -eq 'audit-verifier' } | Select-Object -First 1
    if ($null -eq $verifierStage) { return $false }
    $file = Join-Path $StagesDir $verifierStage.artifact
    if (-not (Test-Path -LiteralPath $file)) { return $false }
    return (Get-Content -LiteralPath $file -TotalCount 1) -eq 'Verdict: FAIL'
}

# stages/lessons.md exists and has no unresolved 'Applied: TODO' line -- each '- <lesson>' bullet
# audit-lessons.ps1 writes is followed by an 'Applied: <status>' line the orchestrator must resolve before the
# lesson counts as applied. Returns '' when valid, or the reason it is not; audit-done.ps1 and
# audit-commit-stage.ps1 -Lessons both call this so the check has one definition (#1345 review).
function Test-LessonsResolved([string]$LessonsFile) {
    if (-not (Test-Path -LiteralPath $LessonsFile)) { return 'missing stages\lessons.md (run audit-lessons.ps1)' }
    $lessonsText = Get-Content -LiteralPath $LessonsFile -Raw
    if ($lessonsText -match 'Applied:\s*TODO') { return 'stages\lessons.md still has an unresolved "Applied: TODO" line' }
    return ''
}

# The '## Findings' or '## Lessons for the pipeline' section of a stage artifact, as raw text; '' when the
# file or the section does not exist. $Heading excludes the leading '##'.
function Get-StageSection([string]$Path, [string]$Heading) {
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $text = Get-Content -LiteralPath $Path -Raw
    # The backtick before '$(?<body>' keeps it a literal regex group, not a PowerShell subexpression: an
    # unescaped '$(' inside a double-quoted string is evaluated by PowerShell itself before the regex ever
    # sees it.
    $pattern = "(?ms)^##\s*$([regex]::Escape($Heading))\s*`$(?<body>.*?)(?=^##\s|\z)"
    $m = [regex]::Match($text, $pattern)
    if (-not $m.Success) { return '' }
    return $m.Groups['body'].Value.Trim()
}

# Splits one stage's '## Findings' section text (as returned by Get-StageSection) into individual findings
# (#1375). The layout every stage agent (issue-auditor, test-auditor, docs-reviewer) writes: one numbered
# paragraph per finding, starting with "N. **Blocker**", "N. **Major**" or "N. **Minor**" followed by an em
# dash or a hyphen and the body; continuation lines belong to that finding until the next numbered finding or
# the next '## ' heading. Returns an array of @{ Stage; Id; Severity; Text } (Id is the finding's own number
# as a string, unique within $Stage). An explicit "- none" section (the convention the stage agents use when
# nothing survives review) yields an empty array. A non-empty section with no recognizable numbered findings
# never yields zero silently: it becomes one finding with Severity 'Unknown' and the whole section as Text, so
# a stage's real findings are never dropped by a formatting drift the parser does not recognize.
function Split-Findings([string]$Stage, [string]$FindingsText) {
    $results = [System.Collections.Generic.List[pscustomobject]]::new()
    $text = if ($null -eq $FindingsText) { '' } else { $FindingsText.Trim() }
    if ([string]::IsNullOrWhiteSpace($text)) { return $results }
    if ($text -match '(?i)^-\s*none\s*$') { return $results }

    $startPattern = '^(?<id>\d+)\.\s+\*\*(?<sev>Blocker|Major|Minor)\*\*\s*[—-]\s*(?<body>.*)$'
    $current = $null
    foreach ($line in ($text -split "`r?`n")) {
        $lineMatch = [regex]::Match($line, $startPattern)
        if ($lineMatch.Success) {
            if ($null -ne $current) { $results.Add([pscustomobject]@{ Stage = $Stage; Id = $current.Id; Severity = $current.Severity; Text = ($current.Lines -join "`n").Trim() }) }
            $current = [pscustomobject]@{ Id = $lineMatch.Groups['id'].Value; Severity = $lineMatch.Groups['sev'].Value; Lines = [System.Collections.Generic.List[string]]::new() }
            $current.Lines.Add($lineMatch.Groups['body'].Value)
        }
        elseif ($line -match '^##\s') {
            # The input is already the extracted Findings section, so a '## ' line here means the section
            # boundary was mis-detected upstream; stop rather than absorb the next section into a finding.
            break
        }
        elseif ($null -ne $current) {
            $current.Lines.Add($line)
        }
    }
    if ($null -ne $current) { $results.Add([pscustomobject]@{ Stage = $Stage; Id = $current.Id; Severity = $current.Severity; Text = ($current.Lines -join "`n").Trim() }) }

    if ($results.Count -eq 0) {
        $results.Add([pscustomobject]@{ Stage = $Stage; Id = '1'; Severity = 'Unknown'; Text = $text })
    }
    return $results
}
