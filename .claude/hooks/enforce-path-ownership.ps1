# PreToolUse hook (Write|Edit|NotebookEdit and Bash|PowerShell), wired in the frontmatter of issue-worker and
# docs-writer with -Agent <name>: a file belongs to the specialist that owns its kind, and the other agents
# delegate it (#1181; categories in _repo-paths.ps1, Get-PathCategory).
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
# project are allowed.
#
# Both the file tools AND shell writes are covered (#1345 review): a Set-Content, Add-Content, Out-File,
# redirection, [IO.File]/[IO.StreamWriter] call, Copy-Item/Move-Item or their Bash equivalents (cp, mv, touch,
# tee, >, >>) that targets a stage artifact bypasses the ownership rule exactly like a Write/Edit call would,
# and block-main-checkout-writes.ps1's "Edit tool only for source files" rule explicitly excludes `artifacts/`
# (its own Test-RepoFile), so nothing else stood in the way of a stage agent using its own Bash/PowerShell tool
# to fabricate another stage's file. Every write target Get-ShellWrites (_write-targets.ps1) recognises is
# checked the same way as a Write/Edit target.
#
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call.

param([string]$Agent)

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')
    . (Join-Path $PSScriptRoot '_repo-paths.ps1')
    . (Join-Path $PSScriptRoot '_write-targets.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $tool = [string]$payload.tool_name
    if ($tool -notin 'Write', 'Edit', 'MultiEdit', 'NotebookEdit', 'Bash', 'PowerShell') { exit 0 }

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

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }

    # Checks one resolved absolute path against every ownership rule. Returns $true (allowed) or $false
    # (blocked; the reason is already on stderr). A path outside the project is always allowed.
    function Test-PathOwnership([string]$Full) {
        $location = Get-RepoLocation $Full $layout
        if ($null -eq $location) { return $true }

        $relative = $location.Relative
        $category = Get-PathCategory $relative

        # #1345 fabrication gap: a SPEC-003 audit-stage artifact (artifacts/knowledge/stages/<file>) may be
        # written ONLY by the agent tools/ai/audit/pipeline.json assigns to that stage — never the orchestrator
        # (main session, an empty/absent $Agent) and never a different agent, so a coordinator or a wrong stage
        # cannot fabricate another stage's outcome. This runs for every caller (not only the audit-stage agents
        # below), because the gap is exactly a caller OTHER than the assigned agent writing the file. Exceptions,
        # by construction rather than by name here: stages/lessons.md is not a pipeline.stages entry (the
        # orchestrator fills its "Applied:" lines with the Write/Edit tool, so it must reach the default allow at
        # the end of this function); the 'remediation' stage's artifact is written by
        # tools/ai/audit/audit-draft-remediation.ps1 with Set-Content, never through a tool this hook lets any
        # caller reach for that path, so no caller here is ever its legitimate writer.
        # Matched case-insensitively: the filesystem this project runs on is case-insensitive, so
        # 'Artifacts\Knowledge\Stages\code.md' resolves to the very same on-disk file as
        # 'artifacts/knowledge/stages/code.md' and must not evade this check by casing alone.
        $stageArtifactMatch = [regex]::Match($relative, '^artifacts/knowledge/stages/(?<file>[^/]+)$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($stageArtifactMatch.Success) {
            $pipelinePath = Join-Path $location.Root 'tools\ai\audit\pipeline.json'
            $pipeline = $null
            if (Test-Path -LiteralPath $pipelinePath) {
                try { $pipeline = Get-Content -LiteralPath $pipelinePath -Raw | ConvertFrom-Json } catch { $pipeline = $null }
            }
            $stageDef = if ($null -ne $pipeline) { @($pipeline.stages) | Where-Object { $_.artifact -ieq $stageArtifactMatch.Groups['file'].Value } | Select-Object -First 1 } else { $null }
            if ($null -ne $stageDef) {
                $expectedAgent = [string]$stageDef.agent
                $knownStageAgents = 'issue-archivist', 'issue-auditor', 'test-auditor', 'audit-verifier', 'docs-reviewer'
                $callerLabel = if ([string]::IsNullOrWhiteSpace($Agent)) { 'the orchestrator (main session)' } else { $Agent }
                if ($expectedAgent -notin $knownStageAgents) {
                    [Console]::Error.WriteLine("Blocked: '$relative' is the '$($stageDef.stage)' stage artifact, written only by its own script (tools/ai/audit/audit-draft-remediation.ps1) via Set-Content, never through a tool this hook governs; $callerLabel may not write it this way (#1345).")
                    return $false
                }
                if ($Agent -ne $expectedAgent) {
                    [Console]::Error.WriteLine("Blocked: '$relative' is the '$($stageDef.stage)' stage artifact, owned by $expectedAgent (tools/ai/audit/pipeline.json); $callerLabel may not write it (#1345: a stage artifact is written only by the agent the pipeline assigns to that stage — this closes the gap that let a coordinator fabricate a stage's outcome).")
                    return $false
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
                return $true
            }
            # $stageArtifactMatch succeeded but the file names no pipeline.stages entry (e.g. lessons.md): not
            # covered by this gap-closing check; the rules below (and the default allow for the orchestrator) apply.
        }

        if ($Agent -eq 'issue-worker') {
            if ($category -eq 'docs') {
                [Console]::Error.WriteLine("Blocked: '$relative' is documentation, which docs-writer owns (#1181; .claude/agents/issue-worker.md, Delegation). Spawn docs-writer in the foreground on this worktree with the facts and decisions the page needs, and make no edits to it yourself. docs/plans/** and docs/knowledge/** are the only documentation an issue-worker writes (the latter per SPEC-003 DEC-005, #1311).")
                return $false
            }
            if ($category -in 'changelog', 'publicapi', 'manifest') {
                $kind = @{ changelog = 'a changelog fragment'; publicapi = 'a PublicAPI file'; manifest = 'a coverage manifest' }[$category]
                [Console]::Error.WriteLine("Blocked: '$relative' is $kind; these already-decided edits belong to mechanical-fixer (#1181; .claude/agents/issue-worker.md, Delegation). Spawn mechanical-fixer in the foreground on this worktree with the exact lines to add or change, and make no edits until it returns.")
                return $false
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
                return $false
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
                    return $false
                }
                return $true
            }
            [Console]::Error.WriteLine("Blocked: $Agent writes only its own SPEC-003 audit-stage artifact under artifacts/knowledge/stages/ and, for issue-archivist, the knowledge record under artifacts/knowledge/issues/; '$relative' is not one of them (#1345). This is a single-owner audit-stage role: report anything else to the orchestrator instead of editing it.")
            return $false
        }

        return $true
    }

    if ($tool -in 'Write', 'Edit', 'MultiEdit', 'NotebookEdit') {
        $target = [string]$payload.tool_input.file_path
        if ([string]::IsNullOrWhiteSpace($target)) { $target = [string]$payload.tool_input.notebook_path }
        if ([string]::IsNullOrWhiteSpace($target)) { exit 0 }
        if (-not (Test-PathOwnership (Get-FullPath $target $cwd))) { exit 2 }
        exit 0
    }

    # Bash/PowerShell: every write target the command contains (Set-Content, redirection, [IO.File] calls,
    # Copy-Item/Move-Item, Bash cp/mv/touch/tee, ...) is checked the same way, so a stage agent cannot reach a
    # stage artifact (or, for issue-worker/docs-writer, a category it does not own) through the shell instead
    # of the Write/Edit tool.
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }
    $scan = Get-ShellWrites -Command $command -Bash:($tool -eq 'Bash') -Cwd $cwd
    foreach ($w in $scan.Writes) {
        if ($null -eq $w.Full) { continue }
        if (-not (Test-PathOwnership $w.Full)) { exit 2 }
    }
    exit 0
}
catch {
    exit 0
}
