# PreToolUse hook (Agent), scoped to the issue-worker subagent's frontmatter: blocks spawning any
# subagent other than the ones the issue-worker is allowed to delegate to.
#
# .claude/agents/issue-worker.md, Protocol: an issue-worker only spawns `ci-diagnoser`, `mechanical-fixer`,
# `Explore` (read-only research) or `adversarial-reviewer` (read-only self-review of its own diff before it
# reports, #1181); it never spawns another `issue-worker` (which could recurse
# indefinitely and bypass the orchestrator's per-issue worktree/brief control) or a general-purpose agent
# (which has no protocol constraints at all). This hook enforces that allowlist mechanically so a worker
# cannot spawn a disallowed subagent even if its brief, a nested agent, or a skill it loaded asks it to.
#
# Inspected: the `subagent_type` field of the Agent tool call. Missing or empty means the default
# `general-purpose` agent, which is blocked. Exit code 2 blocks the call and shows stderr to Claude; any
# failure of the hook itself allows the call (fail open).

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json

    $subagentType = [string]$payload.tool_input.subagent_type
    $allowed = @('ci-diagnoser', 'mechanical-fixer', 'Explore', 'adversarial-reviewer')

    if ([string]::IsNullOrWhiteSpace($subagentType)) {
        [Console]::Error.WriteLine("Blocked: an issue-worker never spawns a general-purpose agent (empty/missing subagent_type). Allowed subagent types: $($allowed -join ', ') (see .claude/agents/issue-worker.md, Protocol). Report the step to the orchestrator instead.")
        exit 2
    }

    if ($allowed -cnotcontains $subagentType) {
        [Console]::Error.WriteLine("Blocked: an issue-worker may only spawn these subagent types: $($allowed -join ', ') (see .claude/agents/issue-worker.md, Protocol). '$subagentType' is not one of them. Report the step to the orchestrator instead.")
        exit 2
    }

    exit 0
}
catch {
    exit 0
}
