# UserPromptSubmit hook, wired in .claude/settings.json (#2516): a session the maintainer marked as the
# orchestrator (tools/ai/orchestrator-mode.ps1 -On) gets a reminder of at most 3 lines on every prompt: what
# the orchestrator keeps, what it delegates, and the budget of the day.
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

    # The cap belongs to the budget day (Friday 03:00Z based) the session was marked on; on a later one the line says so.
    $day = Get-OrchestratorBudgetDay ([TimeProvider]::System.GetUtcNow().UtcDateTime)
    $markedDayStart = try { [datetimeoffset]::Parse([string]$state.budgetDayStartUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal).UtcDateTime } catch { $null }
    $label = if ($null -eq $markedDayStart -or $markedDayStart -ne $day.StartUtc) {
        $setOn = if ($null -ne $markedDayStart) { $markedDayStart.ToString('yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture) } else { 'an unknown day' }
        "Budget (set $setOn, re-run -On for today)"
    } else { "Budget today ($($day.Name) of the week resetting Fri 03:00Z)" }
    $lines = @(
        'Opus decides, sequences, explains dilemmas, merges; edits, issue/PR comments, gh batches, digests and long reads go to mechanical-fixer / Haiku / Sonnet / local model.',
        "${label}: started at $(Format-OrchestratorPercent $state.startPercent)%, stop order at $(Format-OrchestratorPercent $state.stopPercent)%, cap $(Format-OrchestratorPercent $state.capPercent)%; check usage before refilling slots.",
        'See CLAUDE.md "Orchestration and delegation".'
    )
    @{ hookSpecificOutput = @{ hookEventName = 'UserPromptSubmit'; additionalContext = ($lines -join "`n") } } | ConvertTo-Json -Compress
}
catch { }
exit 0
