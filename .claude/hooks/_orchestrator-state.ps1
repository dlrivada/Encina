# Shared by tools/ai/orchestrator-mode.ps1 and the two orchestrator-mode hooks (#2516): where the state of a
# session the maintainer marked as the orchestrator lives, and how to read it.
#
# One JSON file per marked session at <main checkout>\artifacts\orchestrator-mode\<session_id>.json (artifacts\
# is git-ignored). The main checkout is resolved with _repo-paths.ps1, so a worktree copy of a hook and the
# main checkout read the same state. ENCINA_ORCHESTRATOR_STATE_ROOT (and orchestrator-mode.ps1 -StateRoot)
# only relocate the folder so tests can use a temp one; neither turns enforcement off.
#
# Content: sessionId, markedAtUtc, startPercent, pacePercent, capPercent (= startPercent + pacePercent).

. (Join-Path $PSScriptRoot '_repo-paths.ps1')

# The folder with the state files, or $null when the main checkout cannot be resolved.
function Get-OrchestratorStateRoot([string]$ProjectDir) {
    $override = [string]$env:ENCINA_ORCHESTRATOR_STATE_ROOT
    if (-not [string]::IsNullOrWhiteSpace($override)) { return $override }
    $layout = Get-RepoLayout $ProjectDir
    if ($null -eq $layout) { return $null }
    return Join-Path $layout.MainRoot 'artifacts\orchestrator-mode'
}

# A session id becomes a file name: only the characters of a GUID or a slug.
function Test-OrchestratorSessionId([string]$SessionId) {
    return $SessionId -match '^[A-Za-z0-9_-]{1,100}$'
}

function Get-OrchestratorStatePath([string]$StateRoot, [string]$SessionId) {
    return Join-Path $StateRoot "$SessionId.json"
}

# A percentage as text, with the invariant culture ("59", "17.5"), whatever the host's locale.
function Format-OrchestratorPercent($Value) {
    return ([double]$Value).ToString('0.##', [Globalization.CultureInfo]::InvariantCulture)
}

# markedAtUtc as a UTC DateTime ($null when missing or unparseable). ConvertFrom-Json turns an ISO date into a
# local DateTime, so both forms are normalised.
function Get-OrchestratorMarkedAt($State) {
    try {
        $value = $State.markedAtUtc
        if ($value -is [datetime]) { return $value.ToUniversalTime() }
        return [datetimeoffset]::Parse([string]$value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal).UtcDateTime
    }
    catch { return $null }
}

# The state of a marked session, or $null (unmarked, unreadable or malformed).
function Get-OrchestratorState([string]$StateRoot, [string]$SessionId) {
    try {
        if ([string]::IsNullOrWhiteSpace($StateRoot) -or -not (Test-OrchestratorSessionId $SessionId)) { return $null }
        $path = Get-OrchestratorStatePath $StateRoot $SessionId
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $null }
        $state = Get-Content -LiteralPath $path -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        if ([string]$state.sessionId -ne $SessionId) { return $null }
        foreach ($name in 'startPercent', 'pacePercent', 'capPercent') {
            if ($null -eq $state.$name) { return $null }
            $null = [double]$state.$name
        }
        return $state
    }
    catch { return $null }
}
