# PreToolUse hook (Write|Edit|NotebookEdit and Bash|PowerShell), wired in the project .claude/settings.json:
# the main session (the orchestrator) does not edit src/ or tests/, in the main checkout or in any worktree;
# it briefs an issue-worker instead (#1181). A rebase conflict in src/ or tests/ is resolved by an
# issue-worker too, briefed through the worker-brief skill.
#
# Exempt: the governed writing agents, told apart by the hook input's agent_type (issue-worker,
# mechanical-fixer, docs-writer), which carry their own frontmatter hooks. Every other caller is treated like
# the main session: the main session itself (no agent_id) and any other subagent it spawns (general-purpose,
# Plan, claude, ...), so the orchestrator cannot route an edit through an ungoverned agent. Everything outside
# src/** and tests/** stays allowed (specifications, plans, .claude, memory, scratch files, git operations
# that do not write those folders).
#
# Denied:
#   - a Write/Edit/NotebookEdit whose path resolves under <checkout>/src/ or <checkout>/tests/, where
#     <checkout> is the main checkout or a worktree under .claude\worktrees\;
#   - a shell write whose target resolves there (the write detection of _write-targets.ps1, shared with
#     block-main-checkout-writes.ps1, including Invoke-WebRequest/Invoke-RestMethod -OutFile and
#     Start-Process -RedirectStandardOutput/-RedirectStandardError), and an Expand-Archive whose destination is
#     there or is the root of a checkout;
#   - `git checkout [<rev>] -- <paths>` / `git checkout <rev> <paths>` and `git restore` (other than --staged
#     alone) whose pathspecs resolve there or to a checkout root;
#   - `git apply` / `git am` whose patch names a file there, or whose patch cannot be read (stdin, a variable);
#   - `dotnet run <file>.cs` / `dotnet run --file <file>.cs` / `pwsh`/`powershell -File <file>.ps1`: the hook
#     reads the named script's own text and denies when it both references src/ or tests/ and contains a
#     file-write API (_write-targets.ps1, Test-ScriptHasWriteApi/Test-ScriptReferencesPath); a script path the
#     hook cannot resolve, or cannot read, is denied too, since it cannot rule out a write there (#1181; this
#     only partially closes the gap, since it is a text heuristic, not an execution of the script).
# The commands of a `pwsh -Command` / `bash -c` wrapper are analysed like the others.
# Not seen: targets that depend on a variable, deletions, and writes by other programs (dotnet run of a script
# that is not itself a bare or `--file` .cs argument, compiled tools, ...).
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call
# (fail open).

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')
    . (Join-Path $PSScriptRoot '_repo-paths.ps1')
    . (Join-Path $PSScriptRoot '_write-targets.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    # agent_id/agent_type: present (agent_type names the caller) for a subagent's own tool call, absent for the
    # main session's (https://code.claude.com/docs/en/hooks.md, https://code.claude.com/docs/en/sub-agents.md).
    $governed = @('issue-worker', 'mechanical-fixer', 'docs-writer')
    if (-not [string]::IsNullOrWhiteSpace([string]$payload.agent_id) -and $governed -ccontains [string]$payload.agent_type) { exit 0 }
    $who = if ([string]::IsNullOrWhiteSpace([string]$payload.agent_id)) { 'The orchestrator' } else { "A $([string]$payload.agent_type) subagent (not a governed writing agent)" }

    $projectDir = [string]$env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($projectDir)) { exit 0 }
    $layout = Get-RepoLayout $projectDir
    if ($null -eq $layout) { exit 0 }

    function Test-Guarded([string]$Full, [bool]$CheckoutRoot = $false) {
        if ([string]::IsNullOrEmpty($Full)) { return $false }
        $location = Get-RepoLocation $Full $layout
        if ($null -eq $location) { return $false }
        return $location.Relative -match '^(src|tests)(/|$)' -or ($CheckoutRoot -and $location.Relative -eq '')
    }

    function Write-Block([string]$What) {
        [Console]::Error.WriteLine("Blocked: $who does not edit src/ or tests/; brief an issue-worker (worker-brief skill), which also resolves rebase conflicts there. $What (#1181; .claude/agents/README.md, Delegation).")
        exit 2
    }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $tool = [string]$payload.tool_name

    if ($tool -in 'Write', 'Edit', 'MultiEdit', 'NotebookEdit') {
        $target = [string]$payload.tool_input.file_path
        if ([string]::IsNullOrWhiteSpace($target)) { $target = [string]$payload.tool_input.notebook_path }
        $full = Get-FullPath $target $cwd
        if (Test-Guarded $full) { Write-Block "Target: $full" }
        exit 0
    }

    if ($tool -notin 'Bash', 'PowerShell') { exit 0 }
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

    $scan = Get-ShellWrites -Command $command -Bash:($tool -eq 'Bash') -Cwd $cwd
    foreach ($w in $scan.Writes) {
        if (Test-Guarded $w.Full $w.Directory) { Write-Block "Target: $($w.Full) ($($w.What))" }
    }

    foreach ($g in $scan.Git) {
        foreach ($p in $g.Paths) {
            if (Test-Guarded $p $true) { Write-Block "'git $($g.Verb)' writes $p" }
        }
        if ($g.Verb -notin 'apply', 'am') { continue }
        $location = if ($g.Dir) { Get-RepoLocation (Get-FullPath $g.Dir $null) $layout } else { $null }
        if ($null -eq $location) { continue }
        if ($g.PatchUnresolved) { Write-Block "'git $($g.Verb)' in $($g.Dir) applies a patch the hook cannot read (stdin or a variable), so it cannot tell whether it writes src/ or tests/; pass the patch file by its literal path" }
        foreach ($patch in $g.Patches) {
            $text = Get-Content -LiteralPath $patch -Raw
            foreach ($m in [regex]::Matches($text, '(?m)^(?:diff --git a/(?<a>\S+) b/(?<b>\S+)|(?:\+\+\+|---) (?:[ab]/)?(?<p>\S+))')) {
                foreach ($group in 'a', 'b', 'p') {
                    $relative = $m.Groups[$group].Value
                    if (-not $relative -or $relative -eq '/dev/null') { continue }
                    $full = Get-FullPath $relative $location.Root
                    if (Test-Guarded $full) { Write-Block "'git $($g.Verb)' applies $patch, which changes $relative" }
                }
            }
        }
    }

    # `dotnet run <file>.cs` / `pwsh -File <file>.ps1`: read the script's own text, since the analysis above
    # only sees the invoking statement, not what the launched script does (#1181).
    foreach ($s in $scan.Scripts) {
        if ($null -eq $s.Full) { Write-Block "runs '$($s.Raw)' ($($s.Kind)), whose script path the hook cannot resolve, so it cannot rule out writes to src/ or tests/; pass its literal path" }
        $text = $null
        try { $text = Get-Content -LiteralPath $s.Full -Raw -ErrorAction Stop } catch { }
        if ($null -eq $text) { Write-Block "runs '$($s.Full)' ($($s.Kind)), which the hook could not read, so it cannot rule out writes to src/ or tests/" }
        if ((Test-ScriptHasWriteApi $text) -and (Test-ScriptReferencesPath $text @('src/', 'src\', 'tests/', 'tests\'))) {
            Write-Block "runs '$($s.Full)' ($($s.Kind)), which references src/ or tests/ and writes files"
        }
    }
    exit 0
}
catch {
    exit 0
}
