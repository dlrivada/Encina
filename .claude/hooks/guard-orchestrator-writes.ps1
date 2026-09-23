# PreToolUse hook (Write|Edit|NotebookEdit and Bash|PowerShell), wired in the project .claude/settings.json:
# the main session (the orchestrator) does not edit src/ or tests/, in the main checkout or in any worktree;
# it briefs an issue-worker instead (#1181).
#
# The main session is told apart by the hook input: agent_id is present only when the tool call comes from a
# subagent, and subagents are governed by their own frontmatter hooks, so any call with an agent_id is
# allowed. Everything outside src/** and tests/** stays allowed for the orchestrator (specifications, plans,
# .claude, memory, scratch files, git operations).
#
# Denied for the main session: a Write/Edit/NotebookEdit whose path resolves under <checkout>/src/ or
# <checkout>/tests/, where <checkout> is the main checkout or a worktree under .claude\worktrees\; and a shell
# write whose target resolves there (the write detection of _write-targets.ps1, shared with
# block-main-checkout-writes.ps1). Targets that depend on a variable, git commands, deletions and writes by
# other programs are not seen. Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook
# itself allows the call (fail open).

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')
    . (Join-Path $PSScriptRoot '_repo-paths.ps1')
    . (Join-Path $PSScriptRoot '_write-targets.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    if (-not [string]::IsNullOrWhiteSpace([string]$payload.agent_id)) { exit 0 }

    $projectDir = [string]$env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($projectDir)) { exit 0 }
    $layout = Get-RepoLayout $projectDir
    if ($null -eq $layout) { exit 0 }

    function Test-Guarded([string]$Full) {
        if ([string]::IsNullOrEmpty($Full)) { return $false }
        $location = Get-RepoLocation $Full $layout
        return $null -ne $location -and $location.Relative -match '^(src|tests)(/|$)'
    }

    function Write-Block([string]$Path) {
        [Console]::Error.WriteLine("Blocked: The orchestrator does not edit src/ or tests/; brief an issue-worker (worker-brief skill). Target: $Path (#1181; .claude/agents/README.md, Delegation).")
        exit 2
    }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $tool = [string]$payload.tool_name

    if ($tool -in 'Write', 'Edit', 'MultiEdit', 'NotebookEdit') {
        $target = [string]$payload.tool_input.file_path
        if ([string]::IsNullOrWhiteSpace($target)) { $target = [string]$payload.tool_input.notebook_path }
        $full = Get-FullPath $target $cwd
        if (Test-Guarded $full) { Write-Block $full }
        exit 0
    }

    if ($tool -notin 'Bash', 'PowerShell') { exit 0 }
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

    $scan = Get-ShellWrites -Command $command -Bash:($tool -eq 'Bash') -Cwd $cwd
    foreach ($w in $scan.Writes) {
        if (Test-Guarded $w.Full) { Write-Block $w.Full }
    }
    exit 0
}
catch {
    exit 0
}
