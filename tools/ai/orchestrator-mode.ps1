<#
.SYNOPSIS
    Marks, unmarks or reports the Claude Code session that acts as the orchestrator (#2516).

.DESCRIPTION
    The budget follows the weekly formula in .claude/hooks/_orchestrator-state.ps1 (week Fri 03:00Z to Fri 03:00Z;
    day share = floor(remaining / days left), Thursday takes the rest; stop order = cap - 2); -CapPercent and
    -StopPercent override it verbatim (#3175).
    A marked session gets a short delegation and budget reminder on every prompt
    (.claude/hooks/orchestrator-prompt-reminder.ps1) and its specialist-level steps are denied with a message
    naming the specialist (.claude/hooks/orchestrator-delegation-guard.ps1). Every other session and every
    subagent is untouched. State: one JSON file per session at
    <main checkout>\artifacts\orchestrator-mode\<session_id>.json (-StateRoot only relocates it for tests).

.EXAMPLE
    pwsh -File tools/ai/orchestrator-mode.ps1 -On -StartPercent 59
    pwsh -File tools/ai/orchestrator-mode.ps1 -Status
    pwsh -File tools/ai/orchestrator-mode.ps1 -Off
#>
[CmdletBinding(DefaultParameterSetName = 'Status')]
param(
    [Parameter(Mandatory, ParameterSetName = 'On')][switch]$On,
    [Parameter(Mandatory, ParameterSetName = 'On')][ValidateRange(0, 100)][double]$StartPercent,
    # Explicit overrides, used verbatim; with only one of them the other is 2 points away (stop = cap - 2).
    [Parameter(ParameterSetName = 'On')][ValidateRange(0, 100)][double]$CapPercent,
    [Parameter(ParameterSetName = 'On')][ValidateRange(0, 100)][double]$StopPercent,
    # The instant the budget day is computed for; only tests pass it.
    [Parameter(ParameterSetName = 'On')][datetimeoffset]$NowUtc,
    [Parameter(Mandatory, ParameterSetName = 'Off')][switch]$Off,
    [Parameter(ParameterSetName = 'Status')][switch]$Status,
    # Defaults to $env:CLAUDE_CODE_SESSION_ID, which Claude Code exports to tool shells and which equals the hook
    # payload's session_id.
    [string]$SessionId,
    [string]$StateRoot
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '..\..\.claude\hooks\_orchestrator-state.ps1')

if ([string]::IsNullOrWhiteSpace($SessionId)) { $SessionId = [string]$env:CLAUDE_CODE_SESSION_ID }
if ([string]::IsNullOrWhiteSpace($SessionId)) {
    throw 'No session id: pass -SessionId or run inside Claude Code, which sets CLAUDE_CODE_SESSION_ID.'
}
if (-not (Test-OrchestratorSessionId $SessionId)) {
    throw "Invalid session id '$SessionId': use letters, digits, '-' and '_' only (at most 100)."
}
if ([string]::IsNullOrWhiteSpace($StateRoot)) {
    $StateRoot = Get-OrchestratorStateRoot (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
}
if ([string]::IsNullOrWhiteSpace($StateRoot)) { throw 'Cannot resolve the state folder; pass -StateRoot.' }
$path = Get-OrchestratorStatePath $StateRoot $SessionId

if ($On) {
    New-Item -ItemType Directory -Force $StateRoot | Out-Null
    $now = if ($PSBoundParameters.ContainsKey('NowUtc')) { $NowUtc.UtcDateTime } else { [TimeProvider]::System.GetUtcNow().UtcDateTime }
    $overrides = @{}
    foreach ($name in 'CapPercent', 'StopPercent') { if ($PSBoundParameters.ContainsKey($name)) { $overrides[$name] = $PSBoundParameters[$name] } }
    $budget = Get-OrchestratorBudget -StartPercent $StartPercent -NowUtc $now @overrides
    $state = [ordered]@{
        sessionId         = $SessionId
        markedAtUtc       = $now.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
        budgetDayStartUtc = $budget.Day.StartUtc.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
        budgetDay         = $budget.Day.Name
        startPercent      = $StartPercent
        capPercent        = $budget.CapPercent
        stopPercent       = $budget.StopPercent
    }
    [IO.File]::WriteAllText($path, ($state | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    "Session $SessionId marked as orchestrator: $($budget.Day.Name) of the week resetting Fri 03:00Z, started at $(Format-OrchestratorPercent $StartPercent)%, stop order at $(Format-OrchestratorPercent $budget.StopPercent)%, cap $(Format-OrchestratorPercent $budget.CapPercent)%."
}
elseif ($Off) {
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        Remove-Item -LiteralPath $path -Force
        "Session $SessionId unmarked."
    }
    else { "Session $SessionId was not marked." }
}
else {
    $state = Get-OrchestratorState $StateRoot $SessionId
    if ($null -eq $state) { "Session $SessionId is unmarked." }
    else {
        $markedAt = Get-OrchestratorMarkedAt $state
        $since = if ($null -ne $markedAt) { $markedAt.ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture) } else { 'unknown' }
        "Session $SessionId is marked as orchestrator (since $since): started at $(Format-OrchestratorPercent $state.startPercent)%, stop order at $(Format-OrchestratorPercent $state.stopPercent)%, cap $(Format-OrchestratorPercent $state.capPercent)%."
    }
}
