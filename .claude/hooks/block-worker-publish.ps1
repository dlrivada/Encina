# PreToolUse hook (Bash|PowerShell), scoped to the frontmatter of the writing and reviewing subagents
# (issue-worker, mechanical-fixer, docs-writer, docs-reviewer, site-steward, and, since #1447, pr-reviewer):
# blocks publish-side git/gh actions. The
# messages name the agent from the hook input's agent_type, present for a subagent's own tool call
# (https://code.claude.com/docs/en/hooks.md, https://code.claude.com/docs/en/sub-agents.md); this hook only
# runs scoped to one of these agents, so it is used solely to word the message, not to gate behaviour. The
# commands of a `pwsh -Command` / `bash -c` wrapper are inspected too (_command-text.ps1).
#
# .claude/agents/issue-worker.md, Protocol: an issue-worker commits locally in its own worktree but never
# pushes, never opens/edits/merges/comments on/reviews/closes/readies a pull request, never creates/edits/
# comments on/closes/reopens an issue, and never calls the GitHub API with a mutating method. Those actions stay
# with the orchestrator. This hook enforces the rule mechanically so a worker cannot run them even if its brief,
# a nested agent, or a skill it loaded asks it to.
#
# Inspected: every `git [global options] <subcommand>` statement, blocked unless the subcommand is on an
# allowlist of local/read-only verbs (so `push`, `send-pack`, `http-push` and any alias standing in for them
# are blocked by default) and no statement defines a git alias; every `gh pr create|edit|merge|comment|review|
# close|ready` and `gh issue create|edit|comment|close|reopen` statement; and every `gh api` statement that
# either names a mutating -X/--method (POST/PATCH/PUT/DELETE) or supplies a data field (-f/-F/--raw-field/
# --field/--input) without an explicit -X GET/--method GET (gh defaults to POST once a field is supplied).
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call
# (fail open).

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')

    . (Join-Path $PSScriptRoot '_read-payload.ps1')
    $payload = Read-HookStdin | ConvertFrom-Json
    $who = if ([string]::IsNullOrWhiteSpace([string]$payload.agent_type)) { 'a worker agent' } else { "a $([string]$payload.agent_type)" }
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }
    if ($command -notmatch '\bgit\b|\bgh\b') { exit 0 }

    $statements = Split-CommandStatements -Text $command -Bash:($payload.tool_name -eq 'Bash')

    $prVerbs = @('create', 'edit', 'merge', 'comment', 'review', 'close', 'ready')
    $issueVerbs = @('create', 'edit', 'comment', 'close', 'reopen')
    $apiValueOptions = @('-X', '--method', '-H', '--header', '-f', '--raw-field', '-F', '--field', '-q', '--jq', '-t', '--template', '--input', '--hostname', '--cache', '--paginate-lookahead')
    $apiDataOptions = @('-f', '--raw-field', '-F', '--field', '--input')

    # Everything a worker may do locally: inspection, staging, committing, branching, local history rewrite.
    # Anything not listed here — 'push' above all, but also 'send-pack', 'http-push', an alias standing in
    # for either, or any other subcommand — is blocked. Tags are local; 'push' remains the only publish path.
    $allowedGitSubcommands = @(
        'add', 'am', 'apply', 'archive', 'bisect', 'blame', 'branch', 'cat-file', 'check-ignore', 'checkout', 'cherry-pick',
        'clean', 'clone', 'commit', 'config', 'describe', 'diff', 'fetch', 'format-patch', 'grep', 'init', 'log',
        'ls-files', 'ls-remote', 'ls-tree', 'merge', 'merge-base', 'mv', 'notes', 'pull', 'rebase', 'reflog',
        'remote', 'reset', 'restore', 'rev-list', 'rev-parse', 'revert', 'rm', 'shortlog', 'show', 'show-ref',
        'sparse-checkout', 'stash', 'status', 'submodule', 'switch', 'tag', 'version', 'worktree'
    )

    foreach ($tokens in $statements) {
        # git [-C dir] [-c k=v] [--opt[=v]] ... <subcommand>: only a subcommand from $allowedGitSubcommands may
        # run, and no statement may define a git alias (which could stand in for a blocked subcommand).
        $k = Resolve-Executable $tokens
        if ($k -ge 0 -and (Get-ExecutableName $tokens[$k].Value) -eq 'git') {
            $j = $k + 1
            $definesAlias = $false
            $definesTarProgram = $false
            $definesDynamicConfig = $false
            while ($j -lt $tokens.Count -and -not $tokens[$j].Quoted -and $tokens[$j].Value.StartsWith('-')) {
                $opt = $tokens[$j].Value
                if ($opt -ceq '-C' -and $j + 1 -lt $tokens.Count) { $j += 2; continue }
                if ($opt -ceq '-c' -and $j + 1 -lt $tokens.Count) {
                    if (-not $tokens[$j + 1].Dynamic -and $tokens[$j + 1].Value -match '^alias\.') { $definesAlias = $true }
                    if ($tokens[$j + 1].Value -match '^(?i)tar\.') { $definesTarProgram = $true }
                    if ($tokens[$j + 1].Dynamic -or $tokens[$j + 1].Subexpression) { $definesDynamicConfig = $true }
                    $j += 2
                    continue
                }
                if ($opt -match '^--config-env(=|$)' -and ($opt -match '^--config-env=(?i)tar\.' -or ($opt -ceq '--config-env' -and $j + 1 -lt $tokens.Count -and $tokens[$j + 1].Value -match '^(?i)tar\.'))) { $definesTarProgram = $true }
                if ($opt -in '--git-dir', '--work-tree', '--namespace', '--exec-path' -and -not $opt.Contains('=')) { $j += 2; continue }
                $j++
            }

            if ($definesAlias) {
                [Console]::Error.WriteLine("Blocked: $who never defines a git alias (see .claude/agents/issue-worker.md, Protocol). An alias could stand in for a blocked subcommand such as 'push'; the orchestrator handles publishing.")
                exit 2
            }

            if ($definesTarProgram) {
                [Console]::Error.WriteLine("Blocked: $who never sets a git 'tar.*' configuration (see .claude/agents/issue-worker.md, Protocol). 'tar.<format>.command' names a program that 'git archive' runs, so it could run any command; use a built-in archive format.")
                exit 2
            }

            $verb = if ($j -lt $tokens.Count) { $tokens[$j].Value } else { $null }

            if ($verb -eq 'config') {
                # Operands of git config: the key and, when setting, the value. -f/--file/--blob take a value.
                $configOperands = [System.Collections.Generic.List[object]]::new()
                $configReads = $false
                for ($m = $j + 1; $m -lt $tokens.Count; $m++) {
                    $v = $tokens[$m].Value
                    if ($v -in '-f', '--file', '--blob') { $m++; continue }
                    if ($v.StartsWith('-')) { if ($v -match '^(--get|--list|-l$|--unset|--remove-section|--rename-section|--show|--default)') { $configReads = $true }; continue }
                    $configOperands.Add($tokens[$m])
                }
                if ($configOperands.Count -gt 0) {
                    $key = $configOperands[0]
                    if (-not $key.Dynamic -and $key.Value -match '^alias\.') {
                        [Console]::Error.WriteLine("Blocked: $who never defines a git alias with 'git config' (see .claude/agents/issue-worker.md, Protocol). An alias could stand in for a blocked subcommand such as 'push'; the orchestrator handles publishing.")
                        exit 2
                    }
                    # Reading or removing a tar.* key is harmless; setting one (key and value, no read option) is not.
                    if ($key.Value -match '^(?i)tar\.' -and $configOperands.Count -ge 2 -and -not $configReads) {
                        [Console]::Error.WriteLine("Blocked: $who never sets a git 'tar.*' configuration with 'git config' (see .claude/agents/issue-worker.md, Protocol). 'tar.<format>.command' names a program that 'git archive' runs, so it could run any command; use a built-in archive format.")
                        exit 2
                    }
                }
            }

            # git archive --remote contacts another repository, --exec runs a program there: both leave the
            # local, read-only use that justifies the verb (#2009). Options may follow the tree-ish, so every token
            # before `--` is read; git accepts any unambiguous prefix of a long option (--rem, --ex), and the
            # quotes around an option do not change what git sees.
            if ($verb -ceq 'archive') {
                # The environment can inject the same tar.<format>.command configuration as -c does.
                if ($definesDynamicConfig -or $command -match 'GIT_CONFIG_(COUNT|KEY_|VALUE_|PARAMETERS)') {
                    [Console]::Error.WriteLine("Blocked: $who never runs 'git archive' next to a git configuration it cannot read (-c with a variable, GIT_CONFIG_* variables): 'tar.<format>.command' names a program that 'git archive' runs (see .claude/agents/issue-worker.md, Protocol).")
                    exit 2
                }
                foreach ($tok in @($tokens | Select-Object -Skip ($j + 1))) {
                    # A variable, splat or subexpression may hold --remote or --exec (or an unseen --output
                    # target): the hook cannot read it, so it denies, as it does for a variable subcommand.
                    if ($tok.Value -eq '--') { break }
                    if ($tok.Dynamic -or $tok.Subexpression) {
                        [Console]::Error.WriteLine("Blocked: $who never runs 'git archive' with an argument the hook cannot read ('$($tok.Value)'): a variable could hold --remote, --exec or an --output path (see .claude/agents/issue-worker.md, Protocol). Pass the arguments as literals.")
                        exit 2
                    }
                    if (-not $tok.Value.StartsWith('--')) { continue }
                    $optionName = $tok.Value.Substring(2)
                    $equals = $optionName.IndexOf('=')
                    if ($equals -ge 0) { $optionName = $optionName.Substring(0, $equals) }
                    if ($optionName.Length -ge 1 -and ('remote'.StartsWith($optionName, [StringComparison]::Ordinal) -or 'exec'.StartsWith($optionName, [StringComparison]::Ordinal))) {
                        [Console]::Error.WriteLine("Blocked: $who never runs 'git archive' with --remote or --exec ('$($tok.Value)'): they contact another repository or run a program there (see .claude/agents/issue-worker.md, Protocol). Archive the local tree to stdout or with --output to a scratch path.")
                        exit 2
                    }
                }
            }

            if ($null -ne $verb -and $allowedGitSubcommands -cnotcontains $verb) {
                [Console]::Error.WriteLine("Blocked: $who may only run these git subcommands: $($allowedGitSubcommands -join ', ') (see .claude/agents/issue-worker.md, Protocol). '$verb' is not one of them (push, send-pack and http-push publish the repository; the orchestrator handles that).")
                exit 2
            }
        }

        # gh pr create|edit|merge|comment|review|close|ready
        foreach ($verb in $prVerbs) {
            if ((Find-Invocation $tokens 'gh' @('pr', $verb)) -ge 0) {
                [Console]::Error.WriteLine("Blocked: $who never runs 'gh pr $verb' (see .claude/agents/issue-worker.md, Protocol). Report to the orchestrator, which runs the pr-cycle skill.")
                exit 2
            }
        }

        # gh issue create|edit|comment|close|reopen
        foreach ($verb in $issueVerbs) {
            if ((Find-Invocation $tokens 'gh' @('issue', $verb)) -ge 0) {
                [Console]::Error.WriteLine("Blocked: $who never runs 'gh issue $verb' (see .claude/agents/issue-worker.md, Protocol). Describe the follow-up in the report instead; the orchestrator opens it.")
                exit 2
            }
        }

        # gh api ... -X/--method POST|PATCH|PUT|DELETE, or any data field (-f/-F/--raw-field/--field/--input)
        # without an explicit -X GET/--method GET — gh defaults to POST once a field is supplied.
        $at = Find-Invocation $tokens 'gh' @('api')
        if ($at -ge 0) {
            $options = Get-CommandOptions $tokens $at $apiValueOptions
            $explicitMethod = $null
            foreach ($m in (Get-OptionValues $options @('-X', '--method'))) {
                if (-not $m.Dynamic) { $explicitMethod = $m.Value.ToUpperInvariant() }
            }
            if ($explicitMethod -in 'POST', 'PATCH', 'PUT', 'DELETE') {
                [Console]::Error.WriteLine("Blocked: $who never calls 'gh api' with a mutating method ($explicitMethod) (see .claude/agents/issue-worker.md, Protocol). Report the needed API call to the orchestrator instead.")
                exit 2
            }
            if ($explicitMethod -ne 'GET' -and (Test-OptionPresent $options $apiDataOptions)) {
                [Console]::Error.WriteLine("Blocked: $who never calls 'gh api' with data fields and no explicit GET method (gh defaults to POST) (see .claude/agents/issue-worker.md, Protocol). Report the needed API call to the orchestrator instead.")
                exit 2
            }
        }
    }
    exit 0
}
catch {
    exit 0
}
