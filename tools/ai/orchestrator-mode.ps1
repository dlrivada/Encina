<#
.SYNOPSIS
    Marks, unmarks or reports the Claude Code session that acts as the orchestrator (#2516).

.DESCRIPTION
    A marked session gets a short delegation and budget-pace reminder on every prompt
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
    [Parameter(ParameterSetName = 'On')][ValidateRange(0, 100)][double]$PacePercent = 17,
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
    $state = [ordered]@{
        sessionId    = $SessionId
        markedAtUtc  = [TimeProvider]::System.GetUtcNow().ToString('yyyy-MM-ddTHH:mm:ssZ')
        startPercent = $StartPercent
        pacePercent  = $PacePercent
        capPercent   = $StartPercent + $PacePercent
    }
    [IO.File]::WriteAllText($path, ($state | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    "Session $SessionId marked as orchestrator: started at $StartPercent%, pace $PacePercent%/day, cap $($state.capPercent)%."
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
    else { "Session $SessionId is marked as orchestrator (since $($state.markedAtUtc)): started at $($state.startPercent)%, pace $($state.pacePercent)%/day, cap $($state.capPercent)%." }
}
