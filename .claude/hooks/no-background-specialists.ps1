# PreToolUse hook (Agent|Task), wired unconditionally in the project .claude/settings.json AND, with
# -Agent <name>, in the frontmatter of every agent that may itself delegate (issue-worker, docs-writer,
# mechanical-fixer today; the SPEC-003 audit-stage agents once phase B adds their frontmatter) (#1345):
# denies run_in_background: true on any Agent/Task tool call made BY a subagent. A specialist spawned in the
# background and awaited by ending the caller's turn stalls for good, because its completion notice reaches
# the orchestrator, not the caller (2026-09-24 decision; the issue-worker's own Method step already required
# foreground spawns, but nothing enforced it). The main session (the orchestrator) is NOT restricted here: it
# deliberately runs background agents (fire-and-forget research, parallel workers).
#
# Caller detection mirrors block-worker-spawn.ps1: the hook input's agent_type is present for a subagent's
# own tool call, absent for the main session's (https://code.claude.com/docs/en/hooks.md); when agent_type
# is absent OR does not match a DIFFERENT agent (a nested agent that inherited this hook, which has its own
# copy), -Agent is the fallback so a blank/missing agent_type never silently exempts a genuine subagent
# call — the same two-signal pattern block-worker-spawn.ps1 and enforce-path-ownership.ps1 use, not a single
# point of trust.
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call.

param([string]$Agent)

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    if ([string]$payload.tool_name -notin 'Agent', 'Task') { exit 0 }

    $inputAgentType = [string]$payload.agent_type
    if ([string]::IsNullOrWhiteSpace($inputAgentType)) { $caller = $Agent }
    elseif (-not [string]::IsNullOrWhiteSpace($Agent) -and $inputAgentType -ne $Agent) { exit 0 }
    else { $caller = $inputAgentType }
    if ([string]::IsNullOrWhiteSpace($caller)) { exit 0 }

    if ($payload.tool_input.run_in_background -eq $true) {
        $subagent = [string]$payload.tool_input.subagent_type
        $target = if ($subagent) { $subagent } else { 'a subagent' }
        [Console]::Error.WriteLine("Blocked: a $caller may not spawn $target with run_in_background: true (#1345, #1190). Every specialist spawn from a worker or a stage agent runs in the foreground; a background spawn awaited by ending the turn stalls, because its completion notice reaches the orchestrator, not you.")
        exit 2
    }
    exit 0
}
catch {
    exit 0
}
