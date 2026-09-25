# PreToolUse hook (Write|Edit|NotebookEdit), wired in the frontmatter of issue-worker and docs-writer with
# -Agent <name>: a file belongs to the specialist that owns its kind, and the other agents delegate it
# (#1181; categories in _repo-paths.ps1, Get-PathCategory).
#
#   issue-worker  may not edit documentation (docs/**/*.md except docs/plans/** and docs/knowledge/**, the
#                 images docs pages show, the root README.md, package READMEs and other .md under src/,
#                 CONTRIBUTING.md): spawn docs-writer. The site's code and data under docs/ (*.js, *.html,
#                 *.json, *.yml, ...) are code: the issue-worker edits them and self-reviews them with
#                 adversarial-reviewer. docs/knowledge/** is the issue-worker's own: SPEC-003 DEC-005 has the
#                 closing issue-worker write the per-issue knowledge record and audit result in the same pull
#                 request that closes the issue, because it holds the facts and the audit outcomes of its own
#                 diff; a record is structured data, not prose (#1311).
#                 may not edit changelog.d/**, **/PublicAPI.*.txt, .github/coverage-manifest/**: spawn
#                 mechanical-fixer with the exact lines.
#   docs-writer   allowlist: documentation, docs/knowledge/** (keeps access so it can still fix a record while
#                 writing the destination page that cites it), README.md and CONTRIBUTING.md anywhere
#                 (.github/**/README.md included), changelog.d/** (fragments the brief asks for) and
#                 artifacts/** (its issue files); never .claude/**. Everything else (src/, tests/, build files,
#                 docs/ site code and data) is denied: spawn mechanical-fixer for an already-decided edit,
#                 otherwise report it.
#   the five SPEC-003 audit-stage agents (issue-archivist, issue-auditor, test-auditor, audit-verifier,
#                 docs-reviewer in audit mode; #1345) each write ONLY their own stage artifact under
#                 artifacts/knowledge/stages/, as tools/ai/audit/pipeline.json assigns it — never another
#                 stage's file, and never src/, tests/, docs/ or .claude/. issue-archivist also owns the
#                 knowledge record under artifacts/knowledge/issues/. The stage-ownership check below runs for
#                 EVERY caller, not only these five, so the orchestrator (main session) and any other agent
#                 are denied from writing a stage artifact too — closing the gap that let a coordinator
#                 fabricate a stage's outcome. A successful write records its author in the sidecar
#                 artifacts/knowledge/stages/.authors.json, which audit-commit-stage.ps1 checks before
#                 committing.
#   others        not restricted (mechanical-fixer is the delegate).
#
# docs/plans/** stays with the issue-worker: an implementation plan is an issue-scoped working document
# produced with the implementation-plan prompt, not published documentation.
#
# The rule applies to the agent named by -Agent; when the hook input names a different agent_type (a nested
# agent that inherited the hook), the call is allowed, since that agent has its own hooks. Paths outside the
# project are allowed. Only the file tools are covered: shell writes to these paths are not (the source-edit
# rule of block-main-checkout-writes.ps1 already blocks content writes to .md/.txt/.json files).
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call.

param([string]$Agent)

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_repo-paths.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    if ([string]$payload.tool_name -notin 'Write', 'Edit', 'MultiEdit', 'NotebookEdit') { exit 0 }

    # agent_id/agent_type: present for a subagent's own tool call, absent for the main session's
    # (https://code.claude.com/docs/en/hooks.md, https://code.claude.com/docs/en/sub-agents.md). A missing
    # $caller falls back to -Agent (this frontmatter hook's own agent); a different, non-empty $caller (a
    # nested agent that inherited the hook) is a mismatch: that agent has its own hooks, so the call is let
    # through here.
    $caller = [string]$payload.agent_type
    if ([string]::IsNullOrWhiteSpace($Agent)) { $Agent = $caller }
    elseif (-not [string]::IsNullOrWhiteSpace($caller) -and $caller -ne $Agent) { exit 0 }

    $projectDir = [string]$env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($projectDir)) { exit 0 }
    $layout = Get-RepoLayout $projectDir
    if ($null -eq $layout) { exit 0 }

    $target = [string]$payload.tool_input.file_path
    if ([string]::IsNullOrWhiteSpace($target)) { $target = [string]$payload.tool_input.notebook_path }
    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $location = Get-RepoLocation (Get-FullPath $target $cwd) $layout
    if ($null -eq $location) { exit 0 }

    $relative = $location.Relative
    $category = Get-PathCategory $relative

    # #1345 fabrication gap: a SPEC-003 audit-stage artifact (artifacts/knowledge/stages/<file>) may be
    # written ONLY by the agent tools/ai/audit/pipeline.json assigns to that stage — never the orchestrator
    # (main session, an empty/absent $Agent) and never a different agent, so a coordinator or a wrong stage
    # cannot fabricate another stage's outcome. This runs for every caller (not only the audit-stage agents
    # below), because the gap is exactly a caller OTHER than the assigned agent writing the file. Exceptions,
    # by construction rather than by name here: stages/lessons.md is not a pipeline.stages entry (the
    # orchestrator fills its "Applied:" lines with the Write/Edit tool, so it must reach the default allow at
    # the end of this hook — handled by simply not matching a stage below); the 'remediation' stage's artifact
    # is written by tools/ai/audit/audit-draft-remediation.ps1 with Set-Content, never through the Write/Edit
    # tool this hook governs, so no caller here is ever its legitimate writer.
    $stageArtifactMatch = [regex]::Match($relative, '^artifacts/knowledge/stages/(?<file>[^/]+)$')
    if ($stageArtifactMatch.Success) {
        $pipelinePath = Join-Path $location.Root 'tools\ai\audit\pipeline.json'
        $pipeline = $null
        if (Test-Path -LiteralPath $pipelinePath) {
            try { $pipeline = Get-Content -LiteralPath $pipelinePath -Raw | ConvertFrom-Json } catch { $pipeline = $null }
        }
        $stageDef = if ($null -ne $pipeline) { @($pipeline.stages) | Where-Object { $_.artifact -eq $stageArtifactMatch.Groups['file'].Value } | Select-Object -First 1 } else { $null }
        if ($null -ne $stageDef) {
            $expectedAgent = [string]$stageDef.agent
            $knownStageAgents = 'issue-archivist', 'issue-auditor', 'test-auditor', 'audit-verifier', 'docs-reviewer'
            $callerLabel = if ([string]::IsNullOrWhiteSpace($Agent)) { 'the orchestrator (main session)' } else { $Agent }
            if ($expectedAgent -notin $knownStageAgents) {
                [Console]::Error.WriteLine("Blocked: '$relative' is the '$($stageDef.stage)' stage artifact, written only by its own script (tools/ai/audit/audit-draft-remediation.ps1) via Set-Content, never through the Write/Edit tool; $callerLabel may not write it this way (#1345).")
                exit 2
            }
            if ($Agent -ne $expectedAgent) {
                [Console]::Error.WriteLine("Blocked: '$relative' is the '$($stageDef.stage)' stage artifact, owned by $expectedAgent (tools/ai/audit/pipeline.json); $callerLabel may not write it (#1345: a stage artifact is written only by the agent the pipeline assigns to that stage — this closes the gap that let a coordinator fabricate a stage's outcome).")
                exit 2
            }
            # Allowed: record authorship in the sidecar so audit-commit-stage.ps1 can refuse to commit a
            # stage whose last recorded writer does not match the agent pipeline.json assigns to it.
            try {
                $authorsPath = Join-Path $location.Root 'artifacts\knowledge\stages\.authors.json'
                $authors = @{}
                if (Test-Path -LiteralPath $authorsPath) {
                    $existing = Get-Content -LiteralPath $authorsPath -Raw | ConvertFrom-Json -AsHashtable
                    if ($null -ne $existing) { $authors = $existing }
                }
                $authors[[string]$stageDef.stage] = @{ agent = $Agent; utc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ') }
                New-Item -ItemType Directory -Force (Split-Path -Parent $authorsPath) | Out-Null
                ($authors | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath $authorsPath -Encoding utf8
            }
            catch { }
            exit 0
        }
        # $stageArtifactMatch succeeded but the file names no pipeline.stages entry (e.g. lessons.md): not
        # covered by this gap-closing check; the rules below (and the default allow for the orchestrator) apply.
    }

    if ($Agent -eq 'issue-worker') {
        if ($category -eq 'docs') {
            [Console]::Error.WriteLine("Blocked: '$relative' is documentation, which docs-writer owns (#1181; .claude/agents/issue-worker.md, Delegation). Spawn docs-writer in the foreground on this worktree with the facts and decisions the page needs, and make no edits to it yourself. docs/plans/** and docs/knowledge/** are the only documentation an issue-worker writes (the latter per SPEC-003 DEC-005, #1311).")
            exit 2
        }
        if ($category -in 'changelog', 'publicapi', 'manifest') {
            $kind = @{ changelog = 'a changelog fragment'; publicapi = 'a PublicAPI file'; manifest = 'a coverage manifest' }[$category]
            [Console]::Error.WriteLine("Blocked: '$relative' is $kind; these already-decided edits belong to mechanical-fixer (#1181; .claude/agents/issue-worker.md, Delegation). Spawn mechanical-fixer in the foreground on this worktree with the exact lines to add or change, and make no edits until it returns.")
            exit 2
        }
    }
    elseif ($Agent -eq 'docs-writer') {
        $allowed = $relative -notmatch '^\.claude(/|$)' -and (
            $category -in 'docs', 'knowledge', 'changelog' -or
            $relative -match '(^|/)README\.md$' -or
            $relative -match '(^|/)CONTRIBUTING\.md$' -or
            $relative -match '^artifacts/')
        if (-not $allowed) {
            [Console]::Error.WriteLine("Blocked: docs-writer edits only documentation (docs/**/*.md and the images they show, READMEs, CONTRIBUTING.md), changelog fragments and its artifacts/ files; '$relative' is not one of them (#1181; .claude/agents/docs-writer.md, Delegation). Code, tests, .claude/, build files and the site's code and data under docs/ (*.js, *.html, *.json, *.yml such as docs/_config.yml) belong to others: spawn mechanical-fixer for an already-decided edit; otherwise list the change in your report for the orchestrator.")
            exit 2
        }
    }
    elseif ($Agent -in 'issue-archivist', 'issue-auditor', 'test-auditor', 'audit-verifier', 'docs-reviewer') {
        # A write to its own stage artifact already returned above (the stage-ownership check runs for every
        # caller before this branch). What is left to allow here is issue-archivist's knowledge record; anything
        # else under artifacts/knowledge/stages/ that names no pipeline stage (e.g. lessons.md) or that lies
        # outside artifacts/knowledge/ entirely is denied for these single-owner roles.
        if ($relative -match '^artifacts/knowledge/issues/[^/]+\.md$') {
            if ($Agent -ne 'issue-archivist') {
                [Console]::Error.WriteLine("Blocked: the knowledge record ('$relative') belongs to issue-archivist, not $Agent (#1345 single-owner roles).")
                exit 2
            }
            exit 0
        }
        [Console]::Error.WriteLine("Blocked: $Agent writes only its own SPEC-003 audit-stage artifact under artifacts/knowledge/stages/ and, for issue-archivist, the knowledge record under artifacts/knowledge/issues/; '$relative' is not one of them (#1345). This is a single-owner audit-stage role: report anything else to the orchestrator instead of editing it.")
        exit 2
    }

    exit 0
}
catch {
    exit 0
}
