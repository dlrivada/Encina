# UserPromptSubmit hook, wired in .claude/settings.json (#2516): a session the maintainer marked as the
# orchestrator (tools/ai/orchestrator-mode.ps1 -On) gets a reminder of at most 3 lines on every prompt: what
# the orchestrator keeps, what it delegates, and the budget pace of the day.
#
# Silent (exit 0, no output) for an unmarked session, for a payload with agent_id (a subagent) and on any error
# reading the payload or the state: a reminder must never block a prompt. There is no bypass flag.

param()

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_read-payload.ps1')
    . (Join-Path $PSScriptRoot '_orchestrator-state.ps1')
    $payload = Read-HookStdin | ConvertFrom-Json
    if (-not [string]::IsNullOrWhiteSpace([string]$payload.agent_id)) { exit 0 }

    $projectDir = if ([string]::IsNullOrWhiteSpace([string]$env:CLAUDE_PROJECT_DIR)) { [string]$payload.cwd } else { [string]$env:CLAUDE_PROJECT_DIR }
    $state = Get-OrchestratorState (Get-OrchestratorStateRoot $projectDir) ([string]$payload.session_id)
    if ($null -eq $state) { exit 0 }

    # The cap belongs to the day the session was marked; on a later UTC day the line says so.
    $markedAt = Get-OrchestratorMarkedAt $state
    $today = [TimeProvider]::System.GetUtcNow().UtcDateTime.Date
    $label = if ($null -ne $markedAt -and $markedAt.Date -ne $today) { "Budget pace (set $($markedAt.ToString('yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)), re-run -On for today)" } else { 'Budget pace today' }
    $lines = @(
        'Opus decides, sequences, explains dilemmas, merges; edits, issue/PR comments, gh batches, digests and long reads go to mechanical-fixer / Haiku / Sonnet / local model.',
        "${label}: started at $(Format-OrchestratorPercent $state.startPercent)%, cap $(Format-OrchestratorPercent $state.capPercent)% (pace $(Format-OrchestratorPercent $state.pacePercent)%/day); check usage before refilling slots.",
        'See CLAUDE.md "Orchestration and delegation".'
    )
    @{ hookSpecificOutput = @{ hookEventName = 'UserPromptSubmit'; additionalContext = ($lines -join "`n") } } | ConvertTo-Json -Compress
}
catch { }
exit 0
