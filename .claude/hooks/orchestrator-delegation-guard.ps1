# PreToolUse hook (Read, Write|Edit|MultiEdit|NotebookEdit and Bash|PowerShell), wired in .claude/settings.json
# (#2516): in a session the maintainer marked as the orchestrator (tools/ai/orchestrator-mode.ps1 -On) the
# specialist-level steps are denied with a message naming the specialist that owns them. Every other session
# and every subagent (payload with agent_id) is untouched: exit 0, no output.
#
# Denied for the marked session:
#   a. Read of a file with more than 150 lines when the call has no limit or a limit above 150 (images and
#      PDFs excepted). Exempt paths: the user memory dir (<home>\.claude\projects\*\memory\), artifacts\handoff\
#      of the main checkout or a worktree, and CLAUDE.md / AGENTS.md at a checkout root.
#      -> an Explore or Sonnet subagent reads it and returns the conclusion.
#   b. Write/Edit/MultiEdit/NotebookEdit outside the user memory dir and the Claude scratchpad root
#      (<temp>\claude\). -> mechanical-fixer (Haiku), docs-writer, or an issue-worker brief.
#   c. A shell statement `gh issue comment|edit` or `gh pr comment|edit`, and a shell write (Set-Content,
#      Add-Content, Out-File, redirection, ... as resolved by _write-targets.ps1) whose target resolves inside
#      the main checkout or a .claude\worktrees\ folder. -> mechanical-fixer (writes) or a Haiku agent (gh
#      comment/edit batches). Read-only gh (issue view/list, pr view/list/checks, api GET) stays allowed.
#      Also `gh issue|pr close|reopen` with --comment or -c. Writes into artifacts\handoff\ and
#      artifacts\local-ai\briefs\ (git-ignored; handoff notes and local-model briefs) are allowed in (b) and (c).
#   d. A command made only of calls to tools/ai/orchestrator-mode.ps1 is allowed, so -Off works; its
#      redirections are still write targets and judged by (c).
#
# Fail open: a malformed payload, an unreadable state or any error in the hook allows the call. The hook
# enforces a working practice, not a security boundary. There is no bypass flag or environment variable;
# ENCINA_ORCHESTRATOR_STATE_ROOT only relocates the state folder for tests.
# Exit code 2 blocks the call and shows stderr to Claude.

param()

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_read-payload.ps1')
    . (Join-Path $PSScriptRoot '_orchestrator-state.ps1')

    $payload = Read-HookStdin | ConvertFrom-Json
    if (-not [string]::IsNullOrWhiteSpace([string]$payload.agent_id)) { exit 0 }

    # Latency prelude: a subagent or an unmarked session exits before the heavy helpers are loaded.
    $projectDir = if ([string]::IsNullOrWhiteSpace([string]$env:CLAUDE_PROJECT_DIR)) { [string]$payload.cwd } else { [string]$env:CLAUDE_PROJECT_DIR }
    $stateRoot = Get-OrchestratorStateRoot $projectDir
    $sessionId = [string]$payload.session_id
    if ([string]::IsNullOrWhiteSpace($stateRoot) -or -not (Test-OrchestratorSessionId $sessionId)) { exit 0 }
    if (-not (Test-Path -LiteralPath (Get-OrchestratorStatePath $stateRoot $sessionId) -PathType Leaf)) { exit 0 }

    . (Join-Path $PSScriptRoot '_command-text.ps1')
    . (Join-Path $PSScriptRoot '_write-targets.ps1')
    $state = Get-OrchestratorState $stateRoot $sessionId
    if ($null -eq $state) { exit 0 }
    $layout = Get-RepoLayout $projectDir
    if ($null -eq $layout) { exit 0 }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $tool = [string]$payload.tool_name

    function Write-Deny([string]$Message) {
        [Console]::Error.WriteLine("Blocked (orchestrator mode, #2516): $Message The maintainer ends the mode with 'pwsh -File tools/ai/orchestrator-mode.ps1 -Off' (.claude/agents/README.md, Orchestrator mode).")
        exit 2
    }

    # <home>\.claude\projects\<project>\memory\ : the maintainer's auto-memory.
    function Test-MemoryPath([string]$Full) {
        $projects = Join-Path (Get-HomeDirectory) '.claude\projects'
        if (-not (Test-Under $Full $projects)) { return $false }
        $rest = $Full.Substring([Math]::Min($projects.TrimEnd('\', '/').Length + 1, $Full.Length))
        return $rest -match '^[^\\/]+[\\/]memory([\\/]|$)'
    }

    # The two git-ignored folders the orchestrator writes itself: handoff notes and local-model briefs.
    function Test-AllowedArtifact([string]$Full) {
        $location = Get-RepoLocation $Full $layout
        return $null -ne $location -and $location.Relative -match '^artifacts/(handoff|local-ai/briefs)/'
    }

    function Test-ScratchPath([string]$Full) {
        $bases = @([IO.Path]::GetTempPath(), $env:TEMP, $env:TMP) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        foreach ($base in $bases) {
            if (Test-Under $Full (Join-Path $base 'claude')) { return $true }
        }
        return $false
    }

    if ($tool -eq 'Read') {
        $limit = $payload.tool_input.limit
        if ($null -ne $limit -and [double]$limit -le 150) { exit 0 }
        $full = Get-FullPath ([string]$payload.tool_input.file_path) $cwd
        if ($null -eq $full -or (Test-MemoryPath $full)) { exit 0 }
        if ($full -match '\.(png|jpe?g|gif|webp|bmp|pdf)$') { exit 0 }
        $location = Get-RepoLocation $full $layout
        if ($null -ne $location -and ($location.Relative -match '^artifacts/handoff/' -or $location.Relative -ceq 'CLAUDE.md' -or $location.Relative -ceq 'AGENTS.md')) { exit 0 }
        if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { exit 0 }
        $lines = 0
        foreach ($line in [IO.File]::ReadLines($full)) { $lines++; if ($lines -gt 150) { break } }
        if ($lines -gt 150) {
            Write-Deny "this Read has no limit (or a limit above 150) on a file of more than 150 lines ($full). Delegate the read to Explore or a Sonnet subagent and keep the conclusion, or read at most 150 lines with limit."
        }
        exit 0
    }

    if ($tool -in 'Write', 'Edit', 'MultiEdit', 'NotebookEdit') {
        $target = [string]$payload.tool_input.file_path
        if ([string]::IsNullOrWhiteSpace($target)) { $target = [string]$payload.tool_input.notebook_path }
        $full = Get-FullPath $target $cwd
        if ($null -eq $full -or (Test-MemoryPath $full) -or (Test-ScratchPath $full) -or (Test-AllowedArtifact $full)) { exit 0 }
        Write-Deny "the orchestrator does not edit files itself ($full). Decided edits go to mechanical-fixer (Haiku), documentation to docs-writer, code to an issue-worker brief; scratch output goes under %TEMP%\claude\."
    }

    if ($tool -notin 'Bash', 'PowerShell') { exit 0 }
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }
    $bash = $tool -eq 'Bash'

    # The script of a statement: the executable (direct, & or . call) or the -File argument of pwsh/powershell.
    function Get-StatementScript($Tokens) {
        $k = Resolve-Executable $Tokens
        if ($k -lt 0) { return $null }
        $candidate = $Tokens[$k].Value
        if ((Get-ExecutableName $candidate) -in 'pwsh', 'powershell', 'pwsh-preview') {
            $candidate = $null
            for ($j = $k + 1; $j -lt $Tokens.Count - 1; $j++) {
                if (-not $Tokens[$j].Quoted -and $Tokens[$j].Value -match '^-f(i(l(e)?)?)?$') { $candidate = $Tokens[$j + 1].Value; break }
            }
        }
        return $candidate
    }

    # Split-CommandStatements returns the statement list as one object: the comma keeps each statement's tokens
    # as one element, so @() cannot wrap the whole list into a single element.
    $statements = @(foreach ($s0 in (Split-CommandStatements -Text $command -Bash:$bash)) { , $s0 })
    $onlyModeScript = $statements.Count -gt 0
    foreach ($tokens in $statements) {
        $script = Get-StatementScript $tokens
        $scriptFull = if ($script) { Get-FullPath $script $cwd } else { $null }
        $scriptLocation = if ($scriptFull) { Get-RepoLocation $scriptFull $layout } else { $null }
        if ($null -eq $scriptLocation -or $scriptLocation.Relative -cne 'tools/ai/orchestrator-mode.ps1') { $onlyModeScript = $false }
    }

    # The mode script itself is allowed, but its redirections are still write targets (scanned below).
    if (-not $onlyModeScript) {
        foreach ($tokens in $statements) {
            foreach ($verb in 'issue', 'pr') {
                if ((Find-Invocation $tokens 'gh' @($verb, 'comment|edit')) -ge 0) {
                    Write-Deny "'gh $verb comment/edit' is a Haiku step. Hand the exact text to a Haiku agent (batches of comments and edits) instead of running it in the orchestrator session. Read-only gh (view, list, checks, api GET) stays allowed."
                }
                # `gh issue|pr close|reopen` posts a comment only with --comment / -c.
                $after = Find-Invocation $tokens 'gh' @($verb, 'close|reopen')
                if ($after -ge 0) {
                    for ($j = $after; $j -lt $tokens.Count; $j++) {
                        if (-not $tokens[$j].Quoted -and $tokens[$j].Value -match '^(--comment(=.*)?|-c)$') {
                            Write-Deny "'gh $verb close/reopen' with a comment is a Haiku step. Hand the exact text to a Haiku agent instead of running it in the orchestrator session; without --comment it stays allowed."
                        }
                    }
                }
            }
        }
    }

    $scan = Get-ShellWrites -Command $command -Bash:$bash -Cwd $cwd
    foreach ($w in $scan.Writes) {
        if ($null -ne $w.Full -and (Test-Under $w.Full $layout.MainRoot) -and -not (Test-AllowedArtifact $w.Full)) {
            Write-Deny "this command writes a repository file ($($w.Full)). Decided writes go to mechanical-fixer (Haiku); scratch output goes under %TEMP%\claude\ (writes to artifacts\handoff\ and artifacts\local-ai\briefs\ are allowed)."
        }
    }
    exit 0
}
catch {
    exit 0
}
