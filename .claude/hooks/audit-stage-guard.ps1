# PreToolUse hook (Agent|Task), wired unconditionally in the project .claude/settings.json (#1345): enforces
# the SPEC-003 audit pipeline discipline — one audit open at a time, one issue per stage spawn, stages run
# in the fixed order of tools/ai/audit/pipeline.json (never hard-coded here), no model below Sonnet, and the
# old free-form coordinator path is closed.
#
# Applies when the spawned subagent_type is one of the audit-stage agents (issue-archivist, issue-auditor,
# test-auditor, audit-verifier), or docs-reviewer when its prompt names an audit worktree (wia-<n>): a
# docs-reviewer spawned by docs-writer for an ordinary documentation self-review, with no audit worktree
# named, is not an audit stage and is left alone.
#
# Denies when:
#   - no audit is open (artifacts/knowledge/current-audit.json, resolved from the MAIN checkout, is missing);
#   - the prompt does not name both the open issue (#<n>) and its worktree (wia-<n>);
#   - the prompt also names a DIFFERENT wia-<m> (batching issues into one spawn, #1345's founding failure);
#   - the requested model is one of pipeline.json's forbiddenModels (haiku);
#   - the subagent is not the stage tools/ai/audit/pipeline.json (via Get-NextStage) reports as next —
#     except: after the verifier's last verdict was FAIL, any stage may be re-run out of order.
#
# Also denies any issue-worker or general-purpose spawn whose prompt mentions "SPEC-003 audit": the old
# coordinator-does-everything path (#1345) is closed; the pipeline's own scripts and stage agents are the
# only way to run the audit now.
#
# pipeline.json is read from the OPEN AUDIT'S OWN worktree (not this hook's checkout), so a fixture or a
# later reorder of the file changes the stage this hook expects without redeploying the hook.
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call.

param()

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    if ([string]$payload.tool_name -notin 'Agent', 'Task') { exit 0 }

    $subagent = [string]$payload.tool_input.subagent_type
    $prompt = [string]$payload.tool_input.prompt

    if ($subagent -in 'issue-worker', 'general-purpose' -and $prompt -match 'SPEC-003 audit') {
        [Console]::Error.WriteLine("Blocked: the SPEC-003 audit no longer runs through a $subagent coordinator (#1345). Use tools/ai/audit/audit-next.ps1 and the fixed stage agents (issue-archivist, issue-auditor, test-auditor, docs-reviewer, audit-verifier) instead.")
        exit 2
    }

    $stageAgents = 'issue-archivist', 'issue-auditor', 'test-auditor', 'audit-verifier'
    # Requiring both 'wia-<n>' AND the word 'audit' keeps an ordinary docs-writer self-review — whose prompt
    # might legitimately mention a 'wia-<n>' worktree name for unrelated reasons, e.g. this very pipeline's
    # own documentation — from being misread as an audit-stage spawn and denied for having no open audit.
    $isDocsStage = $subagent -eq 'docs-reviewer' -and $prompt -match 'wia-\d+' -and $prompt -match '(?i)\baudit\b'
    if ($subagent -notin $stageAgents -and -not $isDocsStage) { exit 0 }

    $projectDir = [string]$env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($projectDir)) { exit 0 }
    $mainRoot = $projectDir
    $marker = [IO.Path]::DirectorySeparatorChar + '.claude' + [IO.Path]::DirectorySeparatorChar + 'worktrees' + [IO.Path]::DirectorySeparatorChar
    $at = $mainRoot.IndexOf($marker, [StringComparison]::OrdinalIgnoreCase)
    if ($at -ge 0) { $mainRoot = $mainRoot.Substring(0, $at) }
    $currentAuditPath = Join-Path $mainRoot 'artifacts\knowledge\current-audit.json'

    if (-not (Test-Path -LiteralPath $currentAuditPath)) {
        [Console]::Error.WriteLine('Blocked: no open SPEC-003 audit (artifacts/knowledge/current-audit.json not found). Run tools/ai/audit/audit-next.ps1 first (#1345).')
        exit 2
    }
    $audit = Get-Content -LiteralPath $currentAuditPath -Raw | ConvertFrom-Json
    $n = [string]$audit.issue
    $wt = [string]$audit.worktree

    $mentionsIssue = $prompt -match "#$n(\D|$)"
    $mentionsWorktree = $prompt -match "wia-$n(\D|$)"
    if (-not $mentionsIssue -or -not $mentionsWorktree) {
        [Console]::Error.WriteLine("Blocked: the open SPEC-003 audit is issue #$n, worktree wia-$n, but this prompt does not name both. Name the worktree explicitly in every specialist spawn (#1345, #1190).")
        exit 2
    }
    $otherWia = @([regex]::Matches($prompt, 'wia-(?<n>\d+)') | ForEach-Object { $_.Groups['n'].Value } | Where-Object { $_ -ne $n } | Select-Object -Unique)
    if ($otherWia.Count -gt 0) {
        [Console]::Error.WriteLine("Blocked: this prompt also mentions wia-$($otherWia -join ', wia-'), but only issue #$n is open right now (#1345). Batching issues into one spawn is the exact failure this pipeline closes.")
        exit 2
    }

    $pipelinePath = Join-Path $wt 'tools\ai\audit\pipeline.json'
    if (-not (Test-Path -LiteralPath $pipelinePath)) { exit 0 }
    $pipeline = Get-Content -LiteralPath $pipelinePath -Raw | ConvertFrom-Json

    $model = [string]$payload.tool_input.model
    if ($model -and $pipeline.forbiddenModels -and $model -in @($pipeline.forbiddenModels)) {
        [Console]::Error.WriteLine("Blocked: an audit-stage agent may not run on '$model' (#1345, tools/ai/audit/pipeline.json forbiddenModels). Use the agent's own default model or Sonnet.")
        exit 2
    }

    $stagesDir = Join-Path $wt 'artifacts\knowledge\stages'

    # A stage is done only when it has a 'Stage: <name>' commit AND the artifact has no uncommitted changes
    # since (git status --porcelain clean): --grep alone would prove a commit once existed, not that the
    # file on disk right now is what was committed. Mirrors _audit-lib.ps1's Test-StageCommitted / Get-NextStage
    # (inlined here so this hook has no dependency beyond pipeline.json).
    function Test-StageCommitted([string]$Worktree, [string]$StageName, [string]$ArtifactRelativePath) {
        $commit = & git -C $Worktree log -1 --grep "Stage: $StageName" --fixed-strings --pretty=format:%H 2>$null
        if ([string]::IsNullOrWhiteSpace(($commit | Select-Object -First 1))) { return $false }
        $dirty = & git -C $Worktree status --porcelain -- $ArtifactRelativePath 2>$null
        return [string]::IsNullOrWhiteSpace(($dirty | Select-Object -First 1))
    }

    $nextStage = $null
    foreach ($stage in @($pipeline.stages)) {
        $file = Join-Path $stagesDir $stage.artifact
        $relative = "artifacts/knowledge/stages/$($stage.artifact)"
        $done = (Test-Path -LiteralPath $file) -and (Test-StageCommitted $wt $stage.stage $relative)
        if (-not $done) { $nextStage = $stage; break }
    }

    # Resolve the verifier's artifact name from pipeline.json (the stage whose agent is audit-verifier),
    # the same way tools/ai/audit/_audit-lib.ps1's Test-LastVerdictFail does, instead of hard-coding
    # 'verification.md' (#1345).
    $verifierStage = @($pipeline.stages) | Where-Object { [string]$_.agent -eq 'audit-verifier' } | Select-Object -First 1
    $verificationFile = if ($verifierStage) { Join-Path $stagesDir $verifierStage.artifact } else { Join-Path $stagesDir 'verification.md' }
    $lastVerdictFail = $false
    if (Test-Path -LiteralPath $verificationFile) {
        $firstLine = Get-Content -LiteralPath $verificationFile -TotalCount 1
        $lastVerdictFail = $firstLine -eq 'Verdict: FAIL'
    }
    if ($lastVerdictFail) { exit 0 }

    if ($null -eq $nextStage) {
        [Console]::Error.WriteLine("Blocked: every pipeline stage for #$n already has a committed artifact and the last verdict was not FAIL; run tools/ai/audit/audit-done.ps1 instead of spawning another stage agent (#1345).")
        exit 2
    }
    $expectedAgent = [string]$nextStage.agent
    if ($subagent -ne $expectedAgent) {
        $instead = if ($expectedAgent -match '^issue-|^audit-|^docs-reviewer$') { "spawn $expectedAgent" } else { "run tools/ai/audit/audit-draft-remediation.ps1 (the '$($nextStage.stage)' stage runs a script, not an agent spawn)" }
        [Console]::Error.WriteLine("Blocked: the next stage due for #$n is '$($nextStage.stage)' ($instead), not a $subagent spawn (#1345; tools/ai/audit/audit-stage.ps1 -Next). Stages run in the fixed order of pipeline.json.")
        exit 2
    }
    exit 0
}
catch {
    exit 0
}
