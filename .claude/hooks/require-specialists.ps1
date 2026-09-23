# Stop hook in the frontmatter of issue-worker and docs-writer (Claude Code runs it as SubagentStop), with
# -Agent <name>: before the agent stops, checks that it spawned the specialists its diff requires (#1181).
#
#   issue-worker  production code changed (src/** other than .md, .github/scripts/**, .claude/hooks/**)
#                   -> adversarial-reviewer (self-review, issue-worker.md Method step 2)
#                 documentation changed (docs/** except docs/plans/**, READMEs, CONTRIBUTING.md)
#                   -> docs-writer
#                 changelog.d/**, **/PublicAPI.*.txt or .github/coverage-manifest/** changed
#                   -> mechanical-fixer
#   docs-writer   documentation changed -> docs-reviewer
#
# Spawned specialists: the subagent_type of every Agent (or Task) tool_use in the agent's own transcript:
# agent_transcript_path, else <dir of transcript_path>\<session>\subagents\agent-<agent_id>.jsonl, else
# <dir of transcript_path>\subagents\agent-<agent_id>.jsonl.
# Changed files: in the agent's worktree (the hook input's cwd when it is a worktree under
# .claude\worktrees\, else the worktree the brief, the transcript's first user message, names), the union of
# `git diff --name-only origin/main...HEAD` (or main...HEAD), `git diff --name-only HEAD` and the untracked
# files. The diff covers every commit of the branch since it left main, including a specialist's commits.
#
# When a specialist is missing, the hook answers {"decision":"block","reason":...}: the agent continues with
# the list of specialists to run and why. When stop_hook_active is true (the agent is already continuing
# because of this hook) the hook allows the stop, so it forces one extra pass, not a loop. A missing
# transcript or worktree allows the stop with a warning to the user (systemMessage); any other failure of
# the hook allows the stop (fail open).

param([string]$Agent)

$ErrorActionPreference = 'Stop'

function Write-AllowWithWarning([string]$Text) {
    @{ systemMessage = "require-specialists (#1181): $Text" } | ConvertTo-Json -Compress
    exit 0
}

try {
    . (Join-Path $PSScriptRoot '_repo-paths.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    if ($payload.stop_hook_active -eq $true) { exit 0 }

    $caller = [string]$payload.agent_type
    if ([string]::IsNullOrWhiteSpace($Agent)) { $Agent = $caller }
    elseif (-not [string]::IsNullOrWhiteSpace($caller) -and $caller -ne $Agent) { exit 0 }
    if ($Agent -notin 'issue-worker', 'docs-writer') { exit 0 }

    # 1. The agent's own transcript.
    $candidates = [System.Collections.Generic.List[string]]::new()
    if ($payload.agent_transcript_path) { $candidates.Add([string]$payload.agent_transcript_path) }
    if ($payload.transcript_path -and $payload.agent_id) {
        $parent = [string]$payload.transcript_path
        $dir = Split-Path -Parent $parent
        $session = [IO.Path]::GetFileNameWithoutExtension($parent)
        $candidates.Add((Join-Path $dir "$session\subagents\agent-$($payload.agent_id).jsonl"))
        $candidates.Add((Join-Path $dir "subagents\agent-$($payload.agent_id).jsonl"))
    }
    $transcript = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if (-not $transcript) { Write-AllowWithWarning "the $Agent transcript was not found, so the check that it spawned its specialists was skipped." }

    $spawned = [System.Collections.Generic.HashSet[string]]::new()
    $briefWorktree = $null
    $briefRead = $false
    foreach ($line in [IO.File]::ReadLines($transcript)) {
        if (-not $briefRead -and $line.Contains('"type":"user"')) {
            $briefRead = $true
            try {
                $content = ($line | ConvertFrom-Json -Depth 64).message.content
                $text = if ($content -is [string]) { $content } else { (@($content) | Where-Object { $_.type -eq 'text' } | ForEach-Object { $_.text }) -join "`n" }
                $m = [regex]::Match($text, '[\\/]\.claude[\\/]worktrees[\\/](?<n>[^\\/\s`''"<>,;:)*?|]+)', 'IgnoreCase')
                if ($m.Success) { $briefWorktree = $m.Groups['n'].Value.TrimEnd('.') }
            }
            catch { }
        }
        if (-not $line.Contains('"tool_use"') -or -not $line.Contains('subagent_type')) { continue }
        try {
            $entry = $line | ConvertFrom-Json -Depth 64
            foreach ($item in @($entry.message.content)) {
                if ($item.type -eq 'tool_use' -and $item.name -in 'Agent', 'Task' -and $item.input.subagent_type) { [void]$spawned.Add([string]$item.input.subagent_type) }
            }
        }
        catch {
            foreach ($m in [regex]::Matches($line, '"subagent_type"\s*:\s*"(?<t>[^"\\]+)"')) { [void]$spawned.Add($m.Groups['t'].Value) }
        }
    }

    # 2. The worktree.
    $layout = if ($env:CLAUDE_PROJECT_DIR) { Get-RepoLayout $env:CLAUDE_PROJECT_DIR } else { $null }
    $cwd = Get-FullPath ([string]$payload.cwd) $null
    $worktree = $null
    if ($null -ne $layout -and $null -ne $cwd) {
        $location = Get-RepoLocation $cwd $layout
        if ($null -ne $location -and $location.InWorktree) { $worktree = $location.Root }
    }
    if (-not $worktree -and $briefWorktree -and $null -ne $layout) {
        $named = Join-Path $layout.Worktrees $briefWorktree
        if (Test-Path -LiteralPath $named -PathType Container) { $worktree = $named }
    }
    if (-not $worktree -and $null -ne $cwd -and ($null -eq $layout -or -not (Test-MainCheckout $cwd $layout))) { $worktree = $cwd }
    if (-not $worktree) { Write-AllowWithWarning "could not tell which worktree the $Agent worked in (its cwd is the main checkout and its brief names no worktree), so the specialist check was skipped." }

    # 3. The changed files.
    $top = & git -C $worktree rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $top) { Write-AllowWithWarning "$worktree is not a git work tree, so the specialist check was skipped." }
    $top = [IO.Path]::GetFullPath([string]($top | Select-Object -First 1))
    $files = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($base in 'origin/main', 'main') {
        & git -C $top rev-parse --verify --quiet "$base^{commit}" 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            foreach ($f in (& git -C $top diff --name-only "$base...HEAD" 2>$null)) { if ($f) { [void]$files.Add($f) } }
            break
        }
    }
    foreach ($f in (& git -C $top diff --name-only HEAD 2>$null)) { if ($f) { [void]$files.Add($f) } }
    foreach ($f in (& git -C $top ls-files --others --exclude-standard 2>$null)) { if ($f) { [void]$files.Add($f) } }

    # 4. The specialists the diff requires.
    $required = [ordered]@{}
    foreach ($f in ($files | Sort-Object)) {
        $category = Get-PathCategory $f
        $specialist = $null
        if ($Agent -eq 'issue-worker') {
            if ($category -eq 'code') { $specialist = 'adversarial-reviewer' }
            elseif ($category -eq 'docs') { $specialist = 'docs-writer' }
            elseif ($category -in 'changelog', 'publicapi', 'manifest') { $specialist = 'mechanical-fixer' }
        }
        elseif ($category -eq 'docs') { $specialist = 'docs-reviewer' }
        if ($specialist) {
            if (-not $required.Contains($specialist)) { $required[$specialist] = [System.Collections.Generic.List[string]]::new() }
            $required[$specialist].Add($f)
        }
    }

    $why = @{
        'adversarial-reviewer' = "production code changed; spawn adversarial-reviewer in the foreground on 'git -C $top diff origin/main...HEAD' with the brief's acceptance criteria, fix every blocker and major, re-run the verification"
        'docs-writer'          = 'documentation changed, and documentation belongs to docs-writer; spawn it on this worktree to write or re-check these pages'
        'mechanical-fixer'     = 'changelog fragments, PublicAPI files or coverage manifests changed, and those edits belong to mechanical-fixer; spawn it on this worktree with the exact lines'
        'docs-reviewer'        = 'documentation changed; spawn docs-reviewer on these pages and fix every blocker and major it reports'
    }
    $missing = @($required.Keys | Where-Object { -not $spawned.Contains($_) })
    if ($missing.Count -eq 0) { exit 0 }

    $lines = foreach ($s in $missing) {
        $sample = @($required[$s] | Select-Object -First 5) -join ', '
        $more = if ($required[$s].Count -gt 5) { " and $($required[$s].Count - 5) more" } else { '' }
        "- $s`: $($why[$s]) ($sample$more)."
    }
    $reason = "Delegation is mandatory (#1181, .claude/agents/$Agent.md): before you stop, run the specialists your diff requires and that you have not spawned yet:`n$($lines -join "`n")`nThen update your report's delegation section. If you cannot spawn one, say so in the report."
    @{ decision = 'block'; reason = $reason } | ConvertTo-Json -Compress
    exit 0
}
catch {
    exit 0
}
