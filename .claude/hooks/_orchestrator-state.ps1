# Shared by tools/ai/orchestrator-mode.ps1 and the two orchestrator-mode hooks (#2516): where the state of a
# session the maintainer marked as the orchestrator lives, and how to read it.
#
# One JSON file per marked session at <main checkout>\artifacts\orchestrator-mode\<session_id>.json (artifacts\
# is git-ignored). The main checkout is resolved with _repo-paths.ps1, so a worktree copy of a hook and the
# main checkout read the same state. ENCINA_ORCHESTRATOR_STATE_ROOT (and orchestrator-mode.ps1 -StateRoot)
# only relocate the folder so tests can use a temp one; neither turns enforcement off.
#
# Content: sessionId, markedAtUtc, budgetDayStartUtc (the Friday-03:00Z-relative day the mark belongs to),
# budgetDay (its weekday name), startPercent, capPercent, stopPercent (#3175).
#
# Budget formula (the maintainer's permanent weekly rule): the week runs Friday 03:00Z to Friday 03:00Z, day d is
# 0 (Friday) .. 6 (Thursday). With remaining = 100 - start, the day's share is floor(remaining / (7 - d)), so
# Thursday gets all that remains. cap = start + share (at most 100); the stop order goes out at cap - 2.

# The weekly budget day of an instant: Index 0 (Friday) .. 6 (Thursday), its Name and the UTC instant it started.
function Get-OrchestratorBudgetDay([datetime]$NowUtc) {
    $names = 'Friday', 'Saturday', 'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday'
    $shifted = $NowUtc.ToUniversalTime().AddHours(-3)
    $index = ([int]$shifted.DayOfWeek - [int][DayOfWeek]::Friday + 7) % 7
    return [pscustomobject]@{
        Index    = $index
        Name     = $names[$index]
        StartUtc = [datetime]::SpecifyKind($shifted.Date.AddHours(3), [DateTimeKind]::Utc)
    }
}

# Cap and stop order for a day that starts at $StartPercent. An explicit -CapPercent / -StopPercent is used
# verbatim; when only one is given the other is derived 2 points away from it.
function Get-OrchestratorBudget([double]$StartPercent, [datetime]$NowUtc, $CapPercent = $null, $StopPercent = $null) {
    $day = Get-OrchestratorBudgetDay $NowUtc
    if ($null -ne $CapPercent) { $cap = [double]$CapPercent }
    elseif ($null -ne $StopPercent) { $cap = [math]::Min(100, [double]$StopPercent + 2) }
    else {
        $share = [math]::Floor((100 - $StartPercent) / (7 - $day.Index))
        $cap = [math]::Min(100, $StartPercent + $share)
    }
    $stop = if ($null -ne $StopPercent) { [double]$StopPercent } else { [math]::Max(0, $cap - 2) }
    return [pscustomobject]@{ Day = $day; CapPercent = $cap; StopPercent = $stop }
}

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
        foreach ($name in 'startPercent', 'capPercent', 'stopPercent') {
            if ($null -eq $state.$name) { return $null }
            $null = [double]$state.$name
        }
        return $state
    }
    catch { return $null }
}
