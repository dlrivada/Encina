# Stop hook in the frontmatter of issue-worker and docs-writer (Claude Code runs it as SubagentStop, and a
# SubagentStop input always carries agent_id/agent_type: https://code.claude.com/docs/en/hooks.md,
# https://code.claude.com/docs/en/sub-agents.md), with -Agent <name>: before the agent stops, checks that it
# spawned the specialists its diff requires (#1181).
#
#   issue-worker  production code changed (src/** other than .md, docs/** site code and data, .github/scripts/**,
#                 .claude/hooks/**) -> adversarial-reviewer (self-review, issue-worker.md Method step 2)
#                 documentation changed (docs/**/*.md except docs/plans/**, the images docs pages show, READMEs,
#                 CONTRIBUTING.md) -> docs-writer
#                 changelog.d/**, **/PublicAPI.*.txt or .github/coverage-manifest/** changed
#                   -> mechanical-fixer
#   docs-writer   documentation changed -> docs-reviewer
#   (categories: _repo-paths.ps1, Get-PathCategory)
#
# Spawned specialists: the subagent_type of every Agent (or Task) tool_use in the agent's own transcript whose
# tool_result exists and is not an error (a denied or failed spawn does not count): agent_transcript_path,
# else <dir of transcript_path>\<session>\subagents\agent-<agent_id>.jsonl, else
# <dir of transcript_path>\subagents\agent-<agent_id>.jsonl.
# Changed files: in the agent's worktree (the hook input's cwd when it is a worktree under .claude\worktrees\;
# else, among the worktrees named in the text of the transcript's user messages (the brief and later
# corrections, not tool output), the first that exists and has changes, or the first that exists), the union
# of `git diff --name-only origin/main...HEAD` (or main...HEAD), `git diff --name-only HEAD` and the untracked
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

    # agent_id/agent_type: a SubagentStop input always carries both; a plain Stop input (the main session
    # stopping) carries neither (https://code.claude.com/docs/en/hooks.md). Since -Agent names the specific
    # subagent this frontmatter hook belongs to, an empty $caller (a plain Stop, or a field genuinely absent)
    # is a mismatch just like a different agent_type is: neither is this agent's own stop, so the check is
    # skipped rather than assumed to apply.
    $caller = [string]$payload.agent_type
    if ([string]::IsNullOrWhiteSpace($Agent)) { $Agent = $caller }
    elseif ($caller -ne $Agent) { exit 0 }
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

    # Spawns: tool_use id -> subagent_type. A spawn counts only when its tool_result exists and is not an error
    # (a spawn the hooks denied, or that failed to start, did not run the specialist).
    $spawnTypes = @{}
    $resultOk = [System.Collections.Generic.HashSet[string]]::new()
    $resultError = [System.Collections.Generic.HashSet[string]]::new()
    # Worktree names mentioned in the text of the user messages (the brief, the orchestrator's corrections), in
    # order; tool results are not read, since command output names other worktrees too.
    $mentioned = [System.Collections.Generic.List[string]]::new()
    foreach ($line in [IO.File]::ReadLines($transcript)) {
        $isUser = $line.Contains('"type":"user"')
        $isSpawn = $line.Contains('"tool_use"') -and $line.Contains('subagent_type')
        if (-not $isUser -and -not $isSpawn) { continue }
        try { $entry = $line | ConvertFrom-Json -Depth 64 } catch { continue }
        $content = $entry.message.content
        if ($entry.type -eq 'user') {
            $text = if ($content -is [string]) { $content } else { (@($content) | Where-Object { $_.type -eq 'text' } | ForEach-Object { $_.text }) -join "`n" }
            foreach ($m in [regex]::Matches([string]$text, '[\\/]\.claude[\\/]worktrees[\\/](?<n>[^\\/\s`''"<>,;:)*?|]+)', 'IgnoreCase')) {
                $name = $m.Groups['n'].Value.TrimEnd('.')
                if ($name -and -not $mentioned.Contains($name)) { $mentioned.Add($name) }
            }
            foreach ($item in @($content)) {
                if ($item -is [string] -or $item.type -ne 'tool_result' -or -not $item.tool_use_id) { continue }
                if ($item.is_error -eq $true) { [void]$resultError.Add([string]$item.tool_use_id) } else { [void]$resultOk.Add([string]$item.tool_use_id) }
            }
            continue
        }
        foreach ($item in @($content)) {
            if ($item.type -eq 'tool_use' -and $item.name -in 'Agent', 'Task' -and $item.input.subagent_type -and $item.id) { $spawnTypes[[string]$item.id] = [string]$item.input.subagent_type }
        }
    }
    $spawned = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($id in $spawnTypes.Keys) {
        if ($resultOk.Contains($id) -and -not $resultError.Contains($id)) { [void]$spawned.Add($spawnTypes[$id]) }
    }

    # The files a git work tree changed since its branch left main, or $null when $Dir is not a work tree.
    function Get-ChangedFiles([string]$Dir) {
        $top = & git -C $Dir rev-parse --show-toplevel 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $top) { return $null }
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
        return [pscustomobject]@{ Top = $top; Files = $files }
    }

    # 2. The worktree: the cwd when it is a worktree under .claude\worktrees\; else, among the worktrees the user
    # messages name, the first that exists and has changes (else the first that exists); else a cwd outside the
    # main checkout.
    $layout = if ($env:CLAUDE_PROJECT_DIR) { Get-RepoLayout $env:CLAUDE_PROJECT_DIR } else { $null }
    $cwd = Get-FullPath ([string]$payload.cwd) $null
    $worktree = $null
    $changes = $null
    if ($null -ne $layout -and $null -ne $cwd) {
        $location = Get-RepoLocation $cwd $layout
        if ($null -ne $location -and $location.InWorktree) { $worktree = $location.Root }
    }
    $existing = [System.Collections.Generic.List[string]]::new()
    if (-not $worktree -and $null -ne $layout) {
        foreach ($name in $mentioned) {
            $named = Join-Path $layout.Worktrees $name
            if (-not (Test-Path -LiteralPath $named -PathType Container)) { continue }
            $existing.Add($named)
            $candidate = Get-ChangedFiles $named
            if ($null -ne $candidate -and $candidate.Files.Count -gt 0) { $worktree = $named; $changes = $candidate; break }
        }
        if (-not $worktree -and $existing.Count -gt 0) { $worktree = $existing[0] }
    }
    if (-not $worktree -and $null -ne $cwd -and ($null -eq $layout -or -not (Test-MainCheckout $cwd $layout))) { $worktree = $cwd }
    if (-not $worktree) {
        $cwdText = if ($null -ne $cwd) { "its cwd ($cwd) is not a worktree under $($layout.Worktrees)" } else { 'the hook input has no cwd' }
        $namedText = if ($mentioned.Count -gt 0) { "the worktrees its messages name ($($mentioned -join ', ')) do not exist" } else { 'its messages name no worktree under .claude\worktrees\' }
        Write-AllowWithWarning "could not tell which worktree the $Agent worked in: $cwdText, and $namedText. The specialist check was skipped."
    }

    # 3. The changed files.
    if ($null -eq $changes) { $changes = Get-ChangedFiles $worktree }
    if ($null -eq $changes) { Write-AllowWithWarning "$worktree is not a git work tree, so the specialist check was skipped." }
    $top = $changes.Top
    $files = $changes.Files

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
