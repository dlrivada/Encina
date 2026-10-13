# tools/ai/audit/audit-done.ps1 (#1345; replaces the old artifacts/knowledge/collect.ps1)
#
# The only way to close a SPEC-003 audit. Refuses, listing every reason, when:
#   - any pipeline stage's artifact is missing, or present but not committed with audit-commit-stage.ps1;
#   - stages/verification.md does not start with the pipeline's verdict line ('Verdict: PASS');
#   - (#1555) an earlier stage's artifact (archivist, code, tests, docs, remediation) was committed AFTER the
#     verifier stage's own last commit -- that PASS verdict is stale, because the verifier never inspected the
#     re-committed content. Detected purely from git history (Get-StaleStageAfterVerification in
#     _audit-lib.ps1, shared with .claude/hooks/audit-stage-guard.ps1, which is what lets audit-verifier be
#     re-spawned in that case): no marker file, no new state;
#   - stages/lessons.md is missing, or still has an unresolved 'Applied: TODO' line;
#   - the worktree's knowledge-records.cs --check fails against artifacts/knowledge/issues (#1457: this also
#     writes artifacts/knowledge/stages/.rerun-archivist, letting audit-stage-guard.ps1 allow a re-spawn of
#     issue-archivist to fix the record even though every stage is already committed).
#
# Otherwise (#1735) it PUBLISHES the audit first: in a temporary worktree it creates the branch
# knowledge/audit-<n> from origin/main, copies the record to docs/knowledge/issues/<n>.md (replacing a record
# already there), the audit result to docs/knowledge/audits/issue-<n>.md (generated from the verification
# stage when the pipeline wrote none) and the stage files to docs/knowledge/audits/<n>/stages/, validates the
# whole docs/knowledge tree with knowledge-records.cs --check, commits "docs(knowledge): SPEC-003 audit of #<n>",
# pushes and opens a pull request with "Refs #1345" (Publish-AuditKnowledge in _audit-lib.ps1). When publishing fails, NOTHING else
# happens: the audit worktree, the audit/<n> branch and its open-audit state file stay, and the script can be re-run.
#
# Only after the pull request exists it copies the collected records, audit results, remediation drafts, stage
# artifacts and ledger lines into the main artifacts/knowledge (still git-ignored), appends
# artifacts/knowledge/progress.csv, appends any role-tagged lesson (stages/lessons.md 'Applied: role:<agent>'
# line) to .claude/agents/lessons/<agent>.md (#1345), removes the wia-<n> worktree AND its audit/<n> branch,
# and deletes the audit's state file artifacts/knowledge/open-audits/<n>.json.
#
# #2234: several audits may be open at once and close in any order. -Issue <n> names the audit to close
# (optional from its wia-<n> worktree or with one open audit; Resolve-OpenAudit, _audit-lib.ps1). Closing touches
# only that audit: its worktree, branch and state file, one progress.csv row (the queue reads progress.csv as a
# set, so the order of the rows does not matter) and its own lessons; the other open audits keep running.
#
# -NoPublish: runs every check, builds and commits the publication branch locally, prints the push and
# 'gh pr create' commands it would run, and stops. It does not push, open a pull request or close the audit
# (the audit worktree, branch and state file stay).

param([switch]$NoPublish, [int]$Issue)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$knowledgeRoot = Get-KnowledgeRoot $mainRoot
try { $audit = Resolve-OpenAudit $mainRoot $Issue $PSScriptRoot }
catch { Write-Error "audit-done: $($_.Exception.Message)"; exit 1 }

$n = [string]$audit.issue
$wt = [string]$audit.worktree
$branch = [string]$audit.branch
$stagesDir = Get-StagesDir $wt
$pipeline = Get-AuditPipeline $audit
$isDelta = Test-DeltaAudit $audit
$deltaSet = if ($isDelta) { [string]$audit.set } else { '' }
$deltaFolder = if ($isDelta) { [string]$pipeline.delta.folder } else { '' }

$reasons = [System.Collections.Generic.List[string]]::new()

foreach ($stage in $pipeline.stages) {
    $file = Join-Path $stagesDir $stage.artifact
    if (-not (Test-Path -LiteralPath $file)) {
        $reasons.Add("missing stages\$($stage.artifact) (stage: $($stage.stage))")
        continue
    }
    $relative = "artifacts/knowledge/stages/$($stage.artifact)"
    if (-not (Test-StageCommitted $wt $stage.stage $relative)) {
        $reasons.Add("stages\$($stage.artifact) exists but is not committed clean with audit-commit-stage.ps1 -Stage $($stage.stage) (no 'Stage: $($stage.stage)' commit on $branch, or the file has uncommitted changes since)")
    }
}

$verificationFile = Join-Path $stagesDir 'verification.md'
if (Test-Path -LiteralPath $verificationFile) {
    $firstLine = Get-Content -LiteralPath $verificationFile -TotalCount 1
    if ($firstLine -ne $pipeline.verdictLine) {
        $reasons.Add("stages\verification.md does not start with '$($pipeline.verdictLine)' (found: '$firstLine')")
    }
}

# #1555: a stage re-committed after the verifier's own last commit makes that verdict stale -- the verifier
# never inspected the new content. Refuse to close until audit-verifier has re-run and produced a fresh verdict.
$staleStage = Get-StaleStageAfterVerification $wt $pipeline
if ($staleStage) {
    $verifierStage = $pipeline.stages | Where-Object { $_.agent -eq 'audit-verifier' } | Select-Object -First 1
    $reasons.Add("stages\$($verifierStage.artifact) is stale: stages\$($staleStage.artifact) (the '$($staleStage.stage)' stage) was committed after the last verification commit; re-run audit-verifier before closing the audit (#1555)")
}

$lessonsFile = Join-Path $stagesDir 'lessons.md'
$lessonsReason = Test-LessonsResolved $lessonsFile
if ($lessonsReason) { $reasons.Add($lessonsReason) }

$recordsDir = Join-Path $wt 'artifacts\knowledge\issues'
$knowledgeScript = Join-Path $wt '.github\scripts\knowledge-records.cs'
$rerunArchivistMarker = Join-Path $stagesDir '.rerun-archivist'
if ($isDelta) {
    # #1763: a delta audit writes no knowledge record (the original record stays as published); the full
    # docs/knowledge check runs on the publication checkout.
}
elseif (Test-Path -LiteralPath $knowledgeScript) {
    # --skip-audit-links: the audit result is not in docs/knowledge yet; the full check runs on the publication checkout.
    $checkOutput = & dotnet run --file $knowledgeScript -- --check --dir $recordsDir --skip-audit-links 2>&1
    if ($LASTEXITCODE -ne 0) {
        $checkText = ($checkOutput -join "`n")
        $reasons.Add("knowledge-records --check failed:`n$checkText")
        # #1457: the knowledge record fails validation but every stage is already committed, so
        # audit-stage-guard.ps1's normal fixed-order rule has nothing left to re-run -- without this marker
        # the audit deadlocks (audit-done refuses, the guard refuses to re-spawn issue-archivist). Writing it
        # here is the one authorized way to unblock the FAIL loop for a record-format-only problem, without
        # requiring a full verifier re-run (audit-stage-guard.ps1 checks it in addition to a FAIL verdict).
        $timestamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Set-Content -LiteralPath $rerunArchivistMarker -Value "$checkText`n`nUTC: $timestamp`n"
    }
}
else {
    $reasons.Add("$knowledgeScript not found in the audit worktree; cannot validate the knowledge record.")
}

if ($reasons.Count -gt 0) {
    $bulleted = ($reasons | ForEach-Object { "- $_" }) -join "`n"
    $markerHint = if (Test-Path -LiteralPath $rerunArchivistMarker) {
        "`nThe knowledge record failed 'knowledge-records --check'; artifacts\knowledge\stages\.rerun-archivist was written so audit-stage-guard.ps1 allows re-spawning issue-archivist to fix it (#1457). Once the archivist stage is re-committed and the record passes the check, run audit-done.ps1 again -- no verifier re-run is required when only the record format changed."
    }
    else { '' }
    Write-Error "audit-done: audit for #$n is incomplete:`n$bulleted$markerHint"
    exit 1
}

$remediationDrafts = @((Join-Path $knowledgeRoot 'remediation'), (Join-Path $wt 'artifacts\knowledge\remediation'), (Join-Path $wt 'artifacts\issues'))
$publish = Publish-AuditKnowledge -Issue ([int]$n) -MainRoot $mainRoot -AuditWorktree $wt -StagesDir $stagesDir -Pipeline $pipeline `
    -DraftDirs $remediationDrafts -OpenedCsv (Join-Path $knowledgeRoot 'remediation\opened.csv') -NoPublish:$NoPublish `
    -DeltaFolder $deltaFolder -DeltaSet $deltaSet
if (-not $publish.Ok) {
    Write-Error "audit-done: publishing the audit of #$n failed; the audit worktree, branch $branch and open-audits/$n.json are kept, nothing else was changed:`n$($publish.Message)"
    exit 1
}
if ($NoPublish) {
    "audit-done -NoPublish: publication branch $($publish.Branch) prepared locally; the audit of #$n stays open. Commands that would run:"
    $publish.Planned | ForEach-Object { "  $_" }
    exit 0
}
"audit-done: published $($publish.Branch): $($publish.PrUrl)"

foreach ($sub in 'issues', 'audits', 'remediation') { New-Item -ItemType Directory -Force (Join-Path $knowledgeRoot $sub) | Out-Null }
$src = Join-Path $wt 'artifacts\knowledge'
Get-ChildItem (Join-Path $src 'issues') -File -ErrorAction SilentlyContinue | Copy-Item -Destination (Join-Path $knowledgeRoot 'issues') -Force
Get-ChildItem (Join-Path $src 'audits') -File -ErrorAction SilentlyContinue | Copy-Item -Destination (Join-Path $knowledgeRoot 'audits') -Force
Get-ChildItem (Join-Path $src 'remediation') -File -ErrorAction SilentlyContinue | Copy-Item -Destination (Join-Path $knowledgeRoot 'remediation') -Force

# A stage may have written a draft to the issue-worker's default follow-up folder instead of
# artifacts/knowledge/remediation; accept it too, with a warning (old collect.ps1 behaviour).
$strayDrafts = @(Get-ChildItem (Join-Path $wt 'artifacts\issues') -Filter "$n-*.md" -File -ErrorAction SilentlyContinue)
if ($strayDrafts.Count -gt 0) {
    Write-Warning "audit-done: found remediation draft(s) under artifacts\issues instead of artifacts\knowledge\remediation: $(($strayDrafts | ForEach-Object { $_.Name }) -join ', ')"
    $strayDrafts | Copy-Item -Destination (Join-Path $knowledgeRoot 'remediation') -Force
}

# A delta audit archives next to, never over, the original audit's stages (stages\<n>-<delta folder>).
$stagesDest = if ($isDelta) { Join-Path $knowledgeRoot "stages\$n-$deltaFolder" } else { Join-Path $knowledgeRoot "stages\$n" }
New-Item -ItemType Directory -Force $stagesDest | Out-Null
Copy-Item (Join-Path $stagesDir '*') -Destination $stagesDest -Recurse -Force

# #1345: role memory. A lessons.md item's 'Applied:' line naming 'role:<agent>' is a lesson for that agent's
# own memory, not a one-off orchestrator fix; append it to .claude/agents/lessons/<agent>.md (main checkout)
# with today's date and this issue number, so the agent reads it at the start of its next spawn.
$appliedRoles = 0
if (Test-Path -LiteralPath $lessonsFile) {
    $lessonsLines = @(Get-Content -LiteralPath $lessonsFile)
    $today = [DateTime]::UtcNow.ToString('yyyy-MM-dd')
    for ($i = 0; $i -lt $lessonsLines.Count - 1; $i++) {
        if ($lessonsLines[$i] -notmatch '^-\s+(?<text>.+)$') { continue }
        $text = $Matches['text']
        if ($lessonsLines[$i + 1] -notmatch '^Applied:\s*role:(?<agent>[\w-]+)\s*(?<detail>.*)$') { continue }
        $agentName = $Matches['agent']
        $detail = $Matches['detail'].Trim()
        $roleLessonsFile = Join-Path $mainRoot ".claude\agents\lessons\$agentName.md"
        if (-not (Test-Path -LiteralPath $roleLessonsFile)) {
            Write-Warning "audit-done: stages\lessons.md names 'role:$agentName' but .claude\agents\lessons\$agentName.md does not exist; the lesson '$text' was NOT recorded anywhere. Check the agent name for a typo."
            continue
        }
        $suffix = if ($detail) { " ($detail)" } else { '' }
        Add-Content -LiteralPath $roleLessonsFile -Value "- ($today, #$n) $text$suffix"
        $appliedRoles++
    }
}

$ledger = Join-Path $wt 'artifacts\agent-usage\ledger.csv'
if (Test-Path -LiteralPath $ledger) { Get-Content -LiteralPath $ledger | Select-Object -Skip 1 | Add-Content (Join-Path $knowledgeRoot 'agent-ledger.csv') }

$remPrefix = if ($isDelta) { "$n-$deltaFolder" } else { "$n" }
$remCount = @(Get-ChildItem (Join-Path $knowledgeRoot 'remediation') -Filter "$remPrefix-*.md" -ErrorAction SilentlyContinue).Count
if ($isDelta) {
    # The delta set has its own progress file; progress.csv (the original audits) is never touched.
    Add-Content (Get-DeltaProgressPath $knowledgeRoot $deltaSet) "$n,done,$remCount,$($publish.PrUrl)"
}
else {
    Add-Content (Join-Path $knowledgeRoot 'progress.csv') "$n,done,,,,$remCount,`"`""
}

$rmOut = & git -C $mainRoot worktree remove $wt --force 2>&1
if ($LASTEXITCODE -ne 0) { Write-Error "audit-done: git worktree remove $wt failed: $rmOut"; exit 1 }
if ($branch) {
    $brOut = & git -C $mainRoot branch -D $branch 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "audit-done: git branch -D $branch failed: $brOut"; exit 1 }
}
$stateLock = Enter-OpenAuditsLock $mainRoot
try { Remove-OpenAudit $mainRoot ([int]$n) }
finally { Exit-OpenAuditsLock $stateLock }

"audit-done: closed audit for #$n (remediation drafts: $remCount; role lessons applied: $appliedRoles; stages archived to artifacts\knowledge\stages\$n; branch $branch removed; pull request: $($publish.PrUrl))"
