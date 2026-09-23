# PreToolUse hook (Agent), wired in the frontmatter of every agent that may delegate: blocks spawning any
# subagent type outside that agent's allowlist (#1181).
#
#   issue-worker      ci-diagnoser, mechanical-fixer, Explore, adversarial-reviewer (self-review), docs-writer
#   docs-writer       mechanical-fixer, docs-reviewer (self-review), Explore
#   mechanical-fixer  ci-diagnoser, Explore
#
# No agent spawns another issue-worker (which could recurse and bypass the orchestrator's per-issue
# worktree and brief) or a general-purpose agent (which has no protocol constraints at all). The `tools:`
# line of each agent lists the same types as `Agent(...)`, but Claude Code ignores that list inside a
# subagent definition (it applies only to a main thread started with `claude --agent`), so this hook is the
# enforcement.
#
# The agent comes from the hook input's agent_type (set for tool calls inside a subagent) and, when that is
# absent, from the -Agent argument of the frontmatter command. An agent without an allowlist is not
# restricted. Inspected: the `subagent_type` of the Agent tool call; missing or empty means the default
# `general-purpose` agent, which is blocked. Exit code 2 blocks the call and shows stderr to Claude; any
# failure of the hook itself allows the call (fail open).

param([string]$Agent)

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json

    $allowlists = @{
        'issue-worker'     = @('ci-diagnoser', 'mechanical-fixer', 'Explore', 'adversarial-reviewer', 'docs-writer')
        'docs-writer'      = @('mechanical-fixer', 'docs-reviewer', 'Explore')
        'mechanical-fixer' = @('ci-diagnoser', 'Explore')
    }

    $caller = [string]$payload.agent_type
    if ([string]::IsNullOrWhiteSpace($caller)) { $caller = $Agent }
    if ([string]::IsNullOrWhiteSpace($caller) -or -not $allowlists.ContainsKey($caller)) { exit 0 }
    $allowed = $allowlists[$caller]

    $subagentType = [string]$payload.tool_input.subagent_type
    if ([string]::IsNullOrWhiteSpace($subagentType)) {
        [Console]::Error.WriteLine("Blocked: a $caller never spawns a general-purpose agent (empty/missing subagent_type). Allowed subagent types: $($allowed -join ', ') (see .claude/agents/$caller.md; #1181). Report the step to the orchestrator instead.")
        exit 2
    }

    if ($allowed -cnotcontains $subagentType) {
        [Console]::Error.WriteLine("Blocked: a $caller may only spawn these subagent types: $($allowed -join ', ') (see .claude/agents/$caller.md; #1181). '$subagentType' is not one of them. Report the step to the orchestrator instead.")
        exit 2
    }

    exit 0
}
catch {
    exit 0
}
