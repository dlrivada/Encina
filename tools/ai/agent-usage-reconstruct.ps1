# Rebuilds missing rows of the agent usage ledger from the subagent transcripts, and warns about days with
# subagent activity but no ledger row (#2734). The SubagentStop hook (.claude/hooks/record-agent-usage.ps1) records
# every stop since 2026-11-10; this script covers the days before it and spots gaps later.
#
#   pwsh -NoProfile -File tools/ai/agent-usage-reconstruct.ps1 -From 2026-10-11 -To 2026-10-22            # dry run
#   pwsh -NoProfile -File tools/ai/agent-usage-reconstruct.ps1 -From 2026-10-11 -To 2026-10-22 -Append    # write
#   pwsh -NoProfile -File tools/ai/agent-usage-reconstruct.ps1 -CheckDays 30                              # missing-day check
#
# Row format and rules are the hook's: timestampUtc,agent,task,model,subagentTokens,notes. The stop time is the
# timestamp of the transcript's last line (UTC); the window is decided by it, both days inclusive. agent = the
# meta file's agentType (else "unknown"); task = issue-<n> of the first "Issue #<n>" of the first user line, else
# the worktree folder name of the first line's cwd, else session-<first 8 of the session folder>; model and tokens
# come from the LAST non-synthetic assistant message (tokens = input + cache creation + cache read + output).
# A transcript whose agent id is already in the ledger is skipped, so a second run adds nothing. Existing rows are
# never rewritten or reordered; new rows are appended sorted by timestamp. The main session's own usage is not a
# subagent and is not reconstructed.
param(
    [string]$From,
    [string]$To,
    [string]$Ledger,
    [string]$TranscriptRoot,
    [switch]$Append,
    [int]$CheckDays = 0
)

$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture

. (Join-Path $PSScriptRoot '..\..\.claude\hooks\_repo-paths.ps1')
$layout = Get-RepoLayout (Join-Path $PSScriptRoot '..\..')

if ([string]::IsNullOrWhiteSpace($Ledger)) { $Ledger = Join-Path $layout.MainRoot 'artifacts\agent-usage\ledger.csv' }
if ([string]::IsNullOrWhiteSpace($TranscriptRoot)) {
    $projectFolder = $layout.MainRoot -replace '[:\\/]', '-'
    $TranscriptRoot = Join-Path (Join-Path (Get-HomeDirectory) '.claude\projects') $projectFolder
}

function ConvertTo-UtcDay([string]$Text) {
    [DateTime]::ParseExact($Text, 'yyyy-MM-dd', $inv, [Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal)
}

# One transcript -> a row object, or $null when it has no non-synthetic assistant message. LastUtc is set whenever
# the file has a readable last timestamp, so callers can still place a skipped transcript on a day.
function Read-Transcript([IO.FileInfo]$File) {
    $lines = [IO.File]::ReadAllLines($File.FullName)
    $agentId = $File.BaseName -replace '^agent-', ''
    $lastUtc = $null
    for ($i = $lines.Length - 1; $i -ge 0; $i--) {
        $m = [regex]::Match($lines[$i], '"timestamp":"(?<t>[^"]+)"')
        if ($m.Success) {
            $parsed = [DateTimeOffset]::MinValue
            if ([DateTimeOffset]::TryParse($m.Groups['t'].Value, $inv, [Globalization.DateTimeStyles]::AssumeUniversal, [ref]$parsed)) { $lastUtc = $parsed.UtcDateTime; break }
        }
    }
    $result = [pscustomobject]@{ AgentId = $agentId; LastUtc = $lastUtc; Row = $null }
    if ($null -eq $lastUtc) { return $result }

    $task = $null; $firstCwd = $null; $firstUserSeen = $false
    foreach ($line in $lines) {
        if ($null -eq $firstCwd -and $line.Contains('"cwd"')) {
            $c = [regex]::Match($line, '"cwd":"(?<c>(?:[^"\\]|\\.)*)"')
            if ($c.Success) { $firstCwd = ('"' + $c.Groups['c'].Value + '"') | ConvertFrom-Json }
        }
        if (-not $firstUserSeen -and $line.Contains('"type":"user"')) {
            $firstUserSeen = $true
            $m = [regex]::Match($line, '(?i)issue #(?<n>\d+)')
            if ($m.Success) { $task = "issue-$($m.Groups['n'].Value)" }
        }
        if ($firstUserSeen -and $null -ne $firstCwd) { break }
    }

    $model = ''; $tokens = $null
    for ($i = $lines.Length - 1; $i -ge 0; $i--) {
        $line = $lines[$i]
        if (-not $line.Contains('"usage"') -or -not $line.Contains('"assistant"')) { continue }
        try { $entry = $line | ConvertFrom-Json -Depth 64 } catch { continue }
        $usage = $entry.message.usage
        if ($entry.message.role -ne 'assistant' -or $null -eq $usage) { continue }
        if ([string]$entry.message.model -eq '<synthetic>') { continue }
        $turn = [long]$usage.input_tokens + [long]$usage.cache_creation_input_tokens + [long]$usage.cache_read_input_tokens + [long]$usage.output_tokens
        if ($turn -le 0) { continue }
        $tokens = $turn
        if ($entry.message.model) { $model = [string]$entry.message.model }
        break
    }
    if ($null -eq $tokens) { return $result }

    $agentType = 'unknown'
    $metaPath = [IO.Path]::ChangeExtension($File.FullName, 'meta.json')
    if (Test-Path -LiteralPath $metaPath -PathType Leaf) {
        try { $meta = Get-Content -LiteralPath $metaPath -Raw | ConvertFrom-Json; if ($meta.agentType) { $agentType = [string]$meta.agentType } } catch { }
    }
    if (-not $task) {
        $w = [regex]::Match([string]$firstCwd, '[\\/]\.claude[\\/]worktrees[\\/](?<n>[^\\/]+)')
        if ($w.Success) { $task = $w.Groups['n'].Value }
        else {
            $session = Split-Path -Leaf (Split-Path -Parent (Split-Path -Parent $File.FullName))
            $task = 'session-' + $session.Substring(0, [Math]::Min(8, $session.Length))
        }
    }
    $clean = { param([string]$v) ($v -replace '[,"\r\n]', ' ').Trim() }
    $stamp = $lastUtc.ToString('yyyy-MM-ddTHH:mm:ssZ', $inv)
    $result.Row = '{0},{1},{2},{3},{4},"reconstructed agent-{5}"' -f $stamp, (& $clean $agentType), (& $clean $task), (& $clean $model), $tokens, (& $clean $agentId)
    return $result
}

function Get-TranscriptFiles([DateTime]$FromDay) {
    if (-not (Test-Path -LiteralPath $TranscriptRoot -PathType Container)) { throw "Transcript root not found: $TranscriptRoot" }
    # A transcript's last line is never later than its write time, so an older write time is outside any window.
    Get-ChildItem -LiteralPath $TranscriptRoot -Recurse -Filter 'agent-*.jsonl' -File |
        Where-Object { $_.Directory.Name -eq 'subagents' -and $_.LastWriteTimeUtc -ge $FromDay }
}

function Read-LedgerState {
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $dayCounts = @{}
    if (Test-Path -LiteralPath $Ledger -PathType Leaf) {
        foreach ($line in [IO.File]::ReadLines($Ledger)) {
            $m = [regex]::Match($line, 'agent-(?<id>[0-9A-Za-z]+)"?\s*$')
            if ($m.Success) { [void]$ids.Add($m.Groups['id'].Value) }
            if ($line.Length -ge 10 -and $line -match '^(?<d>\d{4}-\d{2}-\d{2})T') { $dayCounts[$Matches['d']] = 1 + [int]$dayCounts[$Matches['d']] }
        }
    }
    [pscustomobject]@{ Ids = $ids; Days = $dayCounts }
}

if ($CheckDays -gt 0) {
    $Days = $CheckDays
    $today = (Get-Date).ToUniversalTime().Date
    $start = $today.AddDays(-($Days - 1))
    $state = Read-LedgerState
    $active = @{}
    foreach ($file in Get-TranscriptFiles $start) {
        $t = Read-Transcript $file
        if ($null -ne $t.LastUtc -and $t.LastUtc -ge $start) { $active[$t.LastUtc.ToString('yyyy-MM-dd', $inv)] = 1 + [int]$active[$t.LastUtc.ToString('yyyy-MM-dd', $inv)] }
    }
    $warned = 0
    foreach ($day in ($active.Keys | Sort-Object)) {
        if (-not $state.Days.ContainsKey($day)) { $warned++; "WARN $day has $($active[$day]) subagent transcript(s) but no ledger row in $Ledger" }
    }
    "checked last $Days day(s): $($active.Count) active, $warned without ledger rows"
    exit 0
}

if ([string]::IsNullOrWhiteSpace($From)) { throw '-From is required (yyyy-MM-dd).' }
$fromDay = ConvertTo-UtcDay $From
$toDay = if ([string]::IsNullOrWhiteSpace($To)) { (Get-Date).ToUniversalTime().Date } else { ConvertTo-UtcDay $To }
$toExclusive = $toDay.AddDays(1)
if ($toDay -lt $fromDay) { throw '-To is before -From.' }

$state = Read-LedgerState
$rows = [Collections.Generic.List[object]]::new()
$skipped = 0; $already = 0; $outside = 0
foreach ($file in Get-TranscriptFiles $fromDay) {
    $t = Read-Transcript $file
    if ($null -eq $t.LastUtc -or $t.LastUtc -lt $fromDay -or $t.LastUtc -ge $toExclusive) { $outside++; continue }
    if ($state.Ids.Contains($t.AgentId)) { $already++; continue }
    if ($null -eq $t.Row) { $skipped++; continue }
    $rows.Add([pscustomobject]@{ Stamp = $t.LastUtc; Row = $t.Row })
}
$sorted = @($rows | Sort-Object Stamp, Row)

"window: $($fromDay.ToString('yyyy-MM-dd', $inv)) .. $($toDay.ToString('yyyy-MM-dd', $inv)) (UTC, inclusive)"
"rows to add: $($sorted.Count)"
"skipped: $skipped"
"already in ledger: $already"
"outside window: $outside"
'per day:'
$sorted | Group-Object { $_.Stamp.ToString('yyyy-MM-dd', $inv) } | Sort-Object Name | ForEach-Object { "  $($_.Name): $($_.Count)" }

if (-not $Append) {
    'first rows (dry run, nothing written):'
    $sorted | Select-Object -First 5 | ForEach-Object { "  $($_.Row)" }
    exit 0
}
if ($sorted.Count -eq 0) { 'nothing to append'; exit 0 }

New-Item -ItemType Directory -Force (Split-Path -Parent $Ledger) | Out-Null
$stream = [IO.File]::Open($Ledger, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $reader = [IO.StreamReader]::new($stream, [Text.UTF8Encoding]::new($false), $true, 1024, $true)
    $existing = $reader.ReadToEnd()
    $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false), 1024, $true)
    if ($existing.Length -eq 0) { $writer.Write("timestampUtc,agent,task,model,subagentTokens,notes`n") }
    elseif (-not $existing.EndsWith("`n")) { $writer.Write("`n") }
    # Re-check the ids under the lock: a row written since the dry-run read (a hook stop, a second run) is not doubled.
    $fresh = @($sorted | Where-Object { -not ($existing -match ('agent-' + [regex]::Escape(($_.Row -replace '^.*agent-', '').TrimEnd('"')) + '"\s*(\r?\n|$)')) })
    foreach ($r in $fresh) { $writer.Write($r.Row + "`n") }
    $writer.Flush()
}
finally { $stream.Dispose() }
"appended $($fresh.Count) row(s) to $Ledger"
