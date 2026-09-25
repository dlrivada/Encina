# PreToolUse hook (Write|Edit|NotebookEdit and Bash|PowerShell), wired in the frontmatter of issue-worker and
# docs-writer with -Agent <name>: a file belongs to the specialist that owns its kind, and the other agents
# delegate it (#1181; categories in _repo-paths.ps1, Get-PathCategory).
#
# Wired twice, deliberately (#1345 review, minor 3): once per-agent in frontmatter with an explicit -Agent
# <name> (the fallback used when the hook input carries no agent_type at all, i.e. the call is that agent's
# own, not a nested agent's), and once project-wide in .claude/settings.json with no -Agent, so $Agent comes
# entirely from the hook input's agent_type. The project-wide instance is what catches a caller with no
# dedicated frontmatter hook of its own (mechanical-fixer, the SPEC-003 audit-stage agents before their own
# frontmatter existed) and the orchestrator (main session), whose calls carry no agent_type at all and so fall
# through to the same stage-artifact and .authors.json rules below. Removing either copy would reopen a gap:
# dropping the frontmatter copy loses the -Agent fallback for an agent's own top-level calls when its own
# tool-call payload happens to omit agent_type; dropping the project-wide copy loses coverage for every caller
# without a dedicated frontmatter hook.
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
# git itself is also a bypass vector (#1345 review), closed two ways:
#   - `git checkout <rev> -- <path>` / `git restore <path>` can overwrite a stage artifact's working-tree
#     content without going through any tool this hook otherwise inspects; every path Get-ShellWrites resolves
#     for those verbs (`.Git[].Paths`) goes through the same Test-PathOwnership check as a Write/Edit target.
#   - `git commit` / `git apply` / `git am`, run directly instead of through tools/ai/audit/audit-commit-stage.ps1
#     (which alone checks the .authors.json sidecar) or audit-draft-remediation.ps1, would let anyone commit a
#     fabricated stage artifact, or apply a patch whose content this analysis cannot inspect. Since neither
#     script has any legitimate reason to be reached any other way inside an open audit's own worktree
#     (`.claude/worktrees/wia-<n>`), those three verbs are denied outright there for every caller when they are
#     the command's own top-level git invocation (not when they run inside a `pwsh -File` script this hook does
#     not execute) — fail-closed, per AGENTS.md §3, rather than trying to parse patch content.
#   - `.authors.json` itself: its only legitimate writer is this hook's own Set-Content call below, never a
#     tool call, so a direct Write/Edit/shell-write to it is always denied, including for the orchestrator
#     (previously it fell through to the default allow, since it names no pipeline.stages entry).
#
# #1374: the hook is wired twice on purpose (see above), so two of its own instances can update the sidecar
# concurrently for the same tool call. Their read-modify-write on .authors.json is serialized with a named
# System.Threading.Mutex derived from the sidecar's full path (a hex SHA-256 of the lower-cased path, so it is
# stable across processes and short enough for a kernel object name), WaitOne with a 5-second timeout; a
# timeout denies the write (exit 2) rather than racing the other instance. The write itself is atomic: the
# updated JSON goes to a per-process temp file next to the sidecar, then [System.IO.File]::Move(...,
# overwrite: true) replaces the sidecar in one filesystem operation, so a reader never observes a half-written
# or concatenated file. A sidecar that exists but fails to parse as a single JSON object denies the write
# (fail closed, AGENTS.md §3) with the parse error and the repair instruction ("run
# tools/ai/audit/audit-stage.ps1 -RepairAuthors from the main checkout") instead of being silently swallowed
# and masking the real cause, as it did before #1374.
#
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call
# (except the authorship-sidecar write above, which denies on its own failure instead — see #1374).

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

    # A short, stable, filesystem-independent name for a named Mutex guarding $Path: a hex SHA-256 of the
    # lower-cased full path (#1374), so two hook processes racing on the very same sidecar always compute the
    # same mutex name regardless of case differences the filesystem itself ignores.
    function Get-PathLockName([string]$Path) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Path.ToLowerInvariant())
        $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
        $hex = -join ($hash | ForEach-Object { $_.ToString('x2') })
        return "Local\Encina.AuthorsSidecar.$hex"
    }

    # Records $Agent as the last writer of $Stage in $Root's authorship sidecar (#1374). Serializes the
    # read-modify-write across every concurrent instance of this hook with a named Mutex (5-second timeout),
    # writes atomically (temp file next to the sidecar, then an overwriting File.Move), and fails closed: a
    # lock timeout, an unparseable existing sidecar, or any other exception returns $false (denying the write
    # that triggered it) with the reason on stderr, instead of silently doing nothing as the previous
    # `catch { }` did. An existing sidecar that is not a single JSON object is reported with the exact parse
    # error and the sanctioned repair command, never masked.
    function Set-StageAuthor([string]$Root, [string]$Stage, [string]$Agent) {
        $authorsPath = Join-Path $Root 'artifacts\knowledge\stages\.authors.json'
        $mutex = [System.Threading.Mutex]::new($false, (Get-PathLockName $authorsPath))
        $acquired = $false
        try {
            try { $acquired = $mutex.WaitOne(5000) }
            catch [System.Threading.AbandonedMutexException] { $acquired = $true }
            if (-not $acquired) {
                [Console]::Error.WriteLine("Blocked: could not acquire the lock on 'artifacts/knowledge/stages/.authors.json' within 5 seconds (another hook instance may be stuck); the '$Stage' stage authorship was not recorded (#1374).")
                return $false
            }

            $authors = @{}
            if (Test-Path -LiteralPath $authorsPath) {
                $raw = Get-Content -LiteralPath $authorsPath -Raw
                if (-not [string]::IsNullOrWhiteSpace($raw)) {
                    try {
                        $parsed = $raw | ConvertFrom-Json -AsHashtable
                        if ($null -ne $parsed) { $authors = $parsed }
                    }
                    catch {
                        [Console]::Error.WriteLine("Blocked: 'artifacts/knowledge/stages/.authors.json' is not valid JSON ($($_.Exception.Message)); run 'pwsh -NoProfile -File tools/ai/audit/audit-stage.ps1 -RepairAuthors' from the main checkout to repair it, then have $Agent re-write the '$Stage' stage artifact (#1374).")
                        return $false
                    }
                }
            }

            $authors[$Stage] = @{ agent = $Agent; utc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ') }
            $dir = Split-Path -Parent $authorsPath
            New-Item -ItemType Directory -Force $dir | Out-Null
            $tempPath = Join-Path $dir ".authors.json.$PID.$([guid]::NewGuid().ToString('N')).tmp"
            ($authors | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath $tempPath -Encoding utf8
            [System.IO.File]::Move($tempPath, $authorsPath, $true)
            return $true
        }
        catch {
            [Console]::Error.WriteLine("Blocked: could not record authorship for the '$Stage' stage in artifacts/knowledge/stages/.authors.json: $($_.Exception.Message) (#1374).")
            return $false
        }
        finally {
            if ($acquired) { $mutex.ReleaseMutex() }
            $mutex.Dispose()
        }
    }

    # Checks one resolved absolute path against every ownership rule. Returns $true (allowed) or $false
    # (blocked; the reason is already on stderr). A path outside the project is always allowed.
    function Test-PathOwnership([string]$Full) {
        $location = Get-RepoLocation $Full $layout
        if ($null -eq $location) { return $true }

        $relative = $location.Relative
        $category = Get-PathCategory $relative

        # #1345 review: the authorship sidecar has exactly one legitimate writer — this hook's own Set-Content
        # call below, invoked directly by the hook process, never through a Write/Edit/shell tool call. Denied
        # unconditionally, including for the orchestrator (an empty $Agent), which would otherwise fall through
        # every rule below to the default allow, since '.authors.json' names no pipeline.stages entry.
        if ($relative -match '(?i)^artifacts/knowledge/stages/\.authors\.json$') {
            [Console]::Error.WriteLine("Blocked: '$relative' is the stage-authorship sidecar, written only by enforce-path-ownership.ps1 itself when it allows a stage-artifact write; no tool call may write it directly, including the orchestrator (#1345 review).")
            return $false
        }

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
                # #1374: this hook runs twice concurrently for the same tool call (see header comment), so the
                # read-modify-write below is serialized with a named mutex and written atomically via a
                # temp-file-and-move, and any failure (lock timeout, unparseable sidecar, anything else) DENIES
                # the write instead of silently allowing it — a swallowed failure here previously masked a
                # corrupted sidecar as "no recorded author" downstream (#1374).
                if (-not (Set-StageAuthor $location.Root $stageDef.stage $Agent)) { return $false }
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

    # git as a bypass vector (#1345 review; see the header comment). `checkout <rev> -- <path>` / `restore
    # <path>` can overwrite a stage artifact's content directly: every resolved path goes through the same
    # ownership check. `commit` / `apply` / `am`, run as this command's own top-level git invocation (not
    # inside a `pwsh -File` script this analysis does not execute), are denied outright inside an open audit's
    # worktree (`.claude/worktrees/wia-<n>`) for every caller: neither verb has a legitimate direct use there
    # (tools/ai/audit/audit-commit-stage.ps1 and audit-draft-remediation.ps1 are the only sanctioned writers,
    # and calling one of those scripts is a `pwsh`/`dotnet` command, never a bare `git commit`/`apply`/`am`),
    # and `apply`/`am` can touch file content this text-only analysis cannot inspect, so this fails closed.
    $auditWorktreePattern = '[\\/]\.claude[\\/]worktrees[\\/]wia-\d+(?:[\\/]|$)'
    foreach ($g in $scan.Git) {
        foreach ($p in @($g.Paths)) { if (-not (Test-PathOwnership $p)) { exit 2 } }
        if ($g.Verb -in 'commit', 'apply', 'am' -and $g.Dir -match $auditWorktreePattern) {
            [Console]::Error.WriteLine("Blocked: 'git $($g.Verb)' inside an open audit's worktree ($($g.Dir)) is denied for every caller; only tools/ai/audit/audit-commit-stage.ps1 (which checks the .authors.json sidecar) and audit-draft-remediation.ps1 may commit or apply changes there (#1345 review: a bare git commit/apply/am would bypass the stage-authorship check entirely).")
            exit 2
        }
    }
    exit 0
}
catch {
    exit 0
}
