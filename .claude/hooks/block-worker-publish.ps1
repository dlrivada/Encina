# PreToolUse hook (Bash|PowerShell), scoped to the issue-worker subagent's frontmatter: blocks publish-side
# git/gh actions.
#
# .claude/agents/issue-worker.md, Protocol: an issue-worker commits locally in its own worktree but never
# pushes, never opens/edits/merges/comments on/reviews/closes/readies a pull request, never creates/edits/
# comments on/closes/reopens an issue, and never calls the GitHub API with a mutating method. Those actions stay
# with the orchestrator. This hook enforces the rule mechanically so a worker cannot run them even if its brief,
# a nested agent, or a skill it loaded asks it to.
#
# Inspected: the arguments of every `git [global options] push` statement, every
# `gh pr create|edit|merge|comment|review|close|ready` and `gh issue create|edit|comment|close|reopen`
# statement, and every `gh api ... -X/--method POST|PATCH|PUT|DELETE` statement. Exit code 2 blocks the call
# and shows stderr to Claude; any failure of the hook itself allows the call (fail open).

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }
    if ($command -notmatch '\bgit\b|\bgh\b') { exit 0 }

    $statements = Split-CommandStatements -Text $command -Bash:($payload.tool_name -eq 'Bash')

    $prVerbs = @('create', 'edit', 'merge', 'comment', 'review', 'close', 'ready')
    $issueVerbs = @('create', 'edit', 'comment', 'close', 'reopen')
    $apiValueOptions = @('-X', '--method', '-H', '--header', '-f', '--raw-field', '-F', '--field', '-q', '--jq', '-t', '--template', '--input', '--hostname', '--cache', '--paginate-lookahead')

    foreach ($tokens in $statements) {
        # git [-C dir] [-c k=v] [--opt[=v]] ... push
        $k = Resolve-Executable $tokens
        if ($k -ge 0 -and (Get-ExecutableName $tokens[$k].Value) -eq 'git') {
            $j = $k + 1
            while ($j -lt $tokens.Count -and -not $tokens[$j].Quoted -and $tokens[$j].Value.StartsWith('-')) {
                $opt = $tokens[$j].Value
                if ($opt -ceq '-C' -and $j + 1 -lt $tokens.Count) { $j += 2; continue }
                if ($opt -ceq '-c' -or ($opt -in '--git-dir', '--work-tree', '--namespace', '--exec-path' -and -not $opt.Contains('='))) { $j += 2; continue }
                $j++
            }
            if ($j -lt $tokens.Count -and $tokens[$j].Value -eq 'push') {
                [Console]::Error.WriteLine("Blocked: an issue-worker never runs 'git push' (see .claude/agents/issue-worker.md, Protocol). Commit locally and report; the orchestrator pushes.")
                exit 2
            }
        }

        # gh pr create|edit|merge|comment|review|close|ready
        foreach ($verb in $prVerbs) {
            if ((Find-Invocation $tokens 'gh' @('pr', $verb)) -ge 0) {
                [Console]::Error.WriteLine("Blocked: an issue-worker never runs 'gh pr $verb' (see .claude/agents/issue-worker.md, Protocol). Report to the orchestrator, which runs the pr-cycle skill.")
                exit 2
            }
        }

        # gh issue create|edit|comment|close|reopen
        foreach ($verb in $issueVerbs) {
            if ((Find-Invocation $tokens 'gh' @('issue', $verb)) -ge 0) {
                [Console]::Error.WriteLine("Blocked: an issue-worker never runs 'gh issue $verb' (see .claude/agents/issue-worker.md, Protocol). Describe the follow-up in the report instead; the orchestrator opens it.")
                exit 2
            }
        }

        # gh api ... -X/--method POST|PATCH|PUT|DELETE
        $at = Find-Invocation $tokens 'gh' @('api')
        if ($at -ge 0) {
            $options = Get-CommandOptions $tokens $at $apiValueOptions
            foreach ($m in (Get-OptionValues $options @('-X', '--method'))) {
                if (-not $m.Dynamic -and (@('POST', 'PATCH', 'PUT', 'DELETE') -ccontains $m.Value.ToUpperInvariant())) {
                    [Console]::Error.WriteLine("Blocked: an issue-worker never calls 'gh api' with a mutating method ($($m.Value)) (see .claude/agents/issue-worker.md, Protocol). Report the needed API call to the orchestrator instead.")
                    exit 2
                }
            }
        }
    }
    exit 0
}
catch {
    exit 0
}
