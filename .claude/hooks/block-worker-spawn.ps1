# PreToolUse hook (Agent), wired in the frontmatter of every agent that may delegate and, with
# -Agent orchestrator, in the project .claude/settings.json: blocks spawning any subagent type outside the
# caller's allowlist (#1181).
#
#   orchestrator      issue-worker, mechanical-fixer, docs-writer, docs-reviewer, adversarial-reviewer,
#                     ci-diagnoser, pr-watcher, Explore, Plan, claude-code-guide, general-purpose,
#                     issue-archivist, issue-auditor, test-auditor, audit-verifier (the SPEC-003 audit
#                     pipeline's stage agents, #1345 — audit-stage-guard.ps1 enforces which one, in which
#                     order, and for which open audit)
#   issue-worker      ci-diagnoser, mechanical-fixer, Explore, adversarial-reviewer (self-review), docs-writer,
#                     docs-reviewer (#1345: the docs stage of the SPEC-003 audit pipeline spawns docs-reviewer
#                     directly, without going through docs-writer)
#   docs-writer       mechanical-fixer, docs-reviewer (self-review), Explore
#   mechanical-fixer  ci-diagnoser, Explore
#   issue-archivist, issue-auditor, test-auditor, audit-verifier, docs-reviewer, adversarial-reviewer,
#   ci-diagnoser, pr-watcher   empty (#1345). Each is either a SPEC-003 audit stage agent (single-owner,
#                     no delegation) or a read-only specialist whose own definition lists no Agent tool; any
#                     spawn they attempt is denied.
#
# The orchestrator (the main session) spawns the specialists and the read-only research agents. It may spawn
# general-purpose for research: whether that agent only reads cannot be enforced here, but
# guard-orchestrator-writes.ps1 treats every subagent other than issue-worker, mechanical-fixer and
# docs-writer like the main session, so a general-purpose agent cannot edit src/ or tests/ either. Any other
# type (claude, statusline-setup, plugin agents, ...) is blocked.
# No worker spawns another issue-worker (which could recurse and bypass the orchestrator's per-issue worktree
# and brief) or a general-purpose agent (which has no protocol constraints at all). The `tools:` line of each
# agent lists the same types as `Agent(...)`, but Claude Code ignores that list inside a subagent definition
# (it applies only to a main thread started with `claude --agent`), so this hook is the enforcement.
#
# The caller is the hook input's agent_type when it has an allowlist (set for tool calls inside a subagent);
# otherwise the -Agent argument of the command (the frontmatter's agent, or orchestrator in settings.json, so
# an ungoverned subagent gets the orchestrator's list). A caller without an allowlist is not restricted.
# agent_id/agent_type are present for a subagent's own tool call and absent for the main session's
# (https://code.claude.com/docs/en/hooks.md, https://code.claude.com/docs/en/sub-agents.md); an absent
# agent_type falls back to -Agent below, which is how the main session's own PreToolUse Agent call (wired with
# -Agent orchestrator in settings.json) is told apart from a subagent's.
# Inspected: the `subagent_type` of the Agent tool call; missing or empty means the default `general-purpose`
# agent. Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call
# (fail open).

param([string]$Agent)

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json

    $allowlists = @{
        'orchestrator'          = @('issue-worker', 'mechanical-fixer', 'docs-writer', 'docs-reviewer', 'adversarial-reviewer', 'ci-diagnoser', 'pr-watcher', 'Explore', 'Plan', 'claude-code-guide', 'general-purpose', 'issue-archivist', 'issue-auditor', 'test-auditor', 'audit-verifier')
        'issue-worker'          = @('ci-diagnoser', 'mechanical-fixer', 'Explore', 'adversarial-reviewer', 'docs-writer', 'docs-reviewer')
        'docs-writer'           = @('mechanical-fixer', 'docs-reviewer', 'Explore')
        'mechanical-fixer'      = @('ci-diagnoser', 'Explore')
        'issue-archivist'       = @()
        'issue-auditor'         = @()
        'test-auditor'          = @()
        'audit-verifier'        = @()
        'docs-reviewer'         = @()
        'adversarial-reviewer'  = @()
        'ci-diagnoser'          = @()
        'pr-watcher'            = @()
    }

    $caller = [string]$payload.agent_type
    if ([string]::IsNullOrWhiteSpace($caller) -or -not $allowlists.ContainsKey($caller)) { $caller = $Agent }
    if ([string]::IsNullOrWhiteSpace($caller) -or -not $allowlists.ContainsKey($caller)) { exit 0 }
    $allowed = $allowlists[$caller]
    $definition = if ($caller -eq 'orchestrator') { '.claude/agents/README.md, Delegation' } else { ".claude/agents/$caller.md" }
    $instead = if ($caller -eq 'orchestrator') { 'Use the specialist that owns the step.' } else { 'Report the step to the orchestrator instead.' }

    if ($allowed.Count -eq 0) {
        [Console]::Error.WriteLine("Blocked: $caller cannot spawn any subagent - a stage agent or a read-only specialist never delegates (#1345). See $definition. $instead")
        exit 2
    }

    $subagentType = [string]$payload.tool_input.subagent_type
    if ([string]::IsNullOrWhiteSpace($subagentType)) {
        if ($allowed -ccontains 'general-purpose') { exit 0 }
        [Console]::Error.WriteLine("Blocked: a $caller never spawns a general-purpose agent (empty/missing subagent_type). Allowed subagent types: $($allowed -join ', ') (see $definition; #1181). $instead")
        exit 2
    }

    if ($allowed -cnotcontains $subagentType) {
        [Console]::Error.WriteLine("Blocked: a $caller may only spawn these subagent types: $($allowed -join ', ') (see $definition; #1181). '$subagentType' is not one of them. $instead")
        exit 2
    }

    exit 0
}
catch {
    exit 0
}
