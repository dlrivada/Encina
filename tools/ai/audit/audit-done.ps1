# tools/ai/audit/audit-done.ps1 (#1345; replaces the old artifacts/knowledge/collect.ps1)
#
# The only way to close a SPEC-003 audit. Refuses, listing every reason, when:
#   - any pipeline stage's artifact is missing, or present but not committed with audit-commit-stage.ps1;
#   - stages/verification.md does not start with the pipeline's verdict line ('Verdict: PASS');
#   - stages/lessons.md is missing, or still has an unresolved 'Applied: TODO' line;
#   - the worktree's knowledge-records.cs --check fails against artifacts/knowledge/issues.
#
# Otherwise it copies the collected records, audit results, remediation drafts, stage artifacts and ledger
# lines into the main artifacts/knowledge, appends artifacts/knowledge/progress.csv, appends any role-tagged
# lesson (stages/lessons.md 'Applied: role:<agent>' line) to .claude/agents/lessons/<agent>.md (#1345), removes
# the wia-<n> worktree AND its audit/<n> branch, and deletes current-audit.json.

param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$knowledgeRoot = Get-KnowledgeRoot $mainRoot
$currentAuditPath = Get-CurrentAuditPath $mainRoot
$audit = Get-CurrentAudit $mainRoot
if ($null -eq $audit) { Write-Error 'audit-done: no open audit (artifacts/knowledge/current-audit.json not found).'; exit 1 }

$n = [string]$audit.issue
$wt = [string]$audit.worktree
$branch = [string]$audit.branch
$stagesDir = Get-StagesDir $wt
$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')

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

$lessonsFile = Join-Path $stagesDir 'lessons.md'
if (-not (Test-Path -LiteralPath $lessonsFile)) {
    $reasons.Add('missing stages\lessons.md (run audit-lessons.ps1)')
}
else {
    $lessonsText = Get-Content -LiteralPath $lessonsFile -Raw
    if ($lessonsText -match 'Applied:\s*TODO') { $reasons.Add('stages\lessons.md still has an unresolved "Applied: TODO" line') }
}

$recordsDir = Join-Path $wt 'artifacts\knowledge\issues'
$knowledgeScript = Join-Path $wt '.github\scripts\knowledge-records.cs'
if (Test-Path -LiteralPath $knowledgeScript) {
    $checkOutput = & dotnet run --file $knowledgeScript -- --check --dir $recordsDir 2>&1
    if ($LASTEXITCODE -ne 0) { $reasons.Add("knowledge-records --check failed:`n$($checkOutput -join "`n")") }
}
else {
    $reasons.Add("$knowledgeScript not found in the audit worktree; cannot validate the knowledge record.")
}

if ($reasons.Count -gt 0) {
    $bulleted = ($reasons | ForEach-Object { "- $_" }) -join "`n"
    Write-Error "audit-done: audit for #$n is incomplete:`n$bulleted"
    exit 1
}

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

$stagesDest = Join-Path $knowledgeRoot "stages\$n"
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

$remCount = @(Get-ChildItem (Join-Path $knowledgeRoot 'remediation') -Filter "$n-*.md" -ErrorAction SilentlyContinue).Count
Add-Content (Join-Path $knowledgeRoot 'progress.csv') "$n,done,,,,$remCount,`"`""

& git -C $mainRoot worktree remove $wt --force 2>&1 | Out-Null
if ($branch) { & git -C $mainRoot branch -D $branch 2>&1 | Out-Null }
Remove-Item -LiteralPath $currentAuditPath -Force

"audit-done: closed audit for #$n (remediation drafts: $remCount; role lessons applied: $appliedRoles; stages archived to artifacts\knowledge\stages\$n; branch $branch removed)"
